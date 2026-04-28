using System.Collections.Generic;

namespace CrescentWreath.Client.Net
{
public enum DebugFlowStepStatus
{
    pending,
    passed,
    failed,
}

public sealed class DebugFlowStepState
{
    public int stepNumber;
    public string stepKey = string.Empty;
    public string displayName = string.Empty;
    public DebugFlowStepStatus status;
    public string note = string.Empty;

    public DebugFlowStepState deepClone()
    {
        return new DebugFlowStepState
        {
            stepNumber = stepNumber,
            stepKey = stepKey,
            displayName = displayName,
            status = status,
            note = note,
        };
    }
}

public sealed class DebugFlowChecklistRuntime
{
    private readonly List<DebugFlowStepState> stepStates = new();
    private string lastStepResult = "No flow step executed yet.";
    private long? expectedNextPlayerForStep9;

    public DebugFlowChecklistRuntime()
    {
        stepStates.Add(new DebugFlowStepState { stepNumber = 1, stepKey = "connect", displayName = "Connect", status = DebugFlowStepStatus.pending });
        stepStates.Add(new DebugFlowStepState { stepNumber = 2, stepKey = "enterAction", displayName = "EnterAction", status = DebugFlowStepStatus.pending });
        stepStates.Add(new DebugFlowStepState { stepNumber = 3, stepKey = "draw", displayName = "Draw", status = DebugFlowStepStatus.pending });
        stepStates.Add(new DebugFlowStepState { stepNumber = 4, stepKey = "playSelected", displayName = "Play Selected", status = DebugFlowStepStatus.pending });
        stepStates.Add(new DebugFlowStepState { stepNumber = 5, stepKey = "enterSummon", displayName = "EnterSummon", status = DebugFlowStepStatus.pending });
        stepStates.Add(new DebugFlowStepState { stepNumber = 6, stepKey = "summonSelected", displayName = "Summon Selected", status = DebugFlowStepStatus.pending });
        stepStates.Add(new DebugFlowStepState { stepNumber = 7, stepKey = "enterEnd", displayName = "EnterEnd", status = DebugFlowStepStatus.pending });
        stepStates.Add(new DebugFlowStepState { stepNumber = 8, stepKey = "startNextTurn", displayName = "StartNextTurn", status = DebugFlowStepStatus.pending });
        stepStates.Add(new DebugFlowStepState { stepNumber = 9, stepKey = "nextPlayerEnterAction", displayName = "Next Player EnterAction", status = DebugFlowStepStatus.pending });
    }

    public int currentStepIndex
    {
        get
        {
            for (var index = 0; index < stepStates.Count; index++)
            {
                if (stepStates[index].status != DebugFlowStepStatus.passed)
                {
                    return index + 1;
                }
            }

            return stepStates.Count + 1;
        }
    }

    public bool isCompleted => currentStepIndex > stepStates.Count;

    public string recommendedNextStep
    {
        get
        {
            if (isCompleted)
            {
                return "completed";
            }

            var step = stepStates[currentStepIndex - 1];
            return $"{step.stepNumber}. {step.displayName}";
        }
    }

    public string lastStepResultText => lastStepResult;

    public List<DebugFlowStepState> getStepStatesSnapshot()
    {
        var snapshot = new List<DebugFlowStepState>(stepStates.Count);
        for (var index = 0; index < stepStates.Count; index++)
        {
            snapshot.Add(stepStates[index].deepClone());
        }

        return snapshot;
    }

    public void OnConnectionStateChanged(string connectionState)
    {
        if (currentStepIndex != 1)
        {
            return;
        }

        if (connectionState == "connected")
        {
            markStep(1, DebugFlowStepStatus.passed, "socket connected");
            return;
        }

        if (connectionState.StartsWith("disconnected"))
        {
            markStep(1, DebugFlowStepStatus.failed, connectionState);
        }
    }

    public void RecordProjectionResponse(
        string actionType,
        ProjectionViewModel currentProjection,
        ProjectionViewModel previousProjection,
        bool playSelectionCleared,
        bool summonSelectionCleared)
    {
        var stepIndex = currentStepIndex;
        if (stepIndex < 1 || stepIndex > stepStates.Count)
        {
            lastStepResult = "flow already completed";
            return;
        }

        if (!currentProjection.isSucceeded)
        {
            var failureReason = string.IsNullOrWhiteSpace(currentProjection.errorCode)
                ? "request failed"
                : currentProjection.errorCode;
            markStep(stepIndex, DebugFlowStepStatus.failed, $"{actionType}: {failureReason}");
            return;
        }

        var expectedActionType = expectedActionTypeForStep(stepIndex);
        if (expectedActionType != actionType)
        {
            markStep(
                stepIndex,
                DebugFlowStepStatus.failed,
                $"expected action={expectedActionType}, actual={actionType}");
            return;
        }

        var isStepPassed = evaluateStep(
            stepIndex,
            currentProjection,
            previousProjection,
            playSelectionCleared,
            summonSelectionCleared,
            out var reason);

        markStep(stepIndex, isStepPassed ? DebugFlowStepStatus.passed : DebugFlowStepStatus.failed, reason);
    }

    private void markStep(int stepNumber, DebugFlowStepStatus status, string reason)
    {
        var stepState = stepStates[stepNumber - 1];
        stepState.status = status;
        stepState.note = reason;
        lastStepResult = $"step {stepNumber} {status}: {reason}";
    }

    private static string expectedActionTypeForStep(int stepNumber)
    {
        return stepNumber switch
        {
            2 => "enterActionPhase",
            3 => "drawOneCard",
            4 => "playTreasureCard",
            5 => "enterSummonPhase",
            6 => "summonTreasureCard",
            7 => "enterEndPhase",
            8 => "startNextTurn",
            9 => "enterActionPhase",
            _ => string.Empty,
        };
    }

    private bool evaluateStep(
        int stepNumber,
        ProjectionViewModel currentProjection,
        ProjectionViewModel previousProjection,
        bool playSelectionCleared,
        bool summonSelectionCleared,
        out string reason)
    {
        reason = stepNumber switch
        {
            2 when currentProjection.currentPhase == "action" => "phase is action",
            2 => "phase is not action",

            3 when currentProjection.viewerHandCardCount > previousProjection.viewerHandCardCount => "hand count increased",
            3 => "hand count did not increase",

            4 when !playSelectionCleared => "played selected hand card was not cleared",
            4 when currentProjection.viewerHandCardCount < previousProjection.viewerHandCardCount => "hand count decreased",
            4 when currentProjection.fieldCards.Count > previousProjection.fieldCards.Count => "field count increased",
            4 => "play result not observed in hand/field",

            5 when currentProjection.currentPhase == "summon" => "phase is summon",
            5 => "phase is not summon",

            6 when !summonSelectionCleared => "summoned selected card was not cleared",
            6 when currentProjection.summonZoneCards.Count < previousProjection.summonZoneCards.Count => "summon zone count decreased",
            6 => "summon zone count did not decrease",

            7 when currentProjection.currentPhase == "end" => "phase is end",
            7 => "phase is not end",

            8 when currentProjection.turnNumber <= previousProjection.turnNumber => "turn number did not advance",
            8 when currentProjection.currentPlayerNumericId == previousProjection.currentPlayerNumericId => "current player did not switch",
            8 => "turn advanced and player switched",

            9 when currentProjection.currentPhase != "action" => "phase is not action",
            9 when expectedNextPlayerForStep9.HasValue && currentProjection.currentPlayerNumericId != expectedNextPlayerForStep9 => "current player mismatch after next turn",
            9 => "next player entered action",

            _ => "unsupported step",
        };

        if (stepNumber == 8 &&
            currentProjection.turnNumber > previousProjection.turnNumber &&
            currentProjection.currentPlayerNumericId != previousProjection.currentPlayerNumericId)
        {
            expectedNextPlayerForStep9 = currentProjection.currentPlayerNumericId;
        }

        return stepNumber switch
        {
            2 => currentProjection.currentPhase == "action",
            3 => currentProjection.viewerHandCardCount > previousProjection.viewerHandCardCount,
            4 => playSelectionCleared &&
                 (currentProjection.viewerHandCardCount < previousProjection.viewerHandCardCount ||
                  currentProjection.fieldCards.Count > previousProjection.fieldCards.Count),
            5 => currentProjection.currentPhase == "summon",
            6 => summonSelectionCleared &&
                 currentProjection.summonZoneCards.Count < previousProjection.summonZoneCards.Count,
            7 => currentProjection.currentPhase == "end",
            8 => currentProjection.turnNumber > previousProjection.turnNumber &&
                 currentProjection.currentPlayerNumericId != previousProjection.currentPlayerNumericId,
            9 => currentProjection.currentPhase == "action" &&
                 (!expectedNextPlayerForStep9.HasValue || currentProjection.currentPlayerNumericId == expectedNextPlayerForStep9),
            _ => false,
        };
    }
}
}
