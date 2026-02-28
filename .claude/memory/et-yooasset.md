# ET 中的 YooAsset 资源管理

> YooAsset 版本：2.3.6 | 官方文档：https://www.yooasset.com/ | 源码：https://github.com/tuyoogame/YooAsset

---

## 一、在 ET 中的封装结构

ET 对 YooAsset 做了两层封装，分别用于不同阶段：

### ResourcesComponent（全局单例，AOT 层）
- 位置：`cn.etetet.yooassets/Runtime/ET/ResourcesComponent.cs`
- 类型：`Singleton<ResourcesComponent>`，非 Entity 组件
- 用途：**启动阶段**加载 DLL、Config、AotDll 等基础资源（此时 Fiber 还未创建，无法用 Entity 组件）
- 职责：初始化 YooAssets、创建 Package、管理 PlayMode

```csharp
// 启动阶段用法
await ResourcesComponent.Instance.CreatePackageAsync("DefaultPackage", true);
TextAsset dll = await ResourcesComponent.Instance.LoadAssetAsync<TextAsset>("ET.Model.dll");
```

**注意**：`LoadAssetAsync` 加载后会立即 `Release()` handle，适合一次性读取（如 DLL bytes），不适合需要长期持有的资源。

### ResourcesLoaderComponent（Entity 组件，热更层）
- 数据定义：`cn.etetet.yooassets/Scripts/ModelView/Client/ResourcesLoaderComponent.cs`
- 逻辑实现：`cn.etetet.yooassets/Scripts/HotfixView/Client/ResourcesLoaderComponentSystem.cs`
- 类型：`[ComponentOf]` 的 Entity 组件
- 用途：**游戏运行阶段**的资源加载，生命周期跟随父 Entity

```csharp
// 游戏运行中用法 —— 资源生命周期跟随 Entity
var loader = scene.GetComponent<ResourcesLoaderComponent>();
GameObject prefab = await loader.LoadAssetAsync<GameObject>("Assets/Bundles/Unit/Skeleton.prefab");
```

#### 关键设计
1. **生命周期绑定**：loader 内部用 `Dictionary<string, HandleBase>` 缓存所有 handle，Entity Dispose 时自动 Release 所有资源。例如 CurrentScene 的 loader 在切场景时自动释放该场景的所有资源
2. **协程锁防重复加载**：同一 location 的并发加载请求通过 `CoroutineLock` 保证只加载一次
3. **支持指定 Package**：可通过 `AddComponent<ResourcesLoaderComponent, string>(packageName)` 指定非默认 Package

#### 支持的加载类型
| 方法 | 用途 |
|------|------|
| `LoadAssetAsync<T>(location)` | 加载单个资源 |
| `LoadAllAssetsAsync<T>(location)` | 加载同路径下所有资源，返回 `Dictionary<string, T>` |
| `LoadSceneAsync(location, mode)` | 加载场景 |

## 二、4 种 PlayMode

通过 `YooConfig` ScriptableObject 配置（位于 `cn.etetet.yooassets/Resources/YooConfig`）：

```csharp
public class YooConfig : ScriptableObject
{
    public EPlayMode EPlayMode;  // 运行模式
    public string Url;           // CDN 地址（HostPlayMode 用）
}
```

| 模式 | 用途 | 说明 |
|------|------|------|
| **EditorSimulateMode** | 开发调试 | 编辑器模拟，不需要打 AB 包，直接加载资源，**日常开发用这个** |
| **OfflinePlayMode** | 单机包 | 从 StreamingAssets 加载内置资源，不联网 |
| **HostPlayMode** | 热更包 | 从 CDN 下载资源，支持增量更新，**正式发布用这个** |
| **WebPlayMode** | WebGL | WebGL 平台专用 |

## 三、资源加载流程

```
YooAssets.Initialize()
    ↓
CreatePackageAsync("DefaultPackage")
    ↓  根据 PlayMode 选择初始化参数
package.InitializeAsync(parameters)
    ↓
package.RequestPackageVersionAsync()  → 获取最新版本号
    ↓
package.UpdatePackageManifestAsync()  → 更新清单
    ↓
可以开始加载资源
```

## 四、CDN 地址规则

`ResourcesComponent` 中的 `GetHostServerURL` 按平台拼接：
- Android: `{url}/CDN/Android/v1.0`
- iOS: `{url}/CDN/IPhone/v1.0`
- PC: `{url}/CDN/PC/v1.0`
- WebGL: `{url}/StreamingAssets/Bundles/{packageName}`

## 五、打包流程中的资源步骤

1. 打开 `YooAsset → AssetBundle Builder` 窗口
2. BuildPipeline 选 **ScriptableBuildPipeline**
3. BuildMode 选 **IncrementalBuild**
4. CopyBuildinFileOption 选 **ClearAndCopyAll**
5. 点击 **Click Build**
6. YooConfig 中 EPlayMode 改为 **HostPlayMode**
7. `ET → Loader → Build Tool` 点击 BuildPackage

## 六、开发注意事项

1. **日常开发用 EditorSimulateMode**，不需要每次打 AB 包
2. **游戏资源用 ResourcesLoaderComponent 加载**，不要用 ResourcesComponent（后者仅用于启动阶段）
3. **资源路径格式**：`Packages/cn.etetet.xxx/Bundles/...` 或 `Assets/Bundles/...`
4. **切场景/销毁 Entity 时资源自动释放**，不需要手动管理 handle
5. ResourcesLoaderComponent 在 **HotfixView** 程序集中（客户端专用），服务端不可用
