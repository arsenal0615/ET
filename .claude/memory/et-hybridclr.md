# ET 中的 HybridCLR 热更新

## 日常开发无需关心 HybridCLR

编辑器下是 Mono 运行时，F6 编译 + Play 即可。HybridCLR 只在真机打包时生效。

## 四个热更 DLL

| DLL | 内容 | 热重载 |
|-----|------|--------|
| ET.Model.dll | Entity/Component 数据定义 | 不参与（改了必须重启） |
| ET.ModelView.dll | 客户端数据定义 | 不参与 |
| ET.Hotfix.dll | System/Handler 逻辑 | F7 可热重载 |
| ET.HotfixView.dll | 客户端视图逻辑 | F7 可热重载 |

编译产物：`Temp/Bin/Debug/` → 复制到 `Packages/cn.etetet.loader/Bundles/Code/` 作为 `.bytes`。

## F6 编译流程

```
F6 → AssetDatabase.Refresh → CodeMode 切换 → CompilePlayerScripts → 复制 DLL 到 Code/ → Refresh
```

## F7 热重载流程（仅 Play 模式）

```
F7 → 重新加载 Hotfix + HotfixView DLL → 重新注册 CodeTypes → 触发所有组件的 LoadSystem
```

## 真机打包要点

1. `HybridCLR → Generate → All` — 生成桥接函数
2. `ET → HybridCLR → CopyAotDlls` — 复制 AOT DLL 到 Bundles/AotDlls
3. AOT 元数据补充：`RuntimeApi.LoadMetadataForAOTAssembly(bytes, HomologousImageMode.SuperSet)`
4. 泛型相关崩溃通常是 AotDll 没补充完整
