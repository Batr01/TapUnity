using UnityEngine;
using UnityEngine.Rendering;

namespace Tap.Core
{
    /// <summary>
    /// Prevents SRP runtime debugger from polling legacy Input APIs.
    /// This keeps projects running when only the new Input System is enabled.
    /// </summary>
    internal static class RenderingDebugInputCompatibility
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void DisableRuntimeRenderingDebuggerInput()
        {
            DebugManager.instance.enableRuntimeUI = false;
            DebugManager.instance.displayRuntimeUI = false;
            DebugManager.instance.displayPersistentRuntimeUI = false;
        }
    }
}
