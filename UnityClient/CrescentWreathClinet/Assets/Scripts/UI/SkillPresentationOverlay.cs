using CrescentWreath.Client.Net;
using CrescentWreath.Client.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class SkillPresentationOverlay : MonoBehaviour
{
    private CanvasGroup? canvasGroup;
    private RectTransform? cardContainer;
    private Image? dimmer;
    private Image? characterCardImage;
    private Image? glowFrame;
    private TMP_Text? playerNameText;
    private TMP_Text? characterNameText;
    private TMP_Text? skillNameText;
    private TMP_Text? skillTypeText;
    private TMP_Text? effectLabelText;

    public void Configure(Canvas overlayCanvas, TMP_FontAsset? font)
    {
        if (canvasGroup is not null)
        {
            return;
        }

        transform.SetParent(overlayCanvas.transform, false);
        var root = (RectTransform)transform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.SetAsLastSibling();

        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        dimmer = createImage("Dimmer", transform, Vector2.zero, Vector2.one);
        dimmer.color = new Color(0.005f, 0.015f, 0.025f, 0.68f);
        dimmer.raycastTarget = false;

        var accent = createImage("AccentLine", transform, new Vector2(0f, 0.17f), new Vector2(1f, 0.185f));
        accent.color = new Color(0.95f, 0.64f, 0.16f, 0.95f);
        accent.raycastTarget = false;

        cardContainer = createRect("CharacterCardContainer", transform);
        cardContainer.anchorMin = new Vector2(0.22f, 0.5f);
        cardContainer.anchorMax = new Vector2(0.22f, 0.5f);
        cardContainer.sizeDelta = new Vector2(330f, 462f);
        cardContainer.anchoredPosition = new Vector2(-Screen.width * 0.42f, 0f);

        glowFrame = createImage("GlowFrame", cardContainer, Vector2.zero, Vector2.one);
        glowFrame.color = new Color(1f, 0.72f, 0.2f, 0.32f);
        glowFrame.rectTransform.offsetMin = new Vector2(-10f, -10f);
        glowFrame.rectTransform.offsetMax = new Vector2(10f, 10f);
        glowFrame.raycastTarget = false;

        characterCardImage = createImage("CharacterCardImage", cardContainer, Vector2.zero, Vector2.one);
        characterCardImage.preserveAspect = true;
        characterCardImage.raycastTarget = false;

        var textPanel = createImage("TextPanel", transform, new Vector2(0.43f, 0.28f), new Vector2(0.91f, 0.72f));
        textPanel.color = new Color(0.015f, 0.07f, 0.09f, 0.9f);
        textPanel.raycastTarget = false;

        playerNameText = createText("PlayerNameText", textPanel.transform, font, 22f, FontStyles.Normal);
        setTextRect(playerNameText.rectTransform, 0.08f, 0.73f, 0.92f, 0.9f);
        characterNameText = createText("CharacterNameText", textPanel.transform, font, 34f, FontStyles.Bold);
        setTextRect(characterNameText.rectTransform, 0.08f, 0.52f, 0.92f, 0.76f);
        skillTypeText = createText("SkillTypeText", textPanel.transform, font, 20f, FontStyles.Bold);
        setTextRect(skillTypeText.rectTransform, 0.08f, 0.36f, 0.92f, 0.52f);
        skillNameText = createText("SkillNameText", textPanel.transform, font, 42f, FontStyles.Bold);
        setTextRect(skillNameText.rectTransform, 0.08f, 0.1f, 0.92f, 0.39f);
        effectLabelText = createText("EffectLabel", transform, font, 18f, FontStyles.Bold);
        setTextRect(effectLabelText.rectTransform, 0.43f, 0.18f, 0.91f, 0.25f);
        effectLabelText.alignment = TextAlignmentOptions.Right;

        gameObject.SetActive(false);
    }

    public void Bind(
        ProjectionSkillPresentationEventViewModel skillEvent,
        string playerName,
        string characterName,
        string skillName,
        string skillTypeRaw,
        string vfxKey,
        float dimAlpha)
    {
        transform.SetAsLastSibling();
        characterCardImage!.sprite = CardArtService.GetSpriteForDefinition(skillEvent.characterDefinitionId);
        characterCardImage.color = characterCardImage.sprite is null ? new Color(0.15f, 0.2f, 0.22f, 1f) : Color.white;
        playerNameText!.text = playerName;
        characterNameText!.text = characterName;
        skillNameText!.text = skillName;
        skillTypeText!.text = resolveTypeLabel(skillEvent.triggerTypeKey, skillTypeRaw);
        effectLabelText!.text = $"技能事件 · {skillEvent.skillKey}";
        dimmer!.color = new Color(0.005f, 0.015f, 0.025f, Mathf.Clamp01(dimAlpha));
        glowFrame!.color = resolveAccentColor(skillEvent.triggerTypeKey, vfxKey, 0.36f);
        skillTypeText.color = resolveAccentColor(skillEvent.triggerTypeKey, vfxKey, 1f);
    }

    public void SetVisual(float alpha, float enterProgress, float exitProgress, float cardScale)
    {
        gameObject.SetActive(alpha > 0.001f);
        canvasGroup!.alpha = Mathf.Clamp01(alpha);
        var visibleX = -Screen.width * 0.06f;
        var hiddenX = -Screen.width * 0.42f;
        var x = Mathf.Lerp(hiddenX, visibleX, Mathf.SmoothStep(0f, 1f, enterProgress));
        x = Mathf.Lerp(x, -Screen.width * 0.34f, Mathf.SmoothStep(0f, 1f, exitProgress));
        cardContainer!.anchoredPosition = new Vector2(x, 0f);
        var pulse = 1f + Mathf.Sin(Time.unscaledTime * 12f) * 0.018f;
        cardContainer.localScale = Vector3.one * Mathf.Lerp(1f, cardScale * pulse, enterProgress);
    }

    public void Hide()
    {
        if (canvasGroup is not null)
        {
            canvasGroup.alpha = 0f;
        }
        gameObject.SetActive(false);
    }

    private static string resolveTypeLabel(string triggerTypeKey, string skillTypeRaw)
    {
        return triggerTypeKey switch
        {
            "response" => "响应技能",
            "automatic" => "自动触发",
            "arrival" => "降临触发",
            "passiveTriggered" => "被动触发",
            _ => string.IsNullOrWhiteSpace(skillTypeRaw) ? "技能发动" : $"{skillTypeRaw}技能发动",
        };
    }

    private static Color resolveAccentColor(string triggerTypeKey, string vfxKey, float alpha)
    {
        if (vfxKey.Contains("Blue", System.StringComparison.OrdinalIgnoreCase))
        {
            return new Color(0.52f, 0.78f, 1f, alpha);
        }

        if (vfxKey.Contains("Cyan", System.StringComparison.OrdinalIgnoreCase))
        {
            return new Color(0.36f, 0.95f, 0.86f, alpha);
        }

        return triggerTypeKey switch
        {
            "response" => new Color(0.52f, 0.78f, 1f, alpha),
            "automatic" or "arrival" or "passiveTriggered" => new Color(0.36f, 0.95f, 0.86f, alpha),
            _ => new Color(1f, 0.72f, 0.22f, alpha),
        };
    }

    private static RectTransform createRect(string name, Transform parent)
    {
        var value = new GameObject(name, typeof(RectTransform));
        value.transform.SetParent(parent, false);
        return (RectTransform)value.transform;
    }

    private static Image createImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        var value = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        value.transform.SetParent(parent, false);
        var rect = (RectTransform)value.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return value.GetComponent<Image>();
    }

    private static TMP_Text createText(string name, Transform parent, TMP_FontAsset? font, float size, FontStyles style)
    {
        var value = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        value.transform.SetParent(parent, false);
        var text = value.GetComponent<TextMeshProUGUI>();
        if (font is not null)
        {
            text.font = font;
        }
        text.fontSize = size;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private static void setTextRect(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
}
