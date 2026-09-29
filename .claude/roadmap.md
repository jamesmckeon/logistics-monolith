# Roadmap & Suggested Topics

Two things live here:
1. **Claude's suggested Items** you didn't name yet — candidates to pull onto the backlog.
2. A **suggested learning sequence** so the practice compounds toward the staff/principal,
   high-volume-logistics goal.

Nothing here is auto-added to the backlog. Say "add T-candidates X, Y, Z" (dedup runs
first) to promote any of these.

---

## 1. Suggested Items you haven't listed yet

Your seed items (Domain Events, 2PC, Saga, Outbox, Idempotency Keys) are a strong core.
Here are high-leverage additions for the target roles, grouped by theme. ★ = I'd
prioritize these for a high-volume logistics + staff/principal track.

### Consistency & messaging (the natural next ring around your seeds)
- ★ **Inbox pattern / consumer-side deduplication** — the other half of outbox; makes
  at-least-once delivery safe. Pairs directly with T-004/T-005.
- ★ **CQRS + read models** — separate write model from query-optimized read models; core
  to scaling logistics reads (tracking, dashboards).
- ★ **Event Sourcing** — audit-perfect shipment/order history; teaches you when *not* to
  use it, too.
- **Change Data Capture (CDC)** — outbox's competitor (Debezium-style); trade-off practice.
- ★ **Delivery semantics** — at-least-once vs at-most-once, and why "exactly-once" is a
  myth handled by idempotency + dedup. Ties your seeds together conceptually.
- **Message ordering & partitioning** — per-shipment ordering guarantees at volume.
- ★ **Schema/contract evolution** — versioning events so producers/consumers deploy
  independently; consumer-driven contract tests.
- **Dead-letter queues & poison-message handling.**

### Distributed systems depth
- ★ **Optimistic vs pessimistic concurrency** — inventory reservations under contention
  (overselling is *the* classic logistics bug). High ROI.
- **TCC (Try-Confirm/Cancel)** — a 2PC alternative; excellent contrast piece for T-002.
- **Distributed locking & leader election** — single-active workers, scheduled jobs.
- **Logical/vector/hybrid-logical clocks** — ordering without a global clock.
- **Eventual consistency modeling** — making it explicit and safe rather than accidental.

### Resilience & reliability (RESIL) — under-practiced, heavily interviewed
- ★ **Retry w/ exponential backoff + jitter**, **circuit breaker**, **bulkhead**,
  **timeout/deadline budgets** — the resilience quartet, via
  `Microsoft.Extensions.Resilience` / Polly.
- **Backpressure & load shedding** — surviving traffic spikes (peak-season logistics).
- ★ **SLIs / SLOs / error budgets** — the language staff engineers use to make reliability
  trade-offs.

### Data & scale (DATA / SYSD)
- ★ **Inventory reservation / overselling prevention** — a whole story arc on its own.
- **Sharding & partitioning strategies**; **keyset (cursor) pagination** for huge lists.
- **Caching strategy** (cache-aside, invalidation) — `HybridCache` in .NET 9+.
- **Rate limiting** — `System.Threading.RateLimiting`.
- **Back-of-envelope capacity estimation** — sizing a high-volume system.

### Architecture & API (OOAD / API)
- ★ **Hexagonal / Ports & Adapters** and **Clean Architecture** — how you'll structure
  every practice API; makes testing and pattern-swapping easy.
- **Anti-Corruption Layer** — integrating messy carrier/3PL APIs cleanly.
- **gRPC/Protobuf** for internal high-volume calls; **async request-reply** for
  long-running operations (e.g., label generation, route optimization).
- **Idempotent HTTP verbs & conditional requests (ETag/If-Match).**

### Observability (OBS)
- ★ **Distributed tracing + correlation IDs (OpenTelemetry)** — non-negotiable at scale;
  practice propagating context across the async/messaging boundary.
- **RED/USE metrics**, **structured logging**, **health checks**.

### Staff/Principal craft (STAFF) — what actually separates senior from staff
- ★ **Architecture Decision Records (ADRs)** — write one per non-trivial choice you make
  in these exercises. Cheap, and exactly the artifact staff engineers are judged on.
- ★ **Design docs / RFCs with explicit trade-off analysis.**
- ★ **System-design practice** — whiteboard a high-volume logistics system end-to-end
  (I can run these as timed prompts).
- **Non-functional-requirements elicitation**; **mentoring/review** skills.

---

## 2. Suggested learning sequence

A dependency-aware order. Each phase builds a small logistics service and layers on the
next concepts. Do the stories in phase order; do the reading/ADR alongside.

**Phase 0 — Foundation (structure everything else sits on)**
`Hexagonal/Ports & Adapters` → `DDD: Aggregates & invariants` → `Value Objects vs
Entities` → `Domain Events (T-001)`. Deliverable: an Ordering context you can extend.
Write your first **ADR** here.

**Phase 1 — Safe writes & retries**
`Idempotency Keys (T-005)` → `Optimistic concurrency` → `Inventory reservation /
overselling`. Deliverable: a reservation endpoint that's correct under retries and
concurrency.

**Phase 2 — Cross-service consistency**
`Delivery semantics` → `Outbox (T-004)` → `Inbox / consumer dedup` → `Two-Phase Commit
(T-002)` (learn + reject) → `Saga (T-003)`. Deliverable: an order→reserve→ship→bill flow
that survives partial failure. This is the heart of the program.

**Phase 3 — Read side & scale**
`CQRS + read models` → `Event Sourcing` (optional) → `keyset pagination` → `caching` →
`sharding/partitioning`. Deliverable: a tracking/read API that scales independently.

**Phase 4 — Resilience & operability**
`Resilience quartet` → `backpressure/load shedding` → `OpenTelemetry tracing` →
`SLIs/SLOs`. Deliverable: the phase-2 flow, now observable and hardened.

**Phase 5 — Staff synthesis**
`System-design whiteboards` + `RFCs/ADRs` over everything built. Deliverable: a design
doc that ties the contexts into one high-volume logistics platform, with trade-offs you
can defend in an interview.

---

_Update this file as priorities shift. When an Item here gets promoted to the backlog,
it also picks up a `[backlogged: T-###]` tag in [categories.md](categories.md)._
