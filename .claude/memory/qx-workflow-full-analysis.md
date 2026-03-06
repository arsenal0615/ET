# QX 全兴工作流全局分析报告

> 分析范围：A1 自走棋验收周期（5 个变更，2026-03-02 ~ 2026-03-06）
> 数据来源：workflow-record.json、qx-workflow-issues.md、5 个变更归档、autochess-patterns.md

---

## 一、工作流全景

### 1.1 变更执行时间线

| # | 变更 | 日期 | 流程路径 | 任务数 | 发现的"坑" |
|---|------|------|----------|--------|------------|
| 1 | a1-foundation | 03-02 | proposal → design → plan → exec | ~16 | Event SceneType.Map (坑1) |
| 2 | a1-economy | 03-03 | proposal → design → plan → exec | ~8 | FriendOf 完整性 (坑2) |
| 3 | a1-shop | 03-03 | proposal → design → plan → exec | ~8 | EnableClass (坑3), ET0013 循环依赖 (坑4) |
| 4 | a1-unit | 03-04 | proposal → design → plan → exec | ~12 | 命名空间冲突 global:: (坑5) |
| 5 | a1-synergy | 03-05~06 | proposal → specs → design → plan → exec → verify → finishing → archive → compound | ~14 | EntitySystemOf 缺失 (坑6) |

**关键观察：**
- 前 4 个变更的流程路径较短（proposal → design → plan → exec），没有 verify/finishing/archive/compound
- 第 5 个变更（synergy）首次走完了**完整的变更生命周期**，包含了 specs 阶段
- QX 工作流的完整流程是在 synergy 阶段才真正验证的

### 1.2 流程演化

```
a1-foundation:  proposal → design → plan → exec ................... (最小路径)
a1-economy:     proposal → design → plan → exec ................... (同上)
a1-shop:        proposal → design → plan → exec ................... (同上)
a1-unit:        proposal → design → plan → exec ................... (同上)
a1-synergy:     proposal → specs → design → plan → exec → verify → finishing → archive → compound (完整路径)
```

**分析：** 前 4 个变更缺少 verify/archive/compound 步骤。这意味着：
1. 没有形式化验证步骤 — 编译错误可能被遗留
2. 没有归档 — 变更文档没有标准化的完成标记（后来补充归档）
3. 没有复合积累 — 每个变更的经验教训没有被及时提取

这不完全是问题 — 前期变更本身是在"探索建立 QX 工作流"，流程自然不完整。但这意味着 **QX 工作流的端到端验证只有 1 次数据点（synergy）**。

---

## 二、各阶段有效性分析

### 2.1 Proposal 阶段

**跨变更对比：**

| 变更 | Proposal 长度 | 范围边界定义 | 依赖分析 | 评分 |
|------|--------------|-------------|----------|------|
| foundation | ~30 行 | 清晰（4 块基础设施） | 无前置依赖 | 良好 |
| economy | ~30 行 | 清晰（E2 Story 2.1/2.2） | 准确引用 foundation | 良好 |
| shop | ~30 行 | 清晰（E3 与 E4 边界） | 准确引用 economy | 优秀 |
| unit | ~30 行 | 优秀（E4 与 E3/E5 边界） | 准确引用 shop | 优秀 |
| synergy | ~30 行 | 优秀（明确排除 E7 战斗逻辑） | 准确引用 unit | 优秀 |

**趋势：** Proposal 质量随迭代提升。后期 proposal 的边界定义越来越精确（从"4 块基础设施"到"明确列出不包含的内容"）。

**有效性评估：** 高。每个 proposal 都成功阻止了范围蔓延，后续阶段没有出现"做着做着发现需要更多东西"的情况。

### 2.2 Specs 阶段

**只有 synergy 使用了 specs**：5 个独立 spec 文件，每个定义一个 capability 的边界和验收标准。

**对比：** 前 4 个变更没有 specs，直接从 proposal 跳到 design。
- 对于简单变更（economy: 8 个任务），跳过 specs 是合理的
- 对于复杂变更（synergy: 14 个任务，5 个独立 capability），specs 提供了有价值的中间层

**建议：** 按复杂度分级 — 任务数 <= 8 可跳过 specs，> 8 建议使用。

### 2.3 Design 阶段

**跨变更对比：**

| 变更 | 技术决策数 | 遗漏 | 对 plan 的支撑 |
|------|-----------|------|---------------|
| foundation | ~5 | 无明显遗漏 | 良好 |
| economy | ~4 | 无 | 优秀（ApplyDelta 模式直接转化为 plan） |
| shop | ~5 | 无 | 优秀（预扣机制写入 design） |
| unit | ~6 | 无 | 优秀（门面模式、副作用前预检写入 design） |
| synergy | 7 (TD-1~7) | **遗漏 EntitySystemOf** | 良好（但有盲点） |

**关键发现：** Design 阶段在**业务逻辑**维度表现优秀，但在**框架机械性要求**维度有盲点。EntitySystemOf 遗漏不是设计决策失误，而是对 ET 框架的隐式契约认知不完整。

**根因：** design 模板的"新增类型清单"只列 Entity/数据类/Service，没有提示需要列出 System 注册类。

### 2.4 Plan 阶段

**跨变更对比：**

| 变更 | 任务粒度 | Context 完整性 | 依赖链正确性 | 一次通过率 |
|------|---------|---------------|-------------|-----------|
| foundation | ~16 任务 | 基本完整 | 正确 | ~95% |
| economy | ~8 任务 | 完整 | 正确 | ~100% |
| shop | ~8 任务 | 完整 | 正确 | ~90% (ET0013 问题) |
| unit | ~12 任务 | 完整 | 正确 | ~90% (global:: 问题) |
| synergy | 14 任务 | 优秀 | 正确 | ~93% (1 Critical 遗漏) |

**趋势：** Plan 质量稳定在高水平。Context 块（Depends/Reads/Why）的引入显著降低了 exec 阶段 Agent 的自由发挥空间。

**值得注意的模式：** 每个变更的 plan 都至少有 1 个"坑"需要在 exec 中修复 — 这不是 plan 的问题，而是 ET 框架的隐式规则太多，plan 无法 100% 预测所有框架约束。

### 2.5 Exec 阶段

**每任务一 Agent 模式的效果：**
- 优势：上下文隔离，任务间无干扰，13~16 个任务中绝大多数一次通过
- 劣势：per-task 审查视野局限在单个任务文件范围，无法发现跨文件架构问题
- Final Review 弥补了 per-task 审查的盲点（synergy 中捕获了 SynergyComponentSystem 遗漏）

**跨会话恢复问题：**
- synergy 的 exec 横跨会话，恢复时背景 Agent 通知引发 hook 风暴
- 前 4 个变更可能也有类似情况但未被记录（因为当时没有完整的 workflow-record 系统）

### 2.6 Verify / Finishing / Archive / Compound

**只有 synergy 走完了这 4 步。** 评估：

| 阶段 | 职责 | 实际表现 | 问题 |
|------|------|---------|------|
| verify | 构建验证 | 通过（12 预存错误，0 新增） | 无自动化测试可执行 |
| finishing | 收尾/提交 | 完成，但**混入了 hook 修复** | 职责不纯粹 |
| archive | 归档变更文档 | 完成（含 tech-debt.md） | 正常 |
| compound | 提取知识 | 更新了 autochess-patterns.md、system-map.md、MEMORY.md | 有效 |

---

## 三、Hook 系统分析

### 3.1 Hook 架构

```
用户输入 → qx-workflow-record-hook (Submit)
              标记 pending_record=true
              ↓
AI 回答完成 → qx-workflow-stop-hook (Stop)
              读取 pending_record
              注入记录指令 + 推荐下一步
              清除 pending_record
```

### 3.2 已发现并修复的问题

| # | 问题 | 根因 | 修复方案 | 效果 |
|---|------|------|---------|------|
| 1 | CLAUDE.md 三方评审引导错误 | 概览流程图把可选步骤画成必经 | 修改 CLAUDE.md 措辞 | 消除错误推荐 |
| 2 | stop-hook 无去重 | 同一 pending_record 在每次 AI 回复时都触发 | 增加 `_last_blocked_trigger` + 5min 窗口 | 阻止重复 block |
| 3 | task-notification 触发记录 | submit-hook 不区分通知和用户输入 | submit-hook 跳过 task-notification/hook-feedback | 通知不再标记 pending |
| 4 | 跨会话 Agent 通知 | 旧会话 Agent 的结果在新会话到达 | #2/#3 修复后不再触发循环 | 已缓解 |

### 3.3 尚存的 Hook 设计问题

**问题 A：转换表不完整**
- `/qx-dev-exec` → 推荐 `/qx-verify`（已修复，见 stop-hook 第 45 行）
- 但工作流转换表是**硬编码**在 hook 中的，每次流程变更需要同步修改两处（CLAUDE.md + hook）

**问题 B：记录指令的价值存疑**
- stop-hook 注入"请追加一行记录到日志"的指令
- 实际上 AI 每次被 block 后追加的"一句话摘要"信息量很低
- a1-synergy 的分析证明：**深度分析报告**远比逐步简要记录有价值
- 建议：stop-hook 的记录指令从"追加一行"改为"更新分析报告"，或完全移除（让 /qx-compound 一次性生成）

**问题 C：follow-up 步骤膨胀**
- workflow-record.json 中 20 个步骤里只有 7 个是 QX 命令，其余 13 个是 follow-up
- follow-up 包括：打开文件、Agent 通知、用户反馈 — 噪声大于信号
- 建议：follow-up 步骤不计入 step_count，或单独统计

**问题 D：Session 隔离不彻底**
- stop-hook 有 session_id 检查（第 87 行），但 submit-hook 对 isActive 的判断只看时间戳，不看 session_id
- 新会话继承旧会话的 active 状态可能导致意外行为

---

## 四、知识积累效果分析

### 4.1 "坑"的发现与传播

| 坑 | 首次发现 | 如何被发现 | 写入知识库 | 后续是否避免 |
|------|---------|-----------|-----------|-------------|
| 坑1 Event SceneType.Map | foundation exec | 编译报错 | autochess-patterns.md | 是 — 后续全部用 Map |
| 坑2 FriendOf 完整性 | economy exec | 编译报错 | autochess-patterns.md | 是 |
| 坑3 EnableClass | shop exec | 编译报错 ET0032 | autochess-patterns.md | 是 — unit 中 UnitInfo 直接用 |
| 坑4 ET0013 循环依赖 | shop exec | 编译报错 | autochess-patterns.md | 是 — unit 中内联了逻辑 |
| 坑5 命名空间冲突 | unit exec | 编译报错 | autochess-patterns.md | 是（statesync 已修复） |
| 坑6 EntitySystemOf | synergy exec | Final Review 发现 | autochess-patterns.md | 待验证（下个变更） |

**积累效果：** 复合积累机制在**编译期可检测的问题**上表现优秀 — 坑 1~5 一旦发现就被记录，后续变更不再踩。但**运行时才暴露的问题**（坑 6）的传播链更长，需要额外的审查机制。

### 4.2 模式的传播

autochess-patterns.md 记录了 8 个开发模式。验证其传播效果：

| 模式 | 首次建立 | 后续复用次数 |
|------|---------|-------------|
| 静态服务类 | economy | 3 次（shop/unit/synergy） |
| 预扣机制 | shop | 0 次（仅 shop 使用） |
| DeriveSubSeed | foundation | 3 次（economy/shop/synergy） |
| 纯C#数据类 | unit | 1 次（synergy 的 TraitSnapshot） |
| 副作用前预检 | unit | 0 次（仅 unit 使用） |
| 门面服务类 | unit | 0 次（仅 unit 使用） |
| Modifiers快照 | synergy | 0 次（刚建立，待 E7 验证） |
| EntitySystemOf配对 | synergy | 0 次（刚建立，待验证） |

**分析：** 高复用模式（静态服务类、DeriveSubSeed）的积累价值最大。单次使用的模式（预扣机制、门面服务类）的记录价值更多在于**避免未来重新发明**，而非直接复用。

---

## 五、工作流整体评估

### 5.1 优势

1. **proposal 的范围控制效果显著** — 5 个变更无一发生范围蔓延
2. **design → plan 的传递保真度高** — 技术决策表直接转化为可执行任务
3. **exec 的多 Agent 模式高效** — 任务一次通过率 > 90%
4. **复合积累机制有效** — 坑不重复踩，模式可复用
5. **变更之间的渐进式构建** — 每个变更的 proposal 精确引用前置变更，依赖链清晰

### 5.2 不足

1. **前 4 个变更没有走完整流程** — verify/archive/compound 被跳过，经验提取延迟
2. **Hook 系统复杂度高** — 5 个 hook 文件，状态管理分散，调试困难
3. **记录机制价值低** — stop-hook 注入的"一行记录"信息量不够，深度分析才有意义
4. **specs 阶段使用不一致** — 只有 synergy 使用了 specs，缺乏何时该用的指导
5. **设计模板缺少框架检查项** — design 的"新增类型清单"不提示 System 注册类
6. **无自动化测试集成** — verify 只能做编译检查，无法运行 AutoChessTestHelper
7. **workflow-record 只是最近一个会话的快照** — 没有跨变更的历史记录

### 5.3 风险

1. **过度工程化风险** — 对于小变更（economy: 8 任务），完整的 proposal → specs → design → plan → exec → verify → archive → compound 流程可能太重
2. **Hook 脆弱性** — 状态文件损坏、session 冲突、时间窗口竞态条件都可能导致 hook 行为异常
3. **单人验收偏差** — 目前只有一个人在使用 QX，反馈循环可能过于适配个人习惯

---

## 六、改进建议

### P0 — 应立即执行

| # | 建议 | 理由 |
|---|------|------|
| 1 | design 模板增加 "System 注册类" 检查项 | 防止 EntitySystemOf 遗漏（坑 6 的教训） |
| 2 | plan 检查清单增加 "IAwake/IDestroy → EntitySystemOf" | 双重保险 |

### P1 — 下次迭代改进

| # | 建议 | 理由 |
|---|------|------|
| 3 | stop-hook 移除逐步记录指令 | 信息量低，用 /qx-compound 一次性生成深度分析 |
| 4 | 流程分级：轻量级（<= 8 任务）vs 完整（> 8 任务） | 避免小变更走重流程 |
| 5 | workflow-record 增加跨变更历史 | 当前只记录最近会话，丢失全局视角 |
| 6 | finishing 不承载修复职责 | hook 修复用 /qx-dev-debug 独立处理 |

### P2 — 长期优化

| # | 建议 | 理由 |
|---|------|------|
| 7 | per-task 审查增加"Entity 是否有对应 System"检查 | 提前捕获框架合规问题 |
| 8 | Hook 状态集中管理 | 5 个 hook 共享 1 个 JSON 状态文件，减少状态分散 |
| 9 | 增加 workflow-record 的分析视图 | 自动统计：每变更耗时、坑数、一次通过率 |
| 10 | exec 进度持久化（跨会话恢复协议） | 记录每个 Task 的 Agent 输出摘要 |

---

## 七、结论

QX 工作流在 A1 验收周期中证明了其核心价值：**proposal 范围控制 + design 技术决策 + plan 任务分解 + exec 多 Agent 执行**这条主链路是高效的。5 个变更从零建立了一个完整的自走棋服务端逻辑层，每个变更的 proposal 精确承接前置变更，依赖链无断裂。

主要改进方向：
1. **流程适配** — 按变更复杂度分级，避免一刀切
2. **Hook 简化** — 减少状态管理复杂度，聚焦核心价值（推荐下一步）
3. **知识提取时机** — 从"每步一行记录"改为"每变更一次深度分析"
4. **框架合规检查** — 将 ET 框架的隐式规则显式化到模板和检查清单中

QX 工作流仍处于 v0.1.0，A1 周期本身就是它的验收测试。上述问题是正常的迭代反馈，不是架构缺陷。
