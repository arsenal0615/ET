---
name: qx-dev-continue
description: "自动推进变更到下一步。检测当前进度并调用对应 Skill。触发词: 继续, 下一步, next step, continue, /qx-dev-continue"
---

# 自动推进变更

检测活跃变更的当前进度，自动调用下一个 Skill。用户只需反复说"继续"即可推进整个流程。

**开始时宣告：** "正在检测变更 [名称] 的进度..."

## 流程

1. 如果没有指定变更，从上下文推断或询问
2. 扫描 `docs/changes/<name>/` 确定进度：

```
proposal.md 不存在?  -> 调用 qx-dev-create
proposal.md 存在但无 specs/ 且功能复杂?
                     -> 提示: "此功能较复杂，建议先 /qx-dev-create spec 细化需求，或直接 /qx-dev-design"
design.md 不存在?    -> 调用 qx-dev-design（简单变更可跳过）
plan.md 不存在?      -> 调用 qx-dev-plan
plan.md 有 - [ ]?    -> 调用 qx-dev-exec
所有 [x]?            -> 调用 qx-dev-close
```

3. 宣告当前进度和下一步：
```
Change: add-combo-system
已完成: proposal, design
下一步: 创建 plan.md
```

4. 执行适当的 Skill
