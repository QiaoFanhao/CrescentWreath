using System;
using CrescentWreath.Client.Presentation;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class WorldCardView : MonoBehaviour
{
    public const float StandardCardWidth = 1f;
    public const float StandardCardHeight = 1f;
    public const float CardFaceHeight = 1.4f;
    public const float CardBodyThickness = 0.02f;
    private const float MaxPileBodyThickness = 0.16f;
    private const float PileBodyThicknessPerCard = 0.006f;
    public static readonly Vector3 DefaultCardSize = new(StandardCardWidth, CardBodyThickness, StandardCardHeight);
    public static readonly Vector3 CardBodyLocalScale = new(StandardCardWidth, CardBodyThickness, CardFaceHeight);

    [SerializeField]
    private Transform? cardBody;

    [SerializeField]
    private SpriteRenderer? frontSurface;

    [SerializeField]
    private SpriteRenderer? backSurface;

    [SerializeField]
    private GameObject? selectionOutline;

    [SerializeField]
    private GameObject? hoverHighlight;

    [SerializeField]
    private BoxCollider? boxCollider;

    [SerializeField]
    private TMPro.TextMeshPro? pileCountText;

    [SerializeField]
    private TMPro.TextMeshPro? activatedText;

    private WorldCardViewModel model = new();
    private Vector3 restLocalPosition;
    private bool highlighted;

    public event Action<WorldCardView>? Clicked;
    public event Action<WorldCardView>? DoubleClicked;

    public long CardInstanceNumericId => model.cardInstanceNumericId;
    public string DefinitionId => model.definitionId;
    public bool IsFaceUp => model.isFaceUp;
    public bool IsSelected => model.isSelected;
    public bool IsInteractable => model.isInteractable;
    public bool IsReadOnly => model.isReadOnly;
    public WorldCardDisplayMode DisplayMode => model.displayMode;
    public string TooltipTitle => model.tooltipTitle;
    public string TooltipDescription => model.tooltipDescription;
    public WorldCardViewModel CurrentModel => model.Clone();

    public void ConfigureForFactory(
        Transform newCardBody,
        SpriteRenderer newFrontSurface,
        SpriteRenderer newBackSurface,
        GameObject newSelectionOutline,
        GameObject newHoverHighlight,
        BoxCollider newBoxCollider,
        TMPro.TextMeshPro? newPileCountText = null,
        TMPro.TextMeshPro? newActivatedText = null)
    {
        cardBody = newCardBody;
        frontSurface = newFrontSurface;
        backSurface = newBackSurface;
        selectionOutline = newSelectionOutline;
        hoverHighlight = newHoverHighlight;
        boxCollider = newBoxCollider;
        pileCountText = newPileCountText;
        activatedText = newActivatedText;
        restLocalPosition = transform.localPosition;
    }

    private void Awake()
    {
        restLocalPosition = transform.localPosition;
        updateVisualState();
    }

    public void Bind(WorldCardViewModel newModel)
    {
        model = newModel.Clone();
        setSprite();
        updateVisualState();
    }

    public void SetFaceUp(bool isFaceUp)
    {
        model.isFaceUp = isFaceUp;
        updateVisualState();
    }

    public void SetSelected(bool selected)
    {
        model.isSelected = selected;
        updateVisualState();
    }

    public void SetInteractable(bool interactable)
    {
        model.isInteractable = interactable;
        updateVisualState();
    }

    public void SetReadOnly(bool readOnly)
    {
        model.isReadOnly = readOnly;
        updateVisualState();
    }

    public void SetHighlighted(bool isHighlighted)
    {
        highlighted = isHighlighted;
        updateVisualState();
    }

    public void SetRestLocalPosition(Vector3 newRestLocalPosition)
    {
        restLocalPosition = newRestLocalPosition;
        updateVisualState();
    }

    public void NotifyClicked()
    {
        Clicked?.Invoke(this);
    }

    public void NotifyDoubleClicked()
    {
        DoubleClicked?.Invoke(this);
    }

    public string BuildDebugDescription()
    {
        var side = model.isFaceUp ? "正面" : "背面";
        var mode = model.isReadOnly ? "只读" : model.isInteractable ? "可交互" : "禁用";
        var summonCost = model.baseSummonSigilCost.HasValue || model.effectiveSummonSigilCost.HasValue
            ? $"\n召唤费用: {formatNullableInt(model.baseSummonSigilCost)} -> {formatNullableInt(model.effectiveSummonSigilCost)}"
            : string.Empty;
        var summonable = model.baseSummonSigilCost.HasValue || model.effectiveSummonSigilCost.HasValue
            ? $"\n可召唤: {model.isSummonableByViewer} reason={model.summonUnavailableReasonKey}"
            : string.Empty;
        var overlay = model.overlayCardCount > 0 ? $"\n下方叠放: {model.overlayCardCount}" : string.Empty;
        var tooltip = string.IsNullOrWhiteSpace(model.tooltipTitle)
            ? string.Empty
            : $"\n状态说明: {model.tooltipTitle} - {model.tooltipDescription}";
        return $"{model.definitionId} #{model.cardInstanceNumericId}\n{side} / {mode}\nowner={model.ownerPlayerNumericId} zone={model.zoneKey} defensePlaced={model.isDefensePlacedOnField}{summonCost}{summonable}{overlay}{tooltip}\n显示模式: {model.displayMode}";
    }

    private void setSprite()
    {
        var frontSprite = BuffIconSpriteService.TryGetSprite(model.definitionId, out var buffSprite)
            ? buffSprite
            : CardArtService.GetSpriteForDefinition(model.definitionId);
        var backSprite = CardArtService.GetCardBackSprite();
        if (frontSurface is not null)
        {
            frontSurface.sprite = frontSprite ?? backSprite;
            fitSpriteToCard(frontSurface);
        }

        if (backSurface is not null)
        {
            backSurface.sprite = backSprite ?? frontSprite;
            fitSpriteToCard(backSurface);
        }
    }

    private void updateVisualState()
    {
        if (frontSurface is not null)
        {
            frontSurface.gameObject.SetActive(model.displayMode != WorldCardDisplayMode.emptyZone && model.isFaceUp);
            frontSurface.color = model.displayMode == WorldCardDisplayMode.disabled
                ? new Color(0.55f, 0.55f, 0.55f, 1f)
                : model.isInteractable || model.isReadOnly
                ? Color.white
                : new Color(0.55f, 0.55f, 0.55f, 1f);
        }

        if (backSurface is not null)
        {
            backSurface.gameObject.SetActive(model.displayMode != WorldCardDisplayMode.emptyZone && !model.isFaceUp);
        }

        if (selectionOutline is not null)
        {
            selectionOutline.SetActive(model.isSelected || model.overlayCardCount > 0 || model.isActivated);
        }

        if (hoverHighlight is not null)
        {
            hoverHighlight.SetActive(highlighted || model.isActivated);
        }

        transform.localPosition = highlighted
            ? restLocalPosition + Vector3.up * 0.12f
            : restLocalPosition;

        if (boxCollider is not null)
        {
            boxCollider.size = model.isDefensePlacedOnField
                ? new Vector3(1.48f, 0.1f, 1.08f)
                : new Vector3(1.08f, 0.1f, 1.48f);
            boxCollider.enabled = model.isInteractable || model.isReadOnly;
        }

        var defenseRotation = model.isDefensePlacedOnField ? 90f : 0f;
        if (cardBody is not null)
        {
            cardBody.localEulerAngles = new Vector3(0f, defenseRotation, 0f);
            applyPileBodyVisual(cardBody);
        }

        updatePileCountText(defenseRotation);
        updateActivatedText(defenseRotation);

        if (frontSurface is not null)
        {
            frontSurface.transform.localEulerAngles = new Vector3(90f, defenseRotation, 0f);
        }

        if (backSurface is not null)
        {
            backSurface.transform.localEulerAngles = new Vector3(90f, defenseRotation, 0f);
        }

        if (selectionOutline is not null)
        {
            selectionOutline.transform.localEulerAngles = new Vector3(0f, defenseRotation, 0f);
        }

        if (hoverHighlight is not null)
        {
            hoverHighlight.transform.localEulerAngles = new Vector3(0f, defenseRotation, 0f);
        }
    }

    private static void fitSpriteToCard(SpriteRenderer spriteRenderer)
    {
        var sprite = spriteRenderer.sprite;
        if (sprite is null)
        {
            spriteRenderer.transform.localScale = Vector3.one;
            return;
        }

        var bounds = sprite.bounds.size;
        var targetWidth = StandardCardWidth * 0.985f;
        var targetHeight = CardFaceHeight * 0.985f;
        var widthScale = bounds.x <= 0f ? 1f : targetWidth / bounds.x;
        var heightScale = bounds.y <= 0f ? 1f : targetHeight / bounds.y;
        spriteRenderer.transform.localScale = new Vector3(widthScale, heightScale, 1f);
    }

    private static string formatNullableInt(int? value)
    {
        return value.HasValue ? value.Value.ToString() : "(无)";
    }

    private void applyPileBodyVisual(Transform body)
    {
        var pileCount = Math.Max(1, model.pileCardCount);
        var isEmptyZone = model.displayMode == WorldCardDisplayMode.emptyZone;
        var isPile = isEmptyZone ||
                     model.displayMode == WorldCardDisplayMode.deckBack ||
                     model.zoneKey.Contains("Deck", StringComparison.OrdinalIgnoreCase) ||
                     model.zoneKey.Contains("deck", StringComparison.OrdinalIgnoreCase) ||
                     model.zoneKey.Contains("discard", StringComparison.OrdinalIgnoreCase) ||
                     model.zoneKey.Contains("gap", StringComparison.OrdinalIgnoreCase) ||
                     model.zoneKey.Contains("sakuraCakeZone", StringComparison.OrdinalIgnoreCase) ||
                     model.zoneKey.Contains("resolvedAnomaly", StringComparison.OrdinalIgnoreCase);
        var bodyRenderer = body.GetComponent<MeshRenderer>();
        if (bodyRenderer is not null)
        {
            bodyRenderer.enabled = isEmptyZone || (isPile && pileCount > 1);
        }

        if (!isPile)
        {
            body.localScale = CardBodyLocalScale;
            body.localPosition = Vector3.zero;
            return;
        }

        var thickness = Math.Min(MaxPileBodyThickness, CardBodyThickness + (pileCount - 1) * PileBodyThicknessPerCard);
        if (isEmptyZone)
        {
            thickness = CardBodyThickness;
        }

        body.localScale = new Vector3(CardBodyLocalScale.x, thickness, CardBodyLocalScale.z);
        // Keep the top surface below the sprite so the visible pile grows downward.
        body.localPosition = Vector3.down * ((thickness - CardBodyThickness) * 0.5f);
    }

    private void updatePileCountText(float defenseRotation)
    {
        if (pileCountText is null)
        {
            return;
        }

        var shouldShow =
            model.overlayCardCount > 0 ||
            model.displayMode == WorldCardDisplayMode.emptyZone ||
            model.displayMode == WorldCardDisplayMode.deckBack ||
            model.zoneKey.Contains("Deck", StringComparison.OrdinalIgnoreCase) ||
            model.zoneKey.Contains("deck", StringComparison.OrdinalIgnoreCase) ||
            model.zoneKey.Contains("discard", StringComparison.OrdinalIgnoreCase) ||
            model.zoneKey.Contains("gap", StringComparison.OrdinalIgnoreCase) ||
            model.zoneKey.Contains("sakuraCakeZone", StringComparison.OrdinalIgnoreCase) ||
            model.zoneKey.Contains("resolvedAnomaly", StringComparison.OrdinalIgnoreCase);
        pileCountText.gameObject.SetActive(shouldShow);
        if (!shouldShow)
        {
            return;
        }

        pileCountText.text = model.overlayCardCount > 0
            ? $"下方x{Math.Max(0, model.overlayCardCount)}"
            : formatPileCountText();
        pileCountText.transform.localEulerAngles = new Vector3(90f, defenseRotation, 0f);
    }

    private void updateActivatedText(float defenseRotation)
    {
        if (activatedText is null)
        {
            return;
        }

        activatedText.gameObject.SetActive(model.isActivated);
        if (!model.isActivated)
        {
            return;
        }

        activatedText.text = "已启动";
        activatedText.transform.localEulerAngles = new Vector3(90f, defenseRotation, 0f);
    }

    private string formatPileCountText()
    {
        var countText = Math.Max(0, model.pileCardCount).ToString();
        if (model.zoneKey.Contains("sakuraCakeZone", StringComparison.OrdinalIgnoreCase))
        {
            return $"樱花饼x{countText}";
        }

        return $"x{countText}";
    }
}
}
