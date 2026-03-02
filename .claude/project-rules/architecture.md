# ET 架构参考

## 包系统（ET9）

所有框架代码位于 `Library/PackageCache/` 下的 `cn.etetet.*` 包中。关键包：
- **cn.etetet.core** — Entity、Fiber、EventSystem、ObjectPool、World、ETTask
- **cn.etetet.loader** — 代码编译、热重载、构建工具、CodeMode 管理
- **cn.etetet.proto** — Proto 消息定义和代码生成
- **cn.etetet.excel** — Excel 配置导出工具
- **cn.etetet.statesync** — 状态同步演示（主演示包）
- **cn.etetet.sourcegenerator** — Roslyn 分析器和源生成器
- **cn.etetet.netinner** — 内部网络通信（Fiber 间 Actor 消息）
- **cn.etetet.login** — 登录流程
- **cn.etetet.unit** — 单位/角色管理
- **cn.etetet.move** — 移动系统
- **cn.etetet.ai** — AI 框架
- **cn.etetet.aoi** — 兴趣区域（AOI）
- **cn.etetet.hybridclr** — 客户端热更新支持
- **cn.etetet.numeric** — KV 数值组件
- **cn.etetet.actorlocation** — Actor Location 机制
- **cn.etetet.db** — MongoDB 封装
- **cn.etetet.yooassets** — YooAsset 资源管理封装
- **cn.etetet.fairygui** — FairyGUI UI 框架（第三方库，Runtime/ 模式）
- **cn.etetet.recast** — 3D Recast 寻路
- **cn.etetet.router** — 软路由（防网络攻击）
- **cn.etetet.lockstep** — 帧同步演示

## 四程序集分离

代码分为 4 个程序集（F6 编译的热更新 DLL）：

| 程序集 | 内容 | 规则 |
|--------|------|------|
| **ET.Model** | Entity/Component 定义、数据结构 | 只有数据字段，Entity 类不能有方法 |
| **ET.ModelView** | 客户端专用 Model（依赖 Unity 的组件） | 与 Model 相同规则，用于 View 层 |
| **ET.Hotfix** | 逻辑/System、消息处理器、事件处理器 | 只允许静态类或标注 EnableClassAttribute 的类 |
| **ET.HotfixView** | 客户端专用 View 逻辑（UI、渲染） | 与 Hotfix 相同规则，用于 View 层 |

## 代码可见性（CodeMode）

每个包的代码按 `Client/`、`Server/`、`Share/` 目录组织。`GlobalConfig` 中的 **CodeMode** 设置控制哪些代码参与编译：
- **ClientServer** — 所有代码可见（推荐开发时使用）
- **Client** — 仅 Client + Share（用于独立客户端构建）
- **Server** — 仅 Server + Share

## 包内目录规范

### 热更代码包（标准模式）

每个包 `Packages/cn.etetet.*/Scripts/` 下分 `Model/` 和 `Hotfix/`，每层再分 `Client/` `Server/` `Share/`。

| 目录 | 用途 |
|------|------|
| **Scripts/** | 热更代码，下含 Model/Hotfix/ModelView/HotfixView |
| **CodeMode/** | 模式相关代码，子目录 Server/Client/ClientServer |
| **Runtime/** | AOT 代码，需定义 asmdef |
| **Editor/** | 编辑器代码 |
| **DotNet~/** | .NET 专用工程（`~` 后缀让 Unity 忽略） |
| **Excel/** | Excel 配置表 |
| **Proto/** | 消息定义 |

关键规则：
- 每个包顶层放 `Ignore` 的 asmdef → 默认代码不生效，只有显式 asmdef 才生效
- `Scripts/Share/` = 双端共用，`CodeMode/ClientServer/` = 仅编辑器 ClientServer 模式

### 第三方库包（Runtime 模式）

FairyGUI、YooAssets 等用 `Runtime/` + `Editor/` 模式，不参与四程序集分离。

## 入口点

- Unity 场景：`Packages/cn.etetet.loader/Scenes/Init.unity`
- 代码入口：cn.etetet.core 中的 `Entry.StartAsync()` — 初始化 World 单例（ObjectPool、EventSystem、MessageQueue、NetServices），加载配置，创建主 Fiber

## 配置

- **GlobalConfig**：`Packages/cn.etetet.loader/Resources/GlobalConfig` — CodeMode、SceneName、Address
- **YooConfig**：`Packages/cn.etetet.yooassets/Resources/YooConfig` — 资源加载模式（开发时用 EditorSimulateMode）
- **StartConfig**：演示包中的 Excel 文件（`Excel/StartConfig/Localhost/` 或 `Release/`）— Machine、Process、Scene、Zone 配置

## Unity 菜单工具

- `ET/Loader/ServerTools` — 从 Unity 启动/停止服务器
- `ET/Loader/Build Tool` — 构建客户端包（输出：`./Release/`）
- `ET/Loader/Add ENABLE_VIEW` / `Remove ENABLE_VIEW` — 切换 Hierarchy 中 Entity 可视化
- `ET/Proto/Proto2CS` — 编译 Proto 文件
- `ET/Excel/ExcelExporter` — 导出 Excel 配置

## 构建和运行命令

```bash
# Proto 转 C#（消息类）
dotnet ./Packages/cn.etetet.proto/DotNet~/Exe/ET.Proto2CS.dll ./

# Excel 配置导出
dotnet ./Packages/cn.etetet.excel/DotNet~/Exe/ET.ExcelExporter.dll ./

# 编译解决方案（需要代理下载 NuGet 包）
dotnet build ET.sln

# 启动服务器（单进程，从 ET 根目录执行）
dotnet Bin/ET.App.dll --SceneName=StateSync --Process=1 --StartConfig=StartConfig/Localhost --Console=1
```

