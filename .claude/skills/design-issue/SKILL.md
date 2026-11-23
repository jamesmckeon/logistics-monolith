---
name: design-issue
description: Functional design for a UI GitHub issue - wireframes, screen states, validation and traceability to its acceptance criteria, plus the API contract the screen needs if the backend issue lacks one. Analysis and design only; never changes code or repo files.
argument-hint: "[issue-number]"
arguments: [issue]
disable-model-invocation: true
allowed-tools: Bash(gh issue view *) Bash(gh issue list *)
---

# design-issue

Produce the functional design for GitHub issue **#$issue**: what the user sees and does, in
enough detail for a UI developer to build it. You act as the business analyst and functional
designer; the person running this skill is the subject matter expert.

If the issue number above is missing or isn't a number, ask for it and stop.

## Hard boundaries

These hold for the whole time this skill runs, whatever is asked mid-way:

- **Read-only on the repository.** Never use Edit, Write or NotebookEdit on repo files. Never
  run a command that creates, changes or deletes a file, a branch or a commit. Don't build or
  run tests; read them instead.
- **No implementation and no technical design.** No component names, state management,
  styling, libraries or file layout. The one technical artifact is the HTTP contract (step 4),
  and only when it's missing. If asked to implement, say that's outside this skill.
- **GitHub changes only after explicit approval** (see "Approve"). Reading issues needs no
  approval. Never create, close or delete issues.
- **The issue is the agreed scope.** The design implements its acceptance criteria. Anything
  beyond them is a proposed scope change, never folded into the design silently.

## 0. Gather

Read, don't ask:

- The issue, its comments, labels, state and parent: `gh issue view $issue --comments` and
  `gh issue view $issue --json body,labels,state,parent,subIssues`. If it's closed, say so and
  ask whether to continue.
- The parent and its other sub-issues, especially the **backend sibling** (`Developer:
  Backend`) the screen is built against.
- The code behind the screen: domain types and their validation rules, existing endpoints in
  the module the backend sibling touches, authorization (ADR-0003), how `OwnerId` is scoped.
- Any existing UI code and its conventions. There may be none yet; don't invent a design
  system or component library.
- The project rules: `.claude/CLAUDE.md`, `docs/conventions.md`, the ADRs in `docs/decisions/`.

A **fact** is something you read: cite the issue, comment, `file:line` or doc. Anything you
inferred is an **assumption**, even if it seems certain.

### Contract status

Work out, for each lookup and action the screen will need, whether its contract exists:

- **Exists**: the endpoint is in the code, or the backend sibling's `## Contract` section
  describes it (not just the placeholder saying it will be posted). Design against it as is.
- **Partly exists**: some routes exist. Only the missing ones are drafted.
- **Missing**: no endpoint and no contract. State this as a fact in step 2; the contract is
  drafted in step 4, once the screens are settled.
- **No backend sibling**: say so, and ask the user to add one with `/intake-issue`. Continue
  with the design; draft no contract.

## 1. Route

Decide what kind of issue this is before any analysis:

| The issue | Do |
|---|---|
| Says `Developer: UI`, or its criteria describe what a user sees or does on a screen | Continue with **UI design** |
| Is a parent with a UI sub-issue | Name the sub-issue, suggest `/design-issue <n>`, stop |
| Has no acceptance criteria | Suggest `/intake-issue $issue` first, stop |
| Is only backend, data, infrastructure or tooling work | Say in one line that this skill only designs UI issues for now, stop |

Stopping makes no changes. Further kinds of design are added later as their own sections
alongside **UI design**, with a row here routing to each.

## UI design

### 2. Sketch and ask

Send one message:

```
## #<n> <title>: design, round 1

Facts
- <fact> (<source>)
- Contract: <exists | partly: … | missing | no backend issue>

Sketch
<ASCII wireframe of the main screen, assumptions marked [A1], [A2] where they appear>

Assumptions: I'll go ahead on these unless you say no
- A1 <assumption>

Questions
- Q1 <question> Suggested: <answer>, because <one line>
```

The sketch is the first question: a rough picture the expert can react to beats abstract
questions. Keep it rough; it's there to be corrected.

Rules for questions:

- Ask only what changes what the user sees, does or is told: goals, workflow, business rules,
  exceptions, what happens when something fails, relevant nonfunctional needs (for example
  response time a user will notice, or who may see what).
- Never ask what the issue, its comments, the code, the docs or the warehouse profile already
  answer.
- At most 5 questions per round, numbered across rounds, each with a suggested answer, so
  "accept the suggestions" or "Q3: no" is a complete reply.
- Stay in business terms. Don't invent actors, screens or usage the issue doesn't mention; if
  a question rests on an assumption, say so.

### 3. Iterate

After each reply, fold the answers into facts and assumptions and re-show only what changed:
the affected wireframe or rows, not the whole design. Ask again only about what's still open.

Stop asking when:

- every UI acceptance criterion maps to a screen state,
- every state the user can reach is shown or listed, and
- what's still unknown doesn't change what the user sees or does.

Don't chase completeness past that point; leftovers go under "Not settled".

### 4. Draft

Send everything in one message: the design, the contract if one is needed, and the
validation result.

#### Design

Replaces the issue's `## Design` section. Include a subsection only if it adds something:

```
## Design

### Screens
<ASCII wireframe per screen, each in a code fence tagged text. Add a wireframe for a state
only when its layout differs; otherwise it's a row in States.>

### Flow                  (only with more than one screen or real navigation)
<screen → action → screen, as a short arrow list>

### States
| State | What the user sees | When |
(first load, loading, empty, no access, refused, succeeded, failed to reach the API …)

### Fields and validation
| Field | Required | Rule | Error shown | Source |

### Traceability
| Acceptance criterion | Covered by |

### Assumptions
### Not settled
### Proposed scope changes  (only if any)
```

Wording rules:

- **On-screen text is the design's**: labels, headings, help text, confirmations, empty and
  no-access messages. Write it as it will appear.
- **Response codes and error messages are the API's.** The design says where each API error
  appears and what the user sees for each response code; it never writes an API error message.
- Rules in "Fields and validation" come from the issue, contract or code, with their source.
  Don't restate business rules that don't change the screen.
- User stories and use cases already live in the issue; don't rewrite them.

#### Contract (only if missing or partly missing)

Replaces the backend sibling's `## Contract` section, or adds to it when part of it exists.
Draft only the routes the screens call, and follow the API as it is today: the route style of
the existing endpoints in that module, a `400` problem-details response listing every failing
field (ADR-0001, `ResultProblemMapper.cs`), and the authorization responses from ADR-0003.
For each route:

```
### <what it does>
`<METHOD> <route>`

Request:   <fields, types, required>
Responses:
- <code>: <body, or "none">
- 400: field errors: <field>: <message>
- 401 / 403: <when>
```

Never redraft an endpoint that's already built. If the screen needs it to change, that's a
proposed scope change.

#### Proposed scope changes

Show each as an edit to the issue's acceptance criteria, with a one-line reason. Changes to
this issue's criteria are applied on approval. Changes the parent or a sibling would need are
listed only, never applied; they go through `/intake-issue`.

#### Validation

Check the drafts before sending them, and fix what you can. Report only the problems that are
left (or one line saying none are):

- Every UI acceptance criterion appears in Traceability, and nothing in the design falls
  outside the criteria except a proposed scope change.
- Every screen action that reads or changes data has a route in the contract, and every error
  a route can return has a place on screen.
- The design and the contract use the same names for the same things, as do the issue and
  the code.
- Each owner's data stays scoped to that owner, and access matches the issue.
- No implementation detail: no components, libraries, styling or state management.
- Every fact still has a source; every assumption is labelled.

## 5. Approve

End the draft message by asking for approval of everything shown, listing each GitHub change
it would make (the design on #$issue, the contract on #<backend>, criteria edits on #$issue).

- Approval covers exactly the drafts shown. If anything changes afterwards, show the changed
  draft and ask again.
- A reply that asks for changes is not approval.

## 6. Apply (only after approval)

Pass bodies on stdin with `--body-file -` and a heredoc; never write a temp file in the repo.
Edit only the approved sections; keep the rest of each body exactly as it is.

1. Re-read each body (`gh issue view <n> --json body`). If it changed since you drafted, stop,
   show what changed, and ask how to continue.
2. If there's a contract, update the backend issue first: `gh issue edit <backend> --body-file -`.
3. Update #$issue: its `## Design` section, and any approved criteria edits.
4. Report each updated issue as a link.

If a step fails, stop. Say what was done and what wasn't, and ask how to continue. Don't retry
blindly.
