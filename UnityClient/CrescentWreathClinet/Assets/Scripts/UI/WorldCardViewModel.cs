namespace CrescentWreath.Client.UI
{
public enum WorldCardDisplayMode
{
    normal,
    deckBack,
    readOnlyMirror,
    disabled,
    emptyZone,
}

public sealed class WorldCardViewModel
{
    public long cardInstanceNumericId;
    public string definitionId = string.Empty;
    public long ownerPlayerNumericId;
    public string zoneKey = string.Empty;
    public bool isFaceUp;
    public bool isSelected;
    public bool isInteractable;
    public bool isReadOnly;
    public bool isDefensePlacedOnField;
    public bool isActivated;
    public int? baseSummonSigilCost;
    public int? effectiveSummonSigilCost;
    public bool isSummonableByViewer;
    public string summonUnavailableReasonKey = string.Empty;
    public WorldCardDisplayMode displayMode = WorldCardDisplayMode.normal;
    public int pileCardCount = 1;
    public int overlayCardCount;
    public string tooltipTitle = string.Empty;
    public string tooltipDescription = string.Empty;

    public WorldCardViewModel Clone()
    {
        return new WorldCardViewModel
        {
            cardInstanceNumericId = cardInstanceNumericId,
            definitionId = definitionId,
            ownerPlayerNumericId = ownerPlayerNumericId,
            zoneKey = zoneKey,
            isFaceUp = isFaceUp,
            isSelected = isSelected,
            isInteractable = isInteractable,
            isReadOnly = isReadOnly,
            isDefensePlacedOnField = isDefensePlacedOnField,
            isActivated = isActivated,
            baseSummonSigilCost = baseSummonSigilCost,
            effectiveSummonSigilCost = effectiveSummonSigilCost,
            isSummonableByViewer = isSummonableByViewer,
            summonUnavailableReasonKey = summonUnavailableReasonKey,
            displayMode = displayMode,
            pileCardCount = pileCardCount,
            overlayCardCount = overlayCardCount,
            tooltipTitle = tooltipTitle,
            tooltipDescription = tooltipDescription,
        };
    }
}
}
