# Authoritative Sources (Project Canon)

Governs how Claude vets responses on **api-lab**. Read this before any design,
architecture, or DDD answer, and ground claims against it.

## Status legend

- **✅ Confirmed** — authoritative. The source's definitions/conventions **override
  Claude's instinct**; Claude may assert their positions as grounded. Conflicts
  *between* confirmed sources are surfaced (never picked silently) and settled by an ADR.
- **⏳ Pending** — reference/awareness only. Claude may mention them but must **label them
  unconfirmed** and must **not** present their opinions/conventions as authoritative, nor
  let them override a confirmed source or settle a decision. A topic covered only by
  pending sources (or none) is **ungoverned**: Claude flags it and treats its guidance as
  reasoning, not doctrine.

## Sources

| # | Source | Lineage / use | Status |
|---|--------|---------------|--------|
| 1 | Evans, *DDD* (Blue Book); Vernon, *Implementing DDD* (Red Book) | DDD concepts: value objects, entities, aggregates, bounded contexts, context mapping | ✅ Confirmed |
| 2 | Fowler, *PoEAA* + martinfowler.com | Enterprise/persistence patterns: **Money, Gateway**, Repository, Unit of Work, Service Layer; anti-patterns (Anemic Domain Model) | ✅ Confirmed |
| 3 | R. C. Martin — SOLID/DIP, *Clean Architecture*; A. Cockburn — Hexagonal (Ports & Adapters) | Architecture principles: dependency rule, ports/boundaries | ✅ Confirmed |
| — | [kgrzybek/modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd) | .NET reference implementation: modular structure, inter-module messaging, CQRS/outbox | ✅ Confirmed |
| 4 | Wlaschin, *Domain Modeling Made Functional* | Result / railway-oriented modeling within DDD | ⏳ Pending |
| 5 | Ardalis / eShopOnWeb + libs (`Ardalis.Result`, `GuardClauses`, `Specification`, `SmartEnum`) | .NET tactical rendering; Result library | ⏳ Pending |
| 6 | Bogard — Vertical Slice Architecture | Feature-first alternative to layered/clean architecture | ⏳ Pending |

## Operating rules

1. **Confirmed wins.** When Claude's instinct conflicts with a confirmed source, the source wins.
2. **Conflicts between confirmed sources** are surfaced, not resolved silently → an ADR decides for this project.
3. **Gaps are flagged.** If only pending sources (or none) cover a topic, Claude says so and marks its answer as reasoning, not doctrine.
4. **Public sources are fetchable/quotable** (Grzybek, eShopOnWeb, martinfowler.com, MS Learn). Book sources (Evans, Vernon, Fowler PoEAA, Martin, Wlaschin) are cited from knowledge — flag "not verified against the text this turn."

## Known gaps & notes

- **`Result<T>` is currently ungoverned.** The confirmed set (Evans/Vernon/Fowler/Martin)
  is silent on Result — it belongs to the functional/railway lineage (Wlaschin, #4), which
  is Pending. Until a Result source is confirmed, Result guidance is Claude's reasoning,
  not doctrine.
- **Fowler's Value Object (PoEAA) ≠ DDD's Value Object.** For VOs, defer to Evans/Vernon (#1);
  use Fowler as cross-reference.
- **#3 is contested.** Clean Architecture is the orthodoxy that Bogard's VSA (#6, Pending)
  reacts against; treat layering as *a* choice with a live opposing school. Cockburn's
  Hexagonal predates and parallels Martin's formulation. *Clean Code* (craftsmanship) is
  deliberately excluded — #3 is SOLID + Clean Architecture only.
