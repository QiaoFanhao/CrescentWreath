# docs/HANDOFF.md — RuleCore 开发交接文档

## 1. 项目概述

《新月花冠》电子版 — 东方Project × Type-Moon 2v2 卡牌桌游电子化。服务器权威架构：
- **RuleCore**：纯 C# 类库（netstandard2.1），规则引擎 ← 当前唯一在开发的部分
- **Server**：引用 RuleCore（未开始）
- **UnityClient**：引用 RuleCore DLL（未开始）

**代码规模**：约 31000 行（源码 ~13200 + 测试 ~18100），298 个测试。

## 2. 硬约束

- 只在 RuleCore 内工作，不碰 Server/UnityClient/docs
- netstandard2.1，不用 Unity API
- 命名跟术语规范表（`docs/` 下）
- 规则来源优先级：规则书/FAQ > 详设文档 > 工程文档 > 代码
- `dotnet build` 验证（沙箱 `dotnet test` 因 NuGet SSL 问题会失败，测试需本地跑）
- 先说计划再动手；发现设计冲突先停下报告

## 3. 已完成功能

### 对局初始化
GameInitializer.createStandard2v2MatchState() — 4玩家/2队伍/MatchMeta/25个zone/每人10张初始牌抽6张/killScore=10/leyline=0

### 回合流程
start→action→summon→end→nextTurn。EnterActionPhase给skillPoint=1；EnterSummonPhase锁定lockedSigil；EnterEndPhase清场+弃牌到6+补牌到6+清零资源；StartNextTurn座次轮转+防御牌回手+Shackle弃牌处理。

### 资源生命周期
mana（打牌累加/结束清零）、sigilPreview（打牌累加/summon锁定后清零）、lockedSigil（summon锁定/召唤扣除/结束清空）、skillPoint（action开始=1/技能扣除）。

### 伤害/防御/击杀
完整伤害链：Barrier检查→Penetrate→damageType vs defenseType匹配→防御值结算→灵脉+1→击杀检查→替代效应→killScore→奖励抓牌→回满血。Charm禁防/Silence禁响应。直接击杀路径。防御/反击响应窗口。Seal→防御值-1。

### 状态异常
StatusRuntime全套：Barrier/Seal/Shackle/Silence/Charm/Penetrate。Shackle回合开始强制弃4。

### Definition数据层
TreasureDefinition（已接入：starter牌+T016）、CharacterDefinition（已接入：C004）、AnomalyDefinition（已接入：A001-A003/A005-A009共8张）。A004/A010占位。

### 异变系统（项目最大子系统，~16000行）
三层结构：降临(Arrival)→条件(Condition)→奖励(Reward)。AnomalyProcessor(1867行)顶层编排，14种continuation。

**已实现降临**：A003(选对手施加Shackle+幽香队灵脉)、A005(免费召唤)、A006(领先队Seal/C009豁免)、A007(可选放逐/C007额外)、A008→A009(公共宝具放逐到间隙)

**已实现条件**：A001(mana+HP费用)、A002(友方弃牌)、A003(mana)、A005(DefenseLikePlace式弃牌)、A008(对手可选回收)、A009(mana+对手可选结界/樱花饼)

**已实现奖励**：leyline/killScore变化、状态施加、A002(可选放逐+樱花饼替换)、A005(选牌到手)、A006(治疗非人类)、A008(两仪式可选抓牌)、A009(间隙区选牌到手)

## 4. 关键文件

| 文件 | 行数 | 职责 |
|------|------|------|
| AnomalyProcessor.cs | 1867 | 异变顶层编排 |
| ActionRequestProcessor.cs | 1802 | 请求分发+打牌/召唤/抽牌/技能/伤害响应 |
| AnomalyConditionInputRuntime.cs | 1403 | 异变条件输入 |
| AnomalyRewardInputRuntime.cs | 1167 | 异变奖励输入 |
| AnomalyArrivalInputRuntime.cs | 1037 | 异变降临输入 |
| DamageProcessor.cs | 623 | 伤害链全流程 |
| TurnTransitionProcessor.cs | 593 | 阶段切换/回合轮转/Shackle |
| AnomalyRewardExecutor.cs | 527 | 异变奖励执行 |
| EndPhaseProcessor.cs | 455 | 结束阶段 |
| StatusRuntime.cs | 362 | 状态系统 |
| GameInitializer.cs | 309 | 开局初始化 |

## 5. 已知缺口

### P1 横向补齐（建议优先）
- T001-T029 宝具定义未全部接入（InMemoryTreasureDefinitionSource 只有 starter+T016）
- 角色定义只有 C004，需扩展
- ActionRequestProcessor 残留 ~300 行 scripted 测试卡逻辑待清理

### P2 异变收尾
- A004（永夜）、A010（命运之夜）仍为占位

### P3 系统级
- maxLeyline / 回合开始响应窗口 / Overlay完整路径 / 指示物 / CardDefinition完整层

## 6. 里程碑
M1✅ M2✅ M2.7+✅ → M3进行中 → M4/M5未开始

## 7. 建议后续优先级
1. T001-T029宝具定义接入
2. 更多角色CharacterDefinition
3. 清理scripted测试卡
4. A004/A010异变收尾
5. Server骨架
