using System.Collections.Generic;
using CrescentWreath.Client.Net;
using CrescentWreath.Client.Presentation;
using CrescentWreath.Client.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.Tests.EditMode
{
public sealed class CardArtCatalogTests
{
    private const string CatalogAssetPath = "Assets/Resources/CardArt/CardArtCatalog.asset";

    [TearDown]
    public void TearDown()
    {
        CardArtService.ClearCacheForTests();
    }

    [Test]
    public void PackagedCatalog_ShouldContainAllKnownCardIllustrations()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(CatalogAssetPath);

        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog!.CardBack, Is.Not.Null);
        Assert.That(catalog.EntryCount, Is.EqualTo(42));

        foreach (var definitionId in expectedDefinitionIds())
        {
            Assert.That(
                catalog.TryGetSprite(definitionId, out var sprite),
                Is.True,
                $"Missing card art catalog entry for {definitionId}.");
            Assert.That(sprite, Is.Not.Null, $"Missing sprite for {definitionId}.");
        }
    }

    [TestCase("starter:kourindouCoupon", "T001B")]
    [TestCase("starter:magicCircuit", "T002B")]
    public void Catalog_ShouldResolveStarterAliases(string alias, string expectedDefinitionId)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(CatalogAssetPath);

        Assert.That(catalog!.TryGetSprite(alias, out var aliasSprite), Is.True);
        Assert.That(catalog.TryGetSprite(expectedDefinitionId, out var expectedSprite), Is.True);
        Assert.That(aliasSprite, Is.SameAs(expectedSprite));
    }

    [Test]
    public void UnknownDefinition_ShouldFallbackToCardBack()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(CatalogAssetPath);

        Assert.That(catalog!.GetSpriteOrCardBack("UNKNOWN"), Is.SameAs(catalog.CardBack));
    }

    [Test]
    public void CardArtService_ShouldResolveWithoutAssetDatabaseAtRuntimeBoundary()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(CatalogAssetPath);
        CardArtService.SetCatalogForTests(catalog);

        var sprite = CardArtService.GetSpriteForDefinition("T001");
        var texture = CardArtService.GetTextureForDefinition("T001");

        Assert.That(sprite, Is.Not.Null);
        Assert.That(texture, Is.SameAs(sprite!.texture));
    }

    [Test]
    public void CardView_Bind_ShouldDisplayArtAndSelectionState()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<CardArtCatalog>(CatalogAssetPath);
        CardArtService.SetCatalogForTests(catalog);

        var root = new GameObject("CardViewTest", typeof(RectTransform), typeof(Image), typeof(Button));
        var artworkObject = new GameObject("Artwork", typeof(RectTransform), typeof(Image));
        artworkObject.transform.SetParent(root.transform, false);
        var definitionObject = new GameObject("Definition", typeof(RectTransform), typeof(Text));
        definitionObject.transform.SetParent(root.transform, false);
        var instanceObject = new GameObject("Instance", typeof(RectTransform), typeof(Text));
        instanceObject.transform.SetParent(root.transform, false);
        var zoneObject = new GameObject("Zone", typeof(RectTransform), typeof(Text));
        zoneObject.transform.SetParent(root.transform, false);

        try
        {
            var cardView = root.AddComponent<CardView>();
            var background = root.GetComponent<Image>();
            var artwork = artworkObject.GetComponent<Image>();
            var definitionText = definitionObject.GetComponent<Text>();
            var instanceText = instanceObject.GetComponent<Text>();
            var zoneText = zoneObject.GetComponent<Text>();
            var button = root.GetComponent<Button>();
            cardView.ConfigureForEditor(
                background,
                artwork,
                definitionText,
                instanceText,
                zoneText,
                button);

            cardView.Bind(
                new ProjectionCardViewModel
                {
                    cardInstanceNumericId = 100001,
                    definitionId = "T001",
                    zoneKey = "hand",
                },
                isFaceUp: true,
                isSelected: true,
                isInteractable: true,
                clickHandler: null);

            Assert.That(artwork.sprite, Is.Not.Null);
            Assert.That(definitionText.text, Is.EqualTo("T001"));
            Assert.That(instanceText.text, Is.EqualTo("#100001"));
            Assert.That(zoneText.text, Is.EqualTo("hand"));
            Assert.That(button.interactable, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static IEnumerable<string> expectedDefinitionIds()
    {
        for (var index = 1; index <= 29; index++)
        {
            yield return $"T{index:000}";
        }

        yield return "T001B";
        yield return "T002B";
        yield return "S001";

        for (var index = 1; index <= 10; index++)
        {
            yield return $"A{index:000}";
        }
    }
}
}
