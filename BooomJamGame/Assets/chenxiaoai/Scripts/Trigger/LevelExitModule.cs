using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡出口脚本。
/// 当 Player 类型的卡牌与其接触（触发器碰撞）时，跳转到指定场景。
/// 作为一个独立的场景交互脚本，它不需要继承 ModuleBase。
/// </summary>
public class LevelExitModule : MonoBehaviour
{
    [Header("Transition Settings")]
    [Tooltip("要跳转的目标场景名称")]
    public string targetSceneName;
    
    [Tooltip("是否在跳转前自动保存玩家数据 (配合 PlayerPersistenceModule)")]
    public bool saveBeforeExit = true;

    private bool hasExited = false;

    /// <summary>
    /// 触发检测：当有物体进入触发器时调用
    /// 注意：此物体必须带有 Collider，且 Is Trigger 为 true
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (hasExited) return;

        // 1. 检查进入的物体是否有 EntityCore
        EntityCore otherCore = other.GetComponent<EntityCore>();
        
        // 2. 判断是否为玩家
        if (otherCore != null && otherCore.type == EntityType.Player)
        {
            TriggerExit();
        }
    }

    private void TriggerExit()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError($"[{gameObject.name}] 无法跳转：目标场景名称为空！");
            return;
        }

        hasExited = true;
        Debug.Log($"[{gameObject.name}] 检测到玩家，正在准备进入下一关: {targetSceneName}");

        // 跳转场景
        SceneManager.LoadScene(targetSceneName);
    }
}
