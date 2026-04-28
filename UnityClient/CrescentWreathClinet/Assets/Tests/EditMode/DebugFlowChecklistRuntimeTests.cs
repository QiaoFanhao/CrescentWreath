using CrescentWreath.Client.Net;
using NUnit.Framework;

namespace CrescentWreath.Client.Tests.EditMode
{
public class DebugFlowChecklistRuntimeTests
{
    [Test]
    public void Runtime_ShouldProgressToCompleted_WhenNineStepsAreValid()
    {
        var runtime = new DebugFlowChecklistRuntime();

        runtime.OnConnectionStateChanged("connected");

        var baseline = buildProjection(phase: "start", turnNumber: 1, currentPlayerNumericId: 1, handCount: 1, fieldCount: 0, summonCount: 1, isSucceeded: true);

        var step2 = buildProjection("action", 1, 1, 1, 0, 1, true);
        runtime.RecordProjectionResponse("enterActionPhase", step2, baseline, playSelectionCleared: true, summonSelectionCleared: true);

        var step3 = buildProjection("action", 1, 1, 2, 0, 1, true);
        runtime.RecordProjectionResponse("drawOneCard", step3, step2, playSelectionCleared: true, summonSelectionCleared: true);

        var step4 = buildProjection("action", 1, 1, 1, 1, 1, true);
        runtime.RecordProjectionResponse("playTreasureCard", step4, step3, playSelectionCleared: true, summonSelectionCleared: true);

        var step5 = buildProjection("summon", 1, 1, 1, 1, 2, true);
        runtime.RecordProjectionResponse("enterSummonPhase", step5, step4, playSelectionCleared: true, summonSelectionCleared: true);

        var step6 = buildProjection("summon", 1, 1, 1, 1, 1, true);
        runtime.RecordProjectionResponse("summonTreasureCard", step6, step5, playSelectionCleared: true, summonSelectionCleared: true);

        var step7 = buildProjection("end", 1, 1, 1, 1, 1, true);
        runtime.RecordProjectionResponse("enterEndPhase", step7, step6, playSelectionCleared: true, summonSelectionCleared: true);

        var step8 = buildProjection("start", 2, 2, 1, 1, 1, true);
        runtime.RecordProjectionResponse("startNextTurn", step8, step7, playSelectionCleared: true, summonSelectionCleared: true);

        var step9 = buildProjection("action", 2, 2, 1, 1, 1, true);
        runtime.RecordProjectionResponse("enterActionPhase", step9, step8, playSelectionCleared: true, summonSelectionCleared: true);

        Assert.That(runtime.isCompleted, Is.True);
        Assert.That(runtime.currentStepIndex, Is.EqualTo(10));

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

        runtime.RecordProjectionResponse("drawOneCard", current, previous, playSelectionCleared: true, summonSelectionCleared: true);

        Assert.That(runtime.currentStepIndex, Is.EqualTo(2));
        Assert.That(runtime.lastStepResultText, Does.Contain("expected action=enterActionPhase"));

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

        runtime.RecordProjectionResponse("enterActionPhase", failed, previous, playSelectionCleared: true, summonSelectionCleared: true);

        Assert.That(runtime.currentStepIndex, Is.EqualTo(2));
        Assert.That(runtime.lastStepResultText, Does.Contain("request_rejected"));

        var snapshot = runtime.getStepStatesSnapshot();
        Assert.That(snapshot[1].status, Is.EqualTo(DebugFlowStepStatus.failed));
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
