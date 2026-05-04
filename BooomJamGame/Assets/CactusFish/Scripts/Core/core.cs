// IModuleCore.cs
public interface IModuleCore
{
    //代码装载
    void OnModuleLoad(EntityCore entity);
    //使用
    void OnModuleTick();
    //代码卸载
    void OnModuleUnload();
}