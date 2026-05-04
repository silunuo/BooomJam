// PrefabMaker.cs
// 预制体管理器 - 用于创建和管理游戏预制体的编辑器工具
// 功能：创建新预制体、编辑现有预制体属性、同步数据到 EnemyDataTable
// 位置：必须放在 Editor 文件夹下

using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CactusFish.EditorTools
{
    public class PrefabMaker : EditorWindow
    {
        // ==================== 页面枚举 ====================
        private enum TabPage
        {
            ManagePrefabs,
            CreatePrefab
        }

        // ==================== 页面状态 ====================
        private TabPage _currentPage = TabPage.ManagePrefabs;

        // ==================== 管理页面字段 ====================
        private List<GameObject> _loadedPrefabs;
        private int _selectedPrefabIndex;
        private Vector2 _prefabListScrollPos;
        private Vector2 _propertyScrollPos;
        private List<GameObject> _pendingDeletePrefabs;
        private List<string> _pendingRemoveSkills;
        private List<string> _pendingAddSkills;
        private Dictionary<string, Dictionary<string, object>> _pendingSkillData;
        private string _loadPath;

        // ==================== 创建页面字段 ====================
        private GameObject _baseTemplate;
        private GameObject _previewObject;
        private List<Type> _allModuleTypes;
        private string _savePath;
        private string _prefabName;
        private EntityType _createType;
        private int _createHealth;
        private int _createAttack;
        private int _createDefense;
        private int _createGold;
        private string _tableSavePath;
        private List<string> _createPendingAddSkills;
        private Dictionary<string, Dictionary<string, object>> _createPendingSkillData;
        private Vector2 _createPageScrollPos;

        // ==================== 数据表引用 ====================
        private EnemyDataTable _enemyDataTable;

        // ==================== 生命周期 ====================
        [MenuItem("Tools/CactusFish/预制体管理器")]
        public static void ShowWindow() => GetWindow<PrefabMaker>("预制体管理器");

        void OnEnable()
        {
            InitializeFields();
            RefreshData();
        }

        void OnDisable()
        {
            CleanupPreviewObject();
        }

        void OnGUI()
        {
            DrawTabBar();
            switch (_currentPage)
            {
                case TabPage.ManagePrefabs: DrawManagePrefabsPage(); break;
                case TabPage.CreatePrefab: DrawCreatePrefabPage(); break;
            }
        }

        // ==================== 初始化 ====================
        private void InitializeFields()
        {
            _loadedPrefabs = new List<GameObject>();
            _selectedPrefabIndex = -1;
            _pendingDeletePrefabs = new List<GameObject>();
            _pendingRemoveSkills = new List<string>();
            _pendingAddSkills = new List<string>();
            _pendingSkillData = new Dictionary<string, Dictionary<string, object>>();
            _loadPath = "Assets/CactusFish/Resources/Prefabs";

            _allModuleTypes = new List<Type>();
            _savePath = "Assets/CactusFish/Resources/Prefabs";
            _prefabName = "NewEnemy";
            _createType = EntityType.Enemy;
            _createHealth = 100;
            _createAttack = 10;
            _createDefense = 5;
            _createGold = 10;
            _tableSavePath = "Assets/CactusFish/Resources/SO";
            _createPendingAddSkills = new List<string>();
            _createPendingSkillData = new Dictionary<string, Dictionary<string, object>>();
            _createPageScrollPos = Vector2.zero;
        }

        private void RefreshData()
        {
            RefreshPrefabList();
            RefreshModuleList();
            LoadEnemyDataTable();
        }

        private void CleanupPreviewObject()
        {
            if (_previewObject != null)
            {
                DestroyImmediate(_previewObject);
                _previewObject = null;
            }
        }

        // ==================== 标签栏 ====================
        private void DrawTabBar()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Toggle(_currentPage == TabPage.ManagePrefabs, "管理预制体", EditorStyles.toolbarButton))
                _currentPage = TabPage.ManagePrefabs;
            if (GUILayout.Toggle(_currentPage == TabPage.CreatePrefab, "创建预制体", EditorStyles.toolbarButton))
                _currentPage = TabPage.CreatePrefab;
            GUILayout.EndHorizontal();
        }

        // ==================== 管理页面 ====================
        private void DrawManagePrefabsPage()
        {
            DrawPendingDeleteWarning();
            DrawPrefabList();
            DrawPropertyEditor();
        }

        private void DrawPendingDeleteWarning()
        {
            if (_pendingDeletePrefabs.Count <= 0) return;
            EditorGUILayout.HelpBox($"有 {_pendingDeletePrefabs.Count} 个预制体待删除", MessageType.Warning);
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("确认删除", GUILayout.Height(25))) ConfirmDelete();
            GUI.backgroundColor = Color.white;
            if (GUILayout.Button("取消删除", GUILayout.Height(25))) _pendingDeletePrefabs.Clear();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();
        }

        private void DrawPrefabList()
        {
            GUILayout.Label("预制体列表", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新列表")) RefreshPrefabList();
            _loadPath = EditorGUILayout.TextField("读取路径", _loadPath);
            GUILayout.EndHorizontal();
            _enemyDataTable = (EnemyDataTable)EditorGUILayout.ObjectField("数据表", _enemyDataTable, typeof(EnemyDataTable), false);

            _prefabListScrollPos = EditorGUILayout.BeginScrollView(_prefabListScrollPos, GUILayout.Height(150));
            for (int i = 0; i < _loadedPrefabs.Count; i++)
            {
                if (_pendingDeletePrefabs.Contains(_loadedPrefabs[i])) continue;
                if (GUILayout.Toggle(_selectedPrefabIndex == i, _loadedPrefabs[i].name, EditorStyles.toolbarButton))
                    _selectedPrefabIndex = i;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawPropertyEditor()
        {
            if (_selectedPrefabIndex < 0 || _selectedPrefabIndex >= _loadedPrefabs.Count)
            {
                EditorGUILayout.HelpBox("请选择一个预制体", MessageType.Info);
                return;
            }

            GameObject prefab = _loadedPrefabs[_selectedPrefabIndex];
            EntityCore core = prefab.GetComponent<EntityCore>();
            if (core == null)
            {
                EditorGUILayout.HelpBox("预制体没有 EntityCore 组件", MessageType.Warning);
                return;
            }

            _propertyScrollPos = EditorGUILayout.BeginScrollView(_propertyScrollPos);
            DrawBasicProperties(core, prefab);
            DrawSkillList(core);
            DrawSkillDataEditor(prefab);
            DrawAddSkillSection(core);
            DrawActionButtons(prefab);
            EditorGUILayout.EndScrollView();
        }

        private void DrawBasicProperties(EntityCore core, GameObject prefab)
        {
            GUILayout.Label("基础属性", EditorStyles.boldLabel);
            GUI.enabled = false;
            EditorGUILayout.IntField("ID", core.id);
            GUI.enabled = true;

            EditorGUI.BeginChangeCheck();
            core.entityName = EditorGUILayout.TextField("名称", core.entityName);
            core.type = (EntityType)EditorGUILayout.EnumPopup("类型", core.type);
            core.maxHealth = EditorGUILayout.IntField("生命值", core.maxHealth);
            core.attack = EditorGUILayout.IntField("攻击力", core.attack);
            core.defense = EditorGUILayout.IntField("防御力", core.defense);
            core.gold = EditorGUILayout.IntField("金币奖励", core.gold);
            if (EditorGUI.EndChangeCheck()) EditorUtility.SetDirty(prefab);
        }

        private void DrawSkillList(EntityCore core)
        {
            EditorGUILayout.Space();
            GUILayout.Label("技能列表", EditorStyles.boldLabel);
            foreach (string skill in new List<string>(core.skills))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(skill);
                string buttonText = _pendingRemoveSkills.Contains(skill) ? "取消" : "移除";
                if (GUILayout.Button(buttonText, GUILayout.Width(60)))
                    TogglePendingSkill(skill, _pendingRemoveSkills);
                GUILayout.EndHorizontal();
            }
            if (_pendingRemoveSkills.Count > 0)
                EditorGUILayout.HelpBox("待删除技能：" + string.Join(", ", _pendingRemoveSkills), MessageType.Warning);
        }

        private void DrawSkillDataEditor(GameObject prefab)
        {
            EditorGUILayout.Space();
            GUILayout.Label("技能数据编辑", EditorStyles.boldLabel);
            ModuleBase[] modules = prefab.GetComponents<ModuleBase>();
            if (modules == null) return;

            foreach (ModuleBase module in modules.Where(m => m != null))
            {
                string moduleName = module.GetType().Name;
                if (_pendingRemoveSkills.Contains(moduleName)) continue;

                GUILayout.BeginVertical("box");
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{moduleName} 数据", EditorStyles.boldLabel);
                if (GUILayout.Button("移除", GUILayout.Width(60)))
                    _pendingRemoveSkills.Add(moduleName);
                GUILayout.EndHorizontal();
                DrawModuleProperties(module, prefab);
                GUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawModuleProperties(ModuleBase module, GameObject targetObject)
        {
            SerializedObject serializedModule = new SerializedObject(module);
            SerializedProperty iterator = serializedModule.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script") continue;
                EditorGUILayout.PropertyField(iterator, true);
            }
            if (serializedModule.ApplyModifiedProperties())
                EditorUtility.SetDirty(targetObject);
        }

        private void DrawAddSkillSection(EntityCore core)
        {
            EditorGUILayout.Space();
            GUILayout.Label("添加技能", EditorStyles.boldLabel);
            if (_allModuleTypes.Count <= 0) return;

            GameObject prefab = _loadedPrefabs[_selectedPrefabIndex];
            ModuleBase[] existingModules = prefab.GetComponents<ModuleBase>();
            List<string> existingSkillNames = existingModules?.Where(m => m != null).Select(m => m.GetType().Name).ToList() ?? new List<string>();

            List<string> availableSkills = _allModuleTypes
                .Select(t => t.Name)
                .Where(name => !existingSkillNames.Contains(name) && !_pendingAddSkills.Contains(name) && !_pendingRemoveSkills.Contains(name))
                .ToList();

            if (availableSkills.Count > 0)
            {
                foreach (string skill in availableSkills)
                {
                    GUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(skill);
                    if (GUILayout.Button("添加", GUILayout.Width(60)))
                        _pendingAddSkills.Add(skill);
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("没有可添加的技能");
            }

            if (_pendingAddSkills.Count > 0)
            {
                EditorGUILayout.Space();
                GUILayout.Label("待添加技能：", EditorStyles.boldLabel);
                foreach (string skillName in _pendingAddSkills.ToArray())
                    DrawPendingSkillEditor(skillName, _pendingSkillData);
            }
        }

        private void DrawPendingSkillEditor(string skillName, Dictionary<string, Dictionary<string, object>> pendingData)
        {
            Type skillType = _allModuleTypes.FirstOrDefault(t => t.Name == skillName);
            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{skillName} 数据", EditorStyles.boldLabel);
            if (GUILayout.Button("取消", GUILayout.Width(60)))
            {
                _pendingAddSkills.Remove(skillName);
                if (pendingData.ContainsKey(skillName))
                    pendingData.Remove(skillName);
            }
            GUILayout.EndHorizontal();

            if (skillType != null && typeof(MonoBehaviour).IsAssignableFrom(skillType))
            {
                GameObject tempGO = new GameObject();
                MonoBehaviour tempModule = (MonoBehaviour)tempGO.AddComponent(skillType);

                if (pendingData.ContainsKey(skillName))
                {
                    SerializedObject tempSerialized = new SerializedObject(tempModule);
                    foreach (var kvp in pendingData[skillName])
                    {
                        SerializedProperty property = tempSerialized.FindProperty(kvp.Key);
                        if (property != null) SetPropertyValue(property, kvp.Value);
                    }
                    tempSerialized.ApplyModifiedProperties();
                }

                SerializedObject serializedModule = new SerializedObject(tempModule);
                SerializedProperty iterator = serializedModule.GetIterator();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.name == "m_Script") continue;
                    EditorGUILayout.PropertyField(iterator, true);
                }

                if (!pendingData.ContainsKey(skillName))
                    pendingData[skillName] = new Dictionary<string, object>();

                iterator = serializedModule.GetIterator();
                enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    enterChildren = false;
                    if (iterator.name == "m_Script") continue;
                    object value = GetPropertyValue(iterator);
                    if (value != null) pendingData[skillName][iterator.name] = value;
                }

                serializedModule.ApplyModifiedProperties();
                DestroyImmediate(tempGO);
            }
            GUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawActionButtons(GameObject prefab)
        {
            EditorGUILayout.Space();
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("删除预制体", GUILayout.Height(25)))
                if (!_pendingDeletePrefabs.Contains(prefab)) _pendingDeletePrefabs.Add(prefab);
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("保存修改", GUILayout.Height(35)))
                SavePrefabChanges(prefab, prefab.GetComponent<EntityCore>());
            GUI.backgroundColor = Color.white;
        }

        // ==================== 创建页面 ====================
        private void DrawCreatePrefabPage()
        {
            _createPageScrollPos = EditorGUILayout.BeginScrollView(_createPageScrollPos, false, true, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            DrawBaseSettings();

            if (_previewObject == null)
            {
                EditorGUILayout.HelpBox("请先选择一个基础模板", MessageType.Warning);
                DrawGenerateButton();
                EditorGUILayout.EndScrollView();
                return;
            }

            DrawDataTableField();
            DrawCreateValues();
            DrawCreateSkillEditor();
            DrawGenerateButton();
            EditorGUILayout.EndScrollView();
        }

        private void DrawBaseSettings()
        {
            GUILayout.Label("基础设置", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _baseTemplate = (GameObject)EditorGUILayout.ObjectField("基础模板", _baseTemplate, typeof(GameObject), false);
            _prefabName = EditorGUILayout.TextField("预制体名字", _prefabName);
            _savePath = EditorGUILayout.TextField("预制体保存路径", _savePath);
            _tableSavePath = EditorGUILayout.TextField("表保存路径", _tableSavePath);
            if (EditorGUI.EndChangeCheck()) RebuildPreviewObject();
        }

        private void DrawDataTableField()
        {
            EditorGUILayout.Space();
            GUILayout.Label("数据表", EditorStyles.boldLabel);
            _enemyDataTable = (EnemyDataTable)EditorGUILayout.ObjectField("EnemyDataTable", _enemyDataTable, typeof(EnemyDataTable), false);
        }

        private void DrawCreateValues()
        {
            EditorGUILayout.Space();
            GUILayout.Label("数值设置", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            _createType = (EntityType)EditorGUILayout.EnumPopup("类型", _createType);
            _createHealth = EditorGUILayout.IntField("生命值", _createHealth);
            _createAttack = EditorGUILayout.IntField("攻击力", _createAttack);
            _createDefense = EditorGUILayout.IntField("防御力", _createDefense);
            _createGold = EditorGUILayout.IntField("金币奖励", _createGold);
            if (EditorGUI.EndChangeCheck()) UpdatePreviewCoreData();
        }

        private void DrawCreateSkillEditor()
        {
            EditorGUILayout.Space();
            GUILayout.Label("技能数据编辑", EditorStyles.boldLabel);

            ModuleBase[] existingModules = _previewObject.GetComponents<ModuleBase>();
            List<string> existingSkillNames = existingModules?.Where(m => m != null).Select(m => m.GetType().Name).ToList() ?? new List<string>();

            if (_allModuleTypes.Count > 0)
            {
                List<string> availableSkills = _allModuleTypes
                    .Select(t => t.Name)
                    .Where(name => !existingSkillNames.Contains(name) && !_createPendingAddSkills.Contains(name))
                    .ToList();

                if (availableSkills.Count > 0)
                {
                    foreach (string skill in availableSkills)
                    {
                        GUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(skill);
                        if (GUILayout.Button("添加", GUILayout.Width(60)))
                            _createPendingAddSkills.Add(skill);
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("没有可添加的技能");
                }
            }

            if (_createPendingAddSkills.Count > 0)
            {
                EditorGUILayout.Space();
                GUILayout.Label("待添加技能：", EditorStyles.boldLabel);
                foreach (string skillName in _createPendingAddSkills.ToArray())
                {
                    Type skillType = _allModuleTypes.FirstOrDefault(t => t.Name == skillName);
                    GUILayout.BeginVertical("box");
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{skillName} 数据", EditorStyles.boldLabel);
                    if (GUILayout.Button("取消", GUILayout.Width(60)))
                    {
                        _createPendingAddSkills.Remove(skillName);
                        if (_createPendingSkillData.ContainsKey(skillName))
                            _createPendingSkillData.Remove(skillName);
                    }
                    GUILayout.EndHorizontal();

                    if (skillType != null && typeof(MonoBehaviour).IsAssignableFrom(skillType))
                    {
                        GameObject tempGO = new GameObject();
                        MonoBehaviour tempModule = (MonoBehaviour)tempGO.AddComponent(skillType);

                        if (_createPendingSkillData.ContainsKey(skillName))
                        {
                            SerializedObject tempSerialized = new SerializedObject(tempModule);
                            foreach (var kvp in _createPendingSkillData[skillName])
                            {
                                SerializedProperty property = tempSerialized.FindProperty(kvp.Key);
                                if (property != null) SetPropertyValue(property, kvp.Value);
                            }
                            tempSerialized.ApplyModifiedProperties();
                        }

                        SerializedObject serializedModule = new SerializedObject(tempModule);
                        SerializedProperty iterator = serializedModule.GetIterator();
                        bool enterChildren = true;
                        while (iterator.NextVisible(enterChildren))
                        {
                            enterChildren = false;
                            if (iterator.name == "m_Script") continue;
                            EditorGUILayout.PropertyField(iterator, true);
                        }

                        if (!_createPendingSkillData.ContainsKey(skillName))
                            _createPendingSkillData[skillName] = new Dictionary<string, object>();

                        iterator = serializedModule.GetIterator();
                        enterChildren = true;
                        while (iterator.NextVisible(enterChildren))
                        {
                            enterChildren = false;
                            if (iterator.name == "m_Script") continue;
                            object value = GetPropertyValue(iterator);
                            if (value != null) _createPendingSkillData[skillName][iterator.name] = value;
                        }

                        serializedModule.ApplyModifiedProperties();
                        DestroyImmediate(tempGO);
                    }
                    GUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }
        }

        private void DrawGenerateButton()
        {
            EditorGUILayout.Space();
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("生成预制体", GUILayout.Height(40))) GeneratePrefab();
            GUI.backgroundColor = Color.white;
        }

        // ==================== 预览对象 ====================
        private void RebuildPreviewObject()
        {
            CleanupPreviewObject();
            if (_baseTemplate != null)
            {
                _previewObject = Instantiate(_baseTemplate);
                _previewObject.name = "PreviewObject";
                _previewObject.hideFlags = HideFlags.HideAndDontSave;
                if (_previewObject.GetComponent<EntityCore>() == null)
                    _previewObject.AddComponent<EntityCore>();
                UpdatePreviewCoreData();
            }
        }

        private void UpdatePreviewCoreData(int id = -1, List<string> skills = null)
        {
            if (_previewObject == null) return;
            EntityCore core = _previewObject.GetComponent<EntityCore>();
            if (core != null)
            {
                if (id >= 0) core.id = id;
                core.type = _createType;
                core.entityName = _prefabName;
                core.maxHealth = _createHealth;
                core.currentHealth = _createHealth;
                core.attack = _createAttack;
                core.defense = _createDefense;
                core.gold = _createGold;
                if (skills != null) core.skills = new List<string>(skills);
            }
        }

        // ==================== 工具方法 ====================
        private void TogglePendingSkill(string skill, List<string> list)
        {
            if (list.Contains(skill)) list.Remove(skill);
            else list.Add(skill);
        }

        private object GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: return property.intValue;
                case SerializedPropertyType.Float: return property.floatValue;
                case SerializedPropertyType.Boolean: return property.boolValue;
                case SerializedPropertyType.String: return property.stringValue;
                case SerializedPropertyType.Vector2: return property.vector2Value;
                case SerializedPropertyType.Vector3: return property.vector3Value;
                case SerializedPropertyType.Vector4: return property.vector4Value;
                case SerializedPropertyType.Color: return property.colorValue;
                case SerializedPropertyType.Enum: return property.enumValueIndex;
                default: return null;
            }
        }

        private void SetPropertyValue(SerializedProperty property, object value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer: property.intValue = (int)value; break;
                case SerializedPropertyType.Float: property.floatValue = (float)value; break;
                case SerializedPropertyType.Boolean: property.boolValue = (bool)value; break;
                case SerializedPropertyType.String: property.stringValue = (string)value; break;
                case SerializedPropertyType.Vector2: property.vector2Value = (Vector2)value; break;
                case SerializedPropertyType.Vector3: property.vector3Value = (Vector3)value; break;
                case SerializedPropertyType.Vector4: property.vector4Value = (Vector4)value; break;
                case SerializedPropertyType.Color: property.colorValue = (Color)value; break;
                case SerializedPropertyType.Enum: property.enumValueIndex = (int)value; break;
            }
        }

        private string GetUniqueName(string baseName)
        {
            string uniqueName = baseName;
            int counter = 1;
            while (_loadedPrefabs.Any(p => p.name == uniqueName))
                uniqueName = $"{baseName} ({counter++})";
            if (_enemyDataTable != null)
                while (_enemyDataTable.enemies.Any(e => e.name == uniqueName))
                    uniqueName = $"{baseName} ({counter++})";
            return uniqueName;
        }

        private bool ValidateName(string name, int currentId = -1)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (_loadedPrefabs.Any(p => p.name == name && (currentId < 0 || p.GetComponent<EntityCore>()?.id != currentId)))
                return false;
            if (_enemyDataTable != null && _enemyDataTable.enemies.Any(e => e.name == name && e.id != currentId))
                return false;
            return true;
        }

        // ==================== 数据刷新 ====================
        private void LoadEnemyDataTable()
        {
            string[] guids = AssetDatabase.FindAssets("t:EnemyDataTable");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _enemyDataTable = AssetDatabase.LoadAssetAtPath<EnemyDataTable>(path);
            }
        }

        private void RefreshPrefabList()
        {
            _loadedPrefabs.Clear();
            if (string.IsNullOrEmpty(_loadPath))
            {
                _loadPath = "Assets/CactusFish/Resources/Prefabs";
            }
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { _loadPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) _loadedPrefabs.Add(prefab);
            }
        }

        private void RefreshModuleList()
        {
            _allModuleTypes.Clear();
            var baseType = typeof(ModuleBase);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && baseType.IsAssignableFrom(t) && !t.IsGenericType);
                    _allModuleTypes.AddRange(types);
                }
                catch (ReflectionTypeLoadException) { }
            }
        }

        // ==================== 保存操作 ====================
        private void SavePrefabChanges(GameObject prefab, EntityCore core)
        {
            if (string.IsNullOrEmpty(core.entityName))
            {
                ShowNotification(new GUIContent("错误：名称不能为空！"));
                return;
            }

            try
            {
                if (!ValidateName(core.entityName, core.id))
                {
                    string uniqueName = GetUniqueName(core.entityName);
                    ShowNotification(new GUIContent($"名称重复，已自动调整为：{uniqueName}"));
                    core.entityName = uniqueName;
                }

                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                if (prefab.name != core.entityName)
                {
                    AssetDatabase.RenameAsset(prefabPath, core.entityName);
                    prefabPath = AssetDatabase.GUIDToAssetPath(AssetDatabase.AssetPathToGUID(prefabPath));
                }
                prefab.name = core.entityName;

                ProcessPendingRemoveSkills(prefab, core);
                ProcessPendingAddSkills(prefab, core);

                EditorUtility.SetDirty(prefab);
                PrefabUtility.SavePrefabAsset(prefab);
                SyncToDataTable(prefab, core, prefabPath);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                RefreshPrefabList();
                ShowNotification(new GUIContent("保存成功！"));
            }
            catch (Exception e)
            {
                ShowNotification(new GUIContent($"保存失败：{e.Message}"));
            }
        }

        private void ProcessPendingRemoveSkills(GameObject prefab, EntityCore core)
        {
            foreach (string skill in _pendingRemoveSkills.ToArray())
            {
                core.skills.Remove(skill);
                Type skillType = _allModuleTypes.FirstOrDefault(t => t.Name == skill);
                if (skillType != null)
                {
                    Component comp = prefab.GetComponent(skillType);
                    if (comp != null) DestroyImmediate(comp, true);
                }
            }
            _pendingRemoveSkills.Clear();
        }

        private void ProcessPendingAddSkills(GameObject prefab, EntityCore core)
        {
            foreach (string skill in _pendingAddSkills.ToArray())
            {
                core.skills.Add(skill);
                Type skillType = _allModuleTypes.FirstOrDefault(t => t.Name == skill);
                if (skillType != null && prefab.GetComponent(skillType) == null)
                {
                    MonoBehaviour newModule = (MonoBehaviour)prefab.AddComponent(skillType);
                    ApplyPendingSkillData(newModule, skill, _pendingSkillData);
                }
            }
            _pendingAddSkills.Clear();
            _pendingSkillData.Clear();
        }

        private void ApplyPendingSkillData(MonoBehaviour module, string skillName, Dictionary<string, Dictionary<string, object>> pendingData)
        {
            if (!pendingData.ContainsKey(skillName)) return;
            SerializedObject serializedModule = new SerializedObject(module);
            foreach (var kvp in pendingData[skillName])
            {
                SerializedProperty property = serializedModule.FindProperty(kvp.Key);
                if (property != null) SetPropertyValue(property, kvp.Value);
            }
            serializedModule.ApplyModifiedProperties();
        }

        private void SyncToDataTable(GameObject prefab, EntityCore core, string prefabPath)
        {
            if (_enemyDataTable == null) return;
            EnemyData existingData = _enemyDataTable.enemies.Find(e => e.id == core.id);
            if (existingData != null)
            {
                existingData.type = core.type;
                existingData.name = core.entityName;
                existingData.resourceName = prefabPath;
                existingData.health = core.maxHealth;
                existingData.attack = core.attack;
                existingData.defense = core.defense;
                existingData.gold = core.gold;
                existingData.skills = new List<string>(core.skills);
            }
            else
            {
                Debug.LogWarning($"[PrefabMaker] 未找到 ID 为 {core.id} 的数据，跳过同步");
            }
            EditorUtility.SetDirty(_enemyDataTable);
        }

        // ==================== 删除操作 ====================
        private void ConfirmDelete()
        {
            foreach (var prefab in _pendingDeletePrefabs.ToArray())
            {
                try
                {
                    string prefabPath = AssetDatabase.GetAssetPath(prefab);
                    if (_enemyDataTable != null)
                    {
                        EnemyData dataToRemove = _enemyDataTable.enemies.Find(e => e.resourceName == prefabPath);
                        if (dataToRemove != null)
                        {
                            _enemyDataTable.enemies.Remove(dataToRemove);
                            EditorUtility.SetDirty(_enemyDataTable);
                        }
                    }
                    AssetDatabase.DeleteAsset(prefabPath);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PrefabMaker] 删除失败：{e.Message}");
                }
            }

            _pendingDeletePrefabs.Clear();
            _selectedPrefabIndex = -1;
            AssetDatabase.Refresh();
            RefreshPrefabList();
            ShowNotification(new GUIContent("删除完成！"));
        }

        // ==================== 生成操作 ====================
        private void ApplyCreatePendingSkills(GameObject targetObject)
        {
            foreach (string skillName in _createPendingAddSkills)
            {
                Type skillType = _allModuleTypes.FirstOrDefault(t => t.Name == skillName);
                if (skillType != null && targetObject.GetComponent(skillType) == null)
                {
                    MonoBehaviour newModule = (MonoBehaviour)targetObject.AddComponent(skillType);
                    if (_createPendingSkillData.ContainsKey(skillName))
                    {
                        SerializedObject serializedModule = new SerializedObject(newModule);
                        foreach (var kvp in _createPendingSkillData[skillName])
                        {
                            SerializedProperty property = serializedModule.FindProperty(kvp.Key);
                            if (property != null) SetPropertyValue(property, kvp.Value);
                        }
                        serializedModule.ApplyModifiedProperties();
                    }
                }
            }
            _createPendingAddSkills.Clear();
            _createPendingSkillData.Clear();
        }

        private void GeneratePrefab()
        {
            if (_previewObject == null)
            {
                ShowNotification(new GUIContent("错误：请先拖入一个基础模板物体！"));
                return;
            }

            if (string.IsNullOrEmpty(_prefabName))
            {
                ShowNotification(new GUIContent("错误：预制体名称不能为空！"));
                return;
            }

            try
            {
                if (!Directory.Exists(_savePath)) Directory.CreateDirectory(_savePath);

                string finalName = GetUniqueName(_prefabName);
                if (finalName != _prefabName)
                {
                    ShowNotification(new GUIContent($"名称已自动调整为：{finalName}"));
                    _prefabName = finalName;
                }

                List<string> skillsToAdd = new List<string>(_createPendingAddSkills);

                GameObject tempObject = Instantiate(_previewObject);
                tempObject.name = _prefabName;
                tempObject.hideFlags = HideFlags.None;

                EntityCore core = tempObject.GetComponent<EntityCore>();
                int newId = 101;
                if (_enemyDataTable != null && _enemyDataTable.enemies.Count > 0)
                    newId = _enemyDataTable.enemies.Max(e => e.id) + 1;
                if (core != null) core.id = newId;

                ApplyCreatePendingSkills(tempObject);

                // 关键修复：更新 tempObject 上的 EntityCore 的 skills 列表
                if (core != null && skillsToAdd != null)
                {
                    core.skills = new List<string>(skillsToAdd);
                }

                string fullPath = Path.Combine(_savePath, _prefabName + ".prefab");
                fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);
                PrefabUtility.SaveAsPrefabAsset(tempObject, fullPath, out bool success);

                DestroyImmediate(tempObject);

                if (success)
                {
                    // 更新 previewObject 的 EntityCore 数据
                    UpdatePreviewCoreData(newId, skillsToAdd);

                    CreateOrGetDataTable();
                    AddDataToTable(fullPath, newId, skillsToAdd);

                    AssetDatabase.Refresh();

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);

                    ShowNotification(new GUIContent("生成成功！"));
                }
            }
            catch (Exception e)
            {
                ShowNotification(new GUIContent($"生成失败：{e.Message}"));
            }
        }

        private void CreateOrGetDataTable()
        {
            if (_enemyDataTable != null) return;
            if (!Directory.Exists(_tableSavePath)) Directory.CreateDirectory(_tableSavePath);
            string tablePath = Path.Combine(_tableSavePath, "EnemyDataTable.asset");
            tablePath = AssetDatabase.GenerateUniqueAssetPath(tablePath);
            _enemyDataTable = ScriptableObject.CreateInstance<EnemyDataTable>();
            AssetDatabase.CreateAsset(_enemyDataTable, tablePath);
        }

        private void AddDataToTable(string fullPath, int newId, List<string> skillsToAdd)
        {
            string uniqueName = GetUniqueName(_prefabName);
            if (uniqueName != _prefabName) _prefabName = uniqueName;

            List<string> skillNames = new List<string>();
            ModuleBase[] modules = _previewObject.GetComponents<ModuleBase>();
            if (modules != null)
                foreach (var module in modules.Where(m => m != null))
                    skillNames.Add(module.GetType().Name);

            foreach (string skillName in skillsToAdd)
                if (!skillNames.Contains(skillName))
                    skillNames.Add(skillName);

            EnemyData newData = new EnemyData
            {
                id = newId,
                type = _createType,
                name = _prefabName,
                resourceName = fullPath,
                health = _createHealth,
                attack = _createAttack,
                defense = _createDefense,
                gold = _createGold,
                skills = skillNames
            };
            _enemyDataTable.enemies.Add(newData);
            EditorUtility.SetDirty(_enemyDataTable);
            AssetDatabase.SaveAssets();
        }
    }
}