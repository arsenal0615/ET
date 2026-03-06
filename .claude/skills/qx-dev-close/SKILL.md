---
name: qx-dev-close
description: "验证变更实现完整性、处理分支收尾并归档。触发词: close change, archive, verify change, finish change, /qx-dev-close"
---

# 收尾变更

验证变更的实现完整性，处理分支合并/PR，然后归档。一步完成验证 + 收尾 + 归档。

**开始时宣告：** "正在使用 qx-dev-close 验证并归档变更 [名称]。"

## 何时使用

- 变更的所有计划任务完成后
- 准备归档一个变更时
- 实现完成且所有测试通过后

## 流程

### 步骤 1：验证完整性

统计 `plan.md` 中的 `- [ ]` vs `- [x]`：
- 全部完成：继续
- 有未完成：警告并要求确认 "还有 N 个任务未完成。仍然归档吗？"

### 步骤 2：验证正确性（如果 specs/ 存在）

对 `specs/` 中的每个需求：
- 搜索代码库中的实现证据
- 对每个场景，检查是否存在对应的测试或逻辑
- 如果需求没有实现证据则标记 WARNING

### 步骤 3：验证框架合规

- 阅读 CLAUDE.md 获取项目规则和构建命令
- 运行项目的构建/编译命令
- 验证不存在框架规则违反

**如果构建/测试失败：**
```
测试失败（N 个失败）。必须修复后才能继续：

[显示失败详情]

在测试通过之前无法归档。
```
停止。不要继续后续步骤。

### 步骤 4：处理技术债务（如果 tech-debt.md 存在）

检查 `docs/changes/<name>/tech-debt.md`：
- 列出所有未处理的 Important 问题
- 让用户确认每组问题的处置：
  - "已修复"
  - "推迟到后续迭代"
  - "不再相关"
- 更新 tech-debt.md 的处置记录

### 步骤 5：同步 specs（如果 specs/ 存在）

列出增量 specs 并询问是否同步到 `docs/specs/`：
- 如果选择同步：AI 驱动的智能合并
  - 新增：添加新的需求块
  - 修改：合并变更到现有需求中
  - 删除：移除需求块
- 如果不同步：直接归档

### 步骤 6：分支收尾

检测当前分支状态：
```bash
git merge-base HEAD main 2>/dev/null || git merge-base HEAD master 2>/dev/null
```

**如果在独立分支上（非主分支）：** 展示选项：

```
实现已完成。你想要做什么？

1. 本地合并回 [base-branch]
2. 推送并创建 Pull Request
3. 保持分支现状（我之后自己处理）
4. 丢弃这项工作

选哪个？
```

执行用户选择：
- **选项 1**：checkout base → pull → merge → 验证测试 → 删除 feature 分支
- **选项 2**：push -u origin → gh pr create → 报告 PR URL
- **选项 3**：报告分支名，不做操作
- **选项 4**：确认后 checkout base → branch -D

如果在 worktree 中（`git worktree list` 检测），提议移除 worktree。

**如果已在主分支上：** 跳过此步骤。

### 步骤 7：归档

```
docs/changes/<name>/ -> docs/changes/archive/YYYY-MM-DD-<name>/
```

### 步骤 8：输出报告

```
## 验证: <name>

| 维度     | 状态  | 分数 |
|---------|-------|------|
| 完整性   | Y / N | N/M  |
| 正确性   | Y / N | N/M  |
| 框架合规 | Y / N | N/M  |

### 问题（如有）
- [ ] ...

## 已归档: <name>
位置: docs/changes/archive/YYYY-MM-DD-<name>/
Specs 已同步: ...
任务完成: N/M
```

建议下一步：
- "/qx-compound — 提取知识，复合积累"

## 核心原则

- **验证通过即归档** — 不需要分多步
- **优雅降级** — 没有 specs 跳过正确性检查，没有 tech-debt 跳过债务处理，在主分支上跳过分支收尾
- **证据先于断言** — 必须运行构建命令，不能口头声称通过

## 红线

**绝不：**
- 在测试失败时继续归档
- 不经确认就删除分支或工作
- 未经明确要求就 force-push

**始终：**
- 在归档前验证测试
- 选项 4（丢弃）需要输入确认
- 如果在 worktree 中，提议清理

## 相关 Skills

- **qx-verify** — 在收尾前验证工作
- **qx-code-review** — 在合并/PR 前审查代码
- **qx-compound** — 完成后提取经验
