# ADR-0004 — Destination zone-charge table: ZIP-range model and membership rule

## Status

Accepted — 2026-08-12

## Context

STORY-0001 (#1) prices an order's estimated fulfillment quote as
`Σ(line pick fee + weight handling) + destination-zone surcharge`, where the destination
**zone is derived from the address using the current rate/zone table**. This ADR fixes two
things the story deliberately left abstract so the modeling choice is recorded, not implied
by code:

1. **Whose surcharge is this, and is it carrier-dependent?**
2. **How is the destination-zone surcharge table structured, and how is a destination's zone
   membership decided?**

The trigger was a real ambiguity. "Surcharge" is also a carrier term — UPS/FedEx publish a
**Delivery Area Surcharge (DAS)**: a carrier-specific, enumerated list of destination ZIPs,
each mapped to a tier and per-package fee, refreshed quarterly/annually. If the story's
surcharge were a carrier DAS, it would be **carrier-dependent**, which would require assigning
a carrier at estimate time and pull in carrier rate-shopping. That path is a different bounded
context (Shipping/Carrier) and is **explicitly out of scope** for STORY-0001 (line 44).

USPS is a third, unrelated concept: USPS prices **distance-based postage zones (1–9)** derived
from **3-digit ZIP prefixes** via a zone chart — that is base postage granularity, not a
destination surcharge, and also not what this story models.

## Decision

**1. The destination-zone surcharge is Throughline's own, carrier-independent fee.**
Throughline is the 3PL; the surcharge is part of Throughline's rate/zone table that it charges
the merchant, decided before any carrier is chosen. It is **not** a carrier DAS and **not** a
USPS postage zone. No carrier is assigned when an order is estimated. This is consistent with
ADR-0002 (per-merchant, USD pricing): the table is Throughline's to define.

**2. The table is modeled as start/end ZIP-code ranges.**
Each zone-charge entry is a contiguous range defined by a **start ZIP** and an **end ZIP**
(both 5-digit), carrying a surcharge amount. Throughline owns these ranges; there is no
external authority (USPS or any carrier) whose boundaries must be matched.

**3. Membership rule (the one correctness rule that matters here).**
A destination ZIP is in a zone-charge range **iff its first 5 digits are ≥ the range's start
ZIP and ≤ the range's end ZIP** (inclusive on both ends). Because comparison is on the **first
5 digits only**, a ZIP+4 destination reduces to its 5-digit parent for membership:
`05003-0001` → `05003`, which is in `[05001, 05003]`. The +4 add-on never changes zone
membership. (5-digit ZIPs are zero-padded, so lexical and numeric ordering agree.)

## Alternatives considered

- **Carrier DAS (enumerated 5-digit ZIP set, per carrier).** Rejected: carrier-dependent,
  drags in carrier assignment + rate-shopping, both out of scope for STORY-0001. Belongs to a
  future Shipping/Carrier context if ever modeled.
- **USPS 3-digit prefix zone chart.** Rejected: that is distance-based *postage* zoning, a
  different concept from a destination surcharge, and finer/coarser in ways irrelevant here.
- **Full-precision comparison including the +4 (total order over the whole code).** Rejected:
  it makes a ZIP+4 of a range's *end* fall outside the range (`05003-0001 > 05003`), which is
  arbitrary — there is no business reason to place a +4 in a different zone from its 5-digit
  parent. The first-5-digit rule avoids this entirely.

## Consequences

**Positive**
- Membership is a deterministic 5-digit comparison — no clock, no external lookup — which
  directly serves the story's **exact** and **reproducible** quote ACs.
- Throughline owns the ranges, so the model carries no dependency on carrier or USPS data and
  no carrier need be resolved to produce an estimate.
- +4 handling is unambiguous and testable: normalize to 5 digits, compare inclusively.

**Negative / costs**
- A range model assumes surcharge zones are **contiguous** ZIP spans. Real hard-to-serve areas
  are often scattered; if Throughline's future zones aren't contiguous, ranges may fragment or
  need many rows. Accepted for now as the simplest faithful model for this story.
- **Range disjointness is an invariant of the context that owns the rate table — not
  Ordering's.** "Ranges must be disjoint (no overlaps)" is a *collection-level* invariant: it
  constrains the relationship between ranges, so a single `PostalZone` cannot enforce it. That
  forces the ranges to be children of a **rate-table aggregate** in the owning **Pricing/Rating**
  context (not yet formally carved in this repo), whose consistency boundary is scoped
  **per merchant** (per ADR-0002 — two merchants' ranges may overlap freely; only one merchant's
  ranges must be disjoint). That aggregate enforces non-overlap on every mutation, or the rule is
  validated at the table's publish/ingestion boundary if the table is authored as a batch — either
  way the table is never persisted in an overlapping state.
- **Ordering consumes a table it is entitled to assume is already disjoint.** As the downstream
  consumer (customer/supplier, per ADR-0003), Ordering must **not** re-check for overlaps or carry
  a tie-break at quote time; it resolves exactly one zone per destination and trusts the supplier's
  invariant. A tie-break in Ordering would only be needed if the upstream invariant weren't
  actually enforced — which would be a bug in the Pricing context, not something Ordering should
  paper over.

**Revisit when:** Throughline needs carrier-specific surcharges (introduces a Shipping/Carrier
context and carrier assignment — reopens decision 1), or zone definitions stop being
expressible as contiguous ZIP ranges (reopens decision 2).

## Related

- STORY-0001 (#1) — order intake & fulfillment quote; the surcharge is the third quote term.
- ADR-0002 — per-merchant, USD pricing (this table is Throughline's to define per merchant).
- Items: T-018 (Value Objects — `PostalCode`, `PostalZone`), T-023 (Domain Services — the
  pricing/zone resolution consuming the external rate table).
