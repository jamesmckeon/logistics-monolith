# ADR-0002 — Commercial model: currency scope and pricing model

## Status

Accepted — 2026-08-09

## Context

STORY-0001 (#1) requires the intake acknowledgment to return an estimated fulfillment
quote "in the merchant's currency," priced from an external rate/zone table. The phrase
"a merchant" implies the platform serves many merchants, which raised two commercial
questions the pricing model can't be built without:

1. **Currency** — can merchants transact in different currencies? If so, quoting in "the
   merchant's currency" implies either a multi-currency price list or runtime FX
   conversion. Conversion is in direct tension with the story's own constraints: quotes
   must be **exact** (no rounding drift), **reproducible** (same order + same rate table →
   same price), and **clock-independent**. Exchange rates introduce rounding, movement over
   time, and an as-of date — so conversion would break the rules the story is built on.

2. **Pricing model** — is pricing a single published rate card applied to all merchants,
   or **per-merchant contract pricing** negotiated per account? In high-volume logistics,
   rates are typically a negotiated term of each merchant's contract: the same destination
   can carry different fees for different merchants. This determines what "the rate table"
   even means (global vs. keyed by merchant).

These are commercial (go-to-market and margin) decisions, not technical ones. In this lab
the engineer owns implementation; the commercial-owner role is played to unblock the model.

## Decision

**1. Domestic-only, single currency (USD).** We onboard US merchants only for the
foreseeable roadmap. Every merchant is quoted in USD. There is **no currency conversion and
no multi-currency support**. FX brings rate risk, cross-border tax, and reconciliation cost
we are not staffed to own. International remains a deliberate future expansion decision that
is **not** greenlit now. "The merchant's currency" is therefore trivially USD today; the
language is retained in contracts because international is a known eventual direction, but
nothing is built for it now.

**2. Per-merchant contract pricing.** Rates are negotiated per merchant. The **same
destination can carry a different pick fee and surcharge for two different merchants**. Rate
lookup is scoped per merchant (by their contract), not global. A single public rate card
would surrender margin and cost anchor accounts. Quoting is always "this merchant's rates
for this order," never a generic price.

**Explicitly deferred (kept out of current scope):** multi-currency / international; any
published or list-price concept.

## Consequences

**Positive**
- The currency dimension collapses to a constant (USD) — no FX engine, no as-of-rate
  bookkeeping, no cross-currency arithmetic — which keeps the story's exact/reproducible/
  clock-free constraints cheap to satisfy.
- Per-merchant pricing reflects real logistics commercial reality, so the domain model
  earns its complexity honestly rather than manufacturing it.
- Gives the rate/zone model a clear, single answer: **rates vary by merchant, all in USD.**

**Negative / costs**
- Committing to a single currency now means onboarding a non-USD merchant later is a
  material change, not a config tweak. Accepted deliberately given the domestic-only roadmap.
- Per-merchant pricing means rates are scoped per merchant rather than shared, a larger
  commercial surface to maintain than a single published card. Accepted as the cost of a
  realistic model.

**Revisit when:** we commit to a non-US region (reopens currency), or a commercial decision
introduces standard/list pricing alongside negotiated contracts (reopens pricing model).
