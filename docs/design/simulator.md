# Throughline traffic simulator

Backlog: T-039 (Customer OMS Traffic Simulator). Status: design agreed 2026-10-18; iteration 1 code built on `feat/oms-simulator` (build steps 1–9), not yet run against Azure or deployed.

## Purpose

Throughline should receive realistic traffic around the clock without anyone sending requests by hand. The simulator plays the people and systems that call Throughline in production: client-owners' order management systems, and the 3PL's own warehouse staff. That way the deployed system is always exercised, and its telemetry, failure handling and data reflect real use rather than demos.

The simulator is test tooling, not a Throughline capability. It is a design document rather than a story, and it is not load testing: NBomber covers that (T-038), with sped-up time and short runs. The simulator runs in real time, all day and all week.

## Decisions

| # | Decision | Why |
|---|---|---|
| 1 | **Same repo, own project:** `tools/Throughline.Simulator` in `Throughline.sln`, with its own test project. Its tests run in the existing build-and-test workflow, but it **deploys through its own workflow**, never through Throughline's deployment pipeline. | A contract change and the simulator change that follows it land in one PR. The simulator plays an outside party, so its releases are independent of Throughline's: a Throughline deploy never redeploys the simulator, and the reverse. |
| 2 | **External client only.** Everything it *sends* goes over HTTP with its own request models, and it references no Throughline code. The one exception is decision 11: it reads the SKU list from Throughline's database. | It must behave like a customer's system. Sharing Throughline's types would hide contract breaks. |
| 3 | **A worker service** (`BackgroundService`), running as a third container app (`ca-throughline-simulator`) in `cae-throughline`. No ingress, 1 replica. | It runs continuously and keeps state between actions, which a per-run timer function can't. It uses the same image and revision flow as the API. |
| 4 | **Real time only.** Each actor waits for its next action according to its profile. | The point is watching the system develop over days. Sped-up time is NBomber's job. |
| 5 | **One profile per customer**, in configuration. | Customers differ in volume, timing, order shape and how badly they behave. |
| 6 | **Warehouse users are 3PL employees, shared across customers.** Each holds a functional role plus the `Owner.{id}` roles for the customers they work for. | This is how a shared 3PL building works, and it matches ADR-0003. |
| 7 | **Shifts belong to the facility, not to customers.** What differs per customer is order cutoffs and SLAs, which decide when allocators release that customer's orders. | Same as 6. |
| 8 | **The 3PL's staff allocate orders,** not the customer's users. | Allocation and wave release are warehouse work (ADR-0003). |
| 9 | **People's tokens come from a separate test tenant.** Test users there sign in with the password grant (ROPC), so the simulator can run unattended; security defaults are off in that tenant only. `throughline-api` stays registered in the main tenant and becomes multi-tenant; the API trusts both tenants' issuers. Applications (customer OMSs) stay in the main tenant with client credentials. | The password grant can't do MFA. Turning off security defaults in the main tenant would remove MFA for everyone, and Conditional Access needs a paid licence. The exact multi-tenant setup is verified in iteration 2. |
| 10 | **No shared trace with Throughline.** The simulator suppresses outgoing `traceparent` headers. Requests are linked across the two sides by business keys (PO number, reference number, the order ID Throughline returns). | An external OMS doesn't share trace context with the WMS. Investigating a customer's report should work the way it would in production. |
| 11 | **SKUs are read from Throughline's `inventory.skus`**, at startup and once a day, as a login (`throughline_simulator`) whose group role (`sku_catalog_reader`) can only `SELECT` from that table. Popularity is a stable pseudo-random order (`ORDER BY md5(code)`). Reference data used by warehouse users (hold reasons, locations, carriers) will come from configuration. | Loading the catalog is test-data setup, not simulated traffic, so how the simulator learns the SKUs doesn't affect realism, and reading the table keeps it always correct. The cost is coupling to Inventory's table layout, accepted for test tooling. Throughline has no item-master endpoint; a real one would be a Throughline story (item onboarding, client portal), not built for the simulator. |
| 12 | **Errors and resends are deliberate and configured per customer:** a rate for each kind of bad request, and for identical and changed resends. | Exercises validation, idempotent intake and dead-lettering continuously, and gives you something real to alert on. |
| 13 | **Retries like a well-behaved client:** retry only on timeouts, 5xx and 429, with exponential backoff, jitter and a maximum number of attempts. A retry resends the same order (same PO and reference). Never retry a 400. On a 401, fetch a new token once. An optional "badly behaved partner" setting retries aggressively with no backoff. | Real clients retry. Retrying the same order is how duplicates reach Throughline. |
| 14 | **Telemetry through OpenTelemetry to Application Insights:** one structured log per request (customer, actor, endpoint, PO, reference, status, latency, Throughline's order ID) and metrics for requests sent by outcome and latency. Request bodies are not logged. Tokens are never logged. | You check in during the day; the Logs view and a workbook are where you look. |
| 15 | **A stop switch:** `Simulator:Enabled`, plus `Enabled` per customer. Changing it is a new revision (about a minute). The container app's **Stop** button is the hard stop. | Traffic can be stopped without a code change. |
| 16 | **A ledger (iteration 2):** a separate `simulator` database on the same Postgres server, holding in-flight work (orders awaiting allocation, expected deliveries) and a row per request with its outcome. | Survives restarts, and lets you reconcile what was sent with what Throughline holds. A gap is a real finding, such as a lost message. |

## Budget

Total budget is **$150/month**. Raise the subscription's budget alert to $150, with alerts at 50%, 80% and 100%.

| Item | Rough monthly cost | Notes |
|---|---|---|
| Container Apps (API + simulator, 1 replica each) | $20–40 | Consumption plan |
| Postgres Flexible Server, B1ms | $15–20 | Can move to B2s or B2ms (2 vCores, about $30–60) if metrics show it's needed. Compute can be changed at any time; storage can't shrink. |
| Container registry, Basic | $5 | |
| Log Analytics / Application Insights | $0–20 | Free up to 5 GB/month, then about $2.30/GB. **Set a daily cap** on the workspace so all-day traffic can't run up the bill. |

Prices are approximate; check the portal's estimates. Start on B1ms and scale up when the database's CPU metrics show it running out of burst credits. Watching that happen is part of the exercise.

## Customer profile

Every value below is a **starting proposal to confirm together** before iteration 1 is built.

| Setting | Harrow & Finch (owner 1) proposal |
|---|---|
| Time zone | America/Chicago |
| Orders per day (average day) | 600 |
| Hourly curve | Direct-to-consumer: low overnight, rising through the day, peak 19:00–22:00 local |
| Weekly curve | Monday highest, Saturday lowest |
| Lines per order | p50 2, p95 6 |
| Quantity per line | Mostly 1, p95 4 |
| SKU popularity | The 10 named SKUs plus a long tail over `HFO-000001`–`HFO-006000`; about 20% of SKUs take 80% of lines |
| Sending style | Each order sent as it arrives (an option to batch every N minutes comes later) |
| Identical resend rate | 2% |
| Changed resend rate (same reference, different lines) | 0.5% |
| Unknown SKU | 0.5% |
| Missing required field | 0.2% |
| Invalid quantity (0 or negative) | 0.2% |
| Retries | Up to 3 attempts, backoff from 2 s with jitter, 10 s per attempt |

Orders arrive as a Poisson process whose rate follows the hourly and weekly curves, so gaps are random around the current average rate. That gives realistic bursts and lulls rather than a fixed beat.

## Expected outcome of each request type

Traced through `CreateOrderHandler` and Inventory's `OrderConfirmedHandler` on 2026-10-18. Throughline matches resends on owner plus reference number.

| Request | Response | What happens next |
|---|---|---|
| New, valid order | **201 Created** with the order ID | `OrderConfirmed` goes through the outbox. Inventory records the order for allocation (not allocated until stock and allocation exist). |
| Identical resend | **200 OK** with the same order ID | Nothing; no second order. |
| Changed resend (same reference, different content) | **409 Conflict** | Nothing. |
| Missing required field | **400 Bad Request** | Nothing. |
| Unknown SKU | **201 Created**: intake doesn't check SKUs | Inventory doesn't know the SKU, so it retries after 1, 5 and 15 minutes, then dead-letters the message. |
| Quantity 0 or negative | **201 Created**: intake doesn't check quantities | Inventory rejects the lines as invalid and dead-letters the message immediately. |
| Blank SKU on a line | **500**: the `SkuCode` constructor throws | Not injected. A retrying client would resend it three times. See findings. |

## Iteration 1: Harrow & Finch's OMS, EDI 940 intake only

**Scope:** one actor, the Harrow & Finch OMS, calling `POST /orders` (and `GET /orders/{id}` to confirm an order it created). It uses the existing `oms-harrow-finch` app registration (client credentials, main tenant). No warehouse users, no test tenant, no ledger.

Orders will be confirmed but not allocated until receiving and allocation exist (iterations 2 and 3). Unallocated orders accumulating is expected until then.

**Build steps:**
1. Branch `feat/oms-simulator` from `main`.
2. Confirm the profile values above.
3. Scaffold `tools/Throughline.Simulator` (Worker SDK, .NET 10) and `tools/Throughline.Simulator.Tests` (NUnit, per `docs/testing.md`); add both to the solution; add packages through `Directory.Packages.props` (`Azure.Identity`, `Microsoft.Extensions.Http.Resilience`, the OpenTelemetry packages already in use, `Microsoft.Extensions.TimeProvider.Testing` for tests).
4. **Profile and catalog:** typed options with validation at startup, so a bad profile stops the simulator with a clear message. Load the catalog from `inventory.skus` with the read-only login; fail at startup if the customer has too few SKUs, and keep the previous catalog if a daily reload fails.
5. **Arrival schedule:** works out the current rate from the customer's local time, hour and weekday (daylight saving included) and draws the next gap. Uses `TimeProvider` and a seedable `Random`, so tests are deterministic.
6. **Order generator:** builds the 940 from the catalog and profile, and applies the error and resend rates. Before writing it, read the Ordering code and tests to record the **expected outcome of each error type** (for example, whether an unknown SKU is rejected at intake or accepted and later dead-lettered by Inventory). That list becomes the "expected" side of the logs.
7. **OMS client:** a typed `HttpClient`; tokens from `ClientSecretCredential` (cached and renewed automatically); the standard resilience handler restricted to the retry rules in decision 13; outgoing trace headers suppressed; one structured log per request.
8. **Worker:** one loop per enabled customer, honoring the stop switch.
9. **Tests:** schedule rates by local hour and weekday, including a daylight-saving change; generated orders always use catalog SKUs and stay within the line and quantity limits; error and resend rates come out within tolerance over many draws; only the right status codes are retried.
10. **Run locally** from Rider against the Azure API for an hour. Check the orders in DataGrip and the simulator's logs.
11. **Deploy** (by hand for iteration 1; a separate `deploy-simulator.yaml` workflow, triggered only by changes under `tools/Throughline.Simulator/`, comes later): publish the image, create `ca-throughline-simulator` (no ingress, 0.25 vCPU / 0.5 Gi, min and max 1), with the OMS client secret and the catalog connection string as Container Apps secrets, and the API URL, tenant ID and client IDs as environment variables.
12. **Telemetry:** connect the environment's OpenTelemetry agent to Application Insights, set the workspace's daily cap, and raise the budget alert to $150.
13. Leave it running and check in through the day.

**Done when:** orders arrive in Throughline at the profile's rate around the clock; bad requests and resends appear at their configured rates with the expected outcomes; and any order can be traced from the simulator's log to Throughline's logs by its PO or reference number.

## Later iterations

| Iteration | Adds |
|---|---|
| 2 | Test tenant and multi-tenant API registration; receiving operators on facility shifts posting unplanned deliveries for the SKUs being consumed; the ledger database. |
| 3 | Allocators releasing orders ahead of each customer's cutoff. |
| 4 | Customers 2 and 3 (Brightwell Nutrition, Kestrel Industrial Supply) with their own profiles and app registrations. |
| 5 | A reconciliation report: what was sent against what Throughline holds. |

## Findings about Throughline

Building the simulator exposes things Throughline lacks. Each is a candidate story or backlog item, not part of the simulator.

- **Orders have no received timestamp.** `orders.orders` records nothing about when an order arrived, so Throughline can't answer "orders received per hour" or apply same-day-ship cutoffs. The simulator's own logs are the only record of arrival times today.
- **No allocation worklist.** Nothing returns "orders waiting for allocation", so allocators can only act on order IDs the simulator remembers.
- **No reference-data reads.** Warehouse users would choose hold reasons, locations and carriers in the WMS, but no endpoint returns them.
- **The API trusts trace context from outside callers.** ASP.NET Core continues an incoming `traceparent`, so an external caller can choose Throughline's trace IDs and sampling. At an external boundary the usual practice is to start a new trace and record the caller's as a link.
- **No correlation ID in successful responses.** Errors include a trace ID (ProblemDetails), but a 201 doesn't, so a customer has nothing of Throughline's to quote except the order ID.
- **Order intake accepts quantities of 0 or less.** The 940 is accepted (201) and only Inventory rejects it, by dead-lettering `OrderConfirmed`. The customer is told the order was accepted when it can never ship.
- **Order intake accepts SKUs the owner doesn't have.** Same pattern: 201 at intake, then dead-lettered by Inventory after about 21 minutes of retries. Whether intake should check SKUs against the item master is a real design question (it would couple Ordering to Inventory's data), but today the customer gets no rejection.
- **A blank SKU on a line returns 500.** `CreateOrderCommand.Validate` doesn't check line SKUs, and the `SkuCode` constructor throws. Bad input should be a 400; a 500 also invites well-behaved clients to retry.
- **Refusals aren't logged.** The reasons for 401s and 403s are logged by ASP.NET Core at Information level, which `appsettings.json` suppresses (`Microsoft.AspNetCore` is set to Warning).
