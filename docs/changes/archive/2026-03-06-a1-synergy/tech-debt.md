# 技术债务 — a1-synergy

> 以下 Important 级别问题在代码审查中被发现但未修复。
> Archive 时确认：全部推迟到后续迭代（E7 战斗系统实现时一并优化）。

## 未修复的 Important 问题

### I1 — GC 分配优化
- `SynergyService.Recalculate` 每次调用创建 `new Dictionary<string, int>()` 和 `new List<SynergyEntry>()`，`HasChanged` 内部又创建 `new Dictionary<string, SynergyEntry>()`
- Deployment 阶段每次摆位操作都会触发，频繁 GC 分配
- **建议:** 在 SynergyComponent 上缓存复用这些临时集合，或预设 capacity（如 13）

### I2 — Goblin 模板列表缓存
- `GoblinGiftService.TryGiveGifts` 每次调用遍历全部 24 个模板收集 Goblin 标签单位
- 该列表是静态不变的
- **建议:** 缓存为 `[StaticField]` 静态字段，在 `AutoChessConfigLoader.Init()` 时构建一次
