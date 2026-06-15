using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.ServerPrototype;

public sealed class ServerErrorProjection
{
    public string code { get; set; } = string.Empty;
    public string message { get; set; } = string.Empty;
}

public sealed class ServerStateProjection
{
    public long viewerPlayerNumericId { get; set; }
    public string matchState { get; set; } = string.Empty;
    public long? winnerTeamNumericId { get; set; }
    public ServerTurnProjection? turn { get; set; }
    public List<ServerTeamProjection> teams { get; } = new();
    public List<ServerPlayerProjection> players { get; } = new();
    public ServerPublicZonesProjection? publicZones { get; set; }
    public ServerAnomalyProjection? currentAnomaly { get; set; }
    public List<ServerCharacterProjection> characters { get; } = new();
    public ServerCharacterSelectionProjection? characterSelection { get; set; }
    public List<ServerCharacterDefinitionProjection> characterDefinitions { get; } = new();
}

public sealed class ServerCharacterSelectionProjection
{
    public bool isActive { get; set; }
    public bool isCompleted { get; set; }
    public long? currentSelectingPlayerNumericId { get; set; }
    public List<ServerSelectedCharacterProjection> selections { get; } = new();
}

public sealed class ServerSelectedCharacterProjection
{
    public long playerNumericId { get; set; }
    public string characterDefinitionId { get; set; } = string.Empty;
}

public sealed class ServerCharacterDefinitionProjection
{
    public string definitionId { get; set; } = string.Empty;
    public string characterName { get; set; } = string.Empty;
    public string factionKey { get; set; } = string.Empty;
    public int baseMaxHp { get; set; }
    public bool isImplemented { get; set; }
    public List<string> raceTags { get; } = new();
    public List<ServerCharacterSkillDefinitionProjection> skills { get; } = new();
}

public sealed class ServerCharacterSkillDefinitionProjection
{
    public string skillKey { get; set; } = string.Empty;
    public string skillName { get; set; } = string.Empty;
    public int skillOrder { get; set; }
    public string skillTypeRaw { get; set; } = string.Empty;
    public string skillCostRaw { get; set; } = string.Empty;
    public string effectText { get; set; } = string.Empty;
}

public sealed class ServerTurnProjection
{
    public int turnNumber { get; set; }
    public long currentPlayerNumericId { get; set; }
    public long currentTeamNumericId { get; set; }
    public string currentPhase { get; set; } = string.Empty;
    public int phaseStepIndex { get; set; }
    public bool hasResolvedAnomalyThisTurn { get; set; }
}

public sealed class ServerTeamProjection
{
    public long teamNumericId { get; set; }
    public int leyline { get; set; }
    public int killScore { get; set; }
    public List<long> memberPlayerNumericIds { get; } = new();
}

public sealed class ServerPlayerProjection
{
    public long playerNumericId { get; set; }
    public long teamNumericId { get; set; }
    public long? activeCharacterInstanceNumericId { get; set; }
    public int mana { get; set; }
    public int skillPoint { get; set; }
    public int sigilPreview { get; set; }
    public int? lockedSigil { get; set; }
    public bool isSigilLocked { get; set; }
    public List<string> statusKeys { get; } = new();
    public ServerZoneProjection deckZone { get; set; } = new();
    public ServerZoneProjection handZone { get; set; } = new();
    public ServerZoneProjection discardZone { get; set; } = new();
    public ServerZoneProjection fieldZone { get; set; } = new();
    public ServerZoneProjection characterSetAsideZone { get; set; } = new();
}

public sealed class ServerPublicZonesProjection
{
    public ServerZoneProjection publicTreasureDeckZone { get; set; } = new();
    public ServerZoneProjection summonZone { get; set; } = new();
    public ServerZoneProjection gapZone { get; set; } = new();
    public ServerZoneProjection sakuraCakeDeckZone { get; set; } = new();
    public ServerZoneProjection anomalyDeckZone { get; set; } = new();
}

public sealed class ServerAnomalyProjection
{
    public string definitionId { get; set; } = string.Empty;
    public string name { get; set; } = string.Empty;
    public string arrivalText { get; set; } = string.Empty;
    public string resolveText { get; set; } = string.Empty;
    public string oncePerTurnHint { get; set; } = string.Empty;
    public string resolveConditionKey { get; set; } = string.Empty;
    public string resolveRewardKey { get; set; } = string.Empty;
    public int remainingDeckCount { get; set; }
    public bool hasResolvedThisTurn { get; set; }
}

public sealed class ServerCharacterProjection
{
    public long characterInstanceNumericId { get; set; }
    public string definitionId { get; set; } = string.Empty;
    public string factionKey { get; set; } = string.Empty;
    public long ownerPlayerNumericId { get; set; }
    public int currentHp { get; set; }
    public int maxHp { get; set; }
    public bool isAlive { get; set; }
    public bool isInPlay { get; set; }
    public bool isActivated { get; set; }
    public List<string> raceTags { get; } = new();
    public List<string> statusKeys { get; } = new();
    public List<ServerMarkerProjection> markers { get; } = new();
}

public sealed class ServerMarkerProjection
{
    public string markerTypeKey { get; set; } = string.Empty;
    public int count { get; set; }
    public int maxCount { get; set; }
    public string displayNameKey { get; set; } = string.Empty;
}

public sealed class ServerZoneProjection
{
    public long zoneNumericId { get; set; }
    public string zoneKey { get; set; } = string.Empty;
    public int cardCount { get; set; }
    public bool isContentVisible { get; set; }
    public int hiddenCardCount { get; set; }
    public List<ServerCardProjection> cards { get; } = new();
}

public sealed class ServerCardProjection
{
    public long cardInstanceNumericId { get; set; }
    public string definitionId { get; set; } = string.Empty;
    public long ownerPlayerNumericId { get; set; }
    public string zoneKey { get; set; } = string.Empty;
    public bool isFaceUp { get; set; }
    public bool isSetAside { get; set; }
    public bool isDefensePlacedOnField { get; set; }
    public long? overlayContainerCardInstanceNumericId { get; set; }
    public int? overlayOrderIndex { get; set; }
    public int overlayCardCount { get; set; }
}

public sealed class ServerInteractionProjection
{
    public ServerInputContextProjection? inputContext { get; set; }
    public ServerResponseWindowProjection? responseWindow { get; set; }
}

public sealed class ServerInputContextProjection
{
    public long inputContextNumericId { get; set; }
    public long? requiredPlayerNumericId { get; set; }
    public List<long> requiredPlayerNumericIds { get; } = new();
    public List<long> submittedPlayerNumericIds { get; } = new();
    public int requiredPlayerCount { get; set; }
    public int submittedPlayerCount { get; set; }
    public bool isViewerRequiredPlayer { get; set; }
    public bool isViewerSubmittedPlayer { get; set; }
    public string? inputTypeKey { get; set; }
    public string? contextKey { get; set; }
    public int choiceCount { get; set; }
    public List<string> choiceKeys { get; } = new();
    public string? selectedChoiceKey { get; set; }
}

public sealed class ServerResponseWindowProjection
{
    public long responseWindowNumericId { get; set; }
    public string? windowTypeKey { get; set; }
    public string responseWindowOriginType { get; set; } = string.Empty;
    public long? currentResponderPlayerNumericId { get; set; }
    public bool isViewerCurrentResponder { get; set; }
    public List<long> responderPlayerNumericIds { get; } = new();
    public string? pendingDamageResponseStageKey { get; set; }
    public string? pendingDamageTypeKey { get; set; }
    public long? pendingDamageTargetCharacterInstanceNumericId { get; set; }
    public long? pendingDamageDefenderPlayerNumericId { get; set; }
}

public sealed class ServerEventLogEntry
{
    public long eventId { get; set; }
    public string eventTypeKey { get; set; } = string.Empty;
    public long? sourceActionChainNumericId { get; set; }
    public long? cardInstanceNumericId { get; set; }
    public long? ownerPlayerNumericId { get; set; }
    public string? definitionId { get; set; }
    public string? revealReasonKey { get; set; }
    public string? fromZoneKey { get; set; }
    public string? toZoneKey { get; set; }
    public string? moveReason { get; set; }
    public long? targetPlayerNumericId { get; set; }
    public long? targetCharacterInstanceNumericId { get; set; }
    public int? hpBefore { get; set; }
    public int? hpAfter { get; set; }
    public int? delta { get; set; }
    public string? statusKey { get; set; }
    public bool? isApplied { get; set; }
    public long? targetCardInstanceNumericId { get; set; }
    public long? damageContextNumericId { get; set; }
    public int? finalDamageValue { get; set; }
    public bool? didDealDamage { get; set; }
    public string? windowKindKey { get; set; }
    public bool? isOpened { get; set; }
    public long? responseWindowNumericId { get; set; }
    public long? inputContextNumericId { get; set; }
    public string? markerTypeKey { get; set; }
    public int? markerBeforeCount { get; set; }
    public int? markerAfterCount { get; set; }
    public bool? wasActivated { get; set; }
    public bool? isActivated { get; set; }
    public string? message { get; set; }
}

internal static class ServerProjectionBuilder
{
    public static ServerStateProjection buildStateProjection(GameState gameState, PlayerId viewerPlayerId)
    {
        var projection = new ServerStateProjection
        {
            viewerPlayerNumericId = viewerPlayerId.Value,
            matchState = gameState.matchState.ToString(),
            winnerTeamNumericId = gameState.winnerTeamId?.Value,
        };

        if (gameState.turnState is not null)
        {
            projection.turn = new ServerTurnProjection
            {
                turnNumber = gameState.turnState.turnNumber,
                currentPlayerNumericId = gameState.turnState.currentPlayerId.Value,
                currentTeamNumericId = gameState.turnState.currentTeamId.Value,
                currentPhase = gameState.turnState.currentPhase.ToString(),
                phaseStepIndex = gameState.turnState.phaseStepIndex,
                hasResolvedAnomalyThisTurn = gameState.turnState.hasResolvedAnomalyThisTurn,
            };
        }

        if (gameState.characterSelectionState is not null)
        {
            projection.characterSelection = new ServerCharacterSelectionProjection
            {
                isActive = !gameState.characterSelectionState.isCompleted,
                isCompleted = gameState.characterSelectionState.isCompleted,
                currentSelectingPlayerNumericId =
                    gameState.characterSelectionState.currentSelectingPlayerId?.Value,
            };
            foreach (var selection in gameState.characterSelectionState.selectedCharacterDefinitionIds
                         .OrderBy(entry => entry.Key.Value))
            {
                projection.characterSelection.selections.Add(new ServerSelectedCharacterProjection
                {
                    playerNumericId = selection.Key.Value,
                    characterDefinitionId = selection.Value,
                });
            }
        }

        foreach (var definition in CharacterDefinitionRepository.getAllDefinitions())
        {
            var definitionProjection = new ServerCharacterDefinitionProjection
            {
                definitionId = definition.definitionId,
                characterName = definition.characterName,
                factionKey = definition.factionKey,
                baseMaxHp = definition.baseMaxHp,
                isImplemented = definition.isImplemented,
            };
            definitionProjection.raceTags.AddRange(definition.raceTags);
            foreach (var skill in definition.skills.Values.OrderBy(skill => skill.skillOrder))
            {
                definitionProjection.skills.Add(new ServerCharacterSkillDefinitionProjection
                {
                    skillKey = skill.skillKey,
                    skillName = skill.skillName,
                    skillOrder = skill.skillOrder,
                    skillTypeRaw = skill.skillTypeRaw,
                    skillCostRaw = skill.skillCostRaw,
                    effectText = skill.effectText,
                });
            }
            projection.characterDefinitions.Add(definitionProjection);
        }

        foreach (var teamState in gameState.teams.Values.OrderBy(team => team.teamId.Value))
        {
            var teamProjection = new ServerTeamProjection
            {
                teamNumericId = teamState.teamId.Value,
                leyline = teamState.leyline,
                killScore = teamState.killScore,
            };
            foreach (var memberPlayerId in teamState.memberPlayerIds)
            {
                teamProjection.memberPlayerNumericIds.Add(memberPlayerId.Value);
            }
            projection.teams.Add(teamProjection);
        }

        foreach (var playerState in gameState.players.Values.OrderBy(player => player.playerId.Value))
        {
            var playerProjection = new ServerPlayerProjection
            {
                playerNumericId = playerState.playerId.Value,
                teamNumericId = playerState.teamId.Value,
                activeCharacterInstanceNumericId = playerState.activeCharacterInstanceId?.Value,
                mana = playerState.mana,
                skillPoint = playerState.skillPoint,
                sigilPreview = playerState.sigilPreview,
                lockedSigil = playerState.lockedSigil,
                isSigilLocked = playerState.isSigilLocked,
                deckZone = buildZoneProjection(gameState, playerState.deckZoneId, viewerPlayerId),
                handZone = buildZoneProjection(gameState, playerState.handZoneId, viewerPlayerId),
                discardZone = buildZoneProjection(gameState, playerState.discardZoneId, viewerPlayerId),
                fieldZone = buildZoneProjection(gameState, playerState.fieldZoneId, viewerPlayerId),
                characterSetAsideZone = buildZoneProjection(gameState, playerState.characterSetAsideZoneId, viewerPlayerId),
            };

            var playerStatuses = gameState.statusInstances
                .Where(status => status.targetPlayerId == playerState.playerId)
                .Select(status => status.statusKey)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(statusKey => statusKey, StringComparer.Ordinal);
            playerProjection.statusKeys.AddRange(playerStatuses);
            projection.players.Add(playerProjection);
        }

        if (gameState.publicState is not null)
        {
            projection.publicZones = new ServerPublicZonesProjection
            {
                publicTreasureDeckZone = buildZoneProjection(gameState, gameState.publicState.publicTreasureDeckZoneId, viewerPlayerId, forceVisible: true),
                summonZone = buildZoneProjection(gameState, gameState.publicState.summonZoneId, viewerPlayerId, forceVisible: true),
                gapZone = buildZoneProjection(gameState, gameState.publicState.gapZoneId, viewerPlayerId, forceVisible: true),
                sakuraCakeDeckZone = buildZoneProjection(gameState, gameState.publicState.sakuraCakeDeckZoneId, viewerPlayerId, forceVisible: true),
                anomalyDeckZone = buildZoneProjection(gameState, gameState.publicState.anomalyDeckZoneId, viewerPlayerId, forceVisible: true),
            };
        }

        projection.currentAnomaly = buildCurrentAnomalyProjection(gameState);

        foreach (var characterInstance in gameState.characterInstances.Values.OrderBy(character => character.characterInstanceId.Value))
        {
            var characterProjection = new ServerCharacterProjection
            {
                characterInstanceNumericId = characterInstance.characterInstanceId.Value,
                definitionId = characterInstance.definitionId,
                factionKey = CharacterDefinitionRepository.resolveByDefinitionId(characterInstance.definitionId).factionKey,
                ownerPlayerNumericId = characterInstance.ownerPlayerId.Value,
                currentHp = characterInstance.currentHp,
                maxHp = characterInstance.maxHp,
                isAlive = characterInstance.isAlive,
                isInPlay = characterInstance.isInPlay,
                isActivated = characterInstance.isActivated,
            };
            characterProjection.raceTags.AddRange(
                characterInstance.raceTags
                    .Where(raceTag => !string.IsNullOrWhiteSpace(raceTag))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(raceTag => raceTag, StringComparer.Ordinal));

            var characterStatuses = gameState.statusInstances
                .Where(status => status.targetCharacterInstanceId == characterInstance.characterInstanceId)
                .Select(status => status.statusKey)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(statusKey => statusKey, StringComparer.Ordinal);
            characterProjection.statusKeys.AddRange(characterStatuses);

            foreach (var markerEntry in characterInstance.markerState.markerMap
                         .Where(entry => entry.Value > 0)
                         .OrderBy(entry => entry.Key, StringComparer.Ordinal))
            {
                characterProjection.markers.Add(new ServerMarkerProjection
                {
                    markerTypeKey = markerEntry.Key,
                    count = markerEntry.Value,
                    maxCount = MarkerRuntime.getMarkerCap(characterInstance, markerEntry.Key),
                    displayNameKey = markerEntry.Key,
                });
            }

            projection.characters.Add(characterProjection);
        }

        return projection;
    }

    private static ServerAnomalyProjection? buildCurrentAnomalyProjection(GameState gameState)
    {
        if (gameState.currentAnomalyState is null ||
            string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
        {
            return null;
        }

        var anomalyDefinition = AnomalyDefinitionRepository.resolveByDefinitionId(
            gameState.currentAnomalyState.currentAnomalyDefinitionId);
        return new ServerAnomalyProjection
        {
            definitionId = anomalyDefinition.definitionId,
            name = anomalyDefinition.name,
            arrivalText = anomalyDefinition.arrivalText,
            resolveText = anomalyDefinition.resolveText,
            oncePerTurnHint = anomalyDefinition.oncePerTurnHint,
            resolveConditionKey = anomalyDefinition.resolveConditionKey,
            resolveRewardKey = anomalyDefinition.resolveRewardKey,
            remainingDeckCount = gameState.currentAnomalyState.anomalyDeckDefinitionIds.Count,
            hasResolvedThisTurn = gameState.turnState?.hasResolvedAnomalyThisTurn ?? false,
        };
    }

    public static ServerInteractionProjection buildInteractionProjection(GameState gameState, PlayerId viewerPlayerId)
    {
        var interactionProjection = new ServerInteractionProjection();
        if (gameState.currentInputContext is not null)
        {
            var hasParallelRequiredPlayers = gameState.currentInputContext.requiredPlayerIds.Count > 0;
            var isViewerRequiredPlayer = hasParallelRequiredPlayers
                ? gameState.currentInputContext.requiredPlayerIds.Contains(viewerPlayerId)
                : gameState.currentInputContext.requiredPlayerId == viewerPlayerId;
            var isViewerSubmittedPlayer = hasParallelRequiredPlayers &&
                gameState.currentInputContext.submittedPlayerIds.Contains(viewerPlayerId);
            var inputContextProjection = new ServerInputContextProjection
            {
                inputContextNumericId = gameState.currentInputContext.inputContextId.Value,
                requiredPlayerNumericId = gameState.currentInputContext.requiredPlayerId?.Value,
                isViewerRequiredPlayer = isViewerRequiredPlayer,
                isViewerSubmittedPlayer = isViewerSubmittedPlayer,
                inputTypeKey = gameState.currentInputContext.inputTypeKey,
                contextKey = gameState.currentInputContext.contextKey,
                selectedChoiceKey = gameState.currentInputContext.selectedChoiceKey,
            };

            if (hasParallelRequiredPlayers)
            {
                foreach (var requiredPlayerId in gameState.currentInputContext.requiredPlayerIds)
                {
                    inputContextProjection.requiredPlayerNumericIds.Add(requiredPlayerId.Value);
                }

                foreach (var submittedPlayerId in gameState.currentInputContext.submittedPlayerIds)
                {
                    inputContextProjection.submittedPlayerNumericIds.Add(submittedPlayerId.Value);
                }

                inputContextProjection.requiredPlayerCount = inputContextProjection.requiredPlayerNumericIds.Count;
                inputContextProjection.submittedPlayerCount = inputContextProjection.submittedPlayerNumericIds.Count;

                if (isViewerRequiredPlayer &&
                    gameState.currentInputContext.choiceKeysByRequiredPlayerNumericId.TryGetValue(viewerPlayerId.Value, out var viewerChoiceKeys))
                {
                    inputContextProjection.choiceKeys.AddRange(viewerChoiceKeys);
                }
                inputContextProjection.choiceCount = inputContextProjection.choiceKeys.Count;
            }
            else
            {
                inputContextProjection.requiredPlayerCount =
                    gameState.currentInputContext.requiredPlayerId.HasValue ? 1 : 0;
                inputContextProjection.submittedPlayerCount = 0;
                if (gameState.currentInputContext.requiredPlayerId is null || isViewerRequiredPlayer)
                {
                    inputContextProjection.choiceKeys.AddRange(gameState.currentInputContext.choiceKeys);
                }

                inputContextProjection.choiceCount = gameState.currentInputContext.choiceKeys.Count;
            }

            interactionProjection.inputContext = inputContextProjection;
        }

        if (gameState.currentResponseWindow is not null)
        {
            var responseWindowProjection = new ServerResponseWindowProjection
            {
                responseWindowNumericId = gameState.currentResponseWindow.responseWindowId.Value,
                windowTypeKey = gameState.currentResponseWindow.windowTypeKey,
                responseWindowOriginType = gameState.currentResponseWindow.originType.ToString(),
                currentResponderPlayerNumericId = gameState.currentResponseWindow.currentResponderPlayerId?.Value,
                isViewerCurrentResponder = gameState.currentResponseWindow.currentResponderPlayerId == viewerPlayerId,
                pendingDamageResponseStageKey = gameState.currentResponseWindow.pendingDamageResponseStageKey,
                pendingDamageTypeKey = gameState.currentResponseWindow.pendingDamageTypeKey,
                pendingDamageTargetCharacterInstanceNumericId = gameState.currentResponseWindow.pendingDamageTargetCharacterInstanceId?.Value,
                pendingDamageDefenderPlayerNumericId = gameState.currentResponseWindow.pendingDamageDefenderPlayerId?.Value,
            };
            foreach (var responderPlayerId in gameState.currentResponseWindow.responderPlayerIds)
            {
                responseWindowProjection.responderPlayerNumericIds.Add(responderPlayerId.Value);
            }

            interactionProjection.responseWindow = responseWindowProjection;
        }

        return interactionProjection;
    }

    public static List<ServerEventLogEntry> buildEventLog(List<GameEvent> producedEvents)
    {
        var projectedEvents = new List<ServerEventLogEntry>();
        foreach (var producedEvent in producedEvents)
        {
            if (producedEvent is CharacterSelectedEvent characterSelectedEvent)
            {
                projectedEvents.Add(new ServerEventLogEntry
                {
                    eventId = characterSelectedEvent.eventId,
                    eventTypeKey = characterSelectedEvent.eventTypeKey,
                    ownerPlayerNumericId = characterSelectedEvent.playerId.Value,
                    targetCharacterInstanceNumericId =
                        characterSelectedEvent.characterInstanceId.Value,
                    definitionId = characterSelectedEvent.characterDefinitionId,
                });
                continue;
            }

            if (producedEvent is CardMovedEvent cardMovedEvent)
            {
                projectedEvents.Add(new ServerEventLogEntry
                {
                    eventId = cardMovedEvent.eventId,
                    eventTypeKey = cardMovedEvent.eventTypeKey,
                    sourceActionChainNumericId = cardMovedEvent.sourceActionChainId?.Value,
                    cardInstanceNumericId = cardMovedEvent.cardInstanceId.Value,
                    fromZoneKey = cardMovedEvent.fromZoneKey.ToString(),
                    toZoneKey = cardMovedEvent.toZoneKey.ToString(),
                    moveReason = cardMovedEvent.moveReason.ToString(),
                });
                continue;
            }

            if (producedEvent is HpChangedEvent hpChangedEvent)
            {
                projectedEvents.Add(new ServerEventLogEntry
                {
                    eventId = hpChangedEvent.eventId,
                    eventTypeKey = hpChangedEvent.eventTypeKey,
                    sourceActionChainNumericId = hpChangedEvent.sourceActionChainId?.Value,
                    targetPlayerNumericId = hpChangedEvent.targetPlayerId.Value,
                    targetCharacterInstanceNumericId = hpChangedEvent.targetCharacterInstanceId?.Value,
                    hpBefore = hpChangedEvent.hpBefore,
                    hpAfter = hpChangedEvent.hpAfter,
                    delta = hpChangedEvent.delta,
                });
                continue;
            }

            if (producedEvent is CardRevealedEvent cardRevealedEvent)
            {
                projectedEvents.Add(new ServerEventLogEntry
                {
                    eventId = cardRevealedEvent.eventId,
                    eventTypeKey = cardRevealedEvent.eventTypeKey,
                    sourceActionChainNumericId = cardRevealedEvent.sourceActionChainId?.Value,
                    cardInstanceNumericId = cardRevealedEvent.cardInstanceId.Value,
                    ownerPlayerNumericId = cardRevealedEvent.ownerPlayerId.Value,
                    definitionId = cardRevealedEvent.definitionId,
                    revealReasonKey = cardRevealedEvent.revealReasonKey,
                });
                continue;
            }

            if (producedEvent is StatusChangedEvent statusChangedEvent)
            {
                projectedEvents.Add(new ServerEventLogEntry
                {
                    eventId = statusChangedEvent.eventId,
                    eventTypeKey = statusChangedEvent.eventTypeKey,
                    sourceActionChainNumericId = statusChangedEvent.sourceActionChainId?.Value,
                    statusKey = statusChangedEvent.statusKey,
                    targetCardInstanceNumericId = statusChangedEvent.targetCardInstanceId?.Value,
                    targetCharacterInstanceNumericId = statusChangedEvent.targetCharacterInstanceId?.Value,
                    targetPlayerNumericId = statusChangedEvent.targetPlayerId?.Value,
                    isApplied = statusChangedEvent.isApplied,
                });
                continue;
            }

            if (producedEvent is DamageResolvedEvent damageResolvedEvent)
            {
                projectedEvents.Add(new ServerEventLogEntry
                {
                    eventId = damageResolvedEvent.eventId,
                    eventTypeKey = damageResolvedEvent.eventTypeKey,
                    sourceActionChainNumericId = damageResolvedEvent.sourceActionChainId?.Value,
                    damageContextNumericId = damageResolvedEvent.damageContextId.Value,
                    finalDamageValue = damageResolvedEvent.finalDamageValue,
                    didDealDamage = damageResolvedEvent.didDealDamage,
                });
                continue;
            }

            if (producedEvent is MarkerChangedEvent markerChangedEvent)
            {
                projectedEvents.Add(new ServerEventLogEntry
                {
                    eventId = markerChangedEvent.eventId,
                    eventTypeKey = markerChangedEvent.eventTypeKey,
                    sourceActionChainNumericId = markerChangedEvent.sourceActionChainId?.Value,
                    targetPlayerNumericId = markerChangedEvent.targetPlayerId.Value,
                    targetCharacterInstanceNumericId = markerChangedEvent.targetCharacterInstanceId.Value,
                    markerTypeKey = markerChangedEvent.markerTypeKey,
                    markerBeforeCount = markerChangedEvent.beforeCount,
                    markerAfterCount = markerChangedEvent.afterCount,
                    delta = markerChangedEvent.delta,
                });
                continue;
            }

            if (producedEvent is CharacterActivationChangedEvent activationChangedEvent)
            {
                projectedEvents.Add(new ServerEventLogEntry
                {
                    eventId = activationChangedEvent.eventId,
                    eventTypeKey = activationChangedEvent.eventTypeKey,
                    sourceActionChainNumericId = activationChangedEvent.sourceActionChainId?.Value,
                    targetPlayerNumericId = activationChangedEvent.targetPlayerId.Value,
                    targetCharacterInstanceNumericId = activationChangedEvent.targetCharacterInstanceId.Value,
                    wasActivated = activationChangedEvent.wasActivated,
                    isActivated = activationChangedEvent.isActivated,
                });
                continue;
            }

            if (producedEvent is AnomalyRewardPlaceholderEvent anomalyRewardPlaceholderEvent)
            {
                projectedEvents.Add(new ServerEventLogEntry
                {
                    eventId = anomalyRewardPlaceholderEvent.eventId,
                    eventTypeKey = anomalyRewardPlaceholderEvent.eventTypeKey,
                    sourceActionChainNumericId = anomalyRewardPlaceholderEvent.sourceActionChainId?.Value,
                    definitionId = anomalyRewardPlaceholderEvent.anomalyDefinitionId,
                    message = anomalyRewardPlaceholderEvent.message,
                });
                continue;
            }

            if (producedEvent is InteractionWindowEvent interactionWindowEvent &&
                (string.Equals(interactionWindowEvent.eventTypeKey, "inputContextOpened", StringComparison.Ordinal) ||
                 string.Equals(interactionWindowEvent.eventTypeKey, "responseWindowOpened", StringComparison.Ordinal)))
            {
                projectedEvents.Add(new ServerEventLogEntry
                {
                    eventId = interactionWindowEvent.eventId,
                    eventTypeKey = interactionWindowEvent.eventTypeKey,
                    sourceActionChainNumericId = interactionWindowEvent.sourceActionChainId?.Value,
                    windowKindKey = interactionWindowEvent.windowKindKey,
                    isOpened = interactionWindowEvent.isOpened,
                    responseWindowNumericId = interactionWindowEvent.responseWindowId?.Value,
                    inputContextNumericId = interactionWindowEvent.inputContextId?.Value,
                });
            }
        }

        return projectedEvents;
    }

    public static PlayerId resolveViewerPlayerId(GameState gameState, long preferredViewerPlayerNumericId)
    {
        var preferredViewerPlayerId = new PlayerId(preferredViewerPlayerNumericId);
        if (gameState.players.ContainsKey(preferredViewerPlayerId))
        {
            return preferredViewerPlayerId;
        }

        if (gameState.turnState is not null && gameState.players.ContainsKey(gameState.turnState.currentPlayerId))
        {
            return gameState.turnState.currentPlayerId;
        }

        foreach (var playerId in gameState.players.Keys.OrderBy(player => player.Value))
        {
            return playerId;
        }

        return preferredViewerPlayerId;
    }

    private static ServerZoneProjection buildZoneProjection(
        GameState gameState,
        ZoneId zoneId,
        PlayerId viewerPlayerId,
        bool forceVisible = false)
    {
        if (!gameState.zones.TryGetValue(zoneId, out var zoneState))
        {
            return new ServerZoneProjection
            {
                zoneNumericId = zoneId.Value,
                zoneKey = "unknown",
                cardCount = 0,
                isContentVisible = false,
                hiddenCardCount = 0,
            };
        }

        var isContentVisible = forceVisible ||
                               zoneState.publicOrPrivate == ZonePublicOrPrivate.publicZone ||
                               zoneState.ownerPlayerId == viewerPlayerId;
        var zoneProjection = new ServerZoneProjection
        {
            zoneNumericId = zoneState.zoneId.Value,
            zoneKey = zoneState.zoneType.ToString(),
            cardCount = zoneState.cardInstanceIds.Count,
            isContentVisible = isContentVisible,
            hiddenCardCount = isContentVisible ? 0 : zoneState.cardInstanceIds.Count,
        };

        if (!isContentVisible)
        {
            return zoneProjection;
        }

        foreach (var cardInstanceId in zoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance))
            {
                continue;
            }

            zoneProjection.cards.Add(new ServerCardProjection
            {
                cardInstanceNumericId = cardInstance.cardInstanceId.Value,
                definitionId = cardInstance.definitionId,
                ownerPlayerNumericId = cardInstance.ownerPlayerId.Value,
                zoneKey = cardInstance.zoneKey.ToString(),
                isFaceUp = cardInstance.isFaceUp,
                isSetAside = cardInstance.isSetAside,
                isDefensePlacedOnField = cardInstance.isDefensePlacedOnField,
                overlayContainerCardInstanceNumericId = cardInstance.overlayContainerCardInstanceId?.Value,
                overlayOrderIndex = cardInstance.overlayOrderIndex,
                overlayCardCount = OverlayRuntime.getOverlayCardCount(gameState, cardInstance.cardInstanceId),
            });
        }

        return zoneProjection;
    }
}
