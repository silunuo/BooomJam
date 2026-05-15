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
    public Color unlockedColor = Color.white;
    public Color lockedColor = Color.gray;
    public Color canUnlockColor = Color.yellow;

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        button.onClick.AddListener(OnNodeClicked);
    }

    public void RefreshUI()
    {
        if (GameManager.instance == null) return;

        bool isUnlocked = GameManager.instance.IsSkillUnlocked(skillID);
        bool canUnlock = CanBeUnlocked();

        // 更新颜色
        if (isUnlocked)
            icon.color = unlockedColor;
        else if (canUnlock)
            icon.color = canUnlockColor;
        else
            icon.color = lockedColor;

        // 更新交互状态：已解锁或目前无法解锁时不可点击
        button.interactable = !isUnlocked && canUnlock;
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

        // 根据表格效果实现逻辑（示例）
        switch (skillID)
        {
            case "skill_WildStrike": // 狂野一击（攻击类一阶）
                player.attack += 5;
                break;
            case "skill_BloodBlade": // 嗜血之刃（攻击类二阶）
                // 特殊逻辑实现...
                break;
            // ... 继续添加其他技能效果
        }

        Debug.Log($"[SkillTree] 解锁技能 {skillName}，应用效果。");
    }
}
