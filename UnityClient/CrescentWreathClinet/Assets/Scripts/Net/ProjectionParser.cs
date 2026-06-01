using System;
using System.Linq;
using UnityEngine;

namespace CrescentWreath.Client.Net
{
public static class ProjectionParser
{
    private const int EventLogLimit = 20;

    public static ProjectionViewModel Parse(string rawJson, long fallbackViewerPlayerNumericId)
    {
        var projection = ProjectionViewModel.createDefault(fallbackViewerPlayerNumericId);
        try
        {
            var response = JsonUtility.FromJson<ResponseEnvelopeDto>(rawJson);
            if (response is null)
            {
                projection.errorCode = "parse_error";
                projection.errorMessage = "Response envelope is null.";
                return projection;
            }

            var resolvedViewerPlayerNumericId = response.viewerPlayerNumericId > 0
                ? response.viewerPlayerNumericId
                : fallbackViewerPlayerNumericId;

            projection.viewerPlayerNumericId = resolvedViewerPlayerNumericId;
            projection.isSucceeded = response.isSucceeded;
            projection.hasStateProjection = response.stateProjection is not null;

            if (response.error is not null)
            {
                projection.errorCode = response.error.code ?? string.Empty;
                projection.errorMessage = response.error.message ?? string.Empty;
            }

            if (response.stateProjection?.turn is not null)
            {
                projection.turnNumber = response.stateProjection.turn.turnNumber;
                projection.currentPhase = response.stateProjection.turn.currentPhase ?? string.Empty;
                if (response.stateProjection.turn.currentPlayerNumericId > 0)
                {
                    projection.currentPlayerNumericId = response.stateProjection.turn.currentPlayerNumericId;
                }
            }

            fillTeamSummaries(projection, response.stateProjection?.teams);
            fillPlayerSummaries(projection, response.stateProjection?.players, response.stateProjection?.characters);

            var viewerPlayer = response.stateProjection?.players?
                .FirstOrDefault(player => player is not null && player.playerNumericId == resolvedViewerPlayerNumericId);

            if (viewerPlayer is not null)
            {
                projection.mana = viewerPlayer.mana;
                projection.skillPoint = viewerPlayer.skillPoint;
                projection.sigilPreview = viewerPlayer.sigilPreview;
                projection.lockedSigil = viewerPlayer.isSigilLocked ? viewerPlayer.lockedSigil : null;
                projection.viewerHandCardCount = viewerPlayer.handCardCount > 0
                    ? viewerPlayer.handCardCount
                    : viewerPlayer.handZone?.cardCount ?? 0;
                projection.discardCount = viewerPlayer.discardZone?.cardCount ?? 0;

                fillCards(projection.handCards, viewerPlayer.handZone);
                fillCards(projection.discardCards, viewerPlayer.discardZone);
                fillCards(projection.fieldCards, viewerPlayer.fieldZone);

                if (viewerPlayer.activeCharacterInstanceNumericId > 0 && response.stateProjection?.characters is not null)
                {
                    var activeCharacter = response.stateProjection.characters
                        .FirstOrDefault(character =>
                            character is not null &&
                            character.characterInstanceNumericId == viewerPlayer.activeCharacterInstanceNumericId);
                    if (activeCharacter is not null)
                    {
                        projection.activeCharacterCurrentHp = activeCharacter.currentHp;
                        projection.activeCharacterMaxHp = activeCharacter.maxHp;
                        if (activeCharacter.statusKeys is not null)
                        {
                            projection.activeCharacterStatusKeys.AddRange(
                                activeCharacter.statusKeys.Where(statusKey => !string.IsNullOrWhiteSpace(statusKey)));
                        }
                    }
                }
            }

            fillCards(projection.summonZoneCards, response.stateProjection?.publicZones?.summonZone);
            fillCards(projection.sakuraCakeCards, response.stateProjection?.publicZones?.sakuraCakeDeckZone);
            fillCards(projection.gapZoneCards, response.stateProjection?.publicZones?.gapZone);
            fillInteraction(projection, response.interaction);
            fillEventLog(projection, response.eventLog);

            return projection;
        }
        catch (Exception exception)
        {
            projection.isSucceeded = false;
            projection.errorCode = "parse_error";
            projection.errorMessage = exception.Message;
            projection.hasStateProjection = false;
            return projection;
        }
    }

    private static void fillCards(System.Collections.Generic.List<ProjectionCardViewModel> targetCards, ZoneProjectionDto? zone)
    {
        if (zone?.cards is null)
        {
            return;
        }

        foreach (var card in zone.cards)
        {
            if (card is null || card.cardInstanceNumericId <= 0)
            {
                continue;
            }

            targetCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = card.cardInstanceNumericId,
                definitionId = card.definitionId ?? string.Empty,
                zoneKey = card.zoneKey ?? string.Empty,
            });
        }
    }

    private static void fillTeamSummaries(
        ProjectionViewModel projection,
        TeamProjectionDto[]? teams)
    {
        if (teams is null)
        {
            return;
        }

        foreach (var team in teams)
        {
            if (team is null || team.teamNumericId <= 0)
            {
                continue;
            }

            projection.teamSummaries.Add(new ProjectionTeamSummaryViewModel
            {
                teamNumericId = team.teamNumericId,
                leyline = team.leyline,
                killScore = team.killScore,
            });
        }
    }

    private static void fillPlayerSummaries(
        ProjectionViewModel projection,
        PlayerProjectionDto[]? players,
        CharacterProjectionDto[]? characters)
    {
        if (players is null)
        {
            return;
        }

        foreach (var player in players)
        {
            if (player is null || player.playerNumericId <= 0)
            {
                continue;
            }

            var summary = new ProjectionPlayerSummaryViewModel
            {
                playerNumericId = player.playerNumericId,
                teamNumericId = player.teamNumericId,
                isCurrentPlayer = projection.currentPlayerNumericId.HasValue &&
                                  projection.currentPlayerNumericId.Value == player.playerNumericId,
                isViewerPlayer = player.playerNumericId == projection.viewerPlayerNumericId,
                mana = player.mana,
                skillPoint = player.skillPoint,
                sigilPreview = player.sigilPreview,
                isSigilLocked = player.isSigilLocked,
                lockedSigil = player.isSigilLocked ? player.lockedSigil : null,
                handCount = player.handCardCount > 0
                    ? player.handCardCount
                    : player.handZone?.cardCount ?? 0,
                fieldCount = player.fieldZone?.cardCount ?? 0,
                discardCount = player.discardZone?.cardCount ?? 0,
                activeCharacterInstanceNumericId = player.activeCharacterInstanceNumericId > 0
                    ? player.activeCharacterInstanceNumericId
                    : null,
            };

            if (player.statusKeys is not null)
            {
                foreach (var statusKey in player.statusKeys)
                {
                    if (string.IsNullOrWhiteSpace(statusKey))
                    {
                        continue;
                    }

                    summary.playerStatusKeys.Add(statusKey);
                }
            }

            if (summary.activeCharacterInstanceNumericId.HasValue && characters is not null)
            {
                var activeCharacter = characters.FirstOrDefault(character =>
                    character is not null &&
                    character.characterInstanceNumericId == summary.activeCharacterInstanceNumericId.Value);
                if (activeCharacter is not null)
                {
                    summary.activeCharacterCurrentHp = activeCharacter.currentHp;
                    summary.activeCharacterMaxHp = activeCharacter.maxHp;
                    if (activeCharacter.statusKeys is not null)
                    {
                        foreach (var statusKey in activeCharacter.statusKeys)
                        {
                            if (string.IsNullOrWhiteSpace(statusKey))
                            {
                                continue;
                            }

                            summary.activeCharacterStatusKeys.Add(statusKey);
                        }
                    }
                }
            }

            projection.playerSummaries.Add(summary);
        }
    }

    private static void fillInteraction(ProjectionViewModel projection, InteractionDto? interaction)
    {
        if (interaction?.inputContext is not null &&
            interaction.inputContext.inputContextNumericId > 0)
        {
            projection.interaction.hasInputContext = true;
            projection.interaction.inputContextNumericId = interaction.inputContext.inputContextNumericId;
            if (interaction.inputContext.requiredPlayerNumericId > 0)
            {
                projection.interaction.inputRequiredPlayerNumericId = interaction.inputContext.requiredPlayerNumericId;
            }

            if (interaction.inputContext.requiredPlayerNumericIds is not null)
            {
                foreach (var requiredPlayerNumericId in interaction.inputContext.requiredPlayerNumericIds)
                {
                    if (requiredPlayerNumericId <= 0)
                    {
                        continue;
                    }

                    projection.interaction.inputRequiredPlayerNumericIds.Add(requiredPlayerNumericId);
                }
            }

            if (interaction.inputContext.submittedPlayerNumericIds is not null)
            {
                foreach (var submittedPlayerNumericId in interaction.inputContext.submittedPlayerNumericIds)
                {
                    if (submittedPlayerNumericId <= 0)
                    {
                        continue;
                    }

                    projection.interaction.inputSubmittedPlayerNumericIds.Add(submittedPlayerNumericId);
                }
            }

            projection.interaction.inputRequiredPlayerCount = interaction.inputContext.requiredPlayerCount;
            projection.interaction.inputSubmittedPlayerCount = interaction.inputContext.submittedPlayerCount;
            projection.interaction.isViewerRequiredPlayerForInput = interaction.inputContext.isViewerRequiredPlayer;
            projection.interaction.isViewerSubmittedInput = interaction.inputContext.isViewerSubmittedPlayer;
            projection.interaction.inputTypeKey = interaction.inputContext.inputTypeKey ?? string.Empty;
            projection.interaction.contextKey = interaction.inputContext.contextKey ?? string.Empty;
            projection.interaction.inputChoiceCount = interaction.inputContext.choiceCount;
            if (interaction.inputContext.choiceKeys is not null)
            {
                foreach (var choiceKey in interaction.inputContext.choiceKeys)
                {
                    if (string.IsNullOrWhiteSpace(choiceKey))
                    {
                        continue;
                    }

                    projection.interaction.inputChoiceKeys.Add(choiceKey);
                }
            }

            projection.interaction.selectedChoiceKey = interaction.inputContext.selectedChoiceKey ?? string.Empty;
        }

        if (interaction?.responseWindow is null)
        {
            return;
        }

        if (interaction.responseWindow.responseWindowNumericId <= 0)
        {
            projection.interaction.hasResponseWindow = false;
            projection.interaction.responseWindowNumericId = null;
            projection.interaction.responseCurrentResponderPlayerNumericId = null;
            projection.interaction.responseResponderCount = 0;
            projection.interaction.responseWindowOriginType = string.Empty;
            projection.interaction.pendingDamageResponseStageKey = string.Empty;
            projection.interaction.pendingDamageTypeKey = string.Empty;
            projection.interaction.pendingDamageTargetCharacterInstanceNumericId = null;
            projection.interaction.pendingDamageDefenderPlayerNumericId = null;
            return;
        }

        projection.interaction.hasResponseWindow = true;
        projection.interaction.responseWindowNumericId = interaction.responseWindow.responseWindowNumericId;
        if (interaction.responseWindow.currentResponderPlayerNumericId > 0)
        {
            projection.interaction.responseCurrentResponderPlayerNumericId = interaction.responseWindow.currentResponderPlayerNumericId;
        }

        projection.interaction.responseResponderCount = interaction.responseWindow.responderPlayerNumericIds?.Length ?? 0;
        projection.interaction.responseWindowOriginType = interaction.responseWindow.responseWindowOriginType ?? string.Empty;
        projection.interaction.pendingDamageResponseStageKey = interaction.responseWindow.pendingDamageResponseStageKey ?? string.Empty;
        projection.interaction.pendingDamageTypeKey = interaction.responseWindow.pendingDamageTypeKey ?? string.Empty;
        if (interaction.responseWindow.pendingDamageTargetCharacterInstanceNumericId > 0)
        {
            projection.interaction.pendingDamageTargetCharacterInstanceNumericId =
                interaction.responseWindow.pendingDamageTargetCharacterInstanceNumericId;
        }

        if (interaction.responseWindow.pendingDamageDefenderPlayerNumericId > 0)
        {
            projection.interaction.pendingDamageDefenderPlayerNumericId =
                interaction.responseWindow.pendingDamageDefenderPlayerNumericId;
        }
    }

    private static void fillEventLog(ProjectionViewModel projection, EventLogEntryDto[]? eventLog)
    {
        if (eventLog is null || eventLog.Length == 0)
        {
            return;
        }

        var startIndex = Math.Max(0, eventLog.Length - EventLogLimit);
        for (var index = startIndex; index < eventLog.Length; index++)
        {
            var eventEntry = eventLog[index];
            if (eventEntry is null)
            {
                continue;
            }

            var eventTypeKey = !string.IsNullOrWhiteSpace(eventEntry.eventTypeKey)
                ? eventEntry.eventTypeKey
                : eventEntry.eventType ?? string.Empty;
            if (string.IsNullOrWhiteSpace(eventTypeKey))
            {
                eventTypeKey = "unknownEvent";
            }

            projection.recentEventTypeKey = eventTypeKey;
            projection.eventLog.Add(buildEventLine(eventTypeKey, eventEntry));
        }
    }

    private static string buildEventLine(string eventTypeKey, EventLogEntryDto eventEntry)
    {
        var eventLine = eventTypeKey;

        if (eventEntry.cardInstanceNumericId > 0)
        {
            eventLine += $" card={eventEntry.cardInstanceNumericId}";
        }

        if (!string.IsNullOrWhiteSpace(eventEntry.moveReason))
        {
            eventLine += $" move={eventEntry.moveReason}";
        }

        if (eventEntry.finalDamageValue.HasValue)
        {
            eventLine += $" dmg={eventEntry.finalDamageValue.Value}";
        }

        return eventLine;
    }

    [Serializable]
    private sealed class ResponseEnvelopeDto
    {
        public long viewerPlayerNumericId;
        public bool isSucceeded;
        public ErrorDto? error;
        public StateProjectionDto? stateProjection;
        public InteractionDto? interaction;
        public EventLogEntryDto[]? eventLog;
    }

    [Serializable]
    private sealed class ErrorDto
    {
        public string? code;
        public string? message;
    }

    [Serializable]
    private sealed class StateProjectionDto
    {
        public TurnDto? turn;
        public TeamProjectionDto[]? teams;
        public PlayerProjectionDto[]? players;
        public PublicZonesProjectionDto? publicZones;
        public CharacterProjectionDto[]? characters;
    }

    [Serializable]
    private sealed class TeamProjectionDto
    {
        public long teamNumericId;
        public int leyline;
        public int killScore;
    }

    [Serializable]
    private sealed class TurnDto
    {
        public int turnNumber;
        public string? currentPhase;
        public long currentPlayerNumericId;
    }

    [Serializable]
    private sealed class PlayerProjectionDto
    {
        public long playerNumericId;
        public long teamNumericId;
        public long activeCharacterInstanceNumericId;
        public int mana;
        public int skillPoint;
        public int sigilPreview;
        public int lockedSigil;
        public bool isSigilLocked;
        public int handCardCount;
        public string[]? statusKeys;
        public ZoneProjectionDto? handZone;
        public ZoneProjectionDto? fieldZone;
        public ZoneProjectionDto? discardZone;
    }

    [Serializable]
    private sealed class PublicZonesProjectionDto
    {
        public ZoneProjectionDto? summonZone;
        public ZoneProjectionDto? sakuraCakeDeckZone;
        public ZoneProjectionDto? gapZone;
    }

    [Serializable]
    private sealed class CharacterProjectionDto
    {
        public long characterInstanceNumericId;
        public int currentHp;
        public int maxHp;
        public string[]? statusKeys;
    }

    [Serializable]
    private sealed class ZoneProjectionDto
    {
        public int cardCount;
        public CardProjectionDto[]? cards;
    }

    [Serializable]
    private sealed class CardProjectionDto
    {
        public long cardInstanceNumericId;
        public string? definitionId;
        public string? zoneKey;
    }

    [Serializable]
    private sealed class InteractionDto
    {
        public InputContextDto? inputContext;
        public ResponseWindowDto? responseWindow;
    }

    [Serializable]
    private sealed class InputContextDto
    {
        public long inputContextNumericId;
        public long requiredPlayerNumericId;
        public long[]? requiredPlayerNumericIds;
        public long[]? submittedPlayerNumericIds;
        public int requiredPlayerCount;
        public int submittedPlayerCount;
        public bool isViewerRequiredPlayer;
        public bool isViewerSubmittedPlayer;
        public string? inputTypeKey;
        public string? contextKey;
        public int choiceCount;
        public string[]? choiceKeys;
        public string? selectedChoiceKey;
    }

    [Serializable]
    private sealed class ResponseWindowDto
    {
        public long responseWindowNumericId;
        public string? responseWindowOriginType;
        public long currentResponderPlayerNumericId;
        public long[]? responderPlayerNumericIds;
        public string? pendingDamageResponseStageKey;
        public string? pendingDamageTypeKey;
        public long pendingDamageTargetCharacterInstanceNumericId;
        public long pendingDamageDefenderPlayerNumericId;
    }

    [Serializable]
    private sealed class EventLogEntryDto
    {
        public string eventTypeKey = string.Empty;
        public string? eventType;
        public long cardInstanceNumericId;
        public string? moveReason;
        public int? finalDamageValue;
    }
}
}
