using System.Collections.Generic;

namespace CrescentWreath.RuleCore.Definitions;

public sealed class InMemoryCharacterDefinitionSource : ICharacterDefinitionSource
{
    private static readonly IReadOnlyList<CharacterDefinition> CharacterDefinitions = new List<CharacterDefinition>
    {
        new()
        {
            definitionId = "C001",
            characterName = "博丽灵梦",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C001:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C001:1",
                    skillName = "八方鬼缚阵",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C001:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C001:2",
                    skillName = "博丽大结界",
                    skillOrder = 2,
                    skillTypeRaw = "响应",
                    skillCostRaw = "任意玩家回合开始时，弃1张防御为3或更高牌",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C001:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C001:3",
                    skillName = "梦想天生",
                    skillOrder = 3,
                    skillTypeRaw = "启动",
                    skillCostRaw = "⑥，II",
                    manaCost = 6,
                    skillPointCost = 2,
                    skillType = "active",
                },
                ["C001:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C001:4",
                    skillName = "不可思议的空飞巫女",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑦，III，每回合一次",
                    manaCost = 7,
                    skillPointCost = 3,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C002",
            characterName = "东风谷早苗",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C002:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C002:1",
                    skillName = "摩西的奇迹",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "③",
                    manaCost = 3,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C002:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C002:2",
                    skillName = "新星辉煌之夜",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C002:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C002:3",
                    skillName = "九字刺",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "",
                    manaCost = 6,
                    skillPointCost = 2,
                    skillType = "active",
                },
                ["C002:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C002:4",
                    skillName = "五谷丰登之祝福",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "④，II",
                    manaCost = 4,
                    skillPointCost = 2,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C003",
            characterName = "蕾米莉亚",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C003:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C003:1",
                    skillName = "红色不夜城",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C003:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C003:2",
                    skillName = "冈格尼尔",
                    skillOrder = 2,
                    skillTypeRaw = "响应",
                    skillCostRaw = "每回合1次，你或操控芙兰朵露的队友给予对手伤害时，你弃1张召唤费用为3或更高的牌",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C003:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C003:3",
                    skillName = "绯色宿命",
                    skillOrder = 3,
                    skillTypeRaw = "启动",
                    skillCostRaw = "",
                    manaCost = 6,
                    skillPointCost = 3,
                    skillType = "active",
                },
                ["C003:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C003:4",
                    skillName = "红色幻想乡",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑥III",
                    manaCost = 6,
                    skillPointCost = 3,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C004",
            characterName = "魔理沙",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C004:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C004:1",
                    skillName = "极限火花",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑤",
                    manaCost = 5,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C004:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C004:2",
                    skillName = "星尘狂欢",
                    skillOrder = 2,
                    skillTypeRaw = "响应",
                    skillCostRaw = "每回合一次，若你在防御后未被击杀，弃1张手牌",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C004:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C004:3",
                    skillName = "八卦炉充能",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "③",
                    manaCost = 3,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C004:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C004:4",
                    skillName = "炽烈彗星",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑦II",
                    manaCost = 7,
                    skillPointCost = 2,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C005",
            characterName = "魂魄妖梦",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C005:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C005:1",
                    skillName = "无为无策的冥罚",
                    skillOrder = 1,
                    skillTypeRaw = "响应",
                    skillCostRaw = "你给予对手体术伤害时",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C005:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C005:2",
                    skillName = "樱花闪闪",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④，X剑气",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C005:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C005:3",
                    skillName = "六根清净斩",
                    skillOrder = 3,
                    skillTypeRaw = "响应",
                    skillCostRaw = "当你完全防御对手一次伤害后，移除1剑气指示物",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C005:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C005:4",
                    skillName = "西行寺春风斩",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "④II，移除X剑气指示物",
                    manaCost = 4,
                    skillPointCost = 2,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C006",
            characterName = "十六夜咲夜",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C006:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C006:1",
                    skillName = "杀人玩偶",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C006:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C006:2",
                    skillName = "狂气杰克",
                    skillOrder = 2,
                    skillTypeRaw = "响应",
                    skillCostRaw = "启动状态下，每回合1次，任意玩家防御前，I",
                    manaCost = 0,
                    skillPointCost = 1,
                    skillType = "response",
                },
                ["C006:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C006:3",
                    skillName = "特制时停表",
                    skillOrder = 3,
                    skillTypeRaw = "启动",
                    skillCostRaw = "⑥，II",
                    manaCost = 6,
                    skillPointCost = 2,
                    skillType = "active",
                },
                ["C006:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C006:4",
                    skillName = "咲夜的世界",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑧，V",
                    manaCost = 8,
                    skillPointCost = 5,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C007",
            characterName = "莲",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C007:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C007:1",
                    skillName = "梦萝莉的魅影",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "②",
                    manaCost = 2,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C007:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C007:2",
                    skillName = "让人脸红心跳的美梦",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C007:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C007:3",
                    skillName = "让人胆战心惊的噩梦",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④，移除3梦境指示物",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C007:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C007:4",
                    skillName = "冰淇淋甜点白日梦",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑤II",
                    manaCost = 5,
                    skillPointCost = 2,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C008",
            characterName = "阿尔托莉亚",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C008:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C008:1",
                    skillName = "风王结界",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "③",
                    manaCost = 3,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C008:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C008:2",
                    skillName = "魔力铠甲",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "③",
                    manaCost = 3,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C008:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C008:3",
                    skillName = "魔力补充行为",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑥I",
                    manaCost = 6,
                    skillPointCost = 1,
                    skillType = "active",
                },
                ["C008:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C008:4",
                    skillName = "约定的胜利之剑",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑦，III",
                    manaCost = 7,
                    skillPointCost = 3,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C009",
            characterName = "两仪式",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C009:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C009:1",
                    skillName = "直死魔眼",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④，减少自己1生命",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C009:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C009:2",
                    skillName = "杀人鬼的资质",
                    skillOrder = 2,
                    skillTypeRaw = "响应",
                    skillCostRaw = "当对手防御你的伤害时，弃2张牌",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C009:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C009:3",
                    skillName = "秘剑·猫返",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "③",
                    manaCost = 3,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C009:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C009:4",
                    skillName = "根源式",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "弃2张手牌，③，III",
                    manaCost = 3,
                    skillPointCost = 3,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C010",
            characterName = "远野秋叶",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C010:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C010:1",
                    skillName = "赫译·红叶",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑤",
                    manaCost = 5,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C010:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C010:2",
                    skillName = "胸部乃绝对铁壁",
                    skillOrder = 2,
                    skillTypeRaw = "响应",
                    skillCostRaw = "受到1伤害时",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C010:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C010:3",
                    skillName = "赤主形态",
                    skillOrder = 3,
                    skillTypeRaw = "启动",
                    skillCostRaw = "③",
                    manaCost = 3,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C010:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C010:4",
                    skillName = "赤主·焰华",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "启动状态下，④，III",
                    manaCost = 4,
                    skillPointCost = 3,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C011",
            characterName = "爱尔奎特",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C011:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C011:1",
                    skillName = "魅惑魔眼",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑤，I",
                    manaCost = 5,
                    skillPointCost = 1,
                    skillType = "active",
                },
                ["C011:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C011:2",
                    skillName = "真祖格斗术",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "每回合1次，⑤",
                    manaCost = 5,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C011:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C011:3",
                    skillName = "魔法少女白姬变身",
                    skillOrder = 3,
                    skillTypeRaw = "启动",
                    skillCostRaw = "④，I",
                    manaCost = 4,
                    skillPointCost = 1,
                    skillType = "active",
                },
                ["C011:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C011:4",
                    skillName = "空想具现化",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑩，V",
                    manaCost = 10,
                    skillPointCost = 5,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C012",
            characterName = "巫净琥珀",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C012:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C012:1",
                    skillName = "开打靠靭琥珀脚",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "③",
                    manaCost = 3,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C012:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C012:2",
                    skillName = "正义的魔法少女变身",
                    skillOrder = 2,
                    skillTypeRaw = "启动",
                    skillCostRaw = "④，I",
                    manaCost = 4,
                    skillPointCost = 1,
                    skillType = "active",
                },
                ["C012:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C012:3",
                    skillName = "“Hey Jonny,come on!”",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C012:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C012:4",
                    skillName = "拔刀奥义·贺正箒星",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑥，III",
                    manaCost = 6,
                    skillPointCost = 3,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C013",
            characterName = "巫净翡翠",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C013:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C013:1",
                    skillName = "巨大人形兵器原形",
                    skillOrder = 1,
                    skillTypeRaw = "响应",
                    skillCostRaw = "在任意玩家的回合开始时，弃2张牌",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C013:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C013:2",
                    skillName = "异常的味觉",
                    skillOrder = 2,
                    skillTypeRaw = "响应",
                    skillCostRaw = "目标对手防御时，弃1张牌，II",
                    manaCost = 0,
                    skillPointCost = 2,
                    skillType = "response",
                },
                ["C013:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C013:3",
                    skillName = "暗黑翡翠拳",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑥",
                    manaCost = 6,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C013:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C013:4",
                    skillName = "“你被犯人了”",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑤，II",
                    manaCost = 5,
                    skillPointCost = 2,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C014",
            characterName = "芙兰朵露",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C014:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C014:1",
                    skillName = "四重存在",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑥",
                    manaCost = 6,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C014:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C014:2",
                    skillName = "之后就谁都不在了",
                    skillOrder = 2,
                    skillTypeRaw = "响应",
                    skillCostRaw = "你给予任意数量对手体术或咒术伤害时，减少X生命并弃X张牌",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C014:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C014:3",
                    skillName = "莱汶丁",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "③，弃1张牌",
                    manaCost = 3,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C014:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C014:4",
                    skillName = "495年的波纹",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "IV，弃1张牌，移除X个毁灭指示物",
                    manaCost = 0,
                    skillPointCost = 4,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C015",
            characterName = "八云紫",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C015:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C015:1",
                    skillName = "间隙的妖怪",
                    skillOrder = 1,
                    skillTypeRaw = "响应",
                    skillCostRaw = "每回合一次，你的召唤阶段中",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C015:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C015:2",
                    skillName = "客观结界",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑤，放逐X张手牌",
                    manaCost = 5,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C015:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C015:3",
                    skillName = "突然废站下车之旅",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "③，从手牌中放逐2张牌",
                    manaCost = 3,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C015:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C015:4",
                    skillName = "梦幻泡影",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑤II",
                    manaCost = 5,
                    skillPointCost = 2,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C016",
            characterName = "射命丸文",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C016:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C016:1",
                    skillName = "镰鼬面纱",
                    skillOrder = 1,
                    skillTypeRaw = "响应",
                    skillCostRaw = "当你使用一次通常技时，移除你的【结界】效果",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C016:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C016:2",
                    skillName = "天狗报即日限",
                    skillOrder = 2,
                    skillTypeRaw = "响应",
                    skillCostRaw = "任意玩家回合开始时，弃1张召唤费用为3或更高的牌",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C016:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C016:3",
                    skillName = "幻想风靡",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑥",
                    manaCost = 6,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C016:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C016:4",
                    skillName = "天孙降临",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "④，II",
                    manaCost = 4,
                    skillPointCost = 2,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C017",
            characterName = "帕秋莉",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C017:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C017:1",
                    skillName = "七曜魔法",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C017:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C017:2",
                    skillName = "元素收割者",
                    skillOrder = 2,
                    skillTypeRaw = "响应",
                    skillCostRaw = "当目标对手使用通常技并支付完其费用后，弃一张牌并移除5种元素指示物",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C017:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C017:3",
                    skillName = "贤者之石",
                    skillOrder = 3,
                    skillTypeRaw = "启动",
                    skillCostRaw = "在你有5种元素指示物时，⑥，II",
                    manaCost = 6,
                    skillPointCost = 2,
                    skillType = "active",
                },
                ["C017:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C017:4",
                    skillName = "皇家烈焰",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "x,IV",
                    manaCost = 0,
                    skillPointCost = 4,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C018",
            characterName = "幽幽子",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C018:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C018:1",
                    skillName = "永不餍足的胃",
                    skillOrder = 1,
                    skillTypeRaw = "响应",
                    skillCostRaw = "你的召唤阶段中",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C018:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C018:2",
                    skillName = "凤蝶纹的死枪",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑥，每回合一次",
                    manaCost = 6,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C018:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C018:3",
                    skillName = "依恋未酌宴",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C018:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C018:4",
                    skillName = "西行寺无余涅槃",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑥，III",
                    manaCost = 6,
                    skillPointCost = 3,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C019",
            characterName = "藤原妹红",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C019:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C019:1",
                    skillName = "不死鸟重生",
                    skillOrder = 1,
                    skillTypeRaw = "响应",
                    skillCostRaw = "启动状态下被击杀时",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C019:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C019:2",
                    skillName = "凤翼天翔",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑤，I",
                    manaCost = 5,
                    skillPointCost = 1,
                    skillType = "active",
                },
                ["C019:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C019:3",
                    skillName = "不死鸟之尾",
                    skillOrder = 3,
                    skillTypeRaw = "启动",
                    skillCostRaw = "III",
                    manaCost = 0,
                    skillPointCost = 3,
                    skillType = "active",
                },
                ["C019:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C019:4",
                    skillName = "不朽的弹幕",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑥，I",
                    manaCost = 6,
                    skillPointCost = 1,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C020",
            characterName = "蓬莱山辉夜",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C020:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C020:1",
                    skillName = "竹取物语",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C020:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C020:2",
                    skillName = "永夜返",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "②",
                    manaCost = 2,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C020:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C020:3",
                    skillName = "须臾间的永恒",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "当你的生命值低于4时，②",
                    manaCost = 2,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C020:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C020:4",
                    skillName = "蓬莱的树海",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑥，II",
                    manaCost = 6,
                    skillPointCost = 2,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C021",
            characterName = "雪露",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C021:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C021:1",
                    skillName = "咖喱能量",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "每回合1次，弃一张牌",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C021:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C021:2",
                    skillName = "黑键投射",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④，弃一张牌",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C021:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C021:3",
                    skillName = "火葬式典",
                    skillOrder = 3,
                    skillTypeRaw = "响应",
                    skillCostRaw = "使用黑键投射时，额外消耗④，I",
                    manaCost = 4,
                    skillPointCost = 1,
                    skillType = "response",
                },
                ["C021:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C021:4",
                    skillName = "第七圣典原罪救济",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "④，II",
                    manaCost = 4,
                    skillPointCost = 2,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C022",
            characterName = "爱丽丝",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C022:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C022:1",
                    skillName = "少女文乐",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "②",
                    manaCost = 2,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C022:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C022:2",
                    skillName = "归于虚无",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④，1人形指示物",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C022:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C022:3",
                    skillName = "大千枪",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④，2人形指示物",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C022:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C022:4",
                    skillName = "人形战争",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "④，III，移除X人形",
                    manaCost = 4,
                    skillPointCost = 3,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C023",
            characterName = "风见幽香",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C023:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C023:1",
                    skillName = "梦幻馆主",
                    skillOrder = 1,
                    skillTypeRaw = "响应",
                    skillCostRaw = "当你发动带体术或咒术伤害的技能时，弃1张牌",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C023:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C023:2",
                    skillName = "幻想乡花开",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑥，每回合1次",
                    manaCost = 6,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C023:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C023:3",
                    skillName = "双生火花",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑥，I，弃X张牌",
                    manaCost = 6,
                    skillPointCost = 1,
                    skillType = "active",
                },
                ["C023:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C023:4",
                    skillName = "花鸟风月 啸风弄月",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑦，II",
                    manaCost = 7,
                    skillPointCost = 2,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C024",
            characterName = "伊吹萃香",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C024:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C024:1",
                    skillName = "小小的百鬼夜行",
                    skillOrder = 1,
                    skillTypeRaw = "启动",
                    skillCostRaw = "⑥，III",
                    manaCost = 6,
                    skillPointCost = 3,
                    skillType = "active",
                },
                ["C024:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C024:2",
                    skillName = "超高密度燐祸术",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑥",
                    manaCost = 6,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C024:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C024:3",
                    skillName = "云集雾散",
                    skillOrder = 3,
                    skillTypeRaw = "响应",
                    skillCostRaw = "当你受到一次不致命的伤害时，减少1点生命",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C024:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C024:4",
                    skillName = "大江山悉皆杀",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "启动状态下，弃4张牌",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C025",
            characterName = "丰聪耳神子",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C025:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C025:1",
                    skillName = "和为贵",
                    skillOrder = 1,
                    skillTypeRaw = "响应",
                    skillCostRaw = "目标对手用宝具牌给予友方玩家伤害时，移除1神灵",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C025:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C025:2",
                    skillName = "豪族乱舞",
                    skillOrder = 2,
                    skillTypeRaw = "响应",
                    skillCostRaw = "对手防御伤害时，你移除1神灵并弃1张召唤费用大于1的牌",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C025:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C025:3",
                    skillName = "救世之光",
                    skillOrder = 3,
                    skillTypeRaw = "响应",
                    skillCostRaw = "目标队友发动通常技时，移除2神灵并弃1张带技能值的牌，I",
                    manaCost = 0,
                    skillPointCost = 1,
                    skillType = "response",
                },
                ["C025:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C025:4",
                    skillName = "神灵大宇宙",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑤，I",
                    manaCost = 5,
                    skillPointCost = 1,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C026",
            characterName = "比那名居天子",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C026:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C026:1",
                    skillName = "天启气象之剑",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C026:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C026:2",
                    skillName = "开天辟地之冲击",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "③，I",
                    manaCost = 3,
                    skillPointCost = 1,
                    skillType = "active",
                },
                ["C026:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C026:3",
                    skillName = "不让土壤之剑",
                    skillOrder = 3,
                    skillTypeRaw = "响应",
                    skillCostRaw = "存在天气时，若友方玩家防御对手给予的伤害后未被击杀，I",
                    manaCost = 0,
                    skillPointCost = 1,
                    skillType = "response",
                },
                ["C026:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C026:4",
                    skillName = "全人类的绯想天",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "存在天气时，⑥，II",
                    manaCost = 6,
                    skillPointCost = 2,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C027",
            characterName = "美杜莎",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C027:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C027:1",
                    skillName = "石化魔眼",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑥，II",
                    manaCost = 6,
                    skillPointCost = 2,
                    skillType = "active",
                },
                ["C027:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C027:2",
                    skillName = "他者封印·鲜血神殿",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "II",
                    manaCost = 0,
                    skillPointCost = 2,
                    skillType = "active",
                },
                ["C027:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C027:3",
                    skillName = "自我封印·暗黑神殿",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑤",
                    manaCost = 5,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C027:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C027:4",
                    skillName = "骑英的缰绳",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑥，I",
                    manaCost = 6,
                    skillPointCost = 1,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C028",
            characterName = "远坂凛",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C028:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C028:1",
                    skillName = "魔力宝石",
                    skillOrder = 1,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑥",
                    manaCost = 6,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C028:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C028:2",
                    skillName = "泽尔里奇宝箱",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "③",
                    manaCost = 3,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C028:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C028:3",
                    skillName = "魔法少女变身",
                    skillOrder = 3,
                    skillTypeRaw = "启动",
                    skillCostRaw = "④，I",
                    manaCost = 4,
                    skillPointCost = 1,
                    skillType = "active",
                },
                ["C028:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C028:4",
                    skillName = "平行世界魔力干涉",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "移除3个宝石指示物，④，IV",
                    manaCost = 4,
                    skillPointCost = 4,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C029",
            characterName = "卡莲",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C029:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C029:1",
                    skillName = "被虐灵媒体质",
                    skillOrder = 1,
                    skillTypeRaw = "响应",
                    skillCostRaw = "当你防御时，弃3张牌",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C029:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C029:2",
                    skillName = "必杀的毒舌",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑤",
                    manaCost = 5,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C029:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C029:3",
                    skillName = "魔法少女卡莲变身",
                    skillOrder = 3,
                    skillTypeRaw = "启动",
                    skillCostRaw = "④，I",
                    manaCost = 4,
                    skillPointCost = 1,
                    skillType = "active",
                },
                ["C029:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C029:4",
                    skillName = "マグダラ的圣骸布",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "④，IV",
                    manaCost = 4,
                    skillPointCost = 4,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C030",
            characterName = "伊莉雅",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C030:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C030:1",
                    skillName = "圣杯容器",
                    skillOrder = 1,
                    skillTypeRaw = "响应",
                    skillCostRaw = "每回合1次，任意玩家回合开始时，弃4张召唤费用大于1的牌",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C030:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C030:2",
                    skillName = "BerserCar",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "⑥",
                    manaCost = 6,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C030:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C030:3",
                    skillName = "魔法少女伊莉雅变身",
                    skillOrder = 3,
                    skillTypeRaw = "启动",
                    skillCostRaw = "④，I",
                    manaCost = 4,
                    skillPointCost = 1,
                    skillType = "active",
                },
                ["C030:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C030:4",
                    skillName = "老虎道场大乱斗",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "⑦，II",
                    manaCost = 7,
                    skillPointCost = 2,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "C031",
            characterName = "弓冢五月",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>
            {
                ["C031:1"] = new CharacterSkillDefinition
                {
                    skillKey = "C031:1",
                    skillName = "新人吸血鬼",
                    skillOrder = 1,
                    skillTypeRaw = "响应",
                    skillCostRaw = "当你的生命值为1时，任意回合结束时",
                    manaCost = 0,
                    skillPointCost = 0,
                    skillType = "response",
                },
                ["C031:2"] = new CharacterSkillDefinition
                {
                    skillKey = "C031:2",
                    skillName = "无法到达远野君处之拳",
                    skillOrder = 2,
                    skillTypeRaw = "通常",
                    skillCostRaw = "④，X",
                    manaCost = 4,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C031:3"] = new CharacterSkillDefinition
                {
                    skillKey = "C031:3",
                    skillName = "小巷子同盟",
                    skillOrder = 3,
                    skillTypeRaw = "通常",
                    skillCostRaw = "③",
                    manaCost = 3,
                    skillPointCost = 0,
                    skillType = "active",
                },
                ["C031:4"] = new CharacterSkillDefinition
                {
                    skillKey = "C031:4",
                    skillName = "枯竭庭园",
                    skillOrder = 4,
                    skillTypeRaw = "终结",
                    skillCostRaw = "V",
                    manaCost = 0,
                    skillPointCost = 5,
                    skillType = "ultimate",
                },
            },
        },
        new()
        {
            definitionId = "test:human",
            characterName = "test:human",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "human",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>(),
        },
        new()
        {
            definitionId = "test:nonHuman",
            characterName = "test:nonHuman",
            baseMaxHp = 4,
            raceTags = new List<string>
            {
                "nonHuman",
            },
            skills = new Dictionary<string, CharacterSkillDefinition>(),
        },
    };

    public IReadOnlyList<CharacterDefinition> getCharacterDefinitions()
    {
        return CharacterDefinitions;
    }
}

