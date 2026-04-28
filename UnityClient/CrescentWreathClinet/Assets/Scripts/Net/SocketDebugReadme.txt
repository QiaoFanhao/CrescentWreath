Socket Debug Minimal Bridge

1) Recommended transport
- Install Best WebSockets V3 package so `Best.WebSockets.WebSocket` is available.
- This bridge will automatically use Best WebSockets when found.
- If missing, it falls back to `ClientWebSocket` for desktop/editor smoke testing.

2) Scene
- Open `Assets/Scenes/SocketDebug.unity`.
- The `SocketDebugSceneBootstrap` script auto-creates `SocketDebugPanel` in this scene.

3) Default endpoint
- ws://127.0.0.1:18080/ws

4) Supported actionType in this panel
- drawOneCard
- playTreasureCard
