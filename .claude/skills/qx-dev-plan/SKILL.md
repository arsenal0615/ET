---
name: qx-dev-plan
description: "在有规格、设计或需求的多步骤任务开始编码之前使用。创建小粒度的 TDD 实施计划，具备项目框架感知能力。"
---

# 编写实施计划（Writing Plans）

## 概述

编写全面的实施计划，假设工程师对我们的代码库零上下文且审美存疑。记录他们需要知道的一切：每个任务要修改哪些文件、代码、测试、可能需要查阅的文档、如何测试。以小粒度任务形式给出完整计划。DRY（不重复）、YAGNI（不过度设计）、TDD（测试驱动）、频繁提交。

假设他们是有能力的开发者，但对项目框架和问题域几乎一无所知。假设他们不太擅长测试设计。

**启动时宣告：** "正在使用 qx-dev-plan 创建实施计划。"

## 变更上下文检测

编写计划前，检测是否在变更上下文中工作：

1. **检查对话上下文** — 用户是否提到了变更名称？你是否在 `docs/changes/<name>/` 中工作？
2. **检查显式路径** — 用户是否引用了 `docs/changes/` 中的文件？

**如果检测到变更上下文（Change Mode）：**
- 读取 `docs/changes/<name>/proposal.md`、`design.md` 和 `specs/` 作为输入上下文
- 计划保存到：`docs/changes/<name>/plan.md`
- 任务使用复选框格式（见下方 Change Mode 任务结构）

**如果没有变更上下文（Quick Mode）：**
- 计划保存到：`docs/plans/YYYY-MM-DD-<feature-name>.md`
- 使用原始任务格式（见下方任务结构）

## 项目上下文加载

**编写任何计划之前，读取项目的 CLAUDE.md 和 MEMORY.md 以了解：**
- 框架规则和编码约定
- 模块/程序集组织和放置规则
- 代码生成步骤（Proto 编译、配置导出等）
- 构建和验证命令
- 文件命名约定
- 关键模式和反模式

## 小粒度任务粒度

**每一步是一个动作（2-5 分钟）：**
- "编写失败测试" - 一步
- "运行确认失败" - 一步
- "实现最小代码使测试通过" - 一步
- "运行测试确认通过" - 一步
- "提交" - 一步

## 计划文档头部

**每个计划必须以此头部开始：**

```markdown
# [Feature Name] Implementation Plan

> **For Claude:** REQUIRED: Use `/qx-dev-exec` to implement this plan task-by-task.

**Goal:** [一句话描述要构建什么]

**Architecture:** [2-3 句话关于实现方法]

**Tech Stack:** [涉及的关键技术/模块]

**Impact:**
- Modules/Assemblies: [列出受影响的模块]
- Code generation changes: [是/否 — 如果是，需要哪些生成步骤]
- New data models: [列出，附带所有权声明]
- New messages/protocols: [列出类型]

**Rules:** [列出本计划涉及的 project-rules 文件名，2-4 个，不含路径前缀]
- 例：ecs-patterns, code-templates, messaging-network

**Design Ref:** [设计文档路径，如有；无则写 None]
- 例：docs/changes/battle/design.md

---
```

## 任务结构（Quick Mode）

````markdown
### Task N: [Component Name]

**Context:**
- Depends: [前置任务编号；无依赖则省略此行]
- Reads: [需要预读的现有文件路径；无则省略此行]
- Why: [一句话设计意图 — 必填]

**Files:**
- Create: `path/to/new/file.cs`
- Modify: `path/to/existing/file.cs:123-145`
- Test: `path/to/test/file.cs`

**Step 1: 编写失败测试**

```csharp
[Test]
public void TestSpecificBehavior()
{
    // Arrange
    var result = MySystem.Calculate(input);

    // Assert
    Assert.AreEqual(expected, result);
}
```

**Step 2: 运行测试验证失败**

Run: `dotnet test --filter "TestSpecificBehavior" -v n`
Expected: FAIL，报 "method not found" 或类似错误

**Step 3: 编写最小实现**

```csharp
public static class MySystem
{
    public static int Calculate(int input)
    {
        return input * multiplier;
    }
}
```

**Step 4: 运行测试验证通过**

Run: `dotnet test --filter "TestSpecificBehavior" -v n`
Expected: PASS

**Step 5: 编译检查**

Run: [CLAUDE.md 中的项目构建命令]
Expected: Build succeeded, 0 errors

**Step 6: 提交**

```bash
git add <specific files>
git commit -m "feat: add specific feature"
```
````

## Change Mode 任务结构

为变更编写计划时（`docs/changes/<name>/plan.md`），使用复选框格式进行持久化跟踪：

````markdown
## 1. [Group Name]

- [ ] 1.1 [任务描述]

**Context:**
- Depends: [前置任务编号，如 1.0；无依赖则省略此行]
- Reads: [需要预读的现有文件路径；无则省略此行]
- Why: [一句话设计意图 — 必填]

**Files:**
- Create: `path/to/new/file.cs`
- Test: `path/to/test/file.cs`

**Steps:**
1. 编写失败测试
2. 运行验证失败
3. 实现最小代码
4. 运行验证通过
5. 编译检查（项目构建命令）
6. 提交

- [ ] 1.2 [下一个任务描述]

**Context:**
- Why: [一句话设计意图]

...

## 2. [Next Group]

- [ ] 2.1 [任务描述]

**Context:**
- Depends: 1.1, 1.2
- Why: [一句话设计意图]

...
````

**与 Quick Mode 的主要区别：**
- 任务使用 `- [ ]` 复选框格式（执行时更新为 `- [x]`）
- 任务编号为 `N.M`（组.任务）
- 组编号为 `## N. Group Name`
- 进度通过复选框状态在会话间持久化

## 框架感知的计划编写

> **重要：** 阅读 CLAUDE.md 了解项目特定规则。将以下模式适配到你的项目框架。

编写计划时，确保每个计划都涉及项目特定的关注点：

### 模块/程序集放置
对于每个新文件，根据项目架构指定它属于哪个模块或程序集（阅读 CLAUDE.md 了解具体的模块组织方式）。

### 文件路径约定
始终使用遵循项目目录结构约定的完整路径。

### 代码生成步骤
如果计划涉及新的协议/消息定义或配置数据：
- 包含定义源文件的任务（proto 文件、Excel 等）
- 包含运行代码生成命令的任务
- 包含验证生成代码编译通过的步骤

### 数据模型声明任务
当计划创建新数据模型时，包含以下任务：
- 创建带有正确所有权注解的数据定义
- 创建带有必需标记的关联逻辑模块
- 验证编译通过

## 注意事项

- 始终使用精确的文件路径（遵循项目路径约定）
- 计划中给出完整代码（而非"添加验证"）
- 精确的命令和预期输出
- DRY（不重复）、YAGNI（不过度设计）、TDD（测试驱动）、频繁提交
- 每个新数据模型都需要定义文件 + 逻辑文件
- 每个代码生成变更都需要生成步骤
- 始终包含构建验证步骤
- 每个任务必须有 `**Context:**` 块，至少包含 `Why`
- `Depends` 只声明直接依赖（非传递依赖）
- `Reads` 只列出真正需要的文件（不要贪多）
- 计划头部 `Rules` 应列出 2-4 个相关规则文件（不是全部 9 个）

## 执行交接

保存计划后，提供执行选择：

**Change Mode：**

**"计划已完成并保存到 `docs/changes/<name>/plan.md`。两种执行方式：**

**1. 多 Agent 执行（当前会话）** — 使用 `/qx-dev-exec` 按任务分派 Agent，任务间审查，快速迭代

**2. 新会话** — 开启新会话，加载计划，带检查点执行。进度通过复选框跟踪 — 随时可恢复。

**选择哪种方式？"**

**Quick Mode：**

**"计划已完成并保存到 `docs/plans/<filename>.md`。两种执行方式：**

**1. 多 Agent 执行（当前会话）** — 使用 `/qx-dev-exec` 按任务分派 Agent，含审查

**2. 新会话** — 开启新会话，加载计划，带检查点执行

**选择哪种方式？"**

## 相关 Skills

- **qx-tdd** — 计划执行期间的测试驱动纪律
- **qx-code-review** — 审查已完成的任务
- **qx-dev-exec** — 使用多 Agent 编排执行计划
- **qx-dev-change** — 变更生命周期（计划是其中一个阶段）
