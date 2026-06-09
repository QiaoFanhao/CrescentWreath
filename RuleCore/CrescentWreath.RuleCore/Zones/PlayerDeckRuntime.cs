using System;
using System.Collections.Generic;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Zones;

public static class PlayerDeckRuntime
{
    private static readonly object SharedRandomLock = new();
    private static readonly Random SharedRandom = new();

    public static List<CardInstanceId> createShuffledCardInstanceIds(IReadOnlyList<CardInstanceId> cardInstanceIds)
    {
        var shuffledCardInstanceIds = new List<CardInstanceId>(cardInstanceIds);
        shuffleCardInstanceIdsInPlace(shuffledCardInstanceIds);
        return shuffledCardInstanceIds;
    }

    public static List<CardInstanceId> createShuffledCardInstanceIds(
        IReadOnlyList<CardInstanceId> cardInstanceIds,
        int shuffleSeed)
    {
        var shuffledCardInstanceIds = new List<CardInstanceId>(cardInstanceIds);
        shuffleCardInstanceIdsInPlace(shuffledCardInstanceIds, new Random(shuffleSeed));
        return shuffledCardInstanceIds;
    }

    private static void shuffleCardInstanceIdsInPlace(List<CardInstanceId> cardInstanceIds)
    {
        for (var index = cardInstanceIds.Count - 1; index > 0; index--)
        {
            int randomIndex;
            lock (SharedRandomLock)
            {
                randomIndex = SharedRandom.Next(index + 1);
            }

            (cardInstanceIds[index], cardInstanceIds[randomIndex]) =
                (cardInstanceIds[randomIndex], cardInstanceIds[index]);
        }
    }

    private static void shuffleCardInstanceIdsInPlace(List<CardInstanceId> cardInstanceIds, Random random)
    {
        for (var index = cardInstanceIds.Count - 1; index > 0; index--)
        {
            var randomIndex = random.Next(index + 1);
            (cardInstanceIds[index], cardInstanceIds[randomIndex]) =
                (cardInstanceIds[randomIndex], cardInstanceIds[index]);
        }
    }
}
