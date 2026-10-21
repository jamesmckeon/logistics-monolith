# Code Conventions

Conventions for **C# production code** in this repo. These are binding when Claude writes, edits,
reviews, or proposes code. Test conventions are separate, in [testing.md](testing.md).

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

## Quick checklist

- [ ] Domain dates are `AppDateTime` or `NonFutureDateTime`
- [ ] The current time comes from `AppDateTime.Now`, read in application code
- [ ] Integration events carry `DateTimeOffset`; handlers wrap it in `AppDateTime`
