# QX A1 工作流深度分析 — a1-synergy

> 分析对象：a1-synergy（羁绊系统）变更的完整生命周期
> 工作流路径：proposal -> specs -> design -> plan -> exec -> verify -> finishing
> 跨越会话数：3（design+plan | exec | verify+finishing+hook修复）

---

## 一、步骤执行总览

| # | Skill | 输入 | 产出 | 耗时估计 | 结果 |
|---|-------|------|------|----------|------|
| 1 | /qx-managing-changes | a1-unit 完成后启动 | proposal + 5 specs + design（7个技术决策） | ~30min | 完成，有1个引导错误 |
| 2 | /qx-writing-plans | design.md | plan.md（7阶段14任务） | ~15min | 完成，质量好 |
| 3 | /qx-exec | plan.md | 14个任务全部实现，8个git提交，17个文件 | ~2h（跨会话） | 完成，Final Review 捕获1个Critical |
| 4 | /qx-verification | exec 完成后 | 构建验证通过（12 errors 全预存） | ~5min | 通过 |
| 5 | /qx-finishing | verify 通过后 | hook修复 + 提交 + 收尾选项 | ~10min | 完成 |

---

## 二、逐步深度分析

### Step 1: /qx-managing-changes (create+propose+spec+design)

**做对了什么：**
- Proposal 精确划定了范围边界（本变更 vs E7 战斗模拟器），避免了范围蔓延
- 7个技术决策（TD-1~TD-7）全部有替代方案分析和理由，不是拍脑袋决定
- SynergyComponent 挂 MatchPlayer（TD-1）、纯C#数据类（TD-2）、Modifiers不修改UnitInfo（TD-3）都是正确的架构决策
- 影响分析清单完整，准确预测了需要修改的文件

**引入的问题：**
- **Issue #1: CLAUDE.md 引导错误** — 概览流程图把三方评审画成必经步骤，导致 AI 在 design 完成后错误推荐 `/qx-review`。用户不需要三方评审时造成困惑。后来修复了 CLAUDE.md。
- **影响范围**：仅造成一次错误推荐，用户忽略后正常继续，未造成实质浪费。

**对后续的影响：**
- Design 文档质量高，plan 和 exec 阶段几乎没有返工需要重新解读 design 意图
- 技术决策表（TD-1~7）在 exec 审查中被直接引用作为对照标准，证明了前期设计的价值
- 唯一遗漏：design 没有明确要求 SynergyComponentSystem（IAwake/IDestroy 的 EntitySystem 注册类），这个遗漏在 exec Final Review 中才被发现

**收敛性评估：** 良好。5个 spec 精确定义了 5 个能力边界，design 的 7 个决策消除了主要的架构歧义。

---

### Step 2: /qx-writing-plans

**做对了什么：**
- 任务粒度恰当：14个任务，每个 ~30min 可完成，适合单个 Agent 一次性处理
- 依赖关系正确：数据模型(1.x) → 核心逻辑(2.x) → Goblin(3.x) → 阶段处理(4.x) → 集成(5.x) → 测试(6.x) → 收尾(7.x)
- 每个任务的 Context 块（Depends/Reads/Why）完整，exec 阶段的上下文注入基本不需要补充
- Steps 包含了具体代码片段和编译命令，降低了 Agent 的自由发挥空间

**潜在问题：**
- 计划中 Task 1.3（SynergyComponent）声明了 `IAwake, IDestroy`，但没有对应的 Task 要求创建 `SynergyComponentSystem`。这是 ET 框架的要求：声明了 IAwake/IDestroy 的 Entity 必须有对应的 `[EntitySystemOf]` System 类注册生命周期方法。**这个遗漏直到 exec Final Review 才被发现。**
- **根因分析**：plan 作者对 ET 框架的 EntitySystem 注册机制理解不完整。design 也没有在"新增类型清单"中列出 System 类。这说明 design → plan 的知识传递在框架层面有盲点。
- **建议**：plan 模板或 project-rules 中应增加检查项："声明了 IAwake/IDestroy/IDeserialize 的 Entity 是否有对应的 EntitySystemOf System 类？"

**对后续的影响：**
- 14个任务中 13 个一次通过，1 个遗漏在 Final Review 修复（约 5min 额外工作）
- 任务间依赖关系正确，没有出现后续任务阻塞的情况

**收敛性评估：** 优秀。计划本身是正确的路径规划，唯一的"坑"是 ET 框架的隐式要求遗漏。

---

### Step 3: /qx-exec (多 Agent 执行)

**做对了什么：**
- 前 5 个数据模型任务（1.1~1.5）通过子 Agent 高效完成，每个任务一个 Agent，无上下文污染
- 两阶段审查（规格合规 + 代码质量）在 per-task 级别运行，捕获了几个小问题（如不必要的 FriendOf 声明）
- Final Review 捕获了 **Critical 级别** 的遗漏（缺 SynergyComponentSystem），这是 per-task 审查遗漏的跨文件架构问题
- 修复后立即验证编译通过

**引入的问题：**
- **跨会话恢复**：exec 执行到一半时上下文压缩，需要从 conversation summary 恢复。恢复后：
  - 背景 Agent（Task 2.2, 3.1）已完成但产出需要重新验证（读取文件确认）
  - 恢复过程本身是成功的，但消耗了额外上下文来重建状态
- **背景 Agent 通知风暴**：5个旧 Agent 的 task-notification 在恢复后集中到达，每个触发一轮 hook → 9+ 次重复 block（Issue #2/#3/#4）
- **GC 分配问题（I1）和模板缓存问题（I2）**：质量审查发现了 Important 级别问题但未要求修复，记入 tech-debt

**qx-exec 设计的有效性：**

| 机制 | 有效性 | 证据 |
|------|--------|------|
| 每任务一个 Agent | 高 | 13/14 任务一次通过，Agent 间无上下文干扰 |
| 预加载上下文（static_context） | 高 | Agent 不需要自己去读 CLAUDE.md 或 project-rules |
| 规格审查 | 中 | 捕获了小问题，但遗漏了 SynergyComponentSystem |
| 质量审查 | 中 | 发现了 GC 和缓存问题，但对框架合规检查不如构建分析器 |
| Final Review | 高 | 唯一捕获跨文件架构问题的环节 |
| task_outputs 依赖链 | 高 | 后续任务正确接收了前置任务的产出 |
| TodoWrite + 复选框双重跟踪 | 中 | 跟踪有效但跨会话恢复时需要重新对齐 |

**关键发现：per-task 审查 vs Final Review 的互补性**

per-task 审查的视野局限在单个任务的文件范围内。SynergyComponentSystem 的遗漏之所以被 per-task 审查忽略，是因为：
- Task 1.3 创建了 SynergyComponent 声明 IAwake/IDestroy — 审查通过（"声明了接口"）
- Task 2.1 创建了 SynergyService — 审查通过（"逻辑正确"）
- 但没有任何单个 Task 负责创建 SynergyComponentSystem
- Final Review 从整体角度看到了声明（IAwake）和注册（EntitySystemOf）之间的断裂

**结论：Final Review 不可省略，它与 per-task 审查的价值是正交的。**

**收敛性评估：** 整体良好。14 任务全部完成 + 1 个 Critical 修复。但跨会话恢复和 hook 风暴引入了噪音。

---

### Step 4: /qx-verification

**做对了什么：**
- 运行了完整构建（`dotnet build ET.sln`）
- 正确识别 12 个错误全为预存（与 a1-synergy 无关）
- 逐项检查了 14/14 任务完成、8 个提交、17 个文件变更

**潜在问题：**
- 验证只做了编译，没有运行测试（项目当前没有自动化测试命令）
- AutoChessTestHelper 的测试需要运行时环境（Unity 或 ET Server），CLI 无法执行

**收敛性评估：** 符合预期。在 CLI 能力范围内做了最大程度的验证。

---

### Step 5: /qx-finishing

**做对了什么：**
- 发现了 stop-hook 去重问题并主动修复（Issue #2/#3/#4）
- 修复方案简洁有效（submit hook 过滤 + stop hook 时间窗口去重）
- 冒烟测试验证了修复效果
- 正确识别了"在 release9.0 分支直接工作"的特殊情况，调整了收尾选项

**引入的问题：**
- finishing 阶段变成了"修复工作流本身"而非"收尾项目工作"，职责混淆
- 理想情况下 hook 修复应该作为独立变更（/qx-debug），而非嵌入 finishing 流程

**收敛性评估：** 完成了收尾，但额外修复工作延长了流程。

---

## 三、全链路问题分析

### 3.1 错误传播链

```
design 遗漏 EntitySystemOf 类
  └→ plan 没有对应 Task
      └→ exec per-task 审查无法发现（跨文件问题）
          └→ exec Final Review 捕获并修复（+5min）
```

**评价：** 防线虽然晚，但最终生效。理想路径是 design 阶段就列出所有需要的类（包括 System 类）。

### 3.2 上下文效率

| 阶段 | 上下文利用 | 问题 |
|------|-----------|------|
| design | 高 — 一次会话完成全部设计文档 | 无 |
| plan | 高 — design 文档完整，plan 一次成型 | 无 |
| exec | 中 — 跨会话压缩丢失了执行细节 | 恢复需额外验证背景 Agent 产出 |
| verify | 高 — 简洁明确 | 无 |
| finishing | 低 — hook 修复占据了大量上下文 | 职责混淆 |

### 3.3 工作流自动化问题

| 问题 | 严重性 | 影响 | 状态 |
|------|--------|------|------|
| CLAUDE.md 三方评审引导错误 | 低 | 一次错误推荐 | 已修复 |
| stop-hook 无去重 | 高 | 9+ 次重复 block，大量上下文浪费 | 已修复 |
| task-notification 触发 hook | 高 | 背景 Agent 通知驱动无限循环 | 已修复 |
| 跨会话 Agent 通知延迟 | 中 | 旧会话的 Agent 结果在新会话到达 | 已缓解（不再触发循环） |

---

## 四、收敛性总评

### 4.1 信息衰减分析

```
GDD 定义 → proposal 保留度 ~95%（范围清晰）
proposal → design 保留度 ~90%（遗漏 EntitySystemOf 类）
design → plan 保留度 ~95%（任务粒度好，Context 块完整）
plan → exec 保留度 ~85%（跨会话压缩损失，1个Critical遗漏）
exec → verify 保留度 ~100%（构建验证无损）
```

总体信息衰减：GDD → 最终代码 约 70%（合理范围内）。

### 4.2 各步骤对收敛的贡献

| 步骤 | 收敛贡献 | 发散/干扰 | 净效果 |
|------|----------|-----------|--------|
| managing-changes | 消除了架构歧义，定义了精确范围 | 1次错误推荐（三方评审） | 强收敛 |
| writing-plans | 将设计转化为可执行路径 | 遗漏 EntitySystemOf Task | 强收敛 |
| exec | 14 任务全部实现 | 跨会话恢复噪音 + hook 风暴 | 强收敛（有干扰） |
| verification | 确认实现完整性 | 无 | 收敛确认 |
| finishing | 提交 + 收尾 | hook 修复混入 | 弱收敛（因混入修复） |

### 4.3 下一步建议的正确性

| 步骤完成后 | Hook 推荐 | 实际需要 | 正确？ |
|-----------|-----------|----------|--------|
| /qx-change design | /qx-plan | /qx-plan | 正确 |
| /qx-plan | /qx-exec | /qx-exec | 正确 |
| /qx-exec | /qx-finishing | /qx-verify（应先验证） | 偏差 |
| /qx-verify | /qx-finishing | /qx-finishing | 正确 |
| /qx-finishing | /qx-change archive | /qx-change archive | 正确 |

**Issue：exec 完成后应推荐 /qx-verify 而非直接 /qx-finishing。** 工作流转换表中 `/qx-exec → /qx-finishing` 跳过了验证步骤。

---

## 五、工作流改进建议

### P0（应立即修复）

1. **工作流转换表修正**：`/qx-exec` → 推荐 `/qx-verify` 而非 `/qx-finishing`
2. **Plan 模板增加 EntitySystem 检查项**：声明了 IAwake/IDestroy 的 Entity 必须有对应的 [EntitySystemOf] System 类 Task

### P1（下次迭代改进）

3. **Design 新增类型清单应包含 System 类**：design.md 的"新增类型清单"只列了 Entity/数据类/Service，应增加 System 类列
4. **Finishing 不应承载修复职责**：hook 修复应该通过 /qx-debug 独立处理，finishing 保持纯粹的收尾流程
5. **exec 跨会话恢复指南**：在 qx-exec skill 中增加恢复协议（如何验证背景 Agent 产出）

### P2（长期优化）

6. **per-task 审查增加跨文件检查**：至少检查 "本 Task 创建的 Entity 是否有对应的 System 注册类"
7. **Hook 状态隔离**：每个会话有独立的 hook 状态，避免跨会话干扰
8. **exec 进度持久化**：exec.json 应记录每个 Task 的 agent 输出摘要，跨会话恢复时不需要重新验证

---

## 工作流步骤记录

| # | Skill | 摘要 | 问题 |
|---|-------|------|------|
| 21 | /qx-managing-changes | create 子命令启动，等待用户输入变更名称 | |
| 22 | (follow-up) | 追加记录行，继续等待用户输入变更名称 | |
| 23 | (follow-up) | 分析依赖图，推断下一个变更为 a1-skill（E6 技能系统），等待用户确认 | |
| 25 | /qx-managing-changes | propose 完成：发现配置层已完整，提案聚焦技能执行引擎（5 capabilities） | |
| 26 | /qx-managing-changes | spec 完成：5 个 capability 共 17 需求 54 场景，覆盖触发/目标/效果/法力/执行管线 | |
| 28 | /qx-writing-plans | plan 完成：8 组 15 任务，数据模型→HexUtil→Mana→Trigger→Target→Effect→Executor→集成 | |
| 29 | /qx-exec | 开始执行：构建静态上下文，分派 Task 1.1/1.2/1.3 三个并行 Agent（数据模型层） | 进行中，尚未完成 |
| 30-42 | /qx-exec | 16/16 任务全部完成，8 次提交，Final Review APPROVED，3 个 Important 记录到 tech-debt | 跨上下文续接（context compaction），stop hook 误触一次 |
| 43 | /qx-verify | 16/16 任务 checkbox 全部 [x]，12 文件存在，编译零新增错误，验证通过 VERIFIED | |
| 44 | /qx-finishing | 收尾确认：10 个提交已在 release9.0 上，无独立分支需合并，展示 4 选项等待用户选择 | |
| 45 | (follow-up: /qx-finishing) | 用户询问下一步推荐，建议 compound → archive → sprint status 顺序 | |
| 46 | git push | 推送 release9.0 到远程，10 个提交同步完成 | |
| 47 | /qx-managing-changes | archive 完成：tech-debt 3项全部推迟，5个specs同步到docs/specs/，变更移至archive/2026-03-06-a1-skill/ | |
| 48 | /qx-compound | 提取经验：system-map新增技能子系统章节，MEMORY新增坑7+2个模式，验收进度更新 | |
| 49 | /qx-managing-changes | create 子命令启动，无活跃变更，等待用户输入新变更名称 | |
| 50 | (follow-up: /qx-managing-changes) | 创建 battle-system 变更目录和 .change.yaml，建议下一步 propose |
| 51 | (follow-up: /qx-managing-changes) | 全自动模式：从 propose 一路执行到 compound 完成，含 proposal+10specs+design+plan(15task)+11新文件+5修改+5测试+archive+specs同步+system-map更新+知识提取 | 1个新增编译错误(SimpleRng ET0004)已修复(移到Model程序集)；IEnumerable不可索引需foreach+break | |
| 57-63 | /qx-change create→compound | 全自动模式（e9-networking 网络与同步）：create→propose→5specs→design(6决策)→plan(10组18任务)→exec(31文件5436行)→verify(4维全PASS)→archive(5specs同步)→compound(system-map+MEMORY+5新坑模式) | ET0031(Proto new→Create)在5文件重复出现需完整重写；CombatDamageType/CombatWinner枚举值猜错；context compaction中途触发1次 |
