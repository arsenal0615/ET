# FairyGUI 在 ET 中的集成

## 包概况

| 项 | 值 |
|---|---|
| 包名 | `cn.etetet.fairygui` |
| 来源 | 从 `FairyGUI-unity` 仓库整合 |
| 位置 | `Packages/cn.etetet.fairygui/` |
| 运行时程序集 | `ET.FairyGUI` |
| 编辑器程序集 | `ET.FairyGUI.Editor` |
| 命名空间 | `FairyGUI`、`FairyGUI.Utils`（保留原始命名空间） |
| 依赖 | `com.unity.ugui`（Unity 6 中 TMP 已合并进 ugui） |

## 包结构模式

FairyGUI 是第三方 UI 库，采用 `Runtime/` + `Editor/` 模式（同 `cn.etetet.yooassets`），**不参与** ET 四程序集热更分离：

```
cn.etetet.fairygui/
├── package.json
├── Runtime/                 # 运行时 (158 个 .cs + 1 个 .shader)
│   ├── ET.FairyGUI.asmdef   # references: ["Unity.TextMeshPro"]
│   ├── Core/                # 渲染核心 (DisplayObject, NGraphics, Stage)
│   │   ├── HitTest/         # 碰撞检测 (7 个)
│   │   ├── Mesh/            # 网格生成 (12 个)
│   │   └── Text/            # 文本渲染 (13 个, 含 InputTextField)
│   ├── Event/               # 事件 (EventDispatcher, EventListener 等 6 个)
│   ├── Extensions/          # 扩展 (DragonBones, Spine, TMP, WebGL)
│   ├── Filter/              # 滤镜 (Blur, Color)
│   ├── Gesture/             # 手势 (LongPress, Pinch, Rotation, Swipe)
│   ├── Tween/               # 补间 (GTween, GTweener, EaseManager)
│   ├── UI/                  # UI 组件 (43 个)
│   │   ├── Action/          # 控制器动作
│   │   ├── Gears/           # Gear 状态联动 (12 个)
│   │   └── Tree/            # 树形控件
│   └── Utils/               # 工具 (XML, HTML, ByteBuffer, UBBParser)
├── Editor/                  # 编辑器 (8 个 Inspector/Window)
│   └── ET.FairyGUI.Editor.asmdef  # references: ["ET.FairyGUI"], Editor only
└── Resources/Shaders/       # 内置 Shader (Image, Text, BMFont, BlurFilter)
```

## 核心类层级

```
GObject (所有 UI 元素基类)
├── GComponent (容器)
│   ├── GRoot (全局根节点, GRoot.inst)
│   ├── GList (列表/虚拟列表)
│   ├── GComboBox, GLabel, GButton
│   ├── GProgressBar, GSlider, GScrollBar
│   └── Window (窗口基类)
├── GImage, GGraph, GMovieClip
├── GTextField, GRichTextField, GTextInput
├── GLoader, GLoader3D
└── GTree
```

## 关键类

| 类 | 职责 |
|---|---|
| `UIPackage` | 管理 UI 资源包：`AddPackage()` / `RemovePackage()` |
| `UIPanel` | MonoBehaviour，将 FairyGUI 面板挂载到 Unity 场景 |
| `GRoot` | 全局根节点单例 `GRoot.inst` |
| `Controller` | 组件状态控制器 |
| `Transition` | 组件过渡动画 |
| `ScrollPane` | 滚动容器（支持虚拟列表） |
| `UIObjectFactory` | 自定义组件映射 `SetPackageItemExtension<T>(url)` |
| `Stage` | 渲染和输入管理底层单例 |
| `DragDropManager` | 全局拖放管理 |
| `PopupMenu` | 弹出菜单 |

## TextMeshPro 支持

- 条件编译：`#if FAIRYGUI_TMPRO`
- 启用：Player Settings > Scripting Define Symbols 添加 `FAIRYGUI_TMPRO`
- 相关类：`TMPFont`、`TMPTextFormat`（`Runtime/Extensions/TextMeshPro/`）
- 专用 Shader：`FairyGUI-TMP.shader`

## 其他扩展的条件编译

| 宏定义 | 功能 |
|---|---|
| `FAIRYGUI_TMPRO` | TextMeshPro 支持 |
| `FAIRYGUI_DRAGONBONES` | DragonBones 骨骼动画 |
| `FAIRYGUI_SPINE` | Spine 骨骼动画 |

## ET 代码引用 FairyGUI

其他 ET 包的 asmdef 要引用 FairyGUI 时：
```json
{
  "references": ["ET.FairyGUI"]
}
```

ET 热更代码（Scripts/Model 或 Hotfix 层）不直接引用 FairyGUI 的 asmdef，而是通过 AssemblyReference.asmref 间接引用。如果 ET 的 UI 系统需要调用 FairyGUI API，通常在 ModelView/HotfixView 层（客户端专用）进行。

## 内置 Shader

| Shader | 用途 |
|---|---|
| `Resources/Shaders/FairyGUI-Image.shader` | 图片渲染 |
| `Resources/Shaders/FairyGUI-Text.shader` | 文本渲染 |
| `Resources/Shaders/FairyGUI-BMFont.shader` | 位图字体 |
| `Resources/Shaders/AddOn/FairyGUI-BlurFilter.shader` | 模糊滤镜 |
| `Runtime/Extensions/TextMeshPro/Shaders/FairyGUI-TMP.shader` | TMP 距离场 |
