# QX 工作流问题记录

| # | 类别 | 描述 | 状态 |
|---|------|------|------|
| 1 | CLAUDE.md | 概览把三方评审画成必经步骤，与 CONTINUE 逻辑不一致导致错误推荐 | 已修复 |
| 2 | stop-hook | 无去重机制：同一阶段完成后每次 AI 回复都触发 hook，导致重复要求记录。旧背景 Agent 通知延迟到达时会产生 5-10 次重复触发 | 已修复：stop-hook 增加 `_last_blocked_trigger` + 5min 窗口去重 |
| 3 | stop-hook | hook 不区分"AI 回复旧通知"和"AI 完成新工作"，应只在状态真正变更时触发记录请求 | 已修复：submit-hook 对 task-notification 和 hook-feedback 跳过 pending_record 标记 |
| 4 | background-agents | 跨会话恢复后旧 Agent 的 task-notification 仍会到达，每个通知触发一轮对话+hook，消耗大量上下文 | 已缓解：#2/#3 修复后通知不再触发重复 block 循环 |
