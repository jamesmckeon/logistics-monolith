# Knowledge Categories & Item Catalog

This is the **taxonomy** and **dedup reference**. It defines the Knowledge Categories,
catalogs known Items (whether or not they're on the active backlog), and records aliases
so `add` requests can be matched against existing concepts.

- To see what's **actively tracked** and its status, see [backlog.md](backlog.md).
- The catalog below is the **menu** of things worth practicing. Items marked `[backlogged]`
  already have a `T-###`. Everything else is a candidate — adding it goes through the
  dedup-confirm flow in [CLAUDE.md](CLAUDE.md) §5A.

---

## Knowledge Categories

| Code | Category | Focus |
|---|---|---|
| **DDD** | Domain-Driven Design | Modeling the logistics domain: aggregates, invariants, ubiquitous language, bounded contexts. |
| **EDA** | Event-Driven Architecture | Producing/consuming events, messaging, async workflows, decoupling. |
| **DIST** | Distributed Systems | Consistency, failure, coordination, and time across services. |
| **SYSD** | System Design | End-to-end architecture, scaling, storage, trade-offs, NFRs. |
| **OOAD** | Object-Oriented Analysis & Design | Responsibilities, SOLID, design patterns, clean/hexagonal architecture. |
| **RESIL** | Resilience & Reliability | Behavior under failure and load: retries, timeouts, backpressure, SLOs. |
| **DATA** | Data & Storage | Modeling, concurrency, sharding, read models, caching for high write/read volume. |
| **API** | API & Contract Design | REST/gRPC design, versioning, idempotency, pagination, contracts. |
| **OBS** | Observability & Operability | Tracing, metrics, logging, SLIs/SLOs, operability. |
| **STAFF** | Staff/Principal Craft | Design docs/RFCs/ADRs, trade-off analysis, system-design communication, mentoring. |

An Item may belong to several categories (e.g., Outbox Pattern → EDA + DIST + DATA).

---

## Item Catalog

### DDD — Domain-Driven Design
- Domain Events `[backlogged: T-001]`
- Aggregates & aggregate design rules (invariants, transactional boundaries) `[backlogged: T-017]`
- Value Objects vs Entities `[backlogged: T-018]`
- Ubiquitous Language `[backlogged: T-019]`
- Bounded Contexts & Context Mapping `[backlogged: T-020]`
- Anti-Corruption Layer `[backlogged: T-021]`
- Repository & Unit of Work `[backlogged: T-022]`
- Domain Services vs Application Services `[backlogged: T-023]`
- Specification pattern `[backlogged: T-024]`
- Factories for complex aggregate creation `[backlogged: T-025]`

### EDA — Event-Driven Architecture
- Domain Events `[backlogged: T-001]`
- Outbox Pattern `[backlogged: T-004]`
- Inbox Pattern / message deduplication on the consumer `[backlogged: T-006]`
- Saga Pattern (orchestration vs choreography) `[backlogged: T-003]`
- Event Sourcing
- CQRS (command/query separation, read models) `[backlogged: T-007]`
- Change Data Capture (CDC)
- Dead-letter queues & poison-message handling
- Message ordering & partitioning
- Schema/contract evolution & versioning of events `[backlogged: T-009]`

### DIST — Distributed Systems
- Two-Phase Commit (and why to avoid it) `[backlogged: T-002]`
- Idempotency Keys `[backlogged: T-005]`
- Delivery semantics: at-least-once / at-most-once / exactly-once (myth) `[backlogged: T-008]`
- Eventual consistency & consistency models
- Consistency boundary identification (where strong vs eventual is required; races at the boundary; relocating invariants) `[backlogged: T-026]`
- Distributed locking & leader election
- Consistent hashing
- Logical/vector/hybrid-logical clocks; ordering & causality
- TCC (Try-Confirm/Cancel) as a 2PC alternative
- Distributed transactions vs sagas — trade-off analysis
- CAP / PACELC in practice

### SYSD — System Design
- Designing an order/shipment lifecycle service end-to-end
- Sharding & partitioning strategies
- Multi-tenancy
- Read models / materialized views
- Rate limiting & throttling design
- Caching strategy (cache-aside, write-through, invalidation)
- Feature flags / progressive delivery
- Capacity planning & back-of-envelope estimation
- Strangler-fig migration of a legacy monolith

### OOAD — Object-Oriented Analysis & Design
- SOLID principles (applied, not recited)
- Responsibility-driven design / CRC
- GoF patterns that actually earn their place (Strategy, State, Decorator, …)
- Hexagonal / Ports & Adapters
- Clean Architecture layering
- Domain modeling from requirements

### RESIL — Resilience & Reliability
- Retry with exponential backoff + jitter `[backlogged: T-012]`
- Circuit breaker `[backlogged: T-012]`
- Bulkhead isolation `[backlogged: T-012]`
- Timeout budgets / deadline propagation `[backlogged: T-012]`
- Backpressure & flow control
- Load shedding
- SLIs / SLOs / error budgets `[backlogged: T-013]`

### DATA — Data & Storage
- Optimistic vs pessimistic concurrency (row versioning, ETags) `[backlogged: T-010]`
- Transactional Outbox `[backlogged: T-004]`
- Inventory reservation / overselling prevention under contention `[backlogged: T-011]`
- High-write-throughput / access-pattern-driven data modeling (enumerate read/write paths first; shape per pattern) `[backlogged: T-027]`
- Index design for specific queries (key order, covering columns, seek vs scan) `[backlogged: T-028]`
- Narrating schema as scale reasoning (present schema via access patterns + consistency boundaries) `[backlogged: T-029]`
- Partitioning / time-series storage for tracking events
- Read/write model separation (part of CQRS + Read Models `[backlogged: T-007]`)

### API — API & Contract Design
- Idempotency Keys `[backlogged: T-005]`
- Idempotent HTTP verbs & conditional requests (ETag / If-Match)
- API versioning strategies
- Pagination (offset vs keyset/cursor)
- Consumer-driven contract testing
- gRPC / Protobuf for high-volume internal calls
- Long-running operations / async request-reply
- Real-time push / streaming to clients at scale (SSE vs WebSockets vs gRPC-streaming; fan-out, backpressure, resume) `[backlogged: T-030]`

### OBS — Observability & Operability
- Distributed tracing & correlation IDs (OpenTelemetry)
- Metrics (RED / USE method)
- Structured logging
- Health checks & readiness/liveness
- Alerting on SLOs

### STAFF — Staff/Principal Craft
- Architecture Decision Records (ADRs) `[backlogged: T-014]`
- Design docs / RFCs with trade-off analysis `[backlogged: T-015]`
- System-design interview practice (whiteboard a logistics system) `[backlogged: T-016]`
- Non-functional-requirements elicitation
- Reviewing & mentoring (giving staff-level feedback)

---

## Dedup alias table

Maps alternative phrasings/acronyms to a canonical Item. Extend this whenever the user
confirms that a new phrasing means an existing concept.

| Alias / phrasing | Canonical Item |
|---|---|
| 2PC, XA transaction, distributed commit | Two-Phase Commit |
| Idempotency key, dedup key, request key, Idempotency-Key header | Idempotency Keys |
| Transactional outbox, outbox | Outbox Pattern |
| Process manager, orchestrator saga, choreography saga | Saga Pattern |
| Domain event, integration event* | Domain Events (*note: integration events are related but distinct — confirm) |
| Optimistic locking, row versioning, `xmin`/`rowversion` | Optimistic vs pessimistic concurrency |
| OTel, tracing, spans | Distributed tracing & correlation IDs |
| Read model vs write model separation, read/write model split | CQRS + Read Models (T-007) |
| Access-pattern-driven storage modeling, access-pattern-first schema design | High-Write-Throughput / Access-Pattern-Driven Data Modeling (T-027) |
| Consistency boundary placement, strong vs eventual boundary | Consistency Boundary Identification (T-026) |
| Covering index, index seek vs scan, key-order design | Index Design for Specific Queries (T-028) |
| Schema as scale story, narrating the schema | Narrating Schema as Scale Reasoning (T-029) |
| SSE vs WebSockets, server-sent events, live push, streaming to clients, real-time fan-out | Real-Time Push / Streaming to Clients at Scale (T-030) |
