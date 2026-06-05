using TapBrawl.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.Editor
{
    public static class LobbyCurrencyBarSetup
    {
        private const string LobbyScenePath = "Assets/Scenes/Lobby.unity";

        [MenuItem("Tap/Setup Lobby Currency Bar")]
        public static void SetupLobbyCurrencyBar()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "Lobby Currency Bar",
                    "Создать видимую панель валюты на Canvas лобби?",
                    "Выполнить",
                    "Отмена"))
                return;

            RunSetup();
        }

        public static void RunSetup()
        {
            Undo.SetCurrentGroupName("Setup Lobby Currency Bar");
            var group = Undo.GetCurrentGroup();

            var lobbyScene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
            var canvas = FindLobbyCanvas();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "В Lobby нет Canvas.", "OK");
                return;
            }

            var bar = EnsureCurrencyBar(canvas.transform);
            WireLobbyScreenController(bar);

            EditorSceneManager.MarkSceneDirty(lobbyScene);
            EditorSceneManager.SaveScene(lobbyScene);
            Undo.CollapseUndoOperations(group);

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Готово", "LobbyCurrencyBar создан и подключён.", "OK");
        }

        private static Canvas? FindLobbyCanvas()
        {
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (c.isRootCanvas && c.gameObject.scene.path == LobbyScenePath)
                    return c;
            }

            return null;
        }

        private static GameObject EnsureCurrencyBar(Transform canvasTransform)
        {
            var existing = canvasTransform.Find("LobbyCurrencyBar");
            if (existing != null)
                return existing.gameObject;

            var bar = new GameObject("LobbyCurrencyBar", typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(bar, "Create LobbyCurrencyBar");
            bar.transform.SetParent(canvasTransform, false);

            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -112f);
            rt.sizeDelta = new Vector2(0f, 48f);

            var img = bar.GetComponent<Image>();
            img.color = new Color(0.12f, 0.14f, 0.2f, 0.85f);
            img.raycastTarget = false;

            CreateCurrencyText(bar.transform, "CurrencyText", new Vector2(16f, 0f), new Vector2(-16f, 0f));
            return bar;
        }

        private static Text CreateCurrencyText(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            var text = go.GetComponent<Text>();
            text.text = "Монеты: 0 · Adipoint: 0 (≈ 0 монет)";
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = UiModalStyle.ProfileAccentTextColor;
            text.raycastTarget = false;
            return text;
        }

        private static void WireLobbyScreenController(GameObject bar)
        {
            var controller = Object.FindFirstObjectByType<LobbyScreenController>();
            if (controller == null)
                return;

            var currencyText = bar.transform.Find("CurrencyText")?.GetComponent<Text>();
            var so = new SerializedObject(controller);
            so.FindProperty("lobbyCurrencyText").objectReferenceValue = currencyText;
            so.FindProperty("coinsText").objectReferenceValue = null;
            so.FindProperty("gemsText").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
