using UnityEngine;

/// <summary>
/// 控制卡牌的交互效果，包括鼠标悬停时的平滑上升和 Shader 高亮效果。
/// </summary>
public class CardController : MonoBehaviour
{
    [Header("Hover Settings")]
    [Tooltip("鼠标悬停时卡牌上升的高度")]
    public float hoverHeight = 0.5f;
    [Tooltip("位移平滑速度")]
    public float moveSpeed = 10f;

    [Header("Highlight Settings")]
    [Tooltip("Shader 中控制高亮强度的属性名称")]
    public string emissionStrengthProperty = "_EmissionStrength";
    [Tooltip("普通状态下的发光强度")]
    public float normalEmission = 0f;
    [Tooltip("悬停状态下的发光强度")]
    public float hoverEmission = 1.5f;
    [Tooltip("高亮变化平滑速度")]
    public float highlightSpeed = 5f;

    [Header("Floating Animation")]
    [Tooltip("是否开启悬停时的微弱浮动效果")]
    public bool enableBobbing = true;
    public float bobAmount = 0.1f;
    public float bobSpeed = 2f;

    private Vector3 basePosition;
    private Vector3 targetPosition;
    private Material cardMaterial;
    private float currentEmission;
    private float targetEmission;
    private bool isHovering = false;
    private bool isDragging = false;
    private bool isExternalAnimating = false;

    public bool IsExternalAnimating
    {
        get => isExternalAnimating;
        set => isExternalAnimating = value;
    }

    private Plane dragPlane;

    void Start()
    {
        // 记录初始位置
        basePosition = transform.position;
        targetPosition = basePosition;

        // 获取渲染器上的材质
        // 使用 .material 会自动创建一个材质实例，防止修改材质球资源文件
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            cardMaterial = renderer.material;
            currentEmission = normalEmission;
            targetEmission = normalEmission;
            cardMaterial.SetFloat(emissionStrengthProperty, currentEmission);
        }
        else
        {
            Debug.LogError("CardController: 未能在物体上找到 Renderer 组件！");
        }
    }

    void Update()
    {
        if (isExternalAnimating) return;

        // 如果正在拖拽，实时更新目标位置
        if (isDragging)
        {
            UpdateDraggingPosition();
        }

        // 计算目标位置（包含基础位移）
        Vector3 finalTarget = targetPosition;

        // 如果开启了浮动且正在悬停且不在拖拽中，加入正弦波偏移
        if (enableBobbing && isHovering && !isDragging)
        {
            finalTarget += Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        }

        // 平滑移动位置
        transform.position = Vector3.Lerp(transform.position, finalTarget, Time.deltaTime * moveSpeed);

        // 平滑更新 Shader 属性
        if (cardMaterial != null)
        {
            currentEmission = Mathf.Lerp(currentEmission, targetEmission, Time.deltaTime * highlightSpeed);
            cardMaterial.SetFloat(emissionStrengthProperty, currentEmission);
        }
    }

    /// <summary>
    /// 当鼠标进入碰撞体范围时触发
    /// 注意：物体必须带有 Collider 组件
    /// </summary>
    void OnMouseEnter()
    {
        isHovering = true;
        if (!isDragging)
        {
            targetPosition = basePosition + Vector3.up * hoverHeight;
            targetEmission = hoverEmission;
        }
    }

    /// <summary>
    /// 当鼠标离开碰撞体范围时触发
    /// </summary>
    void OnMouseExit()
    {
        isHovering = false;
        if (!isDragging)
        {
            targetPosition = basePosition;
            targetEmission = normalEmission;
        }
    }

    /// <summary>
    /// 鼠标点击时，初始化拖拽平面
    /// </summary>
    void OnMouseDown()
    {
        isDragging = true;
        // 创建一个位于当前高度的水平面 (法线向上)
        dragPlane = new Plane(Vector3.up, new Vector3(0, basePosition.y, 0));
    }

    /// <summary>
    /// 鼠标拖拽时，更新基准位置
    /// </summary>
    void OnMouseDrag()
    {
        // 逻辑已移至 UpdateDraggingPosition，以便在 Update 中统一处理
    }

    void OnMouseUp()
    {
        isDragging = false;
        // 拖拽结束，强制回到基准高度并关闭高亮（即“自动落下”）
        targetPosition = basePosition;
        targetEmission = normalEmission;
    }

    /// <summary>
    /// 计算鼠标在水平面上的投影点并更新位置
    /// </summary>
    private void UpdateDraggingPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        float enter;
        if (dragPlane.Raycast(ray, out enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            // 更新基准位置（保持原始 Y 轴）
            basePosition = new Vector3(hitPoint.x, basePosition.y, hitPoint.z);
            // 拖拽时，目标位置即为基准位置（或者可以加上悬停高度）
            targetPosition = basePosition + Vector3.up * hoverHeight;
        }
    }

    private void OnDestroy()
    {
        // 良好的习惯：清理动态生成的材质实例
        if (cardMaterial != null)
        {
            Destroy(cardMaterial);
        }
    }
}
