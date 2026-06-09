using System.Collections.Generic;

namespace CrescentWreath.Client.Net
{
public sealed class ProjectionCardViewModel
{
    public long cardInstanceNumericId;
    public string definitionId = string.Empty;
    public string zoneKey = string.Empty;
    public long? overlayContainerCardInstanceNumericId;
    public int? overlayOrderIndex;
    public int overlayCardCount;
}

public sealed class ProjectionInteractionViewModel
{
    public bool hasInputContext;
    public long? inputContextNumericId;
    public bool hasResponseWindow;
    public long? inputRequiredPlayerNumericId;
    public readonly List<long> inputRequiredPlayerNumericIds = new();
    public readonly List<long> inputSubmittedPlayerNumericIds = new();
    public int inputRequiredPlayerCount;
    public int inputSubmittedPlayerCount;
    public bool isViewerRequiredPlayerForInput;
    public bool isViewerSubmittedInput;
    public string inputTypeKey = string.Empty;
    public string contextKey = string.Empty;
    public int inputChoiceCount;
    public readonly List<string> inputChoiceKeys = new();
    public string selectedChoiceKey = string.Empty;
    public long? responseWindowNumericId;
    public long? responseCurrentResponderPlayerNumericId;
    public int responseResponderCount;
    public string responseWindowOriginType = string.Empty;
    public string pendingDamageResponseStageKey = string.Empty;
    public string pendingDamageTypeKey = string.Empty;
    public long? pendingDamageTargetCharacterInstanceNumericId;
    public long? pendingDamageDefenderPlayerNumericId;
}

public sealed class ProjectionMarkerViewModel
{
    public string markerTypeKey = string.Empty;
    public int count;
    public int maxCount;
    public string displayNameKey = string.Empty;
}

public sealed class ProjectionPlayerSummaryViewModel
{
    public long playerNumericId;
    public long teamNumericId;
    public bool isCurrentPlayer;
    public bool isViewerPlayer;
    public int mana;
    public int skillPoint;
    public int sigilPreview;
    public int? lockedSigil;
    public bool isSigilLocked;
    public int handCount;
    public int fieldCount;
    public int discardCount;
    public long? activeCharacterInstanceNumericId;
    public int? activeCharacterCurrentHp;
    public int? activeCharacterMaxHp;
    public readonly List<string> playerStatusKeys = new();
    public readonly List<string> activeCharacterStatusKeys = new();
    public readonly List<string> activeCharacterRaceTags = new();
    public readonly List<ProjectionMarkerViewModel> activeCharacterMarkers = new();
    public string activeCharacterFactionKey = string.Empty;
    public bool activeCharacterIsActivated;
}

public sealed class ProjectionTeamSummaryViewModel
{
    public long teamNumericId;
    public int leyline;
    public int killScore;
}

public sealed class ProjectionAnomalyViewModel
{
    public bool hasCurrentAnomaly;
    public string definitionId = string.Empty;
    public string name = string.Empty;
    public string arrivalText = string.Empty;
    public string resolveText = string.Empty;
    public string oncePerTurnHint = string.Empty;
    public string resolveConditionKey = string.Empty;
    public string resolveRewardKey = string.Empty;
    public int remainingDeckCount;
    public bool hasResolvedThisTurn;
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
    public readonly List<string> activeCharacterRaceTags = new();
    public readonly List<ProjectionMarkerViewModel> activeCharacterMarkers = new();
    public string activeCharacterFactionKey = string.Empty;
    public bool activeCharacterIsActivated;

    public readonly List<ProjectionCardViewModel> handCards = new();
    public readonly List<ProjectionCardViewModel> discardCards = new();
    public readonly List<ProjectionCardViewModel> fieldCards = new();
    public readonly List<ProjectionCardViewModel> summonZoneCards = new();
    public readonly List<ProjectionCardViewModel> sakuraCakeCards = new();
    public readonly List<ProjectionCardViewModel> gapZoneCards = new();
    public readonly List<ProjectionTeamSummaryViewModel> teamSummaries = new();
    public readonly List<ProjectionPlayerSummaryViewModel> playerSummaries = new();

    public readonly List<string> eventLog = new();
    public string recentEventTypeKey = string.Empty;

    public readonly ProjectionInteractionViewModel interaction = new();
    public readonly ProjectionAnomalyViewModel currentAnomaly = new();

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
            activeCharacterFactionKey = activeCharacterFactionKey,
            activeCharacterIsActivated = activeCharacterIsActivated,
            recentEventTypeKey = recentEventTypeKey,
        };

        foreach (var statusKey in activeCharacterStatusKeys)
        {
            cloned.activeCharacterStatusKeys.Add(statusKey);
        }
        foreach (var raceTag in activeCharacterRaceTags)
        {
            cloned.activeCharacterRaceTags.Add(raceTag);
        }
        foreach (var marker in activeCharacterMarkers)
        {
            cloned.activeCharacterMarkers.Add(new ProjectionMarkerViewModel
            {
                markerTypeKey = marker.markerTypeKey,
                count = marker.count,
                maxCount = marker.maxCount,
                displayNameKey = marker.displayNameKey,
            });
        }

        foreach (var card in handCards)
        {
            cloned.handCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = card.cardInstanceNumericId,
                definitionId = card.definitionId,
                zoneKey = card.zoneKey,
                overlayContainerCardInstanceNumericId = card.overlayContainerCardInstanceNumericId,
                overlayOrderIndex = card.overlayOrderIndex,
                overlayCardCount = card.overlayCardCount,
            });
        }

        foreach (var card in discardCards)
        {
            cloned.discardCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = card.cardInstanceNumericId,
                definitionId = card.definitionId,
                zoneKey = card.zoneKey,
                overlayContainerCardInstanceNumericId = card.overlayContainerCardInstanceNumericId,
                overlayOrderIndex = card.overlayOrderIndex,
                overlayCardCount = card.overlayCardCount,
            });
        }

        foreach (var card in fieldCards)
        {
            cloned.fieldCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = card.cardInstanceNumericId,
                definitionId = card.definitionId,
                zoneKey = card.zoneKey,
                overlayContainerCardInstanceNumericId = card.overlayContainerCardInstanceNumericId,
                overlayOrderIndex = card.overlayOrderIndex,
                overlayCardCount = card.overlayCardCount,
            });
        }

        foreach (var card in summonZoneCards)
        {
            cloned.summonZoneCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = card.cardInstanceNumericId,
                definitionId = card.definitionId,
                zoneKey = card.zoneKey,
                overlayContainerCardInstanceNumericId = card.overlayContainerCardInstanceNumericId,
                overlayOrderIndex = card.overlayOrderIndex,
                overlayCardCount = card.overlayCardCount,
            });
        }

        foreach (var card in sakuraCakeCards)
        {
            cloned.sakuraCakeCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = card.cardInstanceNumericId,
                definitionId = card.definitionId,
                zoneKey = card.zoneKey,
                overlayContainerCardInstanceNumericId = card.overlayContainerCardInstanceNumericId,
                overlayOrderIndex = card.overlayOrderIndex,
                overlayCardCount = card.overlayCardCount,
            });
        }

        foreach (var card in gapZoneCards)
        {
            cloned.gapZoneCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = card.cardInstanceNumericId,
                definitionId = card.definitionId,
                zoneKey = card.zoneKey,
                overlayContainerCardInstanceNumericId = card.overlayContainerCardInstanceNumericId,
                overlayOrderIndex = card.overlayOrderIndex,
                overlayCardCount = card.overlayCardCount,
            });
        }

        foreach (var teamSummary in teamSummaries)
        {
            cloned.teamSummaries.Add(new ProjectionTeamSummaryViewModel
            {
                teamNumericId = teamSummary.teamNumericId,
                leyline = teamSummary.leyline,
                killScore = teamSummary.killScore,
            });
        }

        foreach (var playerSummary in playerSummaries)
        {
            var clonedPlayerSummary = new ProjectionPlayerSummaryViewModel
            {
                playerNumericId = playerSummary.playerNumericId,
                teamNumericId = playerSummary.teamNumericId,
                isCurrentPlayer = playerSummary.isCurrentPlayer,
                isViewerPlayer = playerSummary.isViewerPlayer,
                mana = playerSummary.mana,
                skillPoint = playerSummary.skillPoint,
                sigilPreview = playerSummary.sigilPreview,
                lockedSigil = playerSummary.lockedSigil,
                isSigilLocked = playerSummary.isSigilLocked,
                handCount = playerSummary.handCount,
                fieldCount = playerSummary.fieldCount,
                discardCount = playerSummary.discardCount,
                activeCharacterInstanceNumericId = playerSummary.activeCharacterInstanceNumericId,
                activeCharacterCurrentHp = playerSummary.activeCharacterCurrentHp,
                activeCharacterMaxHp = playerSummary.activeCharacterMaxHp,
                activeCharacterFactionKey = playerSummary.activeCharacterFactionKey,
                activeCharacterIsActivated = playerSummary.activeCharacterIsActivated,
            };
            foreach (var statusKey in playerSummary.playerStatusKeys)
            {
                clonedPlayerSummary.playerStatusKeys.Add(statusKey);
            }
            foreach (var statusKey in playerSummary.activeCharacterStatusKeys)
            {
                clonedPlayerSummary.activeCharacterStatusKeys.Add(statusKey);
            }
            foreach (var raceTag in playerSummary.activeCharacterRaceTags)
            {
                clonedPlayerSummary.activeCharacterRaceTags.Add(raceTag);
            }
            foreach (var marker in playerSummary.activeCharacterMarkers)
            {
                clonedPlayerSummary.activeCharacterMarkers.Add(new ProjectionMarkerViewModel
                {
                    markerTypeKey = marker.markerTypeKey,
                    count = marker.count,
                    maxCount = marker.maxCount,
                    displayNameKey = marker.displayNameKey,
                });
            }

            cloned.playerSummaries.Add(clonedPlayerSummary);
        }

        foreach (var eventLine in eventLog)
        {
            cloned.eventLog.Add(eventLine);
        }

        cloned.currentAnomaly.hasCurrentAnomaly = currentAnomaly.hasCurrentAnomaly;
        cloned.currentAnomaly.definitionId = currentAnomaly.definitionId;
        cloned.currentAnomaly.name = currentAnomaly.name;
        cloned.currentAnomaly.arrivalText = currentAnomaly.arrivalText;
        cloned.currentAnomaly.resolveText = currentAnomaly.resolveText;
        cloned.currentAnomaly.oncePerTurnHint = currentAnomaly.oncePerTurnHint;
        cloned.currentAnomaly.resolveConditionKey = currentAnomaly.resolveConditionKey;
        cloned.currentAnomaly.resolveRewardKey = currentAnomaly.resolveRewardKey;
        cloned.currentAnomaly.remainingDeckCount = currentAnomaly.remainingDeckCount;
        cloned.currentAnomaly.hasResolvedThisTurn = currentAnomaly.hasResolvedThisTurn;

        cloned.interaction.hasInputContext = interaction.hasInputContext;
        cloned.interaction.inputContextNumericId = interaction.inputContextNumericId;
        cloned.interaction.hasResponseWindow = interaction.hasResponseWindow;
        cloned.interaction.inputRequiredPlayerNumericId = interaction.inputRequiredPlayerNumericId;
        foreach (var requiredPlayerNumericId in interaction.inputRequiredPlayerNumericIds)
        {
            cloned.interaction.inputRequiredPlayerNumericIds.Add(requiredPlayerNumericId);
        }
        foreach (var submittedPlayerNumericId in interaction.inputSubmittedPlayerNumericIds)
        {
            cloned.interaction.inputSubmittedPlayerNumericIds.Add(submittedPlayerNumericId);
        }
        cloned.interaction.inputRequiredPlayerCount = interaction.inputRequiredPlayerCount;
        cloned.interaction.inputSubmittedPlayerCount = interaction.inputSubmittedPlayerCount;
        cloned.interaction.isViewerRequiredPlayerForInput = interaction.isViewerRequiredPlayerForInput;
        cloned.interaction.isViewerSubmittedInput = interaction.isViewerSubmittedInput;
        cloned.interaction.inputTypeKey = interaction.inputTypeKey;
        cloned.interaction.contextKey = interaction.contextKey;
        cloned.interaction.inputChoiceCount = interaction.inputChoiceCount;
        foreach (var choiceKey in interaction.inputChoiceKeys)
        {
            cloned.interaction.inputChoiceKeys.Add(choiceKey);
        }
        cloned.interaction.selectedChoiceKey = interaction.selectedChoiceKey;
        cloned.interaction.responseWindowNumericId = interaction.responseWindowNumericId;
        cloned.interaction.responseCurrentResponderPlayerNumericId = interaction.responseCurrentResponderPlayerNumericId;
        cloned.interaction.responseResponderCount = interaction.responseResponderCount;
        cloned.interaction.responseWindowOriginType = interaction.responseWindowOriginType;
        cloned.interaction.pendingDamageResponseStageKey = interaction.pendingDamageResponseStageKey;
        cloned.interaction.pendingDamageTypeKey = interaction.pendingDamageTypeKey;
        cloned.interaction.pendingDamageTargetCharacterInstanceNumericId = interaction.pendingDamageTargetCharacterInstanceNumericId;
        cloned.interaction.pendingDamageDefenderPlayerNumericId = interaction.pendingDamageDefenderPlayerNumericId;

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
        merged.interaction.inputContextNumericId = incoming.interaction.inputContextNumericId;
        merged.interaction.hasResponseWindow = incoming.interaction.hasResponseWindow;
        merged.interaction.inputRequiredPlayerNumericId = incoming.interaction.inputRequiredPlayerNumericId;
        merged.interaction.inputRequiredPlayerNumericIds.Clear();
        foreach (var requiredPlayerNumericId in incoming.interaction.inputRequiredPlayerNumericIds)
        {
            merged.interaction.inputRequiredPlayerNumericIds.Add(requiredPlayerNumericId);
        }
        merged.interaction.inputSubmittedPlayerNumericIds.Clear();
        foreach (var submittedPlayerNumericId in incoming.interaction.inputSubmittedPlayerNumericIds)
        {
            merged.interaction.inputSubmittedPlayerNumericIds.Add(submittedPlayerNumericId);
        }
        merged.interaction.inputRequiredPlayerCount = incoming.interaction.inputRequiredPlayerCount;
        merged.interaction.inputSubmittedPlayerCount = incoming.interaction.inputSubmittedPlayerCount;
        merged.interaction.isViewerRequiredPlayerForInput = incoming.interaction.isViewerRequiredPlayerForInput;
        merged.interaction.isViewerSubmittedInput = incoming.interaction.isViewerSubmittedInput;
        merged.interaction.inputTypeKey = incoming.interaction.inputTypeKey;
        merged.interaction.contextKey = incoming.interaction.contextKey;
        merged.interaction.inputChoiceCount = incoming.interaction.inputChoiceCount;
        merged.interaction.inputChoiceKeys.Clear();
        foreach (var choiceKey in incoming.interaction.inputChoiceKeys)
        {
            merged.interaction.inputChoiceKeys.Add(choiceKey);
        }
        merged.interaction.selectedChoiceKey = incoming.interaction.selectedChoiceKey;
        merged.interaction.responseWindowNumericId = incoming.interaction.responseWindowNumericId;
        merged.interaction.responseCurrentResponderPlayerNumericId = incoming.interaction.responseCurrentResponderPlayerNumericId;
        merged.interaction.responseResponderCount = incoming.interaction.responseResponderCount;
        merged.interaction.responseWindowOriginType = incoming.interaction.responseWindowOriginType;
        merged.interaction.pendingDamageResponseStageKey = incoming.interaction.pendingDamageResponseStageKey;
        merged.interaction.pendingDamageTypeKey = incoming.interaction.pendingDamageTypeKey;
        merged.interaction.pendingDamageTargetCharacterInstanceNumericId = incoming.interaction.pendingDamageTargetCharacterInstanceNumericId;
        merged.interaction.pendingDamageDefenderPlayerNumericId = incoming.interaction.pendingDamageDefenderPlayerNumericId;

        return merged;
    }
}
}
