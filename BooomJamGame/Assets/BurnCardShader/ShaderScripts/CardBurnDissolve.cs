// ============================================================================
// CardBurnDissolve.cs
//
// 功能：
//   1. 卡牌死亡时临时替换为燃烧溶解材质
//   2. 继承原卡牌贴图，避免每张卡单独配一套燃烧材质
//   3. 推动 _Cutoff 参数播放溶解动画，播放后可销毁卡牌
//
// 使用：
//   1. 挂到卡牌物体上
//   2. 拖入 Renderer 和燃烧材质 Mat_CardBurn
//   3. 死亡结算处调用 Play() 或 StartCoroutine(PlayAndWait())
// ============================================================================

using System.Collections;
using UnityEngine;

/// <summary>
/// 控制卡牌死亡时的燃烧溶解表现。
/// </summary>
public class CardBurnDissolve : MonoBehaviour
{
    // ================================================================
    // Inspector 配置
    // ================================================================

    [Header("目标")]
    [Tooltip("要替换材质的 Renderer。留空时会自动取当前物体上的 Renderer")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("要替换的材质槽。普通单材质卡牌保持 0")]
    [SerializeField] private int materialIndex = 0;

    [Header("燃烧材质")]
    [Tooltip("燃烧溶解材质，例如 Assets/Shader/BurnCard/Mat_CardBurn")]
    [SerializeField] private Material burnMaterial;

    [Tooltip("燃烧材质接收卡面贴图的属性名")]
    [SerializeField] private string burnMainTextureProperty = "_MainTex";

    [Tooltip("从原材质里尝试读取卡面贴图的属性名。镭射卡当前使用 _Texture2D")]
    [SerializeField] private string[] sourceTextureProperties = { "_MainTex", "_Texture2D", "_BaseMap" };

    [Header("播放")]
    [Tooltip("溶解参数名")]
    [SerializeField] private string cutoffProperty = "_Cutoff";

    [Tooltip("溶解起点。常用 0")]
    [SerializeField] private float startCutoff = 0f;

    [Tooltip("溶解终点。常用 1")]
    [SerializeField] private float endCutoff = 1f;

    [Tooltip("燃烧溶解播放时长，单位秒")]
    [SerializeField] private float duration = 0.8f;

    [Tooltip("播放结束后是否销毁整张卡牌")]
    [SerializeField] private bool destroyAfterComplete = true;

    [Header("卡牌适配")]
    [Tooltip("关闭方向燃烧，避免当前 Cube 卡牌沿厚度方向溶解")]
    [SerializeField] private bool disableDirectionDissolve = true;

    [Tooltip("关闭旗帜顶点摆动，避免卡牌网格硬变形")]
    [SerializeField] private bool disableVertexAnimation = true;

    [SerializeField] private string dissolveTypeProperty = "_DissolveType";
    [SerializeField] private string dissolveKeyword = "_DISSOLVETYPE_ON";
    [SerializeField] private string weightXProperty = "_WeightX";
    [SerializeField] private string weightZProperty = "_WeightZ";
    [SerializeField] private string speedProperty = "_Speed";

    [Header("调试")]
    [Tooltip("缺配置时是否打印提示")]
    [SerializeField] private bool logWarnings = true;

    // ================================================================
    // 运行时状态
    // ================================================================

    /// <summary>
    /// 当前是否正在播放死亡燃烧动画。
    /// </summary>
    public bool IsPlaying { get; private set; }

    // ================================================================
    // 核心 API
    // ================================================================

    /// <summary>
    /// 播放死亡燃烧动画。适合从按钮、事件或不需要等待的死亡逻辑调用。
    /// </summary>
    public void Play()
    {
        if (IsPlaying) return;
        StartCoroutine(PlayRoutine());
    }

    /// <summary>
    /// 播放死亡燃烧动画并等待结束。适合 CombatModule 这类协程结算里调用。
    /// </summary>
    public IEnumerator PlayAndWait()
    {
        if (IsPlaying)
        {
            while (IsPlaying)
            {
                yield return null;
            }

            yield break;
        }

        yield return PlayRoutine();
    }

    // ================================================================
    // 内部流程
    // ================================================================

    private IEnumerator PlayRoutine()
    {
        IsPlaying = true;

        if (!TryResolveRenderer(out Renderer renderer))
        {
            FinishPlayback();
            yield break;
        }

        if (burnMaterial == null)
        {
            LogWarning("缺少 burnMaterial，无法播放燃烧溶解。");
            FinishPlayback();
            yield break;
        }

        Material[] materials = renderer.materials;
        if (materials.Length == 0)
        {
            LogWarning("Renderer 没有材质槽，无法替换燃烧材质。");
            FinishPlayback();
            yield break;
        }

        int slotIndex = Mathf.Clamp(materialIndex, 0, materials.Length - 1);
        Material sourceMaterial = materials[slotIndex];
        Material runtimeBurnMaterial = new Material(burnMaterial);

        CopySourceTexture(sourceMaterial, runtimeBurnMaterial);
        PrepareBurnMaterial(runtimeBurnMaterial);

        materials[slotIndex] = runtimeBurnMaterial;
        renderer.materials = materials;

        yield return AnimateCutoff(runtimeBurnMaterial);

        FinishPlayback();

        if (destroyAfterComplete)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator AnimateCutoff(Material runtimeBurnMaterial)
    {
        int cutoffId = Shader.PropertyToID(cutoffProperty);
        if (!runtimeBurnMaterial.HasProperty(cutoffId))
        {
            LogWarning($"燃烧材质缺少 {cutoffProperty} 参数。");
            yield break;
        }

        float safeDuration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;

        runtimeBurnMaterial.SetFloat(cutoffId, startCutoff);

        while (elapsed < safeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            runtimeBurnMaterial.SetFloat(cutoffId, Mathf.Lerp(startCutoff, endCutoff, t));
            yield return null;
        }

        runtimeBurnMaterial.SetFloat(cutoffId, endCutoff);
    }

    private bool TryResolveRenderer(out Renderer renderer)
    {
        renderer = targetRenderer;
        if (renderer == null)
        {
            renderer = GetComponent<Renderer>();
        }

        if (renderer != null) return true;

        LogWarning("当前物体上没有 Renderer，也没有手动指定 targetRenderer。");
        return false;
    }

    private void CopySourceTexture(Material sourceMaterial, Material runtimeBurnMaterial)
    {
        if (sourceMaterial == null) return;
        if (string.IsNullOrWhiteSpace(burnMainTextureProperty)) return;

        int targetTextureId = Shader.PropertyToID(burnMainTextureProperty);
        if (!runtimeBurnMaterial.HasProperty(targetTextureId)) return;

        Texture sourceTexture = FindSourceTexture(sourceMaterial);
        if (sourceTexture == null) return;

        runtimeBurnMaterial.SetTexture(targetTextureId, sourceTexture);
    }

    private Texture FindSourceTexture(Material sourceMaterial)
    {
        foreach (string propertyName in sourceTextureProperties)
        {
            if (string.IsNullOrWhiteSpace(propertyName)) continue;

            int propertyId = Shader.PropertyToID(propertyName);
            if (!sourceMaterial.HasProperty(propertyId)) continue;

            Texture texture = sourceMaterial.GetTexture(propertyId);
            if (texture != null) return texture;
        }

        return null;
    }

    private void PrepareBurnMaterial(Material runtimeBurnMaterial)
    {
        if (disableDirectionDissolve)
        {
            SetFloatIfExists(runtimeBurnMaterial, dissolveTypeProperty, 0f);

            if (!string.IsNullOrWhiteSpace(dissolveKeyword))
            {
                runtimeBurnMaterial.DisableKeyword(dissolveKeyword);
            }
        }

        if (!disableVertexAnimation) return;

        SetFloatIfExists(runtimeBurnMaterial, weightXProperty, 0f);
        SetFloatIfExists(runtimeBurnMaterial, weightZProperty, 0f);
        SetFloatIfExists(runtimeBurnMaterial, speedProperty, 0f);
    }

    private void SetFloatIfExists(Material material, string propertyName, float value)
    {
        if (string.IsNullOrWhiteSpace(propertyName)) return;

        int propertyId = Shader.PropertyToID(propertyName);
        if (material.HasProperty(propertyId))
        {
            material.SetFloat(propertyId, value);
        }
    }

    private void FinishPlayback()
    {
        IsPlaying = false;
    }

    private void LogWarning(string message)
    {
        if (!logWarnings) return;
        Debug.LogWarning($"[CardBurnDissolve] {message}", this);
    }
}
