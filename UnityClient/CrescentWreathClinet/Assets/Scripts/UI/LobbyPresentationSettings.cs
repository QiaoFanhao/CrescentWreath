using UnityEngine;

namespace CrescentWreath.Client.UI
{
[CreateAssetMenu(
    fileName = "LobbyPresentationSettings",
    menuName = "Crescent Wreath/UI/Lobby Presentation Settings")]
public sealed class LobbyPresentationSettings : ScriptableObject
{
    public bool showDevelopmentOverlay;
    [Range(0.08f, 0.3f)] public float pageTransitionDuration = 0.16f;
    [Range(12f, 48f)] public float pageSlideDistance = 28f;
    [Range(0.4f, 1f)] public float battlefieldTransitionDuration = 0.65f;

    public static LobbyPresentationSettings CreateRuntimeDefault()
    {
        return CreateInstance<LobbyPresentationSettings>();
    }
}
}
