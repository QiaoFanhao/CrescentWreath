# Unity Smoke Test Checklist (P-Bridge-Unity-09)

## Preconditions
- Server WebSocket host is running (`ws://127.0.0.1:18080/ws`).
  - Recommended startup: `dotnet run --project Server/CrescentWreath.ServerPrototype.Host/CrescentWreath.ServerPrototype.Host.csproj -- 18080`
- Unity scene has `SocketDebugPanel` active.
- Default local IDs: `viewerPlayerNumericId == actorPlayerNumericId == 1`.
- Use `Flow Checklist` mode switch to choose `A / B / C`.
- Before each full regression, run `调试：重开本局` once to freeze a clean single-session baseline.
- 若重开后仍停在 `start` 阶段，先点击一次 `调试：进入行动`，再执行 Checklist A。

## Multi-Client (4) Startup Notes
- The host now binds connection identity via query: `?viewerPlayerNumericId=1..4`.
- Open up to four Unity clients and set each client `viewerPlayerNumericId`/`actorPlayerNumericId` to the same value (`1`, `2`, `3`, `4`).
- Connect each client to the same URL base: `ws://127.0.0.1:18080/ws` (bridge appends viewer query automatically).
- If one viewer ID is already occupied, the extra connection is rejected with `409`.
- Successful actions from one client are broadcast to other connected viewers as push updates.

---

## One-Click Macro Usage
- `Run A Full`: auto-drive Checklist A until pass/fail stop.
- `Run B Full`: auto-drive Checklist B until pass/fail stop.
- `Run C Full`: auto-drive Checklist C until pass/fail stop.
- `停止 Full`: stop current macro run manually.
- Failure responses do **not** auto-skip steps; check `Flow Trace` and red local intercept banner.

---

## Checklist A：基础主流程（主流程）
| Step | Action | Expected Key Fields | Result | Notes |
|---|---|---|---|---|
| A1 | Connect | `connection=connected` | ☐ Pass ☐ Fail | |
| A2 | Draw | `handCards.Count` increases and `currentPhase=action` | ☐ Pass ☐ Fail | |
| A3 | Select hand + Play Selected | `hand` decreases or `field` increases; selected hand cleared | ☐ Pass ☐ Fail | |
| A4 | EnterSummon | `currentPhase=summon` | ☐ Pass ☐ Fail | |
| A5 | Select summon/sakura + Summon Selected | `summonZone/sakura` changes; selected summon cleared | ☐ Pass ☐ Fail | |
| A6 | End Turn (`enterEndPhase`) | 请求成功返回 | ☐ Pass ☐ Fail | |
| A7 | Verify next player switched | `turnNumber` increases and `currentPlayerNumericId` switches | ☐ Pass ☐ Fail | |
| A8 | Verify next player action ready | `currentPhase=action` for switched player | ☐ Pass ☐ Fail | |

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
- 非响应者客户端只会显示观察信息，不会显示 response/defense 提交按钮。

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
