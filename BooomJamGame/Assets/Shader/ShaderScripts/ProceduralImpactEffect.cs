// ============================================================================
// ProceduralImpactEffect.cs — 运行时拼装受击和爆炸粒子层
//
// 功能：
//   1. 让特效 prefab 保持轻量，只保存配置和材质引用
//   2. 播放时生成粒子系统层级，方便快速调受击和爆炸效果
//   3. 播放结束后按配置自动销毁实例
// ============================================================================

using UnityEngine;

namespace BooomJam.Effects
{
    /// <summary>
    /// 挂在特效 prefab 上的运行时生成器。
    /// 根据配置生成受击或爆炸的粒子层，并在播放时统一启动。
    /// </summary>
    public sealed class ProceduralImpactEffect : MonoBehaviour
    {
        /// <summary>
        /// 特效类型。决定本组件生成受击层级还是爆炸层级。
        /// </summary>
        public enum EffectKind
        {
            /// <summary>
            /// 金色受击：尖刺片、亮点、短线。
            /// </summary>
            Hit,

            /// <summary>
            /// 金色爆炸：中心闪光、外扩尖刺、火星、烟尘。
            /// </summary>
            Explosion
        }

        // ================================================================
        //  Inspector 配置
        // ================================================================

        [Header("=== 特效类型 ===")]
        [Tooltip("选择要生成的特效类型。Hit 用于受击，Explosion 用于爆炸")]
        [SerializeField] private EffectKind m_EffectKind = EffectKind.Hit;

        [Header("=== 材质引用 ===")]
        [Tooltip("尖刺片材质，用于受击尖刺和爆炸外扩尖刺")]
        [SerializeField] private Material m_HitSlashMaterial = null;

        [Tooltip("亮点材质，用于火星、短线和小亮点")]
        [SerializeField] private Material m_GlowDotMaterial = null;

        [Tooltip("爆炸核心材质，用于中心闪光")]
        [SerializeField] private Material m_ExplosionCoreMaterial = null;

        [Tooltip("烟尘材质，用于爆炸后的低透明烟尘")]
        [SerializeField] private Material m_ExplosionSmokeMaterial = null;

        [Header("=== 播放控制 ===")]
        [Tooltip("启用时自动播放。用于把 prefab 直接拖进场景或 Instantiate 后立即生效")]
        [SerializeField] private bool m_PlayOnEnable = true;

        [Tooltip("播放完成后自动销毁整个特效实例")]
        [SerializeField] private bool m_AutoDestroy = true;

        [Tooltip("特效主体持续时间（秒）。自动销毁会在这个时间后多等 0.25 秒")]
        [SerializeField] private float m_Lifetime = 1.4f;

        // ================================================================
        //  运行时状态
        // ================================================================

        // 每次 Play 都会重建，避免重复播放时旧粒子层留在对象下面。
        private Transform m_GeneratedRoot;

        // ================================================================
        //  生命周期
        // ================================================================

        private void OnEnable()
        {
            if (!Application.isPlaying || !m_PlayOnEnable)
            {
                return;
            }

            Play();
        }

        // ================================================================
        //  核心 API
        // ================================================================

        /// <summary>
        /// 重新生成当前类型的粒子层，并立即播放所有子粒子系统。
        /// 可被外部代码手动调用，用于复用同一个特效实例。
        /// </summary>
        public void Play()
        {
            BuildEffect();

            if (m_GeneratedRoot != null)
            {
                m_GeneratedRoot.gameObject.SetActive(true);
            }

            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                systems[i].Play(true);
            }

            if (m_AutoDestroy)
            {
                Destroy(gameObject, m_Lifetime + 0.25f);
            }
        }

        // ================================================================
        //  构建流程
        // ================================================================

        /// <summary>
        /// 清理旧的生成层级，再按特效类型创建新的粒子系统。
        /// </summary>
        private void BuildEffect()
        {
            ClearGeneratedRoot();

            // 所有运行时创建的粒子系统都放在同一个根节点下，方便清理。
            GameObject root = new GameObject("__GeneratedEffect");
            m_GeneratedRoot = root.transform;
            m_GeneratedRoot.SetParent(transform, false);
            m_GeneratedRoot.gameObject.SetActive(false);

            if (m_EffectKind == EffectKind.Hit)
            {
                BuildHit();
                return;
            }

            BuildExplosion();
        }

        /// <summary>
        /// 清理上一次播放时创建的粒子层。
        /// </summary>
        private void ClearGeneratedRoot()
        {
            if (m_GeneratedRoot == null)
            {
                return;
            }

            Destroy(m_GeneratedRoot.gameObject);
            m_GeneratedRoot = null;
        }

        /// <summary>
        /// 创建受击效果的三层粒子：尖刺片、亮点、短线。
        /// </summary>
        private void BuildHit()
        {
            ParticleSystem slash = CreateSystem("SlashSpikes", m_HitSlashMaterial);
            ConfigureMain(slash, 0.24f, 0.36f, 0.65f, 1.25f, 0.25f, 0.75f, new Color(1f, 0.78f, 0.35f, 0.9f));
            ConfigureBurst(slash, 0f, 9);
            ConfigureCircleShape(slash, 0.12f);
            ConfigureStretchedRenderer(slash, m_HitSlashMaterial, 2.6f, 0.18f);
            ConfigureFade(slash, new Color(1f, 0.82f, 0.36f, 0.95f), new Color(1f, 0.82f, 0.36f, 0f));

            ParticleSystem sparks = CreateSystem("GlowDots", m_GlowDotMaterial);
            ConfigureMain(sparks, 0.25f, 0.48f, 0.9f, 2.2f, 0.04f, 0.12f, new Color(1f, 0.9f, 0.55f, 1f));
            ConfigureBurst(sparks, 0.02f, 14);
            ConfigureSphereShape(sparks, 0.08f);
            ConfigureBillboardRenderer(sparks, m_GlowDotMaterial);
            ConfigureFade(sparks, new Color(1f, 0.9f, 0.55f, 1f), new Color(1f, 0.55f, 0.2f, 0f));

            ParticleSystem streaks = CreateSystem("ShortStreaks", m_GlowDotMaterial);
            ConfigureMain(streaks, 0.18f, 0.34f, 1.4f, 3.0f, 0.05f, 0.09f, new Color(1f, 0.82f, 0.45f, 0.95f));
            ConfigureBurst(streaks, 0.01f, 8);
            ConfigureCircleShape(streaks, 0.06f);
            ConfigureStretchedRenderer(streaks, m_GlowDotMaterial, 2.8f, 0.35f);
            ConfigureFade(streaks, new Color(1f, 0.9f, 0.56f, 0.95f), new Color(1f, 0.55f, 0.2f, 0f));
        }

        /// <summary>
        /// 创建爆炸效果的四层粒子：中心闪光、外扩尖刺、火星、烟尘。
        /// </summary>
        private void BuildExplosion()
        {
            ParticleSystem core = CreateSystem("ExplosionCore", m_ExplosionCoreMaterial);
            ConfigureMain(core, 0.18f, 0.28f, 0.15f, 0.45f, 1.1f, 1.8f, new Color(1f, 0.82f, 0.34f, 0.95f));
            ConfigureBurst(core, 0f, 5);
            ConfigureSphereShape(core, 0.05f);
            ConfigureBillboardRenderer(core, m_ExplosionCoreMaterial);
            ConfigureFade(core, new Color(1f, 0.9f, 0.5f, 1f), new Color(1f, 0.5f, 0.18f, 0f));

            ParticleSystem spikes = CreateSystem("ExplosionSpikes", m_HitSlashMaterial);
            ConfigureMain(spikes, 0.28f, 0.42f, 1.8f, 3.4f, 0.75f, 1.55f, new Color(1f, 0.72f, 0.26f, 0.9f));
            ConfigureBurst(spikes, 0.01f, 18);
            ConfigureSphereShape(spikes, 0.08f);
            ConfigureStretchedRenderer(spikes, m_HitSlashMaterial, 3.2f, 0.22f);
            ConfigureFade(spikes, new Color(1f, 0.78f, 0.3f, 0.9f), new Color(1f, 0.45f, 0.12f, 0f));

            ParticleSystem sparks = CreateSystem("ExplosionSparks", m_GlowDotMaterial);
            ConfigureMain(sparks, 0.34f, 0.62f, 2.2f, 4.2f, 0.04f, 0.1f, new Color(1f, 0.88f, 0.5f, 1f));
            ConfigureBurst(sparks, 0.03f, 26);
            ConfigureSphereShape(sparks, 0.1f);
            ConfigureStretchedRenderer(sparks, m_GlowDotMaterial, 2.1f, 0.4f);
            ConfigureFade(sparks, new Color(1f, 0.92f, 0.6f, 1f), new Color(1f, 0.5f, 0.16f, 0f));

            ParticleSystem smoke = CreateSystem("SoftSmoke", m_ExplosionSmokeMaterial);
            ConfigureMain(smoke, 0.55f, 0.85f, 0.35f, 0.9f, 1.0f, 2.2f, new Color(0.72f, 0.62f, 0.42f, 0.28f));
            ConfigureBurst(smoke, 0.03f, 7);
            ConfigureSphereShape(smoke, 0.22f);
            ConfigureBillboardRenderer(smoke, m_ExplosionSmokeMaterial);
            ConfigureFade(smoke, new Color(0.72f, 0.62f, 0.42f, 0.28f), new Color(0.72f, 0.62f, 0.42f, 0f));
        }

        // ================================================================
        //  粒子系统配置
        // ================================================================

        /// <summary>
        /// 创建一个子粒子系统，并绑定指定材质。
        /// </summary>
        private ParticleSystem CreateSystem(string systemName, Material material)
        {
            GameObject child = new GameObject(systemName);
            child.transform.SetParent(m_GeneratedRoot, false);

            ParticleSystem particleSystem = child.AddComponent<ParticleSystem>();
            ParticleSystemRenderer renderer = child.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            return particleSystem;
        }

        /// <summary>
        /// 设置粒子的基础生命周期、速度、大小和颜色。
        /// </summary>
        private static void ConfigureMain(ParticleSystem particleSystem, float lifetimeMin, float lifetimeMax, float speedMin, float speedMax, float sizeMin, float sizeMax, Color color)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = lifetimeMax;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeMin, lifetimeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = color;
            main.maxParticles = 64;
        }

        /// <summary>
        /// 设置一次性 Burst，避免持续发射带来额外粒子。
        /// </summary>
        private static void ConfigureBurst(ParticleSystem particleSystem, float time, short count)
        {
            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(time, count) });
        }

        /// <summary>
        /// 设置圆形发射范围，适合平面受击扩散。
        /// </summary>
        private static void ConfigureCircleShape(ParticleSystem particleSystem, float radius)
        {
            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.arc = 360f;
            shape.randomDirectionAmount = 0.55f;
        }

        /// <summary>
        /// 设置球形发射范围，适合爆炸和立体火星。
        /// </summary>
        private static void ConfigureSphereShape(ParticleSystem particleSystem, float radius)
        {
            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;
            shape.randomDirectionAmount = 0.75f;
        }

        /// <summary>
        /// 设置普通 Billboard 渲染，适合亮点、中心闪光和烟尘。
        /// </summary>
        private static void ConfigureBillboardRenderer(ParticleSystem particleSystem, Material material)
        {
            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingFudge = 0.1f;
        }

        /// <summary>
        /// 设置速度拉伸渲染，适合尖刺片和短线。
        /// </summary>
        private static void ConfigureStretchedRenderer(ParticleSystem particleSystem, Material material, float lengthScale, float speedScale)
        {
            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.lengthScale = lengthScale;
            renderer.velocityScale = speedScale;
            renderer.sortingFudge = 0.2f;
        }

        /// <summary>
        /// 设置颜色和大小随生命周期变化，让粒子快速亮起后淡出。
        /// </summary>
        private static void ConfigureFade(ParticleSystem particleSystem, Color startColor, Color endColor)
        {
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(startColor, 0f),
                    new GradientColorKey(endColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(startColor.a, 0f),
                    new GradientAlphaKey(endColor.a, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve(
                new Keyframe(0f, 0.45f),
                new Keyframe(0.2f, 1f),
                new Keyframe(1f, 0.1f));
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
        }
    }
}
