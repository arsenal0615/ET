# ET 代码审查检查清单

> 用于 code-reviewer 的框架合规审查维度。

## 编译期规则（违反即报错）

- [ ] Entity 类没有定义方法（逻辑在 System 静态扩展方法中）
- [ ] 没有 `new` Entity/Component（使用 AddComponent/AddChild）
- [ ] 异步方法返回 ETTask（不是 Task 或 async void）
- [ ] 没有静态字段（除非标注 [StaticField]）
- [ ] Hotfix 程序集只有静态类（或 [EnableClass] 标注的类）
- [ ] AddComponent 类型匹配 [ComponentOf] 声明
- [ ] AddChild 类型匹配 [ChildOf] 声明
- [ ] 命名空间正确（ET / ET.Client / ET.Server）
- [ ] Client 类没有出现在 Server 程序集

## 四程序集分离

- [ ] Component 数据定义在 Model/ModelView 程序集
- [ ] System 逻辑在 Hotfix/HotfixView 程序集
- [ ] Client-only 代码在 ModelView/HotfixView
- [ ] Server-only 代码在正确的命名空间

## 属性和标注

- [ ] System 类有 `partial` 关键字
- [ ] System 类有 [EntitySystemOf(typeof(Component))]
- [ ] System 类有 [FriendOf(typeof(Component))]
- [ ] 生命周期方法有 [EntitySystem] 标注
- [ ] 消息处理器有 [MessageHandler(SceneType.X)]
- [ ] 事件处理器有 [Event(SceneType.X)]

## 消息和网络

- [ ] 新消息已定义 Proto 并运行 Proto2CS
- [ ] 消息方向正确（C2G_, C2M_, G2C_, M2C_）
- [ ] Request 有对应 ResponseType 注释
- [ ] 跨 Fiber 通信使用 Actor 消息（不直接访问对象）

## 配置和资源

- [ ] 新配置已定义 Excel 并运行 ExcelExporter
- [ ] 资源加载使用框架提供的 API
