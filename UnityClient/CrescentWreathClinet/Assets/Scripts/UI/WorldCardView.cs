using System;
using System.Collections;
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
    private Transform? visualRoot;

    [SerializeField]
    private Transform? cardBody;

    [SerializeField]
    private SpriteRenderer? frontSurface;

    [SerializeField]
    private SpriteRenderer? backSurface;

    [SerializeField]
    private Transform? effectAnchor;

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
    private Quaternion restLocalRotation = Quaternion.identity;
    private Vector3 restLocalScale = Vector3.one;
    private Coroutine? motionCoroutine;
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
    public Transform EffectAnchor => effectAnchor != null ? effectAnchor : visualRoot != null ? visualRoot : transform;
    public string VisualKey { get; private set; } = string.Empty;
    public string ZoneViewKey { get; private set; } = string.Empty;
    public bool IsMotionActive => motionCoroutine is not null;
    public bool CanInspect =>
        model.displayMode != WorldCardDisplayMode.emptyZone &&
        model.displayMode != WorldCardDisplayMode.deckBack &&
        !model.zoneKey.Contains("Deck", StringComparison.OrdinalIgnoreCase) &&
        !model.zoneKey.Contains("deck", StringComparison.OrdinalIgnoreCase);

    public void ConfigureForFactory(
        Transform newVisualRoot,
        Transform newCardBody,
        SpriteRenderer newFrontSurface,
        SpriteRenderer newBackSurface,
        Transform newEffectAnchor,
        GameObject newSelectionOutline,
        GameObject newHoverHighlight,
        BoxCollider newBoxCollider,
        TMPro.TextMeshPro? newPileCountText = null,
        TMPro.TextMeshPro? newActivatedText = null)
    {
        visualRoot = newVisualRoot;
        cardBody = newCardBody;
        frontSurface = newFrontSurface;
        backSurface = newBackSurface;
        effectAnchor = newEffectAnchor;
        selectionOutline = newSelectionOutline;
        hoverHighlight = newHoverHighlight;
        boxCollider = newBoxCollider;
        pileCountText = newPileCountText;
        activatedText = newActivatedText;
        restLocalPosition = transform.localPosition;
        restLocalRotation = transform.localRotation;
        restLocalScale = transform.localScale;
    }

    public void SetVisualContext(string visualKey, string zoneViewKey)
    {
        VisualKey = visualKey ?? string.Empty;
        ZoneViewKey = zoneViewKey ?? string.Empty;
    }

    private void Awake()
    {
        restLocalPosition = transform.localPosition;
        restLocalRotation = transform.localRotation;
        restLocalScale = transform.localScale;
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
        if (!Application.isPlaying || motionCoroutine is null)
        {
            applyDisplayedPose(currentRestPose());
        }

        updateVisualState();
    }

    public void SnapTo(CardPose pose)
    {
        stopMotion();
        restLocalPosition = pose.Position;
        restLocalRotation = pose.Rotation;
        restLocalScale = pose.Scale;
        applyDisplayedPose(currentRestPose());
        updateVisualState();
    }

    public void AnimateTo(CardPose targetPose, float duration)
    {
        AnimateTo(targetPose, duration, CardAnimationSettings.Default, 0f);
    }

    public void AnimateTo(CardPose targetPose, float duration, CardAnimationSettings settings, float delay = 0f)
    {
        AnimateTo(targetPose, duration, settings, delay, settings.moveHeightArc, 1f);
    }

    public void AnimateTo(
        CardPose targetPose,
        float duration,
        CardAnimationSettings settings,
        float delay,
        float arcHeight,
        float scalePeak)
    {
        restLocalPosition = targetPose.Position;
        restLocalRotation = targetPose.Rotation;
        restLocalScale = targetPose.Scale;

        if (!Application.isPlaying || duration <= 0f)
        {
            stopMotion();
            applyDisplayedPose(currentRestPose());
            updateVisualState();
            return;
        }

        stopMotion();
        motionCoroutine = StartCoroutine(animateToRoutine(
            currentDisplayedPose(),
            currentRestPose(),
            duration,
            delay,
            settings,
            arcHeight,
            scalePeak));
    }

    public void PlayEnter(CardPose targetPose, CardPose? spawnPose = null)
    {
        PlayEnter(targetPose, spawnPose, CardAnimationSettings.Default, 0f);
    }

    public void PlayEnter(CardPose targetPose, CardPose? spawnPose, CardAnimationSettings settings, float delay = 0f)
    {
        PlayEnterFromSource(targetPose, spawnPose, settings, delay, settings.enterDuration);
    }

    public void PlayEnterFromSource(
        CardPose targetPose,
        CardPose? spawnPose,
        CardAnimationSettings settings,
        float delay = 0f,
        float duration = -1f)
    {
        stopMotion();
        restLocalPosition = targetPose.Position;
        restLocalRotation = targetPose.Rotation;
        restLocalScale = targetPose.Scale;

        var initialPose = spawnPose ?? new CardPose(
            targetPose.Position + Vector3.up * settings.enterHeightOffset,
            targetPose.Rotation,
            targetPose.Scale * 0.86f);
        applyDisplayedPose(initialPose);

        var activeDuration = duration >= 0f ? duration : settings.enterDuration;
        if (!Application.isPlaying || activeDuration <= 0f)
        {
            applyDisplayedPose(currentRestPose());
            updateVisualState();
            return;
        }

        motionCoroutine = StartCoroutine(animateToRoutine(
            initialPose,
            currentRestPose(),
            activeDuration,
            delay,
            settings,
            settings.arcHeightLarge,
            settings.flightScalePeak));
    }

    public void PlayExit(Action onCompleted)
    {
        PlayExit(onCompleted, CardAnimationSettings.Default, 0f);
    }

    public void PlayExit(Action onCompleted, CardAnimationSettings settings, float delay = 0f)
    {
        if (!Application.isPlaying || settings.exitDuration <= 0f)
        {
            stopMotion();
            onCompleted();
            return;
        }

        stopMotion();
        var startPose = currentDisplayedPose();
        var exitPose = new CardPose(
            startPose.Position + Vector3.up * settings.exitHeightOffset,
            startPose.Rotation,
            startPose.Scale * 0.82f);
        motionCoroutine = StartCoroutine(animateToRoutine(
            startPose,
            exitPose,
            settings.exitDuration,
            delay,
            settings,
            settings.arcHeightSmall,
            1f,
            onCompleted));
    }

    public void PlayExitTo(CardPose exitPose, Action onCompleted, CardAnimationSettings settings, float delay = 0f)
    {
        if (!Application.isPlaying || settings.exitToSinkDuration <= 0f)
        {
            stopMotion();
            onCompleted();
            return;
        }

        stopMotion();
        var startPose = currentDisplayedPose();
        motionCoroutine = StartCoroutine(animateToRoutine(
            startPose,
            exitPose,
            settings.exitToSinkDuration,
            delay,
            settings,
            settings.arcHeightSmall,
            1f,
            onCompleted));
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

    public CardInspectViewModel BuildInspectViewModel()
    {
        var isPublicFront = model.isFaceUp;
        var definitionId = isPublicFront ? model.definitionId : string.Empty;
        var displayEntry = isPublicFront ? CardDisplayCatalogService.TryGetEntry(model.definitionId) : null;
        return new CardInspectViewModel
        {
            cardInstanceNumericId = model.cardInstanceNumericId,
            definitionId = definitionId,
            displayName = isPublicFront ? displayEntry?.name ?? CardNameLocalizer.Localize(model.definitionId) : "背面 / 未知卡牌",
            attributeText = isPublicFront ? CardDisplayCatalogService.BuildAttributeText(model.definitionId) : string.Empty,
            rulesText = isPublicFront ? CardDisplayCatalogService.BuildRulesText(model.definitionId) : string.Empty,
            ownerPlayerNumericId = model.ownerPlayerNumericId,
            zoneKey = model.zoneKey,
            isFaceUp = isPublicFront,
            isDefensePlacedOnField = model.isDefensePlacedOnField,
            baseSummonSigilCost = model.baseSummonSigilCost,
            effectiveSummonSigilCost = model.effectiveSummonSigilCost,
            isSummonableByViewer = model.isSummonableByViewer,
            summonUnavailableReasonKey = model.summonUnavailableReasonKey,
            artworkSprite = isPublicFront
                ? BuffIconSpriteService.TryGetSprite(model.definitionId, out var buffSprite)
                    ? buffSprite
                    : CardArtService.GetSpriteForDefinition(model.definitionId)
                : CardArtService.GetCardBackSprite(),
        };
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

        if (visualRoot is not null)
        {
            visualRoot.localPosition = highlighted
                ? Vector3.up * 0.12f
                : Vector3.zero;
        }

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

        if (effectAnchor is not null)
        {
            // VFX uses this stable card-space anchor instead of the sprite surface axes.
            effectAnchor.localPosition = new Vector3(0f, 0.065f, 0f);
            effectAnchor.localEulerAngles = new Vector3(0f, defenseRotation, 0f);
            effectAnchor.localScale = new Vector3(StandardCardWidth, 1f, CardFaceHeight);
        }
    }

    private void OnDrawGizmosSelected()
    {
        var anchor = effectAnchor != null ? effectAnchor : transform;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(anchor.position, 0.055f);
        Gizmos.DrawLine(anchor.position, anchor.position + anchor.up * 0.28f);
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

    private IEnumerator animateToRoutine(
        CardPose from,
        CardPose to,
        float duration,
        float delay,
        CardAnimationSettings settings,
        float arcHeight,
        float scalePeak,
        Action? onCompleted = null)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var eased = EaseOutCubic(duration <= 0f ? 1f : elapsed / duration);
            var position = Vector3.LerpUnclamped(from.Position, to.Position, eased);
            if (settings.useArcMotion && arcHeight > 0f)
            {
                position += Vector3.up * (Mathf.Sin(eased * Mathf.PI) * arcHeight);
            }

            var scale = Vector3.LerpUnclamped(from.Scale, to.Scale, eased);
            if (scalePeak > 1f)
            {
                scale *= 1f + (scalePeak - 1f) * Mathf.Sin(eased * Mathf.PI);
            }

            applyDisplayedPose(new CardPose(
                position,
                Quaternion.SlerpUnclamped(from.Rotation, to.Rotation, eased),
                scale));
            yield return null;
        }

        applyDisplayedPose(to);
        motionCoroutine = null;
        onCompleted?.Invoke();
    }

    private void stopMotion()
    {
        if (motionCoroutine is null)
        {
            return;
        }

        StopCoroutine(motionCoroutine);
        motionCoroutine = null;
    }

    private CardPose currentDisplayedPose()
    {
        return new CardPose(transform.localPosition, transform.localRotation, transform.localScale);
    }

    private CardPose currentRestPose()
    {
        return new CardPose(
            restLocalPosition,
            restLocalRotation,
            restLocalScale);
    }

    private void applyDisplayedPose(CardPose pose)
    {
        transform.localPosition = pose.Position;
        transform.localRotation = pose.Rotation;
        transform.localScale = pose.Scale;
    }

    public static float EaseOutCubic(float t)
    {
        t = Mathf.Clamp01(t);
        return 1f - Mathf.Pow(1f - t, 3f);
    }
}
}


