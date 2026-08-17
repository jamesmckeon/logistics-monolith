# ADR-0005 — Error taxonomy for order estimation: Validation at the boundary vs Unavailable for reference-data gaps

## Status

Accepted — 2026-08-14

## Context

The Ordering module reports failures through a `Result<T>` carrying `Error`s, each tagged
with an `ErrorType` enum. Building the estimate for an order (STORY-0001) has two distinct
failure sources that were being conflated under one vague category:

1. **The submission itself is malformed** — a blank required field, a zero-quantity line,
   no items, an ill-formed postal code. This is the *caller's* input.
2. **Reference data needed to price a valid submission is missing** — a submitted SKU has no
   pick fee or no attributes, the destination falls in no configured zone, or the merchant has
   no handling rate. `OrderEstimateRequestBuilder` discovers these while self-sourcing its parts.

At the time the enum held only `Validation` and `Unexpected`, and neither fits case 2:

- **`Validation` asserts the client's input was bad.** But a missing lookup datum is observed
  only as *absence*, and absence carries no blame: it is equally consistent with "the merchant
  sent a SKU we don't stock" and "the SKU is legitimate but Throughline's pick-fee table is
  stale." The builder cannot distinguish these, so it must not blame the caller.
- **`Unexpected` is the bucket for the un-foreseen** — an unhandled throw, a bug. An unmaintained
  rate table is the opposite: a *foreseeable* operational failure. Tagging it `Unexpected` is
  both vague and semantically wrong.

The deciding premise (see Related): **Throughline guarantees its reference data is complete and
current.** Under that guarantee, a lookup gap at estimate time is by definition a Throughline-side
failure, not something the merchant can fix by changing their request.

Two audiences pull on the answer: what we *return to the customer* (a category, mapped later to
an HTTP status) versus how we *classify internally* (for logs/alerting). The taxonomy below is
the customer-facing category; the specific cause ("no handling rate for merchant X") stays in
telemetry.

## Decision

`ErrorType` for the Ordering estimate flow is assigned as follows:

- **`Validation`** — client-attributable, structural/format problems, caught at the **command
  boundary** in `CreateOrderCommand.Create`: missing/blank required fields, quantity < 1, zero
  items, invalid postal-code format. Because a command exists only via `Create`, downstream code
  (the builder, the aggregate) assumes structural validity and does **not** re-validate.
- **`Unavailable`** — Throughline-side **reference-data gaps** found while building the estimate:
  no pick fee, no SKU attributes, destination in no zone, or no merchant handling rate. This is
  blame-neutral to the caller (their request was valid; our data doesn't cover it), is **alarmed
  on**, and logs the missing datum. The builder **fails fast** on the first gap — each resolution
  is an I/O round-trip, so aggregating every miss is not worth the extra calls.
- **`Unexpected`** — reserved for the genuinely un-foreseen (unhandled faults/bugs). It is **not**
  used for missing reference data.

Blame the caller only where the caller is provably at fault (structural input), and only at the
boundary that owns that judgment. Everything the builder resolves is Throughline's own data, so
any gap it finds is Throughline's.

## Consequences

**Positive**

- Clean, honest mapping to transport later: `Validation` → 4xx, `Unavailable` → 503,
  `Unexpected` → 500. The category matches who is actually at fault.
- The merchant is never told to fix a problem that is ours. A stale rate table surfaces as an
  **operational signal** (alarm + logged datum), not a silent client rejection that hides our
  maintenance failure.
- The taxonomy stays small and each member means one thing; `Unexpected` is no longer a
  catch-all.
- Structural validation stays at the boundary (parse, don't validate); the builder and aggregate
  are freed from re-checking input.

**Negative / costs**

- **The "Throughline guarantees the data" premise is load-bearing.** If Ordering ever accepts open
  SKU strings without upstream catalog validation, a SKU gap is no longer unambiguously ours, and
  the `Unavailable` classification for that case must be revisited (a blame-neutral not-found plus
  telemetry, rather than a system error).
- **`Unavailable` conventionally implies *transient* / retryable**, but a missing config is not
  self-healing on retry — it clears when Throughline fixes the data. Accepted: still far more
  honest than `Validation` or `Unexpected`. The durable-vs-transient nuance is handled at the HTTP
  edge (a generic 503 backed by an internal "configuration/data-integrity" cause in logs), not by
  this enum.
- The `ErrorType` → HTTP-status mapping is **deferred** — no API surface exists yet. This ADR
  fixes the categories; the transport mapping is decided when that layer is built.

**Alternatives considered**

- **A closed union of domain-specific error cases** (Wlaschin school: `SkuNotPriceable`,
  `DestinationNotServiceable`, `MerchantRateNotConfigured`) rather than a generic enum. More
  type-honest and exhaustively switchable; heavier. Deferred — worth practicing once, overkill to
  adopt repo-wide now.
- **`NotFound` for the reference-data gaps.** Rejected as the primary answer: it reads as
  client-facing ("the thing you referenced isn't here") and risks re-introducing blame via a 4xx,
  when the fault is ours.
- **A dedicated `Configuration` / `Misconfiguration` member.** Useful as an *internal* cause label,
  but too internal to surface to a merchant. Kept for logs; not added to the customer-facing enum.

**Revisit when:** SKU validity stops being guaranteed upstream (reopens the `Unavailable` choice
for SKU gaps), a Result-taxonomy source is confirmed in the canon (may impose a standard status
set), or the API layer is built (fixes the enum → HTTP mapping and the transient-retry semantics).

## Related

- STORY-0001 (#1) — order intake & fulfillment quote; the flow these errors arise in.
- ADR-0004 — the zone table Ordering consumes; a destination in no zone is one `Unavailable` case.
- `IOrderEstimateRequestBuilder` — contract naming `Unavailable` for reference-data gaps.
- Items: T-023 (Domain vs Application services — validation-at-boundary vs builder resolution).
