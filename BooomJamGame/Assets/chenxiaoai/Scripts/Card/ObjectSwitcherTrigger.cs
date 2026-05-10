using UnityEngine;
using DG.Tweening; // 确保你已经导入了 DOTween 插件

/// <summary>
/// 挂载在场景中的 Trigger 物体上。
/// 当玩家卡牌进入时，使用 DOTween 切换镜头内的物体。
/// </summary>
public class ObjectSwitcherTrigger : MonoBehaviour
{
    [Header("Objects to Switch")]
    [Tooltip("需要移出镜头的物体列表")]
    public Transform[] objectsToExit;
    [Tooltip("需要移入镜头的物体列表")]
    public Transform[] objectsToEnter;

    [Header("Movement Settings")]
    [Tooltip("移出的方向偏移量 (例如 Vector3(-10, 0, 0) 表示向左移出)")]
    public Vector3 exitOffset = new Vector3(-10, 0, 0);
    [Tooltip("移入的方向偏移量 (新物体会从 目标位置 + offset 的地方移入)")]
    public Vector3 enterOffset = new Vector3(10, 0, 0);

    [Header("Animation Settings")]
    [Tooltip("动画持续时间")]
    public float duration = 1.0f;
    [Tooltip("动画缓冲类型")]
    public Ease easeType = Ease.InOutQuad;

    [Header("State")]
    private bool hasTriggered = false;

    private void Start()
    {
        // 预处理：将移入的物体先放到“镜头外”的起始位置
        foreach (var obj in objectsToEnter)
        {
            if (obj != null)
            {
                // 物体当前位置是“目标位置”，我们需要把它挪到“起始位置”
                obj.position += enterOffset;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        EntityCore core = other.GetComponent<EntityCore>();
        if (core != null && core.type == EntityType.Player && !hasTriggered)
        {
            SwitchObjects();
        }
    }

    private void SwitchObjects()
    {
        hasTriggered = true;
        Debug.Log("检测到玩家卡牌，开始统一位移切换物体...");

        // 1. 批量移出：当前位置 -> 当前位置 + exitOffset
        foreach (var obj in objectsToExit)
        {
            if (obj != null)
            {
                obj.DOMove(obj.position + exitOffset, duration).SetEase(easeType);
            }
        }

        // 2. 批量移入：当前位置 (已在镜头外) -> 当前位置 - enterOffset (回到原位)
        foreach (var obj in objectsToEnter)
        {
            if (obj != null)
            {
                obj.DOMove(obj.position - enterOffset, duration).SetEase(easeType);
            }
        }
    }

    // 在编辑器中绘制辅助线
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        foreach (var obj in objectsToExit)
        {
            if (obj != null) Gizmos.DrawLine(obj.position, obj.position + exitOffset);
        }

        Gizmos.color = Color.green;
        foreach (var obj in objectsToEnter)
        {
            if (obj != null) Gizmos.DrawLine(obj.position + enterOffset, obj.position);
        }
    }
}
