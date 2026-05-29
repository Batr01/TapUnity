using System.IO;
using UnityEditor;
using UnityEngine;

namespace TapBrawl.Editor
{
    public static class TapBuildPaths
    {
        public static string ExternalBuildsDir =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Builds"));

        [MenuItem("Tap/Build/Open External Builds Folder")]
        public static void OpenBuildsFolder()
        {
            var dir = ExternalBuildsDir;
            Directory.CreateDirectory(dir);
            EditorUtility.RevealInFinder(dir);
            Debug.Log($"Собирайте APK/AAB сюда: {dir}");
        }
    }
}
