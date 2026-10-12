# ADR-0001 — Endpoints report validation failures together, not one at a time

## Status

Accepted — 2026-10-11 (proposed 2026-08-16).

## Context

A request to an endpoint can be invalid in several ways at once: more than one required field
missing, a malformed postal code and a malformed state, an empty item list.

If the endpoint reports only the first problem it finds, the caller corrects that one, resubmits,
and is told about the next. Each problem costs a round trip, and the caller never sees the full
list of what is wrong with the request.

## Decision

**When an endpoint rejects a request as invalid, the response lists all the validation failures it
found — not only the first.**

Validation failures are therefore returned, not thrown. Handlers return `Result` / `Result<T>`
(`Throughline.Common.Results`), which carries a list of `FieldError`s — each a description and,
where it applies, the name of the field at fault. The endpoint maps a failed validation result to a
`400` problem-details response containing those errors.

**This is not a promise to find every possible failure.** Validation may stop before all checks
have run:

- **A check depends on an earlier one.** A value has to be present and well-formed before a rule
  about its content can be applied.
- **A check is costly.** A request already known to be invalid from its own content is rejected
  without running checks that need a database query — a missing required field is reported
  without also looking up whether a code elsewhere in the request exists in master data.

Every failure found by the checks that did run is reported.

## Consequences

**Positive**

- A caller can usually correct what is wrong with a request in one round trip.

**Costs / implications**

- Validation code cannot stop at the first problem. It collects each failure and keeps checking,
  up to the point where one of the two cases above applies.
- A caller may still need more than one round trip: a request that fails on its content can fail
  again, once corrected, on a check that was not reached the first time.
- The response has to carry every failure found through to the caller. A mapping that drops or
  summarises some of them defeats the decision at the last step.

**Alternatives considered**

- **Throw on the first invalid value.** Rejected: the caller gets one error at a time, and
  exceptions become control flow for input that is expected to be wrong some of the time.
