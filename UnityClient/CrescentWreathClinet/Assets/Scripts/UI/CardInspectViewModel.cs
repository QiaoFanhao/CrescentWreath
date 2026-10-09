using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class CardInspectViewModel
{
    public long cardInstanceNumericId;
    public string definitionId = string.Empty;
    public string displayName = string.Empty;
    public string attributeText = string.Empty;
    public string rulesText = string.Empty;
    public long ownerPlayerNumericId;
    public string zoneKey = string.Empty;
    public bool isFaceUp;
    public bool isDefensePlacedOnField;
    public int? baseSummonSigilCost;
    public int? effectiveSummonSigilCost;
    public bool isSummonableByViewer;
    public string summonUnavailableReasonKey = string.Empty;
    public Sprite? artworkSprite;
}
}
