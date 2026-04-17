using CrescentWreath.RuleCore.ActionSystem;

namespace CrescentWreath.RuleCore.Tests;

public class TemporaryTreasureDefinitionResolverTests
{
    [Theory]
    [InlineData("starter:magicCircuit", 1, 0, false)]
    [InlineData("starter:kourindouCoupon", 0, 1, false)]
    [InlineData("test-summon-card", 0, 0, false)]
    [InlineData("T016", 0, 0, true)]
    public void ResolveDefinition_WhenDefinitionIdIsKnown_ShouldReturnExpectedTemporaryShape(
        string definitionId,
        int expectedManaGainOnEnterField,
        int expectedSigilPreviewGainOnEnterField,
        bool expectedPersistOnFieldAcrossEnd)
    {
        var definition = TemporaryTreasureDefinitionResolver.resolveDefinition(definitionId);

        Assert.Equal(expectedManaGainOnEnterField, definition.manaGainOnEnterField);
        Assert.Equal(expectedSigilPreviewGainOnEnterField, definition.sigilPreviewGainOnEnterField);
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

    [Fact]
    public void ResolveSummonSigilCost_WhenDefinitionIdIsTestSummonCard_ShouldReturnExpectedValue()
    {
        var costFromTemporaryResolver = TemporaryTreasureDefinitionResolver.resolveSummonSigilCost("test-summon-card");
        var costFromCompatibilityResolver = TreasureResourceValueResolver.resolveSummonSigilCost("test-summon-card");

        Assert.Equal(1, costFromTemporaryResolver);
        Assert.Equal(1, costFromCompatibilityResolver);
    }

    [Fact]
    public void ResolveSummonSigilCost_WhenDefinitionIdIsUnsupported_ShouldKeepExistingExceptionText()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => TreasureResourceValueResolver.resolveSummonSigilCost("unsupported:definition"));

        Assert.Equal(
            "SummonTreasureCardActionRequest currently supports only definitionId test-summon-card for lockedSigil payment.",
            ex.Message);
    }
}
