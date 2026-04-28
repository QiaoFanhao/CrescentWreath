# Unity Smoke Test Checklist (P-Bridge-Unity-07)

## Preconditions
- Server WebSocket host is running (`ws://127.0.0.1:18080/ws`).
- Unity scene has `SocketDebugPanel` active.
- `viewerPlayerNumericId == actorPlayerNumericId` (default: `1`).

## Flow Steps (9)
| Step | Action | Expected Key Projection Fields | Result | Notes |
|---|---|---|---|---|
| 1 | Connect | connection=`connected` | ☐ Pass ☐ Fail | |
| 2 | EnterAction | `currentPhase=action`, `currentPlayerNumericId` unchanged | ☐ Pass ☐ Fail | |
| 3 | Draw | `handCards.Count` increases, `eventLog.Count` updates | ☐ Pass ☐ Fail | |
| 4 | Select hand card + Play Selected | `handCards.Count` decreases and/or `fieldCards.Count` increases; `selectedHandCardId` cleared | ☐ Pass ☐ Fail | |
| 5 | EnterSummon | `currentPhase=summon` | ☐ Pass ☐ Fail | |
| 6 | Select summon card + Summon Selected | `summonZoneCards.Count` decreases; `selectedSummonCardId` cleared | ☐ Pass ☐ Fail | |
| 7 | EnterEnd | `currentPhase=end` | ☐ Pass ☐ Fail | |
| 8 | StartNextTurn | `turnNumber` increases, `currentPlayerNumericId` switches | ☐ Pass ☐ Fail | |
| 9 | Next Player EnterAction | `currentPhase=action`, `currentPlayerNumericId` stays at new player | ☐ Pass ☐ Fail | |

## Error-path checks
- Click `Play Selected` without selected hand card:
  - Expect local error text.
  - Expect no outbound request generated for this click.
- Click `Summon Selected` without selected summon card:
  - Expect local error text.
  - Expect no outbound request generated for this click.

## Trace fields to verify per successful response
- actionType
- currentPhase
- currentPlayerNumericId
- handCount
- fieldCount
- summonZoneCount
- eventLogCount

## Final result
- Flow Checklist reaches step 9 passed.
- `Recommended Next Step` shows completed.
- Raw Request / Raw Response remain visible for debugging.
