using UnityEngine;

namespace CrescentWreath.Client.UI
{
public static class HpCardSpriteService
{
    private const string ResourcePathPrefix = "UI/HP/hp_card_HP";
    private static readonly Sprite?[] spritesByHp = new Sprite?[5];

    public static Sprite? GetSpriteForHp(int? currentHp)
    {
        if (!currentHp.HasValue)
        {
            return null;
        }

        var clampedHp = Mathf.Clamp(currentHp.Value, 1, 4);
        if (spritesByHp[clampedHp] is not null)
        {
            return spritesByHp[clampedHp];
        }

        spritesByHp[clampedHp] = Resources.Load<Sprite>(ResourcePathPrefix + clampedHp.ToString());
        if (spritesByHp[clampedHp] is null)
        {
            var texture = Resources.Load<Texture2D>(ResourcePathPrefix + clampedHp.ToString());
            if (texture is not null)
            {
                spritesByHp[clampedHp] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
        }
        return spritesByHp[clampedHp];
    }
}
}
