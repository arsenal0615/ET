# ET 第三方集成与子系统参考

## MongoBson 序列化

ET 统一使用 MongoBson 作为序列化方案，覆盖：对象 clone、数据库存储、进程间消息、日志、配置文件。

- 支持 json 和 bson 双格式：`obj.ToJson()` / `obj.ToBson()`
- 支持继承结构：自动注册所有子类
- `[BsonIgnore]`：忽略字段；`[BsonElement("别名")]`：字段别名
- `[BsonIgnoreExtraElements]`：忽略多余字段（版本兼容必备）
- `ISupportInitialize`：`BeginInit()` 反序列化前调用，`EndInit()` 反序列化后调用
- Proto 消息类都是 `partial class`，可扩展实现 ISupportInitialize

## FairyGUI

| 项 | 值 |
|---|---|
| 包名 | `cn.etetet.fairygui` |
| 程序集 | `ET.FairyGUI` / `ET.FairyGUI.Editor` |
| 命名空间 | `FairyGUI`、`FairyGUI.Utils` |
| 模式 | Runtime/ + Editor/（不参与四程序集分离） |

核心类：`GObject`（基类）→ `GComponent`（容器）→ `GRoot`（全局根）；`UIPackage`（资源包）、`Controller`（状态）、`ScrollPane`（滚动）

条件编译：`FAIRYGUI_TMPRO`（TMP）、`FAIRYGUI_DRAGONBONES`、`FAIRYGUI_SPINE`

ET 引用：asmdef 添加 `"ET.FairyGUI"`，热更代码在 ModelView/HotfixView 层调用。

## HybridCLR 热更新

日常开发无需关心，编辑器下 F6 编译 + Play 即可。HybridCLR 只在真机打包时生效。

| DLL | 内容 | F7 热重载 |
|-----|------|-----------|
| ET.Model.dll | 数据定义 | 不支持（改了必须重启） |
| ET.ModelView.dll | 客户端数据 | 不支持 |
| ET.Hotfix.dll | 逻辑 | 支持 |
| ET.HotfixView.dll | 客户端视图逻辑 | 支持 |

- **F6**：CompilePlayerScripts → 复制 DLL 到 `Bundles/Code/*.bytes`
- **F7**：重新加载 Hotfix DLL → 重新注册 CodeTypes → 触发 LoadSystem
- **真机打包**：`HybridCLR → Generate → All` → `ET → HybridCLR → CopyAotDlls` → 补充 AOT 元数据

## YooAsset 资源管理

| 层 | 类型 | 用途 |
|----|------|------|
| **ResourcesComponent** | Singleton（AOT） | 启动阶段加载 DLL/Config（Fiber 未创建） |
| **ResourcesLoaderComponent** | Entity 组件（HotfixView） | 运行阶段资源，生命周期跟随 Entity |

**关键规则**：运行时资源用 ResourcesLoaderComponent。Entity 销毁时自动释放。

PlayMode：EditorSimulateMode（开发）、OfflinePlayMode（单机）、HostPlayMode（热更）、WebPlayMode（WebGL）
