# 04_01_RuleCore_总体架构

## 1. 文档目的

本文档用于定义《新月花冠》电子版项目中 `RuleCore` 的总体工程架构。

`RuleCore` 是整个项目的规则核心类库，其职责不是提供 UI、网络或 Unity 场景能力，而是将游戏规则以显式、可执行、可扩展的方式建模为纯 C# 运行时系统。

本文档的目标是回答以下问题：

- `RuleCore` 的定位是什么
- `RuleCore` 应包含哪些模块
- 各模块的职责边界是什么
- 模块之间如何协作
- 哪些内容应进入 `RuleCore`
- 哪些内容不应进入 `RuleCore`
- 后续第一批类结构设计应在怎样的边界内展开

本文档不负责给出所有类的最终字段细节。
具体核心类与第一批对象结构，将在 `04_02_RuleCore_第一批类结构.md` 中展开。

---

## 2. RuleCore 的定位

`RuleCore` 是《新月花冠》电子版项目的**规则真相核心**。

它的本质是一个纯 C# 类库，用于：

- 建模对局中的权威状态
- 接收动作请求
- 按规则推进时序链与结算链
- 输出规则事件、状态变化与后续输入需求
- 为服务器提供正式裁定能力
- 为客户端提供本地预演、验证辅助与单机测试能力

### 2.1 RuleCore 的核心特征

`RuleCore` 应具备以下特征：

- 纯 C# 实现
- 不依赖 Unity API
- 不依赖 MonoBehaviour 生命周期
- 不依赖具体网络框架
- 使用显式状态对象表达规则真相
- 使用显式链路对象表达结算过程
- 支持后续序列化、快照、恢复与测试

### 2.2 RuleCore 在整体工程中的位置

整体依赖方向如下：

```text
RuleCore <- Server
RuleCore <- UnityClient
```

即：

* `Server` 引用 `RuleCore` 进行正式裁定
* `UnityClient` 可引用 `RuleCore` 进行本地辅助与预演
* `RuleCore` 本身不依赖 `Server`
* `RuleCore` 本身不依赖 `UnityClient`

---

## 3. RuleCore 的职责范围

### 3.1 RuleCore 负责的内容

`RuleCore` 负责以下内容：

#### （1）对局状态建模

包括但不限于：

* `GameState`
* `PlayerState`
* `TeamState`
* `CharacterInstance`
* `CardInstance`
* `ZoneState`
* 当前异变运行时状态
* 当前响应窗口
* 当前输入上下文
* 当前结算链

#### （2）规则请求建模

包括但不限于：

* 打牌请求
* 技能发动请求
* 防御声明请求
* 输入选择请求
* 响应声明请求

#### （3）规则链与结算链推进

包括但不限于：

* `ActionChain`
* `EffectChain`
* `ResponseWindow`
* `InputContext`
* 时序事件派发
* 伤害链推进
* 状态变化落地

#### （4）核心规则容器

包括但不限于：

* 区域与卡牌移动
* 伤害 / 防御 / 生命变化 / 击杀
* 状态异常
* 角色技能入口
* 宝具进入与离场语义
* 异变运行时状态

#### （5）规则执行结果输出

包括但不限于：

* 更新后的 `GameState`
* 事件列表
* 新开启的响应窗口
* 新开启的输入上下文
* 日志所需的规则事件数据

---

### 3.2 RuleCore 不负责的内容

`RuleCore` 不应负责以下内容：

#### （1）UI

包括但不限于：

* 按钮点击
* 面板开关
* 卡牌高亮表现
* 特效与动画播放
* 日志面板呈现样式

#### （2）Unity 运行时表现

包括但不限于：

* MonoBehaviour 生命周期
* Scene 管理
* Prefab 实例化
* 拖拽交互
* 资源加载

#### （3）网络连接

包括但不限于：

* Socket / WebSocket / HTTP 连接
* 消息收发
* 房间通信
* 玩家会话管理

#### （4）房间与账号逻辑

包括但不限于：

* 匹配
* 房间创建与销毁
* 玩家上线 / 下线
* 断线重连会话恢复策略本身

#### （5）最终可见性裁剪

`RuleCore` 可以提供状态与事件基础，但“每个客户端最终看见什么”通常应在 `Server` 层结合玩家身份裁剪后下发。
`RuleCore` 不应直接耦合某一套具体网络协议或客户端视图结构。

---

## 4. RuleCore 的总体设计原则

### 4.1 显式状态对象优先

规则真相必须通过显式状态对象表达，而不是散落在临时变量、局部流程或 UI 脚本中。

例如：

* 角色当前是否存在于场上，应体现在实例或区域状态中
* 当前是否存在响应窗口，应体现在 `ResponseWindow` 状态对象中
* 当前是否存在等待玩家处理的输入，应体现在 `InputContext` 中
* 当前伤害结算应通过 `DamageContext` 表达，而不是只在某段函数栈里短暂存在

---

### 4.2 规则链显式建模

《新月花冠》不是一个“按钮按下立即结算完毕”的简单系统。
因此，动作与效果的推进过程必须显式建模。

必须显式存在的链路包括但不限于：

* 动作进入系统的入口链
* 效果依次推进的结算链
* 可被打断或响应的窗口对象
* 需要进一步选择的输入上下文
* 伤害从赋予到最终生效的链式过程

RuleCore 不能仅依赖一串线性函数把结果直接算完，而必须保留中间态。

---

### 4.3 Definition / Instance 分离

所有规则对象应尽量区分：

* ​**Definition**​：静态定义、配置、文本来源
* ​**Instance**​：运行时实例、对局中的实际存在

例如：

* `CardDefinition` 与 `CardInstance`
* `CharacterDefinition` 与 `CharacterInstance`
* `StatusDefinition` 与 `StatusInstance`
* `AnomalyDefinition` 与 `currentAnomalyState`

这样做的原因是：

* 配置数据与运行时状态职责不同
* 运行时状态需要持有动态字段
* 后续同步、保存、日志、调试都更清晰
* 客户端展示与服务器裁定能共享同一份定义层

---

### 4.4 通用骨架优先于特例硬编码

RuleCore 首先要建立的是：

* 状态容器
* 结算入口
* 通用规则链
* 统一状态系统
* 统一区域系统

而不是先把某张牌或某个角色的特效硬塞进去。

任何个例效果，都应尽量挂靠在通用框架上，例如：

* 伤害替代效果挂在伤害链检查点
* 状态异常挂在统一状态容器
* 特殊输入选择挂在 `InputContext`
* 响应技挂在 `ResponseWindow`

---

### 4.5 中间态可保存、可恢复

由于项目是联机卡牌游戏，且存在响应窗口、输入中断与复杂结算链，RuleCore 设计时必须考虑：

* 结算进行中断线恢复
* 响应窗口中的等待恢复
* 输入上下文未完成恢复
* 复杂伤害链中间状态重建
* 调试回放与日志追踪

因此关键状态对象应朝“可序列化 / 可重建”的方向设计。

---

### 4.6 规则层与协议层解耦

RuleCore 应关注：

* 规则对象
* 规则推进
* 规则事件

而不是：

* 某条网络消息字段叫什么
* 某个 UI 面板如何显示
* 某个客户端 DTO 如何组织

RuleCore 输出的应是规则意义明确的对象与事件，由 `Server` 或 `UnityClient` 再转换为各自使用的数据格式。

---

## 5. RuleCore 的模块划分

当前阶段，建议将 `RuleCore` 划分为以下主要模块。

RuleCore
├─ Definitions
├─ GameState
├─ Entities
├─ Zones
├─ ActionSystem
├─ EffectSystem
├─ ResponseSystem
├─ DamageSystem
├─ StatusSystem
└─ Events

以下为各模块说明。

### 5.1 Definitions

负责定义层对象。

包括但不限于：

* `CardDefinition`
* `CharacterDefinition`
* `SkillDefinition`
* `TreasureDefinition`
* `AnomalyDefinition`
* `StatusDefinition`
* 效果节点定义

职责：

* 保存静态配置与规则定义信息
* 为运行时实例提供来源
* 作为客户端与服务器共享的规则静态数据来源

不负责：

* 保存运行时血量、区域、状态层数等动态字段
* 直接推进结算

---

### 5.2 GameState

负责全局对局状态根对象及其聚合关系。

包括但不限于：

* `GameState`
* `PlayerState`
* `TeamState`
* 当前回合信息
* 当前阶段信息
* 当前异变运行时状态
* 当前动作链 / 响应窗口 / 输入上下文引用

职责：

* 作为规则执行的唯一状态根
* 挂载全局状态对象
* 为后续序列化与同步提供核心结构

不负责：

* 自己执行所有业务逻辑
* 直接承担 UI 或网络职责

---

### 5.3 Entities

负责运行时实体对象。

包括但不限于：

* `CardInstance`
* `CharacterInstance`
* 未来可能的叠放容器实例
* 其他需要在对局中拥有唯一身份的对象

职责：

* 表达“这一局中的这个具体对象”
* 持有动态运行时状态
* 作为规则链与日志中的对象引用目标

不负责：

* 自己决定全局时序
* 直接发送网络消息

---

### 5.4 Zones

负责区域与对象位置语义。

包括但不限于：

* `ZoneState`
* 区域枚举与区域键
* 卡牌移动原因
* 区域进入 / 离开语义
* 区域合法性约束

职责：

* 表达对象当前处于哪个正式区域
* 保留移动的规则语义
* 为打出、召唤、防御放置、放逐、置外等行为提供统一承载

不负责：

* 用 UI 槽位逻辑替代规则区域逻辑

---

### 5.5 ActionSystem

负责动作请求进入系统的入口。

包括但不限于：

* `ActionRequest`
* 请求校验入口
* 动作受理与拒绝结果
* 动作转化为规则链的起点

职责：

* 接收玩家或系统发起的规则动作
* 将动作转换为规则系统中的正式输入
* 作为一切主动行为进入 RuleCore 的总入口

不负责：

* 承担完整效果结算细节
* 直接兼任网络 DTO

---

### 5.6 EffectSystem

负责效果推进与结算帧组织。

包括但不限于：

* `ActionChain`
* `EffectChain`
* `EffectFrame`
* 效果节点运行时上下文
* 效果继续 / 挂起 / 结束的管理

职责：

* 组织动作进入后的实际效果推进
* 管理效果帧队列与结算顺序
* 为响应窗口与输入上下文让出控制权

不负责：

* 把所有具体规则都硬写成一个巨型函数

---

### 5.7 ResponseSystem

负责所有响应窗口与中断式规则交互。

包括但不限于：

* `ResponseWindow`
* `InputContext`
* 响应资格记录
* 同一结算内响应次数限制
* 玩家等待状态

职责：

* 显式表达“当前哪些人可以响应”
* 显式表达“当前必须由谁做进一步选择”
* 支撑复杂结算中的停顿与继续

不负责：

* 替代 UI 层的弹窗表现
* 自己持有网络连接

---

### 5.8 DamageSystem

负责伤害相关规则链。

包括但不限于：

* `DamageContext`
* 防御声明结果
* 伤害替代与免疫
* 生命变化
* 击杀归属
* `KillContext`

职责：

* 将“给予伤害”“实际受到伤害”“生命变化”“击杀”明确区分
* 统一承载标准伤害链
* 为后续非标准伤害替代效果提供挂载点

不负责：

* 用简单扣数值取代完整伤害链

---

### 5.9 StatusSystem

负责所有正式状态异常与持续状态。

包括但不限于：

* `StatusInstance`
* 状态施加
* 状态持续
* 状态清除
* 状态触发条件与生命周期

职责：

* 统一承载 `Barrier`、`Seal`、`Shackle`、`Silence`、`Charm`、`Penetrate` 等状态
* 避免状态规则分散在各模块中以临时布尔值存在
* 为后续扩展更多状态提供统一容器

不负责：

* 将状态逻辑散写进 UI 或牌面个例代码中

---

### 5.10 Events

负责规则事件输出。

包括但不限于：

* 游戏事件基础类型
* 伤害发生事件
* 卡牌移动事件
* 状态施加事件
* 击杀事件
* 响应窗口开启 / 关闭事件

职责：

* 为日志、调试、同步、回放提供统一事件来源
* 将规则推进过程中发生的关键节点显式化

不负责：

* 决定 UI 具体如何播放这些事件

---

## 6. 模块之间的协作关系

RuleCore 的总体协作关系可以概括为：

ActionRequest
-> ActionSystem
-> EffectSystem
-> ResponseSystem / DamageSystem / StatusSystem / Zones
-> GameState 更新
-> Events 输出

### 6.1 基本流程说明

#### 第一步：动作进入

玩家或系统通过 `ActionRequest` 提交一个正式动作请求。

#### 第二步：动作受理

`ActionSystem` 负责识别动作类型，并将其转化为正式规则链的起点。

#### 第三步：效果推进

`EffectSystem` 负责按顺序推进效果帧。

#### 第四步：必要时分流到专门模块

例如：

* 区域移动交给 `Zones`
* 伤害处理交给 `DamageSystem`
* 状态施加交给 `StatusSystem`
* 响应停顿交给 `ResponseSystem`

#### 第五步：更新全局状态

所有正式结果最终应落回 `GameState` 及其挂载的实体与区域对象中。

#### 第六步：输出规则事件

关键节点通过 `Events` 模块输出，为日志、调试与后续同步提供依据。

---

## 7. RuleCore 的推荐目录结构

以下目录结构为当前推荐方案，可根据实现需要微调，但整体边界应保持一致。

RuleCore/
Definitions/
GameState/
Entities/
Zones/
ActionSystem/
EffectSystem/
ResponseSystem/
DamageSystem/
StatusSystem/
Events/

若后续需要，可再增加：

* `Utilities/`
* `Validation/`
* `Serialization/`
* `TestingHelpers/`

但在初期阶段，不建议为了“看起来完整”过早拆出过多目录。

---

## 8. RuleCore 当前阶段的实现重点

当前阶段的 RuleCore 建设重点，不是实现所有规则细节，而是建立可生长的骨架。

优先级如下：

### 第一优先级

* `GameState` 根结构
* `PlayerState` / `TeamState`
* `CardInstance` / `CharacterInstance`
* `ZoneState`
* `ActionRequest`
* `ActionChain`
* `ResponseWindow`
* `InputContext`

### 第二优先级

* `DamageContext`
* `KillContext`
* `StatusInstance`
* 统一事件基础结构

### 第三优先级

* 定义层系统
* 复杂效果节点承载
* 异变完整运行时支持
* 更复杂的叠放与盖放系统

---

## 9. 当前阶段暂不解决的问题

在 RuleCore 总体架构阶段，以下问题可以暂不最终定死：

* 客户端最终视图 DTO 的具体形状
* 网络消息字段与协议实现细节
* Unity 中如何映射 UI 动画
* 完整日志展示格式
* 所有特例牌效果的完整表达方式
* 完整回放系统方案

这些问题后续会在：

* `Server`
* `UnityClient`
* 协议与同步设计
* 效果表达系统设计

中逐步展开。

---

## 10. 与后续文档的关系

本文档定义的是 `RuleCore` 的总体架构与模块边界。

后续文档应在本文档基础上展开：

* `04_02_RuleCore_第一批类结构.md`
  * 负责细化第一批核心类与对象关系
* `04_03_RuleCore_状态流转与结算模型.md`
  * 负责细化动作进入、结算推进、响应停顿与状态更新流程
* 后续 Server / UnityClient 文档
  * 应遵守本文档定义的 RuleCore 边界，不得反向污染

若后续文档与本文档冲突，应优先检查是否破坏了总体架构原则，再决定修订方向。

---

## 11. 总结

`RuleCore` 是《新月花冠》电子版项目的规则真相核心。
它既不是服务器工程，也不是 Unity 工程，而是一套独立、纯净、可扩展的规则运行时系统。

它的职责可以概括为：

* 保存正式对局状态
* 接收正式动作请求
* 推进正式规则链
* 输出正式规则结果

其总体模块划分为：

* `Definitions`
* `GameState`
* `Entities`
* `Zones`
* `ActionSystem`
* `EffectSystem`
* `ResponseSystem`
* `DamageSystem`
* `StatusSystem`
* `Events`

后续所有第一批类设计与状态流转设计，都应在这一总体架构之下继续细化，而不应偏离为 UI、网络或个例脚本驱动的实现方式。

