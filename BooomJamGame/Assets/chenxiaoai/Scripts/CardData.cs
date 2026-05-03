using UnityEngine;
using System;

public enum CardType { Combat, Resource }

/// <summary>
/// 存储卡牌的核心数据：生命值、攻击力和防御力。
/// </summary>
public class CardData : MonoBehaviour
{
    [Header("Card Settings")]
    public CardType cardType = CardType.Combat;

    [Header("Base Stats (For Combat)")]
    public int maxHealth = 10;
    public int currentHealth = 10;
    public int attack = 5;
    public int defense = 3;

    [Header("Resource Stats (For Resource Card)")]
    public int healthBonus = 0;
    public int attackBonus = 0;
    public int defenseBonus = 0;

    // 当数据发生变化时通知 UI 的事件
    public event Action OnDataChanged;

    private void Start()
    {
        currentHealth = maxHealth;
        NotifyDataChanged();
    }

    /// <summary>
    /// 受到伤害的逻辑
    /// </summary>
    /// <param name="incomingDamage">受到的原始伤害值</param>
    public void TakeDamage(int incomingDamage)
    {
        // 伤害计算：实际伤害 = 对方攻击力 - 自身防御力（最小为0）
        int actualDamage = Mathf.Max(0, incomingDamage - defense);
        currentHealth -= actualDamage;
        
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log($"{gameObject.name} 受到 {actualDamage} 点伤害 (防御抵扣 {defense})，剩余生命: {currentHealth}");
        
        NotifyDataChanged();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} 已死亡");
        // 这里可以添加死亡动画或销毁逻辑
        // Destroy(gameObject, 0.5f);
    }

    public void NotifyDataChanged()
    {
        OnDataChanged?.Invoke();
    }

    // 用于在 Inspector 中修改数值后实时刷新 UI (可选)
    private void OnValidate()
    {
        NotifyDataChanged();
    }
}
