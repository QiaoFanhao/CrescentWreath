using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class WorldCardTooltipView : MonoBehaviour
{
    private const string ThemeResourcePath = "UI/BattlefieldUiTheme";
    private static WorldCardTooltipView? instance;

    private RectTransform? panel;
    private CanvasGroup? canvasGroup;
    private TextMeshProUGUI? titleText;
    private TextMeshProUGUI? descriptionText;
    private bool visible;

    public static void Show(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
        {
            Hide();
            return;
        }

        var view = getOrCreate();
        view.setText(title, description);
        view.setVisible(true);
    }

    public static void Hide()
    {
        if (instance is null)
        {
            return;
        }

        instance.setVisible(false);
    }

    private static WorldCardTooltipView getOrCreate()
    {
        if (instance is not null)
        {
            return instance;
        }

        var rootObject = new GameObject(
            "WorldCardTooltipCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(WorldCardTooltipView));
        DontDestroyOnLoad(rootObject);

        var canvas = rootObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5000;

        var scaler = rootObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        instance = rootObject.GetComponent<WorldCardTooltipView>();
        instance.build();
        return instance;
    }

    private void Awake()
    {
        instance = this;
        build();
        setVisible(false);
    }

    private void Update()
    {
        if (!visible || panel is null)
        {
            return;
        }

        var mousePosition = Mouse.current?.position.ReadValue() ?? Vector2.zero;
        var offset = new Vector2(24f, -24f);
        panel.position = mousePosition + offset;
    }

    private void build()
    {
        if (panel is not null)
        {
            return;
        }

        var theme = Resources.Load<BattlefieldUiTheme>(ThemeResourcePath);
        var font = theme?.PrimaryFont;
        var panelObject = new GameObject(
            "TooltipPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));
        panelObject.transform.SetParent(transform, false);
        panel = panelObject.GetComponent<RectTransform>();
        panel.sizeDelta = new Vector2(360f, 112f);
        panel.pivot = new Vector2(0f, 1f);

        var image = panelObject.GetComponent<Image>();
        image.color = new Color(0.02f, 0.08f, 0.11f, 0.94f);

        canvasGroup = panelObject.GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        titleText = createText(panel, "Title", 18f, FontStyles.Bold, font);
        titleText.rectTransform.anchorMin = new Vector2(0.04f, 0.58f);
        titleText.rectTransform.anchorMax = new Vector2(0.96f, 0.94f);
        titleText.color = new Color(1f, 0.83f, 0.32f, 1f);

        descriptionText = createText(panel, "Description", 15f, FontStyles.Normal, font);
        descriptionText.rectTransform.anchorMin = new Vector2(0.04f, 0.08f);
        descriptionText.rectTransform.anchorMax = new Vector2(0.96f, 0.58f);
        descriptionText.color = Color.white;
    }

    private static TextMeshProUGUI createText(
        Transform parent,
        string name,
        float fontSize,
        FontStyles fontStyle,
        TMP_FontAsset? font)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        if (font is not null)
        {
            text.font = font;
        }

        var rectTransform = text.rectTransform;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        return text;
    }

    private void setText(string title, string description)
    {
        if (titleText is not null)
        {
            titleText.text = title;
        }

        if (descriptionText is not null)
        {
            descriptionText.text = description;
        }
    }

    private void setVisible(bool shouldBeVisible)
    {
        visible = shouldBeVisible;
        if (canvasGroup is null)
        {
            return;
        }

        canvasGroup.alpha = shouldBeVisible ? 1f : 0f;
    }
}
}
