---
name: qx-dev-status
description: "查看变更状态或列出所有变更。触发词: 变更状态, 进度, 列出变更, list changes, /qx-dev-status"
---

# 变更状态

查看指定变更的进度，或列出所有活跃变更。

## 流程

**如果指定了变更名（或上下文中有活跃变更）：**

```
Change: <名称>

产物:
  [x] proposal.md
  [x] design.md
  [ ] specs/
  [x] plan.md

任务: 5/12 完成 (42%)
  已完成: 1.1, 1.2, 2.1, 2.2, 2.3
  下一个: 3.1 — <描述>
```

**如果没有指定变更（或显式要求列出）：**

1. 扫描 `docs/changes/`（排除 `archive/`）
2. 对每个变更目录统计 plan.md 中的复选框
3. 显示：

```
活跃变更:
  add-combo-system    tasks: 3/8
  optimize-ai         tasks: 0/0 (尚无计划)

近期归档:
  2026-02-15-fix-inventory-ui
  2026-02-10-add-quest-system
```
