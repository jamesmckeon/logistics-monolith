# ADR-0006 — Module internal structure: layered/Clean vs vertical slice (demonstrate both)

## Status

**Proposed — 2026-08-16.** This ADR records a *plan*, not a ratified decision. It is accepted only when
the vertical-slice module is actually built and the comparison is judged worth keeping. Until then the
governing register entry (MOD-04 in [MODERNIZATION-REGISTER.md](MODERNIZATION-REGISTER.md)) stays 🔷 Open.

## Context

This repo is positioned as a .NET 10 successor to
[kgrzybek/modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd) (✅ Confirmed,
canon source #—). The original structures **every** module the same way: layered (Application / Domain /
Infrastructure per module).

Two schools are in tension in the project canon ([sources.md](../sources.md)):

- **#3 Clean / layered architecture** (R. C. Martin; Cockburn Hexagonal) — ✅ **Confirmed, but explicitly
  contested**. Layers are the top-level partition; the dependency rule is enforced at assembly boundaries;
  abstractions (e.g. `IOrderRepository`) live in Domain/Application, implementations in Infrastructure.
- **#6 Vertical Slice Architecture** (Bogard) — ⏳ **Pending**, named as the live school that *reacts
  against* the ceremony of layered/Clean. The **feature/use case** is the top-level unit; a slice cuts
  *through* the layers (endpoint → handler → persistence colocated) and typically **drops the repository
  abstraction**, letting the slice use the DbContext directly. Simple slices stay simple; complex slices
  keep a rich domain. "Things that change together live together."

Canon operating-rule 2/3: a choice between a confirmed source and a named opposing school is **settled by
an ADR for this project** — hence this document.

**The user's current (layered) convention**, to be preserved for the first module(s):

- **Per module:** three layers/projects — Application, Domain, Infrastructure.
- **Application** organized by **use case** (e.g. `CreateOrderHandler`).
- **Domain** organized by **entity/aggregate** (e.g. an `Orders` namespace holding `Order` and
  `IOrderRepository`).
- **Infrastructure** organized mostly by **entity**, less rigidly.
- Each layer carries a **shared/common** namespace for classes specific to that layer.

Note: organizing the Application layer *by use case* is a step toward the feature-first mindset, but the
**layer remains the primary boundary** — a single use case's code is still spread across three projects.
That is what distinguishes this from VSA, where the feature is the boundary.

## Decision (planned)

Rather than pick one style repo-wide, **demonstrate both** — legitimate precisely because a modular
monolith grants each module its own internal structure (modules integrate only via published contracts):

1. **Ordering** (and any module already begun) is finished in the user's **layered/Clean** convention
   above. No rework.
2. A **later module is built vertical-slice** as a deliberate side-by-side comparison for repo viewers —
   an artifact the original repo does not provide.
3. **Target VSA module: Inventory** (reservation / optimistic concurrency / overselling) — chosen for
   **real write-side complexity** so the comparison is a fair fight, not layered-DDD vs trivial CRUD.
   (Tracking is the fallback "VSA where it shines" read-heavy showcase, but less apples-to-apples.)

**Guardrails (so the comparison teaches rather than misleads):**

- The VSA module must be an **honest** VSA: a rich write-side domain (the `Inventory`/reservation
  aggregate and its invariants) is retained; VSA drops the *ceremony* (repository interface, mapping
  layers) and colocates endpoint→handler→persistence. It must **not** decay into an anemic
  transaction-script-in-a-folder — that would compare layered-DDD against a strawman.
- Domain complexity must be **comparable** to Ordering, or apparent "wins" (e.g. line count) are artifacts
  of a trivial domain, not the architecture.
- **Intent is documented**: a per-module README on each, plus this ADR, naming the comparison axes.

**Comparison axes** viewers should evaluate:

| Axis | Layered/Clean (Ordering) | Vertical slice (Inventory) |
|---|---|---|
| Where one use case's code lives | Spread across 3 projects | Colocated in the slice |
| Abstractions per feature | Repository + mapping + interfaces | Minimal; slice → DbContext |
| Blast radius of a change | Touch multiple layers | Contained to the slice |
| Testability | Mock the repo; unit-test in isolation | May reach for Testcontainers sooner (MOD-11) |
| Onboarding / navigation | Learn the layer map once | Learn per-slice; less indirection |
| Cross-cutting concerns | Domain/App services | Pipeline behaviors / endpoint filters |

### Read-side ports & the hexagon within a slice

Both module styles are built **hexagonally** (Cockburn, canon #3 ✅): the core owns *ports* and
infrastructure plugs in via *adapters*. VSA and hexagonal are orthogonal — feature-first decomposition
does not require dropping ports — so the VSA (Inventory) module keeps driven ports on the **write** side
(persistence, event publish, carrier/3PL gateways), preferring **feature-scoped, role-based ports**
(e.g. `IReserveStock`) over a broad shared `IInventoryRepository` — more hexagonal in spirit *and* more
VSA than a single shared repository.

The only genuinely contested question is **whether reads sit behind a port**. Rule for this project:

> **Reads bypass the write aggregate** (read from a projection/read model, don't rehydrate the aggregate
> to render a view). **Port a read when (a)** its handler has logic worth isolating, **or (b)** there is a
> **concrete, nameable expectation of re-platforming that read store** (CQRS read models, `HybridCache`,
> search index, sharding — all on the [roadmap](../roadmap.md)). **A pure passthrough read with no
> re-platform on the horizon is covered by the integration tier, not a mockable query port.**

This reconciles the three forces that were weighed (2026-08-16):

- **Testability.** The mandated integration tier (per-module, real DB via Testcontainers — MOD-11) owns a
  read's actual correctness: the SQL/projection, joins, filtering. A unit test that mocks a *passthrough*
  query is tautological and falls foul of [testing.md](../testing.md)'s rule against tests that "prove
  almost nothing about the collaboration." So testability argues for a read port **only** where the
  handler has logic a mock can meaningfully stand in for.
- **Purity / uniformity.** Keeping the core free of `DbContext`/`IQueryable` types is a legitimate values
  choice; a module may port *all* reads for consistency, accepting thin unit tests plus integration
  coverage of the adapters. Permitted, not required — and it is the *only* remaining argument for porting a
  passthrough read once the integration tier exists.
- **Provider-change / swappability.** Real on the read side (high-volume systems go polyglot on reads:
  cache, replica, denormalized view, search), but it pays only for a **named** swap. EF Core's provider
  model already covers relational→relational; a port *localizes* a swap but does not *shrink* it; a
  wholesale migration rewrites every adapter regardless. So this justifies porting the reads we can point
  at a likely re-platform for — not every read "if needed" (that is the speculative-generality trap VSA
  exists to resist).

**Anti-pattern to avoid:** manufacturing a query port solely to satisfy a "each module has unit tests"
requirement. That is test-induced design damage — the unit tier is satisfied by code that *has* behavior
(the aggregate, write handlers, validation, a pure row→DTO mapper tested directly), not by inventing seams
for logic-free reads.

## Consequences

**Positive**
- A rare, concrete side-by-side of the *same* domain style under two architectures — directly serves the
  repo's "underexplained online" positioning.
- The user gains hands-on experience with VSA (an explicit goal), not just Ordering's familiar layout.
- Resolves the canon's #3-vs-#6 tension honestly instead of asserting one school as universal.

**Negative / costs**
- Two internal structures mean a viewer must learn both — acceptable *because comparison is the goal*,
  but it must be signposted (READMEs + this ADR) or it reads as inconsistency.
- Architecture/boundary tests (MOD-12) must encode **per-module** structural rules, not one global rule.
- Risk of an unfair comparison if the guardrails above slip (anemic VSA, or mismatched complexity).

**Alternatives considered**
- **Layered everywhere** (mirror the original). Safe, consistent, but forfeits the teaching artifact and
  leaves VSA unpracticed.
- **VSA everywhere.** Rejected: discards the user's established layered convention and the DDD-heavy
  Ordering work already underway.
- **Hybrid within one module** (vertical slices inside a layered skeleton). Instructive but muddier as a
  *comparison* — harder for a viewer to see each style in its pure form. May still emerge naturally in the
  VSA module (simple slices vs complex slices).

**Revisit / ratify when:** the Inventory (or chosen) VSA module is built — at which point flip MOD-04 to
✅, promote this ADR from Proposed to Accepted, and fill in any deltas discovered while building it. Also
revisit if source #6 (VSA) is confirmed or a conflicting confirmed source appears.

## Related

- [MODERNIZATION-REGISTER.md](MODERNIZATION-REGISTER.md) — MOD-04 (this ADR's register entry), MOD-02
  (mediation/pipeline — VSA's cross-cutting mechanism), MOD-11 (Testcontainers), MOD-12 (boundary tests).
- [sources.md](../sources.md) — canon #3 (Clean, ✅ contested) vs #6 (VSA, ⏳ Pending); rule that contested
  schools are settled by ADR.
- Bogard, *Vertical Slice Architecture* (jimmybogard.com) — the #6 source.
