using UnityEngine;
using UnityEngine.EventSystems;

namespace CrescentWreath.Client.UI
{
[RequireComponent(typeof(WorldCardView))]
public sealed class WorldCardPointerController :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    private WorldCardView? view;

    private void Awake()
    {
        view = GetComponent<WorldCardView>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        view ??= GetComponent<WorldCardView>();
        view.SetHighlighted(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        view ??= GetComponent<WorldCardView>();
        view.SetHighlighted(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        view ??= GetComponent<WorldCardView>();
        if (eventData.clickCount >= 2)
        {
            view.NotifyDoubleClicked();
            return;
        }

        view.NotifyClicked();
    }
}
}
