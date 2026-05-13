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

    [Header("Acceleration Settings")]
    [Tooltip("每回合减少的时间比例 (例如 0.8 表示下一回合时间是现在的 80%)")]
    public float speedMultiplierPerTurn = 0.85f;
    [Tooltip("最小动画持续时间，防止无限变快")]
    public float minAttackDuration = 0.05f;
    [Tooltip("最小延迟时间")]
    public float minRetaliationDelay = 0.1f;

    [Header("Positioning Settings")]
    [Tooltip("战斗时距离敌人的固定间距")]
    public float combatSnapDistance = 1.2f;
    [Tooltip("进入战斗位置的平滑时间")]
    public float enterCombatPosDuration = 0.3f;

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
    public IEnumerator PerformCombatSequence(EntityCore targetCore, Vector3 originalReturnPos)
    {
        if (isCombatInProgress) yield break;
        isCombatInProgress = true;

        // 获取目标战斗模块
        CombatModule targetCombat = targetCore.GetComponent<CombatModule>();

        // --- 0. 计算并移动到正方向战斗位置 ---
        Vector3 enemyPos = targetCore.transform.position;
        Vector3 playerPos = transform.position;
        Vector3 directionToPlayer = (playerPos - enemyPos);
        directionToPlayer.y = 0; // 忽略高度差

        Vector3 snapDirection;
        // 判断主要方向
        if (Mathf.Abs(directionToPlayer.x) > Mathf.Abs(directionToPlayer.z))
        {
            // 左右方向
            snapDirection = directionToPlayer.x > 0 ? Vector3.right : Vector3.left;
        }
        else
        {
            // 前后方向
            snapDirection = directionToPlayer.z > 0 ? Vector3.forward : Vector3.back;
        }

        Vector3 combatStartPos = enemyPos + snapDirection * combatSnapDistance;
         // 修正：强制将玩家的高度设置为与敌人一致，防止卡牌在空中对打
         combatStartPos.y = enemyPos.y; 
 
         // 平滑移动到这个正方向位置
         yield return StartCoroutine(MoveTo(combatStartPos, enterCombatPosDuration));
         
         // 更新后续弹回的位置
         Vector3 returnPos = combatStartPos;

         // 确保战斗过程中的高度统一（处理可能的微小偏差）
         returnPos.y = enemyPos.y;

        // 同步更新视觉模块的基础位置，防止战斗结束后跳回
        CardVisualModule visual = GetComponent<CardVisualModule>();
        if (visual != null)
        {
            visual.SyncBasePosition();
        }

        float currentAttackDur = attackDuration;
        float currentRetalDelay = retaliationDelay;

        // 战斗主循环：直到一方生命值为 0
        while (Core.currentHealth > 0 && targetCore.currentHealth > 0)
        {
            // --- 1. 攻击者发起攻击 ---
            Vector3 startPos = transform.position;
            Vector3 attackPos = targetCore.transform.position;
            
            // 动画：冲过去
            yield return StartCoroutine(MoveTo(attackPos, currentAttackDur));

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
            yield return StartCoroutine(MoveTo(returnPos, currentAttackDur));

            // 检查目标是否死亡
            if (targetCore.currentHealth <= 0)
            {
                Debug.Log($"[{targetCore.entityName}] 已死亡，正在移除卡牌。");
                
                // 击杀奖励：将敌人身上的 gold 增加给玩家
                if (Core.type == EntityType.Player)
                {
                    Core.gold += targetCore.gold;
                    Debug.Log($"[Combat] 击杀奖励：玩家获得了 {targetCore.gold} 金钱！当前总金钱: {Core.gold}");
                }
                
                yield return StartCoroutine(RemoveCardWithDeathEffect(targetCore.gameObject));
                break; // 结束战斗循环
            }

            // --- 2. 目标反击 ---
            yield return new WaitForSeconds(currentRetalDelay);
            
            if (targetCombat != null)
            {
                Vector3 targetStartPos = targetCore.transform.position;
                // 反击动画：目标冲向当前实体
                yield return StartCoroutine(targetCombat.MoveTo(transform.position, currentAttackDur));

                // 伤害结算：自身扣血
                int counterDamage = Mathf.Max(0, targetCore.attack - Core.defense);
                Core.currentHealth -= counterDamage;
                Debug.Log($"[{targetCore.entityName}] 反击了 [{Core.entityName}]，造成 {counterDamage} 点伤害，自身剩余血量: {Core.currentHealth}");

                // 视觉反馈：自身闪红
                StartCoroutine(FlashHitEffect());

                // 目标弹回原位
                yield return StartCoroutine(targetCombat.MoveTo(targetStartPos, currentAttackDur));
            }

            // 检查自身是否死亡
            if (Core.currentHealth <= 0)
            {
                Debug.Log($"[{Core.entityName}] 已死亡，正在移除玩家卡牌。");
                yield return StartCoroutine(RemoveCardWithDeathEffect(gameObject));
                break; // 结束战斗循环
            }

            // 每轮战斗结束后的短暂停歇，并加速下一回合
            yield return new WaitForSeconds(currentRetalDelay);
            
            // 加速：减少动画时长和延迟
            currentAttackDur = Mathf.Max(minAttackDuration, currentAttackDur * speedMultiplierPerTurn);
            currentRetalDelay = Mathf.Max(minRetaliationDelay, currentRetalDelay * speedMultiplierPerTurn);
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
    /// 移除死亡卡牌：有燃烧溶解组件时先播放动画，结束后再销毁。
    /// 注意：调用到这里时，死亡判定和奖励结算已经完成；动画只会延后 Destroy。
    /// 后续如果死亡期间还能被点击或检测到，可以先禁用 Collider / CardVisualModule。
    /// </summary>
    private IEnumerator RemoveCardWithDeathEffect(GameObject cardObject)
    {
        if (cardObject == null) yield break;

        if (cardObject.TryGetComponent(out CardBurnDissolve burnDissolve))
        {
            yield return burnDissolve.PlayAndWait();
        }

        if (cardObject != null)
        {
            Destroy(cardObject);
        }
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
