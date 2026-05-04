using UnityEngine;

public class Test112 : ModuleBase
{
    public float t = 0f;
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
        Debug.Log("测试112模块已上线！");
    }
    public override void OnModuleUnload()
    {
        // 清理逻辑，比如停下脚步
        Debug.Log("测试112模块已下线。");
    }
    public override void OnModuleTick()
    {
        t += Time.deltaTime;
        if (t >= 1f)
        {
            Debug.Log("测试112模块已执行一次！");
        }
    }
}
