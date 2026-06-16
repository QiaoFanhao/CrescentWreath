using CrescentWreath.Client.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CrescentWreath.Client.UI
{
public static class GameClientSceneBootstrap
{
    private const string GameClientSceneName = "GameClient";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ensureGameClientRoot()
    {
        if (!string.Equals(
                SceneManager.GetActiveScene().name,
                GameClientSceneName,
                System.StringComparison.Ordinal))
        {
            return;
        }

        var existingRoot = Object.FindFirstObjectByType<GameClientRoot>();
        if (existingRoot is not null)
        {
            BattlefieldSceneSkeletonFactory.Ensure(existingRoot);
            return;
        }

        var rootObject = new GameObject("GameClientRoot");
        var cardArtService = rootObject.AddComponent<CardArtService>();
        var projectionViewState = rootObject.AddComponent<ProjectionViewState>();
        var gameClientRoot = rootObject.AddComponent<GameClientRoot>();

        // The authored GameClient scene owns the formal TMP battlefield hierarchy.
        // This fallback only keeps an empty, safe root for damaged or stripped scenes.
        var overlayObject = new GameObject("LegacyInteractionOverlayFallback");
        overlayObject.transform.SetParent(rootObject.transform, false);
        overlayObject.SetActive(false);
        var interactionOverlay = overlayObject.AddComponent<InteractionOverlay>();

        if (Object.FindFirstObjectByType<EventSystem>() is null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        gameClientRoot.Configure(cardArtService, projectionViewState, interactionOverlay);
        BattlefieldSceneSkeletonFactory.Ensure(gameClientRoot);
    }
}
}
