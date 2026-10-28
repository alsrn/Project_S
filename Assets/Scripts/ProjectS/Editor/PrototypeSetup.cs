using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectS.Actions;
using ProjectS.Combat;
using ProjectS.Core;
using ProjectS.Targeting;
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
        public const string ActionsFolder = "Assets/Data/Actions";
        public const string EnemyPath = UnitsFolder + "/Enemy_Raptor.asset";
        public const string Stage1Path = StagesFolder + "/Stage1.asset";
        public const string RosterPath = UnitsFolder + "/PrototypeRoster.asset";
        public const string MovePath = ActionsFolder + "/Move_Default.asset";
        public const string CardsFolder = "Assets/Data/Cards";
        public const string RulesFolder = "Assets/Data/Rules";
        public const string RulesPath = RulesFolder + "/BattleRules.asset";
        public const string ScenePath = "Assets/Scenes/Stage1Prototype.unity";

        private struct UnitSpec
        {
            public string Id, Name;
            public int Hp, Attack, Defense, Move, Range, Unlock;
            public Color Color;

            public UnitSpec(string id, string name, int hp, int attack, int defense, int move, int range, int unlock, Color color)
            {
                Id = id; Name = name; Hp = hp; Attack = attack; Defense = defense;
                Move = move; Range = range; Unlock = unlock; Color = color;
            }
        }

        // 출처: GameDesigns/GDD_Main/캐릭터 스킬 및 능력(동아리).xlsx  — "NxN" 범위는 N 으로 입력(PROTOTYPE 해석)
        private static readonly UnitSpec[] Characters =
        {
            new UnitSpec("Clare", "클레어", 1500, 40, 45, 7, 4, 1, new Color(0.25f, 0.55f, 0.95f)),
            new UnitSpec("Lian", "리안", 2500, 25, 60, 4, 3, 1, new Color(0.2f, 0.8f, 0.75f)),
            new UnitSpec("Ader", "에더 키리스텔", 1100, 35, 30, 5, 5, 1, new Color(0.55f, 0.4f, 0.95f)),
            new UnitSpec("Melidia", "멜리디아", 1350, 15, 35, 5, 4, 2, new Color(0.95f, 0.6f, 0.85f)),
            new UnitSpec("Katie", "케이티 페어크로스", 900, 75, 25, 4, 6, 2, new Color(0.95f, 0.75f, 0.2f)),
            new UnitSpec("Nika", "니카", 1500, 60, 40, 8, 3, 3, new Color(1f, 0.5f, 0.2f)),
            new UnitSpec("Kano", "카노", 750, 80, 15, 3, 8, 3, new Color(0.4f, 0.85f, 0.3f)),
        };

        // 출처: GameDesigns/GDD_Main/라스피 적 기획(동아리).xlsx
        private static readonly UnitSpec Raptor =
            new UnitSpec("Raptor", "랩터", 1200, 40, 30, 5, 5, 1, new Color(0.85f, 0.25f, 0.25f));

        private struct CardSpec
        {
            public string File, Name, Description;
            public int Cost;
            public float Multiplier;

            public CardSpec(string file, string name, int cost, float multiplier, string description)
            {
                File = file; Name = name; Cost = cost; Multiplier = multiplier; Description = description;
            }
        }

        // 출처: GameDesigns/GDD_Main/카드 디자인(동아리).xlsx — 캐릭터별 평타 카드(키 = 캐릭터 Id)
        private static readonly Dictionary<string, CardSpec> BasicAttackCards = new Dictionary<string, CardSpec>
        {
            ["Clare"] = new CardSpec("Clare_SpinSlash", "회전 베기", 1, 2f, "현재 공격력의 x2배 피해를 준다"),
            // PROTOTYPE 밸런스: 단순 차감 공식에서 GDD x1 로는 랩터(방어 30)에 피해가 거의 없어 배율을 올림.
            // 목표: 테스트 배율 x15 에서 아무 두 명의 평타로 랩터(1200) 처치, 세 캐릭터 피해가 비슷하게(675~750).
            ["Lian"] = new CardSpec("Lian_Swing", "휘두르기", 1, 3f, "현재 공격력 만큼의 피해를 준다 (PROTOTYPE 밸런스: x3)"),
            ["Ader"] = new CardSpec("Ader_BloodShot", "혈탄 발사", 1, 2.2f, "현재 공격력 만큼의 피해를 준다 (PROTOTYPE 밸런스: x2.2)"),
            ["Melidia"] = new CardSpec("Melidia_MelodyStrike", "멜로디 타격", 1, 1f, "현재 공격력 만큼의 피해를 준다"),
            ["Katie"] = new CardSpec("Katie_MagicBullet", "마탄", 2, 2f, "현재 공격력의 x2배 피해를 준다"),
            ["Nika"] = new CardSpec("Nika_FirePunch", "불주먹", 1, 1f, "현재 공격력 만큼의 피해를 준다"),
            ["Kano"] = new CardSpec("Kano_Aim", "정조준", 2, 3f,
                "현재 공격력의 x3배의 피해를 준다. (PROTOTYPE: 연속 사용·SP 증가 메커니즘 미구현)"),
        };

        [MenuItem("Project S/Setup Stage1 Prototype")]
        public static void Run()
        {
            EnsureFolder(UnitsFolder);
            EnsureFolder(StagesFolder);
            EnsureFolder(ActionsFolder);
            EnsureFolder(CardsFolder);
            EnsureFolder(RulesFolder);

            // 기본값 = 단순 차감, STS 드로우 5장. 테스트 배율 x15(승패 흐름 시험용, 기획 협의값)
            var rules = LoadOrCreate<BattleRules>(RulesPath, so => so.FindProperty("debugDamageMultiplier").floatValue = 15f);

            var enemy = CreateUnit(EnemyPath, Raptor);

            var move = LoadOrCreate<MoveDefinition>(MovePath, so =>
            {
                so.FindProperty("displayName").stringValue = "이동";
                so.FindProperty("baseCost").intValue = 1; // PROTOTYPE: 이동 1회 = SP 1
                so.FindProperty("secondsPerTile").floatValue = 0.15f;
            });

            // 출처: GameDesigns/GDD_Main/스테이지 기획(동아리).xlsx
            var stage = LoadOrCreate<StageDefinition>(Stage1Path, so =>
            {
                so.FindProperty("displayName").stringValue = "스테이지 1";
                so.FindProperty("stageNumber").intValue = 1;
                so.FindProperty("partySize").intValue = 3;
                so.FindProperty("turnLimit").intValue = 10;
                so.FindProperty("maxSp").intValue = 20;
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

            var characters = Characters.Select(c =>
            {
                var unit = CreateUnit($"{UnitsFolder}/Char_{c.Id}.asset", c);
                if (BasicAttackCards.TryGetValue(c.Id, out var cardSpec))
                    AssignCardsIfEmpty(unit, CreateBasicAttackCard(cardSpec));
                return unit;
            }).ToList();

            var roster = LoadOrCreate<PrototypeRoster>(RosterPath, so =>
            {
                var list = so.FindProperty("candidates");
                list.arraySize = characters.Count;
                for (int i = 0; i < characters.Count; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = characters[i];
            });

            EnsureScene(stage, roster, move, rules);
            AddSceneToBuildSettingsFirst();
            AssetDatabase.SaveAssets();
            Debug.Log("[PrototypeSetup] Done");
        }

        private static UnitDefinition CreateUnit(string path, UnitSpec spec) =>
            LoadOrCreate<UnitDefinition>(path, so =>
            {
                so.FindProperty("displayName").stringValue = spec.Name;
                so.FindProperty("maxHp").intValue = spec.Hp;
                so.FindProperty("attack").intValue = spec.Attack;
                so.FindProperty("defense").intValue = spec.Defense;
                so.FindProperty("moveRange").intValue = spec.Move;
                so.FindProperty("attackRange").intValue = spec.Range;
                so.FindProperty("unlockStage").intValue = spec.Unlock;
                so.FindProperty("color").colorValue = spec.Color;
            });

        /// <summary>단일 적 대상 · 사용자 공격 범위 · 공격력 배율 피해 카드.</summary>
        private static CardDefinition CreateBasicAttackCard(CardSpec spec) =>
            LoadOrCreate<CardDefinition>($"{CardsFolder}/Card_{spec.File}.asset", so =>
            {
                so.FindProperty("displayName").stringValue = spec.Name;
                so.FindProperty("baseCost").intValue = spec.Cost;
                so.FindProperty("description").stringValue = spec.Description;

                var requirements = so.FindProperty("targetRequirements");
                requirements.arraySize = 1;
                var req = requirements.GetArrayElementAtIndex(0);
                req.FindPropertyRelative("kind").enumValueIndex = (int)TargetKind.Unit;
                req.FindPropertyRelative("relation").enumValueIndex = (int)TargetRelation.Enemy;
                req.FindPropertyRelative("requireAlive").boolValue = true;
                req.FindPropertyRelative("rangeSource").enumValueIndex = (int)RangeSource.ActorAttackRange;
                req.FindPropertyRelative("fixedRange").intValue = 0;
                req.FindPropertyRelative("requireLineOfSight").boolValue = false;

                var effects = so.FindProperty("effects");
                effects.arraySize = 1;
                effects.GetArrayElementAtIndex(0).managedReferenceValue = new DamageEffect
                {
                    targetIndex = 0,
                    attackMultiplier = spec.Multiplier,
                };
            });

        private static void AssignCardsIfEmpty(UnitDefinition unit, params CardDefinition[] cards)
        {
            var so = new SerializedObject(unit);
            var list = so.FindProperty("cards");
            if (list.arraySize > 0) return;

            list.arraySize = cards.Length;
            for (int i = 0; i < cards.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
            so.ApplyModifiedPropertiesWithoutUndo();
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

        /// <summary>씬이 없으면 만들고, 있으면 비어 있는 참조만 채운다(이미 지정된 참조는 유지).</summary>
        private static void EnsureScene(StageDefinition stage, PrototypeRoster roster, MoveDefinition move, BattleRules rules)
        {
            bool exists = File.Exists(ScenePath);
            var scene = exists
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            if (!exists)
            {
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                camGo.AddComponent<AudioListener>();
                camGo.transform.position = new Vector3(0f, 0f, -10f);
                new GameObject("StageFlow").AddComponent<StageFlowController>();
            }

            var flow = Object.FindFirstObjectByType<StageFlowController>();
            var so = new SerializedObject(flow);
            AssignIfEmpty(so, "stage", stage);
            AssignIfEmpty(so, "roster", roster);
            AssignIfEmpty(so, "moveDefinition", move);
            AssignIfEmpty(so, "battleRules", rules);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[PrototypeSetup] {(exists ? "Updated" : "Created")} {ScenePath}");
        }

        private static void AssignIfEmpty(SerializedObject so, string property, Object value)
        {
            var p = so.FindProperty(property);
            if (p.objectReferenceValue == null) p.objectReferenceValue = value;
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
