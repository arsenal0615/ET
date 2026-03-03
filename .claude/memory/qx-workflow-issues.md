# QX 工作流验收问题记录

> 在 A1 项目中逐步验收全兴工作流时发现的问题和改进建议。
> 每个问题标注：环节、严重程度（P0 阻塞 / P1 应改 / P2 建议）、状态。

---

## 问题列表

### Issue #1 — qx-game-design 缺少"导入模式"
- **环节:** `/qx-gdd` (qx-game-design skill)
- **严重程度:** P1
- **状态:** 待修复
- **描述:** skill 假设从零协作发现（逐个问"电梯演讲是什么？"），但当项目已有完整策划文档时，这个流程很冗余。
- **建议:** 在 Preflight 阶段增加分支：检测到 `design/` 目录或已有详细设计时，切换为"提取+整合+确认"模式，而非逐个问基础问题。
- **涉及文件:** `.claude/skills/qx-game-design/qx-game-design.md`

### Issue #2 — GDD 模板太骨架，缺少填写指引
- **环节:** `/qx-gdd` (gdd-template.md)
- **严重程度:** P2
- **状态:** 待修复
- **描述:** `gdd-template.md` 只有 50 行空标题，没有字段说明、填写提示、示例内容。AI 或人类使用时缺乏指引。
- **建议:** 在每个 section 下加简短提示语，如 `<!-- Vision Statement: 1-2 句话描述核心体验 -->`，或给出示例片段。
- **涉及文件:** `.claude/qx/templates/gdd-template.md`

### Issue #3 — GDD 模板缺少 Appendix/文档索引结构
- **环节:** `/qx-gdd` (gdd-template.md)
- **严重程度:** P2
- **状态:** 待修复
- **描述:** 大型项目有多层文档（概要设计→详细设计→技能设计等），GDD 需要一个索引指向这些详细文档。模板中缺少此结构。
- **建议:** 在模板末尾增加 `## Appendix: Related Documents` section。
- **涉及文件:** `.claude/qx/templates/gdd-template.md`

### Issue #4 — 三方评审缺少过程可见性
- **环节:** `/qx-review` (qx-three-party-review skill)
- **严重程度:** P1
- **状态:** 待修复
- **描述:** 三个评审员 Agent 并行运行后直接返回综合报告。用户看不到评审过程——不知道每个评审员具体在讨论什么、关注了哪些点、是怎么得出结论的。整个过程像一个黑盒。
- **建议:** 改进方案（可选一或组合）：
  1. **进度通报** — 每个评审员开始时向用户汇报"我是 X 评审员，将从 Y 角度审查"
  2. **逐条呈现** — 三个评审员结果分别展示（而非只给综合报告），让用户看到原始意见
  3. **交叉讨论轮** — 评审员之间有一轮"看到对方意见后的回应"，制造真正的讨论感
  4. **实时流式输出** — 利用 background agent 的通知机制，让评审员的结论逐条推送
- **涉及文件:** `.claude/skills/qx-three-party-review/SKILL.md`

### Issue #5 — 三方评审缺少"作者回应"闭环步骤
- **环节:** `/qx-review` (qx-three-party-review skill)
- **严重程度:** P2
- **状态:** 待修复
- **描述:** 当前流程是"评审 → 呈现 → 建议下一步"，缺少"作者逐条回应每个 Critical/Important 的处置决定"的闭环。评审报告里的问题没有强制要求回应和关闭。
- **建议:** 在 Step 4（Present to Author）后增加 Step 4.5：要求作者对每个 Critical 问题给出处置决定（修复/标注 Known Issue/推迟），并记录到评审文档中。
- **涉及文件:** `.claude/skills/qx-three-party-review/SKILL.md`

### Issue #7 — qx-change 的 propose→design 之间缺少审查节点
- **环节:** `/qx-change` (qx-managing-changes skill)
- **严重程度:** P2
- **状态:** 待修复
- **描述:** "继续"自动推进时，proposal 完成后直接开始写 design，没有让用户确认 proposal 是否正确。对于大型变更，如果 proposal 方向错了，design 工作全部白费。
- **建议:** proposal 完成后自动暂停，问用户"proposal 确认后再继续 design？"。在 CONTINUE 逻辑中增加此检查点。
- **涉及文件:** `.claude/skills/qx-managing-changes/SKILL.md`

### Issue #6 — qx-stories 缺少依赖完整性验证步骤
- **环节:** `/qx-stories`
- **严重程度:** P2
- **状态:** 待修复
- **描述:** Story 拆分时依赖关系靠人工维护，skill 没有要求做依赖矩阵交叉验证。可能遗漏依赖导致 Sprint 规划时出现阻塞。
- **建议:** 在 Story 输出后增加"依赖矩阵验证"步骤：生成依赖图，检查是否有循环依赖或遗漏依赖。
- **涉及文件:** `.claude/commands/qx-stories.md`

### Issue #8 — qx-exec 缺少并行执行能力
- **环节:** `/qx-exec` (qx-exec skill)
- **严重程度:** P1
- **状态:** 待修复
- **描述:** 当前 qx-exec 严格串行执行所有任务（每个 Phase 一个 programmer agent → reviewer → 下一个 Phase）。即使 plan 中有互不依赖的任务（如定义多个独立枚举/常量文件），也无法并行派发，导致执行效率低。
- **建议:**
  1. **plan 模板增加并行标记语法** — 如 `<!-- parallel-start -->` / `<!-- parallel-end -->` 或任务元数据 `parallel: true`，让 plan 作者显式标注可并行区域
  2. **qx-exec 增加并行调度逻辑** — 检测到并行标记或无依赖任务组时，用 `run_in_background: true` 并行派发多个 implementer agent
  3. **考虑 worktree 隔离** — 并行任务如果修改重叠文件，可用 `isolation: "worktree"` 避免冲突
  4. **依赖图自动分析** — 根据任务的 Depends/Reads 字段自动计算可并行任务组（而非仅依赖手动标注）
- **涉及文件:** `.claude/skills/qx-exec/SKILL.md`, `.claude/qx/templates/plan-template.md`

### Issue #9 — qx-change propose/design/create 完成后无自动 git commit 步骤
- **环节:** `/qx-change` (qx-managing-changes skill)
- **严重程度:** P1
- **状态:** 待修复
- **描述:** `/qx-change propose` 和 `/qx-change design` 写完文档后不会自动提交。整个执行期间 proposal.md、design.md、.change.yaml 都是未跟踪文件，直到 `/qx-finishing` 才被发现并补提交。会话中断会导致这些文档丢失，且版本历史不完整。
- **建议:** 在 propose、design 操作末尾增加 `git add + git commit` 步骤，与 `/qx-plan`（plan.md 在 exec 时自然被提交）保持一致。
- **涉及文件:** `.claude/skills/qx-managing-changes/SKILL.md`

### Issue #10 — Sprint 规划环节容易被跳过，无强制检查点
- **环节:** `/qx-sprint` → `/qx-change create`
- **严重程度:** P2
- **状态:** 待修复
- **描述:** Story 拆分完成后，工作流规范建议运行 `/qx-sprint plan`，但缺少强制检查。A1 项目整个执行过程中 Sprint 规划从未运行，导致无任务优先级追踪和速度数据。
- **建议:** 在 `/qx-change create` 时检查是否有活跃 Sprint 状态，若无则提示"要先运行 /qx-sprint plan 吗？"。
- **涉及文件:** `.claude/skills/qx-managing-changes/SKILL.md`

### Issue #11 — per-task reviewer 未注入 design.md 结构约束
- **环节:** `/qx-exec` (per-task spec reviewer)
- **严重程度:** P1
- **状态:** 待修复
- **描述:** qx-exec 的 per-task spec reviewer 只校验"计划任务是否实现"，未注入 design.md 的结构约束（如 [FriendOf] 声明要求、子种子隔离要求）。导致 FriendOf 缺失和 subSeed 问题在 per-task 阶段被忽略或低估，最终由 Final Review 捕获（增加了一轮修复成本）。
- **建议:** 在 per-task spec reviewer prompt 中注入 design.md 的"决策约束"摘要（特别是结构图和关键设计决策），让 reviewer 能对照 design 检查实现。
- **涉及文件:** `.claude/skills/qx-exec/SKILL.md`

### Issue #12 — reviewer agent 可能读取过期文件缓存
- **环节:** `/qx-exec` (per-task reviewer)
- **严重程度:** P1
- **状态:** 待修复
- **描述:** Phase 6 的 spec reviewer 重新提出了 SceneType.Main 问题，而该问题已在 Phase 5 修复。reviewer 读取了修复前的代码版本（LLM 文件缓存或 orchestrator 注入的代码快照未更新）。
- **建议:** orchestrator 在派发 reviewer 时，应主动读取相关文件的当前内容并注入 reviewer prompt（而非依赖 implementer 提供的代码快照）；或在 reviewer prompt 中明确要求"先用 Read 工具读取最新文件"。
- **涉及文件:** `.claude/skills/qx-exec/SKILL.md`

### Issue #13 — 命令链路缺乏标准化引导，完整收尾依赖用户记忆
- **环节:** 整体工作流链路
- **严重程度:** P2
- **状态:** 待修复
- **描述:** `exec → verify → finishing → archive → compound` 这条收尾链路全部依赖用户逐步手动触发，每个 skill 的"建议下一步"是文字提示而非结构化 checklist。用户容易遗漏步骤（如 a1-foundation 跳了 finishing，a1-economy 差点漏掉变更文档提交）。
- **建议:** 每个 skill 结束时输出标准化的"收尾 checklist"，标注哪些步骤已完成、哪些必须手动执行。可考虑在 qx-change 中维护一个轻量的"变更状态机"（created→proposed→designed→planned→implemented→verified→archived）。
- **涉及文件:** 各 skill 的收尾步骤

### Issue #14 — Important 级别代码质量问题无后续追踪机制
- **环节:** `/qx-exec` code quality reviewer
- **严重程度:** P2
- **状态:** 待修复
- **描述:** quality reviewer 报告的 Important 问题（如 AEvent 冗余 await、测试边界覆盖不足）在 qx-exec 中没有强制处理。只有 Critical 被强制修复，Important 被记录在 reviewer 输出中但无追踪机制，实际上被遗忘。
- **建议:** Important 问题应自动写入一个"技术债务"待办列表（如 docs/tech-debt.md），在变更 archive 时做一次确认。
- **涉及文件:** `.claude/skills/qx-exec/SKILL.md`, `.claude/skills/qx-code-review/SKILL.md`
