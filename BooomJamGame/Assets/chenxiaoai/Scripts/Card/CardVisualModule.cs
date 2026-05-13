using UnityEngine;

/// <summary>
/// 控制卡牌的交互视觉效果，包括鼠标悬停时的平滑上升和 Shader 高亮效果。
/// 继承自 ModuleBase 以适配项目的模块化架构。
/// </summary>
public class CardVisualModule : ModuleBase
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

    [Header("Floating Settings")]
    [Tooltip("是否开启悬停时的微弱浮动效果")]
    public bool enableBobbing = false;
    public float bobAmount = 0.1f;
    public float bobSpeed = 2f;

    [Header("Interaction Settings")]
    [Tooltip("检测资源卡的范围")]
    public float resourceDetectionRadius = 0.5f;
    [Tooltip("检测敌人的范围")]
    public float combatDetectionRadius = 0.5f;
    [Tooltip("吸附到资源卡时的位置偏移")]
    public Vector3 snapOffset = new Vector3(0, 0, -0.4f);
    [Tooltip("吸附动画持续时间")]
    public float snapDuration = 0.3f;

    private Vector3 basePosition;
    private Vector3 targetPosition;
    private Material cardMaterial;
    private float currentEmission;
    private float targetEmission;
    private bool isHovering = false;
    private bool isDragging = false;
    private bool isExternalAnimating = false;

    [Header("Input Settings")]
    [Tooltip("长按判定时间 (秒)，超过此时间则判定为拖拽")]
    public float longPressThreshold = 0.1f;
    private float mouseDownTime;
    private bool potentialClick = false;

    private Plane dragPlane;
    private Vector3 dragOffset;

    public bool IsExternalAnimating
    {
        get => isExternalAnimating;
        set 
        {
            isExternalAnimating = value;
            // 当外部动画结束时，刷新基础位置，防止卡牌跳回移动前的位置
            if (!isExternalAnimating)
            {
                basePosition = transform.position;
                targetPosition = basePosition;
            }
        }
    }

    /// <summary>
    /// 手动强制刷新基础位置（用于战斗位置同步）
    /// </summary>
    public void SyncBasePosition()
    {
        basePosition = transform.position;
        targetPosition = basePosition;
    }

    public override void OnModuleLoad(EntityCore entity)
    {
        // 记录初始位置
        basePosition = transform.position;
        targetPosition = basePosition;

        // 获取渲染器上的材质
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // 使用 .material 会自动创建一个材质实例，防止修改材质球资源文件
            cardMaterial = renderer.material;
            currentEmission = normalEmission;
            targetEmission = normalEmission;
            cardMaterial.SetFloat(emissionStrengthProperty, currentEmission);
        }
        else
        {
            Debug.LogError($"CardVisualModule [{gameObject.name}]: 未能在物体上找到 Renderer 组件！");
        }
        
        Debug.Log($"[{entity.gameObject.name}] 卡牌视觉模块已装载。");
    }

    public override void OnModuleTick()
    {
        if (isExternalAnimating) return;

        // 处理长按逻辑
        if (potentialClick && !isDragging)
        {
            if (Time.time - mouseDownTime > longPressThreshold)
            {
                StartDragging();
            }
        }

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

    private void UpdateDraggingPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 pointOnPlane = ray.GetPoint(enter);
            // 拖拽时，目标位置始终保持在悬浮高度
            targetPosition = pointOnPlane + dragOffset;
            targetPosition.y = basePosition.y + hoverHeight;
        }
    }

    public override void OnModuleUnload()
    {
        Debug.Log($"[{gameObject.name}] 卡牌视觉模块已卸载。");
    }

    /// <summary>
    /// 当鼠标进入碰撞体范围时触发
    /// 注意：物体必须带有 Collider 组件
    /// </summary>
    private void OnMouseEnter()
    {
        if (isDragging) return;
        isHovering = true;
        targetPosition = basePosition + Vector3.up * hoverHeight;
        targetEmission = hoverEmission;
    }

    /// <summary>
    /// 当鼠标离开碰撞体范围时触发
    /// </summary>
    private void OnMouseExit()
    {
        isHovering = false;
        if (!isDragging)
        {
            targetPosition = basePosition;
            targetEmission = normalEmission;
        }
    }

    /// <summary>
    /// 鼠标按下
    /// </summary>
    private void OnMouseDown()
    {
        if (isExternalAnimating) return;
        
        mouseDownTime = Time.time;
        potentialClick = true;
    }

    private void StartDragging()
    {
        isDragging = true;
        potentialClick = false; // 既然已经开始拖拽，就不再是点击
        
        // 拖拽时保持高亮
        targetEmission = hoverEmission;
        
        // 创建一个位于悬浮高度的水平面，确保拖拽时卡牌高度一致
        dragPlane = new Plane(Vector3.up, new Vector3(transform.position.x, basePosition.y + hoverHeight, transform.position.z));

        // 计算鼠标点击点与卡牌中心的偏移
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float enter))
        {
            Vector3 hitPoint = ray.GetPoint(enter);
            dragOffset = transform.position - hitPoint;
        }

        // 拖拽时隐藏信息面板
        if (UIManager.instance != null) UIManager.instance.HideCardInfo();
    }

    /// <summary>
    /// 鼠标松开停止拖拽
    /// </summary>
    private void OnMouseUp()
    {
        if (potentialClick)
        {
            // 如果松开时还没达到长按阈值，判定为点击
            float pressDuration = Time.time - mouseDownTime;
            if (pressDuration <= longPressThreshold)
            {
                if (UIManager.instance != null) UIManager.instance.ShowCardInfo(Core);
            }
            potentialClick = false;
            return;
        }

        if (!isDragging) return;

        isDragging = false;

        // 只有 Player 类型的卡牌才触发交互检测
        if (Core != null && Core.type == EntityType.Player)
        {
            // 1. 优先检测敌人（触发战斗）
            Collider[] combatColliders = Physics.OverlapSphere(transform.position, combatDetectionRadius);
            foreach (var col in combatColliders)
            {
                if (col.gameObject == gameObject) continue;
                
                CombatModule targetCombat = col.GetComponent<CombatModule>();
                if (targetCombat != null)
                {
                    EntityCore targetCore = col.GetComponent<EntityCore>();
                    if (targetCore != null && targetCore.type == EntityType.Enemy)
                    {
                        CombatModule myCombat = GetComponent<CombatModule>();
                        if (myCombat != null)
                        {
                            // 这里不再传递 finalBasePos，因为 CombatModule 内部会计算新的正方向位置
                            StartCoroutine(myCombat.PerformCombatSequence(targetCore, basePosition));
                            // 注意：basePosition 的更新会在战斗开始时由 CombatModule 处理（视觉上）
                            // 为了同步逻辑位置，我们在战斗协程中处理它，或者在这里先简单同步
                            return;
                        }
                    }
                }
            }

            // 2. 其次检测资源卡（触发吸附）
            Collider[] resourceColliders = Physics.OverlapSphere(transform.position, resourceDetectionRadius);
            foreach (var col in resourceColliders)
            {
                if (col.gameObject == gameObject) continue;

                ResourceModule resource = col.GetComponent<ResourceModule>();
                if (resource != null)
                {
                    StartCoroutine(PerformResourceSnap(resource));
                    return;
                }
            }
        }
        
        // 如果没有触发任何交互，则正常落地
        basePosition = new Vector3(transform.position.x, basePosition.y, transform.position.z);
        targetPosition = basePosition;
        
        if (!isHovering)
        {
            targetEmission = normalEmission;
        }
    }

    private System.Collections.IEnumerator PerformResourceSnap(ResourceModule resource)
    {
        isExternalAnimating = true;
        isHovering = false;
        targetEmission = normalEmission;

        // 1. 计算吸附位置 (资源卡的下方偏移)
        Vector3 resourcePos = resource.transform.position;
        Vector3 snapPos = resourcePos + snapOffset;
        
        // 2. 平滑移动到吸附点
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / snapDuration;
            // 使用平滑插值
            transform.position = Vector3.Lerp(startPos, snapPos, t);
            yield return null;
        }
        transform.position = snapPos;

        // 3. 等待一小会儿，增强视觉上的“吸附感”
        yield return new WaitForSeconds(0.1f);

        // 4. 消耗资源
        resource.Consume(Core);

        // 5. 动画结束，更新玩家卡牌的基础位置
        basePosition = new Vector3(transform.position.x, basePosition.y, transform.position.z);
        targetPosition = basePosition;
        isExternalAnimating = false;
    }
}
