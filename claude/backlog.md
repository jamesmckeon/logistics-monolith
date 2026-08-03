# Backlog — Master Item Tracker

Single source of truth for **item status**. Statuses: ⬜ Not Started · 🟡 In Progress ·
✅ Completed · ❌ Canceled. See [CLAUDE.md](CLAUDE.md) for operating rules and
[categories.md](categories.md) for the taxonomy and the full candidate catalog.

_Seeded on 2026-07-27 with the five example items from the initial request, then
expanded the same day with T-006–T-016 (consistency/messaging ring, concurrency +
inventory, resilience + SLOs, staff craft). Everything else in the catalog is a
candidate — ask to add it (dedup runs first)._

| ID | Item | Categories | Status | Priority | Stories | Updated | Notes |
|---|---|---|---|---|---|---|---|
| T-001 | Domain Events | DDD, EDA | ⬜ Not Started | High | STORY-0001 (optional) | 2026-08-02 | Distinguish domain vs integration events. STORY-0001 touches event *modeling* only (recording, not dispatch). |
| T-002 | Two-Phase Commit | DIST | ⬜ Not Started | — | — | 2026-07-27 | Learn it *and* why to prefer sagas/TCC. |
| T-003 | Saga Pattern | EDA, DIST | ⬜ Not Started | — | — | 2026-07-27 | Orchestration vs choreography. |
| T-004 | Outbox Pattern | EDA, DIST, DATA | ⬜ Not Started | — | — | 2026-07-27 | Pairs with inbox/consumer dedup. |
| T-005 | Idempotency Keys | DIST, API | ⬜ Not Started | — | — | 2026-07-27 | Retry-safe writes; storage of keys. |
| T-006 | Inbox Pattern / Consumer Dedup | EDA, DIST | ⬜ Not Started | — | — | 2026-07-27 | Other half of outbox; makes at-least-once safe. |
| T-007 | CQRS + Read Models | EDA, SYSD, DATA | ⬜ Not Started | — | — | 2026-08-02 | Scale reads (tracking/dashboards) independently. Includes read-vs-write model separation: when the read shape should differ from the write shape — and the judgment to *not* separate when the ratio doesn't justify it. |
| T-008 | Delivery Semantics | DIST, EDA | ⬜ Not Started | — | — | 2026-07-27 | At-least/at-most/exactly-once; ties to T-004/T-005. |
| T-009 | Schema/Contract Evolution & Versioning | EDA, API | ⬜ Not Started | — | — | 2026-07-27 | Independent producer/consumer deploys; contract tests. |
| T-010 | Optimistic vs Pessimistic Concurrency | DATA, DIST | ⬜ Not Started | — | — | 2026-07-27 | Row versioning/ETags under contention. |
| T-011 | Inventory Reservation / Overselling Prevention | DATA, SYSD | ⬜ Not Started | — | — | 2026-07-27 | Classic high-volume logistics correctness bug. |
| T-012 | Resilience Quartet | RESIL | ⬜ Not Started | — | — | 2026-07-27 | Retry+jitter, circuit breaker, bulkhead, timeout budgets (Polly/Resilience). |
| T-013 | SLIs / SLOs / Error Budgets | RESIL, OBS, STAFF | ⬜ Not Started | — | — | 2026-07-27 | The language staff use for reliability trade-offs. |
| T-014 | Architecture Decision Records (ADRs) | STAFF | ❌ Canceled | — | — | 2026-07-28 | Program-seeded; user already has ADR experience. Not a practice target. |
| T-015 | Design Docs / RFCs w/ Trade-off Analysis | STAFF | ⬜ Not Started | — | — | 2026-07-27 | Staff artifact; defend decisions. |
| T-016 | System-Design Whiteboard Practice | SYSD, STAFF | ⬜ Not Started | — | — | 2026-07-27 | Timed end-to-end logistics system design. |
| T-017 | Aggregates & Aggregate Design | DDD | 🟡 In Progress | High | STORY-0001 | 2026-08-02 | Invariants + transactional boundaries; the consistency unit. |
| T-018 | Value Objects vs Entities | DDD | 🟡 In Progress | High | STORY-0001 | 2026-08-02 | Identity vs value equality; immutability. |
| T-019 | Ubiquitous Language | DDD | ⬜ Not Started | — | — | 2026-07-28 | Shared domain vocabulary across code + conversation. |
| T-020 | Bounded Contexts & Context Mapping | DDD | ⬜ Not Started | — | — | 2026-07-28 | Context boundaries + relationships; the module seams. |
| T-021 | Anti-Corruption Layer | DDD | ⬜ Not Started | — | — | 2026-07-28 | Isolate messy carrier/3PL models from your domain. |
| T-022 | Repository & Unit of Work | DDD, DATA | ⬜ Not Started | High | — | 2026-07-28 | Persistence abstraction + atomic transaction boundary. |
| T-023 | Domain Services vs Application Services | DDD, OOAD | 🟡 In Progress | — | STORY-0001 | 2026-08-02 | Where logic lives; orchestration vs domain rules. |
| T-024 | Specification pattern | DDD, OOAD | ⬜ Not Started | High | — | 2026-07-28 | Composable, reusable business rules/queries. |
| T-025 | Factories for complex aggregate creation | DDD, OOAD | ⬜ Not Started | — | — | 2026-07-28 | Enforce invariants at construction time. |
| T-026 | Consistency Boundary Identification | DIST, SYSD, DATA | ⬜ Not Started | — | — | 2026-08-02 | Place strong- vs eventual-consistency boundaries *explicitly*; reason about races there (pick decrement vs cycle count); relocate an invariant so the race stops existing rather than defending it at write time. |
| T-027 | High-Write-Throughput / Access-Pattern-Driven Data Modeling | DATA, SYSD | ⬜ Not Started | — | — | 2026-08-02 | Enumerate read/write paths and characterize each (read/write-heavy, append/mutate, hot/cold) *before* designing tables. Inventory-by-location ≠ inventory-by-SKU — separate design problems. |
| T-028 | Index Design for Specific Queries | DATA | ⬜ Not Started | — | — | 2026-08-02 | Key order + covering columns for the queries that actually run; seek vs scan; be able to state the resulting plan, not assume an index on the column is fast. |
| T-029 | Narrating Schema as Scale Reasoning | STAFF, DATA, SYSD | ⬜ Not Started | — | — | 2026-08-02 | Present a schema via access patterns + consistency boundaries, not a domain model rendered into tables. Declining to discuss the schema forfeits the vehicle for the scale conversation. |
| T-030 | Real-Time Push / Streaming to Clients at Scale | API, SYSD, RESIL | ⬜ Not Started | — | — | 2026-08-02 | Fan-out live state to many clients (tracking maps, yard/wave boards). SSE vs WebSockets vs gRPC-streaming as the concrete pick, but the real skill is connection state (sticky vs pub/sub backplane), backpressure on slow consumers (drop/coalesce/conflate), and resume-on-reconnect (cursor replay vs snapshot+delta). Links T-007, T-008, backpressure. |

<!--
ADD-ITEM CHECKLIST (see CLAUDE.md §5A):
1. Dedup against this table + categories.md alias table. If a possible match, STOP and confirm.
2. Assign next T-### (never reuse). Confirm Categories if ambiguous.
3. Status ⬜ Not Started, Added=today, append row, keep sorted by ID.
Next free ID: T-031
-->
