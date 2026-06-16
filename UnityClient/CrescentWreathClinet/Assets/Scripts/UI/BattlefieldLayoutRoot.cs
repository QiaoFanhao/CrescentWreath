using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldLayoutRoot : MonoBehaviour
{
    [SerializeField]
    private BattlefieldUiTheme? theme;

    [SerializeField]
    private BattlefieldTableWorld? tableWorld;

    [SerializeField]
    private Canvas? screenHudCanvas;

    [SerializeField]
    private Canvas? overlayCanvas;

    [SerializeField]
    private Canvas? debugCanvas;

    [SerializeField]
    private RectTransform? localHandArea;

    [SerializeField]
    private RectTransform? interactionDimmer;

    [SerializeField]
    private GameObject? flatLayoutDraft;

    public BattlefieldUiTheme? Theme => theme;
    public BattlefieldTableWorld? TableWorld => tableWorld;
    public Canvas? ScreenHudCanvas => screenHudCanvas;
    public Canvas? OverlayCanvas => overlayCanvas;
    public Canvas? DebugCanvas => debugCanvas;
    public RectTransform? LocalHandArea => localHandArea;
    public RectTransform? InteractionDimmer => interactionDimmer;
    public GameObject? FlatLayoutDraft => flatLayoutDraft;
    public bool IsDebugVisible => debugCanvas is not null && debugCanvas.gameObject.activeSelf;

    public void SetDebugVisible(bool isVisible)
    {
        if (debugCanvas is not null)
        {
            debugCanvas.gameObject.SetActive(isVisible);
        }
    }

    public void SetOverlayPreviewVisible(bool isVisible)
    {
        if (interactionDimmer is not null)
        {
            interactionDimmer.gameObject.SetActive(isVisible);
        }
    }

    public void Configure(
        BattlefieldUiTheme newTheme,
        BattlefieldTableWorld newTableWorld,
        Canvas newScreenHudCanvas,
        Canvas newOverlayCanvas,
        Canvas newDebugCanvas,
        RectTransform newLocalHandArea,
        RectTransform newInteractionDimmer,
        GameObject newFlatLayoutDraft)
    {
        theme = newTheme;
        tableWorld = newTableWorld;
        screenHudCanvas = newScreenHudCanvas;
        overlayCanvas = newOverlayCanvas;
        debugCanvas = newDebugCanvas;
        localHandArea = newLocalHandArea;
        interactionDimmer = newInteractionDimmer;
        flatLayoutDraft = newFlatLayoutDraft;
    }
}
}
