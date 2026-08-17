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
