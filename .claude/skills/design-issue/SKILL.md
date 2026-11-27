---
name: design-issue
description: Functional design for a UI GitHub issue - use cases, a clickable mockup of every screen state, validation and traceability to its acceptance criteria, plus the API contract the screen needs if the backend issue lacks one. Analysis and design only; never changes code or repo files.
argument-hint: "[issue-number]"
arguments: [issue]
disable-model-invocation: true
allowed-tools: Bash(gh issue view *) Bash(gh issue list *)
---

# design-issue

Produce the functional design for GitHub issue **#$issue**: its use cases, and what the user
sees and does, in enough detail for a UI developer to build it. You act as the business analyst and functional
designer; the person running this skill is the subject matter expert.

If the issue number above is missing or isn't a number, ask for it and stop.

## Hard boundaries

These hold for the whole time this skill runs, whatever is asked mid-way:

- **Read-only on the repository.** Never use Edit, Write or NotebookEdit on repo files. Never
  run a command that creates, changes or deletes a file, a branch or a commit. Don't build or
  run tests; read them instead. The mockup page (step 4) is written in the session's
  scratchpad and published as an Artifact, never saved in the repo.
- **No implementation and no technical design.** No component names, state management,
  styling, libraries or file layout. The one technical artifact is the HTTP contract (step 4),
  and only when it's missing. If asked to implement, say that's outside this skill. The
  mockup's look is illustration, not design (see "Mockup").
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
- The project rules: `.claude/CLAUDE.md`, the ADRs in `docs/decisions/`, and
  `docs/conventions.md`, especially **Web app screens**. That section settles how every screen
  handles business rules, 401 and 403, and failures to reach the API. Apply it; don't ask about
  it or design it again.

What you read is working material, not something to report. Anything you concluded from it is
an **assumption**, even if it seems certain. When a source backs an assumption or a suggested
answer, cite it inline (the issue, comment, `file:line` or doc).

### Contract status

Work out, for each lookup and action the screen will need, whether its contract exists:

- **Exists**: the endpoint is in the code, or the backend sibling's `## Contract` section
  describes it (not just the placeholder saying it will be posted). Design against it as is.
- **Partly exists**: some routes exist. Only the missing ones are drafted.
- **Missing**: no endpoint and no contract. Say so in step 2's Contract line; the contract is
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

### 2. Draft the use case and ask

Send one message:

```
## #<n> <title>: design, round 1

Contract: <exists | partly: … | missing | no backend issue>

Use case: <goal>
<main success scenario and extensions, see "Use cases" below>

Questions
- Q1 <question> Suggested: <answer>, because <one line>
```

The use case is the first question: a draft scenario the expert can react to beats abstract
questions. Keep it rough; it's there to be corrected.

**Quick mockups, only when they help.** When a question is best answered by looking at the
screen, such as layout, what appears together, or how a state looks, include a quick mockup
of just that part, in any round. It can be rough, in whatever form is quickest to read, or it
can be the start of the design's mockup (step 4), republished to the same link as answers
come in. Only the approved mockup goes in the issue. Don't add one by default.

#### Use cases

The issue won't have use cases; writing them is this skill's job. Make each only as formal as
it needs to be: when a user-story sentence and a few steps say everything, stop there.

- **One per goal** the user has on the screen. Most UI issues have one.
- **Goal and primary actor** from the issue: the goal as a short verb phrase ("Add a SKU"),
  the actor as the issue names them ("receiving operator or support engineer").
- **Main success scenario:** numbered steps, each what the user does or what Throughline does
  in response, in business terms. Say "chooses the owner", not "picks from the dropdown":
  the mockup shows the controls.
- **Extensions:** each is keyed to the step where it happens (`3a`, `3b`) and gives the
  condition, what the user is shown and can do, and where it rejoins the main scenario or
  that the use case ends there. Refusals, missing access, nothing to choose from and failing
  to reach the API are extensions. Where `docs/conventions.md` already says what happens
  (401, 403, failing to reach the API), the extension follows it and says so in a line.
- **Decide the screen's behaviour yourself.** What the screen does next is yours to design:
  what it keeps or clears after a success, what's chosen already, what's greyed out until
  something is chosen. Write it into the step where it happens, for example "8. The screen
  confirms the SKU with its code as stored and the owner, then clears the form and keeps the
  owner chosen [A1]." Don't turn it into a question.
- A screen detail, such as sort order or which fields are required, shows in the mockup or in
  "Fields and validation". There's no separate list of assumptions: anything you chose rather
  than read is marked `[A<n>]` where it appears, and you go ahead on it unless the expert says
  no.

Rules for questions:

- Ask only what needs the expert's knowledge of the business and changes what the user sees,
  does or is told: goals, workflow, business rules, exceptions, relevant nonfunctional needs
  (for example response time a user will notice, or who may see what). If you can decide it
  as the designer, decide it in the use case instead.
- Never ask what the issue, its comments, the code, the docs or the warehouse profile already
  answer.
- At most 5 questions per round, numbered across rounds, each with a suggested answer, so
  "accept the suggestions" or "Q3: no" is a complete reply.
- Stay in business terms. Don't invent actors, screens or usage the issue doesn't mention; if
  a question rests on an assumption, say so.

### 3. Iterate

After each reply, fold the answers into the design and re-show only what
changed: the affected steps, extensions or rows, not the whole design. Ask again only about
what's still open, with a quick mockup only where it helps answer.

Stop asking when:

- every UI acceptance criterion maps to a use case step or extension, and to a screen state,
- every extension has a screen state, and every state the user can reach is shown or
  listed, and
- what's still unknown doesn't change what the user sees or does.

Don't chase completeness past that point; leftovers go under "Not settled".

### 4. Draft

Send everything in one message: the mockup link, the design, the contract if one is needed,
and the validation result.

#### Mockup

The screens and their states are documented as one clickable mockup page, published with the
Artifact tool and linked from the issue's `## Design` section. GitHub can't show an HTML page
in an issue, so the link is what the issue carries; there are no ASCII wireframes and no
States table in the issue.

- **A state switcher above the screen**: one button per state the user can reach (first
  load, loading, empty, no access, refused, succeeded, failed to reach the API …), with a
  "When:" line saying when that state happens. Each state has its own link (`#<state>`). With
  more than one screen, each screen's states are entries in the same switcher.
- **The screen, drawn like the real app.** If the web app exists, copy its look: its header,
  its signed-in bar, its theme colours (read them from the UI code). If it doesn't, keep it
  plain and neutral.
- **On-screen text exactly as designed.** Labels, help text, banners and messages are the
  design's wording.
- **Example data, marked as examples.** Use the canonical owners and the issue's real values
  (codes, product lines, quantities). API error text appears only as a marked example
  ("e.g. …"), since error messages are the API's.
- **A note at the foot of the page:** "The mockup shows what's on the screen, the wording and
  each state. Its colours, sizes and spacing are only illustration."
- **One URL for the life of the issue.** Within a session, republish the same file to update
  it. If the issue's Design section already links a mockup, read it with the Artifact tool and
  republish to that URL instead of making a new one.
- The page is private to the user until they share it. Say so when you send the link.

#### Design

Replaces the issue's `## Design` section. Include a subsection only if it adds something:

```
## Design

[Open the <screen name> mockup](<mockup URL>): a clickable page with a switcher for each screen state.

The mockup shows what's on the screen, the wording and each state. Its colours, sizes and spacing are only illustration.

### Use cases
<one per goal: goal, primary actor, main success scenario, extensions>

### Flow                  (only with more than one screen or real navigation)
<screen → action → screen, as a short arrow list>

### Fields and validation
| Field | Required | Rule | Error shown | Source |

### Traceability
| Acceptance criterion | Covered by (use case step or extension, mockup state) |

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
- The issue's user story, where it has one, is the use case's goal. Don't restate it
  separately.

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
- The design follows **Web app screens** in `docs/conventions.md`: no business rule checked
  on the screen, 401 and 403 left to the app-wide pages, and failures to reach the API shown
  with the convention's placement and wording. A screen that needs to depart from it is a
  proposed scope change, not a quiet exception.
- Every extension has a state in the mockup's switcher, every state the user can reach is
  there with its "When:" line, and the mockup's wording matches the design's.
- Every screen action that reads or changes data has a route in the contract, and every error
  a route can return has a place on screen.
- The design and the contract use the same names for the same things, as do the issue and
  the code.
- Each owner's data stays scoped to that owner, and access matches the issue.
- No implementation detail: no components, libraries, styling or state management in the
  written design. The mockup carries its illustration note.
- Every assumption is labelled, and cites its source where it has one.

## 5. Approve

End the draft message by asking for approval of everything shown, listing each GitHub change
it would make (the design on #$issue, the contract on #<backend>, criteria edits on #$issue).

- Approval covers exactly the drafts shown, the mockup included. If anything changes
  afterwards, republish or show the changed draft and ask again.
- A reply that asks for changes is not approval.

## 6. Apply (only after approval)

Pass bodies on stdin with `--body-file -` and a heredoc; never write a temp file in the repo.
Edit only the approved sections; keep the rest of each body exactly as it is.

1. Re-read each body (`gh issue view <n> --json body`). If it changed since you drafted, stop,
   show what changed, and ask how to continue.
2. If there's a contract, update the backend issue first: `gh issue edit <backend> --body-file -`.
3. Update #$issue: its `## Design` section, with the mockup link in place of any placeholder
   or earlier wireframes, and any approved criteria edits. Don't post the mockup as a comment.
4. Report each updated issue as a link.

If a step fails, stop. Say what was done and what wasn't, and ask how to continue. Don't retry
blindly.
