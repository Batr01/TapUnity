using System;
using System.Collections.Generic;
using UnityEngine;

namespace TapBrawl.Avatars
{
    public static class AvatarIds
    {
        public const string Default = "default";
        public const string Blue = "blue";
        public const string Purple = "purple";
    }

    [CreateAssetMenu(fileName = "AvatarCatalog", menuName = "TapBrawl/Avatar Catalog")]
    public sealed class AvatarCatalogAsset : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string id;
            public Sprite sprite;
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public IReadOnlyList<Entry> Entries => entries;

        public Sprite? GetSprite(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].id == id)
                    return entries[i].sprite;
            }

            return null;
        }

        public static AvatarCatalogAsset? LoadDefault() =>
            Resources.Load<AvatarCatalogAsset>("Avatars/AvatarCatalog");
    }
}
