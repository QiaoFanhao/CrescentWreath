using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class LobbyInputCaretView : MonoBehaviour
{
    private TMP_InputField? inputField;
    private TMP_Text? textComponent;
    private RectTransform? caretRect;
    private Image? caretImage;

    public void Configure(TMP_InputField newInputField, TMP_Text newTextComponent)
    {
        inputField = newInputField;
        textComponent = newTextComponent;
        ensureCaret();
    }

    private void LateUpdate()
    {
        if (inputField is null ||
            textComponent is null ||
            caretRect is null ||
            caretImage is null)
        {
            return;
        }

        var shouldShow = inputField.isFocused && inputField.interactable && !inputField.readOnly;
        caretImage.enabled = shouldShow && Mathf.Repeat(Time.unscaledTime * 2.6f, 1f) < 0.72f;
        if (!shouldShow)
        {
            return;
        }

        textComponent.ForceMeshUpdate();
        var caretX = resolveCaretX();
        caretRect.localPosition = new Vector3(caretX, 0f, 0f);
        caretRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(3f, inputField.caretWidth));
        caretRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(22f, textComponent.fontSize + 10f));
    }

    private void ensureCaret()
    {
        if (textComponent is null)
        {
            return;
        }

        if (caretRect is not null && caretImage is not null)
        {
            return;
        }

        var caretObject = new GameObject("VisibleCaret", typeof(RectTransform), typeof(Image));
        caretObject.transform.SetParent(textComponent.transform, false);
        caretRect = caretObject.GetComponent<RectTransform>();
        caretRect.anchorMin = new Vector2(0f, 0.5f);
        caretRect.anchorMax = new Vector2(0f, 0.5f);
        caretRect.pivot = new Vector2(0.5f, 0.5f);
        caretImage = caretObject.GetComponent<Image>();
        caretImage.color = new Color(0.42f, 0.92f, 1f, 1f);
        caretImage.raycastTarget = false;
        caretImage.enabled = false;
    }

    private float resolveCaretX()
    {
        if (inputField is null || textComponent is null)
        {
            return 0f;
        }

        var textInfo = textComponent.textInfo;
        var characterCount = textInfo.characterCount;
        var caretPosition = Mathf.Clamp(inputField.caretPosition, 0, inputField.text?.Length ?? 0);
        if (characterCount <= 0 || caretPosition <= 0)
        {
            return textComponent.rectTransform.rect.xMin + 2f;
        }

        var characterIndex = Mathf.Min(caretPosition - 1, characterCount - 1);
        var characterInfo = textInfo.characterInfo[characterIndex];
        return characterInfo.topRight.x + 1f;
    }
}
}
