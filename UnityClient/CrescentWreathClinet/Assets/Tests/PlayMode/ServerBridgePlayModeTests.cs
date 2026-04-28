using System;
using CrescentWreath.Client.Net;
using NUnit.Framework;
using UnityEngine;

namespace CrescentWreath.Client.Tests.PlayMode
{
public class ServerBridgePlayModeTests
{
    [Test]
    public void BridgeFlow_ShouldPublishSummaryFromIncomingResponse()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 1,
            actorPlayerNumericId = 1,
        };

        ServerResponseSummary? receivedSummary = null;
        bridge.OnSummaryUpdated += summary => receivedSummary = summary;

        bridge.Connect("ws://127.0.0.1:18080/ws");
        bridge.SendDrawOneCard();

        const string responseJson =
            "{" +
            "\"isSucceeded\":true," +
            "\"viewerPlayerNumericId\":1," +
            "\"stateProjection\":{" +
            "\"turn\":{\"currentPhase\":\"action\",\"currentPlayerNumericId\":1}," +
            "\"players\":[{" +
            "\"playerNumericId\":1," +
            "\"handZone\":{\"cardCount\":6}" +
            "}]}," +
            "\"eventLog\":[{\"eventTypeKey\":\"cardMoved\"}]" +
            "}";

        fakeSocketClient.EmitText(responseJson);

        Assert.That(receivedSummary, Is.Not.Null);
        Assert.That(receivedSummary!.isSucceeded, Is.True);
        Assert.That(receivedSummary.currentPhase, Is.EqualTo("action"));
        Assert.That(receivedSummary.myHandCount, Is.EqualTo(6));
        Assert.That(receivedSummary.recentEventTypeKey, Is.EqualTo("cardMoved"));
    }

    [Test]
    public void BridgeFlow_ShouldDriveProjectionForMinimalTurnSequence()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 1,
            actorPlayerNumericId = 1,
        };
        var flowRuntime = new DebugFlowChecklistRuntime();

        var rawRequestCount = 0;
        var rawResponseCount = 0;
        ProjectionViewModel? latestProjection = null;
        var previousProjection = ProjectionViewModel.createDefault(1);
        previousProjection.isSucceeded = true;
        previousProjection.hasStateProjection = true;
        previousProjection.currentPhase = "start";
        previousProjection.turnNumber = 1;
        previousProjection.currentPlayerNumericId = 1;
        previousProjection.viewerHandCardCount = 0;

        bridge.OnRawRequest += _ => rawRequestCount++;
        bridge.OnRawResponse += _ => rawResponseCount++;
        bridge.OnProjectionUpdated += projection => latestProjection = projection;

        bridge.Connect("ws://127.0.0.1:18080/ws");
        flowRuntime.OnConnectionStateChanged("connected");

        long? selectedHandCardId = null;
        long? selectedSummonCardId = null;

        bridge.SendEnterActionPhase();
        fakeSocketClient.EmitText(buildResponse("action", 1, 1, new[] { 1001L }, new[] { 9001L }, "phaseChanged"));
        Assert.That(latestProjection, Is.Not.Null);
        Assert.That(latestProjection!.currentPhase, Is.EqualTo("action"));
        flowRuntime.RecordProjectionResponse("enterActionPhase", latestProjection, previousProjection, playSelectionCleared: true, summonSelectionCleared: true);
        previousProjection = latestProjection.deepClone();

        bridge.SendDrawOneCard();
        fakeSocketClient.EmitText(buildResponse("action", 1, 1, new[] { 1001L, 1002L }, new[] { 9001L }, "cardMoved"));
        Assert.That(latestProjection!.handCards.Count, Is.EqualTo(2));
        flowRuntime.RecordProjectionResponse("drawOneCard", latestProjection, previousProjection, playSelectionCleared: true, summonSelectionCleared: true);
        previousProjection = latestProjection.deepClone();
        selectedHandCardId = latestProjection.handCards[0].cardInstanceNumericId;
        Assert.That(selectedHandCardId, Is.Not.Null);

        bridge.SendPlayTreasureCard(selectedHandCardId!.Value);
        fakeSocketClient.EmitText(buildResponse("action", 1, 1, new[] { 1002L }, new[] { 9001L }, "cardMoved"));
        Assert.That(latestProjection.currentPhase, Is.EqualTo("action"));
        Assert.That(latestProjection.handCards.Count, Is.EqualTo(1));
        if (latestProjection.handCards.TrueForAll(card => card.cardInstanceNumericId != selectedHandCardId.Value))
        {
            selectedHandCardId = null;
        }
        Assert.That(selectedHandCardId, Is.Null);
        flowRuntime.RecordProjectionResponse("playTreasureCard", latestProjection, previousProjection, playSelectionCleared: true, summonSelectionCleared: true);
        previousProjection = latestProjection.deepClone();

        bridge.SendEnterSummonPhase();
        fakeSocketClient.EmitText(buildResponse("summon", 1, 1, new[] { 1002L }, new[] { 9001L, 9002L }, "phaseChanged"));
        Assert.That(latestProjection.currentPhase, Is.EqualTo("summon"));
        Assert.That(latestProjection.summonZoneCards.Count, Is.EqualTo(2));
        flowRuntime.RecordProjectionResponse("enterSummonPhase", latestProjection, previousProjection, playSelectionCleared: true, summonSelectionCleared: true);
        previousProjection = latestProjection.deepClone();
        selectedSummonCardId = latestProjection.summonZoneCards[0].cardInstanceNumericId;
        Assert.That(selectedSummonCardId, Is.Not.Null);

        bridge.SendSummonTreasureCard(selectedSummonCardId!.Value);
        fakeSocketClient.EmitText(buildResponse("summon", 1, 1, new[] { 1002L }, new[] { 9002L }, "cardMoved"));
        Assert.That(latestProjection.summonZoneCards.Count, Is.EqualTo(1));
        if (latestProjection.summonZoneCards.TrueForAll(card => card.cardInstanceNumericId != selectedSummonCardId.Value))
        {
            selectedSummonCardId = null;
        }
        Assert.That(selectedSummonCardId, Is.Null);
        flowRuntime.RecordProjectionResponse("summonTreasureCard", latestProjection, previousProjection, playSelectionCleared: true, summonSelectionCleared: true);
        previousProjection = latestProjection.deepClone();

        bridge.SendEnterEndPhase();
        fakeSocketClient.EmitText(buildResponse("end", 1, 1, new[] { 1002L }, new[] { 9002L }, "phaseChanged"));
        Assert.That(latestProjection.currentPhase, Is.EqualTo("end"));
        flowRuntime.RecordProjectionResponse("enterEndPhase", latestProjection, previousProjection, playSelectionCleared: true, summonSelectionCleared: true);
        previousProjection = latestProjection.deepClone();

        bridge.SendStartNextTurn();
        fakeSocketClient.EmitText(buildResponse("start", 2, 2, new[] { 1002L }, new[] { 9002L }, "turnStarted"));
        Assert.That(latestProjection.currentPhase, Is.EqualTo("start"));
        Assert.That(latestProjection.turnNumber, Is.EqualTo(2));
        Assert.That(latestProjection.currentPlayerNumericId, Is.EqualTo(2));
        flowRuntime.RecordProjectionResponse("startNextTurn", latestProjection, previousProjection, playSelectionCleared: true, summonSelectionCleared: true);
        previousProjection = latestProjection.deepClone();

        bridge.SendEnterActionPhase();
        fakeSocketClient.EmitText(buildResponse("action", 2, 2, new[] { 1002L }, new[] { 9002L }, "phaseChanged"));
        Assert.That(latestProjection.currentPhase, Is.EqualTo("action"));
        Assert.That(latestProjection.currentPlayerNumericId, Is.EqualTo(2));
        flowRuntime.RecordProjectionResponse("enterActionPhase", latestProjection, previousProjection, playSelectionCleared: true, summonSelectionCleared: true);

        Assert.That(rawRequestCount, Is.EqualTo(8));
        Assert.That(rawResponseCount, Is.EqualTo(8));
        Assert.That(latestProjection.eventLog, Is.Not.Empty);
        Assert.That(flowRuntime.isCompleted, Is.True);
        Assert.That(flowRuntime.currentStepIndex, Is.EqualTo(10));
    }

    [Test]
    public void BridgeFlow_ShouldParseSakuraCakeCardsAndSendSummonBySelectedCardId()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 1,
            actorPlayerNumericId = 1,
        };

        ProjectionViewModel? latestProjection = null;
        bridge.OnProjectionUpdated += projection => latestProjection = projection;

        bridge.Connect("ws://127.0.0.1:18080/ws");
        fakeSocketClient.EmitText(
            buildResponse(
                phase: "summon",
                turnNumber: 1,
                currentPlayerNumericId: 1,
                handCardIds: new[] { 1001L },
                summonCardIds: new[] { 9001L },
                eventTypeKey: "phaseChanged",
                sakuraCardIds: new[] { 7001L, 7002L }));

        Assert.That(latestProjection, Is.Not.Null);
        Assert.That(latestProjection!.sakuraCakeCards.Count, Is.EqualTo(2));
        Assert.That(latestProjection.sakuraCakeCards[0].definitionId, Is.EqualTo("S001"));

        var selectedSakuraCardId = latestProjection.sakuraCakeCards[0].cardInstanceNumericId;
        bridge.SendSummonTreasureCard(selectedSakuraCardId);

        Assert.That(fakeSocketClient.lastSentText, Is.Not.Empty);
        var envelope = JsonUtility.FromJson<EnvelopeDto>(fakeSocketClient.lastSentText);
        Assert.That(envelope, Is.Not.Null);
        Assert.That(envelope!.actionType, Is.EqualTo("summonTreasureCard"));
        Assert.That(envelope.payload.actorPlayerNumericId, Is.EqualTo(1));
        Assert.That(envelope.payload.cardInstanceNumericId, Is.EqualTo(selectedSakuraCardId));
    }

    private static string buildResponse(
        string phase,
        int turnNumber,
        long currentPlayerNumericId,
        long[] handCardIds,
        long[] summonCardIds,
        string eventTypeKey,
        long[]? sakuraCardIds = null)
    {
        sakuraCardIds ??= System.Array.Empty<long>();
        var handCardsJson = string.Join(
            ",",
            System.Array.ConvertAll(
                handCardIds,
                cardId => $"{{\"cardInstanceNumericId\":{cardId},\"definitionId\":\"T{cardId % 1000:000}\",\"zoneKey\":\"hand\"}}"));
        var summonCardsJson = string.Join(
            ",",
            System.Array.ConvertAll(
                summonCardIds,
                cardId => $"{{\"cardInstanceNumericId\":{cardId},\"definitionId\":\"T{cardId % 1000:000}\",\"zoneKey\":\"summonZone\"}}"));
        var sakuraCardsJson = string.Join(
            ",",
            System.Array.ConvertAll(
                sakuraCardIds,
                cardId => $"{{\"cardInstanceNumericId\":{cardId},\"definitionId\":\"S001\",\"zoneKey\":\"sakuraCakeDeck\"}}"));

        return
            "{"
            + "\"isSucceeded\":true,"
            + "\"viewerPlayerNumericId\":1,"
            + "\"stateProjection\":{"
            + "\"turn\":{"
            + "\"turnNumber\":" + turnNumber + ","
            + "\"currentPhase\":\"" + phase + "\","
            + "\"currentPlayerNumericId\":" + currentPlayerNumericId
            + "},"
            + "\"players\":[{"
            + "\"playerNumericId\":1,"
            + "\"activeCharacterInstanceNumericId\":200001,"
            + "\"mana\":1,\"skillPoint\":1,\"sigilPreview\":0,\"lockedSigil\":0,\"isSigilLocked\":false,"
            + "\"handCardCount\":" + handCardIds.Length + ","
            + "\"handZone\":{\"cardCount\":" + handCardIds.Length + ",\"cards\":[" + handCardsJson + "]},"
            + "\"fieldZone\":{\"cardCount\":0,\"cards\":[]},"
            + "\"discardZone\":{\"cardCount\":0,\"cards\":[]}"
            + "}],"
            + "\"publicZones\":{"
            + "\"summonZone\":{\"cardCount\":" + summonCardIds.Length + ",\"cards\":[" + summonCardsJson + "]},"
            + "\"sakuraCakeDeckZone\":{\"cardCount\":" + sakuraCardIds.Length + ",\"cards\":[" + sakuraCardsJson + "]}"
            + "},"
            + "\"characters\":[{\"characterInstanceNumericId\":200001,\"currentHp\":4,\"maxHp\":4,\"statusKeys\":[]}]"
            + "},"
            + "\"eventLog\":[{\"eventTypeKey\":\"" + eventTypeKey + "\"}]"
            + "}";
    }

    [Serializable]
    private sealed class EnvelopeDto
    {
        public string actionType = string.Empty;
        public PayloadDto payload = new();
    }

    [Serializable]
    private sealed class PayloadDto
    {
        public long actorPlayerNumericId;
        public long cardInstanceNumericId;
    }

    private sealed class FakeSocketClient : ITextSocketClient
    {
        public event System.Action? OnConnected;
        public event System.Action<string>? OnDisconnected;
        public event System.Action<string>? OnTextMessage;
        public event System.Action<string>? OnError;

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
    }
}
}


