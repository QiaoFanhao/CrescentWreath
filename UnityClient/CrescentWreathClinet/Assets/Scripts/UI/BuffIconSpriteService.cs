using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public static class BuffIconSpriteService
{
    private const string ResourcePrefix = "UI/Buff/";
    private static readonly Dictionary<string, string> ResourceNameByDefinitionId =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["buff:base01"] = "base01",
            ["buff:base02"] = "base02",
            ["buff:base03"] = "base03",
            ["buff:C004_01"] = "C004_01",
            ["buff:C008_01"] = "C008_01",
            ["buff:C008_02"] = "C008_02",
            ["buff:C011_01"] = "C011_01",
        };

    private static readonly Dictionary<string, (string title, string description)> TooltipByDefinitionId =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["buff:base01"] = ("魅惑", "魅惑：受到伤害时会优先消耗，且不能进行防御。"),
            ["buff:base02"] = ("沉默", "沉默：不能使用响应技。"),
            ["buff:base03"] = ("穿透", "穿透：本次伤害即使被完全防御也会造成1点伤害。"),
            ["buff:C004_01"] = ("魔理沙增幅", "你的下一次咒术伤害+2。"),
            ["buff:C008_01"] = ("体术增幅", "你的下一次体术伤害+1。"),
            ["buff:C008_02"] = ("咒术增幅", "你的下一次咒术伤害+1。"),
            ["buff:C011_01"] = ("白姬变身", "本回合你的所有伤害+1。"),
        };

    private static readonly Dictionary<string, Sprite?> SpriteCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> WarnedMissingSprites = new(StringComparer.OrdinalIgnoreCase);

    public static string NormalizeStatusDefinitionId(string statusKey)
    {
        return statusKey?.Trim().ToLowerInvariant() switch
        {
            "seal" or "status:seal" => "Seal",
            "shackle" or "status:shackle" => "Shackle",
            "barrier" or "status:barrier" => "Barrier",
            "charm" or "status:charm" => "buff:base01",
            "silence" or "status:silence" => "buff:base02",
            "penetrate" or "status:penetrate" => "buff:base03",
            "physicalorspelldamageboostnext" or "status:physicalorspelldamageboostnext" => "buff:C004_01",
            "physicaldamageboostnext" or "status:physicaldamageboostnext" => "buff:C008_01",
            "spelldamageboostnext" or "status:spelldamageboostnext" => "buff:C008_02",
            "c011alldamageboostthisturn" or "status:c011alldamageboostthisturn" => "buff:C011_01",
            _ => string.Empty,
        };
    }

    public static bool TryGetSprite(string definitionId, out Sprite? sprite)
    {
        sprite = null;
        if (string.IsNullOrWhiteSpace(definitionId) ||
            !ResourceNameByDefinitionId.TryGetValue(definitionId.Trim(), out var resourceName))
        {
            return false;
        }

        if (SpriteCache.TryGetValue(definitionId, out sprite))
        {
            return sprite is not null;
        }

        var resourcePath = ResourcePrefix + resourceName;
        sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite is null)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture is not null)
            {
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
        }

        SpriteCache[definitionId] = sprite;
        if (sprite is null && WarnedMissingSprites.Add(definitionId))
        {
            Debug.LogWarning($"[BuffIconSpriteService] Missing buff icon resource: {resourcePath}");
        }

        return sprite is not null;
    }

    public static bool TryGetTooltip(string definitionId, out string title, out string description)
    {
        title = string.Empty;
        description = string.Empty;
        if (string.IsNullOrWhiteSpace(definitionId) ||
            !TooltipByDefinitionId.TryGetValue(definitionId.Trim(), out var tooltip))
        {
            return false;
        }

        title = tooltip.title;
        description = tooltip.description;
        return true;
    }
}
}
