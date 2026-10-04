# STORY-0004 — Receive Unplanned Client-Owner Deliveries with Inventory and Exception Outcomes

> **Source of truth:** GitHub issue [#14](https://github.com/jamesmckeon/logistics-monolith/issues/14).
> The sections above the scaffolding divider mirror the issue body.

- **Status:** In Progress (branch `issue-14-unplanned-receipt`)
- **Difficulty / time box:** not set in the issue
- **Skills reviewed through:** T-005 (Idempotency Keys), T-026 (Consistency Boundary Identification), T-008 (Delivery Semantics)  ← the lens, not the reason this story exists
- **Bounded context(s):** Inbound / Receiving; Inventory
- **Owner(s) / Facility:** any configured client-owner; single DC
- **Created:** 2026-10-01 (mirror created; issue predates it)

## Business context

A delivery can arrive for a known client-owner without an advance shipment notice. The receiving operator checks the goods before submitting `POST /inventory/receipts`; the supplied hold code records any damage or other restriction identified during that process.

The delivery can contain usable stock, identifiable but restricted stock, and a pallet whose SKU or barcode is not recognized for that owner. Throughline must record the appropriate outcome for each pallet without making receipt of valid pallets depend on resolving another pallet's exception.

An unplanned receipt records the delivery and its pallet outcomes. Identified stock becomes on-hand inventory; an unidentified pallet is recorded as physical custody awaiting item identification, without creating normal SKU inventory. Receiving does not associate stock with an outbound order or allocate it automatically.

## User story

As a **warehouse receiving operator**, I want to record an unplanned delivery and the outcome for each pallet, so that usable stock becomes available, restricted stock remains controlled, and unidentified goods remain traceable for follow-up.

## Scope

- One warehouse and one client-owner per receipt, with owner isolation throughout.
- One product per pallet, identified by an LPN or SSCC; mixed-SKU pallets are excluded.
- One lot per pallet for lot-controlled SKUs; quantities in eaches.
- Three operational cases:

| Case | Receiving outcome | Inventory effect |
| --- | --- | --- |
| Valid SKU, no receiving restriction | Receive into the operator-selected bulk location. | Create on-hand inventory available for explicit allocation. |
| Valid SKU, damaged or otherwise restricted | Receive on hold at its actual bulk or quarantine location. | Create on-hand inventory unavailable for allocation. |
| SKU or barcode unrecognized for the owner | Record physical custody at a receiving-exception location. | Create an unresolved receiving exception, not normal SKU inventory. |

- Capture inbound shipment references with the receipt and retain their association with every recorded pallet outcome.
- Inspection occurs before the POST. There is no separate inspection, receipt confirmation, finish, or close operation in this story.

## Acceptance criteria

- [ ] **Record an unplanned delivery.** An authorized operator can submit at least one pallet for a configured client-owner without an expected receipt. Throughline assigns a human-readable receipt number that operators can use to find the receipt, identifies the receipt as unplanned, and records the operator and posting time. A receipt can contain inventory outcomes, unresolved exceptions, or both.

- [ ] **Capture pallet information.** Each submitted pallet includes its LPN or SSCC, supplied SKU or barcode, positive quantity in eaches, actual location, and physical receipt date/time. Preserve the supplied identifier for traceability. For a recognized SKU, capture and validate lot and expiry information when required by that owner's SKU configuration.

- [ ] **Receive unrestricted stock.** A valid pallet with no hold code is received into a configured bulk location. Its quantity becomes on-hand and available when the POST records that pallet successfully; no later confirmation or inspection gate is required.

- [ ] **Receive restricted stock.** A valid pallet with an active configured hold code is received at its actual bulk or quarantine location. Its quantity is on-hand but unavailable for allocation. The hold applies to the whole pallet and remains visible with its canonical reason code. A quarantine destination requires a hold code; choosing a location does not replace recording the receiving restriction.

- [ ] **Record an unrecognized item as an exception.** If the supplied SKU or barcode cannot be resolved within the specified owner, record the pallet in a configured receiving-exception location. Retain its owner, LPN, supplied item identifier, reported quantity, actual location, physical receipt timestamp, any supplied lot/expiry information and hold reason, and receipt/shipment association. Mark the reason as unresolved item identity. Do not create a placeholder SKU or an inventory record with a missing SKU. SKU-specific lot/expiry requirements are checked when identity is resolved in the follow-up story.

- [ ] **Make exceptions visible.** Authorized operators can retrieve the receipt's pallet outcomes and find unresolved receiving exceptions by owner, receipt or LPN. An exception is distinguishable from on-hand inventory and contributes nothing to on-hand or available SKU balances. A missing hold code never makes unidentified goods allocatable.

- [ ] **Process pallets independently.** One pallet's unresolved item identity or pallet-level validation failure does not prevent other acceptable pallets in the request from being recorded. Return an explicit outcome for each submitted pallet: received available, received on hold, recorded as an unresolved receiving exception, or rejected with validation/conflict details. Do not silently drop a pallet or imply that a recorded exception is inventory.

- [ ] **Validate the request and each pallet.** Authorization, configured owner, request structure and a nonempty pallet list are request-level prerequisites. Each pallet still requires a valid LPN/SSCC, positive quantity, nonempty item identifier, valid timestamp, and an existing location appropriate to its outcome. Reject invalid/inactive hold codes. A rejected pallet creates neither inventory nor an unresolved custody record. A request with no recorded pallets creates no receipt. Never resolve an item using another owner's master data or disclose another owner's inventory details.

- [ ] **Validate location purpose.** Unheld inventory must be in bulk storage; held inventory can be in bulk storage or quarantine; unidentified goods must be in a receiving-exception location. Record where the goods actually are. General location status/holds, capacity, cube, weight, stack-height and pallet-position enforcement remain outside scope; configured locations are assumed to have sufficient capacity. These destination rules do not introduce directed putaway or a general movement workflow.

- [ ] **Capture shipment identification.** Accept optional receipt-level BOL number, delivery/packing-slip reference, carrier SCAC, carrier name, trailer or freight-container number, and ship-from identification. Keep BOL and delivery/packing-slip references distinct. Carrier name can identify the carrier when SCAC is unknown. These fields are shared by all inventory and exception outcomes on the receipt. Missing external paperwork does not block an unplanned receipt. No carrier-master or live SCAC lookup is required.

- [ ] **Prevent duplicate physical-pallet records.** An LPN/SSCC already identifying inventory or an unresolved receiving exception in the warehouse cannot be recorded as a new arrival. Apply this protection to concurrent requests and duplicate LPNs within a request. Equivalent request retries return the recorded outcome without adding inventory, custody records or receipts.

- [ ] **Do not record the same delivery twice**. If an operator resubmits a delivery because the first response was interrupted or uncertain, Throughline returns the previously recorded receipt and pallet outcomes without creating duplicate stock, custody records, or receipts. If the resubmitted information differs, Throughline rejects it as a conflicting submission rather than guessing which version is correct.

- [ ] **Keep allocation explicit.** A later operator allocation request applies the existing owner-isolation, FIFO and allocation-completeness rules. Only eligible inventory from this receipt participates; held pallets and unresolved exceptions do not.

- [ ] **Preserve owner isolation.** Receipt history, shipment references, inventory, receiving exceptions and replayed outcomes are accessible only within the authorized owner context.

## API contract

`POST /inventory/receipts` records the receipt outcomes immediately. It does not create an open receipt requiring later confirmation or closure.

- Preserve the existing owner context and pallet list. Add the physical receipt timestamp, explicit shipment-reference fields and a client-assigned `receiptId` (GUID). The submitting client generates the `receiptId` once per delivery submission and reuses it on every resubmission; it is the receipt's identity.
- Throughline assigns the receipt number when the receipt is first recorded. The client never supplies it.
- A newly recorded receipt returns `201 Created`, its `receiptId`, its receipt number, and per-pallet outcomes correlated with the submitted pallet. This includes mixed inventory/exception/rejected results and a receipt containing only unresolved exceptions.
- A resubmission of an existing `receiptId` with equivalent content returns `200 OK` with the existing receipt and the recorded per-pallet outcomes. Equivalence is judged on the submitted delivery content only; which operator resubmits it, or when, does not make it different.
- A request-level validation failure, or a request in which every pallet fails validation and nothing is recorded, returns `400` with actionable errors and creates no receipt.
- A resubmission of an existing `receiptId` with different content returns `409` without new effects. A `receiptId` that already exists outside the caller's owner context also returns `409` and discloses nothing about the existing receipt. An already-recorded LPN is a pallet conflict; where other pallets are recorded, report the conflict in that pallet's result. If nothing can be recorded and the failure includes an LPN conflict, return `409` with the relevant per-pallet errors.
- A previously unknown SKU is an accepted exception outcome only when the custody information and receiving-exception destination are valid; it is not an unconditional `sku_not_found` rejection.
- BOL, packing-slip, trailer/container and other shipment references are traceability data, not receipt identifiers. They do not determine whether a submission is a resubmission.

## End-to-end acceptance example

1. A confirmed order has outstanding demand for 80 eaches of SKU A.
2. A delivery arrives for the same owner with no advance notice. Before submitting the POST, the operator checks three pallets and places them in their recorded locations.
3. Pallet A contains 100 eaches of SKU A, has no hold code, and is at `BULK-01`. Pallet B contains 20 eaches of a known SKU, is damaged, and is at `QUARANTINE-01` with `DAMAGED`. Pallet C contains 10 eaches under an unrecognized item identifier and is at `RECEIVING-EXCEPTION-01`.
4. One POST records A as available inventory, B as held inventory, and C as an unresolved receiving exception. All three are traceable to the same receipt and supplied inbound shipment references.
5. The operator invokes allocation. FIFO commits 80 eaches from A, leaving 20 available. B and C are not eligible.
6. Resubmitting the same delivery (same `receiptId` and content) returns the original receipt number and outcomes without creating additional stock or custody records. Resolving C, releasing B, or returning B is follow-up work, not another step required to complete this POST.

## Business rules and deliberate Throughline policies

- The absence of a 943 or other advance notice does not prevent authorized receiving.
- The receiving operator's confirmation records the prior physical checks. A separate supervisor approval or formal QC subsystem is not required for this operation.
- A null/omitted hold code means no inventory hold only for a recognized, otherwise valid pallet. A supplied code must match an active warehouse-wide configured reason; store its canonical code. Example reasons are `DAMAGED`, `EXPIRED` and `OWNER_REVIEW`. A `QUALITY_INSPECTION` reason, if configured, is only a restriction reason and does not introduce an inspection workflow.
- The original physical receipt timestamp is preserved for FIFO when identified inventory is created or subsequently moved, including later conversion of an unresolved exception.
- Capturing unidentified goods separately from SKU inventory, requiring a receiving-exception destination, and the request/result semantics above are deliberate Throughline policies. They are not claimed as a documented Infor or Oracle unknown-SKU workflow.
- Expected-versus-actual shortage and overage calculations do not apply because there is no expected receipt.
- Receipts carry a system-assigned, human-readable receipt number, following conventional WMS practice (Infor WMS system-assigns ASN/receipt numbers). The receipt number is what people use to refer to a receipt; the client-assigned `receiptId` is what lets a device safely resubmit a delivery before it has learned the receipt number.

## Consistency, observability and validation

- For each accepted inventory pallet, its receipt history, inventory and hold status must commit together. For each exception pallet, custody history and the unresolved exception must commit together. Every committed outcome must remain linked to the receipt and shipment context. No orphan inventory or falsely successful outcome may remain after failure.
- Resubmitting a delivery must be safe after an uncertain response or interrupted processing, including when some pallet effects have already committed. A pallet rejected for validation must not roll back unrelated accepted pallets.
- Preserve the LPN identity across inventory and unresolved-custody records so concurrent receiving cannot record the same physical pallet twice.
- Log operator, owner, receipt, LPN, correlation information and outcome using existing conventions. Distinguish available inventory, held inventory, unresolved exceptions and rejections.
- Validate mixed outcomes; exceptions-only receipts; all-rejected requests; missing paperwork; invalid hold codes and locations; required lot/expiry data; owner isolation; duplicate LPNs; concurrent receiving; resubmissions with changed content; response-loss resubmissions; receipt-number assignment; and allocation that excludes held/unidentified goods.
- Learning focus: T-005 (idempotency), T-026 (consistency boundaries), and T-008 (safe retry after uncertain completion). These do not require an event bus or distributed workflow.

## Dependencies and related behavior

- Integrates with [#10 — Allocate a confirmed order using FIFO and the client-owner's allocation policy](https://github.com/jamesmckeon/logistics-monolith/issues/10).
- [#6 — Accept an owner's stock transfer advice (943)](https://github.com/jamesmckeon/logistics-monolith/issues/6) is an optional planned-receipt path, not a prerequisite.
- [#16 — Resolve receiving item-identity exceptions and complete pallet receipt](https://github.com/jamesmckeon/logistics-monolith/issues/16) covers operational case 4.
- [#17 — Release receiving holds and make eligible pallets available for allocation](https://github.com/jamesmckeon/logistics-monolith/issues/17) covers operational case 5.
- [#18 — Return held inbound pallets and record their physical departure](https://github.com/jamesmckeon/logistics-monolith/issues/18) covers operational case 6.
- These follow-ups depend on this story; they do not block availability of valid unheld pallets.

## Out of scope

- Receiving against a 943/expected receipt, expected-versus-actual reconciliation, 944 receipt advice, supplier 856 intake, and raw X12 processing.
- Mixed-SKU pallets, pallet splitting/merging, nested handling units, serial-controlled receiving and UOM conversion.
- Creating SKU master records, configuring alternate-item mappings, and converting unresolved exceptions into inventory (follow-up issue).
- Releasing holds, returning goods and other inventory disposition (follow-up issues); general receipt reversal/correction.
- Formal QC tasks, inspection result management, separate receipt confirmation and receipt closure.
- General receiving-staging/putaway task workflows, directed putaway, general location holds, capacity enforcement, cross-docking, automatic allocation, picking and shipping.
- Carrier-master management, live SCAC verification, document uploads, BOL generation, yard/transportation management and multiple warehouses.

## Behavioral references

Sources reviewed September 29, 2026; receipt-numbering behavior re-checked October 1, 2026. Vendor capabilities inform the operational model; the exception and API policies above are Throughline decisions.

- [Infor WMS 2026.x — Using quick data entry to create a receipt](https://docs.infor.com/wms/2026.x/en-us/wmsolh/xmo1612894102882.html): manual receipt header/detail entry; the receipt number is system-assigned.
- [Infor WMS 2026.x — Adding an ASN/Receipt without a PO](https://docs.infor.com/wms/2026.x/en-us/wmsolh/ppo1612894112745.html): direct entry with carrier/trailer information and pallet details; the ASN/receipt number is system-generated.
- [Infor WMS 2026.x — Receiving using the workstation](https://docs.infor.com/wms/2026.x/en-us/useradminlib/scerecug/daq1612893684364.html): individual or selected-line receipt, actual quantities/locations and LPN receiving holds.
- [Infor WMS 2026.x — Performing a quality check using an ASN](https://docs.infor.com/wms/2026.x/en-us/useradminlib/scerecug/kkc1612893632478.html): configurable QC capability, not evidence of a mandatory inspection stage for every receipt.
- [Infor WMS 2026.x — Carrier configuration](https://docs.infor.com/wms/2026.x/en-us/useradminlib/sceconfigug/carrier_general.html): separate internal carrier key and SCAC fields.
- [Oracle WMS Cloud 26B — Receive by Load](https://docs.oracle.com/en/cloud/saas/warehouse-management/26b/owmol/rf-receiving-process.html): a load is received through successive LPN operations; this does not establish a vendor bulk-API transaction policy or unknown-SKU triage feature.


---

# — Claude scaffolding (not in the issue) —

## Skills this exercises

- **T-005 Idempotency Keys.** The client-assigned `receiptId` is a client-created identity
  (functionally an idempotency key made permanent). Traps:
  - Comparing the domain entity or the application command instead of the submitted content.
    The command carries `OperatorId`/`OwnerId`, so a retry by another operator would look like
    a conflict.
  - C# `record` equality compares `IEnumerable<>` members by reference, so commands with pallet
    lines are never `Equals`.
  - Fingerprint normalization: omitted vs `null`, code casing/whitespace, `Expires` kind/offset,
    and whether pallet order is significant.
  - Treating shipment references (BOL, packing slip, trailer/container) as unique. They are not.
- **T-026 Consistency Boundary Identification.** Two uniqueness rules at two levels: `receiptId`
  per receipt, LPN/SSCC per warehouse across inventory **and** unresolved exceptions. Traps:
  check-then-insert races, duplicate LPNs split across the received and exception lists.
- **T-008 Delivery Semantics.** A lost response must be resolvable by resubmission, including
  concurrent resubmissions of the same `receiptId`.

## Issue

[#14](https://github.com/jamesmckeon/logistics-monolith/issues/14). Everything above the
scaffolding divider mirrors the issue body.
