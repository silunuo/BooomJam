// ============================================================================
// ImpactEffectPlayer.cs — 受击和爆炸特效播放入口
//
// 功能：
//   1. 在战斗逻辑里生成受击、爆炸和手绘纸板受击特效
//   2. 统一处理 prefab 未绑定时的提示
// ============================================================================

using UnityEngine;

namespace BooomJam.Effects
{
    /// <summary>
    /// 挂在场景对象上的特效播放入口。
    /// 战斗代码可以通过它在指定位置生成受击、爆炸或手绘纸板受击 prefab。
    /// </summary>
    public sealed class ImpactEffectPlayer : MonoBehaviour
    {
        // ================================================================
        //  Inspector 配置
        // ================================================================

        [Header("=== 特效 Prefab ===")]
        [Tooltip("受击特效 prefab。默认使用 PF_HitImpact_Gold")]
        [SerializeField] private GameObject m_HitPrefab = null;

        [Tooltip("爆炸特效 prefab。默认使用 PF_StylizedExplosion_Gold")]
        [SerializeField] private GameObject m_ExplosionPrefab = null;

        [Tooltip("手绘纸板受击 prefab。默认使用 PF_HandDrawnImpact_Cardboard")]
        [SerializeField] private GameObject m_HandDrawnImpactPrefab = null;

        // ================================================================
        //  核心 API
        // ================================================================

        /// <summary>
        /// 在指定位置播放受击特效。
        /// </summary>
        /// <param name="position">特效生成的世界坐标。</param>
        /// <param name="rotation">特效生成时的世界旋转。</param>
        /// <returns>生成出来的特效实例；prefab 未绑定时返回 null。</returns>
        public GameObject PlayHit(Vector3 position, Quaternion rotation)
        {
            return Spawn(m_HitPrefab, position, rotation);
        }

        /// <summary>
        /// 在指定位置播放爆炸特效。
        /// </summary>
        /// <param name="position">特效生成的世界坐标。</param>
        /// <param name="rotation">特效生成时的世界旋转。</param>
        /// <returns>生成出来的特效实例；prefab 未绑定时返回 null。</returns>
        public GameObject PlayExplosion(Vector3 position, Quaternion rotation)
        {
            return Spawn(m_ExplosionPrefab, position, rotation);
        }

        /// <summary>
        /// 在指定位置播放手绘纸板受击特效。
        /// </summary>
        /// <param name="position">特效生成的世界坐标。</param>
        /// <param name="rotation">特效生成时的世界旋转。手绘贴片使用本地 Y 轴作为平面法线。</param>
        /// <returns>生成出来的特效实例；prefab 未绑定时返回 null。</returns>
        public GameObject PlayHandDrawnImpact(Vector3 position, Quaternion rotation)
        {
            return Spawn(m_HandDrawnImpactPrefab, position, rotation);
        }

        /// <summary>
        /// 在指定平面法线方向播放手绘纸板受击特效。
        /// </summary>
        /// <param name="position">特效生成的世界坐标。</param>
        /// <param name="surfaceNormal">命中平面的世界法线。</param>
        /// <returns>生成出来的特效实例；prefab 未绑定时返回 null。</returns>
        public GameObject PlayHandDrawnImpactOnPlane(Vector3 position, Vector3 surfaceNormal)
        {
            return Spawn(m_HandDrawnImpactPrefab, position, BuildPlaneRotation(surfaceNormal));
        }

        /// <summary>
        /// PlayHandDrawnImpact 的同义入口，方便按 PaperImpact 资源名查找。
        /// </summary>
        /// <param name="position">特效生成的世界坐标。</param>
        /// <param name="rotation">特效生成时的世界旋转。</param>
        /// <returns>生成出来的特效实例；prefab 未绑定时返回 null。</returns>
        public GameObject PlayPaperImpact(Vector3 position, Quaternion rotation)
        {
            return PlayHandDrawnImpact(position, rotation);
        }

        /// <summary>
        /// PlayHandDrawnImpactOnPlane 的同义入口，方便按 PaperImpact 资源名查找。
        /// </summary>
        /// <param name="position">特效生成的世界坐标。</param>
        /// <param name="surfaceNormal">命中平面的世界法线。</param>
        /// <returns>生成出来的特效实例；prefab 未绑定时返回 null。</returns>
        public GameObject PlayPaperImpactOnPlane(Vector3 position, Vector3 surfaceNormal)
        {
            return PlayHandDrawnImpactOnPlane(position, surfaceNormal);
        }

        // ================================================================
        //  内部辅助
        // ================================================================

        /// <summary>
        /// 生成特效 prefab，并在 prefab 缺失时输出统一日志。
        /// </summary>
        private static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[ImpactEffectPlayer] 未绑定 prefab。");
                return null;
            }

            return Instantiate(prefab, position, rotation);
        }

        private static Quaternion BuildPlaneRotation(Vector3 surfaceNormal)
        {
            if (surfaceNormal.sqrMagnitude <= 0.0001f)
            {
                surfaceNormal = Vector3.up;
            }

            return Quaternion.FromToRotation(Vector3.up, surfaceNormal.normalized);
        }
    }
}
