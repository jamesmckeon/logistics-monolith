# ADR-0002 — Module extractability contract

## Status

Accepted — 2026-10-11 (proposed 2026-08-19).

Not yet met: the host does not collect messaging spans (clause B.5), no health or readiness
endpoints exist (clause B.6), and container publishing (clause B.7) has not been exercised.

## Context

This repo is positioned as a **.NET 10 successor to
[kgrzybek/modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd)**, and
its central narrative is **monolith-first, extract-when-proven**: modules live in one process now
and split into their own processes only when a concrete force justifies it. For that evolution to
be a *deployment change* rather than a *rewrite*, extractability has to be a designed-in property
held from the **first** module — the couplings that block extraction (shared tables, in-process
assumptions, reliable-delivery assumptions) are cheap to forbid early and ruinously expensive to
unwind later.

A module inside the monolith is a set
of class libraries with no deployment target of its own.It gains one only when wrapped in a
host. 

This ADR settles two things that were otherwise left implicit:

1. **What justifies extracting a module** — so we neither pre-split nor drift into a distributed
   big-ball-of-mud, and
2. **The host-agnostic contract every module must satisfy** — which is also the real answer to
   "what tech stacks must a module be compliant with."

## Decision

### A. Extraction is demand-driven, along modeled bounded-context seams

Extract a module only when a **concrete force** outweighs the operational cost of running another
process (Newman, *Monolith to Microservices*: extract when the coupling cost of keeping it in
exceeds the operational cost of pulling it out). Do **not** pre-split. The recognised drivers:

- **Independent scaling** — a module's load profile diverges (Tracking ingest vs transactional Ordering).
- **Independent deploy cadence / team autonomy** (Conway) — a team needs to ship without a monolith release.
- **Fault isolation** — a crash-prone or risky module (flaky 3PL/carrier calls) must not sink order capture.
- **Divergent runtime / resource needs** — CPU-bound routing/optimization wants its own scaling, maybe its own runtime.
- **Compliance / data residency** — a regulatory boundary (PCI on Billing, EU shipment data) forces a process boundary.
- **Divergent SLA / availability** — 99.99% Tracking-ingest vs 99% internal reporting.

Most drivers are organisational or scaling forces, **not** "the code got big." Expect the **first**
extraction to be a **worker** (outbox / high-volume ingest) — stateless, async, clearest scaling
driver, lowest risk — not a request/response module.

### B. The extractability contract — every module complies from day one

1. **Host-agnostic module code.** No host-specific APIs: no `System.Web`, no `HttpContext` past the
   module's `Presentation` layer into Application/Domain, no `web.config`-driven behaviour.
   Configuration comes from `IConfiguration` / env vars / `IOptions`, never machine config.
2. **Cross-module communication only through the transport-swappable seam**: in-process
   today, RabbitMQ / Azure Service Bus tomorrow, **same contract**. No module reaches into another
   module's internals — inbound cross-module calls go through its published `*.Contracts`.
3. **Data ownership**: schema-per-module in one Postgres now → database-per-module on
   extraction. No cross-module foreign keys, joins, or shared writes. Shared tables are un-splittable.
4. **Integration is hostile-from-day-one**: at-least-once, idempotent,
   duplicate- and reorder-tolerant, inbox/outbox. The in-proc version must **not** assume reliable,
   ordered, exactly-once delivery — the network will violate every one of those on extraction.
5. **Observability crosses the async boundary**: trace context travels with every integration
   event and the host collects the messaging spans, so a handler's work appears in the same trace
   as the request that caused it — within one process now, across processes after extraction,
   with no code change.
6. **Stateless + graceful shutdown**: no in-process session state; `BackgroundService`s honour
   cancellation (drain); every host exposes health and readiness endpoints. Required for
   horizontal scale and a clean shutdown when a host is stopped.
7. **Container-publishable**: `dotnet publish /t:PublishContainer`, linux, non-root — no Dockerfile.

**What extraction then costs.** Because each module owns its endpoints and exposes
`Add{Module}()` / `Map{Module}()` extensions, extracting a module is a **new ~20-line host** that
references that one module and calls those two extensions.

## Consequences

**Positive**

- Extraction is a deployment change, not a rewrite.
- The contract forces the right distributed-systems habits (idempotency, data ownership, context
  propagation) *early*, while they are cheap — instead of discovering them under load after a split.
- Every future module gets a concrete compliance checklist; "is this module extractable?" becomes testable.

**Costs / implications**

- **Hostile-from-day-one integration is real up-front work** for what is, today, an in-process call
  that "could" be a plain method invocation. Accepted: it is the load-bearing clause — skipping it is
  the thing that turns extraction into a rewrite.
- **Schema-per-module forbids convenient cross-module joins.** Data another module owns is reached via
  contracts / read models, not a SQL join. This is a genuine ergonomic cost paid for splittability.
- **OTel-across-the-boundary must exist before it visibly pays off** — wiring cost with deferred benefit.

**Alternatives considered**

- **Design for the monolith only; refactor to extract if/when needed (pure YAGNI).** Rejected: the
  couplings that block extraction (shared tables, reliable-delivery assumptions, in-proc reach-ins) are
  precisely the ones that are ruinously expensive to unwind later. The contract is cheap to hold now,
  dear to retrofit — this is the rare case where a little up-front design is the YAGNI-honest choice.
- **Pre-split into microservices now.** Rejected: contradicts the monolith-first positioning and pays
  the distributed-systems tax before any driver justifies it.
- **Centralised presentation (kgrzybek's single `API` project).** Rejected: a shared host becomes the
  carve-out point on extraction, whereas per-module presentation means the module's HTTP surface
  leaves with the module.

**Revisit when**

- The first module is actually extracted (amend the contract against what reality demanded).
- A driver appears that section A's list does not cover.

## Related

- **Per-module presentation** — the `Presentation` + `Add{Module}`/`Map{Module}` structure this ADR
  relies on to make extraction a ~20-line host.
- kgrzybek/modular-monolith-with-ddd — the reference implementation this repo modernizes.
