---
name: write-tdd-tests
description: Write TDD-style unit/integration tests for a C# class or method in this repo, following the project's test conventions. Use when the user asks to write tests test-first, or invokes /write-tdd-tests.
---

# write-tdd-tests

Write tests for a C# SUT **as if its implementation is still a stub** — the tests define
the expected behavior, not the current code.

## Steps

1. **Ask which SUT.** If the user didn't name one, ask for the class or method to test.

2. **Load conventions.** Follow the test conventions in `claude/testing.md` (imported via
   `claude/CLAUDE.md`) exactly — class/naming/namespace rules, Moq usage, `Assert.That` +
   `Assert.Multiple`, `_sut` field, regions.

3. **Derive expected behavior from documentation, not the implementation.**
   - If the SUT implements an interface, read that interface's XML doc comments
     (`<summary>`/`<remarks>`/`<param>`) and treat them as the behavioral spec.
   - Otherwise use XML docs on the SUT itself.
   - **Do NOT read the method body to decide what to assert** — treat the implementation
     as a stub. The tests describe what it *should* do.
   - If documentation is missing or too thin to define expected behavior, **stop and ask
     the user to explain what the SUT does** before writing anything.

4. **Add only missing tests.** Read any existing test file for the SUT first. Never
   rewrite, replace, or edit existing tests — append only the cases not already covered.

5. **Verify they compile,** then run them. Because the implementation may be a stub, tests
   are allowed to fail — report which pass and which fail, so the user can drive the
   implementation to green.
