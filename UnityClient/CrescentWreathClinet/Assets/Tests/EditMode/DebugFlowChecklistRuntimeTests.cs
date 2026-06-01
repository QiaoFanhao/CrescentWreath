using CrescentWreath.Client.Net;
using NUnit.Framework;

namespace CrescentWreath.Client.Tests.EditMode
{
public class DebugFlowChecklistRuntimeTests
{
    [Test]
    public void Runtime_ShouldProgressToCompleted_WhenEightStepsAreValid()
    {
        var runtime = new DebugFlowChecklistRuntime();

        runtime.OnConnectionStateChanged("connected");

        var baseline = buildProjection(phase: "action", turnNumber: 1, currentPlayerNumericId: 1, handCount: 1, fieldCount: 0, summonCount: 1, isSucceeded: true);

        var step2 = buildProjection("action", 1, 1, 2, 0, 1, true);
        runtime.RecordProjectionResponse("drawOneCard", step2, baseline, playSelectionCleared: true, summonSelectionCleared: true);

        var step3 = buildProjection("action", 1, 1, 1, 1, 1, true);
        runtime.RecordProjectionResponse("playTreasureCard", step3, step2, playSelectionCleared: true, summonSelectionCleared: true);

        var step4 = buildProjection("summon", 1, 1, 1, 1, 2, true);
        runtime.RecordProjectionResponse("enterSummonPhase", step4, step3, playSelectionCleared: true, summonSelectionCleared: true);

        var step5 = buildProjection("summon", 1, 1, 1, 1, 1, true);
        runtime.RecordProjectionResponse("summonTreasureCard", step5, step4, playSelectionCleared: true, summonSelectionCleared: true);

        var step6 = buildProjection("action", 2, 2, 1, 1, 1, true);
        runtime.RecordProjectionResponse("enterEndPhase", step6, step5, playSelectionCleared: true, summonSelectionCleared: true);

        Assert.That(runtime.isCompleted, Is.True);
        Assert.That(runtime.currentStepIndex, Is.EqualTo(9));

        var snapshot = runtime.getStepStatesSnapshot();
        Assert.That(snapshot.TrueForAll(step => step.status == DebugFlowStepStatus.passed), Is.True);
    }

    [Test]
    public void Runtime_WhenActionTypeMismatch_ShouldNotAdvanceAndMarkStepFailed()
    {
        var runtime = new DebugFlowChecklistRuntime();
        runtime.OnConnectionStateChanged("connected");

        var previous = buildProjection("start", 1, 1, 1, 0, 1, true);
        var current = buildProjection("start", 1, 1, 2, 0, 1, true);

        runtime.RecordProjectionResponse("enterSummonPhase", current, previous, playSelectionCleared: true, summonSelectionCleared: true);

        Assert.That(runtime.currentStepIndex, Is.EqualTo(2));
        Assert.That(runtime.lastStepResultText, Does.Contain("expected action=drawOneCard"));

        var snapshot = runtime.getStepStatesSnapshot();
        Assert.That(snapshot[1].status, Is.EqualTo(DebugFlowStepStatus.failed));
    }

    [Test]
    public void Runtime_WhenRequestFailed_ShouldKeepStepPendingAndRecordFailureReason()
    {
        var runtime = new DebugFlowChecklistRuntime();
        runtime.OnConnectionStateChanged("connected");

        var previous = buildProjection("start", 1, 1, 1, 0, 1, true);
        var failed = buildProjection("start", 1, 1, 1, 0, 1, false);
        failed.errorCode = "request_rejected";

        runtime.RecordProjectionResponse("drawOneCard", failed, previous, playSelectionCleared: true, summonSelectionCleared: true);

        Assert.That(runtime.currentStepIndex, Is.EqualTo(2));
        Assert.That(runtime.lastStepResultText, Does.Contain("request_rejected"));

        var snapshot = runtime.getStepStatesSnapshot();
        Assert.That(snapshot[1].status, Is.EqualTo(DebugFlowStepStatus.failed));
    }

    [Test]
    public void Runtime_ResponseWindowFlow_ShouldComplete_WhenDebugOpenThenSubmitResponseNoSucceeds()
    {
        var runtime = new DebugFlowChecklistRuntime();
        runtime.setCurrentMode(DebugChecklistMode.responseWindowB);

        var previous = buildProjection("action", 1, 1, 6, 0, 6, true);
        var opened = buildProjection("action", 1, 1, 6, 0, 6, true);
        opened.interaction.hasResponseWindow = true;
        opened.interaction.responseWindowNumericId = 901;
        opened.interaction.responseCurrentResponderPlayerNumericId = 2;

        runtime.RecordProjectionResponse(
            DebugChecklistMode.responseWindowB,
            "debugOpenDamageResponseWindow",
            opened,
            previous,
            playSelectionCleared: true,
            summonSelectionCleared: true);

        var resolved = buildProjection("action", 1, 1, 6, 0, 6, true);
        resolved.interaction.hasResponseWindow = false;
        resolved.interaction.responseWindowNumericId = null;
        resolved.eventLog.Add("responseWindowClosed");
        resolved.eventLog.Add("damageResolved dmg=2");
        resolved.eventLog.Add("hpChanged");

        runtime.RecordProjectionResponse(
            DebugChecklistMode.responseWindowB,
            "submitResponse",
            resolved,
            opened,
            playSelectionCleared: true,
            summonSelectionCleared: true);

        Assert.That(runtime.isCompletedForMode(DebugChecklistMode.responseWindowB), Is.True);
        var snapshot = runtime.getStepStatesSnapshot(DebugChecklistMode.responseWindowB);
        Assert.That(snapshot.TrueForAll(step => step.status == DebugFlowStepStatus.passed), Is.True);
    }

    [Test]
    public void Runtime_InputContextFlow_ShouldComplete_WhenEnterEndOpensContextThenSubmitChoiceSucceeds()
    {
        var runtime = new DebugFlowChecklistRuntime();
        runtime.setCurrentMode(DebugChecklistMode.inputContextC);

        var baseline = buildProjection("start", 1, 1, 6, 0, 6, true);
        var action = buildProjection("action", 1, 1, 6, 0, 6, true);
        runtime.RecordProjectionResponse(
            DebugChecklistMode.inputContextC,
            "enterActionPhase",
            action,
            baseline,
            playSelectionCleared: true,
            summonSelectionCleared: true);

        var drew = buildProjection("action", 1, 1, 7, 0, 6, true);
        runtime.RecordProjectionResponse(
            DebugChecklistMode.inputContextC,
            "drawOneCard",
            drew,
            action,
            playSelectionCleared: true,
            summonSelectionCleared: true);

        var opened = buildProjection("end", 1, 1, 7, 0, 6, true);
        opened.interaction.hasInputContext = true;
        opened.interaction.inputContextNumericId = 501;
        opened.interaction.inputRequiredPlayerNumericId = 1;
        opened.interaction.inputChoiceCount = 1;
        opened.interaction.inputChoiceKeys.Add("discardCard:100001");
        opened.eventLog.Add("inputContextOpened");

        runtime.RecordProjectionResponse(
            DebugChecklistMode.inputContextC,
            "enterEndPhase",
            opened,
            drew,
            playSelectionCleared: true,
            summonSelectionCleared: true);

        var closed = buildProjection("end", 1, 1, 6, 0, 6, true);
        closed.interaction.hasInputContext = false;
        closed.interaction.inputContextNumericId = null;
        closed.eventLog.Add("inputContextClosed");
        closed.eventLog.Add("cardMoved move=discard");

        runtime.RecordProjectionResponse(
            DebugChecklistMode.inputContextC,
            "submitInputChoice",
            closed,
            opened,
            playSelectionCleared: true,
            summonSelectionCleared: true);

        Assert.That(runtime.isCompletedForMode(DebugChecklistMode.inputContextC), Is.True);
        var snapshot = runtime.getStepStatesSnapshot(DebugChecklistMode.inputContextC);
        Assert.That(snapshot.TrueForAll(step => step.status == DebugFlowStepStatus.passed), Is.True);
    }

    [Test]
    public void Runtime_WhenModeSwitches_ShouldExposeModeSpecificRecommendedStep()
    {
        var runtime = new DebugFlowChecklistRuntime();
        runtime.OnConnectionStateChanged("connected");

        runtime.setCurrentMode(DebugChecklistMode.mainFlowA);
        Assert.That(runtime.recommendedNextStep, Is.EqualTo("Draw"));

        runtime.setCurrentMode(DebugChecklistMode.responseWindowB);
        Assert.That(runtime.recommendedNextStep, Is.EqualTo("Debug Open DamageWindow"));

        runtime.setCurrentMode(DebugChecklistMode.inputContextC);
        Assert.That(runtime.recommendedNextStep, Is.EqualTo("EnterAction"));
    }

    [Test]
    public void Runtime_GetStepStatus_ShouldReturnFailedAfterRequestRejected()
    {
        var runtime = new DebugFlowChecklistRuntime();
        runtime.OnConnectionStateChanged("connected");

        var previous = buildProjection("start", 1, 1, 1, 0, 1, true);
        var failed = buildProjection("start", 1, 1, 1, 0, 1, false);
        failed.errorCode = "request_rejected";

        runtime.RecordProjectionResponse("drawOneCard", failed, previous, playSelectionCleared: true, summonSelectionCleared: true);

        Assert.That(runtime.getStepStatus(DebugChecklistMode.mainFlowA, 2), Is.EqualTo(DebugFlowStepStatus.failed));
    }

    private static ProjectionViewModel buildProjection(
        string phase,
        int turnNumber,
        long currentPlayerNumericId,
        int handCount,
        int fieldCount,
        int summonCount,
        bool isSucceeded)
    {
        var projection = ProjectionViewModel.createDefault(1);
        projection.isSucceeded = isSucceeded;
        projection.hasStateProjection = true;
        projection.currentPhase = phase;
        projection.turnNumber = turnNumber;
        projection.currentPlayerNumericId = currentPlayerNumericId;
        projection.viewerHandCardCount = handCount;

        for (var index = 0; index < handCount; index++)
        {
            projection.handCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = 1000 + index,
                definitionId = "T001",
                zoneKey = "hand",
            });
        }

        for (var index = 0; index < fieldCount; index++)
        {
            projection.fieldCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = 2000 + index,
                definitionId = "T002",
                zoneKey = "field",
            });
        }

        for (var index = 0; index < summonCount; index++)
        {
            projection.summonZoneCards.Add(new ProjectionCardViewModel
            {
                cardInstanceNumericId = 3000 + index,
                definitionId = "T003",
                zoneKey = "summonZone",
            });
        }

        return projection;
    }
}
}
