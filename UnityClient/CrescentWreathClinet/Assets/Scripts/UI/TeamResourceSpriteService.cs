using UnityEngine;

namespace CrescentWreath.Client.UI
{
public static class TeamResourceSpriteService
{
    private const string ResourcePathPrefix = "UI/Team/";
    private static readonly System.Collections.Generic.Dictionary<string, Sprite?> cache = new();

    public static Sprite? GetBaseSprite(bool isTeamA)
    {
        return load(isTeamA ? "team_a_base" : "team_b_base");
    }

    public static Sprite? GetKillSprite(bool isTeamA, bool isOn)
    {
        return load(isTeamA
            ? isOn ? "team_a_kill_on" : "team_a_kill_off"
            : isOn ? "team_b_kill_on" : "team_b_kill_off");
    }

    public static Sprite? GetLeylineSprite(bool isTeamA, bool isOn)
    {
        return load(isTeamA
            ? isOn ? "team_a_lingmai_on" : "team_a_lingmai_off"
            : isOn ? "team_b_lingmai_on" : "team_b_lingmai_off");
    }

    public static Sprite? GetTeamIconSprite(long? teamNumericId)
    {
        if (!teamNumericId.HasValue)
        {
            return null;
        }

        return load(teamNumericId.Value == 1 ? "team_a_icon" : "team_b_icon");
    }

    private static Sprite? load(string resourceName)
    {
        if (cache.TryGetValue(resourceName, out var cached))
        {
            return cached;
        }

        var sprite = Resources.Load<Sprite>(ResourcePathPrefix + resourceName);
        if (sprite is null)
        {
            var texture = Resources.Load<Texture2D>(ResourcePathPrefix + resourceName);
            if (texture is not null)
            {
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
        }

        cache[resourceName] = sprite;
        return sprite;
    }
}
}
