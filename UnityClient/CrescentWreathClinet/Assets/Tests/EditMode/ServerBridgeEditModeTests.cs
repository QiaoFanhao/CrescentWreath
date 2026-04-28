using System;
using System.Collections.Generic;
using CrescentWreath.Client.Net;
using NUnit.Framework;
using UnityEngine;

namespace CrescentWreath.Client.Tests.EditMode
{
public class ServerBridgeEditModeTests
{
    [Test]
    public void SendDrawOneCard_ShouldBuildExpectedEnvelope()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 1001,
            actorPlayerNumericId = 1001,
        };

        bridge.SendDrawOneCard();

        Assert.That(fakeSocketClient.lastSentText, Is.Not.Empty);
        var root = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(root.actionType, Is.EqualTo("drawOneCard"));
        Assert.That(root.requestId, Is.EqualTo(1));
        Assert.That(root.viewerPlayerNumericId, Is.EqualTo(1001));
        Assert.That(root.payload.actorPlayerNumericId, Is.EqualTo(1001));
    }

    [Test]
    public void SendDrawOneCard_ShouldEmitRawRequestBeforeSend()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 1,
            actorPlayerNumericId = 1,
        };

        string? emittedRawRequest = null;
        bridge.OnRawRequest += rawRequest => emittedRawRequest = rawRequest;

        bridge.SendDrawOneCard();

        Assert.That(emittedRawRequest, Is.Not.Null.And.Not.Empty);
        Assert.That(emittedRawRequest, Is.EqualTo(fakeSocketClient.lastSentText));
    }

    [Test]
    public void SendPlayTreasureCard_ShouldBuildExpectedEnvelope()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 1002,
            actorPlayerNumericId = 1002,
        };

        bridge.SendPlayTreasureCard(9001);

        var root = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(root.actionType, Is.EqualTo("playTreasureCard"));
        Assert.That(root.viewerPlayerNumericId, Is.EqualTo(1002));
        Assert.That(root.payload.actorPlayerNumericId, Is.EqualTo(1002));
        Assert.That(root.payload.cardInstanceNumericId, Is.EqualTo(9001));
        Assert.That(root.payload.playMode, Is.EqualTo("normal"));
    }

    [Test]
    public void ParseSummary_ShouldReadMinimalFieldsSafely()
    {
        const string responseJson =
            "{"
            + "\"requestId\":1,"
            + "\"isSucceeded\":true,"
            + "\"stateProjection\":{"
            + "\"turn\":{\"currentPhase\":\"action\",\"currentPlayerNumericId\":1001},"
            + "\"players\":[{"
            + "\"playerNumericId\":1001,"
            + "\"handZone\":{\"cardCount\":5}"
            + "}]},"
            + "\"eventLog\":[{\"eventTypeKey\":\"cardMoved\"}]"
            + "}";

        var summary = ServerBridge.ParseSummary(responseJson, 1001);

        Assert.That(summary.isSucceeded, Is.True);
        Assert.That(summary.currentPhase, Is.EqualTo("action"));
        Assert.That(summary.currentPlayerNumericId, Is.EqualTo(1001));
        Assert.That(summary.myHandCount, Is.EqualTo(5));
        Assert.That(summary.recentEventTypeKey, Is.EqualTo("cardMoved"));
    }

    [Test]
    public void ParseSummary_WhenFieldsMissing_ShouldNotThrow()
    {
        var summary = ServerBridge.ParseSummary("{\"isSucceeded\":false}", 1001);

        Assert.That(summary.isSucceeded, Is.False);
        Assert.That(summary.currentPhase, Is.EqualTo(string.Empty));
        Assert.That(summary.myHandCount, Is.EqualTo(0));
    }

    [Test]
    public void SendPhaseActions_ShouldBuildExpectedEnvelope()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 1,
            actorPlayerNumericId = 1,
        };

        bridge.SendEnterActionPhase();
        var enterActionRoot = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(enterActionRoot.actionType, Is.EqualTo("enterActionPhase"));
        Assert.That(enterActionRoot.payload.actorPlayerNumericId, Is.EqualTo(1));

        bridge.SendEnterSummonPhase();
        var enterSummonRoot = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(enterSummonRoot.actionType, Is.EqualTo("enterSummonPhase"));
        Assert.That(enterSummonRoot.payload.actorPlayerNumericId, Is.EqualTo(1));

        bridge.SendEnterEndPhase();
        var enterEndRoot = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(enterEndRoot.actionType, Is.EqualTo("enterEndPhase"));
        Assert.That(enterEndRoot.payload.actorPlayerNumericId, Is.EqualTo(1));

        bridge.SendStartNextTurn();
        var startNextTurnRoot = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(startNextTurnRoot.actionType, Is.EqualTo("startNextTurn"));
        Assert.That(startNextTurnRoot.payload.actorPlayerNumericId, Is.EqualTo(1));
    }

    [Test]
    public void SendSummonAndDefense_ShouldBuildExpectedEnvelope()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 1,
            actorPlayerNumericId = 1,
        };

        bridge.SendSummonTreasureCard(9002);
        var summonRoot = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(summonRoot.actionType, Is.EqualTo("summonTreasureCard"));
        Assert.That(summonRoot.payload.actorPlayerNumericId, Is.EqualTo(1));
        Assert.That(summonRoot.payload.cardInstanceNumericId, Is.EqualTo(9002));

        bridge.SendSubmitDefenseFixedReduce1();
        var fixedDefenseRoot = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(fixedDefenseRoot.actionType, Is.EqualTo("submitDefense"));
        Assert.That(fixedDefenseRoot.payload.defenseTypeKey, Is.EqualTo("fixedReduce1"));
        Assert.That(fixedDefenseRoot.payload.defenseCardInstanceNumericId, Is.EqualTo(0));

        bridge.SendSubmitDefenseFormal("physical", 1234);
        var formalDefenseRoot = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(formalDefenseRoot.actionType, Is.EqualTo("submitDefense"));
        Assert.That(formalDefenseRoot.payload.defenseTypeKey, Is.EqualTo("physical"));
        Assert.That(formalDefenseRoot.payload.defenseCardInstanceNumericId, Is.EqualTo(1234));
    }

    [Test]
    public void ParseSummary_ShouldUseResponseViewerAndInteraction()
    {
        const string responseJson =
            "{"
            + "\"requestId\":9,"
            + "\"viewerPlayerNumericId\":1,"
            + "\"isSucceeded\":false,"
            + "\"stateProjection\":{"
            + "\"turn\":{\"currentPhase\":\"summon\",\"currentPlayerNumericId\":1},"
            + "\"players\":[{"
            + "\"playerNumericId\":1,"
            + "\"handZone\":{\"cardCount\":6}"
            + "},{"
            + "\"playerNumericId\":1001,"
            + "\"handZone\":{\"cardCount\":0}"
            + "}]},"
            + "\"interaction\":{"
            + "\"inputContext\":{\"requiredPlayerNumericId\":2,\"choiceCount\":3},"
            + "\"responseWindow\":{\"currentResponderPlayerNumericId\":4,\"responderPlayerNumericIds\":[1,2]}"
            + "}"
            + "}";

        var summary = ServerBridge.ParseSummary(responseJson, 1001);

        Assert.That(summary.viewerPlayerNumericId, Is.EqualTo(1));
        Assert.That(summary.currentPhase, Is.EqualTo("summon"));
        Assert.That(summary.currentPlayerNumericId, Is.EqualTo(1));
        Assert.That(summary.myHandCount, Is.EqualTo(6));
        Assert.That(summary.hasInputContext, Is.True);
        Assert.That(summary.inputRequiredPlayerNumericId, Is.EqualTo(2));
        Assert.That(summary.inputChoiceCount, Is.EqualTo(3));
        Assert.That(summary.hasResponseWindow, Is.True);
        Assert.That(summary.responseCurrentResponderPlayerNumericId, Is.EqualTo(4));
        Assert.That(summary.responseResponderCount, Is.EqualTo(2));
    }

    [Test]
    public void ProjectionParser_ParseCompleteResponse_ShouldExtractProjectionFields()
    {
        const string responseJson =
            "{"
            + "\"requestId\":10,"
            + "\"viewerPlayerNumericId\":1,"
            + "\"isSucceeded\":true,"
            + "\"stateProjection\":{"
            + "\"turn\":{\"turnNumber\":3,\"currentPhase\":\"summon\",\"currentPlayerNumericId\":2},"
            + "\"players\":[{"
            + "\"playerNumericId\":1,"
            + "\"activeCharacterInstanceNumericId\":200001,"
            + "\"mana\":4,\"skillPoint\":2,\"sigilPreview\":1,\"lockedSigil\":3,\"isSigilLocked\":true,"
            + "\"handCardCount\":2,"
            + "\"handZone\":{\"cardCount\":2,\"cards\":[{\"cardInstanceNumericId\":101,\"definitionId\":\"T001\",\"zoneKey\":\"hand\"},{\"cardInstanceNumericId\":102,\"definitionId\":\"T002\",\"zoneKey\":\"hand\"}]},"
            + "\"fieldZone\":{\"cardCount\":1,\"cards\":[{\"cardInstanceNumericId\":201,\"definitionId\":\"T003\",\"zoneKey\":\"field\"}]},"
            + "\"discardZone\":{\"cardCount\":5,\"cards\":[]}"
            + "}],"
            + "\"publicZones\":{"
            + "\"summonZone\":{\"cardCount\":1,\"cards\":[{\"cardInstanceNumericId\":301,\"definitionId\":\"T004\",\"zoneKey\":\"summonZone\"}]},"
            + "\"sakuraCakeDeckZone\":{\"cardCount\":2,\"cards\":[{\"cardInstanceNumericId\":401,\"definitionId\":\"S001\",\"zoneKey\":\"sakuraCakeDeck\"},{\"cardInstanceNumericId\":402,\"definitionId\":\"S001\",\"zoneKey\":\"sakuraCakeDeck\"}]}"
            + "},"
            + "\"characters\":[{\"characterInstanceNumericId\":200001,\"currentHp\":3,\"maxHp\":4,\"statusKeys\":[\"Seal\",\"Shackle\"]}]"
            + "},"
            + "\"eventLog\":[{\"eventTypeKey\":\"cardMoved\",\"cardInstanceNumericId\":101,\"moveReason\":\"play\"}],"
            + "\"interaction\":{\"inputContext\":{\"requiredPlayerNumericId\":1,\"choiceCount\":2},\"responseWindow\":{\"currentResponderPlayerNumericId\":2,\"responderPlayerNumericIds\":[1,2]}}"
            + "}";

        var projection = ProjectionParser.Parse(responseJson, 1);

        Assert.That(projection.isSucceeded, Is.True);
        Assert.That(projection.viewerPlayerNumericId, Is.EqualTo(1));
        Assert.That(projection.turnNumber, Is.EqualTo(3));
        Assert.That(projection.currentPhase, Is.EqualTo("summon"));
        Assert.That(projection.currentPlayerNumericId, Is.EqualTo(2));
        Assert.That(projection.mana, Is.EqualTo(4));
        Assert.That(projection.skillPoint, Is.EqualTo(2));
        Assert.That(projection.sigilPreview, Is.EqualTo(1));
        Assert.That(projection.lockedSigil, Is.EqualTo(3));
        Assert.That(projection.viewerHandCardCount, Is.EqualTo(2));
        Assert.That(projection.handCards.Count, Is.EqualTo(2));
        Assert.That(projection.fieldCards.Count, Is.EqualTo(1));
        Assert.That(projection.discardCount, Is.EqualTo(5));
        Assert.That(projection.summonZoneCards.Count, Is.EqualTo(1));
        Assert.That(projection.sakuraCakeCards.Count, Is.EqualTo(2));
        Assert.That(projection.activeCharacterCurrentHp, Is.EqualTo(3));
        Assert.That(projection.activeCharacterMaxHp, Is.EqualTo(4));
        Assert.That(projection.activeCharacterStatusKeys, Is.EquivalentTo(new[] { "Seal", "Shackle" }));
        Assert.That(projection.eventLog.Count, Is.EqualTo(1));
        Assert.That(projection.recentEventTypeKey, Is.EqualTo("cardMoved"));
        Assert.That(projection.interaction.hasInputContext, Is.True);
        Assert.That(projection.interaction.inputRequiredPlayerNumericId, Is.EqualTo(1));
        Assert.That(projection.interaction.inputChoiceCount, Is.EqualTo(2));
        Assert.That(projection.interaction.hasResponseWindow, Is.True);
        Assert.That(projection.interaction.responseCurrentResponderPlayerNumericId, Is.EqualTo(2));
        Assert.That(projection.interaction.responseResponderCount, Is.EqualTo(2));
    }

    [Test]
    public void ProjectionParser_ParseMissingFields_ShouldReturnSafeDefaults()
    {
        var projection = ProjectionParser.Parse("{\"isSucceeded\":false}", 1);

        Assert.That(projection.isSucceeded, Is.False);
        Assert.That(projection.viewerPlayerNumericId, Is.EqualTo(1));
        Assert.That(projection.currentPhase, Is.EqualTo(string.Empty));
        Assert.That(projection.handCards, Is.Empty);
        Assert.That(projection.fieldCards, Is.Empty);
        Assert.That(projection.summonZoneCards, Is.Empty);
        Assert.That(projection.sakuraCakeCards, Is.Empty);
        Assert.That(projection.eventLog, Is.Empty);
    }

    [Test]
    public void Bridge_OnProjectionUpdated_WhenFailureWithoutState_ShouldKeepLastProjectionData()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 1,
            actorPlayerNumericId = 1,
        };

        ProjectionViewModel? lastProjection = null;
        bridge.OnProjectionUpdated += projection => lastProjection = projection;

        const string successResponseJson =
            "{"
            + "\"isSucceeded\":true,"
            + "\"viewerPlayerNumericId\":1,"
            + "\"stateProjection\":{"
            + "\"turn\":{\"turnNumber\":1,\"currentPhase\":\"action\",\"currentPlayerNumericId\":1},"
            + "\"players\":[{\"playerNumericId\":1,\"handCardCount\":1,\"handZone\":{\"cardCount\":1,\"cards\":[{\"cardInstanceNumericId\":1001,\"definitionId\":\"T001\",\"zoneKey\":\"hand\"}]},\"fieldZone\":{\"cardCount\":0,\"cards\":[]},\"discardZone\":{\"cardCount\":0,\"cards\":[]}}],"
            + "\"publicZones\":{\"summonZone\":{\"cardCount\":0,\"cards\":[]}}"
            + "},"
            + "\"eventLog\":[{\"eventTypeKey\":\"cardMoved\"}]"
            + "}";

        fakeSocketClient.EmitText(successResponseJson);
        Assert.That(lastProjection, Is.Not.Null);
        Assert.That(lastProjection!.handCards.Count, Is.EqualTo(1));
        Assert.That(lastProjection.errorCode, Is.EqualTo(string.Empty));

        const string failedResponseWithoutStateJson =
            "{"
            + "\"isSucceeded\":false,"
            + "\"viewerPlayerNumericId\":1,"
            + "\"error\":{\"code\":\"request_rejected\",\"message\":\"mock rejection\"}"
            + "}";

        fakeSocketClient.EmitText(failedResponseWithoutStateJson);
        Assert.That(lastProjection, Is.Not.Null);
        Assert.That(lastProjection!.isSucceeded, Is.False);
        Assert.That(lastProjection.errorCode, Is.EqualTo("request_rejected"));
        Assert.That(lastProjection.errorMessage, Is.EqualTo("mock rejection"));
        Assert.That(lastProjection.handCards.Count, Is.EqualTo(1));
    }

    [Test]
    public void SelectionHelper_PruneSelectionIfMissing_ShouldKeepSelectionWhenCardStillExists()
    {
        var cards = new List<ProjectionCardViewModel>
        {
            new ProjectionCardViewModel { cardInstanceNumericId = 1001, definitionId = "T001", zoneKey = "hand" },
            new ProjectionCardViewModel { cardInstanceNumericId = 1002, definitionId = "T002", zoneKey = "hand" },
        };

        var prunedSelection = SocketDebugPanel.PruneSelectionIfMissing(1002, cards);

        Assert.That(prunedSelection, Is.EqualTo(1002));
    }

    [Test]
    public void SelectionHelper_PruneSelectionIfMissing_ShouldClearSelectionWhenCardRemoved()
    {
        var cards = new List<ProjectionCardViewModel>
        {
            new ProjectionCardViewModel { cardInstanceNumericId = 1001, definitionId = "T001", zoneKey = "hand" },
        };

        var prunedSelection = SocketDebugPanel.PruneSelectionIfMissing(2000, cards);

        Assert.That(prunedSelection, Is.Null);
    }

    [Test]
    public void SelectionHelper_TryResolveSelectedOrManualCardId_ShouldPreferSelectedOverManual()
    {
        var resolved = SocketDebugPanel.TryResolveSelectedOrManualCardId(1001, "9999", out var resolvedCardId);

        Assert.That(resolved, Is.True);
        Assert.That(resolvedCardId, Is.EqualTo(1001));
    }

    [Test]
    public void SelectionHelper_TryResolveSelectedCardId_WhenMissing_ShouldReturnFalse()
    {
        var resolved = SocketDebugPanel.TryResolveSelectedCardId(null, out _);

        Assert.That(resolved, Is.False);
    }

    [Test]
    public void SelectionHelper_TryResolveSelectedOrManualCardId_WhenMissingAndManualInvalid_ShouldReturnFalse()
    {
        var resolved = SocketDebugPanel.TryResolveSelectedOrManualCardId(null, "invalid-id", out _);

        Assert.That(resolved, Is.False);
    }

    private static EnvelopeDto parseEnvelope(string json)
    {
        var parsed = JsonUtility.FromJson<EnvelopeDto>(json);
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.payload, Is.Not.Null);
        return parsed;
    }

    [Serializable]
    private sealed class EnvelopeDto
    {
        public long requestId;
        public long viewerPlayerNumericId;
        public string actionType = string.Empty;
        public PayloadDto payload = new();
    }

    [Serializable]
    private sealed class PayloadDto
    {
        public long actorPlayerNumericId;
        public long cardInstanceNumericId;
        public string playMode = string.Empty;
        public string defenseTypeKey = string.Empty;
        public long defenseCardInstanceNumericId;
    }

    private sealed class FakeSocketClient : ITextSocketClient
    {
        public event Action? OnConnected;
        public event Action<string>? OnDisconnected;
        public event Action<string>? OnTextMessage;
        public event Action<string>? OnError;

        public bool isConnected { get; private set; }
        public string lastSentText { get; private set; } = string.Empty;

        public void Connect(string url)
        {
            isConnected = true;
            OnConnected?.Invoke();
        }

        public void Disconnect()
        {
            isConnected = false;
            OnDisconnected?.Invoke("manual");
        }

        public void SendText(string text)
        {
            lastSentText = text;
        }

        public void Dispose()
        {
        }

        public void EmitText(string json)
        {
            OnTextMessage?.Invoke(json);
        }

        public void EmitError(string error)
        {
            OnError?.Invoke(error);
        }
    }
}
}
