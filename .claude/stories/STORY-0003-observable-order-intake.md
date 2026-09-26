# STORY-0003 — Observable, Owner-Scoped Order Intake

> **Source of truth:** GitHub issue [#5](https://github.com/jamesmckeon/logistics-monolith/issues/5).
> The sections above the scaffolding divider mirror the issue body. Makes the intake path from
> issue [#4](https://github.com/jamesmckeon/logistics-monolith/issues/4) observable — changes no
> intake behavior.

- **Status:** Ready
- **Difficulty:** Core (leans Stretch on the correlation adopt/echo + concurrent-attribution correctness)
- **Est. time box:** ~3–4h
- **Skills reviewed through:** T-035 (Structured Logging), T-036 (Distributed Tracing & Correlation IDs)  ← the lens, not the reason this story exists
- **Bounded context(s):** Order Intake & Pricing *(existing `Ordering` module)*; cross-cutting **Observability**
- **Owner(s) / Facility:** any billed-3PL client-owner; single DC (facility not modeled — consistent with STORY-0002)
- **Created:** 2026-09-05

## Business context

Throughline runs a **shared, multi-client DC**: ~40 client-owners transmit warehouse shipping
orders at the §2 baseline (~40k order-lines/day, 3–4× seasonal peaks) under a **99.9% availability
commitment on the intake path**. Today that path — the endpoints that accept an order
(`POST /orders`) and retrieve one (`GET /orders/{orderId}`) — **emits nothing**. When an owner's
integration calls asking *"we submitted 200 orders this morning, 12 came back rejected — which ones
and why?"* or *"order X was never acknowledged, what happened?"*, on-call — who operates the DC on
behalf of **all** owners — has no way to reconstruct what happened to a **single submission**, and
no reliable way to **attribute** each record to the client it belongs to. During a peak, triaging a
spike of failures is guesswork. **Operability of the intake path is the requirement.**

**Units** are unchanged from STORY-0002 (eaches). This story **changes no intake behavior** — it
makes the existing path observable.

## User story

As an **on-call / operations engineer at the 3PL operator** — who legitimately works across **all**
client-owners — I want to **reconstruct exactly what happened to any single order submission, see
every unit of work done for it tied together, and have each record correctly attributed to its
owning client**, so that **I can answer an owner's "what happened to this order?" and triage intake
failures against our availability commitment.**

## Scope

**In scope** — for the existing intake path (`POST /orders` accept/reject and
`GET /orders/{orderId}` retrieve):
- Every request on the path (any endpoint) emits **one structured, machine-parseable record** — a
  **per-request pipeline concern** — carrying the method, route, response status, duration, and
  **owning client**, plus the order's **operational attributes as structured fields**: the **client
  identifiers** (purchase-order + reference number), the **order id** on acceptance, and the
  **reason code(s) + offending field/line (with SKU and quantity)** on rejection. Emit as **named
  fields, not an opaque body blob**, so records stay queryable.
- Every emitted record — and every unit of work done while handling one request (the handler /
  query and the database call) — carries **one correlation identity** that ties them together and
  is **stable for the whole request**, so all records for one submission can be pulled as a set.
- That correlation identity is **adopted from the caller** when presented in the **standard
  interchange form** and **propagated onward** unchanged (to the DB call and to any future
  module/service); when **absent**, the system **originates one and returns it** to the caller.
- The **owning client** appears on **every** emitted record as a first-class, **filterable**
  attribute, so an operator can **pivot telemetry by owner** and attribute every record to the
  right client. Operators see across all owners — this is *attribution*, **not** isolation.

**Out of scope:**
- **Metrics, dashboards, SLO / error-budget measurement, and alerting** — the next story (T-013).
- Health / readiness / liveness endpoints.
- Any change to **intake validation or outcomes** (STORY-0002 semantics stay identical), including
  the **owner-scoping of the data path itself** — an owner integration still sees only its own
  orders; that isolation is STORY-0002's (T-033), not this story's.
- Deploying a **telemetry backend / collector** — emit in the standard shape; *where* it ships is
  config, not this story.
- **Personal-data governance on telemetry export** — order attributes (including ship-to) are
  operational data, logged **in full** to Throughline's own logs. *If/when* telemetry is exported
  to a third-party backend (deferred with the collector decision above), revisit which consumer
  personal data may cross that processor boundary (DPA / retention / jurisdiction). **Not a v1
  redaction requirement.**
- **Facility** on telemetry — single DC; not modeled.

## Acceptance criteria

1. **Every request on the intake path — any endpoint, any outcome — produces exactly one
   structured, correlated record** carrying the request method, route, response status, and
   duration, tagged with the owning client. This is a **per-request pipeline concern**, not
   per-endpoint / per-outcome logging.
2. **Any order is identifiable, and the outcome diagnosable, from explicit structured fields on the
   record.** Every record carries the **client's own identifiers** — the **purchase-order number and
   reference number** — so *any* order can be found, **including a rejection, which has no order
   id**. On acceptance the record also carries the assigned **order id**; on rejection it carries
   the **reason code(s) and the offending field / line, with the line's SKU and quantity**.
3. Given any single request, when its emitted records are collected, then **all** of them — across
   the request record, the handler/query, and the database call — **share one correlation
   identity**, and that identity is **stable** for the whole request.
4. Given a caller that presents a correlation identity in the **standard interchange form**, when
   the request is handled, then that identity is **adopted** (not replaced) and **echoed back** to
   the caller; given a caller that presents **none**, then one is **originated** and returned.
5. Given **two owners' requests handled concurrently**, when their records are emitted, then each
   record is **attributed to the owner whose request produced it** — no owner id, order id, or data
   **bleeds** onto another owner's record (a per-request scoping / async bug must never
   mis-attribute).
6. **Observation does not alter behavior:** submissions accepted/rejected before this story behave
   **identically** after it, and a **telemetry failure never fails a valid order**.

## Constraints & non-functional requirements

_Business rules and NFRs that make the correct design necessary — stated as rules, not technique
names._
- The intake path carries a **99.9% availability** commitment and **sub-second reads** (§2):
  **telemetry must not become a latency or failure source on the hot path** — emitting a record
  must add no meaningful latency and must **never throw into the request** (no blocking flush on
  the request thread; drop rather than fail).
- Records must be **queryable by owning client and by correlation identity** — free-text prose a
  human must eyeball does **not** satisfy this.
- The correlation identity must use the **standard, widely-interoperable interchange form** so it
  survives a future **split of a module into its own service without a new scheme** — a bespoke
  in-process-only id fails criterion 5.
- **Telemetry is not owner-isolated** — operators work across all owners. What matters is that
  every record is **correctly and unambiguously attributed** to its owning client so activity can
  be filtered and pivoted by owner; **mis-attribution** (owner A's record tagged owner B) is a
  correctness bug.
- **Order attributes are operational data, logged in full** — the ship-to address, SKUs, and
  quantities are what the warehouse acts on, and the 3PL's own operators need them for triage; this
  story does **not** redact them from internal logs. (Personal-data governance applies only if
  telemetry is later exported to a third-party backend — see Scope / out-of-scope.)

## Open questions

- **Rejection severity:** a business rejection (zero-line, bad quantity) is *expected flow*, not a
  system fault — record it at an informational/warning level, reserving error-level strictly for
  unexpected faults? (Recommend yes.)
- **Correlation echo:** confirm the response surface for returning the correlation identity to the
  caller (standard trace-response vs an explicit response header).

---

# — Claude scaffolding (not in the issue) —

## Skills this exercises

_Kept separate from the requirement on purpose — the story must stand on its own even if this were
deleted._

- **Structured Logging (T-035):** message templates with **named properties**
  (`"Order {OrderId} accepted for owner {OwnerId}"`), never interpolated strings; **outcome**,
  **reason**, and **owning client** as queryable fields (owner is a *filter dimension* so an
  operator can pivot by client — see the tenancy note below). *Traps:* `$"…{x}…"` interpolation
  that destroys structure; log-and-throw / double-logging the same failure; **level misuse** (a
  business rejection is expected flow — Information/Warning, not Error — reserve Error for
  unexpected faults).
- **Distributed Tracing & Correlation IDs (T-036):** one trace spanning
  endpoint → handler/query → DB, using **W3C Trace Context (`traceparent`)**; **adopt** inbound
  context, **propagate** it to child work, and stamp the trace/correlation id onto every log record
  so logs and the trace **join**. *Traps:* inventing a bespoke `X-Correlation-Id` instead of
  standard trace context; starting a **new root** when one was supplied (breaking the chain);
  failing to flow the id to the DB call; hand-rolling `AsyncLocal` instead of using the platform
  primitive that already survives `await` (the same primitive keeps each request's owner from
  bleeding onto another's record under concurrency — criterion 6).

**Tenancy & data note — three things people conflate; be clear which this story owns:**
1. **Owner-vs-owner isolation** (one client never sees another's data) — the **data path**, already
   enforced by STORY-0002 / T-033. **Not** re-litigated here, and it does **not** apply to internal
   telemetry: the operator is the 3PL and sees *all* owners.
2. **Owner attribution** (every telemetry record is tagged with — and filterable by — the correct
   owning client, with no cross-request bleed) — **in scope**, part of T-035/T-036.
3. **End-customer personal data** — order attributes including the ship-to address are the WMS's
   **operational data**, logged **in full** to our own logs (a 3PL operator's on-call needs them);
   **not** redacted in v1. The only real caveat is *data-governance on export* to a third-party
   telemetry backend (deferred) — never a reason to strip order data from internal logs.

*The central trap:* **treating logging as print statements.** At this scale an uncorrelated,
unstructured, owner-blind line is nearly useless for triage — the value is that **every record for
one submission joins on one id and is filterable by owner.**

## Hints (optional — ignore if you want the full challenge)

- .NET's own primitives do the hard parts: `System.Diagnostics.ActivitySource` / `Activity` gives
  you **W3C `traceparent` propagation** and a **stable id across `await`** via `Activity.Current` —
  no custom correlation header, no hand-rolled `AsyncLocal`. `ILogger.BeginScope` attaches
  `OwnerId` + ids to every record inside the scope (and, being per-request, is what stops owner
  bleed under concurrency). `[LoggerMessage]` source-gen keeps the hot path allocation-free.
- ASP.NET Core already reads an inbound `traceparent` into `Activity.Current` when tracing is
  configured — you mostly **consume** it, then **originate-and-return** when it's absent.
- **PII on export (deferred, not v1):** order attributes are logged as operational data now — no
  redaction. If a third-party telemetry backend is added later, `Microsoft.Extensions.Compliance`
  (`[LogProperties]` / `DataClassification`) is how you'd classify/redact consumer PII at *that*
  boundary — nothing to redact for this story.
- The concrete surface to make observable already exists: `OrderingExtensions.MapOrdering`
  (`POST /orders` → `CreateOrderHandler.CreateOrderAsync(ownerId, …)`, `GET /orders/{orderId}` →
  `GetOrderByIdQuery`), with `RequestContext.OwnerId` as the owner and the EF/Npgsql call as the
  downstream work to correlate.
- Authoritative sources are the **Observability & Logging** register in
  [sources.md](../sources.md) — O-1 (OTel), O-2 (W3C Trace Context), O-5 (structured / high-perf
  logging), O-6 (distributed tracing in .NET), O-8 (redaction / data-minimization). This story is
  scoped so metrics / SLOs (O-7, O-11) come next.

## Definition of done

- Acceptance criteria met; tests cover **each rule** — one structured / correlated / owner-tagged
  record per request (method, route, status, duration); **any order findable via the client's
  PO/reference number, including on rejection**; **order id on accept**, **reason code + offending
  field/line (SKU/qty) on reject**; **one correlation id across a request**; **adopt-vs-originate**
  correlation; **concurrent requests never mis-attribute the owner**; and **behavior-unchanged** (a
  telemetry failure never fails a valid order). Note which `src/` project(s) you touched. Then ask
  for a `review` pass.

## Issue

Filed as [#5](https://github.com/jamesmckeon/logistics-monolith/issues/5) on 2026-09-05 (title:
*"Order intake observability: structured, owner-scoped, correlated telemetry on the intake path"*).
Everything above the scaffolding divider mirrors the issue body.
