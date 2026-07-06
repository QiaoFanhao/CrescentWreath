using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class GameClientConnectionPanel : MonoBehaviour
{
    [SerializeField]
    private GameClientNetworkController? networkController;

    [SerializeField]
    private TMP_InputField? wsUrlInput;

    [SerializeField]
    private TMP_InputField? viewerIdInput;

    [SerializeField]
    private Button? connectButton;

    [SerializeField]
    private Button? disconnectButton;

    [SerializeField]
    private TMP_Text? statusText;

    [SerializeField]
    private TMP_Text? errorText;

    [SerializeField]
    private CanvasGroup? canvasGroup;

    private bool isPanelVisible = true;
    private bool wasConnected;

    public void Configure(
        GameClientNetworkController newNetworkController,
        TMP_InputField newWsUrlInput,
        TMP_InputField newViewerIdInput,
        Button newConnectButton,
        Button newDisconnectButton,
        TMP_Text newStatusText,
        TMP_Text newErrorText)
    {
        if (networkController is not null)
        {
            networkController.StateChanged -= refresh;
        }

        networkController = newNetworkController;
        wsUrlInput = newWsUrlInput;
        viewerIdInput = newViewerIdInput;
        connectButton = newConnectButton;
        disconnectButton = newDisconnectButton;
        statusText = newStatusText;
        errorText = newErrorText;
        canvasGroup = ensureCanvasGroup();

        networkController.StateChanged -= refresh;
        networkController.StateChanged += refresh;
        if (string.IsNullOrWhiteSpace(wsUrlInput.text))
        {
            wsUrlInput.text = networkController.WsUrl;
        }

        if (string.IsNullOrWhiteSpace(viewerIdInput.text))
        {
            viewerIdInput.text = networkController.ViewerPlayerNumericId.ToString();
        }

        bindButtons();
        refresh();
    }

    private void OnEnable()
    {
        canvasGroup = ensureCanvasGroup();
        bindButtons();
        if (networkController is not null)
        {
            networkController.StateChanged -= refresh;
            networkController.StateChanged += refresh;
        }

        refresh();
    }

    private void OnDisable()
    {
        if (connectButton is not null)
        {
            connectButton.onClick.RemoveListener(connect);
        }

        if (disconnectButton is not null)
        {
            disconnectButton.onClick.RemoveListener(disconnect);
        }

        if (networkController is not null)
        {
            networkController.StateChanged -= refresh;
        }
    }

    private void Update()
    {
        if (Keyboard.current?.f2Key.wasPressedThisFrame == true)
        {
            setPanelVisible(!isPanelVisible);
        }
    }

    private void bindButtons()
    {
        if (connectButton is not null)
        {
            connectButton.onClick.RemoveListener(connect);
            connectButton.onClick.AddListener(connect);
        }

        if (disconnectButton is not null)
        {
            disconnectButton.onClick.RemoveListener(disconnect);
            disconnectButton.onClick.AddListener(disconnect);
        }
    }

    private void connect()
    {
        if (networkController is null)
        {
            return;
        }

        networkController.Connect(wsUrlInput?.text ?? string.Empty, viewerIdInput?.text ?? string.Empty);
        refresh();
    }

    private void disconnect()
    {
        networkController?.Disconnect();
        refresh();
    }

    private void refresh()
    {
        if (networkController is null)
        {
            return;
        }

        var isConnected = networkController.IsConnected;
        if (isConnected && !wasConnected)
        {
            setPanelVisible(false);
        }
        else if (!isConnected && wasConnected)
        {
            setPanelVisible(true);
        }

        wasConnected = isConnected;

        if (statusText is not null)
        {
            var pendingText = networkController.IsRequestInFlight ? " / 请求中" : string.Empty;
            statusText.text =
                $"连接状态：{localizeConnectionState(networkController.ConnectionState)} / 玩家ID={networkController.ViewerPlayerNumericId}{pendingText}";
        }

        if (errorText is not null)
        {
            errorText.text = string.IsNullOrWhiteSpace(networkController.LastError)
                ? string.Empty
                : $"错误：{networkController.LastError}";
        }

        if (connectButton is not null)
        {
            connectButton.interactable = !isConnected && !networkController.IsRequestInFlight;
        }

        if (disconnectButton is not null)
        {
            disconnectButton.interactable = isConnected;
        }

        if (wsUrlInput is not null)
        {
            wsUrlInput.interactable = !isConnected;
        }

        if (viewerIdInput is not null)
        {
            viewerIdInput.interactable = !isConnected;
        }
    }

    private void setPanelVisible(bool visible)
    {
        isPanelVisible = visible;
        canvasGroup = ensureCanvasGroup();
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private CanvasGroup ensureCanvasGroup()
    {
        if (TryGetComponent<CanvasGroup>(out var existingCanvasGroup) && existingCanvasGroup is not null)
        {
            return existingCanvasGroup;
        }

        return gameObject.AddComponent<CanvasGroup>();
    }

    private static string localizeConnectionState(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return "未连接";
        }

        if (state.StartsWith("disconnected", System.StringComparison.Ordinal))
        {
            return "已断开";
        }

        return state switch
        {
            "connected" => "已连接",
            "connecting" => "连接中",
            _ => state,
        };
    }
}
}
