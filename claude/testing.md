# Test Conventions

Conventions for **C# unit and integration tests** in this repo. These are binding when
Claude writes, edits, or reviews tests. Stack: **NUnit 4** + **Moq**, targeting .NET 10.

---

## Test class

- **Always `sealed`.** No test class is meant to be inherited from.
- **Always carries a category** — exactly one of `[Category("Unit")]` or
  `[Category("Integration")]` on the class. This lets suites be filtered by
  `--filter "Category=Unit"`.

```csharp
[Category("Unit")]
public sealed class MoneyTests
{
    // ...
}
```

---

## Namespace

- The test file's namespace **mirrors the SUT's namespace**, differing only by the
  `.Tests` segment at the project boundary. Everything after it must match.

```csharp
// SUT:  namespace Throughline.Modules.Ordering.Domain.Pricing;   (Money)
// Test: namespace Throughline.Modules.Ordering.Domain.Tests.Pricing;   (MoneyTests)
```

The trailing sub-namespace (`.Pricing`) is identical on both sides — only `.Domain` gains
`.Tests` because the test lives in the `*.Domain.Tests` project.

---

## Naming

- Test method names follow **`MethodUnderTest_Condition_ExpectedResult`**.
  - `MethodUnderTest` — the member being exercised.
  - `Condition` — the state/input that distinguishes this case.
  - `ExpectedResult` — the observable outcome.
- Names **must not exceed 100 characters.** If a name wants to be longer, the case is
  probably doing too much — split it, or tighten the condition/result phrasing.

```csharp
FromRate_Integer_ReturnsExpected
Charge_WhenIdempotencyKeyReused_DoesNotDoubleCharge
```

---

## Structure

- **Arrange / Act / Assert.** Keep the three phases visually distinct.
- **When a method has more than 2 tests, wrap them in a `#region` named after the method
  under test** (see `MoneyTests.cs`). Methods with 2 or fewer tests need no region.
- **One logical behavior per test.** Parameterize input variations with `[TestCase]` /
  `[TestCaseSource]` rather than looping or branching inside a test.
- **No conditionals or loops in test bodies** that determine what is asserted — a test
  should have one deterministic path.

---

## System under test (SUT)

- The SUT is a **private `_sut` field**, constructed in a `[SetUp]` method (or field
  initializer) so every test starts from a fresh instance.
- **Exception:** if construction genuinely varies per test (different constructor
  arguments are the thing under test, or the type can't be built once up front), build
  the SUT locally in each test instead. Prefer `_sut` whenever the constructor is stable.

```csharp
[Category("Unit")]
public sealed class OrderEstimateServiceTests
{
    private Mock<IRateProvider> _rateProvider = null!;
    private OrderEstimateService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _rateProvider = new Mock<IRateProvider>(MockBehavior.Strict);
        _sut = new OrderEstimateService(_rateProvider.Object);
    }
}
```

---

## Mocking (Moq)

- Use **Moq** for all mocking.
- **`It.IsAny<T>()` is reserved for exceptional circumstances.** Verifying the *identity*
  of arguments passed to a dependency is a core purpose of these tests — assert on the
  actual expected value, or use `It.Is<T>(x => ...)` to pin the argument. A test that
  waves through inputs with `It.IsAny` proves almost nothing about the collaboration.
- Prefer `MockBehavior.Strict` so unexpected interactions fail loudly, unless a test
  specifically needs loose behavior.

```csharp
// Good — pins the argument identity
_rateProvider.Verify(p => p.GetRate(expectedLane), Times.Once);

// Avoid — proves nothing about which lane was requested
_rateProvider.Verify(p => p.GetRate(It.IsAny<Lane>()), Times.Once);
```

---

## Arrangement — no overridden defaults

- **`[SetUp]` constructs the mocks and the `_sut` — nothing else.** Do **not** put a
  default mock setup (or a shared default input, e.g. default command items) in `[SetUp]`
  that some tests then override.
- **If any test needs a different setup for a given mock than another test does, every
  test sets that mock up explicitly.** A default that some tests silently override hides
  what is actually in effect for a given test — the reader can't see the real arrangement
  without cross-referencing `[SetUp]`. Each test should be self-contained.
- Keep reusable **data** as `static readonly` fields (valid building blocks), but **wire
  it per test** — small `GivenX(...)` helpers each test calls, and inputs passed per test.

```csharp
// Good — each test declares its own world; SetUp only builds mocks + _sut
GivenPickFees(Sku1PickFee, Sku2PickFee);
GivenZones(Zone);

// Avoid — a SetUp default that half the tests override; the effective state is non-local
```

---

## Assertions

- Use **`Assert.That` (constraint syntax)** — not the classic `Assert.AreEqual` family.
- Wrap multiple related assertions in **`Assert.Multiple`** so a failure reports every
  broken expectation at once, not just the first.

```csharp
Assert.Multiple(() =>
{
    Assert.That(estimate.Total.Value, Is.EqualTo(expectedTotal));
    Assert.That(estimate.Currency, Is.EqualTo("USD"));
    Assert.That(estimate.Items, Has.Count.EqualTo(2));
});
```

---

## Quick checklist

- [ ] Class is `sealed` with `[Category("Unit")]` or `[Category("Integration")]`
- [ ] Method name is `MethodUnderTest_Condition_ExpectedResult`, ≤ 100 chars
- [ ] SUT is a private `_sut` (unless construction forbids it)
- [ ] `[SetUp]` builds only mocks + `_sut`; no default setup that tests override
- [ ] Moq used; `It.IsAny` only where argument identity genuinely doesn't matter
- [ ] `Assert.That` throughout; grouped assertions inside `Assert.Multiple`
