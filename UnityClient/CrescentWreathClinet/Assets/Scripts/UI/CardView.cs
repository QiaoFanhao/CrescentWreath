using System;
using CrescentWreath.Client.Net;
using CrescentWreath.Client.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class CardView : MonoBehaviour
{
    [SerializeField]
    private Image? backgroundImage;

    [SerializeField]
    private Image? artworkImage;

    [SerializeField]
    private Text? definitionIdText;

    [SerializeField]
    private Text? instanceIdText;

    [SerializeField]
    private Text? zoneKeyText;

    [SerializeField]
    private Button? button;

    [SerializeField]
    private Color normalColor = new(0.12f, 0.13f, 0.15f, 1f);

    [SerializeField]
    private Color selectedColor = new(0.92f, 0.68f, 0.12f, 1f);

    [SerializeField]
    private Color disabledColor = new(0.25f, 0.25f, 0.25f, 0.85f);

    private ProjectionCardViewModel? boundCard;
    private Action<ProjectionCardViewModel>? onClicked;

    public void Bind(
        ProjectionCardViewModel card,
        bool isFaceUp,
        bool isSelected,
        bool isInteractable,
        Action<ProjectionCardViewModel>? clickHandler)
    {
        boundCard = card;
        onClicked = clickHandler;

        if (artworkImage is not null)
        {
            var catalog = CardArtService.Catalog;
            artworkImage.sprite = isFaceUp
                ? CardArtService.GetSpriteForDefinition(card.definitionId)
                : catalog?.CardBack;
            artworkImage.enabled = artworkImage.sprite is not null;
            artworkImage.preserveAspect = true;
        }

        if (definitionIdText is not null)
        {
            definitionIdText.text = isFaceUp ? card.definitionId : "CARD BACK";
        }

        if (instanceIdText is not null)
        {
            instanceIdText.text = $"#{card.cardInstanceNumericId}";
        }

        if (zoneKeyText is not null)
        {
            zoneKeyText.text = card.zoneKey;
        }

        if (backgroundImage is not null)
        {
            backgroundImage.color = !isInteractable
                ? disabledColor
                : isSelected
                    ? selectedColor
                    : normalColor;
        }

        if (button is not null)
        {
            button.interactable = isInteractable;
        }
    }

    private void Awake()
    {
        button?.onClick.AddListener(handleClick);
    }

    private void OnDestroy()
    {
        button?.onClick.RemoveListener(handleClick);
    }

    private void handleClick()
    {
        if (boundCard is not null)
        {
            onClicked?.Invoke(boundCard);
        }
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        Image newBackgroundImage,
        Image newArtworkImage,
        Text newDefinitionIdText,
        Text newInstanceIdText,
        Text newZoneKeyText,
        Button newButton)
    {
        backgroundImage = newBackgroundImage;
        artworkImage = newArtworkImage;
        definitionIdText = newDefinitionIdText;
        instanceIdText = newInstanceIdText;
        zoneKeyText = newZoneKeyText;
        button = newButton;
    }
#endif
}
}
