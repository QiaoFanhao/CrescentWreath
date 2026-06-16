using TMPro;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public enum BattlefieldSeatDirection
{
    bottom,
    left,
    top,
    right,
}

public sealed class BattlefieldPlayerSeatView : MonoBehaviour
{
    [SerializeField]
    private BattlefieldSeatDirection direction;

    [SerializeField]
    private RectTransform? characterPortraitPlaceholder;

    [SerializeField]
    private TMP_Text? playerNameText;

    [SerializeField]
    private TMP_Text? hpText;

    [SerializeField]
    private RectTransform? resourceBarPlaceholder;

    [SerializeField]
    private RectTransform? statusContainer;

    [SerializeField]
    private RectTransform? counterContainer;

    [SerializeField]
    private TMP_Text? handCountText;

    [SerializeField]
    private TMP_Text? deckCountText;

    [SerializeField]
    private TMP_Text? discardCountText;

    [SerializeField]
    private RectTransform? publicFieldContainer;

    public BattlefieldSeatDirection Direction => direction;
    public TMP_Text? PlayerNameText => playerNameText;
    public TMP_Text? HpText => hpText;
    public RectTransform? PublicFieldContainer => publicFieldContainer;

#if UNITY_EDITOR
    public void ConfigureForEditor(
        BattlefieldSeatDirection newDirection,
        RectTransform portrait,
        TMP_Text playerName,
        TMP_Text hp,
        RectTransform resourceBar,
        RectTransform statuses,
        RectTransform counters,
        TMP_Text handCount,
        TMP_Text deckCount,
        TMP_Text discardCount,
        RectTransform publicField)
    {
        direction = newDirection;
        characterPortraitPlaceholder = portrait;
        playerNameText = playerName;
        hpText = hp;
        resourceBarPlaceholder = resourceBar;
        statusContainer = statuses;
        counterContainer = counters;
        handCountText = handCount;
        deckCountText = deckCount;
        discardCountText = discardCount;
        publicFieldContainer = publicField;
    }
#endif
}
}
