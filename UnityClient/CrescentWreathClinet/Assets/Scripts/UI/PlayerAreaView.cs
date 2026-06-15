using CrescentWreath.Client.Net;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class PlayerAreaView : MonoBehaviour
{
    [SerializeField]
    private Text? summaryText;

    public void Bind(ProjectionPlayerSummaryViewModel player)
    {
        if (summaryText is null)
        {
            return;
        }

        summaryText.text =
            $"P{player.playerNumericId}  HP {player.activeCharacterCurrentHp}/{player.activeCharacterMaxHp}\n" +
            $"手牌 {player.handCount}  阵地 {player.fieldCount}  弃牌 {player.discardCount}";
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(Text newSummaryText)
    {
        summaryText = newSummaryText;
    }
#endif
}
}
