using System.Collections.Generic;
using System.IO;
using ProjectS.Combat;
using ProjectS.UI;
using UnityEditor;
using UnityEngine;

namespace ProjectS.EditorTools
{
    /// <summary>
    /// 아트 가져오기 + 표시용 프리팹/설정 생성 (PrototypeSetup.Run 에서 호출).
    /// - 프로젝트 밖 ArtSources(한글 파일명)를 영문 이름으로 Assets/Art 에 복사하고 스프라이트 임포트 설정을 맞춘다.
    ///   이미 있는 파일은 덮어쓰지 않는다. ArtSources 가 없으면(다른 PC) 건너뛴다 — 복사본은 저장소에 커밋되어 있다.
    /// - TileView / UnitView 프리팹, PresentationConfig 생성.
    /// - 유닛 스프라이트가 비어 있으면 연결.
    /// 아트 교체: Assets/Art 의 파일을 바꾸거나, UnitDefinition / PresentationConfig 의 스프라이트 참조를 바꾼다.
    /// </summary>
    public static partial class PrototypeSetup
    {
        public const string ArtFolder = "Assets/Art";
        public const string PrefabsFolder = "Assets/Prefabs";
        public const string PresentationPath = "Assets/Data/Presentation/PresentationConfig.asset";
        private const string WhiteSpritePath = ArtFolder + "/Common/White.png";
        private const string TilePrefabPath = PrefabsFolder + "/TileView.prefab";
        private const string UnitPrefabPath = PrefabsFolder + "/UnitView.prefab";

        private enum ArtKind
        {
            Unit,
            Tile,
            Ui,
        }

        /// <summary>(ArtSources 기준 원본 경로, Assets/Art 기준 대상 경로, 종류)</summary>
        private static readonly (string source, string target, ArtKind kind)[] ArtFiles =
        {
            ("01_clare/클레어.png", "Units/Clare.png", ArtKind.Unit),
            ("02_lian/리안.png", "Units/Lian.png", ArtKind.Unit),
            ("03_kiristel_Ader/에더.png", "Units/Ader.png", ArtKind.Unit),
            ("03_Nika/니카.png", "Units/Nika.png", ArtKind.Unit),
            ("04_Kaiti/케이티.png", "Units/Katie.png", ArtKind.Unit),
            ("05_kano/카노.png", "Units/Kano.png", ArtKind.Unit),
            ("98_mob1/mob1_front.png", "Units/Raptor_front.png", ArtKind.Unit),
            ("98_mob1/mob1_back.png", "Units/Raptor_back.png", ArtKind.Unit),
            ("98_mob1/mob1_left.png", "Units/Raptor_left.png", ArtKind.Unit),
            ("98_mob1/mob1_right.png", "Units/Raptor_right.png", ArtKind.Unit),
            // mob2 는 1스테이지에 쓰이지 않음(2스테이지 적 후보). 이름은 원본 그대로 유지.
            ("99_mob2/mob2_front.png", "Units/Mob2_front.png", ArtKind.Unit),
            ("99_mob2/mob2_back.png", "Units/Mob2_back.png", ArtKind.Unit),
            ("99_mob2/mob2_left.png", "Units/Mob2_left.png", ArtKind.Unit),
            ("99_mob2/mob2_right.png", "Units/Mob2_right.png", ArtKind.Unit),
            ("tile_grass.png", "Tiles/Grass.png", ArtKind.Tile),
            ("tile_dirt.png", "Tiles/Dirt.png", ArtKind.Tile),
            ("tile_stone.png", "Tiles/Stone.png", ArtKind.Tile),
            ("tile_steel.png", "Tiles/Steel.png", ArtKind.Tile),
            ("4 battle victory/victory.png", "UI/Victory.png", ArtKind.Ui),
            ("5 battle/turn_end.png", "UI/TurnEnd.png", ArtKind.Ui),
        };

        /// <summary>캐릭터 Id → 정면 스프라이트 파일(Assets/Art/Units)</summary>
        private static readonly Dictionary<string, string> CharacterSprites = new Dictionary<string, string>
        {
            ["Clare"] = "Clare", ["Lian"] = "Lian", ["Ader"] = "Ader",
            ["Nika"] = "Nika", ["Katie"] = "Katie", ["Kano"] = "Kano",
            // 멜리디아: ArtSources 에 스프라이트 없음 → 색 사각형
        };

        private static PresentationConfig SetupArt()
        {
            ImportArtSources();
            var white = EnsureWhiteSprite();
            var tilePrefab = EnsureTilePrefab(white);
            var unitPrefab = EnsureUnitPrefab(white);

            EnsureFolder(Path.GetDirectoryName(PresentationPath).Replace('\\', '/'));
            return LoadOrCreate<PresentationConfig>(PresentationPath, so =>
            {
                so.FindProperty("tilePrefab").objectReferenceValue = tilePrefab;
                so.FindProperty("unitPrefab").objectReferenceValue = unitPrefab;
                so.FindProperty("tileSprite").objectReferenceValue = LoadSprite("Tiles/Grass");
                so.FindProperty("victoryBanner").objectReferenceValue = LoadSprite("UI/Victory");
                so.FindProperty("turnEndButton").objectReferenceValue = LoadSprite("UI/TurnEnd");
            });
        }

        // ---------------- 아트 복사 / 임포트 ----------------

        private static void ImportArtSources()
        {
            string sourceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../ArtSources"));
            bool hasSource = Directory.Exists(sourceRoot);
            if (!hasSource) Debug.LogWarning($"[PrototypeSetup] ArtSources 폴더가 없어 아트 복사를 건너뜁니다: {sourceRoot}");

            foreach (var (source, target, kind) in ArtFiles)
            {
                string assetPath = $"{ArtFolder}/{target}";
                if (!File.Exists(assetPath))
                {
                    string src = Path.Combine(sourceRoot, source);
                    if (!hasSource || !File.Exists(src)) continue;

                    EnsureFolder(Path.GetDirectoryName(assetPath).Replace('\\', '/'));
                    File.Copy(src, assetPath);
                    AssetDatabase.ImportAsset(assetPath);
                    Debug.Log($"[PrototypeSetup] Copied art {source} -> {assetPath}");
                }

                ConfigureSprite(assetPath, kind);
            }
        }

        private static void ConfigureSprite(string assetPath, ArtKind kind)
        {
            if (!(AssetImporter.GetAtPath(assetPath) is TextureImporter importer)) return;
            if (importer.textureType == TextureImporterType.Sprite && importer.userData == "ProjectS") return; // 이미 설정됨(수동 변경 보존)

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            switch (kind)
            {
                case ArtKind.Unit: // 도트 스프라이트: 선명하게
                    importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.spritePixelsPerUnit = 128;
                    break;
                case ArtKind.Tile:
                    importer.filterMode = FilterMode.Bilinear;
                    importer.spritePixelsPerUnit = 490;
                    break;
                default:
                    importer.filterMode = FilterMode.Bilinear;
                    importer.spritePixelsPerUnit = 100;
                    break;
            }

            importer.userData = "ProjectS";
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string pathInArt) =>
            AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{pathInArt}.png");

        private static Sprite EnsureWhiteSprite()
        {
            if (!File.Exists(WhiteSpritePath))
            {
                EnsureFolder(Path.GetDirectoryName(WhiteSpritePath).Replace('\\', '/'));
                var tex = new Texture2D(4, 4);
                var pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
                tex.SetPixels(pixels);
                File.WriteAllBytes(WhiteSpritePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(WhiteSpritePath);
            }

            if (AssetImporter.GetAtPath(WhiteSpritePath) is TextureImporter importer && importer.userData != "ProjectS")
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 4; // 1 unit
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.userData = "ProjectS";
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(WhiteSpritePath);
        }

        // ---------------- 프리팹 ----------------

        private static TileView EnsureTilePrefab(Sprite white)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TileView>(TilePrefabPath);
            if (existing != null) return existing;

            EnsureFolder(PrefabsFolder);
            var root = new GameObject("TileView");
            var view = root.AddComponent<TileView>();
            var baseRenderer = AddSprite(root.transform, "Base", white);
            var highlight = AddSprite(root.transform, "Highlight", white);

            var so = new SerializedObject(view);
            so.FindProperty("baseRenderer").objectReferenceValue = baseRenderer;
            so.FindProperty("highlightRenderer").objectReferenceValue = highlight;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab<TileView>(root, TilePrefabPath);
        }

        private static UnitView EnsureUnitPrefab(Sprite white)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UnitView>(UnitPrefabPath);
            if (existing != null) return existing;

            EnsureFolder(PrefabsFolder);
            var root = new GameObject("UnitView");
            var view = root.AddComponent<UnitView>();
            var body = AddSprite(root.transform, "Body", white);
            var hpBack = AddSprite(root.transform, "HpBack", white);
            var fillRoot = new GameObject("HpFillRoot").transform;
            fillRoot.SetParent(root.transform, false);
            var hpFill = AddSprite(fillRoot, "HpFill", white);

            var so = new SerializedObject(view);
            so.FindProperty("body").objectReferenceValue = body;
            so.FindProperty("hpBack").objectReferenceValue = hpBack;
            so.FindProperty("hpFillRoot").objectReferenceValue = fillRoot;
            so.FindProperty("hpFill").objectReferenceValue = hpFill;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab<UnitView>(root, UnitPrefabPath);
        }

        private static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            return sr;
        }

        private static T SavePrefab<T>(GameObject root, string path) where T : Component
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[PrototypeSetup] Created {path}");
            return prefab.GetComponent<T>();
        }

        // ---------------- 유닛 스프라이트 연결 ----------------

        private static void AssignUnitSprites(UnitDefinition unit, string front, string back = null, string left = null, string right = null)
        {
            var so = new SerializedObject(unit);
            var frontProp = so.FindProperty("spriteFront");
            if (frontProp.objectReferenceValue != null) return; // 이미 지정됨

            frontProp.objectReferenceValue = LoadSprite($"Units/{front}");
            if (back != null) so.FindProperty("spriteBack").objectReferenceValue = LoadSprite($"Units/{back}");
            if (left != null) so.FindProperty("spriteLeft").objectReferenceValue = LoadSprite($"Units/{left}");
            if (right != null) so.FindProperty("spriteRight").objectReferenceValue = LoadSprite($"Units/{right}");
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
