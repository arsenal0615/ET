---
name: 'qx-stories'
description: '拆分 Epic 和 Story。将 GDD 或功能设计分解为可执行的开发任务。'
---

Break down a game design document or feature into Epics and Stories for sprint planning.

## Process

1. Read the source document (GDD, feature spec, or design doc)
2. Identify major functional areas → **Epics**
3. Break each Epic into implementable **Stories** with:
   - Story ID (Epic.Story format, e.g., 1.1, 1.2, 2.1)
   - Title
   - Description (As a [player], I want [action], so that [benefit])
   - Acceptance criteria
   - Story point estimate (1/2/3/5/8/13)
   - Dependencies
4. Order by priority and dependency chain
5. Save to: `docs/game-design/stories-{name}.md`

After stories are created, suggest: `/qx-sprint` for sprint planning.

Source document or topic: $ARGUMENTS
