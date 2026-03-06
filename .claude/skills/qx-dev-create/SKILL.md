---
name: qx-dev-create
description: "创建新的技术变更并编写提案。可选细化需求规格。触发词: create change, new feature, new change, proposal, specs, /qx-dev-create"
---

# 创建变更

创建新的变更目录并编写提案（proposal）。变更是位于 `docs/changes/<name>/` 的基于目录的容器。

**开始时宣告：** "正在使用 qx-dev-create 创建变更 [名称]。"

## 何时使用

- 启动新的游戏系统或功能开发
- 跨角色交接（策划 -> 程序）
- 需要长期追踪的技术调研

**不需要此 Skill 的场景：** 小 Bug 修复、简单重构、配置变更 — 直接做。

## 变更目录结构

```
docs/changes/<name>/
  proposal.md        # 为什么和做什么（必需）
  specs/             # 增量规格（可选，仅复杂功能）
    <capability>/
      spec.md
```

> 状态由目录位置决定：`docs/changes/<name>/` = 活跃，`docs/changes/archive/YYYY-MM-DD-<name>/` = 已归档

## 流程

### 步骤 1：创建变更目录

1. 从用户获取名称（或从描述中推导），转换为 kebab-case
2. 检查 `docs/changes/` 是否有重复（含 `archive/`）
3. 创建 `docs/changes/<name>/` 目录

### 步骤 2：编写 proposal.md

阅读 CLAUDE.md 获取项目特定的架构和约定，然后起草：

```markdown
## 为什么

<!-- 解决什么问题？为什么是现在？ -->

## 变更内容

<!-- 变更的要点列表。要具体。 -->

## 影响

<!-- 受影响的代码、API、依赖、系统 -->

### 框架影响检查清单
- [ ] **受影响的模块/程序集**: 哪些？
- [ ] **需要新的协议/消息定义？** 如是，列出它们
- [ ] **需要新的配置/数据文件？** 如是，列出它们
- [ ] **跨进程/跨线程通信？** 如是，识别边界
- [ ] **新的数据模型类型？** 列出及其所有权声明
- [ ] **影响 system-map.md？** 如是，哪些系统受影响
```

**注意：**
- 保持简洁（1-2 页），聚焦"为什么"而非"怎么做"
- 阅读 `docs/system-map.md` 了解现有系统关系

### 步骤 3：建议下一步

根据复杂度给出不同建议：

```
简单变更:  -> /qx-dev-design 或直接 /qx-dev-plan
复杂功能:  -> /qx-dev-create spec（细化需求）
高风险:    -> /qx-meeting（三方评审）
```

## SPEC — 细化需求（可选子命令）

**触发词：** "写规格"、"定义需求"、"/qx-dev-create spec"

**何时使用：** 仅当功能复杂、需要精确的 WHEN/THEN 场景定义时。

**前提：** `proposal.md` 必须存在

**流程：**
1. 阅读 `proposal.md` 识别需要细化的功能点
2. 为每个功能点创建 `specs/<capability>/spec.md`

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

## 删除的需求

### Requirement: <名称>
**原因**: <为什么删除>
**迁移**: <替代方案>
```

**规则：**
- 每个需求至少一个场景
- 场景使用 `#### Scenario:`（4 个井号）
- 修改的需求必须包含完整更新后的内容

## 核心原则

- **proposal 是唯一必需产物** — specs 仅复杂功能需要
- **变更目录是交接物** — 策划创建 proposal，程序从 design 接手
- **检查 system-map.md** — 在提案中引用持久化系统关系图
