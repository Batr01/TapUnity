using UnityEngine;

namespace TapBrawl.Network
{
    public static class BackendConfigLocator
    {
        private const string DefaultAssetPath = "Assets/ScriptableObjects/BackendConfig.asset";

        public static BackendConfig? Resolve(BackendConfig? assigned)
        {
            if (assigned != null)
                return assigned;

#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<BackendConfig>(DefaultAssetPath);
#else
            var all = Resources.FindObjectsOfTypeAll<BackendConfig>();
            return all.Length > 0 ? all[0] : null;
#endif
        }
    }
}
