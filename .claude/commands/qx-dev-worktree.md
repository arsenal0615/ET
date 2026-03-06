---
name: qx-dev-worktree
description: 创建隔离 git worktree 进行功能开发。自动命名、安全检查、与 qx-dev-change 集成。
allowed-tools: ["Bash", "Read", "Glob"]
---

# Git Worktree 隔离开发

在开始功能开发或执行计划前，创建隔离的 git worktree。

## 流程

### 1. 安全检查

```bash
# 确认当前不在 worktree 中
git worktree list

# 检查工作区是否干净（未提交的更改会丢失隔离意义）
git status --short
```

**如果工作区不干净**：提醒用户先提交或暂存（`git stash`）。

### 2. 确定 worktree 名称

**命名规范**：
- 如果有活跃的 change（`docs/changes/` 下的目录）：`qx-{change-name}`
- 如果用户指定了名称：使用用户指定的名称
- 否则：`qx-{简短描述}`（由用户确认）

### 3. 创建 worktree

使用 Claude Code 的 `EnterWorktree` 工具创建 worktree。

worktree 创建后，会话自动切换到新 worktree 目录。

### 4. 确认

```
Worktree 已创建：
- 分支：{branch-name}
- 路径：{worktree-path}
- 基于：{base-branch}

接下来可以：
- /qx-dev-exec plan.md — 在隔离环境中执行计划
- /qx-dev-change create — 创建新的技术变更
- 直接开始开发

完成后用 /qx-finishing 处理合并/清理。
```

## 注意事项

- Worktree 共享同一个 git 仓库，commit 会同步
- 退出会话时会提示保留或删除 worktree
- 不要在多个 worktree 中同时修改同一个文件
