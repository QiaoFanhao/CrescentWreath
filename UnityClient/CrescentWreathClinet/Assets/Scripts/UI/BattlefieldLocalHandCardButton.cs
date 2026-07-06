using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldLocalHandCardButton :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [SerializeField]
    private Image? image;

    private long cardInstanceNumericId;
    private Action<long>? clicked;
    private Vector3 restScale = Vector3.one;

    public void Configure(long newCardInstanceNumericId, Action<long> newClicked)
    {
        cardInstanceNumericId = newCardInstanceNumericId;
        clicked = newClicked;
        image ??= GetComponent<Image>();
        restScale = Vector3.one;
        transform.localScale = restScale;
        setHighlighted(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = restScale * 1.08f;
        setHighlighted(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = restScale;
        setHighlighted(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        clicked?.Invoke(cardInstanceNumericId);
    }

    private void setHighlighted(bool highlighted)
    {
        if (image is not null)
        {
            image.color = highlighted ? new Color(1f, 0.92f, 0.45f, 1f) : Color.white;
        }
    }
}
}
