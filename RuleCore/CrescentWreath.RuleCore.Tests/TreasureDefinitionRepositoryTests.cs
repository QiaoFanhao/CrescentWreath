using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CrescentWreath.RuleCore.Definitions;
using Newtonsoft.Json;

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
    }

    [Fact]
    public void InMemoryTreasureDefinitionSource_ShouldContainExactlyTwentyNineRealTreasureDefinitions()
    {
        var source = new InMemoryTreasureDefinitionSource();
        var definitions = source.getTreasureDefinitions();

        var realTreasureDefinitions = definitions.Where(
            d => d.definitionId.Length == 4 && d.definitionId.StartsWith("T", StringComparison.Ordinal));
        Assert.Equal(29, realTreasureDefinitions.Count());
    }

    [Fact]
    public void InMemoryTreasureDefinitionSource_GameplayTreasureDefinitions_ShouldAlwaysHaveSummonCost()
    {
        var source = new InMemoryTreasureDefinitionSource();
        var definitions = source.getTreasureDefinitions();

        var gameplayTreasureDefinitions = definitions.Where(
            d => d.definitionId.StartsWith("starter:", StringComparison.Ordinal)
                || d.definitionId.Equals("S001", StringComparison.Ordinal)
                || (d.definitionId.Length == 4 && d.definitionId.StartsWith("T", StringComparison.Ordinal)));

        Assert.All(gameplayTreasureDefinitions, definition => Assert.True(
            definition.summonSigilCost.HasValue,
            $"Gameplay treasure definition '{definition.definitionId}' must define summonSigilCost."));
        Assert.Equal(0, definitions.Single(d => d.definitionId == "starter:magicCircuit").summonSigilCost);
        Assert.Equal(0, definitions.Single(d => d.definitionId == "starter:kourindouCoupon").summonSigilCost);
    }

    [Fact]
    public void TreasureBaseline_ShouldContainExactlyT001ToT029()
    {
        var baselineEntries = loadTreasureBaselineEntries();
        var baselineDefinitionIds = baselineEntries.Select(entry => entry.definitionId).ToArray();
        var expectedDefinitionIds = Enumerable.Range(1, 29).Select(index => $"T{index:000}").ToArray();

        Assert.Equal(29, baselineDefinitionIds.Length);
        Assert.Equal(expectedDefinitionIds, baselineDefinitionIds);
        Assert.Equal(29, baselineDefinitionIds.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void ResolveByDefinitionId_WhenRealTreasureDefinitionsAreLoadedFromBaseline_ShouldMatchAllNumericFields()
    {
        var baselineEntries = loadTreasureBaselineEntries();

        foreach (var baselineEntry in baselineEntries)
        {
            var definition = TreasureDefinitionRepository.resolveByDefinitionId(baselineEntry.definitionId);

            Assert.Equal(baselineEntry.definitionId, definition.definitionId);
            Assert.Equal(baselineEntry.initialPublicDeckCopies, definition.initialPublicDeckCopies);
            Assert.Equal(baselineEntry.summonSigilCost, definition.summonSigilCost);
            Assert.Equal(baselineEntry.manaGainOnEnterField, definition.manaGainOnEnterField);
            Assert.Equal(baselineEntry.sigilPreviewGainOnEnterField, definition.sigilPreviewGainOnEnterField);
            Assert.Equal(baselineEntry.defenseTypeKey, definition.defenseTypeKey);
            Assert.Equal(baselineEntry.defenseValue, definition.defenseValue);
            Assert.Equal(baselineEntry.persistOnFieldAcrossEnd, definition.persistOnFieldAcrossEnd);
        }
    }

    [Fact]
    public void RealTreasureBaseline_ShouldCoverPhysicalSpellAndDualDefenseTypes()
    {
        var defenseTypeKeys = loadTreasureBaselineEntries()
            .Select(entry => entry.defenseTypeKey)
            .Where(defenseTypeKey => !string.IsNullOrWhiteSpace(defenseTypeKey))
            .Distinct(StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("physical", defenseTypeKeys);
        Assert.Contains("spell", defenseTypeKeys);
        Assert.Contains("dual", defenseTypeKeys);
    }

    [Fact]
    public void ResolveByDefinitionId_WhenRealTreasureDefinitionsAreLoadedFromBaseline_ShouldKeepCanDefenseEquivalentShape()
    {
        foreach (var baselineEntry in loadTreasureBaselineEntries())
        {
            var definition = TreasureDefinitionRepository.resolveByDefinitionId(baselineEntry.definitionId);
            var baselineCanDefense = baselineEntry.defenseValue.HasValue && !string.IsNullOrWhiteSpace(baselineEntry.defenseTypeKey);
            var definitionCanDefense = definition.defenseValue.HasValue && !string.IsNullOrWhiteSpace(definition.defenseTypeKey);

            Assert.Equal(baselineCanDefense, definitionCanDefense);
        }
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

    [Fact]
    public void GetDeclarableTreasureDefinitionIds_ShouldIncludeGameplayTreasuresAndExcludeTestDefinitions()
    {
        var definitionIds = TreasureDefinitionRepository.getDeclarableTreasureDefinitionIds();

        Assert.Contains("starter:magicCircuit", definitionIds);
        Assert.Contains("starter:kourindouCoupon", definitionIds);
        Assert.Contains("S001", definitionIds);
        foreach (var index in Enumerable.Range(1, 29))
        {
            Assert.Contains($"T{index:000}", definitionIds);
        }

        Assert.DoesNotContain("test-summon-card", definitionIds);
        Assert.DoesNotContain("test:defensePhysical2", definitionIds);
        Assert.Equal(32, definitionIds.Count);
    }

    [Fact]
    public void SkillPointGainOnPlay_ShouldIdentifyA004PaymentCards()
    {
        var expectedDefinitionIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "S001",
            "T005",
            "T011",
            "T018",
        };

        var definitions = new InMemoryTreasureDefinitionSource().getTreasureDefinitions();
        var actualDefinitionIds = definitions
            .Where(definition => definition.skillPointGainOnPlay > 0)
            .Select(definition => definition.definitionId)
            .ToHashSet(StringComparer.Ordinal);

        Assert.True(expectedDefinitionIds.SetEquals(actualDefinitionIds));
        Assert.All(
            definitions.Where(definition => expectedDefinitionIds.Contains(definition.definitionId)),
            definition => Assert.Equal(1, definition.skillPointGainOnPlay));
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

    private static IReadOnlyList<TreasureBaselineEntry> loadTreasureBaselineEntries()
    {
        var baselinePath = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            "treasure_t001_t029_baseline.json");

        Assert.True(File.Exists(baselinePath), $"Treasure baseline file not found: {baselinePath}");

        var json = File.ReadAllText(baselinePath);
        var baselineEntries = JsonConvert.DeserializeObject<List<TreasureBaselineEntry>>(json);
        Assert.NotNull(baselineEntries);

        return baselineEntries!;
    }

    private sealed class TreasureBaselineEntry
    {
        public string definitionId { get; set; } = string.Empty;
        public int initialPublicDeckCopies { get; set; }
        public int summonSigilCost { get; set; }
        public int manaGainOnEnterField { get; set; }
        public int sigilPreviewGainOnEnterField { get; set; }
        public string? defenseTypeKey { get; set; }
        public int? defenseValue { get; set; }
        public bool persistOnFieldAcrossEnd { get; set; }
    }
}
