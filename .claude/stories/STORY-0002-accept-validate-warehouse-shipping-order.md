# STORY-0002 — Accept & Validate Warehouse Shipping Order

> **Source of truth:** GitHub issue [#4](https://github.com/jamesmckeon/logistics-monolith/issues/4).
> The sections above the scaffolding divider mirror the issue body verbatim.
> Carve-out from issue [#1](https://github.com/jamesmckeon/logistics-monolith/issues/1): #1's
> priced-quote feature is an OMS/Billing concern and stays parked; this story is the WMS-core
> intake that a real WMS (Manhattan Active WM / Oracle WMS Cloud) actually owns.

- **Status:** Ready
- **Difficulty:** Core (leans Warm-up)
- **Est. time box:** ~2–3h
- **Skills reviewed through:** T-017 (Aggregates), T-018 (Value Objects vs Entities), T-023 (Domain vs Application Services); owner-scoping also exercises T-033 (Multi-Tenant Isolation)
- **Bounded context(s):** Order Intake & Pricing *(existing `Ordering` module)*
- **Owner(s) / Facility:** any billed-3PL client-owner; single DC (facility not modeled on the order)
- **Created:** 2026-08-24

## Business context

Throughline's client-owners (depositors) transmit **outbound warehouse shipping orders**
(940-style) instructing the DC to ship their goods to their customers. Orders arrive from the
~40 onboarded owners at the §2 baseline (~40k order-lines/day, 3–4× seasonal peaks). Before any
warehouse work is scheduled, Throughline must **accept or reject** each incoming order on
validity and return an **acknowledgment**, so a client knows their order was received as a
valid, owned order.

**Units:** order quantities are in individual sellable units (**eaches**) — a quantity of 1 is
one unit of that SKU, not a carton or pallet.

## User story

As a **client-owner's integration (depositor)**, I want to **submit a warehouse shipping order
and receive an acceptance acknowledgment (or a rejection with reasons)**, so that **I know
Throughline accepted a valid, owned order before any warehouse work begins**.

## Scope

**In scope:** capture an owner's warehouse shipping order (owning client, a ship-to destination,
and order lines of SKU + quantity in eaches); validate it; produce an **owner-scoped `Order`
aggregate** with a stable order id; return an **acceptance acknowledgment** (accepted + id, or
rejected + reasons); lock contents once accepted.

**Out of scope:**
- **Pricing / fulfillment quote** — this is an OMS/Billing concern (Billing is deferred, §2);
  it stays in issue #1's parked pricing code, not here.
- Inventory reservation/allocation, wave/pick/pack/ship, carrier selection.
- **EDI wire parsing** — model the domain, not the X12 940 envelope.
- **Duplicate-submission idempotency** (the *same order* arriving twice) — a later story.
- **Multi-facility** — single DC; no `Facility` on the order (determined upstream).

## Acceptance criteria

- [ ] Given a shipping order **from a known owner** with **≥1 line**, a **valid ship-to
  destination**, and **every line valid**, when submitted, then it is **accepted**, an
  **owner-scoped order** is created with a **stable order id**, and an **acceptance
  acknowledgment** is returned.
- [ ] Given a **zero-line** order, when submitted, then it is **rejected** and **no order is
  created**.
- [ ] Given a line with **quantity < 1** (eaches), when submitted, then the **entire order is
  rejected** and **no order is created** — invalid lines are never dropped or corrected.
- [ ] Given the **same SKU more than once** in one order, when the order is built, then the
  outcome is **well-defined and consistent** (consolidated into one line, or rejected — see
  Open questions).
- [ ] Given an order that has been **accepted**, when a caller attempts to change its lines or
  destination, then the change is **rejected** (contents are locked once accepted).
- [ ] An **accepted order belongs to exactly one owner** and is only ever retrievable within
  that owner's context — **one owner's orders are never visible to another owner**.
- [ ] The system **never exposes, stores, or returns a partially-valid or invalid order** — not
  even briefly.

## Constraints / non-functional

- Orders arrive from **onboarded client-owners**; a submission **carries its owning client**,
  and an order with **no valid owner is rejected**. An order can never exist unowned.
- **One owner's orders and data must never be visible to another owner.**
- An order is **fully accepted or fully rejected** — no partial acceptance.
- Intake is **write-heavy, with client retries and 3–4× seasonal peaks**; accepting an order
  must be **predictable** and must **not depend on wall-clock time** or on data that can change
  underneath it.

## Open questions

- **Duplicate SKU** within one order — consolidate quantities, or reject?
- **Order lifecycle** — a single submit/accept step, or an explicit `draft → accepted`
  transition?

---

# — Claude scaffolding (not in the issue) —

## Skills this exercises

- **Aggregates (T-017):** `Order` is the consistency boundary. Zero-lines, quantity,
  duplicate-SKU, immutability-once-accepted, and "never expose an invalid order, not even
  briefly" force every invariant **through the aggregate root**, with the line collection never
  leaked. *Traps:* exposing `List<OrderLine>`; building an empty order then adding lines (a
  *transient* violation of the non-empty rule); "validate later."
- **Value Objects vs Entities (T-018):** `OwnerId`, `SkuCode`, `Quantity` (eaches), and the
  ship-to `Address` are **immutable value objects** with construction-time validation;
  `OrderLine` is an **entity** with identity *inside* the aggregate. *Traps:* value-equality
  bugs from mutable/collection fields; primitive obsession (`string`/`int` where a VO belongs).
- **Domain vs Application Services (T-023):** validation/acceptance is **domain logic** on the
  aggregate; orchestration (receive → validate → persist → acknowledge) is the **application
  service**. *Trap:* the anemic-domain trap — acceptance rules leaking into the handler.
- **Owner isolation (T-033, lightly):** the owner is part of the order's **identity/tenancy**,
  scoped on every read *and* write — not a lookup bolted on afterward.

*The central trap:* **conflating the ship-to (consignee) with the owner.** The consignee is
*where goods go*; the owner is *whose goods they are*. Two different parties on the same order.

## Hints (optional — ignore if you want the full challenge)

- An `Order` cannot exist **unowned** — put `OwnerId` in the constructor/factory, not a setter.
- A private ctor + static factory (e.g., `Order.Accept(ownerId, shipTo, lines)`) means you never
  hold an invalid `Order`; construct **with the lines in hand** to avoid the transient-empty window.
- **No `Facility` on the order** — single DC; the destination DC is determined upstream.
- You already have most of this: reuse your existing `Order`/`OrderLine` and value objects. This
  story is the **same aggregate with owner scoping and without the quote** — the pricing/estimate
  types from #1 stay where they are, unused by this path.

## Definition of done

- Acceptance criteria met; unit tests cover **each rule** (zero-line, quantity < 1, duplicate-SKU,
  immutability-once-accepted, owner-scoping); results are **reproducible** (no wall-clock
  dependence). Note which `src/` project(s) you touched. Then ask for a `review` pass.

## Issue

Filed as [#4](https://github.com/jamesmckeon/logistics-monolith/issues/4) on 2026-08-24
(title: *"Order intake: accept and validate an owner's warehouse shipping order, return an
acknowledgment"*). Everything above the scaffolding divider is the issue body.
