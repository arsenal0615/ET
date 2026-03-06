---
name: qx-finishing
description: "在实现完成且所有测试通过后使用。通过提供合并、PR 或清理的结构化选项来引导开发工作的收尾。"
---

# 开发工作收尾

## 概述

通过提供清晰的选项并处理所选的工作流，引导开发工作的收尾。

**核心原则：** 验证测试 → 展示选项 → 执行选择 → 清理。

**启动时宣布：** "正在使用 qx-finishing 来完成这项工作的收尾。"

## 流程

### 步骤 1：收尾前验证

**在展示选项之前，验证工作已完成：**

运行项目的构建和测试命令：
```bash
# 构建（根据项目调整）
dotnet build   # 或项目特定的构建命令

# 测试
dotnet test    # 或项目特定的测试命令
```

> **注意：** 阅读项目 CLAUDE.md 获取具体的构建/测试命令。

**如果测试失败：**
```
测试失败（N 个失败）。必须修复后才能完成：

[显示失败详情]

在测试通过之前无法进行合并/PR。
```

停止。不要进入步骤 2。

**如果测试通过：** 继续步骤 2。

### 步骤 2：检查变更上下文

检测是否在变更生命周期内工作：
1. 检查 `docs/changes/` 中是否有与本次工作匹配的活跃变更
2. 如果找到，建议先运行 `/qx-dev-change verify`

### 步骤 3：确定基准分支

```bash
git merge-base HEAD main 2>/dev/null || git merge-base HEAD master 2>/dev/null
```

或询问："这个分支是从 [main] 分出的 -- 是否正确？"

### 步骤 4：展示选项

展示恰好这 4 个选项：

```
实现已完成。你想要做什么？

1. 本地合并回 [base-branch]
2. 推送并创建 Pull Request
3. 保持分支现状（我之后自己处理）
4. 丢弃这项工作

选哪个？
```

**不要添加解释** -- 保持选项简洁。

### 步骤 5：执行选择

#### 选项 1：本地合并

```bash
git checkout [base-branch]
git pull
git merge [feature-branch]

# 在合并结果上验证测试
[test command]

# 如果测试通过
git branch -d [feature-branch]
```

然后：清理（步骤 6）

#### 选项 2：推送并创建 PR

```bash
git push -u origin [feature-branch]

gh pr create --title "[title]" --body "$(cat <<'EOF'
## Summary
- [2-3 bullets of what changed]

## Test Plan
- [ ] [verification steps]
EOF
)"
```

向用户报告 PR URL。然后：清理（步骤 6）

#### 选项 3：保持现状

报告："保持分支 [name] 现状。你可以之后返回继续。"

**不做清理。**

#### 选项 4：丢弃

**先确认：**
```
这将永久删除：
- 分支 [name]
- 所有提交：[commit-list]

输入 'discard' 确认。
```

等待精确确认。确认后：
```bash
git checkout [base-branch]
git branch -D [feature-branch]
```

然后：清理（步骤 6）

### 步骤 6：清理

**针对选项 1、2、4：**

检查是否在 worktree 中：
```bash
git worktree list
```

如果在 worktree 中，提议移除它。

**针对选项 3：** 保持一切现状。

### 步骤 7：更新变更状态

如果在变更生命周期内工作（`docs/changes/[name]/`）：
- 选项 1（合并）：建议运行 `/qx-dev-change archive`
- 选项 2（PR）：说明应在 PR 合并后再归档
- 选项 4（丢弃）：建议清理变更目录

## 快速参考

| 选项 | 合并 | 推送 | 保留分支 | 清理 |
|------|------|------|----------|------|
| 1. 本地合并 | 是 | - | 删除 | 是 |
| 2. 创建 PR | - | 是 | 保留 | 仅 worktree |
| 3. 保持现状 | - | - | 保留 | 否 |
| 4. 丢弃 | - | - | 强制删除 | 是 |

## 红线

**绝不：**
- 在测试失败时继续
- 不验证测试结果就合并
- 不经确认就删除工作
- 未经明确要求就 force-push

**始终：**
- 在提供选项前验证测试
- 展示恰好 4 个选项
- 选项 4 需要输入确认
- 如果在变更生命周期内，更新变更状态

## 相关 Skills

- **qx-verification** -- 在收尾前验证工作
- **qx-code-review** -- 在合并/PR 前审查代码
- **qx-managing-changes** -- 完成后归档变更
- **qx-compound** -- 完成后提取经验
