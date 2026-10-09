namespace CrescentWreath.Client.UI
{
public enum EffectVfxAttachMode
{
    worldAtCard = 0,
    worldAtTarget = 1,
    screenOverlay = 2,
    tableCenter = 3,

    cardSurface = worldAtCard,
    targetCharacterArea = worldAtTarget,
    tableCenterFallback = tableCenter,
}
}
