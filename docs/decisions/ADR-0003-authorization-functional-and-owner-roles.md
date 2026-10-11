# ADR-0003 — Authorization: functional and owner roles in Entra, permissions in code

## Status

Accepted — 2026-10-10. Not yet implemented; issue #22 is the first implementation.

## Context

Ordering, Receiving and Inventory are about to be deployed to Azure: App Service first, with
one or more modules expected to move to Azure Container Apps or Azure Container Instances later
(ADR-0002). Today the five HTTP endpoints accept anonymous requests, and the owner a request
acts for is read from an `owner_id` header that nothing checks.

Every request has to answer two separate questions:

1. **May this caller use this function?** (receive a delivery, allocate orders, …)
2. **May this caller act for the owner it names?**

Callers are of two kinds: a **service** (an EDI integrator submitting orders) that
authenticates as an application, and a **person** (a warehouse operations user) who signs in
with a user account. An operator in a shared 3PL building works for several owners, so the
owner cannot be inferred from who is calling; the request must name it.

## Decision

1. **Microsoft Entra ID authenticates, and the token is validated in application code**
   (JWT bearer), not by the hosting platform's built-in authentication.

2. **Functional roles are Entra app roles named for job functions**, not for individual
   actions. Each is declared for one kind of account — services or people — never both, so a
   service cannot pass as a person. The starting set:

   | Role | Held by | Permissions today |
   |---|---|---|
   | `OrderIntakeIntegration` | Service accounts | Create order, Read order |
   | `Receiving` | Operator accounts | Receive delivery |
   | `OrderPlanning` | Operator accounts | Allocate orders, Read allocation |

   Roles are not tied one-to-one to modules. As functionality is added, a role gains
   permissions, and a new role appears only when an endpoint needs one.

3. **Throughline maps roles to permissions in code. Each endpoint requires one permission and
   never looks at a role.**

4. **An owner role is an Entra app role, one per owner** (placeholder name `Owner.{ownerId}`),
   assignable to both kinds of account. It arrives in the same `roles` claim as the functional
   roles and carries no permission. The request names the owner (the `owner_id` header today),
   and is allowed only if the caller holds that owner's role.

5. **A caller's functional roles apply to every owner whose owner role it holds.** A
   functional role cannot be limited to one owner: an operator cannot be given Receiving for
   one owner and Order planning for another.

6. **Both checks are required on every endpoint.** Missing or invalid credentials → 401. A
   caller without a role granting the endpoint's permission, or without the owner role for
   the owner named → 403.

## Consequences

**Positive**

- **The token carries everything.** Nothing in Throughline stores who may do what or for whom.
  Validation behaves the same on App Service, Container Apps and Container Instances, and a
  module moved to its own host needs no shared access store (ADR-0002).
- **Roles grow by department.** A new action is a new permission on an existing role, not a
  new role.

**Negative / costs**

- **Revocation is not immediate.** A removed functional or owner role stays effective until the
  caller's current token expires.
- **Every new owner is a change in Entra** — a new app role plus assignments — and
  Throughline's owner ids are duplicated there.
- **The token grows with the number of owner roles** an account holds.
- **A functional role cannot be limited to particular owners.**
- **Changing which role grants which permission is a code change and a deploy.**

**Alternatives considered**

- **Check roles directly on endpoints.** Simplest. Rejected: it ties each endpoint to a role
  name, so every change to who may do what edits endpoints.
- **Store role assignments and permissions in Throughline.** Gives immediate revocation and
  in-app administration. Rejected for now: it needs access storage, administration and caching
  that no issue requires, and every extracted module would need to reach that store. Moving to
  it later changes only the component that produces a caller's permissions, because endpoints
  check permissions.
- **Record which owners a caller may act for in a Throughline table.** Rejected for the same
  reasons, plus an unresolved question of which module would own a table all three modules
  must consult. Within Entra, app roles are the only mechanism confirmed to work for both
  service and user accounts.

**Open:** the naming convention for owner roles.

**Revisit when:** an issue needs a functional role limited to particular owners, immediate
revocation, or in-app administration of access; the number of owners makes maintaining them in
Entra a burden; or Facility is modeled and owner roles need a facility dimension.
