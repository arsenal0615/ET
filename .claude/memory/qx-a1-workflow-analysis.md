# QX 工作流 A1 项目实际运转分析

> 数据来源：本会话上下文 + 上一会话压缩摘要（a1-foundation 部分部分来自摘要推断）
> 记录时间：2026-03-03
> 分析目的：迭代优化全兴工作流——淘汰不必要环节、优化待改进环节、新增必要环节

---

## 一、实际执行流水账

### 宏观阶段

```
GDD 创建 → 三方评审 → Story 拆分
  → [Sprint 规划：跳过]
  → a1-foundation 变更:
      qx-change create → propose → design → plan → exec(21任务/6阶段) → verify → archive
  → a1-economy 变更:
      qx-change create → propose → design → plan → exec(15任务/6阶段) → verify → finishing → archive → compound
```

### 命令执行记录

| 顺序 | 命令 | 谁触发 | 输出质量 | 备注 |
|------|------|--------|----------|------|
| 1 | /qx-gdd | 用户 | 良好 | |
| 2 | /qx-review | 用户 | 良好 | 三方评审，并行 Agent |
| 3 | /qx-stories | 用户 | 良好 | 拆分 Epic/Story |
| 4 | [Sprint 规划] | 未执行 | — | **遗漏** |
| 5 | /qx-change create a1-foundation | 用户 | 良好 | |
| 6 | /qx-change propose | 用户 | 良好 | |
| 7 | /qx-change design | 用户 | 良好 | |
| 8 | /qx-plan | 用户 | 良好 | 21 任务 |
| 9 | /qx-exec (a1-foundation) | 用户 | 基本良好 | 全串行，无并行 |
| 10 | /qx-verify (a1-foundation) | 用户 | 通过 | |
| 11 | /qx-change archive (a1-foundation) | 用户 | 良好 | |
| 12 | /qx-change create a1-economy | 用户 | 良好 | |
| 13 | /qx-change propose | 用户 | 良好 | |
| 14 | /qx-change design | 用户 | 良好 | |
| 15 | /qx-plan | 用户 | 良好 | 15 任务 |
| 16 | /qx-exec (a1-economy) | 用户 | 有错误 | 见错误清单 |
| 17 | /qx-verify (a1-economy) | 用户 | 通过 | |
| 18 | /qx-finishing | 用户 | 良好 | Push 到 main |
| 19 | /qx-change archive (a1-economy) | 用户 | 良好 | |
| 20 | /qx-compound | 用户 | 良好 | |

---

## 二、遗忘/跳过的环节

> **分类说明：**
> - 🔴 **AI 未提醒，Skill 设计缺失** — skill 规范无此步骤，AI 无提示依据
> - 🟡 **AI 提醒了，用户主动跳过** — AI 在输出中建议了，用户未执行
> - 🔵 **前一会话，无法确认** — 发生在上一会话，当前仅有压缩摘要，细节不可验证
> - ⚪ **规范未要求，但实践中有价值** — 不是遗忘，而是规范本身不完整

---

### 2.1 Sprint 规划从未执行

- **分类**: 🔴 **AI 未提醒，Skill 设计缺失**
- **实际情况**: a1-foundation 和 a1-economy 执行全程无 Sprint 规划
  - `/qx-stories` 完成后，MEMORY.md 记录"下一步: Sprint 规划"，但 AI 没有在用户执行 `/qx-change create` 时主动提醒
  - 用户直接开始变更，AI 直接处理，**双方均未提及 Sprint 规划**
- **根因**: `/qx-change create` 的 skill 规范没有"检查是否有活跃 Sprint"的步骤；MEMORY 里的"建议"不会自动触发提醒

---

### 2.2 /qx-impact 在 design 前未执行

- **分类**: 🔴 **AI 未提醒，Skill 设计缺失**
- **实际情况**: 两次 `/qx-change design` 都直接进入 design，未调用 `/qx-impact`
  - AI 在执行 design 时读取了部分文件，但**未主动提示"要先跑 /qx-impact 吗？"**
  - 用户也未主动发起
- **根因**: `/qx-impact` 是独立命令，在 `/qx-change design` 的 skill 规范里没有被提及为前置步骤

---

### 2.3 变更文档（proposal/design/.change.yaml）从未在创建后提交

- **分类**: 🔴 **AI 未提醒，Skill 设计缺失**
- **实际情况（以 a1-economy 为例，本会话可验证）**:
  - AI 写完 proposal.md 后，**没有执行 git commit，也没有提示用户提交**
  - AI 写完 design.md 后，同样**没有提交，也没有提示**
  - 直到 `/qx-finishing` 时，`git status` 显示 3 个未跟踪文件，才补提交
  - 用户全程**没有要求 AI 不要提交** — 不是用户跳过，而是 AI 和 skill 规范都没有这一步
- **根因**: `/qx-change propose` 和 `/qx-change design` 的 skill 规范里无 git commit 步骤，这是规范设计缺失

---

### 2.4 a1-foundation 未运行 /qx-finishing

- **分类**: 🔵 **前一会话，无法确认** + 部分推断
- **实际情况**:
  - 前一会话摘要显示：`qx-verify (a1-foundation)` 通过后，下一步是 `/qx-change archive`，无 `/qx-finishing` 记录
  - **无法确认 AI 是否在 verify 报告末尾建议了 /qx-finishing**
  - 如果建议了但用户跳过 → 🟡；如果 AI 没建议 → 🔴
  - 推断倾向：`/qx-verify` 的 skill 规范末尾是写"建议下一步: /qx-finishing"的，AI 可能建议了，但用户直接执行了 archive
- **实际影响**: a1-foundation 的代码在整个 a1-economy 执行周期内都是未推送状态，最终和 a1-economy 一起在 `/qx-finishing` 时推送

---

## 三、效果不达预期的环节（需改进）

### 3.1 qx-exec 的 per-task review 遗漏了跨文件架构问题

**观察到的问题:**
- a1-economy Phase 5：per-task spec reviewer 发现了 `SceneType.Main` 错误 ✅（有效）
- a1-economy Phase 6：per-task spec reviewer 将 subSeed 问题标为 I1（重要），Final Review 升级为 C1（Critical）
- Final Review 还捕获了 C2（FriendOf 缺失），这在 per-task 中未被发现

**原因分析:**
- per-task reviewer 只看当前任务的文件
- `[FriendOf]` 需要对照 design.md 的"结构图"才能发现，design.md 未注入 per-task reviewer
- subSeed 问题在 per-task 被低估（I1），在 Final Review 被正确定级（C1）——两轮 reviewer 对同一问题的严重性判断不一致

**改进建议:**
- per-task reviewer prompt 应注入 design.md 的"结构约束"部分
- Final Review 应明确说明"这是对 design.md 决策的合规性检查"

### 3.2 Phase 6 reviewer 读取了过期缓存文件

**观察到的问题:**
- Phase 6 spec reviewer 重新提出了 SceneType.Main 问题（I2），实际上 Phase 5 已修复
- reviewer 的文件读取结果是修复前的版本（LLM 文件缓存或 reviewer agent 未执行最新读取）

**改进建议:**
- reviewer prompt 中明确要求"现在读取文件的当前状态"，不依赖 implementer 提供的代码快照
- 可在 reviewer prompt 前自动读取文件并注入最新内容

### 3.3 qx-exec 的修复流程需要重派 implementer

**观察到的问题:**
- Phase 5 SceneType 错误由 orchestrator（主会话）直接修复，而不是重派 programmer agent
- 这违反了"fresh agent per task"原则，但效率更高
- Phase 4 subSeed 决策被 implementer 简化（不用 subSeed），Final Review 要求恢复——orchestrator 再次直接修复

**权衡:**
- 对于单行修复（SceneType 错误），直接修复比重派 agent 快得多
- 对于架构决策修复（subSeed），也是 orchestrator 直接修复更可控
- qx-exec 规范说"re-dispatch with same pre-loaded context"，实践中 orchestrator 直接修复更合适

**建议:** 在 qx-exec 规范中增加"简单修复可由 orchestrator 直接处理"的判断标准

---

## 四、低价值/高开销环节（淘汰候选）

### 4.1 每个 AEvent 早返回分支都写 `await ETTask.CompletedTask`

这是代码质量问题，不是工作流问题。但 reviewer 发现了这个 Important 问题，然后被忽略（Important 级别未强制修复）。

**工作流层面:** reviewer 发现 Important 级别问题后无后续追踪机制，容易被遗忘。

### 4.2 per-task quality review 对简单文件的价值有限

- 对于"追加 4 个常量到 AutoChessDefine.cs"这类任务，两轮 review（spec + quality）明显过重
- 建议：对"仅追加内容"类任务，可减少为单轮 spec 审查

---

## 五、高价值环节（不可省略）

| 环节 | 价值 | 证据 |
|------|------|------|
| **Final Review** | 捕获 per-task 遗漏的跨文件架构问题 | 发现 C1(subSeed) + C2(FriendOf) |
| **qx-verify 的逐项证据收集** | 强制实际运行命令，防止"口头完成" | 运行了 dotnet build + 代码读取 |
| **qx-compound 的 system-map 更新** | 持久化系统知识，下个变更可直接参考 | system-map 新增 autochess 章节 |
| **design.md 的技术决策记录** | 减少 implementer 猜测，reviewer 有依据 | 6 个 TD 给出明确的结构要求 |

---

## 六、命令链路的自然过渡问题

工作流规范里各命令有"建议下一步"，但缺少强制引导：

| 完成后 | 规范建议 | 实际触发方式 | 问题 |
|--------|----------|------------|------|
| qx-exec 完成 | 建议 /qx-verify | 用户手动执行 | 无强制提示 |
| qx-verify 通过 | 建议 /qx-finishing | 用户手动执行 | 无强制提示 |
| qx-finishing push | 建议 /qx-change archive | 用户手动执行 | 无强制提示 |
| qx-change archive | 建议 /qx-compound | 用户手动执行 | 无强制提示 |
| qx-compound 完成 | 建议下一步 | 无明确建议 | **链路断裂** |

**根因:** 每个 skill 独立运行，链路衔接依赖用户记忆。
**建议:** 每个 skill 在完成时输出标准化的"下一步"建议，形成明确的 checklist。

---

## 七、Agent 质量观察

| Agent 角色 | 准确性 | 问题 | 建议 |
|-----------|--------|------|------|
| Implementer (programmer) | 总体良好 | Phase 5 SceneType 错误；Phase 4 主动简化了 subSeed（合理但违反规范） | 需要 design.md 约束注入更完整 |
| per-task spec reviewer | 中等 | Phase 6 读取了过期文件；subSeed 严重性低估 | 需要注入 design.md；强制读取最新文件 |
| per-task quality reviewer | 良好 | 无重大问题 | |
| Final Review (holistic) | 优秀 | 捕获了两个 Critical | 不可省略 |
| Fix re-reviewer (haiku) | 良好 | 快速、准确 | 复查用 haiku 模型是正确的 |

---

## 八、变更文档与代码的时序对齐问题

**理想时序:**
```
qx-change create → 提交 .change.yaml
qx-change propose → 提交 proposal.md
qx-change design → 提交 design.md
qx-plan → 提交 plan.md
qx-exec 每个任务 → 提交代码
qx-verify → 更新 plan.md 完成标准
qx-finishing → push 所有内容
qx-change archive → 移动目录 + 提交
```

**实际时序（a1-economy）:**
```
qx-change create → .change.yaml 未提交
qx-change propose → proposal.md 未提交
qx-change design → design.md 未提交
qx-plan → plan.md 在 qx-exec 第一阶段时才被提交（因为任务需要读它）
qx-exec 每个任务 → 代码正确提交
qx-verify → 更新了 plan.md 完成标准（此时才提交）
qx-finishing → 发现 3 个未跟踪文件，补提交 proposal/design/.change.yaml
```

---

## 九、新发现的工作流问题（补充到 qx-workflow-issues.md）

| # | 问题 | 环节 | 严重程度 |
|---|------|------|---------|
| #9 | propose/design/create 完成后无自动 git commit 步骤 | qx-change | P1 |
| #10 | Sprint 规划环节容易被跳过，无强制检查点 | qx-sprint | P2 |
| #11 | per-task reviewer 未注入 design.md 约束，漏检 FriendOf 等结构要求 | qx-exec | P1 |
| #12 | reviewer 可能使用 LLM 缓存的文件内容（非最新）| qx-exec | P1 |
| #13 | 命令链路缺乏自动化引导，用户需记忆"exec→verify→finishing→archive→compound"完整链路 | 整体 | P2 |
| #14 | Important 级别的代码质量问题没有后续追踪机制 | qx-exec / qx-code-review | P2 |

---

## 十、优化优先级排序

### P0（阻塞）
- 无

### P1（应改）
1. **变更文档自动提交** — propose/design 后加 git commit 步骤（#9）
2. **per-task reviewer 注入 design.md 约束** — 减少 FriendOf、架构决策的遗漏（#11）
3. **reviewer 强制读取最新文件** — 避免读取过期缓存（#12）

### P2（建议）
4. **Sprint 规划检查点** — /qx-change create 时检测是否有活跃 Sprint（#10）
5. **命令链路标准化引导** — 每个 skill 结束时输出"下一步"checklist（#13）
6. **Important 问题追踪** — 代码质量 Important 问题建立待办跟踪（#14）
7. **per-task 审查权重调整** — "仅追加"类任务减少为单轮审查（无编号，新建议）

---

## 十一、a1-shop 变更执行记录

> 开始时间：2026-03-03（新会话）

### 步骤记录

| 顺序 | 步骤 | Token | 备注 |
|------|------|-------|------|
| 1 | /qx-change create a1-shop | 不可用（主会话） | 检查重复 → 创建目录 → 写 .change.yaml |
| 2 | /qx-change propose | 不可用（主会话） | 读 system-map + 现有代码（MatchRoom/MatchPlayer/Define/UnitTemplateDef/PrngPurpose/TestHelper）后写 proposal.md，提交 |
| 3 | /qx-change design | 不可用（主会话） | 读 AutoChessConfigLoader（确认模板ID范围，2费ID=1-7）+ system-map 后写 design.md（6个决策），提交 |
| 4 | /qx-plan | 不可用（主会话） | 读 MatchRoomFactory/MatchRoomSystem/MatchPlayerSystem + ecs-patterns，写 plan.md（13任务，7组），提交 |
| 5 | /qx-exec Task 1.1 | ~37K（上会话 sub-agent） | 创建 ShopOffer.cs + SharedPoolComponent.cs；发现 ET0032 规则：非 Entity 类需 [EnableClass] |
| 6 | /qx-exec Task 1.2+1.3（并行） | ~34K+33K | ShopComponent.cs 创建；MatchPlayer.PlayerIndex 字段添加 |
| 7 | /qx-exec Task 2.1+2.2+2.3（并行） | ~36K+38K+33K | SharedPoolComponentSystem + ShopComponentSystem + MatchPlayerSystem Awake |
| 8 | /qx-exec Task 3.1+4.1+5.1（并行） | ~39K+35K+42K | ShopService 核心 + FirstRoundGiftService + MatchRoomFactory 集成；5.1 发现需 [FriendOf(MatchPlayer)] |
| 9 | /qx-exec Task 3.2+5.2+6.1（并行） | ~41K+45K+36K | TryBuy（内联相位门控避免ET0013）+ EliminatePlayer（内联Offer回收）+ PhaseChangedEventHandler_Shop（修正 args.MatchRoomId 模式）|
| 10 | /qx-exec Task 3.3+7.1（并行） | ~35K+55K | TrySell + TestShop 集成测试（7.1 额外修复了 AutoChessTestHelper 的 ET0002 错误）|
| 11 | 计划 checkbox 更新 | 主会话 | 13/13 勾选，完成标准更新 |

### 关键技术发现

1. **ET0032 [EnableClass]**：非 Entity 的普通 C# 类在 Model 程序集中必须标注 `[EnableClass]`（ShopOffer 案例）
2. **ET0013 循环依赖规避**：ShopService 中内联相位判断（`Deployment || Battle`）替代调用 `RoundFSMComponentSystem.CanOperate`；EliminatePlayer 中内联 Offer 回收替代调用 `ShopService.ReturnOffersToPool`
3. **PhaseChangedEvent 模式**：事件无 Room 引用字段，必须通过 `matchComp.GetRoom(args.MatchRoomId)` 获取；`args.Round`（不是 `room.CurrentRound`）
4. **[Event(SceneType.Map)]**：PhaseChangedEvent handler 使用 Map 而非 Server
5. **Wave 并行效果**：本次执行采用 4 个波次并行，节省大量时间；Task 3.3 和 Task 7.1 并行时，3.3 较早完成（89s vs 244s），7.1 编译时可用

### 剩余预存在问题（非本次引入）

编译后剩余 12 个错误（非本变更引入）：
- AutoChessConfigLoader.cs：ET0004/ET0015（静态字段声明），需添加 [StaticField] 标注
- MatchComponentSystem.cs:27：CS0411（GetChild 类型参数无法推断）
- MatchRoomSystem.cs:137：CS1061（ETTask.Coroutine 扩展方法找不到）
- ET0013：RoundFSMComponentSystem ↔ MatchRoomSystem 循环依赖

---

## 附：已记录问题总览（qx-workflow-issues.md）

| # | 环节 | 严重程度 | 状态 |
|---|------|---------|------|
| #1 | qx-gdd 缺少导入模式 | P1 | 待修复 |
| #2 | GDD 模板缺少填写指引 | P2 | 待修复 |
| #3 | GDD 模板缺少文档索引 | P2 | 待修复 |
| #4 | 三方评审缺少过程可见性 | P1 | 待修复 |
| #5 | 三方评审缺少作者回应闭环 | P2 | 待修复 |
| #6 | qx-stories 缺少依赖完整性验证 | P2 | 待修复 |
| #7 | qx-change 的 propose→design 缺少检查点 | P2 | 待修复 |
| #8 | qx-exec 缺少并行执行能力 | P1 | 待修复 |
| #9~#14 | 本次分析新发现（见上） | P1-P2 | 待记录 |
