# AudioManager 音频接入说明

> 这套音频资源按命名前缀分好类。每个音频文件都有一个对应的 `BGMEntry` 或 `SFXEntry` 资产，`AudioManager.prefab` 已经把这些资产挂到列表里。

## 文件清单

| 路径 | 用途 |
|------|------|
| `audio/AudioManager.cs` | 音频管理器，场景中挂一个即可 |
| `audio/BGMEntry.cs` | 单首 BGM 的配置资产类型 |
| `audio/SFXEntry.cs` | 单个音效的配置资产类型 |
| `Prefab/Manager/AudioManager.prefab` | 已配置好的音频管理器 prefab |
| `Prefab/BGM/**` | BGMEntry 资产，和 BGM 音频一一对应 |
| `Prefab/SFX/**` | SFXEntry 资产，和 SFX 音频一一对应 |

## 资源分类

| 分类 | 资源 ID |
|------|---------|
| `BGM/deskScene` | `deskScene_bg` |
| `BGM/indoorScene` | `indoorScene_bg` |
| `SFX/common` | `common_UIClick` |
| `SFX/deskInteract` | `deskInteract_Battle`, `deskInteract_FallWater`, `deskInteract_GodBless`, `deskInteract_GoldCoin`, `deskInteract_LifeWater`, `deskInteract_PutDownCard`, `deskInteract_Shield`, `deskInteract_Shield_backup`, `deskInteract_Sword`, `deskInteract_TapCard` |
| `SFX/deskScene` | `deskScene_Lose`, `deskScene_Win` |
| `SFX/indoorInteract` | `indoorInteract_Common` |
| `SFX/indoorRole` | `indoorRole_DaughterSad`, `indoorRole_DebtorSneer`, `indoorRole_Dialogue`, `indoorRole_PlayerAngry`, `indoorRole_WifeSigh` |
| `SFX/indoorScene` | `indoorScene_OpenDoor`, `indoorScene_PoliceCar`, `indoorScene_Walk` |

## 快速上手

1. 打开第一个会运行的场景。
2. 将 `Assets/Resources/GameAudio/Prefab/Manager/AudioManager.prefab` 拖到场景里。
3. 运行后，`AudioManager` 会自动 `DontDestroyOnLoad`，后续场景不用重复放。
4. 代码里通过 `AudioManager.Instance` 调用。

```csharp
AudioManager.Instance.PlayBGM("deskScene_bg");
AudioManager.Instance.PlaySFX("common_UIClick");
AudioManager.Instance.PlaySFX("deskInteract_Sword", transform.position);
```

## 常用 API

| API | 用途 |
|-----|------|
| `PlayBGM(string bgmID)` | 播放或切换 BGM |
| `StopBGM(float fadeDuration = -1f)` | 淡出停止 BGM |
| `PlaySFX(string sfxID)` | 播放 2D 音效 |
| `PlaySFX(string sfxID, Vector3 worldPosition)` | 在世界坐标播放 3D 音效 |
| `StopSFX(string sfxID)` | 停止指定音效 |
| `StopAllSFX()` | 停止所有音效 |
| `SetMasterVolume(float volume)` | 设置主音量，范围 `0~1` |
| `SetBGMVolume(float volume)` | 设置 BGM 音量，范围 `0~1` |
| `SetSFXVolume(float volume)` | 设置 SFX 音量，范围 `0~1` |

## 挂载说明

`AudioManager.prefab` 已经挂好：

- `sfxEntries`：22 个 `SFXEntry`
- `bgmEntries`：2 个 `BGMEntry`
- `sfxPoolSize`：12
- `defaultBGMFadeDuration`：1.5 秒
- `mainMixer`：暂时为空，需要混音器时再手动拖入

如果以后新增音频，按这几步做：

1. 把音频放到对应分类目录。
2. 右键创建 `Audio/SFX Entry` 或 `Audio/BGM Entry`。
3. `ID` 填音频文件名，不带扩展名。
4. 把音频拖到 `clip` 或 `clips`。
5. 把新资产拖到 `AudioManager.prefab` 对应列表。

## 验证清单

- `AudioManager.prefab` 在启动场景里有一个实例。
- `sfxEntries` 和 `bgmEntries` 没有空项。
- 播放时使用的 ID 和资产里的 `sfxID` / `bgmID` 一致。
- BGM 切场景后如果还要继续播放，不需要重新创建 `AudioManager`。
