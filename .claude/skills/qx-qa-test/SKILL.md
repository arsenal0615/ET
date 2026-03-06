---
name: qx-qa-test
description: "在为游戏功能创建测试场景、测试计划或测试设计文档时使用。基于风险评估和玩家影响产出优先级排序的测试覆盖方案。"
---

# 游戏测试设计

## 概述

为游戏功能创建全面的测试场景，覆盖游戏机制、进度系统、多人功能和平台需求。基于风险评估和玩家影响产出优先级排序的测试计划。

**启动时宣布：** "正在使用 qx-qa-test 为 [功能/sprint] 创建测试设计。"

## 适用场景

- 新功能需要测试覆盖设计
- Sprint 测试规划
- 为游戏系统创建测试场景
- 为重大发布制定测试策略

## 前置条件

- 游戏设计文档可用（GDD、功能规格）
- 了解目标平台
- 了解核心游戏循环

## 流程

### 步骤 1：收集上下文

1. **阅读游戏设计文档**
   - 在 `docs/game-design/` 中定位 GDD 或功能规格
   - 识别待测试的核心机制和功能
   - 记录目标平台

2. **识别关键系统**
   - 核心游戏循环
   - 进度/存档系统
   - 多人功能（如适用）
   - 商业化系统（如适用）

3. **评估风险领域**
   - 玩家直接感知的功能（最高优先级）
   - 数据持久化（存档/读档）
   - 平台需求
   - 性能关键路径

### 步骤 2：定义测试分类

#### 核心玩法测试

| 分类 | 关注点 | 优先级 |
|------|--------|--------|
| 核心循环 | 主要机制执行 | P0 |
| 战斗/交互 | 命中检测、反馈 | P0 |
| 移动 | 物理、碰撞、手感 | P0 |
| UI/UX | 菜单导航、HUD | P1 |
| 音频 | 音效触发、音乐 | P2 |

#### 进度测试

| 分类 | 关注点 | 优先级 |
|------|--------|--------|
| 存档/读档 | 数据持久化 | P0 |
| 解锁 | 内容门控 | P1 |
| 经济 | 货币、奖励 | P1 |
| 成就 | 触发条件 | P2 |

#### 多人测试（如适用）

| 分类 | 关注点 | 优先级 |
|------|--------|--------|
| 连接性 | 加入/退出处理 | P0 |
| 同步 | 状态一致性 | P0 |
| 延迟 | 弱网环境 | P1 |
| 匹配 | 玩家分组 | P1 |

#### 平台测试

| 分类 | 关注点 | 优先级 |
|------|--------|--------|
| 输入 | 手柄/触控支持 | P0 |
| 性能 | 帧率、加载时间 | P1 |
| 无障碍 | 辅助功能 | P1 |

### 步骤 3：创建测试场景

**场景格式：**
```
SCENARIO: [描述性名称]
  GIVEN [初始状态/前置条件]
  WHEN [执行的操作]
  THEN [预期结果]
  PRIORITY: P0/P1/P2/P3
  CATEGORY: [gameplay/progression/multiplayer/platform]
```

**示例 -- 玩法：**
```
SCENARIO: Basic Attack Hits Enemy
  GIVEN player is within attack range of enemy
  AND enemy has 100 health
  WHEN player performs basic attack
  THEN enemy receives damage
  AND damage feedback plays (visual + audio)
  AND enemy health decreases
  PRIORITY: P0
  CATEGORY: gameplay
```

**示例 -- 进度：**
```
SCENARIO: Save Preserves Player Progress
  GIVEN player has 500 gold and 3 items
  WHEN game saves and reloads
  THEN player has 500 gold and same 3 items
  AND player is at same position
  PRIORITY: P0
  CATEGORY: progression
```

**示例 -- 多人：**
```
SCENARIO: Gameplay Under High Latency
  GIVEN 2 players in session with 200ms latency
  WHEN Player 1 attacks Player 2
  THEN damage is applied correctly
  AND positions remain synchronized
  PRIORITY: P1
  CATEGORY: multiplayer
```

**E2E（端到端）旅程格式：**
```
E2E SCENARIO: [玩家旅程名称]
  GIVEN [初始游戏状态]
  WHEN [玩家操作序列]
  THEN [可观测结果]
  TIMEOUT: [预期最大持续时间（秒）]
  PRIORITY: P0/P1
  CATEGORY: e2e
```

### 步骤 4：覆盖优先级排序

**风险优先级矩阵：**
```
                    影响程度
                低        高
            ┌─────────┬─────────┐
      高    │   P2    │   P0    │
发生概率    ├─────────┼─────────┤
      低    │   P3    │   P1    │
            └─────────┴─────────┘
```

**按优先级的覆盖目标：**

| 优先级 | 标准 | 单元测试 | 集成测试 | E2E | 手动测试 |
|--------|------|----------|----------|-----|----------|
| P0 | 发布阻塞项 | 100% | 80% | 核心流程 | 冒烟测试 |
| P1 | 主要功能 | 90% | 70% | 正常路径 | 完整测试 |
| P2 | 次要功能 | 80% | 50% | - | 定向测试 |
| P3 | 边界情况 | 60% | - | - | 按需测试 |

### 步骤 5：生成测试设计文档

保存到：`docs/test-plans/[feature-name]-test-design.md`

```markdown
# 游戏测试设计：[功能/Sprint 名称]

## 概述
- 功能描述和核心机制
- 目标平台
- 测试范围（包含/排除）

## 风险评估
| 领域 | 风险 | 缓解策略 |
|------|------|----------|
| [领域] | [潜在问题] | [测试策略] |

## 测试场景

### 核心玩法测试
[SCENARIO 块...]

### 进度测试
[SCENARIO 块...]

### 多人测试（如适用）
[SCENARIO 块...]

### 平台测试
[SCENARIO 块...]

### E2E 旅程测试
[SCENARIO 块...]

## 覆盖矩阵
| 功能 | P0 | P1 | P2 | P3 | 总计 |
|------|----|----|----|----|------|
| [功能] | N | N | N | N | N |

## 自动化策略
### 建议自动化
- [场景] -- 原因

### 需要手动测试
- [场景] -- 原因（如：需要人工判断"手感"）

## 玩家测试建议
- 内部测试：[关注点、参与者、持续时间]
- 外部测试：[关注点、目标受众、持续时间]

## 后续步骤
1. [ ] 与团队评审测试设计
2. [ ] 实现 P0 自动化测试
3. [ ] 规划玩家测试环节
```

### 步骤 6：报告总结

```
测试设计完成 -- [功能名称]

已创建场景数：[数量]
  P0（关键）：[数量]
  P1（高）：[数量]
  P2（中）：[数量]
  P3（低）：[数量]

关注领域：核心玩法、进度、[多人]、平台

下一步：与团队评审 → 实现 P0 测试 → 规划玩家测试
```

## 核心原则

- **基于风险排序** -- 优先测试高影响、高概率的场景
- **聚焦玩家体验** -- 优先测试玩家实际感知到的内容
- **自动化重复项** -- 探索性测试用手动，回归测试用自动化
- **不重复分析器的工作** -- 了解构建系统已经捕获了哪些问题

> **注意：** 阅读项目的 CLAUDE.md，了解已有的静态分析和编译期检查。不要为编译器已能捕获的问题编写测试。

## 相关 Skills

- **qx-tdd** -- 用于测试驱动实现测试场景
- **qx-verify** -- 用于在声称完成之前验证测试结果
- **qx-gd-gdd** -- GDD 提供待测试的功能
- **qx-gd-sprint** -- Sprint 上下文决定测试范围
