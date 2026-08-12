# ADR-0003 — SKU master ↔ Ordering: context relationship and attribute access

- **Status:** Accepted — 2026-08-11.
- **Amendment (2026-08-11):** Adopted **Option A (Grzybek-strict): no shared domain kernel.**
  This supersedes the original wording that put `SkuCode` "in the shared kernel," which
  contradicted this ADR's own reliance on Grzybek's rule that a module may depend only on
  another module's integration-events assembly. See Decision §5 and the revised §1.
- **Deciders:** Jamesey (engineer), Claude (mentor)
- **Related items:** T-020 (Bounded Contexts & Context Mapping) — primary rep; supports STORY-0001 (#1). Touches deferred mechanism items T-004/T-006/T-008 (Phase 2) and T-007 (Phase 3).
- **Builds on:** ADR-0001 (modular monolith; integration-event bus; module-owned tables), ADR-0002 (per-merchant, USD-only pricing).
- **Source grounding:** `claude/sources.md`. Grzybek **verified against the repo this turn** (async-only; module-owned data; modules depend only on another module's integration-events assembly). Evans/Vernon, Fowler, Martin/Cockburn **cited from knowledge, not verified against the texts this turn.**

---

## Context

STORY-0001 must return an estimated quote whose formula includes weight-based handling, so
Ordering needs **unit weight per SKU** at quote time. Per issue #1, "product weight belongs
to the **SKU master**, which we maintain" — a bounded context with a different model and
language (product identity / physical attributes) than Ordering (orders / lines / quotes).
The per-merchant pick/surcharge **rate table** is a *separate* source (ADR-0002) and is out
of this ADR's scope; this ADR is only about **SKU-attribute access**.

ADR-0001 already fixed the inter-module rules: communication via a transport-agnostic
**integration-event bus**, **each module owns its tables**, and **no cross-module references
except through published contracts / integration events**. A synchronous direct method call
into the SKU master's domain is therefore already excluded.

Two things remain to decide at this seam:
1. The **context-mapping relationship** between the two contexts.
2. **How** Ordering obtains SKU weight within ADR-0001's rules — the one crack ADR-0001
   leaves: "published contracts" could mean a synchronous *query* contract, or async
   integration events plus a locally-owned copy.

## Decision

**1. Relationship = Customer/Supplier (not Anti-Corruption Layer).**
Ordering is the customer (downstream); the SKU master is the supplier (upstream). We own and
maintain both, so Vernon's criterion for an ACL — defend against an upstream model you do
*not* control or cannot influence — does not hold. `SkuCode` is the correlation identifier:
Ordering declares **its own** `SkuCode` (see §5); the code **value** is what travels across
the seam inside integration events. Ordering translates the supplier's attributes into its
own local model at the boundary — a *light* translation, not defensive isolation.

**2. Attribute access = async local read model (not a synchronous query contract).**
The SKU master publishes SKU-attribute integration events; Ordering maintains its **own local
read-model copy** of the attributes it needs (`SkuCode → UnitWeight`); the quote is computed
from Ordering's local copy. There is **no call to the SKU master at quote time.** This is the
direct application of ADR-0001's event bus + module-owned tables, and of Grzybek's confirmed
async-only rule.

**3. Seam in code = a mechanism-neutral port owned by Ordering.**
`ISkuAttributeSource.Resolve(IReadOnlyCollection<SkuCode>) → Result<IReadOnlyList<SkuAttributes>>`,
defined in Ordering's domain (DIP; Fowler's Gateway; Cockburn's port). Unknown SKU → a
`Result` failure that feeds the "reject the submission" outcome (same path as quantity < 1).
Batched — one resolve for all lines, never per-line. The port hides the mechanism so the
read-model implementation drops in later without reshaping Ordering.

**4. STORY-0001 scope.**
Build the **port** plus an **in-memory adapter** seeded with the agreed SKUs, standing in for
the local read model. **Do not** build the integration-event pipeline (Phase 2: T-004/T-006/
T-008) or the persisted read model (Phase 3: T-007) — both are explicitly out of scope per
issue #1 and the roadmap. Shape the stub as "resolve already-owned attributes," faithful to
the future local copy, so it does not lie about the seam.

**5. Shared-code structure = no shared domain kernel (Option A, Grzybek-strict).**
There is **no `SharedKernel` project of business types.** Grounding: Grzybek (✅ confirmed,
verified against the repo) — a module may depend only on *another module's integration-events
assembly*, and modules **re-declare their own identifiers** (`MemberId`, `PaymentId` in the
reference; here Ordering owns its own `SkuCode` and `MerchantId`, and the SKU/merchant masters
own theirs). Cross-context correlation is by the identifier **value** carried in integration
events, not by a co-owned type. The only shared assembly is **`BuildingBlocks`**, and it holds
**infrastructure base types only** (`Entity`, `AggregateRoot`, `ValueObject`, `IDomainEvent`,
`TypedIdValueBase`, business-rule + event-bus plumbing) — never a logistics concept.
Consequence: `SharedKernel.Skus` / `SharedKernel.Merchants` should not exist; the git move
that pulled `Sku`/`SkuCode` into `Ordering.Domain` is the correct direction. `MerchantPurchaseOrder`
is an Ordering concept and lives in Ordering, not in any shared assembly.

## Consequences

**Positive**
- **Serves the AC directly.** Reading an Ordering-owned copy means quotes don't depend on SKU
  master latency/availability, and Ordering controls *when* weight updates apply — so the same
  order re-quoted against the same rate table is deterministic, and the hot intake path
  (millions/day, retried) has no synchronous fan-out.
- **No architectural exception.** Aligns with ADR-0001 and Grzybek; nothing to justify later.
- **Minimal coupling.** `SkuCode` stays the only shared type; the SKU master's model never
  leaks into Ordering.

**Negative / costs**
- **Eventual consistency.** Ordering prices from weight as-of the last integration event it
  processed; a just-changed weight may lag. Accepted — a quote is a point-in-time snapshot and
  determinism matters more than freshness here. Must be made explicit when the pipeline is
  built, including how an unknown/just-created SKU not yet projected is handled.
- **Duplicated data.** Weight lives in both the SKU master and Ordering's read model. Accepted
  as the standard CQRS/read-model trade-off — it is what buys the availability/determinism.
- **More moving parts later** than a synchronous query. Accepted, and deferred to the phases
  (2–3) that teach exactly those parts.

## Alternatives considered

- **Synchronous published query contract** (SKU master exposes a query API Ordering calls at
  quote time). A defensible reading of ADR-0001's "published contracts," and permitted by the
  structural sources (Fowler/Martin — a port's adapter *could* be a sync call). **Rejected:**
  contradicts Grzybek's confirmed async-only rule and ADR-0001's dominant event-bus intent, and
  couples quote availability + reproducibility to the SKU master on the hottest path.
- **Anti-Corruption Layer.** **Rejected** on Vernon's own criteria — an ACL defends against an
  upstream you do not control; we own the SKU master. Would add translation ceremony without
  the isolation need. (T-021 stays reserved for a genuine external/3PL model — e.g. carrier APIs.)
- **Shared Kernel** (a co-owned assembly of shared types — even one as small as `SkuCode`).
  A legitimate Evans/Vernon context-mapping pattern, but the highest-commitment one, and it
  **conflicts with Grzybek's confirmed rule** that a module depends only on another module's
  integration-events assembly. **Rejected** per the §5 amendment (Option A). A **Conformist**
  relationship or a shared kernel of the *full attribute set* is doubly rejected — it would
  couple Ordering's model and release cadence to the SKU master's.
- **Direct synchronous in-process method call** into the SKU master's domain. **Already
  excluded** by ADR-0001 (no cross-module references except via contracts / integration events).

## Revisit when

- The SKU master becomes a genuinely **external / third-party** system (reopens ACL vs.
  Customer/Supplier).
- A **real-time weight-accuracy** requirement makes eventual consistency unacceptable (reopens
  synchronous access vs. read model).
- ADR-0001's inter-module transport decision changes.
