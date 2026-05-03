using UnityEngine;
using TMPro;

/// <summary>
/// 负责将 CardData 中的数据显示在卡牌表面的文本组件上。
/// </summary>
public class CardDisplay : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("左上角的攻击力文本")]
    public TMP_Text attackText;
    [Tooltip("左下角的生命值文本")]
    public TMP_Text healthText;
    [Tooltip("右下角的防御力文本")]
    public TMP_Text defenseText;

    private CardData cardData;

    private void Awake()
    {
        cardData = GetComponent<CardData>();
    }

    private void OnEnable()
    {
        if (cardData != null)
        {
            cardData.OnDataChanged += UpdateUI;
        }
        UpdateUI();
    }

    private void OnDisable()
    {
        if (cardData != null)
        {
            cardData.OnDataChanged -= UpdateUI;
        }
    }

    /// <summary>
    /// 刷新 UI 显示
    /// </summary>
    public void UpdateUI()
    {
        if (cardData == null) return;

        if (cardData.cardType == CardType.Combat)
        {
            if (attackText != null)
                attackText.text = cardData.attack.ToString();

            if (healthText != null)
                healthText.text = $"{cardData.currentHealth}/{cardData.maxHealth}";

            if (defenseText != null)
                defenseText.text = cardData.defense.ToString();
        }
        else if (cardData.cardType == CardType.Resource)
        {
            // 资源卡显示加成数值，例如 "+5"
            if (attackText != null)
                attackText.text = cardData.attackBonus > 0 ? $"+{cardData.attackBonus}" : "";

            if (healthText != null)
                healthText.text = cardData.healthBonus > 0 ? $"+{cardData.healthBonus}" : "";

            if (defenseText != null)
                defenseText.text = cardData.defenseBonus > 0 ? $"+{cardData.defenseBonus}" : "";
        }
    }
}
