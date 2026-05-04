// FireBreathModule.cs
using UnityEngine;

// 注意这里是继承 ModuleBase
public class FireBreathModule : ModuleBase
{
    public GameObject firePrefab;
    public float fireInterval = 2f;
    private float _timer;

    public override void OnModuleLoad(EntityCore entity)
    {
        _timer = 0;
        Debug.Log($"[{entity.gameObject.name}] 喷火模块装载完毕。");
    }

    public override void OnModuleTick()
    {
        _timer += Time.deltaTime;
        if (_timer >= fireInterval)
        {
            SpitFire();
            _timer = 0;
            Debug.Log("喷火了");
        }
    }

    public override void OnModuleUnload()
    {
        Debug.Log($"[{Core.gameObject.name}] 喷火模块卸载，停止喷火。");
    }

    private void SpitFire()
    {
        if (firePrefab != null)
        {
            Vector3 spawnPos = Core.transform.position + Core.transform.forward * 1.2f;
            Instantiate(firePrefab, spawnPos, Core.transform.rotation);
        }
    }
}