using System;
using System.Collections.Generic;
using CrescentWreath.Client.Net;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class PublicZoneView : MonoBehaviour
{
    [SerializeField]
    private RectTransform? contentRoot;

    [SerializeField]
    private CardView? cardViewPrefab;

    private readonly List<CardView> activeViews = new();

    public void Bind(
        IReadOnlyList<ProjectionCardViewModel> cards,
        long? selectedCardId,
        bool isInteractable,
        Action<ProjectionCardViewModel>? onCardClicked)
    {
        clearViews();
        if (contentRoot is null || cardViewPrefab is null)
        {
            return;
        }

        foreach (var card in cards)
        {
            var cardView = Instantiate(cardViewPrefab, contentRoot);
            cardView.Bind(
                card,
                isFaceUp: true,
                isSelected: selectedCardId == card.cardInstanceNumericId,
                isInteractable,
                onCardClicked);
            activeViews.Add(cardView);
        }
    }

    private void clearViews()
    {
        foreach (var view in activeViews)
        {
            if (view is not null)
            {
                Destroy(view.gameObject);
            }
        }

        activeViews.Clear();
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(RectTransform newContentRoot, CardView newCardViewPrefab)
    {
        contentRoot = newContentRoot;
        cardViewPrefab = newCardViewPrefab;
    }
#endif
}
}
