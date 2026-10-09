using System;
using CrescentWreath.Client.Net;
using CrescentWreath.Client.Presentation;
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
    private ProjectionCardViewModel? cardModel;
    private Action<long>? clicked;
    private Vector3 restScale = Vector3.one;

    public void Configure(ProjectionCardViewModel newCardModel, Action<long> newClicked)
    {
        cardModel = newCardModel;
        cardInstanceNumericId = newCardModel.cardInstanceNumericId;
        clicked = newClicked;
        image ??= GetComponent<Image>();
        restScale = Vector3.one;
        transform.localScale = restScale;
        setHighlighted(false);
    }

    public bool TryBuildInspectViewModel(out CardInspectViewModel inspectModel)
    {
        if (cardModel is null)
        {
            inspectModel = new CardInspectViewModel();
            return false;
        }

        var displayEntry = CardDisplayCatalogService.TryGetEntry(cardModel.definitionId);
        inspectModel = new CardInspectViewModel
        {
            cardInstanceNumericId = cardModel.cardInstanceNumericId,
            definitionId = cardModel.definitionId,
            displayName = displayEntry?.name ?? CardNameLocalizer.Localize(cardModel.definitionId),
            attributeText = CardDisplayCatalogService.BuildAttributeText(cardModel.definitionId),
            rulesText = CardDisplayCatalogService.BuildRulesText(cardModel.definitionId),
            ownerPlayerNumericId = cardModel.ownerPlayerNumericId,
            zoneKey = cardModel.zoneKey,
            isFaceUp = true,
            isDefensePlacedOnField = cardModel.isDefensePlacedOnField,
            baseSummonSigilCost = cardModel.baseSummonSigilCost,
            effectiveSummonSigilCost = cardModel.effectiveSummonSigilCost,
            isSummonableByViewer = cardModel.isSummonableByViewer,
            summonUnavailableReasonKey = cardModel.summonUnavailableReasonKey,
            artworkSprite = CardArtService.GetSpriteForDefinition(cardModel.definitionId),
        };
        return true;
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
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

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
