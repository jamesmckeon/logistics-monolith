# Code Conventions

Conventions for **C# production code**, **schema changes** and **web app screens** in this repo.
These are binding when Claude writes, edits, reviews, proposes or designs code or screens. Test
conventions are separate, in [testing.md](testing.md).

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
- **One change per changeset, with no `--rollback`.** Nothing rolls a changeset back: prod is
  fixed forward. Older changesets keep their `--rollback` lines, because applied changesets are
  never edited.
- **Never edit an applied changeset.** Liquibase checksums it; a fix is a new changeset.
- **Indexes on existing tables** use `CREATE INDEX CONCURRENTLY IF NOT EXISTS`, alone in a
  changeset with `runInTransaction:false`.
- **Prod is fixed forward, never rolled back.** A change that breaks prod is reversed or corrected
  by a new changeset through the Validate Merge Action and QA. Rolling back a new column or table
  would drop the receipts, orders or allocations every owner recorded in it. For a risky change,
  write and test the reversal before it ships. See ADR-0004.
- **A Wolverine upgrade ships with its schema change**, as a changeset in the same pull request.
  Wolverine doesn't change its tables itself.

```sql
--liquibase formatted sql

--changeset jamesmckeon:add-orders-priority-column
ALTER TABLE orders.orders ADD COLUMN priority integer;
```

---

## Web app screens

How every screen in the web app (`src/web`) behaves, so each screen's design doesn't decide it
again.

### Business rules live in the API

- **The API owns the rules.** A screen doesn't repeat them: it sends what the user entered and
  shows the API's reasons when it's refused. One place holds each rule, so a screen can't
  disagree with the API.
- **A screen only shapes input.** It marks required fields and can limit what a field accepts,
  such as whole numbers in a quantity box, so the API gets a value it can judge.
- **Refusal reasons are the API's words.** Each reason goes under the field it names; a reason
  that names no field goes at the top of the form. Every reason is shown together (ADR-0001), and
  nothing the user entered is cleared.

### Not signed in, or no access

These are app-wide pages, not designed per screen:

- **401 (sign-in expired or missing):** a page saying "Your sign-in has expired. Sign in again to
  continue." with a **Sign in** button.
- **403 (no access):** a page saying "You don't have access to this. Ask your administrator if
  you need it."

A screen that knows before calling the API that the user has no access, for example because a
lookup came back empty, says so in its own words instead.

### When Throughline doesn't answer as expected

This covers anything that isn't an answer or a refusal: no connection, a timeout, or a server
error (5xx).

- **One message for every unexpected failure.** A screen doesn't explain technical causes.
- **Where it appears depends on what failed:**
  - **An action**, such as adding a SKU: the user stays on the screen, the message sits at the top
    of the form until their next try, and nothing they entered is cleared.
  - **Something the screen needs before it can be used**, such as a list to choose from: a
    message with a **Try again** button takes the place of the form.
  - **The screen failing while it draws:** the app's "Something went wrong" page, with
    **Reload**.
- **Actions that change data are never retried automatically.** The user retries by pressing the
  button again. Reads may retry quietly a few times before showing the message.
- **Every action is safe to retry.** After a timeout the user can't tell whether the action
  happened, so the API makes repeating it harmless: a repeat is refused as a duplicate or does
  nothing more. That's what lets the message say "try again".
- **A reference the user can quote to support.** When the server answered with an error, the
  message shows its trace ID as "Reference: …", so support can find it in the logs. When the
  request never reached the server, there's no reference to show.

Wording for an action: "Throughline didn't respond, so the SKU may not have been added. Your
entries are kept; try again." plus "Reference: …" when there is one.

---

## Quick checklist

- [ ] Domain dates are `AppDateTime` or `NonFutureDateTime`
- [ ] The current time comes from `AppDateTime.Now`, read in application code
- [ ] Integration events carry `DateTimeOffset`; handlers wrap it in `AppDateTime`
- [ ] Schema changes are expand changesets, with no `--rollback`; a contraction has its own issue
- [ ] A fault in prod's schema is fixed by a new changeset, never by rolling prod back
- [ ] Applied changesets are never edited; a fix is a new changeset
- [ ] Screens don't repeat business rules; they show the API's reasons under each field
- [ ] 401 and 403 go to the app-wide pages, not per-screen messages
- [ ] Unexpected failures show one message, keep what was entered, and show a reference when there is one
- [ ] Actions that change data are never retried automatically, and the API makes them safe to retry
