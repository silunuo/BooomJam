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
    private CardHandDrawnHitEffectTrigger handDrawnHitEffectTrigger;

    // 技能相关状态变量
    private int combatRoundCount = 0;
    private int baseAttackAtCombatStart;
    private int baseDefenseAtCombatStart;
    private int baseMaxHPAtCombatStart;

    public override void OnModuleLoad(EntityCore entity)
    {        Renderer renderer = GetComponent<Renderer>();
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

        TryGetComponent(out handDrawnHitEffectTrigger);
        
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
    /// 检查实体是否拥有某个技能
    /// </summary>
    private bool HasSkill(string skillID)
    {        return Core != null && Core.skills.Contains(skillID);
    }

    /// <summary>
    /// 触发战斗序列，直到一方死亡
    /// </summary>
    public IEnumerator PerformCombatSequence(EntityCore targetCore, Vector3 originalReturnPos)
    {        if (isCombatInProgress) yield break;
        isCombatInProgress = true;

        // 获取目标战斗模块
        CombatModule targetCombat = targetCore.GetComponent<CombatModule>();

        // --- 技能初始化：记录战斗开始时的基础数值 ---
        combatRoundCount = 0;
        baseAttackAtCombatStart = Core.attack;
        baseDefenseAtCombatStart = Core.defense;
        baseMaxHPAtCombatStart = Core.maxHealth;

        // [技能] 狂野一击 (skill_WildStrike)：战斗开始前额外造成一次自身攻击力 150% 的伤害
        if (HasSkill("狂野一击"))
        {
            // 临时提升攻击力以产生视觉反馈
            Core.attack = Mathf.RoundToInt(baseAttackAtCombatStart * 1.5f);
            
            int finalDamage = Mathf.Max(1, Core.attack - targetCore.defense);
            targetCore.currentHealth -= finalDamage;
            Debug.Log($"[技能-狂野一击] 战斗前突袭！攻击力提升至 {Core.attack}，对 [{targetCore.entityName}](防御:{targetCore.defense}) 造成 {finalDamage} 点伤害。");
            
            if (targetCombat != null) StartCoroutine(targetCombat.FlashHitEffect());
            
            // 立即恢复攻击力，确保卡牌数值回弹
            Core.attack = baseAttackAtCombatStart;

            // 如果突袭直接杀死了目标
            if (targetCore.currentHealth <= 0)
            {
                yield return StartCoroutine(HandleTargetDeath(targetCore));
                isCombatInProgress = false;
                yield break;
            }
            
            yield return new WaitForSeconds(0.5f); // 停顿一下让玩家看清数值变化
        }

        // --- 0. 计算并移动到正方向战斗位置 ---
        Vector3 enemyPos = targetCore.transform.position;
        Vector3 playerPos = transform.position;
        Vector3 directionToPlayer = (playerPos - enemyPos);
        directionToPlayer.y = 0; // 忽略高度差

        Vector3 snapDirection;
        // 判断主要方向
        if (Mathf.Abs(directionToPlayer.x) > Mathf.Abs(directionToPlayer.z))
        {            // 左右方向
            snapDirection = directionToPlayer.x > 0 ? Vector3.right : Vector3.left;
        }
        else
        {            // 前后方向
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
        {            visual.SyncBasePosition();
        }

        float currentAttackDur = attackDuration;
        float currentRetalDelay = retaliationDelay;

        // 战斗主循环：直到一方生命值为 0
        while (Core.currentHealth > 0 && targetCore.currentHealth > 0)
        {
            combatRoundCount++;

            // 关键修复：每回合开始前，先将属性恢复到战斗开始前的基础值，防止数值叠加错误
            Core.attack = baseAttackAtCombatStart;
            Core.defense = baseDefenseAtCombatStart;

            Debug.Log($"--- 第 {combatRoundCount} 回合 ---");

            // --- 技能：回合开始触发 ---
            // [技能] 伺机而动 (skill_WaitForOpportunity)：战斗开始前三回合防御力增加基础值 20%
            if (HasSkill("伺机而动") && combatRoundCount <= 3)
            {
                int bonusDef = Mathf.RoundToInt(baseDefenseAtCombatStart * 0.2f);
                Core.defense += bonusDef;
            }

            // [技能] 愈战愈勇 (skill_BattleFury)：战斗时每回合主角的攻击力增加基础值的 2%
            if (HasSkill("愈战愈勇"))
            {
                int bonusAtk = Mathf.RoundToInt(baseAttackAtCombatStart * 0.02f * combatRoundCount);
                Core.attack += bonusAtk;
            }

            // [技能] 神圣庇护 (skill_DivineProtection)：每回合回复防御力 10% 的生命值
            if (HasSkill("神圣庇护"))
            {                int healAmount = Mathf.Max(1, Mathf.RoundToInt(Core.defense * 0.1f));
                Core.currentHealth = Mathf.Min(Core.maxHealth, Core.currentHealth + healAmount);
                Debug.Log($"[技能-神圣庇护] 回复了 {healAmount} 点生命值。");
            }

            // --- 1. 攻击者发起攻击 ---
            Vector3 startPos = transform.position;
            Vector3 attackPos = targetCore.transform.position;
            
            // 动画：冲过去
            yield return StartCoroutine(MoveTo(attackPos, currentAttackDur));

            // [技能] 持盾猛击 (skill_ShieldBash)：攻击时防御力的 20% 等量增加到攻击力上
            int originalAtkForBash = Core.attack;
            if (HasSkill("持盾猛击"))
            {
                int bonus = Mathf.RoundToInt(Core.defense * 0.2f);
                Core.attack += bonus;
                Debug.Log($"[技能-持盾猛击] 防御转化为力量！攻击力临时提升: {bonus}");
            }

            // 伤害结算：目标扣血
            int damage = Mathf.Max(1, Core.attack - targetCore.defense);

            // [技能] 嗜血之刃 (skill_BloodBlade)：攻击造成伤害的 10% 会回复自身
            if (HasSkill("嗜血之刃"))
            {
                int lifesteal = Mathf.Max(1, Mathf.RoundToInt(damage * 0.1f));
                Core.currentHealth = Mathf.Min(Core.maxHealth, Core.currentHealth + lifesteal);
                Debug.Log($"[技能-嗜血之刃] 吸取了 {lifesteal} 点生命值。");
            }

            targetCore.currentHealth -= damage;
            Debug.Log($"[{Core.entityName}] 攻击了 [{targetCore.entityName}]，造成 {damage} 点伤害，目标剩余血量: {targetCore.currentHealth}");

            // 视觉反馈：目标闪红
            if (targetCombat != null)
            {
                StartCoroutine(targetCombat.FlashHitEffect());
            }

            // 动画：弹回原位
            yield return StartCoroutine(MoveTo(returnPos, currentAttackDur));

            // 立即恢复攻击力（持盾猛击结束）
            Core.attack = originalAtkForBash;

            // [技能] 极限撕裂 (skill_ExtremeRip)：若攻击后怪物血量会降低到 15% 以下，则直接击杀目标
            if (HasSkill("极限撕裂") && targetCore.currentHealth > 0)
            {
                if (targetCore.currentHealth < (targetCore.maxHealth * 0.15f))
                {
                    targetCore.currentHealth = 0;
                    Debug.Log($"[技能-极限撕裂] 目标血量低于 15%，触发处决！");
                }
            }

            // 检查目标是否死亡
            if (targetCore.currentHealth <= 0)
            {
                yield return StartCoroutine(HandleTargetDeath(targetCore));
                break; // 结束战斗循环
            }

            // --- 2. 目标反击 ---
            yield return new WaitForSeconds(currentRetalDelay);
            
            if (targetCombat != null)
            {                Vector3 targetStartPos = targetCore.transform.position;
                // 反击动画：目标冲向当前实体
                yield return StartCoroutine(targetCombat.MoveTo(transform.position, currentAttackDur));

                // 伤害结算：自身扣血
                int rawIncomingDamage = targetCore.attack;
                
                // [技能] 迎难而上 (skill_RiseAgainst)：若自身攻击力低于对方，自身受到伤害减少 10%
                if (HasSkill("迎难而上") && Core.attack < targetCore.attack)
                {                    rawIncomingDamage = Mathf.RoundToInt(rawIncomingDamage * 0.9f);
                }

                int counterDamage = Mathf.Max(1, rawIncomingDamage - Core.defense);

                // [技能] 顽强不屈 (skill_RenaciousWill)：单次受到伤害不超过自身基础血量的 10%
                if (HasSkill("顽强不屈"))
                {                    int maxDamageAllowed = Mathf.RoundToInt(baseMaxHPAtCombatStart * 0.1f);
                    if (counterDamage > maxDamageAllowed)
                    {                        counterDamage = maxDamageAllowed;
                        Debug.Log($"[技能-顽强不屈] 减免了过高伤害！最终承受: {counterDamage}");
                    }
                }

                Core.currentHealth -= counterDamage;
                Debug.Log($"[{targetCore.entityName}] 反击了 [{Core.entityName}]，造成 {counterDamage} 点伤害，自身剩余血量: {Core.currentHealth}");

                // 视觉反馈：自身闪红
                StartCoroutine(FlashHitEffect());

                // 目标弹回原位
                yield return StartCoroutine(targetCombat.MoveTo(targetStartPos, currentAttackDur));
            }

            // 检查自身是否死亡
            if (Core.currentHealth <= 0)
            {                Debug.Log($"[{Core.entityName}] 已死亡，正在移除玩家卡牌。");
                yield return StartCoroutine(RemoveCardWithDeathEffect(gameObject));
                break; // 结束战斗循环
            }

            // 每轮战斗结束后的短暂停歇，并加速下一回合
            yield return new WaitForSeconds(currentRetalDelay);
            
            // 加速：减少动画时长和延迟
            currentAttackDur = Mathf.Max(minAttackDuration, currentAttackDur * speedMultiplierPerTurn);
            currentRetalDelay = Mathf.Max(minRetaliationDelay, currentRetalDelay * speedMultiplierPerTurn);
        }

        // 战斗结束后恢复临时属性
        Core.attack = baseAttackAtCombatStart;
        Core.defense = baseDefenseAtCombatStart;

        isCombatInProgress = false;
    }

    /// <summary>
    /// 处理目标死亡后的逻辑（奖励结算、动画、销毁）
    /// </summary>
    private IEnumerator HandleTargetDeath(EntityCore targetCore)
    {        Debug.Log($"[{targetCore.entityName}] 已死亡，正在移除卡牌。");
        
        // 击杀奖励：将敌人身上的 gold 增加给玩家
        if (Core.type == EntityType.Player)
        {            int finalGoldReward = targetCore.gold;

            // [技能] 贪婪之刃 (skill_GreedBlade)：击败目标后额外掉落 10% 的金币
            if (HasSkill("贪婪之刃"))
            {                int bonusGold = Mathf.RoundToInt(finalGoldReward * 0.1f);
                finalGoldReward += bonusGold;
                Debug.Log($"[技能-贪婪之刃] 额外获得了 {bonusGold} 金币！");
            }

            Core.gold += finalGoldReward;
            Debug.Log($"[Combat] 击杀奖励：玩家获得了 {finalGoldReward} 金钱！当前总金钱: {Core.gold}");
            
            // 刷新 UI
            if (UIManager.instance != null) UIManager.instance.UpdateGoldUI();
        }
        
        yield return StartCoroutine(RemoveCardWithDeathEffect(targetCore.gameObject));
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
        PlayHandDrawnHitEffect();

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

    private void PlayHandDrawnHitEffect()
    {
        if (handDrawnHitEffectTrigger == null)
        {
            TryGetComponent(out handDrawnHitEffectTrigger);
        }

        if (handDrawnHitEffectTrigger != null)
        {
            handDrawnHitEffectTrigger.PlayHitEffect();
        }
    }
}
