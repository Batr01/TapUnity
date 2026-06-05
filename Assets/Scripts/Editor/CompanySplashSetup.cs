using TapBrawl.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.Editor
{
    public static class CompanySplashSetup
    {
        private const string BootScenePath = "Assets/Scenes/Boot.unity";
        private const string ShineShaderPath = "Assets/TextMesh Pro/Shaders/Tap_TMP_ShineSweep.shader";
        private static readonly Color SplashBackground = new(0.13725491f, 0.12156863f, 0.1254902f, 1f);

        [MenuItem("Tap/Setup Company Splash")]
        public static void SetupCompanySplash()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "Company Splash",
                    "Добавить экран Adiponya с бликом по тексту в Boot?",
                    "Выполнить",
                    "Отмена"))
                return;

            RunSetup();
        }

        public static void RunSetup()
        {
            Undo.SetCurrentGroupName("Setup Company Splash");
            var group = Undo.GetCurrentGroup();

            var bootScene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "В Boot нет Canvas.", "OK");
                return;
            }

            var existing = canvas.transform.Find("CompanySplash");
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);

            var splashRoot = CreateStretchPanel(canvas.transform, "CompanySplash");
            splashRoot.gameObject.AddComponent<CanvasGroup>();
            var backdrop = splashRoot.gameObject.AddComponent<Image>();
            backdrop.color = SplashBackground;
            backdrop.raycastTarget = true;

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(TextShineSweep));
            Undo.RegisterCreatedObjectUndo(titleGo, "Company Splash Title");
            titleGo.transform.SetParent(splashRoot, false);
            var titleRt = titleGo.GetComponent<RectTransform>();
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.5f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.sizeDelta = new Vector2(1200f, 220f);
            titleRt.anchoredPosition = Vector2.zero;

            var title = titleGo.GetComponent<TextMeshProUGUI>();
            title.text = "Adiponya";
            title.fontSize = 96;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.92f, 0.92f, 0.94f, 1f);
            title.raycastTarget = false;

            var shine = titleGo.GetComponent<TextShineSweep>();
            var shineShader = AssetDatabase.LoadAssetAtPath<Shader>(ShineShaderPath);
            var shineSo = new SerializedObject(shine);
            shineSo.FindProperty("shineShader").objectReferenceValue = shineShader;
            shineSo.ApplyModifiedPropertiesWithoutUndo();

            var splashView = splashRoot.gameObject.AddComponent<CompanySplashView>();
            var splashSo = new SerializedObject(splashView);
            splashSo.FindProperty("canvasGroup").objectReferenceValue = splashRoot.GetComponent<CanvasGroup>();
            splashSo.FindProperty("titleText").objectReferenceValue = title;
            splashSo.FindProperty("shineSweep").objectReferenceValue = shine;
            splashSo.FindProperty("companyName").stringValue = "Adiponya";
            splashSo.ApplyModifiedPropertiesWithoutUndo();

            splashRoot.SetAsLastSibling();
            splashRoot.gameObject.SetActive(false);

            var boot = Object.FindFirstObjectByType<BootScreenController>();
            if (boot != null)
            {
                var bootSo = new SerializedObject(boot);
                bootSo.FindProperty("companySplash").objectReferenceValue = splashView;
                bootSo.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(bootScene);
            EditorSceneManager.SaveScene(bootScene);
            Undo.CollapseUndoOperations(group);

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Готово", "Экран Adiponya добавлен в Boot.", "OK");
        }

        private static RectTransform CreateStretchPanel(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, name);
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            return rt;
        }
    }
}
