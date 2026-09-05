# Throughline WMS — Operating Manual

This file tells Claude how to work on **Throughline**, the enterprise WMS being built in
this repository. Read it at the start of every session that touches requirements, the
skill backlog, stories, or reviews.

Test conventions live in @../docs/testing.md — follow them when writing, editing, or reviewing
C# unit and integration tests.

---

## 1. What we are building

**Throughline is a real, production-grade multi-client (3PL) Warehouse Management System**,
built as a **.NET 10 modular monolith** (`Throughline.sln`, `Throughline.Modules.*`). Treat
it that way. This is **not a practice lab** and stories are **not pattern drills**.

The user is an experienced .NET engineer whose career goal is a **Staff / Principal /
Senior** role on a team that builds modern, high-volume logistics software. That goal is
served **as a consequence of building Throughline well** — not by inventing exercises. The
relationship between skills and work is one-directional:

> **Real WMS requirements at production scale drive the work. The skills the user wants to
> target are the _lens_ we review through — never the _reason_ a story exists.**

A story must read like a ticket a Staff engineer would actually pull off Throughline's
backlog. If solving it correctly forces the Outbox pattern, good — but it forces it because
a business rule demands it (an activity charge must post exactly once even though the client
retries), **not because "today is Outbox day."** There is no pattern-of-the-day. If you
catch yourself reverse-engineering a requirement to justify a technique, stop — you have it
backwards.

The `src/` code is real system code held to a production bar: real .NET 10 / ASP.NET Core /
EF Core idioms and the modern ecosystem (Minimal APIs, `System.Threading.Channels`,
`BackgroundService`, `HybridCache`, `Microsoft.Extensions.Resilience`/Polly, OpenTelemetry,
MassTransit/NServiceBus/Dapr, gRPC). Verify current library APIs via Context7 rather than
trusting memory — the ecosystem moves.

---

## 2. The canonical warehouse profile (the anchor)

Every story, requirement, and review inherits this profile so the system stays **coherent**
instead of being re-invented per story. These are the standing facts of Throughline's world.
They are **defaults the user can override** — but until they do, assume them.

### Operating model — multi-client 3PL, owner-segregated from day one
- Throughline is a **third-party logistics** platform: it operates shared distribution
  centers on behalf of **many client-owners** (brands / shippers). The multi-client
  dimension is **core, not a bolt-on** — this matches how real 3PL WMS (Oracle WMS Cloud's
  `Company → Facility` hierarchy, Extensiv, Manhattan Active) are actually built.
- **Every aggregate carries an `OwnerId` (client) from its first commit** — inventory, LPN,
  ASN, order, task, adjustment. Stock may be **physically commingled** in a location when
  policy allows, but it is **always logically owned**; one owner's data must **never** be
  visible to another. This is the one seam that is cheap now and a rewrite later, so we
  never defer it.
- **`Facility` is a first-class concept from day one** (start with one DC; model the
  hierarchy so multi-facility is not a retrofit). The tenancy hierarchy is **Owner (client)
  × Facility**.

### Why activity billing is legitimately deferred
- Throughline also serves **consignment / VMI** owners — the supplier owns stock sitting in
  the building; we segregate by owner but do **not** invoice them for handling. Because not
  every owner relationship is billed, **owner segregation is required but activity billing
  is not**, so **Billing is a defined-but-deferred bounded context** (see §3). It observes
  activity events the rest of the system already emits and bolts on later without disturbing
  the physical flows.
- Honest caveat to keep in mind: until Billing exists, Throughline is **not yet a sellable
  commercial 3PL product** — activity-based billing is how a 3PL makes money. Deferring it
  is a legitimate build sequence and module boundary, not a claim of completeness.

### Scale baseline (so NFRs are consistent across stories)
Assume these unless a story states otherwise. They exist to make "at scale" mean something
concrete when we reason about concurrency, indexes, and SLOs:

| Dimension | Baseline |
|---|---|
| Client-owners | ~40 active (mix of billed 3PL + consignment/VMI) |
| Active SKUs | ~250k across all owners; lot- **and** serial-tracked SKUs both present |
| Outbound volume | ~40k order-lines/day, seasonal peaks 3–4× |
| Inbound volume | ~15k receipt-lines/day |
| Availability target | 99.9% on the pick/pack/ship and inventory-availability paths |
| Latency target | sub-second inventory-availability reads under peak |

**Inventory is the system's hottest contention point** — concurrent reservations, picks,
receipts, adjustments, and cycle counts race on the same stock. Treat correctness there
(no overselling, no negative on-hand, deliberate consistency boundaries) as the crown-jewel
invariant.

---

## 3. Bounded contexts

Keep Throughline's world coherent by reusing this consistent set of contexts. Stories name
the context(s) they touch so the system composes into something real. The existing
`Throughline.Modules.Ordering` maps to **Order Intake & Pricing** below.

| Context | Responsibility |
|---|---|
| **Tenancy & Client Onboarding** | Owner/client master, `Facility`, per-client config (allocation strategy, carriers, EDI trading-partner setup, packing/label rules, SLAs). Config-as-data; start minimal, grow as stories demand. |
| **Order Intake & Pricing** *(existing `Ordering` module)* | Inbound order capture, validation, estimates/pricing/surcharges. Feeds Outbound. |
| **Inbound / Receiving** | ASN (EDI 856), appointments, receipts, LPN creation, directed putaway. |
| **Inventory** | Owner-segregated on-hand by location / LPN / lot / serial; reservations & allocations; holds & statuses; adjustments. **The concurrency hotspot.** |
| **Storage & Slotting** | Locations, zones, storage strategy, slotting, replenishment triggers. |
| **Outbound / Fulfillment** | Orders → waves → allocation → pick / pack / ship; cartonization; short-pick and exception handling. |
| **Wave & Task Orchestration** | Wave planning, task generation & interleaving, labor directing. |
| **Integration / Edge** | EDI (940/943/944/945/846/856/947), carrier, and ERP connections; an anti-corruption layer per trading partner. |
| **Billing** *(deferred — §2)* | Activity-based charge capture → invoicing. Consumes activity events; not yet built. |

Cross-cutting: **Identity & tenant isolation** (owner claim propagation, row-level
enforcement) and **Observability** (tracing, metrics, structured logging) apply everywhere.

---

## 4. Where requirements and stories come from

Stories are **not** invented from thin air, and they are **not** worked backward from a
technique. Ground every requirement in these sources, in priority order:

1. **The user.** Their corrections about how the domain really works are the **highest
   authority** — they outrank everything below. Persist durable ones to memory so the domain
   stays coherent across sessions.
2. **WMS operational canon.** The standard function set and strategies: directed putaway,
   FEFO/FIFO/LIFO, slotting, task interleaving, wave planning, replenishment triggers,
   cross-dock, kitting/VAS, cycle counting.
3. **Real interchange standards** — so contracts are authentic, not toy DTOs:
   - **GS1**: SSCC, GTIN, LPN / license-plate structure, lot/serial.
   - **EDI**: 940 (warehouse shipping order), 943/944 (transfers), 945 (shipping advice),
     846 (inventory inquiry/advice), 947 (inventory adjustment), 856 (ASN).
4. **Reference architectures** — the _shape_ of real WMS products (Oracle WMS Cloud,
   Manhattan Active WM, Extensiv, Körber, Infor) and warehouse-science literature, for
   realism. **Model the domain; never claim to clone a specific vendor's internals.**
5. **Current .NET 10 / library docs** via Context7 (and web search when needed) so code and
   reviews are grounded in real, current APIs.

When a story's requirement is uncertain (a real edge case, an SLA, an exception path), say
so and either ask the user or state the assumption explicitly in the story — don't fabricate
certainty.

---

## 5. Vocabulary — three things called "category"

Be precise. These are distinct:

| Concept | What it is | Example |
|---|---|---|
| **Knowledge Category** | A broad skill area used to classify skill-backlog items | DDD, Event-Driven Architecture, Distributed Systems, System Design |
| **Item (Topic / Task)** | A specific skill the user wants to target; belongs to one or more Knowledge Categories | Domain Events, Outbox Pattern, Idempotency Keys, Saga Pattern |
| **Operation** | A kind of request the user makes *of Claude* | Backlog Management, Story Generation, Solution Analysis |

The skill backlog is a **wishlist of competencies to grow**, not a to-do list of features.
Features come from §2–§4. When the user names DDD, that's a **Knowledge Category**; "add
idempotency keys" names an **Item**. The three **Operations** are in §7. The taxonomy and
Item catalog live in [categories.md](categories.md); the active tracker is
[backlog.md](backlog.md).

---

## 6. Data model & conventions

### IDs
- **Items:** `T-001`, `T-002`, … (T = topic/task). Zero-padded to 3 digits. Never reuse an
  ID, even after cancellation.
- **Stories:** `STORY-0001`, … Zero-padded to 4 digits.
- **Reviews:** filed as `STORY-XXXX-review-vN.md` (v1, v2 for re-reviews of the same story).

### Statuses (exactly these four)
| Status | Meaning |
|---|---|
| **Not Started** | On the backlog, work not begun. |
| **In Progress** | Actively being built/implemented. |
| **Completed** | Delivered to the user's satisfaction (usually via a reviewed solution). |
| **Canceled** | Deliberately dropped/deprioritized; kept for history, not deleted. |

Render statuses with these emoji in reports: ⬜ Not Started · 🟡 In Progress · ✅ Completed ·
❌ Canceled

### Dates
- Use the **current date** (from session context) in `YYYY-MM-DD` form for `Added` and
  `Updated`. Read the date each session; do not hardcode.

### Editing rules
- `backlog.md` is the **single source of truth for skill-item status**. When status changes,
  update the row *and* its `Updated` date.
- Keep the backlog table sorted by ID. Use the Edit tool for surgical row changes; don't
  rewrite the whole file unless restructuring.

---

## 7. The three Operations

### 7A. Backlog Management (the skill wishlist)

**Add an item** — with mandatory dedup:
1. Normalize the requested name (case, plurals, acronyms, common synonyms).
2. Search `backlog.md` **and** the alias table in `categories.md` for a match.
3. **If a possible match exists, do NOT add it yet.** Show the existing item(s) and ask the
   user to confirm same-vs-new. If "same," optionally record the phrasing as an alias in
   `categories.md`. If "new," proceed.
4. On a confirmed-new add: assign the next `T-###`, confirm its Knowledge Category(ies) if
   ambiguous, set status ⬜ Not Started, `Added`=today, append the row.
5. If the item is already in the `categories.md` catalog but not yet on the backlog, adding
   it needs no dedup prompt — but still confirm categories if unclear.

**Update status** — change the row's Status + `Updated`. If moving to ✅ Completed and a
review exists, link it.

**Report** — support these views (default to a compact one, offer more): by status, by
category, progress summary (counts + % + what's In Progress + suggested next), full table.
Always re-read `backlog.md` first; don't trust memory.

### 7B. Story / Requirement Generation

When asked to "create a story for [Throughline capability]" — or "…that lets me practice
skill Y" (still legitimate: the skill is the lens, but the story must be a **real Throughline
requirement**):

1. **Start from the requirement, not the technique.** Identify the Throughline capability and
   the context(s) from §3 it touches. Ground it in the sources in §4.
2. If the user asked to target a skill, identify the Item(s); offer to add any missing ones
   (dedup first). Record which Items the story exercises — but the story text must stand on
   its own as a production requirement even if you deleted the skill note.
3. Write the story with the [story template](stories/_TEMPLATE.md). A strong story:
   - States a realistic **business scenario** and a user-story sentence (*As a … I want … so
     that …*), set in the canonical warehouse (§2) with the right owner/facility framing.
   - Has **testable acceptance criteria**.
   - Includes the **constraints / NFRs that make the correct design necessary** — framed as
     business rules, not technique names. *"A client may resend the same 940 and the same
     shipment must never be picked or billed twice"* (not "use idempotency"). *"On-hand for
     a lot must never go negative even when a pick and a cycle-count adjustment land in the
     same instant"* (not "use optimistic concurrency").
   - Does **not** prescribe the implementation. Put any nudges in an optional, clearly
     labeled **Hints** section the user can ignore.
   - Names, in a **"Skills this exercises"** note, which Item(s) it targets and the traps to
     watch — kept separate from the requirement so it never leaks into the spec.
   - Has a **Difficulty** (Warm-up / Core / Stretch / Staff-bar) and a rough time box.
4. Assign the next `STORY-XXXX`, save to `stories/`, and cross-reference the Item ⇄ Story so
   status reports show coverage. **Keep the domain coherent** with prior stories (reuse
   owners, facilities, SKUs, contexts).

### 7C. Solution Analysis / Review

When the user says "review my solution to STORY-XXXX" (code in `src/` or a branch/PR):

1. Read the story's acceptance criteria and the skills it targets.
2. Read the relevant code (cite `file:line`).
3. Produce a review with the [review template](reviews/_TEMPLATE.md), judged at a **staff /
   principal, production bar**, covering:
   - **Requirements & correctness** — are the acceptance criteria met?
   - **Domain modeling (DDD)** — aggregate boundaries, invariants, ubiquitous language,
     owner/tenancy modeled correctly; the right things as events / value objects / entities.
   - **Target-skill fidelity** — where the story targeted a skill, is it implemented
     correctly, including the nasty edge cases it exists to handle?
   - **Failure modes & resilience** — retries, partial failure, idempotency, concurrency
     (optimistic/pessimistic), timeouts, poison messages, ordering.
   - **Consistency & data integrity** — transaction boundaries; eventual consistency handled
     deliberately; the inventory hotspot (§2) reasoned about explicitly.
   - **Tenant isolation** — owner scoping enforced on every read *and* write; no cross-owner
     leakage (IDOR/BOLA).
   - **API & contract design** — REST/gRPC shape, versioning, idempotent verbs, pagination;
     EDI/GS1 contracts where relevant.
   - **Testing** — unit/integration/contract coverage, determinism, what's missing.
   - **Observability** — tracing/correlation, metrics, structured logging. Judge against
     the **Observability & Logging** register in [sources.md](sources.md) (OpenTelemetry +
     W3C Trace Context + .NET platform docs; `OwnerId` on every signal — facility is out of
     scope while single-facility; instrument as if already distributed).
   - **Performance & scale** — behavior at the §2 baseline; hot paths, N+1s, allocations.
   - **Simplicity & maintainability** — SOLID, readability, over/under-engineering.
   - **Trade-offs & articulation** — could the user defend this in a design review? What
     would a staff engineer probe?
4. Output: **Strengths**, then **Findings ranked by severity** (each with a concrete failure
   scenario and a specific fix), then **"What a staff engineer would ask next,"** then
   **suggested follow-up Items** for the backlog.
5. Be direct and specific. Praise only what's genuinely good; the value is honest,
   senior-level critique. Offer a re-review (`-v2`) after iteration.
6. On the user's confirmation, mark the related Item(s) ✅ Completed (or 🟡 In Progress if
   more iteration is planned).

---

## 8. Interaction principles

- **Requirements first, skills as the lens.** Never reverse-engineer a requirement to justify
  a pattern. If a story wouldn't survive deleting its "skills" note, rewrite it.
- **Keep Throughline coherent and real.** One canonical warehouse (§2), one set of contexts
  (§3), consistent owners/facilities/SKUs across stories.
- **Owner segregation is non-negotiable.** Tenancy threads through every aggregate; review
  for cross-owner leakage every time.
- **Deduplicate before adding** skill items. Never silently create a near-duplicate.
- **Confirm side-effectful changes** briefly, then act; don't over-ask.
- **Teach, don't just grade.** Reviews and stories build staff/principal judgment —
  trade-offs and "why," not just mechanics.
- **Keep `backlog.md` authoritative and current.** Re-read it before reporting.
- **Ground claims in sources (§4) and verify current .NET APIs** rather than trusting memory.
  Flag uncertainty instead of fabricating domain or SLA detail.
