# Authoritative Sources (Project Canon)

Governs how Claude vets responses on **Throughline**. Read this before any design,
architecture, or DDD answer, and ground claims against it.

This file holds **two source registers under one status legend**: the **design /
architecture / DDD** set (the table below) and an **Observability & Logging** set (at the
end of the file). Read the latter before designing, building, or reviewing any logging,
tracing, metrics, or correlation — the logging subsystem is not yet implemented, so these
are the sources that govern both how it gets built and how it gets reviewed.

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
