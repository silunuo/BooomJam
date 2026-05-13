using UnityEngine;

/// <summary>
/// 商店触发器脚本，挂载在场景中的商店物体上。
/// 当玩家卡牌接触时，呼出商店界面。
/// </summary>
public class ShopTrigger : MonoBehaviour
{
    [Header("Shop Configuration")]
    [Tooltip("起始价格")]
    public int startPrice = 20;
    [Tooltip("每次购买增加的价格")]
    public int priceIncrement = 2;
    [Header("Bonus Values")]
    [Tooltip("增加的攻击力")]
    public int attackBonus = 2;
    [Tooltip("增加的防御力")]
    public int defenseBonus = 2;
    [Tooltip("增加的生命值")]
    public int healthBonus = 10;

    private bool playerInside = false;

    private void OnTriggerEnter(Collider other)
    {
        EntityCore otherCore = other.GetComponent<EntityCore>();
        if (otherCore != null && otherCore.type == EntityType.Player)
        {
            playerInside = true;
            if (ShopManager.instance != null)
            {
                // 将当前触发器配置的数值传递给商店管理器
                ShopManager.instance.InitShopValues(startPrice, priceIncrement, attackBonus, defenseBonus, healthBonus);
                ShopManager.instance.OpenShop();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        EntityCore otherCore = other.GetComponent<EntityCore>();
        if (otherCore != null && otherCore.type == EntityType.Player)
        {
            playerInside = false;
            // 如果你想离开商店范围自动关闭，可以取消下面注释
            // if (ShopManager.instance != null) ShopManager.instance.CloseShop();
        }
    }
}
