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
migrate job opens a temporary rule for the runner's IP and closes it unconditionally
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

Use a login that can run EF Core migrations (i.e. apply DDL). `throughlineadmin` has this
access. Do not use an application login with only DML permissions.

---

## 5. Verify

Trigger a run manually from **Actions → Deploy Throughline API → Run workflow**, selecting
`main`. The run should pass all three jobs (Test → Apply migrations → Publish and deploy)
and end with the health check reporting `Revision is healthy.`

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

No rebuild is needed. The running schema may be ahead of the rolled-back code;
see issue #42 for the database rollback decision.
