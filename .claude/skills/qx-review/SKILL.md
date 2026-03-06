---
name: qx-three-party-review
description: "在设计文档、GDD、提案或计划需要多角度评审后再继续时使用。编排结构化评审，由 3 个基于角色的评审者提供多元化专家反馈。"
---

# 开三方评审（Three-Party Review）

## 概述

编排对设计文档、提案或计划的结构化多角度评审。三个基于角色的评审者各自带来不同的视角 -- 战略、技术和对抗 -- 在进入实现之前暴露盲点。

**核心原则：** 三个视角能发现一个视角遗漏的问题。战略 + 技术 + 对抗 = 全面评审。

**启动时宣布：** "正在使用 qx-three-party-review 对 [文档] 进行三方评审。"

## 适用场景

- GDD 或功能设计已完成，需要在 Story 拆分前进行验证
- 技术提案或规格需要跨角色评审
- UX 设计需要非 UX 角度的反馈
- 任何重要文档在成为实现基础之前
- Sprint 计划需要在承诺前进行验证

## 前置条件

1. 确定待评审的文档（必须作为文件存在）
2. 完整阅读文档
3. 根据文档类型选择适当的评审者视角

## 流程

### 步骤 1：选择三位评审者

根据文档类型，从 QX 角色池中选择 3 位评审者：

**游戏设计文档（GDD）：**

| 评审者 | 角色 | 视角 |
|--------|------|------|
| **战略** | game-designer | 玩家体验、市场契合度、趣味性 |
| **技术** | programmer | 可行性、性能、架构影响 |
| **对抗** | qa-tester | 边界情况、可利用漏洞、可能出错的地方 |

**技术提案/规格：**

| 评审者 | 角色 | 视角 |
|--------|------|------|
| **战略** | game-designer | 这是否服务于游戏愿景？ |
| **技术** | programmer | 架构、模式、可维护性 |
| **对抗** | qa-tester | 可测试性、失败模式、遗漏的需求 |

**UX 设计：**

| 评审者 | 角色 | 视角 |
|--------|------|------|
| **战略** | game-designer | 玩家需求、游戏流程影响 |
| **技术** | programmer | 实现可行性、平台约束 |
| **用户代言** | artist-ux | 无障碍性、一致性、交互质量 |

**Sprint 计划：**

| 评审者 | 角色 | 视角 |
|--------|------|------|
| **战略** | game-designer | 优先级是否与游戏愿景对齐 |
| **技术** | programmer | 估算准确性、依赖风险 |
| **质量** | qa-tester | 测试覆盖缺口、风险领域 |

### 步骤 2：分派评审者

使用 Agent 工具并行分派 3 个评审子 Agent：

```
For each reviewer:
  Agent(subagent_type="[role-agent]", prompt="""
    You are reviewing [document path] as the [lens] reviewer.

    Your review lens: [lens description]

    Read the document, then provide:

    ## [Lens] Review

    ### Strengths
    - [What this document does well from your perspective]

    ### Concerns
    For each concern, classify severity:
    - **Critical** — Must fix before proceeding
    - **Important** — Should address, but not blocking
    - **Suggestion** — Nice to have improvement

    ### Questions
    - [Anything unclear that needs author clarification]

    ### Verdict
    APPROVE / APPROVE WITH CONDITIONS / REQUEST CHANGES
  """)
```

> **注意：** 使用 QX 角色 Agent（game-designer、programmer、artist-ux、qa-tester）作为评审者类型。这些 Agent 已加载了各自领域的知识。

### 步骤 3：逐条展示 + 综合评审意见

3 位评审者全部返回后，**先逐条展示每位评审者的原始报告，再综合**。

#### 3a. 逐条展示原始报告

按顺序向用户展示每位评审者的完整报告，让用户看到原始观点：

```
📋 评审者 1/3 — [角色名]（[视角]）

[评审者的完整原始报告 — Strengths / Concerns / Questions / Verdict]

---

📋 评审者 2/3 — [角色名]（[视角]）

[评审者的完整原始报告]

---

📋 评审者 3/3 — [角色名]（[视角]）

[评审者的完整原始报告]
```

> **注意：** 每位评审者的报告要完整展示，不要省略或压缩。用户需要看到原始观点才能判断综合结论是否公正。

#### 3b. 综合评审意见

在展示完所有原始报告后，综合其发现：

```markdown
## 三方评审总结

**文档：** [名称]
**日期：** [日期]
**评审者：** [3 位评审者姓名及角色]

### 共识结论
[APPROVE / APPROVE WITH CONDITIONS / REQUEST CHANGES]

### 关键问题（必须修复）
1. [问题] -- 由 [评审者] 提出
2. ...

### 重要问题（应当解决）
1. [问题] -- 由 [评审者] 提出
2. ...

### 建议
1. [建议] -- 由 [评审者] 提出
2. ...

### 意见分歧
[评审者之间意见不一致之处，记录双方立场]

### 需要作者回应的问题
1. [问题] -- 来自 [评审者]
2. ...
```

### 步骤 4：向作者呈现

向用户呈现综合评审结果：

```
三方评审完成 -- [文档名称]

结论：[APPROVE / APPROVE WITH CONDITIONS / REQUEST CHANGES]
  关键问题：[N] 个
  重要问题：[N] 个
  建议：[N] 个

[如果 REQUEST CHANGES]：必须解决 [N] 个关键问题后才能继续。
[如果 APPROVE WITH CONDITIONS]：可以继续，但应解决 [N] 个重要问题。
[如果 APPROVE]：可以进入下一阶段。
```

### 步骤 4.5：作者回应（闭环）

综合报告呈现后，对每个 Critical 和 Important 问题要求作者给出处置决定。

**Critical 问题（必须逐条回应）：**

对每个 Critical 问题，使用 AskUserQuestion 提问：
- 选项 1: "修复" — 现在修改文档，修复后更新评审记录
- 选项 2: "推迟（需说明理由）" — 记录为已知问题，附理由
- 选项 3: "不同意（反驳）" — 提供反驳理由，记录分歧

**Important 问题（应当回应，可批量处理）：**

列出所有 Important 问题，使用 AskUserQuestion（multiSelect: true）让作者批量选择需要修复的：
- 未被选中的 Important 问题记录为"已确认，后续处理"

**记录格式：**

在综合报告末尾追加"作者回应"章节：

```markdown
### 作者回应

| # | 级别 | 问题摘要 | 处置 | 备注 |
|---|------|---------|------|------|
| C1 | Critical | [摘要] | 修复/推迟/不同意 | [理由] |
| C2 | Critical | [摘要] | 修复/推迟/不同意 | [理由] |
| I1 | Important | [摘要] | 修复/确认 | |
| I2 | Important | [摘要] | 修复/确认 | |
```

> **关键规则：** 如果有 Critical 问题被标记为"推迟"或"不同意"，评审结论自动降级：不能给出 APPROVE，至少为 APPROVE WITH CONDITIONS。

### 步骤 5：保存评审记录

保存到：`docs/reviews/[document-name]-review-[date].md`

评审记录应包含：
1. 三位评审者的原始报告（完整保留）
2. 综合评审意见
3. 作者回应表（来自步骤 4.5）
4. 最终结论（考虑作者回应后的调整）

### 步骤 6：提供后续步骤

根据结论：

**如果 APPROVE：**
- "可以进入下一阶段。你想做什么？"
- 建议合适的下一个 Skill（如 GDD 后使用 `/qx-gd-stories`，规格后使用 `/qx-dev-plan`）

**如果 APPROVE WITH CONDITIONS：**
- "可以继续，但建议先解决 [N] 个重要问题。"
- 提供选择："现在解决问题，还是继续并作为后续跟进？"
- 如果有被作者"推迟"的 Critical 问题，明确列出并警告风险

**如果 REQUEST CHANGES：**
- "必须解决 [N] 个关键问题后才能继续。"
- 列出关键问题
- "你想现在修改文档吗？"

## 评审质量标准

**好的评审：**
- 引用文档的具体章节
- 解释为什么某事是问题，而非仅仅指出是什么
- 在提出问题时提供替代方案
- 肯定优点，而非只挑毛病

**差的评审：**
- 笼统（"这里需要改进"）
- 只有批评（没有提到优点）
- 偏离视角（技术评审者评论美术方向）
- 走过场（APPROVE 但没有实质内容）

## 核心原则

- **始终三个视角** -- 绝不减少评审者数量
- **并行分派** -- 同时运行 3 个评审以提高速度
- **严重性分类** -- 每个问题都标注 关键/重要/建议
- **分歧是有价值的** -- 评审者之间的分歧能暴露重要的权衡
- **作者决定** -- 评审提供信息，作者做最终决定
- **基于证据** -- 评审引用文档的具体章节

## 相关 Skills

- **qx-game-design** -- 创建待评审的 GDD
- **qx-writing-plans** -- 创建待评审的计划
- **qx-ux-design** -- 创建待评审的 UX 规格
- **qx-code-review** -- 代码层面的评审（实现后）
