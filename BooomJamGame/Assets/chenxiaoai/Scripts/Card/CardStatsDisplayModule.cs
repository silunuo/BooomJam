using UnityEngine;
using TMPro;

/// <summary>
/// 负责将 EntityCore 中的数据显示在卡牌表面的文本组件上。
/// 继承自 ModuleBase 以适配项目的模块化架构。
/// </summary>
public class CardStatsDisplayModule : ModuleBase
{
    [Header("UI References")]
    [Tooltip("左上角的攻击力文本")]
    public TMP_Text attackText;
    [Tooltip("左下角的生命值文本")]
    public TMP_Text healthText;
    [Tooltip("右下角的防御力文本")]
    public TMP_Text defenseText;

    private EntityCore entityCore;

    public override void OnModuleLoad(EntityCore entity)
    {
        entityCore = entity;
        UpdateUI();
        Debug.Log($"[{gameObject.name}] 卡牌数值显示模块已装载。");
    }

    public override void OnModuleTick()
    {
        // 实时更新 UI 数值（如果数值在战斗中发生变化）
        UpdateUI();
    }

    public override void OnModuleUnload()
    {
        Debug.Log($"[{gameObject.name}] 卡牌数值显示模块已卸载。");
    }

    /// <summary>
    /// 刷新 UI 显示
    /// </summary>
    public void UpdateUI()
    {
        if (entityCore == null) return;

        if (attackText != null)
            attackText.text = entityCore.attack.ToString();

        if (healthText != null)
            healthText.text = $"{entityCore.currentHealth}/{entityCore.maxHealth}";

        if (defenseText != null)
            defenseText.text = entityCore.defense.ToString();
    }
}
