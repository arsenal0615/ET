# Testing Anti-Patterns

**Load this reference when:** writing or changing tests, adding mocks, or tempted to add test-only methods to production code.

## Overview

Tests must verify real behavior, not mock behavior. Mocks are a means to isolate, not the thing being tested.

**Core principle:** Test what the code does, not what the mocks do.

**Following strict TDD prevents these anti-patterns.**

## The Iron Laws

```
1. NEVER test mock behavior
2. NEVER add test-only methods to production classes
3. NEVER mock without understanding dependencies
4. NEVER bypass object lifecycle in tests
5. NEVER test methods that shouldn't exist on data classes
```

## Anti-Pattern 1: Testing Mock Behavior

**The violation:**
```csharp
[Test]
public void SendMessage_ShouldWork()
{
    var mockSession = new Mock<ISession>();
    mockSession.Setup(s => s.Send(It.IsAny<IMessage>())).Returns(true);

    var result = mockSession.Object.Send(new LoginRequest());

    // BAD: only verifying mock works as configured
    mockSession.Verify(s => s.Send(It.IsAny<IMessage>()), Times.Once);
    Assert.IsTrue(result); // this true is what WE set up
}
```

**Why this is wrong:**
- You're verifying the mock works, not that the component works
- Test passes when mock is present, fails when it's not
- Tells you nothing about real behavior

**The fix:**
```csharp
[Test]
public void LoginHandler_ShouldSendResponseToClient()
{
    var handler = new LoginHandler();
    var session = CreateTestSession();
    var request = new LoginRequest { Account = "test", Password = "123" };
    var response = new LoginResponse();

    handler.Run(session, request, response);

    // GOOD: verify real behavior - response correctly populated
    Assert.AreEqual(0, response.Error);
    Assert.IsNotEmpty(response.Key);
}
```

### Gate Function
```
BEFORE asserting on any mock element:
  Ask: "Am I testing real component behavior or just mock existence?"
  IF testing mock existence → STOP — Delete the assertion or unmock the component
  Test real behavior instead
```

## Anti-Pattern 2: Test-Only Methods in Production

**The violation:**
```csharp
// In production code
public class HealthComponent
{
    private int _hp;

    // BAD: this method exists only for tests
    public void ResetForTesting()
    {
        this._hp = 0;
    }
}
```

**Why this is wrong:**
- Production class polluted with test-only code
- Dangerous if accidentally called in production
- Violates YAGNI and separation of concerns
- May violate framework rules (e.g., data classes should not have methods)

**The fix:**
```csharp
// In test utilities — use the normal lifecycle
[Test]
public void HealthComponent_AfterReset_ShouldHaveDefaultValues()
{
    var scene = CreateTestScene();
    var character = scene.CreateChild<Character>();

    // Reset by removing and re-adding (normal lifecycle)
    character.RemoveComponent<HealthComponent>();
    var health = character.AddComponent<HealthComponent>();

    Assert.AreEqual(0, health.Hp);
}
```

### Gate Function
```
BEFORE adding any method to production class:
  Ask: "Is this only used by tests?"
  IF yes → STOP — Don't add it. Put it in test utilities instead.
  Ask: "Does this class own this resource's lifecycle?"
  IF no → STOP — Wrong class for this method
```

## Anti-Pattern 3: Mocking Without Understanding

**The violation:**
```csharp
[Test]
public void MoveComponent_Update_ShouldMoveCharacter()
{
    var mockMove = new Mock<IMoveComponent>();
    // BAD: mocking the thing we're testing
    mockMove.Setup(m => m.Speed).Returns(5.0f);

    MoveSystem.Update(mockMove.Object, 0.1f);

    // What are we even testing here?
    mockMove.Verify();
}
```

**Why this is wrong:**
- Mocked method had side effects test depended on
- Over-mocking to "be safe" breaks actual behavior
- Test passes for wrong reason or fails mysteriously

**The fix:**
```csharp
[Test]
public void MoveSystem_Update_ShouldAdvancePosition()
{
    var scene = CreateTestScene();
    var character = scene.CreateChild<Character>();
    var move = character.AddComponent<MoveComponent>();

    // Set up REAL state
    move.SetPath(new Vector3[] { Vector3.zero, new Vector3(10, 0, 0) });
    move.Speed = 5.0f;

    // Call REAL method
    MoveSystem.Update(move, 1.0f);

    // Verify REAL result
    Assert.AreNotEqual(Vector3.zero, character.Position);
}
```

### Gate Function
```
BEFORE mocking any method:
  STOP — Don't mock yet

  1. Ask: "What side effects does the real method have?"
  2. Ask: "Does this test depend on any of those side effects?"
  3. Ask: "Do I fully understand what this test needs?"

  IF depends on side effects → Mock at lower level
  IF unsure → Run test with real implementation FIRST, observe, THEN add minimal mocking
```

## Anti-Pattern 4: Incomplete Mocks

**The violation:**
```csharp
var mockAOI = new Mock<IAOIComponent>();
// Only mock partial methods, others return defaults (null/0/false)
mockAOI.Setup(a => a.GetUnitsInRange(It.IsAny<float>()))
       .Returns(new List<Character>());
// Missing: GetUnitCount, IsInRange, etc. that downstream code uses
```

**Why this is wrong:**
- Partial mocks hide structural assumptions
- Downstream code may depend on fields you didn't include
- Tests pass but integration fails
- False confidence

**The fix:** Mock the COMPLETE data structure as it exists in reality, or better yet, use real components in a test scene.

### Gate Function
```
BEFORE creating mock responses:
  Check: "What fields does the real API/component expose?"
  Include ALL fields system might consume downstream
  If uncertain → Use real components instead of mocks
```

## Anti-Pattern 5: Integration Tests as Afterthought

**The violation:**
```
Implementation complete ✓
No tests written ✗
"Ready for testing"
```

**Why this is wrong:**
- Testing is part of implementation, not optional follow-up
- TDD would have caught this
- Can't claim complete without tests

**The fix:**
```
TDD cycle:
1. Write failing test
2. Implement to pass
3. Refactor
4. THEN claim complete
```

## Anti-Pattern 6: Testing Methods on Data-Only Classes

**The violation:**
```csharp
// WRONG: Data class with logic methods
public class HealthComponent
{
    private int _hp;

    // BAD: If framework separates data from logic,
    // data classes should NOT have methods
    public void TakeDamage(int damage)
    {
        _hp -= damage;
    }
}

// WRONG test
[Test]
public void HealthComponent_TakeDamage_ShouldReduceHP()
{
    var health = new HealthComponent();
    health.TakeDamage(10);
}
```

**Why this is wrong:**
- Many frameworks enforce data/logic separation (data classes must not have methods)
- All logic belongs in separate System/Service classes
- Compiler/analyzer may reject this at compile time
- Check CLAUDE.md for your project's data/logic separation rules

**The fix:**
```csharp
// Correct: Data class — data only
public class HealthComponent
{
    public int Hp;
    public int MaxHp;
}

// Correct: Logic in separate System class
public static class HealthSystem
{
    public static void TakeDamage(HealthComponent self, int damage)
    {
        self.Hp = Math.Max(0, self.Hp - damage);
    }
}

// Correct test — tests the System method
[Test]
public void HealthSystem_TakeDamage_ShouldReduceHP()
{
    var scene = CreateTestScene();
    var character = scene.CreateChild<Character>();
    var health = character.AddComponent<HealthComponent>();
    health.Hp = 100;
    health.MaxHp = 100;

    HealthSystem.TakeDamage(health, 10);

    Assert.AreEqual(90, health.Hp);
}
```

### Gate Function
```
BEFORE testing any data class behavior:
  Ask: "Am I calling a method ON a data class, or on a System/Service?"
  IF on data class → STOP — Check CLAUDE.md for data/logic separation rules
  Move logic to the correct location, test there
```

## Anti-Pattern 7: Bypassing Object Lifecycle in Tests

**The violation:**
```csharp
[Test]
public void MoveComponent_ShouldInitializeCorrectly()
{
    var move = new MoveComponent(); // BAD: bypasses lifecycle
    move.Speed = 5.0f;
    Assert.AreEqual(5.0f, move.Speed);
}
```

**Why this is wrong:**
- Many frameworks use object pools or factories for lifecycle management
- Direct `new` bypasses initialization/destruction lifecycle hooks
- Compiler/analyzer may report an error
- Test results don't represent real runtime behavior
- Check CLAUDE.md for your project's object creation rules

**The fix:**
```csharp
[Test]
public void MoveComponent_Init_ShouldSetDefaultSpeed()
{
    var scene = CreateTestScene();
    var character = scene.CreateChild<Character>();
    var move = character.AddComponent<MoveComponent>(); // Correct: lifecycle-managed

    // Initialization has been properly called
    Assert.AreEqual(MoveComponent.DefaultSpeed, move.Speed);
}
```

### Gate Function
```
BEFORE creating components/entities in tests:
  Ask: "Am I using `new XxxComponent()` directly?"
  IF yes → STOP — Check CLAUDE.md for the correct creation pattern
  Use the framework's designated creation method instead
```

## Quick Reference

| Anti-Pattern | Fix |
|--------------|-----|
| Assert on mock elements | Test real component or unmock it |
| Test-only methods in production | Move to test utilities |
| Mock without understanding | Understand dependencies first, mock minimally |
| Incomplete mocks | Mirror real API completely, or use real components |
| Tests as afterthought | TDD — tests first |
| Testing methods on data classes | Move logic to System/Service class, test there |
| Bypassing object lifecycle | Use framework's creation method |

## Red Flags

- Assertion checks for mock test IDs
- Methods only called in test files
- Mock setup is >50% of test
- Test fails when you remove mock
- Can't explain why mock is needed
- Mocking "just to be safe"
- Direct `new` on framework-managed types in test code
- Testing instance methods on data-only classes
- Using wrong async primitives in async tests

## The Bottom Line

**Mocks are tools to isolate, not things to test.**

If TDD reveals you're testing mock behavior, you've gone wrong.

Fix: Test real behavior or question why you're mocking at all.
