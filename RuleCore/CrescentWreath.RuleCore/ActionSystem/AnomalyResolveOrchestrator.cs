using System;

namespace CrescentWreath.RuleCore.ActionSystem;

public static class AnomalyResolveOrchestrator
{
    public static void executePostConditionFlow(
        Action applyResolveCostsAndRewardsOrThrow,
        Func<bool>? tryOpenRewardInputContinuation,
        Action finalizeSuccessfulResolve)
    {
        applyResolveCostsAndRewardsOrThrow();

        if (tryOpenRewardInputContinuation is not null &&
            tryOpenRewardInputContinuation())
        {
            return;
        }

        finalizeSuccessfulResolve();
    }
}
