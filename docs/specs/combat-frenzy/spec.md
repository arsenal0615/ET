## 新增需求

### Requirement: Frenzy Activation
tick >= 400 时 SHALL 激活 Frenzy Time。

#### Scenario: Frenzy triggers at tick 400
- **WHEN** tick 从 399 推进到 400
- **THEN** 输出 FRENZY_START 事件，所有单位攻速 ×2

#### Scenario: Invisibility duration halved
- **WHEN** Frenzy 激活后施加隐身 Buff（原 duration=100 ticks）
- **THEN** 实际 duration = floor(100 × 0.5) = 50 ticks

#### Scenario: Already active frenzy
- **WHEN** Frenzy 已激活，tick=401
- **THEN** 不重复触发，攻速倍率维持 ×2
