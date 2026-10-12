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
Updated: 2026-10-11

> **ADR numbering (2026-10-11).** The user cut the ADR set to three and renumbered them:
> [ADR-0001](../../docs/decisions/ADR-0001-endpoints-report-validation-failures-together.md) endpoints report validation failures together (was 0007),
> [ADR-0002](../../docs/decisions/ADR-0002-module-extractability-contract.md) module extractability contract (was 0008), and ADR-0003 authorization (was 0009).
> The old ADRs 0001–0006 were deleted. ADR numbers in older commits and issues refer to the old set.

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
| MOD-01 | License-clean library posture (no commercial MediatR/AutoMapper/MassTransit deps) | ✅ Decided | In force — checked 2026-10-11 |
| MOD-02 | Mediation + messaging + outbox strategy → **adopt Wolverine** | ✅ Decided | Decided 2026-09-16 (ADR pending) |
| MOD-03 | Object mapping: explicit / Mapperly (drop AutoMapper) | ✅ Decided | Explicit mapping in use — checked 2026-10-11 |
| MOD-04 | Module internal structure: layered vs vertical slice | 🔷 Open | Comparison plan withdrawn 2026-10-11; no ADR |
| MOD-05 | Value-object persistence: EF Core complex types vs owned entities | 🧭 Leaning | First EF persistence of the Ordering aggregate |
| MOD-06 | Strongly-typed IDs (Vogen / source-gen) vs primitives | ✅ Decided | Hand-written id types in use — checked 2026-10-11 |
| MOD-07 | Database engine: PostgreSQL vs SQL Server | ✅ Decided | PostgreSQL in use; named in ADR-0002 |
| MOD-08 | API layer: Minimal APIs + native OpenAPI vs controllers vs FastEndpoints | ✅ Decided | Minimal APIs + native OpenAPI in use — checked 2026-10-11 |
| MOD-09 | Observability: OpenTelemetry as first-class (traces/metrics/logs) | ✅ Decided | Traces + logs wired; gaps noted — checked 2026-10-11 |
| MOD-10 | .NET Aspire for orchestration / local dev / dashboard | 🧭 Leaning | More than one process/dependency (DB, broker) exists |
| MOD-11 | Integration tests on Testcontainers (real DB/broker per run) | ✅ Decided | Testcontainers Postgres in use — checked 2026-10-11 |
| MOD-12 | Architecture/boundary tests (NetArchTest / ArchUnitNET) | ✅ Decided | Not adopting — user, 2026-10-11 |
| MOD-13 | Transport-swappable integration events (in-proc seam → broker later) | ✅ Decided | In force via Wolverine; ADR-0002 clause B.2 |
| MOD-14 | Resilience via `Microsoft.Extensions.Resilience` (Polly v8) | 🧭 Leaning | First outbound/carrier/3PL call |
| MOD-15 | Caching via `HybridCache` | 🧭 Leaning | Phase 3 read side |
| MOD-16 | Background processing: `BackgroundService` + Channels vs Quartz/Hangfire | 🧭 Leaning | Building the outbox processor / async workers |
| MOD-17 | Result pattern over exceptions for domain errors | ✅ Decided | — (in force; see ADR-0001) |
| MOD-18 | Container publish via .NET SDK (no Dockerfile) | 🧭 Leaning | First deployable artifact |
| MOD-19 | Source generators: `[LoggerMessage]`, System.Text.Json source-gen | 🧭 Leaning | Hot-path / performance pass |
| MOD-20 | Module extractability contract (host-agnostic; extract on demand) | ✅ Decided | Accepted 2026-10-11 — see ADR-0002 |
| MOD-21 | Domain base types: shared equality base `ValueObject` + `Entity<TId> : ValueObject` (kgrzybek keeps two unrelated bases) | ✅ Decided | Decided 2026-09-19 (no ADR; folded here) |
| MOD-22 | Authorization model: Entra app roles assign roles; Throughline maps role → permission in code; endpoints check permissions | ✅ Decided | Revised 2026-10-10 (was server-side resolution, 2026-09-29) · [ADR-0003](../../docs/decisions/ADR-0003-authorization-functional-and-owner-roles.md) |

---

## Entries

### MOD-01 — License-clean library posture · ✅ Decided
**Question:** Do we forbid runtime dependencies on the now-commercial MediatR / AutoMapper /
MassTransit (and similar) in the reference implementation?
**Why it matters:** A repo meant to be *copied* shouldn't hand readers a licensing liability or a
frozen-MIT dead end. Also a teaching opportunity — "here's the modern license-clean way."
**Lean:** Yes. Prefer OSS-permissive or first-party (Microsoft.Extensions.\*) building blocks.
Feeds MOD-02, MOD-03, MOD-16.
**Becomes live when:** any time one of these packages is the reflexive choice.
**Lands in:** a short ADR stating the posture (or folded into MOD-02's ADR).
**Decided (2026-10-11):** in force. The README states the repo stays license-clean, and no MediatR, AutoMapper
or MassTransit package is referenced in `Directory.Packages.props`. No ADR.

### MOD-02 — Mediation + messaging + outbox strategy · ✅ Decided (adopt Wolverine · 2026-09-16 · ADR pending)
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

**Decision (2026-09-16):** Adopt **(a) Wolverine** (WolverineFx, MIT — license-clean, satisfies
MOD-01). Rationale: the distributed-evolution story is the centerpiece, and Wolverine unifies
in-proc dispatch + async messaging + a first-class EF Core **transactional outbox/inbox**, so it
provides the MOD-13 seam natively and **supersedes the hand-rolled `BackgroundService` outbox
processor** considered under MOD-16 (for the messaging outbox specifically). **How we got here:**
we began hand-rolling the outbox for `OrderConfirmed` (Ordering→Inventory) to *learn the mechanics*
— dual-write, outbox, idempotent consumer, at-least-once. Having internalized those, continuing to
hand-roll the plumbing (name↔type registry, JSON envelope, drainer loop) was judged low-ROI
incidental complexity: the concepts are the durable skill, the plumbing is what a mature library
should own. The hand-rolled analysis is retained as the ADR's before/after. **Still to do:** write
the ADR (concepts → what Wolverine replaces); decide endpoint integration under MOD-08
(Wolverine.HTTP vs Minimal API + `IMessageBus`) when wiring the producer. Wolverine APIs verified
via Context7 2026-09-16.

### MOD-03 — Object mapping · ✅ Decided (explicit mapping)
**Question:** How do we map domain ↔ DTO / read models without AutoMapper?
**Lean:** Explicit mapping, or **Mapperly** (source-gen) for boilerplate-heavy cases. Reflection-based
object mapping has fallen out of senior favor independent of licensing.
**Becomes live when:** first DTO or CQRS read-model projection.
**Lands in:** likely a note in the MOD-02 ADR or its own short ADR.
**Decided in practice (2026-10-11, from the code):** mapping is hand-written (`OrderMapper`, `Select` projections
in queries, `FromResult` factories on responses). Neither AutoMapper nor Mapperly is referenced. No ADR.

### MOD-04 — Module internal structure: layered vs vertical slice · 🔷 Open
**Question:** The original is layered per module. Keep that, adopt vertical slice (VSA), or a hybrid?
**Why it matters:** shapes every module's internal structure.
**Withdrawn plan (2026-08-16 → 2026-10-11):** the plan was to demonstrate both — Ordering in a
layered/Clean convention, Inventory as a vertical slice — as a side-by-side comparison. The user
deleted its ADR on 2026-10-11: it was never accepted and the modules were not built that way.
**What the code has (checked 2026-10-11):** Ordering, Inventory and Receiving are each a single project
with `Application` / `Domain` / `Infrastructure` / `Presentation` folders (plus a `Contracts` project
for Ordering and Receiving). Nothing enforces layering between the folders. Inventory keeps repository
interfaces and a unit of work; plain reads go straight to the `DbContext`.
**User's stated position (README, "Modelling"):** not strict Clean, layered or VSA; each use case is a
handler in the application layer; each bounded context's design may differ and evolve; borrowing more
from VSA as the project grows.
**Interacts with:** MOD-02.
**Status:** 🔷 Open — no ADR. Do not write one unless the user asks.

### MOD-05 — Value-object persistence · 🧭 Leaning
**Question:** Map value objects (`Money`, `PostalCode`, `StreetAddress`, `AddressState`) as EF Core
**complex types** (EF 8+) or the old **owned entities**?
**Lean:** Complex types — the modern, correct replacement for the owned-entity hack the original used.
**Becomes live when:** first EF persistence of the Ordering aggregate.
**Lands in:** ADR "Persisting value objects with EF Core complex types."

### MOD-06 — Strongly-typed IDs · ✅ Decided (hand-written, no generator)
**Question:** Wrap identifiers (OrderId, SkuId, MerchantId) in strongly-typed IDs, or use primitives?
**Lean:** Adopt — via **Vogen** or a `StronglyTypedId` source generator. Kills primitive obsession;
strong DDD signal. Requires EF value converters + JSON/serialization + model-binding wiring — worth an
ADR because that wiring is the interesting part.
**Becomes live when:** first persisted aggregate or API contract.
**Lands in:** ADR "Strongly-typed IDs across persistence and transport."
**Decided in practice (2026-10-11, from the code):** aggregate ids are hand-written `ValueObject` types, one per
module (`OrderId`, `EntityId`, `UniqueId`), persisted through EF value converters. No Vogen or other
source generator. `OwnerId` is a plain `int` everywhere. No ADR.

### MOD-07 — Database engine · ✅ Decided (PostgreSQL)
**Lean:** **PostgreSQL** over SQL Server — closer to modern logistics shops, Testcontainers-friendly,
free. Keep the CQRS read/write split (e.g. Dapper reads / EF writes) if desired; that's still sound.
**Becomes live when:** first persistence layer.
**Lands in:** brief ADR (engine + rationale). Data ownership / schema-per-module is clause B.3 of the
extractability contract (MOD-20 / ADR-0002).
**Decided (2026-10-11):** PostgreSQL — Npgsql EF provider, Wolverine's Postgres persistence, Postgres in
`compose.yaml` and in the integration tests; ADR-0002 clause B.3 (Accepted) names it. Reads and writes
both go through EF Core; Dapper is not used. No separate engine ADR.

### MOD-08 — API layer · ✅ Decided (Minimal APIs + native OpenAPI)
**Lean:** **Minimal APIs** with route groups, endpoint filters, `TypedResults`, and **native OpenAPI**
(`Microsoft.AspNetCore.OpenApi`; Swashbuckle dropped from default templates). FastEndpoints (REPR) is
the fallback if we want more structure without controllers.
**Becomes live when:** first HTTP endpoint.
**Lands in:** ADR "API surface: Minimal APIs + native OpenAPI."
**Note (2026-09-18):** staying with Minimal APIs for now. The Wolverine messaging adoption
does **not** require Wolverine.HTTP — the intake endpoint publishes its integration event via
`IDbContextOutbox<OrdersDbContext>` (Wolverine's documented outbox-from-a-plain-endpoint path),
keeping the endpoint style unchanged. Plan: **trial Wolverine.HTTP in a future story** as a
deliberate side-by-side comparison, then decide whether to migrate the API surface.
**Decided in practice (2026-10-11, from the code):** all three modules map endpoints with Minimal API route
groups and `TypedResults`; the host uses `Microsoft.AspNetCore.OpenApi`. No controllers, FastEndpoints or
Wolverine.HTTP. The Wolverine.HTTP trial in the 2026-09-18 note is still a possible future story. No ADR.

### MOD-09 — Observability (OpenTelemetry) · ✅ Decided
**Lean:** OTel as first-class — `ActivitySource` traces, `System.Diagnostics.Metrics`, OTLP export;
propagate context **across the async/messaging boundary** (the hard, high-value part for logistics:
trace an order through Ordering → Fulfillment → Shipping). Wire early (Aspire service defaults), harden
in Phase 4. Replaces the original's Serilog+ELK+custom-correlation approach.
**Becomes live when:** as soon as there's a request path worth tracing.
**Lands in:** ADR; also a roadmap Item (already listed as an OBS candidate). Context propagation across
the async boundary is clause B.5 of the extractability contract (MOD-20 / ADR-0002).
**Note (2026-10-11):** the user confirmed B.5 stays a requirement. Gap: `Program.cs` registers ASP.NET
Core and HttpClient tracing but not `.AddSource("Wolverine")`, which Wolverine's docs require for its
spans to be collected (verified via Context7 2026-10-11). Not run against the dashboard.
**Decided (2026-10-11):** OpenTelemetry is the observability stack — traces and logs export over OTLP from the
host, and ADR-0002 clause B.5 (Accepted) requires trace context across messaging. **Not wired yet:**
metrics (no `WithMetrics`), the `Wolverine` trace source (note above), and Aspire service defaults
(MOD-10 is not adopted).

### MOD-10 — .NET Aspire · 🧭 Leaning
**Question:** Use Aspire for local orchestration, service defaults (OTel/health/resilience wiring), and
the dashboard?
**Lean:** Adopt — makes the repo runnable in one command (huge for a clone-to-learn repo) and wires
much of MOD-09/MOD-14 for free. Cost: an Aspire dependency + AppHost project to learn.
**Becomes live when:** more than one process/dependency exists (DB, broker, worker).
**Lands in:** ADR "Local orchestration & service defaults via .NET Aspire."

### MOD-11 — Integration tests on Testcontainers · ✅ Decided
**Lean:** Adopt — real Postgres/RabbitMQ per test run, killing shared-DB flakiness. Complements the
existing NUnit 4 + Moq unit conventions (see [testing.md](../../docs/testing.md)); this is the integration tier.
**Becomes live when:** first integration test that needs real infra.
**Lands in:** extend testing.md + a short ADR.
**Decided in practice (2026-10-11, from the code):** `Throughline.Api.Tests` runs against a real Postgres via
`Testcontainers.PostgreSql`, and CI runs that suite as the Integration category. Postgres only — there is
no broker to containerise while messaging uses Wolverine's Postgres-backed local queues. No ADR.

### MOD-12 — Architecture / boundary tests · ✅ Decided — not adopting (2026-10-11)
**Was leaning:** adopt **NetArchTest** or **ArchUnitNET** in CI to enforce that modules talk only via
published contracts.
**Decision (user, 2026-10-11):** "architecture tests no" — the answer when asked which unmet clauses of
the extractability contract were still intended. They were removed from ADR-0002 as an enforcement
requirement. (Recording this as "not adopting" is Claude's reading of that answer; reopen if the user
says otherwise.)
**What holds the boundaries today:** project references — a module references only another module's
`Contracts` project.

### MOD-13 — Transport-swappable integration events · ✅ Decided
**Question:** Make the in-process integration-event bus a seam that can become RabbitMQ/Service Bus
without touching module code?
**Why it matters:** The "monolith-first, extract-when-proven" evolution is the whole *point* of the
modular monolith in 2025 and is underexplained online — a core narrative for this repo.
**Lean:** Yes, as a design principle. Interacts with MOD-02 (Wolverine provides this seam natively).
This seam is clause B.2 of the extractability contract (MOD-20 / ADR-0002) — the load-bearing one.
**Becomes live when:** first cross-module event.
**Lands in:** folded into MOD-02's ADR or its own.
**Decided (2026-10-11):** in force. Modules exchange integration events only through `*.Contracts` projects over
Wolverine (durable local queues today), and ADR-0002 clause B.2 (Accepted) makes it a rule.

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
Expected failures flow through `Result` / `Result<T>` carrying `FieldError`s and an `ErrorType`
(`Validation`, `Conflict`, `NotFound`), not exceptions. Already in force.
**See [ADR-0001](../../docs/decisions/ADR-0001-endpoints-report-validation-failures-together.md)** — endpoints report validation failures together, not one at a time;
validation failures are returned, not thrown.
**Note (2026-10-11):** the earlier error-taxonomy ADR was deleted — it described the removed
order-estimate feature and an `Unavailable` / `Unexpected` taxonomy the code no longer has. The
always-valid-domain rules (`Create()` vs constructor, which exception for which cause, what a command
may check) were cut from the ADR by the user as coding conventions; they are not recorded anywhere yet.

### MOD-18 — Container publish via SDK · 🧭 Leaning
**Lean:** `dotnet publish /t:PublishContainer` — no Dockerfile. Low priority until there's a deployable.
Container-publishable is clause B.7 of the extractability contract (MOD-20 / ADR-0002); not yet exercised.
**Becomes live when:** first deployable artifact / CI publish step.

### MOD-19 — Source generators for hot paths · 🧭 Leaning
**Lean:** `[LoggerMessage]` logging + System.Text.Json source-gen contexts; signals a 2025 codebase and
avoids reflection on hot paths. Low priority; a polish pass.
**Becomes live when:** a performance/hot-path pass, or when logging volume grows.

### MOD-20 — Module extractability contract · ✅ Decided (2026-10-11 · [ADR-0002](../../docs/decisions/ADR-0002-module-extractability-contract.md))
**Question:** What must be true of every module so a future split into its own process is a
*deployment change, not a rewrite*?
**Key reframe:** a module in the monolith is class libraries with no deployment target; it gains one
only when wrapped in a host. So we design for **extractability** and bind the target late.
**Decision (in ADR-0002):**
- **Extraction is demand-driven** along modeled bounded-context seams (independent scaling, deploy
  cadence/team autonomy, fault isolation, divergent runtime/compliance/SLA) — monolith-first, no
  pre-splitting; the first extraction is expected to be a **worker**, not a request/response module.
- **The extractability contract** every module holds from day one: host-agnostic code (no
  `System.Web`/`web.config`; `HttpContext` stays in `Presentation`), cross-module comms only via the
  transport-swappable seam (MOD-13), data ownership / schema-per-module (MOD-07),
  hostile-from-day-one integration (idempotent, at-least-once, inbox/outbox — MOD-16/MOD-02), trace
  context across the messaging boundary (MOD-09), stateless + graceful shutdown + health and readiness
  endpoints, container-publishable (MOD-18).
- Per-module `Presentation` + `Add{Module}`/`Map{Module}` (MOD-08) makes extraction a ~20-line host.

**Changed 2026-10-11 (user):**
- **Deployment posture cut from the ADR** (OCI / Azure Container Apps primary, IIS non-goal, Aspire
  rehearsal). Hosting is now decided in no ADR; ADR-0003 mentions "App Service first, Container Apps or
  Container Instances later" only as context.
- **Architecture tests are not an enforcement requirement** (see MOD-12).
- **Tracing across messaging and health endpoints stay as requirements**, although not yet built.

**Not yet met (checked 2026-10-11):** messaging spans are not collected (B.5 — see MOD-09); no health or
readiness endpoints are mapped (B.6); container publishing has not been exercised (B.7).
**Status:** ✅ Decided — ADR-0002 **Accepted** 2026-10-11.
**Binds:** MOD-13, MOD-07, MOD-09, MOD-18, MOD-16, MOD-02; relies on MOD-08.
**Lands in:** [ADR-0002](../../docs/decisions/ADR-0002-module-extractability-contract.md) (Accepted).

### MOD-21 — Domain base types · ✅ Decided (2026-09-19)
**Decision:** One shared equality base, **`ValueObject`** (the `GetAtomicValues`-driven
`Equals`/`==`/`!=`/`GetHashCode` this repo already had), with **`Entity<TId> : ValueObject`** as its
subtype. A value object yields **all** its attributes (value equality); `Entity<TId>` yields **only**
`Id` via a **sealed** `GetAtomicValues`, so identity can't be diluted and equality survives state
changes. The base's name reflects the common case; entities reuse the mechanism.
**Considered and dropped:** a neutral third base (`EquatableByValues`) sitting under both. Cut to keep
the DDD `ValueObject` name on the ~15 value objects rather than renaming them; accepted cost is that
`Entity<TId>` is-a `ValueObject` at the type level (naming, not behavior).
**Divergence from kgrzybek:** the original keeps two *unrelated* bases — its `ValueObject` compares all
properties, its `Entity` implements no equality (reference/Id only). Here the mechanism is shared and
`Entity` is a subtype; entities get value-based identity equality by design, which the original lacks.
**Why it came up:** Inventory's `Order` gained a mutable `AllocationStatus` (Confirmed → Partially →
Fully Allocated) yet needed `==`/`Equals`; it had been sitting on `ValueObject` while comparing on id
alone — i.e. it was an entity all along. `Order : Entity<OwnerOrderId>`.
**No ADR** — folded here per its size; base classes carry `<summary>` docs stating the intent.

### MOD-22 — Authorization model · ✅ Decided (roles assigned in Entra, permissions mapped in Throughline · revised 2026-10-10 · [ADR-0003](../../docs/decisions/ADR-0003-authorization-functional-and-owner-roles.md))
**Question:** Where do operator permissions (first one: *receive delivery*) live, and how are they
evaluated?
**Options:**
- **(a) Permissions in the token** — IdP issues roles/permission claims; the API checks claims only.
  Stateless and simple; costs revocation lag until token expiry and token bloat as permissions grow.
- **(b) Permissions resolved server-side** — IdP proves *who*; Throughline holds user → role →
  permission and resolves it per request (cached). Changes take effect immediately; costs a lookup.
  This is kgrzybek's model (UserAccess module + permission attribute + custom authorization handler).
**Decision (2026-09-29):** **(b).** Rationale: permissions are likely scoped by Throughline's own data
(facility, possibly owner), which the IdP doesn't know — *assumption, to confirm*. Evaluation is
two-layered: an endpoint policy answers "may this user receive at all?"; a resource-based check
(`IAuthorizationService.AuthorizeAsync(user, resource, requirement)`) inside the handler answers "may
they act on *this* delivery?" once it is loaded.
**Why it came up:** receiving (issue #14) is the first operation requiring a named permission. Current
state had no authentication/authorization wired, and `owner_id` was read from an unauthenticated
header (`RequestContext`).
**Revision (2026-10-10): (c) hybrid, chosen by the user.** Microsoft Entra ID is the IdP.
- **Role assignment lives in Entra** as app roles on the API's app registration; roles arrive in
  the token's `roles` claim.
- **Throughline maps role → permissions in code** (a static map, no user-access tables).
  Endpoints check **permissions, never roles** (the kgrzybek shape).
- **The 2026-09-29 second layer (a resource-based check on the loaded record) is withdrawn**
  (user, 2026-10-10). A caller who may act for an owner may see all of that owner's data; there
  are no checks on individual orders or receipts. Data is segregated by owner id, every
  endpoint receives the owner up front, and every query is filtered by it. The
  owner-level question — may this caller act for the owner they name — is a check on the owner
  id before any data access (see the owner check, below).
- **Token validation is in app code**, not platform built-in auth, so it behaves the same on
  App Service, Container Apps and Container Instances (ADR-0002 host-agnostic clause).
- **Deny by default** via a fallback policy; the dev-only OpenAPI endpoint needs an explicit
  anonymous exemption.
- **Owner check — in scope for #22 (user, 2026-10-10).** A caller may read and write data only
  for owners whose owner role it holds. `OwnerId` comes from the `owner_id` header today and nothing
  checks it. An owner's own system acts for one owner; an operator may hold several, so the
  request must name the owner. **Design accepted by the user 2026-10-10:** an owner role is an
  Entra app role per owner (placeholder name `Owner.7`), assigned to the account beside its
  functional roles and arriving in the same `roles` claim. It carries no permission; it is
  compared with the owner named in the request. A caller's functional roles apply to every owner
  whose role it holds; a functional role cannot be limited to particular owners (Oracle WMS Cloud
  keeps function and client eligibility as two
  separate settings; no story needs more). App roles are the one Entra mechanism confirmed to
  work for both service and user accounts.

**First implementation: issue #22** (the source of truth for scope). Three roles to start, named
for job functions (user, 2026-10-10): `OrderIntakeIntegration` (service accounts), `Receiving`
and `OrderPlanning` (operator accounts). They replace the action-named `OrderConfirmer`,
`DeliveryReceiver`, `OrderAllocator`. **Roles are not tied one-to-one to modules**: each happens
to cover one module's endpoints today, but roles and permissions will grow as functionality is
added. State rules per endpoint permission, never as
"the module's role".
These are the Entra app roles; they sit alongside the role → permission map above, they do not
replace it (user, 2026-10-10). Each persona is assigned one or more of them.
Missing or invalid credentials → 401; authenticated without a role granting the endpoint's
permission → 403.
The issue's out-of-scope section was removed by the user on 2026-10-10, and the issue now
carries the owner-check acceptance criteria and the job-function role names.

**Why (b) was revised:** its rationale was recorded as an unconfirmed assumption (permissions
scoped by facility/owner data). Facility is not modeled, and owner scoping is handled by the
resource check under every option. Entra documents app roles as its recommended option and a
custom store as the most complex. With (c) an extracted module validates a self-contained token
and needs no shared permission store. **Cost accepted:** a removed role stays effective until
the token expires. Moving to (b) later changes only the component that produces a caller's
permissions, because endpoints check permissions.
Sources: the Security register in [sources.md](../sources.md).

**Callers (user, 2026-10-10):** there are no real users or owners. Requests come only from
manual Bruno/Insomnia calls and load-testing frameworks, but those tools impersonate personas:
e.g. an EDI integrator service posting to Ordering (a service identity) and a warehouse
operations user posting to Receiving (a person). The persona is switched by switching
credentials in the tool. Design for the persona, not the tool.

**Still open:**
- Where user access lives (own module vs shared) — interacts with MOD-04 and MOD-20.
- Permission scope for operators: any owner vs an eligible-owner list. Oracle WMS Cloud documents
  a per-user eligible-companies list; Infor not confirmed. Facility scope is out while Facility
  is unmodeled.
- Internal `UserId`: lean is a v7-GUID value object (matching `OrderId`), mapped from the IdP's
  `(issuer, sub)` external login stored as strings — `sub` is opaque per OIDC, never parsed as a GUID.
  Business records (`receivedBy`, events, audit) carry the internal `UserId`.
- Carrying the operator off the HTTP request: explicit in command/event payloads for business records;
  Wolverine's `EnableRelayOfUserName` (relays `Identity.Name` onto `Envelope.UserName` + `enduser.id`
  OTel tag, verified via Context7 2026-09-29) is metadata only, and its auto-middleware targets
  Wolverine.HTTP endpoints — confirm behavior with plain Minimal APIs.

---

## Graduation checklist (when a decision goes ✅)
1. Write the ADR (`ADR-000N-*.md`) in the shape of the existing ones in `docs/decisions/` (Status /
   Context / Decision / Consequences / Alternatives considered). One decision per ADR; check every
   named type and claim against the code; never cite this register, `sources.md`, backlog items or
   any other `.claude` file in it.
2. Flip the entry's status to ✅ Decided and link the ADR.
3. If it spawned skills to practice, add roadmap/backlog Items via the dedup flow.
4. Bump this file's `Updated:` line.
