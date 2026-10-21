# Dead Letter Queue Processing

Wolverine's dead-letter expiration is turned off. Each dead letter should be explicitly processed.

## DLQ Location & Logic

Wolverine failed messages in the **`wolverine` schema of the `throughline` database** When a message is dead-lettered depends on the exception the handler throws (`Program.cs`):

| Exception | Retries before dead-lettering |
|---|---|
| `UnrecoverableMessageException` (the data can never succeed) | None |
| `MissingDependencyException` (reference data not found) | Retries after 1, 5 and 15 minutes, so about 21 minutes after the first attempt |
| Anything else | Wolverine's default error handling |



## 1. Alerting

### How it works

Each time Wolverine dead-letters a message, it logs one Error with the text **"Envelope … was moved to the error queue"** (event ID 108). The alert matches that text rather than every Error. Failed attempts that are going to be retried also log at Error, so alerting on Error level would count retries as well.

The path is: the API exports logs over OTLP → the `cae-throughline` environment's OpenTelemetry agent → Application Insights → a log search alert → an action group that emails you.

The alert uses logs rather than metrics. Wolverine does publish a `wolverine-dead-letter-queue` counter, but the Container Apps agent's Application Insights destination accepts only logs and traces, and the API doesn't register Wolverine's meter.



## 2. Viewing

The database is the source of truth. Run these queries in DataGrip against the `throughline` database on `psql-throughline`.

**1. What's dead-lettered, grouped.** Start here: work through a group, not one row at a time.

```sql
select message_type, exception_type, count(*), min(sent_at), max(sent_at)
from wolverine.wolverine_dead_letters
group by 1, 2;
```

**2. Each dead letter, with its owner, order ID and trace ID.** This works for both current message types, because both events start with `ownerId`. `order_id` is blank for `AllocatablePallets`, whose message has `receiptId` instead (`message ->> 'receiptId'`). *Tested against a copy of one stored row; not yet run on Azure.*

```sql
select id, sent_at, message_type, exception_type, exception_message,
       msg ->> 'ownerId' as owner_id,
       msg ->> 'orderId' as order_id,
       substring(encode(body, 'escape') from '00-([0-9a-f]{32})-[0-9a-f]{16}-0[01]') as trace_id,
       msg
from wolverine.wolverine_dead_letters,
     lateral (select convert_from(
                       substring(body from position(convert_to('{"ownerId"', 'UTF8') in body)),
                       'UTF8')::jsonb as msg) m
order by sent_at desc;
```

Use this query for unknown-SKU dead letters in particular: their `exception_message` lists only the SKUs, not the order.

**3. The dead letter for a given order.**

```sql
select id, exception_type, exception_message, sent_at
from wolverine.wolverine_dead_letters
where position(convert_to('<order-id>', 'UTF8') in body) > 0;
```

**4. Messages still being retried, so not dead-lettered yet.** An order with an unknown SKU sits here for about 21 minutes first.

```sql
select message_type, status, attempts, execution_time
from wolverine.wolverine_incoming_envelopes
where status = 'Scheduled'
order by execution_time;
```

**In Application Insights**, the trace ID from query 2 finds the request that created the order, and everything logged under it. This depends on setup step 1 working.

```kusto
union requests, traces, exceptions
| where operation_Id == "<trace-id>"
| order by timestamp asc
```

The alert query from setup step 1 also shows dead letters over time, without opening the database. Save it in **Logs** as `Dead letters`.

## 3. Triage

For each group from query 1:

1. **Find the kind** from the message type and exception type in the table below.
2. **Read the reason.** For bad data it's in `exception_message`. For anything else, it's the `exception-stack` header in `body`, or the exception in Application Insights under the trace ID.
3. **Decide whether to replay, have the data resent, or discard**, using the table.

Replaying sends exactly the same message again. It can succeed only if something on Throughline's side has changed: the missing reference data now exists, a bug in Inventory is fixed, or the database is back up. Fixing the sender doesn't change a message that's already stored.

| Message | Exception | What it means | Decision |
|---|---|---|---|
| `OrderConfirmed` | `UnrecoverableMessageException` | Ordering accepted the order (201), but it has data Inventory can never allocate. Today that's a quantity of 0 or less, and `exception_message` says which. | **Don't replay.** The client has to send a corrected order. Ordering returns 409 for different contents under the same reference, so the corrected order needs a **new reference number**. The original order stays in `orders.orders` with no allocation; there's no way to cancel it. Then discard the dead letter. |
| `OrderConfirmed` | `MissingDependencyException` | A SKU on the order isn't in Inventory's SKU list for that owner, and it still wasn't there after about 21 minutes of retries. `exception_message` lists the SKUs. | Decide whether the SKU is real for that owner. **If it is** (the SKU list is behind), add the SKU, then replay. SKUs are only added through `db/seed/05_skus.sql`; there's no endpoint. **If it isn't** (a wrong code, like the simulator's `HFO-UNKNOWN-*`), handle it like the row above: a new order under a new reference, then discard. |
| `AllocatablePallets` | `UnrecoverableMessageException` | Receiving published pallets that Inventory rejects; `exception_message` lists each LPN and why. Nothing from the receipt was saved. | Replay only if Inventory's rule was wrong and has been fixed. If the receipt data is wrong, it has to be received again correctly. Throughline has no defined way to correct a receipt yet. |
| `AllocatablePallets` | `MissingDependencyException` | A pallet's SKU isn't in the owner's SKU list. | Same as unknown SKUs on orders: add the SKU and replay if it's real. Otherwise the receipt has to be corrected, which has no defined path yet. |
| Any | Anything else | Not a business rule: an infrastructure failure (the database unavailable, a timeout) or a bug. | **Infrastructure:** replay once it's healthy. **Bug:** fix it, deploy, then replay. |

The dead-letter table holds every owner's messages. Only 3PL operations staff should have access to it, through the database login.
