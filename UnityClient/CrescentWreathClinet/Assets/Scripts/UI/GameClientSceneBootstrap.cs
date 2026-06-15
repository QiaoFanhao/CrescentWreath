using CrescentWreath.Client.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
                System.StringComparison.Ordinal) ||
            Object.FindObjectOfType<GameClientRoot>() is not null)
        {
            return;
        }

        var rootObject = new GameObject("GameClientRoot");
        var cardArtService = rootObject.AddComponent<CardArtService>();
        var projectionViewState = rootObject.AddComponent<ProjectionViewState>();
        var gameClientRoot = rootObject.AddComponent<GameClientRoot>();

        var canvasObject = new GameObject(
            "GameCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var overlayObject = new GameObject(
            "InteractionOverlay",
            typeof(RectTransform),
            typeof(Image),
            typeof(CanvasGroup),
            typeof(InteractionOverlay));
        overlayObject.transform.SetParent(canvasObject.transform, false);
        var overlayRect = (RectTransform)overlayObject.transform;
        overlayRect.anchorMin = new Vector2(0.5f, 0.5f);
        overlayRect.anchorMax = new Vector2(0.5f, 0.5f);
        overlayRect.sizeDelta = new Vector2(620f, 280f);
        overlayObject.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.96f);

        var title = createText("Title", overlayObject.transform, 24);
        title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = new Vector2(0f, -32f);
        title.rectTransform.sizeDelta = new Vector2(580f, 42f);

        var detail = createText("Detail", overlayObject.transform, 18);
        detail.alignment = TextAnchor.UpperLeft;
        detail.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        detail.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        detail.rectTransform.anchoredPosition = new Vector2(0f, -92f);
        detail.rectTransform.sizeDelta = new Vector2(580f, 160f);

        var canvasGroup = overlayObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        var interactionOverlay = overlayObject.GetComponent<InteractionOverlay>();
        interactionOverlay.Configure(canvasGroup, title, detail);

        if (Object.FindObjectOfType<EventSystem>() is null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        gameClientRoot.Configure(cardArtService, projectionViewState, interactionOverlay);
    }

    private static Text createText(string name, Transform parent, int fontSize)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }
}
}
