# Workflows

The GitHub Actions workflows in `.github/workflows`. How to use them to ship a change is in
[deploying.md](deploying.md).

| Workflow | What it does | Runs | From |
|---|---|---|---|
| **Validate Merge** (`build-and-test.yaml`) | The pull request gate: builds the solution and runs the unit and integration tests, which branch protection on `main` requires to pass. Schema changes are proven by Deploy DB (QA), not here. Deploys nothing. | On every pull request and every push to `main`, or by hand | any branch |
| **Deploy API (prod)** (`deploy-api.yaml`) | Tests, builds an image tagged with the commit SHA, deploys it to `ca-throughline-api` and waits for the new revision to be healthy | By hand | `main` only |
| **Deploy API (QA)** (`deploy-api-qa.yaml`) | Tests the branch, starts the QA database if it's stopped, builds an image tagged with the commit SHA, deploys it to `ca-throughline-api-qa` and waits for `/health` | By hand | any branch |
| **Deploy DB (prod)** (`deploy-migrations.yaml`) | Applies pending Liquibase changesets to `psql-throughline`. `preview-update` prints the SQL without running it. Never rolls back. | By hand | `main` only |
| **Deploy DB (QA)** (`deploy-migrations-qa.yaml`) | Starts the QA database if it's stopped, then applies pending Liquibase changesets to `psql-throughline-perf`. `preview-update` prints the SQL without running it. | By hand | any branch |
| **Deploy simulator** (`deploy-simulator.yaml`) | Deploys the traffic simulator to `ca-throughline-simulator` | On a push to `main` that changes the simulator, its tests, the shared package versions or this workflow, or by hand | `main` on a push; the branch you pick by hand |
| **Reset QA data** (`reset-qa.yaml`) | Stops the QA app, replaces QA's `throughline` database with a copy of `throughline_baseline`, then starts the app and waits for `/health` | By hand | any branch |

A workflow run by hand uses the copy of the workflow on the branch you pick, not main's, so a
branch that's behind main runs older versions of the workflows.

Deploy API (QA), Reset QA data and Deploy DB (QA) share one concurrency group, so
none of them runs against QA while another is running.
