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

    private static void fillInteraction(ProjectionViewModel projection, InteractionDto? interaction)
    {
        if (interaction?.inputContext is not null)
        {
            projection.interaction.hasInputContext = true;
            if (interaction.inputContext.requiredPlayerNumericId > 0)
            {
                projection.interaction.inputRequiredPlayerNumericId = interaction.inputContext.requiredPlayerNumericId;
            }

            projection.interaction.inputChoiceCount = interaction.inputContext.choiceCount;
        }

        if (interaction?.responseWindow is not null)
        {
            projection.interaction.hasResponseWindow = true;
            if (interaction.responseWindow.currentResponderPlayerNumericId > 0)
            {
                projection.interaction.responseCurrentResponderPlayerNumericId = interaction.responseWindow.currentResponderPlayerNumericId;
            }

            projection.interaction.responseResponderCount = interaction.responseWindow.responderPlayerNumericIds?.Length ?? 0;
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
        public PlayerProjectionDto[]? players;
        public PublicZonesProjectionDto? publicZones;
        public CharacterProjectionDto[]? characters;
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
        public long activeCharacterInstanceNumericId;
        public int mana;
        public int skillPoint;
        public int sigilPreview;
        public int lockedSigil;
        public bool isSigilLocked;
        public int handCardCount;
        public ZoneProjectionDto? handZone;
        public ZoneProjectionDto? fieldZone;
        public ZoneProjectionDto? discardZone;
    }

    [Serializable]
    private sealed class PublicZonesProjectionDto
    {
        public ZoneProjectionDto? summonZone;
        public ZoneProjectionDto? sakuraCakeDeckZone;
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
        public long requiredPlayerNumericId;
        public int choiceCount;
    }

    [Serializable]
    private sealed class ResponseWindowDto
    {
        public long currentResponderPlayerNumericId;
        public long[]? responderPlayerNumericIds;
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
