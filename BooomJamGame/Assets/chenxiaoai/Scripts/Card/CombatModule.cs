using UnityEngine;
using System.Collections;

/// <summary>
/// 战斗模块，处理卡牌之间的战斗逻辑、位移攻击动画和受击反馈。
/// </summary>
public class CombatModule : ModuleBase
{
    [Header("Combat Settings")]
    [Tooltip("攻击动画持续时间")]
    public float attackDuration = 0.2f;
    [Tooltip("反击触发延迟 (秒)")]
    public float retaliationDelay = 1.0f;
    [Tooltip("受击时闪烁的颜色")]
    public Color hitFlashColor = Color.red;
    [Tooltip("受击时闪烁的强度")]
    public float hitFlashIntensity = 3f;
    [Tooltip("红光闪烁持续时间")]
    public float hitFlashDuration = 0.3f;

    [Header("Shader Properties")]
    public string emissionColorProperty = "_EmissionColor";
    public string emissionStrengthProperty = "_EmissionStrength";

    private Material cardMaterial;
    private Color originalEmissionColor;
    private float originalEmissionStrength;
    private bool isCombatInProgress = false;

    public override void OnModuleLoad(EntityCore entity)
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            cardMaterial = renderer.material;
            if (cardMaterial.HasProperty(emissionColorProperty))
                originalEmissionColor = cardMaterial.GetColor(emissionColorProperty);
            
            // 默认原始强度统一设为 0，确保战斗后彻底关闭发光
            originalEmissionStrength = 0f;
            
            if (cardMaterial.HasProperty(emissionStrengthProperty))
            {
                // 强制初始化发光强度为 0，防止出生时全亮
                cardMaterial.SetFloat(emissionStrengthProperty, 0f);
            }
        }
        
        Debug.Log($"[{gameObject.name}] 战斗模块已装载并重置发光。");
    }

    public override void OnModuleTick()
    {
        // 战斗逻辑主要由外部触发或协程处理
    }

    public override void OnModuleUnload()
    {
        Debug.Log($"[{gameObject.name}] 战斗模块已卸载。");
    }

    /// <summary>
    /// 触发战斗序列，直到一方死亡
    /// </summary>
    public IEnumerator PerformCombatSequence(EntityCore targetCore, Vector3 returnPos)
    {
        if (isCombatInProgress) yield break;
        isCombatInProgress = true;

        // 获取目标战斗模块
        CombatModule targetCombat = targetCore.GetComponent<CombatModule>();

        // 战斗主循环：直到一方生命值为 0
        while (Core.currentHealth > 0 && targetCore.currentHealth > 0)
        {
            // --- 1. 攻击者发起攻击 ---
            Vector3 startPos = transform.position;
            Vector3 attackPos = targetCore.transform.position;
            
            // 动画：冲过去
            yield return StartCoroutine(MoveTo(attackPos, attackDuration));

            // 伤害结算：目标扣血
            int damage = Mathf.Max(0, Core.attack - targetCore.defense);
            targetCore.currentHealth -= damage;
            Debug.Log($"[{Core.entityName}] 攻击了 [{targetCore.entityName}]，造成 {damage} 点伤害，目标剩余血量: {targetCore.currentHealth}");

            // 视觉反馈：目标闪红
            if (targetCombat != null)
            {
                StartCoroutine(targetCombat.FlashHitEffect());
            }

            // 动画：弹回原位
            yield return StartCoroutine(MoveTo(returnPos, attackDuration));

            // 检查目标是否死亡
            if (targetCore.currentHealth <= 0)
            {
                Debug.Log($"[{targetCore.entityName}] 已死亡，正在移除卡牌。");
                Destroy(targetCore.gameObject);
                break; // 结束战斗循环
            }

            // --- 2. 目标反击 ---
            yield return new WaitForSeconds(retaliationDelay);
            
            if (targetCombat != null)
            {
                Vector3 targetStartPos = targetCore.transform.position;
                // 反击动画：目标冲向当前实体
                yield return StartCoroutine(targetCombat.MoveTo(transform.position, attackDuration));

                // 伤害结算：自身扣血
                int counterDamage = Mathf.Max(0, targetCore.attack - Core.defense);
                Core.currentHealth -= counterDamage;
                Debug.Log($"[{targetCore.entityName}] 反击了 [{Core.entityName}]，造成 {counterDamage} 点伤害，自身剩余血量: {Core.currentHealth}");

                // 视觉反馈：自身闪红
                StartCoroutine(FlashHitEffect());

                // 目标弹回原位
                yield return StartCoroutine(targetCombat.MoveTo(targetStartPos, attackDuration));
            }

            // 检查自身是否死亡
            if (Core.currentHealth <= 0)
            {
                Debug.Log($"[{Core.entityName}] 已死亡，正在移除玩家卡牌。");
                Destroy(gameObject);
                break; // 结束战斗循环
            }

            // 每轮战斗结束后的短暂间歇
            yield return new WaitForSeconds(0.2f);
        }

        isCombatInProgress = false;
    }

    /// <summary>
    /// 平滑移动的内部协程
    /// </summary>
    public IEnumerator MoveTo(Vector3 targetPos, float duration)
    {
        Vector3 initialPos = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(initialPos, targetPos, elapsed / duration);
            yield return null;
        }
        transform.position = targetPos;
    }

    /// <summary>
    /// 受击闪红光效果
    /// </summary>
    public IEnumerator FlashHitEffect()
    {
        if (cardMaterial == null) yield break;

        // 设置为指定的受击颜色
        cardMaterial.SetColor(emissionColorProperty, hitFlashColor);
        cardMaterial.SetFloat(emissionStrengthProperty, hitFlashIntensity);

        float elapsed = 0f;
        while (elapsed < hitFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / hitFlashDuration;
            // 平滑恢复到原始颜色和强度
            cardMaterial.SetColor(emissionColorProperty, Color.Lerp(hitFlashColor, originalEmissionColor, t));
            cardMaterial.SetFloat(emissionStrengthProperty, Mathf.Lerp(hitFlashIntensity, originalEmissionStrength, t));
            yield return null;
        }

        // 确保恢复最终值
        cardMaterial.SetColor(emissionColorProperty, originalEmissionColor);
        cardMaterial.SetFloat(emissionStrengthProperty, originalEmissionStrength);
    }
}
