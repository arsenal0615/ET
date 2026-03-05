---
name: qx-managing-changes
description: "用于创建新功能或变更、继续进行中的变更、定义需求、编写提案、设计方案、检查变更状态、验证实现完整性、归档已完成工作或列出活跃变更。触发词: create change, continue, new feature, proposal, requirements, specs, design, verify, archive, status, list changes, /qx-change"
---

# 管理变更

管理"变更"的完整生命周期 — 从提案到归档。变更是位于 `docs/changes/<name>/` 的基于目录的容器，追踪一个功能、修复或修改的所有产物。

**开始时宣告：** "我正在使用 qx-managing-changes skill 来 [创建/提案/设计/验证/归档] 这个变更。"

## 何时使用此模式 vs 快速模式

| 场景 | 使用此模式（变更模式） | 使用快速模式（qx-brainstorm） |
|------|----------------------|-------------------------------|
| 新游戏系统或功能 | ✓ | |
| 跨角色交接（策划 → 程序） | ✓ | |
| 需求需要长期追踪 | ✓ | |
| 带正式设计的技术调研 | ✓ | |
| 小 Bug 修复 | | ✓ |
| 简单重构 | | ✓ |
| 配置变更或微调 | | ✓ |

## 变更目录结构

```
docs/changes/<name>/
├── .change.yaml       # 元数据（创建日期、状态）
├── proposal.md        # 为什么和做什么（必需）
├── design.md          # 技术方案（可选）
├── specs/             # 增量规格（可选）
│   └── <capability>/
│       └── spec.md    # 新增/修改/删除 的需求
└── plan.md            # 带复选框的实施任务
```

## 操作

从对话上下文推断操作。如果有歧义，向用户确认。

---

### CREATE — 启动新变更

**触发词：** "启动新功能"、"创建变更"、"新变更"、"/qx-change create"

**流程：**
1. 从用户获取名称（或从描述中推导）
2. 转换为 kebab-case（例如 "combo attack system" → `combo-attack-system`）
3. 检查 `docs/changes/` 是否有重复
4. 创建目录和元数据：

```
docs/changes/<name>/
└── .change.yaml
```

`.change.yaml` 内容：
```yaml
created: YYYY-MM-DD
status: active
```

5. 宣告创建了什么并建议下一步："变更已创建。要编写提案吗？"

---

### CONTINUE — 自动推进到下一步

**触发词：** "继续"、"下一步"、"接着做"、"/qx-change continue"

**流程：**
1. 如果没有指定变更，从上下文推断或询问
2. 扫描 `docs/changes/<name>/` 确定已存在的内容：

```
按顺序检查:
  proposal.md 存在?  → 否  → 创建提案（运行 PROPOSE）
  proposal.md 存在?  → 是  → 检查 capabilities
    列出了 Capabilities 但没有 specs/?  → 创建规格（运行 SPEC）
  design.md 存在?    → 否  → 创建设计（运行 DESIGN）
  plan.md 存在?      → 否  → 创建计划（调用 qx-writing-plans skill）
  plan.md 有 - [ ]?  → 是  → 恢复执行（调用 qx-exec skill）
  所有任务 [x]?      → 是  → 建议验证/归档
```

3. 宣告检测到的内容和下一步将创建的内容：
   ```
   Change: add-combo-system
   已完成: proposal ✓, specs ✓
   下一步: 创建 design.md
   ```

4. 执行适当的操作或调用适当的 skill

**注意：**
- 这是"持续说继续"的工作流 — 用户不需要知道下一个操作是什么
- 如果 proposal 中列出了 capabilities 但 specs 是可选的（纯技术变更），跳到 design
- 到达计划创建时，交给 `qx-writing-plans` skill
- 到达执行时，交给 `qx-exec` skill

---

### PROPOSE — 定义做什么和为什么

**触发词：** "写提案"、"定义变更"、"/qx-change propose"

**前提：** 变更必须已存在（如不存在先运行 CREATE）

**流程：**
1. 如果没有活跃变更，询问哪个变更或创建一个
2. 阅读 CLAUDE.md 获取项目特定的架构和约定
3. 使用此模板起草 `proposal.md`：

```markdown
## 为什么

<!-- 解决什么问题？为什么是现在？ -->

## 变更内容

<!-- 变更的要点列表。要具体。 -->

## Capabilities

### 新增 Capabilities
<!-- 每个都会成为一个 spec 文件。使用 kebab-case 名称。 -->
- `<name>`: <简要描述>

### 修改的 Capabilities
<!-- 需求变更的现有 capabilities。 -->
<!-- 检查 docs/specs/ 获取现有 spec 名称。 -->

## 影响

<!-- 受影响的代码、API、依赖、系统 -->

### 框架影响检查清单
<!-- 阅读 CLAUDE.md 获取项目特定项。常见检查项: -->
- [ ] **受影响的模块/程序集**: 哪些？
- [ ] **需要新的协议/消息定义？** 如是，列出它们
- [ ] **需要新的配置/数据文件？** 如是，列出它们
- [ ] **跨进程/跨线程通信？** 如是，识别边界
- [ ] **新的数据模型类型？** 列出及其所有权声明
- [ ] **影响 system-map.md？** 如是，哪些系统受影响
```

**注意：**
- Capabilities 部分对于纯技术变更是可选的
- 列出的每个 capability 都需要对应的 spec 文件
- 保持简洁（1-2 页）。聚焦"为什么"而非"怎么做"。

---

### SPEC — 定义需求

**触发词：** "写规格"、"定义需求"、"/qx-change spec"

**前提：** `proposal.md` 必须存在

**流程：**
1. 阅读 `proposal.md` 识别 capabilities
2. 为每个 capability 创建 `docs/changes/<name>/specs/<capability>/spec.md`
3. 如果修改现有 capability，先阅读 `docs/specs/<capability>/spec.md`

**增量 spec 模板：**
```markdown
## 新增需求

### Requirement: <名称>
<使用 SHALL/MUST 的描述>

#### Scenario: <场景名称>
- **WHEN** <条件>
- **THEN** <预期结果>

## 修改的需求

### Requirement: <现有需求名称>
<!-- 从主 spec 复制完整需求文本，然后修改 -->

#### Scenario: <新增或变更的场景>
- **WHEN** <条件>
- **THEN** <新的预期结果>

## 删除的需求

### Requirement: <名称>
**原因**: <为什么删除>
**迁移**: <替代方案>

## 重命名的需求
- FROM: `### Requirement: Old Name`
- TO: `### Requirement: New Name`
```

**规则：**
- 每个需求必须有至少一个场景
- 场景必须使用 `#### Scenario:`（4 个井号）
- 使用 SHALL/MUST 表达规范性需求
- 修改的需求必须包含完整更新后的内容，不能只写 diff
- 对于纯技术变更，完全跳过此操作

---

### DESIGN — 定义怎么做

**触发词：** "设计方案"、"技术方案"、"/qx-change design"

**前提：** `proposal.md` 必须存在。推荐有 specs 但不强制。

**流程：**
1. 阅读 `proposal.md` 和所有 specs 获取上下文
2. 阅读 `docs/system-map.md` 了解现有系统关系
3. 阅读 CLAUDE.md 获取项目特定的架构模式
4. 使用此模板起草 `design.md`：

```markdown
## 背景

<!-- 背景和当前状态 -->

## 目标 / 非目标

**目标：**
<!-- 此设计旨在达成什么 -->

**非目标：**
<!-- 明确不在范围内的内容 -->

## 决策

### 决策 1: <标题>

**选择：** <决定了什么>

**考虑过的替代方案：**
- A) <方案> → <为什么不选>
- B) <方案> → <为什么不选>

**理由：** <为什么选择这个>

## 框架特定决策

<!-- 阅读 CLAUDE.md 获取项目特定的架构模式。 -->
<!-- 记录以下方面的决策: -->
<!-- - 数据模型设计（所有权、生命周期、关系） -->
<!-- - 消息/协议类型选择 -->
<!-- - 并发/线程模型选择 -->
<!-- - 模块/程序集放置 -->

## 风险 / 权衡

| 风险 | 缓解措施 |
|------|----------|
| <风险> | <缓解措施> |
```

**注意：**
- 对于简单变更是可选的 — 如果 proposal 就够了就跳过
- 聚焦架构和"为什么选 X 而非 Y"，而不是逐行代码
- 好的设计文档解释技术决策背后的推理
- 框架特定决策部分是可选的 — 只包含适用的子节

---

### STATUS — 检查进度

**触发词：** "什么状态"、"进度"、"/qx-change status"

**流程：**
1. 如果没有指定变更，询问或从上下文推断
2. 阅读变更目录并报告：

```
Change: <名称>
Created: <日期>
Status: <active/archived>

产物:
  [x] proposal.md
  [x] design.md
  [ ] specs/
  [x] plan.md

任务: 5/12 完成 (42%)
  已完成: 1.1, 1.2, 2.1, 2.2, 2.3
  下一个: 3.1 — <描述>
```

3. 统计 `plan.md` 中的复选框：`- [x]` = 完成，`- [ ]` = 待办

---

### LIST — 显示所有变更

**触发词：** "列出变更"、"有哪些变更"、"/qx-change list"

**流程：**
1. 扫描 `docs/changes/`（排除 `archive/`）
2. 对每个变更目录：
   - 阅读 `.change.yaml` 获取创建日期
   - 如果存在 `plan.md`，统计复选框
3. 显示：

```
活跃变更:
  add-combo-system    created: 2026-02-20    tasks: 3/8
  optimize-ai         created: 2026-02-25    tasks: 0/0 (尚无计划)

近期归档:
  2026-02-15-fix-inventory-ui
  2026-02-10-add-quest-system
```

---

### VERIFY — 检查实现完整性

**触发词：** "验证"、"检查完整性"、"/qx-change verify"

**前提：** `plan.md` 必须存在

**流程：**

三维验证，优雅降级：

**1. 完整性（始终检查）**
- 统计 `plan.md` 中的 `- [ ]` vs `- [x]`
- 报告："N/M 个任务完成"
- 如果有未完成任务则为 CRITICAL

**2. 正确性（如果 specs 存在）**
- 对 `specs/` 中的每个需求，搜索代码库中的实现证据
- 对每个场景，检查是否存在对应的测试或逻辑
- 如果需求没有实现证据则为 WARNING

**3. 一致性（如果 design.md 存在）**
- 对 `design.md` 中的每个决策，验证实现是否遵循了选定的方案
- 如果实现偏离了设计则为 SUGGESTION

**4. 框架合规（始终检查）**
- 阅读 CLAUDE.md 获取项目特定规则和验证命令
- 运行项目的构建/编译命令
- 验证框架特定的声明和注解是否正确
- 检查代码生成步骤是否已运行（如适用）
- 验证不存在框架规则违反

**输出格式：**
```
## 验证: <change-name>

| 维度           | 状态    | 分数  |
|---------------|---------|-------|
| 完整性         | ✓ / ✗  | N/M   |
| 正确性         | ✓ / ✗  | N/M   |
| 一致性         | ✓ / △  | N/M   |
| 框架合规       | ✓ / ✗  | N/M   |

### CRITICAL
- [ ] 任务 3.2 未完成
- [ ] 需求 "Combo Chain" 没有实现

### WARNING
- Spec 场景 "damage overflow" 没有测试覆盖
- 定义变更后未运行代码生成步骤

### SUGGESTION
- 设计说用方案 A 但实现用了方案 B
```

---

### ARCHIVE — 收尾变更

**触发词：** "归档"、"完成了"、"/qx-change archive"

**前提：** 变更必须存在

**流程：**

1. **检查完成状态**
   - 统计 `plan.md` 中的未完成任务
   - 如果未完成：警告并要求确认
   - "还有 3 个任务未完成。仍然归档吗？"

1.5. **检查技术债务**

检查 `docs/changes/<name>/tech-debt.md` 是否存在：
- 如果存在且有未处理的 Important 问题：
  - 列出所有问题
  - 使用 AskUserQuestion 让用户确认每组问题的处置：
    - "已修复（本次或后续 PR 中）"
    - "确认推迟到后续迭代"
    - "不再相关（删除）"
  - 更新 tech-debt.md 的处置记录
- 如果不存在或为空：跳过

2. **检查增量 specs**
   - 查找 `docs/changes/<name>/specs/` 目录
   - 如果存在增量 specs：

   ```
   发现增量 specs:
     specs/combat-system/spec.md
       新增: Combo Chain 需求（2 个场景）
       修改: Damage Calculation（1 个新场景）

   选项:
   1. 同步到主 specs（推荐）— 合并到 docs/specs/
   2. 不同步直接归档
   ```

3. **同步 specs（如果选择了）**
   - 对每个增量 spec：
     - 阅读 `docs/specs/<capability>/spec.md`（不存在则创建）
     - 应用新增：添加新的需求块
     - 应用修改：合并变更到现有需求中，保留未修改的内容
     - 应用删除：移除需求块
     - 应用重命名：重命名需求标题
   - 这是 AI 驱动的智能合并，不是程序化的 — 保留增量中未提及的现有内容

4. **移动到归档**
   ```
   docs/changes/<name>/ → docs/changes/archive/YYYY-MM-DD-<name>/
   ```

5. **显示摘要**
   ```
   ## 已归档: <name>

   位置: docs/changes/archive/YYYY-MM-DD-<name>/
   Specs 已同步: combat-system（2 个新增，1 个修改）
   任务完成: 12/12
   ```

---

## 核心原则

- **除 proposal 外产物都是可选的** — 简单变更可以走 proposal → plan → 执行
- **Specs 用于功能需求** — 纯技术工作（性能、重构）跳过
- **不要强制完整工作流** — 如果用户只想创建和计划，让他们这样做
- **变更目录是交接物** — 策划创建 proposal + specs，程序从 design 接手
- **一次一个变更，一个会话** — 避免跨会话并发修改同一变更
- **检查 system-map.md** — 在提案和设计期间引用持久化系统关系图
