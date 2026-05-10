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
    private bool isAnimating = false;

    private void OnTriggerEnter(Collider other)
    {
        EntityCore core = other.GetComponent<EntityCore>();
        if (core != null && core.type == EntityType.Player && !hasTriggered && !isAnimating)
        {
            SwitchObjects();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        EntityCore core = other.GetComponent<EntityCore>();
        if (core != null && core.type == EntityType.Player)
        {
            // 当玩家离开触发区域时，允许下次再次触发
            hasTriggered = false;
        }
    }

    private void SwitchObjects()
    {
        hasTriggered = true;
        isAnimating = true;
        Debug.Log("触发物体位移切换...");

        int totalTweens = objectsToExit.Length + objectsToEnter.Length;
        int completedTweens = 0;

        void OnAnyTweenComplete()
        {
            completedTweens++;
            if (completedTweens >= totalTweens)
            {
                isAnimating = false;
            }
        }

        // 直接根据当前位置和 Offset 进行位移
        // 旧物体：从当前摆放位置 -> 加上 exitOffset
        foreach (var obj in objectsToExit)
        {
            if (obj != null)
            {
                // 关键修复：锁定当前物体及其所有子物体中的视觉模块，防止子物体卡牌弹回
                CardVisualModule[] childVisuals = obj.GetComponentsInChildren<CardVisualModule>();
                foreach (var v in childVisuals)
                {
                    v.IsExternalAnimating = true;
                }

                // 执行父物体位移
                obj.DOMove(obj.position + exitOffset, duration).SetEase(easeType).OnComplete(() => {
                    // 动画完成后恢复视觉模块的控制
                    if (obj != null)
                    {
                        CardVisualModule[] childVisuals = obj.GetComponentsInChildren<CardVisualModule>();
                        foreach (var v in childVisuals)
                        {
                            v.IsExternalAnimating = false;
                        }
                    }
                    OnAnyTweenComplete();
                });
            }
            else
            {
                OnAnyTweenComplete();
            }
        }

        // 新物体：从当前摆放位置 -> 加上 enterOffset
        foreach (var obj in objectsToEnter)
        {
            if (obj != null)
            {
                // 关键修复：锁定新物体及其所有子物体中的视觉模块
                CardVisualModule[] childVisuals = obj.GetComponentsInChildren<CardVisualModule>();
                foreach (var v in childVisuals)
                {
                    v.IsExternalAnimating = true;
                }

                // 执行父物体位移
                obj.DOMove(obj.position + enterOffset, duration).SetEase(easeType).OnComplete(() => {
                    // 动画完成后恢复视觉模块的控制
                    if (obj != null)
                    {
                        CardVisualModule[] childVisuals = obj.GetComponentsInChildren<CardVisualModule>();
                        foreach (var v in childVisuals)
                        {
                            v.IsExternalAnimating = false;
                        }
                    }
                    OnAnyTweenComplete();
                });
            }
            else
            {
                OnAnyTweenComplete();
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
