using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace TapBrawl.Editor
{
    public static class TapCircleSpriteSheetsSetup
    {
        private const string ConfigPath = "Assets/ScriptableObjects/DefaultSpawnConfig.asset";

        private static readonly SheetDef[] Sheets =
        {
            new("Assets/Art/Sprites/blue_ball_sheet.png", 64, 8, "blue_ball", true),
            new("Assets/Art/Sprites/gold_ball_sheet.png", 64, 8, "gold_ball", true),
            new("Assets/Art/Sprites/bomb_ball_sheet.png", 64, 8, "bomb_ball", true),
            new("Assets/Art/Sprites/phantom_ball_sheet.png", 64, 8, "phantom_ball", true),
            new("Assets/Art/Sprites/hit_explosion_sheet.png", 96, 10, "hit_explosion", false),
            new("Assets/Art/Sprites/gold_hit_sheet.png", 96, 10, "gold_hit", false),
            new("Assets/Art/Sprites/chain_hit_sheet.png", 96, 10, "chain_hit", false),
            new("Assets/Art/Sprites/perfect_hit_sheet.png", 96, 10, "perfect_hit", false),
            new("Assets/Art/Sprites/bomb_tap_sheet.png", 96, 12, "bomb_tap", false),
            new("Assets/Art/Sprites/phantom_miss_sheet.png", 64, 6, "phantom_miss", false),
        };

        [MenuItem("Tap/Art/Setup All Circle Sprite Sheets")]
        public static void SetupAll()
        {
            foreach (var sheet in Sheets)
                SliceSheet(sheet);

            AssetDatabase.Refresh();
            AssignDefaultConfig();
            AssetDatabase.SaveAssets();
            LogAssignmentWarnings();
            Debug.Log("All circle sprite sheets sliced and assigned to DefaultSpawnConfig.");
        }

        [MenuItem("Tap/Art/Setup Blue Ball Sprite Sheet")]
        public static void SetupBlueBallOnly() => SliceSheet(Sheets[0]);

        [MenuItem("Tap/Art/Setup Hit Explosion Sprite Sheet")]
        public static void SetupHitExplosionOnly() => SliceSheet(Sheets[4]);

        private static void SliceSheet(SheetDef sheet)
        {
            var importer = AssetImporter.GetAtPath(sheet.Path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Missing texture: {sheet.Path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.spritePixelsPerUnit = sheet.FrameSize;

#pragma warning disable 618
            var sprites = new SpriteMetaData[sheet.FrameCount];
            for (var i = 0; i < sheet.FrameCount; i++)
            {
                sprites[i] = new SpriteMetaData
                {
                    name = $"{sheet.Prefix}_{i:00}",
                    rect = sheet.Horizontal
                        ? new Rect(i * sheet.FrameSize, 0, sheet.FrameSize, sheet.FrameSize)
                        : new Rect(0, i * sheet.FrameSize, sheet.FrameSize, sheet.FrameSize),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                };
            }

            importer.spritesheet = sprites;
#pragma warning restore 618
            importer.SaveAndReimport();
        }

        private static void AssignDefaultConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<TapBrawl.Core.CircleSpawnConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError($"Config not found: {ConfigPath}");
                return;
            }

            var so = new SerializedObject(config);
            SetAnim(so, "normalIdleAnim", LoadFrames(Sheets[0]), 12f, 1f);
            SetAnim(so, "goldIdleAnim", LoadFrames(Sheets[1]), 12f, 1f);
            SetAnim(so, "bombIdleAnim", LoadFrames(Sheets[2]), 14f, 1f);
            SetAnim(so, "phantomIdleAnim", LoadFrames(Sheets[3]), 10f, 1f);
            SetAnim(so, "normalHitAnim", LoadFrames(Sheets[4]), 20f, 1.35f);
            SetAnim(so, "goldHitAnim", LoadFrames(Sheets[5]), 20f, 1.4f);
            SetAnim(so, "phantomHitAnim", LoadFrames(Sheets[4]), 20f, 1.3f);
            SetAnim(so, "perfectHitAnim", LoadFrames(Sheets[7]), 22f, 1.65f);
            SetAnim(so, "chainHitAnim", LoadFrames(Sheets[6]), 24f, 1.45f);
            SetAnim(so, "bombTapAnim", LoadFrames(Sheets[8]), 18f, 4.8f);
            SetAnim(so, "phantomMissAnim", LoadFrames(Sheets[9]), 14f, 0.9f);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static List<Sprite> LoadFrames(SheetDef sheet)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(sheet.Path);
            var ordered = new List<Sprite>(sheet.FrameCount);
            for (var i = 0; i < sheet.FrameCount; i++)
            {
                var sprite = FindSprite(assets, $"{sheet.Prefix}_{i:00}");
                if (sprite != null)
                    ordered.Add(sprite);
            }

            return ordered;
        }

        private static void SetAnim(SerializedObject so, string propertyName, List<Sprite> sprites, float fps, float scale)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"Missing property: {propertyName}");
                return;
            }

            SetArray(prop.FindPropertyRelative("frames"), sprites);
            prop.FindPropertyRelative("fps").floatValue = fps;
            prop.FindPropertyRelative("scale").floatValue = scale;
        }

        private static void SetArray(SerializedProperty prop, List<Sprite> sprites)
        {
            if (prop == null)
                return;
            prop.arraySize = sprites.Count;
            for (var i = 0; i < sprites.Count; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        private static void LogAssignmentWarnings()
        {
            var config = AssetDatabase.LoadAssetAtPath<TapBrawl.Core.CircleSpawnConfig>(ConfigPath);
            if (config == null)
                return;

            WarnIfFewFrames("normalHitAnim", config.normalHitAnim, 8);
            WarnIfFewFrames("perfectHitAnim", config.perfectHitAnim, 8);
            WarnIfFewFrames("goldHitAnim", config.goldHitAnim, 8);
            WarnIfFewFrames("chainHitAnim", config.chainHitAnim, 8);
            WarnIfFewFrames("bombTapAnim", config.bombTapAnim, 10);
        }

        private static void WarnIfFewFrames(string label, TapBrawl.Core.SpriteSheetAnimSet anim, int expectedMin)
        {
            if (anim.ValidFrameCount < expectedMin)
            {
                Debug.LogWarning(
                    $"[Tap/Art] {label}: назначено {anim.ValidFrameCount} кадров (ожидалось ≥{expectedMin}). " +
                    "Перезапусти Setup All или проверь slice в PNG.");
            }
        }

        private static Sprite? FindSprite(Object[] assets, string name)
        {
            foreach (var asset in assets)
            {
                if (asset is Sprite sprite && sprite.name == name)
                    return sprite;
            }

            return null;
        }

        private readonly struct SheetDef
        {
            public readonly string Path;
            public readonly int FrameSize;
            public readonly int FrameCount;
            public readonly string Prefix;
            public readonly bool Horizontal;

            public SheetDef(string path, int frameSize, int frameCount, string prefix, bool horizontal)
            {
                Path = path;
                FrameSize = frameSize;
                FrameCount = frameCount;
                Prefix = prefix;
                Horizontal = horizontal;
            }
        }
    }
}
