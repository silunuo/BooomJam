// ============================================================================
// ImpactEffectPlayer.cs — 受击和爆炸特效播放入口
//
// 功能：
//   1. 在战斗逻辑里生成受击特效和爆炸特效
//   2. 统一处理 prefab 未绑定时的提示
// ============================================================================

using UnityEngine;

namespace BooomJam.Effects
{
    /// <summary>
    /// 挂在场景对象上的特效播放入口。
    /// 战斗代码可以通过它在指定位置生成受击或爆炸 prefab。
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
                Debug.LogWarning("未发现perfab");
                return null;
            }

            return Instantiate(prefab, position, rotation);
        }
    }
}
