# STORY-XXXX — <Short title>

- **Status:** Draft | Ready | In Progress | Delivered
- **Difficulty:** Warm-up | Core | Stretch | Staff-bar
- **Est. time box:** ~Xh
- **Skills reviewed through:** T-### (<Item>), T-### (<Item>)  ← the lens, not the reason this story exists
- **Bounded context(s):** Tenancy & Client Onboarding | Order Intake & Pricing | Inbound/Receiving | Inventory | Storage & Slotting | Outbound/Fulfillment | Wave & Task Orchestration | Integration/Edge | Billing (deferred)
- **Owner(s) / Facility:** <which client-owner(s) and facility this plays out in; consignment/VMI vs billed 3PL if it matters>
- **Created:** YYYY-MM-DD

## Business context
<2–4 sentences grounded in Throughline's canonical warehouse profile (CLAUDE.md §2 — the
multi-client 3PL DC and its scale baseline). Who's involved, what's at stake, and the
owner/facility framing. Don't re-invent volumes — inherit the baseline; state a number only
where this story deviates from it.>

## User story
As a **<role>**, I want **<capability>**, so that **<business outcome>**.

## Scope
**In scope:** <what to build>
**Out of scope:** <explicitly excluded, to keep it focused>

## Acceptance criteria
1. <Testable, observable behavior.>
2. …

## Constraints & non-functional requirements
_Business rules and NFRs that make the correct design necessary. State them as rules, **not**
technique names — the design is the user's to choose. Don't remove them._
- <e.g., "A client may resend the same 940; the shipment must never be picked or billed twice.">
- <e.g., "On-hand for a lot must never go negative even when a pick and a cycle-count adjustment land at the same instant.">
- <e.g., "One owner's stock and orders must never be visible to another owner.">
- <Throughput / latency / consistency requirement, relative to the §2 baseline.>

## Skills this exercises
_Kept separate from the requirement on purpose — this note must never leak into the spec
above. The story must stand on its own as a real Throughline requirement even if this section
were deleted._
<Name the target Item(s) and the specific traps to get right — the review lens, not the reason
the story exists.>

## Hints (optional — ignore if you want the full challenge)
<Light nudges only. Never a full solution.>

## Definition of done
- Acceptance criteria met, tests included, and ready for a `review` pass.
