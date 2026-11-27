---
name: intake-issue
description: Refine a GitHub issue into clear requirements and a minimal set of child issues for one UI and one backend developer. Requirements only; never changes code or repo files.
argument-hint: "[issue-number]"
arguments: [issue]
disable-model-invocation: true
allowed-tools: Bash(gh issue view *) Bash(gh issue list *)
---

# intake-issue

Refine GitHub issue **#$issue** into requirements a developer can pick up, then split it into
the fewest child issues that let **one UI developer and one backend developer** work, in
parallel where they can.

If the issue number above is missing or isn't a number, ask for it and stop.

## Hard boundaries

These hold for the whole time this skill runs, whatever is asked mid-way:

- **Read-only on the repository.** Never use Edit, Write or NotebookEdit on repo files. Never
  run a command that creates, changes or deletes a file, a branch or a commit. Don't build or
  run tests (both write files); read the tests instead.
- **No implementation.** You may read code and mention an implementation approach when it
  settles a requirement question. If asked to implement, say that's outside this skill and can
  start once the intake is done.
- **GitHub changes only after explicit approval** (see "Apply"). Reading issues needs no
  approval.

## 1. Gather

Read, don't ask:

- The issue, its comments, labels, state, parent and existing sub-issues:
  `gh issue view $issue --comments` and
  `gh issue view $issue --json labels,state,parent,subIssues`.
  If the issue is closed, say so and ask whether to continue.
- Open issues that overlap it: `gh issue list --search "<key terms>" --state open`.
- The code the issue touches: the modules, endpoints, domain types, authorization, how
  `OwnerId` is scoped, schema in `db/changelog/`, and the existing tests for that area. Look at
  how a sibling module already solved the same kind of problem.
- The project rules: `.claude/CLAUDE.md` (warehouse profile, scale baseline, bounded
  contexts), `docs/conventions.md`, `docs/testing.md`, ADRs in `docs/decisions/`.

A **fact** is something you read: cite the issue, comment, `file:line` or doc. Anything you
inferred is an **assumption**, even if it seems certain.

## 2. Intake brief and questions

Find the gaps in scope, behaviour, rules, access, and nonfunctional requirements, and any
acceptance criterion that can't be tested. Reply in this shape:

```
## #<n> <title>: intake

Facts
- <fact> (<source>)

Assumptions: I'll go ahead on these unless you say no
- A1 <assumption>

Questions
Scope
- Q1 <question> Suggested: <answer>, because <one line>
Behaviour
- Q2 …
Rules and data
Access
Load and scale
```

Rules for questions:

- Ask only what changes scope, an acceptance criterion, or the split into child issues.
  Anything else goes under "Not settled" in the drafts.
- Never ask what the issue, its comments, the code, the docs or the warehouse profile in
  `.claude/CLAUDE.md` already answer. Use the profile's scale baseline unless the issue says
  otherwise.
- At most 7 questions per round, numbered across rounds, each with a suggested answer, so
  "accept the suggestions" or "Q3: no" is a complete reply. Leave out empty groups.
- If the work needs something the repo doesn't have yet (a front end, a module, a role), state
  that as a fact and ask whether it's in this issue's scope.
- Stay in business terms. Don't invent actors, screens or usage the issue doesn't mention;
  if a question rests on an assumption, say so.

After each reply, fold the answers into facts and assumptions, and ask again only about what's
still open. Stop asking when:

- every acceptance criterion can be checked by someone other than its author,
- the scope is clear enough to split, and
- what's still unknown doesn't change the split or an acceptance criterion.

Don't chase completeness past that point.

## 3. Draft

Send everything in one message: parent update, work plan, child issues, validation.

### Parent issue

First a short list of what changed and why (criteria added, removed or reworded). Then the
full proposed body. Keep the author's wording wherever it's still right; change only what
the intake settled. Shape, as in #67:

```
## Context
## Acceptance criteria        (- [ ] checkboxes)
## Out of scope               (only if needed)
## Not settled                (only if anything is)
## Sub-issues
The work is split into the sub-issues listed below.
```

The data the business needs captured, such as the fields a record is added with, is an
acceptance criterion. How the screen and the API exchange it (lookups, request and response
bodies, status codes, message wording) is the backend child's HTTP contract, not part of the
parent.

### Work plan

```
| Draft | Title | Developer | Depends on | Can start |
```

Splitting rules:

- Start from one child per developer who has work. Don't invent UI work for a backend-only
  issue.
- Split further only when a piece can be verified on its own and either ships separately or
  has a different dependency. Never split by layer or by technique.
- Consider a child for each module that consumes an event the issue publishes, apart from the
  publisher's child. This is a split by module, not by layer. Propose it in the work plan when
  the consumer is a different module from the publisher, already exists, and the issue gives it
  an outcome someone can check in that module's own work, such as a delivery being received or
  an order line being allocated. Each consumer child depends on the event contract being
  agreed, not built. When any of those doesn't hold, ask under Scope instead, with a suggested
  answer.
- The UI child depends on the backend child's **contract being agreed**, not on the backend
  being built, so both can start at once.
- A schema change belongs to the child whose code needs it. If it leaves a contraction for
  later, add a `tech-debt` child titled "Contraction: …" (see `docs/conventions.md`).

### Child issues

Title: `<parent theme>: <outcome>`, matching the existing sub-issues (for example
"Dead letters: replay endpoint"). Labels: the parent's. Body:

```
## Context

Part of #<parent>. Developer: <UI | Backend>

<one or two sentences on what this delivers>

## Acceptance criteria

- [ ] <observable outcome>

## Depends on

- <C-label or #n>: <what it needs from it>    (or: Nothing; can start now.)

## Contract                                    (backend child with a UI partner)

The HTTP contract (routes, requests, responses and status codes) the UI is built against is
posted here for the UI developer to review before the UI is built against it.

## Design                                      (UI child)

Wireframes are posted here for review before the screen is built.

## Not settled

- <open detail that doesn't block starting>
```

Acceptance criteria state outcomes, not implementation: no class names, tables or patterns.
Never add a "is tested" criterion; testing is always expected.

### Validation

Check the drafts before sending them, and fix what you can. Report only the problems that are
left (or one line saying none are):

- Every parent criterion is covered by at least one child; no child criterion falls outside
  the parent.
- Every criterion can be tested and is stated as a business outcome.
- Dependencies point one way, with no cycles, and the UI child can start without the backend
  being built.
- Each owner's data stays scoped to that owner, and access is restricted where the issue
  asks for it.
- The drafts agree with each other and with the facts: names, terms and rules.
- Nothing duplicates an open issue or an existing sub-issue. Propose changes to an existing
  sub-issue instead of creating a near-copy.
- Every fact still has a source; every assumption is labelled.

## 4. Approve

End the draft message by asking for approval of the whole set, or of the parent alone.

- Approval covers exactly the drafts shown. If anything changes afterwards, show the changed
  draft and ask again.
- A reply that asks for changes is not approval.

## 5. Apply (only after approval)

Pass bodies on stdin with `--body-file -` and a heredoc; never write a temp file in the repo.

1. Update the parent: `gh issue edit <parent> --body-file -`.
2. Create the children in dependency order, so each "Depends on" can use a real issue
   number, each linked as a sub-issue as it's created:
   `gh issue create --parent <parent> --title "…" --label "…" --body-file -`.
3. If an approved change edits an existing sub-issue, apply it with `gh issue edit`.
4. Report the parent and each child as links.

If a step fails, stop. Say what was done and what wasn't, and ask how to continue. Don't
retry blindly, and don't delete or close anything to undo it.
