using TapBrawl.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TapBrawl.Editor
{
    public static class BootUpdateModalSetup
    {
        private const string BootScenePath = "Assets/Scenes/Boot.unity";

        [MenuItem("Tap/Setup Boot Update Modal")]
        public static void SetupBootUpdateModal()
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "Boot Update Modal",
                    "Создать модалку force-update в Boot и привязать к BootScreenController?",
                    "Выполнить",
                    "Отмена"))
                return;

            RunSetup();
        }

        public static void RunSetup()
        {
            Undo.SetCurrentGroupName("Setup Boot Update Modal");
            var group = Undo.GetCurrentGroup();

            var bootScene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Ошибка", "В Boot нет Canvas.", "OK");
                return;
            }

            var existing = canvas.GetComponentInChildren<UpdateRequiredModal>(true);
            if (existing != null)
                Undo.DestroyObjectImmediate(existing.gameObject);

            var modal = UpdateRequiredModal.EnsureOnCanvas(canvas.transform);
            modal.gameObject.SetActive(false);

            var boot = Object.FindFirstObjectByType<BootScreenController>();
            if (boot != null)
            {
                var so = new SerializedObject(boot);
                so.FindProperty("updateRequiredModal").objectReferenceValue = modal;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(bootScene);
            EditorSceneManager.SaveScene(bootScene);
            Undo.CollapseUndoOperations(group);

            if (!Application.isBatchMode)
                EditorUtility.DisplayDialog("Готово", "Модалка force-update добавлена в Boot.", "OK");
        }
    }
}
