using UnityEngine;

/// <summary>
/// 资源模块，标记一个实体为可消耗的资源（如剑、护甲）。
/// 使用 EntityCore 的数值作为提供给玩家的加成。
/// </summary>
public class ResourceModule : ModuleBase
{
    public enum ResourceType { Weapon, Armor, Consumable }
    
    [Header("资源设置")]
    public ResourceType resourceType = ResourceType.Weapon;
    
    [Tooltip("资源消耗后增加的攻击力 (默认取 EntityCore 的 attack)")]
    public int atkBonus;
    [Tooltip("资源消耗后增加的防御力 (默认取 EntityCore 的 defense)")]
    public int defBonus;

    public override void OnModuleLoad(EntityCore entity)
    {
        // 如果没有手动设置，则默认读取 EntityCore 的数值
        if (atkBonus == 0) atkBonus = entity.attack;
        if (defBonus == 0) defBonus = entity.defense;
        
        Debug.Log($"[{gameObject.name}] 资源模块已加载：ATK+{atkBonus}, DEF+{defBonus}");
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

        Debug.Log($"[{playerCore.gameObject.name}] 消耗了 [{gameObject.name}]，当前属性：ATK {playerCore.attack}, DEF {playerCore.defense}");

        // 资源卡消失
        Destroy(gameObject);
    }
}
