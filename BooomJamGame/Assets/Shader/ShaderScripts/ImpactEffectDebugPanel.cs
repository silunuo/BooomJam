// ============================================================================
// ImpactEffectDebugPanel.cs — 特效运行时按钮调试面板
//
// 功能：
//   1. 在 Game 视图左上角显示调试按钮
//   2. 不依赖键盘焦点，点击按钮就能生成特效
//   3. 默认按 XZ 平面的卡牌法线生成手绘纸板受击
// ============================================================================

using UnityEngine;

namespace BooomJam.Effects
{
    /// <summary>
    /// 挂在场景空物体上的运行时调试面板。
    /// 适合中文 Unity 界面下快速验收特效 prefab 和脚本调用。
    /// </summary>
    public sealed class ImpactEffectDebugPanel : MonoBehaviour
    {
        // ================================================================
        //  Inspector 配置
        // ================================================================

        [Header("=== 面板 ===")]
        [Tooltip("是否在 Game 视图显示调试按钮")]
        [SerializeField] private bool m_ShowPanel = true;

        [Tooltip("调试面板在 Game 视图里的位置和大小")]
        [SerializeField] private Rect m_PanelRect = new Rect(16f, 16f, 190f, 160f);

        [Header("=== 播放入口 ===")]
        [Tooltip("场景中的 ImpactEffectPlayer。为空时会直接 Instantiate 下方 prefab")]
        [SerializeField] private ImpactEffectPlayer m_EffectPlayer = null;

        [Tooltip("金色受击 prefab。没有 Effect Player 时使用")]
        [SerializeField] private GameObject m_HitPrefab = null;

        [Tooltip("金色爆炸 prefab。没有 Effect Player 时使用")]
        [SerializeField] private GameObject m_ExplosionPrefab = null;

        [Tooltip("手绘纸板受击 prefab。没有 Effect Player 时使用")]
        [SerializeField] private GameObject m_HandDrawnImpactPrefab = null;

        [Header("=== 生成位置 ===")]
        [Tooltip("特效生成位置。为空时使用当前物体位置")]
        [SerializeField] private Transform m_TargetPoint = null;

        [Tooltip("基于目标点追加的世界坐标偏移")]
        [SerializeField] private Vector3 m_PositionOffset = Vector3.zero;

        [Tooltip("受击面的世界法线。当前卡牌平面通常用 Vector3.up")]
        [SerializeField] private Vector3 m_SurfaceNormal = Vector3.up;

        [Tooltip("不为空时优先使用该物体的 forward 作为受击面法线")]
        [SerializeField] private Transform m_DirectionSource = null;

        // ================================================================
        //  生命周期
        // ================================================================

        private void Awake()
        {
            if (m_EffectPlayer == null)
            {
                TryGetComponent(out m_EffectPlayer);
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || !m_ShowPanel)
            {
                return;
            }

            GUILayout.BeginArea(m_PanelRect, "特效调试", GUI.skin.window);

            if (GUILayout.Button("手绘纸板受击"))
            {
                PlayHandDrawnImpact();
            }

            if (GUILayout.Button("金色受击"))
            {
                PlayHit();
            }

            if (GUILayout.Button("金色爆炸"))
            {
                PlayExplosion();
            }

            GUILayout.Label("默认法线: World Up");
            GUILayout.EndArea();
        }

        // ================================================================
        //  按钮回调
        // ================================================================

        /// <summary>
        /// 点击按钮时播放手绘纸板受击。
        /// </summary>
        public void PlayHandDrawnImpact()
        {
            Vector3 position = ResolvePosition();
            Quaternion rotation = ResolvePlaneRotation();

            if (m_EffectPlayer != null)
            {
                m_EffectPlayer.PlayHandDrawnImpactOnPlane(position, ResolveSurfaceNormal());
                return;
            }

            SpawnDirect(m_HandDrawnImpactPrefab, position, rotation, "手绘纸板受击");
        }

        /// <summary>
        /// 点击按钮时播放金色受击。
        /// </summary>
        public void PlayHit()
        {
            Vector3 position = ResolvePosition();
            Quaternion rotation = ResolveLookRotation();

            if (m_EffectPlayer != null)
            {
                m_EffectPlayer.PlayHit(position, rotation);
                return;
            }

            SpawnDirect(m_HitPrefab, position, rotation, "金色受击");
        }

        /// <summary>
        /// 点击按钮时播放金色爆炸。
        /// </summary>
        public void PlayExplosion()
        {
            Vector3 position = ResolvePosition();
            Quaternion rotation = ResolveLookRotation();

            if (m_EffectPlayer != null)
            {
                m_EffectPlayer.PlayExplosion(position, rotation);
                return;
            }

            SpawnDirect(m_ExplosionPrefab, position, rotation, "金色爆炸");
        }

        // ================================================================
        //  内部辅助
        // ================================================================

        private Vector3 ResolvePosition()
        {
            Vector3 basePosition = m_TargetPoint != null ? m_TargetPoint.position : transform.position;
            return basePosition + m_PositionOffset;
        }

        private Quaternion ResolveLookRotation()
        {
            Vector3 normal = ResolveSurfaceNormal();
            if (normal.sqrMagnitude <= 0.0001f)
            {
                normal = Vector3.up;
            }

            return Quaternion.LookRotation(normal.normalized);
        }

        private Quaternion ResolvePlaneRotation()
        {
            Vector3 normal = ResolveSurfaceNormal();
            if (normal.sqrMagnitude <= 0.0001f)
            {
                normal = Vector3.up;
            }

            return Quaternion.FromToRotation(Vector3.up, normal.normalized);
        }

        private Vector3 ResolveSurfaceNormal()
        {
            return m_DirectionSource != null ? m_DirectionSource.forward : m_SurfaceNormal;
        }

        private static void SpawnDirect(GameObject prefab, Vector3 position, Quaternion rotation, string effectName)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[ImpactEffectDebugPanel] 未绑定 {effectName} prefab。");
                return;
            }

            Instantiate(prefab, position, rotation);
        }
    }
}
