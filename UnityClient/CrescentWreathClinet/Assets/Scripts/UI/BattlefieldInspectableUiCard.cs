using System;
using CrescentWreath.Client.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldInspectableUiCard :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [SerializeField]
    private Image? image;

    private CardInspectViewModel? inspectModel;
    private Action? leftClicked;
    private Vector3 restScale = Vector3.one;
    private Color restColor = Color.white;

    public void Configure(CardInspectViewModel newInspectModel, Action? newLeftClicked)
    {
        inspectModel = newInspectModel;
        leftClicked = newLeftClicked;
        image ??= GetComponent<Image>();
        restColor = image is null ? Color.white : image.color;
        restScale = Vector3.one;
        transform.localScale = restScale;
        setHighlighted(false);
    }

    public bool TryBuildInspectViewModel(out CardInspectViewModel model)
    {
        if (inspectModel is null)
        {
            model = new CardInspectViewModel();
            return false;
        }

        model = inspectModel;
        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = restScale * 1.04f;
        setHighlighted(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = restScale;
        setHighlighted(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        leftClicked?.Invoke();
    }

    private void setHighlighted(bool highlighted)
    {
        if (image is not null)
        {
            image.color = highlighted ? new Color(1f, 0.92f, 0.5f, 1f) : restColor;
        }
    }
}
}
