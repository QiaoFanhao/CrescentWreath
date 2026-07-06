using CrescentWreath.Client.Presentation;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
[ExecuteAlways]
public sealed class GameClientRoot : MonoBehaviour
{
    [SerializeField]
    private CardArtService? cardArtService;

    [SerializeField]
    private ProjectionViewState? projectionViewState;

    [SerializeField]
    private InteractionOverlay? interactionOverlay;

    [SerializeField]
    private BattlefieldLayoutRoot? battlefieldLayoutRoot;

    [SerializeField]
    private GameClientNetworkController? gameClientNetworkController;

    public CardArtService? CardArtService => cardArtService;
    public ProjectionViewState? ProjectionViewState => projectionViewState;
    public InteractionOverlay? InteractionOverlay => interactionOverlay;
    public BattlefieldLayoutRoot? BattlefieldLayoutRoot => battlefieldLayoutRoot;
    public GameClientNetworkController? GameClientNetworkController => gameClientNetworkController;

    private void OnEnable()
    {
        if (string.Equals(
                gameObject.scene.name,
                "GameClient",
                System.StringComparison.Ordinal))
        {
            BattlefieldSceneSkeletonFactory.Ensure(this);
        }
    }

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

    public void ConfigureBattlefieldLayout(BattlefieldLayoutRoot newBattlefieldLayoutRoot)
    {
        battlefieldLayoutRoot = newBattlefieldLayoutRoot;
    }

    public void ConfigureNetworkController(GameClientNetworkController newGameClientNetworkController)
    {
        gameClientNetworkController = newGameClientNetworkController;
    }
}
}
