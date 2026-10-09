# Crescent Wreath / 新月花冠

Unity-based four-player, 2v2 online card game prototype. The project uses a
server-authoritative rules engine and viewer-scoped WebSocket projections.

## Current Features

- Four-player 2v2 LAN multiplayer
- Lobby, team selection, ready flow, and server-assigned player seats
- Server-authoritative RuleCore and structured trace logging
- Viewer-scoped WebSocket state projection
- 3D battlefield and card inspection
- Character, Treasure, Anomaly, ResponseWindow, and InputContext systems
- Card movement, VFX/SFX hooks, and skill cut-in presentation

## Architecture

`RuleCore -> ServerGameSession -> Viewer Projection -> Unity Client`

- `RuleCore/`: deterministic game state and rule resolution
- `Server/`: lobby, authoritative session, WebSocket host, and projections
- `UnityClient/CrescentWreathClinet/`: Unity client and presentation layer
- `docs/`: rulebook and architecture/design documents

## Development Status

LAN Alpha / portfolio prototype. Steam, public matchmaking, accounts,
spectating, and reconnect support are outside the current scope.

## Local Development

- Unity editor: `6000.3.11f1`
- Server: run `Server/CrescentWreath.ServerPrototype.Host/run-host.bat`
- Default Lobby endpoint: `ws://127.0.0.1:18080/ws?mode=lobby`
- LAN players can replace `127.0.0.1` in the connection page with the host's
  LAN or VPN IPv4 address.

Legacy development connections using
`/ws?viewerPlayerNumericId=1..4` remain available for debugging.

## Third-party Assets

Some art, VFX, SFX, fonts, UI assets, and game reference materials are
third-party works. They are not claimed as original work by this repository.
Commercial VFX and SFX source assets are intentionally excluded where their
licenses do not permit public redistribution. Import licensed copies locally
and rebuild the effect catalogs from the Unity editor when those presentations
are required.

No open-source license is granted for project-owned game content unless a file
or directory explicitly states otherwise. Bundled third-party packages retain
their own license terms.
