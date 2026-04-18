using System.Linq;
using CrescentWreath.RuleCore.Definitions;

namespace CrescentWreath.RuleCore.Tests;

public class CharacterDefinitionRepositoryTests
{
    [Fact]
    public void ResolveByDefinitionId_WhenDefinitionIdIsKnown_ShouldReturnExpectedBaseFieldsAndSkills()
    {
        var definition = CharacterDefinitionRepository.resolveByDefinitionId("C004");

        Assert.Equal("C004", definition.definitionId);
        Assert.Equal("魔理沙", definition.characterName);
        Assert.Equal(4, definition.baseMaxHp);
        Assert.Contains("human", definition.raceTags);
        Assert.Equal(4, definition.skills.Count);

        var skill = definition.skills["C004:1"];
        Assert.Equal("极限火花", skill.skillName);
        Assert.Equal(1, skill.skillOrder);
        Assert.Equal("通常", skill.skillTypeRaw);
        Assert.Equal("active", skill.skillType);
        Assert.Equal(5, skill.manaCost);
        Assert.Equal(0, skill.skillPointCost);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenTypeIsHumanAndNonHuman_ShouldContainBothRaceTags()
    {
        var definition = CharacterDefinitionRepository.resolveByDefinitionId("C005");

        Assert.Contains("human", definition.raceTags);
        Assert.Contains("nonHuman", definition.raceTags);
        Assert.Equal(2, definition.raceTags.Count);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenDefinitionIdIsUnknown_ShouldReturnEmptyDefaults()
    {
        var definition = CharacterDefinitionRepository.resolveByDefinitionId("C999");

        Assert.Equal("C999", definition.definitionId);
        Assert.Equal(string.Empty, definition.characterName);
        Assert.Equal(4, definition.baseMaxHp);
        Assert.Empty(definition.raceTags);
        Assert.Empty(definition.skills);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenResolvingAllRealCharacterIds_ShouldExistFromC001ToC031()
    {
        for (var i = 1; i <= 31; i++)
        {
            var definitionId = $"C{i:000}";
            var definition = CharacterDefinitionRepository.resolveByDefinitionId(definitionId);

            Assert.Equal(definitionId, definition.definitionId);
            Assert.False(string.IsNullOrWhiteSpace(definition.characterName));
            Assert.Equal(4, definition.baseMaxHp);
        }
    }

    [Fact]
    public void ResolveByDefinitionId_WhenCharacterContainsExtendedRowsInSource_ShouldKeepOnlyStandardSkills()
    {
        var definition = CharacterDefinitionRepository.resolveByDefinitionId("C017");

        Assert.Equal(4, definition.skills.Count);
        Assert.True(definition.skills.ContainsKey("C017:1"));
        Assert.True(definition.skills.ContainsKey("C017:2"));
        Assert.True(definition.skills.ContainsKey("C017:3"));
        Assert.True(definition.skills.ContainsKey("C017:4"));
    }

    [Fact]
    public void TryResolveSkillCost_WhenDefinitionIdAndSkillKeyAreKnown_ShouldReturnExpectedCosts()
    {
        var found = CharacterDefinitionRepository.tryResolveSkillCost("C001", "C001:3", out var manaCost, out var skillPointCost);

        Assert.True(found);
        Assert.Equal(6, manaCost);
        Assert.Equal(2, skillPointCost);
    }

    [Fact]
    public void TryResolveSkillCost_WhenSkillCostColumnIsEmpty_ShouldFallbackToEffectAndReturnExpectedCosts()
    {
        var found = CharacterDefinitionRepository.tryResolveSkillCost("C002", "C002:3", out var manaCost, out var skillPointCost);

        Assert.True(found);
        Assert.Equal(6, manaCost);
        Assert.Equal(2, skillPointCost);
    }

    [Fact]
    public void TryResolveSkillCost_WhenCostIsComplexAndManaCannotBeParsed_ShouldDegradeToManaZero()
    {
        var found = CharacterDefinitionRepository.tryResolveSkillCost("C017", "C017:4", out var manaCost, out var skillPointCost);

        Assert.True(found);
        Assert.Equal(0, manaCost);
        Assert.Equal(4, skillPointCost);
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
}

