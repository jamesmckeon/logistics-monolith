# Code Conventions

Conventions for **C# production code** and **schema changes** in this repo. These are binding when
Claude writes, edits, reviews, or proposes code. Test conventions are separate, in
[testing.md](testing.md).

---

## Dates and times

- **Domain dates are `AppDateTime`** (`Throughline.Common.Models`), always at UTC +0. Use
  **`NonFutureDateTime`**, its subclass, when a date can't be in the future, such as when something
  was received or created.
- **The current time is `AppDateTime.Now`.** Don't call `DateTimeOffset.UtcNow`, `DateTime.UtcNow`
  or `DateTime.Now`, and don't inject `TimeProvider`, in module code. Application code reads the
  clock and passes the value into the domain; domain methods take the date as a parameter.
- **Integration event contracts carry `DateTimeOffset`**, not `AppDateTime`, so contracts stay
  plain data. The consuming handler wraps the value: `new AppDateTime(message.ReceivedOn)`.
- **Persistence maps `AppDateTime` to `timestamptz`** through a value converter
  (`AppDateTimeValueConverter`, and `NonFutureDateTimeValueConverter` where needed). Persistence
  models that aren't domain classes, such as Ordering's `OrderRecord`, may hold `DateTimeOffset`.

```csharp
// Good — application code reads the clock; the domain takes the date
order.StartAllocating(AppDateTime.Now);

// Avoid — a second source of "now", and a raw DateTimeOffset in the domain
order.StartAllocating(DateTimeOffset.UtcNow);
```

---

## Schema changes

Schema changes are Liquibase changesets, written in SQL, in `db/changelog/<module>/changes/`: one
file per change, named `yyyyMMddHHmm-description.sql`. EF Core maps and queries; it doesn't migrate.

- **Expand, then contract.** Every change leaves the schema working for the code running now.
  Drops and renames wait until no code on any environment uses the old structure; track each one
  as a tech-debt issue titled "Contraction: …" when the expand change ships.
- **One change per changeset, each with a `--rollback`.** CI applies, rolls back and re-applies
  every changeset.
- **Never edit an applied changeset.** Liquibase checksums it; a fix is a new changeset.
- **Indexes on existing tables** use `CREATE INDEX CONCURRENTLY IF NOT EXISTS`, alone in a
  changeset with `runInTransaction:false`.
- **Prod is fixed forward, never rolled back.** A change that breaks prod is reversed or corrected
  by a new changeset through the Build & Test Action and QA. Rolling back a new column or table
  would drop the receipts, orders or allocations every owner recorded in it. For a risky change,
  write and test the reversal before it ships. See ADR-0004.
- **A Wolverine upgrade ships with its schema change**, as a changeset in the same pull request.
  Wolverine doesn't change its tables itself.

```sql
--liquibase formatted sql

--changeset jamesmckeon:add-orders-priority-column
ALTER TABLE orders.orders ADD COLUMN priority integer;
--rollback ALTER TABLE orders.orders DROP COLUMN priority;
```

---

## Quick checklist

- [ ] Domain dates are `AppDateTime` or `NonFutureDateTime`
- [ ] The current time comes from `AppDateTime.Now`, read in application code
- [ ] Integration events carry `DateTimeOffset`; handlers wrap it in `AppDateTime`
- [ ] Schema changes are expand changesets, each with a `--rollback`; a contraction has its own issue
- [ ] A fault in prod's schema is fixed by a new changeset, never by rolling prod back
- [ ] Applied changesets are never edited; a fix is a new changeset
