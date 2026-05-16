using UnityEngine;

/// <summary>
/// 公告栏触发器。当玩家接触到此触发器时，调用 BillboardManager 打开 UI。
/// </summary>
public class BillboardTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 1. 检查进入的物体是否有 EntityCore
        EntityCore otherCore = other.GetComponent<EntityCore>();

        // 2. 判断是否为玩家 (根据项目约定 type == EntityType.Player)
        if (otherCore != null && otherCore.type == EntityType.Player)
        {
            if (BillboardManager.instance != null)
            {
                BillboardManager.instance.OpenBillboard();
                Debug.Log($"[BillboardTrigger] 玩家接触了公告栏 [{gameObject.name}]，正在打开面板。");
            }
            else
            {
                Debug.LogWarning("[BillboardTrigger] 场景中没有找到 BillboardManager 实例！");
            }
        }
    }
}
