using UnityEngine;

/// <summary>
/// 资源模块，标记一个实体为可消耗的资源（如剑、护甲）。
/// 使用 EntityCore 的数值作为提供给玩家的加成。
/// </summary>
public class ResourceModule : ModuleBase
{
    public enum ResourceType { Weapon, Armor, Consumable, Gold, Drug }
    
    [Header("资源设置")]
    public ResourceType resourceType = ResourceType.Weapon;
    
    [Tooltip("资源消耗后增加的攻击力 (默认取 EntityCore 的 attack)")]
    public int atkBonus;
    [Tooltip("资源消耗后增加的防御力 (默认取 EntityCore 的 defense)")]
    public int defBonus;
    [Tooltip("资源消耗后增加的金钱 (如果是 Gold 类型，默认取 EntityCore 的 gold)")]
    public int goldBonus;
    [Tooltip("资源消耗后恢复的生命值")]
    public int hpBonus;

    public override void OnModuleLoad(EntityCore entity)
    {
        // 如果没有手动设置，则默认读取 EntityCore 的数值
        if (atkBonus == 0) atkBonus = entity.attack;
        if (defBonus == 0) defBonus = entity.defense;
        if (goldBonus == 0) goldBonus = entity.gold;
        // 药物类型可以默认读取 entity 的 currentHealth 或者单独设置
        if (hpBonus == 0 && resourceType == ResourceType.Drug) hpBonus = entity.currentHealth;
        
        Debug.Log($"[{gameObject.name}] 资源模块已加载：ATK+{atkBonus}, DEF+{defBonus}, GOLD+{goldBonus}, HP+{hpBonus}");
    }

    public override void OnModuleTick()
    {
        // 资源卡通常不需要每帧逻辑
    }

    public override void OnModuleUnload()
    {
        Debug.Log($"[{gameObject.name}] 资源模块已卸载。");
    }

    /// <summary>
    /// 被消耗时的逻辑
    /// </summary>
    /// <param name="playerCore">接收加成的玩家核心</param>
    public void Consume(EntityCore playerCore)
    {
        if (playerCore == null) return;

        playerCore.attack += atkBonus;
        playerCore.defense += defBonus;
        playerCore.gold += goldBonus;
        
        // 处理生命值恢复
        if (hpBonus > 0)
        {
            playerCore.currentHealth = Mathf.Min(playerCore.maxHealth, playerCore.currentHealth + hpBonus);
        }

        Debug.Log($"[{playerCore.gameObject.name}] 消耗了 [{gameObject.name}]，当前属性：ATK {playerCore.attack}, DEF {playerCore.defense}, GOLD {playerCore.gold}, HP {playerCore.currentHealth}");

        // 资源卡消失
        Destroy(gameObject);
    }
}
