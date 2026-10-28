using System.IO;
using System.Linq;
using ProjectS.Combat;
using ProjectS.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ProjectS.EditorTools
{
    /// <summary>
    /// 1스테이지 프로토타입의 데이터 에셋과 씬을 만든다. 메뉴: Project S > Setup Stage1 Prototype
    /// 이미 있는 에셋·씬은 덮어쓰지 않는다(기획자가 수정한 값 보존). 다시 만들려면 해당 파일을 지운 뒤 실행.
    /// 배치 모드: Unity -batchmode -executeMethod ProjectS.EditorTools.PrototypeSetup.Run -quit
    /// </summary>
    public static class PrototypeSetup
    {
        public const string UnitsFolder = "Assets/Data/Units";
        public const string StagesFolder = "Assets/Data/Stages";
        public const string EnemyPath = UnitsFolder + "/Enemy_Grunt.asset";
        public const string Stage1Path = StagesFolder + "/Stage1.asset";
        public const string ScenePath = "Assets/Scenes/Stage1Prototype.unity";

        [MenuItem("Project S/Setup Stage1 Prototype")]
        public static void Run()
        {
            EnsureFolder(UnitsFolder);
            EnsureFolder(StagesFolder);

            var enemy = LoadOrCreate<UnitDefinition>(EnemyPath, so =>
            {
                // PROTOTYPE: 적 수치 임시값
                so.FindProperty("displayName").stringValue = "Grunt";
                so.FindProperty("maxHp").intValue = 6;
                so.FindProperty("color").colorValue = new Color(0.85f, 0.25f, 0.25f);
            });

            var stage = LoadOrCreate<StageDefinition>(Stage1Path, so =>
            {
                so.FindProperty("displayName").stringValue = "Stage 1 (Prototype)";
                so.FindProperty("width").intValue = 20;
                so.FindProperty("height").intValue = 20;

                // 기획 확정값: 적 1종 3기 (1,10), (2,5), (2,15)
                var spawnCoords = new[] { new Vector2Int(1, 10), new Vector2Int(2, 5), new Vector2Int(2, 15) };
                var spawns = so.FindProperty("enemySpawns");
                spawns.arraySize = spawnCoords.Length;
                for (int i = 0; i < spawnCoords.Length; i++)
                {
                    var element = spawns.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("unit").objectReferenceValue = enemy;
                    element.FindPropertyRelative("designCoord").vector2IntValue = spawnCoords[i];
                }

                // 기획 확정값: 아군 배치 구역 (1,18)~(20,20)
                so.FindProperty("deploymentZoneMin").vector2IntValue = new Vector2Int(1, 18);
                so.FindProperty("deploymentZoneMax").vector2IntValue = new Vector2Int(20, 20);
            });

            CreateSceneIfMissing(stage);
            AddSceneToBuildSettingsFirst();
            AssetDatabase.SaveAssets();
            Debug.Log("[PrototypeSetup] Done");
        }

        private static T LoadOrCreate<T>(string path, System.Action<SerializedObject> initialize) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            var so = new SerializedObject(asset);
            initialize(so);
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[PrototypeSetup] Created {path}");
            return asset;
        }

        private static void CreateSceneIfMissing(StageDefinition stage)
        {
            if (File.Exists(ScenePath)) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            var flowGo = new GameObject("StageFlow");
            var flow = flowGo.AddComponent<StageFlowController>();
            var so = new SerializedObject(flow);
            so.FindProperty("stage").objectReferenceValue = stage;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[PrototypeSetup] Created {ScenePath}");
        }

        private static void AddSceneToBuildSettingsFirst()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;

            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
