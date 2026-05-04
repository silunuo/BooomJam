// MovementModule.cs
using UnityEngine;

public class MovementModule : ModuleBase
{
    public float speed = 5f;

    public override void OnModuleLoad(EntityCore entity)
    {
        // 初始化逻辑，比如获取组件
        Debug.Log("移动模块已上线，准备冲刺！");
    }

    public override void OnModuleTick()
    {
        // 具体移动逻辑
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }

    public override void OnModuleUnload()
    {
        // 清理逻辑，比如停下脚步
        Debug.Log("移动模块已下线，停止移动。");
    }
}