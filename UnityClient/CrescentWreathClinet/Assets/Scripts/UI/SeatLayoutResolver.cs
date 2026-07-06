using System.Collections.Generic;
using System.Linq;
using CrescentWreath.Client.Net;

namespace CrescentWreath.Client.UI
{
public sealed class SeatLayoutResult
{
    public bool isResolved;
    public long? bottomPlayerNumericId;
    public long? topPlayerNumericId;
    public long? leftPlayerNumericId;
    public long? rightPlayerNumericId;
    public string failureReasonKey = string.Empty;
}

public static class SeatLayoutResolver
{
    public static SeatLayoutResult Resolve(
        long viewerPlayerNumericId,
        IReadOnlyList<long> turnOrderPlayerNumericIds,
        IReadOnlyList<ProjectionPlayerSummaryViewModel> playerSummaries)
    {
        if (viewerPlayerNumericId <= 0)
        {
            return failed("missingViewer");
        }

        if (turnOrderPlayerNumericIds is null ||
            turnOrderPlayerNumericIds.Count <= 0 ||
            !turnOrderPlayerNumericIds.Contains(viewerPlayerNumericId))
        {
            return failed("missingSeatOrder");
        }

        if (playerSummaries is null)
        {
            return failed("missingViewerTeam");
        }

        var playersById = playerSummaries
            .Where(player => player.playerNumericId > 0 && player.teamNumericId > 0)
            .GroupBy(player => player.playerNumericId)
            .ToDictionary(group => group.Key, group => group.First());
        if (!playersById.TryGetValue(viewerPlayerNumericId, out var viewerSummary))
        {
            return failed("missingViewerTeam");
        }

        foreach (var playerId in turnOrderPlayerNumericIds)
        {
            if (!playersById.ContainsKey(playerId))
            {
                return failed("missingSeatPlayerTeam");
            }
        }

        var topPlayerNumericId = turnOrderPlayerNumericIds
            .FirstOrDefault(playerId =>
                playerId != viewerPlayerNumericId &&
                playersById[playerId].teamNumericId == viewerSummary.teamNumericId);
        if (topPlayerNumericId <= 0)
        {
            return failed("missingTeammate");
        }

        var viewerIndex = indexOf(turnOrderPlayerNumericIds, viewerPlayerNumericId);
        var leftPlayerNumericId = findEnemyInDirection(
            turnOrderPlayerNumericIds,
            playersById,
            viewerSummary.teamNumericId,
            viewerIndex,
            step: 1);
        var rightPlayerNumericId = findEnemyInDirection(
            turnOrderPlayerNumericIds,
            playersById,
            viewerSummary.teamNumericId,
            viewerIndex,
            step: -1);
        if (!leftPlayerNumericId.HasValue || !rightPlayerNumericId.HasValue)
        {
            return failed("missingOpponent");
        }

        return new SeatLayoutResult
        {
            isResolved = true,
            bottomPlayerNumericId = viewerPlayerNumericId,
            topPlayerNumericId = topPlayerNumericId,
            leftPlayerNumericId = leftPlayerNumericId,
            rightPlayerNumericId = rightPlayerNumericId,
        };
    }

    private static long? findEnemyInDirection(
        IReadOnlyList<long> seatOrderPlayerNumericIds,
        IReadOnlyDictionary<long, ProjectionPlayerSummaryViewModel> playersById,
        long viewerTeamNumericId,
        int viewerIndex,
        int step)
    {
        for (var offset = 1; offset < seatOrderPlayerNumericIds.Count; offset += 1)
        {
            var index = (viewerIndex + step * offset + seatOrderPlayerNumericIds.Count) %
                        seatOrderPlayerNumericIds.Count;
            var playerId = seatOrderPlayerNumericIds[index];
            if (playersById[playerId].teamNumericId != viewerTeamNumericId)
            {
                return playerId;
            }
        }

        return null;
    }

    private static int indexOf(IReadOnlyList<long> values, long targetValue)
    {
        for (var index = 0; index < values.Count; index += 1)
        {
            if (values[index] == targetValue)
            {
                return index;
            }
        }

        return -1;
    }

    private static SeatLayoutResult failed(string failureReasonKey)
    {
        return new SeatLayoutResult
        {
            isResolved = false,
            failureReasonKey = failureReasonKey,
        };
    }
}
}
