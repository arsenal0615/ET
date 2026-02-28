# ET9 包系统与序列化

---

## 一、Package 目录规范

### 命名格式
`cn.etetet.{包名}`，如 `cn.etetet.core`、`cn.etetet.statesync`

### 包内目录结构
| 目录 | 用途 | 说明 |
|------|------|------|
| **Scripts/** | 热更代码 | 下含 Model/Hotfix/ModelView/HotfixView，每个再分 Server/Client/Share |
| **CodeMode/** | 模式相关代码 | 类似 Scripts，但子目录是 Server/Client/ClientServer |
| **Runtime/** | AOT 代码 | 需定义 asmdef，如 ET.Core |
| **Editor/** | 编辑器代码 | 命名如 ET.Core.Editor |
| **DotNet~/** | .NET 专用工程 | `~` 后缀让 Unity 忽略 |
| **Excel/** | Excel 配置表 | ExcelExporter 工具扫描导出 |
| **Proto/** | 消息定义 | Proto2CS 工具扫描生成 C# |
| **Plugins/** | 插件 | 无严格限制 |
| **Scenes/** | 场景 | 无严格限制 |

### 关键规则
- 每个包顶层放一个 `Ignore` 的 asmdef → 默认代码不生效，只有显式 asmdef 才生效
- 每个包放 `packagegit.json` → 定义包 ID、包名、git 依赖
- cn.etetet 开头的包安装后自动移到 Packages 目录（变成 Custom 包）
- 自定义菜单格式：`ET -> {包名} -> {功能}`

### Scripts vs CodeMode 区别
- `Scripts/Share/` = 双端共用代码
- `CodeMode/ClientServer/` = 仅 Unity 编辑器 ClientServer 模式下使用

## 二、核心包列表（常用）

| 包名 | 功能 |
|------|------|
| cn.etetet.core | 框架核心：Entity, Fiber, EventSystem, ETTask, World |
| cn.etetet.loader | 加载器：编译、热重载、构建工具、CodeMode |
| cn.etetet.proto | Proto 消息定义和代码生成 |
| cn.etetet.excel | Excel 配置导出工具 |
| cn.etetet.statesync | 状态同步 demo |
| cn.etetet.lockstep | 帧同步 demo |
| cn.etetet.login | 登录流程（realm/gate/scene） |
| cn.etetet.unit | 带位置旋转的场景实体 |
| cn.etetet.move | MMO 移动组件 |
| cn.etetet.actorlocation | Actor Location 机制 |
| cn.etetet.netinner | 内网消息模块 |
| cn.etetet.numeric | KV 数值组件 |
| cn.etetet.ai | 行为机 AI 模块 |
| cn.etetet.aoi | 九宫格 AOI |
| cn.etetet.db | MongoDB 封装 |
| cn.etetet.hybridclr | HybridCLR 热更支持 |
| cn.etetet.sourcegenerator | 分析器和代码生成器 |
| cn.etetet.yooassets | YooAsset 资源管理封装 |
| cn.etetet.recast | 3D Recast 寻路 |
| cn.etetet.router | 软路由（防网络攻击） |

## 三、包更新方式

1. 将项目和包全部提交到 git 仓库作为 master 分支
2. 开发时切出 dev 分支进行开发
3. 更新时切回 master 分支更新包
4. 更新完提交 master，切回 dev 合并 master

## 四、MongoBson 序列化

### 为什么选 MongoBson
ET 框架统一使用 MongoBson 作为序列化方案，覆盖所有场景：
- 对象 clone、数据库存储（二进制）、进程间消息（二进制）、日志（文本）、配置文件（文本）
- 同一个对象可以无转换地在网络传输、数据库存储、日志输出之间流转

### 核心能力
1. **支持 json 和 bson 双格式**：`obj.ToJson()` / `obj.ToBson()`
2. **支持继承结构**：通过 `BsonClassMap.LookupClassMap(type)` 自动注册所有子类
3. **`[BsonIgnore]`**：忽略字段不序列化
4. **`[BsonElement("别名")]`**：字段别名，private 字段也可序列化
5. **`[BsonIgnoreExtraElements]`**：反序列化时忽略多余字段（版本兼容必备）
6. **ISupportInitialize 接口**：`BeginInit()` 反序列化前调用，`EndInit()` 反序列化后调用

### ISupportInitialize 的妙用
```csharp
// 配置中存 string 地址，反序列化后自动转 IPEndPoint
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

// Proto 消息反序列化后自动把 repeated 转 Dictionary
public partial class G2C_EnterMap : ISupportInitialize
{
    public Dictionary<long, UnitInfo> unitsDict = new();
    public void BeginInit() { }
    public void EndInit()
    {
        foreach (var unit in this.Units)
            this.unitsDict.Add(unit.UnitId, unit);
    }
}
```

### Proto 消息都是 partial class
ET 生成的 Proto 消息类都是 `partial class`，可以通过扩展添加计算字段、实现 ISupportInitialize 等。
