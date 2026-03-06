---
name: qx-dev-worktree
description: "创建隔离 git worktree 进行功能开发。自动命名、安全检查、与变更生命周期集成。触发词: worktree, 隔离开发, /qx-dev-worktree"
---

# Git Worktree 隔离开发

在开始功能开发或执行计划前，创建隔离的 git worktree。

**开始时宣告：** "正在使用 qx-dev-worktree 创建隔离工作区。"

## 何时使用

- 可能破坏主工作区的大型或高风险变更
- 可能想要丢弃的实验性功能
- 并行开发：主工作区保持干净以进行其他工作
- `/qx-dev-exec --worktree` 内部调用

**不需要此 Skill 的场景：** 小型、安全的变更（worktree 开销不值得），想要立即应用的快速修复。

## 流程

### 步骤 1：安全检查

```bash
# 确认当前不在 worktree 中
git worktree list

# 检查工作区是否干净
git status --short
```

**如果工作区不干净**：提醒用户先提交或暂存（`git stash`）。

**如果已在 worktree 中**：警告并停止。

### 步骤 2：确定 worktree 名称

**命名规范**：
- 如果有活跃的 change（`docs/changes/` 下的目录）：`qx-{change-name}`
- 如果用户指定了名称：使用用户指定的名称
- 否则：`qx-{简短描述}`（由用户确认）

### 步骤 3：创建 worktree

使用 Claude Code 的 `EnterWorktree` 工具创建 worktree。

worktree 创建后，会话自动切换到新 worktree 目录。

### 步骤 4：确认输出

```
Worktree 已创建：
- 分支：{branch-name}
- 路径：{worktree-path}
- 基于：{base-branch}

接下来可以：
- /qx-dev-exec plan.md — 在隔离环境中执行计划
- /qx-dev-create — 创建新的技术变更
- 直接开始开发

完成后用 /qx-dev-close 处理验证/合并/归档。
```

## 注意事项

- Worktree 共享同一个 git 仓库，commit 会同步
- 退出会话时会提示保留或删除 worktree
- 不要在多个 worktree 中同时修改同一个文件

## 相关 Skills

- **qx-dev-exec** — `--worktree` 模式内部调用本 Skill
- **qx-dev-close** — 完成后验证/合并/归档（含 worktree 清理）
- **qx-dev-create** — 在 worktree 中创建新变更
