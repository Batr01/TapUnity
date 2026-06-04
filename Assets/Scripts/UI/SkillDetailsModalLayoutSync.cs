using UnityEngine;
using UnityEngine.UI;

namespace TapBrawl.UI
{
    [DisallowMultipleComponent]
    public sealed class SkillDetailsModalLayoutSync : MonoBehaviour
    {
        private void Awake()
        {
            var content = transform.Find("Content");
            if (content != null)
            {
                UiModalStyle.ApplyPanelRect((RectTransform)content);
                var img = content.GetComponent<Image>();
                if (img == null)
                    img = content.gameObject.AddComponent<Image>();
                UiModalStyle.ApplyPanel(img);
            }

            UiModalStyle.ApplyLobbyModalChrome(transform);
        }
    }
}
