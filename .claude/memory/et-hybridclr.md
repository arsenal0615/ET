# ET 中的 HybridCLR 热更新

> HybridCLR 版本：7.8.1 | 官方文档：https://hybridclr.doc.code-philosophy.com/docs/intro | 源码：https://github.com/focus-creative-games/hybridclr

---

## 一、HybridCLR 是什么

HybridCLR 将 Unity 的 il2cpp（纯 AOT 运行时）改造为 **AOT + Interpreter 混合运行时**，使得 C# 代码可以热更新。

核心原理：
- **AOT 代码**：编译时已转为 C++ 的程序集（如 Unity 引擎代码、第三方库），运行效率高
- **热更代码**：运行时通过解释器执行的程序集（如 ET.Model.dll），可随时替换
- **元数据补充**：热更代码中使用的泛型等特性，需要补充 AOT 程序集的元数据才能正常工作

## 二、ET 的四个热更 DLL

ET 通过 F6 编译出 4 个热更 DLL：

| DLL | 内容 | 作用 |
|-----|------|------|
| **ET.Model.dll** | Entity/Component 数据定义 | 双端共用的数据层 |
| **ET.ModelView.dll** | 客户端 Unity 相关数据定义 | 客户端数据层 |
| **ET.Hotfix.dll** | 逻辑/System/Handler | 双端共用的逻辑层 |
| **ET.HotfixView.dll** | 客户端 UI/渲染逻辑 | 客户端逻辑层 |

编译产物路径：`Temp/Bin/Debug/` → 复制到 `Packages/cn.etetet.loader/Bundles/Code/` 作为 `.bytes` 资源。

## 三、DLL 加载流程 (CodeLoader)

`CodeLoader`（位于 `cn.etetet.loader/Scripts/Loader/Client/CodeLoader.cs`）负责整个加载流程：

```
CodeLoader.Start()
    ↓
DownloadAsync()  ← 非编辑器下从 YooAsset 加载 DLL 和 AotDll
    ↓
[非编辑器 + IL2CPP] LoadMetadataForAOTAssembly()  ← 补充 AOT 元数据
    ↓
Assembly.Load(ET.Model.dll)
Assembly.Load(ET.ModelView.dll)
    ↓
LoadHotfix()  ← 加载 ET.Hotfix.dll + ET.HotfixView.dll
    ↓
World.Instance.AddSingleton<CodeTypes>()  ← 注册所有程序集的类型
    ↓
ET.Entry.Start()  ← 调用游戏入口
```

### 编辑器 vs 真机的区别

| 环节 | 编辑器 | 真机 |
|------|--------|------|
| DLL 来源 | 从本地文件 `Define.CodeDir` 读取 | 从 YooAsset 的 AB 包加载 |
| AOT 元数据 | 不需要（编辑器是 Mono 运行时） | 需要 `LoadMetadataForAOTAssembly` |
| 运行方式 | Mono JIT 直接执行 | il2cpp AOT + HybridCLR 解释器 |

### AOT 元数据补充

```csharp
// 加载所有 AOT DLL 并补充元数据
foreach (var kv in this.aotDlls)
{
    TextAsset textAsset = kv.Value;
    RuntimeApi.LoadMetadataForAOTAssembly(textAsset.bytes, HomologousImageMode.SuperSet);
}
```

- `HomologousImageMode.SuperSet`：补充的 DLL 是 AOT DLL 的超集（推荐模式）
- AotDll 资源路径：`Packages/cn.etetet.loader/Bundles/AotDlls/`
- AotDll 通过 `ET → HybridCLR → CopyAotDlls` 菜单复制到 Bundles 目录

## 四、F6 编译流程详解

`AssemblyTool.DoCompile()`（位于 `cn.etetet.loader/Editor/Helper/AssemblyTool.cs`）：

```
F6 快捷键
    ↓
AssetDatabase.Refresh()  ← 确保文件时间戳正确
    ↓
CodeModeChangeHelper.ChangeToCodeMode()  ← 根据 GlobalConfig.CodeMode 切换代码可见性
    ↓
PlayerBuildInterface.CompilePlayerScripts()  ← Unity API 编译出 DLL
    ↓  输出到 Define.BuildOutputDir (Temp/Bin/Debug/)
CopyHotUpdateDlls()  ← 复制为 .dll.bytes / .pdb.bytes 到 Define.CodeDir
    ↓
AssetDatabase.Refresh()
```

四个 DLL 名字固定：`ET.Hotfix`, `ET.HotfixView`, `ET.Model`, `ET.ModelView`。

## 五、F7 热重载流程

```
F7 快捷键（仅 Play 模式生效）
    ↓
CodeLoader.Instance.Reload()
    ↓
LoadHotfix()  ← 重新加载 ET.Hotfix.dll + ET.HotfixView.dll
    ↓
World.Instance.AddSingleton<CodeTypes>()  ← 重新注册类型
    ↓
codeTypes.CodeProcess()  ← 触发所有组件的 LoadSystem
```

**注意**：热重载只替换 Hotfix / HotfixView 两个程序集，Model / ModelView 不重载（因为它们包含正在使用的 Entity 数据定义，重载会导致类型不匹配）。

## 六、打包流程中的 HybridCLR 步骤

完整打包顺序：
1. 去掉 `Development Build`
2. `HybridCLR → Installer` 安装（首次）
3. 编译 ET.sln
4. **`HybridCLR → Generate → All`** ← 生成桥接函数、AOT 引用等
5. **`ET → HybridCLR → CopyAotDlls`** ← 复制需要补充元数据的 DLL 到 Bundles/AotDlls
6. 用 YooAsset 打 AB 包
7. BuildPackage

## 七、关键 API

```csharp
// 补充 AOT 元数据（真机 IL2CPP 环境必须调用）
HybridCLR.RuntimeApi.LoadMetadataForAOTAssembly(byte[] dllBytes, HomologousImageMode mode);

// 预 JIT 方法（减少首次执行延迟，可选优化）
HybridCLR.RuntimeApi.PreJitMethod(MethodInfo method);
HybridCLR.RuntimeApi.PreJitClass(Type type);

// 设置解释器栈大小（高级调优）
HybridCLR.RuntimeApi.SetInterpreterThreadObjectStackSize(int size);
HybridCLR.RuntimeApi.SetInterpreterThreadFrameStackSize(int size);
```

## 八、开发注意事项

1. **日常开发不需要关心 HybridCLR**：编辑器下是 Mono 运行时，F6 编译 + Play 即可
2. **HybridCLR 只在真机打包时生效**：il2cpp + HybridCLR 解释器
3. **热更代码限制很少**：支持泛型、反射、多线程、MonoBehaviour、DOTS 等，几乎无缝
4. **AOT 元数据补充不能遗漏**：打包后如果泛型相关代码崩溃，通常是 AotDll 没补充完整
5. **ET 的 [StaticField] 规则与热重载相关**：静态字段在热重载时不会重置，所以 ET 禁止静态字段（除非显式标注）
6. **Model/ModelView 不参与热重载**：数据定义改了必须重启，只有逻辑（Hotfix/HotfixView）可以热重载
