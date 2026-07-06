using UnityEngine;
using UnityEngine.InputSystem;

namespace CrescentWreath.Client.UI
{
public sealed class BattlefieldDebugCanvasToggle : MonoBehaviour
{
    [SerializeField]
    private BattlefieldLayoutRoot? layoutRoot;

    public void Configure(BattlefieldLayoutRoot newLayoutRoot)
    {
        layoutRoot = newLayoutRoot;
    }

    private void Awake()
    {
        layoutRoot ??= GetComponent<BattlefieldLayoutRoot>();
    }

    private void Update()
    {
        if (Keyboard.current?.f10Key.wasPressedThisFrame != true)
        {
            return;
        }

        layoutRoot ??= GetComponent<BattlefieldLayoutRoot>();
        if (layoutRoot is not null)
        {
            layoutRoot.SetDebugVisible(!layoutRoot.IsDebugVisible);
        }
    }
}
}
