using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.Tests;

public class PlayerDeckRuntimeTests
{
    [Fact]
    public void CreateShuffledCardInstanceIds_WhenSeedProvided_ShouldUseDeterministicFisherYatesOrder()
    {
        var cardInstanceIds = new[]
        {
            new CardInstanceId(1),
            new CardInstanceId(2),
            new CardInstanceId(3),
            new CardInstanceId(4),
            new CardInstanceId(5),
        };

        var shuffledCardInstanceIds = PlayerDeckRuntime.createShuffledCardInstanceIds(cardInstanceIds, 12345);

        Assert.Equal(
            new[]
            {
                new CardInstanceId(4),
                new CardInstanceId(2),
                new CardInstanceId(3),
                new CardInstanceId(5),
                new CardInstanceId(1),
            },
            shuffledCardInstanceIds);
        Assert.Equal(new CardInstanceId(1), cardInstanceIds[0]);
    }

    [Fact]
    public void CreateShuffledCardInstanceIds_ShouldPreserveEveryCardInstanceId()
    {
        var cardInstanceIds = new[]
        {
            new CardInstanceId(10),
            new CardInstanceId(20),
            new CardInstanceId(30),
            new CardInstanceId(40),
        };

        var shuffledCardInstanceIds = PlayerDeckRuntime.createShuffledCardInstanceIds(cardInstanceIds, 24680);

        Assert.Equal(cardInstanceIds.OrderBy(card => card.Value), shuffledCardInstanceIds.OrderBy(card => card.Value));
    }
}
