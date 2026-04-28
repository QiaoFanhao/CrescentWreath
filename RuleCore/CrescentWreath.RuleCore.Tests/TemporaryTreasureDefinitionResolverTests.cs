using CrescentWreath.RuleCore.ActionSystem;

namespace CrescentWreath.RuleCore.Tests;

public class TemporaryTreasureDefinitionResolverTests
{
    [Theory]
    [InlineData("starter:magicCircuit", 1, 0, null, false)]
    [InlineData("starter:kourindouCoupon", 0, 1, null, false)]
    [InlineData("test-summon-card", 0, 0, 1, false)]
    [InlineData("T001", 1, 1, 3, false)]
    [InlineData("T002", 2, 1, 2, false)]
    [InlineData("T016", 0, 0, 4, true)]
    [InlineData("S001", 1, 2, 3, false)]
    public void ResolveDefinition_WhenDefinitionIdIsKnown_ShouldReturnExpectedTemporaryShape(
        string definitionId,
        int expectedManaGainOnEnterField,
        int expectedSigilPreviewGainOnEnterField,
        int? expectedSummonSigilCost,
        bool expectedPersistOnFieldAcrossEnd)
    {
        var definition = TemporaryTreasureDefinitionResolver.resolveDefinition(definitionId);

        Assert.Equal(expectedManaGainOnEnterField, definition.manaGainOnEnterField);
        Assert.Equal(expectedSigilPreviewGainOnEnterField, definition.sigilPreviewGainOnEnterField);
        Assert.Equal(expectedSummonSigilCost, definition.summonSigilCost);
        Assert.Equal(expectedPersistOnFieldAcrossEnd, definition.persistOnFieldAcrossEnd);
    }

    [Fact]
    public void ResolveDefinition_WhenDefinitionIdIsUnknown_ShouldReturnDefaultTemporaryShape()
    {
        var definition = TemporaryTreasureDefinitionResolver.resolveDefinition("unknown:definition");

        Assert.Equal(0, definition.manaGainOnEnterField);
        Assert.Equal(0, definition.sigilPreviewGainOnEnterField);
        Assert.Null(definition.summonSigilCost);
        Assert.False(definition.persistOnFieldAcrossEnd);
    }

    [Theory]
    [InlineData("test-summon-card", 1)]
    [InlineData("T001", 3)]
    [InlineData("T016", 4)]
    [InlineData("T024", 1)]
    [InlineData("S001", 3)]
    public void ResolveSummonSigilCost_WhenDefinitionIdHasSummonCost_ShouldReturnExpectedValue(
        string definitionId,
        int expectedSummonSigilCost)
    {
        var costFromTemporaryResolver = TemporaryTreasureDefinitionResolver.resolveSummonSigilCost(definitionId);
        var costFromCompatibilityResolver = TreasureResourceValueResolver.resolveSummonSigilCost(definitionId);

        Assert.Equal(expectedSummonSigilCost, costFromTemporaryResolver);
        Assert.Equal(expectedSummonSigilCost, costFromCompatibilityResolver);
    }

    [Fact]
    public void ResolveSummonSigilCost_WhenDefinitionIdIsUnsupported_ShouldKeepExistingExceptionText()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => TreasureResourceValueResolver.resolveSummonSigilCost("unsupported:definition"));

        Assert.Equal(
            "SummonTreasureCardActionRequest requires treasure definition summonSigilCost to be defined.",
            ex.Message);
    }
}
