using System;
using System.Linq;
using CrescentWreath.RuleCore.Definitions;

namespace CrescentWreath.RuleCore.Tests;

public class TreasureDefinitionRepositoryTests
{
    [Fact]
    public void InMemoryTreasureDefinitionSource_ShouldContainExpectedSeedDefinitions()
    {
        var source = new InMemoryTreasureDefinitionSource();
        var definitions = source.getTreasureDefinitions();

        Assert.Contains(definitions, d => d.definitionId == "starter:magicCircuit");
        Assert.Contains(definitions, d => d.definitionId == "starter:kourindouCoupon");
        Assert.Contains(definitions, d => d.definitionId == "test-summon-card");
        Assert.Contains(definitions, d => d.definitionId == "test:defensePhysical2");
        Assert.Contains(definitions, d => d.definitionId == "test:defensePhysical1");
        Assert.Contains(definitions, d => d.definitionId == "test:defenseSpell2");
        Assert.Contains(definitions, d => d.definitionId == "test:defenseDual2");
        Assert.Contains(definitions, d => d.definitionId == "S001");

        foreach (var treasureId in Enumerable.Range(1, 29).Select(i => $"T{i:000}"))
        {
            Assert.Contains(definitions, d => string.Equals(d.definitionId, treasureId, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void InMemoryTreasureDefinitionSource_ShouldContainExactlyTwentyNineRealTreasureDefinitions()
    {
        var source = new InMemoryTreasureDefinitionSource();
        var definitions = source.getTreasureDefinitions();

        var realTreasureDefinitions = definitions.Where(d => d.definitionId.Length == 4 && d.definitionId.StartsWith("T", StringComparison.Ordinal));
        Assert.Equal(29, realTreasureDefinitions.Count());
    }

    [Theory]
    [InlineData("starter:magicCircuit", 1, 0, null, false, null, null)]
    [InlineData("starter:kourindouCoupon", 0, 1, null, false, null, null)]
    [InlineData("test-summon-card", 0, 0, 1, false, null, null)]
    [InlineData("T001", 1, 1, 3, false, 2, "dual")]
    [InlineData("T002", 2, 1, 2, false, 4, "spell")]
    [InlineData("T003", 2, 2, 6, false, 4, "physical")]
    [InlineData("T016", 0, 0, 4, true, 3, "dual")]
    [InlineData("S001", 1, 2, 3, false, 3, "dual")]
    public void ResolveByDefinitionId_WhenKnownDefinition_ShouldReturnExpectedValues(
        string definitionId,
        int expectedManaGainOnEnterField,
        int expectedSigilPreviewGainOnEnterField,
        int? expectedSummonSigilCost,
        bool expectedPersistOnFieldAcrossEnd,
        int? expectedDefenseValue,
        string? expectedDefenseTypeKey)
    {
        var definition = TreasureDefinitionRepository.resolveByDefinitionId(definitionId);

        Assert.Equal(definitionId, definition.definitionId);
        Assert.Equal(expectedManaGainOnEnterField, definition.manaGainOnEnterField);
        Assert.Equal(expectedSigilPreviewGainOnEnterField, definition.sigilPreviewGainOnEnterField);
        Assert.Equal(expectedSummonSigilCost, definition.summonSigilCost);
        Assert.Equal(expectedPersistOnFieldAcrossEnd, definition.persistOnFieldAcrossEnd);
        Assert.Equal(expectedDefenseValue, definition.defenseValue);
        Assert.Equal(expectedDefenseTypeKey, definition.defenseTypeKey);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenUnknownDefinition_ShouldReturnDefaultShapeWithInputDefinitionId()
    {
        var definition = TreasureDefinitionRepository.resolveByDefinitionId("unknown:treasure");

        Assert.Equal("unknown:treasure", definition.definitionId);
        Assert.Equal(0, definition.manaGainOnEnterField);
        Assert.Equal(0, definition.sigilPreviewGainOnEnterField);
        Assert.Null(definition.summonSigilCost);
        Assert.False(definition.persistOnFieldAcrossEnd);
        Assert.Null(definition.defenseValue);
        Assert.Null(definition.defenseTypeKey);
    }

    [Theory]
    [InlineData("test:defensePhysical2", 2, "physical")]
    [InlineData("test:defensePhysical1", 1, "physical")]
    [InlineData("test:defenseSpell2", 2, "spell")]
    [InlineData("test:defenseDual2", 2, "dual")]
    public void ResolveByDefinitionId_WhenKnownDefenseDefinition_ShouldReturnExpectedDefenseProfile(
        string definitionId,
        int expectedDefenseValue,
        string expectedDefenseTypeKey)
    {
        var definition = TreasureDefinitionRepository.resolveByDefinitionId(definitionId);

        Assert.Equal(expectedDefenseValue, definition.defenseValue);
        Assert.Equal(expectedDefenseTypeKey, definition.defenseTypeKey);
    }
}
