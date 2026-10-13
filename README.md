# logistics-monolith

[![Build & Test](https://github.com/jamesmckeon/logistics-monolith/actions/workflows/build-and-test.yaml/badge.svg?branch=main)](https://github.com/jamesmckeon/logistics-monolith/actions/workflows/build-and-test.yaml)

> **🚧 Work in progress.** Throughline is under active development and is built one story
> at a time. Some modules work end to end; others are planned scope only. See
> [Status](#status) for what exists today.

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

## Status

Work is driven by [GitHub issues](https://github.com/jamesmckeon/logistics-monolith/issues).
Each issue is a business-level story, built spec → tests → code, and merged by PR once CI
is green.

| Module | State | What exists |
|---|---|---|
| Ordering | ✅ On `main` | Order intake with validation and acknowledgement; safe handling of 940 retransmissions (equivalent resends acknowledged, changed duplicates rejected); owner-scoped OpenTelemetry tracing on the intake path; `OrderConfirmed` published through a transactional outbox (Wolverine) |
| Inventory | ✅ On `main` | FIFO allocation of confirmed orders that honors each client-owner's allocation policy |
| Receiving | 🟡 In progress | Receiving unplanned (no-ASN) deliveries, with inventory and exception outcomes |
| Fulfillment, Shipping, Tracking, Billing | ⬜ Planned | Not started |

**Next up:** receiving exceptions, holds and returns; inbound stock-transfer advice (EDI
943); and quote validity windows.

## Running locally

Needs the .NET 10 SDK, Docker, and the EF Core tools (`dotnet tool install --global dotnet-ef`).

**1. Start Postgres**

```bash
docker compose up -d
```

This also starts the Aspire dashboard at <http://localhost:18888>, where the API's traces and
logs show up.

**2. Create the schemas.** Each module owns its own schema and migrations, so there is one
command per module:

```bash
dotnet ef database update --project src/modules/ordering/Throughline.Modules.Ordering --startup-project src/api/Throughline.Api --context OrdersDbContext
dotnet ef database update --project src/modules/inventory/Throughline.Modules.Inventory --startup-project src/api/Throughline.Api --context InventoryDbContext
dotnet ef database update --project src/modules/receiving/Throughline.Modules.Receiving --startup-project src/api/Throughline.Api --context ReceivingDbContext
```

**3. Load the reference data:** three client-owners with their SKUs, plus locations, carriers
and hold reasons. The scripts are safe to re-run.

```bash
for f in db/seed/0*.sql; do docker exec -i throughline-pg psql -U postgres -d throughline -v ON_ERROR_STOP=1 < "$f"; done
```

**4. Run the API**

```bash
dotnet run --project src/api/Throughline.Api --launch-profile http
```

It listens on <http://localhost:5026>. `/health`, `/alive` and `/openapi/v1.json` answer
without a token.

**5. Create a token.** Every other endpoint needs a bearer token and an `owner_id` header. The
token must hold the role for the endpoint (`OrderIntakeIntegrator`, `Inventory` or `Receiving`)
and `Owner.{id}` for the owner named in the header:

```bash
dotnet user-jwts create --project src/api/Throughline.Api --role OrderIntakeIntegrator --role Owner.1
```

The signing key is stored in your user profile, not in the repo, so each clone creates its own
tokens. The role model is described in
[ADR-0003](docs/decisions/ADR-0003-authorization-functional-and-owner-roles.md).

**6. Send requests.** [testing/bruno](testing/bruno) is a [Bruno](https://www.usebruno.com)
collection with a request for every endpoint. Open that folder as a collection, select the
`Local` environment, and paste the token into its `authToken` secret. The collection sends the
token and the `owner_id` header on every request.

### Tests

```bash
dotnet test
```

The integration tests start their own Postgres through Testcontainers, so Docker has to be
running. `dotnet test --filter "Category=Unit"` runs the unit tests alone.

# Modelling

I'm not a purist. If an approach or framework isn't immediately appropriate for a scenario, I most likely will find one that's better suited. Consistency is crucial for making code navigable *within* a bounded context, but forcing one pattern across every context can make the code less readable and understandable.

I don't adhere strictly to Clean, layered, or Vertical Slice Architecture. Each use case is a handler in the application layer, and the business logic behind it is written to be as expressive as possible. Beyond that, I abstract only as much as is needed to clearly separate responsibilities and make the code testable. Expect the design of each bounded context to differ, and to evolve as stories are built; I'm deliberately borrowing more from Vertical Slice Architecture as the project grows.