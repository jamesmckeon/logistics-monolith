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

## API modules and endpoints

How a module and its HTTP endpoints are built, so a new module, or a new endpoint in an existing
one, doesn't decide it again. ADR-0001 (validation failures), ADR-0002 (module extractability) and
ADR-0003 (authorization) apply alongside these.

### Projects and folders

- **A module is `Throughline.Modules.{Module}`, tested by `Throughline.Modules.{Module}.Tests`.**
  It has a `Throughline.Modules.{Module}.Contracts` project only when it publishes integration
  events.
- **A module references the shared projects and other modules' `.Contracts` only**, never another
  module's main project. A `.Contracts` project references only `Throughline.Common` and has
  `Events/` and `Models/` folders.
- **`InternalsVisibleTo`** names the module's tests, `Throughline.Api.Tests` and
  `DynamicProxyGenAssembly2`.
- **Types are `sealed` and `internal`**, commands and models included, unless something outside the
  module needs them. One type per file.
- **Folders:**
  - `Application/{Action}/` holds `{Action}Command`, `{Action}Handler` and `{Action}Result`, with no
    subfolder.
  - `Application/Queries/` holds `{Get…}Query` classes; `Application/Models/` holds the
    `{Thing}Model` types they return.
  - `Auth/{Module}Policies.cs` holds the policy names.
  - `Domain/` holds the domain, including repository interfaces. A module with no domain of its
    own, such as Platform, has no `Domain/`.
  - `Infrastructure/Db/` holds the DbContext, with `Configurations/`, `Converters/` and `Models/`.
  - `Infrastructure/Common/` holds `InfrastructureSettings` and, where there is one, the unit of
    work.
  - `Infrastructure/Events/` holds the module's integration event handlers.
  - `Presentation/{Module}Extensions.cs` registers the module and maps every one of its endpoints.

### Domain and persistence

- **Repository interfaces live in `Domain/`**, unless there's a good reason not to.
- **The domain stays as pure as possible.** Domain classes are mapped by configurations in
  `Infrastructure/Db/Configurations/`. When mapping a domain class directly would push a
  persistence concern into it, such as a public setter, a constructor only EF needs or a property
  only the database needs, map a separate class in `Infrastructure/Db/Models/` and convert between
  the two.
- **The DbContext implements the module's repository interfaces**, and each interface is
  registered against the same scoped instance, so they all share one change tracker. Don't
  register a separate repository class per interface.
- **Repositories may save.** Add an `IUnitOfWork`, interface and implementation both in
  `Infrastructure/Common/`, only when a handler needs to control when changes are saved, such as
  saving more than once in a request.
- **Queries read straight from the DbContext.** A `{Get…}Query` projects rows into `{Thing}Model`
  types with `AsNoTracking`, without loading domain classes. Commands go through the domain and its
  repositories. A separate read store, projections, eventual consistency and event sourcing haven't
  been needed yet; add them when a story does.
- **Concurrency is handled case by case**, with a comment saying why the case needs it: an
  optimistic concurrency check, a row lock, a unique index or a single atomic statement.
- **A unique-key violation is caught by `SqlState` and a constraint-name constant**:
  `PostgresErrorCodes.UniqueViolation` and a constant such as
  `OrderRecordConfiguration.OwnerReferenceIndexName`, never a string written at the catch.
- **The schema name is `InfrastructureSettings.SchemaName`.** The module registers its context with
  `AddModuleDbContext<TContext>(configuration, InfrastructureSettings.SchemaName)`, and the context
  calls `HasDefaultSchema(InfrastructureSettings.SchemaName)` and `ApplyConfigurationsFromAssembly`.
  Its changesets go in the folder named after the schema, such as `db/changelog/orders/` for
  Ordering.
- **Value converters** for the module's value types live in `Infrastructure/Db/Converters/` and are
  registered in the DbContext's `ConfigureConventions`.

### Registering a module

- **`Presentation/{Module}Extensions.cs`** has `public const string {Module}Route`,
  `Add{Module}(IServiceCollection, IConfiguration)` and `Map{Module}(IEndpointRouteBuilder)`.
  `Program.cs` calls `Add{Module}` with the other modules' and `Map{Module}` after the app is built.
- **`Add{Module}` registers the DbContext, then handlers, queries and services, then policies.**
  Handlers and queries are registered as their own scoped classes, without an interface.
- **Settings are bound with `AddOptions<T>().Bind(…).Validate(…).ValidateOnStart()`**, so a missing
  or malformed setting stops the app at startup instead of failing the first request.
- **A module that handles integration events adds its assembly to Wolverine's discovery** in
  `Program.cs` (`opts.Discovery.IncludeAssembly(…)`). Wolverine finds handlers only in the
  assemblies it's given.

### Routes

- **One route group per module, at the module's name** (`/inventory`, `/receiving`), with
  `.WithTags("{Module}")`.
- **Resources are plural, kebab-case nouns:** `/platform/dead-letters`.
- **An action is a verb after its collection:** `POST /inventory/orders/allocate`. **A named list is
  a fixed segment:** `GET /inventory/orders/unreleased`.
- **Route ids carry a type constraint:** `{orderId:guid}`.
- **The owner is never in the path.** It comes from the `owner_id` header, through
  `RequestContext.OwnerId` (ADR-0003).

### Requests and responses

- **The request body binds straight to the `{Action}Command`.** There's no separate request class.
- **Endpoints return the Application types:** `{Action}Result` and `{Thing}Model`. A
  `{Action}Response` in `Presentation/` is added only when the HTTP shape has to differ from the
  Application type.
- **A list is wrapped in an object** (`{ "deadLetters": [ … ] }`), so paging can be added later
  without changing the response's shape.
- **JSON uses the framework defaults**: camelCase property names. Dates are `DateTimeOffset`.
  Responses carry `OwnerId`.
- **Status, outcome and error-code strings are UPPER_SNAKE:** `FULLY_ALLOCATED`, `ORDER_NOT_FOUND`.
  An enum's `ToString()` never goes into a response.
- **Success codes:** a read answers 200. A create answers 201, with a `Location` header when the
  resource it created has a GET.
- **Every endpoint declares its responses** for OpenAPI: typed results (`Results<Ok<T>,
  NotFound<ProblemDetails>>`) when the lambda builds every response itself, or `.Produces<T>()`,
  `.ProducesValidationProblem()` and `.ProducesProblem(statusCode)` when it returns
  `ToFailureResponse()`, whose `IResult` OpenAPI can't see into.

### Errors

- **Every error response is problem details:** 400, 404 and 409 included, and errors particular to
  one endpoint too. Never a bare list or an empty body.
- **A failed `Result<T>` becomes its response through `.ToFailureResponse()`:** validation is 400,
  conflict is 409 and not found is 404.
- **Another owner's item is not found (404), never 403**, so a caller can't probe for ids it
  doesn't own.
- **A field error names the field as its JSON property** (`purchaseOrderNumber`), so a screen can
  put the reason under that field. A required field's reason is "<field> is required.". A bad
  query-string value names its parameter (`batchSize`).
- **Every error response carries the request's trace ID**, which screens show as "Reference: …".
- **Unhandled exceptions become a 500 in `GlobalExceptionHandler`.** Endpoints don't catch
  exceptions to turn them into responses.

### Endpoints and handlers

- **An endpoint is a lambda in `Map{Module}` that calls one handler or query and turns its result
  into a response.** It holds no logic of its own. It takes the request's values, the handler or
  query, `RequestContext`, `ILoggerFactory` and a `CancellationToken`. There's no mediator.
- **A command handler is `{Action}Handler.{Action}Async(int ownerId, Guid callerId, {Action}Command
  command, CancellationToken token)`**, returning `Task<Result<{Action}Result>>`. **A query is
  `{Get…}Query.Get…Async(int ownerId, …, CancellationToken token)`**, returning `Task<Result<T>>`.
  A missing item is `Result<T>.NotFound`.
- **`ownerId` comes first** in every handler, query and repository method that takes one. **The
  token comes last**, as `CancellationToken token`, with no default.
- **Responses are built with `TypedResults`**, not `Results`.
- **The `CancellationToken` is passed all the way down to EF.** Clean-up after a failure uses
  `CancellationToken.None`, because the request's token may be the thing that was cancelled.

```csharp
group.MapGet("/orders/{orderId:guid}", async (
        Guid orderId,
        RequestContext requestContext,
        ILoggerFactory loggerFactory,
        GetOrderQuery query,
        CancellationToken token) =>
    {
        using var _ = LoggerScopeFactory.OwnerScope(
            loggerFactory.CreateLogger("Inventory"), requestContext.OwnerId);

        var result = await query.GetOrderByIdAsync(requestContext.OwnerId, orderId, token);

        return result.Succeeded ? TypedResults.Ok(result.Value) : result.ToFailureResponse();
    })
    .RequireAuthorization(InventoryPolicies.ReadAllocatedOrder)
    .Produces<OrderAllocationModel>()
    .ProducesProblem(StatusCodes.Status404NotFound);
```

### Validation

- **Validation collects every failure it finds** (ADR-0001), cheapest checks first: the command's
  `Validate()` returns a `Result`, then domain factories (`X.Create(…)`) return `Result<X>`, then
  checks that need the database run.
- **Required fields are checked first**, in `Validate()`, so a missing field is a 400 and never
  reaches code that would fail on a null.

### Authorization and owner scoping

- **Each permission is a policy** (ADR-0003): a constant in `internal static class
  {Module}Policies`, named for the permission (`ReceiveDelivery`), registered in `Add{Module}`
  with `RequireRole(ThroughlineRoles.X)`. Each endpoint calls
  `.RequireAuthorization({Module}Policies.X)`.
- **The host adds the owner-role check to every policy.** A policy over every owner's data, such as
  Platform's, adds `NotOwnerScopedRequirement` instead, and its endpoints take the owner as an
  optional filter.
- **The owner is `RequestContext.OwnerId` and the caller is `RequestContext.CallerId`.** Every query
  and every write filters by owner.

### Integration events

- **An event is a `{Thing}IntegrationEvent` sealed record** in `{Module}.Contracts/Events/`,
  deriving from `IntegrationEventBase(CreatedBy)`. It carries `int OwnerId` first, `DateTimeOffset`
  dates and `IReadOnlyCollection<T>` lists. Data nested in it is a `{Thing}Model` in
  `Contracts/Models/`.
- **An event is published through `IDbContextOutbox`**, in the same save as the change that caused
  it.
- **A consumer is a `{Thing}Handler`** in its module's `Infrastructure/Events/`, marked
  `[Transactional(typeof(TDbContext))]`, with `Handle(message, TDbContext db, ILogger<T> logger,
  CancellationToken token)`. Missing master data throws `MissingDependencyException`, which is
  retried later; data that can never succeed throws `UnrecoverableMessageException`, which is
  dead-lettered.
- **Modules talk to each other only through integration events and `.Contracts`.**

### Logging

- **An owner-scoped endpoint opens
  `LoggerScopeFactory.OwnerScope(loggerFactory.CreateLogger("{Module}"), requestContext.OwnerId)`**,
  which tags the request's span with `owner_id` and puts `OwnerId` on every log record. An event
  handler opens `logger.BeginScope("Owner {OwnerId}", message.OwnerId)`.
- **Handlers log their decisions at Information**, with placeholders: `{OwnerId}`, no `@` for plain
  values. Unexpected exceptions are logged at Error by `GlobalExceptionHandler` or Wolverine; code
  doesn't catch them only to log them.

---

## Web app screens

How every screen in the web app (`src/web`) behaves, so each screen's design doesn't decide it
again.

### Business rules live in the API

- **The API owns the rules.** A screen doesn't repeat them, apart from checking that required
  fields are filled in: it sends what the user entered and shows the API's reasons when it's
  refused. One place holds each rule, so a screen can't disagree with the API.
- **A screen doesn't send a request with a required field empty.** Each empty field shows
  "<Field> is required." beneath it, all at once, and nothing the user entered is cleared.
  Otherwise a screen only shapes input: it can limit what a field accepts, such as whole numbers
  in a quantity box, so the API gets a value it can judge.
- **The API checks required fields too,** at its boundary (usually the application layer), and
  refuses a missing one with a 400 field error like any other rule. The screen's check saves a
  round trip; the API's is the one that holds.
- **Refusal reasons are the API's words.** Each reason goes under the field it names; a reason
  that names no field goes at the top of the form. Every reason is shown together (ADR-0001), and
  nothing the user entered is cleared.
- **Adding something that already exists is refused as "already exists".** The API answers 409,
  even when the add is an exact repeat of one that went through, and the screen shows its reason
  like any other refusal. Every screen handles a 409 this way.

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
  nothing more. A repeated add is always refused as "already exists" (409). That's what lets the
  message say "try again".
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
- [ ] A module references only the shared projects and other modules' `.Contracts`; its types are `sealed` and `internal`
- [ ] Repository interfaces live in `Domain/`; persistence concerns stay out of domain classes
- [ ] Queries project straight from the DbContext with `AsNoTracking`; commands go through the domain
- [ ] Routes sit under the module's name, with plural kebab-case nouns and typed route ids
- [ ] Endpoints return Application types, declare their responses, and build them with `TypedResults`
- [ ] Every error response is problem details, names fields as their JSON properties and carries the trace ID
- [ ] Handlers return `Result<T>`, take `ownerId` first and `CancellationToken token` last, and collect every validation failure
- [ ] Every endpoint requires a `{Module}Policies` permission, and every query and write filters by owner
- [ ] Status, outcome and error-code strings are UPPER_SNAKE
- [ ] Owner-scoped endpoints open `LoggerScopeFactory.OwnerScope`
- [ ] Screens don't repeat business rules, apart from required fields; they show the API's reasons under each field
- [ ] Screens don't send with a required field empty and say "<Field> is required."; the API checks required fields too
- [ ] Adding something that already exists gets a 409, shown as "already exists" like any refusal
- [ ] 401 and 403 go to the app-wide pages, not per-screen messages
- [ ] Unexpected failures show one message, keep what was entered, and show a reference when there is one
- [ ] Actions that change data are never retried automatically, and the API makes them safe to retry
