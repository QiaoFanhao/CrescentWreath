using TMPro;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldPublicZoneView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text? titleText;

    [SerializeField]
    private RectTransform? publicTreasureDeckSlot;

    [SerializeField]
    private RectTransform? summonZoneContainer;

    [SerializeField]
    private RectTransform? sakuraCakeDeckSlot;

    [SerializeField]
    private RectTransform? sakuraCakeZoneContainer;

    public RectTransform? SummonZoneContainer => summonZoneContainer;

#if UNITY_EDITOR
    public void ConfigureForEditor(
        TMP_Text title,
        RectTransform treasureDeck,
        RectTransform summonZone,
        RectTransform sakuraDeck,
        RectTransform sakuraZone)
    {
        titleText = title;
        publicTreasureDeckSlot = treasureDeck;
        summonZoneContainer = summonZone;
        sakuraCakeDeckSlot = sakuraDeck;
        sakuraCakeZoneContainer = sakuraZone;
    }
#endif
}
}
