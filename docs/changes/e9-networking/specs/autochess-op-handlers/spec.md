## 新增需求

### Requirement: PhaseGate 门控

所有操作 Handler SHALL 在处理前检查当前阶段是否允许该操作：
- Buy: Deployment + Battle（Battle 时只进板凳）
- Sell: Deployment（棋盘+板凳）+ Battle（仅板凳）
- Place/Swap: 仅 Deployment

#### Scenario: 部署阶段购买
- **WHEN** 当前阶段为 Deployment 且客户端发送 Buy
- **THEN** 允许购买，单位可放入棋盘或板凳

#### Scenario: 战斗阶段购买
- **WHEN** 当前阶段为 Battle 且客户端发送 Buy
- **THEN** 允许购买，单位只能放入板凳

#### Scenario: 战斗阶段摆位
- **WHEN** 当前阶段为 Battle 且客户端发送 Place/Swap
- **THEN** 拒绝操作，返回 PHASE_NOT_ALLOWED 错误码

#### Scenario: 非游戏阶段操作
- **WHEN** 当前阶段为 RoundStart/PreBattle/RoundEnd 且收到任何操作
- **THEN** 拒绝操作，返回 PHASE_NOT_ALLOWED 错误码

### Requirement: Buy Handler

Buy Handler SHALL：
1. 检查 PhaseGate
2. 检查 slotIndex 有效（0-2）且 slot 有 Offer
3. 调用 ShopService.TryBuy（内含圣水/板凳容量检查）
4. 成功：广播操作结果 + 更新后的商店/圣水/单位状态
5. 失败：返回错误码（INSUFFICIENT_ELIXIR / BENCH_FULL / INVALID_SLOT）

#### Scenario: 购买成功
- **WHEN** 圣水充足且板凳有空位
- **THEN** 扣圣水、单位入板凳、商店刷新、广播成功结果

#### Scenario: 圣水不足
- **WHEN** 圣水不够支付费用
- **THEN** 返回 INSUFFICIENT_ELIXIR，不改变任何状态

#### Scenario: 板凳已满
- **WHEN** 板凳 5 槽全满
- **THEN** 返回 BENCH_FULL，不改变任何状态

### Requirement: Sell Handler

Sell Handler SHALL：
1. 检查 PhaseGate
2. 检查 instId 对应的单位存在且属于该玩家
3. Battle 阶段只能卖板凳单位
4. 调用 UnitService.Sell
5. 广播操作结果

#### Scenario: 部署阶段出售棋盘单位
- **WHEN** Deployment 阶段出售棋盘上的单位
- **THEN** 返还圣水（cost-1）、回流卡池、移除单位

#### Scenario: 战斗阶段出售棋盘单位
- **WHEN** Battle 阶段出售棋盘上的单位
- **THEN** 拒绝，返回 CANNOT_SELL_BOARD_IN_BATTLE

### Requirement: Place Handler

Place Handler SHALL：
1. 检查 PhaseGate（仅 Deployment）
2. 检查 instId 存在且属于该玩家
3. 检查目标坐标合法且未被占用
4. 检查人口上限
5. 调用 RosterService 移动单位
6. 广播操作结果

#### Scenario: 放置成功
- **WHEN** 板凳单位放置到空棋盘格
- **THEN** 单位移到棋盘，popUsed +1

#### Scenario: 超人口
- **WHEN** popUsed >= popCap
- **THEN** 返回 POPCAP_EXCEEDED

### Requirement: Swap Handler

Swap Handler SHALL：
1. 检查 PhaseGate（仅 Deployment）
2. 检查两个 instId 都存在且属于该玩家
3. 交换两个单位的位置
4. 广播操作结果

#### Scenario: 棋盘↔板凳交换
- **WHEN** 交换一个棋盘单位和一个板凳单位
- **THEN** 两者位置互换，popUsed 不变

### Requirement: 错误码枚举

操作错误码 SHALL 定义在 Model/Share 中，包含：
- OK = 0
- PHASE_NOT_ALLOWED = 1
- INVALID_SLOT = 2
- INSUFFICIENT_ELIXIR = 3
- BENCH_FULL = 4
- UNIT_NOT_FOUND = 5
- CANNOT_SELL_BOARD_IN_BATTLE = 6
- POS_OCCUPIED = 7
- POPCAP_EXCEEDED = 8
- INVALID_POSITION = 9

#### Scenario: 错误码可在客户端使用
- **WHEN** 客户端收到 OpResult
- **THEN** 可用枚举值映射为用户可读的错误提示
