using CrescentWreath.RuleCore.ActionSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Ids;

namespace CrescentWreath.RuleCore.Tests;

public class MarkerRuntimeTests
{
    [Fact]
    public void AddMarker_WhenMarkerIsAllowed_ShouldIncreaseCountAndEmitEvent()
    {
        var character = createCharacter("C005");

        var markerChangedEvent = MarkerRuntime.addMarker(
            character,
            "swordAura",
            delta: 1,
            new ActionChainId(101),
            eventId: 102);

        Assert.Equal(1, MarkerRuntime.getMarkerCount(character, "swordAura"));
        Assert.Equal("markerChanged", markerChangedEvent.eventTypeKey);
        Assert.Equal("swordAura", markerChangedEvent.markerTypeKey);
        Assert.Equal(0, markerChangedEvent.beforeCount);
        Assert.Equal(1, markerChangedEvent.afterCount);
        Assert.Equal(1, markerChangedEvent.delta);
    }

    [Fact]
    public void AddMarker_WhenMarkerWouldExceedCap_ShouldClampToCap()
    {
        var character = createCharacter("C017");

        MarkerRuntime.addMarker(character, "fire", delta: 1, new ActionChainId(201), eventId: 202);
        var markerChangedEvent = MarkerRuntime.addMarker(character, "fire", delta: 1, new ActionChainId(201), eventId: 203);

        Assert.Equal(1, MarkerRuntime.getMarkerCount(character, "fire"));
        Assert.Equal(1, markerChangedEvent.beforeCount);
        Assert.Equal(1, markerChangedEvent.afterCount);
        Assert.Equal(0, markerChangedEvent.delta);
    }

    [Fact]
    public void CanAddMarker_WhenMarkerIsNotAllowed_ShouldReturnFalse()
    {
        var character = createCharacter("C001");

        Assert.False(MarkerRuntime.canAddMarker(character, "swordAura"));
    }

    private static CharacterInstance createCharacter(string definitionId)
    {
        return new CharacterInstance
        {
            characterInstanceId = new CharacterInstanceId(100001),
            definitionId = definitionId,
            ownerPlayerId = new PlayerId(1),
            currentHp = 4,
            maxHp = 4,
            isAlive = true,
            isInPlay = true,
        };
    }
}
