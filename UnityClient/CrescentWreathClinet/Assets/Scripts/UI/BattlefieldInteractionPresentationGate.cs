using CrescentWreath.Client.Net;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public static class BattlefieldInteractionPresentationGate
{
    private static float hiddenUntilUnscaledTime;
    private static string currentPresentationStep = "(none)";
    private static string deferredOverlayType = "none";
    private static string lastEffectCategoryKey = "(none)";
    private static long? lastSourceCardInstanceNumericId;

    public static bool IsPresentationBusy => RemainingDelaySeconds > 0f;

    public static float RemainingDelaySeconds => Mathf.Max(0f, hiddenUntilUnscaledTime - Time.unscaledTime);

    public static string CurrentPresentationStep => IsPresentationBusy ? currentPresentationStep : "(none)";

    public static string DeferredOverlayType => IsPresentationBusy ? deferredOverlayType : "none";

    public static string LastEffectCategoryKey => lastEffectCategoryKey;

    public static long? LastSourceCardInstanceNumericId => lastSourceCardInstanceNumericId;

    public static string DebugSummary =>
        $"presentationBusy={IsPresentationBusy}; step={CurrentPresentationStep}; " +
        $"remaining={RemainingDelaySeconds:0.00}s; deferredOverlay={DeferredOverlayType}; " +
        $"lastEffectCategory={LastEffectCategoryKey}; " +
        $"lastSourceCard={LastSourceCardInstanceNumericId?.ToString() ?? "(none)"}";

    public static void DelayFor(
        float seconds,
        string effectCategoryKey = "",
        long? sourceCardInstanceNumericId = null,
        string presentationStep = "effect")
    {
        if (seconds <= 0f)
        {
            return;
        }

        hiddenUntilUnscaledTime = Mathf.Max(hiddenUntilUnscaledTime, Time.unscaledTime + seconds);
        currentPresentationStep = string.IsNullOrWhiteSpace(presentationStep) ? "effect" : presentationStep;
        deferredOverlayType = "none";
        if (!string.IsNullOrWhiteSpace(effectCategoryKey))
        {
            lastEffectCategoryKey = effectCategoryKey;
        }

        lastSourceCardInstanceNumericId = sourceCardInstanceNumericId;
    }

    public static bool ShouldDelayInteraction(ProjectionViewModel? projection, bool isNewInteraction, out float remainingSeconds)
    {
        remainingSeconds = RemainingDelaySeconds;
        if (projection is null || !isNewInteraction || remainingSeconds <= 0f)
        {
            return false;
        }

        deferredOverlayType = projection.interaction.hasInputContext
            ? "inputContext"
            : projection.interaction.hasResponseWindow
                ? "responseWindow"
                : "interaction";
        return true;
    }
}
}
