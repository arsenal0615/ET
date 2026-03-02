---
name: qx-tdd
description: "Use when implementing any feature or bugfix in the ET/Unity project, before writing implementation code. Enforces test-driven development discipline."
---

# Test-Driven Development (TDD)

## Overview

Write the test first. Watch it fail. Write minimal code to pass.

**Core principle:** If you didn't watch the test fail, you don't know if it tests the right thing.

**Violating the letter of the rules is violating the spirit of the rules.**

## When to Use

**Always:**
- New features
- Bug fixes
- Refactoring
- Behavior changes

**Exceptions (ask your human partner):**
- Throwaway prototypes
- Generated code
- Configuration files

Thinking "skip TDD just this once"? Stop. That's rationalization.

## The Iron Law

```
NO PRODUCTION CODE WITHOUT A FAILING TEST FIRST
```

Write code before the test? Delete it. Start over.

**No exceptions:**
- Don't keep it as "reference"
- Don't "adapt" it while writing tests
- Don't look at it
- Delete means delete

Implement fresh from tests. Period.

## Red-Green-Refactor

```
RED → Verify RED → GREEN → Verify GREEN → REFACTOR → Repeat
```

### RED - Write Failing Test

Write one minimal test showing what should happen.

<Good>
```csharp
[Test]
public void NumericComponent_HP_ShouldNotExceedMaxHP()
{
    var scene = CreateTestScene();
    var unit = scene.AddChild<Unit>();
    var numeric = unit.AddComponent<NumericComponent>();
    numeric.Set(NumericType.MaxHp, 100);

    numeric.Set(NumericType.Hp, 150); // exceeds max

    int actualHp = numeric.GetAsInt(NumericType.Hp);
    Assert.LessOrEqual(actualHp, 100, "HP should not exceed MaxHP");
}
```
Clear name, tests real behavior, one thing
</Good>

<Bad>
```csharp
[Test]
public void TestNumeric()
{
    var mock = new Mock<NumericComponent>();
    mock.Setup(n => n.GetAsInt(It.IsAny<int>())).Returns(100);
    Assert.AreEqual(100, mock.Object.GetAsInt(NumericType.Hp));
}
```
Vague name, tests mock not code
</Bad>

**Requirements:**
- One behavior
- Clear name
- Real code (no mocks unless unavoidable)

### Verify RED - Watch It Fail

**MANDATORY. Never skip.**

```bash
dotnet test --filter "NumericComponent_HP_ShouldNotExceedMaxHP"
```

Confirm:
- Test fails (not errors)
- Failure message is expected
- Fails because feature missing (not typos)

**Test passes?** You're testing existing behavior. Fix test.

**Test errors?** Fix error, re-run until it fails correctly.

### GREEN - Minimal Code

Write simplest code to pass the test.

<Good>
```csharp
public static void SetHP(this NumericComponent self, float value)
{
    float maxHp = self.GetAsFloat(NumericType.MaxHp);
    if (maxHp > 0) value = Math.Min(value, maxHp);
    self.Set(NumericType.Hp, value);
}
```
Just enough to pass
</Good>

<Bad>
```csharp
public static void SetHP(this NumericComponent self, float value,
    bool clamp = true, float? overrideMax = null,
    Action<float> onChanged = null)
{
    // YAGNI
}
```
Over-engineered
</Bad>

Don't add features, refactor other code, or "improve" beyond the test.

### Verify GREEN - Watch It Pass

**MANDATORY.**

```bash
dotnet test
```

Confirm:
- Test passes
- Other tests still pass
- Output pristine (no errors, warnings)

**Test fails?** Fix code, not test.

**Other tests fail?** Fix now.

### REFACTOR - Clean Up

After green only:
- Remove duplication
- Improve names
- Extract helpers

Keep tests green. Don't add behavior.

### Repeat

Next failing test for next feature.

## Good Tests

| Quality | Good | Bad |
|---------|------|-----|
| **Minimal** | One thing. "and" in name? Split it. | `Test_ValidatesEmailAndDomainAndWhitespace` |
| **Clear** | Name describes behavior | `Test1` |
| **Shows intent** | Demonstrates desired API | Obscures what code should do |

## Why Order Matters

**"I'll write tests after to verify it works"**

Tests written after code pass immediately. Passing immediately proves nothing:
- Might test wrong thing
- Might test implementation, not behavior
- Might miss edge cases you forgot
- You never saw it catch the bug

Test-first forces you to see the test fail, proving it actually tests something.

**"Deleting X hours of work is wasteful"**

Sunk cost fallacy. The time is already gone. Your choice now:
- Delete and rewrite with TDD (X more hours, high confidence)
- Keep it and add tests after (30 min, low confidence, likely bugs)

The "waste" is keeping code you can't trust.

**"TDD is dogmatic, being pragmatic means adapting"**

TDD IS pragmatic:
- Finds bugs before commit (faster than debugging after)
- Prevents regressions (tests catch breaks immediately)
- Documents behavior (tests show how to use code)
- Enables refactoring (change freely, tests catch breaks)

## Common Rationalizations

| Excuse | Reality |
|--------|---------|
| "Too simple to test" | Simple code breaks. Test takes 30 seconds. |
| "I'll test after" | Tests passing immediately prove nothing. |
| "Tests after achieve same goals" | Tests-after = "what does this do?" Tests-first = "what should this do?" |
| "Already manually tested" | Ad-hoc ≠ systematic. No record, can't re-run. |
| "Deleting X hours is wasteful" | Sunk cost fallacy. Keeping unverified code is technical debt. |
| "Keep as reference, write tests first" | You'll adapt it. That's testing after. Delete means delete. |
| "Need to explore first" | Fine. Throw away exploration, start with TDD. |
| "Test hard = design unclear" | Listen to test. Hard to test = hard to use. |
| "TDD will slow me down" | TDD faster than debugging. Pragmatic = test-first. |
| "Manual test faster" | Manual doesn't prove edge cases. You'll re-test every change. |
| "Existing code has no tests" | You're improving it. Add tests for existing code. |

## Red Flags - STOP and Start Over

- Code before test
- Test after implementation
- Test passes immediately
- Can't explain why test failed
- Tests added "later"
- Rationalizing "just this once"
- "I already manually tested it"
- "Tests after achieve the same purpose"
- "Keep as reference" or "adapt existing code"
- "Already spent X hours, deleting is wasteful"
- "TDD is dogmatic, I'm being pragmatic"
- "This is different because..."

**All of these mean: Delete code. Start over with TDD.**

## ET Framework Testing Notes

ET projects have specific testing constraints:

### Entity/Component Testing
```csharp
// Correct: create via AddComponent, test lifecycle in mock Scene
[Test]
public void XunLuoPathComponent_GetCurrent_ReturnsCorrectPath()
{
    var scene = CreateTestScene();
    var unit = scene.AddChild<Unit>();
    var path = unit.AddComponent<XunLuoPathComponent>();
    path.path = new float3[] { new float3(1, 0, 0), new float3(2, 0, 0) };
    path.Index = 1;

    float3 current = path.GetCurrent();

    Assert.AreEqual(new float3(2, 0, 0), current);
}
```

### Core Constraints
- **No `new` on Entity types** — Must use `AddComponent<T>()` / `AddChild<T>()` (object pool managed)
- **Async tests must use ETTask** — Never use `Task` or `async void`
- **Test System extension methods** — Entity classes must not have methods; test the static System class
- **NumericComponent formula validation** — Input KV pairs → verify computed results
- **Proto message testing** — Verify serialization/deserialization correctness
- **Config table testing** — Verify Excel-exported Config data loads correctly

### Test Commands
```bash
# Server-side unit tests
dotnet test

# Compilation check (catches Analyzer errors)
dotnet build ET.sln

# Unity Test Runner (client-side tests)
# Unity menu: Window > General > Test Runner

# Compile hot-update DLLs
# Unity shortcut: F6
```

## Example: Bug Fix

**Bug:** Empty email accepted in account registration

**RED**
```csharp
[Test]
public void AccountValidator_RejectsEmptyAccount()
{
    var result = AccountHelper.ValidateAccount("");
    Assert.AreEqual("Account required", result.Error);
}
```

**Verify RED**
```
Expected: "Account required"
But was: null
```

**GREEN**
```csharp
public static ValidateResult ValidateAccount(string account)
{
    if (string.IsNullOrWhiteSpace(account))
    {
        return new ValidateResult { Error = "Account required" };
    }
    // ...existing logic
}
```

**Verify GREEN**
```
PASS
```

**REFACTOR**
Extract validation for multiple fields if needed.

## Verification Checklist

Before marking work complete:

- [ ] Every new function/method has a test
- [ ] Watched each test fail before implementing
- [ ] Each test failed for expected reason (feature missing, not typo)
- [ ] Wrote minimal code to pass each test
- [ ] All tests pass
- [ ] Output pristine (no errors, warnings)
- [ ] Tests use real code (mocks only if unavoidable)
- [ ] Edge cases and errors covered

Can't check all boxes? You skipped TDD. Start over.

## When Stuck

| Problem | Solution |
|---------|----------|
| Don't know how to test | Write wished-for API. Write assertion first. Ask your human partner. |
| Test too complicated | Design too complicated. Simplify interface. |
| Must mock everything | Code too coupled. Use dependency injection. |
| Test setup huge | Extract helpers. Still complex? Simplify design. |

## Debugging Integration

Bug found? Write failing test reproducing it. Follow TDD cycle. Test proves fix and prevents regression.

Never fix bugs without a test.

## Testing Anti-Patterns

When adding mocks or test utilities, read @testing-anti-patterns.md to avoid common pitfalls:
- Testing mock behavior instead of real behavior
- Adding test-only methods to production classes
- Mocking without understanding dependencies
- `new` Entity in tests (must use AddComponent)
- Testing Entity methods directly (must test System extensions)

## Final Rule

```
Production code → test exists and failed first
Otherwise → not TDD
```

No exceptions without your human partner's permission.
