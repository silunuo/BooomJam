using UnityEngine;

public class Test111 : ModuleBase
{
    public Vector3 targetPosition;
    float t = 0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public override void OnModuleLoad(EntityCore entity)
    {
        // 初始化逻辑，比如获取组件
        Debug.Log("测试111模块已上线！");
    }
    public override void OnModuleUnload()
    {
        // 清理逻辑，比如停下脚步
        Debug.Log("测试111模块已下线。");
    }
    public override void OnModuleTick()
    {
        t += Time.deltaTime;
        if (t >= 1f)
        {
            transform.position = targetPosition;
            t = 0f;
        }
    }
}
