using System.Collections.Generic;

namespace CrescentWreath.Client.Net
{
public sealed class ProjectionCardViewModel
{
    public long cardInstanceNumericId;
    public string definitionId = string.Empty;
    public string zoneKey = string.Empty;
}

public sealed class ProjectionInteractionViewModel
{
    public bool hasInputContext;
    public bool hasResponseWindow;
    public long? inputRequiredPlayerNumericId;
    public int inputChoiceCount;
    public long? responseCurrentResponderPlayerNumericId;
    public int responseResponderCount;
}

public sealed class ProjectionViewModel
{
    public bool isSucceeded;
    public string errorCode = string.Empty;
    public string errorMessage = string.Empty;
    public long viewerPlayerNumericId;
    public bool hasStateProjection;

    public int turnNumber;
    public string currentPhase = string.Empty;
    public long? currentPlayerNumericId;

    public int mana;
    public int skillPoint;
    public int sigilPreview;
    public int? lockedSigil;
    public int viewerHandCardCount;
    public int discardCount;

    public int? activeCharacterCurrentHp;
    public int? activeCharacterMaxHp;
    public readonly List<string> activeCharacterStatusKeys = new();

    public readonly List<ProjectionCardViewModel> handCards = new();
    public readonly List<ProjectionCardViewModel> fieldCards = new();
    public readonly List<ProjectionCardViewModel> summonZoneCards = new();
    public readonly List<ProjectionCardViewModel> sakuraCakeCards = new();

    public readonly List<string> eventLog = new();
    public string recentEventTypeKey = string.Empty;

    public readonly ProjectionInteractionViewModel interaction = new();

    public static ProjectionViewModel createDefault(long viewerPlayerNumericId)
    {
        return new ProjectionViewModel
        {
            viewerPlayerNumericId = viewerPlayerNumericId,
            isSucceeded = false,
            hasStateProjection = false,
        };
    }

    public ProjectionViewModel deepClone()
    {
        var cloned = new ProjectionViewModel
        {
            isSucceeded = isSucceeded,
            errorCode = errorCode,
            errorMessage = errorMessage,
            viewerPlayerNumericId = viewerPlayerNumericId,
            hasStateProjection = hasStateProjection,
            turnNumber = turnNumber,
            currentPhase = currentPhase,
            currentPlayerNumericId = currentPlayerNumericId,
            mana = mana,
            skillPoint = skillPoint,
            sigilPreview = sigilPreview,
            lockedSigil = lockedSigil,
            viewerHandCardCount = viewerHandCardCount,
            discardCount = discardCount,
            activeCharacterCurrentHp = activeCharacterCurrentHp,
            activeCharacterMaxHp = activeCharacterMaxHp,
            recentEventTypeKey = recentEventTypeKey,
        };

        foreach (var statusKey in activeCharacterStatusKeys)
        {
            cloned.activeCharacterStatusKeys.Add(statusKey);
        }

        foreach (var card in handCards)
        {
            cloned.handCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = card.cardInstanceNumericId,
                definitionId = card.definitionId,
                zoneKey = card.zoneKey,
            });
        }

        foreach (var card in fieldCards)
        {
            cloned.fieldCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = card.cardInstanceNumericId,
                definitionId = card.definitionId,
                zoneKey = card.zoneKey,
            });
        }

        foreach (var card in summonZoneCards)
        {
            cloned.summonZoneCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = card.cardInstanceNumericId,
                definitionId = card.definitionId,
                zoneKey = card.zoneKey,
            });
        }

        foreach (var card in sakuraCakeCards)
        {
            cloned.sakuraCakeCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = card.cardInstanceNumericId,
                definitionId = card.definitionId,
                zoneKey = card.zoneKey,
            });
        }

        foreach (var eventLine in eventLog)
        {
            cloned.eventLog.Add(eventLine);
        }

        cloned.interaction.hasInputContext = interaction.hasInputContext;
        cloned.interaction.hasResponseWindow = interaction.hasResponseWindow;
        cloned.interaction.inputRequiredPlayerNumericId = interaction.inputRequiredPlayerNumericId;
        cloned.interaction.inputChoiceCount = interaction.inputChoiceCount;
        cloned.interaction.responseCurrentResponderPlayerNumericId = interaction.responseCurrentResponderPlayerNumericId;
        cloned.interaction.responseResponderCount = interaction.responseResponderCount;

        return cloned;
    }

    public static ProjectionViewModel mergeLatestWithIncomingFailure(ProjectionViewModel latest, ProjectionViewModel incoming)
    {
        var merged = latest.deepClone();
        merged.isSucceeded = incoming.isSucceeded;
        merged.errorCode = incoming.errorCode;
        merged.errorMessage = incoming.errorMessage;
        merged.viewerPlayerNumericId = incoming.viewerPlayerNumericId;
        merged.hasStateProjection = latest.hasStateProjection;

        merged.recentEventTypeKey = incoming.recentEventTypeKey;
        merged.eventLog.Clear();
        if (incoming.eventLog.Count > 0)
        {
            foreach (var eventLine in incoming.eventLog)
            {
                merged.eventLog.Add(eventLine);
            }
        }
        else
        {
            foreach (var eventLine in latest.eventLog)
            {
                merged.eventLog.Add(eventLine);
            }
        }

        merged.interaction.hasInputContext = incoming.interaction.hasInputContext;
        merged.interaction.hasResponseWindow = incoming.interaction.hasResponseWindow;
        merged.interaction.inputRequiredPlayerNumericId = incoming.interaction.inputRequiredPlayerNumericId;
        merged.interaction.inputChoiceCount = incoming.interaction.inputChoiceCount;
        merged.interaction.responseCurrentResponderPlayerNumericId = incoming.interaction.responseCurrentResponderPlayerNumericId;
        merged.interaction.responseResponderCount = incoming.interaction.responseResponderCount;

        return merged;
    }
}
}
