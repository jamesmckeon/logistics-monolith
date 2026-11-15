# Deploying Throughline

All deploys are triggered manually from **Actions**. A merge to main deploys nothing.

| Workflow | Deploys | From |
|---|---|---|
| **Deploy Throughline API** (`deploy-api.yaml`) | `ca-throughline-api` (prod) | `main` only |
| **Deploy database migrations** (`deploy-migrations.yaml`) | prod schema changes | `main` only |
| **Deploy Throughline API to QA** (`deploy-api-qa.yaml`) | `ca-throughline-api-qa` (QA) | any branch |

---

## Deploy a branch to QA

QA is where a branch is deployed and tested before it is merged and deployed to prod.

1. Update the branch from main. GitHub runs the copy of the workflow on the branch you pick, so
   the branch must contain `deploy-api-qa.yaml`.
2. Run **Actions → Deploy Throughline API to QA → Run workflow** and pick the branch.

The run tests the branch and starts the QA database if it is stopped. It then builds an image
tagged with the commit SHA, deploys it, and waits for `/health`. A Logic App stops the QA database
every night at midnight Pacific.

---

## Deploy to prod

1. Ship any schema change first: run **Actions → Deploy database migrations → Run workflow** on
   `main`. `preview-update` prints the SQL that would run; `update` tags each module's changelog
   with the commit SHA, then applies what's pending.
2. Run **Actions → Deploy Throughline API → Run workflow** on `main`. The run passes both jobs
   (Test → Publish and deploy) and ends with the health check reporting `Revision is healthy.`

---

## Verify what is live

```bash
az containerapp show \
  --name ca-throughline-api \
  --resource-group rg-throughline \
  --query "properties.template.containers[0].image" \
  --output tsv
```

The tag should match the commit SHA from the run. For QA, use `ca-throughline-api-qa`.

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

For QA, run the same command against `ca-throughline-api-qa`, or deploy another branch to QA.
