using UnityEngine;
using System.Collections.Generic;

// 实体类型枚举
public enum EntityType
{
    Enemy,
    Item,
    Player,
    Boss
}

[CreateAssetMenu(fileName = "EnemyDataTable", menuName = "CactusFish/EnemyDataTable")]
public class EnemyDataTable : ScriptableObject
{
    public List<EnemyData> enemies = new List<EnemyData>();
}

[System.Serializable]
public class EnemyData
{
    [Header("敌人ID")]
    public int id;
    [Header("敌人类型")]
    public EntityType type;
    [Header("敌人名称")]
    public string name;
    [Header("敌人资源名称")]
    public string resourceName;
    [Header("敌人生命值")]
    public int health;
    [Header("敌人攻击值")]                  
    public int attack;
    [Header("敌人防御值")]      
    public int defense;
    [Header("敌人金币值")]
    public int gold;
    [Header("敌人技能")]
    public List<string> skills = new List<string>();
}

// 用于挂载在预制体上的组件，存储敌人数据
public class EnemyDataComponent : MonoBehaviour
{
    [Header("敌人数据")]
    public EnemyData data = new EnemyData();
}