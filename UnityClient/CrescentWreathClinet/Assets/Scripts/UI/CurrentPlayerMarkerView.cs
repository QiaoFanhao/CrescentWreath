using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class CurrentPlayerMarkerView : MonoBehaviour
{
    private const string ResourcePath = "UI/HP/currentplayer";
    private static Sprite? cachedSprite;

    [SerializeField]
    private SpriteRenderer? spriteRenderer;

    [SerializeField]
    private BoxCollider? boxCollider;

    public void Configure(Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
    {
        ensureVisuals();
        transform.localPosition = localPosition;
        transform.localEulerAngles = localEulerAngles;
        transform.localScale = localScale;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        WorldCardTooltipView.Hide();
        gameObject.SetActive(false);
    }

    private void OnMouseEnter()
    {
        WorldCardTooltipView.Show("当前回合玩家", "轮到这名玩家行动。");
    }

    private void OnMouseExit()
    {
        WorldCardTooltipView.Hide();
    }

    private void OnDisable()
    {
        WorldCardTooltipView.Hide();
    }

    private void ensureVisuals()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.sprite = getSprite();
        spriteRenderer.sortingOrder = 20;
        spriteRenderer.color = Color.white;

        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider>();
        }

        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        if (boxCollider == null)
        {
            return;
        }

        boxCollider.center = Vector3.zero;
        boxCollider.size = new Vector3(0.72f, 0.06f, 0.42f);
    }

    private static Sprite? getSprite()
    {
        if (cachedSprite is not null)
        {
            return cachedSprite;
        }

        cachedSprite = Resources.Load<Sprite>(ResourcePath);
        if (cachedSprite is not null)
        {
            return cachedSprite;
        }

        var texture = Resources.Load<Texture2D>(ResourcePath);
        if (texture is null)
        {
            return null;
        }

        cachedSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        return cachedSprite;
    }
}
}
