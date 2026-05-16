# Unity Smoke Test Checklist (P-Bridge-Unity-09)

## Preconditions
- Server WebSocket host is running (`ws://127.0.0.1:18080/ws`).
- Unity scene has `SocketDebugPanel` active.
- Default local IDs: `viewerPlayerNumericId == actorPlayerNumericId == 1`.
- Use `Flow Checklist` mode switch to choose `A / B / C`.

---

## Checklist A：基础主流程（主流程）
| Step | Action | Expected Key Fields | Result | Notes |
|---|---|---|---|---|
| A1 | Connect | `connection=connected` | ☐ Pass ☐ Fail | |
| A2 | EnterAction | `currentPhase=action` | ☐ Pass ☐ Fail | |
| A3 | Draw | `handCards.Count` increases | ☐ Pass ☐ Fail | |
| A4 | Select hand + Play Selected | `hand` decreases or `field` increases; selected hand cleared | ☐ Pass ☐ Fail | |
| A5 | EnterSummon | `currentPhase=summon` | ☐ Pass ☐ Fail | |
| A6 | Select summon/sakura + Summon Selected | `summonZone/sakura` changes; selected summon cleared | ☐ Pass ☐ Fail | |
| A7 | EnterEnd | `currentPhase=end` | ☐ Pass ☐ Fail | |
| A8 | StartNextTurn | `turnNumber` increases and `currentPlayerNumericId` switches | ☐ Pass ☐ Fail | |
| A9 | Next Player EnterAction | `currentPhase=action` with switched player | ☐ Pass ☐ Fail | |

Key checks:
- `currentPhase`
- `currentPlayerNumericId`
- `handCards.Count / fieldCards.Count / discardCount`
- `summonZoneCards.Count / sakuraCakeCards.Count`
- `eventLog.Count`

---

## Checklist B：ResponseWindow 续跑
| Step | Action | Expected Key Fields | Result | Notes |
|---|---|---|---|---|
| B1 | DebugOpenDamageWindow | request action=`debugOpenDamageResponseWindow` succeeded | ☐ Pass ☐ Fail | |
| B2 | Verify window | `hasResponseWindow=true` | ☐ Pass ☐ Fail | |
| B3 | Verify responder | `responseWindowNumericId>0` and `currentResponderPlayerNumericId>0` | ☐ Pass ☐ Fail | |
| B4 | Submit no-response | action=`submitResponse` returned | ☐ Pass ☐ Fail | |
| B5 | Verify success | `isSucceeded=true` | ☐ Pass ☐ Fail | |
| B6 | Verify closed | `hasResponseWindow=false` | ☐ Pass ☐ Fail | |
| B7 | Verify damage event | `eventLog` contains `damageResolved` | ☐ Pass ☐ Fail | |
| B8 | Verify hp event | `eventLog` contains `hpChanged` | ☐ Pass ☐ Fail | |

Tips:
- If local actor mismatches current responder, use panel button `将操作者切到当前响应者`.

---

## Checklist C：InputContext 续跑
Default stable path: `EnterAction -> Draw -> EnterEnd` to trigger `endPhase:discardToHandLimit` input.

| Step | Action | Expected Key Fields | Result | Notes |
|---|---|---|---|---|
| C1 | EnterAction | `currentPhase=action` | ☐ Pass ☐ Fail | |
| C2 | Draw | `handCards.Count` increases (normally 7) | ☐ Pass ☐ Fail | |
| C3 | EnterEnd | action returned successfully | ☐ Pass ☐ Fail | |
| C4 | Verify input opened | `hasInputContext=true` | ☐ Pass ☐ Fail | |
| C5 | Verify input detail | `inputContextNumericId>0`, `requiredPlayerNumericId>0`, `choiceKeys` not empty | ☐ Pass ☐ Fail | |
| C6 | SubmitInputChoice | action=`submitInputChoice` returned | ☐ Pass ☐ Fail | |
| C7 | Verify success | `isSucceeded=true` | ☐ Pass ☐ Fail | |
| C8 | Verify context advanced | inputContext closed or moved to next context | ☐ Pass ☐ Fail | |
| C9 | Verify input events | `eventLog` contains `inputContextClosed` or input-related event updates | ☐ Pass ☐ Fail | |

---

## Error-path sanity checks
- Click `Play Selected` without selection:
  - Expect local error and **no outbound request**.
- Click `Summon Selected` without selection:
  - Expect local error and **no outbound request**.
- Click `Response: No` while actor mismatches responder:
  - Expect local red block banner and **no outbound request**.

---

## Manual Test Report Template
- Date:
- Commit:
- Server restarted before test: Yes / No
- Unity mode: Editor PlayMode / Build

### Checklist A
- Result: Pass / Fail
- Notes:

### Checklist B
- Result: Pass / Fail
- Notes:

### Checklist C
- Result: Pass / Fail
- Notes:

### Key observed values
- Last `currentPhase`:
- Last `currentPlayerNumericId`:
- Last `hand/field/discard`:
- Last `summon/sakura` counts:
- Last `responseWindowNumericId`:
- Last `inputContextNumericId`:

### Error / exception logs
- 

### Final conclusion
- 
