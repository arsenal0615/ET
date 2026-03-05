---
name: qx-tdd
description: "在实现任何功能或修复 Bug 之前使用，先于编写实现代码。强制执行测试驱动开发纪律。"
---

# 测试驱动开发（TDD）

## 概述

先写测试。看着它失败。写最少的代码让它通过。

**核心原则：** 如果你没有看着测试失败，你就不知道它是否在测试正确的事情。

**违反规则的字面意思就是违反规则的精神。**

## 适用场景

**始终使用：**
- 新功能
- Bug 修复
- 重构（Refactoring）
- 行为变更

**例外（需征得你的人类伙伴同意）：**
- 一次性原型
- 生成的代码
- 配置文件

想着"就这一次跳过 TDD"？停下。那是在自我合理化。

## 铁律

```
没有先失败的测试，就不写生产代码
```

先写了代码再写测试？删掉。重新开始。

**没有例外：**
- 不要留作"参考"
- 不要在写测试时"改编"它
- 不要看它
- 删除就是删除

从测试出发，全新实现。句号。

## 红-绿-重构（Red-Green-Refactor）

```
RED → 验证 RED → GREEN → 验证 GREEN → REFACTOR → 重复
```

### RED - 编写失败的测试

编写一个最小的测试，展示预期行为。

<Good>
```csharp
[Test]
public void HealthSystem_Damage_ShouldNotExceedMaxHP()
{
    var unit = CreateTestUnit();
    SetMaxHP(unit, 100);

    ApplyDamage(unit, 150); // 超过最大值

    int actualHp = GetHP(unit);
    Assert.LessOrEqual(actualHp, 100, "HP should not exceed MaxHP");
}
```
名称清晰，测试真实行为，只测一件事
</Good>

<Bad>
```csharp
[Test]
public void TestHealth()
{
    var mock = new Mock<IHealthSystem>();
    mock.Setup(n => n.GetHP()).Returns(100);
    Assert.AreEqual(100, mock.Object.GetHP());
}
```
名称模糊，测试的是 mock 而非真实代码
</Bad>

**要求：**
- 只测一个行为
- 名称清晰
- 使用真实代码（除非不可避免才用 mock）

### 验证 RED - 看着它失败

**强制步骤。绝不跳过。**

```bash
# 运行指定测试（根据项目调整命令）
dotnet test --filter "HealthSystem_Damage_ShouldNotExceedMaxHP"
```

确认：
- 测试失败（不是报错）
- 失败信息符合预期
- 因为功能缺失而失败（不是拼写错误）

**测试通过了？** 你在测试已有行为。修正测试。

**测试报错了？** 修复错误，重新运行直到正确失败。

### GREEN - 最少代码

写最简单的代码让测试通过。

<Good>
```csharp
public static void ApplyDamage(Unit unit, int damage)
{
    int maxHp = GetMaxHP(unit);
    int newHp = GetHP(unit) - damage;
    if (maxHp > 0) newHp = Math.Max(0, Math.Min(newHp, maxHp));
    SetHP(unit, newHp);
}
```
刚好够让测试通过
</Good>

<Bad>
```csharp
public static void ApplyDamage(Unit unit, int damage,
    bool clamp = true, float? overrideMax = null,
    Action<int> onChanged = null)
{
    // YAGNI（你不会需要它的）
}
```
过度设计
</Bad>

不要添加功能、重构其他代码，或做超出测试要求的"改进"。

### 验证 GREEN - 看着它通过

**强制步骤。**

```bash
dotnet test
```

确认：
- 测试通过
- 其他测试仍然通过
- 输出干净（无错误、无警告）

**测试失败了？** 修复代码，不是测试。

**其他测试失败了？** 立即修复。

### REFACTOR - 清理

仅在 GREEN 之后：
- 消除重复
- 改进命名
- 提取辅助方法

保持测试为绿。不添加行为。

### 重复

下一个失败的测试，对应下一个功能。

## 好的测试

| 质量 | 好的 | 差的 |
|------|------|------|
| **最小化** | 只测一件事。名称中有 "and"？拆分它。 | `Test_ValidatesEmailAndDomainAndWhitespace` |
| **清晰** | 名称描述行为 | `Test1` |
| **展示意图** | 展示期望的 API 用法 | 模糊了代码应该做什么 |

## 为什么顺序很重要

**"我之后写测试来验证它能工作"**

代码写完后写的测试会立即通过。立即通过什么也证明不了：
- 可能测的是错误的东西
- 可能测的是实现而非行为
- 可能遗漏了你忘记的边界情况
- 你从未看到它捕获过 Bug

先写测试迫使你看到测试失败，证明它确实在测试某些东西。

**"删掉 X 小时的工作太浪费了"**

沉没成本谬误（Sunk cost fallacy）。时间已经花了。你现在的选择：
- 删除并用 TDD 重写（再花 X 小时，高可信度）
- 保留并事后补测试（30 分钟，低可信度，大概率有 Bug）

真正的"浪费"是保留你无法信任的代码。

**"TDD 太教条了，务实就是要灵活"**

TDD 本身就是务实的：
- 提交前发现 Bug（比提交后调试更快）
- 防止回归（测试立即捕获破坏）
- 记录行为（测试展示如何使用代码）
- 支持重构（放心修改，测试捕获破坏）

## 常见自我合理化

| 借口 | 现实 |
|------|------|
| "太简单了不需要测试" | 简单的代码也会出错。测试只需 30 秒。 |
| "我之后再测" | 立即通过的测试什么也证明不了。 |
| "事后补测试也能达到同样目的" | 事后补测试 = "这段代码做了什么？" 先写测试 = "这段代码应该做什么？" |
| "我已经手动测过了" | 临时测试不等于系统测试。没有记录，无法重复运行。 |
| "删掉 X 小时的工作太浪费了" | 沉没成本谬误。保留未验证的代码是技术债务。 |
| "留作参考，测试还是先写" | 你会去改编它的。那就是事后补测试。删除就是删除。 |
| "我需要先探索一下" | 可以。扔掉探索成果，然后用 TDD 开始。 |
| "测试难写 = 设计不清晰" | 倾听测试。难以测试 = 难以使用。 |
| "TDD 会拖慢我" | TDD 比调试更快。务实 = 先写测试。 |
| "手动测试更快" | 手动测试无法证明边界情况。每次改动你都得重新测。 |
| "现有代码没有测试" | 你在改进它。为现有代码添加测试。 |

## 红线 - 停下来，重新开始

- 先写代码再写测试
- 实现之后才写测试
- 测试立即通过
- 无法解释测试为什么失败
- "之后再"添加测试
- 自我合理化"就这一次"
- "我已经手动测过了"
- "事后补测试也能达到同样目的"
- "留作参考"或"改编现有代码"
- "已经花了 X 小时，删掉太浪费"
- "TDD 太教条了，我在务实"
- "这次情况不一样，因为..."

**以上所有情况都意味着：删除代码。用 TDD 重新开始。**

## 项目特定的测试说明

> **重要：** 阅读项目的 CLAUDE.md 和 MEMORY.md 了解框架特定的测试约束、约定和命令。每个项目都有自己的规则，包括：
> - 如何创建测试对象（对象池、工厂等）
> - 异步测试模式（项目特定的异步框架）
> - 逻辑归属（数据与逻辑分离模式）
> - 构建/测试命令和验证步骤

## 示例：Bug 修复

**Bug：** 账号注册时接受了空邮箱

**RED**
```csharp
[Test]
public void AccountValidator_RejectsEmptyAccount()
{
    var result = AccountHelper.ValidateAccount("");
    Assert.AreEqual("Account required", result.Error);
}
```

**验证 RED**
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
    // ...现有逻辑
}
```

**验证 GREEN**
```
PASS
```

**REFACTOR**
如有需要，提取多字段的验证逻辑。

## 验证清单

在标记工作完成之前：

- [ ] 每个新函数/方法都有测试
- [ ] 在实现之前看着每个测试失败
- [ ] 每个测试因预期原因失败（功能缺失，非拼写错误）
- [ ] 为每个测试写了最少的通过代码
- [ ] 所有测试通过
- [ ] 输出干净（无错误、无警告）
- [ ] 测试使用真实代码（仅在不可避免时使用 mock）
- [ ] 边界情况和错误情况已覆盖

不能全部勾选？你跳过了 TDD。重新开始。

## 卡住时怎么办

| 问题 | 解决方案 |
|------|----------|
| 不知道怎么测试 | 写出你期望的 API。先写断言。问你的人类伙伴。 |
| 测试太复杂 | 设计太复杂。简化接口。 |
| 必须 mock 所有东西 | 代码耦合太重。使用依赖注入（Dependency Injection）。 |
| 测试准备工作太庞大 | 提取辅助方法。还是复杂？简化设计。 |

## 调试集成

发现 Bug？写一个能复现它的失败测试。遵循 TDD 循环。测试证明修复有效并防止回归。

绝不在没有测试的情况下修复 Bug。

## 测试反模式（Anti-Patterns）

在添加 mock 或测试工具时，阅读 @testing-anti-patterns.md 以避免常见陷阱：
- 测试 mock 行为而非真实行为
- 在生产类中添加仅供测试用的方法
- 不理解依赖关系就使用 mock
- 创建绕过正常生命周期的测试对象
- 直接测试数据类方法而非逻辑模块的扩展方法

## 最终规则

```
生产代码 → 必须有先失败的测试
否则 → 不是 TDD
```

没有你的人类伙伴的许可，不设例外。
