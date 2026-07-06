using System;
using System.Collections.Generic;

namespace CrescentWreath.Client.UI
{
public static class CardNameLocalizer
{
    private static readonly IReadOnlyDictionary<string, string> NamesByDefinitionId =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["S001"] = "樱花饼",
            ["starter:kourindouCoupon"] = "香霖堂购物券",
            ["starter:magicCircuit"] = "魔术回路",
            ["T001B"] = "香霖堂购物券",
            ["T002B"] = "魔术回路",
            ["T001"] = "月时计",
            ["T002"] = "七色魔法书",
            ["T003"] = "绯想之剑",
            ["T004"] = "楼观剑",
            ["T005"] = "魔眼封印眼镜",
            ["T006"] = "水晶剑Azoth",
            ["T007"] = "梦魔的铃铛",
            ["T008"] = "起源魔弹",
            ["T009"] = "模拟神经Etherite",
            ["T010"] = "伊吹瓢",
            ["T011"] = "迷你八卦炉",
            ["T012"] = "空空如也的赛钱箱",
            ["T013"] = "圣骸布",
            ["T014"] = "遥远的理想乡",
            ["T015"] = "黑键",
            ["T016"] = "机械翡翠",
            ["T017"] = "天之杯的幻想",
            ["T018"] = "九字兼定",
            ["T019"] = "空气魔法卷轴",
            ["T020"] = "非想天则",
            ["T021"] = "制御棒",
            ["T022"] = "阿鲁猫火箭",
            ["T023"] = "阴阳玉",
            ["T024"] = "EX变身红丝带",
            ["T025"] = "正式外典Gamaliel",
            ["T026"] = "火焰的车轮",
            ["T027"] = "蕾咪的威严",
            ["T028"] = "圣德道士之笏",
            ["T029"] = "世纪末风神传说",
            ["A001"] = "红雾异变",
            ["A002"] = "春雪异变",
            ["A003"] = "花开异变",
            ["A004"] = "永夜异变",
            ["A005"] = "温泉异变",
            ["A006"] = "瓦拉齐亚之夜",
            ["A007"] = "噩梦倒影",
            ["A008"] = "矛盾螺旋",
            ["A009"] = "666",
            ["A010"] = "命运长夜",
            ["Barrier"] = "结界",
            ["Seal"] = "封印",
            ["Shackle"] = "禁锢",
        };

    public static string Localize(string definitionId)
    {
        if (string.IsNullOrWhiteSpace(definitionId))
        {
            return "未知卡牌";
        }

        var normalized = definitionId.Trim();
        return NamesByDefinitionId.TryGetValue(normalized, out var name)
            ? name
            : normalized;
    }
}
}
