# STORY-0001 — Order Intake & Fulfillment Quote

- **Status:** Ready
- **Difficulty:** Core
- **Est. time box:** ~3–4h
- **Targets:** T-017 (Aggregates & Aggregate Design), T-018 (Value Objects vs Entities), T-023 (Domain Services vs Application Services); optional T-001 (Domain Events)
- **Bounded context(s):** Ordering
- **Created:** 2026-08-02

## Business context
Throughline ingests orders from thousands of merchants across their sales channels —
millions of orders/day at seasonal peaks, arriving via a public intake API that clients
**will** retry. Before any warehouse work is committed, Throughline acknowledges each order
with an **estimated fulfillment quote** (per-item pick fee + weight-based handling + a
destination-zone surcharge) so the merchant sees, at intake time, that the order is valid
and roughly what it will cost. This story builds the **Ordering** domain model behind that
intake acknowledgment.

## User story
As a **merchant integration**, I want to **submit an order and get back an acknowledged order
with an estimated fulfillment quote**, so that **I know the order is valid and what it will
cost before fulfillment begins**.

## Scope
**In scope:** The Ordering domain model — the `Order` aggregate + its `OrderLine` entity, the
supporting value objects, a **domain service** that computes the estimated quote, and an
**application service** that orchestrates intake. Unit tests are required. A Minimal API
endpoint to drive it is optional.

**Out of scope:** persistence infrastructure beyond a trivial in-memory or simple EF repo;
domain-event **dispatch**/outbox; real billing; inventory reservation; carrier rate-shopping;
any cross-module wiring. Keep it inside the Ordering module.

## Acceptance criteria
1. Submitting an order with **≥1 valid line**, a **destination address**, and a **service
   level** returns a submitted order with a stable `OrderId` and a computed **estimated
   quote** in a single currency.
2. An order **cannot be submitted with zero lines** — rejected with a domain error.
3. A line **quantity must be ≥ 1**; invalid quantities are rejected at construction, not later.
4. Line management (adding the **same SKU twice**, etc.) is enforced **through the aggregate
   root** — callers cannot reach in and mutate the line collection directly. You decide whether
   a duplicate SKU **consolidates** (sum quantities) or is **rejected**, and justify it.
5. **Money arithmetic never mixes currencies** — adding two `Money` values of different
   currencies is a domain error. Money is exact (no floating-point money).
6. **Value equality holds** where it should: two `Address` values with identical fields are
   equal; the estimated quote is a **pure function** of (lines, weights, destination zone,
   rate table) — same inputs always yield the same quote.
7. Once submitted, the order's **lines and destination are immutable**; attempts to modify
   throw a domain error. It must be **impossible to obtain an `Order` in an invalid state**.
8. The estimated quote = `Σ(line pick fee + weight handling) + destination-zone surcharge`,
   where the **zone is derived from the destination address** via a rate/zone table **supplied
   to the calculator** (not embedded in the order).
9. *(Optional / stretch — T-001)* On successful submission, the `Order` **records** an
   `OrderSubmitted` domain event (OrderId, MerchantId, line count, quote) on the aggregate.
   Recording only — no dispatch machinery.

## Constraints & non-functional requirements
_These are what make the target patterns the natural/forced solution — don't remove them._
- The **zone→surcharge** and **per-SKU pick fees** come from a **rate table that is not part
  of the Order** and changes over time. The `Order` must not embed, cache, or query it.
  → *forces the pricing logic into a domain service, not an aggregate method.*
- Intake is **write-heavy and retried**; constructing/submitting an `Order` must be
  **deterministic and side-effect-free** — no hidden clock reads or rate lookups inside the
  aggregate. → *forces orchestration (load rates, call calculator, persist, return ack) into an
  application service, distinct from the pure domain.*
- **All invariants hold inside the aggregate boundary** — encapsulated collections, controlled
  construction. → *forces factory/constructor discipline; no invalid instance is reachable.*
- **No primitive obsession** on domain-meaningful values (money, SKU, quantity, weight,
  address). → *forces value objects.*

## Why this exercises the target skills
- **Aggregates (T-017):** `Order` is the consistency boundary. Every invariant — non-empty,
  post-submit immutability, line management — is enforced through the root, and the internal
  line collection never leaks. *Traps:* exposing `List<OrderLine>` directly; letting callers
  build an invalid order then "validate later"; putting the rate lookup on the aggregate so it
  reaches outside its own boundary.
- **Value Objects vs Entities (T-018):** `Address`, `Money`, `Sku`, `Quantity`, `Weight` are
  immutable value objects with value equality and construction-time validation; `OrderLine` is
  an **entity** with identity *inside* the aggregate. *Traps:* making `OrderLine` a value
  object (or `Money` an entity); value-equality bugs from mutable/collection fields; reaching
  for `decimal`/`string` where a value object belongs.
- **Domain Services (T-023):** the quote calculation needs the rate/zone table — knowledge that
  belongs to **no single aggregate** — so it lives in a **stateless domain service** operating
  on domain types, while the **application service** orchestrates. *Traps:* the anemic-domain
  failure (pricing math ends up in the application service); or the opposite, the aggregate
  reaching out to fetch rates. Be able to articulate *why each piece of logic lives where it
  does* — that articulation is the actual skill T-023 targets.
- **Domain Events (T-001, optional):** practice *when* to raise and *what* to include, by having
  the aggregate record `OrderSubmitted` at the moment of the invariant-protected transition —
  without the distraction of dispatch/outbox (that's a later story).

## Hints (optional — ignore if you want the full challenge)
- Consider a private constructor + a static factory (e.g., `Order.Submit(merchantId, lines,
  destination, serviceLevel, quoteCalculator)`), or a `Draft → Submitted` two-step if you prefer
  an explicit lifecycle. Either is defensible — **decide deliberately and be ready to justify it.**
- Model the rate table behind a small **domain abstraction** (e.g., `IRateCard` /
  `IZoneResolver`) that the domain service consumes; the application service supplies the
  concrete data. Keep the domain service free of I/O.
- C# `record` types help with value equality — but watch collection/mutable fields, which break
  structural equality quietly.
- Put it in the Ordering module of the `Throughline` solution (`Throughline.Modules.Ordering.*`).
  Domain types in `.Domain`, the application service in `.Application`.

## Definition of done
- Acceptance criteria met; unit tests cover **each invariant** and the **quote calculation**,
  including the **currency-mismatch** and **duplicate-SKU** cases; deterministic (no wall-clock
  dependence). Note which `src/` project(s) you placed things in. Then ask for a `review` pass.
