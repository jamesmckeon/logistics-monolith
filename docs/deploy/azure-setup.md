# Azure setup for deploy-api.yaml

One-time setup for the `deploy-api.yaml` CD pipeline (issue #41). Run these steps once;
the workflow itself needs no further Azure configuration after they are done.

Existing resources that already exist and are not created here:
`rg-throughline`, `crthroughline.azurecr.io`, `ca-throughline-api`,
`psql-throughline.postgres.database.azure.com`.

---

## 1. Create the deploy identity

```bash
az ad app create --display-name gh-throughline-api-deploy
```

Copy the `appId` from the output — that is `<APP_ID>` throughout this document.

```bash
az ad sp create --id <APP_ID>
```

Copy the service principal's `id` — that is `<SP_ID>`.

---

## 2. Add the federated credential (scoped to main)

The credential only lets the OIDC token flow succeed for runs on `refs/heads/main`.
A run triggered from any other branch cannot sign in, regardless of what the workflow
file says.

```bash
az ad app federated-credential create \
  --id <APP_ID> \
  --parameters '{
    "name": "gh-throughline-api-deploy-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:jamesmckeon/logistics-monolith:ref:refs/heads/main",
    "description": "GitHub Actions — main branch only",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

---

## 3. Assign roles

### Push images to the registry

```bash
REGISTRY_ID=$(az acr show \
  --name crthroughline \
  --resource-group rg-throughline \
  --query id --output tsv)

az role assignment create \
  --assignee-object-id <SP_ID> \
  --assignee-principal-type ServicePrincipal \
  --role AcrPush \
  --scope "$REGISTRY_ID"
```

### Update the container app

```bash
API_APP_ID=$(az containerapp show \
  --name ca-throughline-api \
  --resource-group rg-throughline \
  --query id --output tsv)

az role assignment create \
  --assignee-object-id <SP_ID> \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope "$API_APP_ID"
```

### Manage Postgres firewall rules (for migrations)

GitHub Actions `ubuntu-latest` runners egress from public internet IPs and are not
covered by the existing `AllowAllAzureServicesAndResourcesWithinAzureIps` rule. The
migrations workflow (`deploy-migrations.yaml`) opens a temporary rule for the runner's IP and closes it unconditionally
afterwards. `Contributor` on the Postgres server is the minimum built-in role that
allows creating and deleting firewall rules.

```bash
PG_ID=$(az postgres flexible-server show \
  --name psql-throughline \
  --resource-group rg-throughline \
  --query id --output tsv)

az role assignment create \
  --assignee-object-id <SP_ID> \
  --assignee-principal-type ServicePrincipal \
  --role Contributor \
  --scope "$PG_ID"
```

---

## 4. Add secrets to the GitHub repository

In **Settings → Secrets and variables → Actions**, add these repository secrets:

| Secret | Value |
|---|---|
| `AZURE_API_CLIENT_ID` | `<APP_ID>` from step 1 |
| `AZURE_TENANT_ID` | Your main tenant ID (already set for the simulator; same value) |
| `AZURE_SUBSCRIPTION_ID` | Your subscription ID (already set for the simulator; same value) |
| `AZURE_DB_CONNECTION_STRING` | The Postgres connection string — see below |

### Connection string format

```
Host=psql-throughline.postgres.database.azure.com;Port=5432;Database=throughline;Username=throughlineadmin;Password=<password>;Ssl Mode=Require
```

The migrations workflow reads its Liquibase connection from this secret, so use a login that can
apply DDL. `throughlineadmin` has this access. Do not use an application login with only DML
permissions.

---

## 5. Verify

Trigger a run manually from **Actions → Deploy Throughline API → Run workflow**, selecting
`main`. The run should pass both jobs (Test → Publish and deploy) and end with the health
check reporting `Revision is healthy.`

Schema changes deploy separately, from **Actions → Deploy database migrations → Run workflow**
on `main`. `preview-update` prints the SQL that would run; `update` tags each module's changelog
with the commit SHA and then applies what's pending.

To verify the new revision is live:

```bash
az containerapp show \
  --name ca-throughline-api \
  --resource-group rg-throughline \
  --query "properties.template.containers[0].image" \
  --output tsv
```

The tag should match the commit SHA from the run.

---

## Code rollback

To roll back the code to a previous commit, point the container app at the prior
SHA-tagged image in the registry:

```bash
az containerapp update \
  --name ca-throughline-api \
  --resource-group rg-throughline \
  --image crthroughline.azurecr.io/throughline-api:<previous-sha>
```

No rebuild is needed. The running schema may be ahead of the rolled-back code. Expand-only
schema changes keep the older code working, so this usually needs no schema rollback. When it
does, run **Deploy database migrations** with `preview-rollback`, then `rollback`, giving the
commit SHA of the update to undo as `rollback-tag`. Each update tags the database before it
applies anything, so that update and every later one are rolled back.

---

## QA environment (`deploy-api-qa.yaml`)

QA is where a branch is deployed and tested before it is merged and deployed to prod. Any branch
can be deployed to QA; prod still deploys from main only.

QA resources, separate from prod:

| Resource | Notes |
|---|---|
| `ca-throughline-api-qa` | Same environment (`cae-throughline`) and settings as `ca-throughline-api`, except `ASPNETCORE_ENVIRONMENT=QA` and 0–1 replicas, so it scales to zero when idle. Its managed identity has `AcrPull` on `crthroughline`. |
| `psql-throughline-perf` | QA's Postgres server. A Logic App, `throughline-stop-qa-db`, stops it every night at midnight Pacific; the QA deploy starts it again. |

The container app's `pg-connection` secret points at the QA server:

```
Host=psql-throughline-perf.postgres.database.azure.com;Port=5432;Database=throughline;Username=throughlinetestadmin;Password=<password>;SSL Mode=Require
```

### QA deploy identity

A separate app registration, `gh-throughline-qa-deploy`, so a QA run can never change prod.

```bash
az ad app create --display-name gh-throughline-qa-deploy
az ad sp create --id <QA_APP_ID>
```

Its federated credential accepts tokens from the GitHub environment `qa`, not from a branch. Any
branch can sign in, but only through a job that uses that environment.

```bash
az ad app federated-credential create \
  --id <QA_APP_ID> \
  --parameters '{
    "name": "gh-throughline-qa-deploy-env-qa",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:jamesmckeon/logistics-monolith:environment:qa",
    "description": "GitHub Actions - qa environment only",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

Roles, each scoped to one QA resource:

| Role | Scope | Why |
|---|---|---|
| `AcrPush` | `crthroughline` | Push the image built from the branch |
| `Contributor` | `ca-throughline-api-qa` | Deploy a new revision |
| `Contributor` | `psql-throughline-perf` | Start the stopped server; manage firewall rules for QA migrations |

### GitHub `qa` environment

In **Settings → Environments → qa**, add one environment secret:

| Secret | Value |
|---|---|
| `AZURE_API_CLIENT_ID` | `<QA_APP_ID>` |

An environment secret overrides the repository secret of the same name, so the QA workflow uses
the same secret names as prod. `AZURE_TENANT_ID` and `AZURE_SUBSCRIPTION_ID` come from the
repository secrets.

### Deploy a branch to QA

GitHub only lists a manually triggered workflow once its file is on main, and runs the copy on the
branch you pick. Update the branch from main first, then run **Actions → Deploy Throughline API
to QA → Run workflow** and pick the branch.
