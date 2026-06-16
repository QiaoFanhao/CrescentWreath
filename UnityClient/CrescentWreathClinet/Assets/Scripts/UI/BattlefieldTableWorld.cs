using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldTableWorld : MonoBehaviour
{
    [SerializeField]
    private Camera? mainCamera;

    [SerializeField]
    private Light? directionalLight;

    [SerializeField]
    private Transform? tableSurface;

    [SerializeField]
    private Transform? publicArea;

    [SerializeField]
    private Transform? playerFieldAreas;

    [SerializeField]
    private Transform? currentPlayerMirrorArea;

    public Camera? MainCamera => mainCamera;
    public Light? DirectionalLight => directionalLight;
    public Transform? TableSurface => tableSurface;
    public Transform? PublicArea => publicArea;
    public Transform? PlayerFieldAreas => playerFieldAreas;
    public Transform? CurrentPlayerMirrorArea => currentPlayerMirrorArea;

    public void Configure(
        Camera newMainCamera,
        Light newDirectionalLight,
        Transform newTableSurface,
        Transform newPublicArea,
        Transform newPlayerFieldAreas,
        Transform newCurrentPlayerMirrorArea)
    {
        mainCamera = newMainCamera;
        directionalLight = newDirectionalLight;
        tableSurface = newTableSurface;
        publicArea = newPublicArea;
        playerFieldAreas = newPlayerFieldAreas;
        currentPlayerMirrorArea = newCurrentPlayerMirrorArea;
    }
}
}
