using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.Client.Net;

namespace CrescentWreath.Client.UI
{
public sealed class InputContextDescriptionViewModel
{
    public string title = string.Empty;
    public string detail = string.Empty;
    public string waitingText = string.Empty;
    public string submittedText = string.Empty;
}

public static class InputContextDescriptionResolver
{
    public static InputContextDescriptionViewModel Resolve(ProjectionViewModel projection)
    {
        var interaction = projection.interaction;
        var context = interaction.contextKey ?? string.Empty;
        var inputType = interaction.inputTypeKey ?? string.Empty;
        var requiredPlayers = formatRequiredPlayers(projection);
        var submittedText = $"已提交，等待其他玩家完成。{interaction.inputSubmittedPlayerCount}/{interaction.inputRequiredPlayerCount}";

        if (contains(context, inputType, "c001Barrier"))
        {
            return create(
                "博丽大结界",
                "【博丽灵梦】的响应技【博丽大结界】触发：可以弃置1张防御值3或更高的牌，给予当前回合玩家【结界】。",
                $"等待 {requiredPlayers} 选择是否发动【博丽大结界】。",
                submittedText);
        }

        if (contains(context, inputType, "turnStartC013MechanicalJadeChoice", "turnStart:c013MechanicalJade"))
        {
            return create(
                "巨大人形兵器原形",
                "【巫净翡翠】的响应技【巨大人形兵器原形】触发：可以弃置2张手牌，将召唤区的【机械翡翠】置入你的弃牌区。",
                $"等待 {requiredPlayers} 选择是否发动【巨大人形兵器原形】。",
                submittedText);
        }

        if (contains(context, inputType, "C013:2", "discardForDefenseValue"))
        {
            return create(
                "异常的味觉",
                "【巫净翡翠】的响应技【异常的味觉】触发：可以弃置1张手牌并支付II灵脉，用弃牌的防御值替换本次防御值。",
                $"等待 {requiredPlayers} 选择是否发动【异常的味觉】。",
                submittedText);
        }

        if (contains(context, inputType, "character:C003:2", "gungnirDiscard"))
        {
            return create(
                "冈格尼尔",
                "【蕾米莉亚】的响应技【冈格尼尔】触发：你或操控芙兰朵露的队友即将给予对手伤害，可以弃置1张召唤费用3或更高的手牌，使本次伤害获得【穿透】。",
                $"等待 {requiredPlayers} 选择是否弃牌让本次伤害获得【穿透】。",
                submittedText);
        }

        if (contains(context, inputType, "character:C005:2:sixRealmsChoice"))
        {
            return create(
                "六根清净斩",
                "【魂魄妖梦】完全防御后，可以移除1个剑气，展示牌库顶牌并对伤害来源角色反击。",
                $"等待 {requiredPlayers} 选择是否发动【六根清净斩】。",
                submittedText);
        }

        if (contains(context, inputType, "character:C031:2:leyline", "character:C031:leylineAmount"))
        {
            return create(
                "无法到达远野君处之拳",
                "【弓冢五月】使用【无法到达远野君处之拳】：选择消耗X点灵脉，之后给予目标对手3+X点体术伤害。",
                $"等待 {requiredPlayers} 选择要消耗的灵脉数量。",
                submittedText);
        }

        if (contains(context, inputType, "character:C031:2:targetOpponent"))
        {
            return create(
                "无法到达远野君处之拳",
                "【弓冢五月】使用【无法到达远野君处之拳】：请选择目标对手，造成3+X点体术伤害。",
                $"等待 {requiredPlayers} 选择【无法到达远野君处之拳】的目标。",
                submittedText);
        }

        if (contains(context, inputType, "character:C031:3:declareCardName"))
        {
            return create(
                "小巷子同盟：宣言卡名",
                "【弓冢五月】使用【小巷子同盟】：宣言一个牌名，翻开目标对手牌库顶1张牌；若命中则获得1灵脉，否则双方各抓1张牌。",
                $"等待 {requiredPlayers} 宣言卡名。",
                submittedText);
        }

        if (contains(context, inputType, "character:C031:3:targetOpponentDeck"))
        {
            return create(
                "小巷子同盟：选择目标",
                "【弓冢五月】使用【小巷子同盟】：请选择牌库非空的目标对手，翻开其牌库顶牌进行比对。",
                $"等待 {requiredPlayers} 选择【小巷子同盟】的目标对手。",
                submittedText);
        }

        if (contains(context, inputType, "character:C022:2:defenseCard", "character:C022:defenseCard"))
        {
            return create(
                "归于虚无",
                "【爱丽丝】使用【归于虚无】：选择目标对手阵地区一张防御牌或防御式放置牌，将其置入弃牌区，并对其队友造成【4-该牌召唤费用】点咒术伤害。",
                $"等待 {requiredPlayers} 选择【归于虚无】要处理的防御牌。",
                submittedText);
        }

        if (contains(context, inputType, "character:C022:3:targetOpponent"))
        {
            return create(
                "大千枪",
                "【爱丽丝】使用【大千枪】：已经抓2张牌并公开展示，请选择目标对手；将造成展示牌防御值之和-2的体术伤害，对帕秋莉伤害+1。",
                $"等待 {requiredPlayers} 选择【大千枪】的目标。",
                submittedText);
        }

        if (contains(context, inputType, "character:C022:4:selectDoll"))
        {
            return create(
                "人形战争：选择X",
                "【爱丽丝】使用【人形战争】：选择要移除的人形指示物数量X。",
                $"等待 {requiredPlayers} 选择【人形战争】的X值。",
                submittedText);
        }

        if (contains(context, inputType, "character:C022:4:targetOpponent"))
        {
            return create(
                "人形战争：选择目标",
                "【爱丽丝】使用【人形战争】：请选择目标对手。之后目标需要选择X项效果。",
                $"等待 {requiredPlayers} 选择【人形战争】的目标对手。",
                submittedText);
        }

        if (contains(context, inputType, "character:C022:4:targetOptions"))
        {
            return create(
                "人形战争：目标选择",
                "【爱丽丝】的【人形战争】要求你选择X项：爱丽丝抓X张牌、你弃X张牌、你受到X点直接伤害、你的队伍扣除X点灵脉。",
                $"等待 {requiredPlayers} 选择【人形战争】的效果项。",
                submittedText);
        }

        if (contains(context, inputType, "character:C022:4:targetDiscard"))
        {
            return create(
                "人形战争：弃牌",
                "【人形战争】正在处理目标弃牌效果：请选择要弃置的手牌。",
                $"等待 {requiredPlayers} 选择【人形战争】要弃置的手牌。",
                submittedText);
        }

        if (contains(context, inputType, "treasureOnPlay:T010", "T010"))
        {
            var isMoveToHand = contains(context, inputType, "OptionalMoveSelectedToHand", "optionalMoveSelectedToHand");
            var isKillOverlay = contains(context, inputType, "kill", "Kill", "setAside", "SetAside", "overlay", "Overlay");
            return create(
                "伊吹瓢",
                isKillOverlay
                    ? "【伊吹瓢】击杀相关效果触发：请选择由自己或队友的角色处理盖放牌。"
                    : isMoveToHand
                    ? "【伊吹瓢】效果：刚放到牌库顶的牌召唤费用小于5。是否将该牌加入手牌？"
                    : "【伊吹瓢】效果：请选择自己弃牌区的一张牌，将其移动到牌库顶。",
                isKillOverlay
                    ? $"等待 {requiredPlayers} 选择【伊吹瓢】击杀触发的处理角色。"
                    : isMoveToHand
                    ? $"等待 {requiredPlayers} 选择是否将牌库顶牌加入手牌。"
                    : $"等待 {requiredPlayers} 选择要移到牌库顶的弃牌。",
                submittedText);
        }

        if (contains(context, inputType, "treasureOnPlay:T022", "T022"))
        {
            return create(
                "阿鲁猫型火箭",
                "【阿鲁猫型火箭】效果：你可以从手牌或弃牌区放逐1张牌；结算后给予自己【禁锢】。",
                $"等待 {requiredPlayers} 选择【阿鲁猫型火箭】要放逐的牌。",
                submittedText);
        }

        if (contains(context, inputType, "treasureOnPlay:T003", "T003"))
        {
            var isBanish = contains(context, inputType, "OptionalBanish", "optionalBanish");
            return create(
                "绯想之剑",
                isBanish
                    ? "【绯想之剑】效果：可以从手牌或弃牌区放逐1张牌；之后选择目标对手造成2点体术伤害。"
                    : "【绯想之剑】效果：请选择要造成2点体术伤害的目标对手。",
                isBanish
                    ? $"等待 {requiredPlayers} 选择【绯想之剑】要放逐的牌。"
                    : $"等待 {requiredPlayers} 选择【绯想之剑】的伤害目标。",
                submittedText);
        }

        if (contains(context, inputType, "treasureOnPlay:T004", "T004"))
        {
            var isRemoveStatus = contains(context, inputType, "RemoveShackleOrSeal", "removeShackleOrSeal");
            return create(
                "楼观剑",
                isRemoveStatus
                    ? "【楼观剑】效果：可以选择一名玩家，解除其角色的【封印】或【禁锢】；之后选择目标对手造成3点体术伤害。"
                    : "【楼观剑】效果：请选择要造成3点体术伤害的目标对手。",
                isRemoveStatus
                    ? $"等待 {requiredPlayers} 选择【楼观剑】要解除状态的目标。"
                    : $"等待 {requiredPlayers} 选择【楼观剑】的伤害目标。",
                submittedText);
        }

        if (contains(context, inputType, "treasureOnPlay:T027", "T027"))
        {
            return create(
                "蕾咪的威严",
                "【蕾咪的威严】效果：抓2张牌后，选择要弃置的手牌。",
                $"等待 {requiredPlayers} 选择【蕾咪的威严】要弃置的手牌。",
                submittedText);
        }

        if (contains(context, inputType, "treasureDefense:T025", "T025", "extraDiscard"))
        {
            return create(
                "正式外典Gamaliel",
                "选择弃掉用于增加防御值的牌，每弃置一张该牌的防御值+1。",
                $"等待 {requiredPlayers} 选择【正式外典Gamaliel】要弃掉用于增加防御值的牌。",
                submittedText);
        }

        if (contains(context, inputType, "treasureOnPlay:T018", "T018"))
        {
            return create(
                "九字兼定",
                "【九字兼定】效果：获得1技能点后，可以选择手牌或弃牌区的1张牌放逐；也可以选择不处理。",
                $"等待 {requiredPlayers} 选择【九字兼定】要放逐的牌。",
                submittedText);
        }

        if (contains(context, inputType, "treasureOnPlay:T013", "T013"))
        {
            var isCharmTarget = contains(context, inputType, "onPlayTargetOpponentCharm");
            var isDeclare = contains(context, inputType, "declareCardName");
            return create(
                isDeclare ? "圣骸布：宣言卡名" : "圣骸布",
                isDeclare
                    ? "使用【圣骸布】：请选择要宣言的卡名。若翻开的牌与宣言一致，后续可以选择目标对手给予【魅惑】。"
                    : isCharmTarget
                        ? "【圣骸布】宣言成功：宣言的卡名与展示的牌一致，请选择一名目标对手给予【魅惑】。"
                        : "【圣骸布】正在公开展示牌库顶牌：所有玩家可确认宣言卡名与实际翻开的牌是否一致。",
                isCharmTarget
                    ? $"等待 {requiredPlayers} 选择【圣骸布】魅惑的目标。"
                    : $"等待 {requiredPlayers} 处理【圣骸布】。",
                submittedText);
        }

        if (contains(context, inputType, "treasureArrival:T005", "T005:arrival"))
        {
            return create(
                "魔眼封印眼镜：降临",
                "【魔眼封印眼镜】降临：每名相关玩家弃置1张手牌。",
                $"等待 {requiredPlayers} 处理【魔眼封印眼镜】降临效果。",
                submittedText);
        }

        if (contains(context, inputType, "treasureArrival:T018", "T018:arrival"))
        {
            return create(
                "九字兼定：降临",
                "【九字兼定】降临：每名相关玩家可以放逐1张自己的手牌或弃牌区牌；也可以不处理。",
                $"等待 {requiredPlayers} 选择是否处理【九字兼定】降临效果。",
                submittedText);
        }

        if (contains(context, inputType, "treasureArrival:T026", "T026:arrival"))
        {
            return create(
                "火焰的车轮：降临",
                "【火焰的车轮】降临：请选择要放逐的弃牌区牌或召唤区牌；也可以不处理。",
                $"等待 {requiredPlayers} 处理【火焰的车轮】降临效果。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A001:arrival"))
        {
            return create(
                "红雾异变：降临",
                "【红雾异变】降临：人外角色的玩家抓1张牌；人类角色的玩家需要弃置1张手牌。",
                $"等待 {requiredPlayers} 处理【红雾异变】降临效果。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A001:reward"))
        {
            return create(
                "红雾异变：解决奖励",
                "【红雾异变】解决奖励：选择目标对手给予【禁锢】。",
                $"等待 {requiredPlayers} 选择【红雾异变】解决奖励目标。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A002:arrival"))
        {
            return create(
                "春雪异变：降临",
                "【春雪异变】降临：相关玩家可以从召唤区直接召唤1张符合条件的牌；也可以不召唤。",
                $"等待 {requiredPlayers} 处理【春雪异变】降临效果。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A002:rewardOptionalBanishAndSakuraReplacement"))
        {
            var isSakuraReplacement = contains(context, inputType, "AfterTwo", "SakuraReplacement");
            return create(
                "春雪异变：解决奖励",
                isSakuraReplacement
                    ? "你已经放逐2张牌：是否召唤等量的【樱花饼】？"
                    : "【春雪异变】解决奖励：你可以放逐自己弃牌区的1张或2张牌；若放逐2张，之后可以选择是否召唤等量的【樱花饼】。",
                $"等待 {requiredPlayers} 处理【春雪异变】解决奖励。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A003:arrival"))
        {
            return create(
                "花开异变：降临",
                "【花开异变】降临：选择一名目标对手给予【禁锢】。",
                $"等待 {requiredPlayers} 选择【花开异变】的禁锢目标。",
                submittedText);
        }

        if (contains(context, inputType, "treasureOnPlay:T007", "T007"))
        {
            return create(
                "梦魇的铃铛",
                "【梦魇的铃铛】触发：选择将它交给友方玩家，或让友方玩家弃牌并获得魔力。",
                $"等待 {requiredPlayers} 选择【梦魇的铃铛】的处理方式。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A003:reward"))
        {
            return create(
                "花开异变：解决奖励",
                "【花开异变】解决奖励正在结算，请按提示选择目标或处理奖励。",
                $"等待 {requiredPlayers} 处理【花开异变】解决奖励。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A004:arrival"))
        {
            return create(
                "永夜异变：降临",
                "【永夜异变】降临：请选择需要返回或处理的防御放置牌。",
                $"等待 {requiredPlayers} 处理【永夜异变】降临效果。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A004:reward"))
        {
            return create(
                "永夜异变：解决奖励",
                "【永夜异变】解决奖励正在结算，请按提示完成选择。",
                $"等待 {requiredPlayers} 处理【永夜异变】解决奖励。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A005:arrival"))
        {
            return create(
                "温泉异变：降临",
                "【温泉异变】降临：可以从召唤区选择1张可召唤牌直接召唤；也可以不处理。",
                $"等待 {requiredPlayers} 处理【温泉异变】降临效果。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A005:condition", "conditionDefenseLikePlace"))
        {
            return create(
                "温泉异变：解决条件",
                "【温泉异变】解决条件：选择符合条件的防御式放置牌。",
                $"等待 {requiredPlayers} 选择【温泉异变】解决条件用牌。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A006:arrival"))
        {
            return create(
                "瓦拉齐亚之夜：降临",
                "【瓦拉齐亚之夜】降临：符合条件的玩家需要弃置阵地区的防御放置牌。",
                $"等待 {requiredPlayers} 处理【瓦拉齐亚之夜】降临效果。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A007:arrival"))
        {
            return create(
                "噩梦倒影：降临",
                "【噩梦倒影】降临：按效果选择要放逐的手牌或弃牌区牌；也可以不处理。",
                $"等待 {requiredPlayers} 处理【噩梦倒影】降临效果。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A007:conditionOpponentOptionalDraw"))
        {
            return create(
                "噩梦倒影：解决条件",
                "【噩梦倒影】解决时，对手可以选择是否抽1张牌。",
                $"等待 {requiredPlayers} 选择是否因【噩梦倒影】抽牌。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A007:rewardTargetCharm"))
        {
            return create(
                "噩梦倒影：解决奖励",
                "【噩梦倒影】解决奖励：选择目标对手给予【魅惑】。",
                $"等待 {requiredPlayers} 选择【噩梦倒影】魅惑目标。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A008:conditionOpponentOptionalDiscardReturn"))
        {
            return create(
                "矛盾螺旋：解决条件",
                "【矛盾螺旋】解决时，对手可以选择弃1张牌，让解决者将牌返回手牌。",
                $"等待 {requiredPlayers} 处理【矛盾螺旋】解决条件。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A008:rewardTargetShackle"))
        {
            return create(
                "矛盾螺旋：解决奖励",
                "【矛盾螺旋】解决奖励：选择目标对手给予【禁锢】。",
                $"等待 {requiredPlayers} 选择【矛盾螺旋】禁锢目标。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A009:arrival"))
        {
            return create(
                "666：降临",
                "【666】降临：按异变效果处理召唤区或相关公开区域牌。",
                $"等待 {requiredPlayers} 处理【666】降临效果。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A009:conditionOpponentOptionalBenefit"))
        {
            return create(
                "666：解决询问",
                "【666】解决时，对手可以选择是否获得【结界】并召唤【樱花饼】。",
                $"等待 {requiredPlayers} 选择是否获得【结界】并召唤【樱花饼】。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A009:reward"))
        {
            return create(
                "666：解决奖励",
                "【666】解决奖励：可以获得【结界】并召唤【樱花饼】，或选择间隙区中召唤费用7以下的宝具加入手牌。",
                $"等待 {requiredPlayers} 处理【666】解决奖励。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A010:killBanishSetAside"))
        {
            return create(
                "命运长夜：击杀触发",
                "【命运长夜】当任意玩家击杀对手时，该玩家队伍选择一名友方玩家将自己盖放的牌放逐；此异变被解决时，放逐所有以此法盖放的牌。选择一张盖牌放逐。",
                $"等待 {requiredPlayers} 选择【命运长夜】要放逐的盖牌。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A010:arrival"))
        {
            return create(
                "命运长夜：降临",
                "【命运长夜】降临效果正在结算：请按效果选择要处理的盖放或放逐对象。",
                $"等待 {requiredPlayers} 处理【命运长夜】降临效果。",
                submittedText);
        }

        if (contains(context, inputType, "anomaly:A010:reward"))
        {
            return create(
                "命运长夜：解决奖励",
                "【命运长夜】解决奖励：选择召唤区的宝具牌置入手牌。",
                $"等待 {requiredPlayers} 处理【命运长夜】解决奖励。",
                submittedText);
        }

        if (contains(context, inputType, "treasureOnPlay:T023", "T023"))
        {
            var isHeal = contains(context, inputType, "TargetHumanHeal", "targetHumanHeal");
            var isDamage = contains(context, inputType, "TargetEnemyNonHuman", "enemyNonHuman");
            return create(
                "阴阳玉",
                isHeal
                    ? "【阴阳玉】效果：灵脉+1。请选择要回复2点生命的人类角色。"
                    : isDamage
                        ? "【阴阳玉】效果：请选择目标人外对手，给予1点直接伤害。"
                        : "【阴阳玉】触发：请选择目标角色。",
                isHeal
                    ? $"等待 {requiredPlayers} 选择【阴阳玉】的回复目标。"
                    : isDamage
                        ? $"等待 {requiredPlayers} 选择【阴阳玉】的伤害目标。"
                        : $"等待 {requiredPlayers} 选择【阴阳玉】的目标。",
                submittedText);
        }

        if (contains(context, inputType, "turnStartShackleDiscard", "shackleDiscard", "Shackle"))
        {
            return create(
                "禁锢解除",
                "你处于【禁锢】：回合开始时需要按要求弃牌解除，或接受禁锢限制。",
                $"等待 {requiredPlayers} 处理【禁锢】。",
                submittedText);
        }

        if (contains(context, inputType, "endPhase:discardToHandLimit", "discardToHandLimit"))
        {
            return create(
                "手牌上限",
                "进入结束阶段时手牌超过上限：请选择要弃置的手牌，直到手牌数降至6张。",
                $"等待 {requiredPlayers} 按手牌上限弃牌。",
                submittedText);
        }

        if (contains(context, inputType, "targetOpponent", "targetPlayer", "TargetPlayerChoice"))
        {
            return create(
                "选择目标",
                "请选择此次效果的目标。选项会显示目标角色名与当前生命值。",
                $"等待 {requiredPlayers} 选择目标。",
                submittedText);
        }

        var inputId = interaction.inputContextNumericId?.ToString() ?? "-";
        var detail = $"{safe(inputType, "未知类型")} / {safe(context, "无上下文")} / 选项 {interaction.inputChoiceKeys.Count}";
        return create($"输入选择 #{inputId}", detail, $"等待 {requiredPlayers} 选择。", submittedText);
    }

    private static InputContextDescriptionViewModel create(string title, string detail, string waitingText, string submittedText)
    {
        return new InputContextDescriptionViewModel
        {
            title = title,
            detail = detail,
            waitingText = waitingText,
            submittedText = submittedText,
        };
    }

    private static string formatRequiredPlayers(ProjectionViewModel projection)
    {
        var ids = projection.interaction.inputRequiredPlayerNumericIds.Count > 0
            ? projection.interaction.inputRequiredPlayerNumericIds
            : projection.interaction.inputRequiredPlayerNumericId.HasValue
                ? new List<long> { projection.interaction.inputRequiredPlayerNumericId.Value }
                : new List<long>();

        if (ids.Count == 0)
        {
            return "相关玩家";
        }

        return string.Join("、", ids.Select(id => formatPlayer(projection, id)));
    }

    private static string formatPlayer(ProjectionViewModel projection, long playerId)
    {
        var player = projection.playerSummaries.FirstOrDefault(summary => summary.playerNumericId == playerId);
        if (player is null)
        {
            return $"P{playerId}";
        }

        var characterName = string.IsNullOrWhiteSpace(player.activeCharacterDefinitionId)
            ? "未选择角色"
            : CardNameLocalizer.Localize(player.activeCharacterDefinitionId);
        return $"{characterName}(P{playerId})";
    }

    private static bool contains(string context, string inputType, params string[] needles)
    {
        foreach (var needle in needles)
        {
            if ((!string.IsNullOrWhiteSpace(context) && context.Contains(needle, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(inputType) && inputType.Contains(needle, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static string safe(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
}
