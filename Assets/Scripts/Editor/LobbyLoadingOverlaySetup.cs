using TapBrawl.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.Editor
{
    public static class LobbyLoadingOverlaySetup
    {
        private const string PrefabPath = "Assets/Prefabs/UI/LoadingOverlay.prefab";

        [MenuItem("Tap/Setup Lobby Loading Overlay")]
        public static void SetupFromMenu() => RunSetupInternal(true);

        public static void RunSetupBatch() => RunSetupInternal(false);

        private static void RunSetupInternal(bool showDialog)
        {
            Undo.SetCurrentGroupName("Setup Lobby Loading Overlay");
            var group = Undo.GetCurrentGroup();

            EnsurePrefab();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "Не удалось создать LoadingOverlay.prefab.", "OK");
                return;
            }

            var lobbyScene = EditorSceneManager.OpenScene(LobbyEditorObjectFind.LobbyScenePath, OpenSceneMode.Single);
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "В сцене Lobby нет Canvas.", "OK");
                return;
            }

            var existing = Object.FindAnyObjectByType<LoadingOverlay>(FindObjectsInactive.Include);
            LoadingOverlay overlay;
            if (existing != null)
            {
                overlay = existing;
            }
            else
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);
                Undo.RegisterCreatedObjectUndo(instance, "Loading Overlay");
                overlay = instance.GetComponent<LoadingOverlay>();
            }

            var controller = Object.FindAnyObjectByType<LobbyScreenController>();
            if (controller != null)
            {
                var so = new SerializedObject(controller);
                so.FindProperty("loadingOverlay").objectReferenceValue = overlay;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(lobbyScene);
            EditorSceneManager.SaveScene(lobbyScene);
            AssetDatabase.SaveAssets();
            Undo.CollapseUndoOperations(group);

            if (showDialog)
                EditorUtility.DisplayDialog("Готово", "LoadingOverlay создан и подключён к Lobby.", "OK");

            Debug.Log("[Lobby] LoadingOverlay настроен.");
        }

        private static void EnsurePrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;

            var root = new GameObject(
                "LoadingOverlay",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(LoadingOverlay));
            StretchFull(root.GetComponent<RectTransform>());
            var overlay = root.GetComponent<LoadingOverlay>();
            overlay.BuildUi();
            root.SetActive(false);

            var dir = System.IO.Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir!))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
                    AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.Refresh();
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
        }

    }
}
