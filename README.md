# logistics-monolith

**Throughline** — a .NET 10 modular monolith that models a
high-volume 3PL fulfillment & shipping platform (Ordering, Inventory, Fulfillment,
Shipping, Tracking, Billing).

## Why this exists

A **.NET 10 take on** [kgrzybek/modular-monolith-with-ddd](https://github.com/kgrzybek/modular-monolith-with-ddd),
differing deliberately on three axes:

- **My modeling & coding practices**, not a generic sample.
- **Logistics** — a rich domain that's underrepresented online.
- The **spec → test → code** workflow I drive with Claude, on display.

It also re-answers a stack question the original couldn't: much of the ecosystem it leaned
on has **gone commercial** — MediatR & AutoMapper (Lucky Penny, 2025) and MassTransit (v9,
2026). This repo stays **license-clean** and current on the modern .NET platform instead.

Architecture and tooling decisions are tracked in
[docs/decisions/](docs/decisions/) (product & engineering ADRs).

# Modelling

I'm not a purist.  If an approach or framework isn't immediately appropriate for a scenario, I most likely will find one that's better suited.  While consistency is crucial for making code navigable, forcing a pattern can actually make it less readable and understandable.  

I strongly prefer allowing business logic that is written in natural logic and modelled intuitively; limiting its expressiveness so it can integrate with a specific ORM or service framework is never an option for me.