# ADR-0001 — Practice domain, architecture, and stack for api-lab

- **Status:** Accepted
- **Date:** 2026-07-28
- **Deciders:** Jamesey (engineer), Claude (mentor)
- **Related items:** T-001–T-016 (whole backlog); this ADR is also the first rep on **T-014 (ADRs)**
- **Supersedes / superseded by:** —

---

## Context

api-lab exists to build the judgment expected of a **Staff / Principal .NET engineer** on a
team that builds **modern, high-volume logistics software**. The backlog's center of mass is
distributed-data and failure-mode reasoning: outbox/inbox, sagas, idempotency, delivery
semantics, optimistic/pessimistic concurrency, CQRS, contract evolution, resilience, SLOs.

Three decisions had to be made before any code is written:

1. **What business domain** should the practice APIs model?
2. **What architecture** should they take (the "microservices vs not" question)?
3. **What concrete stack** should they run on?

Constraints and forces:

- The engineer's background is entirely **Microsoft-stack** (SQL Server, Azure, ASP.NET
  Core). New *concepts* are the goal; new *tooling* is friction that competes with learning.
- The target skills are **stack-independent** — an outbox is an outbox regardless of database
  or broker. Marketability comes from the patterns and the trade-off reasoning, not the tools.
- Staff-bar interviews probe *why you did **not** distribute something* at least as hard as
  they probe distributed mechanics. Premature microservices is a mild negative signal at
  mature, strong-engineering-culture orgs.
- The program must be **incremental**: each concept practiced in isolation via one small
  story, with tooling introduced only when a concept demands it.

## Decision

**1. Domain — a small "fulfillment platform" (WMS/OMS territory), not a WCS.**
Model the transactional heart of high-volume logistics across a fixed set of bounded
contexts, kept consistent across all stories:

- **Ordering** — entry point; accepts orders. *(idempotency, domain events)*
- **Inventory** — stock, reservations, allocation. **Concurrency/correctness showcase.**
- **Fulfillment/Warehouse** — pick/pack/ship workflow (the WMS core). *(sagas, aggregates)*
- **Shipping/Carrier** — rating, labels, carrier integration. *(ACL, async request-reply)*
- **Tracking** — scan/carrier event ingestion. **High-volume / read-side showcase.**
- **Billing** — charge/settle. *(the "never double-charge" that forces idempotency + saga)*

The spine scenario is `order → reserve → pick → ship → bill`, surviving partial failure at
each hop — the single flow that exercises the most backlog items at once.

**2. Architecture — a modular monolith designed to shed services.**
- One deployable ASP.NET Core app, partitioned into the six contexts as separately-compiled
  modules with **enforced boundaries** (no cross-module references except through published
  contracts / integration events; each module owns its tables).
- Module-to-module communication goes through a **transport-agnostic integration-event bus**:
  in-process today, a real broker tomorrow, with the *same* outbox/inbox/saga code either way.
- Later, peel off exactly **one** service (Tracking, or a WCS-flavored sortation slice) onto a
  real broker + gRPC to earn genuine independent-deploy, contract-versioning, and cross-process
  failure practice.

**3. Stack — stay on the Microsoft stack.**
- **.NET 10 + ASP.NET Core** (Minimal APIs at the edge), **EF Core 10** (a DbContext per
  module), **SQL Server**, **Azure Service Bus** (via MassTransit), **OpenTelemetry**,
  **Testcontainers** for integration tests.
- Introduce heavier tooling (.NET Aspire, gRPC, a split deployable) **only when a specific
  story requires it** — not up front.

## Consequences

**Positive**
- Every backlog cluster has a natural home; stories compose into something resembling a real
  platform rather than disconnected katas.
- Learning cost per exercise is minimized: the only new thing is usually the *concept*, since
  the tools are already familiar. SQL Server `rowversion` and Azure Service Bus dedup/sessions/
  DLQ are, in fact, clean teaching vehicles for concurrency (T-010) and delivery semantics (T-008).
- The modular monolith generates the strongest Staff artifact available: a defensible answer to
  *"which boundary did you refuse to distribute, and what force would make you pay that cost?"*
- Aligns with where a large share of .NET Staff roles actually live (Azure shops).

**Negative / costs**
- SQL Server + Azure Service Bus is marginally less pedagogically "pure" for a couple of
  concurrency primitives than Postgres advisory locks. Mitigation: optionally do one later
  Postgres exercise, and stay *conversant* on the trade-off for interviews.
- A modular monolith hides some real distributed-systems pain (true network partitions, partial
  broker outages) until the first service is split. Mitigation: the transport-agnostic bus and
  the planned Tracking split are where that pain gets introduced deliberately.

## Alternatives considered

- **WCS (Warehouse Control System) as the domain** — rejected as the *center*. It exercises only
  a few backlog items (ordering/partitioning, backpressure, device state machines) and requires a
  physical-layer simulator (accidental complexity), and the target job market skews toward
  business-platform work. Retained as an *optional accent*: one sortation/ingest slice later.
- **"Microservices via Web API" from day one** — rejected. Front-loads operational tax (brokers,
  meshes, distributed tracing plumbing) before it teaches anything, and signals the wrong
  judgment. Replaced by modular-monolith-first with a single deliberate service split.
- **Postgres + RabbitMQ** — rejected *for this engineer*. Adds a second unfamiliar variable per
  exercise with no gain in the marketable (stack-independent) skill.

## Follow-ups

- First stories should target Phase 0–1 of the roadmap (Hexagonal → Aggregates → Domain Events →
  Idempotency → concurrency), each as a small standalone spec.
- Revisit this ADR (as ADR-000N) if a concrete target employer's stack differs materially
  (e.g., a genuine k8s/microservices shop, or a Postgres shop).
