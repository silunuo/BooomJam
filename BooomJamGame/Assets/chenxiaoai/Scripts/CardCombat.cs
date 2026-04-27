using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 挂载在卡牌物体上的战斗脚本。
/// 如果 Tag 为 Player，则在鼠标松开时检测范围内的 Enemy 并触发战斗序列。
/// </summary>
public class CardCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    [Tooltip("检测敌人的范围")]
    public float detectionRange = 5f;
    [Tooltip("攻击位移动画持续时间")]
    public float attackDuration = 0.2f;
    [Tooltip("攻击前的落地等待时间")]
    public float landingDelay = 0.3f;
    [Tooltip("攻击后的受击高亮延迟")]
    public float highlightDelay = 0.2f;
    [Tooltip("反击触发延迟")]
    public float retaliationDelay = 0.4f;

    [Header("Highlight Settings")]
    [Tooltip("受击时的高亮发光强度")]
    public float hitEmissionIntensity = 3f;
    [Tooltip("高亮持续时间")]
    public float hitDuration = 0.3f;

    private CardController cardController;
    private Material cardMaterial;
    private Vector3 originalPosition;
    private bool isCombatInProgress = false;

    void Awake()
    {
        cardController = GetComponent<CardController>();
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            cardMaterial = renderer.material;
        }
    }

    void OnMouseUp()
    {
        // 只有 Player 标签的卡牌在松开鼠标时主动触发战斗检测
        if (gameObject.CompareTag("Player") && !isCombatInProgress)
        {
            TryStartCombat();
        }
    }

    private void TryStartCombat()
    {
        // 查找范围内最近的敌人
        GameObject targetEnemy = FindNearestEnemy();
        if (targetEnemy != null)
        {
            StartCoroutine(PerformCombatSequence(targetEnemy));
        }
    }

    private GameObject FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        GameObject nearest = null;
        float minDistance = detectionRange;

        foreach (GameObject enemy in enemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = enemy;
            }
        }
        return nearest;
    }

    private IEnumerator PerformCombatSequence(GameObject enemy)
    {
        isCombatInProgress = true;
        
        // 0. 等待卡牌落地（让 CardController 完成它的 OnMouseUp 落下动作）
        yield return new WaitForSeconds(landingDelay);
        
        // 获取敌人的相关组件
        CardCombat enemyCombat = enemy.GetComponent<CardCombat>();
        CardController enemyController = enemy.GetComponent<CardController>();
        
        if (enemyCombat == null)
        {
            Debug.LogWarning("目标物体没有挂载 CardCombat 脚本");
            isCombatInProgress = false;
            yield break;
        }

        // 1. 锁定双方的常规交互动画
        if (cardController != null) cardController.IsExternalAnimating = true;
        if (enemyController != null) enemyController.IsExternalAnimating = true;

        // 记录双方的起始位置（攻击前的回归点）
        Vector3 playerBasePos = transform.position;
        Vector3 enemyBasePos = enemy.transform.position;

        // 2. 主角攻击敌人并立即返回 (0.2s 去 + 0.2s 回)
        Vector3 dirToEnemy = (enemyBasePos - playerBasePos).normalized;
        Vector3 playerAttackPos = enemyBasePos - dirToEnemy * 0.6f;
        yield return StartCoroutine(MoveTo(transform, playerAttackPos, attackDuration));
        StartCoroutine(MoveTo(transform, playerBasePos, attackDuration)); // 立即开始返回

        // 3. 延迟后敌人高亮 (0.2s 延迟)
        yield return new WaitForSeconds(highlightDelay);
        StartCoroutine(enemyCombat.FlashHighlight());

        // 4. 敌人反击并立即返回 (延迟 0.4s 后触发)
        yield return new WaitForSeconds(retaliationDelay);
        
        Vector3 dirToPlayer = (playerBasePos - enemyBasePos).normalized;
        Vector3 enemyAttackPos = playerBasePos - dirToPlayer * 0.6f;
        yield return StartCoroutine(MoveTo(enemy.transform, enemyAttackPos, attackDuration));
        StartCoroutine(MoveTo(enemy.transform, enemyBasePos, attackDuration)); // 立即开始返回

        // 5. 延迟后主角高亮
        yield return new WaitForSeconds(highlightDelay);
        StartCoroutine(FlashHighlight());

        // 6. 战斗序列结束前的收尾等待
        yield return new WaitForSeconds(attackDuration + 0.1f);

        // 7. 解锁常规交互
        if (cardController != null) cardController.IsExternalAnimating = false;
        if (enemyController != null) enemyController.IsExternalAnimating = false;
        
        isCombatInProgress = false;
    }

    private IEnumerator MoveTo(Transform target, Vector3 endPos, float duration)
    {
        Vector3 startPos = target.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            target.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            yield return null;
        }
        target.position = endPos;
    }

    public IEnumerator FlashHighlight()
    {
        if (cardMaterial == null) yield break;

        string strengthProp = "_EmissionStrength";
        string colorProp = "_EmissionColor";

        float oldStrength = cardMaterial.GetFloat(strengthProp);
        Color oldColor = cardMaterial.GetColor(colorProp);

        cardMaterial.SetColor(colorProp, Color.red);
        cardMaterial.SetFloat(strengthProp, hitEmissionIntensity);

        float elapsed = 0f;
        while (elapsed < hitDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / hitDuration;
            cardMaterial.SetFloat(strengthProp, Mathf.Lerp(hitEmissionIntensity, oldStrength, t));
            cardMaterial.SetColor(colorProp, Color.Lerp(Color.red, oldColor, t));
            yield return null;
        }

        cardMaterial.SetFloat(strengthProp, oldStrength);
        cardMaterial.SetColor(colorProp, oldColor);
    }
}
