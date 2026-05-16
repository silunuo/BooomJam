using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 单个技能节点 UI 逻辑。
/// 处理技能解锁判定、效果应用及视觉反馈。
/// </summary>
public class SkillNodeUI : MonoBehaviour
{
    [Header("Skill Config")]
    public string skillID;
    public string skillName;
    [TextArea(3, 10)]
    public string skillDescription;
    public int tier = 1;

    [Header("UI References")]
    public Image icon;
    public Button button;

    [Header("Visual Config")]
    [Tooltip("解锁后的正常颜色（通常设为白色）")]
    public Color unlockedColor = Color.white;

    private ColorBlock initialColors; // 自动获取 Button 组件上的初始配置

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        
        // 自动记住您在 Button 组件上配好的那一套颜色（点击前的灰色系列）
        if (button != null) initialColors = button.colors;

        button.onClick.AddListener(OnNodeClicked);
        
        // 初始刷新一次
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (GameManager.instance == null || button == null) return;

        bool isUnlocked = GameManager.instance.IsSkillUnlocked(skillID);

        // 获取当前按钮的颜色块副本
        ColorBlock cb = initialColors;

        if (isUnlocked)
        {
            // 如果解锁了，我们把正常、高亮、选中状态都改为解锁色（白色）
            cb.normalColor = unlockedColor;
            cb.highlightedColor = unlockedColor;
            cb.selectedColor = unlockedColor;
            // 按下状态可以稍微深一点或者保持不变，这里根据 initialColors 的比例调整或保持
        }
        // 如果未解锁，cb 保持为 initialColors（即您在 Button 组件配好的灰色系列）

        button.colors = cb;
        button.interactable = true; 
    }

    public bool CanBeUnlocked()
    {
        if (GameManager.instance == null || GameManager.instance.skillPoints <= 0) return false;

        // 1阶技能直接检查技能点（只要有技能点且未解锁即可）
        if (tier == 1) return true;

        // 新逻辑：只要前一阶段点过任意一个技能，即可解锁当前阶段的技能
        SkillTreePanel panel = GetComponentInParent<SkillTreePanel>();
        if (panel != null)
        {
            return panel.IsAnySkillUnlockedInTier(tier - 1);
        }

        return false;
    }

    private void OnNodeClicked()
    {
        // 点击节点改为显示详情，而不是直接解锁
        GetComponentInParent<SkillTreePanel>()?.ShowSkillDetails(this);
    }

    /// <summary>
    /// 执行解锁逻辑，由 SkillTreePanel 的确认按钮调用
    /// </summary>
    public void DoUnlock()
    {
        if (CanBeUnlocked())
        {
            GameManager.instance.skillPoints--;
            GameManager.instance.UnlockSkill(skillID);
            
            // 应用技能效果
            ApplyEffect();

            // 通知面板刷新
            SkillTreePanel panel = GetComponentInParent<SkillTreePanel>();
            if (panel != null)
            {
                panel.RefreshAllNodes();
                // 解锁后重新刷新详情面板状态（隐藏确认按钮）
                panel.ShowSkillDetails(this);
            }
        }
    }

    private void ApplyEffect()
    {
        // 查找玩家 EntityCore
        EntityCore player = null;
        foreach (var core in FindObjectsOfType<EntityCore>())
        {
            if (core.type == EntityType.Player)
            {
                player = core;
                break;
            }
        }

        if (player == null) return;

        // 核心：将解锁的技能名称添加到玩家的技能列表中
        // 战斗逻辑（CombatModule）会根据这个列表触发对应效果
        if (!player.skills.Contains(skillName))
        {
            player.skills.Add(skillName);
        }

        Debug.Log($"[SkillTree] 解锁技能 {skillName}，已注册到玩家技能列表。");
    }
}
