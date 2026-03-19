using System.Collections.Generic;
using CrescentWreath.RuleCore.Events;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class MinimalActionFlowProcessor
{
    private readonly ActionRequestProcessor actionRequestProcessor;

    public MinimalActionFlowProcessor()
        : this(new ActionRequestProcessor())
    {
    }

    public MinimalActionFlowProcessor(ActionRequestProcessor actionRequestProcessor)
    {
        this.actionRequestProcessor = actionRequestProcessor;
    }

    // Legacy compatibility wrapper. Primary M2 flow is ActionRequestProcessor.
    public List<GameEvent> processActionRequest(GameState.GameState gameState, ActionRequest actionRequest)
    {
        return actionRequestProcessor.processActionRequest(gameState, actionRequest);
    }
}
