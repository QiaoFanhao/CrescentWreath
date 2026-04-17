# CLAUDE.md

## 项目概述
《新月花冠》电子版 — 东方Project × Type-Moon 2v2 卡牌桌游电子化。纯 C# 规则引擎（netstandard2.1）。当前只开发 RuleCore。

## 硬约束
1. 只在 RuleCore 内工作，不碰 Server/UnityClient/docs
2. netstandard2.1，不用 Unity API / .NET 6+ 独有 API
3. 命名跟术语规范表（`docs/` 下）：类型名 PascalCase，字段名 camelCase，枚举值 camelCase
4. 规则来源：规则书/FAQ > 详设文档 > 工程文档 > 代码
5. `dotnet build` 验证（`dotnet test` 在沙箱因 NuGet SSL 失败，测试本地跑）
6. 先说计划再动手；设计冲突先停下报告
7. 禁止静默改规则 / 禁止硬编码个例污染骨架 / 禁止假实现冒充完成

## 路径索引
| 内容 | 位置 |
|------|------|
| 源码 | `RuleCore/CrescentWreath.RuleCore/` |
| 测试 | `RuleCore/CrescentWreath.RuleCore.Tests/`（仅修改对应功能时阅读） |
| 术语规范表 | `docs/` 下 |
| 详设文档(12篇+补充A) | `docs/03_详细设计文档/` |
| 工程文档 | `docs/04_工程设计/` |
| 规则书/FAQ | `docs/01_规则书与FAQ/` |
| **完整交接文档** | `docs/HANDOFF.md` |

## 当前状态
约 31000 行代码，298 个测试。M2.7+ 完成，M3 进行中。

**已完成**：对局初始化、回合流程(start→action→summon→end→nextTurn)、资源生命周期(mana/sigil/skillPoint)、打牌/召唤/抽牌、完整伤害链(Barrier/Penetrate/damageType匹配/灵脉/击杀/替代效应/回满血)、状态异常全套(Barrier/Seal/Shackle/Silence/Charm/Penetrate + Shackle回合开始弃牌)、Charm禁防/Silence禁响应、Definition数据层、异变系统三层(降临/条件/奖励，8张异变正式实现)。

**已知缺口**：T001-T029宝具定义未全部接入 / 角色只有C004 / A004+A010占位 / scripted测试卡残留 / maxLeyline / Overlay完整路径 / 指示物系统

## 术语速查
区域：PublicTreasureDeck / AnomalyDeck / SakuraCakeDeck / SummonZone / GapZone / Deck / Hand / Discard / Field / CharacterSetAside / OverlayContainer
动作：Play / Summon / DefensePlace / DefenseLikePlace / Banish / SetAside / Overlay / ReturnToSource
资源：mana / sigilPreview / lockedSigil / skillPoint / leyline / killScore
状态：Barrier / Seal / Shackle / Silence / Charm / Penetrate
