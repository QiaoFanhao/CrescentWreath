using System;
using System.Collections.Generic;
using System.Linq;

namespace CrescentWreath.Client.Net
{
public enum DebugChecklistMode
{
    mainFlowA,
    responseWindowB,
    inputContextC,
}

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
    private readonly Dictionary<DebugChecklistMode, List<DebugFlowStepState>> modeStepStates = new();
    private readonly Dictionary<DebugChecklistMode, string> modeLastStepResults = new();
    private long? expectedNextPlayerForMainFlowStep9;
    private DebugChecklistMode currentMode = DebugChecklistMode.mainFlowA;

    public DebugFlowChecklistRuntime()
    {
        modeStepStates[DebugChecklistMode.mainFlowA] = new List<DebugFlowStepState>
        {
            createStep(1, "connect", "Connect"),
            createStep(2, "enterAction", "EnterAction"),
            createStep(3, "draw", "Draw"),
            createStep(4, "playSelected", "Play Selected"),
            createStep(5, "enterSummon", "EnterSummon"),
            createStep(6, "summonSelected", "Summon Selected"),
            createStep(7, "enterEnd", "EnterEnd"),
            createStep(8, "startNextTurn", "StartNextTurn"),
            createStep(9, "nextPlayerEnterAction", "Next Player EnterAction"),
        };

        modeStepStates[DebugChecklistMode.responseWindowB] = new List<DebugFlowStepState>
        {
            createStep(1, "debugOpenDamageWindow", "Debug Open DamageWindow"),
            createStep(2, "verifyResponseWindow", "ResponseWindow Valid"),
            createStep(3, "verifyCurrentResponder", "Current Responder Valid"),
            createStep(4, "submitResponseNo", "Submit Response No"),
            createStep(5, "submitResponseSucceeded", "Submit Response Succeeded"),
            createStep(6, "responseWindowClosed", "ResponseWindow Closed"),
            createStep(7, "damageResolvedObserved", "DamageResolved Observed"),
            createStep(8, "hpChangedObserved", "HpChanged Observed"),
        };

        modeStepStates[DebugChecklistMode.inputContextC] = new List<DebugFlowStepState>
        {
            createStep(1, "enterAction", "EnterAction"),
            createStep(2, "draw", "Draw"),
            createStep(3, "enterEnd", "EnterEnd"),
            createStep(4, "verifyInputContext", "InputContext Opened"),
            createStep(5, "verifyInputContextDetail", "InputContext Detail Valid"),
            createStep(6, "submitInputChoice", "Submit InputChoice"),
            createStep(7, "submitInputChoiceSucceeded", "InputChoice Succeeded"),
            createStep(8, "inputContextClosedOrAdvanced", "InputContext Closed/Advanced"),
            createStep(9, "inputContextEventObserved", "InputContext Event Observed"),
        };

        foreach (var mode in modeStepStates.Keys)
        {
            modeLastStepResults[mode] = "No flow step executed yet.";
        }
    }

    public int currentStepIndex => getCurrentStepIndex(currentMode);

    public bool isCompleted => isCompletedForMode(currentMode);

    public string recommendedNextStep => getRecommendedNextStep(currentMode);

    public string lastStepResultText => modeLastStepResults[currentMode];

    public List<DebugFlowStepState> getStepStatesSnapshot()
    {
        return getStepStatesSnapshot(currentMode);
    }

    public DebugChecklistMode getCurrentMode()
    {
        return currentMode;
    }

    public void setCurrentMode(DebugChecklistMode mode)
    {
        currentMode = mode;
    }

    public bool isCompletedForMode(DebugChecklistMode mode)
    {
        return getCurrentStepIndex(mode) > modeStepStates[mode].Count;
    }

    public int getCurrentStepIndex(DebugChecklistMode mode)
    {
        var stepStates = modeStepStates[mode];
        for (var index = 0; index < stepStates.Count; index++)
        {
            if (stepStates[index].status != DebugFlowStepStatus.passed)
            {
                return index + 1;
            }
        }

        return stepStates.Count + 1;
    }

    public string getRecommendedNextStep(DebugChecklistMode mode)
    {
        if (isCompletedForMode(mode))
        {
            return "completed";
        }

        var step = modeStepStates[mode][getCurrentStepIndex(mode) - 1];
        return $"{step.stepNumber}. {step.displayName}";
    }

    public string getLastStepResultText(DebugChecklistMode mode)
    {
        return modeLastStepResults[mode];
    }

    public List<DebugFlowStepState> getStepStatesSnapshot(DebugChecklistMode mode)
    {
        var stepStates = modeStepStates[mode];
        var snapshot = new List<DebugFlowStepState>(stepStates.Count);
        for (var index = 0; index < stepStates.Count; index++)
        {
            snapshot.Add(stepStates[index].deepClone());
        }

        return snapshot;
    }

    public void OnConnectionStateChanged(string connectionState)
    {
        var currentStepIndexForMainFlow = getCurrentStepIndex(DebugChecklistMode.mainFlowA);
        if (currentStepIndexForMainFlow != 1)
        {
            return;
        }

        if (connectionState == "connected")
        {
            markStep(DebugChecklistMode.mainFlowA, 1, DebugFlowStepStatus.passed, "socket connected");
            return;
        }

        if (connectionState.StartsWith("disconnected"))
        {
            markStep(DebugChecklistMode.mainFlowA, 1, DebugFlowStepStatus.failed, connectionState);
        }
    }

    public void RecordProjectionResponse(
        string actionType,
        ProjectionViewModel currentProjection,
        ProjectionViewModel previousProjection,
        bool playSelectionCleared,
        bool summonSelectionCleared)
    {
        RecordProjectionResponse(
            currentMode,
            actionType,
            currentProjection,
            previousProjection,
            playSelectionCleared,
            summonSelectionCleared);
    }

    public void RecordProjectionResponse(
        DebugChecklistMode mode,
        string actionType,
        ProjectionViewModel currentProjection,
        ProjectionViewModel previousProjection,
        bool playSelectionCleared,
        bool summonSelectionCleared)
    {
        switch (mode)
        {
            case DebugChecklistMode.mainFlowA:
                recordMainFlowResponse(
                    actionType,
                    currentProjection,
                    previousProjection,
                    playSelectionCleared,
                    summonSelectionCleared);
                return;
            case DebugChecklistMode.responseWindowB:
                recordResponseWindowFlowResponse(actionType, currentProjection, previousProjection);
                return;
            case DebugChecklistMode.inputContextC:
                recordInputContextFlowResponse(actionType, currentProjection, previousProjection);
                return;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown debug checklist mode.");
        }
    }

    private void recordMainFlowResponse(
        string actionType,
        ProjectionViewModel currentProjection,
        ProjectionViewModel previousProjection,
        bool playSelectionCleared,
        bool summonSelectionCleared)
    {
        var stepIndex = getCurrentStepIndex(DebugChecklistMode.mainFlowA);
        if (stepIndex < 1 || stepIndex > modeStepStates[DebugChecklistMode.mainFlowA].Count)
        {
            modeLastStepResults[DebugChecklistMode.mainFlowA] = "flow already completed";
            return;
        }

        if (!currentProjection.isSucceeded)
        {
            var failureReason = string.IsNullOrWhiteSpace(currentProjection.errorCode)
                ? "request failed"
                : currentProjection.errorCode;
            markStep(DebugChecklistMode.mainFlowA, stepIndex, DebugFlowStepStatus.failed, $"{actionType}: {failureReason}");
            return;
        }

        var expectedActionType = expectedMainFlowActionTypeForStep(stepIndex);
        if (expectedActionType != actionType)
        {
            markStep(
                DebugChecklistMode.mainFlowA,
                stepIndex,
                DebugFlowStepStatus.failed,
                $"expected action={expectedActionType}, actual={actionType}");
            return;
        }

        var isStepPassed = evaluateMainFlowStep(
            stepIndex,
            currentProjection,
            previousProjection,
            playSelectionCleared,
            summonSelectionCleared,
            out var reason);

        markStep(
            DebugChecklistMode.mainFlowA,
            stepIndex,
            isStepPassed ? DebugFlowStepStatus.passed : DebugFlowStepStatus.failed,
            reason);
    }

    private void recordResponseWindowFlowResponse(
        string actionType,
        ProjectionViewModel currentProjection,
        ProjectionViewModel previousProjection)
    {
        var initialStep = getCurrentStepIndex(DebugChecklistMode.responseWindowB);
        if (initialStep < 1 || initialStep > modeStepStates[DebugChecklistMode.responseWindowB].Count)
        {
            modeLastStepResults[DebugChecklistMode.responseWindowB] = "flow already completed";
            return;
        }

        if (!currentProjection.isSucceeded)
        {
            var reason = string.IsNullOrWhiteSpace(currentProjection.errorCode)
                ? "request failed"
                : currentProjection.errorCode;
            markStep(DebugChecklistMode.responseWindowB, initialStep, DebugFlowStepStatus.failed, $"{actionType}: {reason}");
            return;
        }

        var runningStep = initialStep;
        while (runningStep <= modeStepStates[DebugChecklistMode.responseWindowB].Count)
        {
            var stepPassed = evaluateResponseWindowStep(
                runningStep,
                actionType,
                currentProjection,
                previousProjection,
                out var reason,
                out var shouldWaitForAnotherAction);

            if (shouldWaitForAnotherAction)
            {
                return;
            }

            if (!stepPassed)
            {
                markStep(DebugChecklistMode.responseWindowB, runningStep, DebugFlowStepStatus.failed, reason);
                return;
            }

            markStep(DebugChecklistMode.responseWindowB, runningStep, DebugFlowStepStatus.passed, reason);
            runningStep++;
        }
    }

    private void recordInputContextFlowResponse(
        string actionType,
        ProjectionViewModel currentProjection,
        ProjectionViewModel previousProjection)
    {
        var initialStep = getCurrentStepIndex(DebugChecklistMode.inputContextC);
        if (initialStep < 1 || initialStep > modeStepStates[DebugChecklistMode.inputContextC].Count)
        {
            modeLastStepResults[DebugChecklistMode.inputContextC] = "flow already completed";
            return;
        }

        if (!currentProjection.isSucceeded)
        {
            var reason = string.IsNullOrWhiteSpace(currentProjection.errorCode)
                ? "request failed"
                : currentProjection.errorCode;
            markStep(DebugChecklistMode.inputContextC, initialStep, DebugFlowStepStatus.failed, $"{actionType}: {reason}");
            return;
        }

        var runningStep = initialStep;
        while (runningStep <= modeStepStates[DebugChecklistMode.inputContextC].Count)
        {
            var stepPassed = evaluateInputContextStep(
                runningStep,
                actionType,
                currentProjection,
                previousProjection,
                out var reason,
                out var shouldWaitForAnotherAction);

            if (shouldWaitForAnotherAction)
            {
                return;
            }

            if (!stepPassed)
            {
                markStep(DebugChecklistMode.inputContextC, runningStep, DebugFlowStepStatus.failed, reason);
                return;
            }

            markStep(DebugChecklistMode.inputContextC, runningStep, DebugFlowStepStatus.passed, reason);
            runningStep++;
        }
    }

    private bool evaluateResponseWindowStep(
        int stepNumber,
        string actionType,
        ProjectionViewModel currentProjection,
        ProjectionViewModel previousProjection,
        out string reason,
        out bool shouldWaitForAnotherAction)
    {
        _ = previousProjection;
        shouldWaitForAnotherAction = false;
        reason = string.Empty;

        switch (stepNumber)
        {
            case 1:
                if (actionType != "debugOpenDamageResponseWindow")
                {
                    reason = "expected action=debugOpenDamageResponseWindow";
                    return false;
                }

                reason = "debug window opened request returned";
                return true;
            case 2:
                if (!currentProjection.interaction.hasResponseWindow)
                {
                    reason = "hasResponseWindow is false";
                    return false;
                }

                reason = "hasResponseWindow is true";
                return true;
            case 3:
                if (!currentProjection.interaction.responseWindowNumericId.HasValue ||
                    currentProjection.interaction.responseWindowNumericId.Value <= 0)
                {
                    reason = "responseWindowNumericId is invalid";
                    return false;
                }

                if (!currentProjection.interaction.responseCurrentResponderPlayerNumericId.HasValue ||
                    currentProjection.interaction.responseCurrentResponderPlayerNumericId.Value <= 0)
                {
                    reason = "currentResponderPlayerNumericId is invalid";
                    return false;
                }

                reason = "responseWindow id and responder are valid";
                return true;
            case 4:
                if (actionType != "submitResponse")
                {
                    shouldWaitForAnotherAction = true;
                    reason = "waiting for submitResponse";
                    return false;
                }

                reason = "submitResponse returned";
                return true;
            case 5:
                reason = "submitResponse succeeded";
                return true;
            case 6:
                if (currentProjection.interaction.hasResponseWindow &&
                    currentProjection.interaction.responseWindowNumericId.HasValue &&
                    currentProjection.interaction.responseWindowNumericId.Value > 0)
                {
                    reason = "responseWindow still open";
                    return false;
                }

                reason = "responseWindow closed";
                return true;
            case 7:
                if (!containsEventKey(currentProjection.eventLog, "damageResolved"))
                {
                    reason = "damageResolved not found in eventLog";
                    return false;
                }

                reason = "damageResolved observed";
                return true;
            case 8:
                if (!containsEventKey(currentProjection.eventLog, "hpChanged"))
                {
                    reason = "hpChanged not found in eventLog";
                    return false;
                }

                reason = "hpChanged observed";
                return true;
            default:
                reason = "unsupported step";
                return false;
        }
    }

    private bool evaluateInputContextStep(
        int stepNumber,
        string actionType,
        ProjectionViewModel currentProjection,
        ProjectionViewModel previousProjection,
        out string reason,
        out bool shouldWaitForAnotherAction)
    {
        shouldWaitForAnotherAction = false;
        reason = string.Empty;

        switch (stepNumber)
        {
            case 1:
                if (actionType != "enterActionPhase")
                {
                    reason = "expected action=enterActionPhase";
                    return false;
                }

                if (currentProjection.currentPhase != "action")
                {
                    reason = "phase is not action";
                    return false;
                }

                reason = "entered action phase";
                return true;
            case 2:
                if (actionType != "drawOneCard")
                {
                    reason = "expected action=drawOneCard";
                    return false;
                }

                if (currentProjection.viewerHandCardCount <= previousProjection.viewerHandCardCount)
                {
                    reason = "hand count did not increase after draw";
                    return false;
                }

                reason = "draw succeeded";
                return true;
            case 3:
                if (actionType != "enterEndPhase")
                {
                    reason = "expected action=enterEndPhase";
                    return false;
                }

                reason = "enterEndPhase returned";
                return true;
            case 4:
                if (!currentProjection.interaction.hasInputContext)
                {
                    reason = "hasInputContext is false";
                    return false;
                }

                reason = "inputContext opened";
                return true;
            case 5:
                if (!currentProjection.interaction.inputContextNumericId.HasValue ||
                    currentProjection.interaction.inputContextNumericId.Value <= 0)
                {
                    reason = "inputContextNumericId is invalid";
                    return false;
                }

                if (!currentProjection.interaction.inputRequiredPlayerNumericId.HasValue ||
                    currentProjection.interaction.inputRequiredPlayerNumericId.Value <= 0)
                {
                    reason = "requiredPlayerNumericId is invalid";
                    return false;
                }

                if (currentProjection.interaction.inputChoiceCount <= 0 &&
                    currentProjection.interaction.inputChoiceKeys.Count == 0)
                {
                    reason = "choiceKeys are empty";
                    return false;
                }

                reason = "inputContext detail is valid";
                return true;
            case 6:
                if (actionType != "submitInputChoice")
                {
                    shouldWaitForAnotherAction = true;
                    reason = "waiting for submitInputChoice";
                    return false;
                }

                reason = "submitInputChoice returned";
                return true;
            case 7:
                reason = "submitInputChoice succeeded";
                return true;
            case 8:
                var inputClosed = !currentProjection.interaction.hasInputContext ||
                                  !currentProjection.interaction.inputContextNumericId.HasValue ||
                                  currentProjection.interaction.inputContextNumericId.Value <= 0;
                var inputAdvanced = currentProjection.interaction.hasInputContext &&
                                    previousProjection.interaction.hasInputContext &&
                                    currentProjection.interaction.inputContextNumericId.HasValue &&
                                    previousProjection.interaction.inputContextNumericId.HasValue &&
                                    currentProjection.interaction.inputContextNumericId.Value != previousProjection.interaction.inputContextNumericId.Value;
                if (!inputClosed && !inputAdvanced)
                {
                    reason = "inputContext neither closed nor advanced";
                    return false;
                }

                reason = inputClosed ? "inputContext closed" : "inputContext advanced";
                return true;
            case 9:
                if (!containsEventKey(currentProjection.eventLog, "inputContextClosed") &&
                    !containsEventKey(currentProjection.eventLog, "inputContextOpened") &&
                    currentProjection.eventLog.Count <= previousProjection.eventLog.Count)
                {
                    reason = "inputContext-related events not observed";
                    return false;
                }

                reason = "inputContext event observed";
                return true;
            default:
                reason = "unsupported step";
                return false;
        }
    }

    private static bool containsEventKey(List<string> eventLog, string eventKey)
    {
        return eventLog.Any(eventLine =>
            eventLine.Contains(eventKey, StringComparison.OrdinalIgnoreCase));
    }

    private void markStep(DebugChecklistMode mode, int stepNumber, DebugFlowStepStatus status, string reason)
    {
        var stepState = modeStepStates[mode][stepNumber - 1];
        stepState.status = status;
        stepState.note = reason;
        modeLastStepResults[mode] = $"step {stepNumber} {status}: {reason}";
    }

    private static string expectedMainFlowActionTypeForStep(int stepNumber)
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

    private bool evaluateMainFlowStep(
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
            9 when expectedNextPlayerForMainFlowStep9.HasValue && currentProjection.currentPlayerNumericId != expectedNextPlayerForMainFlowStep9 => "current player mismatch after next turn",
            9 => "next player entered action",

            _ => "unsupported step",
        };

        if (stepNumber == 8 &&
            currentProjection.turnNumber > previousProjection.turnNumber &&
            currentProjection.currentPlayerNumericId != previousProjection.currentPlayerNumericId)
        {
            expectedNextPlayerForMainFlowStep9 = currentProjection.currentPlayerNumericId;
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
                 (!expectedNextPlayerForMainFlowStep9.HasValue || currentProjection.currentPlayerNumericId == expectedNextPlayerForMainFlowStep9),
            _ => false,
        };
    }

    private static DebugFlowStepState createStep(int stepNumber, string stepKey, string displayName)
    {
        return new DebugFlowStepState
        {
            stepNumber = stepNumber,
            stepKey = stepKey,
            displayName = displayName,
            status = DebugFlowStepStatus.pending,
        };
    }
}
}
