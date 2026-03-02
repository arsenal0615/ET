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
4. NEVER use `new` on Entity types in tests
5. NEVER test methods directly on Entity classes
```

## Anti-Pattern 1: Testing Mock Behavior

**The violation:**
```csharp
[Test]
public void SendMessage_ShouldWork()
{
    var mockSession = new Mock<Session>();
    mockSession.Setup(s => s.Send(It.IsAny<IMessage>())).Returns(true);

    var result = mockSession.Object.Send(new C2G_Login());

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
    var handler = new C2G_LoginHandler();
    var session = CreateTestSession();
    var request = new C2G_Login { Account = "test", Password = "123" };
    var response = new G2C_Login();

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
// In production code (Model assembly)
[ComponentOf(typeof(Unit))]
public class NumericComponent : Entity, IAwake
{
    public Dictionary<int, float> numericDic;

    // BAD: this method exists only for tests
    public void ResetForTesting()
    {
        this.numericDic.Clear();
    }
}
```

**Why this is wrong:**
- Production class polluted with test-only code
- Dangerous if accidentally called in production
- Violates YAGNI and separation of concerns
- Also violates ET rule: Entity classes should not have methods

**The fix:**
```csharp
// In test utilities
[Test]
public void NumericComponent_AfterReset_ShouldHaveDefaultValues()
{
    var scene = CreateTestScene();
    var unit = scene.AddChild<Unit>();

    // Reset by removing and re-adding (normal lifecycle)
    unit.RemoveComponent<NumericComponent>();
    var numeric = unit.AddComponent<NumericComponent>();

    Assert.AreEqual(0, numeric.GetAsInt(NumericType.Hp));
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
public void MoveComponent_Update_ShouldMoveUnit()
{
    var mockMove = new Mock<MoveComponent>();
    // BAD: mocking the thing we're testing
    mockMove.Setup(m => m.Speed).Returns(5.0f);

    MoveComponentSystem.Update(mockMove.Object, 0.1f);

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
public void MoveComponent_Update_ShouldAdvancePosition()
{
    var scene = CreateTestScene();
    var unit = scene.AddChild<Unit>();
    var move = unit.AddComponent<MoveComponent>();

    // Set up REAL state
    move.SetPath(new float3[] { float3.zero, new float3(10, 0, 0) });
    move.Speed = 5.0f;

    // Call REAL method
    MoveComponentSystem.Update(move, 1.0f);

    // Verify REAL result
    Assert.AreNotEqual(float3.zero, unit.Position);
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
var mockAOI = new Mock<AOIComponent>();
// Only mock partial methods, others return defaults (null/0/false)
mockAOI.Setup(a => a.GetUnitsInRange(It.IsAny<float>()))
       .Returns(new List<Unit>());
// Missing: GetUnitCount, IsInRange, etc. that downstream code uses
```

**Why this is wrong:**
- Partial mocks hide structural assumptions
- Downstream code may depend on fields you didn't include
- Tests pass but integration fails
- False confidence

**The fix:** Mock the COMPLETE data structure as it exists in reality, or better yet, use real components in a test Scene.

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

## Anti-Pattern 6: Testing Entity Methods Directly (ET-Specific)

**The violation:**
```csharp
// WRONG Entity definition (violates ET rules)
public class HealthComponent : Entity, IAwake
{
    private int _hp;

    // BAD: Entity should NOT have methods
    public void TakeDamage(int damage)
    {
        _hp -= damage;
    }
}

// WRONG test
[Test]
public void HealthComponent_TakeDamage_ShouldReduceHP()
{
    var health = new HealthComponent(); // BAD: can't new Entity
    health.TakeDamage(10);             // BAD: Entity shouldn't have methods
}
```

**Why this is wrong:**
- Violates ET core rule: Entity classes must not have methods
- All logic belongs in System static extension methods in Hotfix assembly
- Analyzer will reject this at compile time

**The fix:**
```csharp
// Correct Entity (Model assembly) - data only
[ComponentOf(typeof(Unit))]
public class HealthComponent : Entity, IAwake
{
    public int Hp;
    public int MaxHp;
}

// Correct System (Hotfix assembly) - logic here
[EntitySystemOf(typeof(HealthComponent))]
[FriendOf(typeof(HealthComponent))]
public static partial class HealthComponentSystem
{
    public static void TakeDamage(this HealthComponent self, int damage)
    {
        self.Hp = Math.Max(0, self.Hp - damage);
    }
}

// Correct test - tests the System extension method
[Test]
public void HealthComponentSystem_TakeDamage_ShouldReduceHP()
{
    var scene = CreateTestScene();
    var unit = scene.AddChild<Unit>();
    var health = unit.AddComponent<HealthComponent>();
    health.Hp = 100;
    health.MaxHp = 100;

    health.TakeDamage(10); // This calls the System extension method

    Assert.AreEqual(90, health.Hp);
}
```

### Gate Function
```
BEFORE testing any Entity behavior:
  Ask: "Am I calling a method ON the Entity class, or on a System extension?"
  IF on Entity class → STOP — Move logic to System class, test there
```

## Anti-Pattern 7: `new` Entity in Tests (ET-Specific)

**The violation:**
```csharp
[Test]
public void MoveComponent_ShouldInitializeCorrectly()
{
    var move = new MoveComponent(); // BAD: Entity cannot be new'd
    move.Speed = 5.0f;
    Assert.AreEqual(5.0f, move.Speed);
}
```

**Why this is wrong:**
- ET uses object pool for Entity lifecycle management
- Direct `new` bypasses Awake/Destroy lifecycle hooks
- Compiler Analyzer will report error (ET Rule 8)
- Test results don't represent real runtime behavior

**The fix:**
```csharp
[Test]
public void MoveComponent_Awake_ShouldSetDefaultSpeed()
{
    var scene = CreateTestScene();
    var unit = scene.AddChild<Unit>();
    var move = unit.AddComponent<MoveComponent>(); // Correct: pool-managed

    // Awake has been properly called
    Assert.AreEqual(MoveComponent.DefaultSpeed, move.Speed);
}
```

### Gate Function
```
BEFORE creating Entity/Component in tests:
  Ask: "Am I using `new XxxComponent()` or `new XxxEntity()`?"
  IF yes → STOP — Change to AddComponent<T>() / AddChild<T>()
```

## Quick Reference

| Anti-Pattern | Fix |
|--------------|-----|
| Assert on mock elements | Test real component or unmock it |
| Test-only methods in production | Move to test utilities |
| Mock without understanding | Understand dependencies first, mock minimally |
| Incomplete mocks | Mirror real API completely, or use real components |
| Tests as afterthought | TDD — tests first |
| Testing Entity methods | Move logic to System class, test there |
| `new` Entity in tests | Use AddComponent/AddChild |

## Red Flags

- Assertion checks for mock test IDs
- Methods only called in test files
- Mock setup is >50% of test
- Test fails when you remove mock
- Can't explain why mock is needed
- Mocking "just to be safe"
- `new XxxComponent()` or `new XxxEntity()` in test code
- Testing instance methods on Entity classes
- Using `Task` or `async void` instead of `ETTask` in async tests

## The Bottom Line

**Mocks are tools to isolate, not things to test.**

If TDD reveals you're testing mock behavior, you've gone wrong.

Fix: Test real behavior or question why you're mocking at all.
