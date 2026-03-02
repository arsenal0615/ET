# ET 中的 YooAsset 资源管理

## 两层封装（选哪个很关键）

### ResourcesComponent（启动阶段，AOT 层）

- 类型：`Singleton<ResourcesComponent>`，非 Entity 组件
- 用途：**启动阶段**加载 DLL、Config、AotDll（此时 Fiber 还未创建）
- 加载后立即 Release handle，仅适合一次性读取

```csharp
await ResourcesComponent.Instance.CreatePackageAsync("DefaultPackage", true);
TextAsset dll = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>("ET.Model.dll");
```

### ResourcesLoaderComponent（运行阶段，热更层）

- 类型：`[ComponentOf]` 的 Entity 组件，在 **HotfixView** 程序集中
- 用途：**游戏运行阶段**资源加载，生命周期跟随父 Entity
- Entity Dispose 时自动 Release 所有资源

```csharp
var loader = scene.GetComponent<ResourcesLoaderComponent>();
GameObject prefab = await loader.LoadAssetAsync<GameObject>("Assets/Bundles/Unit/Skeleton.prefab");
```

**关键设计**：
- 内部用 Dictionary 缓存 handle，Entity 销毁时自动释放
- 协程锁防重复加载（同一 location 并发请求只加载一次）
- 可指定 Package：`AddComponent<ResourcesLoaderComponent, string>(packageName)`

加载方法：`LoadAssetAsync<T>`、`LoadAllAssetsAsync<T>`、`LoadSceneAsync`

## PlayMode 配置

通过 `YooConfig` ScriptableObject（`cn.etetet.yooassets/Resources/YooConfig`）：

| 模式 | 用途 |
|------|------|
| **EditorSimulateMode** | 日常开发，不需打 AB 包 |
| **OfflinePlayMode** | 单机包，从 StreamingAssets 加载 |
| **HostPlayMode** | 热更包，从 CDN 下载，正式发布用 |
| **WebPlayMode** | WebGL 平台 |

## 开发要点

1. 日常开发用 **EditorSimulateMode**
2. 游戏资源用 **ResourcesLoaderComponent**，不要用 ResourcesComponent
3. 资源路径：`Packages/cn.etetet.xxx/Bundles/...` 或 `Assets/Bundles/...`
4. 切场景/销毁 Entity 时资源自动释放，无需手动管理
5. ResourcesLoaderComponent 在 HotfixView 程序集中，服务端不可用
