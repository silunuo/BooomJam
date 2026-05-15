using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// 技能树主面板管理类。
/// 负责刷新所有节点状态和显示当前技能点。
/// </summary>
public class SkillTreePanel : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI skillPointsText;
    public Button closeButton;

    [Header("Animation Settings")]
    public RectTransform mainPanelRect; // 需要滑动的面板
    public Image blockerMask; // 全屏遮罩图片，用于防止误触
    public float slideDuration = 0.5f;
    public Vector2 hiddenPos = new Vector2(0, 1200);
    public Vector2 visiblePos = Vector2.zero;

    [Header("Detail Panel References")]
    public GameObject detailPanel;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescriptionText;
    public GameObject confirmCancelGroup; // 包含确认和取消按钮的父物体
    public Button confirmButton;
    public Button cancelButton;

    private SkillNodeUI[] allNodes;
    private SkillNodeUI selectedNode;

    private void Awake()
    {
        allNodes = GetComponentsInChildren<SkillNodeUI>(true);
        if (closeButton != null) closeButton.onClick.AddListener(ClosePanel);
        
        // 初始化详情面板按钮
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmUnlock);
        if (cancelButton != null) cancelButton.onClick.AddListener(() => detailPanel?.SetActive(false));
        
        // 初始位置设置
        if (mainPanelRect != null)
        {
            mainPanelRect.anchoredPosition = hiddenPos;
        }

        // 初始隐藏详情面板
        if (detailPanel != null) detailPanel.SetActive(false);
    }

    private void OnEnable()
    {
        RefreshAllNodes();
        // 每次开启面板时也确保详情面板是关闭的
        if (detailPanel != null) detailPanel.SetActive(false);
        
        // 播放进入动画
        OpenPanel();
    }

    public void OpenPanel()
    {
        if (blockerMask != null) blockerMask.enabled = true; // 开始滑动时激活遮罩
        
        if (mainPanelRect != null)
        {
            mainPanelRect.DOKill();
            mainPanelRect.DOAnchorPos(visiblePos, slideDuration).SetEase(Ease.OutBack);
        }
    }

    public void ClosePanel()
    {
        if (mainPanelRect != null)
        {
            mainPanelRect.DOKill();
            mainPanelRect.DOAnchorPos(hiddenPos, slideDuration).SetEase(Ease.InBack).OnComplete(() => {
                if (blockerMask != null) blockerMask.enabled = false; // 完全隐藏后关闭遮罩
                gameObject.SetActive(false);
            });
        }
        else
        {
            if (blockerMask != null) blockerMask.enabled = false;
            gameObject.SetActive(false);
        }
    }

    public void RefreshAllNodes()
    {
        if (GameManager.instance == null) return;

        // 更新技能点显示
        if (skillPointsText != null)
        {
            skillPointsText.text = GameManager.instance.skillPoints.ToString();
        }

        // 刷新每个节点的显示和可点击状态
        foreach (var node in allNodes)
        {
            node.RefreshUI();
        }
    }

    /// <summary>
    /// 显示指定技能的详细信息
    /// </summary>
    public void ShowSkillDetails(SkillNodeUI node)
    {
        selectedNode = node;
        if (detailPanel == null) return;

        detailPanel.SetActive(true);
        if (detailNameText != null) detailNameText.text = node.skillName;
        if (detailDescriptionText != null) detailDescriptionText.text = node.skillDescription;

        bool isUnlocked = GameManager.instance.IsSkillUnlocked(node.skillID);
        bool canUnlock = node.CanBeUnlocked();

        // 只有未解锁且满足解锁条件时，才显示确认/取消按钮
        if (confirmCancelGroup != null)
        {
            confirmCancelGroup.SetActive(!isUnlocked && canUnlock);
        }
    }

    private void OnConfirmUnlock()
    {
        if (selectedNode != null)
        {
            selectedNode.DoUnlock();
        }
    }

    /// <summary>
    /// 检查指定阶级中是否有任何技能已被解锁
    /// </summary>
    public bool IsAnySkillUnlockedInTier(int targetTier)
    {
        if (GameManager.instance == null) return false;

        foreach (var node in allNodes)
        {
            if (node.tier == targetTier && GameManager.instance.IsSkillUnlocked(node.skillID))
            {
                return true;
            }
        }
        return false;
    }
}
