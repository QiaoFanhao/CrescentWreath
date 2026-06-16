using TMPro;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldGrayboxPanel : MonoBehaviour
{
    [SerializeField]
    private string panelKey = string.Empty;

    [SerializeField]
    private TMP_Text? titleText;

    public string PanelKey => panelKey;
    public TMP_Text? TitleText => titleText;

    public void ConfigureForEditor(string newPanelKey, TMP_Text newTitleText)
    {
        Configure(newPanelKey, newTitleText);
    }

    public void Configure(string newPanelKey, TMP_Text? newTitleText)
    {
        panelKey = newPanelKey;
        titleText = newTitleText;
    }
}
}
