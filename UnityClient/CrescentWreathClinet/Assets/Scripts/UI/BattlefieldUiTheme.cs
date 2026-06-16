using TMPro;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
[CreateAssetMenu(
    fileName = "BattlefieldUiTheme",
    menuName = "Crescent Wreath/UI/Battlefield UI Theme")]
public sealed class BattlefieldUiTheme : ScriptableObject
{
    [Header("Font")]
    [SerializeField]
    private TMP_FontAsset? primaryFont;

    [SerializeField]
    [TextArea]
    private string fontResourceStatus =
        "TODO: SourceHanSans - temporary TMP default fallback is in use.";

    [Header("Surfaces")]
    [SerializeField]
    private Color graphiteBackground = new(0.035f, 0.045f, 0.055f, 1f);

    [SerializeField]
    private Color deepBluePanel = new(0.055f, 0.11f, 0.16f, 0.96f);

    [SerializeField]
    private Color cyanTeam = new(0.12f, 0.72f, 0.82f, 1f);

    [SerializeField]
    private Color coralTeam = new(0.92f, 0.34f, 0.28f, 1f);

    [SerializeField]
    private Color goldActive = new(0.96f, 0.73f, 0.2f, 1f);

    [SerializeField]
    private Color darkRedAnomaly = new(0.34f, 0.055f, 0.08f, 0.98f);

    [Header("Text")]
    [SerializeField]
    private Color normalText = new(0.94f, 0.97f, 1f, 1f);

    [SerializeField]
    private Color mutedText = new(0.56f, 0.65f, 0.71f, 1f);

    [SerializeField]
    private Color disabledText = new(0.32f, 0.37f, 0.4f, 1f);

    [SerializeField]
    private Color warning = new(1f, 0.65f, 0.16f, 1f);

    [SerializeField]
    private Color error = new(0.94f, 0.18f, 0.2f, 1f);

    public TMP_FontAsset? PrimaryFont => primaryFont;
    public string FontResourceStatus => fontResourceStatus;
    public Color GraphiteBackground => graphiteBackground;
    public Color DeepBluePanel => deepBluePanel;
    public Color CyanTeam => cyanTeam;
    public Color CoralTeam => coralTeam;
    public Color GoldActive => goldActive;
    public Color DarkRedAnomaly => darkRedAnomaly;
    public Color NormalText => normalText;
    public Color MutedText => mutedText;
    public Color DisabledText => disabledText;
    public Color Warning => warning;
    public Color Error => error;

#if UNITY_EDITOR
    public void ConfigureForEditor(TMP_FontAsset? fontAsset, string resourceStatus)
    {
        primaryFont = fontAsset;
        fontResourceStatus = resourceStatus;
    }
#endif
}
}
