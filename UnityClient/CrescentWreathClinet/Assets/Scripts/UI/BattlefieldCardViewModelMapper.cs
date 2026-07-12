using System.Collections.Generic;
using CrescentWreath.Client.Net;

namespace CrescentWreath.Client.UI
{
public static class BattlefieldCardViewModelMapper
{
    public static WorldCardViewModel FromProjectionCard(
        ProjectionCardViewModel card,
        bool readOnly,
        WorldCardDisplayMode displayMode = WorldCardDisplayMode.normal)
    {
        var isUnavailableSummonCard =
            (card.baseSummonSigilCost.HasValue || card.effectiveSummonSigilCost.HasValue) &&
            !card.isSummonableByViewer;
        var isKnownToViewer = !string.IsNullOrWhiteSpace(card.definitionId);
        return new WorldCardViewModel
        {
            cardInstanceNumericId = card.cardInstanceNumericId,
            definitionId = card.definitionId,
            ownerPlayerNumericId = card.ownerPlayerNumericId,
            zoneKey = card.zoneKey,
            isFaceUp = card.isFaceUp || isKnownToViewer,
            isSelected = card.isSummonableByViewer,
            isInteractable = false,
            isReadOnly = readOnly,
            isDefensePlacedOnField = card.isDefensePlacedOnField,
            baseSummonSigilCost = card.baseSummonSigilCost,
            effectiveSummonSigilCost = card.effectiveSummonSigilCost,
            isSummonableByViewer = card.isSummonableByViewer,
            summonUnavailableReasonKey = card.summonUnavailableReasonKey,
            displayMode = isUnavailableSummonCard ? WorldCardDisplayMode.disabled : displayMode,
            pileCardCount = 1,
            overlayCardCount = card.overlayCardCount,
        };
    }

    public static IReadOnlyList<WorldCardViewModel> FromProjectionCards(
        IReadOnlyList<ProjectionCardViewModel> cards,
        bool readOnly,
        WorldCardDisplayMode displayMode = WorldCardDisplayMode.normal)
    {
        var models = new List<WorldCardViewModel>(cards.Count);
        foreach (var card in cards)
        {
            models.Add(FromProjectionCard(card, readOnly, displayMode));
        }

        return models;
    }

    public static IReadOnlyList<WorldCardViewModel> CreateDeckBackStack(
        string zoneKey,
        long syntheticIdBase,
        string fallbackDefinitionId,
        int count)
    {
        var visibleCount = count <= 0 ? 0 : 1;
        var models = new List<WorldCardViewModel>(visibleCount);
        for (var index = 0; index < visibleCount; index += 1)
        {
            models.Add(new WorldCardViewModel
            {
                cardInstanceNumericId = syntheticIdBase - index,
                definitionId = fallbackDefinitionId,
                zoneKey = zoneKey,
                isFaceUp = false,
                isInteractable = false,
                isReadOnly = true,
                displayMode = WorldCardDisplayMode.deckBack,
                pileCardCount = count,
            });
        }

        return models;
    }

    public static IReadOnlyList<WorldCardViewModel> CreatePublicPilePreview(
        string zoneKey,
        long syntheticIdBase,
        IReadOnlyList<ProjectionCardViewModel> cards,
        int count,
        string fallbackDefinitionId)
    {
        if (cards.Count > 0)
        {
            var topCard = cards[cards.Count - 1];
            var model = FromProjectionCard(topCard, readOnly: true, WorldCardDisplayMode.readOnlyMirror);
            model.zoneKey = zoneKey;
            model.isFaceUp = true;
            model.pileCardCount = count <= 0 ? cards.Count : count;
            return new[] { model };
        }

        if (count > 0)
        {
            return CreateDeckBackStack(zoneKey, syntheticIdBase, fallbackDefinitionId, count);
        }

        return new[]
        {
            new WorldCardViewModel
            {
                cardInstanceNumericId = syntheticIdBase,
                definitionId = fallbackDefinitionId,
                zoneKey = zoneKey,
                isFaceUp = false,
                isInteractable = false,
                isReadOnly = true,
                displayMode = WorldCardDisplayMode.emptyZone,
                pileCardCount = 0,
            },
        };
    }
}
}
