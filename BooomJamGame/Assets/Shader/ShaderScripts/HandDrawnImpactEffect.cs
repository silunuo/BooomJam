// ============================================================================
// HandDrawnImpactEffect.cs — 手绘纸板受击贴片
//
// 功能：
//   1. 把手绘爆点图贴到命中平面上
//   2. 播放一个短促的缩放、闪白和淡出
//   3. 额外生成少量火星，补一点受击反馈
// ============================================================================

using UnityEngine;
using UnityEngine.Rendering;

namespace BooomJam.Effects
{
    /// <summary>
    /// 挂在手绘受击 prefab 上的播放组件。
    /// 它使用一张透明 PNG 作为主体，适合纸板、卡牌、平面单位上的卡通受击反馈。
    /// </summary>
    public sealed class HandDrawnImpactEffect : MonoBehaviour
    {
        // ================================================================
        //  Inspector 配置
        // ================================================================

        [Header("=== 手绘贴片 ===")]
        [Tooltip("手绘受击材质，默认使用 Mat_PaperImpact_Cardboard")]
        [SerializeField] private Material m_ImpactMaterial = null;

        [Tooltip("手绘爆点图。为空时会尝试读取材质的 _MainTex")]
        [SerializeField] private Texture m_ImpactTexture = null;

        [Tooltip("贴片高度，单位按世界坐标算。宽度会按贴图宽高比自动计算")]
        [SerializeField] private float m_BaseHeight = 1.25f;

        [Tooltip("沿本地 Y 轴抬起的距离，用于减少和目标平面的闪烁")]
        [SerializeField] private float m_SurfaceOffset = 0.02f;

        [Tooltip("启用时每次播放都会随机绕本地 Y 轴旋转")]
        [SerializeField] private bool m_RandomZRotation = true;

        [Header("=== 播放节奏 ===")]
        [Tooltip("启用时自动播放。适合 Instantiate 后立即生效")]
        [SerializeField] private bool m_PlayOnEnable = true;

        [Tooltip("播放完成后自动销毁整个实例")]
        [SerializeField] private bool m_AutoDestroy = true;

        [Tooltip("总持续时间，单位秒")]
        [SerializeField] private float m_Lifetime = 0.45f;

        [Tooltip("开头弹出的初始缩放")]
        [SerializeField] private float m_StartScale = 0.18f;

        [Tooltip("弹出后的最大缩放")]
        [SerializeField] private float m_PeakScale = 1.0f;

        [Tooltip("结束前保留的缩放")]
        [SerializeField] private float m_EndScale = 0.88f;

        [Tooltip("开始淡出的归一化时间，0 到 1")]
        [SerializeField] private float m_FadeStart = 0.58f;

        [Tooltip("开头闪黄白的持续时间，单位秒")]
        [SerializeField] private float m_FlashDuration = 0.14f;

        [Header("=== 少量火星 ===")]
        [Tooltip("火星材质，默认复用 Mat_GlowDot")]
        [SerializeField] private Material m_SparkMaterial = null;

        [Tooltip("火星数量。纸板受击建议保持很少")]
        [SerializeField] private int m_SparkCount = 8;

        [Tooltip("火星最小速度")]
        [SerializeField] private float m_SparkSpeedMin = 0.55f;

        [Tooltip("火星最大速度")]
        [SerializeField] private float m_SparkSpeedMax = 1.65f;

        [Tooltip("火星最小尺寸")]
        [SerializeField] private float m_SparkSizeMin = 0.035f;

        [Tooltip("火星最大尺寸")]
        [SerializeField] private float m_SparkSizeMax = 0.085f;

        [Tooltip("火星外侧颜色")]
        [SerializeField] private Color m_SparkOuterColor = new Color(1f, 0.14f, 0.04f, 0.95f);

        [Tooltip("火星中心颜色")]
        [SerializeField] private Color m_SparkInnerColor = new Color(1f, 0.9f, 0.18f, 1f);

        // ================================================================
        //  运行时状态
        // ================================================================

        private static readonly int s_MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int s_AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int s_FlashId = Shader.PropertyToID("_Flash");
        private static Mesh s_QuadMesh;

        private Transform m_GeneratedRoot;
        private Renderer m_ImpactRenderer;
        private MaterialPropertyBlock m_PropertyBlock;
        private float m_Elapsed;
        private bool m_IsPlaying;

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

        private void Update()
        {
            if (!m_IsPlaying)
            {
                return;
            }

            m_Elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(m_Elapsed / Mathf.Max(0.01f, m_Lifetime));
            ApplyAnimation(normalizedTime);

            if (m_Elapsed < m_Lifetime)
            {
                return;
            }

            m_IsPlaying = false;
            ApplyAnimation(1f);

            if (m_AutoDestroy)
            {
                Destroy(gameObject);
            }
        }

        // ================================================================
        //  核心 API
        // ================================================================

        /// <summary>
        /// 重新创建手绘贴片和火星，并从头播放。
        /// </summary>
        public void Play()
        {
            BuildEffect();

            if (m_GeneratedRoot != null)
            {
                m_GeneratedRoot.gameObject.SetActive(true);
            }

            m_Elapsed = 0f;
            m_IsPlaying = true;
            ApplyAnimation(0f);

            ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                systems[i].Play(true);
            }
        }

        /// <summary>
        /// Inspector 右上角组件菜单里的临时播放入口。
        /// </summary>
        [ContextMenu("调试/播放一次")]
        private void DebugPlayOnce()
        {
            Play();
        }

        // ================================================================
        //  构建流程
        // ================================================================

        private void BuildEffect()
        {
            ClearGeneratedRoot();

            GameObject root = new GameObject("__GeneratedHandDrawnImpact");
            m_GeneratedRoot = root.transform;
            m_GeneratedRoot.SetParent(transform, false);
            m_GeneratedRoot.localRotation = m_RandomZRotation ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) : Quaternion.identity;
            m_GeneratedRoot.gameObject.SetActive(false);

            CreateImpactQuad();
            CreateSparks();
        }

        private void ClearGeneratedRoot()
        {
            if (m_GeneratedRoot == null)
            {
                return;
            }

            ParticleSystem[] systems = m_GeneratedRoot.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (Application.isPlaying)
            {
                Destroy(m_GeneratedRoot.gameObject);
            }
            else
            {
                DestroyImmediate(m_GeneratedRoot.gameObject);
            }

            m_GeneratedRoot = null;
            m_ImpactRenderer = null;
        }

        private void CreateImpactQuad()
        {
            GameObject quad = new GameObject("PaperBurst");
            quad.transform.SetParent(m_GeneratedRoot, false);
            quad.transform.localPosition = new Vector3(0f, m_SurfaceOffset, 0f);
            quad.transform.localScale = new Vector3(m_BaseHeight * ResolveTextureAspect(), 1f, m_BaseHeight);

            MeshFilter filter = quad.AddComponent<MeshFilter>();
            filter.sharedMesh = GetQuadMesh();

            MeshRenderer renderer = quad.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m_ImpactMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

            m_ImpactRenderer = renderer;
            m_PropertyBlock ??= new MaterialPropertyBlock();
        }

        private void CreateSparks()
        {
            if (m_SparkMaterial == null || m_SparkCount <= 0)
            {
                return;
            }

            GameObject sparksObject = new GameObject("PaperSparks");
            sparksObject.transform.SetParent(m_GeneratedRoot, false);
            sparksObject.transform.localPosition = new Vector3(0f, m_SurfaceOffset + 0.01f, 0f);

            ParticleSystem sparks = sparksObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = sparks.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 0.2f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(m_SparkSpeedMin, m_SparkSpeedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(m_SparkSizeMin, m_SparkSizeMax);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(m_SparkOuterColor, m_SparkInnerColor);
            main.maxParticles = 24;

            ParticleSystem.EmissionModule emission = sparks.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0.03f, (short)Mathf.Clamp(m_SparkCount, 0, 24)) });

            ParticleSystem.ShapeModule shape = sparks.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.16f;
            shape.randomDirectionAmount = 0.35f;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = sparks.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(m_SparkInnerColor, 0f),
                    new GradientColorKey(m_SparkOuterColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystemRenderer renderer = sparksObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = m_SparkMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.lengthScale = 1.6f;
            renderer.velocityScale = 0.25f;
            renderer.sortingFudge = 0.25f;
        }

        // ================================================================
        //  播放动画
        // ================================================================

        private void ApplyAnimation(float normalizedTime)
        {
            if (m_GeneratedRoot != null)
            {
                float scale = EvaluateScale(normalizedTime);
                m_GeneratedRoot.localScale = new Vector3(scale, scale, scale);
            }

            if (m_ImpactRenderer == null)
            {
                return;
            }

            m_PropertyBlock ??= new MaterialPropertyBlock();
            m_ImpactRenderer.GetPropertyBlock(m_PropertyBlock);
            m_PropertyBlock.SetFloat(s_AlphaId, EvaluateAlpha(normalizedTime));
            m_PropertyBlock.SetFloat(s_FlashId, EvaluateFlash());

            if (m_ImpactTexture != null)
            {
                m_PropertyBlock.SetTexture(s_MainTexId, m_ImpactTexture);
            }

            m_ImpactRenderer.SetPropertyBlock(m_PropertyBlock);
        }

        private float EvaluateScale(float normalizedTime)
        {
            if (normalizedTime < 0.18f)
            {
                float popTime = Mathf.InverseLerp(0f, 0.18f, normalizedTime);
                return Mathf.Lerp(m_StartScale, m_PeakScale, Mathf.SmoothStep(0f, 1f, popTime));
            }

            float settleTime = Mathf.InverseLerp(0.18f, 1f, normalizedTime);
            return Mathf.Lerp(m_PeakScale, m_EndScale, Mathf.SmoothStep(0f, 1f, settleTime));
        }

        private float EvaluateAlpha(float normalizedTime)
        {
            float popIn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.06f, normalizedTime));
            float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(m_FadeStart, 1f, normalizedTime));
            return Mathf.Clamp01(popIn * fadeOut);
        }

        private float EvaluateFlash()
        {
            if (m_FlashDuration <= 0f)
            {
                return 0f;
            }

            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(m_Elapsed / m_FlashDuration));
        }

        // ================================================================
        //  工具
        // ================================================================

        private float ResolveTextureAspect()
        {
            Texture texture = m_ImpactTexture;
            if (texture == null && m_ImpactMaterial != null && m_ImpactMaterial.HasProperty(s_MainTexId))
            {
                texture = m_ImpactMaterial.GetTexture(s_MainTexId);
            }

            if (texture == null || texture.height <= 0)
            {
                return 1f;
            }

            return Mathf.Clamp((float)texture.width / texture.height, 0.25f, 4f);
        }

        private static Mesh GetQuadMesh()
        {
            if (s_QuadMesh != null)
            {
                return s_QuadMesh;
            }

            s_QuadMesh = new Mesh
            {
                name = "HandDrawnImpactQuad",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-0.5f, 0f, -0.5f),
                    new Vector3(0.5f, 0f, -0.5f),
                    new Vector3(-0.5f, 0f, 0.5f),
                    new Vector3(0.5f, 0f, 0.5f)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                },
                triangles = new[] { 0, 2, 1, 2, 3, 1 }
            };
            s_QuadMesh.RecalculateBounds();
            return s_QuadMesh;
        }
    }
}
