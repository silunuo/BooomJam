using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 按钮悬停效果脚本。
/// 适用于 UI 按钮，提供缩放和颜色变化反馈。
/// </summary>
public class UIButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover Settings")]
    public float hoverScale = 1.1f;
    public float duration = 0.2f;
    public Ease easeType = Ease.OutBack;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 鼠标进入时放大
        transform.DOScale(originalScale * hoverScale, duration).SetEase(easeType).SetUpdate(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 鼠标离开时恢复
        transform.DOScale(originalScale, duration).SetEase(Ease.Linear).SetUpdate(true);
    }

    // 针对非 EventSystem 兼容情况的冗余处理
    private void OnMouseEnter()
    {
        transform.DOScale(originalScale * hoverScale, duration).SetEase(easeType).SetUpdate(true);
    }

    private void OnMouseExit()
    {
        transform.DOScale(originalScale, duration).SetEase(Ease.Linear).SetUpdate(true);
    }
}
