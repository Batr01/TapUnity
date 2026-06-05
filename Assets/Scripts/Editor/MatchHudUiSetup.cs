using TapBrawl.Core;
using TapBrawl.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TapBrawl.Editor
{
    public static class MatchHudUiSetup
    {
        public const string MatchScenePath = "Assets/Scenes/Match.unity";
        public const string MatchHudPrefabPath = "Assets/Prefabs/UI/MatchHud.prefab";

        [MenuItem("Tap/Sync Match HUD")]
        public static void SyncMatchSceneMenu()
        {
            if (!System.IO.File.Exists(MatchScenePath))
            {
                EditorUtility.DisplayDialog("Match HUD", "Сцена не найдена: " + MatchScenePath, "OK");
                return;
            }

            var scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
            Undo.SetCurrentGroupName("Sync Match HUD");
            var group = Undo.GetCurrentGroup();
            ApplyActiveScene(silent: false);
            Undo.CollapseUndoOperations(group);
            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog("Match HUD", "Match HUD обновлён.", "OK");
        }

        [MenuItem("Tap/Export Match HUD Prefab")]
        public static void ExportMatchHudPrefab()
        {
            if (!System.IO.File.Exists(MatchScenePath))
                return;

            var scene = EditorSceneManager.OpenScene(MatchScenePath, OpenSceneMode.Single);
            ApplyActiveScene(silent: true);
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
                return;

            var dir = System.IO.Path.GetDirectoryName(MatchHudPrefabPath);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir!);

            var clone = Object.Instantiate(canvas.gameObject);
            clone.name = "MatchHud";
            PrefabUtility.SaveAsPrefabAsset(clone, MatchHudPrefabPath);
            Object.DestroyImmediate(clone);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Match HUD", $"Префаб сохранён: {MatchHudPrefabPath}", "OK");
        }

        public static void ApplyActiveScene(bool silent)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                if (!silent)
                    Debug.LogWarning("[MatchHud] Canvas не найден.");
                return;
            }

            EnsureLayoutApplier(canvas);
            MatchHudLayoutApplier.ResetSessionFlag();
            MatchHudLayoutApplier.Apply(canvas);
            WireMatchControllerSerialized(canvas.transform);
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        }

        private static void EnsureLayoutApplier(Canvas canvas)
        {
            if (canvas.GetComponent<MatchHudLayoutApplier>() == null)
                Undo.AddComponent<MatchHudLayoutApplier>(canvas.gameObject);
        }

        private static void WireMatchControllerSerialized(Transform canvasRoot)
        {
            var match = Object.FindFirstObjectByType<MatchController>();
            if (match == null)
                return;

            match.BindHudLabels(canvasRoot);
            var so = new SerializedObject(match);
            so.FindProperty("scoreLabel").objectReferenceValue = FindTmp(canvasRoot, "ScoreText");
            so.FindProperty("timerLabel").objectReferenceValue = FindTmp(canvasRoot, "TimerText");
            so.FindProperty("modeLabel").objectReferenceValue = FindTmp(canvasRoot, "ModeLabel");
            so.FindProperty("opponentLabel").objectReferenceValue = FindTmp(canvasRoot, "Opponent Label");
            so.ApplyModifiedPropertiesWithoutUndo();

            var duplicate = match.GetComponent<GameplaySkillBarController>();
            if (duplicate != null)
                Undo.DestroyObjectImmediate(duplicate);

            var skillBar = FindRect(canvasRoot, MatchHudLayoutApplier.SkillBarName);
            if (skillBar == null)
                return;

            var skillCtrl = skillBar.GetComponent<GameplaySkillBarController>();
            if (skillCtrl == null)
                skillCtrl = Undo.AddComponent<GameplaySkillBarController>(skillBar.gameObject);
            skillCtrl.BindFromScene(match, canvasRoot);

            var skillSo = new SerializedObject(skillCtrl);
            skillSo.FindProperty("skillEnergyFill").objectReferenceValue =
                FindRect(skillBar, "EnergyBar_Fill")?.GetComponent<Image>();
            skillSo.FindProperty("skillEnergyText").objectReferenceValue =
                FindTmp(skillBar, MatchHudLayoutApplier.EnergyTextName);
            skillSo.FindProperty("opponentSkillNoticeText").objectReferenceValue =
                FindTmp(canvasRoot, "OpponentSkillNoticeText");
            skillSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_Text? FindTmp(Transform root, string objectName)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != objectName)
                    continue;
                return t.GetComponent<TextMeshProUGUI>();
            }
            return null;
        }

        private static RectTransform? FindRect(Transform root, string objectName)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == objectName)
                    return (RectTransform)t;
            }
            return null;
        }
    }
}
