// ============================================================================
// CardHandDrawnHitEffectTrigger.cs — 卡牌受击手绘贴片触发器
//
// 功能：
//   1. 挂在玩家和敌人卡牌 prefab 上，等待 CombatModule 调用
//   2. 受击时在卡牌正上方生成手绘贴片 prefab
//   3. 不修改卡牌 MeshRenderer 的材质槽，避免和卡牌自身 Shader 冲突
// ============================================================================

using UnityEngine;

/// <summary>
/// 卡牌受击时的手绘贴片触发器。
/// 组件只生成独立特效 prefab，不替换卡牌材质。
/// </summary>
public sealed class CardHandDrawnHitEffectTrigger : ModuleBase
{
    // ================================================================
    //  Inspector 配置
    // ================================================================

    [Header("=== 手绘受击 Prefab ===")]
    [Tooltip("受击时生成的手绘纸板贴片 prefab，默认使用 PF_HandDrawnImpact_Cardboard")]
    [SerializeField] private GameObject m_HandDrawnHitPrefab = null;

    [Header("=== 生成位置 ===")]
    [Tooltip("指定受击贴片的生成锚点。为空时使用卡牌 Renderer 顶面中心")]
    [SerializeField] private Transform m_HitAnchor = null;

    [Tooltip("从卡牌顶面继续向上抬起的距离。卡牌是 Cube 时建议略大于 0")]
    [SerializeField] private float m_SurfaceOffset = 0.08f;

    [Tooltip("受击贴片朝向的世界法线。当前平放卡牌默认使用 Vector3.up")]
    [SerializeField] private Vector3 m_SurfaceNormal = Vector3.up;

    [Tooltip("为空时是否在当前物体和子物体里自动查找 Renderer")]
    [SerializeField] private bool m_AutoFindRenderer = true;

    [Header("=== 调试 ===")]
    [Tooltip("缺少 prefab 或 Renderer 时是否打印提示")]
    [SerializeField] private bool m_LogWarnings = true;

    // ================================================================
    //  运行时状态
    // ================================================================

    private Renderer m_CardRenderer;

    // ================================================================
    //  模块生命周期
    // ================================================================

    public override void OnModuleLoad(EntityCore entity)
    {
        CacheRenderer();
    }

    public override void OnModuleTick()
    {
    }

    public override void OnModuleUnload()
    {
    }

    // ================================================================
    //  核心 API
    // ================================================================

    /// <summary>
    /// 播放一次手绘受击贴片。由 CombatModule.FlashHitEffect 调用。
    /// </summary>
    public void PlayHitEffect()
    {
        if (m_HandDrawnHitPrefab == null)
        {
            LogWarning("未绑定手绘受击 prefab。");
            return;
        }

        Vector3 normal = ResolveSurfaceNormal();
        Vector3 position = ResolveSpawnPosition(normal);
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, normal);
        Instantiate(m_HandDrawnHitPrefab, position, rotation);
    }

    // ================================================================
    //  内部辅助
    // ================================================================

    private void CacheRenderer()
    {
        if (!m_AutoFindRenderer)
        {
            return;
        }

        if (!TryGetComponent(out m_CardRenderer))
        {
            m_CardRenderer = GetComponentInChildren<Renderer>();
        }
    }

    private Vector3 ResolveSurfaceNormal()
    {
        if (m_SurfaceNormal.sqrMagnitude <= 0.0001f)
        {
            return Vector3.up;
        }

        return m_SurfaceNormal.normalized;
    }

    private Vector3 ResolveSpawnPosition(Vector3 normal)
    {
        if (m_HitAnchor != null)
        {
            return m_HitAnchor.position + normal * m_SurfaceOffset;
        }

        if (m_CardRenderer == null && m_AutoFindRenderer)
        {
            CacheRenderer();
        }

        if (m_CardRenderer == null)
        {
            LogWarning("未找到 Renderer，使用 Transform 位置作为受击贴片中心。");
            return transform.position + normal * m_SurfaceOffset;
        }

        Bounds bounds = m_CardRenderer.bounds;
        float projectedExtent =
            Mathf.Abs(normal.x) * bounds.extents.x +
            Mathf.Abs(normal.y) * bounds.extents.y +
            Mathf.Abs(normal.z) * bounds.extents.z;

        return bounds.center + normal * (projectedExtent + m_SurfaceOffset);
    }

    private void LogWarning(string message)
    {
        if (!m_LogWarnings)
        {
            return;
        }

        Debug.LogWarning($"[CardHandDrawnHitEffectTrigger] {message}", this);
    }
}
