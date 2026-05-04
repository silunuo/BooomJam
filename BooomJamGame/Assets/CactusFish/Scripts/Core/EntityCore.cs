// EntityCore.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EntityCore : MonoBehaviour
{
    [Header("设置")]
    public bool showDebugLogs = true;

    [Header("个体数据")]
    public int id;
    public EntityType type = EntityType.Enemy;
    public string entityName;
    public int maxHealth = 100;
    public int currentHealth = 100;
    public int attack = 10;
    public int defense = 5;
    public int gold = 10;
    public List<string> skills = new List<string>();

    // 存储当前激活的模块
    private Dictionary<IModuleCore, MonoBehaviour> _moduleMap = new Dictionary<IModuleCore, MonoBehaviour>();

    void Update()
    {
        // 施行：只执行当前活着的模块
        // 注意：我们用 ToList() 复制一份，防止在 Tick 里自己把自己移除导致报错
        foreach (var module in _moduleMap.Keys.ToList())
        {
            // 双重保险：如果脚本被销毁了但没来得及通知，这里跳过
            if (_moduleMap[module] != null && _moduleMap[module].enabled)
            {
                module.OnModuleTick();
            }
        }
    }

    // --- 公共 API：供模块自动调用 ---

    public void RegisterModule(IModuleCore module, MonoBehaviour mono)
    {
        if (!_moduleMap.ContainsKey(module))
        {
            _moduleMap.Add(module, mono);
            module.OnModuleLoad(this);

            if (showDebugLogs) Debug.Log($"[{gameObject.name}] 热插拔：装载模块 [{mono.GetType().Name}]");
        }
    }

    public void UnregisterModule(IModuleCore module, MonoBehaviour mono)
    {
        if (_moduleMap.ContainsKey(module))
        {
            module.OnModuleUnload();
            _moduleMap.Remove(module);

            if (showDebugLogs) Debug.Log($"[{gameObject.name}] 热插拔：卸载模块 [{mono.GetType().Name}]");
        }
    }

    // --- 辅助工具：如果你想在编辑器里手动刷新一下 ---
    [ContextMenu("手动扫描所有模块")]
    public void ManualScanAllModules()
    {
        var allModules = GetComponents<IModuleCore>();
        foreach (var module in allModules)
        {
            // 如果还没注册，就注册一下（Register内部会判断重复，所以直接调没问题）
            RegisterModule(module, module as MonoBehaviour);
        }
    }

    // --- 数据查找 ---

    public EnemyData FindDataById(int targetId)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:EnemyDataTable");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            EnemyDataTable dataTable = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyDataTable>(path);
            if (dataTable != null)
            {
                return dataTable.enemies.Find(e => e.id == targetId);
            }
        }
        return null;
    }

    public EnemyData FindDataByName(string targetName)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:EnemyDataTable");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            EnemyDataTable dataTable = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyDataTable>(path);
            if (dataTable != null)
            {
                return dataTable.enemies.Find(e => e.name == targetName);
            }
        }
        return null;
    }
}