using UnityEngine;
using DG.Tweening;
using System.Collections;

/// <summary>
/// 公告栏管理器，负责管理 BillboardPanel1 和 BillboardPanel1 (1) 的动画和显示状态。
/// </summary>
public class BillboardManager : MonoBehaviour
{
    public static BillboardManager instance;

    [Header("UI Panels")]
    public RectTransform panel1;
    public RectTransform panel2;

    [Header("Animation Settings")]
    [Tooltip("从左向右滑入的时间")]
    public float slideDuration = 0.5f;
    
    [Header("Panel 1 Positions")]
    public Vector2 panel1HiddenPos = new Vector2(-1500, 0);
    public Vector2 panel1VisiblePos = new Vector2(-400, 0);

    [Header("Panel 2 Positions")]
    public Vector2 panel2HiddenPos = new Vector2(-1500, 0);
    public Vector2 panel2VisiblePos = new Vector2(400, 0);

    private bool isPanelOpen = false;
    private bool justOpened = false;

    private void Awake()
    {
        if (instance == null) instance = this;

        // 初始位置设置
        if (panel1 != null) panel1.anchoredPosition = panel1HiddenPos;
        if (panel2 != null) panel2.anchoredPosition = panel2HiddenPos;
    }

    private void Update()
    {
        // 如果面板开启中，且玩家点击了鼠标左键，尝试关闭面板
        if (isPanelOpen && !justOpened && Input.GetMouseButtonDown(0))
        {
            // 如果点击的不是面板区域，则关闭
            if (!IsClickingOnPanels())
            {
                CloseBillboard();
            }
        }
    }

    /// <summary>
    /// 打开公告栏面板
    /// </summary>
    public void OpenBillboard()
    {
        if (isPanelOpen) return;
        isPanelOpen = true;

        // 开启 UIManager 的 3D 场景拦截
        UIManager.IsBlocking3DScene = true;

        // 执行滑动动画
        if (panel1 != null)
        {
            panel1.DOKill(true);
            panel1.anchoredPosition = panel1HiddenPos;
            panel1.DOAnchorPos(panel1VisiblePos, slideDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }
        
        if (panel2 != null)
        {
            panel2.DOKill(true);
            panel2.anchoredPosition = panel2HiddenPos;
            panel2.DOAnchorPos(panel2VisiblePos, slideDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        // 标记刚刚开启
        StartCoroutine(ResetClickFlag());
    }

    /// <summary>
    /// 关闭公告栏面板
    /// </summary>
    public void CloseBillboard()
    {
        if (!isPanelOpen) return;
        isPanelOpen = false;

        // 执行滑动动画：返回各自的隐藏位置
        if (panel1 != null)
        {
            panel1.DOKill(true);
            panel1.DOAnchorPos(panel1HiddenPos, slideDuration).SetEase(Ease.InBack).SetUpdate(true);
        }

        if (panel2 != null)
        {
            panel2.DOKill(true);
            panel2.DOAnchorPos(panel2HiddenPos, slideDuration).SetEase(Ease.InBack).SetUpdate(true);
        }

        // 关闭 3D 场景拦截
        UIManager.IsBlocking3DScene = false;
    }

    private bool IsClickingOnPanels()
    {
        // 检查鼠标是否在 panel1 或 panel2 范围内
        bool overPanel1 = panel1 != null && RectTransformUtility.RectangleContainsScreenPoint(panel1, Input.mousePosition, null);
        bool overPanel2 = panel2 != null && RectTransformUtility.RectangleContainsScreenPoint(panel2, Input.mousePosition, null);
        return overPanel1 || overPanel2;
    }

    private IEnumerator ResetClickFlag()
    {
        justOpened = true;
        yield return new WaitForEndOfFrame();
        justOpened = false;
    }
}
