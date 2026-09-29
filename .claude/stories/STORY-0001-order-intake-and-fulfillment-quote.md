# STORY-0001 — Order Intake & Fulfillment Quote

> **Source of truth:** GitHub issue [#1](https://github.com/jamesmckeon/logistics-monolith/issues/1).
> This file is Claude's working copy — the spec sections below mirror the issue verbatim;
> the sections under "— Claude scaffolding —" are mentoring notes that don't belong in the issue.

- **Status:** Ready
- **Difficulty:** Core
- **Est. time box:** ~3–4h
- **Targets:** T-017 (Aggregates & Aggregate Design), T-018 (Value Objects vs Entities), T-023 (Domain Services vs Application Services); optional T-001 (Domain Events)
- **Bounded context(s):** Ordering
- **Created:** 2026-08-02
- **Updated:** 2026-08-06

## User story

As a **merchant integration**, I want to **submit an order and receive an acknowledgment that includes an estimated fulfillment quote**, so that **I know the order was accepted as valid and what it will cost before any fulfillment work begins**.

## Background

Throughline ingests orders from thousands of merchants — millions per day at seasonal peaks — through a public intake API that clients will retry. Before committing any warehouse work, we acknowledge each order with an **estimated fulfillment quote**: per-item pick fee + weight-based handling + a destination-zone surcharge. This story covers the Ordering domain behind that acknowledgment.

**Units:** order quantities are in individual sellable units (**eaches**) — a quantity of 1 means one unit of that SKU, not a carton or pallet.

## Acceptance criteria

- [ ] Given a submission with **at least one line**, a **destination address**, and **every line valid**, when it is submitted, then the response is an accepted order with a **stable order id** and an **estimated quote** in the **merchant's currency**.
- [ ] Given a submission with **zero lines**, when submitted, then it is **rejected** as invalid.
- [ ] Given a submission containing a line with **quantity < 1**, when it is submitted, then the **entire submission is rejected** and **no order is created** (every line's quantity must be ≥ 1) — invalid lines are not dropped or corrected.
- [ ] Given the **same SKU submitted more than once**, when the order is built, then the outcome is well-defined and consistent (either consolidated into one line, or rejected — see Open questions).
- [ ] Given a computed quote, then it is denominated in the **currency the merchant uses** and every amount is **exact** — pick fees, weight handling, and the destination surcharge total to the quote with **no rounding drift or penny errors**.
- [ ] Given the **same order quoted more than once against the same rate table**, then the price is **identical every time**.
- [ ] Given an order that has been **submitted**, when a caller attempts to change its lines or destination, then the change is **rejected** (contents are locked once submitted).
- [ ] The estimated quote equals `Σ(line pick fee + weight handling) + destination-zone surcharge`, where the destination **zone is derived from the address** using the current rate/zone table.

## Constraints / non-functional

- Per-SKU pick fees and zone surcharges live in a **rate/zone table that changes over time and is external to the order**; the order must not embed, cache, or look it up itself. A quote reflects the rates in force when it was made.
- Intake is **write-heavy and clients retry**. Submitting the same order must behave **predictably** — building/submitting an order must not depend on the clock or on data that can change underneath it.
- The system must **never expose, store, or hand back an order that breaks these rules** — not even briefly.

## Out of scope

- Persistence beyond a trivial store; inventory reservation; carrier rate-shopping; real billing/invoicing; publishing events to other modules.
- **Retry / duplicate-submission (idempotency) handling** — recognizing that the *same order* arrived twice is a later story. (This is distinct from the duplicate-*SKU*-within-one-order question below, which is in scope.)

## Open questions

- **Duplicate SKU** within a single submission — consolidate quantities, or reject?
- **Order lifecycle** — a single submit step, or an explicit `draft → submitted` transition?

---

# — Claude scaffolding (not in the issue) —

## Why this exercises the target skills

- **Aggregates (T-017):** the `Order` is the consistency boundary. The zero-lines,
  quantity, duplicate-SKU, and post-submit-immutability rules — plus "never expose, store, or
  hand back an order that breaks these rules, not even briefly" — force you to enforce every
  invariant **through the aggregate root** and never leak the internal line collection.
  *Traps:* exposing `List<OrderLine>` directly; letting callers build an invalid order then
  "validate later"; putting the rate lookup on the aggregate so it reaches outside its own
  boundary.
- **Value Objects vs Entities (T-018):** `Address`, `Money`, `Sku`, `Quantity`, `Weight` are
  immutable **value objects** with value equality and construction-time validation — that's
  what the exact-money-in-a-single-currency criterion and "amounts are real concepts, not bare
  numbers" are really describing (e.g. a `Weight` can't be accidentally added to a pick fee; a
  destination is identified by its `Address`, so two orders to the same address go to the same
  place). `OrderLine` is an **entity** with identity *inside* the aggregate. The **quote itself
  is a value object** — which is why reproducibility is, underneath, a *pure function* of
  (lines, weights, zone, rate table). *Traps:* making `OrderLine` a value object (or `Money` an
  entity); value-equality bugs from mutable/collection fields; reaching for `decimal`/`string`
  where a value object belongs; representing money as lossy floating-point.
- **Domain Services vs Application Services (T-023):** the quote calculation needs the
  rate/zone table — knowledge belonging to **no single aggregate** — and the "rate table is
  external to the order" constraint keeps it off the aggregate. The **pricing rules are domain
  logic** and must not live in the orchestration layer (the *anemic-domain trap*: pricing math
  ends up in the handler); the "predictable, side-effect-free submission" constraint pushes
  orchestration (load rates → price → return ack) into an **application service**, distinct from
  the pure domain logic. Whether the pricing lives in a dedicated **domain service** or on
  **rich domain types** is your call — be able to articulate *why each piece of logic lives
  where it does.* That articulation is the actual skill T-023 targets.
- **Domain Events (T-001, optional stretch — not a committed AC):** consider recording, on the
  aggregate at the invariant-protected moment of submission, that the order was submitted
  (order id, merchant, line count, quote) so downstream systems could consume it later. Practice
  *when* to raise and *what* to include, without the distraction of dispatch/outbox (a later
  story). If you want this as a real requirement, add it to the issue first.

## Hints (optional — ignore if you want the full challenge)

- Consider a private constructor + a static factory (e.g., `Order.Submit(merchantId, lines,
  destination, quoteCalculator)`), or a `Draft → Submitted` two-step if you prefer an explicit
  lifecycle. Either is defensible — **decide deliberately and be ready to justify it.** Watch
  the window in the immutability rule: building an empty order then adding lines can
  *transiently* violate the non-empty rule; constructing with the lines in hand avoids it.
- Model the rate table behind a small abstraction (e.g., `IRateCard` / `IZoneResolver`) that
  the pricing logic consumes; the application service supplies the concrete data. Keep the
  pricing logic free of I/O.
- The quote is naturally a **value object** (a breakdown + total in one currency), most likely
  **owned by the `Order`** — not a second entity.
- C# `record` types help with value equality — but watch collection/mutable fields, which break
  structural equality quietly.
- Put it in the Ordering module of the `Throughline` solution (`Throughline.Modules.Ordering.*`).
  Domain types in `.Domain`, the application service in `.Application`.

## Definition of done

- Acceptance criteria met; unit tests cover **each business rule** and the **quote
  calculation**, including the **exact-money (no rounding drift)** and **duplicate-SKU** cases;
  results are **reproducible** (no dependence on wall-clock time). Note which `src/` project(s)
  you placed things in. Then ask for a `review` pass.
