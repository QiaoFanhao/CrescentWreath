using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class OverlayCollapseToggle : MonoBehaviour
{
    private const string ToggleObjectName = "OverlayCollapseToggle";

    private readonly Dictionary<Transform, bool> childActiveStateBeforeCollapse = new();

    private BattlefieldUiTheme? theme;
    private Button? toggleButton;
    private TMP_Text? toggleLabel;
    private CanvasGroup? canvasGroup;
    private Image? rootImage;
    private bool isVisible;
    private bool isCollapsed;

    public bool IsCollapsed => isCollapsed;

    public void Configure(BattlefieldUiTheme? newTheme)
    {
        theme = newTheme;
        ensureReferences();
        applyState();
    }

    public void SetVisible(bool visible, bool resetCollapsed = false)
    {
        ensureReferences();
        isVisible = visible;
        if (!visible || resetCollapsed)
        {
            isCollapsed = false;
            childActiveStateBeforeCollapse.Clear();
        }

        applyState();
    }

    public void RefreshIfCollapsed()
    {
        if (isVisible && isCollapsed)
        {
            applyState();
        }
    }

    private void toggleCollapsed()
    {
        ensureReferences();
        if (!isVisible)
        {
            return;
        }

        if (!isCollapsed)
        {
            captureChildActiveStates();
        }

        isCollapsed = !isCollapsed;
        applyState();
    }

    private void ensureReferences()
    {
        canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
        rootImage = rootImage != null ? rootImage : GetComponent<Image>();
        ensureToggleButton();
    }

    private void ensureToggleButton()
    {
        if (toggleButton != null && toggleLabel != null)
        {
            return;
        }

        var toggleTransform = transform.Find(ToggleObjectName);
        GameObject toggleObject;
        if (toggleTransform == null)
        {
            toggleObject = new GameObject(ToggleObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            toggleObject.transform.SetParent(transform, false);
        }
        else
        {
            toggleObject = toggleTransform.gameObject;
        }

        toggleObject.transform.SetAsLastSibling();
        var rect = toggleObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(92f, 30f);
        rect.anchoredPosition = new Vector2(-10f, -10f);

        var image = toggleObject.GetComponent<Image>();
        image.color = new Color(0.10f, 0.34f, 0.42f, 0.96f);
        image.raycastTarget = true;

        toggleButton = toggleObject.GetComponent<Button>();
        toggleButton.onClick.RemoveListener(toggleCollapsed);
        toggleButton.onClick.AddListener(toggleCollapsed);

        toggleLabel = toggleObject.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (toggleLabel == null)
        {
            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(toggleObject.transform, false);
            toggleLabel = labelObject.GetComponent<TMP_Text>();
            var labelRect = toggleLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4f, 1f);
            labelRect.offsetMax = new Vector2(-4f, -1f);
        }

        toggleLabel.fontSize = 12f;
        toggleLabel.alignment = TextAlignmentOptions.Center;
        toggleLabel.color = theme?.NormalText ?? Color.white;
        toggleLabel.raycastTarget = false;
        if (theme?.PrimaryFont != null)
        {
            toggleLabel.font = theme.PrimaryFont;
        }
    }

    private void applyState()
    {
        if (canvasGroup == null)
        {
            return;
        }

        if (!isVisible)
        {
            setDirectContentChildrenActive(false);
            setToggleActive(false);
            setRootImageState(false);
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            return;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        setToggleActive(true);

        if (isCollapsed)
        {
            setDirectContentChildrenActive(false);
            setRootImageState(false);
            setToggleText("显示弹窗");
            return;
        }

        restoreChildActiveStates();
        setRootImageState(true);
        setToggleText("隐藏弹窗");
    }

    private void captureChildActiveStates()
    {
        childActiveStateBeforeCollapse.Clear();
        foreach (Transform child in transform)
        {
            if (isToggleTransform(child))
            {
                continue;
            }

            childActiveStateBeforeCollapse[child] = child.gameObject.activeSelf;
        }
    }

    private void restoreChildActiveStates()
    {
        foreach (Transform child in transform)
        {
            if (isToggleTransform(child))
            {
                continue;
            }

            child.gameObject.SetActive(
                childActiveStateBeforeCollapse.TryGetValue(child, out var wasActive)
                    ? wasActive
                    : true);
        }

        childActiveStateBeforeCollapse.Clear();
    }

    private void setDirectContentChildrenActive(bool active)
    {
        foreach (Transform child in transform)
        {
            if (!isToggleTransform(child))
            {
                child.gameObject.SetActive(active);
            }
        }
    }

    private void setToggleActive(bool active)
    {
        if (toggleButton != null)
        {
            toggleButton.gameObject.SetActive(active);
        }
    }

    private void setRootImageState(bool active)
    {
        if (rootImage == null)
        {
            return;
        }

        rootImage.enabled = active;
        rootImage.raycastTarget = active;
    }

    private void setToggleText(string value)
    {
        if (toggleLabel != null)
        {
            toggleLabel.text = value;
        }
    }

    private bool isToggleTransform(Transform child)
    {
        return toggleButton != null && child == toggleButton.transform;
    }
}
}
