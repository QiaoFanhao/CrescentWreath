# 04_42_RuleCore_M2.7_检查点说明

## 1. 已验证的最小时序能力（Validated）

结论：`RuleCore` 在 M2.7 已验证“请求进入 -> `ActionChain` 推进 -> 窗口/输入停顿 -> 续跑 -> 事件输出”的最小闭环能力。

- `PlayCardActionRequest` 基础闭环已验证：完成区域移动并产出 `CardMovedEvent`。  
  证据：`RuleCore/CrescentWreath.RuleCore.Tests/UnitTest1.cs`  
  用例：`ProcessPlayCardRequest_M2HappyPath_ShouldMoveCardAndProduceCardMovedEvent`
- 同链即时结算路径已验证（`test:onPlayDeal1`）：`ResponseWindow` 开/关后产出伤害事件。  
  证据：`RuleCore/CrescentWreath.RuleCore.Tests/UnitTest1.cs`  
  用例：`ProcessPlayCardRequest_ScriptedOnPlayDamage_ShouldStayInSingleChainAndProduceOrderedEvents`
- 同链输入续跑路径已验证（`test:onPlayChooseDamage`）：`InputContext` 开/关后在同链继续伤害结算。  
  证据：`RuleCore/CrescentWreath.RuleCore.Tests/UnitTest1.cs`、`RuleCore/CrescentWreath.RuleCore.Tests/ActionRequestProcessorInputContextFlowTests.cs`  
  用例：`ProcessPlayCardRequest_ScriptedOnPlayChooseDamage_ShouldOpenInputThenSubmitAndResolveDamageOnSameChain`、`OpenAndSubmitInputChoice_HappyPath_ShouldOpenAndCloseInputContext`
- 同链响应续跑路径已验证（`test:onPlayWaitResponseDamage` + 独立 staged 请求）：开窗暂停后由 `SubmitResponseActionRequest` 续跑并结算伤害。  
  证据：`RuleCore/CrescentWreath.RuleCore.Tests/UnitTest1.cs`、`RuleCore/CrescentWreath.RuleCore.Tests/ActionRequestProcessorResponseWindowDamageTests.cs`  
  用例：`ProcessPlayCardRequest_ScriptedOnPlayWaitResponseDamage_ShouldOpenWindowThenSubmitAndResolveDamageOnSameChain`、`OpenDamageResponseWindowThenSubmitResponse_ShouldContinueSameChainAndResolveDamage`
- 最小合法性防护已覆盖：`InputContext` 提交合法性、staged `ResponseWindow` 重入保护均有失败路径测试。  
  证据：`RuleCore/CrescentWreath.RuleCore.Tests/ActionRequestProcessorInputContextFlowTests.cs`、`RuleCore/CrescentWreath.RuleCore.Tests/ActionRequestProcessorResponseWindowDamageTests.cs`、`RuleCore/CrescentWreath.RuleCore.Tests/UnitTest1.cs`

## 2. 临时脚本/探针流（Temporary Scripted/Probe）

结论：当前存在一组“为骨架验证服务”的探针流，语义是临时的，不代表正式规则表达层已经完成。

- `definitionId` 探针卡：
  - `test:onPlayDeal1`
  - `test:onPlayChooseDamage`
  - `test:onPlayWaitResponseDamage`
- 独立探针请求：
  - `OpenDamageResponseWindowActionRequest`（用于 staged response + pending damage 验证）
- `SubmitResponseActionRequest` 当前行为限制：
  - 仅支持 no-response 续跑：`shouldRespond=false` 且 `responseKey=null`
- 目标选择策略：
  - 仍使用最小确定性选择（按当前测试假设），用于验证链路可续跑，不作为正式目标选择系统结论。

## 3. 与正式详细设计的已知偏差（Intentionally Postponed）

结论：当前延期项需要分成两类看待：一类是“正式设计已定义，但当前代码尚未收敛到该模型”；另一类是“正式规则广度尚未进入实现阶段”。

### 3.1 正式设计已定义，但当前代码尚未对齐

- `ResponseWindowState.originType`：详细设计已要求显式区分 `chain / flow` 来源；当前代码仍处于最小窗口模型，尚未正式落地该字段。
- 伤害正式模型：详细设计中的伤害链已包含 `damageType`、Barrier、防御、免伤替代、HpChange、Leyline、KillCheck 等完整检查点；当前代码仍是最小伤害 happy path 与 staged 续跑验证，不等于正式伤害链已落地。
- `ZoneKey` 与正式区域模型：正式设计与术语规范已冻结正式区域集合；当前代码仍保留若干历史/过渡性 zone 表达，尚未完成最终收口。
- HP / `leyline` / `killScore` 归属：正式设计已将角色生命与队伍资源归属写明；当前代码仍保留最小实现阶段的旧归属方式，尚未完成状态模型对齐。

### 3.2 正式规则广度尚未实现

- 尚未实现真实响应效果执行、多响应者顺序与优先级裁定。
- 尚未实现防御/免疫/替代/结界检查点/状态/击杀的完整联动链。
- 尚未引入通用效果系统与通用 continuation 承载；当前以最小闭环与脚本分支验证结构。
- 当前结果不宣称已解决多玩家同类区域完整模型，也不宣称已完成完整 Definition 驱动。

## 4. 下一阶段建议决策点（Decision Point）

决策主题：下一步是先“收敛时序骨架”，还是先“扩规则广度”。

- 推荐默认：先收敛时序骨架。  
  具体是把 on-play staged continuation 从脚本分支收敛为最小统一承载，再进入 M3 的规则广度扩展。
- 备选方向：继续沿 probe 流扩更多规则闭环。  
  优点是短期推进快；代价是后续统一收敛成本和技术债更高。

建议结论：**优先进入“时序骨架收敛”小阶段（一个可验证增量），以降低 M3 扩展期的结构风险。**
