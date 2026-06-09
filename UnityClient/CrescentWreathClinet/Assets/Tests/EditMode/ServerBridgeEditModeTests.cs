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
    public void Connect_ShouldAppendViewerQueryParameter()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 3,
        };

        bridge.Connect("ws://127.0.0.1:18080/ws");

        Assert.That(fakeSocketClient.lastConnectUrl, Is.EqualTo("ws://127.0.0.1:18080/ws?viewerPlayerNumericId=3"));
    }

    [Test]
    public void Connect_WhenUrlAlreadyContainsQuery_ShouldAppendViewerWithAmpersand()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 4,
        };

        bridge.Connect("ws://127.0.0.1:18080/ws?debug=true");

        Assert.That(fakeSocketClient.lastConnectUrl, Is.EqualTo("ws://127.0.0.1:18080/ws?debug=true&viewerPlayerNumericId=4"));
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
    public void SendSubmitResponseNo_WhenResponseWindowExists_ShouldBuildExpectedEnvelope()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 2,
        };

        fakeSocketClient.EmitText(
            "{"
            + "\"isSucceeded\":true,"
            + "\"viewerPlayerNumericId\":2,"
            + "\"interaction\":{\"responseWindow\":{\"responseWindowNumericId\":789,\"currentResponderPlayerNumericId\":2,\"responderPlayerNumericIds\":[2,1]}}"
            + "}");

        bridge.SendSubmitResponseNo();

        var root = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(root.actionType, Is.EqualTo("submitResponse"));
        Assert.That(root.payload.actorPlayerNumericId, Is.EqualTo(2));
        Assert.That(root.payload.responseWindowNumericId, Is.EqualTo(789));
        Assert.That(root.payload.shouldRespond, Is.False);
    }

    [Test]
    public void SendSubmitResponseNo_WhenResponseWindowMissing_ShouldEmitErrorAndNotSendRequest()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 1,
            actorPlayerNumericId = 1,
        };

        string? capturedError = null;
        bridge.OnError += error => capturedError = error;

        bridge.SendSubmitResponseNo();

        Assert.That(capturedError, Is.Not.Null.And.Contains("requires an active responseWindow"));
        Assert.That(fakeSocketClient.lastSentText, Is.EqualTo(string.Empty));
    }

    [Test]
    public void SendSubmitResponseNoAsActor_WhenResponseWindowExists_ShouldUseLocalPlayerAndIgnoreOverrideActor()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 1,
        };

        fakeSocketClient.EmitText(
            "{"
            + "\"isSucceeded\":true,"
            + "\"viewerPlayerNumericId\":1,"
            + "\"interaction\":{\"responseWindow\":{\"responseWindowNumericId\":790,\"currentResponderPlayerNumericId\":2,\"responderPlayerNumericIds\":[2,1]}}"
            + "}");

        bridge.SendSubmitResponseNoAsActor(2);

        var root = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(root.actionType, Is.EqualTo("submitResponse"));
        Assert.That(root.payload.actorPlayerNumericId, Is.EqualTo(1));
        Assert.That(root.payload.responseWindowNumericId, Is.EqualTo(790));
        Assert.That(root.payload.shouldRespond, Is.False);
    }

    [Test]
    public void SendSubmitInputChoice_WhenInputContextExists_ShouldBuildExpectedEnvelope()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 2,
        };

        fakeSocketClient.EmitText(
            "{"
            + "\"isSucceeded\":true,"
            + "\"viewerPlayerNumericId\":2,"
            + "\"interaction\":{\"inputContext\":{\"inputContextNumericId\":321,\"requiredPlayerNumericId\":2,\"choiceCount\":2,\"choiceKeys\":[\"confirm\",\"decline\"]}}"
            + "}");

        bridge.SendSubmitInputChoice("confirm");

        var root = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(root.actionType, Is.EqualTo("submitInputChoice"));
        Assert.That(root.payload.actorPlayerNumericId, Is.EqualTo(2));
        Assert.That(root.payload.inputContextNumericId, Is.EqualTo(321));
        Assert.That(root.payload.choiceKey, Is.EqualTo("confirm"));
    }

    [Test]
    public void SendSubmitInputChoice_WhenInputContextMissing_ShouldEmitErrorAndNotSendRequest()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 1,
            actorPlayerNumericId = 1,
        };

        string? capturedError = null;
        bridge.OnError += error => capturedError = error;

        bridge.SendSubmitInputChoice("confirm");

        Assert.That(capturedError, Is.Not.Null.And.Contains("active inputContext"));
        Assert.That(fakeSocketClient.lastSentText, Is.EqualTo(string.Empty));
    }

    [Test]
    public void SendDebugOpenDamageResponseWindow_ShouldBuildExpectedEnvelope()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 2,
        };

        bridge.SendDebugOpenDamageResponseWindow();

        var root = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(root.actionType, Is.EqualTo("debugOpenDamageResponseWindow"));
        Assert.That(root.payload.actorPlayerNumericId, Is.EqualTo(2));
        Assert.That(root.payload.targetCharacterInstanceNumericId, Is.EqualTo(0));
        Assert.That(root.payload.baseDamageValue, Is.EqualTo(2));
        Assert.That(root.payload.damageTypeKey, Is.EqualTo("physical"));
    }

    [Test]
    public void SendDebugOpenDamageResponseWindow_WithTargetAndDamageArgs_ShouldBuildExpectedEnvelope()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 3,
        };

        bridge.SendDebugOpenDamageResponseWindow(targetCharacterInstanceNumericId: 200002, baseDamageValue: 3, damageTypeKey: "physical");

        var root = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(root.actionType, Is.EqualTo("debugOpenDamageResponseWindow"));
        Assert.That(root.payload.actorPlayerNumericId, Is.EqualTo(3));
        Assert.That(root.payload.targetCharacterInstanceNumericId, Is.EqualTo(200002));
        Assert.That(root.payload.baseDamageValue, Is.EqualTo(3));
        Assert.That(root.payload.damageTypeKey, Is.EqualTo("physical"));
    }

    [Test]
    public void SendDebugResetMatch_ShouldBuildExpectedEnvelope()
    {
        var fakeSocketClient = new FakeSocketClient();
        using var bridge = new ServerBridge(fakeSocketClient)
        {
            viewerPlayerNumericId = 2,
        };

        bridge.SendDebugResetMatch();

        var root = parseEnvelope(fakeSocketClient.lastSentText);
        Assert.That(root.actionType, Is.EqualTo("debugResetMatch"));
        Assert.That(root.payload.actorPlayerNumericId, Is.EqualTo(2));
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
            + "\"inputContext\":{\"inputContextNumericId\":77,\"requiredPlayerNumericId\":2,\"inputTypeKey\":\"testInput\",\"contextKey\":\"test:context\",\"choiceCount\":3,\"choiceKeys\":[\"a\",\"b\",\"c\"],\"selectedChoiceKey\":\"b\"},"
            + "\"responseWindow\":{\"responseWindowNumericId\":123,\"currentResponderPlayerNumericId\":4,\"responderPlayerNumericIds\":[1,2]}"
            + "}"
            + "}";

        var summary = ServerBridge.ParseSummary(responseJson, 1001);

        Assert.That(summary.viewerPlayerNumericId, Is.EqualTo(1));
        Assert.That(summary.currentPhase, Is.EqualTo("summon"));
        Assert.That(summary.currentPlayerNumericId, Is.EqualTo(1));
        Assert.That(summary.myHandCount, Is.EqualTo(6));
        Assert.That(summary.hasInputContext, Is.True);
        Assert.That(summary.inputContextNumericId, Is.EqualTo(77));
        Assert.That(summary.inputRequiredPlayerNumericId, Is.EqualTo(2));
        Assert.That(summary.inputChoiceCount, Is.EqualTo(3));
        Assert.That(summary.hasResponseWindow, Is.True);
        Assert.That(summary.responseWindowNumericId, Is.EqualTo(123));
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
            + "\"teams\":[{\"teamNumericId\":1,\"leyline\":4,\"killScore\":9},{\"teamNumericId\":2,\"leyline\":6,\"killScore\":8}],"
            + "\"players\":[{"
            + "\"playerNumericId\":1,"
            + "\"teamNumericId\":1,"
            + "\"activeCharacterInstanceNumericId\":200001,"
            + "\"mana\":4,\"skillPoint\":2,\"sigilPreview\":1,\"lockedSigil\":3,\"isSigilLocked\":true,"
            + "\"handCardCount\":2,"
            + "\"handZone\":{\"cardCount\":2,\"cards\":[{\"cardInstanceNumericId\":101,\"definitionId\":\"T001\",\"zoneKey\":\"hand\"},{\"cardInstanceNumericId\":102,\"definitionId\":\"T002\",\"zoneKey\":\"hand\"}]},"
            + "\"fieldZone\":{\"cardCount\":1,\"cards\":[{\"cardInstanceNumericId\":201,\"definitionId\":\"T003\",\"zoneKey\":\"field\"}]},"
            + "\"discardZone\":{\"cardCount\":5,\"cards\":[]}"
            + "},{"
            + "\"playerNumericId\":2,"
            + "\"teamNumericId\":2,"
            + "\"activeCharacterInstanceNumericId\":200002,"
            + "\"mana\":6,\"skillPoint\":1,\"sigilPreview\":5,\"lockedSigil\":2,\"isSigilLocked\":true,"
            + "\"handCardCount\":7,"
            + "\"handZone\":{\"cardCount\":7,\"cards\":[]},"
            + "\"fieldZone\":{\"cardCount\":2,\"cards\":[{\"cardInstanceNumericId\":202,\"definitionId\":\"T005\",\"zoneKey\":\"field\"},{\"cardInstanceNumericId\":203,\"definitionId\":\"T006\",\"zoneKey\":\"field\"}]},"
            + "\"discardZone\":{\"cardCount\":3,\"cards\":[]}"
            + "}],"
            + "\"publicZones\":{"
            + "\"summonZone\":{\"cardCount\":1,\"cards\":[{\"cardInstanceNumericId\":301,\"definitionId\":\"T004\",\"zoneKey\":\"summonZone\"}]},"
            + "\"sakuraCakeDeckZone\":{\"cardCount\":2,\"cards\":[{\"cardInstanceNumericId\":401,\"definitionId\":\"S001\",\"zoneKey\":\"sakuraCakeDeck\"},{\"cardInstanceNumericId\":402,\"definitionId\":\"S001\",\"zoneKey\":\"sakuraCakeDeck\"}]}"
            + "},"
            + "\"characters\":[{\"characterInstanceNumericId\":200001,\"currentHp\":3,\"maxHp\":4,\"raceTags\":[\"human\"],\"statusKeys\":[\"Seal\",\"Shackle\"]},{\"characterInstanceNumericId\":200002,\"currentHp\":2,\"maxHp\":4,\"raceTags\":[\"nonHuman\"],\"statusKeys\":[\"Penetrate\"]}]"
            + "},"
            + "\"eventLog\":[{\"eventTypeKey\":\"cardMoved\",\"cardInstanceNumericId\":101,\"moveReason\":\"play\"}],"
            + "\"interaction\":{\"inputContext\":{\"inputContextNumericId\":66,\"requiredPlayerNumericId\":1,\"inputTypeKey\":\"testInput\",\"contextKey\":\"test:projection\",\"choiceCount\":2,\"choiceKeys\":[\"accept\",\"decline\"],\"selectedChoiceKey\":\"decline\"},\"responseWindow\":{\"responseWindowNumericId\":88,\"responseWindowOriginType\":\"damageResponse\",\"currentResponderPlayerNumericId\":2,\"responderPlayerNumericIds\":[1,2],\"pendingDamageResponseStageKey\":\"awaitDefense\",\"pendingDamageTypeKey\":\"physical\",\"pendingDamageTargetCharacterInstanceNumericId\":200001,\"pendingDamageDefenderPlayerNumericId\":1}}"
            + "}";

        var projection = ProjectionParser.Parse(responseJson, 1);

        Assert.That(projection.isSucceeded, Is.True);
        Assert.That(projection.viewerPlayerNumericId, Is.EqualTo(1));
        Assert.That(projection.turnNumber, Is.EqualTo(3));
        Assert.That(projection.currentPhase, Is.EqualTo("summon"));
        Assert.That(projection.currentPlayerNumericId, Is.EqualTo(2));
        Assert.That(projection.teamSummaries.Count, Is.EqualTo(2));
        var team1 = projection.teamSummaries.Find(team => team.teamNumericId == 1);
        Assert.That(team1, Is.Not.Null);
        Assert.That(team1!.leyline, Is.EqualTo(4));
        Assert.That(team1.killScore, Is.EqualTo(9));
        var team2 = projection.teamSummaries.Find(team => team.teamNumericId == 2);
        Assert.That(team2, Is.Not.Null);
        Assert.That(team2!.leyline, Is.EqualTo(6));
        Assert.That(team2.killScore, Is.EqualTo(8));
        Assert.That(projection.playerSummaries.Count, Is.EqualTo(2));

        var viewerSummary = projection.playerSummaries.Find(summary => summary.playerNumericId == 1);
        Assert.That(viewerSummary, Is.Not.Null);
        Assert.That(viewerSummary!.isViewerPlayer, Is.True);
        Assert.That(viewerSummary.isCurrentPlayer, Is.False);
        Assert.That(viewerSummary.handCount, Is.EqualTo(2));
        Assert.That(viewerSummary.fieldCount, Is.EqualTo(1));
        Assert.That(viewerSummary.discardCount, Is.EqualTo(5));
        Assert.That(viewerSummary.activeCharacterCurrentHp, Is.EqualTo(3));
        Assert.That(viewerSummary.activeCharacterRaceTags, Is.EquivalentTo(new[] { "human" }));
        Assert.That(viewerSummary.activeCharacterStatusKeys, Is.EquivalentTo(new[] { "Seal", "Shackle" }));

        var currentPlayerSummary = projection.playerSummaries.Find(summary => summary.playerNumericId == 2);
        Assert.That(currentPlayerSummary, Is.Not.Null);
        Assert.That(currentPlayerSummary!.isCurrentPlayer, Is.True);
        Assert.That(currentPlayerSummary.isViewerPlayer, Is.False);
        Assert.That(currentPlayerSummary.handCount, Is.EqualTo(7));
        Assert.That(currentPlayerSummary.fieldCount, Is.EqualTo(2));
        Assert.That(currentPlayerSummary.discardCount, Is.EqualTo(3));
        Assert.That(currentPlayerSummary.activeCharacterCurrentHp, Is.EqualTo(2));
        Assert.That(currentPlayerSummary.activeCharacterRaceTags, Is.EquivalentTo(new[] { "nonHuman" }));
        Assert.That(currentPlayerSummary.activeCharacterStatusKeys, Is.EquivalentTo(new[] { "Penetrate" }));
        Assert.That(projection.mana, Is.EqualTo(4));
        Assert.That(projection.skillPoint, Is.EqualTo(2));
        Assert.That(projection.sigilPreview, Is.EqualTo(1));
        Assert.That(projection.lockedSigil, Is.EqualTo(3));
        Assert.That(projection.viewerHandCardCount, Is.EqualTo(2));
        Assert.That(projection.handCards.Count, Is.EqualTo(2));
        Assert.That(projection.handCards.Exists(card => card.cardInstanceNumericId == 202 || card.cardInstanceNumericId == 203), Is.False);
        Assert.That(projection.fieldCards.Count, Is.EqualTo(1));
        Assert.That(projection.discardCount, Is.EqualTo(5));
        Assert.That(projection.summonZoneCards.Count, Is.EqualTo(1));
        Assert.That(projection.sakuraCakeCards.Count, Is.EqualTo(2));
        Assert.That(projection.activeCharacterCurrentHp, Is.EqualTo(3));
        Assert.That(projection.activeCharacterMaxHp, Is.EqualTo(4));
        Assert.That(projection.activeCharacterRaceTags, Is.EquivalentTo(new[] { "human" }));
        Assert.That(projection.activeCharacterStatusKeys, Is.EquivalentTo(new[] { "Seal", "Shackle" }));
        Assert.That(projection.eventLog.Count, Is.EqualTo(1));
        Assert.That(projection.recentEventTypeKey, Is.EqualTo("cardMoved"));
        Assert.That(projection.interaction.hasInputContext, Is.True);
        Assert.That(projection.interaction.inputContextNumericId, Is.EqualTo(66));
        Assert.That(projection.interaction.inputRequiredPlayerNumericId, Is.EqualTo(1));
        Assert.That(projection.interaction.inputTypeKey, Is.EqualTo("testInput"));
        Assert.That(projection.interaction.contextKey, Is.EqualTo("test:projection"));
        Assert.That(projection.interaction.inputChoiceCount, Is.EqualTo(2));
        Assert.That(projection.interaction.inputChoiceKeys, Is.EquivalentTo(new[] { "accept", "decline" }));
        Assert.That(projection.interaction.selectedChoiceKey, Is.EqualTo("decline"));
        Assert.That(projection.interaction.hasResponseWindow, Is.True);
        Assert.That(projection.interaction.responseWindowNumericId, Is.EqualTo(88));
        Assert.That(projection.interaction.responseCurrentResponderPlayerNumericId, Is.EqualTo(2));
        Assert.That(projection.interaction.responseResponderCount, Is.EqualTo(2));
        Assert.That(projection.interaction.responseWindowOriginType, Is.EqualTo("damageResponse"));
        Assert.That(projection.interaction.pendingDamageResponseStageKey, Is.EqualTo("awaitDefense"));
        Assert.That(projection.interaction.pendingDamageTypeKey, Is.EqualTo("physical"));
        Assert.That(projection.interaction.pendingDamageTargetCharacterInstanceNumericId, Is.EqualTo(200001));
        Assert.That(projection.interaction.pendingDamageDefenderPlayerNumericId, Is.EqualTo(1));
    }

    [Test]
    public void ProjectionParser_ParseMissingFields_ShouldReturnSafeDefaults()
    {
        var projection = ProjectionParser.Parse("{\"isSucceeded\":false}", 1);

        Assert.That(projection.isSucceeded, Is.False);
        Assert.That(projection.viewerPlayerNumericId, Is.EqualTo(1));
        Assert.That(projection.currentPhase, Is.EqualTo(string.Empty));
        Assert.That(projection.teamSummaries, Is.Empty);
        Assert.That(projection.handCards, Is.Empty);
        Assert.That(projection.fieldCards, Is.Empty);
        Assert.That(projection.summonZoneCards, Is.Empty);
        Assert.That(projection.sakuraCakeCards, Is.Empty);
        Assert.That(projection.eventLog, Is.Empty);
    }

    [Test]
    public void ProjectionParser_WhenResponseWindowIsNull_ShouldNotMarkHasResponseWindow()
    {
        const string responseJson =
            "{"
            + "\"isSucceeded\":true,"
            + "\"viewerPlayerNumericId\":1,"
            + "\"interaction\":{\"responseWindow\":null}"
            + "}";

        var projection = ProjectionParser.Parse(responseJson, 1);

        Assert.That(projection.interaction.hasResponseWindow, Is.False);
        Assert.That(projection.interaction.responseWindowNumericId, Is.Null);
        Assert.That(projection.interaction.responseCurrentResponderPlayerNumericId, Is.Null);
        Assert.That(projection.interaction.responseResponderCount, Is.EqualTo(0));
    }

    [Test]
    public void ProjectionParser_WhenResponseWindowIsEmptyObject_ShouldNotMarkHasResponseWindow()
    {
        const string responseJson =
            "{"
            + "\"isSucceeded\":true,"
            + "\"viewerPlayerNumericId\":1,"
            + "\"interaction\":{\"responseWindow\":{}}"
            + "}";

        var projection = ProjectionParser.Parse(responseJson, 1);

        Assert.That(projection.interaction.hasResponseWindow, Is.False);
        Assert.That(projection.interaction.responseWindowNumericId, Is.Null);
        Assert.That(projection.interaction.responseCurrentResponderPlayerNumericId, Is.Null);
        Assert.That(projection.interaction.responseResponderCount, Is.EqualTo(0));
    }

    [Test]
    public void ProjectionParser_WhenResponseWindowIdIsZero_ShouldNotMarkHasResponseWindow()
    {
        const string responseJson =
            "{"
            + "\"isSucceeded\":true,"
            + "\"viewerPlayerNumericId\":1,"
            + "\"interaction\":{\"responseWindow\":{\"responseWindowNumericId\":0,\"currentResponderPlayerNumericId\":3,\"responderPlayerNumericIds\":[3,4]}}"
            + "}";

        var projection = ProjectionParser.Parse(responseJson, 1);

        Assert.That(projection.interaction.hasResponseWindow, Is.False);
        Assert.That(projection.interaction.responseWindowNumericId, Is.Null);
        Assert.That(projection.interaction.responseCurrentResponderPlayerNumericId, Is.Null);
        Assert.That(projection.interaction.responseResponderCount, Is.EqualTo(0));
    }

    [Test]
    public void ProjectionParser_WhenResponseWindowIdIsPositive_ShouldMarkHasResponseWindow()
    {
        const string responseJson =
            "{"
            + "\"isSucceeded\":true,"
            + "\"viewerPlayerNumericId\":1,"
            + "\"interaction\":{\"responseWindow\":{\"responseWindowNumericId\":66,\"currentResponderPlayerNumericId\":3,\"responderPlayerNumericIds\":[3,4]}}"
            + "}";

        var projection = ProjectionParser.Parse(responseJson, 1);

        Assert.That(projection.interaction.hasResponseWindow, Is.True);
        Assert.That(projection.interaction.responseWindowNumericId, Is.EqualTo(66));
        Assert.That(projection.interaction.responseCurrentResponderPlayerNumericId, Is.EqualTo(3));
        Assert.That(projection.interaction.responseResponderCount, Is.EqualTo(2));
    }

    [Test]
    public void ProjectionParser_WhenInputContextIdIsZero_ShouldNotMarkHasInputContext()
    {
        const string responseJson =
            "{"
            + "\"isSucceeded\":true,"
            + "\"viewerPlayerNumericId\":1,"
            + "\"interaction\":{\"inputContext\":{\"inputContextNumericId\":0,\"requiredPlayerNumericId\":1,\"choiceCount\":1,\"choiceKeys\":[\"discardCard:1001\"]}}"
            + "}";

        var projection = ProjectionParser.Parse(responseJson, 1);

        Assert.That(projection.interaction.hasInputContext, Is.False);
        Assert.That(projection.interaction.inputContextNumericId, Is.Null);
        Assert.That(projection.interaction.inputRequiredPlayerNumericId, Is.Null);
        Assert.That(projection.interaction.inputChoiceKeys, Is.Empty);
    }

    [Test]
    public void ProjectionParser_WhenInputContextIdIsPositive_ShouldMarkHasInputContext()
    {
        const string responseJson =
            "{"
            + "\"isSucceeded\":true,"
            + "\"viewerPlayerNumericId\":1,"
            + "\"interaction\":{\"inputContext\":{\"inputContextNumericId\":77,\"requiredPlayerNumericId\":2,\"inputTypeKey\":\"testInput\",\"contextKey\":\"test:ctx\",\"choiceCount\":2,\"choiceKeys\":[\"a\",\"b\"],\"selectedChoiceKey\":\"b\"}}"
            + "}";

        var projection = ProjectionParser.Parse(responseJson, 1);

        Assert.That(projection.interaction.hasInputContext, Is.True);
        Assert.That(projection.interaction.inputContextNumericId, Is.EqualTo(77));
        Assert.That(projection.interaction.inputRequiredPlayerNumericId, Is.EqualTo(2));
        Assert.That(projection.interaction.inputTypeKey, Is.EqualTo("testInput"));
        Assert.That(projection.interaction.contextKey, Is.EqualTo("test:ctx"));
        Assert.That(projection.interaction.inputChoiceCount, Is.EqualTo(2));
        Assert.That(projection.interaction.inputChoiceKeys, Is.EquivalentTo(new[] { "a", "b" }));
        Assert.That(projection.interaction.selectedChoiceKey, Is.EqualTo("b"));
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

    [Test]
    public void SelectionHelper_ApplyHandCardSelection_ShouldSyncHandAndDefenseSelection()
    {
        long? selectedHandCardId = null;
        long? selectedDefenseCardId = null;

        SocketDebugPanel.ApplyHandCardSelection(ref selectedHandCardId, ref selectedDefenseCardId, 1234);

        Assert.That(selectedHandCardId, Is.EqualTo(1234));
        Assert.That(selectedDefenseCardId, Is.EqualTo(1234));
    }

    [Test]
    public void SelectionHelper_ApplySummonCardSelection_ShouldSetSummonAndClearSakuraSelection()
    {
        long? selectedSummonCardId = 2001;
        long? selectedSakuraCardId = 3001;

        SocketDebugPanel.ApplySummonCardSelection(ref selectedSummonCardId, ref selectedSakuraCardId, 2002);

        Assert.That(selectedSummonCardId, Is.EqualTo(2002));
        Assert.That(selectedSakuraCardId, Is.Null);
    }

    [Test]
    public void SelectionHelper_ApplySakuraCardSelection_ShouldSetSakuraAndClearSummonSelection()
    {
        long? selectedSakuraCardId = 3001;
        long? selectedSummonCardId = 2001;

        SocketDebugPanel.ApplySakuraCardSelection(ref selectedSakuraCardId, ref selectedSummonCardId, 3002);

        Assert.That(selectedSakuraCardId, Is.EqualTo(3002));
        Assert.That(selectedSummonCardId, Is.Null);
    }

    [Test]
    public void SelectionHelper_CollectShackleDiscardChoiceKeys_ShouldKeepOnlyDiscardCardOptions()
    {
        var inputChoiceKeys = new List<string>
        {
            "discardCard:100001",
            "player:2",
            "discardCard:100002",
            "shackle:decline",
        };

        var shackleDiscardChoiceKeys = SocketDebugPanel.CollectShackleDiscardChoiceKeys(inputChoiceKeys);

        Assert.That(shackleDiscardChoiceKeys, Is.EqualTo(new[] { "discardCard:100001", "discardCard:100002" }));
    }

    [Test]
    public void SelectionHelper_CollectHandCardChoiceKeys_ShouldKeepOnlyHandCardOptions()
    {
        var inputChoiceKeys = new List<string>
        {
            "handCard:100010",
            "discardCard:100001",
            "handCard:100016",
            "anomaly:decline",
        };

        var handCardChoiceKeys = SocketDebugPanel.CollectHandCardChoiceKeys(inputChoiceKeys);

        Assert.That(handCardChoiceKeys, Is.EqualTo(new[] { "handCard:100010", "handCard:100016" }));
    }

    [Test]
    public void SelectionHelper_IsA005ConditionDefenseLikePlaceInputContext_WhenContextKeyMatchesAndHasId_ShouldReturnTrue()
    {
        var projection = ProjectionViewModel.createDefault(2);
        projection.interaction.hasInputContext = true;
        projection.interaction.inputContextNumericId = 5;
        projection.interaction.inputTypeKey = "anomalyA005ConditionDefenseLikePlace";
        projection.interaction.contextKey = "anomaly:A005:conditionDefenseLikePlace";

        var isA005ConditionContext = SocketDebugPanel.IsA005ConditionDefenseLikePlaceInputContext(projection);

        Assert.That(isA005ConditionContext, Is.True);
    }

    [Test]
    public void SelectionHelper_IsA010RewardChooseTwoInputContext_WhenContextKeyMatchesAndHasId_ShouldReturnTrue()
    {
        var projection = ProjectionViewModel.createDefault(1);
        projection.interaction.hasInputContext = true;
        projection.interaction.inputContextNumericId = 10;
        projection.interaction.inputTypeKey = "anomalyA010RewardChooseTwo";
        projection.interaction.contextKey = "anomaly:A010:rewardChooseTwo";

        var isA010RewardContext = SocketDebugPanel.IsA010RewardChooseTwoInputContext(projection);

        Assert.That(isA010RewardContext, Is.True);
    }

    [Test]
    public void SelectionHelper_PrunedSelectedChoiceKeysByAvailable_ShouldRemoveUnavailableSelections()
    {
        var selectedChoiceKeys = new List<string> { "discardCard:100001", "discardCard:100099" };
        var availableChoiceKeys = new List<string> { "discardCard:100001", "discardCard:100002" };

        SocketDebugPanel.PruneSelectedChoiceKeysByAvailable(selectedChoiceKeys, availableChoiceKeys);

        Assert.That(selectedChoiceKeys, Is.EqualTo(new[] { "discardCard:100001" }));
    }

    [Test]
    public void SelectionHelper_TryToggleBoundedChoiceSelection_WhenAddingFifthChoice_ShouldFail()
    {
        var selectedChoiceKeys = new List<string>
        {
            "discardCard:100001",
            "discardCard:100002",
            "discardCard:100003",
            "discardCard:100004",
        };

        var toggled = SocketDebugPanel.TryToggleBoundedChoiceSelection(
            selectedChoiceKeys,
            "discardCard:100005",
            4,
            out var failureReason);

        Assert.That(toggled, Is.False);
        Assert.That(failureReason, Does.Contain("最多只能选择 4 张"));
        Assert.That(selectedChoiceKeys.Count, Is.EqualTo(4));
    }

    [Test]
    public void SelectionHelper_TryToggleBoundedChoiceSelection_WhenChoiceAlreadySelected_ShouldUnselect()
    {
        var selectedChoiceKeys = new List<string>
        {
            "discardCard:100001",
            "discardCard:100002",
        };

        var toggled = SocketDebugPanel.TryToggleBoundedChoiceSelection(
            selectedChoiceKeys,
            "discardCard:100002",
            4,
            out var failureReason);

        Assert.That(toggled, Is.True);
        Assert.That(failureReason, Is.Empty);
        Assert.That(selectedChoiceKeys, Is.EqualTo(new[] { "discardCard:100001" }));
    }

    [Test]
    public void SelectionHelper_IsTurnStartShackleInputContext_WhenContextKeyMatchesAndHasId_ShouldReturnTrue()
    {
        var projection = ProjectionViewModel.createDefault(1);
        projection.interaction.hasInputContext = true;
        projection.interaction.inputContextNumericId = 9;
        projection.interaction.contextKey = "turnStart:shackleDiscard";

        var isShackleContext = SocketDebugPanel.IsTurnStartShackleInputContext(projection);

        Assert.That(isShackleContext, Is.True);
    }

    [Test]
    public void SelectionHelper_TryApplyFieldCardSelection_ShouldKeepAllSelectionsUnchanged()
    {
        long? selectedHandCardId = 1001;
        long? selectedSummonCardId = 2001;
        long? selectedSakuraCardId = 3001;
        long? selectedDefenseCardId = 4001;

        var applied = SocketDebugPanel.TryApplyFieldCardSelection(
            ref selectedHandCardId,
            ref selectedSummonCardId,
            ref selectedSakuraCardId,
            ref selectedDefenseCardId,
            9001);

        Assert.That(applied, Is.False);
        Assert.That(selectedHandCardId, Is.EqualTo(1001));
        Assert.That(selectedSummonCardId, Is.EqualTo(2001));
        Assert.That(selectedSakuraCardId, Is.EqualTo(3001));
        Assert.That(selectedDefenseCardId, Is.EqualTo(4001));
    }

    [Test]
    public void SelectionHelper_BuildFieldCardReadOnlyMessage_ShouldContainEnglishAndChineseHint()
    {
        var message = SocketDebugPanel.BuildFieldCardReadOnlyMessage(9001);

        Assert.That(message, Does.Contain("field card is read-only"));
        Assert.That(message, Does.Contain("鍦轰笂鐗屼粎灞曠ず"));
        Assert.That(message, Does.Contain("9001"));
    }

    [Test]
    public void ResponseWindowHelper_HasRenderableResponseWindow_WhenIdMissing_ShouldReturnFalse()
    {
        var projection = ProjectionViewModel.createDefault(1);
        projection.interaction.hasResponseWindow = true;
        projection.interaction.responseWindowNumericId = null;

        var renderable = SocketDebugPanel.HasRenderableResponseWindow(projection);

        Assert.That(renderable, Is.False);
    }

    [Test]
    public void ResponseWindowHelper_HasRenderableResponseWindow_WhenIdPositive_ShouldReturnTrue()
    {
        var projection = ProjectionViewModel.createDefault(1);
        projection.interaction.hasResponseWindow = true;
        projection.interaction.responseWindowNumericId = 9;

        var renderable = SocketDebugPanel.HasRenderableResponseWindow(projection);

        Assert.That(renderable, Is.True);
    }

    [Test]
    public void ResponseWindowHelper_IsAwaitDefenseStageForResponseWindow_WhenStageIsAwaitDefense_ShouldReturnTrue()
    {
        var projection = ProjectionViewModel.createDefault(1);
        projection.interaction.hasResponseWindow = true;
        projection.interaction.responseWindowNumericId = 9;
        projection.interaction.pendingDamageResponseStageKey = "awaitDefense";

        var isAwaitDefense = SocketDebugPanel.IsAwaitDefenseStageForResponseWindow(projection);
        var isAwaitCounter = SocketDebugPanel.IsLegacyAwaitCounterStageForResponseWindow(projection);

        Assert.That(isAwaitDefense, Is.True);
        Assert.That(isAwaitCounter, Is.False);
    }

    [Test]
    public void ResponseWindowHelper_IsAwaitDefenseStageForResponseWindow_WhenStageIsAwaitCounter_ShouldReturnFalse()
    {
        var projection = ProjectionViewModel.createDefault(1);
        projection.interaction.hasResponseWindow = true;
        projection.interaction.responseWindowNumericId = 9;
        projection.interaction.pendingDamageResponseStageKey = "awaitCounter";

        var isAwaitDefense = SocketDebugPanel.IsAwaitDefenseStageForResponseWindow(projection);
        var isAwaitCounter = SocketDebugPanel.IsLegacyAwaitCounterStageForResponseWindow(projection);

        Assert.That(isAwaitDefense, Is.False);
        Assert.That(isAwaitCounter, Is.True);
    }

    [Test]
    public void ResponseWindowHelper_IsLocalResponderForResponseWindow_WhenViewerMatchesResponder_ShouldReturnTrue()
    {
        var projection = ProjectionViewModel.createDefault(2);
        projection.interaction.hasResponseWindow = true;
        projection.interaction.responseWindowNumericId = 9;
        projection.interaction.responseCurrentResponderPlayerNumericId = 2;

        var isLocalResponder = SocketDebugPanel.IsLocalResponderForResponseWindow(projection);

        Assert.That(isLocalResponder, Is.True);
    }

    [Test]
    public void ResponseWindowHelper_IsLocalResponderForResponseWindow_WhenViewerDiffersResponder_ShouldReturnFalse()
    {
        var projection = ProjectionViewModel.createDefault(4);
        projection.interaction.hasResponseWindow = true;
        projection.interaction.responseWindowNumericId = 9;
        projection.interaction.responseCurrentResponderPlayerNumericId = 2;

        var isLocalResponder = SocketDebugPanel.IsLocalResponderForResponseWindow(projection);

        Assert.That(isLocalResponder, Is.False);
    }

    [Test]
    public void ResponseWindowHelper_TryValidateSubmitResponseNoActor_WhenActorMatches_ShouldReturnTrue()
    {
        var projection = ProjectionViewModel.createDefault(1);
        projection.interaction.hasResponseWindow = true;
        projection.interaction.responseWindowNumericId = 55;
        projection.interaction.responseCurrentResponderPlayerNumericId = 2;

        var canSend = SocketDebugPanel.TryValidateSubmitResponseNoActor(
            projection,
            "2",
            out var responderPlayerNumericId,
            out var failureReason);

        Assert.That(canSend, Is.True);
        Assert.That(responderPlayerNumericId, Is.EqualTo(2));
        Assert.That(failureReason, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ResponseWindowHelper_TryValidateSubmitResponseNoActor_WhenActorMismatches_ShouldReturnFalse()
    {
        var projection = ProjectionViewModel.createDefault(1);
        projection.interaction.hasResponseWindow = true;
        projection.interaction.responseWindowNumericId = 56;
        projection.interaction.responseCurrentResponderPlayerNumericId = 3;

        var canSend = SocketDebugPanel.TryValidateSubmitResponseNoActor(
            projection,
            "1",
            out var responderPlayerNumericId,
            out var failureReason);

        Assert.That(canSend, Is.False);
        Assert.That(responderPlayerNumericId, Is.EqualTo(3));
        Assert.That(failureReason, Does.Contain("currentResponderPlayerNumericId"));
    }

    [Test]
    public void ResponseWindowHelper_TryValidateSubmitResponseNoActor_WhenNoWindow_ShouldReturnFalse()
    {
        var projection = ProjectionViewModel.createDefault(1);

        var canSend = SocketDebugPanel.TryValidateSubmitResponseNoActor(
            projection,
            "1",
            out _,
            out var failureReason);

        Assert.That(canSend, Is.False);
        Assert.That(failureReason, Does.Contain("没有可用的响应窗口"));
    }

    [Test]
    public void ResponseWindowHelper_TryValidateSubmitDefenseActor_WhenActorMatchesResponder_ShouldReturnTrue()
    {
        var projection = ProjectionViewModel.createDefault(1);
        projection.interaction.hasResponseWindow = true;
        projection.interaction.responseWindowNumericId = 123;
        projection.interaction.responseCurrentResponderPlayerNumericId = 2;

        var canSend = SocketDebugPanel.TryValidateSubmitDefenseActor(
            projection,
            "2",
            out var failureReason);

        Assert.That(canSend, Is.True);
        Assert.That(failureReason, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ResponseWindowHelper_TryValidateSubmitDefenseActor_WhenActorMismatchesResponder_ShouldReturnFalse()
    {
        var projection = ProjectionViewModel.createDefault(1);
        projection.interaction.hasResponseWindow = true;
        projection.interaction.responseWindowNumericId = 123;
        projection.interaction.responseCurrentResponderPlayerNumericId = 2;

        var canSend = SocketDebugPanel.TryValidateSubmitDefenseActor(
            projection,
            "1",
            out var failureReason);

        Assert.That(canSend, Is.False);
        Assert.That(failureReason, Does.Contain("currentResponderPlayerNumericId"));
    }

    [Test]
    public void ResponseWindowHelper_TryValidateSelectedDefenseCardInHand_WhenCardExists_ShouldReturnTrue()
    {
        var projection = ProjectionViewModel.createDefault(2);
        projection.handCards.Add(new ProjectionCardViewModel
        {
            cardInstanceNumericId = 100004,
            definitionId = "T001",
            zoneKey = "hand",
        });

        var isValid = SocketDebugPanel.TryValidateSelectedDefenseCardInHand(
            projection,
            100004,
            out var selectedCardId,
            out var selectedCard,
            out var failureReason);

        Assert.That(isValid, Is.True);
        Assert.That(selectedCardId, Is.EqualTo(100004));
        Assert.That(selectedCard, Is.Not.Null);
        Assert.That(selectedCard!.definitionId, Is.EqualTo("T001"));
        Assert.That(failureReason, Is.EqualTo(string.Empty));
    }

    [Test]
    public void ResponseWindowHelper_TryValidateSelectedDefenseCardInHand_WhenCardNotInHand_ShouldReturnFalse()
    {
        var projection = ProjectionViewModel.createDefault(2);
        projection.handCards.Add(new ProjectionCardViewModel
        {
            cardInstanceNumericId = 100004,
            definitionId = "T001",
            zoneKey = "hand",
        });

        var isValid = SocketDebugPanel.TryValidateSelectedDefenseCardInHand(
            projection,
            100099,
            out _,
            out _,
            out var failureReason);

        Assert.That(isValid, Is.False);
        Assert.That(failureReason, Does.Contain("selected defense card is not in hand"));
    }

    [Test]
    public void ResponseWindowHelper_TryInferDefenseTypeKeyFromDefinitionId_ForStarterCards_ShouldReturnDual()
    {
        var canInferMagicCircuit = SocketDebugPanel.TryInferDefenseTypeKeyFromDefinitionId("starter:magicCircuit", out var magicCircuitDefenseTypeKey);
        var canInferKourindouCoupon = SocketDebugPanel.TryInferDefenseTypeKeyFromDefinitionId("starter:kourindouCoupon", out var kourindouCouponDefenseTypeKey);

        Assert.That(canInferMagicCircuit, Is.True);
        Assert.That(magicCircuitDefenseTypeKey, Is.EqualTo("dual"));
        Assert.That(canInferKourindouCoupon, Is.True);
        Assert.That(kourindouCouponDefenseTypeKey, Is.EqualTo("dual"));
    }

    [Test]
    public void ResponseWindowHelper_TryInferDefenseTypeKeyFromDefinitionId_ForTreasureCards_ShouldReturnExpectedTypes()
    {
        var canInferT002 = SocketDebugPanel.TryInferDefenseTypeKeyFromDefinitionId("T002", out var t002DefenseTypeKey);
        var canInferT003 = SocketDebugPanel.TryInferDefenseTypeKeyFromDefinitionId("T003", out var t003DefenseTypeKey);
        var canInferT001 = SocketDebugPanel.TryInferDefenseTypeKeyFromDefinitionId("T001", out var t001DefenseTypeKey);

        Assert.That(canInferT002, Is.True);
        Assert.That(t002DefenseTypeKey, Is.EqualTo("spell"));
        Assert.That(canInferT003, Is.True);
        Assert.That(t003DefenseTypeKey, Is.EqualTo("physical"));
        Assert.That(canInferT001, Is.True);
        Assert.That(t001DefenseTypeKey, Is.EqualTo("dual"));
    }

    [Test]
    public void ResponseWindowHelper_TryResolveFormalDefenseTypeKey_WhenSelectedCardCanInfer_ShouldPreferSelectedCardType()
    {
        var selectedCard = new ProjectionCardViewModel
        {
            cardInstanceNumericId = 100005,
            definitionId = "starter:magicCircuit",
            zoneKey = "hand",
        };

        var resolved = SocketDebugPanel.TryResolveFormalDefenseTypeKey(
            selectedCard,
            "physical",
            out var resolvedDefenseTypeKey,
            out var sourceLabel);

        Assert.That(resolved, Is.True);
        Assert.That(resolvedDefenseTypeKey, Is.EqualTo("dual"));
        Assert.That(sourceLabel, Is.EqualTo("已选防御牌定义"));
    }

    [Test]
    public void ResponseWindowHelper_TryResolveFormalDefenseTypeKey_WhenSelectionCannotInfer_ShouldFallbackToManual()
    {
        var selectedCard = new ProjectionCardViewModel
        {
            cardInstanceNumericId = 100006,
            definitionId = "unknown:defenseCard",
            zoneKey = "hand",
        };

        var resolved = SocketDebugPanel.TryResolveFormalDefenseTypeKey(
            selectedCard,
            "physical",
            out var resolvedDefenseTypeKey,
            out var sourceLabel);

        Assert.That(resolved, Is.True);
        Assert.That(resolvedDefenseTypeKey, Is.EqualTo("physical"));
        Assert.That(sourceLabel, Is.EqualTo("手动输入"));
    }

    [Test]
    public void ResponseWindowHelper_TryExtractFailedReasonKeyFromRawResponse_WhenPresent_ShouldReturnReasonKey()
    {
        const string responseJson =
            "{"
            + "\"requestId\":9,"
            + "\"isSucceeded\":false,"
            + "\"error\":{\"code\":\"request_rejected\",\"message\":\"rejected\",\"failedReasonKey\":\"defenseTypeMismatch\"}"
            + "}";

        var failedReasonKey = SocketDebugPanel.TryExtractFailedReasonKeyFromRawResponse(responseJson);

        Assert.That(failedReasonKey, Is.EqualTo("defenseTypeMismatch"));
    }

    [Test]
    public void ResponseWindowHelper_TryExtractFailedReasonKeyFromRawResponse_WhenMissing_ShouldReturnEmpty()
    {
        const string responseJson =
            "{"
            + "\"requestId\":10,"
            + "\"isSucceeded\":false,"
            + "\"error\":{\"code\":\"request_rejected\",\"message\":\"rejected\"}"
            + "}";

        var failedReasonKey = SocketDebugPanel.TryExtractFailedReasonKeyFromRawResponse(responseJson);

        Assert.That(failedReasonKey, Is.EqualTo(string.Empty));
    }

    [Test]
    public void DamageTargetHelper_TryResolveTargetActiveCharacterInstanceId_WhenValidPlayer_ShouldResolveCharacter()
    {
        var projection = ProjectionViewModel.createDefault(1);
        projection.playerSummaries.Add(new ProjectionPlayerSummaryViewModel
        {
            playerNumericId = 2,
            activeCharacterInstanceNumericId = 200002,
        });

        var resolved = SocketDebugPanel.TryResolveTargetActiveCharacterInstanceId(
            projection,
            "2",
            out var targetCharacterInstanceNumericId,
            out var failureReason);

        Assert.That(resolved, Is.True);
        Assert.That(targetCharacterInstanceNumericId, Is.EqualTo(200002));
        Assert.That(failureReason, Is.EqualTo(string.Empty));
    }

    [Test]
    public void DamageTargetHelper_TryResolveTargetActiveCharacterInstanceId_WhenTargetHasNoActiveCharacter_ShouldReturnFalse()
    {
        var projection = ProjectionViewModel.createDefault(1);
        projection.playerSummaries.Add(new ProjectionPlayerSummaryViewModel
        {
            playerNumericId = 2,
            activeCharacterInstanceNumericId = null,
        });

        var resolved = SocketDebugPanel.TryResolveTargetActiveCharacterInstanceId(
            projection,
            "2",
            out _,
            out var failureReason);

        Assert.That(resolved, Is.False);
        Assert.That(failureReason, Does.Contain("娌℃湁鍙敤鍦ㄥ満瑙掕壊"));
    }

    [Test]
    public void DamageDebugHelper_TryResolveDebugDamageArguments_WhenValidPhysical_ShouldResolve()
    {
        var resolved = SocketDebugPanel.TryResolveDebugDamageArguments(
            "3",
            "physical",
            out var resolvedDamageValue,
            out var resolvedDamageTypeKey,
            out var failureReason);

        Assert.That(resolved, Is.True);
        Assert.That(resolvedDamageValue, Is.EqualTo(3));
        Assert.That(resolvedDamageTypeKey, Is.EqualTo("physical"));
        Assert.That(failureReason, Is.EqualTo(string.Empty));
    }

    [Test]
    public void DamageDebugHelper_TryResolveDebugDamageArguments_WhenValidChineseType_ShouldNormalize()
    {
        var resolved = SocketDebugPanel.TryResolveDebugDamageArguments(
            "5",
            "咒术",
            out var resolvedDamageValue,
            out var resolvedDamageTypeKey,
            out var failureReason);

        Assert.That(resolved, Is.True);
        Assert.That(resolvedDamageValue, Is.EqualTo(5));
        Assert.That(resolvedDamageTypeKey, Is.EqualTo("spell"));
        Assert.That(failureReason, Is.EqualTo(string.Empty));
    }

    [Test]
    public void DamageDebugHelper_TryResolveDebugDamageArguments_WhenInvalidDamageValue_ShouldFail()
    {
        var resolved = SocketDebugPanel.TryResolveDebugDamageArguments(
            "0",
            "direct",
            out _,
            out _,
            out var failureReason);

        Assert.That(resolved, Is.False);
        Assert.That(failureReason, Does.Contain("伤害值必须是大于0的整数"));
    }

    [Test]
    public void DamageDebugHelper_TryResolveDebugDamageArguments_WhenInvalidDamageType_ShouldFail()
    {
        var resolved = SocketDebugPanel.TryResolveDebugDamageArguments(
            "2",
            "fire",
            out _,
            out _,
            out var failureReason);

        Assert.That(resolved, Is.False);
        Assert.That(failureReason, Does.Contain("伤害类型仅支持"));
    }

    [Test]
    public void FlowTraceHelper_TryParsePositiveTraceCount_ShouldFallbackToDefaultWhenInvalid()
    {
        var fallbackForNegative = SocketDebugPanel.TryParsePositiveTraceCount("-1", 20);
        var fallbackForText = SocketDebugPanel.TryParsePositiveTraceCount("abc", 20);
        var parsedValid = SocketDebugPanel.TryParsePositiveTraceCount("15", 20);

        Assert.That(fallbackForNegative, Is.EqualTo(20));
        Assert.That(fallbackForText, Is.EqualTo(20));
        Assert.That(parsedValid, Is.EqualTo(15));
    }

    [Test]
    public void FlowTraceHelper_BuildFlowTraceExport_ShouldReturnLatestNLines()
    {
        var lines = new List<string> { "line-1", "line-2", "line-3", "line-4" };

        var exported = SocketDebugPanel.BuildFlowTraceExport(lines, 2);

        Assert.That(exported, Is.EqualTo("line-3\nline-4"));
    }

    [Test]
    public void FlowTraceHelper_ResolveStepNumberFromResult_ShouldParseStepPrefix()
    {
        var parsed = SocketDebugPanel.ResolveStepNumberFromResult("step 7 passed: test", 99);
        var fallback = SocketDebugPanel.ResolveStepNumberFromResult("unexpected", 99);

        Assert.That(parsed, Is.EqualTo(7));
        Assert.That(fallback, Is.EqualTo(99));
    }

    [Test]
    public void FlowTraceHelper_GetMacroActionTypeForStep_ShouldOnlyReturnWhitelistedActions()
    {
        Assert.That(SocketDebugPanel.GetMacroActionTypeForStep(DebugChecklistMode.mainFlowA, 2), Is.EqualTo("drawOneCard"));
        Assert.That(SocketDebugPanel.GetMacroActionTypeForStep(DebugChecklistMode.mainFlowA, 3), Is.EqualTo("playTreasureCard"));
        Assert.That(SocketDebugPanel.GetMacroActionTypeForStep(DebugChecklistMode.responseWindowB, 1), Is.EqualTo("debugOpenDamageResponseWindow"));
        Assert.That(SocketDebugPanel.GetMacroActionTypeForStep(DebugChecklistMode.responseWindowB, 4), Is.EqualTo("submitResponse"));
        Assert.That(SocketDebugPanel.GetMacroActionTypeForStep(DebugChecklistMode.inputContextC, 6), Is.EqualTo("submitInputChoice"));
        Assert.That(SocketDebugPanel.GetMacroActionTypeForStep(DebugChecklistMode.inputContextC, 8), Is.EqualTo(string.Empty));
    }

    [Test]
    public void FlowTraceHelper_IsMacroStepDispatchable_ShouldRespectActionableStepsOnly()
    {
        Assert.That(SocketDebugPanel.IsMacroStepDispatchable(DebugChecklistMode.mainFlowA, 1), Is.True);
        Assert.That(SocketDebugPanel.IsMacroStepDispatchable(DebugChecklistMode.mainFlowA, 2), Is.True);
        Assert.That(SocketDebugPanel.IsMacroStepDispatchable(DebugChecklistMode.mainFlowA, 10), Is.False);

        Assert.That(SocketDebugPanel.IsMacroStepDispatchable(DebugChecklistMode.responseWindowB, 1), Is.True);
        Assert.That(SocketDebugPanel.IsMacroStepDispatchable(DebugChecklistMode.responseWindowB, 2), Is.False);
        Assert.That(SocketDebugPanel.IsMacroStepDispatchable(DebugChecklistMode.responseWindowB, 4), Is.True);

        Assert.That(SocketDebugPanel.IsMacroStepDispatchable(DebugChecklistMode.inputContextC, 6), Is.True);
        Assert.That(SocketDebugPanel.IsMacroStepDispatchable(DebugChecklistMode.inputContextC, 8), Is.False);
    }

    [Test]
    public void MessageSourceHelper_IsDirectResponseForPendingRequest_WhenIdsMatch_ShouldReturnTrue()
    {
        var isDirect = SocketDebugPanel.IsDirectResponseForPendingRequest(
            requestInFlight: true,
            pendingRequestId: 42,
            responseRequestId: 42);

        Assert.That(isDirect, Is.True);
    }

    [Test]
    public void MessageSourceHelper_IsDirectResponseForPendingRequest_WhenIdsDoNotMatch_ShouldReturnFalse()
    {
        var mismatchId = SocketDebugPanel.IsDirectResponseForPendingRequest(
            requestInFlight: true,
            pendingRequestId: 42,
            responseRequestId: 43);
        var missingPending = SocketDebugPanel.IsDirectResponseForPendingRequest(
            requestInFlight: true,
            pendingRequestId: null,
            responseRequestId: 43);
        var missingResponse = SocketDebugPanel.IsDirectResponseForPendingRequest(
            requestInFlight: true,
            pendingRequestId: 42,
            responseRequestId: null);
        var notInFlight = SocketDebugPanel.IsDirectResponseForPendingRequest(
            requestInFlight: false,
            pendingRequestId: 42,
            responseRequestId: 42);

        Assert.That(mismatchId, Is.False);
        Assert.That(missingPending, Is.False);
        Assert.That(missingResponse, Is.False);
        Assert.That(notInFlight, Is.False);
    }

    [Test]
    public void DebugCardTextureResolver_T001_ShouldMapToRelicsSummon()
    {
        DebugCardTextureResolver.ClearCacheForTests();
        var resolvedPath = DebugCardTextureResolver.ResolveAssetPathForDefinition("T001");
        Assert.That(resolvedPath, Is.EqualTo("Assets/Art/Cards/Illustrations/Relics/Summon/T001.png"));
    }

    [Test]
    public void DebugCardTextureResolver_T029Lowercase_ShouldMapToRelicsSummon()
    {
        DebugCardTextureResolver.ClearCacheForTests();
        var resolvedPath = DebugCardTextureResolver.ResolveAssetPathForDefinition("t029");
        Assert.That(resolvedPath, Is.EqualTo("Assets/Art/Cards/Illustrations/Relics/Summon/T029.png"));
    }

    [Test]
    public void DebugCardTextureResolver_S001_ShouldMapToSakuraCake()
    {
        DebugCardTextureResolver.ClearCacheForTests();
        var resolvedPath = DebugCardTextureResolver.ResolveAssetPathForDefinition("S001");
        Assert.That(resolvedPath, Is.EqualTo("Assets/Art/Cards/Illustrations/Relics/Sakuracake/S001.png"));
    }

    [Test]
    public void DebugCardTextureResolver_A001_ShouldMapToAnomaly()
    {
        DebugCardTextureResolver.ClearCacheForTests();
        var resolvedPath = DebugCardTextureResolver.ResolveAssetPathForDefinition("A001");
        Assert.That(resolvedPath, Is.EqualTo("Assets/Art/Cards/Illustrations/Anomaly/A001.png"));
    }

    [Test]
    public void DebugCardTextureResolver_T001B_ShouldMapToRelicsBasic()
    {
        DebugCardTextureResolver.ClearCacheForTests();
        var resolvedPath = DebugCardTextureResolver.ResolveAssetPathForDefinition("T001B");
        Assert.That(resolvedPath, Is.EqualTo("Assets/Art/Cards/Illustrations/Relics/Basic/T001B.png"));
    }

    [Test]
    public void DebugCardTextureResolver_T002B_ShouldMapToRelicsBasic()
    {
        DebugCardTextureResolver.ClearCacheForTests();
        var resolvedPath = DebugCardTextureResolver.ResolveAssetPathForDefinition("T002B");
        Assert.That(resolvedPath, Is.EqualTo("Assets/Art/Cards/Illustrations/Relics/Basic/T002B.png"));
    }

    [Test]
    public void DebugCardTextureResolver_StarterKourindouCoupon_ShouldMapToT001B()
    {
        DebugCardTextureResolver.ClearCacheForTests();
        var resolvedPath = DebugCardTextureResolver.ResolveAssetPathForDefinition("starter:kourindouCoupon");
        Assert.That(resolvedPath, Is.EqualTo("Assets/Art/Cards/Illustrations/Relics/Basic/T001B.png"));
    }

    [Test]
    public void DebugCardTextureResolver_StarterMagicCircuit_ShouldMapToT002B()
    {
        DebugCardTextureResolver.ClearCacheForTests();
        var resolvedPath = DebugCardTextureResolver.ResolveAssetPathForDefinition("starter:magicCircuit");
        Assert.That(resolvedPath, Is.EqualTo("Assets/Art/Cards/Illustrations/Relics/Basic/T002B.png"));
    }

    [Test]
    public void DebugCardTextureResolver_Unknown_ShouldFallbackToCardBackPath()
    {
        DebugCardTextureResolver.ClearCacheForTests();
        var resolvedPath = DebugCardTextureResolver.ResolveAssetPathForDefinition("UNKNOWN_DEFINITION_ID");
        Assert.That(resolvedPath, Is.EqualTo(DebugCardTextureResolver.CardBackAssetPath));
    }

    [Test]
    public void DebugCardTextureResolver_GetTextureForUnknown_ShouldNotThrow()
    {
        DebugCardTextureResolver.ClearCacheForTests();
        Assert.DoesNotThrow(() =>
        {
            _ = DebugCardTextureResolver.GetTextureForDefinition("UNKNOWN_DEFINITION_ID");
            _ = DebugCardTextureResolver.GetTextureForDefinition("UNKNOWN_DEFINITION_ID");
        });
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
        public long targetCharacterInstanceNumericId;
        public long inputContextNumericId;
        public int baseDamageValue;
        public string damageTypeKey = string.Empty;
        public string playMode = string.Empty;
        public string defenseTypeKey = string.Empty;
        public long defenseCardInstanceNumericId;
        public string choiceKey = string.Empty;
        public string[] choiceKeys = Array.Empty<string>();
        public long responseWindowNumericId;
        public bool shouldRespond;
        public string responseKey = string.Empty;
    }

    private sealed class FakeSocketClient : ITextSocketClient
    {
        public event Action? OnConnected;
        public event Action<string>? OnDisconnected;
        public event Action<string>? OnTextMessage;
        public event Action<string>? OnError;

        public bool isConnected { get; private set; }
        public string lastSentText { get; private set; } = string.Empty;
        public string lastConnectUrl { get; private set; } = string.Empty;

        public void Connect(string url)
        {
            lastConnectUrl = url;
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
