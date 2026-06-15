using CrescentWreath.Client.Net;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class InteractionOverlay : MonoBehaviour
{
    [SerializeField]
    private CanvasGroup? canvasGroup;

    [SerializeField]
    private Text? titleText;

    [SerializeField]
    private Text? detailText;

    public void Bind(ProjectionInteractionViewModel interaction)
    {
        var isVisible = interaction.hasResponseWindow || interaction.hasInputContext;
        if (canvasGroup is not null)
        {
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }

        if (titleText is not null)
        {
            titleText.text = interaction.hasResponseWindow ? "响应窗口" : "输入选择";
        }

        if (detailText is not null)
        {
            detailText.text = interaction.hasResponseWindow
                ? $"ResponseWindow #{interaction.responseWindowNumericId}"
                : $"InputContext #{interaction.inputContextNumericId}";
        }
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(CanvasGroup newCanvasGroup, Text newTitleText, Text newDetailText)
    {
        Configure(newCanvasGroup, newTitleText, newDetailText);
    }
#endif

    public void Configure(CanvasGroup newCanvasGroup, Text newTitleText, Text newDetailText)
    {
        canvasGroup = newCanvasGroup;
        titleText = newTitleText;
        detailText = newDetailText;
    }
}
}
