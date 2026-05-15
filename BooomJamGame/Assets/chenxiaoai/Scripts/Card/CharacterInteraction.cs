using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 挂载在 Cube (角色) 上，处理悬停高亮和点击触发 Timeline 动画。
/// </summary>
public class CharacterInteraction : MonoBehaviour
{
    [Header("Timeline Settings")]
    [Tooltip("要播放的 Timeline 组件")]
    public PlayableDirector director;

    [Header("Highlight Settings")]
    [Tooltip("Shader 中控制高亮强度的属性名称 (建议使用 _EmissionStrength 或 _EmissionColor)")]
    public string emissionProperty = "_EmissionStrength";
    [Tooltip("普通状态下的发光强度")]
    public float normalIntensity = 0f;
    [Tooltip("悬停状态下的发光强度")]
    public float hoverIntensity = 1.5f;
    [Tooltip("变化平滑速度")]
    public float lerpSpeed = 10f;

    private Material targetMaterial;
    private float currentIntensity;
    private float targetIntensity;
    private bool isHovering = false;
    private bool isPlayingTimeline = false;

    private void Start()
    {
        // 获取渲染器上的材质
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            targetMaterial = renderer.material;
            currentIntensity = normalIntensity;
            targetIntensity = normalIntensity;
        }

        // 绑定 Timeline 结束事件
        if (director != null)
        {
            director.stopped += OnTimelineStopped;
        }
    }

    private void Update()
    {
        if (targetMaterial == null) return;

        // 平滑过渡发光强度
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * lerpSpeed);
        
        // 如果是颜色属性，可以用 Color.white * currentIntensity
        // 这里假设是一个 float 属性 (如 Shader Graph 中定义的)
        targetMaterial.SetFloat(emissionProperty, currentIntensity);
    }

    private void OnMouseEnter()
    {
        if (isPlayingTimeline || UIManager.IsBlocking3DScene) return;
        
        isHovering = true;
        targetIntensity = hoverIntensity;
    }

    private void OnMouseExit()
    {
        if (UIManager.IsBlocking3DScene)
        {
            isHovering = false;
            return;
        }

        isHovering = false;
        targetIntensity = normalIntensity;
    }

    private void OnMouseDown()
    {
        if (director != null && !isPlayingTimeline && !UIManager.IsBlocking3DScene)
        {
            PlayInteractionTimeline();
        }
    }

    private void PlayInteractionTimeline()
    {
        Debug.Log($"{gameObject.name}: 开始播放 Timeline 动画");
        isPlayingTimeline = true;
        targetIntensity = normalIntensity; // 播放时取消高亮
        
        if (director != null)
        {
            director.Play();
        }
    }

    private void OnTimelineStopped(PlayableDirector obj)
    {
        Debug.Log($"{gameObject.name}: Timeline 播放结束");
        isPlayingTimeline = false;
        
        // 如果鼠标还在上面，恢复高亮
        if (isHovering)
        {
            targetIntensity = hoverIntensity;
        }
    }

    private void OnDestroy()
    {
        // 记得解绑事件防止内存泄漏
        if (director != null)
        {
            director.stopped -= OnTimelineStopped;
        }
    }
}
