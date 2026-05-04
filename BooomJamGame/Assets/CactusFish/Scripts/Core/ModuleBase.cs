// ModuleBase.cs
using UnityEngine;

// 所有具体模块都继承这个基类，而不是直接继承接口
[RequireComponent(typeof(EntityCore))] // 确保这个物体上必须有核心
public abstract class ModuleBase : MonoBehaviour, IModuleCore
{
    protected EntityCore Core { get; private set; }

    // 当组件被启用（或被添加到物体上）时调用
    void OnEnable()
    {
        // 1. 找到身上的核心
        Core = GetComponent<EntityCore>();

        // 2. 如果找到了，立刻注册
        if (Core != null)
        {
            Core.RegisterModule(this, this);
        }
    }

    // 当组件被禁用（或被销毁/移除）时调用
    void OnDisable()
    {
        // 立刻注销
        if (Core != null)
        {
            Core.UnregisterModule(this, this);
        }
    }

    // --- 接口实现，留给子类去写具体逻辑 ---
    public abstract void OnModuleLoad(EntityCore entity);
    public abstract void OnModuleTick();
    public abstract void OnModuleUnload();
}