# Authoritative Sources (Project Canon)

Governs how Claude vets responses on **Throughline**. Read this before any design,
architecture, or DDD answer, and ground claims against it.

This file holds **five source registers**: the **design / architecture / DDD** set (the
table below), an **Observability & Logging** set, a **Testing — Messaging & Error
Handling** set, and a **Security — Authentication & Authorization** set, all under the
Confirmed/Pending status legend; and a **WMS Functional / Vendor
Reference** register (at the end of the file) under its own tier legend. Read the testing
register before writing or reviewing tests for integration-event handlers. Read the security
register before designing, building, or reviewing authentication, authorization (RBAC), or
owner scoping of a caller. Read the observability register before designing, building, or reviewing
any logging, tracing, metrics, or correlation — the logging subsystem is not yet implemented,
so these are the sources that govern both how it gets built and how it gets reviewed. Read the
**WMS Functional / Vendor Reference** register before writing any story or requirement, and
before answering **any warehouse business-logic question** — including when the user asks you
to act as a **business analyst / SME** fielding domain questions. It governs which vendor docs
back a stated warehouse behavior.

## Status legend

- **✅ Confirmed** — authoritative. The source's definitions/conventions **override
  Claude's instinct**; Claude may assert their positions as grounded. Conflicts
  *between* confirmed sources are surfaced (never picked silently) and settled by an ADR.
- **⏳ Pending** — reference/awareness only. Claude may mention them but must **label them
  unconfirmed** and must **not** present their opinions/conventions as authoritative, nor
  let them override a confirmed source or settle a decision. A topic covered only by
  pending sources (or none) is **ungoverned**: Claude flags it and treats its guidance as
  reasoning, not doctrine.

## Sources

| # | Source | Lineage / use | Status |
|---|--------|---------------|--------|
| 1 | Evans, *DDD* (Blue Book); Vernon, *Implementing DDD* (Red Book) | DDD concepts: value objects, entities, aggregates, bounded contexts, context mapping | ✅ Confirmed |
| 2 | Fowler, *PoEAA* + martinfowler.com | Enterprise/persistence patterns: **Money, Gateway**, Repository, Unit of Work, Service Layer; anti-patterns (Anemic Domain Model) | ✅ Confirmed |
| 3 | R. C. Martin — SOLID/DIP, *Clean Architecture*; A. Cockburn — Hexagonal (Ports & Adapters) | Architecture principles: dependency rule, ports/boundaries | ✅ Confirmed |
| — | [kgrzybek/modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd) | .NET reference implementation: modular structure, inter-module messaging, CQRS/outbox | ✅ Confirmed |
| 4 | Wlaschin, *Domain Modeling Made Functional* | Result / railway-oriented modeling within DDD | ⏳ Pending |
| 5 | Ardalis / eShopOnWeb + libs (`Ardalis.Result`, `GuardClauses`, `Specification`, `SmartEnum`) | .NET tactical rendering; Result library | ⏳ Pending |
| 6 | Bogard — Vertical Slice Architecture | Feature-first alternative to layered/clean architecture | ⏳ Pending |

## Operating rules

1. **Confirmed wins.** When Claude's instinct conflicts with a confirmed source, the source wins.
2. **Conflicts between confirmed sources** are surfaced, not resolved silently → an ADR decides for this project.
3. **Gaps are flagged.** If only pending sources (or none) cover a topic, Claude says so and marks its answer as reasoning, not doctrine.
4. **Public sources are fetchable/quotable** (Grzybek, eShopOnWeb, martinfowler.com, MS Learn). Book sources (Evans, Vernon, Fowler PoEAA, Martin, Wlaschin) are cited from knowledge — flag "not verified against the text this turn."

## Known gaps & notes

- **`Result<T>` is currently ungoverned.** The confirmed set (Evans/Vernon/Fowler/Martin)
  is silent on Result — it belongs to the functional/railway lineage (Wlaschin, #4), which
  is Pending. Until a Result source is confirmed, Result guidance is Claude's reasoning,
  not doctrine.
- **Fowler's Value Object (PoEAA) ≠ DDD's Value Object.** For VOs, defer to Evans/Vernon (#1);
  use Fowler as cross-reference.
- **#3 is contested.** Clean Architecture is the orthodoxy that Bogard's VSA (#6, Pending)
  reacts against; treat layering as *a* choice with a live opposing school. Cockburn's
  Hexagonal predates and parallels Martin's formulation. *Clean Code* (craftsmanship) is
  deliberately excluded — #3 is SOLID + Clean Architecture only.

---

# Observability & Logging (Project Canon)

Same **status legend and operating rules** as the design/DDD register above (Confirmed
overrides Claude's instinct; Pending is awareness-only; conflicts → ADR). Scope: everything
Claude does with **structured logging, distributed tracing, metrics, and correlation** on
Throughline. **The logging subsystem is not yet implemented** — these sources govern both
*how it gets built* and *how it gets reviewed* (they back the Observability bullet in
[CLAUDE.md](CLAUDE.md) §7C).

Two lenses shape every telemetry decision here:

- **It may scale out.** Throughline is a modular monolith today but is built so modules can
  become services (§1). Instrument as if it were *already* distributed — one trace spanning
  modules, standard context propagation — so that splitting a module out is a deployment
  change, not a re-instrumentation.
- **It is multi-tenant.** Owner segregation is non-negotiable (§2–§3). **Telemetry is a read
  path like any other:** `OwnerId` — the tenancy key threaded through every aggregate —
  belongs on every log scope, span, and metric dimension, and no owner's identifying or
  sensitive payload data may leak into telemetry that crosses an owner boundary. Redaction is
  first-class, not a follow-up. **`FacilityId` is deliberately *not* in scope:** Throughline
  is single-facility by product decision (facility is unmodeled on `Order` intake; the only
  facility-retrofit risk lives in Inventory) — add a facility dimension to telemetry *only
  if/when* multi-facility is actually built, not preemptively.

## Sources

Tiered: standards (ground truth) → .NET platform docs (how we do it) → principles (what to
measure).

### Tier 1 — Standards & specifications (the ground truth)

| # | Source | Use | Status |
|---|--------|-----|--------|
| O-1 | **OpenTelemetry** — [specification](https://opentelemetry.io/docs/specs/otel/), [semantic conventions](https://opentelemetry.io/docs/specs/semconv/), [logs data model](https://opentelemetry.io/docs/specs/otel/logs/) | Vendor-neutral model for traces/metrics/logs and the **canonical attribute names** (http, db, messaging, exception…). The shape all telemetry conforms to. | ✅ Confirmed |
| O-2 | **W3C Trace Context** — [traceparent/tracestate](https://www.w3.org/TR/trace-context/); **W3C Baggage** — [baggage](https://www.w3.org/TR/baggage/) | The standard for propagating trace/correlation identity across module (and later service) boundaries. Adopt in-process now so scale-out needs no new correlation scheme. | ✅ Confirmed |
| O-3 | **The Twelve-Factor App — XI. Logs** — [12factor.net/logs](https://12factor.net/logs) | Logs are an **event stream**: the app writes structured events to stdout; the platform routes/aggregates. No in-process log-file management. | ✅ Confirmed *(principle)* |

### Tier 2 — .NET 10 platform docs (how we do it)

| # | Source | Use | Status |
|---|--------|-----|--------|
| O-4 | **.NET Observability with OpenTelemetry** — [observability-with-otel](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel) | Canonical .NET wiring of the three signals over the OTel SDK + OTLP export. | ✅ Confirmed |
| O-5 | **Logging in .NET** ([logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)) + **compile-time / high-performance logging** ([LoggerMessage source generator](https://learn.microsoft.com/en-us/dotnet/core/extensions/logger-message-generator), [high-performance logging](https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging)) | `ILogger` + **structured message templates**; `[LoggerMessage]` source-gen on hot paths (no boxing/allocation, no interpolation); `BeginScope` for ambient owner context. | ✅ Confirmed |
| O-6 | **Distributed tracing in .NET** — [distributed-tracing](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing) (`System.Diagnostics.ActivitySource` / `Activity`) | How spans are produced and propagated in .NET; the substrate OTel traces build on. | ✅ Confirmed |
| O-7 | **Metrics in .NET** — [metrics](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/metrics) (`System.Diagnostics.Metrics.Meter`) | Instruments (counter/histogram/gauge) and dimensions; the substrate OTel metrics build on. | ✅ Confirmed |
| O-8 | **Data redaction** ([data-redaction](https://learn.microsoft.com/en-us/dotnet/core/extensions/data-redaction)) + **compliance libraries** ([compliance](https://learn.microsoft.com/en-us/dotnet/core/extensions/compliance)) — `Microsoft.Extensions.Compliance.*`, `[LogProperties]`/`[TagProvider]`, `DataClassification` | Classify + redact sensitive/owner data in logs. Directly serves the no-cross-owner-leakage rule. | ✅ Confirmed |
| O-9 | **OpenTelemetry .NET SDK** — [opentelemetry.io/docs/languages/dotnet](https://opentelemetry.io/docs/languages/dotnet/) | The .NET implementation of O-1: exporters, resource/attributes, instrumentation libraries. Traces, metrics, and logs are all **stable**. | ✅ Confirmed |
| O-10 | **.NET Aspire — telemetry & ServiceDefaults** — [aspire telemetry](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/telemetry), [OTLP example](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-otlp-example) | Reference for the **default** OTel wiring MS ships (auto-instrument HTTP/gRPC/ASP.NET, OTLP export, dashboard). Mine the wiring pattern — **adopting Aspire itself is an ADR, not a given.** | ⏳ Pending |

### Tier 3 — Principles & literature (what to measure, how to think)

| # | Source | Use | Status |
|---|--------|-----|--------|
| O-11 | **Google SRE Book** & **SRE Workbook** — [sre.google/books](https://sre.google/books/); esp. [Monitoring Distributed Systems](https://sre.google/sre-book/monitoring-distributed-systems/) | The **four golden signals** (latency, traffic, errors, saturation) and **SLI/SLO/error-budget** framing. Tie signals to the §2 SLOs (99.9% pick/pack/ship; sub-second inventory reads). Fetchable at sre.google. | ✅ Confirmed |
| O-12 | **RED method** (Rate/Errors/Duration — Tom Wilkie) & **USE method** (Utilization/Saturation/Errors — Brendan Gregg) | Complementary heuristics: RED for request-driven paths, USE for resources. Use to decide *which* instruments each hot path needs. | ⏳ Pending |
| O-13 | **Observability Engineering** — Majors, Fong-Jones, Miranda (O'Reilly) | High-cardinality **wide structured events**, observability-vs-monitoring, debugging unknown-unknowns. Book cited from knowledge — flag "not verified against the text." | ⏳ Pending |

## How these apply to Throughline (operating notes)

1. **Structured, never interpolated.** Every log is a message template with named properties
   (`"Reserved {Qty} of {Sku} for owner {OwnerId}"`), never `$"…{qty}…"`. Hot paths (the
   inventory reservation loop, §2) use `[LoggerMessage]` source-gen. *(O-1, O-5)*
2. **Owner on everything.** `OwnerId` rides on every log scope (`BeginScope`), span
   attribute, and metric dimension — telemetry is a tenant read path. Never emit one owner's
   identifying data into shared/cross-owner telemetry; classify and redact sensitive fields.
   (`FacilityId` stays out of telemetry while Throughline is single-facility — see the lens
   note above.) *(§2–§3, O-8)*
3. **Correlation now, distribution later.** One `Activity`/trace threads a request across
   modules today via W3C Trace Context; when a module becomes a service, the same
   `traceparent` crosses the wire unchanged. Do **not** invent a bespoke correlation-id
   header. *(§1, O-2, O-6)*
4. **Signals tied to SLOs.** Instrument the golden signals on the pick/pack/ship and
   inventory-availability paths and express them as SLIs against the §2 targets; the
   inventory hotspot additionally needs **contention** metrics (reservation conflicts,
   optimistic-concurrency retries, wait time). *(O-7, O-11, O-12)*
5. **Logs as an event stream.** Emit to stdout / a structured sink and let the platform
   route; the process does not own log files or rotation. *(O-3)*
6. **One OTel-shaped pipeline.** Traces, metrics, and logs all flow through the OTel SDK with
   OTLP export and OTel semantic-convention attribute names, so a future collector/backend
   swap is config, not code. *(O-1, O-4, O-9)*

---

# Testing — Messaging & Error Handling (Project Canon)

Same **status legend and operating rules** as the design/DDD register above. Scope: testing
Wolverine integration-event handlers — in particular their failure paths (which exception a
handler throws, and therefore whether the message retries or dead-letters per the policies in
`src/api/Throughline.Api/Program.cs`). Binding test *conventions* (naming, structure, Moq)
remain in `docs/testing.md`; this register governs *what* to test on the messaging seam.
Added 2026-10-09 after a web review; both sources are Pending until the user confirms them.

## Sources

| # | Source | Use | Status |
|---|--------|-----|--------|
| TS-1 | **Wolverine docs** — [Test Automation Support](https://wolverinefx.net/guide/testing.html) · [Error Handling](https://wolverinefx.net/guide/handlers/error-handling.html) | How to observe handler failures and error-policy outcomes in tests (tracked sessions); how `OnException<T>()` policies are evaluated. Framework docs for the messaging library this project uses. | ⏳ Pending |
| TS-2 | **Khorikov — [When to Mock](https://enterprisecraftsmanship.com/posts/when-to-mock/)** (and *Unit Testing Principles, Practices, and Patterns* — book cited from knowledge, not verified against the text) | Managed vs unmanaged out-of-process dependencies: use real instances of managed ones (the app's own DB) and assert resulting state; mock unmanaged ones (a shared bus) and verify the interaction. | ⏳ Pending |

## Findings from the 2026-10-09 review (apply when testing handler failure paths)

1. **Error-path tests need `DoNotAssertOnExceptionsDetected()`.** A tracked session fails on
   any handler exception by default; the docs call this setting out for error-handling tests.
   *(TS-1)*
2. **`InvokeAsync` won't exercise dead-lettering.** Inline invocation applies only Retry /
   Retry-With-Cooldown automatically, so `MoveToErrorQueue` and `ScheduleRetry` never fire.
   A test of the dead-letter path must publish/send the event through the queue.
   *(TS-1, Error Handling)*
3. **Dead-lettering is observable on the tracked session.** `MovedToErrorQueue`,
   `MessageFailed` and `Requeued` are recorded event types — no need to query the dead-letter
   table. Default session timeout is 5 s; first-use handler compilation can consume it.
   *(TS-1)*
   **Correction (2026-10-10, from Wolverine source `MessageContext.ReScheduleAsync` /
   `EnvelopeHistory`, main branch):** a `ScheduleRetry` records the envelope as **`Requeued`**,
   not `Scheduled`, and `Requeued` is not a terminal tracking event — so the tracked session
   never completes and **times out**. A retry test needs `DoNotAssertOnTimeout()` (plus a
   short `Timeout(...)`) and asserts on `session.Requeued`.
4. **Assert "nothing saved" against the real DB.** The Inventory DB is a managed dependency →
   real instance + state assertion. Backs the all-or-nothing claim in
   `AllocatablePalletsHandler` (the precondition for safely replaying a dead-lettered message).
   *(TS-2)*
5. **Unverified:** what Wolverine does with an exception that matches no `OnException` policy.
   The Error Handling page doesn't say — don't assert a default without checking.
6. **Ungoverned (Claude's reasoning, not doctrine):** treating the thrown exception *type* as
   the handler's contract with the error policy, and asserting it per failure path. No source
   found argues for or against it. Whether Wolverine's local durable queues count as
   "managed" under TS-2 (only this app reads them; stored in this app's `wolverine` schema) is
   also an inference — Khorikov doesn't address in-process module queues.

---

# Security — Authentication & Authorization (Project Canon)

Same **status legend and operating rules** as the design/DDD register above. Scope:
authenticating callers, role-based access control (RBAC), and deriving/enforcing the owner a
caller may act for, across all modules. Hosting path this register assumes: Azure App Service
first, then one or more modules on Azure Container Apps or Azure Container Instances.
Added and confirmed by the user 2026-10-10. The project decision these sources back is the
**authorization model** entry in
[MODERNIZATION-REGISTER.md](decisions/MODERNIZATION-REGISTER.md).

## Sources

"Read" = read on 2026-10-10. "Resolves" = the URL loaded that day but the content is cited
from knowledge — flag "not verified against the text this turn" until read.

### Tier 1 — Standards (the ground truth)

| # | Source | Use | Status |
|---|--------|-----|--------|
| SEC-1 | **NIST RBAC model** — [csrc.nist.gov project page](https://csrc.nist.gov/projects/role-based-access-control) (standardized as ANSI/INCITS 359) | Vocabulary: user, role, permission, role hierarchy, separation of duty. | ✅ Confirmed *(resolves)* |
| SEC-2 | **OWASP API Security Top 10 (2023)** — [API1 Broken Object Level Authorization](https://owasp.org/API-Security/editions/2023/en/0xa1-broken-object-level-authorization/) · [API5 Broken Function Level Authorization](https://owasp.org/API-Security/editions/2023/en/0xa5-broken-function-level-authorization/); **OWASP [Authorization Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Authorization_Cheat_Sheet.html)** | The two failure classes every endpoint is reviewed against: wrong owner's data (object level) and wrong function (function level). | ✅ Confirmed *(resolves)* |
| SEC-3 | **IETF** — [RFC 9068](https://datatracker.ietf.org/doc/html/rfc9068) (JWT access-token profile) · [RFC 8725](https://datatracker.ietf.org/doc/html/rfc8725) (JWT best current practice) · [RFC 9700](https://datatracker.ietf.org/doc/html/rfc9700) (OAuth 2.0 security best current practice) | What an access token contains and how it must be validated. | ✅ Confirmed *(resolves)* |

### Tier 2 — Platform docs (how we do it)

| # | Source | Use | Status |
|---|--------|-----|--------|
| SEC-4 | **ASP.NET Core 10** — [Auth in Minimal APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security) · [Policy-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0) · [Resource-based authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resourcebased?view=aspnetcore-10.0) · [Custom authorization policy providers](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/iauthorizationpolicyprovider?view=aspnetcore-10.0) | How policies, requirements, handlers, the fallback policy, and imperative resource checks are built; `dotnet user-jwts` for local tokens. | ✅ Confirmed *(read)* |
| SEC-5 | **Microsoft Entra** — [Custom RBAC for developers](https://learn.microsoft.com/en-us/entra/identity-platform/custom-rbac-for-developers) · [Implement RBAC in applications](https://learn.microsoft.com/en-us/entra/identity-platform/howto-implement-rbac-for-apps) · [Verify scopes and app roles](https://learn.microsoft.com/en-us/entra/identity-platform/scenario-protected-web-api-verification-scope-app-roles) | Where role assignments live (app roles / groups / custom store); how roles reach the API (`roles` claim); user versus application callers. | ✅ Confirmed *(read)* |
| SEC-6 | **Azure hosting** — [App Service authentication](https://learn.microsoft.com/en-us/azure/app-service/overview-authentication-authorization) · [Container Apps authentication](https://learn.microsoft.com/en-us/azure/container-apps/authentication) | What the platform's built-in auth does and doesn't do. | ✅ Confirmed *(read)* |
| SEC-7 | **Azure Architecture Center** — [Identity in a multitenant solution](https://learn.microsoft.com/en-us/azure/architecture/guide/multitenant/considerations/identity) | Mapping callers to tenants (owners); where tenant authorization data is stored; workload identities. | ✅ Confirmed *(read)* |

### Tier 3 — Reference implementation

[kgrzybek/modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd)
is already ✅ Confirmed in the design register. For this topic use
[§3.9 Security](https://github.com/kgrzybek/modular-monolith-with-ddd#39-security) and its
[`HasPermissionAuthorizationHandler`](https://github.com/kgrzybek/modular-monolith-with-ddd/blob/master/src/API/CompanyName.MyMeetings.API/Configuration/Authorization/HasPermissionAuthorizationHandler.cs)
(read 2026-10-10) for the **shape** user → roles → permissions, with endpoints checking
permissions and never roles. **Do not copy the handler:** it returns `true` for every check
outside DEBUG builds.

## Findings from the 2026-10-10 review

1. **Built-in platform auth authenticates; it does not authorize.** App Service says so
   directly, and Container Apps uses the same system as a sidecar that hands claims to the app
   in headers. No equivalent was found in the Container Instances overview (only managed
   identity). Validating the token in app code behaves the same on all three hosts. *(SEC-6)*
2. **Entra's stated default is app roles; a custom role store is its most complex option**, and
   Entra cannot put custom-store roles into its tokens. *(SEC-5)*
3. **Roles assigned to users and to applications arrive in the same `roles` claim.** Entra
   recommends declaring different roles for each so the two can't be confused. *(SEC-5)*
4. **A fallback policy is the secure default** for endpoints that carry no authorization
   metadata. *(SEC-4)*
5. **Endpoint-level checks run before the resource is loaded**, so a rule that depends on the
   loaded record needs an imperative check (`IAuthorizationService`) after loading. *(SEC-4)*
   **Not needed on Throughline today (user, 2026-10-10):** access is per owner, the owner is
   known before any query, and queries are owner-filtered — so there is no per-record rule.
6. **Ungoverned (Claude's reasoning, not doctrine):** every approach needs a Throughline-side
   mapping from caller to owner, because Entra does not know Throughline's owner ids.
   SEC-7 says a tenant identifier is typically carried as a token claim and that workload
   identities must state which tenant they act for; it does not prescribe the mapping.

---

# WMS Functional / Vendor Reference (Project Canon)

Governs which **published WMS product documentation** backs a stated warehouse behavior on
Throughline. **Read this before writing any story or requirement (CLAUDE.md §4, §7B) and
before answering any warehouse business-logic question — including when the user asks you to
act as a business analyst / SME.** Throughline is **original and vendor-neutral**: these docs
establish *how real WMS behave functionally* so requirements are authentic; they never dictate
Throughline's schema, service boundaries, or proprietary internals.

This register carries its **own tier legend** (not the Confirmed/Pending legend the two
registers above use — vendor functional docs are behavioral references to ground requirements
in, not architectural doctrine that overrides instinct).

## Tier legend

- **Primary** — the default functional reference. Use it to establish core warehouse behavior;
  cite it for source-backed requirements.
- **Targeted** — consult selectively for the specific topics named; do **not** adopt its
  broader domain model.
- **Supplementary** — consult to resolve specific questions or strengthen a requirement in its
  named area; **not** a second primary specification.
- **Deferred / awareness** — may later challenge an established model, but must **not** expand
  the initial backlog or be treated as the behavioral standard now.
- **Comparison-only** — usable as an implementation comparison, **not** as the enterprise
  behavioral standard.

## Sources

| # | Source | Scope / use | Tier |
|---|--------|-------------|------|
| W-1 | **Infor WMS Cloud 2026.x** — [User & Admin Library](https://docs.infor.com/wms/2026.x/en-us/useradminlib/default.html) · [Allocation overview](https://docs.infor.com/wms/2026.09/en-us/useradminlib/sceorproug/ssj1612893899174.html) · [Allocation-strategy config](https://docs.infor.com/wms/2026.x/en-us/useradminlib/sceconfigug/allocation_strategy_header.html) | **Core warehouse behavior**: receiving & receipt expectations, putaway, inventory / LPNs / holds / availability, allocation strategies, replenishment, wave planning, picking, shipment staging & confirmation. | **Primary** |
| W-2 | **Microsoft Dynamics 365 Warehouse Management** — [Wave allocation & parallel processing](https://learn.microsoft.com/en-us/dynamics365/supply-chain/warehousing/wave-allocation-method) · [Wave templates](https://learn.microsoft.com/en-us/dynamics365/supply-chain/warehousing/wave-templates) · [Work templates & location directives](https://learn.microsoft.com/en-us/dynamics365/supply-chain/warehousing/control-warehouse-location-directives) | **Concurrency-focused** topics only: parallel wave allocation, inventory contention & locking, timeouts, work creation, work templates / location directives, and the failure behavior of those operations. **Do not** adopt Dynamics' ERP / legal-entity domain model. | **Targeted** |
| W-3 | **Oracle WMS Cloud 26B** — [Docs catalog](https://docs.oracle.com/en/cloud/saas/warehouse-management/26b/books.html) · [Allocation model](https://docs.oracle.com/en/cloud/saas/warehouse-management/26b/owmwr/allocation.html) · [Integration reconciliation](https://docs.oracle.com/en/cloud/saas/warehouse-management/26b/owmol/before-you-begin.html) | **Integration-focused** topics: APIs, integration reconciliation, scheduled processing, security, auditability, external-system boundaries. Supplementary — not a second primary spec. | **Supplementary** |
| W-4 | **SAP EWM** | May later challenge an established domain model. Do **not** use it to expand the initial backlog or as the behavioral standard now. | **Deferred / awareness** |
| W-5 | **Odoo** | Implementation comparison only; **not** the enterprise behavioral standard. | **Comparison-only** |
| W-6 | **Extensiv 3PL Warehouse Manager** — [Understanding Allocation Logic](https://help.extensiv.com/3pl-warehouse-manager-inventory-management/understanding-allocation-logic) · [Managing Warehouses — Prevent Partial Order Allocation](https://help.extensiv.com/en_US/managing-warehouses) | **3PL-native allocation baseline**: FIFO selection, allocation statuses/outcomes, explicit allocation of outstanding order quantities, and prevent-partial-order (completeness) control. A **Tier-3** (lighter, 3PL-focused) WMS — its simpler allocation model is the **sanctioned starting point for early Inventory/allocation stories**; Infor's richer rules (allocation strategies, rotation, minimum-ship %, line-level completeness) layer in **incrementally** in later stories. Cite for source-backed requirements in this 3PL-allocation baseline area. | **Supplementary** |

The links above are **starting points, not proof** of a particular behavior — read the
relevant page and cite it.

**Decision (2026-09-12):** Extensiv (W-6) is accepted for grounding **first-pass allocation**
because Infor's (W-1) full allocation model is too complex for an initial story. Start from
Extensiv's simpler 3PL model (FIFO, completeness, allocation statuses) and add Infor-level
complexity incrementally as later stories demand it. Infor remains **Primary** for the full
model; this is the deliberate-simplification choice required by research rule 5.

## Research & reasoning rules

1. **Persona first.** Start from a concrete warehouse persona, operational objective, and
   scenario — then read docs to back it. Never invent a business need to exercise a pattern
   (see CLAUDE.md §1, §8 and [[stories-must-be-business-honest]]).
2. **Read, then cite.** Cite the specific page(s) supporting each source-backed requirement.
   Record the accessed release / page-update date where behavior may vary between releases.
3. **Paraphrase.** Never reproduce substantial vendor passages (copyright).
4. **Separate the three voices.** Keep *documented vendor behavior*, *Throughline project
   decisions*, and *unresolved questions* clearly distinct in stories and answers.
5. **Reconcile conflicts deliberately.** When sources disagree, explain the alternatives and
   choose intentionally (record the choice; an ADR if it's structural).
6. **Stay above the implementation line.** Do **not** infer requirements for a vendor's
   database schema, service boundaries, or proprietary internals — model the domain (CLAUDE.md
   §4.4: "never claim to clone a specific vendor's internals").
7. **Failures motivate architecture, not the reverse.** Use realistic business failures to
   justify patterns; don't manufacture failures to reach a technique.

## Scope guard

Do not expand into **WCS, TMS, billing, multiple warehouses, or user interfaces** without an
explicit scope decision from the user. Represent external OMS / ERP / carrier / automation
systems through **contracts and simulators**, not by modeling their internals. (Consistent
with the deferred-Billing and single-facility decisions in CLAUDE.md §2.)
