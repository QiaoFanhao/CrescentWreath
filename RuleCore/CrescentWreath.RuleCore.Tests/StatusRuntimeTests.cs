using System;
using System.Linq;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.StatusSystem;

namespace CrescentWreath.RuleCore.Tests;

public sealed class StatusRuntimeTests
{
    [Fact]
    public void ApplyStatus_WhenTargetIsCharacter_ShouldBeQueryableByCharacter()
    {
        var gameState = new RuleCore.GameState.GameState();
        var characterInstanceId = new CharacterInstanceId(1);
        var appliedStatus = new StatusInstance
        {
            statusKey = "test:seal",
            targetCharacterInstanceId = characterInstanceId,
            durationTypeKey = "turn",
        };

        StatusRuntime.applyStatus(gameState, appliedStatus);

        var queriedStatuses = StatusRuntime.queryStatusesForCharacter(gameState, characterInstanceId);
        Assert.Single(queriedStatuses);
        Assert.Equal("test:seal", queriedStatuses[0].statusKey);
        Assert.Equal(characterInstanceId, queriedStatuses[0].targetCharacterInstanceId);
    }

    [Fact]
    public void ConsumeNextDamageEffectsOnAttempt_WhenStatusExists_ShouldConsumeOnlyOnce()
    {
        var gameState = new RuleCore.GameState.GameState();
        var sourcePlayerId = new PlayerId(1);
        var otherPlayerId = new PlayerId(2);

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "status:penetrate",
                targetPlayerId = sourcePlayerId,
                durationTypeKey = StatusRuntime.DurationTypeKeyNextDamageAttempt,
                stackCount = 1,
            });

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "status:otherDuration",
                targetPlayerId = sourcePlayerId,
                durationTypeKey = "turn",
                stackCount = 1,
            });

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "status:otherPlayer",
                targetPlayerId = otherPlayerId,
                durationTypeKey = StatusRuntime.DurationTypeKeyNextDamageAttempt,
                stackCount = 1,
            });

        var firstConsumed = StatusRuntime.consumeNextDamageEffectsOnAttempt(gameState, sourcePlayerId);
        Assert.Single(firstConsumed);
        Assert.Equal("Penetrate", firstConsumed[0]);

        var secondConsumed = StatusRuntime.consumeNextDamageEffectsOnAttempt(gameState, sourcePlayerId);
        Assert.Empty(secondConsumed);
        Assert.DoesNotContain(gameState.statusInstances, status => status.statusKey == "Penetrate");
        Assert.Contains(gameState.statusInstances, status => status.statusKey == "status:otherDuration");
        Assert.Contains(gameState.statusInstances, status => status.statusKey == "status:otherPlayer");
    }

    [Fact]
    public void ApplyStatus_WhenTargetMissing_ShouldThrow()
    {
        var gameState = new RuleCore.GameState.GameState();
        var invalidStatus = new StatusInstance
        {
            statusKey = "status:invalid",
            durationTypeKey = "turn",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => StatusRuntime.applyStatus(gameState, invalidStatus));

        Assert.Equal(
            "StatusRuntime.applyStatus requires at least one target: targetCharacterInstanceId, targetCardInstanceId, or targetPlayerId.",
            exception.Message);
        Assert.Empty(gameState.statusInstances);
    }

    [Fact]
    public void ApplyStatus_WhenBarrierAppliedTwice_ShouldKeepSingleInstance()
    {
        var gameState = new RuleCore.GameState.GameState();
        var targetCharacterInstanceId = new CharacterInstanceId(10);

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Barrier",
                targetCharacterInstanceId = targetCharacterInstanceId,
                durationTypeKey = "untilConsumed",
            });

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "status:barrier",
                targetCharacterInstanceId = targetCharacterInstanceId,
                durationTypeKey = "untilConsumed",
            });

        var statuses = StatusRuntime.queryStatusesForCharacter(gameState, targetCharacterInstanceId);
        Assert.Single(statuses);
        Assert.Equal("Barrier", statuses[0].statusKey);
        Assert.Equal(1, statuses[0].stackCount);
    }

    [Fact]
    public void ApplyStatus_WhenSealAppliedTwice_ShouldKeepSingleInstance()
    {
        var gameState = new RuleCore.GameState.GameState();
        var targetCharacterInstanceId = new CharacterInstanceId(11);

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Seal",
                targetCharacterInstanceId = targetCharacterInstanceId,
                durationTypeKey = "untilNextTurnStart",
            });

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Seal",
                targetCharacterInstanceId = targetCharacterInstanceId,
                durationTypeKey = "untilNextTurnStart",
            });

        var statuses = StatusRuntime.queryStatusesForCharacter(gameState, targetCharacterInstanceId);
        Assert.Single(statuses);
        Assert.Equal("Seal", statuses[0].statusKey);
        Assert.Equal(1, statuses[0].stackCount);
    }

    [Fact]
    public void RemoveStatusOnCharacter_WhenSealExists_ShouldRemoveOnlyTargetCharacterSeal()
    {
        var gameState = new RuleCore.GameState.GameState();
        var targetCharacterInstanceId = new CharacterInstanceId(111);
        var otherCharacterInstanceId = new CharacterInstanceId(112);

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "status:seal",
                targetCharacterInstanceId = targetCharacterInstanceId,
                durationTypeKey = "untilNextTurnStart",
            });
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Seal",
                targetCharacterInstanceId = otherCharacterInstanceId,
                durationTypeKey = "untilNextTurnStart",
            });
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Barrier",
                targetCharacterInstanceId = targetCharacterInstanceId,
                durationTypeKey = "untilConsumed",
            });

        var removedCount = StatusRuntime.removeStatusOnCharacter(
            gameState,
            targetCharacterInstanceId,
            "Seal");

        Assert.Equal(1, removedCount);
        Assert.DoesNotContain(
            StatusRuntime.queryStatusesForCharacter(gameState, targetCharacterInstanceId),
            status => string.Equals(status.statusKey, "Seal", StringComparison.Ordinal));
        Assert.Contains(
            StatusRuntime.queryStatusesForCharacter(gameState, targetCharacterInstanceId),
            status => string.Equals(status.statusKey, "Barrier", StringComparison.Ordinal));
        Assert.Contains(
            StatusRuntime.queryStatusesForCharacter(gameState, otherCharacterInstanceId),
            status => string.Equals(status.statusKey, "Seal", StringComparison.Ordinal));
    }

    [Fact]
    public void HasStatusOnCharacter_WhenAliasStatusExists_ShouldMatchCanonicalStatusKey()
    {
        var gameState = new RuleCore.GameState.GameState();
        var targetCharacterInstanceId = new CharacterInstanceId(113);

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "status:shackle",
                targetCharacterInstanceId = targetCharacterInstanceId,
                durationTypeKey = "resolveAtNextTurnStart",
            });

        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterInstanceId, "Shackle"));
        Assert.True(StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterInstanceId, "status:shackle"));
        Assert.False(StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterInstanceId, "Seal"));
    }

    [Fact]
    public void HasStatusOnPlayer_WhenAliasStatusExists_ShouldMatchCanonicalStatusKey()
    {
        var gameState = new RuleCore.GameState.GameState();
        var targetPlayerId = new PlayerId(114);
        var otherPlayerId = new PlayerId(115);

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "status:silence",
                targetPlayerId = targetPlayerId,
                durationTypeKey = "untilEndOfTurn",
            });
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Charm",
                targetPlayerId = otherPlayerId,
                durationTypeKey = "untilEndOfTurn",
            });

        Assert.True(StatusRuntime.hasStatusOnPlayer(gameState, targetPlayerId, "Silence"));
        Assert.True(StatusRuntime.hasStatusOnPlayer(gameState, targetPlayerId, "status:silence"));
        Assert.False(StatusRuntime.hasStatusOnPlayer(gameState, targetPlayerId, "Penetrate"));
    }

    [Fact]
    public void ApplyStatus_WhenShackleAppliedTwice_ShouldKeepSingleInstance()
    {
        var gameState = new RuleCore.GameState.GameState();
        var targetCharacterInstanceId = new CharacterInstanceId(12);

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Shackle",
                targetCharacterInstanceId = targetCharacterInstanceId,
                durationTypeKey = "resolveAtNextTurnStart",
            });

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Shackle",
                targetCharacterInstanceId = targetCharacterInstanceId,
                durationTypeKey = "resolveAtNextTurnStart",
            });

        var statuses = StatusRuntime.queryStatusesForCharacter(gameState, targetCharacterInstanceId);
        Assert.Single(statuses);
        Assert.Equal("Shackle", statuses[0].statusKey);
        Assert.Equal(1, statuses[0].stackCount);
    }

    [Fact]
    public void ApplyStatus_WhenSilenceAppliedTwice_ShouldRefreshSingleInstance()
    {
        var gameState = new RuleCore.GameState.GameState();
        var targetPlayerId = new PlayerId(20);

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Silence",
                targetPlayerId = targetPlayerId,
                durationTypeKey = "untilEndOfTurn",
                remainingDuration = 1,
            });

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "status:silence",
                targetPlayerId = targetPlayerId,
                durationTypeKey = "untilEndOfTurn",
                remainingDuration = 3,
            });

        var statuses = gameState.statusInstances
            .Where(status => status.targetPlayerId == targetPlayerId)
            .ToList();
        Assert.Single(statuses);
        Assert.Equal("Silence", statuses[0].statusKey);
        Assert.Equal(3, statuses[0].remainingDuration);
        Assert.Equal(1, statuses[0].stackCount);
    }

    [Fact]
    public void ApplyStatus_WhenCharmAppliedTwice_ShouldRefreshSingleInstance()
    {
        var gameState = new RuleCore.GameState.GameState();
        var targetPlayerId = new PlayerId(21);

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Charm",
                targetPlayerId = targetPlayerId,
                durationTypeKey = "consumeOnNextDamage",
                remainingDuration = 1,
            });

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "status:charm",
                targetPlayerId = targetPlayerId,
                durationTypeKey = "consumeOnNextDamage",
                remainingDuration = 2,
            });

        var statuses = gameState.statusInstances
            .Where(status => status.targetPlayerId == targetPlayerId)
            .ToList();
        Assert.Single(statuses);
        Assert.Equal("Charm", statuses[0].statusKey);
        Assert.Equal(2, statuses[0].remainingDuration);
        Assert.Equal(1, statuses[0].stackCount);
    }

    [Fact]
    public void ApplyStatus_WhenPenetrateAppliedTwice_ShouldRefreshSingleInstanceAndConsumeOnce()
    {
        var gameState = new RuleCore.GameState.GameState();
        var sourcePlayerId = new PlayerId(31);

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Penetrate",
                targetPlayerId = sourcePlayerId,
                durationTypeKey = StatusRuntime.DurationTypeKeyNextDamageAttempt,
                remainingDuration = 1,
            });

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "status:penetrate",
                targetPlayerId = sourcePlayerId,
                durationTypeKey = StatusRuntime.DurationTypeKeyNextDamageAttempt,
                remainingDuration = 2,
            });

        Assert.Single(
            gameState.statusInstances,
            status =>
                status.targetPlayerId == sourcePlayerId &&
                status.statusKey == "Penetrate");

        var firstConsumed = StatusRuntime.consumeNextDamageEffectsOnAttempt(gameState, sourcePlayerId);
        Assert.Single(firstConsumed);
        Assert.Equal("Penetrate", firstConsumed[0]);

        var secondConsumed = StatusRuntime.consumeNextDamageEffectsOnAttempt(gameState, sourcePlayerId);
        Assert.Empty(secondConsumed);
    }

    [Fact]
    public void ClearShortStatusesAtTurnEnd_ShouldRemoveSilenceCharmPenetrateAndKeepLongTermStatuses()
    {
        var gameState = new RuleCore.GameState.GameState();
        var playerId = new PlayerId(41);
        var otherPlayerId = new PlayerId(42);
        var characterInstanceId = new CharacterInstanceId(4101);

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "status:silence",
                targetPlayerId = playerId,
                durationTypeKey = "untilEndOfTurn",
            });
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Charm",
                targetPlayerId = otherPlayerId,
                durationTypeKey = "untilEndOfTurn",
            });
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "status:penetrate",
                targetPlayerId = playerId,
                durationTypeKey = StatusRuntime.DurationTypeKeyNextDamageAttempt,
            });
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Seal",
                targetCharacterInstanceId = characterInstanceId,
                durationTypeKey = "untilNextTurnStart",
            });
        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Barrier",
                targetCharacterInstanceId = characterInstanceId,
                durationTypeKey = "untilConsumed",
            });

        var removedStatuses = StatusRuntime.clearShortStatusesAtTurnEnd(gameState);

        Assert.Equal(3, removedStatuses.Count);
        Assert.Equal("Silence", removedStatuses[0].statusKey);
        Assert.Equal("Charm", removedStatuses[1].statusKey);
        Assert.Equal("Penetrate", removedStatuses[2].statusKey);
        Assert.DoesNotContain(gameState.statusInstances, status => status.statusKey == "Silence");
        Assert.DoesNotContain(gameState.statusInstances, status => status.statusKey == "Charm");
        Assert.DoesNotContain(gameState.statusInstances, status => status.statusKey == "Penetrate");
        Assert.Contains(gameState.statusInstances, status => status.statusKey == "Seal");
        Assert.Contains(gameState.statusInstances, status => status.statusKey == "Barrier");
    }

    [Fact]
    public void ClearShortStatusesAtTurnEnd_WhenPenetrateAlreadyConsumed_ShouldNotConsumeAgain()
    {
        var gameState = new RuleCore.GameState.GameState();
        var playerId = new PlayerId(51);

        StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = "Penetrate",
                targetPlayerId = playerId,
                durationTypeKey = StatusRuntime.DurationTypeKeyNextDamageAttempt,
            });
        StatusRuntime.consumeNextDamageEffectsOnAttempt(gameState, playerId);

        var removedStatuses = StatusRuntime.clearShortStatusesAtTurnEnd(gameState);

        Assert.Empty(removedStatuses);
        Assert.DoesNotContain(gameState.statusInstances, status => status.statusKey == "Penetrate");
    }
}
