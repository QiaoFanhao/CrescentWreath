using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrescentWreath.Client.Presentation
{
public sealed class CardArtService : MonoBehaviour
{
    public const string CatalogResourcesPath = "CardArt/CardArtCatalog";

    private static readonly HashSet<string> warnedDefinitionIds = new(StringComparer.Ordinal);
    private static CardArtCatalog? sharedCatalog;

    [SerializeField]
    private CardArtCatalog? catalog;

    public static CardArtCatalog? Catalog
    {
        get
        {
            if (sharedCatalog is null)
            {
                sharedCatalog = Resources.Load<CardArtCatalog>(CatalogResourcesPath);
            }

            return sharedCatalog;
        }
    }

    private void Awake()
    {
        if (catalog is not null)
        {
            sharedCatalog = catalog;
        }
        else
        {
            catalog = Catalog;
        }
    }

    public static Sprite? GetSpriteForDefinition(string definitionId)
    {
        var loadedCatalog = Catalog;
        if (loadedCatalog is null)
        {
            warnOnce(
                "<catalog>",
                $"Card art catalog was not found at Resources/{CatalogResourcesPath}.asset.");
            return null;
        }

        if (loadedCatalog.TryGetSprite(definitionId, out var sprite) && sprite is not null)
        {
            return sprite;
        }

        warnOnce(definitionId, $"Card art is missing for definitionId '{definitionId}'. Using CardBack.");
        return loadedCatalog.CardBack;
    }

    public static Texture2D? GetTextureForDefinition(string definitionId)
    {
        return GetSpriteForDefinition(definitionId)?.texture;
    }

#if UNITY_EDITOR
    public void SetCatalogForEditor(CardArtCatalog newCatalog)
    {
        catalog = newCatalog;
        sharedCatalog = newCatalog;
    }
#endif

    public static void ClearCacheForTests()
    {
        sharedCatalog = null;
        warnedDefinitionIds.Clear();
    }

    public static void SetCatalogForTests(CardArtCatalog? testCatalog)
    {
        sharedCatalog = testCatalog;
        warnedDefinitionIds.Clear();
    }

    private static void warnOnce(string definitionId, string message)
    {
        var warningKey = string.IsNullOrWhiteSpace(definitionId)
            ? "<empty>"
            : definitionId.Trim().ToUpperInvariant();
        if (warnedDefinitionIds.Add(warningKey))
        {
            Debug.LogWarning($"[CardArtService] {message}");
        }
    }
}
}
