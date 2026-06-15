using CrescentWreath.Client.Presentation;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class GameClientRoot : MonoBehaviour
{
    [SerializeField]
    private CardArtService? cardArtService;

    [SerializeField]
    private ProjectionViewState? projectionViewState;

    [SerializeField]
    private InteractionOverlay? interactionOverlay;

    public CardArtService? CardArtService => cardArtService;
    public ProjectionViewState? ProjectionViewState => projectionViewState;
    public InteractionOverlay? InteractionOverlay => interactionOverlay;

#if UNITY_EDITOR
    public void ConfigureForEditor(
        CardArtService newCardArtService,
        ProjectionViewState newProjectionViewState,
        InteractionOverlay newInteractionOverlay)
    {
        Configure(newCardArtService, newProjectionViewState, newInteractionOverlay);
    }
#endif

    public void Configure(
        CardArtService newCardArtService,
        ProjectionViewState newProjectionViewState,
        InteractionOverlay newInteractionOverlay)
    {
        cardArtService = newCardArtService;
        projectionViewState = newProjectionViewState;
        interactionOverlay = newInteractionOverlay;
    }
}
}
