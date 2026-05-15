using UnityEngine;

/// <summary>
/// 技能点触发器。
/// 当玩家接触到挂载此脚本的物体时，增加技能点并同步 UI。
/// 作为一个独立的场景交互脚本，它继承自 MonoBehaviour，不需要 EntityCore。
/// </summary>
public class GoldTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("增加的技能点数量")]
    public int skillPointsToAdd = 1;
    [Tooltip("触发后是否销毁自身")]
    public bool destroyOnTrigger = true;

    private void OnTriggerEnter(Collider other)
    {
        // 1. 检查进入的物体是否有 EntityCore
        EntityCore otherCore = other.GetComponent<EntityCore>();

        // 2. 判断是否为玩家 (EntityType 在 EnemyDataTable.cs 中定义)
        if (otherCore != null && otherCore.type == EntityType.Player)
        {
            AddSkillPoint();
        }
    }

    private void AddSkillPoint()
    {
        if (GameManager.instance == null) return;

        // 增加技能点
        GameManager.instance.skillPoints += skillPointsToAdd;
        Debug.Log($"[GoldTrigger] 玩家获得 {skillPointsToAdd} 点技能值。当前总计: {GameManager.instance.skillPoints}");

        // 同步刷新技能树 UI（如果面板当前是开启状态）
        if (UIManager.instance != null && UIManager.instance.skillTreePanel != null)
        {
            SkillTreePanel panel = UIManager.instance.skillTreePanel.GetComponent<SkillTreePanel>();
            if (panel != null && UIManager.instance.skillTreePanel.activeSelf)
            {
                panel.RefreshAllNodes();
            }
        }

        // 销毁或禁用
        if (destroyOnTrigger)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
