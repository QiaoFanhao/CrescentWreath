using System;

namespace CrescentWreath.RuleCore.ActionSystem;

internal enum AnomalyContinuationKind
{
    a002ConditionFriendlyDiscardFromHand = 0,
    a005ConditionDefenseLikePlace = 1,
    a005SelectSummonCardToHand = 2,
    a005ArrivalDirectSummonFromSummonZone = 3,
    a003ArrivalSelectOpponentShackle = 4,
    a008ConditionOpponentOptionalDiscardReturn = 5,
    a009ConditionOpponentOptionalBenefit = 6,
    a009SelectGapTreasureToHand = 7,
    a008RewardRyougiOptionalDrawOne = 8,
    a002RewardOptionalBanishDecision = 9,
    a002RewardSelectFirstBanishCard = 10,
    a002RewardSelectSecondBanishCard = 11,
    a002RewardOptionalSakuraReplacement = 12,
    a007ArrivalOptionalBanishFlow = 13,
    a001ArrivalHumanDiscardFlow = 14,
    a006ArrivalHumanDefenseDiscardFlow = 15,
}

internal static class AnomalyContinuationDispatcher
{
    private const string ContinuationKeyPrefix = "continuation:anomaly";

    public static bool isAnomalyContinuationKey(string? pendingContinuationKey)
    {
        return !string.IsNullOrWhiteSpace(pendingContinuationKey) &&
               pendingContinuationKey.StartsWith(ContinuationKeyPrefix, StringComparison.Ordinal);
    }

    public static bool tryResolve(string? pendingContinuationKey, out AnomalyContinuationKind continuationKind)
    {
        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA002ConditionFriendlyDiscardFromHand,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a002ConditionFriendlyDiscardFromHand;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA005ConditionDefenseLikePlace,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a005ConditionDefenseLikePlace;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA005SelectSummonCardToHand,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a005SelectSummonCardToHand;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA005ArrivalDirectSummonFromSummonZone,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a005ArrivalDirectSummonFromSummonZone;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA003ArrivalSelectOpponentShackle,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a003ArrivalSelectOpponentShackle;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA008ConditionOpponentOptionalDiscardReturn,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a008ConditionOpponentOptionalDiscardReturn;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA009ConditionOpponentOptionalBenefit,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a009ConditionOpponentOptionalBenefit;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA009SelectGapTreasureToHand,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a009SelectGapTreasureToHand;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA008RewardRyougiOptionalDrawOne,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a008RewardRyougiOptionalDrawOne;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA002RewardOptionalBanishDecision,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a002RewardOptionalBanishDecision;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA002RewardSelectFirstBanishCard,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a002RewardSelectFirstBanishCard;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA002RewardSelectSecondBanishCard,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a002RewardSelectSecondBanishCard;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA002RewardOptionalSakuraReplacement,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a002RewardOptionalSakuraReplacement;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA007ArrivalOptionalBanishFlow,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a007ArrivalOptionalBanishFlow;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA001ArrivalHumanDiscardFlow,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a001ArrivalHumanDiscardFlow;
            return true;
        }

        if (string.Equals(
                pendingContinuationKey,
                AnomalyProcessor.ContinuationKeyA006ArrivalHumanDefenseDiscardFlow,
                StringComparison.Ordinal))
        {
            continuationKind = AnomalyContinuationKind.a006ArrivalHumanDefenseDiscardFlow;
            return true;
        }

        continuationKind = default;
        return false;
    }

    public static AnomalyContinuationKind resolveOrThrow(string? pendingContinuationKey)
    {
        if (tryResolve(pendingContinuationKey, out var continuationKind))
        {
            return continuationKind;
        }

        if (isAnomalyContinuationKey(pendingContinuationKey))
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest pendingContinuationKey is not a supported anomaly continuation.");
        }

        throw new InvalidOperationException("SubmitInputChoiceActionRequest pendingContinuationKey is not an anomaly continuation.");
    }
}
