# Modernization Decision Register

A living register of **cross-cutting decisions not yet made** for positioning this repo as a
**.NET 10 successor to [kgrzybek/modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd)**.

Unlike an ADR (which records a decision already taken), this file tracks decisions that are
**deferred until a work item makes them live**. Each entry names the trigger that makes it
relevant, a current lean (if any), and the ADR it will graduate into once decided.

**How to use it (Claude + user):**
- **When starting a work item / story:** scan this register for entries whose *Becomes live when*
  matches the work. Surface those so the decision is made deliberately, in context — not
  defaulted silently mid-implementation.
- **When completing a work item:** if the work forced or informed one of these, promote it —
  write the ADR, flip the entry to ✅ Decided, and link the ADR.
- Entries here are **not** backlog Items. Skills to practice still live in
  [roadmap.md](../roadmap.md) / [backlog.md](../backlog.md); this tracks *architectural/tooling
  choices* for the reference implementation itself.

Status legend: 🔷 Open (genuinely undecided) · 🧭 Leaning (a recommended direction, not ratified) ·
✅ Decided (ratified → ADR).

_Created 2026-08-16. Update the `Updated` line on any change._
Updated: 2026-08-19

---

## Positioning (why this register exists)

Three deliberate differentiators from the original:
1. Demonstrates **my** modeling + spec→test→code workflow (not just a generic sample).
2. **Logistics** domain (orders/fulfillment/shipping/inventory/tracking/billing) — underrepresented online.
3. **Current** .NET 10 ecosystem — the original's stack (custom outbox on Quartz, MediatR/AutoMapper,
   Serilog+ELK, MVC controllers, owned-entity value objects) predates most of what's below.

A defining external fact driving several entries: **the Jimmy Bogard / Chris Patterson library
ecosystem the original leaned on has gone commercial** — MediatR + AutoMapper (commercial editions
July 2025, Lucky Penny Software; old versions frozen at MIT) and MassTransit (v9 commercial, official
Q1 2026; v8 Apache-2.0 maintained only through end of 2026). A copy-me reference repo should be
**license-clean**, which reshapes the mediation/messaging/mapping choices.

---

## Register (index)

| ID | Decision | Status | Becomes live when |
|----|----------|--------|-------------------|
| MOD-01 | License-clean library posture (no commercial MediatR/AutoMapper/MassTransit deps) | 🧭 Leaning | Reaching for any of these packages |
| MOD-02 | Mediation + messaging + outbox strategy (Wolverine vs no-mediator vs source-gen dispatcher) | 🔷 Open | First cross-module integration event / outbox (Phase 2) |
| MOD-03 | Object mapping: explicit / Mapperly (drop AutoMapper) | 🧭 Leaning | First DTO / read-model mapping |
| MOD-04 | Module internal structure: layered vs vertical slice (plan: do **both**, as a comparison) | 🔷 Open | Building the 2nd module (target: Inventory) — see ADR-0006 |
| MOD-05 | Value-object persistence: EF Core complex types vs owned entities | 🧭 Leaning | First EF persistence of the Ordering aggregate |
| MOD-06 | Strongly-typed IDs (Vogen / source-gen) vs primitives | 🧭 Leaning | First persisted aggregate / API contract |
| MOD-07 | Database engine: PostgreSQL vs SQL Server | 🧭 Leaning | First persistence layer |
| MOD-08 | API layer: Minimal APIs + native OpenAPI vs controllers vs FastEndpoints | 🧭 Leaning | First HTTP endpoint |
| MOD-09 | Observability: OpenTelemetry as first-class (traces/metrics/logs) | 🧭 Leaning | Wire early; hardened in Phase 4 |
| MOD-10 | .NET Aspire for orchestration / local dev / dashboard | 🧭 Leaning | More than one process/dependency (DB, broker) exists |
| MOD-11 | Integration tests on Testcontainers (real DB/broker per run) | 🧭 Leaning | First integration test needing real infra |
| MOD-12 | Architecture/boundary tests (NetArchTest / ArchUnitNET) | 🧭 Leaning | Second module exists (boundaries to enforce) |
| MOD-13 | Transport-swappable integration events (in-proc seam → broker later) | 🧭 Leaning | First cross-module event |
| MOD-14 | Resilience via `Microsoft.Extensions.Resilience` (Polly v8) | 🧭 Leaning | First outbound/carrier/3PL call |
| MOD-15 | Caching via `HybridCache` | 🧭 Leaning | Phase 3 read side |
| MOD-16 | Background processing: `BackgroundService` + Channels vs Quartz/Hangfire | 🧭 Leaning | Building the outbox processor / async workers |
| MOD-17 | Result pattern over exceptions for domain errors | ✅ Decided | — (in force; see ADR-0005) |
| MOD-18 | Container publish via .NET SDK (no Dockerfile) | 🧭 Leaning | First deployable artifact |
| MOD-19 | Source generators: `[LoggerMessage]`, System.Text.Json source-gen | 🧭 Leaning | Hot-path / performance pass |
| MOD-20 | Module extractability contract & deployment posture (host-agnostic; OCI/ACA target, IIS non-goal) | 🧭 Leaning | Anchor for extraction/deploy decisions — see ADR-0008 |

---

## Entries

### MOD-01 — License-clean library posture · 🧭 Leaning
**Question:** Do we forbid runtime dependencies on the now-commercial MediatR / AutoMapper /
MassTransit (and similar) in the reference implementation?
**Why it matters:** A repo meant to be *copied* shouldn't hand readers a licensing liability or a
frozen-MIT dead end. Also a teaching opportunity — "here's the modern license-clean way."
**Lean:** Yes. Prefer OSS-permissive or first-party (Microsoft.Extensions.\*) building blocks.
Feeds MOD-02, MOD-03, MOD-16.
**Becomes live when:** any time one of these packages is the reflexive choice.
**Lands in:** a short ADR stating the posture (or folded into MOD-02's ADR).

### MOD-02 — Mediation + messaging + outbox strategy · 🔷 Open
**Question:** What plays the role MediatR + a hand-rolled outbox played in the original?
**Options:**
- **(a) Wolverine for everything** — one handler model for in-process dispatch *and* async
  messaging, with a first-class transactional outbox/inbox. Best fit for the "modular monolith that
  can become distributed" centerpiece; a larger, opinionated runtime to learn.
- **(b) No mediator** — inject application handlers directly; cross-cutting concerns via Minimal API
  endpoint filters; a small/OSS outbox for the async half. Cleanest if we reject Wolverine.
- **(c) Source-gen / hand-rolled dispatcher** (e.g. martinothamar `Mediator`) — keeps the MediatR
  programming model, license-clean. Narrowest justification; only if we want that model *and* reject
  Wolverine. Risk: slowly rebuilding MediatR via pipeline behaviors.
**Key insight from discussion (2026-08-16):** the mediator's real value was the **pipeline behaviors**
(validation, unit-of-work, idempotency), not the dispatch. And Wolverine unifies in-proc + async, so
if it's adopted for messaging, using it as the mediator too is the coherent call — a *separate*
dispatcher alongside Wolverine would be redundant. So the live question reduces to **"adopt Wolverine
or not?"** The user has no prior experience with (a)/(b)/(c) — evaluating them is itself a goal.
**Lean:** (a) Wolverine, *given* the distributed-evolution story is the centerpiece — but hold as Open
until we build the first cross-module flow and can feel the trade-off.
**Becomes live when:** first integration event / outbox need (roadmap Phase 2).
**Lands in:** ADR "Mediation & messaging without MediatR/MassTransit." Depends on MOD-01, MOD-13.

### MOD-03 — Object mapping · 🧭 Leaning
**Question:** How do we map domain ↔ DTO / read models without AutoMapper?
**Lean:** Explicit mapping, or **Mapperly** (source-gen) for boilerplate-heavy cases. Reflection-based
object mapping has fallen out of senior favor independent of licensing.
**Becomes live when:** first DTO or CQRS read-model projection.
**Lands in:** likely a note in the MOD-02 ADR or its own short ADR.

### MOD-04 — Module internal structure: layered vs vertical slice · 🔷 Open
**Question:** The original is layered per module. Keep that, adopt vertical slice (VSA), or a hybrid?
**Why it matters:** Where the "my modeling/dev practices" differentiator earns its keep; shapes every
module's internal structure; should be a *deliberate, defended* stance.
**Plan (2026-08-16):** Don't pick one globally — **demonstrate both**, since a modular monolith grants
each module its own internal structure. **Ordering** finishes as-is in the user's **layered/Clean**
convention (layers as the top-level cut: Application organized by use case e.g. `CreateOrderHandler`;
Domain by entity/aggregate e.g. `Orders` ns with `Order` + `IOrderRepository`; Infrastructure mostly by
entity; a shared/common ns per layer). A **later module is built vertical-slice** as a side-by-side
comparison for repo viewers — a teaching artifact the original repo lacks.
**Target for the VSA module:** **Inventory** (reservation / optimistic concurrency / overselling) — real
write-side complexity, so the comparison is a fair fight, not layered-DDD vs a trivial CRUD folder.
(Tracking is the alternative "VSA where it shines" read-heavy showcase, but less apples-to-apples.)
**Guardrails so it teaches rather than misleads:**
- Keep the VSA module an **honest** VSA — a rich write-side domain is still allowed; VSA drops the
  *ceremony* (repository abstraction, mapping layers) and colocates endpoint→handler→persistence. Do
  **not** let it degrade into an anemic transaction-script-in-a-folder.
- **Comparable domain complexity** to Ordering, else line-count "wins" are an artifact of a trivial domain.
- **Document intent**: a per-module README + the ADR, listing the comparison axes (where a use case's
  code lives; abstraction count per feature; change blast radius; testability; onboarding cost; how
  cross-cutting is handled — Domain/App services vs pipeline behaviors).
**Canon:** source #3 (Clean/layered) is ✅ Confirmed *but contested*, with Bogard's VSA (#6) ⏳ Pending as
the named opposing school; per canon operating-rule 2/3, a contested/opposing-school choice is **settled
by an ADR** — so "do both, deliberately" *is* the canonical resolution, not a dodge.
**Interacts with:** MOD-02 — VSA leans on a mediator/pipeline for cross-cutting behaviors, so the VSA
module is a natural place to feel out the Wolverine-vs-not question in context.
**Status:** 🔷 Open — plan recorded; ratify (→ ✅, write the Decision) when the VSA module is actually built.
**Lands in:** [ADR-0006](../../docs/decisions/ADR-0006-module-internal-structure-layered-vs-vertical-slice.md) (Proposed).

### MOD-05 — Value-object persistence · 🧭 Leaning
**Question:** Map value objects (`Money`, `PostalCode`, `StreetAddress`, `AddressState`) as EF Core
**complex types** (EF 8+) or the old **owned entities**?
**Lean:** Complex types — the modern, correct replacement for the owned-entity hack the original used.
**Becomes live when:** first EF persistence of the Ordering aggregate.
**Lands in:** ADR "Persisting value objects with EF Core complex types."

### MOD-06 — Strongly-typed IDs · 🧭 Leaning
**Question:** Wrap identifiers (OrderId, SkuId, MerchantId) in strongly-typed IDs, or use primitives?
**Lean:** Adopt — via **Vogen** or a `StronglyTypedId` source generator. Kills primitive obsession;
strong DDD signal. Requires EF value converters + JSON/serialization + model-binding wiring — worth an
ADR because that wiring is the interesting part.
**Becomes live when:** first persisted aggregate or API contract.
**Lands in:** ADR "Strongly-typed IDs across persistence and transport."

### MOD-07 — Database engine · 🧭 Leaning
**Lean:** **PostgreSQL** over SQL Server — closer to modern logistics shops, Testcontainers-friendly,
free. Keep the CQRS read/write split (e.g. Dapper reads / EF writes) if desired; that's still sound.
**Becomes live when:** first persistence layer.
**Lands in:** brief ADR (engine + rationale). Data ownership / schema-per-module is clause B.3 of the
extractability contract (MOD-20 / ADR-0008).

### MOD-08 — API layer · 🧭 Leaning
**Lean:** **Minimal APIs** with route groups, endpoint filters, `TypedResults`, and **native OpenAPI**
(`Microsoft.AspNetCore.OpenApi`; Swashbuckle dropped from default templates). FastEndpoints (REPR) is
the fallback if we want more structure without controllers.
**Becomes live when:** first HTTP endpoint.
**Lands in:** ADR "API surface: Minimal APIs + native OpenAPI."

### MOD-09 — Observability (OpenTelemetry) · 🧭 Leaning
**Lean:** OTel as first-class — `ActivitySource` traces, `System.Diagnostics.Metrics`, OTLP export;
propagate context **across the async/messaging boundary** (the hard, high-value part for logistics:
trace an order through Ordering → Fulfillment → Shipping). Wire early (Aspire service defaults), harden
in Phase 4. Replaces the original's Serilog+ELK+custom-correlation approach.
**Becomes live when:** as soon as there's a request path worth tracing.
**Lands in:** ADR; also a roadmap Item (already listed as an OBS candidate). Context propagation across
the async boundary is clause B.5 of the extractability contract (MOD-20 / ADR-0008).

### MOD-10 — .NET Aspire · 🧭 Leaning
**Question:** Use Aspire for local orchestration, service defaults (OTel/health/resilience wiring), and
the dashboard?
**Lean:** Adopt — makes the repo runnable in one command (huge for a clone-to-learn repo) and wires
much of MOD-09/MOD-14 for free. Cost: an Aspire dependency + AppHost project to learn.
**Becomes live when:** more than one process/dependency exists (DB, broker, worker).
**Lands in:** ADR "Local orchestration & service defaults via .NET Aspire."

### MOD-11 — Integration tests on Testcontainers · 🧭 Leaning
**Lean:** Adopt — real Postgres/RabbitMQ per test run, killing shared-DB flakiness. Complements the
existing NUnit 4 + Moq unit conventions (see [testing.md](../../docs/testing.md)); this is the integration tier.
**Becomes live when:** first integration test that needs real infra.
**Lands in:** extend testing.md + a short ADR.

### MOD-12 — Architecture / boundary tests · 🧭 Leaning
**Lean:** Adopt **NetArchTest** or **ArchUnitNET** in CI to enforce that modules talk only via published
contracts (no reaching into another module's internals). This is what *keeps* a modular monolith modular
— strong staff-level signal.
**Becomes live when:** a second module exists (there are boundaries to protect).
**Lands in:** ADR + CI wiring.

### MOD-13 — Transport-swappable integration events · 🧭 Leaning
**Question:** Make the in-process integration-event bus a seam that can become RabbitMQ/Service Bus
without touching module code?
**Why it matters:** The "monolith-first, extract-when-proven" evolution is the whole *point* of the
modular monolith in 2025 and is underexplained online — a core narrative for this repo.
**Lean:** Yes, as a design principle. Interacts with MOD-02 (Wolverine provides this seam natively).
This seam is clause B.2 of the extractability contract (MOD-20 / ADR-0008) — the load-bearing one.
**Becomes live when:** first cross-module event.
**Lands in:** folded into MOD-02's ADR or its own.

### MOD-14 — Resilience · 🧭 Leaning
**Lean:** `Microsoft.Extensions.Resilience` / `Microsoft.Extensions.Http.Resilience` (Polly v8 rebuilt)
— retry+jitter, circuit breaker, timeout/bulkhead. Already a roadmap RESIL theme.
**Becomes live when:** first outbound call to a carrier/3PL/other module over HTTP.
**Lands in:** ADR + roadmap Items.

### MOD-15 — Caching · 🧭 Leaning
**Lean:** `HybridCache` (.NET 9) — L1+L2 with stampede protection — for read-side/reference data.
**Becomes live when:** Phase 3 read side / hot reference lookups.
**Lands in:** brief ADR.

### MOD-16 — Background processing · 🧭 Leaning
**Lean:** `BackgroundService` + `System.Threading.Channels` for in-process producer/consumer and the
outbox processor, instead of Quartz/Hangfire — unless durable *scheduling* is genuinely needed.
**Becomes live when:** building the outbox processor or async workers.
**Lands in:** folded into MOD-02's ADR.

### MOD-17 — Result pattern over exceptions · ✅ Decided
Domain/application failures flow through `Result<T>` + `Error`/`ErrorType`, not exceptions. Already in
force. **See [ADR-0005](../../docs/decisions/ADR-0005-error-taxonomy-order-estimation.md)** (and the error-taxonomy ADR
lineage). Kept here as the anchor entry so the register reflects the full stance.

### MOD-18 — Container publish via SDK · 🧭 Leaning
**Lean:** `dotnet publish /t:PublishContainer` — no Dockerfile. Low priority until there's a deployable.
OCI container is the LCD deployment unit in the extractability posture (MOD-20 / ADR-0008, clause B.7).
**Becomes live when:** first deployable artifact / CI publish step.

### MOD-19 — Source generators for hot paths · 🧭 Leaning
**Lean:** `[LoggerMessage]` logging + System.Text.Json source-gen contexts; signals a 2025 codebase and
avoids reflection on hot paths. Low priority; a polish pass.
**Becomes live when:** a performance/hot-path pass, or when logging volume grows.

### MOD-20 — Module extractability contract & deployment posture · 🧭 Leaning
**Question:** What must be true of every module so a future split into its own process is a
*deployment change, not a rewrite* — and what deployment targets does that imply (Azure Container
App? IIS web app?)?
**Key reframe:** a module in the monolith is class libraries with no deployment target; it gains one
only when wrapped in a host. So we don't design *for* a target — we design for **extractability** and
bind the target late. The lowest common denominator is an **OCI (linux) container**.
**Decision (in ADR-0008):**
- **Extraction is demand-driven** along modeled bounded-context seams (independent scaling, deploy
  cadence/team autonomy, fault isolation, divergent runtime/compliance/SLA) — monolith-first, no
  pre-splitting; the first extraction is expected to be a **worker**, not a request/response module.
- **The extractability contract** every module holds from day one: host-agnostic code (no
  `System.Web`/`HttpContext`/`web.config`), cross-module comms only via the transport-swappable seam
  (MOD-13), data ownership / schema-per-module (MOD-07), hostile-from-day-one integration
  (idempotent, at-least-once, inbox/outbox — MOD-16/MOD-02), OTel across the boundary (MOD-09),
  stateless + graceful shutdown + health endpoints, container-publishable (MOD-18). Enforced by
  architecture tests (MOD-12).
- **Deployment posture:** target OCI containers; **Azure Container Apps** primary (Container Apps
  Jobs/Functions for workers, AKS situational, App Service fallback); **IIS/on-prem Windows a
  non-goal** but kept *possible for free* by the host-agnostic clause. Per-module `.Api` +
  `Add{Module}`/`Map{Module}` (MOD-08) makes extraction a ~20-line host; **Aspire (MOD-10)** rehearses
  the multi-process topology locally.
**Status:** 🧭 Leaning — ADR-0008 is **Proposed**; ratifies (→ ✅) when the first module is actually
extracted or the first cross-module event is built and the contract is tested against reality.
**Binds:** MOD-13, MOD-07, MOD-09, MOD-18, MOD-10, MOD-16, MOD-02, MOD-12; relies on MOD-08.
**Lands in:** [ADR-0008](../../docs/decisions/ADR-0008-module-extractability-contract-and-deployment-posture.md) (Proposed).

---

## Graduation checklist (when a decision goes ✅)
1. Write the ADR (`ADR-000N-*.md`), following the ADR-0005 shape (Context / Decision / Consequences /
   Alternatives / Revisit when / Related).
2. Flip the entry's status to ✅ Decided and link the ADR.
3. If it spawned skills to practice, add roadmap/backlog Items via the dedup flow.
4. Bump this file's `Updated:` line.
