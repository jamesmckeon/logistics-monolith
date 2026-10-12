# STORY-0005 — Integration-Event Failure-Path Tests; Correct Invalid Received Pallets

> **Source of truth:** GitHub issue [#25](https://github.com/jamesmckeon/logistics-monolith/issues/25).
> The sections above the scaffolding divider mirror the issue body.

- **Status:** Ready
- **Difficulty / time box:** not set in the issue
- **Skills reviewed through:** T-004 (Outbox Pattern), T-006 (Inbox / Consumer Dedup), T-008 (Delivery Semantics), T-005 (Idempotency Keys), T-026 (Consistency Boundary Identification)  ← the lens, not the reason this story exists
- **Bounded context(s):** Inbound / Receiving; Inventory (and the Ordering → Inventory event flow for work item 1)
- **Owner(s) / Facility:** any configured client-owner; single DC
- **Created:** 2026-10-09

This issue has two work items. Work item 1 hardens the existing Receiving → Inventory and Ordering → Inventory event flows against bad messages. Work item 2 adds a new flow that has to meet the same standard.

---

# Work item 1 — Integration tests for integration-event failure paths

## Context

Inventory handles two integration events today:

| Event | Published by | Handled by |
|---|---|---|
| `OrderConfirmedIntegrationEvent` | Ordering | `OrderConfirmedHandler` |
| `AllocatablePalletsIntegrationEvent` | Receiving | `AllocatablePalletsHandler` |

`IntegrationEventTests` covers only the happy path of each flow. No test covers the failure paths. The exception a handler throws decides what happens to the message, under the policies in `Program.cs`:

| Handler throws | Outcome |
|---|---|
| `MissingDependencyException` | Scheduled retries after 1, 5 and 15 minutes, then dead-lettered. Used when a referenced SKU hasn't replicated to Inventory yet. |
| `UnrecoverableMessageException` | Dead-lettered immediately. Used for a poison message that can never succeed. |

If a handler throws the wrong exception type, the build stays green and the message silently takes the wrong path. A permanently bad message would then wait about 21 minutes before reaching the dead-letter queue. A message that was only early would be dead-lettered when it should have waited for its SKU.

Ordering has no inbound handlers. "Ordering failure paths" here means the Ordering → Inventory flow.

## Acceptance criteria

- [ ] **OrderConfirmed: unknown SKU.** A line references a SKU that Inventory doesn't have for the owner. The message is classified for delayed retry, not immediate dead-letter, and no order allocation is persisted.
- [ ] **OrderConfirmed: invalid content.** A line or order fails Inventory's own validation. The message is classified for immediate dead-letter and no order allocation is persisted.
- [ ] **AllocatablePallets: unknown SKU.** A pallet references a SKU that Inventory doesn't have for the owner. The message is classified for delayed retry and no SKU receipts are persisted.
- [ ] **AllocatablePallets: one invalid pallet among valid ones.** The message is classified for immediate dead-letter, and **no** SKU receipts are persisted, including for the valid pallets in the same message. This is the all-or-nothing property the handler's comment claims. It is what makes replaying the dead-lettered message safe once the problem is fixed.
- [ ] **Policy wiring.** At least one test shows that an unrecoverable failure actually reaches the dead-letter queue. That proves the immediate-dead-letter policy in `Program.cs` is wired, beyond proving the exception type.
- [ ] **Owner isolation on failure.** A SKU that exists only for a different owner is treated as unknown for the message's owner. Another owner's master data never satisfies the message.
- [ ] Tests follow `docs/testing.md` and are categorized `Integration`.

## Notes (from research, not requirements)

- A tracked session fails on any handler exception by default. Wolverine documents `DoNotAssertOnExceptionsDetected()` for error-handling tests.
- Inline `InvokeAsync` applies only Retry and Retry-With-Cooldown automatically. `MoveToErrorQueue` and `ScheduleRetry` won't fire, so a dead-letter test must publish or send the event through the queue.
- The tracked session records `MovedToErrorQueue`, `MessageFailed` and `Requeued`. The default session timeout is 5 seconds.
- **Open question:** Wolverine's error-handling docs don't say what happens to an exception that matches no `OnException` policy. Verify before asserting anything about it.
- Sources: [Wolverine — Test Automation Support](https://wolverinefx.net/guide/testing.html) · [Wolverine — Error Handling](https://wolverinefx.net/guide/handlers/error-handling.html) · [Khorikov — When to Mock](https://enterprisecraftsmanship.com/posts/when-to-mock/) (use the real database, a managed dependency, and assert its resulting state).

---

# Work item 2 — Correct an invalid received pallet and receive it into inventory

## Business context

Under #14, a pallet that arrives with data Throughline can't accept is recorded as a receiving exception at a receiving-exception location. It is held in physical custody but is not inventory. Two of those exceptions are usually operator data-entry mistakes, not missing master data:

- **Mis-keyed SKU.** The pallet is a SKU the owner already has, but the submitted code was wrong, for example a typo or the wrong label scanned.
- **Invalid location.** The submitted location doesn't exist.

In both cases the goods are known and present. Only the record is wrong. An operator needs to correct the pallet's record so the goods become inventory without recording a second arrival. Until then, stock the owner already owns is unavailable to their orders.

## User story

As an **authorized inventory-control operator**, I want to correct a received pallet's mis-keyed SKU or invalid location, so that the goods become accurately recorded inventory, available or held as appropriate, without recording a second arrival or losing the original receipt history.

## Scope

- Pallets recorded as receiving exceptions by #14 with an invalid SKU code and/or an invalid location.
- SKU correction **to an SKU that already exists for the same owner**. Creating a missing SKU, and mapping an unrecognized barcode to an existing SKU, remain in #16.
- Confirming the pallet's actual location as part of the correction.
- One or more new Receiving endpoints for the operator. Once a pallet is valid, Receiving records it as received and notifies Inventory, which records the stock.

## Acceptance criteria

- [ ] **Find the pallet to correct.** An authorized operator can retrieve a correctable pallet for their owner by receipt or LPN. They can see the originally submitted SKU code, location, quantity, lot/expiry, hold code, receipt/shipment references and the exceptions recorded against it.
- [ ] **Correct the SKU.** The operator supplies the correct SKU code. It must identify an existing SKU for the same owner. A code that doesn't, or one that belongs only to another owner, is rejected and changes nothing.
- [ ] **Correct the location.** The operator supplies the pallet's actual location. It must be an existing location suitable for the pallet's outcome: bulk storage for unrestricted stock, and bulk or quarantine for stock with an active hold code. An invalid destination is rejected and changes nothing. Available stock is never recorded as sitting in the receiving-exception area.
- [ ] **Validate against the corrected SKU.** The corrected pallet must meet the corrected SKU's requirements, including any required lot number or expiry date. If it doesn't, the correction is refused and the pallet stays an exception. Supplying missing lot/expiry data is outside this issue.
- [ ] **Receive only when fully valid.** A pallet is received only when **all** of its exceptions are cleared. If it also has an exception this workflow doesn't correct (inactive or unknown hold code, missing lot/expiry, an LPN already in use), the correction is refused, the pallet stays an exception, and the response says which exceptions remain.
- [ ] **Same outcome as a correct original submission.** A corrected pallet ends up exactly as if the corrected data had been submitted originally. With no hold code it is on-hand and available for explicit allocation. With an active hold code it is on-hand on hold and not available for allocation.
- [ ] **Inventory records the stock.** When a corrected pallet becomes available stock, Inventory records it for the owner's SKU with the pallet's quantity and location, so a later allocation request can use it. No automatic allocation occurs.
- [ ] **Preserve arrival time for FIFO.** The stock keeps the pallet's original physical receipt time. The correction time is recorded separately and never replaces the arrival time (#14: "the original physical receipt timestamp is preserved for FIFO … including later conversion of an unresolved exception").
- [ ] **Preserve history.** The pallet stays linked to its original receipt and shipment references. The originally submitted SKU code and location are retained alongside the corrected values, together with the operator and the time of the correction.
- [ ] **Never double-count.** A corrected pallet produces stock exactly once. This holds when an operator retries an uncertain request, when two operators correct the same pallet at the same time, and when Inventory is told about the pallet more than once. An equivalent retry returns the recorded outcome. A conflicting second correction of an already-corrected pallet is rejected without changing it.
- [ ] **Receiving and Inventory agree.** If Receiving records the pallet as received, Inventory must eventually record its stock. If the correction fails, Inventory records nothing. A pallet must never be received in Receiving with its stock permanently missing from Inventory, or the other way around.
- [ ] **Failure paths tested to the work item 1 standard.** Inventory's handling of the corrected-pallet notification has failure-path tests covering classification and nothing persisted on failure, the same as the existing handlers.
- [ ] **Owner isolation.** Operators can't find, view or correct another owner's pallets. A correction can never resolve to another owner's SKU.

## Acceptance example

A delivery for an owner records pallet `LPN-0042`, 40 eaches, as a receiving exception: the operator keyed `SKU-1O0` (letter O) instead of `SKU-100`, and the location `BULK-1` doesn't exist. The operator retrieves the pallet, corrects the SKU to `SKU-100`, and confirms that the pallet is physically at `BULK-01`. The pallet is now received. Inventory shows 40 available eaches of `SKU-100` at `BULK-01`, with the original arrival time. The pallet's history still shows `SKU-1O0` and `BULK-1` as submitted. Retrying the same correction creates no additional stock.

## Open questions

- **Inactive/unknown hold codes: #14 and the code disagree.** #14 says "Reject invalid/inactive hold codes. A rejected pallet creates neither inventory nor an unresolved custody record." The current code instead records such a pallet as a receiving exception (`InvalidHoldReason`). That would make it a correctable state, but correcting it is out of scope here. This needs resolving, in #14 or a separate issue.
- **Missing lot/expiry** is a natural next correction, especially after a SKU correction to a lot- or expiry-tracked SKU. It is out of scope here and is a candidate follow-up.

## Out of scope

- Creating SKU master records and alternate barcode mappings (#16).
- Correcting hold codes, lot/expiry or duplicate LPNs. Quantity corrections.
- Releasing holds (#17) and returns (#18).
- General putaway or movement tasks, automatic allocation, and correcting pallets that were already received successfully.

## Dependencies and related behavior

- Blocked by #14, which records the receiving exceptions this work corrects.
- Related: #16 (unidentified items needing master data), #17 (hold release), #10 (FIFO allocation).


---

# — Claude scaffolding (not in the issue) —

## Skills this exercises

- **T-004 Outbox Pattern.** "Receiving and Inventory agree": marking the pallet received and
  notifying Inventory must commit together. Trap: saving the corrected pallet and publishing in
  two steps (dual write). `ReceiveDeliveryHandler` already shows how the existing flow does it.
- **T-006 Inbox / Consumer Dedup.** "Never double-count … when Inventory is told about the
  pallet more than once". `AllocatablePalletsHandler` doesn't dedupe by LPN or receipt today.
  Trap: assuming at-least-once delivery never actually redelivers.
- **T-008 Delivery Semantics.** Work item 1 is about how each failure is routed (retry vs.
  dead-letter). Work item 2 needs the new flow to make the same choice deliberately.
- **T-005 Idempotency Keys.** Equivalent-retry vs. conflicting-second-correction semantics on
  the new endpoint(s).
- **T-026 Consistency Boundary Identification.** Concurrent corrections of one pallet; the LPN
  moving from exception to received pallet without tripping the duplicate-LPN rule.

## Research backing work item 1

See the Testing register in [sources.md](../sources.md) (TS-1, TS-2, both Pending) and the
[[handler-failure-path-tests-pending]] memory.

## Issue

[#25](https://github.com/jamesmckeon/logistics-monolith/issues/25). Everything above the
scaffolding divider mirrors the issue body.
