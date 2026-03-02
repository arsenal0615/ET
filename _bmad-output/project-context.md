---
project_name: 'ET'
user_name: 'Administrator'
date: '2026-02-28'
sections_completed: ['technology_stack', 'language_rules', 'framework_rules', 'code_quality', 'message_system', 'workflow', 'critical_rules']
status: 'complete'
rule_count: 38
optimized_for_llm: true
---

# AI Agent 项目上下文

_本文件包含 AI Agent 在本项目中实现代码时必须遵循的关键规则和模式。重点关注 Agent 可能遗漏的非显而易见的细节。_

---

## 技术栈与版本

| 技术 | 版本 | 用途 |
|------|------|------|
| C# | - | 客户端与服务端统一语言 |
| Unity | 6000.3.9f1 | 游戏引擎（客户端） |
| .NET | 8 | 服务端运行时 |
| ET 框架 | 9.0 (core v3.0.3) | 自研 ECS 游戏框架 |
| HybridCLR | - | 客户端热更新 |
| KCP | - | UDP 网络协议（支持 TCP/WebSocket 回退） |
| YooAssets | - | 资源管理 |
| MemoryPack | - | 序列化 |
| Roslyn Source Generators | - | 编译期代码生成与分析器规则检查 |
| IDE | Rider 2024.3+ | 必须使用 Rider，不支持 VS |

## 关键实现规则

### C# 语言规则

- **异步方法必须返回 `ETTask`**，禁止使用 `Task` 或 `async void`。ET 使用自研单线程异步模型
- **禁止在 Entity 类型上使用 `new`**，必须通过 `AddComponent<T>()` / `AddChild<T>()` 创建（对象池管理生命周期）
- **禁止声明静态字段**，除非标注 `[StaticField]`（防止热重载时状态泄漏）
- **Entity 类不允许定义方法**，所有逻辑必须写在外部静态 System 类中，作为扩展方法
- **Entity 字段默认私有**，外部访问需在 System 类上标注 `[FriendOf(typeof(XxxComponent))]`
- **Hotfix 程序集中只允许静态类**，或标注 `[EnableClass]` 的类，禁止普通类声明
- **命名空间强制分离**：`ET`（共享）、`ET.Client`（客户端）、`ET.Server`（服务端），Client 类不可出现在 Server 程序集中
- **`AddChild` / `AddComponent` 类型检查**：必须与 `[ComponentOf]` / `[ChildOf]` 声明匹配，否则编译报错

### ET 框架规则

#### 四程序集分离

| 程序集 | 内容 | 规则 |
|--------|------|------|
| **ET.Model** | Entity/Component 定义、数据结构 | 仅数据字段，不允许方法 |
| **ET.ModelView** | 客户端专用 Model（依赖 Unity） | 同 Model 规则 |
| **ET.Hotfix** | 逻辑/System、消息处理、事件处理 | 仅静态类或 `[EnableClass]` 类 |
| **ET.HotfixView** | 客户端专用视图逻辑（UI、渲染） | 同 Hotfix 规则 |

#### Entity-Component 模式

- Component 在 Model 程序集中定义，**只包含数据字段**
- 逻辑在 Hotfix 程序集中以**静态扩展方法**实现，类名为 `{Component}System`
- System 类必须标注 `[EntitySystemOf(typeof(XxxComponent))]` 和 `[FriendOf(typeof(XxxComponent))]`
- 生命周期方法（Awake/Destroy/Update 等）标注 `[EntitySystem]`

#### 关键属性清单

| 属性 | 用途 |
|------|------|
| `[ComponentOf(typeof(Parent))]` | 声明 Component 所属的 Entity |
| `[ChildOf(typeof(Parent))]` | 声明父子 Entity 关系 |
| `[FriendOf(typeof(Entity))]` | 授权 System 类访问 Entity 的私有字段 |
| `[EntitySystemOf(typeof(Entity))]` | 自动生成生命周期 System 样板代码 |
| `[Event(SceneType.X)]` | 事件处理器注册 |
| `[MessageHandler(SceneType.X)]` | 网络消息处理器 |
| `[MessageSessionHandler(SceneType.X)]` | 带响应的 RPC 处理器 |
| `[Invoke(SceneType.X)]` | 函数式调用处理器 |
| `[EnableMethod]` | 允许 Entity 类中声明方法（例外规则） |

#### Fiber 并发模型

- Fiber 是核心调度单元（类似 Erlang 进程），每个 Fiber 拥有独立的 Scene 根节点
- 三种调度器：**Main**（Unity 主线程）、**Thread**（独立线程）、**ThreadPool**（线程池）
- Fiber 之间通过 `MessageQueue` 的 Actor 消息通信，**禁止跨 Fiber 直接访问对象**

### 代码质量与命名规范

#### 文件命名模式

| 类型 | 命名模式 | 示例 |
|------|----------|------|
| Component 类 | `{名称}Component.cs` | `MoveComponent.cs` |
| System 类 | `{Component}System.cs` | `MoveComponentSystem.cs` |
| 消息处理器 | `{消息名}Handler.cs` | `C2G_EnterMapHandler.cs` |
| 事件处理器 | `{事件}_{动作}.cs` | `ChangePosition_NotifyAOI.cs` |
| 工厂类 | `{Entity}Factory.cs` | `UnitFactory.cs` |
| 辅助类 | `{Entity}Helper.cs` | `UnitHelper.cs` |
| AI 行为 | `AI_{行为名}.cs` | `AI_XunLuo.cs` |
| Fiber 初始化 | `FiberInit_{场景名}.cs` | `FiberInit_Map.cs` |
| 事件类型结构体 | `{Entity}EventType.cs` | `UnitEventType.cs` |

#### 目录结构规范

每个包按 `Client/` `Server/` `Share/` 组织代码，由 `CodeMode` 控制编译可见性：
- **ClientServer** — 全部可见（开发推荐）
- **Client** — 仅 Client + Share
- **Server** — 仅 Server + Share

#### 常量 ID 分配公式

- `SceneType = PackageType * 1000 + offset`
- `TimerInvokeType = PackageType * 1000 + offset`
- 各包定义自己的 `PackageType` 常量值，避免 ID 冲突

### 消息系统与网络规则

#### Proto 文件命名

- 文件名格式：`{消息集}_{方向}_{起始Opcode}.proto`
- 方向标识：`_C_`（客户端消息）、`_S_`（服务端内部消息）
- 示例：`StateSyncOuter_C_11001.proto`、`StateSyncInner_S_21001.proto`

#### 消息类型注释（决定代码生成）

| 注释 | 含义 |
|------|------|
| `// IMessage` | 单向通知 |
| `// IRequest` / `// IResponse` | RPC 请求/响应对 |
| `// ISessionRequest` / `// ISessionResponse` | 基于 Session 的 RPC |
| `// ILocationRequest` / `// ILocationResponse` | 基于 Actor 的 RPC |
| `// ISessionMessage` | 单向 Session 消息 |
| `// ILocationMessage` | 单向 Location 消息 |
| `// ResponseType TypeName` | 将 Request 与其 Response 关联 |

#### 消息名方向前缀

- `C2G_` — Client → Gate
- `C2M_` — Client → Map
- `G2C_` — Gate → Client
- `M2C_` — Map → Client
- 消息名与 Handler 文件名对应：`M2C_PathfindingResult` → `M2C_PathfindingResultHandler.cs`

#### 网络通信

- `Session` 是网络连接封装，RPC 调用使用 `session.Call(request)`
- 带 `MailBoxComponent` 的 Entity 可通过 Actor ID 从任意服务器接收消息

### FairyGUI UI 框架

#### 包信息

| 项 | 值 |
|---|---|
| 包名 | `cn.etetet.fairygui` |
| 位置 | `Packages/cn.etetet.fairygui/` |
| 运行时程序集 | `ET.FairyGUI`（asmdef: `Runtime/ET.FairyGUI.asmdef`） |
| 编辑器程序集 | `ET.FairyGUI.Editor`（asmdef: `Editor/ET.FairyGUI.Editor.asmdef`） |
| 命名空间 | `FairyGUI`、`FairyGUI.Utils` |
| 依赖 | `com.unity.ugui`（Unity 6 中 TMP 已合并进 ugui） |

#### 与标准 ET 包的区别

FairyGUI 是**第三方 UI 库**，不参与 ET 四程序集热更分离：
- 使用 `Runtime/` 目录（非 `Scripts/Model/Hotfix/`），与 `cn.etetet.yooassets` 同模式
- 保留原始 `FairyGUI` / `FairyGUI.Utils` 命名空间，未改为 `ET.*`
- 无 `Client/Server/Share` 分层（纯客户端库）
- 其他 ET 包引用 FairyGUI 时，在 asmdef 的 references 中添加 `"ET.FairyGUI"`

#### 目录结构

```
cn.etetet.fairygui/
├── Runtime/              # 运行时代码 (158 个 .cs)
│   ├── Core/             # 渲染核心 (DisplayObject, NGraphics, Stage, Mesh, HitTest, Text)
│   ├── Event/            # 事件系统 (EventDispatcher, EventListener)
│   ├── Extensions/       # 扩展 (DragonBones, Spine, TextMeshPro, WebGL)
│   ├── Filter/           # 滤镜 (BlurFilter, ColorFilter)
│   ├── Gesture/          # 手势 (LongPress, Pinch, Rotation, Swipe)
│   ├── Tween/            # 补间动画 (GTween, GTweener)
│   ├── UI/               # UI 组件 (GObject 层级, UIPackage, ScrollPane, Controller, Transition)
│   └── Utils/            # 工具 (XML, HTML, ByteBuffer, UBBParser)
├── Editor/               # 编辑器扩展 (8 个 Inspector/Window)
└── Resources/Shaders/    # 内置 Shader (Image, Text, BMFont, BlurFilter)
```

#### 核心类层级

```
GObject (所有 UI 元素基类)
├── GComponent (容器，可包含子对象)
│   ├── GRoot (全局根节点，单例)
│   ├── GList (列表/虚拟列表)
│   ├── GComboBox (下拉框)
│   ├── GLabel (标签)
│   ├── GButton (按钮)
│   ├── GProgressBar (进度条)
│   ├── GSlider (滑块)
│   ├── GScrollBar (滚动条)
│   └── Window (窗口基类)
├── GImage (图片)
├── GTextField / GRichTextField (文本)
├── GTextInput (输入框)
├── GGraph (图形)
├── GLoader / GLoader3D (外部资源加载)
├── GMovieClip (序列帧动画)
└── GTree (树形控件)
```

#### 关键类职责

| 类 | 职责 |
|---|---|
| `UIPackage` | 管理 FairyGUI 编辑器导出的 UI 资源包，`AddPackage()` / `RemovePackage()` |
| `UIPanel` | MonoBehaviour 组件，将 FairyGUI 面板挂载到 Unity 场景 |
| `GRoot` | 全局 UI 根节点（单例），`GRoot.inst` 访问 |
| `Controller` | 组件状态控制器（页面切换） |
| `Transition` | 组件过渡动画 |
| `ScrollPane` | 滚动容器（支持虚拟列表） |
| `UIObjectFactory` | 自定义组件类映射（`SetPackageItemExtension<T>(url)`） |
| `Stage` | 渲染和输入管理的底层单例 |

#### TextMeshPro 支持

- TMP 功能在 `#if FAIRYGUI_TMPRO` 条件编译下
- 启用方式：在 Project Settings > Player > Scripting Define Symbols 中添加 `FAIRYGUI_TMPRO`
- 相关类：`TMPFont`、`TMPTextFormat`（位于 `Runtime/Extensions/TextMeshPro/`）
- 包含专用 Shader：`Runtime/Extensions/TextMeshPro/Shaders/FairyGUI-TMP.shader`

### 开发工作流规则

#### 编译与热更新

- **F6** 编译热更新 DLL（ET.Model / ET.ModelView / ET.Hotfix / ET.HotfixView）
- **F7** 运行时热重载 DLL（仅 Play 模式下有效）
- 服务端编译：`dotnet build ET.sln`（需要代理/VPN 下载 NuGet 包）

#### 代码生成流程

- Proto → C#：`dotnet ./Packages/cn.etetet.proto/DotNet~/Exe/ET.Proto2CS.dll ./`
- Excel 配置导出：`dotnet ./Packages/cn.etetet.excel/DotNet~/Exe/ET.ExcelExporter.dll ./`
- 修改 Proto 或 Excel 后**必须**重新运行对应的代码生成工具

#### 首次初始化

- Unity 菜单 `ET/StateSync/Init`（或 `ET/LockStep/Init`）
- 执行：Excel 导出、Proto 编译、程序集引用设置、链接 ET.sln、设置 INITED 宏定义

#### 包结构

- 所有框架代码位于 `Packages/cn.etetet.*` 下
- **ET 业务包**：`Scripts/Model/` + `Scripts/Hotfix/`，每层再分 `Client/` `Server/` `Share/`
- **第三方库包**（FairyGUI、YooAssets）：`Runtime/` + `Editor/` 模式，不参与四程序集分离

### 关键易错规则

#### 绝对禁止

- **禁止在 Entity 类中写方法**：哪怕是简单的 getter/setter 也不行，除非标注 `[EnableMethod]`
- **禁止使用 `new` 创建 Entity/Component**：必须 `AddComponent<T>()` / `AddChild<T>()`，对象池会自动管理回收
- **禁止使用 `Task` / `async void`**：所有异步方法必须返回 `ETTask` / `ETTask<T>`
- **禁止在 Model 程序集中写逻辑**：Model 只放数据定义，逻辑全部在 Hotfix 中
- **禁止跨 Fiber 直接访问对象**：必须通过 Actor 消息通信
- **禁止声明非 `[StaticField]` 的静态字段**：热重载后状态泄漏

#### 常见陷阱

- 新建 Component 忘记标注 `[ComponentOf]` → 编译报错
- System 类忘记标注 `[FriendOf]` → 无法访问 Component 字段
- System 类忘记 `partial` 关键字 → 源代码生成器无法工作
- 生命周期方法忘记标注 `[EntitySystem]` → Awake/Destroy 不会被调用
- Handler 类忘记标注 `[MessageHandler(SceneType.X)]` → 消息无法路由
- 在 Client 命名空间写的代码放到了 Server 目录 → 编译报错
- 修改了 Proto 文件但没重新运行 Proto2CS → 运行时序列化异常
- `AddComponent` 类型与 `[ComponentOf]` 声明不匹配 → 编译报错

#### 代码模板速查

**新建 Component（Model 程序集）：**

```csharp
[ComponentOf(typeof(ParentEntity))]
public class MyComponent : Entity, IAwake, IDestroy
{
    public int MyField;
}
```

**新建 System（Hotfix 程序集）：**

```csharp
[EntitySystemOf(typeof(MyComponent))]
[FriendOf(typeof(MyComponent))]
public static partial class MyComponentSystem
{
    [EntitySystem]
    private static void Awake(this MyComponent self) { }

    [EntitySystem]
    private static void Destroy(this MyComponent self) { }

    public static void MyMethod(this MyComponent self) { }
}
```

**新建消息处理器（Hotfix 程序集）：**

```csharp
[MessageSessionHandler(SceneType.Gate)]
public class C2G_MyRequestHandler : MessageSessionHandler<C2G_MyRequest, G2C_MyResponse>
{
    protected override async ETTask Run(Session session, C2G_MyRequest request, G2C_MyResponse response) { }
}
```

---

## 使用指南

**AI Agent 须知：**

- 在实现任何代码之前先阅读本文件
- 严格遵循所有规则，不可省略或简化
- 有疑问时选择更严格的实现方式
- 发现新模式时应建议更新本文件

**维护建议：**

- 技术栈变更时及时更新版本信息
- 定期审查，移除已过时的规则
- 保持精简，聚焦于 Agent 易出错的非显而易见的细节

最后更新：2026-03-02
