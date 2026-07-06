using TMPro;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class CardDetailPanel : MonoBehaviour
{
    [SerializeField]
    private TMP_Text? detailText;

    public void Configure(TMP_Text newDetailText)
    {
        detailText = newDetailText;
    }

    public void Show(WorldCardViewModel card)
    {
        if (detailText is null)
        {
            return;
        }

        detailText.text =
            $"{card.definitionId} #{card.cardInstanceNumericId}\n" +
            $"owner={card.ownerPlayerNumericId} zone={card.zoneKey}\n" +
            $"faceUp={card.isFaceUp} defensePlaced={card.isDefensePlacedOnField}\n" +
            $"summonCost={formatNullableInt(card.baseSummonSigilCost)} -> {formatNullableInt(card.effectiveSummonSigilCost)}\n" +
            $"summonable={card.isSummonableByViewer} reason={card.summonUnavailableReasonKey}\n" +
            $"mode={card.displayMode}";
    }

    public void ShowMessage(string message)
    {
        if (detailText is not null)
        {
            detailText.text = message;
        }
    }

    private static string formatNullableInt(int? value)
    {
        return value.HasValue ? value.Value.ToString() : "(none)";
    }
}
}
