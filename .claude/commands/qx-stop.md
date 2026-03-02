---
name: 'qx-stop'
description: '停止持久化执行循环。将 loop 状态设为 inactive。'
---

Stop the QX persistent execution loop.

## Action

1. Read `.claude/qx/state/loop.json`
2. Set `"active": false`
3. Write the file back
4. Report: "QX loop stopped. Current progress preserved."

If no active loop exists, report: "No active QX loop found."
