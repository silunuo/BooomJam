using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>
/// 商店管理器，控制商店 UI 的显示、隐藏以及购买逻辑。
/// </summary>
public class ShopManager : MonoBehaviour
{
    public static ShopManager instance;

    [Header("UI Components")]
    public RectTransform shopPanel;
    public Image blockerMask; // 新增遮罩引用
    public Button buyAttackBtn;
    public Button buyDefenseBtn;
    public Button buyHealthBtn;
    public Button exitBtn;
    public TextMeshProUGUI priceBannerText;

    [Header("Settings (Current)")]
    public float slideDuration = 0.5f;
    public int itemPrice = 20;
    public int priceIncrement = 2;
    public int attackBonus = 2;
    public int defenseBonus = 2;
    public int healthBonus = 10;
    
    public Vector2 hiddenPos = new Vector2(0, 1000);
    public Vector2 visiblePos = Vector2.zero;

    private void Awake()
    {
        if (instance == null) instance = this;
        
        // 初始化 UI 状态
        if (shopPanel != null)
        {
            shopPanel.anchoredPosition = hiddenPos;
        }

        // 绑定按钮事件
        if (buyAttackBtn != null) buyAttackBtn.onClick.AddListener(() => Purchase("Attack"));
        if (buyDefenseBtn != null) buyDefenseBtn.onClick.AddListener(() => Purchase("Defense"));
        if (buyHealthBtn != null) buyHealthBtn.onClick.AddListener(() => Purchase("Health"));
        if (exitBtn != null) exitBtn.onClick.AddListener(CloseShop);

        UpdatePriceUI();
    }

    /// <summary>
    /// 初始化商店数值（由触发器调用）
    /// </summary>
    public void InitShopValues(int startPrice, int increment, int atkPlus, int defPlus, int hpPlus)
    {
        itemPrice = startPrice;
        priceIncrement = increment;
        attackBonus = atkPlus;
        defenseBonus = defPlus;
        healthBonus = hpPlus;
        UpdatePriceUI();
    }

    private void UpdatePriceUI()
    {
        if (priceBannerText != null) priceBannerText.text = "全场 " + itemPrice;
    }

    public void OpenShop()
    {
        if (blockerMask != null) blockerMask.enabled = true; // 开启时激活遮罩
        if (shopPanel != null)
        {
            shopPanel.DOAnchorPos(visiblePos, slideDuration).SetEase(Ease.OutBack);
        }
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.DOAnchorPos(hiddenPos, slideDuration).SetEase(Ease.InBack).OnComplete(() => {
                if (blockerMask != null) blockerMask.enabled = false; // 关闭后禁用遮罩
            });
        }
        else
        {
            if (blockerMask != null) blockerMask.enabled = false;
        }
    }

    private void Purchase(string statType)
    {
        // 查找场景中的玩家
        EntityCore player = null;
        foreach (var core in FindObjectsOfType<EntityCore>())
        {
            if (core.type == EntityType.Player)
            {
                player = core;
                break;
            }
        }

        if (player == null)
        {
            Debug.LogWarning("[Shop] 找不到玩家，无法购买！");
            return;
        }

        // 检查金钱是否足够
        if (player.gold < itemPrice)
        {
            Debug.Log("[Shop] 金币不足！需要 " + itemPrice);
            return;
        }

        // 扣钱
        player.gold -= itemPrice;

        // 加属性
        switch (statType)
        {
            case "Attack":
                player.attack += attackBonus;
                Debug.Log($"[Shop] 购买攻击力成功！当前攻击: {player.attack} (+{attackBonus})");
                break;
            case "Defense":
                player.defense += defenseBonus;
                Debug.Log($"[Shop] 购买防御力成功！当前防御: {player.defense} (+{defenseBonus})");
                break;
            case "Health":
                player.maxHealth += healthBonus;
                player.currentHealth += healthBonus;
                Debug.Log($"[Shop] 购买生命值成功！当前生命: {player.currentHealth}/{player.maxHealth} (+{healthBonus})");
                break;
        }

        // 增加价格
        itemPrice += priceIncrement;
        UpdatePriceUI();

        // 刷新 UIManager 的金钱显示
        if (UIManager.instance != null)
        {
            UIManager.instance.UpdateGoldUI();
        }
    }
}
