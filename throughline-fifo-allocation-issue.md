# Allocate a confirmed order using FIFO and the client-owner’s allocation policy

## Business context

Throughline’s client-owners submit warehouse shipping orders that are validated and accepted before fulfillment begins. Once confirmed, an order’s contents are immutable.

An operator must be able to commit available stock to a confirmed order. Allocation identifies the specific inventory supplying each line and makes those quantities unavailable to competing orders.

Client-owners have different requirements when stock is insufficient. Some permit available quantities to be committed while the remainder waits. Others require the entire order to be allocatable before any stock is committed.

This story introduces operator-invoked allocation using FIFO and a customer-owner policy governing allocation completeness.

## User story

As a **warehouse operator**, I want to allocate a confirmed order against its owner’s eligible stock using FIFO and that owner’s allocation-completeness policy, so that inventory is committed appropriately and any outstanding demand is clearly identified.

## Scope

**In scope:**

- Explicit allocation of one confirmed order by an authorized operator.
- FIFO selection of eligible stock belonging to the order’s client-owner.
- Customer-owner configuration allowing partial allocation or requiring complete-order allocation.
- Allocation details identifying source inventory and quantities.
- Allocation outcomes and outstanding demand.
- Further operator-invoked attempts to satisfy outstanding demand.
- Protection against duplicate commitments and concurrent allocation.

**Out of scope:**

- Automatic allocation on order acceptance or receipt of stock.
- Wave planning, batch orchestration, and priority between competing orders.
- Order editing, cancellation, deallocation, or replacement of existing commitments.
- FEFO, shelf-life, serial-selection, customer-specific lot, packaging, and quantity-fit rules.
- Replenishment, pick-work creation, picking, and shipping.
- A user interface or a general-purpose allocation rules engine.

## Business rules

### Confirmed demand

Allocation uses the existing order identity, owner, and confirmed quantities. It does not revise the order or repeat intake validation and normalization.

Duplicate-SKU handling remains the responsibility of the existing intake process. Allocation operates on the resulting confirmed lines.

Quantities remain individual sellable units (**eaches**).

### Eligible inventory and FIFO

Stock must belong to the order’s owner, match the requested SKU, be physically received, and be available for allocation from an eligible location.

Held, restricted, expected, and already committed quantities are excluded.

FIFO selects the oldest eligible inventory by its original physical receipt timestamp. Moving stock does not make it newer. Equally aged inventory is selected consistently.

An order line may be supplied from multiple inventory records. Allocation details retain the source receipt, location, lot, and LPN where applicable.

### Customer-owner allocation policy

Each client-owner has an explicitly configured policy:

| Policy | Behavior when stock is insufficient |
|---|---|
| **Allow partial allocation** | Commit available quantities and retain the remaining demand as unallocated. |
| **Require complete-order allocation** | Add commitments only if all outstanding demand across the order can be satisfied. Otherwise, add none. |

The policy belongs to the inventory owner, not the ship-to recipient or the warehouse as a whole. An operator cannot override it through an individual allocation request.

Existing allocations are preserved. A policy change does not automatically release or replace them.

Allocation completeness is separate from shipment completeness. Partial allocation does not authorize a partial shipment.

## Acceptance criteria

- [ ] **Explicit invocation:** Given a confirmed order, accepting or retransmitting it does not allocate stock. Allocation begins only when an authorized operator requests it.
- [ ] **FIFO selection:** Given 30 available eaches received September 1 and 50 received September 5 for the same owner and SKU, when an order for 40 eaches is allocated, then 30 are committed from the earlier receipt and 10 from the later receipt.
- [ ] **Eligibility:** Given older stock is unavailable because of a hold, restriction, existing commitment, or ineligible location, when allocation runs, then that quantity is excluded and the system considers the remaining eligible stock. Expected receipts cannot satisfy demand.
- [ ] **Owner isolation:** Given different owners hold the same SKU, when an order is allocated, then only its owner’s stock can supply it. Access to allocation results follows the existing owner-access rules.
- [ ] **Partial allocation allowed:** Given the owner permits partial allocation, an order requests 12 eaches of SKU A and 5 of SKU B, and only 8 of A and 5 of B are available, when allocation completes, then 8 of A and 5 of B are committed, with 4 of A remaining unallocated.
- [ ] **Complete-order allocation required:** Given the same demand and availability, but the owner requires complete-order allocation, when allocation is requested, then no new stock is committed to either line and the result identifies the shortage of 4 eaches of SKU A.
- [ ] **Complete-order success:** Given the owner requires complete-order allocation and all outstanding quantities are available, when allocation completes, then every outstanding line quantity is committed. No business result presents only some of that order’s new commitments as successful.
- [ ] **Independent customer policies:** Given two owners have different completeness policies, when their orders encounter equivalent shortages, then each receives the outcome required by its own policy.
- [ ] **Visible outcomes:** After a completed attempt, the operator can see whether the order is fully allocated, partially allocated, or unallocated. Each line shows its confirmed quantity, allocated quantity, outstanding quantity, and source allocation details.
- [ ] **Further allocation:** Given an order remains incompletely allocated and additional stock becomes available, when the operator requests a fresh attempt, then only outstanding demand is considered. Existing commitments remain unchanged, and the owner’s policy governs whether additional quantities may be committed.
- [ ] **Repeated requests:** Given allocation has already been performed, when the operator repeats a request, including after an uncertain response, then existing commitments are preserved and the order is never allocated beyond its confirmed quantities. Repeating allocation for a fully allocated order creates no additional commitments.
- [ ] **Concurrent allocation:** Given operators allocate competing orders against shared stock at the same time, then the same available quantity cannot be committed twice. Resulting commitments and shortages must remain consistent with available stock and each owner’s completeness policy.
- [ ] **Preserved order and inventory:** Allocation changes neither confirmed order contents nor physical on-hand quantities, stock ownership, or location. It creates no picking work and records no shipment.
- [ ] **Unsuccessful processing:** If allocation cannot complete because of missing configuration, invalid inventory data, or a processing failure, the operator receives a distinguishable failure outcome. The system must not report that failure as an ordinary stock shortage or leave untraceable commitments.

## Supporting requirements

- Record the completeness policy applied to each allocation attempt. Missing policy configuration must not silently select a default.
- Use a documented, deterministic tie-breaker for equal receipt timestamps.
- Distinguish replay of a completed attempt from a fresh request to evaluate outstanding demand.
- Recovery after interruption must preserve confirmed commitments and prevent duplicate allocation. The API contract must define how an uncertain outcome is retrieved or replayed.
- Allocation details, inventory commitments, and reported outcomes must agree. Under complete-order policy, the operator must observe either complete satisfaction of outstanding demand or no additional commitments.
- Concurrent inventory changes must coordinate with allocation. Stock-discrepancy reconciliation and changes to already allocated inventory require separate business workflows.
- Extend the existing structured logging and correlation conventions to identify the operator, owner, order, attempt, and outcome. Operational telemetry remains correctly attributed to owners and available to authorized Throughline operations staff.
- Validate shortages under both policies, competing allocations, repeated requests, and interrupted processing.

## Dependencies and existing behavior

- A confirmed, immutable order from **STORY-0002 — Accept & Validate Warehouse Shipping Order**.
- Existing owner-scoped order identity and retransmission behavior from [#7](https://github.com/jamesmckeon/logistics-monolith/issues/7).
- Existing observability conventions from [#5](https://github.com/jamesmckeon/logistics-monolith/issues/5).
- Eligible on-hand inventory with receipt age, source identity, availability, and existing commitments.
- An allocation-completeness setting on each client-owner.

## References and deliberate departures

- [Extensiv — Understanding Allocation Logic](https://help.extensiv.com/3pl-warehouse-manager-inventory-management/understanding-allocation-logic) documents FIFO, allocation outcomes, and explicit allocation of outstanding order quantities.
- [Extensiv — Managing Warehouses](https://help.extensiv.com/en_US/managing-warehouses) documents **Prevent Partial Order Allocation (Manual Allocation)**, which prevents allocation when the whole order cannot be satisfied.

Throughline deliberately applies the completeness policy **per client-owner**, whereas Extensiv documents that setting at warehouse level. Receipt-time FIFO and preservation of existing commitments are the initial Throughline policy; additional selection criteria can be introduced in subsequent stories.

Sources reviewed September 12, 2026.
