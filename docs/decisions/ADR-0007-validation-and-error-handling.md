# ADR-0007 — Validation & error handling: always-valid domain, Result at the boundary

## Status

Proposed — 2026-08-16. Ratify once the pattern is applied across the value objects and the first
handler.

## Context

Two needs pull in opposite directions on how invalid data is signalled:

- A client submitting a request needs **comprehensive** feedback — every problem at once, not just
  the first — so it can fix everything in one round trip.
- The domain model needs to be **trustworthy**: once a domain object exists, code that receives it
  may assume it is valid, without re-checking.

One mechanism cannot serve both. Throwing on the first invalid field gives the domain its guarantee
but yields one error at a time. Returning a `Result` everywhere lets errors accumulate but permits
invalid domain objects to exist (or forces every consumer to re-validate). So the responsibility is
split — by layer, and by cause.

## Decision

**1. `Result<T>` carries comprehensive, client-facing errors.**
Its purpose is to return the *full set* of validation errors to the caller, so the client can
correct everything in one round trip. It accumulates; it does not stop at the first error.

**2. Domain types are always-valid, exposed through two entry points that enforce the same invariants:**

- **`Create(...) → Result<T>`** — the evaluating front door: returns every validation `Error`, or the
  constructed instance. It does **not** throw on domain-rule violations.
- **constructor** — enforces the same invariants and throws **`DomainValidationException`** when any is
  violated. This is the always-valid backstop; it fires only when code constructs a domain object
  while bypassing `Create()` — i.e. on a programmer error.

Within that, failures split by cause:

- **Null on a non-nullable parameter → `ArgumentNullException`.** A precondition breach — a bug —
  thrown from *either* entry point. It is never a collected `Error`; a null is not client-fixable data.
- **A present-but-invalid value (blank, bad format, out of range) → an `Error`.** Surfaced as `Result`
  via `Create()`, or as the thrown `DomainValidationException` via the constructor.

*(How a type shares its invariant checks between the two entry points is an implementation detail, not
part of this contract.)*

**3. Application command types expose `Create() → Result<T>` for structural validation only.**
`XCommand.Create(...)` validates the *shape* of the request — required fields present, non-blank, item
counts, quantities — and returns all such errors comprehensively. A command does **not** validate or
return domain errors, and does not construct domain objects.

**4. Handlers return domain errors, via domain objects.**
A handler takes a structurally-valid command and builds the domain objects it needs through their
`Create()` factories. Domain-rule violations (postal-code format, state format, business invariants)
surface **here**, in the handler's `Result`, and reach the client through it. Domain errors are the
handler's responsibility, not the command's.

Net flow: **command validates the request's shape → handler builds domain objects and surfaces
domain-rule errors → domain constructors guarantee always-valid state** (throwing
`DomainValidationException` as the backstop; `ArgumentNullException` for null precondition breaches).

## Consequences

**Positive**

- The client gets comprehensive errors at each gate — structural from the command, domain from the
  handler — and never a raw exception on the normal path.
- Domain objects are trustworthy by construction; no defensive re-validation scattered through the model.
- Clear ownership: structural validation = command; domain validation = handler (via domain objects);
  invariant guarantee = domain constructor. Null (a bug) is always `ArgumentNullException`, kept
  distinct from domain-rule violations.

**Costs / implications**

- `DomainValidationException` is the always-valid backstop; it fires only on a programmer error
  (constructing while bypassing `Create()`). Because of that it should carry *which* invariant failed,
  so it is debuggable when it does fire.
- Domain value objects follow this contract; their existing `Create() → Result` tests stay valid.
- `CreateOrderCommand.Create` validates structural input only — postal-format/state checks are **not**
  in the command; they move into the handler when it builds the address value objects.
- Handlers build domain objects via `Create()` (not `new`) while validity is still being evaluated, so
  errors accumulate into the `Result` rather than throwing.

**Alternatives considered**

- **Throw everywhere (no `Result`).** Domain stays always-valid, but the client gets one error at a
  time and exceptions become control flow at the boundary. Rejected — fails the comprehensive-feedback goal.
- **`Result` everywhere, including domain construction (no throwing constructors).** Errors accumulate,
  but domain objects can be constructed invalid (or every consumer must re-check), losing the
  always-valid guarantee. Rejected.
- **Treat a null param as a collected `Error`** rather than `ArgumentNullException`. Conflates a
  precondition bug with client-fixable data and abandons the .NET idiom. Rejected.

**Revisit when**

- A transport/API layer is added and needs an `Error`/`ErrorType` → HTTP-status mapping.
- A case appears where domain-rule validation genuinely must happen at the command layer (reopens the
  command-vs-handler split).

## Related

- ADR-0005 — error taxonomy (`Error`/`ErrorType`). This ADR governs **how** validity is signalled and
  **where**; ADR-0005 governs **how failures are categorised**.
