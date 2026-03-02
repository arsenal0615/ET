# ET9 包系统与序列化

## 包目录结构

包名格式：`cn.etetet.{包名}`

```
Packages/cn.etetet.{包名}/
├── Scripts/          ← 热更代码
│   ├── Model/        ← 数据定义（再分 Server/ Client/ Share/）
│   ├── Hotfix/       ← 逻辑实现（再分 Server/ Client/ Share/）
│   ├── ModelView/    ← 客户端数据（再分 Client/）
│   └── HotfixView/   ← 客户端逻辑（再分 Client/）
├── CodeMode/         ← 模式相关代码（子目录：Server/Client/ClientServer）
├── Runtime/          ← AOT 代码（需定义 asmdef）
├── Editor/           ← 编辑器代码
├── DotNet~/          ← .NET 专用工程（~ 让 Unity 忽略）
├── Excel/            ← Excel 配置表
├── Proto/            ← 消息定义
└── Plugins/          ← 插件
```

### 关键规则

- 每个包顶层放 `Ignore` asmdef → 默认代码不生效，只有显式 asmdef 才生效
- `Scripts/Share/` = 双端共用代码
- `CodeMode/ClientServer/` = 仅 Unity ClientServer 模式下使用
- cn.etetet 开头的包安装后自动移到 Packages 目录（Custom 包）

## 常用包

| 包名 | 功能 |
|------|------|
| core | Entity, Fiber, EventSystem, ETTask, World |
| loader | 编译、热重载、构建工具、CodeMode |
| proto | Proto 消息定义和代码生成 |
| excel | Excel 配置导出工具 |
| statesync | 状态同步 demo |
| login | 登录流程 |
| unit | 场景实体 |
| move | 移动组件 |
| actorlocation | Actor Location 机制 |
| numeric | KV 数值组件 |
| ai | 行为机 AI |
| aoi | 九宫格 AOI |
| hybridclr | HybridCLR 热更支持 |
| yooassets | YooAsset 资源管理 |
| fairygui | FairyGUI UI 框架（第三方库，Runtime/ 模式） |
| sourcegenerator | 分析器和代码生成器 |

## MongoBson 序列化

ET 统一使用 MongoBson，同一对象可无转换地在网络传输、数据库存储、日志输出间流转。

### 关键特性

- `obj.ToJson()` / `obj.ToBson()` 双格式
- `[BsonIgnore]`：忽略字段
- `[BsonElement("别名")]`：字段别名，private 也可序列化
- `[BsonIgnoreExtraElements]`：忽略多余字段（版本兼容必备）
- `ISupportInitialize`：`BeginInit()` 反序列化前，`EndInit()` 反序列化后

### ISupportInitialize 用法

```csharp
[BsonIgnoreExtraElements]
public class InnerConfig : AConfigComponent
{
    [BsonIgnore]
    public IPEndPoint IPEndPoint { get; private set; }
    public string Address { get; set; }

    public override void EndInit()
    {
        this.IPEndPoint = NetworkHelper.ToIPEndPoint(this.Address);
    }
}
```

Proto 消息都是 `partial class`，可扩展添加计算字段、实现 ISupportInitialize。
