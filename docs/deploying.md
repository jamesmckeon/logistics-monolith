# Deploying Throughline

API, web app and schema deploys are started by hand from **Actions**; merging to main doesn't
deploy them. What each workflow does is in [workflows.md](workflows.md).

---

## Deploy a branch to QA

QA is where a branch is deployed and tested before it is merged and deployed to prod.

1. If the branch changes the schema, ship that first, as for prod: run **Actions → Deploy DB (QA)
   → Run workflow**, pick the branch, then `preview-update` and `update`. It starts the QA
   database if it's stopped.
2. Run **Actions → Deploy API (QA) → Run workflow** and pick the branch.
3. If the branch changes the web app, run **Actions → Deploy Web (QA) → Run workflow** and pick
   the branch.

The API run tests the branch and starts the QA database if it is stopped. It then builds an image
tagged with the commit SHA, deploys it, and waits for `/health`. A Logic App stops the QA database
every night at midnight Pacific.

The web run lints and tests the web app, builds it for QA, uploads it to `swa-throughline-web-qa`
and waits until <https://purple-tree-0566b1b10.6.azurestaticapps.net/version.txt> shows the commit
SHA. The QA API accepts the web app's calls because its container app lists that address in
`Cors__AllowedOrigins__0`.

---

## Deploy to prod

1. Ship any schema change first: run **Actions → Deploy DB (prod) → Run workflow** on `main`.
   `preview-update` prints the SQL that would run; `update` lists each module's pending
   changesets, then applies them.
2. Run **Actions → Deploy API (prod) → Run workflow** on `main`. The run passes both jobs
   (Test → Publish and deploy) and ends with the health check reporting `Revision is healthy.`
3. If the change touches the web app, run **Actions → Deploy Web (prod) → Run workflow** on
   `main`.

The web run lints and tests the web app, builds it for prod, uploads it to `swa-throughline-web`
and waits until <https://throughline.mckeonlabs.com/version.txt> shows the commit SHA. The prod
API accepts the web app's calls because its container app lists that address in
`Cors__AllowedOrigins__0`.

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

For the prod web app, open <https://throughline.mckeonlabs.com/version.txt>. It shows the commit
SHA of the last deploy. For the QA web app, open
<https://purple-tree-0566b1b10.6.azurestaticapps.net/version.txt>.

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

No rebuild is needed. The running schema may be ahead of the rolled-back code. Expand schema
changes keep the older code working, so the schema stays as it is. Schemas are never rolled
back: if a schema change itself breaks prod, fix it with a new changeset, tested on QA and
deployed like any other (ADR-0004).

For QA, run the same command against `ca-throughline-api-qa`, or deploy another branch to QA. To
undo schema changes on QA, run **Reset QA data**.

To roll back the prod web app, revert the change on `main`, then run **Deploy Web (prod)**. It
runs only from `main` and keeps no earlier builds, so the revert is how an older version goes
back out.

To roll back the QA web app, run **Deploy Web (QA)** on the branch to go back to, such as
`main`. It rebuilds that branch and replaces the site.
