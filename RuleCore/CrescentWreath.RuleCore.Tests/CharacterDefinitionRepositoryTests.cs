using CrescentWreath.RuleCore.Definitions;

namespace CrescentWreath.RuleCore.Tests;

public class CharacterDefinitionRepositoryTests
{
    [Fact]
    public void ResolveByDefinitionId_WhenDefinitionIdIsKnown_ShouldReturnExpectedSkillCost()
    {
        var definition = CharacterDefinitionRepository.resolveByDefinitionId("C004");

        Assert.True(definition.skills.ContainsKey("C004:1"));
        Assert.Equal(5, definition.skills["C004:1"].manaCost);
        Assert.Equal(1, definition.skills["C004:1"].skillPointCost);
        Assert.Contains("human", definition.raceTags);
    }

    [Fact]
    public void TryResolveSkillCost_WhenDefinitionIdAndSkillKeyAreKnown_ShouldReturnExpectedCosts()
    {
        var found = CharacterDefinitionRepository.tryResolveSkillCost("C004", "C004:1", out var manaCost, out var skillPointCost);

        Assert.True(found);
        Assert.Equal(5, manaCost);
        Assert.Equal(1, skillPointCost);
    }

    [Fact]
    public void TryResolveSkillCost_WhenDefinitionIdIsUnknown_ShouldReturnFalse()
    {
        var found = CharacterDefinitionRepository.tryResolveSkillCost("C999", "C999:1", out var manaCost, out var skillPointCost);

        Assert.False(found);
        Assert.Equal(0, manaCost);
        Assert.Equal(0, skillPointCost);
    }

    [Fact]
    public void TryResolveSkillCost_WhenSkillKeyIsUnknownForKnownCharacter_ShouldReturnFalse()
    {
        var found = CharacterDefinitionRepository.tryResolveSkillCost("C004", "C004:999", out var manaCost, out var skillPointCost);

        Assert.False(found);
        Assert.Equal(0, manaCost);
        Assert.Equal(0, skillPointCost);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenDefinitionIdIsUnknown_ShouldReturnEmptyRaceTags()
    {
        var definition = CharacterDefinitionRepository.resolveByDefinitionId("C999");

        Assert.Empty(definition.raceTags);
    }

}
