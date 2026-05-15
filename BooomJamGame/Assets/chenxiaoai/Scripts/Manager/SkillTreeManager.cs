using UnityEngine;
using DG.Tweening;

/// <summary>
/// 技能树管理器，模仿 ShopManager 结构。
/// 负责控制技能树面板的显示、隐藏以及单例访问。
/// </summary>
public class SkillTreeManager : MonoBehaviour
{
    public static SkillTreeManager instance;

    [Header("UI References")]
    public RectTransform skillTreePanelRect;
    public SkillTreePanel panelScript;
    public UnityEngine.UI.Image blockerMask; // 新增遮罩引用

    [Header("Animation Settings")]
    public float slideDuration = 0.5f;
    public Vector2 hiddenPos = new Vector2(0, 1500);
    public Vector2 visiblePos = Vector2.zero;

    private void Awake()
    {
        if (instance == null) instance = this;
        
        // 初始位置设置
        if (skillTreePanelRect != null)
        {
            skillTreePanelRect.anchoredPosition = hiddenPos;
        }

        // 初始关闭遮罩
        if (blockerMask != null) blockerMask.enabled = false;
    }

    public void OpenSkillTree()
    {
        // 开启面板时激活遮罩并拦截 3D 场景
        if (blockerMask != null) blockerMask.enabled = true;
        UIManager.IsBlocking3DScene = true;

        if (skillTreePanelRect != null)
        {
            skillTreePanelRect.DOKill(true);
            // 确保起始位置正确
            skillTreePanelRect.anchoredPosition = hiddenPos;
            skillTreePanelRect.DOAnchorPos(visiblePos, slideDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        if (panelScript != null)
        {
            panelScript.RefreshAllNodes();
        }
    }

    public void CloseSkillTree()
    {
        if (skillTreePanelRect != null)
        {
            skillTreePanelRect.DOKill(true);
            skillTreePanelRect.DOAnchorPos(hiddenPos, slideDuration).SetEase(Ease.InBack).SetUpdate(true)
            .OnComplete(() => {
                // 面板完全退场后禁用遮罩并恢复 3D 场景
                if (blockerMask != null) blockerMask.enabled = false;
                UIManager.IsBlocking3DScene = false;
            });
        }
        else
        {
            if (blockerMask != null) blockerMask.enabled = false;
            UIManager.IsBlocking3DScene = false;
        }
    }
}
