using System;
using System.Collections;
using System.Linq;
using CrescentWreath.Client.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CrescentWreath.Client.UI
{
public sealed class LobbyRootView : MonoBehaviour
{
    private const string SettingsResourcesPath = "UI/LobbyPresentationSettings";
    private static readonly Color Backdrop = new(0.008f, 0.018f, 0.03f, 0.9f);
    private static readonly Color Panel = new(0.025f, 0.075f, 0.105f, 0.97f);
    private static readonly Color PanelRaised = new(0.035f, 0.115f, 0.15f, 0.98f);
    private static readonly Color PanelSoft = new(0.045f, 0.13f, 0.165f, 0.82f);
    private static readonly Color Cyan = new(0.18f, 0.78f, 0.86f, 1f);
    private static readonly Color Gold = new(0.95f, 0.72f, 0.25f, 1f);
    private static readonly Color Coral = new(0.88f, 0.32f, 0.3f, 1f);
    private static readonly Color Muted = new(0.58f, 0.68f, 0.74f, 1f);
    private static readonly Color Success = new(0.35f, 0.86f, 0.62f, 1f);

    [SerializeField] private GameClientNetworkController? networkController;
    [SerializeField] private BattlefieldUiTheme? theme;
    [SerializeField] private LobbyPresentationSettings? presentationSettings;

    private CanvasGroup? canvasGroup;
    private BattlefieldLayoutRoot? layoutRoot;
    private RectTransform? connectPage;
    private RectTransform? roomListPage;
    private RectTransform? roomPage;
    private RectTransform? roomCardsRoot;
    private RectTransform? teamAPlayersRoot;
    private RectTransform? teamBPlayersRoot;
    private TMP_InputField? urlInput;
    private TMP_InputField? nameInput;
    private TMP_InputField? roomNameInput;
    private TMP_Text? connectStatusText;
    private TMP_Text? listConnectionText;
    private TMP_Text? listEmptyText;
    private TMP_Text? selectedRoomText;
    private TMP_Text? roomTitleText;
    private TMP_Text? roomStatusText;
    private TMP_Text? roomConditionText;
    private TMP_Text? selfSummaryText;
    private TMP_Text? unassignedHeaderText;
    private TMP_Text? playersMetricText;
    private TMP_Text? teamAMetricText;
    private TMP_Text? teamBMetricText;
    private TMP_Text? readyMetricText;
    private TMP_Text? readyButtonLabel;
    private Button? connectButton;
    private Button? refreshButton;
    private Button? createRoomButton;
    private Button? joinButton;
    private Button? disconnectButton;
    private Button? teamAButton;
    private Button? teamBButton;
    private Button? readyToggleButton;
    private Button? leaveButton;
    private string selectedRoomId = string.Empty;
    private bool uiBuilt;
    private RectTransform? activePage;
    private Coroutine? pageTransition;
    private Coroutine? battlefieldTransition;
    private RectTransform? matchStartingOverlay;
    private bool isBattlefieldTransitioning;

    public void Configure(GameClientNetworkController newNetworkController, BattlefieldUiTheme newTheme)
    {
        unsubscribe();
        networkController = newNetworkController;
        theme = newTheme;
        presentationSettings ??= Resources.Load<LobbyPresentationSettings>(SettingsResourcesPath) ??
                                 LobbyPresentationSettings.CreateRuntimeDefault();
        layoutRoot ??= GetComponentInParent<BattlefieldLayoutRoot>();
        canvasGroup = ensureCanvasGroup();
        ensureUi();
        subscribe();
        refresh();
    }

    private void OnEnable()
    {
        canvasGroup = ensureCanvasGroup();
        if (networkController is not null && theme is not null)
        {
            ensureUi();
            subscribe();
            refresh();
        }
    }

    private void OnDisable()
    {
        unsubscribe();
        if (Application.isPlaying && !isBattlefieldTransitioning)
        {
            setBattlefieldHudVisible(true, 1f);
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying ||
            isBattlefieldTransitioning ||
            canvasGroup is null ||
            canvasGroup.alpha <= 0.001f ||
            layoutRoot?.DebugCanvas is null)
        {
            return;
        }

        var shouldShowDebug = presentationSettings?.showDevelopmentOverlay == true;
        if (layoutRoot.DebugCanvas.gameObject.activeSelf != shouldShowDebug)
        {
            layoutRoot.SetDebugVisible(shouldShowDebug);
        }
    }

    private void subscribe()
    {
        if (networkController is null)
        {
            return;
        }

        networkController.StateChanged -= refresh;
        networkController.StateChanged += refresh;
    }

    private void unsubscribe()
    {
        if (networkController is not null)
        {
            networkController.StateChanged -= refresh;
        }
    }

    private void refresh()
    {
        if (!uiBuilt || networkController is null)
        {
            return;
        }

        var lobby = networkController.CurrentLobbyProjection;
        if (lobby is { gameStarted: true })
        {
            beginBattlefieldTransition();
            return;
        }

        if (isBattlefieldTransitioning)
        {
            return;
        }

        setLobbyPresentationActive(true);
        setVisible(true);
        if (!networkController.IsConnected)
        {
            showPage(connectPage);
            renderConnectPage();
            return;
        }

        if (lobby?.currentRoom is not null)
        {
            showPage(roomPage);
            renderRoomPage(lobby);
            return;
        }

        showPage(roomListPage);
        renderRoomListPage(lobby);
    }

    private void renderConnectPage()
    {
        if (networkController is null)
        {
            return;
        }

        var connecting = string.Equals(networkController.ConnectionState, "connecting", StringComparison.OrdinalIgnoreCase);
        connectStatusText!.text = connecting
            ? "正在建立连接，请稍候……"
            : string.IsNullOrWhiteSpace(networkController.LastError)
                ? "尚未连接到局域网服务器"
                : "连接失败 · " + networkController.LastError;
        connectStatusText.color = connecting
            ? Gold
            : string.IsNullOrWhiteSpace(networkController.LastError) ? Muted : colorError();
        connectButton!.interactable = !connecting && !networkController.IsRequestInFlight;
    }

    private void renderRoomListPage(LobbyProjectionViewModel? lobby)
    {
        if (networkController is null)
        {
            return;
        }

        var diagnostics = presentationSettings?.showDevelopmentOverlay == true
            ? "    " + networkController.LastNetworkDiagnostics
            : string.Empty;
        listConnectionText!.text = $"● 已连接至  {networkController.LobbyWsUrl}{diagnostics}";
        listConnectionText.color = Success;

        var rooms = lobby?.rooms ?? new System.Collections.Generic.List<LobbyRoomSummaryViewModel>();
        if (rooms.Count == 0)
        {
            selectedRoomId = string.Empty;
        }
        else if (rooms.All(room => !string.Equals(room.roomId, selectedRoomId, StringComparison.Ordinal)))
        {
            selectedRoomId = rooms[0].roomId;
        }

        clearChildren(roomCardsRoot);
        listEmptyText = createText(
            "EmptyState",
            roomCardsRoot!,
            "暂无可加入的房间\n创建一个房间，或点击刷新获取最新列表",
            16f,
            TextAlignmentOptions.Center,
            Muted);
        listEmptyText.textWrappingMode = TextWrappingModes.Normal;
        setPreferredHeight(listEmptyText.gameObject, 150f);
        foreach (var room in rooms)
        {
            addRoomCard(room);
        }

        listEmptyText!.gameObject.SetActive(rooms.Count == 0);
        if (rooms.Count == 0)
        {
            var emptyCreateButton = createButton(
                "EmptyCreateButton",
                roomCardsRoot!,
                "创建第一个房间",
                ButtonStyle.Primary,
                () => networkController?.TryLobbyCreateRoom(roomNameInput?.text ?? string.Empty),
                46f);
            setPreferredWidth(emptyCreateButton.gameObject, 260f);
            emptyCreateButton.interactable = !networkController.IsRequestInFlight;
        }
        var selectedRoom = rooms.FirstOrDefault(room => string.Equals(room.roomId, selectedRoomId, StringComparison.Ordinal));
        selectedRoomText!.text = selectedRoom is null
            ? "尚未选择房间"
            : $"已选择：{selectedRoom.roomName}  ·  {selectedRoom.playerCount}/4 人";

        var canSend = !networkController.IsRequestInFlight;
        refreshButton!.interactable = canSend;
        createRoomButton!.interactable = canSend;
        joinButton!.interactable = canSend && selectedRoom is not null;
        disconnectButton!.interactable = canSend;
    }

    private void renderRoomPage(LobbyProjectionViewModel lobby)
    {
        if (networkController is null || lobby.currentRoom is null)
        {
            return;
        }

        var room = lobby.currentRoom;
        roomTitleText!.text = room.roomName;
        roomStatusText!.text = localizeRoomStatus(room.status);
        roomStatusText.color = room.status switch
        {
            "waiting" => Cyan,
            "starting" => Gold,
            "inGame" => Success,
            _ => Muted,
        };
        var teamACount = room.players.Count(player => player.selectedTeam == "teamA");
        var teamBCount = room.players.Count(player => player.selectedTeam == "teamB");
        var readyCount = room.players.Count(player => player.isReady);
        playersMetricText!.text = $"{room.players.Count}/4";
        teamAMetricText!.text = $"{teamACount}/2";
        teamBMetricText!.text = $"{teamBCount}/2";
        readyMetricText!.text = $"{readyCount}/4";
        roomConditionText!.text = localizeStartReason(room.startReasonKey);

        var self = room.players.FirstOrDefault(player => player.connectionId == lobby.connectedClientId);
        selfSummaryText!.text = self is null
            ? "正在同步你的房间身份……"
            : $"{self.displayName}    {localizeTeam(self.selectedTeam)}    " +
              $"{(self.isReady ? "已准备" : "未准备")}" +
              (self.isHost ? "    ◆ 房主" : string.Empty);

        clearChildren(teamAPlayersRoot);
        clearChildren(teamBPlayersRoot);
        var teamAPlayers = room.players.Where(player => player.selectedTeam == "teamA").ToList();
        var teamBPlayers = room.players.Where(player => player.selectedTeam == "teamB").ToList();
        foreach (var player in teamAPlayers)
        {
            addPlayerCard(teamAPlayersRoot, player, player.connectionId == lobby.connectedClientId);
        }
        foreach (var player in teamBPlayers)
        {
            addPlayerCard(teamBPlayersRoot, player, player.connectionId == lobby.connectedClientId);
        }
        for (var index = teamAPlayers.Count; index < 2; index++)
        {
            addEmptyPlayerSlot(teamAPlayersRoot);
        }
        for (var index = teamBPlayers.Count; index < 2; index++)
        {
            addEmptyPlayerSlot(teamBPlayersRoot);
        }

        var unassignedPlayers = room.players
            .Where(player => player.selectedTeam != "teamA" && player.selectedTeam != "teamB")
            .Select(player => player.displayName)
            .ToArray();
        unassignedHeaderText!.text = unassignedPlayers.Length == 0
            ? "所有玩家均已选择阵营"
            : "未选择阵营： " + string.Join("、", unassignedPlayers);

        var canSend = !networkController.IsRequestInFlight && string.Equals(room.status, "waiting", StringComparison.Ordinal);
        teamAButton!.interactable = canSend && self?.selectedTeam != "teamA";
        teamBButton!.interactable = canSend && self?.selectedTeam != "teamB";
        readyToggleButton!.interactable = canSend && self is not null &&
                                          (self.selectedTeam == "teamA" || self.selectedTeam == "teamB");
        readyButtonLabel!.text = self is { isReady: true } ? "取消准备" : "准备";
        leaveButton!.interactable = canSend;
    }

    private void ensureUi()
    {
        if (uiBuilt)
        {
            return;
        }

        var legacyContent = transform.Find("Content");
        if (legacyContent is not null)
        {
            legacyContent.gameObject.SetActive(false);
        }

        styleRootBackground();
        createBackdropDecoration();
        connectPage = buildConnectPage();
        roomListPage = buildRoomListPage();
        roomPage = buildRoomPage();
        matchStartingOverlay = buildMatchStartingOverlay();
        uiBuilt = true;
    }

    private RectTransform buildConnectPage()
    {
        var page = createPage("ConnectPage");
        var shell = createPanel("ConnectShell", page, new Vector2(0.28f, 0.09f), new Vector2(0.72f, 0.91f), Panel);
        addOutline(shell.gameObject, new Color(0.12f, 0.42f, 0.52f, 0.7f), new Vector2(1.5f, -1.5f));
        var layout = shell.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(54, 54, 28, 24);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        setPreferredHeight(createText("Eyebrow", shell, "CRESCENT WREATH", 14f, TextAlignmentOptions.Center, Gold).gameObject, 24f);
        setPreferredHeight(createText("Title", shell, "新月花冠", 42f, TextAlignmentOptions.Center, colorText(), FontStyles.Bold).gameObject, 58f);
        setPreferredHeight(createText("Subtitle", shell, "局 域 网 对 战", 19f, TextAlignmentOptions.Center, Cyan).gameObject, 34f);
        var description = createText(
            "Description",
            shell,
            "与同一局域网或 Radmin VPN 中的玩家连接",
            14f,
            TextAlignmentOptions.Center,
            Muted);
        setPreferredHeight(description.gameObject, 30f);
        createDivider(shell, Cyan);

        urlInput = createLabeledInput(shell, "服务器地址", networkController?.LobbyWsUrl ?? "ws://127.0.0.1:18080/ws?mode=lobby");
        nameInput = createLabeledInput(shell, "玩家昵称", networkController?.LobbyDisplayName ?? "Player");
        connectStatusText = createText("ConnectionStatus", shell, string.Empty, 14f, TextAlignmentOptions.Center, Muted);
        setPreferredHeight(connectStatusText.gameObject, 40f);
        connectButton = createButton(
            "ConnectButton",
            shell,
            "连接服务器",
            ButtonStyle.Primary,
            () => networkController?.ConnectLobby(urlInput?.text ?? string.Empty, nameInput?.text ?? string.Empty),
            54f);
        setPreferredHeight(createText("Hint", shell, "输入主机的局域网或 Radmin VPN 地址", 12f, TextAlignmentOptions.Center, Muted).gameObject, 28f);
        return page;
    }

    private RectTransform buildRoomListPage()
    {
        var page = createPage("RoomListPage");
        var shell = createPanel("RoomListShell", page, new Vector2(0.09f, 0.07f), new Vector2(0.91f, 0.93f), Panel);
        addOutline(shell.gameObject, new Color(0.12f, 0.42f, 0.52f, 0.62f), new Vector2(1.5f, -1.5f));
        var layout = shell.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(34, 34, 28, 28);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        var header = createHorizontal("Header", shell, 66f, 12f);
        var titleBlock = createVertical("TitleBlock", header, 0f, 0f);
        setFlexibleWidth(titleBlock.gameObject, 1f);
        setPreferredHeight(createText("Title", titleBlock, "局域网房间", 34f, TextAlignmentOptions.Left, colorText(), FontStyles.Bold).gameObject, 42f);
        setPreferredHeight(createText("Subtitle", titleBlock, "选择房间，或创建一场新的对局", 13f, TextAlignmentOptions.Left, Muted).gameObject, 22f);
        var brand = createText("Brand", header, "LAN LOBBY", 13f, TextAlignmentOptions.Right, Gold, FontStyles.Bold);
        setPreferredWidth(brand.gameObject, 150f);

        listConnectionText = createStatusStrip(shell);
        var scroll = createScrollView("RoomScroll", shell, out roomCardsRoot);
        setFlexibleHeight(scroll.gameObject, 1f);
        listEmptyText = createText("EmptyState", roomCardsRoot, string.Empty, 16f, TextAlignmentOptions.Center, Muted);
        setPreferredHeight(listEmptyText.gameObject, 0f);

        var selectionRow = createHorizontal("SelectionRow", shell, 52f, 12f);
        roomNameInput = createInput(selectionRow, "房间名称", (networkController?.LobbyDisplayName ?? "Player") + "的房间");
        setFlexibleWidth(roomNameInput.gameObject, 1f);
        selectedRoomText = createText("SelectedRoom", selectionRow, "尚未选择房间", 13f, TextAlignmentOptions.MidlineRight, Muted);
        setPreferredWidth(selectedRoomText.gameObject, 320f);

        var actions = createHorizontal("Actions", shell, 48f, 10f);
        refreshButton = createButton("RefreshButton", actions, "刷新列表", ButtonStyle.Secondary, () => networkController?.TryLobbyRefresh());
        createRoomButton = createButton("CreateButton", actions, "创建房间", ButtonStyle.Gold, () => networkController?.TryLobbyCreateRoom(roomNameInput?.text ?? string.Empty));
        joinButton = createButton("JoinButton", actions, "加入房间", ButtonStyle.Primary, joinSelectedRoom);
        disconnectButton = createButton("DisconnectButton", actions, "断开连接", ButtonStyle.Danger, () => networkController?.Disconnect());
        setFlexibleWidth(disconnectButton.gameObject, 0f);
        setPreferredWidth(disconnectButton.gameObject, 150f);
        return page;
    }

    private RectTransform buildRoomPage()
    {
        var page = createPage("RoomPage");
        var shell = createPanel("RoomShell", page, new Vector2(0.07f, 0.055f), new Vector2(0.93f, 0.945f), Panel);
        addOutline(shell.gameObject, new Color(0.12f, 0.42f, 0.52f, 0.62f), new Vector2(1.5f, -1.5f));
        var layout = shell.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 24, 24);
        layout.spacing = 11f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;

        var header = createHorizontal("Header", shell, 64f, 12f);
        var roomHeading = createVertical("RoomHeading", header, 0f, 0f);
        setFlexibleWidth(roomHeading.gameObject, 1f);
        roomTitleText = createText("RoomTitle", roomHeading, "房间", 28f, TextAlignmentOptions.Left, colorText(), FontStyles.Bold);
        setPreferredHeight(roomTitleText.gameObject, 38f);
        roomStatusText = createText("RoomStatus", roomHeading, "等待中", 13f, TextAlignmentOptions.Left, Cyan, FontStyles.Bold);
        setPreferredHeight(roomStatusText.gameObject, 22f);
        var roomBadge = createText("RoomBadge", header, "2 VS 2  ·  LAN MATCH", 13f, TextAlignmentOptions.Right, Gold, FontStyles.Bold);
        setPreferredWidth(roomBadge.gameObject, 210f);

        var metrics = createHorizontal("MatchMetrics", shell, 62f, 12f);
        playersMetricText = addMetric(metrics, "PLAYERS", "0/4", 110f);
        teamAMetricText = addMetric(metrics, "TEAM A", "0/2", 110f, Cyan);
        teamBMetricText = addMetric(metrics, "TEAM B", "0/2", 110f, Coral);
        readyMetricText = addMetric(metrics, "READY", "0/4", 110f, Gold);

        roomConditionText = createText("StartConditions", shell, string.Empty, 15f, TextAlignmentOptions.Center, Muted);
        setPreferredHeight(roomConditionText.gameObject, 34f);
        var selfPanel = createPanel("SelfPanel", shell, Vector2.zero, Vector2.one, new Color(0.045f, 0.17f, 0.2f, 0.9f));
        setPreferredHeight(selfPanel.gameObject, 44f);
        selfSummaryText = createText("SelfSummary", selfPanel, string.Empty, 14f, TextAlignmentOptions.Center, colorText(), FontStyles.Bold);
        stretch(selfSummaryText.rectTransform, 16f, 5f);

        var teams = createHorizontal("TeamColumns", shell, 0f, 14f);
        setFlexibleHeight(teams.gameObject, 1f);
        teamAPlayersRoot = createTeamColumn(teams, "TEAM A", Cyan, out _);
        var versus = createText("Versus", teams, "VS", 26f, TextAlignmentOptions.Center, Gold, FontStyles.Bold);
        setPreferredWidth(versus.gameObject, 70f);
        teamBPlayersRoot = createTeamColumn(teams, "TEAM B", Coral, out _);

        unassignedHeaderText = createText("UnassignedPlayers", shell, string.Empty, 13f, TextAlignmentOptions.Center, Muted);
        setPreferredHeight(unassignedHeaderText.gameObject, 30f);

        var teamActions = createHorizontal("TeamActions", shell, 44f, 12f);
        teamAButton = createButton("TeamAButton", teamActions, "加入 Team A", ButtonStyle.TeamA, () => networkController?.TryLobbySetTeam("teamA"));
        teamBButton = createButton("TeamBButton", teamActions, "加入 Team B", ButtonStyle.TeamB, () => networkController?.TryLobbySetTeam("teamB"));

        var primaryActions = createHorizontal("PrimaryActions", shell, 54f, 12f);
        readyToggleButton = createButton("ReadyToggleButton", primaryActions, "准备", ButtonStyle.Gold, toggleReady, 52f);
        readyButtonLabel = readyToggleButton.transform.Find("Label")?.GetComponent<TMP_Text>();
        setFlexibleWidth(readyToggleButton.gameObject, 3f);
        leaveButton = createButton("LeaveButton", primaryActions, "离开房间", ButtonStyle.Danger, () => networkController?.TryLobbyLeaveRoom(), 52f);
        setFlexibleWidth(leaveButton.gameObject, 1f);
        return page;
    }

    private void addRoomCard(LobbyRoomSummaryViewModel room)
    {
        if (roomCardsRoot is null)
        {
            return;
        }

        var selected = string.Equals(room.roomId, selectedRoomId, StringComparison.Ordinal);
        var cardObject = new GameObject("RoomCard_" + room.roomId, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        cardObject.transform.SetParent(roomCardsRoot, false);
        setPreferredHeight(cardObject, 88f);
        var image = cardObject.GetComponent<Image>();
        image.color = selected ? new Color(0.07f, 0.27f, 0.32f, 0.98f) : PanelRaised;
        addOutline(cardObject, selected ? Gold : new Color(0.12f, 0.35f, 0.42f, 0.75f), selected ? new Vector2(2f, -2f) : new Vector2(1f, -1f));
        var button = cardObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => selectRoom(room.roomId));

        var row = cardObject.AddComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(20, 20, 12, 12);
        row.spacing = 18f;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;

        var nameBlock = createVertical("NameBlock", cardObject.transform, 1f, 1f);
        setFlexibleWidth(nameBlock.gameObject, 1f);
        setPreferredHeight(createText("RoomName", nameBlock, room.roomName, 21f, TextAlignmentOptions.Left, colorText(), FontStyles.Bold).gameObject, 32f);
        setPreferredHeight(createText("RoomState", nameBlock, localizeRoomStatus(room.status), 12f, TextAlignmentOptions.Left, room.status == "waiting" ? Cyan : Gold).gameObject, 22f);
        addMetric(cardObject.transform, "玩家", $"{room.playerCount}/4", 86f);
        addMetric(cardObject.transform, "TEAM A", room.teamACount.ToString(), 86f, Cyan);
        addMetric(cardObject.transform, "TEAM B", room.teamBCount.ToString(), 86f, Coral);
        addMetric(cardObject.transform, "READY", room.readyCount.ToString(), 86f, Success);
    }

    private TMP_Text addMetric(Transform parent, string label, string value, float width, Color? accent = null)
    {
        var block = createVertical(label + "Metric", parent, 1f, 2f);
        setPreferredWidth(block.gameObject, width);
        setFlexibleWidth(block.gameObject, 1f);
        var valueText = createText("Value", block, value, 20f, TextAlignmentOptions.Center, accent ?? colorText(), FontStyles.Bold);
        setPreferredHeight(valueText.gameObject, 34f);
        setPreferredHeight(createText("Label", block, label, 10f, TextAlignmentOptions.Center, Muted).gameObject, 20f);
        return valueText;
    }

    private void addPlayerCard(RectTransform? parent, LobbyPlayerViewModel player, bool isSelf)
    {
        if (parent is null)
        {
            return;
        }

        var card = createPanel("Player_" + player.connectionId, parent, Vector2.zero, Vector2.one,
            isSelf ? new Color(0.08f, 0.27f, 0.3f, 0.98f) : PanelSoft);
        setPreferredHeight(card.gameObject, 70f);
        addOutline(card.gameObject, isSelf ? Gold : new Color(0.15f, 0.3f, 0.35f, 0.62f), isSelf ? new Vector2(2f, -2f) : new Vector2(1f, -1f));
        var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(13, 13, 8, 7);
        layout.spacing = 2f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        var playerName = player.displayName +
                         (isSelf ? "   [YOU]" : string.Empty) +
                         (player.isHost ? "   [HOST]" : string.Empty);
        setPreferredHeight(createText("PlayerName", card, playerName, 15f, TextAlignmentOptions.Left, colorText(), FontStyles.Bold).gameObject, 28f);
        var detail = player.isReady ? "✓ READY" : "○ 未准备";
        if (player.assignedPlayerNumericId > 0)
        {
            detail += $"    P{player.assignedPlayerNumericId}";
        }
        setPreferredHeight(createText("PlayerState", card, detail, 12f, TextAlignmentOptions.Left, player.isReady ? Success : Muted).gameObject, 22f);
    }

    private void addEmptyPlayerSlot(RectTransform? parent)
    {
        if (parent is null)
        {
            return;
        }

        var slot = createPanel(
            "EmptyPlayerSlot",
            parent,
            Vector2.zero,
            Vector2.one,
            new Color(0.025f, 0.065f, 0.08f, 0.66f));
        setPreferredHeight(slot.gameObject, 70f);
        addOutline(slot.gameObject, new Color(0.18f, 0.28f, 0.32f, 0.52f), new Vector2(1f, -1f));
        var label = createText(
            "EmptyLabel",
            slot,
            "等待玩家……",
            14f,
            TextAlignmentOptions.Center,
            new Color(Muted.r, Muted.g, Muted.b, 0.72f));
        stretch(label.rectTransform, 12f, 6f);
    }

    private RectTransform createTeamColumn(Transform parent, string title, Color accent, out TMP_Text headerText)
    {
        var column = createPanel(title.Replace(" ", string.Empty) + "Column", parent, Vector2.zero, Vector2.one, new Color(0.02f, 0.07f, 0.095f, 0.94f));
        setFlexibleWidth(column.gameObject, 1f);
        var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 9f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        headerText = createText("Header", column, title, 15f, TextAlignmentOptions.Center, accent, FontStyles.Bold);
        setPreferredHeight(headerText.gameObject, 32f);
        createDivider(column, accent);
        var players = createVertical("Players", column, 9f, 0f);
        setFlexibleHeight(players.gameObject, 1f);
        return players;
    }

    private void selectRoom(string roomId)
    {
        selectedRoomId = roomId;
        renderRoomListPage(networkController?.CurrentLobbyProjection);
    }

    private void joinSelectedRoom()
    {
        if (!string.IsNullOrWhiteSpace(selectedRoomId))
        {
            networkController?.TryLobbyJoinRoom(selectedRoomId);
        }
    }

    private void toggleReady()
    {
        var lobby = networkController?.CurrentLobbyProjection;
        var self = lobby?.currentRoom?.players.FirstOrDefault(
            player => player.connectionId == lobby.connectedClientId);
        if (self is not null)
        {
            networkController?.TryLobbySubmitReadyState(!self.isReady);
        }
    }

    private RectTransform createPage(string name)
    {
        var page = createRect(name, transform);
        stretch(page);
        var group = page.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        page.gameObject.SetActive(false);
        return page;
    }

    private RectTransform buildMatchStartingOverlay()
    {
        var overlay = createPanel(
            "MatchStartingOverlay",
            transform,
            Vector2.zero,
            Vector2.one,
            new Color(0.005f, 0.018f, 0.028f, 0.86f));
        var title = createText(
            "Title",
            overlay,
            "对局开始",
            42f,
            TextAlignmentOptions.Center,
            Gold,
            FontStyles.Bold);
        title.rectTransform.anchorMin = new Vector2(0.25f, 0.44f);
        title.rectTransform.anchorMax = new Vector2(0.75f, 0.56f);
        title.rectTransform.offsetMin = Vector2.zero;
        title.rectTransform.offsetMax = Vector2.zero;
        overlay.gameObject.SetActive(false);
        return overlay;
    }

    private RectTransform createScrollView(string name, Transform parent, out RectTransform content)
    {
        var scrollRoot = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
        scrollRoot.transform.SetParent(parent, false);
        scrollRoot.GetComponent<Image>().color = new Color(0.01f, 0.045f, 0.065f, 0.82f);
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollRoot.transform, false);
        stretch(viewport.GetComponent<RectTransform>(), 8f, 8f);
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.02f);
        viewport.GetComponent<Image>().raycastTarget = true;
        content = createVertical("Content", viewport.transform, 10f, 0f);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = scrollRoot.GetComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = content;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 32f;
        return scrollRoot.GetComponent<RectTransform>();
    }

    private TMP_Text createStatusStrip(Transform parent)
    {
        var strip = createPanel("ConnectionStrip", parent, Vector2.zero, Vector2.one, new Color(0.025f, 0.13f, 0.15f, 0.9f));
        setPreferredHeight(strip.gameObject, 38f);
        var text = createText("ConnectionText", strip, string.Empty, 12f, TextAlignmentOptions.Center, Success);
        stretch(text.rectTransform, 12f, 4f);
        return text;
    }

    private TMP_InputField createLabeledInput(Transform parent, string label, string value)
    {
        var group = createVertical(label.Replace(" ", string.Empty) + "Field", parent, 5f, 0f);
        setPreferredHeight(group.gameObject, 76f);
        setPreferredHeight(createText("Label", group, label, 13f, TextAlignmentOptions.Left, Muted, FontStyles.Bold).gameObject, 22f);
        var input = createInput(group, label, value);
        setPreferredHeight(input.gameObject, 46f);
        return input;
    }

    private TMP_InputField createInput(Transform parent, string placeholder, string value)
    {
        var inputObject = new GameObject(placeholder.Replace(" ", string.Empty) + "Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField), typeof(LayoutElement));
        inputObject.transform.SetParent(parent, false);
        var background = inputObject.GetComponent<Image>();
        background.color = new Color(0.012f, 0.045f, 0.065f, 1f);
        background.raycastTarget = true;
        addOutline(inputObject, new Color(0.12f, 0.36f, 0.43f, 0.9f), new Vector2(1f, -1f));
        setPreferredHeight(inputObject, 46f);
        var input = inputObject.GetComponent<TMP_InputField>();
        input.targetGraphic = background;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.contentType = TMP_InputField.ContentType.Standard;
        input.customCaretColor = true;
        input.caretColor = Cyan;
        input.caretWidth = 3;
        input.caretBlinkRate = 0.85f;
        input.selectionColor = new Color(0.25f, 0.75f, 1f, 0.42f);
        var textArea = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(inputObject.transform, false);
        var viewport = textArea.GetComponent<RectTransform>();
        stretch(viewport, 12f, 4f);
        var text = createText("Text", textArea.transform, value, 15f, TextAlignmentOptions.MidlineLeft, colorText());
        stretch(text.rectTransform, 4f, 0f);
        text.overflowMode = TextOverflowModes.Overflow;
        var placeholderText = createText("Placeholder", textArea.transform, placeholder, 15f, TextAlignmentOptions.MidlineLeft, new Color(Muted.r, Muted.g, Muted.b, 0.65f));
        stretch(placeholderText.rectTransform, 4f, 0f);
        input.textViewport = viewport;
        input.textComponent = text;
        input.placeholder = placeholderText;
        input.text = value;
        input.ForceLabelUpdate();
        inputObject.AddComponent<LobbyInputCaretView>().Configure(input, text);
        return input;
    }

    private Button createButton(string name, Transform parent, string label, ButtonStyle style, Action action, float preferredHeight = 44f)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);
        setPreferredHeight(buttonObject, preferredHeight);
        setFlexibleWidth(buttonObject, 1f);
        var image = buttonObject.GetComponent<Image>();
        image.color = buttonColor(style);
        addOutline(buttonObject, buttonAccent(style), new Vector2(1f, -1f));
        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => action());
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.78f, 0.82f, 0.84f, 1f);
        colors.disabledColor = new Color(0.38f, 0.42f, 0.44f, 0.6f);
        colors.fadeDuration = 0.1f;
        button.colors = colors;
        var text = createText("Label", buttonObject.transform, label, 17f, TextAlignmentOptions.Center, colorText(), FontStyles.Bold);
        stretch(text.rectTransform, 6f, 2f);
        return button;
    }

    private TMP_Text createText(string name, Transform parent, string value, float fontSize, TextAlignmentOptions alignment, Color color, FontStyles style = FontStyles.Normal)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<TMP_Text>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = style;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        if (theme?.PrimaryFont is not null)
        {
            text.font = theme.PrimaryFont;
        }
        return text;
    }

    private static RectTransform createRect(string name, Transform parent)
    {
        var value = new GameObject(name, typeof(RectTransform));
        value.transform.SetParent(parent, false);
        return value.GetComponent<RectTransform>();
    }

    private static RectTransform createPanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = color;
        return rect;
    }

    private static RectTransform createHorizontal(string name, Transform parent, float height, float spacing)
    {
        var row = createRect(name, parent);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        if (height > 0f) setPreferredHeight(row.gameObject, height);
        return row;
    }

    private static RectTransform createVertical(string name, Transform parent, float spacing, float padding)
    {
        var column = createRect(name, parent);
        var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        var inset = Mathf.RoundToInt(padding);
        layout.padding = new RectOffset(inset, inset, inset, inset);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return column;
    }

    private static void createDivider(Transform parent, Color color)
    {
        var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        divider.transform.SetParent(parent, false);
        divider.GetComponent<Image>().color = new Color(color.r, color.g, color.b, 0.58f);
        divider.GetComponent<LayoutElement>().preferredHeight = 2f;
    }

    private void createBackdropDecoration()
    {
        createPanel("TopGlow", transform, new Vector2(0.18f, 0.955f), new Vector2(0.82f, 0.96f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f)).SetAsFirstSibling();
        createPanel("LeftRail", transform, new Vector2(0.035f, 0.22f), new Vector2(0.038f, 0.78f), new Color(Gold.r, Gold.g, Gold.b, 0.32f)).SetAsFirstSibling();
        createPanel("RightRail", transform, new Vector2(0.962f, 0.22f), new Vector2(0.965f, 0.78f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.28f)).SetAsFirstSibling();
    }

    private void styleRootBackground()
    {
        if (TryGetComponent<Image>(out var image))
        {
            image.color = Backdrop;
            image.raycastTarget = true;
        }
    }

    private void showPage(RectTransform? page)
    {
        if (page is null || activePage == page)
        {
            return;
        }

        if (!Application.isPlaying)
        {
            setPageImmediate(page);
            return;
        }

        if (pageTransition is not null)
        {
            StopCoroutine(pageTransition);
        }
        pageTransition = StartCoroutine(transitionPage(activePage, page));
        activePage = page;
    }

    private IEnumerator transitionPage(RectTransform? previous, RectTransform next)
    {
        var duration = presentationSettings?.pageTransitionDuration ?? 0.16f;
        var slideDistance = presentationSettings?.pageSlideDistance ?? 28f;
        var previousGroup = previous?.GetComponent<CanvasGroup>();
        var nextGroup = next.GetComponent<CanvasGroup>();
        foreach (var candidate in new[] { connectPage, roomListPage, roomPage })
        {
            if (candidate is not null && candidate != previous && candidate != next)
            {
                candidate.gameObject.SetActive(false);
            }
        }
        next.gameObject.SetActive(true);
        next.SetAsLastSibling();
        nextGroup.alpha = 0f;
        nextGroup.interactable = false;
        nextGroup.blocksRaycasts = false;
        next.anchoredPosition = new Vector2(slideDistance, 0f);

        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            var eased = Mathf.SmoothStep(0f, 1f, progress);
            nextGroup.alpha = eased;
            next.anchoredPosition = new Vector2(Mathf.Lerp(slideDistance, 0f, eased), 0f);
            if (previousGroup is not null)
            {
                previousGroup.alpha = 1f - eased;
                previous!.anchoredPosition = new Vector2(Mathf.Lerp(0f, -slideDistance, eased), 0f);
            }
            yield return null;
        }

        if (previous is not null)
        {
            previous.gameObject.SetActive(false);
            previous.anchoredPosition = Vector2.zero;
        }
        next.anchoredPosition = Vector2.zero;
        nextGroup.alpha = 1f;
        nextGroup.interactable = true;
        nextGroup.blocksRaycasts = true;
        pageTransition = null;
    }

    private void setPageImmediate(RectTransform page)
    {
        foreach (var candidate in new[] { connectPage, roomListPage, roomPage })
        {
            if (candidate is null)
            {
                continue;
            }
            var active = candidate == page;
            candidate.gameObject.SetActive(active);
            var group = candidate.GetComponent<CanvasGroup>();
            group.alpha = active ? 1f : 0f;
            group.interactable = active;
            group.blocksRaycasts = active;
            candidate.anchoredPosition = Vector2.zero;
        }
        activePage = page;
    }

    private void beginBattlefieldTransition()
    {
        if (isBattlefieldTransitioning)
        {
            return;
        }

        isBattlefieldTransitioning = true;
        canvasGroup = ensureCanvasGroup();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;
        matchStartingOverlay?.gameObject.SetActive(true);
        matchStartingOverlay?.SetAsLastSibling();
        setBattlefieldHudVisible(true, 0f);
        battlefieldTransition = StartCoroutine(transitionToBattlefield());
    }

    private IEnumerator transitionToBattlefield()
    {
        var duration = presentationSettings?.battlefieldTransitionDuration ?? 0.65f;
        var hudGroup = getScreenHudCanvasGroup();
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            var lobbyFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 1f, progress));
            canvasGroup!.alpha = 1f - lobbyFade;
            if (hudGroup is not null)
            {
                hudGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, 1f, progress));
            }
            yield return null;
        }

        matchStartingOverlay?.gameObject.SetActive(false);
        canvasGroup!.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        if (hudGroup is not null)
        {
            hudGroup.alpha = 1f;
            hudGroup.interactable = true;
            hudGroup.blocksRaycasts = true;
        }
        battlefieldTransition = null;
    }

    private void setLobbyPresentationActive(bool active)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        layoutRoot ??= GetComponentInParent<BattlefieldLayoutRoot>();
        if (active)
        {
            setBattlefieldHudVisible(false, 0f);
            layoutRoot?.SetDebugVisible(presentationSettings?.showDevelopmentOverlay == true);
        }
    }

    private void setBattlefieldHudVisible(bool visible, float alpha)
    {
        var hudGroup = getScreenHudCanvasGroup();
        if (layoutRoot?.ScreenHudCanvas is null || hudGroup is null)
        {
            return;
        }

        layoutRoot.ScreenHudCanvas.gameObject.SetActive(visible);
        hudGroup.alpha = alpha;
        hudGroup.interactable = visible;
        hudGroup.blocksRaycasts = visible;
    }

    private CanvasGroup? getScreenHudCanvasGroup()
    {
        layoutRoot ??= GetComponentInParent<BattlefieldLayoutRoot>();
        if (layoutRoot?.ScreenHudCanvas is null)
        {
            return null;
        }

        return layoutRoot.ScreenHudCanvas.TryGetComponent<CanvasGroup>(out var group)
            ? group
            : layoutRoot.ScreenHudCanvas.gameObject.AddComponent<CanvasGroup>();
    }

    private void setVisible(bool visible)
    {
        canvasGroup = ensureCanvasGroup();
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private static void clearChildren(RectTransform? root)
    {
        if (root is null) return;
        for (var index = root.childCount - 1; index >= 0; index--)
        {
            var child = root.GetChild(index).gameObject;
            child.SetActive(false);
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
    }

    private CanvasGroup ensureCanvasGroup()
    {
        return TryGetComponent<CanvasGroup>(out var existing) && existing is not null
            ? existing
            : gameObject.AddComponent<CanvasGroup>();
    }

    private Color colorText() => theme?.NormalText ?? new Color(0.94f, 0.97f, 1f, 1f);
    private Color colorError() => theme?.Error ?? new Color(0.94f, 0.24f, 0.25f, 1f);

    private static string localizeTeam(string team)
    {
        return team switch { "teamA" => "Team A", "teamB" => "Team B", _ => "未选择阵营" };
    }

    private static string localizeRoomStatus(string status)
    {
        return status switch
        {
            "waiting" => "等待中 · waiting",
            "starting" => "正在开始 · starting",
            "inGame" => "对局中 · inGame",
            "closed" => "已关闭 · closed",
            _ => string.IsNullOrWhiteSpace(status) ? "状态未知" : status,
        };
    }

    private static string localizeStartReason(string reasonKey)
    {
        return reasonKey switch
        {
            "ready" => "条件已满足，正在准备对局",
            "notEnoughPlayers" => "等待 4 名玩家加入",
            "teamCountInvalid" => "Team A / Team B 需要各 2 人",
            "notAllReady" => "等待所有玩家准备",
            "roomAlreadyStarted" => "房间已经开始对局",
            "internalError" => "服务器创建对局失败",
            _ => string.IsNullOrWhiteSpace(reasonKey) ? "等待房间状态同步" : reasonKey,
        };
    }

    private static Color buttonColor(ButtonStyle style)
    {
        return style switch
        {
            ButtonStyle.Primary => new Color(0.055f, 0.38f, 0.46f, 0.98f),
            ButtonStyle.Gold => new Color(0.42f, 0.31f, 0.09f, 0.98f),
            ButtonStyle.Danger => new Color(0.32f, 0.095f, 0.105f, 0.98f),
            ButtonStyle.TeamA => new Color(0.035f, 0.31f, 0.38f, 0.98f),
            ButtonStyle.TeamB => new Color(0.38f, 0.12f, 0.12f, 0.98f),
            _ => new Color(0.095f, 0.18f, 0.22f, 0.98f),
        };
    }

    private static Color buttonAccent(ButtonStyle style)
    {
        return style switch
        {
            ButtonStyle.Primary or ButtonStyle.TeamA => Cyan,
            ButtonStyle.Gold => Gold,
            ButtonStyle.Danger or ButtonStyle.TeamB => Coral,
            _ => new Color(0.25f, 0.42f, 0.48f, 0.85f),
        };
    }

    private static void addOutline(GameObject target, Color color, Vector2 distance)
    {
        var outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = distance;
        outline.useGraphicAlpha = true;
    }

    private static void stretch(RectTransform rect, float horizontalInset = 0f, float verticalInset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontalInset, verticalInset);
        rect.offsetMax = new Vector2(-horizontalInset, -verticalInset);
    }

    private static LayoutElement getLayout(GameObject target)
    {
        return target.TryGetComponent<LayoutElement>(out var layout) ? layout : target.AddComponent<LayoutElement>();
    }

    private static void setPreferredHeight(GameObject target, float value) => getLayout(target).preferredHeight = value;
    private static void setPreferredWidth(GameObject target, float value) => getLayout(target).preferredWidth = value;
    private static void setFlexibleHeight(GameObject target, float value) => getLayout(target).flexibleHeight = value;
    private static void setFlexibleWidth(GameObject target, float value) => getLayout(target).flexibleWidth = value;

    private enum ButtonStyle
    {
        Primary,
        Secondary,
        Gold,
        Danger,
        TeamA,
        TeamB,
    }
}
}
