using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using CrescentWreath.Client.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldEffectPresentationTimeline : MonoBehaviour
{
    private const string DefaultCatalogResourcesPath = "EffectPresentation/DefaultEffectPresentationCatalog";
    private const string DefaultSettingsResourcesPath = "EffectPresentation/DefaultEffectPresentationSettings";
    private const float CardActivatePresentationDelaySeconds = 0.24f;
    private static readonly Regex DefinitionIdPattern = new(@"\b[ACTS]\d{3}(?:-\d+)?\b", RegexOptions.Compiled);
    private static readonly Regex CardInstancePattern = new(@"\bcard=(\d+)\b", RegexOptions.Compiled);

    [SerializeField]
    private EffectPresentationCatalog? catalog;

    [SerializeField]
    private EffectPresentationSettings? settings;

    private readonly HashSet<string> shownEventKeys = new();
    private EffectVfxPlayer? vfxPlayer;
    private EffectSfxPlayer? sfxPlayer;
    private BattlefieldView? battlefieldView;
    private Transform? tableCenter;
    private Canvas? overlayCanvas;
    private TMP_FontAsset? primaryFont;
    private readonly Queue<TMP_Text> toastPool = new();
    private readonly List<TMP_Text> activeToasts = new();
    private bool hasSeenProjection;

    public void Configure(
        BattlefieldView newBattlefieldView,
        Transform? newTableCenter,
        Canvas? newOverlayCanvas,
        TMP_FontAsset? newPrimaryFont)
    {
        battlefieldView = newBattlefieldView;
        tableCenter = newTableCenter;
        overlayCanvas = newOverlayCanvas;
        primaryFont = newPrimaryFont;
        catalog ??= Resources.Load<EffectPresentationCatalog>(DefaultCatalogResourcesPath);
        settings ??= Resources.Load<EffectPresentationSettings>(DefaultSettingsResourcesPath) ??
                     EffectPresentationSettings.CreateRuntimeDefault();
        vfxPlayer ??= gameObject.GetComponent<EffectVfxPlayer>() ?? gameObject.AddComponent<EffectVfxPlayer>();
        sfxPlayer ??= gameObject.GetComponent<EffectSfxPlayer>() ?? gameObject.AddComponent<EffectSfxPlayer>();
    }

    public void PresentProjection(ProjectionViewModel projection)
    {
        if (projection is null || !projection.hasStateProjection)
        {
            return;
        }

        if (!hasSeenProjection)
        {
            hasSeenProjection = true;
            for (var index = 0; index < projection.eventLog.Count; index += 1)
            {
                shownEventKeys.Add(buildEventKey(index, projection.eventLog[index]));
            }

            return;
        }

        var hasPresentedAny = false;
        for (var index = 0; index < projection.eventLog.Count; index += 1)
        {
            var eventLine = projection.eventLog[index];
            var eventKey = buildEventKey(index, eventLine);
            if (!shownEventKeys.Add(eventKey))
            {
                continue;
            }

            if (eventLine.StartsWith("skillTriggered", System.StringComparison.Ordinal))
            {
                hasPresentedAny = true;
                continue;
            }

            var categoryKey = EffectPresentationClassifier.Classify(projection.recentEventTypeKey, eventLine);
            present(categoryKey, eventLine);
            hasPresentedAny = true;
        }

        if (!hasPresentedAny && !string.IsNullOrWhiteSpace(projection.recentEventTypeKey))
        {
            var categoryKey = EffectPresentationClassifier.Classify(projection.recentEventTypeKey, string.Empty);
            present(categoryKey, projection.recentEventTypeKey);
        }
    }

    public void TestVfxOnCard(WorldCardView cardView, string categoryKey)
    {
        if (cardView is null)
        {
            Debug.LogWarning("[BattlefieldEffectPresentationTimeline] VFX calibration skipped: missing card view.");
            return;
        }

        var activeCatalog = catalog ?? Resources.Load<EffectPresentationCatalog>(DefaultCatalogResourcesPath);
        if (activeCatalog is null)
        {
            Debug.LogWarning($"[BattlefieldEffectPresentationTimeline] VFX calibration skipped: missing catalog. category={categoryKey}");
            return;
        }

        if (!activeCatalog.TryGetEntry(categoryKey, out var presentationEntry) || presentationEntry is null)
        {
            Debug.LogWarning($"[BattlefieldEffectPresentationTimeline] VFX calibration skipped: missing presentation entry. category={categoryKey}");
            return;
        }

        if (activeCatalog.VfxCatalog is null ||
            !activeCatalog.VfxCatalog.TryGetEntry(presentationEntry.vfxKey, out var vfxEntry) ||
            vfxEntry is null)
        {
            Debug.LogWarning($"[BattlefieldEffectPresentationTimeline] VFX calibration skipped: missing VFX entry. category={categoryKey} vfxKey={presentationEntry.vfxKey}");
            return;
        }

        Debug.Log(
            $"[BattlefieldEffectPresentationTimeline] VFX calibration play. category={categoryKey} " +
            $"card={cardView.DefinitionId}#{cardView.CardInstanceNumericId} visualKey={cardView.VisualKey} " +
            $"zone={cardView.ZoneViewKey} anchor={cardView.EffectAnchor.position.ToString("F3")} " +
            $"anchorRot={cardView.EffectAnchor.rotation.eulerAngles.ToString("F1")}");
        vfxPlayer?.Play(
            vfxEntry,
            cardView.EffectAnchor,
            tableCenter,
            cardView,
            categoryKey,
            usedFallback: false);
        showToast("VFX Calibration", cardView.EffectAnchor);
    }

    private static string buildEventKey(int eventIndex, string eventLine)
    {
        return $"{eventIndex}:{eventLine}";
    }

    private void present(string categoryKey, string eventLine)
    {
        if (BattlefieldInteractionPresentationGate.IsPresentationBusy &&
            string.Equals(
                BattlefieldInteractionPresentationGate.CurrentPresentationStep,
                "skillPresentation",
                System.StringComparison.Ordinal))
        {
            StartCoroutine(presentAfterSkillPresentation(categoryKey, eventLine));
            return;
        }

        var delaySeconds = string.Equals(categoryKey, EffectCategoryKeys.CardActivate, System.StringComparison.Ordinal)
            ? CardActivatePresentationDelaySeconds
            : 0f;
        if (delaySeconds > 0f && Application.isPlaying)
        {
            StartCoroutine(presentAfterDelay(categoryKey, eventLine, delaySeconds));
            return;
        }

        presentNow(categoryKey, eventLine);
    }

    private IEnumerator presentAfterSkillPresentation(string categoryKey, string eventLine)
    {
        while (BattlefieldInteractionPresentationGate.IsPresentationBusy &&
               string.Equals(
                   BattlefieldInteractionPresentationGate.CurrentPresentationStep,
                   "skillPresentation",
                   System.StringComparison.Ordinal))
        {
            yield return null;
        }

        present(categoryKey, eventLine);
    }

    private IEnumerator presentAfterDelay(string categoryKey, string eventLine, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        presentNow(categoryKey, eventLine);
    }

    private void presentNow(string categoryKey, string eventLine)
    {
        var activeCatalog = catalog ?? Resources.Load<EffectPresentationCatalog>(DefaultCatalogResourcesPath);
        if (activeCatalog is null)
        {
            Debug.LogWarning($"[BattlefieldEffectPresentationTimeline] Missing EffectPresentationCatalog. category={categoryKey}");
            return;
        }

        if (!activeCatalog.TryGetEntry(categoryKey, out var presentationEntry) || presentationEntry is null)
        {
            Debug.LogWarning($"[BattlefieldEffectPresentationTimeline] Missing presentation entry. category={categoryKey}");
            return;
        }

        var placement = new EffectPlacement(
            tableCenter,
            null,
            usedFallback: true,
            sourceCardInstanceNumericId: null,
            preferredZoneViewKey: string.Empty,
            fallbackReason: "noVfx");
        if (activeCatalog.VfxCatalog is not null &&
            activeCatalog.VfxCatalog.TryGetEntry(presentationEntry.vfxKey, out var vfxEntry))
        {
            placement = resolveTarget(eventLine, vfxEntry?.attachMode ?? EffectVfxAttachMode.cardSurface, categoryKey);
            if (placement.usedFallback)
            {
                Debug.LogWarning(
                    $"[BattlefieldEffectPresentationTimeline] VFX uses fallback. category={categoryKey} " +
                    $"sourceCardInstance={placement.sourceCardInstanceNumericId?.ToString() ?? "(none)"} " +
                    $"preferredZone={placement.preferredZoneViewKey} fallback={placement.fallbackReason}");
            }

            vfxPlayer?.Play(
                vfxEntry,
                placement.anchor,
                tableCenter,
                placement.sourceCard,
                categoryKey,
                placement.usedFallback);
            showToast(presentationEntry.labelText, placement.anchor);
        }
        else
        {
            Debug.LogWarning($"[BattlefieldEffectPresentationTimeline] Missing VFX mapping. category={categoryKey} vfxKey={presentationEntry.vfxKey}");
            showToast(presentationEntry.labelText, tableCenter);
        }

        if (activeCatalog.SfxCatalog is not null &&
            activeCatalog.SfxCatalog.TryGetEntry(presentationEntry.sfxKey, out var sfxEntry))
        {
            sfxPlayer?.Play(presentationEntry.sfxKey, sfxEntry);
        }
        else
        {
            Debug.LogWarning($"[BattlefieldEffectPresentationTimeline] Missing SFX mapping. category={categoryKey} sfxKey={presentationEntry.sfxKey}");
        }

        var activeSettings = settings ?? Resources.Load<EffectPresentationSettings>(DefaultSettingsResourcesPath) ??
            EffectPresentationSettings.CreateRuntimeDefault();
        var interactionDelaySeconds = activeSettings.ResolveDelaySeconds(
            categoryKey,
            presentationEntry.totalDelayBeforeInteraction);
        BattlefieldInteractionPresentationGate.DelayFor(
            interactionDelaySeconds,
            categoryKey,
            placement.sourceCardInstanceNumericId,
            "effectPresentation");
    }

    private EffectPlacement resolveTarget(string eventLine, EffectVfxAttachMode attachMode, string categoryKey)
    {
        var preferredZoneViewKey = string.Equals(categoryKey, EffectCategoryKeys.CardActivate, System.StringComparison.Ordinal)
            ? "playerField"
            : string.Empty;
        if (attachMode == EffectVfxAttachMode.tableCenter ||
            attachMode == EffectVfxAttachMode.tableCenterFallback ||
            attachMode == EffectVfxAttachMode.screenOverlay)
        {
            return new EffectPlacement(
                tableCenter,
                null,
                usedFallback: attachMode != EffectVfxAttachMode.screenOverlay,
                sourceCardInstanceNumericId: null,
                preferredZoneViewKey: preferredZoneViewKey,
                fallbackReason: attachMode.ToString());
        }

        var cardInstanceMatch = CardInstancePattern.Match(eventLine ?? string.Empty);
        if (cardInstanceMatch.Success &&
            long.TryParse(cardInstanceMatch.Groups[1].Value, out var cardInstanceNumericId))
        {
            if (battlefieldView is not null &&
                battlefieldView.TryFindEffectSourceCardByInstanceId(cardInstanceNumericId, preferredZoneViewKey, out var instanceCardView) &&
                instanceCardView is not null)
            {
                return new EffectPlacement(
                    instanceCardView.EffectAnchor,
                    instanceCardView,
                    usedFallback: false,
                    sourceCardInstanceNumericId: cardInstanceNumericId,
                    preferredZoneViewKey: preferredZoneViewKey,
                    fallbackReason: string.Empty);
            }

            Debug.LogWarning(
                $"[BattlefieldEffectPresentationTimeline] Effect source card not found; fallback to table center. " +
                $"category={categoryKey} sourceCardInstance={cardInstanceNumericId} preferredZone={preferredZoneViewKey} event={eventLine}");
            return new EffectPlacement(
                tableCenter,
                null,
                usedFallback: true,
                sourceCardInstanceNumericId: cardInstanceNumericId,
                preferredZoneViewKey: preferredZoneViewKey,
                fallbackReason: "sourceCardNotFound");
        }

        var match = DefinitionIdPattern.Match(eventLine ?? string.Empty);
        if (match.Success)
        {
            if (battlefieldView is not null &&
                battlefieldView.TryFindFirstCardByDefinitionId(match.Value, out var cardView) &&
                cardView is not null)
            {
                return new EffectPlacement(
                    cardView.EffectAnchor,
                    cardView,
                    usedFallback: false,
                    sourceCardInstanceNumericId: null,
                    preferredZoneViewKey: preferredZoneViewKey,
                    fallbackReason: string.Empty);
            }

            Debug.LogWarning(
                $"[BattlefieldEffectPresentationTimeline] Effect source definition not found; fallback to table center. " +
                $"category={categoryKey} definition={match.Value} event={eventLine}");
            return new EffectPlacement(
                tableCenter,
                null,
                usedFallback: true,
                sourceCardInstanceNumericId: null,
                preferredZoneViewKey: preferredZoneViewKey,
                fallbackReason: "sourceDefinitionNotFound");
        }

        Debug.LogWarning(
            $"[BattlefieldEffectPresentationTimeline] Effect source not resolved; fallback to table center. " +
            $"category={categoryKey} event={eventLine}");
        return new EffectPlacement(
            tableCenter,
            null,
            usedFallback: true,
            sourceCardInstanceNumericId: null,
            preferredZoneViewKey: preferredZoneViewKey,
            fallbackReason: "sourceNotResolved");
    }

    private void showToast(string labelText, Transform? target)
    {
        if (overlayCanvas is null || string.IsNullOrWhiteSpace(labelText))
        {
            return;
        }

        var toastText = rentToastText();
        toastText.text = labelText;
        toastText.gameObject.SetActive(true);
        positionToast(toastText.rectTransform, target);
        activeToasts.Add(toastText);
        StartCoroutine(returnToastAfterDelay(toastText, 0.75f));
    }

    private TMP_Text rentToastText()
    {
        while (toastPool.Count > 0)
        {
            var pooled = toastPool.Dequeue();
            if (pooled != null)
            {
                return pooled;
            }
        }

        return createToastText();
    }

    private TMP_Text createToastText()
    {
        var toastObject = new GameObject("EffectPresentationToast", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        toastObject.transform.SetParent(overlayCanvas!.transform, false);
        var rectTransform = (RectTransform)toastObject.transform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.73f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.73f);
        rectTransform.sizeDelta = new Vector2(360f, 54f);
        rectTransform.anchoredPosition = Vector2.zero;

        var image = toastObject.GetComponent<Image>();
        image.color = new Color(0.02f, 0.08f, 0.1f, 0.78f);

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(toastObject.transform, false);
        var textRectTransform = (RectTransform)textObject.transform;
        textRectTransform.anchorMin = Vector2.zero;
        textRectTransform.anchorMax = Vector2.one;
        textRectTransform.offsetMin = new Vector2(12f, 4f);
        textRectTransform.offsetMax = new Vector2(-12f, -4f);

        var toastText = textObject.GetComponent<TextMeshProUGUI>();
        if (toastText == null)
        {
            toastText = textObject.AddComponent<TextMeshProUGUI>();
        }

        toastText.alignment = TextAlignmentOptions.Center;
        if (primaryFont is not null)
        {
            toastText.font = primaryFont;
        }

        toastText.fontSize = 20f;
        toastText.fontStyle = FontStyles.Bold;
        toastText.color = new Color(1f, 0.92f, 0.55f, 1f);
        toastText.raycastTarget = false;
        toastObject.SetActive(false);
        return toastText;
    }

    private void positionToast(RectTransform toastRectTransform, Transform? target)
    {
        if (overlayCanvas is null)
        {
            return;
        }

        if (target is null || tableCenter is null)
        {
            toastRectTransform.anchorMin = new Vector2(0.5f, 0.73f);
            toastRectTransform.anchorMax = new Vector2(0.5f, 0.73f);
            toastRectTransform.anchoredPosition = Vector2.zero;
            return;
        }

        var sceneCamera = Camera.main;
        var worldCamera = overlayCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : overlayCanvas.worldCamera ?? sceneCamera;
        var screenPosition = RectTransformUtility.WorldToScreenPoint(sceneCamera, target.position);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)overlayCanvas.transform,
                screenPosition,
                worldCamera,
                out var localPosition))
        {
            toastRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            toastRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            toastRectTransform.anchoredPosition = localPosition + new Vector2(0f, 48f);
        }
    }

    private IEnumerator returnToastAfterDelay(TMP_Text toast, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (toast == null)
        {
            yield break;
        }

        toast.text = string.Empty;
        toast.gameObject.SetActive(false);
        activeToasts.Remove(toast);
        toastPool.Enqueue(toast);
    }

    private sealed class EffectPlacement
    {
        public EffectPlacement(
            Transform? anchor,
            WorldCardView? sourceCard,
            bool usedFallback,
            long? sourceCardInstanceNumericId,
            string preferredZoneViewKey,
            string fallbackReason)
        {
            this.anchor = anchor;
            this.sourceCard = sourceCard;
            this.usedFallback = usedFallback;
            this.sourceCardInstanceNumericId = sourceCardInstanceNumericId;
            this.preferredZoneViewKey = preferredZoneViewKey;
            this.fallbackReason = fallbackReason;
        }

        public readonly Transform? anchor;
        public readonly WorldCardView? sourceCard;
        public readonly bool usedFallback;
        public readonly long? sourceCardInstanceNumericId;
        public readonly string preferredZoneViewKey;
        public readonly string fallbackReason;
    }
}
}

