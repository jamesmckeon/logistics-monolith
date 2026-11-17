# ADR-0004 — Schema changes expand then contract, and prod is fixed forward

## Status

Accepted — 2026-11-17.

## Context

Throughline is one deployment on one database, shared by every owner. There is no downtime
window: owners keep receiving, ordering and allocating while schema changes and new code are
deployed.

That rules out the usual way of making a schema rollback safe. A rollback loses nothing only if
nothing has been written since the change. With no downtime window, owners write as soon as the
change is live. Rolling back a new column or table then drops what every owner wrote to it, all
at once, and a Liquibase rollback undoes every changeset applied after the chosen tag, not just
the one at fault.

## Decision

**Schema changes expand, then contract. A schema change that breaks prod is fixed forward with a
new changeset; prod changesets are never rolled back.**

- **Expand.** A change only adds to the schema, or changes it in a way the running code still
  works with. Deploying new code, or rolling it back to the previous image, never needs a schema
  change.
- **Contract.** A drop or rename of something the code used ships only once no code on any
  environment uses the old structure. It is tracked from the time of the expand change as a
  tech-debt issue titled "Contraction: …".
- **Fix forward.** If a change breaks prod (a constraint that rejects writes, a lock on a busy
  table, a wrong grant), the reversal or correction is a new changeset that goes through the
  Validate Merge Action and QA like any other. For a change that's risky to begin with, write and
  test that reversal before the change ships.

## Consequences

**Positive**

- Owners' data is never lost to a schema rollback, and no owner is taken offline for a deploy.
- Every fix to prod goes through the same review and testing as the change it fixes.

**Costs / implications**

- **Fixing forward takes a pipeline run.** Prod stays degraded while the fix is written, tested
  on QA and deployed. A fault in the code is covered by redeploying the previous image; only a
  fault in the schema change itself waits for the fix.
- **Contraction is extra work.** Old columns, tables and grants stay until their contraction
  ships, and each needs an issue to keep track of it.
- New changesets don't carry a `--rollback`, since nothing runs one. Existing changesets keep
  theirs; applied changesets are never edited.

**Alternatives considered**

- **Prepared rollbacks run against prod.** Every changeset carries a tested `--rollback`, run on
  prod when a change goes wrong. Rejected: once owners have written to a new structure, the
  rollback drops their data.
- **Downtime windows.** Stop the application, change the schema, check it, and roll back before
  any writes. Rejected: it takes every owner offline for each deploy, and the rollback is only
  lossless until the window closes.
