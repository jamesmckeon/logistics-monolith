## What and why
<!-- What changes and why. -->
Closes #

## Deploy needed
<!-- Tick all that apply. Schema always ships before the API. -->
- [ ] **DB**: changesets under `db/changelog/` → Deploy DB, then Deploy API
- [ ] **API**: changes under `src/` → Deploy API
- [ ] **Container app settings**: new or changed environment variables or secrets. Deploy API changes only the image, so list each setting and set it by hand before the deploy:
- [ ] **Simulator**: deploys by itself on merge to main
- [ ] **None**: docs, tests or CI only

### If DB
Follows [docs/conventions.md](https://github.com/jamesmckeon/logistics-monolith/blob/main/docs/conventions.md#schema-changes).
- [ ] Expand-only: the code on prod now keeps working on the new schema
- [ ] Contraction issue, if any: #
- [ ] Risky change: the reversal changeset is written and tested on QA (ADR-0004)

## QA
- [ ] Deployed to QA: Deploy DB (QA) first if the schema changed, then Deploy API (QA)
- QA commit SHA:
- What I checked on QA:

## After merge
- [ ] Prod deploys done, in the order ticked above
