using System;
using System.Collections.Generic;
using System.Linq;
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
    public List<ServerCharacterProjection> characters { get; } = new();
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

public sealed class ServerCharacterProjection
{
    public long characterInstanceNumericId { get; set; }
    public string definitionId { get; set; } = string.Empty;
    public long ownerPlayerNumericId { get; set; }
    public int currentHp { get; set; }
    public int maxHp { get; set; }
    public bool isAlive { get; set; }
    public bool isInPlay { get; set; }
    public List<string> statusKeys { get; } = new();
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
    public bool isViewerRequiredPlayer { get; set; }
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

        foreach (var characterInstance in gameState.characterInstances.Values.OrderBy(character => character.characterInstanceId.Value))
        {
            var characterProjection = new ServerCharacterProjection
            {
                characterInstanceNumericId = characterInstance.characterInstanceId.Value,
                definitionId = characterInstance.definitionId,
                ownerPlayerNumericId = characterInstance.ownerPlayerId.Value,
                currentHp = characterInstance.currentHp,
                maxHp = characterInstance.maxHp,
                isAlive = characterInstance.isAlive,
                isInPlay = characterInstance.isInPlay,
            };

            var characterStatuses = gameState.statusInstances
                .Where(status => status.targetCharacterInstanceId == characterInstance.characterInstanceId)
                .Select(status => status.statusKey)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(statusKey => statusKey, StringComparer.Ordinal);
            characterProjection.statusKeys.AddRange(characterStatuses);
            projection.characters.Add(characterProjection);
        }

        return projection;
    }

    public static ServerInteractionProjection buildInteractionProjection(GameState gameState, PlayerId viewerPlayerId)
    {
        var interactionProjection = new ServerInteractionProjection();
        if (gameState.currentInputContext is not null)
        {
            var isViewerRequiredPlayer = gameState.currentInputContext.requiredPlayerId == viewerPlayerId;
            var inputContextProjection = new ServerInputContextProjection
            {
                inputContextNumericId = gameState.currentInputContext.inputContextId.Value,
                requiredPlayerNumericId = gameState.currentInputContext.requiredPlayerId?.Value,
                isViewerRequiredPlayer = isViewerRequiredPlayer,
                inputTypeKey = gameState.currentInputContext.inputTypeKey,
                contextKey = gameState.currentInputContext.contextKey,
                choiceCount = gameState.currentInputContext.choiceKeys.Count,
                selectedChoiceKey = gameState.currentInputContext.selectedChoiceKey,
            };
            if (gameState.currentInputContext.requiredPlayerId is null || isViewerRequiredPlayer)
            {
                inputContextProjection.choiceKeys.AddRange(gameState.currentInputContext.choiceKeys);
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
            });
        }

        return zoneProjection;
    }
}
