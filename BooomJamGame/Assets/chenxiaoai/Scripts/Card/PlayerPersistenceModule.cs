using UnityEngine;

/// <summary>
/// 负责玩家数据的跨场景持久化。
/// 挂载在玩家卡牌上，自动与 GameManager 同步数值。
/// </summary>
public class PlayerPersistenceModule : ModuleBase
{
    [Header("Settings")]
    [Tooltip("是否在加载时自动恢复数据")]
    public bool autoLoad = true;
    [Tooltip("是否在卸载时自动保存数据")]
    public bool autoSave = true;

    public override void OnModuleLoad(EntityCore entity)
    {
        // 只有 Player 类型的实体才需要持久化
        if (entity.type != EntityType.Player) return;

        if (autoLoad && GameManager.instance != null)
        {
            GameManager.instance.LoadPlayerData(entity);
            Debug.Log($"[{gameObject.name}] 已从 GameManager 恢复玩家数据。");
        }
    }

    public override void OnModuleTick()
    {
        // 持久化模块不需要每帧逻辑
    }

    public override void OnModuleUnload()
    {
        if (Core == null || Core.type != EntityType.Player) return;

        if (autoSave && GameManager.instance != null)
        {
            GameManager.instance.SavePlayerData(Core);
            Debug.Log($"[{gameObject.name}] 已将玩家数据保存至 GameManager。");
        }
    }

    /// <summary>
    /// 手动触发保存（例如在某些关键点）
    /// </summary>
    public void ManualSave()
    {
        if (Core != null && GameManager.instance != null)
        {
            GameManager.instance.SavePlayerData(Core);
        }
    }
}
