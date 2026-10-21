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
| 16 | **A ledger:** a separate `simulator` database on the same Postgres server, owned by its own login. It holds each buying customer's sales history, purchase orders, and the trailers on their way. Its schema is created on first use. | Survives restarts, and is the customer's and supplier's side of the story, which Throughline doesn't hold. Later it also supports reconciling what was sent with what Throughline holds. |
| 17 | **Inbound stock is the customer's decision.** Each buying customer has a buyer that places an opening order once, then every week orders last week's sales of each SKU plus a safety margin, in whole cases. The supplier ships single-SKU pallets on trailers that arrive after a lead time, during dock hours; some arrive late. | In a 3PL the client owns the stock and decides when to buy it; the warehouse receives what arrives. "Replace what sold" needs no on-hand figures, which Throughline can't report yet (no 846-style inquiry). Stock drifts and sometimes runs short, as real stock does. |
| 18 | **Receivers are 3PL staff working dock shifts.** While the dock is staffed, each takes the trailer that has waited longest and posts it as one unplanned receipt, signed in as themselves. The trailer's id is the receipt id, so a resend is the same delivery; the operator id is the user's Entra object id. | Matches decisions 6 and 7. Receipts stay unplanned until Throughline supports advance notice (943/856). |

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

## Test tenant setup (iteration 2)

Simulated warehouse staff hold `Receiving`, `Inventory` and `Owner.{id}`, which ADR-0003 reserves for people, so they need real user accounts. They sign in with the password grant (ROPC), which can't do MFA, so they live in a **separate test tenant** with security defaults off; the main tenant keeps MFA (decision 9). `throughline-api` stays registered in the main tenant and becomes multitenant, so the test tenant gets its own copy of it (a service principal) where roles are assigned to the test users.

Written 2026-10-19 (step 1 revised the same day after tenant creation was refused) from Microsoft Learn: [create a tenant](https://learn.microsoft.com/en-us/entra/fundamentals/create-new-tenant), [convert an app to multitenant](https://learn.microsoft.com/en-us/entra/identity-platform/howto-convert-app-to-be-multi-tenant), [ROPC](https://learn.microsoft.com/en-us/entra/identity-platform/v2-oauth-ropc), [app roles](https://learn.microsoft.com/en-us/entra/identity-platform/howto-add-app-roles-in-apps). Completed and verified end to end on 2026-10-19: a token for `receiver1` from the test tenant was accepted by the deployed API, and `POST /receiving/receipts` returned 200. Steps 3 and 4, which the docs didn't fully confirm, worked as written.

| Value | |
|---|---|
| Test tenant | `2ce2eb06-ef08-4a92-9bd5-7035a20fa695` (`jamesmckeon503gmail.onmicrosoft.com`), managed by a separate account |
| Public client `throughline-simulator-users` | `3ab9525a-48a6-4c34-9e38-3269482fc765` |
| Receivers | `receiver1`–`receiver3`, each with `Receiving` and `Owner.1` |
| Delegated scope on `throughline-api` | created as `access_as-user` (hyphen); harmless, since clients request `.default` and the API checks roles |
| `throughline-api` account types | Multiple Entra ID tenants, allowed tenants: the test tenant only |

**Cost:** $0. The tenant is Entra ID Free and holds no Azure resources.

**Values to record:** test tenant ID and domain, the public client's ID, and each test user's password (keep the passwords out of the repo and out of chat).

### 1. Create the test tenant (a second, free account)
Your main tenant is on the free tier, and Microsoft only lets tenants with a paid Entra license create additional tenants (`Base-tenant … does not have a paid license`). Instead, the test tenant is the directory that comes with a **separate free Azure account**:

1. Use a **different email address** from your main account; a new Outlook.com address works. In a separate browser profile (keeping the two accounts' sign-ins apart), go to the Azure free account sign-up and create the account. Phone and card verification are required, but nothing is charged unless you create resources there, and you won't.
2. Sign-up creates a tenant called **Default Directory**. In it: **Microsoft Entra ID** → **Overview**: copy the **Tenant ID** and the **Primary domain** (something like `<name>.onmicrosoft.com`; that's `<test-domain>` below). Optionally rename the directory to `Throughline Test` under **Properties**.
3. The new account is the tenant's Global Administrator; use it for every "test tenant" step below. Create no Azure resources there. If the free credit later expires or the subscription is disabled, the directory and its users remain.

### 2. Turn off security defaults in the test tenant only
In the **test tenant**: Entra ID → **Overview** → **Properties** → **Manage security defaults** → **Disabled** → give a reason (e.g. "Simulated users for automated testing") → **Save**. Double-check the directory name at the top of the portal first; this must never be done in the main tenant.

### 3. Prepare `throughline-api` (main tenant)
1. **Expose an API** → **Add a scope**: scope name `access_as_user`, who can consent **Admins only**, admin consent display name `Access Throughline as the signed-in user`, description of your choice → **Add scope**. User tokens are delegated tokens, so the API needs a delegated scope for clients to request; the roles still decide what each user may do.
2. **Authentication** → **Supported account types** → **Accounts in any organizational directory (Any Microsoft Entra ID tenant - Multitenant)** → **Save**.
   - In the current portal: **Authentication (Preview)** → **Supported accounts** tab → **Multiple Entra ID tenants** → **Allow only certain tenants** → add the test tenant. `api://<api-client-id>` was accepted as the Application ID URI.
3. **Authentication** → **Add a platform** → **Web** → redirect URI `https://jwt.ms` → **Configure**. Step 4's consent page needs somewhere to send you back to.

### 4. Add `throughline-api` to the test tenant
In a private browser window, open (replace the tenant ID):

```
https://login.microsoftonline.com/<test-tenant-id>/adminconsent?client_id=9daefbfa-b583-4f14-90f1-563a807e3260&redirect_uri=https://jwt.ms
```

Sign in as the test tenant's administrator (it will ask that account to set up MFA first, which is expected) and **Accept**. You land on jwt.ms with `admin_consent=True&tenant=<test-tenant-id>` in the URL, and **Enterprise applications** in the test tenant lists `throughline-api`.

### 5. Create the test users (test tenant)
**Users** → **New user** → **Create new user**, for `receiver1`, `receiver2` and `receiver3` (allocators come with issue #35):
- User principal name `receiver1@<test-domain>`, display name `Receiver 1 (simulated)`
- Set a password and record it privately.

Then sign in once as each user in a private window at `https://myaccount.microsoft.com`. New accounts usually must change their password at first sign-in, and the password grant can't do that, so its sign-ins fail until it's done.

### 6. Assign roles (test tenant)
**Enterprise applications** → `throughline-api` → **Users and groups** → **Add user/group**: select a receiver, **Select a role** → `Receiving` → **Assign**. Repeat with `Owner.1`. One role per assignment, so two per receiver. (The free tier assigns roles to users, not groups.)

### 7. Register the simulator's sign-in client (test tenant)
1. **App registrations** → **New registration** → name `throughline-simulator-users`, single tenant, no redirect URI → **Register**. Copy its **Application (client) ID**.
2. **Authentication** → **Advanced settings** → **Allow public client flows** → **Yes** → **Save**. The password grant requires a public client, so there's no client secret.
3. **API permissions** → **Add a permission** → **APIs my organization uses** → `throughline-api` → **Delegated permissions** → `access_as_user` → **Add permissions** → **Grant admin consent for Throughline Test** → **Yes**.

### 8. Get a token as a receiver (terminal)
```bash
TENANT=<test-tenant-id>; CLIENT=<public-client-id>; read -s PW
```
```bash
curl -s -X POST "https://login.microsoftonline.com/$TENANT/oauth2/v2.0/token" -d grant_type=password -d client_id=$CLIENT --data-urlencode "username=receiver1@<test-domain>" --data-urlencode "password=$PW" -d "scope=api://9daefbfa-b583-4f14-90f1-563a807e3260/.default" | jq -r .access_token
```
At [jwt.ms](https://jwt.ms) expect `iss` = `https://sts.windows.net/<test-tenant-id>/`, `aud` = `api://9daefbfa-b583-4f14-90f1-563a807e3260`, and `roles` = `Receiving`, `Owner.1`. If it prints `null`, remove `| jq …` to see the `AADSTS` error: `50126` is a wrong password, `50055`/`50056` a password that still needs changing (step 5), `50076` MFA still enforced (step 2).

### 9. Let the API trust the test tenant
On `ca-throughline-api`, create a new revision adding one environment variable:

| Name | Value |
|---|---|
| `Authentication__Schemes__Bearer__ValidIssuers__0` | `https://sts.windows.net/<test-tenant-id>/` |

It's accepted alongside the existing `ValidIssuer`; Entra signs both tenants' tokens with the same published keys. Local development gets the same issuer in `appsettings.Development.json` (part of the code change). Then a `POST /receiving/receipts` with the step 8 token should get past authorization (not 401 or 403).

**Accepted risk:** the API now trusts any role the test tenant's administrator assigns. That's acceptable while you're its only administrator; a production Throughline would trust only the 3PL's own tenant.

## Iteration 2: buying and receiving (Harrow & Finch)

Built on `feat/simulator-receiving-allocation`. Both are off until configured: `Buying:Enabled` is false in `appsettings.json`, and the facility has no receivers.

**How it runs**
- The **OMS** records each accepted good order (201, kind New) as a sale in the ledger. Deliberately bad orders aren't sales; Inventory dead-letters them.
- The **buyer** places an **opening order** the first time it runs: `OpeningWeeks` of expected demand for the popular SKUs, due over the first day or two. Slow movers are first stocked by a weekly order after they sell. Then, every `ReviewDay` at `ReviewTime` in the customer's time zone, it orders last week's sales plus `SafetyMargin`, rounded up to `CasePack`, packed up to `UnitsPerPallet` per single-SKU pallet and `PalletsPerTrailer` per trailer. Each trailer is due `LeadTimeDaysMin`–`LeadTimeDaysMax` days later at a random time while the dock is staffed, plus one to three days for the `LateShipmentRate` share.
- **Receivers** wait about `MinutesPerTrailer` (randomized ±50%) between trailers. While the dock is staffed, each claims the trailer that's waited longest and posts it to `POST /receiving/receipts` with the owner's `owner_id`. 200 marks the trailer received; 400 or 409 marks it rejected; 401, 403, 5xx or no response puts it back to try again. A claim not completed in 30 minutes can be taken again.
- Rough volume at 600 orders a day: an opening order of about 1,200 pallets (about 47 trailers), then about 50 trailers a week. Three receivers can handle several times that.

**Setup**
1. **Ledger login and database.** In DataGrip, as `throughlineadmin`, connected to the Azure server (any database):
   ```sql
   CREATE ROLE throughline_simulator_ledger WITH LOGIN PASSWORD '<password>';
   -- lets the admin create a database owned by that role
   GRANT throughline_simulator_ledger TO throughlineadmin;
   CREATE DATABASE simulator OWNER throughline_simulator_ledger;
   ```
   Then, still as `throughlineadmin` but connected to the new **`simulator`** database (Azure doesn't let a new database's owner create tables in `public` until granted):
   ```sql
   GRANT USAGE, CREATE ON SCHEMA public TO throughline_simulator_ledger;
   ```
2. **Settings** (user secrets locally; environment variables, with `__` for `:`, and Container Apps secrets in Azure):

   | Key | Value |
   |---|---|
   | `Simulator:LedgerConnectionString` (secret) | `Host=psql-throughline.postgres.database.azure.com;Port=5432;Database=simulator;Username=throughline_simulator_ledger;Password=<password>;Ssl Mode=Require;Maximum Pool Size=5` |
   | `Simulator:Customers:HarrowFinch:Buying:Enabled` | `true` |
   | `Simulator:Facility:UserTenantId` | `2ce2eb06-ef08-4a92-9bd5-7035a20fa695` |
   | `Simulator:Facility:UserClientId` | `3ab9525a-48a6-4c34-9e38-3269482fc765` |
   | `Simulator:Facility:Receivers:0:Username` | `receiver1@jamesmckeon503gmail.onmicrosoft.com` (and `:1:`, `:2:` for receivers 2 and 3) |
   | `Simulator:Facility:Receivers:0:Password` (secret) | that receiver's password |

   Also add `Maximum Pool Size=5` to the catalog connection string, so the simulator can never use more than a handful of the server's roughly 50 connections.
3. **Check:** the log shows an opening purchase order (SKUs, units, pallets, trailers, first due time), each receiver starting with its operator id, and, during dock hours, `Received trailer … as receipt …` lines.

**Daily check additions:** trailers received per day, pallets per receipt outcome (available, on hold, exceptions), and trailers rejected. Exceptions or rejections point at a mismatch between what the buyer sent and Throughline's master data.

## Iteration 3: allocation (Harrow & Finch)

Plan drafted 2026-10-20, to build on `feat/simulator-allocation`. Not started.

**What exists today.** `POST /inventory/orders/allocate` (role `Inventory`, `owner_id` header) allocates a list of order IDs in the order given, FIFO, under the owner's policy (Harrow & Finch is `Partial`). It returns a status per order (`fullyAllocated`, `partiallyAllocated`, `notAllocated`, `failed`) with shortages and errors. `GET /inventory/orders/{orderId}` shows one order's lines. Nothing lists the orders waiting for allocation, so the simulator would have to remember order IDs itself, which a real allocator couldn't.

**How it would run**
- **Allocators** are 3PL inventory-control staff (decisions 6–8), signed in from the test tenant with `Inventory` plus `Owner.{id}` for the customers they work for.
- While they're on shift, they release **waves** for each customer ahead of that customer's cutoff: fetch the owner's worklist, choose the orders for the wave, and post them as one allocation request.
- Orders short of stock stay on the worklist and go into a later wave, after receivers have put more stock away. That exercises "further allocation" and "repeated requests" from the allocation story continuously.

**Build steps**

*Throughline (issue #35), in the same PR (decision 1)*
1. **Worklist endpoint.** List the owner's orders that are confirmed or partially allocated, earliest acceptance into Throughline first, with each line's quantity still to allocate; a batch size with a server-set maximum, no pages; owner-scoped; `Inventory` plus the owner's role, otherwise 403. See the worklist decisions below.
2. **Carry the acceptance time into Inventory.** `inventory.order_allocations` holds only `last_updated`, and `OrderConfirmedIntegrationEvent` carries no order date (only `OccurredOnUtc` from `IntegrationEventBase`, which is when the event was raised). Add Ordering's acceptance time (`orders.created_date`) to the event and store it on `order_allocations`, with a migration. `created_date` is set by the database on insert, so check that the event is built after EF reads it back, or have Ordering set the time itself.
3. **Tests:** unit tests for the query's rules; an integration test only for what needs the running host (owner isolation and 403), per the integration-test rule.

*Test tenant*
4. Create `allocator1` and `allocator2` (display name `Allocator N (simulated)`), sign each in once to change the password, and assign `Inventory` and `Owner.1` as in setup steps 5 and 6.

*Simulator*
5. **Configuration:** `Facility:Allocators` (same shape as `Receivers`), an allocation shift pattern for the facility, and per customer an order cutoff and wave schedule. Values to confirm below. Validation at startup as for the rest of the profile.
6. **`InventoryClient`:** typed client for the worklist (one batch per wave) and `POST /inventory/orders/allocate`, with its own request and response models (decision 2), the `owner_id` header and the staff retry profile.
7. **Wave scheduling** as a pure, `TimeProvider`-driven class: given the facility shift, the customer's time zone, cutoff and wave schedule, when is the next wave due. Unit-tested like `DockCalendar`, including a daylight-saving change.
8. **`AllocatorWorker`:** one loop per allocator; at each due wave it fetches the worklist and posts it. Response handling, like the receivers': 200 logs each order's outcome; 400 means the simulator built a bad request (log an error, don't resend it); 401, 403, 5xx or no response leaves the orders for the next wave. Two allocators must not release the same customer's wave (see decisions below).
9. **Telemetry:** one structured log per wave (allocator, owner, wave time, orders sent, count per outcome, units short, latency), and metrics for orders allocated by outcome and wave latency.
10. **Tests** per `docs/testing.md`: wave times by local time and shift; the worker's handling of each status code.
11. **Run locally** against the Azure API with buying and receiving on, then **deploy** through `deploy-simulator.yaml` with the allocators' passwords as Container Apps secrets.

**Worklist decisions (2026-10-20)**

| Decision | Why |
|---|---|
| **Sorted by earliest acceptance into Throughline**: when Ordering accepted the 940 (`orders.created_date`), then order ID so equal times always come back in the same order. | A project policy, not a WMS convention. Reference WMS make the date configurable: Infor can sequence by Order Date or Requested Ship Date; Oracle selects wave orders by create-time windows to apply cutoffs. When Inventory recorded the order isn't used: it trails acceptance by the outbox delay (about 21 minutes for an order retried for an unknown SKU). Throughline's orders have no requested ship date or priority yet. |
| **The sort is display order, not allocation precedence.** | Selecting which orders enter a wave is separate from which competing order gets stock first. Every reference WMS lets an operator choose specific orders as well as select them by rule. `POST /inventory/orders/allocate` already allocates whatever orders it's given, in the order given, so the caller decides; priority between competing orders stays out of scope, as in the allocation story. |
| **Route: `GET /inventory/orders/awaiting-allocation`**, a named view rather than a status filter on an orders collection. | Which statuses mean "awaiting allocation" (today confirmed or partially allocated) is a business rule, so Inventory owns it; a status filter would put that rule in every client. "Awaiting allocation" rather than "allocatable", which reads as "has stock available". |
| **A batch size, not pages**: the caller says how many orders to return, up to a server-set maximum; the response may be truncated, and the caller allocates and fetches again for more. | The list changes as allocators work, so "page 2" has no stable meaning, and no allocator wants it: the next wave always takes the earliest orders, which are at the top of a fresh fetch. Finding a particular order is a lookup or a filter, not a page. The maximum keeps a peak-day backlog from coming back in one response. Two allocators taking the same order is handled by the allocate endpoint, not the worklist (`failed` with `ORDER_ALLOCATING` while the other is running, `fullyAllocated` with nothing new once it's done). |

**Decisions to make before building**

| Question | Options / proposal |
|---|---|
| Allocation shifts | Reuse the dock shifts, or a separate pattern for inventory control. |
| Harrow & Finch cutoff and waves | To propose together, like the iteration 1 profile. |
| Keeping allocators off each other's waves | Assign each customer to one allocator, or claim the wave in the ledger as trailers are claimed. |
| Wave size | Everything on the worklist, or a maximum per wave. |

**Done when:** during allocation shifts, Harrow & Finch's orders are released in waves ahead of its cutoff; fully allocated orders leave the worklist; short orders are picked up by later waves once stock arrives; and every wave can be found in the simulator's logs and matched to Throughline's by order ID.

## Later iterations

| Iteration | Adds |
|---|---|
| 2 | Test tenant and multitenant API registration (done); the ledger, the buyer and the receivers (built, see below). |
| 3 | Allocators releasing orders ahead of each customer's cutoff, once Throughline has an allocation worklist (issue #35). |
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
