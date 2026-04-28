using UnityEngine;
using UnityEngine.SceneManagement;

namespace CrescentWreath.Client.Net
{
public static class SocketDebugSceneBootstrap
{
    private const string SocketDebugSceneName = "SocketDebug";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ensureDebugPanel()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (!string.Equals(activeScene.name, SocketDebugSceneName, System.StringComparison.Ordinal))
        {
            return;
        }

        if (Object.FindObjectOfType<SocketDebugPanel>() is not null)
        {
            return;
        }

        var panelObject = new GameObject("SocketDebugPanel");
        panelObject.AddComponent<SocketDebugPanel>();
    }
}
}
