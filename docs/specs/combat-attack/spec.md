## 新增需求

### Requirement: Target Selection for Basic Attack
普攻目标选择 SHALL 按：射程内最近敌人 → 仇恨优先（正在攻击自己的） → instId 升序。

#### Scenario: Nearest enemy in range
- **WHEN** 单位 range=1，邻接格有 2 个敌人（距离均为1）
- **THEN** 选 instId 较小者（无仇恨时）

#### Scenario: Aggro priority
- **WHEN** 敌人 A（dist=1）正在攻击本单位，敌人 B（dist=1）攻击别人
- **THEN** 选 A（仇恨优先）

### Requirement: Damage Formula
普攻伤害 MUST 为 floor(atk × damageMultiplier)，暴击时再乘 critMultiplier。

#### Scenario: Normal attack damage
- **WHEN** atk=100, damageMultiplier=1.0, 未暴击
- **THEN** damage = floor(100 × 1.0) = 100

#### Scenario: Critical hit
- **WHEN** atk=100, critChance=0.3, rng.nextFloat()=0.2, critMultiplier=1.5
- **THEN** damage = floor(100 × 1.5) = 150

### Requirement: Damage Reduction
伤害 MUST 应用目标的 DamageReduction：finalDamage = floor(damage × (1 - damageReduction))。

#### Scenario: Giant trait damage reduction
- **WHEN** damage=100, target.DamageReduction=0.4
- **THEN** finalDamage = floor(100 × 0.6) = 60

### Requirement: Attack Speed Interval
攻击间隔 MUST 为 attackIntervalTicks = ceil(1/atkSpeed × 20)。

#### Scenario: First attack timing
- **WHEN** 单位 atkSpeed=1.0
- **THEN** attackIntervalTicks = ceil(20) = 20, 首次攻击在 tick=20

### Requirement: Mana Gain on Attack and Hit
普攻命中后，攻击者获得 ManaGainOnAttack，目标获得 ManaGainOnHit。

#### Scenario: Mana gain
- **WHEN** 攻击者普攻命中目标
- **THEN** 攻击者 mana += 10, 目标 mana += 6

### Requirement: Blaster Distance Damage
Blaster 羁绊的单位 MUST 按距离增伤：damage × (1 + distance × DistanceDamagePerHex)。

#### Scenario: Blaster range 3 attack at distance 3
- **WHEN** Blaster level=1, DistanceDamagePerHex=0.10, 攻击距离=3
- **THEN** 额外 +30% 伤害
