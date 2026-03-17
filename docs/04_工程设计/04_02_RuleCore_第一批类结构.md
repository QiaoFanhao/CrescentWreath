# 04_02_RuleCore_第一批类结构

## 1. 文档目的

本文档用于定义《新月花冠》电子版项目中 `RuleCore` 第一批核心类的结构边界、职责划分与关系组织。

本文档的任务不是给出最终完整实现，也不是一次性确定所有规则细节，而是为 `RuleCore` 第一阶段开发提供一套可落地、可扩展、可被 `Server` 与 `UnityClient` 共同依赖的基础对象骨架。

本文档主要回答以下问题：

- `RuleCore` 第一批必须落地的核心类有哪些
- 这些类各自负责什么
- 它们之间如何关联
- 哪些字段应当属于某个对象
- 哪些职责不应放入该对象
- 哪些部分当前先保留为扩展位

本文档是 `04_01_RuleCore_总体架构.md` 的进一步细化。
若本文档与 `04_01` 的总体边界冲突，应优先回到总体架构层修正，而不是直接突破边界。

---

## 2. 第一批类结构的目标

第一批类结构的目标，不是实现完整游戏，而是建立以下能力：

- 能表达一局游戏的核心状态
- 能表达玩家、队伍、角色、卡牌、区域等正式对象
- 能接受正式动作请求
- 能开启基本结算链
- 能显式表达响应窗口与输入上下文
- 能显式表达伤害链和状态异常的基础容器
- 能为后续 `Server` / `UnityClient` 接入提供统一依赖基础

换句话说，第一批类结构解决的是：

> **“游戏规则开始在代码里存在”**
> 而不是
> **“所有牌和技能都已经实现”。**

---

## 3. 第一批类结构设计原则

### 3.1 状态根对象优先

必须先建立 `GameState` 及其直接挂载对象，而不是先写局部个例逻辑。

### 3.2 Definition / Instance 分离

凡是运行时对象，优先设计为 `Instance`。
静态配置对象（如 `Definition`）可以保留接口与占位，但第一阶段不要求全部展开。

### 3.3 显式中间态优先

动作链、响应窗口、输入上下文、伤害上下文等中间态必须显式建模，不应只存在于函数调用栈里。

### 3.4 单一职责优先

一个对象应尽量只负责一种核心语义，不应把多种层次混在一起。

### 3.5 先骨架后细化

第一批类先解决：

- “这个对象存在”
- “它和谁连接”
- “它负责什么”

而不是一次性塞满所有字段与实现细节。

---

## 4. 第一批类结构总览

当前阶段建议优先建立以下对象组：

### 4.1 标识与引用层

- `PlayerId`
- `TeamId`
- `CardInstanceId`
- `CharacterInstanceId`
- `ActionChainId`
- `ResponseWindowId`
- `InputContextId`
- `DamageContextId`

### 4.2 全局状态层

- `GameState`
- `PlayerState`
- `TeamState`

### 4.3 运行时实体层

- `CardInstance`
- `CharacterInstance`

### 4.4 区域层

- `ZoneState`
- `ZoneKey`
- `CardMoveReason`

### 4.5 动作入口层

- `ActionRequest`
- 第一批基本请求子类

### 4.6 结算链层

- `ActionChainState`
- `EffectFrame`

### 4.7 响应与输入层

- `ResponseWindowState`
- `InputContextState`

### 4.8 伤害与击杀层

- `DamageContext`
- `KillContext`

### 4.9 状态异常层

- `StatusInstance`

### 4.10 事件输出层

- `GameEvent`
- 第一批基础事件类型

---

## 5. 标识与引用层

第一批类结构中，建议优先建立一套显式 ID 类型，而不是直接在各处混用 `int` / `long` / `string`。

这样做的原因是：

- 降低不同 ID 混用的风险
- 提高代码可读性
- 便于后续日志、调试、序列化与跨层传递
- 让 `RuleCore` 对象关系更清晰

### 5.1 推荐对象

- `PlayerId`
- `TeamId`
- `CardInstanceId`
- `CharacterInstanceId`
- `ActionChainId`
- `ResponseWindowId`
- `InputContextId`
- `DamageContextId`

### 5.2 设计要求

这些对象应满足：

- 类型明确
- 不与其他 ID 混用
- 可被序列化
- 可用于字典键
- 可用于日志与事件引用

### 5.3 不负责的内容

ID 对象不负责：

- 保存业务逻辑
- 保存 UI 显示信息
- 替代完整实体对象

---

## 6. 全局状态层

### 6.1 GameState

`GameState` 是整个 `RuleCore` 的状态根对象，也是服务器权威状态的核心承载体。

#### 职责

- 挂载整局游戏的全局状态
- 挂载所有玩家与队伍状态
- 挂载所有运行时实体索引
- 挂载当前结算链、响应窗口、输入上下文
- 挂载当前回合、阶段、异变等全局上下文

#### 建议包含的内容

- 当前回合信息
- 当前阶段信息
- `players`
- `teams`
- `cardInstances`
- `characterInstances`
- 当前 `ActionChainState`
- 当前 `ResponseWindowState`
- 当前 `InputContextState`
- 当前异变运行时状态引用或占位

#### 不负责

- 自己完成所有规则结算
- 自己承担网络消息组织
- 自己承担 UI 视图组织

#### 说明

`GameState` 是“世界本身”，不是“操作世界的方法全集”。

---

### 6.2 PlayerState

`PlayerState` 表达某一名玩家在当前对局中的正式状态。

#### 职责

- 保存玩家的对局级状态
- 关联所属队伍
- 挂载玩家拥有的正式区域
- 保存玩家的生命、灵脉、击杀分等对局属性

#### 建议包含的内容

- `playerId`
- `teamId`
- `hp`
- `leyline`
- `killScore`
- 玩家控制的区域映射
- 必要的玩家局部标记

#### 不负责

- 保存所有卡牌实例本体
- 保存完整角色定义
- 决定全局时序

---

### 6.3 TeamState

`TeamState` 表达 2V2 对局中的队伍状态。

#### 职责

- 表达一个队伍的成员关系
- 挂载队伍级别的状态或共享信息（若未来需要）

#### 建议包含的内容

- `teamId`
- `memberPlayerIds`

#### 不负责

- 替代玩家状态
- 持有所有具体运行时对象

---

## 7. 运行时实体层

### 7.1 CardInstance

`CardInstance` 表达“这一局游戏中的这一张具体卡牌”。

#### 职责

- 作为运行时卡牌实例存在
- 关联其静态定义来源
- 保存当前区域、朝向、公开状态等运行时信息
- 挂载附着于此卡的运行时状态

#### 建议包含的内容

- `cardInstanceId`
- `definitionId`
- `ownerPlayerId`
- 当前所在区域引用
- 当前公开 / 盖放 / 置外等相关状态
- 挂载的 `StatusInstance` 列表或引用
- 必要的实例级临时标记

#### 不负责

- 持有完整定义文本本体
- 直接完成效果结算
- 代替区域管理自身位置合法性

#### 说明

`CardInstance` 表达的是“这一张牌现在是什么”，而不是“这张牌天生写了什么”。

---

### 7.2 CharacterInstance

`CharacterInstance` 表达“这一局中的某个角色实体”。

#### 职责

- 表达角色的运行时存在
- 挂载角色相关的状态、技能可用性、当前位置等信息
- 作为技能与效果的重要来源对象

#### 建议包含的内容

- `characterInstanceId`
- `definitionId`
- 所属玩家
- 当前存活/在场状态
- 当前挂载的状态异常
- 角色当前相关的运行时资源或标记

#### 不负责

- 替代 `PlayerState`
- 替代完整技能定义系统
- 独自控制全局规则流程

#### 说明

角色实例应与玩家状态区分。
玩家是参与者，角色是战场上的正式单位。

---

## 8. 区域层

### 8.1 ZoneKey

`ZoneKey` 用于表达正式规则区域的类型。

#### 第一批应优先覆盖的区域语义

- 牌库
- 手牌
- 弃牌堆
- 除外区
- 召唤区
- `DefensePlace`
- `DefenseLikePlace`
- `GapZone`
- `PublicTreasureDeck`
- `CharacterSetAside`

#### 职责

- 提供统一的区域枚举/键
- 作为 `ZoneState` 与移动语义的基础标识

#### 不负责

- 持有区域内容
- 持有区域 UI 布局信息

---

### 8.2 ZoneState

`ZoneState` 表达一个正式规则区域在当前对局中的运行时状态。

#### 职责

- 保存该区域当前包含的对象
- 提供区域顺序与位置语义基础
- 为卡牌移动提供正式落点

#### 建议包含的内容

- `zoneKey`
- 区域拥有者（若有）
- 当前对象 ID 列表
- 区域相关局部规则标记（若未来需要）

#### 不负责

- 替代对象实例本体
- 负责 UI 槽位排布

---

### 8.3 CardMoveReason

`CardMoveReason` 用于表达卡牌移动的规则语义。

#### 第一批建议包含

- `draw`
- `play`
- `summon`
- `defensePlace`
- `defenseLikePlace`
- `discard`
- `banish`
- `setAside`
- `reveal`
- `returnToSource`

#### 作用

它存在的意义，不是“记录数组移动”，而是保留：

> **为什么移动**

这对后续：

- 触发器
- 日志
- 回放
- 特殊判定
- FAQ 一致性

都非常重要。

---

## 9. 动作入口层

### 9.1 ActionRequest

`ActionRequest` 是一切正式动作进入 `RuleCore` 的统一入口基类。

#### 职责

- 表达某个玩家或系统发起了一个正式规则动作
- 作为 `ActionSystem` 的输入对象
- 与网络消息解耦，不直接等于协议 DTO

#### 建议包含的内容

- `actorPlayerId`
- `requestId`
- 请求来源信息（若需要）
- 请求时间或顺序信息（可选）

#### 不负责

- 直接保存完整结算结果
- 直接承担网络协议字段结构

---

### 9.2 第一批请求子类

第一阶段建议优先建立最基本的动作请求子类，例如：

- `PlayCardActionRequest`
- `ActivateSkillActionRequest`
- `DeclareDefenseActionRequest`
- `SubmitInputChoiceActionRequest`
- `SubmitResponseActionRequest`

#### 作用

第一批子类不一定要全部实现完整逻辑，但至少要建立对象形状与命名体系。

---

## 10. 结算链层

### 10.1 ActionChainState

`ActionChainState` 表达当前正在推进的一条正式动作链。

#### 职责

- 保存某个根动作当前对应的结算上下文
- 组织后续效果推进
- 记录当前链条中的关键事件与过程状态

#### 建议包含的内容

- `actionChainId`
- 根 `ActionRequest`
- 待处理 `EffectFrame` 队列
- 当前链条已产生的事件列表
- 当前链条局部标记

#### 不负责

- 代替全局 `GameState`
- 保存所有模块全部状态

#### 说明

`ActionChainState` 是“这次动作的生命体”，不是“整局游戏的世界”。

---

### 10.2 EffectFrame

`EffectFrame` 表达结算链中的一个具体推进单元。

#### 职责

- 表达某个待执行的效果片段
- 作为效果推进队列中的基本单位
- 连接来源对象、目标对象与效果标识

#### 建议包含的内容

- `effectKey`
- 来源对象引用
- 目标对象引用
- 必要的局部上下文

#### 不负责

- 直接替代完整效果表达系统
- 承担 UI 动画表现

#### 说明

第一阶段的 `EffectFrame` 先作为最小可用承载体存在，后续可再演化为更完整的效果节点系统。

---

## 11. 响应与输入层

### 11.1 ResponseWindowState

`ResponseWindowState` 表达一个正式的响应窗口。

#### 职责

- 表达“当前哪些玩家有机会响应”
- 记录当前窗口的类型与上下文
- 记录同一结算中已使用的响应限制信息

#### 建议包含的内容

- `responseWindowId`
- 窗口类型
- 可响应玩家列表
- 窗口来源链信息
- 同一结算中已使用响应技记录

#### 不负责

- 直接承担客户端弹窗表现
- 持有网络连接

#### 说明

这是规则中的窗口，不是 UI 里的窗口。

---

### 11.2 InputContextState

`InputContextState` 表达当前系统正在等待某位玩家完成进一步选择。

#### 职责

- 表达某个动作/效果未结算完毕，仍需进一步输入
- 记录当前由谁输入、输入什么、可选范围为何

#### 建议包含的内容

- `inputContextId`
- `requiredPlayerId`
- 输入类型
- 候选项集合
- 上下文来源链信息

#### 不负责

- 直接表现为客户端具体弹窗
- 替代规则链本体

#### 说明

例如：

- 目标选择
- 二选一
- 防御声明
- 特定状态要求的回合开始选择

都可以统一挂在 `InputContextState` 上。

---

## 12. 伤害与击杀层

### 12.1 DamageContext

`DamageContext` 表达一段正式伤害链在运行时的上下文。

#### 职责

- 表达伤害从“赋予”到“最终生效”的全过程容器
- 记录防御、替代、免疫、实际伤害落地等关键状态
- 为后续非标准伤害路径提供统一挂载点

#### 建议包含的内容

- `damageContextId`
- 来源对象
- 目标对象
- 初始伤害值
- 防御声明结果
- 是否被替代 / 免疫 / 防止
- 最终实际伤害值
- 是否成功造成伤害

#### 不负责

- 直接替代生命值对象
- 用一个简单扣血函数取代完整链路

#### 说明

`DamageContext` 存在的目的之一，就是保住这些差异：

- 给予伤害
- 实际受到伤害
- 生命变化
- 击杀归属

它们不能在实现中被偷懒揉成一团。

---

### 12.2 KillContext

`KillContext` 表达一次正式击杀关系。

#### 职责

- 表达谁击杀了谁
- 区分是否由伤害导致
- 为 `KillScore`、日志、结算后触发提供依据

#### 建议包含的内容

- 击杀者引用
- 被击杀者引用
- 是否由伤害造成
- 来源伤害上下文引用（若有）

#### 不负责

- 替代伤害链本体
- 直接承担击杀奖励发放

---

## 13. 状态异常层

### 13.1 StatusInstance

`StatusInstance` 表达一个正式状态异常或持续状态在运行时的存在。

#### 职责

- 统一承载各种正式状态
- 保存施加者、层数、持续信息、来源信息
- 避免状态逻辑分散成各处临时布尔值

#### 第一批应能覆盖的类型方向

- `Barrier`
- `Seal`
- `Shackle`
- `Silence`
- `Charm`
- `Penetrate`

#### 建议包含的内容

- `statusKey`
- 施加来源
- 层数
- 持续类型
- 剩余持续信息
- 附加参数（若后续需要）

#### 不负责

- 直接承担 UI 图标表现
- 散落在个例脚本中作为临时特判替代品

---

## 14. 事件输出层

### 14.1 GameEvent

`GameEvent` 是第一批基础规则事件的统一基类或统一抽象。

#### 职责

- 作为规则推进中关键节点的输出表达
- 为日志、调试、同步、回放提供统一基础

#### 第一批建议优先覆盖的事件方向

- 动作受理事件
- 卡牌移动事件
- 伤害事件
- 生命变化事件
- 击杀事件
- 状态施加/移除事件
- 响应窗口开启/关闭事件
- 输入上下文开启/关闭事件

#### 不负责

- 直接决定 UI 如何表现
- 直接作为网络消息发送格式

---

## 15. 第一批类之间的关系

当前阶段，建议将第一批对象之间的关系理解为以下结构：

```text
GameState
├─ PlayerState
├─ TeamState
├─ CardInstance
├─ CharacterInstance
├─ ZoneState
├─ current ActionChainState
├─ current ResponseWindowState
└─ current InputContextState

ActionRequest
  -> ActionChainState
      -> EffectFrame
      -> GameEvent
      -> DamageContext / KillContext
      -> StatusInstance
      -> ZoneState 变化
```

这套关系意味着：

* `GameState` 是根
* `ActionRequest` 是入口
* `ActionChainState` 是过程
* `ResponseWindowState` / `InputContextState` 是停顿点
* `DamageContext` / `KillContext` / `StatusInstance` 是专门规则容器
* `GameEvent` 是输出轨迹

---

## 16. 第一批类当前不追求的内容

为了避免第一阶段过度膨胀，以下内容可以暂不在本篇中定死：

* 完整 `Definition` 层字段设计
* 完整效果 DSL / 效果节点系统
* 完整异变运行时模型
* 叠放容器与盖放系统的全部细节
* 客户端最终视图对象结构
* 网络协议 DTO 具体形状
* 回放系统完整结构

这些将在后续文档中逐步补足。

---

## 17. 第一批类的落地建议

实现时，建议按以下顺序落地：

### 第一批第一层

* ID 类型
* `GameState`
* `PlayerState`
* `TeamState`
* `ZoneKey`
* `ZoneState`
* `CardInstance`
* `CharacterInstance`

### 第一批第二层

* `ActionRequest`
* `ActionChainState`
* `EffectFrame`
* `ResponseWindowState`
* `InputContextState`

### 第一批第三层

* `DamageContext`
* `KillContext`
* `StatusInstance`
* `GameEvent`

这样可以保证：

* 状态根先立住
* 结算过程再接上
* 专门规则容器后补上

---

## 18. 与后续文档的关系

本文档定义的是 `RuleCore` 第一批核心类的静态骨架。

后续文档应在本文档基础上继续细化：

* `04_03_RuleCore_状态流转与结算模型.md`
  * 负责定义这些类如何在一次正式请求中流动起来
* `Server` 相关文档
  * 负责定义 `Server` 如何调用这些类
* `UnityClient` 相关文档
  * 负责定义客户端如何引用和消费这些类

若后续需要新增对象，应优先判断其应当归入：

* 状态根层
* 实体层
* 结算链层
* 响应输入层
* 专门规则容器层

而不是随意新造一个职责混杂的“神对象”。

---

## 19. 总结

`RuleCore` 第一批类结构的核心目标，不是覆盖全部规则细节，而是建立一套可持续生长的规则骨架。

第一批对象可以概括为：

* 状态根：`GameState`
* 参与者：`PlayerState`、`TeamState`
* 运行时实体：`CardInstance`、`CharacterInstance`
* 位置语义：`ZoneState`、`ZoneKey`、`CardMoveReason`
* 动作入口：`ActionRequest`
* 过程链路：`ActionChainState`、`EffectFrame`
* 停顿机制：`ResponseWindowState`、`InputContextState`
* 专门规则容器：`DamageContext`、`KillContext`、`StatusInstance`
* 输出轨迹：`GameEvent`

这批对象共同构成了 RuleCore 第一阶段的“最低可生长骨架”。
后续所有具体规则、网络接入与客户端表现，都应在这套骨架之上继续生长，而不应绕开它另起一套结构。

