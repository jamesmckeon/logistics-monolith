# ADR-0008 — Module extractability contract & deployment posture

## Status

Proposed — 2026-08-19. Ratify when the first module is actually extracted to its own host, or
the first cross-module integration event is built — whichever first tests the contract against
reality.

## Context

This repo is positioned as a **.NET 10 successor to
[kgrzybek/modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd)**, and
its central narrative is **monolith-first, extract-when-proven**: modules live in one process now
and split into their own processes only when a concrete force justifies it. For that evolution to
be a *deployment change* rather than a *rewrite*, extractability has to be a designed-in property
held from the **first** module — the couplings that block extraction (shared tables, in-process
assumptions, reliable-delivery assumptions) are cheap to forbid early and ruinously expensive to
unwind later.

A module inside the monolith is a set
of class libraries with no deployment target of its own.It gains one only when wrapped in a
host. 

This ADR settles two things the register left implicit across MOD-07/09/10/13/16/18:

1. **What justifies extracting a module** — so we neither pre-split nor drift into a distributed
   big-ball-of-mud, and
2. **The host-agnostic contract every module must satisfy** — which is also the real answer to
   "what tech stacks must a module be compliant with."

## Decision

### A. Extraction is demand-driven, along modeled bounded-context seams

Extract a module only when a **concrete force** outweighs the operational cost of running another
process (Newman, *Monolith to Microservices*: extract when the coupling cost of keeping it in
exceeds the operational cost of pulling it out). Do **not** pre-split. The recognised drivers:

- **Independent scaling** — a module's load profile diverges (Tracking ingest vs transactional Ordering).
- **Independent deploy cadence / team autonomy** (Conway) — a team needs to ship without a monolith release.
- **Fault isolation** — a crash-prone or risky module (flaky 3PL/carrier calls) must not sink order capture.
- **Divergent runtime / resource needs** — CPU-bound routing/optimization wants its own scaling, maybe its own runtime.
- **Compliance / data residency** — a regulatory boundary (PCI on Billing, EU shipment data) forces a process boundary.
- **Divergent SLA / availability** — 99.99% Tracking-ingest vs 99% internal reporting.

Most drivers are organisational or scaling forces, **not** "the code got big." Expect the **first**
extraction to be a **worker** (outbox / high-volume ingest) — stateless, async, clearest scaling
driver, lowest risk — not a request/response module.

### B. The extractability contract — every module complies from day one

1. **Host-agnostic module code.** No host-specific APIs: no `System.Web`, no `HttpContext` past the
   `.Api` layer into Application/Domain, no `web.config`-driven behaviour. Configuration comes from
   `IConfiguration` / env vars / `IOptions`, never machine config.
2. **Cross-module communication only through the transport-swappable seam** (MOD-13): in-process
   today, RabbitMQ / Azure Service Bus tomorrow, **same contract**. No module reaches into another
   module's internals — inbound cross-module calls go through its published `*.Contracts`.
3. **Data ownership** (MOD-07): schema-per-module in one Postgres now → database-per-module on
   extraction. No cross-module foreign keys, joins, or shared writes. Shared tables are un-splittable.
4. **Integration is hostile-from-day-one** (MOD-16 / MOD-02): at-least-once, idempotent,
   duplicate- and reorder-tolerant, inbox/outbox. The in-proc version must **not** assume reliable,
   ordered, exactly-once delivery — the network will violate every one of those on extraction.
5. **Observability crosses the async boundary** (MOD-09): OTel context propagation is already wired
   across the messaging seam, so a single-process trace becomes an N-process trace with no code change.
6. **Stateless + graceful shutdown**: no in-process session state; `BackgroundService`s honour
   cancellation (drain); health/readiness endpoints exist. Required for horizontal scale, scale-to-zero,
   and clean SIGTERM on ACA/k8s.
7. **Container-publishable** (MOD-18): `dotnet publish /t:PublishContainer`, linux, non-root — no Dockerfile.

Enforcement is not optional: clauses 1–3 rot without **architecture tests** (MOD-12, NetArchTest).

### C. Deployment posture

- **Target OCI (linux) containers as the lowest common denominator.** Hold the contract above and
  every target below is reachable with **no module code change** — only infra/manifest differs.
- **Primary home: Azure Container Apps** (KEDA autoscale incl. scale-to-zero and scale-on-queue-depth,
  optional Dapr sidecar, managed ingress) — the natural fit for event-driven logistics workers.
  **Container Apps Jobs / Azure Functions** for the pure outbox/ingest workers; **AKS** only where
  full k8s control is genuinely needed; **App Service (Web App for Containers)** as a simpler fallback.
- **IIS / on-prem Windows is a non-goal** for this greenfield .NET 10 repo and is deliberately
  designed *away* from — but remains **possible for free**, because clause B.1 forbids exactly the
  host-specific couplings that would block it. If a client ever demanded on-prem IIS, a module runs
  under Kestrel as a Windows service or behind IIS as a reverse proxy, with no module change.
- **The presentation structure makes extraction cheap.** Because each module owns its `.Api`
  endpoints and exposes `Add{Module}()` / `Map{Module}()` extensions (the MOD-08 direction),
  extracting a module is a **new ~20-line host** that references that one module and calls those two
  extensions. **.NET Aspire (MOD-10)** is the local rehearsal of this multi-process topology.

## Consequences

**Positive**

- Extraction is a deployment change, not a rewrite; the deployment target is late-bound and swappable.
- IIS-compatibility is a free by-product of host-agnosticism, not a design tax we pay up front.
- The contract forces the right distributed-systems habits (idempotency, data ownership, context
  propagation) *early*, while they are cheap — instead of discovering them under load after a split.
- Every future module gets a concrete compliance checklist; "is this module extractable?" becomes testable.

**Costs / implications**

- **Hostile-from-day-one integration is real up-front work** for what is, today, an in-process call
  that "could" be a plain method invocation. Accepted: it is the load-bearing clause — skipping it is
  the thing that turns extraction into a rewrite.
- **Schema-per-module forbids convenient cross-module joins.** Data another module owns is reached via
  contracts / read models, not a SQL join. This is a genuine ergonomic cost paid for splittability.
- **OTel-across-the-boundary must exist before it visibly pays off** — wiring cost with deferred benefit.
- **The contract is only as strong as its enforcement.** Without NetArchTest (MOD-12) guarding clauses
  1–3, the boundaries erode silently and the contract becomes aspirational.

**Alternatives considered**

- **Design for the monolith only; refactor to extract if/when needed (pure YAGNI).** Rejected: the
  couplings that block extraction (shared tables, reliable-delivery assumptions, in-proc reach-ins) are
  precisely the ones that are ruinously expensive to unwind later. The contract is cheap to hold now,
  dear to retrofit — this is the rare case where a little up-front design is the YAGNI-honest choice.
- **Pre-split into microservices now.** Rejected: contradicts the monolith-first positioning and pays
  the distributed-systems tax before any driver justifies it.
- **Target a specific host as a first-class goal** (build for ACA specifically, or hold IIS as a
  primary target). Rejected: couples the design to a deployment choice that should stay late-bound;
  host-agnosticism reaches all targets at once and keeps IIS possible without making it a constraint.
- **Centralised presentation (kgrzybek's single `API` project).** Rejected in the presentation
  discussion: a shared host becomes the carve-out point on extraction, whereas a per-module `.Api`
  means the module's HTTP surface leaves with the module.

**Revisit when**

- The first module is actually extracted (ratify, or amend the contract against what reality demanded).
- A driver appears that section A's list does not cover.
- A hard on-prem / IIS requirement materialises (reopens the section C non-goal).
- The transport seam (MOD-13 / MOD-02) is built — may tighten clauses B.2 and B.4 with specifics.

## Related

- **Modernization Register** — anchored by **MOD-20**; binds **MOD-13** (transport-swappable events),
  **MOD-07** (data ownership), **MOD-09** (observability across the boundary), **MOD-18** (container
  publish), **MOD-10** (Aspire rehearsal), **MOD-16 / MOD-02** (hostile-from-day-one integration),
  **MOD-12** (boundary enforcement).
- **MOD-08 / per-module presentation** — the `.Api` + `Add{Module}`/`Map{Module}` structure this ADR
  relies on to make extraction a ~20-line host.
- **ADR-0006** — module internal structure (layered vs vertical slice); either way the module stays a
  self-contained vertical, including its HTTP surface, which is what makes it extractable.
- kgrzybek/modular-monolith-with-ddd — the reference implementation this repo modernizes.
