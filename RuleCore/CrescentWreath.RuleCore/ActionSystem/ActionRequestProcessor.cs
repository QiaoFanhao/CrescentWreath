using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CrescentWreath.RuleCore.DamageSystem;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Trace;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class ActionRequestProcessor
{
    internal const string ContinuationKeyStagedResponseDamage = "continuation:stagedResponseDamage";
    private const string DefenseTypeKeyFixedReduce1 = "fixedReduce1";
    private const string DefenseTypeKeyPhysical = "physical";
    private const string DefenseTypeKeySpell = "spell";
    private const string DefenseTypeKeyDual = "dual";
    private const string DamageTypeKeyPhysical = "physical";
    private const string DamageTypeKeySpell = "spell";
    private const string DamageTypeKeyDirect = "direct";
    private const string DefenseDeclarationPrefixCard = "cardDefense:";
    internal const string DamageResponseStageAwaitDefense = "awaitDefense";
    internal const string DamageResponseStageAwaitCounter = "awaitCounter";
    private const string DamageCounterTypeKeyCancelFixedReduce1 = "cancelFixedReduce1";
    private const string SkillKeyC002_2 = "C002:2";
    private const string SkillKeyC003_1 = "C003:1";
    private const string SkillKeyC003_2 = "C003:2";
    private const string SkillKeyC003_3 = "C003:3";
    private const string SkillKeyC003_4 = "C003:4";
    private const string ContinuationKeyC003RedNightTarget = "continuation:character:C003:1:targetOpponent";
    private const string ContinuationKeyC003GungnirDiscard = "continuation:character:C003:2:discardForPenetrate";
    private const string ContinuationKeyC003ScarletDestinyTarget = "continuation:character:C003:3:targetOpponent";
    private const string InputTypeKeyC003TargetPlayer = "character:C003:targetPlayer";
    private const string InputTypeKeyC003GungnirDiscard = "character:C003:gungnirDiscard";
    private const string ContextKeyC003RedNightTarget = "character:C003:1:targetOpponent";
    private const string ContextKeyC003GungnirDiscard = "character:C003:2:discardForPenetrate";
    private const string ContextKeyC003ScarletDestinyTarget = "character:C003:3:targetOpponent";
    private const string LocalStateKeyC003RedNightHealAmount = "character:C003:1:healAmount";
    private const string LocalStateKeyC003GungnirSourcePlayerId = "character:C003:2:sourcePlayerId";
    private const string LocalStateKeyC003GungnirSourceCardInstanceId = "character:C003:2:sourceCardInstanceId";
    private const string LocalStateKeyC003GungnirSourceCharacterInstanceId = "character:C003:2:sourceCharacterInstanceId";
    private const string LocalStateKeyC003GungnirTargetCharacterInstanceId = "character:C003:2:targetCharacterInstanceId";
    private const string LocalStateKeyC003GungnirBaseDamageValue = "character:C003:2:baseDamageValue";
    private const string LocalStateKeyC003GungnirDamageType = "character:C003:2:damageType";
    private const string SkillKeyC004_1 = "C004:1";
    private const string SkillKeyC004_2 = "C004:2";
    private const string SkillKeyC004_3 = "C004:3";
    private const string SkillKeyC004_4 = "C004:4";
    private const string ContinuationKeyC004MasterSparkTarget = "continuation:character:C004:1:targetOpponent";
    private const string ContinuationKeyC004StardustDiscard = "continuation:character:C004:2:discardForDirectDamage";
    private const string ContinuationKeyC004BlazingCometTarget = "continuation:character:C004:4:targetOpponent";
    private const string ContinuationKeyC004BlazingCometBanish = "continuation:character:C004:4:banishCard";
    private const string InputTypeKeyC004TargetPlayer = "character:C004:targetPlayer";
    private const string InputTypeKeyC004DiscardCard = "character:C004:discardCard";
    private const string InputTypeKeyC004BanishCard = "character:C004:banishCard";
    private const string ContextKeyC004MasterSparkTarget = "character:C004:1:targetOpponent";
    private const string ContextKeyC004StardustDiscard = "character:C004:2:discardForDirectDamage";
    private const string ContextKeyC004BlazingCometTarget = "character:C004:4:targetOpponent";
    private const string ContextKeyC004BlazingCometBanish = "character:C004:4:banishCard";
    private const string LocalStateKeyC004StardustSourcePlayerId = "character:C004:2:sourcePlayerId";
    private const string LocalStateKeyC004BlazingCometSequence = "character:C004:4:sequence";
    private const string SkillKeyC005_3 = "C005:3";
    private const string SkillKeyC005_4 = "C005:4";
    private const string ContinuationKeyC005SakuraSparkSelectSwordAura = "continuation:character:C005:3:selectSwordAura";
    private const string ContinuationKeyC005SakuraSparkTargets = "continuation:character:C005:3:targets";
    private const string ContinuationKeyC005SpringWindSelectSwordAura = "continuation:character:C005:4:selectSwordAura";
    private const string ContinuationKeyC005SpringWindTarget = "continuation:character:C005:4:target";
    private const string ContinuationKeyC005SixRealmsChoice = "continuation:character:C005:2:sixRealmsChoice";
    private const string InputTypeKeyC005SwordAuraAmount = "character:C005:swordAuraAmount";
    private const string InputTypeKeyC005TargetPlayer = "character:C005:targetPlayer";
    private const string InputTypeKeyC005SixRealmsChoice = "character:C005:2:sixRealmsChoice";
    private const string ContextKeyC005SakuraSparkSwordAura = "character:C005:3:swordAura";
    private const string ContextKeyC005SakuraSparkTargets = "character:C005:3:targets";
    private const string ContextKeyC005SpringWindSwordAura = "character:C005:4:swordAura";
    private const string ContextKeyC005SpringWindTarget = "character:C005:4:target";
    private const string ContextKeyC005SixRealmsChoice = "character:C005:2:sixRealmsChoice";
    private const string ChoiceKeyC005SwordAuraPrefix = "swordAura:";
    private const string ChoiceKeyC005SixRealmsUse = "c005SixRealms:use";
    private const string LocalStateKeyC005ConsumedSwordAura = "character:C005:consumedSwordAura";
    private const string LocalStateKeyC005SakuraSparkRemainingDamages = "character:C005:3:remainingDamages";
    private const string LocalStateKeyC005SixRealmsDefenderCharacterInstanceId = "character:C005:2:defenderCharacterInstanceId";
    private const string LocalStateKeyC005SixRealmsSourceCharacterInstanceId = "character:C005:2:sourceCharacterInstanceId";
    private const string LocalStateKeyC005SwordAuraGrantedForAction = "character:C005:1:swordAuraGrantedForAction";
    private const string RevealReasonKeyC005SixRealms = "character:C005:2:sixRealms";
    private const string ChoiceKeyDecline = "decline";
    private const string ChoiceKeyDiscardCardPrefix = "discardCard:";
    private const string ChoiceKeyBanishCardPrefix = "banishCard:";
    private const string SkillKeyC001_1 = "C001:1";
    private const string SkillKeyC001_3 = "C001:3";
    private const string SkillKeyC001_4 = "C001:4";
    private const string ContinuationKeyC001SealTarget = "continuation:character:C001:1:targetOpponent";
    private const string InputTypeKeyC001TargetPlayer = "character:C001:targetPlayer";
    private const string ContextKeyC001SealTarget = "character:C001:1:targetOpponent";
    private const string SkillKeyC007_1 = "C007:1";
    private const string SkillKeyC007_2 = "C007:2";
    private const string SkillKeyC007_3 = "C007:3";
    private const string SkillKeyC007_4 = "C007:4";
    private const string ContinuationKeyC007NightmareTarget = "continuation:character:C007:3:targetOpponent";
    private const string ContinuationKeyC007DaydreamHealTarget = "continuation:character:C007:4:healTarget";
    private const string ContinuationKeyC007DaydreamDamageTarget = "continuation:character:C007:4:damageTarget";
    private const string InputTypeKeyC007TargetPlayer = "character:C007:targetPlayer";
    private const string ContextKeyC007NightmareTarget = "character:C007:3:targetOpponent";
    private const string ContextKeyC007DaydreamHealTarget = "character:C007:4:healTarget";
    private const string ContextKeyC007DaydreamDamageTarget = "character:C007:4:damageTarget";
    private const string ChoiceKeyPlayerPrefix = "player:";
    private const string SkillKeyC008_1 = "C008:1";
    private const string SkillKeyC008_2 = "C008:2";
    private const string SkillKeyC008_3 = "C008:3";
    private const string SkillKeyC008_4 = "C008:4";
    private const string ContinuationKeyC008WindKingTarget = "continuation:character:C008:1:targetOpponent";
    private const string ContinuationKeyC008WindKingDamageType = "continuation:character:C008:1:damageType";
    private const string InputTypeKeyC008TargetPlayer = "character:C008:targetPlayer";
    private const string InputTypeKeyC008DamageType = "character:C008:damageType";
    private const string ContextKeyC008WindKingTarget = "character:C008:1:targetOpponent";
    private const string ContextKeyC008WindKingDamageType = "character:C008:1:damageType";
    private const string ChoiceKeyDamageTypePhysical = "damageType:physical";
    private const string ChoiceKeyDamageTypeSpell = "damageType:spell";
    private const string LocalStateKeyC008WindKingTargetPlayerId = "character:C008:1:targetPlayerId";
    private const string LocalStateKeyC008RemainingTargetCharacterIds = "character:C008:4:remainingTargetCharacterIds";
    private const string LocalStateKeyC008ExcaliburSequence = "character:C008:4:sequence";
    private const string LocalStateKeyC008QueuedDamageContexts = "character:C008:4:queuedDamageContexts";
    private const string C008ExcaliburSequenceCollecting = "collecting";
    private const string C008ExcaliburSequenceFinalizing = "finalizing";
    private const string SkillKeyC011_1 = "C011:1";
    private const string SkillKeyC011_2 = "C011:2";
    private const string SkillKeyC011_3 = "C011:3";
    private const string SkillKeyC011_4 = "C011:4";
    private const string ContinuationKeyC011MysticEyesTarget = "continuation:character:C011:1:targetOpponent";
    private const string ContinuationKeyC011TrueAncestorCombatTarget = "continuation:character:C011:2:targetOpponent";
    private const string ContinuationKeyC011RealityMarbleSelectSummon = "continuation:character:C011:4:selectSummon";
    private const string InputTypeKeyC011TargetPlayer = "character:C011:targetPlayer";
    private const string InputTypeKeyC011SelectSummon = "character:C011:selectSummon";
    private const string ContextKeyC011MysticEyesTarget = "character:C011:1:targetOpponent";
    private const string ContextKeyC011TrueAncestorCombatTarget = "character:C011:2:targetOpponent";
    private const string ContextKeyC011RealityMarbleSelectSummon = "character:C011:4:selectSummon";
    private const string ChoiceKeySummonZoneCardPrefix = "summonZoneCard:";
    private const string LocalStateKeyC011RealityMarbleRemainingCount = "character:C011:4:remainingCount";
    private const string LocalStateKeyC011MysticEyesPendingFullDefenseCharm = "character:C011:1:pendingFullDefenseCharm";
    private const string LocalStateKeyC011TrueAncestorCombatDeactivateAfterDamage = "character:C011:2:deactivateAfterDamage";
    private const string SkillKeyC013_2 = "C013:2";
    private const string SkillKeyC013_3 = "C013:3";
    private const string SkillKeyC013_4 = "C013:4";
    private const string ContinuationKeyC013AbnormalTasteDiscard = "continuation:character:C013:2:discardForDefenseValue";
    private const string ContinuationKeyC013DarkJadeFistTarget = "continuation:character:C013:3:targetOpponentDeck";
    private const string ContinuationKeyC013CriminalTarget = "continuation:character:C013:4:targetOpponentDeck";
    private const string ContinuationKeyC013CriminalTakeRevealed = "continuation:character:C013:4:takeRevealed";
    private const string InputTypeKeyC013DiscardCard = "character:C013:discardCard";
    private const string InputTypeKeyC013TargetPlayer = "character:C013:targetPlayer";
    private const string InputTypeKeyC013TakeRevealed = "character:C013:takeRevealed";
    private const string ContextKeyC013AbnormalTasteDiscard = "character:C013:2:discardForDefenseValue";
    private const string ContextKeyC013DarkJadeFistTarget = "character:C013:3:targetOpponentDeck";
    private const string ContextKeyC013CriminalTarget = "character:C013:4:targetOpponentDeck";
    private const string ContextKeyC013CriminalTakeRevealed = "character:C013:4:takeRevealed";
    private const string ChoiceKeyC013TakeRevealed = "c013Criminal:take";
    private const string ChoiceKeyC013KeepRevealed = "c013Criminal:decline";
    private const string LocalStateKeyC013CriminalTargetPlayerId = "character:C013:4:targetPlayerId";
    private const string LocalStateKeyC013CriminalRevealedCardInstanceId = "character:C013:4:revealedCardInstanceId";
    private const string LocalStateKeyC013AbnormalTasteDefenseCardInstanceId = "character:C013:2:defenseCardInstanceId";
    private const string LocalStateKeyC013AbnormalTasteDefenderPlayerId = "character:C013:2:defenderPlayerId";
    private const string LocalStateKeyC013AbnormalTasteDefenderCharacterInstanceId = "character:C013:2:defenderCharacterInstanceId";
    private const string LocalStateKeyC013AbnormalTasteProducedStartIndex = "character:C013:2:producedStartIndex";
    private const string RevealReasonKeyC013DarkJadeFist = "character:C013:3:darkJadeFist";
    private const string RevealReasonKeyC013Criminal = "character:C013:4:criminal";
    private const string SkillKeyC018_2 = "C018:2";
    private const string SkillKeyC018_3 = "C018:3";
    private const string SkillKeyC018_4 = "C018:4";
    private const string LocalStateKeyC018SequenceKind = "character:C018:sequenceKind";
    private const string LocalStateKeyC018RemainingTargetCharacterIds = "character:C018:remainingTargetCharacterIds";
    private const string C018SequenceKindDirectDamage = "directDamageAllOthers";
    private const string C018SequenceKindDirectKill = "directKillHpOneOpponents";
    private const string ActivationSkillTypeRaw = "启动";
    private const string SkillKeyC021_1 = "C021:1";
    private const string SkillKeyC021_2 = "C021:2";
    private const string SkillKeyC021_3 = "C021:3";
    private const string SkillKeyC021_4 = "C021:4";
    private const string ContinuationKeyC021CurryDiscard = "continuation:character:C021:1:discard";
    private const string ContinuationKeyC021BlackKeyDiscard = "continuation:character:C021:2:discard";
    private const string ContinuationKeyC021BlackKeyTarget = "continuation:character:C021:2:targetOpponent";
    private const string ContinuationKeyC021FuneralChoice = "continuation:character:C021:3:funeralChoice";
    private const string ContinuationKeyC021SeventhTarget = "continuation:character:C021:4:targetOpponent";
    private const string ContinuationKeyC021SeventhDiscard = "continuation:character:C021:4:discard";
    private const string InputTypeKeyC021DiscardCard = "character:C021:discardCard";
    private const string InputTypeKeyC021TargetPlayer = "character:C021:targetPlayer";
    private const string InputTypeKeyC021FuneralChoice = "character:C021:funeralChoice";
    private const string ContextKeyC021CurryDiscard = "character:C021:1:discard";
    private const string ContextKeyC021BlackKeyDiscard = "character:C021:2:discard";
    private const string ContextKeyC021BlackKeyTarget = "character:C021:2:targetOpponent";
    private const string ContextKeyC021FuneralChoice = "character:C021:3:funeralChoice";
    private const string ContextKeyC021SeventhTarget = "character:C021:4:targetOpponent";
    private const string ContextKeyC021SeventhDiscard = "character:C021:4:discard";
    private const string ChoiceKeyC021FuneralUse = "c021Funeral:use";
    private const string LocalStateKeyC021BlackKeyUsedFreeMana = "character:C021:2:usedFreeMana";
    private const string LocalStateKeyC021BlackKeyFreeManaGranted = "character:C021:2:freeManaGranted";
    private const string LocalStateKeyC021SeventhTargetPlayerId = "character:C021:4:targetPlayerId";
    private const string LocalStateKeyC021SeventhRemainingDiscards = "character:C021:4:remainingDiscards";
    private const string LocalStateKeyC021FuneralSourcePlayerId = "character:C021:3:sourcePlayerId";
    private const string LocalStateKeyC021FuneralSourceCardInstanceId = "character:C021:3:sourceCardInstanceId";
    private const string LocalStateKeyC021FuneralSourceCharacterInstanceId = "character:C021:3:sourceCharacterInstanceId";
    private const string LocalStateKeyC021FuneralTargetCharacterInstanceId = "character:C021:3:targetCharacterInstanceId";
    private const string LocalStateKeyC021FuneralBaseDamageValue = "character:C021:3:baseDamageValue";
    private const string LocalStateKeyC021FuneralDamageType = "character:C021:3:damageType";
    private const string SkillKeyC022_1 = "C022:1";
    private const string SkillKeyC022_2 = "C022:2";
    private const string SkillKeyC022_3 = "C022:3";
    private const string SkillKeyC022_4 = "C022:4";
    private const string ContinuationKeyC022ReturnToVoidDefenseCard = "continuation:character:C022:2:defenseCard";
    private const string ContinuationKeyC022GrandGuignolTarget = "continuation:character:C022:3:targetOpponent";
    private const string ContinuationKeyC022DollWarSelectX = "continuation:character:C022:4:selectDoll";
    private const string ContinuationKeyC022DollWarTarget = "continuation:character:C022:4:targetOpponent";
    private const string ContinuationKeyC022DollWarTargetOptions = "continuation:character:C022:4:targetOptions";
    private const string ContinuationKeyC022DollWarTargetDiscard = "continuation:character:C022:4:targetDiscard";
    private const string InputTypeKeyC022DefenseCard = "character:C022:defenseCard";
    private const string InputTypeKeyC022TargetPlayer = "character:C022:targetPlayer";
    private const string InputTypeKeyC022DollAmount = "character:C022:dollAmount";
    private const string InputTypeKeyC022DollWarOptions = "character:C022:dollWarOptions";
    private const string InputTypeKeyC022DiscardCard = "character:C022:discardCard";
    private const string ContextKeyC022ReturnToVoidDefenseCard = "character:C022:2:defenseCard";
    private const string ContextKeyC022GrandGuignolTarget = "character:C022:3:targetOpponent";
    private const string ContextKeyC022DollWarSelectX = "character:C022:4:selectDoll";
    private const string ContextKeyC022DollWarTarget = "character:C022:4:targetOpponent";
    private const string ContextKeyC022DollWarTargetOptions = "character:C022:4:targetOptions";
    private const string ContextKeyC022DollWarTargetDiscard = "character:C022:4:targetDiscard";
    private const string ChoiceKeyC022DefenseCardPrefix = "c022DefenseCard:";
    private const string ChoiceKeyC022DollPrefix = "doll:";
    private const string ChoiceKeyC022DollWarAliceDraw = "c022DollWar:aliceDraw";
    private const string ChoiceKeyC022DollWarTargetDiscard = "c022DollWar:targetDiscard";
    private const string ChoiceKeyC022DollWarDirectDamage = "c022DollWar:directDamage";
    private const string ChoiceKeyC022DollWarReduceLeyline = "c022DollWar:reduceLeyline";
    private const string LocalStateKeyC022GrandGuignolDamageValue = "character:C022:3:damageValue";
    private const string LocalStateKeyC022DollWarX = "character:C022:4:x";
    private const string LocalStateKeyC022DollWarTargetPlayerId = "character:C022:4:targetPlayerId";
    private const string LocalStateKeyC022DollWarPendingLeylineReduction = "character:C022:4:pendingLeylineReduction";
    private const string LocalStateKeyC022DollWarPendingDirectDamage = "character:C022:4:pendingDirectDamage";
    private const string RevealReasonKeyC022GrandGuignol = "character:C022:3:grandGuignol";
    private const string SkillKeyC014_1 = "C014:1";
    private const string SkillKeyC014_2 = "C014:2";
    private const string SkillKeyC014_3 = "C014:3";
    private const string SkillKeyC014_4 = "C014:4";
    private const string ContinuationKeyC014AndThenChoice = "continuation:character:C014:2:chooseX";
    private const string ContinuationKeyC014AndThenDiscard = "continuation:character:C014:2:discardCards";
    private const string ContinuationKeyC014LaevateinDiscard = "continuation:character:C014:3:discard";
    private const string ContinuationKeyC014LaevateinTarget = "continuation:character:C014:3:targetOpponent";
    private const string ContinuationKeyC014RippleDiscard = "continuation:character:C014:4:discard";
    private const string ContinuationKeyC014RippleX = "continuation:character:C014:4:selectDestruction";
    private const string InputTypeKeyC014AndThenChoice = "character:C014:2:chooseX";
    private const string InputTypeKeyC014AndThenDiscard = "character:C014:2:discardCards";
    private const string InputTypeKeyC014DiscardCard = "character:C014:discardCard";
    private const string InputTypeKeyC014TargetPlayer = "character:C014:targetPlayer";
    private const string InputTypeKeyC014DestructionAmount = "character:C014:destructionAmount";
    private const string ContextKeyC014AndThenChoice = "character:C014:2:chooseX";
    private const string ContextKeyC014AndThenDiscard = "character:C014:2:discardCards";
    private const string ContextKeyC014LaevateinDiscard = "character:C014:3:discard";
    private const string ContextKeyC014LaevateinTarget = "character:C014:3:targetOpponent";
    private const string ContextKeyC014RippleDiscard = "character:C014:4:discard";
    private const string ContextKeyC014RippleX = "character:C014:4:selectDestruction";
    private const string ChoiceKeyC014AndThenXPrefix = "c014AndThen:x:";
    private const string ChoiceKeyC014DestructionPrefix = "destruction:";
    private const string LocalStateKeyC014AndThenSourcePlayerId = "character:C014:2:sourcePlayerId";
    private const string LocalStateKeyC014AndThenSourceCardInstanceId = "character:C014:2:sourceCardInstanceId";
    private const string LocalStateKeyC014AndThenSourceCharacterInstanceId = "character:C014:2:sourceCharacterInstanceId";
    private const string LocalStateKeyC014AndThenTargetCharacterInstanceId = "character:C014:2:targetCharacterInstanceId";
    private const string LocalStateKeyC014AndThenBaseDamageValue = "character:C014:2:baseDamageValue";
    private const string LocalStateKeyC014AndThenDamageType = "character:C014:2:damageType";
    private const string LocalStateKeyC014AndThenSelectedX = "character:C014:2:selectedX";
    private const string LocalStateKeyC014RippleRemainingDamages = "character:C014:4:remainingDamages";
    private const string LocalStateKeyC014RipplePendingSelfDamage = "character:C014:4:pendingSelfDamage";
    private const string LocalStateKeyC014RippleSequence = "character:C014:4:sequence";
    private const string LocalStateKeyC014RippleQueuedDamageContexts = "character:C014:4:queuedDamageContexts";
    private const string LocalStateKeyC014RippleBeforeDefenseResponsesConsumed = "character:C014:4:beforeDefenseResponsesConsumed";
    private const string LocalStateKeyC014RippleDamageBonus = "character:C014:4:damageBonus";
    private const string LocalStateKeyC014RippleForcePenetrate = "character:C014:4:forcePenetrate";
    private const string C014RippleSequenceCollecting = "collecting";
    private const string C014RippleSequenceResolving = "resolving";
    private const string SkillKeyC031_2 = "C031:2";
    private const string SkillKeyC031_3 = "C031:3";
    private const string SkillKeyC031_4 = "C031:4";
    private const string ContinuationKeyC031FarPunchSelectLeyline = "continuation:character:C031:2:selectLeyline";
    private const string ContinuationKeyC031FarPunchTarget = "continuation:character:C031:2:targetOpponent";
    private const string ContinuationKeyC031AlleyDeclare = "continuation:character:C031:3:declareCardName";
    private const string ContinuationKeyC031AlleyTarget = "continuation:character:C031:3:targetOpponentDeck";
    private const string InputTypeKeyC031LeylineAmount = "character:C031:leylineAmount";
    private const string InputTypeKeyC031TargetPlayer = "character:C031:targetPlayer";
    private const string InputTypeKeyC031DeclareCardName = "character:C031:declareCardName";
    private const string ContextKeyC031FarPunchLeyline = "character:C031:2:leyline";
    private const string ContextKeyC031FarPunchTarget = "character:C031:2:targetOpponent";
    private const string ContextKeyC031AlleyDeclare = "character:C031:3:declareCardName";
    private const string ContextKeyC031AlleyTarget = "character:C031:3:targetOpponentDeck";
    private const string ChoiceKeyLeylinePrefix = "leyline:";
    private const string LocalStateKeyC031FarPunchConsumedLeyline = "character:C031:2:consumedLeyline";
    private const string LocalStateKeyC031AlleyDeclaredDefinitionId = "character:C031:3:declaredDefinitionId";
    private const string RevealReasonKeyC031AlleyAlliance = "character:C031:3:alleyAlliance";
    private const string SkillKeyC029_4 = "C029:4";
    private const string MarkerTypeDream = "dream";
    private const string MarkerTypeSwordAura = "swordAura";
    private const string MarkerTypeDestruction = "destruction";
    private const string MarkerTypeDoll = "doll";
    private const string CharacterDefinitionIdC001 = "C001";
    private const string CharacterDefinitionIdC003 = "C003";
    private const string CharacterDefinitionIdC005 = "C005";
    private const string CharacterDefinitionIdC014 = "C014";
    private const string CharacterDefinitionIdC011 = "C011";
    private const string CharacterDefinitionIdC013 = "C013";
    private const string CharacterDefinitionIdC017 = "C017";
    private const string CharacterDefinitionIdC022 = "C022";
    private const string StatusKeyBarrier = "Barrier";
    private const string StatusKeySeal = "Seal";
    private const string StatusKeyShackle = "Shackle";
    private const string StatusKeyCharm = "Charm";
    private const string StatusKeySilence = "Silence";
    private const string StatusKeyPenetrate = "Penetrate";
    private const string TreasureDefinitionIdT014 = "T014";
    private const string TreasureDefinitionIdT015 = "T015";
    private const string TreasureDefinitionIdT020 = "T020";
    private const string TreasureDefinitionIdT025 = "T025";

    private enum DamageResponseStage
    {
        awaitDefense = 0,
        awaitCounter = 1,
    }

    private readonly struct SubmitInputChoiceContinuationState
    {
        public readonly string? pendingContinuationKey;
        public readonly bool isInputChoiceDamageContinuation;
        public readonly bool isTreasureOnPlayContinuation;
        public readonly bool isTreasureArrivalContinuation;
        public readonly bool isEndPhaseHandDiscardContinuation;
        public readonly bool isTurnStartShackleDiscardContinuation;
        public readonly bool isTurnStartC001BarrierContinuation;
        public readonly bool isTurnStartC013MechanicalJadeContinuation;
        public readonly bool isAnomalyContinuation;
        public readonly bool isMechanicalJadeContinuation;
        public readonly bool isTreasureDefenseContinuation;
        public readonly bool isT029DamageImmunityContinuation;
        public readonly bool isC001SkillContinuation;
        public readonly bool isC003SkillContinuation;
        public readonly bool isC007SkillContinuation;
        public readonly bool isC008SkillContinuation;
        public readonly bool isC004SkillContinuation;
        public readonly bool isC005SkillContinuation;
        public readonly bool isC005SixRealmsContinuation;
        public readonly bool isC011SkillContinuation;
        public readonly bool isC013SkillContinuation;
        public readonly bool isC021SkillContinuation;
        public readonly bool isC022SkillContinuation;
        public readonly bool isC014SkillContinuation;
        public readonly bool isC031SkillContinuation;

        public SubmitInputChoiceContinuationState(string? pendingContinuationKey)
        {
            this.pendingContinuationKey = pendingContinuationKey;
            isInputChoiceDamageContinuation =
                pendingContinuationKey == TemporaryOnPlayProbeResolver.ContinuationKeyInputChoiceDamage;
            isTreasureOnPlayContinuation =
                TreasureOnPlayEffectRuntime.isTreasureOnPlayContinuationKey(pendingContinuationKey);
            isTreasureArrivalContinuation =
                TreasureArrivalEffectRuntime.isTreasureArrivalContinuationKey(pendingContinuationKey);
            isEndPhaseHandDiscardContinuation =
                pendingContinuationKey == EndPhaseProcessor.ContinuationKeyEndPhaseHandDiscard;
            isTurnStartShackleDiscardContinuation =
                pendingContinuationKey == TurnTransitionProcessor.ContinuationKeyTurnStartShackleDiscard;
            isTurnStartC001BarrierContinuation =
                pendingContinuationKey == TurnTransitionProcessor.ContinuationKeyTurnStartC001Barrier;
            isTurnStartC013MechanicalJadeContinuation =
                pendingContinuationKey == TurnTransitionProcessor.ContinuationKeyTurnStartC013MechanicalJade;
            isAnomalyContinuation = AnomalyProcessor.isAnomalyContinuationKey(pendingContinuationKey);
            isMechanicalJadeContinuation = MechanicalJadeRuntime.isMechanicalJadeContinuationKey(pendingContinuationKey);
            isTreasureDefenseContinuation = TreasureDefenseEffectRuntime.isTreasureDefenseContinuationKey(pendingContinuationKey);
            isT029DamageImmunityContinuation =
                pendingContinuationKey == DamageProcessor.ContinuationKeyT029DamageImmunity;
            isC001SkillContinuation = isC001SkillContinuationKey(pendingContinuationKey);
            isC003SkillContinuation = isC003SkillContinuationKey(pendingContinuationKey);
            isC007SkillContinuation = isC007SkillContinuationKey(pendingContinuationKey);
            isC008SkillContinuation = isC008SkillContinuationKey(pendingContinuationKey);
            isC004SkillContinuation = isC004SkillContinuationKey(pendingContinuationKey);
            isC005SkillContinuation = isC005SkillContinuationKey(pendingContinuationKey);
            isC005SixRealmsContinuation = string.Equals(
                pendingContinuationKey,
                ContinuationKeyC005SixRealmsChoice,
                StringComparison.Ordinal);
            isC011SkillContinuation = isC011SkillContinuationKey(pendingContinuationKey);
            isC013SkillContinuation = isC013SkillContinuationKey(pendingContinuationKey);
            isC021SkillContinuation = isC021SkillContinuationKey(pendingContinuationKey);
            isC022SkillContinuation = isC022SkillContinuationKey(pendingContinuationKey);
            isC014SkillContinuation = isC014SkillContinuationKey(pendingContinuationKey);
            isC031SkillContinuation = isC031SkillContinuationKey(pendingContinuationKey);
        }
    }

    private readonly ZoneMovementService zoneMovementService;
    private readonly DamageProcessor damageProcessor;
    private readonly TreasureOnPlayEffectRuntime treasureOnPlayEffectRuntime;
    private readonly TreasureArrivalEffectRuntime treasureArrivalEffectRuntime;
    private readonly MechanicalJadeRuntime mechanicalJadeRuntime;
    private readonly TreasureBanishEffectRuntime treasureBanishEffectRuntime;
    private readonly TreasureDefenseEffectRuntime treasureDefenseEffectRuntime;
    private readonly TurnTransitionProcessor turnTransitionProcessor;
    private readonly EndPhaseProcessor endPhaseProcessor;
    private readonly AnomalyProcessor anomalyProcessor;
    private long nextResponseWindowNumericId;
    private long nextInputContextNumericId;

    public ActionRequestProcessor()
        : this(new ZoneMovementService(), new DamageProcessor())
    {
    }

    public ActionRequestProcessor(ZoneMovementService zoneMovementService)
        : this(zoneMovementService, new DamageProcessor())
    {
    }

    public ActionRequestProcessor(ZoneMovementService zoneMovementService, DamageProcessor damageProcessor)
    {
        this.zoneMovementService = zoneMovementService;
        this.damageProcessor = damageProcessor;
        Func<long> nextInputContextIdSupplier = () => Interlocked.Increment(ref nextInputContextNumericId);
        Func<long> nextResponseWindowIdSupplier = () => Interlocked.Increment(ref nextResponseWindowNumericId);
        treasureArrivalEffectRuntime = new TreasureArrivalEffectRuntime(
            nextInputContextIdSupplier,
            zoneMovementService);
        anomalyProcessor = new AnomalyProcessor(
            zoneMovementService,
            nextInputContextIdSupplier,
            tryOpenTreasureArrivalEffectsFromProducedEventsForAnomaly,
            damageProcessor);
        treasureOnPlayEffectRuntime = new TreasureOnPlayEffectRuntime(
            nextInputContextIdSupplier,
            nextResponseWindowIdSupplier,
            zoneMovementService,
            this.damageProcessor,
            anomalyProcessor.forceResolveCurrentAnomalyFromExternalEffect,
            tryOpenBeforeDefenseCharacterResponses);
        mechanicalJadeRuntime = new MechanicalJadeRuntime(
            nextInputContextIdSupplier,
            zoneMovementService);
        treasureBanishEffectRuntime = new TreasureBanishEffectRuntime(zoneMovementService);
        treasureDefenseEffectRuntime = new TreasureDefenseEffectRuntime(
            nextInputContextIdSupplier,
            zoneMovementService);
        endPhaseProcessor = new EndPhaseProcessor(
            zoneMovementService,
            nextInputContextIdSupplier);
        turnTransitionProcessor = new TurnTransitionProcessor(
            zoneMovementService,
            endPhaseProcessor,
            treasureArrivalEffectRuntime,
            nextInputContextIdSupplier);
    }

    public List<GameEvent> processActionRequest(GameState.GameState gameState, ActionRequest actionRequest)
    {
        var beforeInputContextId = gameState.currentInputContext?.inputContextId.Value;
        var beforeResponseWindowId = gameState.currentResponseWindow?.responseWindowId.Value;
        RuleTraceContext.Trace(
            "rule.begin",
            ("requestId", actionRequest.requestId),
            ("requestType", actionRequest.GetType().Name),
            ("actor", actionRequest.actorPlayerId.Value),
            ("source", actionRequest.sourceKey),
            ("matchState", gameState.matchState),
            ("turn", gameState.turnState?.turnNumber),
            ("phase", gameState.turnState?.currentPhase),
            ("currentPlayer", gameState.turnState?.currentPlayerId.Value),
            ("input", summarizeInputContext(gameState)),
            ("response", summarizeResponseWindow(gameState)),
            ("anomaly", gameState.currentAnomalyState?.currentAnomalyDefinitionId));
        try
        {
            var producedEvents = processActionRequestCore(gameState, actionRequest);
            RuleTraceContext.Trace(
                "rule.end",
                ("requestId", actionRequest.requestId),
                ("requestType", actionRequest.GetType().Name),
                ("events", producedEvents.Count),
                ("eventTypes", string.Join(",", producedEvents.Select(gameEvent => gameEvent.eventTypeKey))),
                ("inputBefore", beforeInputContextId),
                ("inputAfter", gameState.currentInputContext?.inputContextId.Value),
                ("responseBefore", beforeResponseWindowId),
                ("responseAfter", gameState.currentResponseWindow?.responseWindowId.Value),
                ("input", summarizeInputContext(gameState)),
                ("response", summarizeResponseWindow(gameState)),
                ("anomaly", gameState.currentAnomalyState?.currentAnomalyDefinitionId));
            traceProducedEvents(producedEvents);
            return producedEvents;
        }
        catch (Exception exception)
        {
            RuleTraceContext.Trace(
                "rule.error",
                ("requestId", actionRequest.requestId),
                ("requestType", actionRequest.GetType().Name),
                ("error", exception.Message),
                ("input", summarizeInputContext(gameState)),
                ("response", summarizeResponseWindow(gameState)));
            throw;
        }
    }

    private List<GameEvent> processActionRequestCore(GameState.GameState gameState, ActionRequest actionRequest)
    {
        if (actionRequest is SubmitCharacterSelectionActionRequest submitCharacterSelectionActionRequest)
        {
            return CharacterSelectionRuntime.submitSelection(gameState, submitCharacterSelectionActionRequest);
        }

        ensureCanAcceptExternalActionRequest(gameState);

        if (actionRequest is PlayTreasureCardActionRequest playTreasureCardActionRequest)
        {
            return processPlayTreasureCardActionRequest(gameState, playTreasureCardActionRequest);
        }

        if (actionRequest is SummonTreasureCardActionRequest summonTreasureCardActionRequest)
        {
            return processSummonTreasureCardActionRequest(gameState, summonTreasureCardActionRequest);
        }

        if (actionRequest is DrawOneCardActionRequest drawOneCardActionRequest)
        {
            return processDrawOneCardActionRequest(gameState, drawOneCardActionRequest);
        }

        if (actionRequest is UseSkillActionRequest useSkillActionRequest)
        {
            return processUseSkillActionRequest(gameState, useSkillActionRequest);
        }

        if (actionRequest is EnterActionPhaseActionRequest enterActionPhaseActionRequest)
        {
            return processEnterActionPhaseActionRequest(gameState, enterActionPhaseActionRequest);
        }

        if (actionRequest is EnterSummonPhaseActionRequest enterSummonPhaseActionRequest)
        {
            return processEnterSummonPhaseActionRequest(gameState, enterSummonPhaseActionRequest);
        }

        if (actionRequest is EnterEndPhaseActionRequest enterEndPhaseActionRequest)
        {
            return processEnterEndPhaseActionRequest(gameState, enterEndPhaseActionRequest);
        }

        if (actionRequest is StartNextTurnActionRequest startNextTurnActionRequest)
        {
            return processStartNextTurnActionRequest(gameState, startNextTurnActionRequest);
        }

        if (actionRequest is TryResolveAnomalyActionRequest tryResolveAnomalyActionRequest)
        {
            return processTryResolveAnomalyActionRequest(gameState, tryResolveAnomalyActionRequest);
        }

        if (actionRequest is SubmitDefenseActionRequest submitDefenseActionRequest)
        {
            return processSubmitDefenseActionRequest(gameState, submitDefenseActionRequest);
        }

        if (actionRequest is SubmitDamageCounterActionRequest submitDamageCounterActionRequest)
        {
            return processSubmitDamageCounterActionRequest(gameState, submitDamageCounterActionRequest);
        }

        if (actionRequest is SubmitResponseActionRequest submitResponseActionRequest)
        {
            return processSubmitResponseActionRequest(gameState, submitResponseActionRequest);
        }

        if (actionRequest is OpenDamageResponseWindowActionRequest openDamageResponseWindowActionRequest)
        {
            return processOpenDamageResponseWindowActionRequest(gameState, openDamageResponseWindowActionRequest);
        }

        if (actionRequest is OpenInputContextActionRequest openInputContextActionRequest)
        {
            return processOpenInputContextActionRequest(gameState, openInputContextActionRequest);
        }

        if (actionRequest is SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
        {
            return processSubmitInputChoiceActionRequest(gameState, submitInputChoiceActionRequest);
        }

        throw new NotSupportedException("Only PlayTreasureCardActionRequest, SummonTreasureCardActionRequest, DrawOneCardActionRequest, UseSkillActionRequest, EnterActionPhaseActionRequest, EnterSummonPhaseActionRequest, EnterEndPhaseActionRequest, StartNextTurnActionRequest, TryResolveAnomalyActionRequest, SubmitDefenseActionRequest, SubmitDamageCounterActionRequest, SubmitResponseActionRequest, OpenDamageResponseWindowActionRequest, OpenInputContextActionRequest, and SubmitInputChoiceActionRequest are supported in current RuleCore flow.");
    }

    private static void traceProducedEvents(List<GameEvent> producedEvents)
    {
        foreach (var gameEvent in producedEvents)
        {
            RuleTraceContext.Trace(
                "rule.event",
                ("eventId", gameEvent.eventId),
                ("eventType", gameEvent.eventTypeKey),
                ("sourceActionChainId", gameEvent.sourceActionChainId?.Value));
        }
    }

    private static string summarizeInputContext(GameState.GameState gameState)
    {
        var input = gameState.currentInputContext;
        if (input is null)
        {
            return "(none)";
        }

        var required = input.requiredPlayerIds.Count > 0
            ? string.Join(",", input.requiredPlayerIds.Select(playerId => playerId.Value))
            : input.requiredPlayerId?.Value.ToString() ?? "(none)";
        var submitted = input.submittedPlayerIds.Count > 0
            ? string.Join(",", input.submittedPlayerIds.Select(playerId => playerId.Value))
            : "(none)";
        return $"id={input.inputContextId.Value};type={input.inputTypeKey};context={input.contextKey};required={required};submitted={submitted};choices={input.choiceKeys.Count}";
    }

    private static string summarizeResponseWindow(GameState.GameState gameState)
    {
        var response = gameState.currentResponseWindow;
        if (response is null)
        {
            return "(none)";
        }

        return $"id={response.responseWindowId.Value};type={response.windowTypeKey};stage={response.pendingDamageResponseStageKey};responder={response.currentResponderPlayerId?.Value};damage={response.pendingDamageBaseDamageValue};damageType={response.pendingDamageTypeKey};sourcePlayer={response.pendingDamageSourcePlayerId?.Value};defender={response.pendingDamageDefenderPlayerId?.Value}";
    }

    private static void ensureCanAcceptExternalActionRequest(GameState.GameState gameState)
    {
        if (gameState.matchState == GameState.MatchState.initializing &&
            gameState.characterSelectionState is { isCompleted: false })
        {
            throw new InvalidOperationException(
                "ActionRequestProcessor cannot accept gameplay action requests before character selection completes.");
        }

        if (gameState.matchState == GameState.MatchState.ended)
        {
            throw new InvalidOperationException("ActionRequestProcessor cannot accept new action requests when gameState.matchState is ended.");
        }
    }

    private List<GameEvent> processPlayTreasureCardActionRequest(
        GameState.GameState gameState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        ensurePlayTreasureCardActionRequestGuard(gameState, playTreasureCardActionRequest);
        var playMoveReason = resolvePlayTreasureCardMoveReason(playTreasureCardActionRequest.playMode);

        var cardInstance = gameState.cardInstances[playTreasureCardActionRequest.cardInstanceId];
        var actorPlayerState = gameState.players[playTreasureCardActionRequest.actorPlayerId];
        var sourceZoneState = gameState.zones[cardInstance.zoneId];
        var targetZoneState = gameState.zones[actorPlayerState.fieldZoneId];

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(playTreasureCardActionRequest.requestId),
            actorPlayerId = playTreasureCardActionRequest.actorPlayerId,
            rootActionRequest = playTreasureCardActionRequest,
            isCompleted = false,
            currentFrameIndex = 0,
        };

        var effectFrame = new EffectFrame
        {
            effectKey = "moveCard",
            movingCardInstanceId = playTreasureCardActionRequest.cardInstanceId,
            fromZoneKey = sourceZoneState.zoneType,
            toZoneKey = targetZoneState.zoneType,
            moveReason = playMoveReason,
        };

        actionChainState.effectFrames.Add(effectFrame);
        gameState.currentActionChain = actionChainState;

        var cardMovedEvent = zoneMovementService.moveCard(
            gameState,
            cardInstance,
            actorPlayerState.fieldZoneId,
            playMoveReason,
            actionChainState.actionChainId,
            playTreasureCardActionRequest.requestId);

        actionChainState.currentFrameIndex = 1;
        actionChainState.producedEvents.Add(cardMovedEvent);
        if (cardInstance.zoneId == actorPlayerState.fieldZoneId)
        {
            cardInstance.isDefensePlacedOnField = playMoveReason == CardMoveReason.defensePlace;
        }

        if (playMoveReason == CardMoveReason.play && cardInstance.zoneId == actorPlayerState.fieldZoneId)
        {
            actorPlayerState.mana += TreasureResourceValueResolver.resolveManaGainOnEnterField(cardInstance.definitionId);
            actorPlayerState.sigilPreview += TreasureResourceValueResolver.resolveSigilPreviewGainOnEnterField(cardInstance.definitionId);
            actorPlayerState.skillPoint += TreasureResourceValueResolver.resolveSkillPointGainOnPlay(cardInstance.definitionId);
        }

        appendScriptedOnPlayEffectEvents(gameState, actionChainState, playTreasureCardActionRequest, cardInstance);
        tryApplyTreasureBanishEffectsFromProducedEvents(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            producedEventsStartIndex: 1);
        tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            producedEventsStartIndex: 1);
        if (gameState.currentInputContext is null && gameState.currentResponseWindow is null)
        {
            actionChainState.isCompleted = true;
        }

        return actionChainState.producedEvents;
    }

    private static CardMoveReason resolvePlayTreasureCardMoveReason(string playMode)
    {
        if (playMode == "normal" || playMode == "play")
        {
            return CardMoveReason.play;
        }

        if (playMode == "defense")
        {
            return CardMoveReason.defensePlace;
        }

        throw new InvalidOperationException("PlayTreasureCardActionRequest playMode must be normal or defense (legacy play is still accepted).");
    }

    private List<GameEvent> processSummonTreasureCardActionRequest(
        GameState.GameState gameState,
        SummonTreasureCardActionRequest summonTreasureCardActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires gameState.turnState to be initialized.");
        }

        if (gameState.turnState.currentPhase != GameState.TurnPhase.summon)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires gameState.turnState.currentPhase to be summon.");
        }

        if (summonTreasureCardActionRequest.actorPlayerId != gameState.turnState.currentPlayerId)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires gameState.currentInputContext to be null.");
        }

        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires gameState.currentResponseWindow to be null.");
        }

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires gameState.publicState to be initialized.");
        }

        if (!gameState.cardInstances.TryGetValue(summonTreasureCardActionRequest.cardInstanceId, out var cardInstance))
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires cardInstanceId to exist in gameState.cardInstances.");
        }

        var isSummonZoneSource = cardInstance.zoneId == gameState.publicState.summonZoneId;
        var isSakuraCakeDeckSource = cardInstance.zoneId == gameState.publicState.sakuraCakeDeckZoneId;
        if (!isSummonZoneSource && !isSakuraCakeDeckSource)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires cardInstance.zoneId to be in gameState.publicState.summonZoneId or gameState.publicState.sakuraCakeDeckZoneId.");
        }

        var actorPlayerState = gameState.players[summonTreasureCardActionRequest.actorPlayerId];
        if (!actorPlayerState.isSigilLocked)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires actor player sigil to be locked.");
        }

        if (!actorPlayerState.lockedSigil.HasValue)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires actor player lockedSigil to be initialized.");
        }

        var summonSigilCost = TreasureResourceValueResolver.resolveSummonSigilCost(cardInstance.definitionId);
        var requiredSigilPayment = Math.Max(1, summonSigilCost - actorPlayerState.summonSigilDiscount);
        if (actorPlayerState.lockedSigil.Value < requiredSigilPayment)
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires actor player lockedSigil to be sufficient for summon cost.");
        }

        if (!gameState.zones.TryGetValue(cardInstance.zoneId, out var sourceZoneState))
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires summon source zone to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(actorPlayerState.discardZoneId, out var targetZoneState))
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires actor discard zone to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.publicTreasureDeckZoneId, out var publicTreasureDeckZoneState))
        {
            throw new InvalidOperationException("SummonTreasureCardActionRequest requires publicTreasureDeck zone to exist in gameState.zones.");
        }

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(summonTreasureCardActionRequest.requestId),
            actorPlayerId = summonTreasureCardActionRequest.actorPlayerId,
            rootActionRequest = summonTreasureCardActionRequest,
            isCompleted = false,
            currentFrameIndex = 0,
        };

        var effectFrame = new EffectFrame
        {
            effectKey = "summonTreasure",
            movingCardInstanceId = summonTreasureCardActionRequest.cardInstanceId,
            fromZoneKey = sourceZoneState.zoneType,
            toZoneKey = targetZoneState.zoneType,
            moveReason = CardMoveReason.summon,
        };

        actionChainState.effectFrames.Add(effectFrame);
        gameState.currentActionChain = actionChainState;
        actorPlayerState.lockedSigil = actorPlayerState.lockedSigil.Value - requiredSigilPayment;

        var cardMovedEvent = zoneMovementService.moveCard(
            gameState,
            cardInstance,
            actorPlayerState.discardZoneId,
            CardMoveReason.summon,
            actionChainState.actionChainId,
            summonTreasureCardActionRequest.requestId);

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.producedEvents.Add(cardMovedEvent);

        var enteredSummonZoneCardInstanceIds = new List<CardInstanceId>();
        if (isSummonZoneSource && publicTreasureDeckZoneState.cardInstanceIds.Count > 0)
        {
            var topPublicTreasureCardInstanceId = publicTreasureDeckZoneState.cardInstanceIds[0];
            var topPublicTreasureCardInstance = gameState.cardInstances[topPublicTreasureCardInstanceId];
            var refillSummonZoneEvent = zoneMovementService.moveCard(
                gameState,
                topPublicTreasureCardInstance,
                gameState.publicState.summonZoneId,
                CardMoveReason.reveal,
                actionChainState.actionChainId,
                summonTreasureCardActionRequest.requestId);
            actionChainState.producedEvents.Add(refillSummonZoneEvent);
            enteredSummonZoneCardInstanceIds.Add(topPublicTreasureCardInstanceId);
        }

        if (enteredSummonZoneCardInstanceIds.Count > 0 &&
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            treasureArrivalEffectRuntime.tryStartArrivalEffectsForEnteredSummonZoneCards(
                gameState,
                actionChainState,
                summonTreasureCardActionRequest.requestId,
                enteredSummonZoneCardInstanceIds);
        }

        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);
        return actionChainState.producedEvents;
    }

    private List<GameEvent> processDrawOneCardActionRequest(
        GameState.GameState gameState,
        DrawOneCardActionRequest drawOneCardActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires gameState.turnState to be initialized.");
        }

        if (drawOneCardActionRequest.actorPlayerId != gameState.turnState.currentPlayerId)
        {
            throw new InvalidOperationException("DrawOneCardActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires gameState.currentInputContext to be null.");
        }

        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires gameState.currentResponseWindow to be null.");
        }

        if (!gameState.players.TryGetValue(drawOneCardActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires actorPlayerId to exist in gameState.players.");
        }

        if (!gameState.zones.TryGetValue(actorPlayerState.deckZoneId, out var deckZoneState))
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires actor player's deckZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(actorPlayerState.handZoneId))
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires actor player's handZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(actorPlayerState.discardZoneId, out var discardZoneState))
        {
            throw new InvalidOperationException("DrawOneCardActionRequest requires actor player's discardZoneId to exist in gameState.zones.");
        }

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(drawOneCardActionRequest.requestId),
            actorPlayerId = drawOneCardActionRequest.actorPlayerId,
            rootActionRequest = drawOneCardActionRequest,
            isCompleted = false,
            currentFrameIndex = 0,
        };

        actionChainState.effectFrames.Add(new EffectFrame
        {
            effectKey = "drawOneCard",
            sourcePlayerId = drawOneCardActionRequest.actorPlayerId,
            fromZoneKey = ZoneKey.deck,
            toZoneKey = ZoneKey.hand,
            moveReason = CardMoveReason.draw,
        });
        gameState.currentActionChain = actionChainState;

        if (deckZoneState.cardInstanceIds.Count == 0 && discardZoneState.cardInstanceIds.Count > 0)
        {
            var discardCardIdsInCurrentOrder = PlayerDeckRuntime.createShuffledCardInstanceIds(discardZoneState.cardInstanceIds);
            foreach (var cardInstanceId in discardCardIdsInCurrentOrder)
            {
                var discardedCardInstance = gameState.cardInstances[cardInstanceId];
                var rebuildEvent = zoneMovementService.moveCard(
                    gameState,
                    discardedCardInstance,
                    actorPlayerState.deckZoneId,
                    CardMoveReason.returnToSource,
                    actionChainState.actionChainId,
                    drawOneCardActionRequest.requestId);
                actionChainState.producedEvents.Add(rebuildEvent);
            }
        }

        if (deckZoneState.cardInstanceIds.Count > 0)
        {
            var topCardInstanceId = deckZoneState.cardInstanceIds[0];
            var topCardInstance = gameState.cardInstances[topCardInstanceId];
            var drawEvent = zoneMovementService.moveCard(
                gameState,
                topCardInstance,
                actorPlayerState.handZoneId,
                CardMoveReason.draw,
                actionChainState.actionChainId,
                drawOneCardActionRequest.requestId);
            actionChainState.producedEvents.Add(drawEvent);
        }

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);
        return actionChainState.producedEvents;
    }

    private List<GameEvent> processUseSkillActionRequest(
        GameState.GameState gameState,
        UseSkillActionRequest useSkillActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires gameState.turnState to be initialized.");
        }

        if (useSkillActionRequest.actorPlayerId != gameState.turnState.currentPlayerId)
        {
            throw new InvalidOperationException("UseSkillActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.");
        }

        if (gameState.turnState.currentPhase != GameState.TurnPhase.action)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires gameState.turnState.currentPhase to be action.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires gameState.currentInputContext to be null.");
        }

        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires gameState.currentResponseWindow to be null.");
        }

        if (!gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("UseSkillActionRequest requires actorPlayerId to exist in gameState.players.");
        }

        if (!gameState.characterInstances.TryGetValue(useSkillActionRequest.characterInstanceId, out var characterInstance))
        {
            throw new InvalidOperationException("UseSkillActionRequest requires characterInstanceId to exist in gameState.characterInstances.");
        }

        if (characterInstance.ownerPlayerId != useSkillActionRequest.actorPlayerId)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires characterInstance.ownerPlayerId to equal actorPlayerId.");
        }

        var characterDefinition = CharacterDefinitionRepository.resolveByDefinitionId(characterInstance.definitionId);
        if (!characterDefinition.skills.TryGetValue(useSkillActionRequest.skillKey, out var skillDefinition))
        {
            throw new InvalidOperationException("UseSkillActionRequest requires skillKey to exist on actor character definition.");
        }

        if (characterInstance.isActivated &&
            string.Equals(skillDefinition.skillTypeRaw, ActivationSkillTypeRaw, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("UseSkillActionRequest requires activated character to not use activation skill again.");
        }

        var (manaCost, leylineCost) = resolveUseSkillResourceCost(
            characterInstance.definitionId,
            useSkillActionRequest.skillKey);
        var shouldConsumeC021BlackKeyFreeMana =
            string.Equals(useSkillActionRequest.skillKey, SkillKeyC021_2, StringComparison.Ordinal) &&
            StatusRuntime.hasStatusOnPlayer(
                gameState,
                useSkillActionRequest.actorPlayerId,
                StatusRuntime.StatusKeyC021BlackKeyProjectionFreeMana);
        if (shouldConsumeC021BlackKeyFreeMana)
        {
            manaCost = 0;
        }

        if (actorPlayerState.skillPoint < 1)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires actor player skillPoint to be at least 1.");
        }

        if (actorPlayerState.mana < manaCost)
        {
            throw new InvalidOperationException("UseSkillActionRequest requires actor player mana to be sufficient for skill cost.");
        }

        if (leylineCost > 0)
        {
            var actorTeamState = gameState.teams[actorPlayerState.teamId];
            if (actorTeamState.leyline < leylineCost)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires team leyline to be sufficient for skill cost.");
            }
        }

        if (isOncePerTurnSkill(useSkillActionRequest.skillKey) &&
            gameState.turnState.usedOncePerTurnSkillKeys.Contains(useSkillActionRequest.skillKey))
        {
            throw new InvalidOperationException("UseSkillActionRequest requires once-per-turn skill to not have been used this turn.");
        }

        if (requiresOpponentTarget(useSkillActionRequest.skillKey))
        {
            if (!useSkillActionRequest.targetCharacterInstanceId.HasValue)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires targetCharacterInstanceId for this skill.");
            }

            if (!gameState.characterInstances.TryGetValue(
                    useSkillActionRequest.targetCharacterInstanceId.Value,
                    out var opponentTargetInstance))
            {
                throw new InvalidOperationException("UseSkillActionRequest requires targetCharacterInstanceId to exist in gameState.characterInstances.");
            }

            if (!gameState.players.TryGetValue(opponentTargetInstance.ownerPlayerId, out var opponentTargetPlayerState))
            {
                throw new InvalidOperationException("UseSkillActionRequest requires target character ownerPlayerId to exist in gameState.players.");
            }

            if (opponentTargetPlayerState.teamId == actorPlayerState.teamId)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires targetCharacterInstanceId to belong to an opponent player.");
            }

            if (!opponentTargetInstance.isAlive || !opponentTargetInstance.isInPlay)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires target character to be alive and in play.");
            }

        }

        if (requiresAllyTarget(useSkillActionRequest.skillKey))
        {
            if (!useSkillActionRequest.targetAllyCharacterInstanceId.HasValue)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires targetAllyCharacterInstanceId for this skill.");
            }

            if (!gameState.characterInstances.TryGetValue(
                    useSkillActionRequest.targetAllyCharacterInstanceId.Value,
                    out var allyTargetInstance))
            {
                throw new InvalidOperationException("UseSkillActionRequest requires targetAllyCharacterInstanceId to exist in gameState.characterInstances.");
            }

            if (!gameState.players.TryGetValue(allyTargetInstance.ownerPlayerId, out var allyTargetPlayerState))
            {
                throw new InvalidOperationException("UseSkillActionRequest requires ally target character ownerPlayerId to exist in gameState.players.");
            }

            if (allyTargetPlayerState.teamId != actorPlayerState.teamId)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires targetAllyCharacterInstanceId to belong to an ally player.");
            }

            if (!allyTargetInstance.isAlive || !allyTargetInstance.isInPlay)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires ally target character to be alive and in play.");
            }
        }

        var hasMarkerCost = tryGetMarkerCost(
            useSkillActionRequest.skillKey,
            out var markerCostType,
            out var markerCostAmount);

        if (hasMarkerCost)
        {
            if (MarkerRuntime.getMarkerCount(characterInstance, markerCostType) < markerCostAmount)
            {
                throw new InvalidOperationException("UseSkillActionRequest requires sufficient markers for skill marker cost.");
            }
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC001_1, StringComparison.Ordinal) &&
            collectOpponentPlayerChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C001:1 requires at least one alive opponent target.");
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC007_3, StringComparison.Ordinal) &&
            collectC007OpponentChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C007:3 requires at least one alive opponent target.");
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC007_4, StringComparison.Ordinal))
        {
            if (collectC007FriendlyChoiceKeys(gameState, actorPlayerState).Count == 0 ||
                collectC007OpponentChoiceKeys(gameState, actorPlayerState).Count == 0)
            {
                throw new InvalidOperationException("UseSkillActionRequest C007:4 requires at least one alive friendly target and one alive opponent target.");
            }
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC008_1, StringComparison.Ordinal) &&
            collectOpponentPlayerChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C008:1 requires at least one alive opponent target.");
        }

        if ((string.Equals(useSkillActionRequest.skillKey, SkillKeyC003_1, StringComparison.Ordinal) ||
             string.Equals(useSkillActionRequest.skillKey, SkillKeyC003_3, StringComparison.Ordinal)) &&
            collectOpponentPlayerChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C003 targeted damage skill requires at least one alive opponent target.");
        }

        if ((string.Equals(useSkillActionRequest.skillKey, SkillKeyC004_1, StringComparison.Ordinal) ||
              string.Equals(useSkillActionRequest.skillKey, SkillKeyC004_4, StringComparison.Ordinal)) &&
            collectOpponentPlayerChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C004 targeted damage skill requires at least one alive opponent target.");
        }

        if ((string.Equals(useSkillActionRequest.skillKey, SkillKeyC005_3, StringComparison.Ordinal) ||
             string.Equals(useSkillActionRequest.skillKey, SkillKeyC005_4, StringComparison.Ordinal)) &&
            collectOpponentPlayerChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C005 targeted damage skill requires at least one alive opponent target.");
        }

        if ((string.Equals(useSkillActionRequest.skillKey, SkillKeyC011_1, StringComparison.Ordinal) ||
              string.Equals(useSkillActionRequest.skillKey, SkillKeyC011_2, StringComparison.Ordinal)) &&
            collectOpponentPlayerChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C011 targeted skill requires at least one alive opponent target.");
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC011_3, StringComparison.Ordinal) &&
            characterInstance.isActivated)
        {
            throw new InvalidOperationException("UseSkillActionRequest C011:3 requires character to not already be activated.");
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC011_4, StringComparison.Ordinal) &&
            collectSummonZoneCardChoiceKeys(gameState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C011:4 requires at least one summonZone card.");
        }

        if ((string.Equals(useSkillActionRequest.skillKey, SkillKeyC013_3, StringComparison.Ordinal) ||
             string.Equals(useSkillActionRequest.skillKey, SkillKeyC013_4, StringComparison.Ordinal)) &&
            collectOpponentPlayerWithNonEmptyDeckChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C013 deck-top skill requires at least one opponent with non-empty deck.");
        }

        if ((string.Equals(useSkillActionRequest.skillKey, SkillKeyC021_1, StringComparison.Ordinal) ||
             string.Equals(useSkillActionRequest.skillKey, SkillKeyC021_2, StringComparison.Ordinal)) &&
            collectPlayerHandDiscardChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C021 discard skill requires at least one hand card.");
        }

        if ((string.Equals(useSkillActionRequest.skillKey, SkillKeyC021_2, StringComparison.Ordinal) ||
              string.Equals(useSkillActionRequest.skillKey, SkillKeyC021_4, StringComparison.Ordinal)) &&
            collectOpponentPlayerChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C021 targeted skill requires at least one alive opponent target.");
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC022_2, StringComparison.Ordinal) &&
            collectC022DefensePlacedCardChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C022:2 requires at least one opponent defense-placed card and its ally target.");
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC022_3, StringComparison.Ordinal) &&
            collectOpponentPlayerChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C022:3 requires at least one alive opponent target.");
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC022_4, StringComparison.Ordinal) &&
            collectOpponentPlayerChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C022:4 requires at least one alive opponent target.");
        }

        if ((string.Equals(useSkillActionRequest.skillKey, SkillKeyC014_3, StringComparison.Ordinal) ||
             string.Equals(useSkillActionRequest.skillKey, SkillKeyC014_4, StringComparison.Ordinal)) &&
            collectPlayerHandDiscardChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C014 discard skill requires at least one hand card.");
        }

        if ((string.Equals(useSkillActionRequest.skillKey, SkillKeyC014_3, StringComparison.Ordinal) ||
              string.Equals(useSkillActionRequest.skillKey, SkillKeyC014_4, StringComparison.Ordinal)) &&
            collectOpponentPlayerChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C014 damage skill requires at least one alive opponent target.");
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC031_2, StringComparison.Ordinal) &&
            collectOpponentPlayerChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C031:2 requires at least one alive opponent target.");
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC031_3, StringComparison.Ordinal) &&
            collectOpponentPlayerWithNonEmptyDeckChoiceKeys(gameState, actorPlayerState).Count == 0)
        {
            throw new InvalidOperationException("UseSkillActionRequest C031:3 requires at least one opponent with non-empty deck.");
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC001_3, StringComparison.Ordinal))
        {
            if (gameState.turnState.hasResolvedAnomalyThisTurn)
            {
                throw new InvalidOperationException("UseSkillActionRequest C001:3 cannot be used after an anomaly has already been resolved this turn.");
            }

            if (gameState.currentAnomalyState is null ||
                string.IsNullOrWhiteSpace(gameState.currentAnomalyState.currentAnomalyDefinitionId))
            {
                throw new InvalidOperationException("UseSkillActionRequest C001:3 requires an active anomaly on the field.");
            }
        }

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(useSkillActionRequest.requestId),
            actorPlayerId = useSkillActionRequest.actorPlayerId,
            rootActionRequest = useSkillActionRequest,
            isCompleted = false,
            currentFrameIndex = 0,
        };

        actionChainState.effectFrames.Add(new EffectFrame
        {
            effectKey = "useSkill",
            sourcePlayerId = useSkillActionRequest.actorPlayerId,
            sourceCharacterInstanceId = useSkillActionRequest.characterInstanceId,
            contextKey = useSkillActionRequest.skillKey,
        });

        gameState.currentActionChain = actionChainState;
        SkillPresentationEventEmitter.appendIfNeeded(
            actionChainState,
            useSkillActionRequest.actorPlayerId,
            characterInstance,
            useSkillActionRequest.skillKey,
            "active");
        actorPlayerState.mana -= manaCost;
        actorPlayerState.skillPoint -= 1;
        if (leylineCost > 0)
        {
            gameState.teams[actorPlayerState.teamId].leyline -= leylineCost;
        }

        if (shouldConsumeC021BlackKeyFreeMana)
        {
            consumeC021BlackKeyFreeManaStatus(
                gameState,
                actionChainState,
                useSkillActionRequest.actorPlayerId,
                useSkillActionRequest.requestId);
            actionChainState.localState[LocalStateKeyC021BlackKeyUsedFreeMana] = "true";
        }

        if (hasMarkerCost)
        {
            actionChainState.producedEvents.Add(MarkerRuntime.removeMarker(
                characterInstance,
                markerCostType,
                markerCostAmount,
                actionChainState.actionChainId,
                useSkillActionRequest.requestId));
        }

        if (isOncePerTurnSkill(useSkillActionRequest.skillKey))
        {
            gameState.turnState.usedOncePerTurnSkillKeys.Add(useSkillActionRequest.skillKey);
        }
        appendUseSkillEffectEvents(
            gameState,
            actionChainState,
            useSkillActionRequest,
            actorPlayerState,
            characterInstance);
        tryApplyTreasureBanishEffectsFromProducedEvents(
            gameState,
            actionChainState,
            useSkillActionRequest.requestId,
            producedEventsStartIndex: 0);
        if (!isSequencedSkill(useSkillActionRequest.skillKey))
        {
            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                producedEventsStartIndex: 0);
        }

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);
        return actionChainState.producedEvents;
    }

    private void appendUseSkillEffectEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        UseSkillActionRequest useSkillActionRequest,
        GameState.PlayerState actorPlayerState,
        CharacterInstance sourceCharacterInstance)
    {
        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC004_1, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC004TargetPlayer,
                ContextKeyC004MasterSparkTarget,
                ContinuationKeyC004MasterSparkTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC004_3, StringComparison.Ordinal))
        {
            var appliedStatus = StatusRuntime.applyStatus(
                gameState,
                new StatusInstance
                {
                    statusKey = StatusRuntime.StatusKeyPhysicalOrSpellDamageBoostNext,
                    applierPlayerId = useSkillActionRequest.actorPlayerId,
                    applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                    targetPlayerId = useSkillActionRequest.actorPlayerId,
                    stackCount = 2,
                    durationTypeKey = StatusRuntime.DurationTypeKeyNextMatchingDamageAttempt,
                });
            actionChainState.producedEvents.Add(new StatusChangedEvent
            {
                eventId = useSkillActionRequest.requestId,
                eventTypeKey = "statusChanged",
                sourceActionChainId = actionChainState.actionChainId,
                statusKey = appliedStatus.statusKey,
                targetPlayerId = appliedStatus.targetPlayerId,
                isApplied = true,
            });
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC004_4, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC004TargetPlayer,
                ContextKeyC004BlazingCometTarget,
                ContinuationKeyC004BlazingCometTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC005_3, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC005SwordAuraAmount,
                ContextKeyC005SakuraSparkSwordAura,
                ContinuationKeyC005SakuraSparkSelectSwordAura,
                collectC005SwordAuraAmountChoiceKeys(sourceCharacterInstance));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC005_4, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC005SwordAuraAmount,
                ContextKeyC005SpringWindSwordAura,
                ContinuationKeyC005SpringWindSelectSwordAura,
                collectC005SwordAuraAmountChoiceKeys(sourceCharacterInstance));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC021_1, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC021DiscardCard,
                ContextKeyC021CurryDiscard,
                ContinuationKeyC021CurryDiscard,
                collectPlayerHandDiscardChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC021_2, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC021DiscardCard,
                ContextKeyC021BlackKeyDiscard,
                ContinuationKeyC021BlackKeyDiscard,
                collectPlayerHandDiscardChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC021_4, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC021TargetPlayer,
                ContextKeyC021SeventhTarget,
                ContinuationKeyC021SeventhTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC002_2, StringComparison.Ordinal))
        {
            appendDrawOneCardEffectEventsForPlayer(
                gameState,
                actionChainState,
                actorPlayerState,
                useSkillActionRequest.requestId);
            foreach (var playerState in gameState.players.Values)
            {
                if (playerState.playerId == useSkillActionRequest.actorPlayerId)
                {
                    continue;
                }

                if (!tryFindAliveInPlayCharacterInstanceByOwner(
                        gameState,
                        playerState.playerId,
                        out var targetCharacterInstance))
                {
                    continue;
                }

                appendHealOnCharacter(
                    actionChainState,
                    useSkillActionRequest.requestId,
                    targetCharacterInstance,
                    healAmount: 2);
            }

            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC018_2, StringComparison.Ordinal))
        {
            startC018SkillSequence(
                gameState,
                actionChainState,
                useSkillActionRequest,
                actorPlayerState,
                C018SequenceKindDirectDamage);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC018_3, StringComparison.Ordinal))
        {
            startC018SkillSequence(
                gameState,
                actionChainState,
                useSkillActionRequest,
                actorPlayerState,
                C018SequenceKindDirectKill);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC018_4, StringComparison.Ordinal))
        {
            foreach (var playerState in gameState.players.Values)
            {
                if (playerState.teamId == actorPlayerState.teamId)
                {
                    continue;
                }

                if (!tryFindAliveInPlayCharacterInstanceByOwner(
                        gameState,
                        playerState.playerId,
                        out var targetCharacterInstance))
                {
                    continue;
                }

                if (targetCharacterInstance.currentHp <= 2)
                {
                    continue;
                }

                var hpBefore = targetCharacterInstance.currentHp;
                targetCharacterInstance.currentHp = 2;
                actionChainState.producedEvents.Add(new HpChangedEvent
                {
                    eventId = useSkillActionRequest.requestId,
                    eventTypeKey = "hpChanged",
                    sourceActionChainId = actionChainState.actionChainId,
                    targetPlayerId = playerState.playerId,
                    targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                    hpBefore = hpBefore,
                    hpAfter = 2,
                    delta = 2 - hpBefore,
                });
            }

            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC001_1, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC001TargetPlayer,
                ContextKeyC001SealTarget,
                ContinuationKeyC001SealTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC001_3, StringComparison.Ordinal))
        {
            actionChainState.producedEvents.Add(CharacterActivationRuntime.setActivated(
                gameState,
                useSkillActionRequest.characterInstanceId,
                true,
                actionChainState.actionChainId,
                useSkillActionRequest.requestId));
            anomalyProcessor.forceResolveCurrentAnomalyFromExternalEffect(
                gameState,
                actionChainState,
                useSkillActionRequest.actorPlayerId,
                useSkillActionRequest.requestId);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC001_4, StringComparison.Ordinal))
        {
            var barrierStatus = StatusRuntime.applyStatus(
                gameState,
                new StatusInstance
                {
                    statusKey = StatusKeyBarrier,
                    applierPlayerId = useSkillActionRequest.actorPlayerId,
                    applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                    targetPlayerId = useSkillActionRequest.actorPlayerId,
                    targetCharacterInstanceId = sourceCharacterInstance.characterInstanceId,
                    stackCount = 1,
                });
            actionChainState.producedEvents.Add(new StatusChangedEvent
            {
                eventId = useSkillActionRequest.requestId,
                eventTypeKey = "statusChanged",
                sourceActionChainId = actionChainState.actionChainId,
                statusKey = barrierStatus.statusKey,
                targetPlayerId = barrierStatus.targetPlayerId,
                targetCharacterInstanceId = barrierStatus.targetCharacterInstanceId,
                isApplied = true,
            });

            var deckZoneState = gameState.zones[actorPlayerState.deckZoneId];
            var deckTopCardIds = new List<CardInstanceId>();
            for (var i = 0; i < deckZoneState.cardInstanceIds.Count && deckTopCardIds.Count < 3; i++)
            {
                deckTopCardIds.Add(deckZoneState.cardInstanceIds[i]);
            }

            foreach (var cardInstanceId in deckTopCardIds)
            {
                var cardInstance = gameState.cardInstances[cardInstanceId];
                var moveEvent = zoneMovementService.moveCard(
                    gameState,
                    cardInstance,
                    actorPlayerState.fieldZoneId,
                    CardMoveReason.defenseLikePlace,
                    actionChainState.actionChainId,
                    useSkillActionRequest.requestId);
                actionChainState.producedEvents.Add(moveEvent);
                cardInstance.isDefensePlacedOnField = true;
            }

            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC003_1, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC003TargetPlayer,
                ContextKeyC003RedNightTarget,
                ContinuationKeyC003RedNightTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC003_3, StringComparison.Ordinal))
        {
            actionChainState.producedEvents.Add(CharacterActivationRuntime.setActivated(
                gameState,
                useSkillActionRequest.characterInstanceId,
                true,
                actionChainState.actionChainId,
                useSkillActionRequest.requestId));
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC003TargetPlayer,
                ContextKeyC003ScarletDestinyTarget,
                ContinuationKeyC003ScarletDestinyTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC003_4, StringComparison.Ordinal))
        {
            var drawCount = sourceCharacterInstance.isActivated ? 3 : 2;
            for (var drawIndex = 0; drawIndex < drawCount; drawIndex++)
            {
                appendDrawOneCardEffectEventsForPlayer(
                    gameState,
                    actionChainState,
                    actorPlayerState,
                    useSkillActionRequest.requestId);
            }

            foreach (var teamState in gameState.teams.Values)
            {
                if (teamState.teamId == actorPlayerState.teamId ||
                    teamState.leyline <= 0)
                {
                    continue;
                }

                teamState.leyline -= 1;
            }

            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC007_1, StringComparison.Ordinal))
        {
            if (MarkerRuntime.canAddMarker(sourceCharacterInstance, MarkerTypeDream))
            {
                actionChainState.producedEvents.Add(MarkerRuntime.addMarker(
                    sourceCharacterInstance,
                    MarkerTypeDream,
                    1,
                    actionChainState.actionChainId,
                    useSkillActionRequest.requestId));
            }

            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC007_2, StringComparison.Ordinal))
        {
            foreach (var playerState in gameState.players.Values)
            {
                if (playerState.teamId != actorPlayerState.teamId)
                {
                    continue;
                }

                if (playerState.playerId == useSkillActionRequest.actorPlayerId)
                {
                    continue;
                }

                appendDrawOneCardEffectEventsForPlayer(
                    gameState,
                    actionChainState,
                    playerState,
                    useSkillActionRequest.requestId);
                break;
            }

            if (MarkerRuntime.getMarkerCount(sourceCharacterInstance, MarkerTypeDream) == 3)
            {
                appendDrawOneCardEffectEventsForPlayer(
                    gameState,
                    actionChainState,
                    actorPlayerState,
                    useSkillActionRequest.requestId);
            }

            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC007_3, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC007TargetPlayer,
                ContextKeyC007NightmareTarget,
                ContinuationKeyC007NightmareTarget,
                collectC007OpponentChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC007_4, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC007TargetPlayer,
                ContextKeyC007DaydreamHealTarget,
                ContinuationKeyC007DaydreamHealTarget,
                collectC007FriendlyChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC008_1, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC008TargetPlayer,
                ContextKeyC008WindKingTarget,
                ContinuationKeyC008WindKingTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC008_2, StringComparison.Ordinal))
        {
            applyCharacterStatus(
                gameState,
                actionChainState,
                useSkillActionRequest,
                StatusKeyBarrier,
                targetPlayerId: useSkillActionRequest.actorPlayerId,
                targetCharacterInstanceId: sourceCharacterInstance.characterInstanceId);
            applyCharacterStatus(
                gameState,
                actionChainState,
                useSkillActionRequest,
                StatusRuntime.StatusKeyPhysicalDamageBoostNext,
                targetPlayerId: useSkillActionRequest.actorPlayerId,
                durationTypeKey: StatusRuntime.DurationTypeKeyNextMatchingDamageAttempt);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC008_3, StringComparison.Ordinal))
        {
            appendDrawOneCardEffectEventsForPlayer(
                gameState,
                actionChainState,
                actorPlayerState,
                useSkillActionRequest.requestId);
            appendDrawOneCardEffectEventsForPlayer(
                gameState,
                actionChainState,
                actorPlayerState,
                useSkillActionRequest.requestId);
            applyCharacterStatus(
                gameState,
                actionChainState,
                useSkillActionRequest,
                StatusRuntime.StatusKeySpellDamageBoostNext,
                targetPlayerId: useSkillActionRequest.actorPlayerId,
                durationTypeKey: StatusRuntime.DurationTypeKeyNextMatchingDamageAttempt);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC008_4, StringComparison.Ordinal))
        {
            startC008ExcaliburSequence(
                gameState,
                actionChainState,
                useSkillActionRequest,
                actorPlayerState);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC011_1, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC011TargetPlayer,
                ContextKeyC011MysticEyesTarget,
                ContinuationKeyC011MysticEyesTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC011_2, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC011TargetPlayer,
                ContextKeyC011TrueAncestorCombatTarget,
                ContinuationKeyC011TrueAncestorCombatTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC011_3, StringComparison.Ordinal))
        {
            actionChainState.producedEvents.Add(CharacterActivationRuntime.setActivated(
                gameState,
                useSkillActionRequest.characterInstanceId,
                true,
                actionChainState.actionChainId,
                useSkillActionRequest.requestId));
            applyCharacterStatus(
                gameState,
                actionChainState,
                useSkillActionRequest,
                StatusRuntime.StatusKeyC011AllDamageBoostThisTurn,
                targetPlayerId: useSkillActionRequest.actorPlayerId,
                durationTypeKey: "turnEnd");
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC011_4, StringComparison.Ordinal))
        {
            actionChainState.localState[LocalStateKeyC011RealityMarbleRemainingCount] = "3";
            continueC011RealityMarbleSequence(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC013_3, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC013TargetPlayer,
                ContextKeyC013DarkJadeFistTarget,
                ContinuationKeyC013DarkJadeFistTarget,
                collectOpponentPlayerWithNonEmptyDeckChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC013_4, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC013TargetPlayer,
                ContextKeyC013CriminalTarget,
                ContinuationKeyC013CriminalTarget,
                collectOpponentPlayerWithNonEmptyDeckChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC014_1, StringComparison.Ordinal))
        {
            for (var drawIndex = 0; drawIndex < 3; drawIndex++)
            {
                appendDrawOneCardEffectEventsForPlayer(
                    gameState,
                    actionChainState,
                    actorPlayerState,
                    useSkillActionRequest.requestId);
            }

            applyCharacterStatus(
                gameState,
                actionChainState,
                useSkillActionRequest,
                StatusRuntime.StatusKeyC014SkipSummonPhase,
                targetPlayerId: useSkillActionRequest.actorPlayerId,
                durationTypeKey: "turnEnd");
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC014_3, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC014DiscardCard,
                ContextKeyC014LaevateinDiscard,
                ContinuationKeyC014LaevateinDiscard,
                collectPlayerHandDiscardChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC014_4, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC014DiscardCard,
                ContextKeyC014RippleDiscard,
                ContinuationKeyC014RippleDiscard,
                collectPlayerHandDiscardChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC022_1, StringComparison.Ordinal))
        {
            if (MarkerRuntime.canAddMarker(sourceCharacterInstance, MarkerTypeDoll))
            {
                actionChainState.producedEvents.Add(MarkerRuntime.addMarker(
                    sourceCharacterInstance,
                    MarkerTypeDoll,
                    1,
                    actionChainState.actionChainId,
                    useSkillActionRequest.requestId));
            }

            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC022_2, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC022DefenseCard,
                ContextKeyC022ReturnToVoidDefenseCard,
                ContinuationKeyC022ReturnToVoidDefenseCard,
                collectC022DefensePlacedCardChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC022_3, StringComparison.Ordinal))
        {
            var revealedCards = drawCardsForPlayerReturningCards(
                gameState,
                actionChainState,
                actorPlayerState,
                2,
                useSkillActionRequest.requestId);
            var damageValue = Math.Max(
                0,
                revealedCards.Sum(card => TreasureResourceValueResolver.resolveDefenseValue(card.definitionId) ?? 0) - 2);
            foreach (var revealedCard in revealedCards)
            {
                actionChainState.producedEvents.Add(new CardRevealedEvent
                {
                    eventId = useSkillActionRequest.requestId,
                    eventTypeKey = "cardRevealed",
                    sourceActionChainId = actionChainState.actionChainId,
                    cardInstanceId = revealedCard.cardInstanceId,
                    ownerPlayerId = revealedCard.ownerPlayerId,
                    definitionId = revealedCard.definitionId,
                    revealReasonKey = RevealReasonKeyC022GrandGuignol,
                });
            }

            actionChainState.localState[LocalStateKeyC022GrandGuignolDamageValue] = damageValue.ToString();
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC022TargetPlayer,
                ContextKeyC022GrandGuignolTarget,
                ContinuationKeyC022GrandGuignolTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC022_4, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC022DollAmount,
                ContextKeyC022DollWarSelectX,
                ContinuationKeyC022DollWarSelectX,
                collectC022DollAmountChoiceKeys(sourceCharacterInstance));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC031_2, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC031LeylineAmount,
                ContextKeyC031FarPunchLeyline,
                ContinuationKeyC031FarPunchSelectLeyline,
                collectLeylineAmountChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC031_3, StringComparison.Ordinal))
        {
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                useSkillActionRequest.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC031DeclareCardName,
                ContextKeyC031AlleyDeclare,
                ContinuationKeyC031AlleyDeclare,
                collectDeclarableCardNameChoiceKeys());
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC031_4, StringComparison.Ordinal))
        {
            applyC031DepletionGarden(
                gameState,
                actionChainState,
                useSkillActionRequest);
            return;
        }

        if (string.Equals(useSkillActionRequest.skillKey, SkillKeyC029_4, StringComparison.Ordinal))
        {
            foreach (var playerState in gameState.players.Values)
            {
                if (playerState.teamId == actorPlayerState.teamId)
                {
                    continue;
                }

                var appliedStatus = StatusRuntime.applyStatus(
                    gameState,
                    new StatusInstance
                    {
                        statusKey = StatusKeyCharm,
                        applierPlayerId = useSkillActionRequest.actorPlayerId,
                        applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                        targetPlayerId = playerState.playerId,
                        stackCount = 1,
                    });
                actionChainState.producedEvents.Add(new StatusChangedEvent
                {
                    eventId = useSkillActionRequest.requestId,
                    eventTypeKey = "statusChanged",
                    sourceActionChainId = actionChainState.actionChainId,
                    statusKey = appliedStatus.statusKey,
                    targetPlayerId = appliedStatus.targetPlayerId,
                    isApplied = true,
                });
            }

            if (sourceCharacterInstance.isAlive && sourceCharacterInstance.isInPlay)
            {
                var damageEvents = damageProcessor.resolveDamage(
                    gameState,
                    new DamageContext
                    {
                        damageContextId = new DamageContextId(useSkillActionRequest.requestId),
                        sourcePlayerId = useSkillActionRequest.actorPlayerId,
                        sourceCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                        targetPlayerId = useSkillActionRequest.actorPlayerId,
                        targetCharacterInstanceId = sourceCharacterInstance.characterInstanceId,
                        baseDamageValue = 1,
                        damageType = DamageTypeKeyDirect,
                    });
                actionChainState.producedEvents.AddRange(damageEvents);
            }
        }
    }

    private void startC018SkillSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        UseSkillActionRequest useSkillActionRequest,
        GameState.PlayerState actorPlayerState,
        string sequenceKind)
    {
        var remainingTargetCharacterIds = new List<CharacterInstanceId>();
        foreach (var playerId in getPlayersInSeatOrder(gameState))
        {
            var playerState = gameState.players[playerId];
            if (string.Equals(sequenceKind, C018SequenceKindDirectDamage, StringComparison.Ordinal))
            {
                if (playerState.playerId == useSkillActionRequest.actorPlayerId)
                {
                    continue;
                }
            }
            else if (playerState.teamId == actorPlayerState.teamId)
            {
                continue;
            }

            if (!tryFindAliveInPlayCharacterInstanceByOwner(
                    gameState,
                    playerState.playerId,
                    out var targetCharacterInstance))
            {
                continue;
            }

            if (string.Equals(sequenceKind, C018SequenceKindDirectKill, StringComparison.Ordinal) &&
                targetCharacterInstance.currentHp != 1)
            {
                continue;
            }

            remainingTargetCharacterIds.Add(targetCharacterInstance.characterInstanceId);
        }

        actionChainState.localState[LocalStateKeyC018SequenceKind] = sequenceKind;
        saveC018RemainingTargetCharacterIds(actionChainState, remainingTargetCharacterIds);
        continueC018SkillSequence(gameState, actionChainState, useSkillActionRequest.requestId);
    }

    private void tryResumeC018SkillSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (!actionChainState.localState.ContainsKey(LocalStateKeyC018SequenceKind) ||
            gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            return;
        }

        continueC018SkillSequence(gameState, actionChainState, eventId);
    }

    private void continueC018SkillSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC018SequenceKind, out var sequenceKind))
        {
            clearC018SkillSequence(actionChainState);
            return;
        }

        while (gameState.matchState == GameState.MatchState.running &&
               gameState.currentInputContext is null &&
               gameState.currentResponseWindow is null &&
               string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            var remainingTargetCharacterIds = loadC018RemainingTargetCharacterIds(actionChainState);
            if (remainingTargetCharacterIds.Count == 0)
            {
                clearC018SkillSequence(actionChainState);
                return;
            }

            var targetCharacterInstanceId = remainingTargetCharacterIds[0];
            remainingTargetCharacterIds.RemoveAt(0);
            saveC018RemainingTargetCharacterIds(actionChainState, remainingTargetCharacterIds);

            if (!gameState.characterInstances.TryGetValue(targetCharacterInstanceId, out var targetCharacterInstance) ||
                !targetCharacterInstance.isAlive ||
                !targetCharacterInstance.isInPlay)
            {
                continue;
            }

            var producedEventsStartIndex = actionChainState.producedEvents.Count;
            if (string.Equals(sequenceKind, C018SequenceKindDirectDamage, StringComparison.Ordinal))
            {
                actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(
                    gameState,
                    new DamageContext
                    {
                        damageContextId = new DamageContextId(eventId),
                        sourcePlayerId = useSkillActionRequest.actorPlayerId,
                        sourceCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                        targetPlayerId = targetCharacterInstance.ownerPlayerId,
                        targetCharacterInstanceId = targetCharacterInstanceId,
                        baseDamageValue = 1,
                        damageType = DamageTypeKeyDirect,
                    }));
            }
            else if (string.Equals(sequenceKind, C018SequenceKindDirectKill, StringComparison.Ordinal))
            {
                if (targetCharacterInstance.currentHp != 1)
                {
                    continue;
                }

                actionChainState.producedEvents.AddRange(damageProcessor.resolveDirectKill(
                    gameState,
                    new KillContext
                    {
                        killContextId = eventId,
                        killerPlayerId = useSkillActionRequest.actorPlayerId,
                        killedCharacterInstanceId = targetCharacterInstanceId,
                        killedPlayerId = targetCharacterInstance.ownerPlayerId,
                    }));
            }
            else
            {
                throw new InvalidOperationException("C018 skill continuation contains an unknown sequence kind.");
            }

            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                eventId,
                producedEventsStartIndex);
        }

        if (gameState.matchState != GameState.MatchState.running)
        {
            clearC018SkillSequence(actionChainState);
        }
    }

    private static List<PlayerId> getPlayersInSeatOrder(GameState.GameState gameState)
    {
        var result = new List<PlayerId>();
        if (gameState.matchMeta is not null)
        {
            foreach (var playerId in gameState.matchMeta.seatOrder)
            {
                if (gameState.players.ContainsKey(playerId))
                {
                    result.Add(playerId);
                }
            }
        }

        var remainingPlayerIds = new List<PlayerId>();
        foreach (var playerId in gameState.players.Keys)
        {
            if (!result.Contains(playerId))
            {
                remainingPlayerIds.Add(playerId);
            }
        }

        remainingPlayerIds.Sort((left, right) => left.Value.CompareTo(right.Value));
        result.AddRange(remainingPlayerIds);
        return result;
    }

    private static void saveC018RemainingTargetCharacterIds(
        ActionChainState actionChainState,
        List<CharacterInstanceId> characterInstanceIds)
    {
        var numericIds = new List<string>();
        foreach (var characterInstanceId in characterInstanceIds)
        {
            numericIds.Add(characterInstanceId.Value.ToString());
        }

        actionChainState.localState[LocalStateKeyC018RemainingTargetCharacterIds] =
            string.Join(",", numericIds);
    }

    private static List<CharacterInstanceId> loadC018RemainingTargetCharacterIds(
        ActionChainState actionChainState)
    {
        var result = new List<CharacterInstanceId>();
        if (!actionChainState.localState.TryGetValue(
                LocalStateKeyC018RemainingTargetCharacterIds,
                out var serializedIds) ||
            string.IsNullOrWhiteSpace(serializedIds))
        {
            return result;
        }

        foreach (var segment in serializedIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!long.TryParse(segment, out var numericId))
            {
                throw new InvalidOperationException("C018 skill continuation contains an invalid target character id.");
            }

            result.Add(new CharacterInstanceId(numericId));
        }

        return result;
    }

    private static void clearC018SkillSequence(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(LocalStateKeyC018SequenceKind);
        actionChainState.localState.Remove(LocalStateKeyC018RemainingTargetCharacterIds);
    }

    private void openCharacterSkillChoiceInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        PlayerId requiredPlayerId,
        string inputTypeKey,
        string contextKey,
        string continuationKey,
        List<string> choiceKeys)
    {
        if (choiceKeys.Count == 0)
        {
            throw new InvalidOperationException("Character skill input requires at least one legal choice.");
        }

        var inputContextId = new InputContextId(Interlocked.Increment(ref nextInputContextNumericId));
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = requiredPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = inputTypeKey,
            contextKey = contextKey,
        };
        inputContextState.choiceKeys.AddRange(choiceKeys);
        gameState.currentInputContext = inputContextState;
        actionChainState.pendingContinuationKey = continuationKey;
        actionChainState.isCompleted = false;
        appendResponseSkillPresentationIfNeeded(
            gameState,
            actionChainState,
            requiredPlayerId,
            contextKey);
        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = eventId,
            eventTypeKey = "inputContextOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextId,
            isOpened = true,
        });
    }

    private static List<string> collectC007FriendlyChoiceKeys(
        GameState.GameState gameState,
        GameState.PlayerState actorPlayerState)
    {
        return collectC007PlayerChoiceKeys(
            gameState,
            playerState => playerState.teamId == actorPlayerState.teamId);
    }

    private static List<string> collectC007OpponentChoiceKeys(
        GameState.GameState gameState,
        GameState.PlayerState actorPlayerState)
    {
        return collectC007PlayerChoiceKeys(
            gameState,
            playerState => playerState.teamId != actorPlayerState.teamId);
    }

    private static List<string> collectC007PlayerChoiceKeys(
        GameState.GameState gameState,
        Func<GameState.PlayerState, bool> playerPredicate)
    {
        var choiceKeys = new List<string>();
        foreach (var playerId in getPlayersInSeatOrder(gameState))
        {
            var playerState = gameState.players[playerId];
            if (!playerPredicate(playerState) ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, playerId, out _))
            {
                continue;
            }

            choiceKeys.Add(ChoiceKeyPlayerPrefix + playerId.Value);
        }

        return choiceKeys;
    }

    private void continueC007SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("C007 skill continuation requires a valid UseSkillActionRequest root.");
        }

        var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
        if (!tryFindAliveInPlayCharacterInstanceByOwner(
                gameState,
                targetPlayerId,
                out var targetCharacterInstance))
        {
            throw new InvalidOperationException("C007 skill continuation requires selected player to have an alive in-play character.");
        }

        if (string.Equals(continuationKey, ContinuationKeyC007NightmareTarget, StringComparison.Ordinal))
        {
            if (!string.Equals(targetCharacterInstance.definitionId, CharacterDefinitionIdC011, StringComparison.Ordinal))
            {
                var appliedStatus = StatusRuntime.applyStatus(
                    gameState,
                    new StatusInstance
                    {
                        statusKey = StatusKeyCharm,
                        applierPlayerId = useSkillActionRequest.actorPlayerId,
                        applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                        targetPlayerId = targetPlayerId,
                        targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                        stackCount = 1,
                    });
                actionChainState.producedEvents.Add(new StatusChangedEvent
                {
                    eventId = request.requestId,
                    eventTypeKey = "statusChanged",
                    sourceActionChainId = actionChainState.actionChainId,
                    statusKey = appliedStatus.statusKey,
                    targetPlayerId = targetPlayerId,
                    targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                    isApplied = true,
                });
            }

            actionChainState.pendingContinuationKey = null;
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC007DaydreamHealTarget, StringComparison.Ordinal))
        {
            appendHealOnCharacter(
                actionChainState,
                request.requestId,
                targetCharacterInstance,
                healAmount: 4);
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC007TargetPlayer,
                ContextKeyC007DaydreamDamageTarget,
                ContinuationKeyC007DaydreamDamageTarget,
                collectC007OpponentChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC007DaydreamDamageTarget, StringComparison.Ordinal))
        {
            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                4,
                DamageTypeKeySpell);
            return;
        }

        throw new InvalidOperationException("C007 skill continuation key is unsupported.");
    }

    private static PlayerId parsePlayerChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(ChoiceKeyPlayerPrefix, StringComparison.Ordinal) ||
            !long.TryParse(choiceKey.Substring(ChoiceKeyPlayerPrefix.Length), out var playerNumericId))
        {
            throw new InvalidOperationException("C007 skill target choice requires player:{id} format.");
        }

        return new PlayerId(playerNumericId);
    }

    private static bool isC007SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC007NightmareTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC007DaydreamHealTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC007DaydreamDamageTarget, StringComparison.Ordinal);
    }

    private void continueC003SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (string.Equals(continuationKey, ContinuationKeyC003GungnirDiscard, StringComparison.Ordinal))
        {
            continueC003GungnirDiscard(
                gameState,
                actionChainState,
                request);
            return;
        }

        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState) ||
            !gameState.characterInstances.TryGetValue(useSkillActionRequest.characterInstanceId, out var sourceCharacterInstance))
        {
            throw new InvalidOperationException("C003 skill continuation requires a valid UseSkillActionRequest root.");
        }

        if (string.Equals(continuationKey, ContinuationKeyC003RedNightTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C003:1 requires selected player to be an alive opponent.");
            }

            var amount = sourceCharacterInstance.isActivated ? 4 : 2;
            actionChainState.localState[LocalStateKeyC003RedNightHealAmount] = amount.ToString();
            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                amount,
                DamageTypeKeySpell);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC003ScarletDestinyTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C003:3 requires selected player to be an alive opponent.");
            }

            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                6,
                DamageTypeKeyPhysical);
            return;
        }

        throw new InvalidOperationException("C003 skill continuation key is unsupported.");
    }

    private static bool isC003SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC003RedNightTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC003GungnirDiscard, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC003ScarletDestinyTarget, StringComparison.Ordinal);
    }

    private void tryResumeC003RedNightHealSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC003RedNightHealAmount, out var healAmountRaw) ||
            !int.TryParse(healAmountRaw, out var healAmount) ||
            healAmount <= 0 ||
            gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey) ||
            actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.characterInstances.TryGetValue(useSkillActionRequest.characterInstanceId, out var sourceCharacterInstance) ||
            !sourceCharacterInstance.isAlive ||
            !sourceCharacterInstance.isInPlay)
        {
            return;
        }

        appendHealOnCharacter(
            actionChainState,
            eventId,
            sourceCharacterInstance,
            healAmount);
        actionChainState.localState.Remove(LocalStateKeyC003RedNightHealAmount);
    }

    private bool tryOpenC003GungnirBeforeDefense(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        DamageContext stagedDamageContext)
    {
        if (gameState.turnState is null ||
            gameState.currentInputContext is not null ||
            stagedDamageContext.sourcePlayerId is null ||
            !stagedDamageContext.targetCharacterInstanceId.HasValue ||
            string.Equals(stagedDamageContext.damageType, DamageTypeKeyDirect, StringComparison.Ordinal))
        {
            return false;
        }

        if (!gameState.players.TryGetValue(stagedDamageContext.sourcePlayerId.Value, out var sourcePlayerState) ||
            !gameState.characterInstances.TryGetValue(stagedDamageContext.targetCharacterInstanceId.Value, out var targetCharacterInstance) ||
            !gameState.players.TryGetValue(targetCharacterInstance.ownerPlayerId, out var targetPlayerState) ||
            sourcePlayerState.teamId == targetPlayerState.teamId ||
            StatusRuntime.hasStatusOnPlayer(gameState, targetCharacterInstance.ownerPlayerId, StatusKeyCharm))
        {
            return false;
        }

        CharacterInstance? sourceCharacterForTrigger = null;
        if (stagedDamageContext.sourceCharacterInstanceId.HasValue &&
            gameState.characterInstances.TryGetValue(
                stagedDamageContext.sourceCharacterInstanceId.Value,
                out var explicitSourceCharacter) &&
            explicitSourceCharacter.ownerPlayerId == stagedDamageContext.sourcePlayerId.Value)
        {
            sourceCharacterForTrigger = explicitSourceCharacter;
        }
        else if (tryFindAliveInPlayCharacterInstanceByOwner(
                     gameState,
                     stagedDamageContext.sourcePlayerId.Value,
                     out var sourceActiveCharacter))
        {
            sourceCharacterForTrigger = sourceActiveCharacter;
        }

        if (sourceCharacterForTrigger is null)
        {
            return false;
        }

        PlayerId? remiliaPlayerId = null;
        CharacterInstance? remiliaCharacter = null;
        if (string.Equals(sourceCharacterForTrigger.definitionId, CharacterDefinitionIdC003, StringComparison.Ordinal) &&
            sourceCharacterForTrigger.isAlive &&
            sourceCharacterForTrigger.isInPlay)
        {
            remiliaPlayerId = sourceCharacterForTrigger.ownerPlayerId;
            remiliaCharacter = sourceCharacterForTrigger;
        }
        else if (string.Equals(sourceCharacterForTrigger.definitionId, CharacterDefinitionIdC014, StringComparison.Ordinal))
        {
            foreach (var characterInstance in gameState.characterInstances.Values)
            {
                if (!characterInstance.isAlive ||
                    !characterInstance.isInPlay ||
                    !string.Equals(characterInstance.definitionId, CharacterDefinitionIdC003, StringComparison.Ordinal) ||
                    !gameState.players.TryGetValue(characterInstance.ownerPlayerId, out var candidatePlayerState) ||
                    candidatePlayerState.teamId != sourcePlayerState.teamId)
                {
                    continue;
                }

                remiliaPlayerId = characterInstance.ownerPlayerId;
                remiliaCharacter = characterInstance;
                break;
            }
        }

        if (!remiliaPlayerId.HasValue ||
            remiliaCharacter is null ||
            StatusRuntime.hasStatusOnPlayer(gameState, remiliaPlayerId.Value, StatusKeySilence) ||
            gameState.turnState.usedOncePerTurnSkillKeys.Contains(SkillKeyC003_2) ||
            !gameState.players.TryGetValue(remiliaPlayerId.Value, out var remiliaPlayerState) ||
            !gameState.zones.TryGetValue(remiliaPlayerState.handZoneId, out var handZoneState))
        {
            return false;
        }

        var choiceKeys = new List<string> { ChoiceKeyDecline };
        foreach (var cardInstanceId in handZoneState.cardInstanceIds)
        {
            if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance))
            {
                continue;
            }

            var summonCost = TreasureDefinitionRepository
                .resolveByDefinitionId(cardInstance.definitionId)
                .summonSigilCost;
            if (summonCost.HasValue && summonCost.Value >= 3)
            {
                choiceKeys.Add(ChoiceKeyDiscardCardPrefix + cardInstanceId.Value);
            }
        }

        if (choiceKeys.Count <= 1)
        {
            return false;
        }

        saveC003GungnirDamageContext(actionChainState, stagedDamageContext);
        openCharacterSkillChoiceInput(
            gameState,
            actionChainState,
            eventId,
            remiliaPlayerId.Value,
            InputTypeKeyC003GungnirDiscard,
            ContextKeyC003GungnirDiscard,
            ContinuationKeyC003GungnirDiscard,
            choiceKeys);
        return true;
    }

    private void continueC003GungnirDiscard(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request)
    {
        var damageContext = restoreC003GungnirDamageContext(actionChainState, request.requestId);
        clearC003GungnirDamageContext(actionChainState);

        if (!string.Equals(request.choiceKey, ChoiceKeyDecline, StringComparison.Ordinal))
        {
            var selectedCardInstanceId = parseCardChoiceKey(request.choiceKey, ChoiceKeyDiscardCardPrefix);
            if (!gameState.players.TryGetValue(request.actorPlayerId, out var remiliaPlayerState) ||
                !gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance) ||
                selectedCardInstance.ownerPlayerId != request.actorPlayerId ||
                selectedCardInstance.zoneId != remiliaPlayerState.handZoneId ||
                !isSummonCostAtLeast(selectedCardInstance.definitionId, 3))
            {
                throw new InvalidOperationException("C003:2 requires selected discard card to be a hand card with summon cost at least 3.");
            }

            actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                gameState,
                selectedCardInstance,
                remiliaPlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                request.requestId));
            gameState.turnState!.usedOncePerTurnSkillKeys.Add(SkillKeyC003_2);
            if (damageContext.sourcePlayerId.HasValue)
            {
                applyPenetrateToSourceForGungnir(
                    gameState,
                    actionChainState,
                    request.requestId,
                    request.actorPlayerId,
                    damageContext.sourcePlayerId.Value,
                    damageContext.sourceCharacterInstanceId);
                if (shouldQueueC014RippleDamage(actionChainState))
                {
                    actionChainState.localState[LocalStateKeyC014RippleForcePenetrate] = "true";
                }
            }
        }

        actionChainState.pendingContinuationKey = null;
        openDamageResponseWindowWithPendingDamage(
            gameState,
            actionChainState,
            request.requestId,
            damageContext.sourcePlayerId!.Value,
            damageContext.sourceCardInstanceId,
            damageContext.sourceCharacterInstanceId,
            damageContext.targetCharacterInstanceId!.Value,
            damageContext.baseDamageValue,
            damageContext.damageType,
            hasAppliedSourceDamageBonuses: true,
            suppressC003Gungnir: true,
            suppressC014AndThen: true);
    }

    private void applyPenetrateToSourceForGungnir(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        PlayerId remiliaPlayerId,
        PlayerId sourcePlayerId,
        CharacterInstanceId? sourceCharacterInstanceId)
    {
        var appliedStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = StatusKeyPenetrate,
                applierPlayerId = remiliaPlayerId,
                targetPlayerId = sourcePlayerId,
                targetCharacterInstanceId = sourceCharacterInstanceId,
                stackCount = 1,
                durationTypeKey = StatusRuntime.DurationTypeKeyNextDamageAttempt,
            });
        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = eventId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = appliedStatus.statusKey,
            targetPlayerId = appliedStatus.targetPlayerId,
            targetCharacterInstanceId = appliedStatus.targetCharacterInstanceId,
            isApplied = true,
        });
    }

    private static void appendResponseSkillPresentationIfNeeded(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId requiredPlayerId,
        string contextKey)
    {
        var skillKey = contextKey switch
        {
            ContextKeyC003GungnirDiscard => SkillKeyC003_2,
            ContextKeyC004StardustDiscard => SkillKeyC004_2,
            ContextKeyC005SixRealmsChoice => "C005:2",
            ContextKeyC013AbnormalTasteDiscard => SkillKeyC013_2,
            ContextKeyC014AndThenChoice => SkillKeyC014_2,
            ContextKeyC021FuneralChoice => "C021:3",
            _ => string.Empty,
        };
        if (skillKey.Length == 0 ||
            !tryFindAliveInPlayCharacterInstanceByOwner(gameState, requiredPlayerId, out var characterInstance))
        {
            return;
        }

        SkillPresentationEventEmitter.appendIfNeeded(
            actionChainState,
            requiredPlayerId,
            characterInstance,
            skillKey,
            "response");
    }

    private static bool isSummonCostAtLeast(string definitionId, int minCost)
    {
        var summonCost = TreasureDefinitionRepository.resolveByDefinitionId(definitionId).summonSigilCost;
        return summonCost.HasValue && summonCost.Value >= minCost;
    }

    private static void saveC003GungnirDamageContext(
        ActionChainState actionChainState,
        DamageContext damageContext)
    {
        actionChainState.localState[LocalStateKeyC003GungnirSourcePlayerId] =
            damageContext.sourcePlayerId!.Value.Value.ToString();
        if (damageContext.sourceCardInstanceId.HasValue)
        {
            actionChainState.localState[LocalStateKeyC003GungnirSourceCardInstanceId] =
                damageContext.sourceCardInstanceId.Value.Value.ToString();
        }
        else
        {
            actionChainState.localState.Remove(LocalStateKeyC003GungnirSourceCardInstanceId);
        }

        if (damageContext.sourceCharacterInstanceId.HasValue)
        {
            actionChainState.localState[LocalStateKeyC003GungnirSourceCharacterInstanceId] =
                damageContext.sourceCharacterInstanceId.Value.Value.ToString();
        }
        else
        {
            actionChainState.localState.Remove(LocalStateKeyC003GungnirSourceCharacterInstanceId);
        }

        actionChainState.localState[LocalStateKeyC003GungnirTargetCharacterInstanceId] =
            damageContext.targetCharacterInstanceId!.Value.Value.ToString();
        actionChainState.localState[LocalStateKeyC003GungnirBaseDamageValue] =
            damageContext.baseDamageValue.ToString();
        actionChainState.localState[LocalStateKeyC003GungnirDamageType] =
            damageContext.damageType;
    }

    private static DamageContext restoreC003GungnirDamageContext(
        ActionChainState actionChainState,
        long requestId)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC003GungnirSourcePlayerId, out var sourcePlayerRaw) ||
            !long.TryParse(sourcePlayerRaw, out var sourcePlayerNumericId) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC003GungnirTargetCharacterInstanceId, out var targetCharacterRaw) ||
            !long.TryParse(targetCharacterRaw, out var targetCharacterNumericId) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC003GungnirBaseDamageValue, out var baseDamageRaw) ||
            !int.TryParse(baseDamageRaw, out var baseDamageValue) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC003GungnirDamageType, out var damageTypeKey))
        {
            throw new InvalidOperationException("C003:2 requires saved pending damage context.");
        }

        CardInstanceId? sourceCardInstanceId = null;
        if (actionChainState.localState.TryGetValue(LocalStateKeyC003GungnirSourceCardInstanceId, out var sourceCardRaw) &&
            long.TryParse(sourceCardRaw, out var sourceCardNumericId))
        {
            sourceCardInstanceId = new CardInstanceId(sourceCardNumericId);
        }

        CharacterInstanceId? sourceCharacterInstanceId = null;
        if (actionChainState.localState.TryGetValue(LocalStateKeyC003GungnirSourceCharacterInstanceId, out var sourceCharacterRaw) &&
            long.TryParse(sourceCharacterRaw, out var sourceCharacterNumericId))
        {
            sourceCharacterInstanceId = new CharacterInstanceId(sourceCharacterNumericId);
        }

        return new DamageContext
        {
            damageContextId = new DamageContextId(requestId),
            sourcePlayerId = new PlayerId(sourcePlayerNumericId),
            sourceCardInstanceId = sourceCardInstanceId,
            sourceCharacterInstanceId = sourceCharacterInstanceId,
            targetCharacterInstanceId = new CharacterInstanceId(targetCharacterNumericId),
            baseDamageValue = baseDamageValue,
            damageType = damageTypeKey,
            hasAppliedSourceDamageBonuses = true,
        };
    }

    private static void clearC003GungnirDamageContext(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(LocalStateKeyC003GungnirSourcePlayerId);
        actionChainState.localState.Remove(LocalStateKeyC003GungnirSourceCardInstanceId);
        actionChainState.localState.Remove(LocalStateKeyC003GungnirSourceCharacterInstanceId);
        actionChainState.localState.Remove(LocalStateKeyC003GungnirTargetCharacterInstanceId);
        actionChainState.localState.Remove(LocalStateKeyC003GungnirBaseDamageValue);
        actionChainState.localState.Remove(LocalStateKeyC003GungnirDamageType);
    }

    private bool tryOpenBeforeDefenseCharacterResponses(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        DamageContext stagedDamageContext)
    {
        return tryOpenBeforeDefenseCharacterResponses(
            gameState,
            actionChainState,
            eventId,
            stagedDamageContext,
            suppressC014AndThen: false,
            suppressC003Gungnir: false);
    }

    private bool tryOpenBeforeDefenseCharacterResponses(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        DamageContext stagedDamageContext,
        bool suppressC014AndThen,
        bool suppressC003Gungnir)
    {
        if (!suppressC014AndThen &&
            tryOpenC014AndThenBeforeDefense(gameState, actionChainState, eventId, stagedDamageContext))
        {
            return true;
        }

        return !suppressC003Gungnir &&
               tryOpenC003GungnirBeforeDefense(gameState, actionChainState, eventId, stagedDamageContext);
    }

    private bool tryOpenC014AndThenBeforeDefense(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        DamageContext stagedDamageContext)
    {
        if (gameState.turnState is null ||
            gameState.currentInputContext is not null ||
            stagedDamageContext.sourcePlayerId is null ||
            !stagedDamageContext.targetCharacterInstanceId.HasValue ||
            string.Equals(stagedDamageContext.damageType, DamageTypeKeyDirect, StringComparison.Ordinal))
        {
            return false;
        }

        if (!gameState.players.TryGetValue(stagedDamageContext.sourcePlayerId.Value, out var sourcePlayerState) ||
            !gameState.characterInstances.TryGetValue(stagedDamageContext.targetCharacterInstanceId.Value, out var targetCharacterInstance) ||
            !gameState.players.TryGetValue(targetCharacterInstance.ownerPlayerId, out var targetPlayerState) ||
            sourcePlayerState.teamId == targetPlayerState.teamId ||
            StatusRuntime.hasStatusOnPlayer(gameState, sourcePlayerState.playerId, StatusKeySilence) ||
            !gameState.zones.TryGetValue(sourcePlayerState.handZoneId, out var handZoneState) ||
            handZoneState.cardInstanceIds.Count == 0)
        {
            return false;
        }

        CharacterInstance? sourceCharacterInstance = null;
        if (stagedDamageContext.sourceCharacterInstanceId.HasValue &&
            gameState.characterInstances.TryGetValue(
                stagedDamageContext.sourceCharacterInstanceId.Value,
                out var explicitSourceCharacter) &&
            explicitSourceCharacter.ownerPlayerId == sourcePlayerState.playerId)
        {
            sourceCharacterInstance = explicitSourceCharacter;
        }
        else if (tryFindAliveInPlayCharacterInstanceByOwner(
                     gameState,
                     sourcePlayerState.playerId,
                     out var sourceActiveCharacter))
        {
            sourceCharacterInstance = sourceActiveCharacter;
            stagedDamageContext.sourceCharacterInstanceId = sourceActiveCharacter.characterInstanceId;
        }

        if (sourceCharacterInstance is null ||
            !string.Equals(sourceCharacterInstance.definitionId, CharacterDefinitionIdC014, StringComparison.Ordinal) ||
            !sourceCharacterInstance.isAlive ||
            !sourceCharacterInstance.isInPlay)
        {
            return false;
        }

        var choiceKeys = new List<string> { ChoiceKeyDecline };
        for (var amount = 1; amount <= handZoneState.cardInstanceIds.Count; amount++)
        {
            choiceKeys.Add(ChoiceKeyC014AndThenXPrefix + amount);
        }

        saveC014AndThenDamageContext(actionChainState, stagedDamageContext);
        openCharacterSkillChoiceInput(
            gameState,
            actionChainState,
            eventId,
            sourcePlayerState.playerId,
            InputTypeKeyC014AndThenChoice,
            ContextKeyC014AndThenChoice,
            ContinuationKeyC014AndThenChoice,
            choiceKeys);
        return true;
    }

    private void continueC014AndThenChoice(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request)
    {
        var damageContext = restoreC014AndThenDamageContext(actionChainState, request.requestId);
        if (string.Equals(request.choiceKey, ChoiceKeyDecline, StringComparison.Ordinal))
        {
            clearC014AndThenDamageContext(actionChainState);
            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                damageContext.sourcePlayerId!.Value,
                damageContext.sourceCardInstanceId,
                damageContext.sourceCharacterInstanceId,
                damageContext.targetCharacterInstanceId!.Value,
                damageContext.baseDamageValue,
                damageContext.damageType,
                hasAppliedSourceDamageBonuses: true,
                suppressC014AndThen: true);
            return;
        }

        var selectedX = parseC014AndThenXChoiceKey(request.choiceKey);
        if (selectedX <= 0 ||
            !damageContext.sourcePlayerId.HasValue ||
            !gameState.players.TryGetValue(damageContext.sourcePlayerId.Value, out var sourcePlayerState) ||
            !gameState.zones.TryGetValue(sourcePlayerState.handZoneId, out var handZoneState) ||
            handZoneState.cardInstanceIds.Count < selectedX)
        {
            throw new InvalidOperationException("C014:2 requires selected X to be positive and not exceed hand count.");
        }

        actionChainState.localState[LocalStateKeyC014AndThenSelectedX] = selectedX.ToString();
        openCharacterSkillChoiceInput(
            gameState,
            actionChainState,
            request.requestId,
            sourcePlayerState.playerId,
            InputTypeKeyC014AndThenDiscard,
            ContextKeyC014AndThenDiscard,
            ContinuationKeyC014AndThenDiscard,
            collectPlayerHandDiscardChoiceKeys(gameState, sourcePlayerState));
    }

    private void continueC014AndThenDiscard(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        var damageContext = restoreC014AndThenDamageContext(actionChainState, request.requestId);
        var selectedX = getC014AndThenSelectedX(actionChainState);
        var selectedChoiceKeys = request.choiceKeys.Count > 0
            ? request.choiceKeys
            : new List<string> { request.choiceKey };
        if (selectedChoiceKeys.Count != selectedX ||
            selectedChoiceKeys.Distinct(StringComparer.Ordinal).Count() != selectedChoiceKeys.Count ||
            selectedChoiceKeys.Any(choiceKey => !inputContextState.choiceKeys.Contains(choiceKey)))
        {
            throw new InvalidOperationException("C014:2 discard continuation requires exactly X unique hand-card choices.");
        }

        if (!damageContext.sourcePlayerId.HasValue ||
            !damageContext.sourceCharacterInstanceId.HasValue ||
            !gameState.players.TryGetValue(damageContext.sourcePlayerId.Value, out var sourcePlayerState) ||
            !gameState.characterInstances.TryGetValue(damageContext.sourceCharacterInstanceId.Value, out var sourceCharacterInstance))
        {
            throw new InvalidOperationException("C014:2 requires saved source player and character.");
        }

        foreach (var choiceKey in selectedChoiceKeys)
        {
            var selectedCardInstanceId = parseCardChoiceKey(choiceKey, ChoiceKeyDiscardCardPrefix);
            if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance) ||
                selectedCardInstance.ownerPlayerId != sourcePlayerState.playerId ||
                selectedCardInstance.zoneId != sourcePlayerState.handZoneId)
            {
                throw new InvalidOperationException("C014:2 requires selected discard cards to remain in source player's hand.");
            }

            actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                gameState,
                selectedCardInstance,
                sourcePlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                request.requestId));
        }

        var hpBefore = sourceCharacterInstance.currentHp;
        sourceCharacterInstance.currentHp = hpBefore - 1;
        actionChainState.producedEvents.Add(new HpChangedEvent
        {
            eventId = request.requestId,
            eventTypeKey = "hpChanged",
            sourceActionChainId = actionChainState.actionChainId,
            targetPlayerId = sourcePlayerState.playerId,
            targetCharacterInstanceId = sourceCharacterInstance.characterInstanceId,
            hpBefore = hpBefore,
            hpAfter = sourceCharacterInstance.currentHp,
            delta = -1,
        });

        if (hpBefore > 0 && sourceCharacterInstance.currentHp <= 0)
        {
            actionChainState.producedEvents.AddRange(damageProcessor.resolveDirectKill(
                gameState,
                new KillContext
                {
                    killContextId = request.requestId,
                    killerPlayerId = null,
                    killedCharacterInstanceId = sourceCharacterInstance.characterInstanceId,
                    killedPlayerId = sourcePlayerState.playerId,
                }));
        }

        clearC014AndThenDamageContext(actionChainState);
        actionChainState.pendingContinuationKey = null;
        if (gameState.matchState != GameState.MatchState.running)
        {
            return;
        }

        var isC014RootSkill =
            actionChainState.rootActionRequest is UseSkillActionRequest useSkillActionRequest &&
            useSkillActionRequest.characterInstanceId == sourceCharacterInstance.characterInstanceId;
        if (!sourceCharacterInstance.isAlive && isC014RootSkill)
        {
            clearC014RippleSequence(actionChainState);
            return;
        }

        if (sourceCharacterInstance.isAlive)
        {
            damageContext.baseDamageValue += selectedX;
            if (shouldQueueC014RippleDamage(actionChainState))
            {
                actionChainState.localState[LocalStateKeyC014RippleDamageBonus] = selectedX.ToString();
            }
        }

        openDamageResponseWindowWithPendingDamage(
            gameState,
            actionChainState,
            request.requestId,
            damageContext.sourcePlayerId!.Value,
            damageContext.sourceCardInstanceId,
            damageContext.sourceCharacterInstanceId,
            damageContext.targetCharacterInstanceId!.Value,
            damageContext.baseDamageValue,
            damageContext.damageType,
            hasAppliedSourceDamageBonuses: true,
            suppressC014AndThen: true);
    }

    private static int parseC014AndThenXChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(ChoiceKeyC014AndThenXPrefix, StringComparison.Ordinal) ||
            !int.TryParse(choiceKey.Substring(ChoiceKeyC014AndThenXPrefix.Length), out var amount))
        {
            throw new InvalidOperationException("C014:2 X choice requires c014AndThen:x:{amount} format.");
        }

        return amount;
    }

    private static int getC014AndThenSelectedX(ActionChainState actionChainState)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC014AndThenSelectedX, out var selectedXRaw) ||
            !int.TryParse(selectedXRaw, out var selectedX))
        {
            throw new InvalidOperationException("C014:2 discard continuation requires saved selected X.");
        }

        return selectedX;
    }

    private static void saveC014AndThenDamageContext(
        ActionChainState actionChainState,
        DamageContext damageContext)
    {
        actionChainState.localState[LocalStateKeyC014AndThenSourcePlayerId] =
            damageContext.sourcePlayerId!.Value.Value.ToString();
        if (damageContext.sourceCardInstanceId.HasValue)
        {
            actionChainState.localState[LocalStateKeyC014AndThenSourceCardInstanceId] =
                damageContext.sourceCardInstanceId.Value.Value.ToString();
        }
        else
        {
            actionChainState.localState.Remove(LocalStateKeyC014AndThenSourceCardInstanceId);
        }

        if (damageContext.sourceCharacterInstanceId.HasValue)
        {
            actionChainState.localState[LocalStateKeyC014AndThenSourceCharacterInstanceId] =
                damageContext.sourceCharacterInstanceId.Value.Value.ToString();
        }
        else
        {
            actionChainState.localState.Remove(LocalStateKeyC014AndThenSourceCharacterInstanceId);
        }

        actionChainState.localState[LocalStateKeyC014AndThenTargetCharacterInstanceId] =
            damageContext.targetCharacterInstanceId!.Value.Value.ToString();
        actionChainState.localState[LocalStateKeyC014AndThenBaseDamageValue] =
            damageContext.baseDamageValue.ToString();
        actionChainState.localState[LocalStateKeyC014AndThenDamageType] =
            damageContext.damageType;
    }

    private static DamageContext restoreC014AndThenDamageContext(
        ActionChainState actionChainState,
        long requestId)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC014AndThenSourcePlayerId, out var sourcePlayerRaw) ||
            !long.TryParse(sourcePlayerRaw, out var sourcePlayerNumericId) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC014AndThenTargetCharacterInstanceId, out var targetCharacterRaw) ||
            !long.TryParse(targetCharacterRaw, out var targetCharacterNumericId) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC014AndThenBaseDamageValue, out var baseDamageRaw) ||
            !int.TryParse(baseDamageRaw, out var baseDamageValue) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC014AndThenDamageType, out var damageTypeKey))
        {
            throw new InvalidOperationException("C014:2 requires saved pending damage context.");
        }

        CardInstanceId? sourceCardInstanceId = null;
        if (actionChainState.localState.TryGetValue(LocalStateKeyC014AndThenSourceCardInstanceId, out var sourceCardRaw) &&
            long.TryParse(sourceCardRaw, out var sourceCardNumericId))
        {
            sourceCardInstanceId = new CardInstanceId(sourceCardNumericId);
        }

        CharacterInstanceId? sourceCharacterInstanceId = null;
        if (actionChainState.localState.TryGetValue(LocalStateKeyC014AndThenSourceCharacterInstanceId, out var sourceCharacterRaw) &&
            long.TryParse(sourceCharacterRaw, out var sourceCharacterNumericId))
        {
            sourceCharacterInstanceId = new CharacterInstanceId(sourceCharacterNumericId);
        }

        return new DamageContext
        {
            damageContextId = new DamageContextId(requestId),
            sourcePlayerId = new PlayerId(sourcePlayerNumericId),
            sourceCardInstanceId = sourceCardInstanceId,
            sourceCharacterInstanceId = sourceCharacterInstanceId,
            targetCharacterInstanceId = new CharacterInstanceId(targetCharacterNumericId),
            baseDamageValue = baseDamageValue,
            damageType = damageTypeKey,
            hasAppliedSourceDamageBonuses = true,
        };
    }

    private static void clearC014AndThenDamageContext(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(LocalStateKeyC014AndThenSourcePlayerId);
        actionChainState.localState.Remove(LocalStateKeyC014AndThenSourceCardInstanceId);
        actionChainState.localState.Remove(LocalStateKeyC014AndThenSourceCharacterInstanceId);
        actionChainState.localState.Remove(LocalStateKeyC014AndThenTargetCharacterInstanceId);
        actionChainState.localState.Remove(LocalStateKeyC014AndThenBaseDamageValue);
        actionChainState.localState.Remove(LocalStateKeyC014AndThenDamageType);
        actionChainState.localState.Remove(LocalStateKeyC014AndThenSelectedX);
    }

    private bool tryOpenC021FuneralBeforeCharmAndBarrier(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        DamageContext stagedDamageContext)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !string.Equals(useSkillActionRequest.skillKey, SkillKeyC021_2, StringComparison.Ordinal) ||
            string.Equals(stagedDamageContext.damageType, DamageTypeKeyDirect, StringComparison.Ordinal) ||
            stagedDamageContext.sourcePlayerId is null ||
            !stagedDamageContext.sourceCharacterInstanceId.HasValue ||
            !stagedDamageContext.targetCharacterInstanceId.HasValue ||
            gameState.currentInputContext is not null)
        {
            return false;
        }

        if (!gameState.players.TryGetValue(stagedDamageContext.sourcePlayerId.Value, out var cielPlayerState) ||
            !gameState.teams.TryGetValue(cielPlayerState.teamId, out var cielTeamState) ||
            !gameState.characterInstances.TryGetValue(stagedDamageContext.sourceCharacterInstanceId.Value, out var cielCharacterInstance) ||
            !string.Equals(cielCharacterInstance.definitionId, "C021", StringComparison.Ordinal) ||
            StatusRuntime.hasStatusOnPlayer(gameState, cielPlayerState.playerId, StatusKeySilence) ||
            cielPlayerState.mana < 4 ||
            cielTeamState.leyline < 1)
        {
            return false;
        }

        saveC021FuneralDamageContext(actionChainState, stagedDamageContext);
        openCharacterSkillChoiceInput(
            gameState,
            actionChainState,
            eventId,
            cielPlayerState.playerId,
            InputTypeKeyC021FuneralChoice,
            ContextKeyC021FuneralChoice,
            ContinuationKeyC021FuneralChoice,
            new List<string> { ChoiceKeyDecline, ChoiceKeyC021FuneralUse });
        return true;
    }

    private void continueC021FuneralChoice(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request)
    {
        var damageContext = restoreC021FuneralDamageContext(actionChainState, request.requestId);
        clearC021FuneralDamageContext(actionChainState);

        if (string.Equals(request.choiceKey, ChoiceKeyC021FuneralUse, StringComparison.Ordinal))
        {
            if (!damageContext.sourcePlayerId.HasValue ||
                !damageContext.sourceCharacterInstanceId.HasValue ||
                !damageContext.targetCharacterInstanceId.HasValue ||
                !gameState.players.TryGetValue(damageContext.sourcePlayerId.Value, out var cielPlayerState) ||
                !gameState.teams.TryGetValue(cielPlayerState.teamId, out var cielTeamState) ||
                request.actorPlayerId != cielPlayerState.playerId ||
                cielPlayerState.mana < 4 ||
                cielTeamState.leyline < 1 ||
                !gameState.characterInstances.TryGetValue(damageContext.targetCharacterInstanceId.Value, out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C021:3 requires source player resources and a valid pending target.");
            }

            cielPlayerState.mana -= 4;
            cielTeamState.leyline -= 1;
            applyStatusFromCharacterSkill(
                gameState,
                actionChainState,
                request.requestId,
                cielPlayerState.playerId,
                damageContext.sourceCharacterInstanceId.Value,
                StatusKeySeal,
                targetCharacterInstance.ownerPlayerId,
                targetCharacterInstance.characterInstanceId);
            damageContext.baseDamageValue += 2;
        }
        else if (!string.Equals(request.choiceKey, ChoiceKeyDecline, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("C021:3 requires choiceKey to be decline or c021Funeral:use.");
        }

        actionChainState.pendingContinuationKey = null;
        openDamageResponseWindowWithPendingDamage(
            gameState,
            actionChainState,
            request.requestId,
            damageContext.sourcePlayerId!.Value,
            damageContext.sourceCardInstanceId,
            damageContext.sourceCharacterInstanceId,
            damageContext.targetCharacterInstanceId!.Value,
            damageContext.baseDamageValue,
            damageContext.damageType,
            hasAppliedSourceDamageBonuses: true,
            suppressC021Funeral: true);
    }

    private static void saveC021FuneralDamageContext(
        ActionChainState actionChainState,
        DamageContext damageContext)
    {
        actionChainState.localState[LocalStateKeyC021FuneralSourcePlayerId] =
            damageContext.sourcePlayerId!.Value.Value.ToString();
        if (damageContext.sourceCardInstanceId.HasValue)
        {
            actionChainState.localState[LocalStateKeyC021FuneralSourceCardInstanceId] =
                damageContext.sourceCardInstanceId.Value.Value.ToString();
        }
        else
        {
            actionChainState.localState.Remove(LocalStateKeyC021FuneralSourceCardInstanceId);
        }

        if (damageContext.sourceCharacterInstanceId.HasValue)
        {
            actionChainState.localState[LocalStateKeyC021FuneralSourceCharacterInstanceId] =
                damageContext.sourceCharacterInstanceId.Value.Value.ToString();
        }
        else
        {
            actionChainState.localState.Remove(LocalStateKeyC021FuneralSourceCharacterInstanceId);
        }

        actionChainState.localState[LocalStateKeyC021FuneralTargetCharacterInstanceId] =
            damageContext.targetCharacterInstanceId!.Value.Value.ToString();
        actionChainState.localState[LocalStateKeyC021FuneralBaseDamageValue] =
            damageContext.baseDamageValue.ToString();
        actionChainState.localState[LocalStateKeyC021FuneralDamageType] =
            damageContext.damageType;
    }

    private static DamageContext restoreC021FuneralDamageContext(
        ActionChainState actionChainState,
        long requestId)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC021FuneralSourcePlayerId, out var sourcePlayerRaw) ||
            !long.TryParse(sourcePlayerRaw, out var sourcePlayerNumericId) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC021FuneralTargetCharacterInstanceId, out var targetCharacterRaw) ||
            !long.TryParse(targetCharacterRaw, out var targetCharacterNumericId) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC021FuneralBaseDamageValue, out var baseDamageRaw) ||
            !int.TryParse(baseDamageRaw, out var baseDamageValue) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC021FuneralDamageType, out var damageTypeKey))
        {
            throw new InvalidOperationException("C021:3 requires saved pending damage context.");
        }

        CardInstanceId? sourceCardInstanceId = null;
        if (actionChainState.localState.TryGetValue(LocalStateKeyC021FuneralSourceCardInstanceId, out var sourceCardRaw) &&
            long.TryParse(sourceCardRaw, out var sourceCardNumericId))
        {
            sourceCardInstanceId = new CardInstanceId(sourceCardNumericId);
        }

        CharacterInstanceId? sourceCharacterInstanceId = null;
        if (actionChainState.localState.TryGetValue(LocalStateKeyC021FuneralSourceCharacterInstanceId, out var sourceCharacterRaw) &&
            long.TryParse(sourceCharacterRaw, out var sourceCharacterNumericId))
        {
            sourceCharacterInstanceId = new CharacterInstanceId(sourceCharacterNumericId);
        }

        return new DamageContext
        {
            damageContextId = new DamageContextId(requestId),
            sourcePlayerId = new PlayerId(sourcePlayerNumericId),
            sourceCardInstanceId = sourceCardInstanceId,
            sourceCharacterInstanceId = sourceCharacterInstanceId,
            targetCharacterInstanceId = new CharacterInstanceId(targetCharacterNumericId),
            baseDamageValue = baseDamageValue,
            damageType = damageTypeKey,
            hasAppliedSourceDamageBonuses = true,
        };
    }

    private static void clearC021FuneralDamageContext(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(LocalStateKeyC021FuneralSourcePlayerId);
        actionChainState.localState.Remove(LocalStateKeyC021FuneralSourceCardInstanceId);
        actionChainState.localState.Remove(LocalStateKeyC021FuneralSourceCharacterInstanceId);
        actionChainState.localState.Remove(LocalStateKeyC021FuneralTargetCharacterInstanceId);
        actionChainState.localState.Remove(LocalStateKeyC021FuneralBaseDamageValue);
        actionChainState.localState.Remove(LocalStateKeyC021FuneralDamageType);
    }

    private void continueC004SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (string.Equals(continuationKey, ContinuationKeyC004StardustDiscard, StringComparison.Ordinal))
        {
            continueC004StardustCarnival(
                gameState,
                actionChainState,
                inputContextState,
                request);
            return;
        }

        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("C004 skill continuation requires a valid UseSkillActionRequest root.");
        }

        if (string.Equals(continuationKey, ContinuationKeyC004MasterSparkTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C004:1 requires selected player to be an alive opponent.");
            }

            var damageValue =
                string.Equals(targetCharacterInstance.definitionId, CharacterDefinitionIdC017, StringComparison.Ordinal) ||
                string.Equals(targetCharacterInstance.definitionId, CharacterDefinitionIdC022, StringComparison.Ordinal)
                    ? 3
                    : 4;
            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                damageValue,
                DamageTypeKeySpell);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC004BlazingCometTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C004:4 requires selected player to be an alive opponent.");
            }

            actionChainState.localState[LocalStateKeyC004BlazingCometSequence] = "awaitBanishAfterDamage";
            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                7,
                DamageTypeKeyPhysical);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC004BlazingCometBanish, StringComparison.Ordinal))
        {
            continueC004BlazingCometBanish(
                gameState,
                actionChainState,
                inputContextState,
                request);
            return;
        }

        throw new InvalidOperationException("C004 skill continuation key is unsupported.");
    }

    private static bool isC004SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC004MasterSparkTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC004StardustDiscard, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC004BlazingCometTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC004BlazingCometBanish, StringComparison.Ordinal);
    }

    private void continueC005SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState) ||
            !gameState.characterInstances.TryGetValue(useSkillActionRequest.characterInstanceId, out var sourceCharacterInstance))
        {
            throw new InvalidOperationException("C005 skill continuation requires a valid UseSkillActionRequest root.");
        }

        if (string.Equals(continuationKey, ContinuationKeyC005SakuraSparkSelectSwordAura, StringComparison.Ordinal) ||
            string.Equals(continuationKey, ContinuationKeyC005SpringWindSelectSwordAura, StringComparison.Ordinal))
        {
            var consumedSwordAura = parseC005SwordAuraChoiceKey(request.choiceKey);
            if (consumedSwordAura > MarkerRuntime.getMarkerCount(sourceCharacterInstance, MarkerTypeSwordAura))
            {
                throw new InvalidOperationException("C005 skill requires selected swordAura cost to be available.");
            }

            if (consumedSwordAura > 0)
            {
                actionChainState.producedEvents.Add(MarkerRuntime.removeMarker(
                    sourceCharacterInstance,
                    MarkerTypeSwordAura,
                    consumedSwordAura,
                    actionChainState.actionChainId,
                    request.requestId));
            }

            actionChainState.localState[LocalStateKeyC005ConsumedSwordAura] = consumedSwordAura.ToString();
            var targetContinuation = string.Equals(
                    continuationKey,
                    ContinuationKeyC005SakuraSparkSelectSwordAura,
                    StringComparison.Ordinal)
                ? ContinuationKeyC005SakuraSparkTargets
                : ContinuationKeyC005SpringWindTarget;
            var targetContext = string.Equals(
                    continuationKey,
                    ContinuationKeyC005SakuraSparkSelectSwordAura,
                    StringComparison.Ordinal)
                ? ContextKeyC005SakuraSparkTargets
                : ContextKeyC005SpringWindTarget;

            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC005TargetPlayer,
                targetContext,
                targetContinuation,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC005SakuraSparkTargets, StringComparison.Ordinal))
        {
            var consumedSwordAura = consumeSavedC005SwordAura(actionChainState);
            var selectedTargetChoiceKeys = getC005SelectedTargetChoiceKeys(request);
            if (selectedTargetChoiceKeys.Count is < 1 or > 2)
            {
                throw new InvalidOperationException("C005:3 requires selecting one or two opponent targets.");
            }

            var damageEntries = new List<string>();
            foreach (var targetChoiceKey in selectedTargetChoiceKeys)
            {
                var targetPlayerId = parsePlayerChoiceKey(targetChoiceKey);
                if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                    targetPlayerState.teamId == actorPlayerState.teamId ||
                    !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out var targetCharacterInstance))
                {
                    throw new InvalidOperationException("C005:3 requires every selected target to be an alive opponent.");
                }

                var damageValue = 1 + consumedSwordAura;
                if (string.Equals(targetCharacterInstance.definitionId, "C018", StringComparison.Ordinal))
                {
                    damageValue = Math.Max(0, damageValue - 1);
                }

                damageEntries.Add(targetCharacterInstance.characterInstanceId.Value + ":" + damageValue);
            }

            actionChainState.localState[LocalStateKeyC005SakuraSparkRemainingDamages] = string.Join(",", damageEntries);
            actionChainState.pendingContinuationKey = null;
            tryResumeC005SakuraSparkSequence(gameState, actionChainState, request.requestId);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC005SpringWindTarget, StringComparison.Ordinal))
        {
            var consumedSwordAura = consumeSavedC005SwordAura(actionChainState);
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C005:4 requires selected player to be an alive opponent.");
            }

            var appliedStatus = StatusRuntime.applyStatus(
                gameState,
                new StatusInstance
                {
                    statusKey = StatusKeyPenetrate,
                    applierPlayerId = useSkillActionRequest.actorPlayerId,
                    applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                    targetPlayerId = useSkillActionRequest.actorPlayerId,
                    targetCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                    stackCount = 1,
                    durationTypeKey = StatusRuntime.DurationTypeKeyNextDamageAttempt,
                });
            actionChainState.producedEvents.Add(new StatusChangedEvent
            {
                eventId = request.requestId,
                eventTypeKey = "statusChanged",
                sourceActionChainId = actionChainState.actionChainId,
                statusKey = appliedStatus.statusKey,
                targetPlayerId = appliedStatus.targetPlayerId,
                targetCharacterInstanceId = appliedStatus.targetCharacterInstanceId,
                isApplied = true,
            });

            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                3 + consumedSwordAura,
                DamageTypeKeyPhysical);
            return;
        }

        throw new InvalidOperationException("C005 skill continuation key is unsupported.");
    }

    private void tryResumeC005SakuraSparkSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC005SakuraSparkRemainingDamages, out var serializedDamages) ||
            gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey) ||
            actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest)
        {
            return;
        }

        var entries = new List<string>();
        foreach (var rawEntry in serializedDamages.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            entries.Add(rawEntry);
        }

        if (entries.Count == 0)
        {
            actionChainState.localState.Remove(LocalStateKeyC005SakuraSparkRemainingDamages);
            return;
        }

        var currentEntry = entries[0];
        entries.RemoveAt(0);
        actionChainState.localState[LocalStateKeyC005SakuraSparkRemainingDamages] = string.Join(",", entries);
        if (!tryParseC005DamageEntry(currentEntry, out var targetCharacterInstanceId, out var damageValue))
        {
            throw new InvalidOperationException("C005:3 stored damage entry is invalid.");
        }

        if (!gameState.characterInstances.TryGetValue(targetCharacterInstanceId, out var targetCharacterInstance) ||
            !targetCharacterInstance.isAlive ||
            !targetCharacterInstance.isInPlay)
        {
            tryResumeC005SakuraSparkSequence(gameState, actionChainState, eventId);
            return;
        }

        openDamageResponseWindowWithPendingDamage(
            gameState,
            actionChainState,
            eventId,
            useSkillActionRequest.actorPlayerId,
            null,
            useSkillActionRequest.characterInstanceId,
            targetCharacterInstance.characterInstanceId,
            damageValue,
            DamageTypeKeyPhysical);
    }

    private static bool isC005SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC005SakuraSparkSelectSwordAura, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC005SakuraSparkTargets, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC005SpringWindSelectSwordAura, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC005SpringWindTarget, StringComparison.Ordinal);
    }

    private static bool isValidC005ChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string? continuationKey)
    {
        if (string.Equals(continuationKey, ContinuationKeyC005SakuraSparkTargets, StringComparison.Ordinal))
        {
            var selectedTargetChoiceKeys = getC005SelectedTargetChoiceKeys(request);
            if (selectedTargetChoiceKeys.Count is < 1 or > 2)
            {
                return false;
            }

            foreach (var selectedTargetChoiceKey in selectedTargetChoiceKeys)
            {
                if (!inputContextState.choiceKeys.Contains(selectedTargetChoiceKey))
                {
                    return false;
                }
            }

            return true;
        }

        return inputContextState.choiceKeys.Contains(request.choiceKey);
    }

    private static List<string> collectC005SwordAuraAmountChoiceKeys(CharacterInstance characterInstance)
    {
        var markerCount = MarkerRuntime.getMarkerCount(characterInstance, MarkerTypeSwordAura);
        var choiceKeys = new List<string>();
        for (var amount = 0; amount <= markerCount; amount++)
        {
            choiceKeys.Add(ChoiceKeyC005SwordAuraPrefix + amount);
        }

        return choiceKeys;
    }

    private static int parseC005SwordAuraChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(ChoiceKeyC005SwordAuraPrefix, StringComparison.Ordinal) ||
            !int.TryParse(choiceKey.Substring(ChoiceKeyC005SwordAuraPrefix.Length), out var amount) ||
            amount < 0)
        {
            throw new InvalidOperationException("C005 swordAura choice requires swordAura:{amount} format.");
        }

        return amount;
    }

    private static int consumeSavedC005SwordAura(ActionChainState actionChainState)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC005ConsumedSwordAura, out var rawAmount) ||
            !int.TryParse(rawAmount, out var amount) ||
            amount < 0)
        {
            throw new InvalidOperationException("C005 skill requires saved swordAura amount.");
        }

        actionChainState.localState.Remove(LocalStateKeyC005ConsumedSwordAura);
        return amount;
    }

    private static List<string> getC005SelectedTargetChoiceKeys(SubmitInputChoiceActionRequest request)
    {
        var choiceKeys = new List<string>();
        foreach (var choiceKey in request.choiceKeys)
        {
            if (!choiceKeys.Contains(choiceKey))
            {
                choiceKeys.Add(choiceKey);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.choiceKey) &&
            !choiceKeys.Contains(request.choiceKey))
        {
            choiceKeys.Add(request.choiceKey);
        }

        return choiceKeys;
    }

    private static bool tryParseC005DamageEntry(
        string serializedEntry,
        out CharacterInstanceId targetCharacterInstanceId,
        out int damageValue)
    {
        targetCharacterInstanceId = default;
        damageValue = 0;
        var parts = serializedEntry.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 ||
            !long.TryParse(parts[0], out var characterNumericId) ||
            !int.TryParse(parts[1], out damageValue) ||
            damageValue < 0)
        {
            return false;
        }

        targetCharacterInstanceId = new CharacterInstanceId(characterNumericId);
        return true;
    }

    private void tryApplyC005SwordAuraBeforePhysicalDamage(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        DamageContext damageContext,
        long eventId)
    {
        if (!string.Equals(damageContext.damageType, DamageTypeKeyPhysical, StringComparison.Ordinal) ||
            actionChainState.localState.ContainsKey(LocalStateKeyC005SwordAuraGrantedForAction) ||
            !damageContext.sourcePlayerId.HasValue ||
            !damageContext.targetPlayerId.HasValue ||
            !gameState.players.TryGetValue(damageContext.sourcePlayerId.Value, out var sourcePlayerState) ||
            !gameState.players.TryGetValue(damageContext.targetPlayerId.Value, out var targetPlayerState) ||
            sourcePlayerState.teamId == targetPlayerState.teamId ||
            StatusRuntime.hasStatusOnPlayer(gameState, sourcePlayerState.playerId, StatusKeySilence))
        {
            return;
        }

        CharacterInstance? sourceCharacterInstance = null;
        if (damageContext.sourceCharacterInstanceId.HasValue &&
            gameState.characterInstances.TryGetValue(damageContext.sourceCharacterInstanceId.Value, out var explicitSourceCharacter))
        {
            sourceCharacterInstance = explicitSourceCharacter;
        }
        else if (tryFindAliveInPlayCharacterInstanceByOwner(gameState, sourcePlayerState.playerId, out var activeSourceCharacter))
        {
            sourceCharacterInstance = activeSourceCharacter;
        }

        if (sourceCharacterInstance is null ||
            !sourceCharacterInstance.isAlive ||
            !sourceCharacterInstance.isInPlay ||
            !string.Equals(sourceCharacterInstance.definitionId, CharacterDefinitionIdC005, StringComparison.Ordinal) ||
            !MarkerRuntime.canAddMarker(sourceCharacterInstance, MarkerTypeSwordAura))
        {
            return;
        }

        SkillPresentationEventEmitter.appendIfNeeded(
            actionChainState,
            sourcePlayerState.playerId,
            sourceCharacterInstance,
            "C005:1",
            "response");

        actionChainState.producedEvents.Add(MarkerRuntime.addMarker(
            sourceCharacterInstance,
            MarkerTypeSwordAura,
            1,
            actionChainState.actionChainId,
            eventId));
        actionChainState.localState[LocalStateKeyC005SwordAuraGrantedForAction] = "1";
    }

    private void tryOpenC005SixRealmsAfterFullDefense(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        ResponseWindowState responseWindowState,
        DamageContext damageContext,
        int producedEventsStartIndex,
        long eventId)
    {
        if (gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            damageContext.didDealDamage ||
            !responseWindowState.pendingDamageDefenderPlayerId.HasValue ||
            !responseWindowState.pendingDamageSourcePlayerId.HasValue ||
            !responseWindowState.pendingDamageTargetCharacterInstanceId.HasValue ||
            !tryParseCardDefenseDeclarationKey(
                responseWindowState.pendingDamageDefenseDeclarationKey ?? string.Empty,
                out _,
                out _))
        {
            return;
        }

        if (didCharacterHpDecreaseFromProducedEvents(
                actionChainState,
                responseWindowState.pendingDamageTargetCharacterInstanceId.Value,
                producedEventsStartIndex))
        {
            return;
        }

        var defenderPlayerId = responseWindowState.pendingDamageDefenderPlayerId.Value;
        var sourcePlayerId = responseWindowState.pendingDamageSourcePlayerId.Value;
        if (!gameState.players.TryGetValue(defenderPlayerId, out var defenderPlayerState) ||
            !gameState.players.TryGetValue(sourcePlayerId, out var sourcePlayerState) ||
            defenderPlayerState.teamId == sourcePlayerState.teamId ||
            StatusRuntime.hasStatusOnPlayer(gameState, defenderPlayerId, StatusKeySilence))
        {
            return;
        }

        if (!gameState.characterInstances.TryGetValue(
                responseWindowState.pendingDamageTargetCharacterInstanceId.Value,
                out var defenderCharacterInstance) ||
            !defenderCharacterInstance.isAlive ||
            !defenderCharacterInstance.isInPlay ||
            !string.Equals(defenderCharacterInstance.definitionId, CharacterDefinitionIdC005, StringComparison.Ordinal) ||
            MarkerRuntime.getMarkerCount(defenderCharacterInstance, MarkerTypeSwordAura) <= 0 ||
            !gameState.zones.TryGetValue(defenderPlayerState.deckZoneId, out var defenderDeckZoneState) ||
            defenderDeckZoneState.cardInstanceIds.Count == 0)
        {
            return;
        }

        var sourceCharacterInstance = default(CharacterInstance);
        if (responseWindowState.pendingDamageSourceCharacterInstanceId.HasValue)
        {
            gameState.characterInstances.TryGetValue(
                responseWindowState.pendingDamageSourceCharacterInstanceId.Value,
                out sourceCharacterInstance);
        }

        if (sourceCharacterInstance is null &&
            !tryFindAliveInPlayCharacterInstanceByOwner(gameState, sourcePlayerId, out sourceCharacterInstance))
        {
            return;
        }

        if (sourceCharacterInstance is null ||
            !sourceCharacterInstance.isAlive ||
            !sourceCharacterInstance.isInPlay)
        {
            return;
        }

        actionChainState.localState[LocalStateKeyC005SixRealmsDefenderCharacterInstanceId] =
            defenderCharacterInstance.characterInstanceId.Value.ToString();
        actionChainState.localState[LocalStateKeyC005SixRealmsSourceCharacterInstanceId] =
            sourceCharacterInstance.characterInstanceId.Value.ToString();
        openCharacterSkillChoiceInput(
            gameState,
            actionChainState,
            eventId,
            defenderPlayerId,
            InputTypeKeyC005SixRealmsChoice,
            ContextKeyC005SixRealmsChoice,
            ContinuationKeyC005SixRealmsChoice,
            new List<string> { ChoiceKeyDecline, ChoiceKeyC005SixRealmsUse });
    }

    private static bool didCharacterHpDecreaseFromProducedEvents(
        ActionChainState actionChainState,
        CharacterInstanceId characterInstanceId,
        int producedEventsStartIndex)
    {
        var startIndex = Math.Max(0, producedEventsStartIndex);
        for (var eventIndex = startIndex; eventIndex < actionChainState.producedEvents.Count; eventIndex++)
        {
            if (actionChainState.producedEvents[eventIndex] is HpChangedEvent hpChangedEvent &&
                hpChangedEvent.targetCharacterInstanceId == characterInstanceId &&
                hpChangedEvent.hpAfter < hpChangedEvent.hpBefore)
            {
                return true;
            }
        }

        return false;
    }

    private void continueC005SixRealmsChoice(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request)
    {
        if (!actionChainState.localState.TryGetValue(
                LocalStateKeyC005SixRealmsDefenderCharacterInstanceId,
                out var defenderCharacterRaw) ||
            !long.TryParse(defenderCharacterRaw, out var defenderCharacterNumericId) ||
            !actionChainState.localState.TryGetValue(
                LocalStateKeyC005SixRealmsSourceCharacterInstanceId,
                out var sourceCharacterRaw) ||
            !long.TryParse(sourceCharacterRaw, out var sourceCharacterNumericId))
        {
            clearC005SixRealmsState(actionChainState);
            throw new InvalidOperationException("C005:2 response requires saved defender and source characters.");
        }

        clearC005SixRealmsState(actionChainState);
        actionChainState.pendingContinuationKey = null;
        if (string.Equals(request.choiceKey, ChoiceKeyDecline, StringComparison.Ordinal))
        {
            return;
        }

        if (!string.Equals(request.choiceKey, ChoiceKeyC005SixRealmsUse, StringComparison.Ordinal) ||
            !gameState.characterInstances.TryGetValue(
                new CharacterInstanceId(defenderCharacterNumericId),
                out var defenderCharacterInstance) ||
            !gameState.characterInstances.TryGetValue(
                new CharacterInstanceId(sourceCharacterNumericId),
                out var sourceCharacterInstance) ||
            !defenderCharacterInstance.isAlive ||
            !defenderCharacterInstance.isInPlay ||
            !sourceCharacterInstance.isAlive ||
            !sourceCharacterInstance.isInPlay ||
            !string.Equals(defenderCharacterInstance.definitionId, CharacterDefinitionIdC005, StringComparison.Ordinal) ||
            MarkerRuntime.getMarkerCount(defenderCharacterInstance, MarkerTypeSwordAura) <= 0 ||
            !gameState.players.TryGetValue(defenderCharacterInstance.ownerPlayerId, out var defenderPlayerState) ||
            !gameState.zones.TryGetValue(defenderPlayerState.deckZoneId, out var defenderDeckZoneState) ||
            defenderDeckZoneState.cardInstanceIds.Count == 0)
        {
            throw new InvalidOperationException("C005:2 response can no longer be activated.");
        }

        actionChainState.producedEvents.Add(MarkerRuntime.removeMarker(
            defenderCharacterInstance,
            MarkerTypeSwordAura,
            1,
            actionChainState.actionChainId,
            request.requestId));

        var revealedCardInstance = gameState.cardInstances[defenderDeckZoneState.cardInstanceIds[0]];
        actionChainState.producedEvents.Add(new CardRevealedEvent
        {
            eventId = request.requestId,
            eventTypeKey = "cardRevealed",
            sourceActionChainId = actionChainState.actionChainId,
            cardInstanceId = revealedCardInstance.cardInstanceId,
            ownerPlayerId = revealedCardInstance.ownerPlayerId,
            definitionId = revealedCardInstance.definitionId,
            revealReasonKey = RevealReasonKeyC005SixRealms,
        });

        var summonCost = TreasureDefinitionRepository
            .resolveByDefinitionId(revealedCardInstance.definitionId)
            .summonSigilCost ?? 0;
        var damageValue = Math.Max(0, summonCost - 1);
        if (damageValue <= 0)
        {
            return;
        }

        openDamageResponseWindowWithPendingDamage(
            gameState,
            actionChainState,
            request.requestId,
            defenderCharacterInstance.ownerPlayerId,
            null,
            defenderCharacterInstance.characterInstanceId,
            sourceCharacterInstance.characterInstanceId,
            damageValue,
            DamageTypeKeyPhysical);
    }

    private static void clearC005SixRealmsState(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(LocalStateKeyC005SixRealmsDefenderCharacterInstanceId);
        actionChainState.localState.Remove(LocalStateKeyC005SixRealmsSourceCharacterInstanceId);
    }

    private void tryOpenC004StardustCarnivalAfterDefense(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        ResponseWindowState responseWindowState,
        long eventId)
    {
        if (gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            gameState.turnState is null ||
            gameState.turnState.usedOncePerTurnSkillKeys.Contains(SkillKeyC004_2) ||
            responseWindowState.pendingDamageDefenseDeclarationKey is null ||
            !responseWindowState.pendingDamageDefenderPlayerId.HasValue ||
            !responseWindowState.pendingDamageSourcePlayerId.HasValue ||
            !responseWindowState.pendingDamageTargetCharacterInstanceId.HasValue)
        {
            return;
        }

        var defenderPlayerId = responseWindowState.pendingDamageDefenderPlayerId.Value;
        var sourcePlayerId = responseWindowState.pendingDamageSourcePlayerId.Value;
        if (StatusRuntime.hasStatusOnPlayer(gameState, defenderPlayerId, StatusKeySilence))
        {
            return;
        }

        if (!gameState.characterInstances.TryGetValue(
                responseWindowState.pendingDamageTargetCharacterInstanceId.Value,
                out var defenderCharacter) ||
            !defenderCharacter.isAlive ||
            !defenderCharacter.isInPlay ||
            !string.Equals(defenderCharacter.definitionId, "C004", StringComparison.Ordinal) ||
            !gameState.players.TryGetValue(defenderPlayerId, out var defenderPlayerState) ||
            !gameState.zones.TryGetValue(defenderPlayerState.handZoneId, out var handZoneState) ||
            handZoneState.cardInstanceIds.Count == 0)
        {
            return;
        }

        var choiceKeys = new List<string> { ChoiceKeyDecline };
        foreach (var cardInstanceId in handZoneState.cardInstanceIds)
        {
            choiceKeys.Add(ChoiceKeyDiscardCardPrefix + cardInstanceId.Value);
        }

        actionChainState.localState[LocalStateKeyC004StardustSourcePlayerId] = sourcePlayerId.Value.ToString();
        openCharacterSkillChoiceInput(
            gameState,
            actionChainState,
            eventId,
            defenderPlayerId,
            InputTypeKeyC004DiscardCard,
            ContextKeyC004StardustDiscard,
            ContinuationKeyC004StardustDiscard,
            choiceKeys);
    }

    private void continueC004StardustCarnival(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        if (request.choiceKey == ChoiceKeyDecline)
        {
            actionChainState.localState.Remove(LocalStateKeyC004StardustSourcePlayerId);
            actionChainState.pendingContinuationKey = null;
            return;
        }

        if (!actionChainState.localState.TryGetValue(LocalStateKeyC004StardustSourcePlayerId, out var sourcePlayerIdRaw) ||
            !long.TryParse(sourcePlayerIdRaw, out var sourcePlayerNumericId))
        {
            throw new InvalidOperationException("C004:2 requires saved damage source player id.");
        }

        var selectedCardInstanceId = parseCardChoiceKey(request.choiceKey, ChoiceKeyDiscardCardPrefix);
        if (!gameState.players.TryGetValue(request.actorPlayerId, out var actorPlayerState) ||
            !gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance) ||
            selectedCardInstance.ownerPlayerId != request.actorPlayerId ||
            selectedCardInstance.zoneId != actorPlayerState.handZoneId)
        {
            throw new InvalidOperationException("C004:2 requires selected discard card to remain in actor hand.");
        }

        actionChainState.producedEvents.Add(zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            actorPlayerState.discardZoneId,
            CardMoveReason.discard,
            actionChainState.actionChainId,
            request.requestId));
        gameState.turnState!.usedOncePerTurnSkillKeys.Add(SkillKeyC004_2);
        actionChainState.localState.Remove(LocalStateKeyC004StardustSourcePlayerId);
        actionChainState.pendingContinuationKey = null;

        var sourcePlayerId = new PlayerId(sourcePlayerNumericId);
        if (!tryFindAliveInPlayCharacterInstanceByOwner(gameState, sourcePlayerId, out var sourceCharacterInstance))
        {
            return;
        }

        tryFindAliveInPlayCharacterInstanceByOwner(
            gameState,
            request.actorPlayerId,
            out var stardustSourceCharacterInstance);
        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(
            gameState,
            new DamageContext
            {
                damageContextId = new DamageContextId(request.requestId),
                sourcePlayerId = request.actorPlayerId,
                sourceCharacterInstanceId = stardustSourceCharacterInstance?.characterInstanceId,
                targetPlayerId = sourcePlayerId,
                targetCharacterInstanceId = sourceCharacterInstance.characterInstanceId,
                baseDamageValue = 1,
                damageType = DamageTypeKeyDirect,
            }));
    }

    private void tryResumeC004BlazingCometSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (!actionChainState.localState.ContainsKey(LocalStateKeyC004BlazingCometSequence) ||
            gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey) ||
            actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState))
        {
            return;
        }

        var choiceKeys = collectC004BanishChoiceKeys(gameState, actorPlayerState);
        if (choiceKeys.Count == 0)
        {
            actionChainState.localState.Remove(LocalStateKeyC004BlazingCometSequence);
            return;
        }

        openCharacterSkillChoiceInput(
            gameState,
            actionChainState,
            eventId,
            useSkillActionRequest.actorPlayerId,
            InputTypeKeyC004BanishCard,
            ContextKeyC004BlazingCometBanish,
            ContinuationKeyC004BlazingCometBanish,
            choiceKeys);
    }

    private void continueC004BlazingCometBanish(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        var selectedCardInstanceId = parseCardChoiceKey(request.choiceKey, ChoiceKeyBanishCardPrefix);
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState) ||
            request.actorPlayerId != useSkillActionRequest.actorPlayerId ||
            !gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance) ||
            selectedCardInstance.ownerPlayerId != useSkillActionRequest.actorPlayerId ||
            (selectedCardInstance.zoneId != actorPlayerState.handZoneId &&
             selectedCardInstance.zoneId != actorPlayerState.discardZoneId) ||
            gameState.publicState is null)
        {
            throw new InvalidOperationException("C004:4 requires selected banish card to remain in actor hand or discard.");
        }

        actionChainState.producedEvents.Add(zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            gameState.publicState.gapZoneId,
            CardMoveReason.banish,
            actionChainState.actionChainId,
            request.requestId));
        actionChainState.localState.Remove(LocalStateKeyC004BlazingCometSequence);
        actionChainState.pendingContinuationKey = null;
        tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
            gameState,
            actionChainState,
            request.requestId,
            producedEventsStartIndex: 0);
    }

    private static List<string> collectC004BanishChoiceKeys(
        GameState.GameState gameState,
        GameState.PlayerState actorPlayerState)
    {
        var choiceKeys = new List<string>();
        if (gameState.zones.TryGetValue(actorPlayerState.handZoneId, out var handZoneState))
        {
            foreach (var cardInstanceId in handZoneState.cardInstanceIds)
            {
                choiceKeys.Add(ChoiceKeyBanishCardPrefix + cardInstanceId.Value);
            }
        }

        if (gameState.zones.TryGetValue(actorPlayerState.discardZoneId, out var discardZoneState))
        {
            foreach (var cardInstanceId in discardZoneState.cardInstanceIds)
            {
                choiceKeys.Add(ChoiceKeyBanishCardPrefix + cardInstanceId.Value);
            }
        }

        return choiceKeys;
    }

    private static CardInstanceId parseCardChoiceKey(string choiceKey, string prefix)
    {
        if (!choiceKey.StartsWith(prefix, StringComparison.Ordinal) ||
            !long.TryParse(choiceKey.Substring(prefix.Length), out var cardInstanceNumericId) ||
            cardInstanceNumericId <= 0)
        {
            throw new InvalidOperationException("Card choice key has invalid format.");
        }

        return new CardInstanceId(cardInstanceNumericId);
    }

    private static List<string> collectPlayerHandDiscardChoiceKeys(
        GameState.GameState gameState,
        GameState.PlayerState playerState)
    {
        var choiceKeys = new List<string>();
        if (!gameState.zones.TryGetValue(playerState.handZoneId, out var handZoneState))
        {
            return choiceKeys;
        }

        foreach (var cardInstanceId in handZoneState.cardInstanceIds)
        {
            choiceKeys.Add(ChoiceKeyDiscardCardPrefix + cardInstanceId.Value);
        }

        return choiceKeys;
    }

    private static List<string> collectOpponentPlayerChoiceKeys(
        GameState.GameState gameState,
        GameState.PlayerState actorPlayerState)
    {
        return collectC007OpponentChoiceKeys(gameState, actorPlayerState);
    }

    private void continueC001SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (!string.Equals(continuationKey, ContinuationKeyC001SealTarget, StringComparison.Ordinal) ||
            actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("C001:1 continuation requires a valid UseSkillActionRequest root.");
        }

        var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
        if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
            targetPlayerState.teamId == actorPlayerState.teamId ||
            !tryFindAliveInPlayCharacterInstanceByOwner(
                gameState,
                targetPlayerId,
                out var targetCharacterInstance))
        {
            throw new InvalidOperationException("C001:1 requires selected player to be an alive opponent.");
        }

        var sealStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = StatusKeySeal,
                applierPlayerId = useSkillActionRequest.actorPlayerId,
                applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                targetPlayerId = targetPlayerId,
                targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                stackCount = 1,
            });
        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = request.requestId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = sealStatus.statusKey,
            targetPlayerId = sealStatus.targetPlayerId,
            targetCharacterInstanceId = sealStatus.targetCharacterInstanceId,
            isApplied = true,
        });

        actionChainState.pendingContinuationKey = null;
        if (gameState.characterInstances.TryGetValue(
                useSkillActionRequest.characterInstanceId,
                out var sourceCharacterInstance) &&
            sourceCharacterInstance.isActivated)
        {
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                3,
                DamageTypeKeySpell);
        }
    }

    private static bool isC001SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC001SealTarget, StringComparison.Ordinal);
    }

    private void continueC008SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("C008 skill continuation requires a valid UseSkillActionRequest root.");
        }

        if (string.Equals(continuationKey, ContinuationKeyC008WindKingTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out _))
            {
                throw new InvalidOperationException("C008:1 requires selected player to be an alive opponent.");
            }

            actionChainState.localState[LocalStateKeyC008WindKingTargetPlayerId] =
                targetPlayerId.Value.ToString();
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC008DamageType,
                ContextKeyC008WindKingDamageType,
                ContinuationKeyC008WindKingDamageType,
                new List<string>
                {
                    ChoiceKeyDamageTypePhysical,
                    ChoiceKeyDamageTypeSpell,
                });
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC008WindKingDamageType, StringComparison.Ordinal))
        {
            if (!actionChainState.localState.TryGetValue(
                    LocalStateKeyC008WindKingTargetPlayerId,
                    out var targetPlayerIdRaw) ||
                !long.TryParse(targetPlayerIdRaw, out var targetPlayerNumericId))
            {
                throw new InvalidOperationException("C008:1 damage-type continuation requires a saved target player.");
            }

            var damageTypeKey = request.choiceKey switch
            {
                ChoiceKeyDamageTypePhysical => DamageTypeKeyPhysical,
                ChoiceKeyDamageTypeSpell => DamageTypeKeySpell,
                _ => throw new InvalidOperationException("C008:1 damage type must be physical or spell."),
            };
            var targetPlayerId = new PlayerId(targetPlayerNumericId);
            if (!tryFindAliveInPlayCharacterInstanceByOwner(
                    gameState,
                    targetPlayerId,
                    out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C008:1 selected opponent no longer has an alive in-play character.");
            }

            actionChainState.localState.Remove(LocalStateKeyC008WindKingTargetPlayerId);
            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                3,
                damageTypeKey);
            return;
        }

        throw new InvalidOperationException("C008 skill continuation key is unsupported.");
    }

    private static bool isC008SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC008WindKingTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC008WindKingDamageType, StringComparison.Ordinal);
    }

    private static void applyCharacterStatus(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        UseSkillActionRequest useSkillActionRequest,
        string statusKey,
        PlayerId? targetPlayerId = null,
        CharacterInstanceId? targetCharacterInstanceId = null,
        string? durationTypeKey = null)
    {
        var appliedStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = statusKey,
                applierPlayerId = useSkillActionRequest.actorPlayerId,
                applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                targetPlayerId = targetPlayerId,
                targetCharacterInstanceId = targetCharacterInstanceId,
                stackCount = 1,
                durationTypeKey = durationTypeKey ?? string.Empty,
            });
        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = useSkillActionRequest.requestId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = appliedStatus.statusKey,
            targetPlayerId = appliedStatus.targetPlayerId,
            targetCharacterInstanceId = appliedStatus.targetCharacterInstanceId,
            isApplied = true,
        });
    }

    private void continueC011SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("C011 skill continuation requires a valid UseSkillActionRequest root.");
        }

        if (string.Equals(continuationKey, ContinuationKeyC011MysticEyesTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C011:1 requires selected player to be an alive opponent.");
            }

            if (!gameState.characterInstances.TryGetValue(
                    useSkillActionRequest.characterInstanceId,
                    out var sourceCharacterInstance))
            {
                throw new InvalidOperationException("C011:1 requires source character to exist.");
            }

            actionChainState.pendingContinuationKey = null;
            if (sourceCharacterInstance.isActivated)
            {
                applyCharmStatusToTarget(
                    gameState,
                    actionChainState,
                    request.requestId,
                    useSkillActionRequest.actorPlayerId,
                    useSkillActionRequest.characterInstanceId,
                    targetPlayerId,
                    targetCharacterInstance.characterInstanceId);
                return;
            }

            actionChainState.localState[LocalStateKeyC011MysticEyesPendingFullDefenseCharm] =
                targetCharacterInstance.characterInstanceId.Value.ToString();
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                3,
                DamageTypeKeySpell);
            if (gameState.currentResponseWindow is null &&
                !string.Equals(
                    actionChainState.pendingContinuationKey,
                    ContinuationKeyC003GungnirDiscard,
                    StringComparison.Ordinal))
            {
                actionChainState.localState.Remove(LocalStateKeyC011MysticEyesPendingFullDefenseCharm);
            }
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC011TrueAncestorCombatTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C011:2 requires selected player to be an alive opponent.");
            }

            actionChainState.pendingContinuationKey = null;
            if (!gameState.characterInstances.TryGetValue(
                    useSkillActionRequest.characterInstanceId,
                    out var sourceCharacterInstance))
            {
                throw new InvalidOperationException("C011:2 requires source character to exist.");
            }

            var damageValue = sourceCharacterInstance.isActivated ? 6 : 4;
            if (sourceCharacterInstance.isActivated)
            {
                actionChainState.localState[LocalStateKeyC011TrueAncestorCombatDeactivateAfterDamage] =
                    useSkillActionRequest.characterInstanceId.Value.ToString();
            }

            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                damageValue,
                DamageTypeKeyPhysical);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC011RealityMarbleSelectSummon, StringComparison.Ordinal))
        {
            continueC011RealityMarbleSelectedSummon(
                gameState,
                actionChainState,
                inputContextState,
                request);
            return;
        }

        throw new InvalidOperationException("C011 skill continuation key is unsupported.");
    }

    private void continueC013SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState))
        {
            if (string.Equals(continuationKey, ContinuationKeyC013AbnormalTasteDiscard, StringComparison.Ordinal))
            {
                continueC013AbnormalTasteDiscard(gameState, actionChainState, request);
                return;
            }

            throw new InvalidOperationException("C013 skill continuation requires a valid UseSkillActionRequest root.");
        }

        if (string.Equals(continuationKey, ContinuationKeyC013AbnormalTasteDiscard, StringComparison.Ordinal))
        {
            continueC013AbnormalTasteDiscard(gameState, actionChainState, request);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC013DarkJadeFistTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            var revealedCardInstance = getOpponentDeckTopCardOrThrow(gameState, actorPlayerState, targetPlayerId, "C013:3");
            var targetCharacterInstance = getAliveInPlayCharacterByOwnerOrThrow(gameState, targetPlayerId, "C013:3");
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState))
            {
                throw new InvalidOperationException("C013:3 requires target player to exist.");
            }

            if (gameState.matchMeta is null)
            {
                throw new InvalidOperationException("C013:3 requires match meta.");
            }

            var targetTeamState = gameState.teams[targetPlayerState.teamId];
            targetTeamState.killScore = Math.Min(gameState.matchMeta.baseKillScore, targetTeamState.killScore + 1);
            actionChainState.producedEvents.Add(new CardRevealedEvent
            {
                eventId = request.requestId,
                eventTypeKey = "cardRevealed",
                sourceActionChainId = actionChainState.actionChainId,
                cardInstanceId = revealedCardInstance.cardInstanceId,
                ownerPlayerId = revealedCardInstance.ownerPlayerId,
                definitionId = revealedCardInstance.definitionId,
                revealReasonKey = RevealReasonKeyC013DarkJadeFist,
            });

            var defenseValue = TreasureResourceValueResolver.resolveDefenseValue(revealedCardInstance.definitionId) ?? 0;
            var damageValue = Math.Max(0, 7 - defenseValue);
            actionChainState.pendingContinuationKey = null;
            appendDirectDamageFromCharacter(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                damageValue);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC013CriminalTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            var revealedCardInstance = getOpponentDeckTopCardOrThrow(gameState, actorPlayerState, targetPlayerId, "C013:4");
            var targetCharacterInstance = getAliveInPlayCharacterByOwnerOrThrow(gameState, targetPlayerId, "C013:4");
            var summonCost = TreasureDefinitionRepository
                .resolveByDefinitionId(revealedCardInstance.definitionId)
                .summonSigilCost ?? 0;
            var damageValue = Math.Max(0, summonCost);

            actionChainState.localState[LocalStateKeyC013CriminalTargetPlayerId] = targetPlayerId.Value.ToString();
            actionChainState.localState[LocalStateKeyC013CriminalRevealedCardInstanceId] =
                revealedCardInstance.cardInstanceId.Value.ToString();
            actionChainState.producedEvents.Add(new CardRevealedEvent
            {
                eventId = request.requestId,
                eventTypeKey = "cardRevealed",
                sourceActionChainId = actionChainState.actionChainId,
                cardInstanceId = revealedCardInstance.cardInstanceId,
                ownerPlayerId = revealedCardInstance.ownerPlayerId,
                definitionId = revealedCardInstance.definitionId,
                revealReasonKey = RevealReasonKeyC013Criminal,
            });
            actionChainState.pendingContinuationKey = null;
            appendDirectDamageFromCharacter(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                damageValue);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC013CriminalTakeRevealed, StringComparison.Ordinal))
        {
            continueC013CriminalTakeRevealed(gameState, actionChainState, request);
            return;
        }

        throw new InvalidOperationException("C013 skill continuation key is unsupported.");
    }

    private bool tryOpenC013AbnormalTasteAfterDefense(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        ResponseWindowState responseWindowState,
        long eventId,
        CardInstance formalDefenseCardInstance,
        GameState.PlayerState defenderPlayerState,
        CharacterInstanceId defenderActiveCharacterInstanceIdBeforeResolution,
        int producedEventsCountBeforeDamageResolution)
    {
        if (!responseWindowState.pendingDamageSourcePlayerId.HasValue ||
            gameState.currentInputContext is not null ||
            !gameState.players.TryGetValue(responseWindowState.pendingDamageSourcePlayerId.Value, out var sourcePlayerState))
        {
            return false;
        }

        var candidate = findAliveInPlayC013OnTeam(gameState, sourcePlayerState.teamId);
        if (candidate.characterInstance is null ||
            candidate.playerState is null ||
            StatusRuntime.hasStatusOnPlayer(gameState, candidate.playerState.playerId, StatusKeySilence) ||
            candidate.playerState.teamId == defenderPlayerState.teamId ||
            !gameState.zones.TryGetValue(candidate.playerState.handZoneId, out var c013HandZoneState) ||
            c013HandZoneState.cardInstanceIds.Count == 0 ||
            !gameState.teams.TryGetValue(candidate.playerState.teamId, out var c013TeamState) ||
            c013TeamState.leyline < 2)
        {
            return false;
        }

        var choiceKeys = new List<string> { ChoiceKeyDecline };
        foreach (var cardInstanceId in c013HandZoneState.cardInstanceIds)
        {
            choiceKeys.Add(ChoiceKeyDiscardCardPrefix + cardInstanceId.Value);
        }

        actionChainState.localState[LocalStateKeyC013AbnormalTasteDefenseCardInstanceId] =
            formalDefenseCardInstance.cardInstanceId.Value.ToString();
        actionChainState.localState[LocalStateKeyC013AbnormalTasteDefenderPlayerId] =
            defenderPlayerState.playerId.Value.ToString();
        actionChainState.localState[LocalStateKeyC013AbnormalTasteDefenderCharacterInstanceId] =
            defenderActiveCharacterInstanceIdBeforeResolution.Value.ToString();
        actionChainState.localState[LocalStateKeyC013AbnormalTasteProducedStartIndex] =
            producedEventsCountBeforeDamageResolution.ToString();

        openCharacterSkillChoiceInput(
            gameState,
            actionChainState,
            eventId,
            candidate.playerState.playerId,
            InputTypeKeyC013DiscardCard,
            ContextKeyC013AbnormalTasteDiscard,
            ContinuationKeyC013AbnormalTasteDiscard,
            choiceKeys);
        return true;
    }

    private void continueC013AbnormalTasteDiscard(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request)
    {
        var responseWindowState = gameState.currentResponseWindow;
        if (responseWindowState is null ||
            !responseWindowState.pendingDamageSourcePlayerId.HasValue ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC013AbnormalTasteDefenseCardInstanceId, out var defenseCardRaw) ||
            !long.TryParse(defenseCardRaw, out var defenseCardNumericId) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC013AbnormalTasteDefenderPlayerId, out var defenderRaw) ||
            !long.TryParse(defenderRaw, out var defenderNumericId) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC013AbnormalTasteDefenderCharacterInstanceId, out var defenderCharacterRaw) ||
            !long.TryParse(defenderCharacterRaw, out var defenderCharacterNumericId) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC013AbnormalTasteProducedStartIndex, out var producedStartRaw) ||
            !int.TryParse(producedStartRaw, out var producedStartIndex))
        {
            clearC013AbnormalTasteState(actionChainState);
            throw new InvalidOperationException("C013:2 continuation requires saved defense response state.");
        }

        var defenderPlayerId = new PlayerId(defenderNumericId);
        var defenderCharacterInstanceId = new CharacterInstanceId(defenderCharacterNumericId);
        var defenseCardInstanceId = new CardInstanceId(defenseCardNumericId);
        if (!gameState.players.TryGetValue(request.actorPlayerId, out var c013PlayerState) ||
            !gameState.teams.TryGetValue(c013PlayerState.teamId, out var c013TeamState))
        {
            clearC013AbnormalTasteState(actionChainState);
            throw new InvalidOperationException("C013:2 continuation requires C013 player state.");
        }

        if (!string.Equals(request.choiceKey, ChoiceKeyDecline, StringComparison.Ordinal))
        {
            var selectedCardInstanceId = parseCardChoiceKey(request.choiceKey, ChoiceKeyDiscardCardPrefix);
            if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance) ||
                selectedCardInstance.ownerPlayerId != request.actorPlayerId ||
                selectedCardInstance.zoneId != c013PlayerState.handZoneId ||
                c013TeamState.leyline < 2)
            {
                clearC013AbnormalTasteState(actionChainState);
                throw new InvalidOperationException("C013:2 requires selected card to be in C013 player's hand and team to have 2 leyline.");
            }

            actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                gameState,
                selectedCardInstance,
                c013PlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                request.requestId));
            c013TeamState.leyline -= 2;

            var replacementDefenseValue =
                TreasureResourceValueResolver.resolveDefenseValue(selectedCardInstance.definitionId) ?? 0;
            if (!tryParseCardDefenseDeclarationKey(
                    responseWindowState.pendingDamageDefenseDeclarationKey ?? string.Empty,
                    out var originalDefenseTypeKey,
                    out _))
            {
                clearC013AbnormalTasteState(actionChainState);
                throw new InvalidOperationException("C013:2 requires current defense declaration to be a card defense declaration.");
            }

            responseWindowState.pendingDamageDefenseDeclarationKey =
                createCardDefenseDeclarationKey(originalDefenseTypeKey, replacementDefenseValue);
            responseWindowState.pendingDamageKillCreditPlayerIdOverride = request.actorPlayerId;
        }

        clearC013AbnormalTasteState(actionChainState);
        actionChainState.pendingContinuationKey = ContinuationKeyStagedResponseDamage;
        closeDamageResponseWindowAndResolveDamage(
            gameState,
            actionChainState,
            responseWindowState,
            request.requestId);

        if (gameState.players.TryGetValue(defenderPlayerId, out var defenderPlayerState) &&
            gameState.cardInstances.TryGetValue(defenseCardInstanceId, out var defenseCardInstance))
        {
            if (string.Equals(defenseCardInstance.definitionId, TreasureDefinitionIdT014, StringComparison.Ordinal))
            {
                applyT014OnDefensePostResolution(
                    gameState,
                    actionChainState,
                    request.requestId,
                    defenderPlayerState,
                    defenseCardInstance,
                    producedStartIndex,
                    defenderCharacterInstanceId);
            }

            if (string.Equals(defenseCardInstance.definitionId, TreasureDefinitionIdT015, StringComparison.Ordinal))
            {
                applyT015OnDefensePostResolution(
                    gameState,
                    actionChainState,
                    request.requestId,
                    defenderPlayerState,
                    responseWindowState.pendingDamageSourcePlayerId.Value,
                    defenseCardInstance,
                    producedStartIndex,
                    defenderCharacterInstanceId);
            }

            if (string.Equals(defenseCardInstance.definitionId, TreasureDefinitionIdT020, StringComparison.Ordinal))
            {
                moveDefensePlacedCardToDiscardPostResolution(
                    gameState,
                    actionChainState,
                    request.requestId,
                    defenderPlayerState,
                    defenseCardInstance);
            }
        }

        tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
            gameState,
            actionChainState,
            request.requestId,
            producedStartIndex);
        tryResumeCharacterSkillSequences(gameState, actionChainState, request.requestId);
    }

    private static (CharacterInstance? characterInstance, GameState.PlayerState? playerState) findAliveInPlayC013OnTeam(
        GameState.GameState gameState,
        TeamId teamId)
    {
        foreach (var characterInstance in gameState.characterInstances.Values)
        {
            if (!characterInstance.isAlive ||
                !characterInstance.isInPlay ||
                !string.Equals(characterInstance.definitionId, CharacterDefinitionIdC013, StringComparison.Ordinal) ||
                !gameState.players.TryGetValue(characterInstance.ownerPlayerId, out var playerState) ||
                playerState.teamId != teamId)
            {
                continue;
            }

            return (characterInstance, playerState);
        }

        return (null, null);
    }

    private static void clearC013AbnormalTasteState(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(LocalStateKeyC013AbnormalTasteDefenseCardInstanceId);
        actionChainState.localState.Remove(LocalStateKeyC013AbnormalTasteDefenderPlayerId);
        actionChainState.localState.Remove(LocalStateKeyC013AbnormalTasteDefenderCharacterInstanceId);
        actionChainState.localState.Remove(LocalStateKeyC013AbnormalTasteProducedStartIndex);
    }

    private static bool isC013SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC013AbnormalTasteDiscard, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC013DarkJadeFistTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC013CriminalTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC013CriminalTakeRevealed, StringComparison.Ordinal);
    }

    private void continueC014SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (string.Equals(continuationKey, ContinuationKeyC014AndThenChoice, StringComparison.Ordinal))
        {
            continueC014AndThenChoice(gameState, actionChainState, request);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC014AndThenDiscard, StringComparison.Ordinal))
        {
            continueC014AndThenDiscard(gameState, actionChainState, inputContextState, request);
            return;
        }

        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState) ||
            !gameState.characterInstances.TryGetValue(useSkillActionRequest.characterInstanceId, out var sourceCharacterInstance))
        {
            throw new InvalidOperationException("C014 skill continuation requires a valid UseSkillActionRequest root.");
        }

        if (string.Equals(continuationKey, ContinuationKeyC014LaevateinDiscard, StringComparison.Ordinal))
        {
            discardSelectedHandCard(gameState, actionChainState, actorPlayerState, request, "C014:3");
            if (MarkerRuntime.canAddMarker(sourceCharacterInstance, MarkerTypeDestruction))
            {
                actionChainState.producedEvents.Add(MarkerRuntime.addMarker(
                    sourceCharacterInstance,
                    MarkerTypeDestruction,
                    1,
                    actionChainState.actionChainId,
                    request.requestId));
            }

            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC014TargetPlayer,
                ContextKeyC014LaevateinTarget,
                ContinuationKeyC014LaevateinTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC014LaevateinTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C014:3 requires selected player to be an alive opponent.");
            }

            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                1 + MarkerRuntime.getMarkerCount(sourceCharacterInstance, MarkerTypeDestruction),
                DamageTypeKeyPhysical);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC014RippleDiscard, StringComparison.Ordinal))
        {
            discardSelectedHandCard(gameState, actionChainState, actorPlayerState, request, "C014:4");
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC014DestructionAmount,
                ContextKeyC014RippleX,
                ContinuationKeyC014RippleX,
                collectC014DestructionAmountChoiceKeys(sourceCharacterInstance));
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC014RippleX, StringComparison.Ordinal))
        {
            var selectedX = parseC014DestructionChoiceKey(request.choiceKey);
            var markerCount = MarkerRuntime.getMarkerCount(sourceCharacterInstance, MarkerTypeDestruction);
            if (selectedX < 0 || selectedX > markerCount)
            {
                throw new InvalidOperationException("C014:4 selected destruction marker amount exceeds current marker count.");
            }

            if (selectedX > 0)
            {
                actionChainState.producedEvents.Add(MarkerRuntime.removeMarker(
                    sourceCharacterInstance,
                    MarkerTypeDestruction,
                    selectedX,
                    actionChainState.actionChainId,
                    request.requestId));
            }

            var remainingDamageEntries = new List<string>();
            foreach (var playerId in getPlayersInSeatOrder(gameState))
            {
                if (!gameState.players.TryGetValue(playerId, out var targetPlayerState) ||
                    targetPlayerState.teamId == actorPlayerState.teamId ||
                    !tryFindAliveInPlayCharacterInstanceByOwner(gameState, playerId, out var targetCharacterInstance))
                {
                    continue;
                }

                remainingDamageEntries.Add($"{targetCharacterInstance.characterInstanceId.Value}:{2 * selectedX}");
            }

            actionChainState.localState[LocalStateKeyC014RippleRemainingDamages] =
                string.Join(",", remainingDamageEntries);
            actionChainState.localState[LocalStateKeyC014RipplePendingSelfDamage] =
                Math.Max(0, selectedX - 1).ToString();
            actionChainState.localState[LocalStateKeyC014RippleSequence] = C014RippleSequenceCollecting;
            actionChainState.localState.Remove(LocalStateKeyC014RippleQueuedDamageContexts);
            actionChainState.localState.Remove(LocalStateKeyC014RippleBeforeDefenseResponsesConsumed);
            actionChainState.localState.Remove(LocalStateKeyC014RippleDamageBonus);
            actionChainState.localState.Remove(LocalStateKeyC014RippleForcePenetrate);
            actionChainState.pendingContinuationKey = null;
            tryResumeC014RippleSequence(gameState, actionChainState, request.requestId);
            return;
        }

        throw new InvalidOperationException("C014 skill continuation key is unsupported.");
    }

    private static bool isC014SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC014AndThenChoice, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC014AndThenDiscard, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC014LaevateinDiscard, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC014LaevateinTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC014RippleDiscard, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC014RippleX, StringComparison.Ordinal);
    }

    private void tryResumeC014RippleSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (!actionChainState.localState.ContainsKey(LocalStateKeyC014RippleRemainingDamages) ||
            gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey) ||
            actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest)
        {
            return;
        }

        while (gameState.matchState == GameState.MatchState.running &&
               gameState.currentInputContext is null &&
               gameState.currentResponseWindow is null &&
               string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            if (actionChainState.localState.TryGetValue(LocalStateKeyC014RippleSequence, out var sequenceState) &&
                string.Equals(sequenceState, C014RippleSequenceResolving, StringComparison.Ordinal))
            {
                if (continueC014RippleFinalDamage(gameState, actionChainState, eventId))
                {
                    continue;
                }

                return;
            }

            var remainingDamages = loadC014RippleRemainingDamages(actionChainState);
            if (remainingDamages.Count > 0)
            {
                var nextDamage = remainingDamages[0];
                remainingDamages.RemoveAt(0);
                saveC014RippleRemainingDamages(actionChainState, remainingDamages);
                if (nextDamage.damageValue <= 0)
                {
                    continue;
                }

                var suppressBeforeDefenseResponses =
                    actionChainState.localState.ContainsKey(LocalStateKeyC014RippleBeforeDefenseResponsesConsumed);
                actionChainState.localState[LocalStateKeyC014RippleBeforeDefenseResponsesConsumed] = "true";
                var damageBonus = loadOptionalIntLocalState(actionChainState, LocalStateKeyC014RippleDamageBonus);
                var forcePenetrate =
                    suppressBeforeDefenseResponses &&
                    actionChainState.localState.ContainsKey(LocalStateKeyC014RippleForcePenetrate);
                openDamageResponseWindowWithPendingDamage(
                    gameState,
                    actionChainState,
                    eventId,
                    useSkillActionRequest.actorPlayerId,
                    null,
                    useSkillActionRequest.characterInstanceId,
                    nextDamage.targetCharacterInstanceId,
                    nextDamage.damageValue + damageBonus,
                    DamageTypeKeySpell,
                    suppressC003Gungnir: suppressBeforeDefenseResponses,
                    suppressC014AndThen: suppressBeforeDefenseResponses,
                    forcePenetrate: forcePenetrate);
                return;
            }

            actionChainState.localState[LocalStateKeyC014RippleSequence] = C014RippleSequenceResolving;
            if (continueC014RippleFinalDamage(gameState, actionChainState, eventId))
            {
                continue;
            }

            return;
        }
    }

    private bool continueC014RippleFinalDamage(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        while (gameState.matchState == GameState.MatchState.running &&
               gameState.currentInputContext is null &&
               gameState.currentResponseWindow is null &&
               string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            var queuedDamageContexts = loadC014QueuedDamageContexts(actionChainState);
            if (queuedDamageContexts.Count > 0)
            {
                var damageContext = queuedDamageContexts[0];
                queuedDamageContexts.RemoveAt(0);
                saveC014QueuedDamageContexts(actionChainState, queuedDamageContexts);
                if (!damageContext.targetCharacterInstanceId.HasValue ||
                    !gameState.characterInstances.TryGetValue(
                        damageContext.targetCharacterInstanceId.Value,
                        out var targetCharacterInstance) ||
                    !targetCharacterInstance.isAlive ||
                    !targetCharacterInstance.isInPlay)
                {
                    continue;
                }

                var queuedDamageProducedEventsStartIndex = actionChainState.producedEvents.Count;
                actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, damageContext));
                tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                    gameState,
                    actionChainState,
                    eventId,
                    queuedDamageProducedEventsStartIndex);
                continue;
            }

            actionChainState.localState.Remove(LocalStateKeyC014RippleRemainingDamages);
            actionChainState.localState.Remove(LocalStateKeyC014RippleQueuedDamageContexts);
            actionChainState.localState.Remove(LocalStateKeyC014RippleSequence);
            actionChainState.localState.Remove(LocalStateKeyC014RippleBeforeDefenseResponsesConsumed);
            actionChainState.localState.Remove(LocalStateKeyC014RippleDamageBonus);
            actionChainState.localState.Remove(LocalStateKeyC014RippleForcePenetrate);
            if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest)
            {
                return false;
            }

            var pendingSelfDamage = 0;
            if (actionChainState.localState.TryGetValue(LocalStateKeyC014RipplePendingSelfDamage, out var selfDamageRaw))
            {
                int.TryParse(selfDamageRaw, out pendingSelfDamage);
            }

            actionChainState.localState.Remove(LocalStateKeyC014RipplePendingSelfDamage);
            if (pendingSelfDamage <= 0 ||
                !gameState.characterInstances.TryGetValue(useSkillActionRequest.characterInstanceId, out var sourceCharacterInstance) ||
                !sourceCharacterInstance.isAlive ||
                !sourceCharacterInstance.isInPlay)
            {
                return false;
            }

            var selfDamageContext = new DamageContext
            {
                damageContextId = new DamageContextId(eventId),
                sourcePlayerId = useSkillActionRequest.actorPlayerId,
                sourceCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                targetPlayerId = useSkillActionRequest.actorPlayerId,
                targetCharacterInstanceId = sourceCharacterInstance.characterInstanceId,
                baseDamageValue = pendingSelfDamage,
                damageType = DamageTypeKeyDirect,
            };
            var producedEventsStartIndex = actionChainState.producedEvents.Count;
            queueOrResolveStagedDamage(
                gameState,
                actionChainState,
                selfDamageContext,
                eventId,
                producedEventsStartIndex);
            return false;
        }

        if (gameState.matchState != GameState.MatchState.running)
        {
            clearC014RippleSequence(actionChainState);
        }

        return false;
    }

    private static List<string> collectC014DestructionAmountChoiceKeys(CharacterInstance sourceCharacterInstance)
    {
        var choiceKeys = new List<string>();
        var markerCount = MarkerRuntime.getMarkerCount(sourceCharacterInstance, MarkerTypeDestruction);
        for (var amount = 0; amount <= markerCount; amount++)
        {
            choiceKeys.Add(ChoiceKeyC014DestructionPrefix + amount);
        }

        return choiceKeys;
    }

    private static int parseC014DestructionChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(ChoiceKeyC014DestructionPrefix, StringComparison.Ordinal) ||
            !int.TryParse(choiceKey.Substring(ChoiceKeyC014DestructionPrefix.Length), out var amount))
        {
            throw new InvalidOperationException("C014 destruction choice requires destruction:{amount} format.");
        }

        return amount;
    }

    private static List<(CharacterInstanceId targetCharacterInstanceId, int damageValue)> loadC014RippleRemainingDamages(
        ActionChainState actionChainState)
    {
        var result = new List<(CharacterInstanceId targetCharacterInstanceId, int damageValue)>();
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC014RippleRemainingDamages, out var serialized) ||
            string.IsNullOrWhiteSpace(serialized))
        {
            return result;
        }

        foreach (var entry in serialized.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split(':');
            if (parts.Length != 2 ||
                !long.TryParse(parts[0], out var characterNumericId) ||
                !int.TryParse(parts[1], out var damageValue))
            {
                throw new InvalidOperationException("C014:4 remaining damage state is malformed.");
            }

            result.Add((new CharacterInstanceId(characterNumericId), damageValue));
        }

        return result;
    }

    private static void saveC014RippleRemainingDamages(
        ActionChainState actionChainState,
        IReadOnlyList<(CharacterInstanceId targetCharacterInstanceId, int damageValue)> remainingDamages)
    {
        actionChainState.localState[LocalStateKeyC014RippleRemainingDamages] =
            string.Join(
                ",",
                remainingDamages.Select(entry => $"{entry.targetCharacterInstanceId.Value}:{entry.damageValue}"));
    }

    private void continueC021SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (string.Equals(continuationKey, ContinuationKeyC021FuneralChoice, StringComparison.Ordinal))
        {
            continueC021FuneralChoice(gameState, actionChainState, request);
            return;
        }

        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState) ||
            !gameState.characterInstances.TryGetValue(useSkillActionRequest.characterInstanceId, out var sourceCharacterInstance))
        {
            throw new InvalidOperationException("C021 skill continuation requires a valid UseSkillActionRequest root.");
        }

        if (string.Equals(continuationKey, ContinuationKeyC021CurryDiscard, StringComparison.Ordinal))
        {
            discardSelectedHandCard(gameState, actionChainState, actorPlayerState, request, "C021:1");
            appendHealOnCharacter(actionChainState, request.requestId, sourceCharacterInstance, healAmount: 1);
            appendDrawOneCardEffectEventsForPlayer(
                gameState,
                actionChainState,
                actorPlayerState,
                request.requestId);
            actionChainState.pendingContinuationKey = null;
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC021BlackKeyDiscard, StringComparison.Ordinal))
        {
            discardSelectedHandCard(gameState, actionChainState, actorPlayerState, request, "C021:2");
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC021TargetPlayer,
                ContextKeyC021BlackKeyTarget,
                ContinuationKeyC021BlackKeyTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC021BlackKeyTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C021:2 requires selected player to be an alive opponent.");
            }

            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                3,
                DamageTypeKeySpell);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC021SeventhTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C021:4 requires selected player to be an alive opponent.");
            }

            applyStatusFromCharacterSkill(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                useSkillActionRequest.characterInstanceId,
                StatusKeyShackle,
                targetPlayerId,
                targetCharacterInstance.characterInstanceId);

            var remainingDiscards = string.Equals(targetCharacterInstance.definitionId, CharacterDefinitionIdC011, StringComparison.Ordinal)
                ? 2
                : 1;
            actionChainState.localState[LocalStateKeyC021SeventhTargetPlayerId] = targetPlayerId.Value.ToString();
            actionChainState.localState[LocalStateKeyC021SeventhRemainingDiscards] = remainingDiscards.ToString();
            openNextC021SeventhDiscardIfNeeded(gameState, actionChainState, request.requestId);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC021SeventhDiscard, StringComparison.Ordinal))
        {
            if (!actionChainState.localState.TryGetValue(LocalStateKeyC021SeventhTargetPlayerId, out var targetPlayerRaw) ||
                !long.TryParse(targetPlayerRaw, out var targetPlayerNumericId) ||
                !gameState.players.TryGetValue(new PlayerId(targetPlayerNumericId), out var targetPlayerState))
            {
                clearC021SeventhState(actionChainState);
                throw new InvalidOperationException("C021:4 discard continuation requires saved target player.");
            }

            discardSelectedHandCard(gameState, actionChainState, targetPlayerState, request, "C021:4");
            var remainingDiscards = getC021SeventhRemainingDiscards(actionChainState) - 1;
            actionChainState.localState[LocalStateKeyC021SeventhRemainingDiscards] = remainingDiscards.ToString();
            openNextC021SeventhDiscardIfNeeded(gameState, actionChainState, request.requestId);
            return;
        }

        throw new InvalidOperationException("C021 skill continuation key is unsupported.");
    }

    private void openNextC021SeventhDiscardIfNeeded(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC021SeventhTargetPlayerId, out var targetPlayerRaw) ||
            !long.TryParse(targetPlayerRaw, out var targetPlayerNumericId))
        {
            clearC021SeventhState(actionChainState);
            actionChainState.pendingContinuationKey = null;
            return;
        }

        var targetPlayerId = new PlayerId(targetPlayerNumericId);
        if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState))
        {
            clearC021SeventhState(actionChainState);
            actionChainState.pendingContinuationKey = null;
            return;
        }

        var remainingDiscards = getC021SeventhRemainingDiscards(actionChainState);
        if (remainingDiscards <= 0)
        {
            clearC021SeventhState(actionChainState);
            actionChainState.pendingContinuationKey = null;
            return;
        }

        var choiceKeys = collectPlayerHandDiscardChoiceKeys(gameState, targetPlayerState);
        if (choiceKeys.Count == 0)
        {
            clearC021SeventhState(actionChainState);
            actionChainState.pendingContinuationKey = null;
            return;
        }

        openCharacterSkillChoiceInput(
            gameState,
            actionChainState,
            eventId,
            targetPlayerId,
            InputTypeKeyC021DiscardCard,
            ContextKeyC021SeventhDiscard,
            ContinuationKeyC021SeventhDiscard,
            choiceKeys);
    }

    private static int getC021SeventhRemainingDiscards(ActionChainState actionChainState)
    {
        return actionChainState.localState.TryGetValue(LocalStateKeyC021SeventhRemainingDiscards, out var remainingRaw) &&
               int.TryParse(remainingRaw, out var remainingDiscards)
            ? remainingDiscards
            : 0;
    }

    private static void clearC021SeventhState(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(LocalStateKeyC021SeventhTargetPlayerId);
        actionChainState.localState.Remove(LocalStateKeyC021SeventhRemainingDiscards);
    }

    private CardInstance discardSelectedHandCard(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        GameState.PlayerState cardOwnerPlayerState,
        SubmitInputChoiceActionRequest request,
        string sourceLabel)
    {
        var selectedCardInstanceId = parseCardChoiceKey(request.choiceKey, ChoiceKeyDiscardCardPrefix);
        if (request.actorPlayerId != cardOwnerPlayerState.playerId ||
            !gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance) ||
            selectedCardInstance.ownerPlayerId != cardOwnerPlayerState.playerId ||
            selectedCardInstance.zoneId != cardOwnerPlayerState.handZoneId)
        {
            throw new InvalidOperationException($"{sourceLabel} requires selected discard card to remain in the required player's hand.");
        }

        actionChainState.producedEvents.Add(zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            cardOwnerPlayerState.discardZoneId,
            CardMoveReason.discard,
            actionChainState.actionChainId,
            request.requestId));
        return selectedCardInstance;
    }

    private static bool isC021SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC021CurryDiscard, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC021BlackKeyDiscard, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC021BlackKeyTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC021FuneralChoice, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC021SeventhTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC021SeventhDiscard, StringComparison.Ordinal);
    }

    private void continueC022SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request,
        InputContextState inputContextState,
        string continuationKey)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState) ||
            !gameState.characterInstances.TryGetValue(useSkillActionRequest.characterInstanceId, out var sourceCharacterInstance))
        {
            throw new InvalidOperationException("C022 skill continuation requires a valid UseSkillActionRequest root.");
        }

        if (string.Equals(continuationKey, ContinuationKeyC022ReturnToVoidDefenseCard, StringComparison.Ordinal))
        {
            var selectedCardInstanceId = parseCardChoiceKey(request.choiceKey, ChoiceKeyC022DefenseCardPrefix);
            if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var defenseCardInstance) ||
                !gameState.players.TryGetValue(defenseCardInstance.ownerPlayerId, out var defenseOwnerPlayerState) ||
                defenseOwnerPlayerState.teamId == actorPlayerState.teamId ||
                defenseCardInstance.zoneId != defenseOwnerPlayerState.fieldZoneId ||
                !defenseCardInstance.isDefensePlacedOnField ||
                !tryFindAliveOpponentTeammate(gameState, actorPlayerState, defenseOwnerPlayerState, out var allyTargetCharacterInstance))
            {
                throw new InvalidOperationException("C022:2 requires selected card to be an opponent defense-placed field card with a valid ally target.");
            }

            var summonCost = TreasureDefinitionRepository
                .resolveByDefinitionId(defenseCardInstance.definitionId)
                .summonSigilCost ?? 0;
            var damageValue = Math.Max(0, 4 - summonCost);
            var movedEvent = zoneMovementService.moveCard(
                gameState,
                defenseCardInstance,
                defenseOwnerPlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                request.requestId);
            actionChainState.producedEvents.Add(movedEvent);
            defenseCardInstance.isDefensePlacedOnField = false;
            actionChainState.pendingContinuationKey = null;
            if (damageValue <= 0)
            {
                actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
                actionChainState.isCompleted = true;
                return;
            }

            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                allyTargetCharacterInstance.characterInstanceId,
                damageValue,
                DamageTypeKeySpell);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC022GrandGuignolTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C022:3 requires selected player to be an alive opponent.");
            }

            if (!actionChainState.localState.TryGetValue(LocalStateKeyC022GrandGuignolDamageValue, out var rawDamageValue) ||
                !int.TryParse(rawDamageValue, out var damageValue))
            {
                throw new InvalidOperationException("C022:3 requires saved damage value.");
            }

            actionChainState.localState.Remove(LocalStateKeyC022GrandGuignolDamageValue);
            if (string.Equals(targetCharacterInstance.definitionId, CharacterDefinitionIdC017, StringComparison.Ordinal))
            {
                damageValue += 1;
            }

            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                damageValue,
                DamageTypeKeyPhysical);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC022DollWarSelectX, StringComparison.Ordinal))
        {
            var selectedX = parseC022DollChoiceKey(request.choiceKey);
            var markerCount = MarkerRuntime.getMarkerCount(sourceCharacterInstance, MarkerTypeDoll);
            if (selectedX < 0 || selectedX > markerCount)
            {
                throw new InvalidOperationException("C022:4 selected doll marker amount exceeds current marker count.");
            }

            if (selectedX > 0)
            {
                actionChainState.producedEvents.Add(MarkerRuntime.removeMarker(
                    sourceCharacterInstance,
                    MarkerTypeDoll,
                    selectedX,
                    actionChainState.actionChainId,
                    request.requestId));
            }

            actionChainState.localState[LocalStateKeyC022DollWarX] = selectedX.ToString();
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC022TargetPlayer,
                ContextKeyC022DollWarTarget,
                ContinuationKeyC022DollWarTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC022DollWarTarget, StringComparison.Ordinal))
        {
            var selectedX = getSavedC022DollWarX(actionChainState);
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out _))
            {
                throw new InvalidOperationException("C022:4 requires selected player to be an alive opponent.");
            }

            actionChainState.localState[LocalStateKeyC022DollWarTargetPlayerId] = targetPlayerId.Value.ToString();
            if (selectedX == 0)
            {
                clearC022DollWarState(actionChainState);
                return;
            }

            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                request.requestId,
                targetPlayerId,
                InputTypeKeyC022DollWarOptions,
                ContextKeyC022DollWarTargetOptions,
                ContinuationKeyC022DollWarTargetOptions,
                new List<string>
                {
                    ChoiceKeyC022DollWarAliceDraw,
                    ChoiceKeyC022DollWarTargetDiscard,
                    ChoiceKeyC022DollWarDirectDamage,
                    ChoiceKeyC022DollWarReduceLeyline,
                });
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC022DollWarTargetOptions, StringComparison.Ordinal))
        {
            continueC022DollWarOptions(gameState, actionChainState, request, inputContextState, useSkillActionRequest);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC022DollWarTargetDiscard, StringComparison.Ordinal))
        {
            continueC022DollWarTargetDiscard(gameState, actionChainState, request, inputContextState, useSkillActionRequest);
            return;
        }

        throw new InvalidOperationException("C022 skill continuation key is unsupported.");
    }

    private void continueC022DollWarOptions(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request,
        InputContextState inputContextState,
        UseSkillActionRequest useSkillActionRequest)
    {
        var selectedX = getSavedC022DollWarX(actionChainState);
        var targetPlayerId = getSavedC022DollWarTargetPlayerId(actionChainState);
        if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("C022:4 requires saved target and actor players.");
        }

        var selectedChoiceKeys = request.choiceKeys.Count > 0
            ? request.choiceKeys
            : new List<string> { request.choiceKey };
        if (selectedChoiceKeys.Count != selectedX ||
            selectedChoiceKeys.Distinct(StringComparer.Ordinal).Count() != selectedChoiceKeys.Count ||
            selectedChoiceKeys.Any(choiceKey => !inputContextState.choiceKeys.Contains(choiceKey)))
        {
            throw new InvalidOperationException("C022:4 target option continuation requires exactly X unique choices.");
        }

        if (selectedChoiceKeys.Contains(ChoiceKeyC022DollWarAliceDraw))
        {
            drawCardsForPlayerReturningCards(gameState, actionChainState, actorPlayerState, selectedX, request.requestId);
        }

        var hasDiscardOption = selectedChoiceKeys.Contains(ChoiceKeyC022DollWarTargetDiscard);
        var hasDirectDamageOption = selectedChoiceKeys.Contains(ChoiceKeyC022DollWarDirectDamage);
        var hasLeylineOption = selectedChoiceKeys.Contains(ChoiceKeyC022DollWarReduceLeyline);
        if (hasDiscardOption)
        {
            if (!gameState.zones.TryGetValue(targetPlayerState.handZoneId, out var targetHandZoneState))
            {
                throw new InvalidOperationException("C022:4 target discard requires target hand zone.");
            }

            if (targetHandZoneState.cardInstanceIds.Count <= selectedX)
            {
                foreach (var cardInstanceId in targetHandZoneState.cardInstanceIds.ToList())
                {
                    var cardInstance = gameState.cardInstances[cardInstanceId];
                    actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                        gameState,
                        cardInstance,
                        targetPlayerState.discardZoneId,
                        CardMoveReason.discard,
                        actionChainState.actionChainId,
                        request.requestId));
                }
            }
            else
            {
                actionChainState.localState[LocalStateKeyC022DollWarPendingLeylineReduction] =
                    hasLeylineOption ? "true" : "false";
                actionChainState.localState[LocalStateKeyC022DollWarPendingDirectDamage] =
                    hasDirectDamageOption ? "true" : "false";
                openCharacterSkillChoiceInput(
                    gameState,
                    actionChainState,
                    request.requestId,
                    targetPlayerId,
                    InputTypeKeyC022DiscardCard,
                    ContextKeyC022DollWarTargetDiscard,
                    ContinuationKeyC022DollWarTargetDiscard,
                    collectPlayerHandDiscardChoiceKeys(gameState, targetPlayerState));
                return;
            }
        }

        resolveC022DollWarTail(gameState, actionChainState, request.requestId, useSkillActionRequest, targetPlayerState, selectedX, hasDirectDamageOption, hasLeylineOption);
    }

    private void continueC022DollWarTargetDiscard(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request,
        InputContextState inputContextState,
        UseSkillActionRequest useSkillActionRequest)
    {
        var selectedX = getSavedC022DollWarX(actionChainState);
        var targetPlayerId = getSavedC022DollWarTargetPlayerId(actionChainState);
        if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
            !gameState.zones.TryGetValue(targetPlayerState.handZoneId, out var targetHandZoneState))
        {
            throw new InvalidOperationException("C022:4 target discard requires saved target player.");
        }

        var selectedChoiceKeys = request.choiceKeys.Count > 0
            ? request.choiceKeys
            : new List<string> { request.choiceKey };
        if (selectedChoiceKeys.Count != selectedX ||
            selectedChoiceKeys.Distinct(StringComparer.Ordinal).Count() != selectedChoiceKeys.Count ||
            selectedChoiceKeys.Any(choiceKey => !inputContextState.choiceKeys.Contains(choiceKey)))
        {
            throw new InvalidOperationException("C022:4 target discard continuation requires exactly X unique hand-card choices.");
        }

        foreach (var choiceKey in selectedChoiceKeys)
        {
            var selectedCardInstanceId = parseCardChoiceKey(choiceKey, ChoiceKeyDiscardCardPrefix);
            if (!targetHandZoneState.cardInstanceIds.Contains(selectedCardInstanceId))
            {
                throw new InvalidOperationException("C022:4 target discard requires selected cards to remain in target hand.");
            }

            var cardInstance = gameState.cardInstances[selectedCardInstanceId];
            actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                gameState,
                cardInstance,
                targetPlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                request.requestId));
        }

        var hasDirectDamageOption =
            actionChainState.localState.TryGetValue(LocalStateKeyC022DollWarPendingDirectDamage, out var directRaw) &&
            string.Equals(directRaw, "true", StringComparison.Ordinal);
        var hasLeylineOption =
            actionChainState.localState.TryGetValue(LocalStateKeyC022DollWarPendingLeylineReduction, out var leylineRaw) &&
            string.Equals(leylineRaw, "true", StringComparison.Ordinal);
        resolveC022DollWarTail(gameState, actionChainState, request.requestId, useSkillActionRequest, targetPlayerState, selectedX, hasDirectDamageOption, hasLeylineOption);
    }

    private void resolveC022DollWarTail(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        UseSkillActionRequest useSkillActionRequest,
        GameState.PlayerState targetPlayerState,
        int selectedX,
        bool hasDirectDamageOption,
        bool hasLeylineOption)
    {
        if (hasLeylineOption)
        {
            actionChainState.localState[LocalStateKeyC022DollWarPendingLeylineReduction] = selectedX.ToString();
        }
        else
        {
            actionChainState.localState.Remove(LocalStateKeyC022DollWarPendingLeylineReduction);
        }

        actionChainState.localState.Remove(LocalStateKeyC022DollWarPendingDirectDamage);

        if (hasDirectDamageOption &&
            tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerState.playerId, out var targetCharacterInstance))
        {
            appendDirectDamageFromCharacter(
                gameState,
                actionChainState,
                requestId,
                useSkillActionRequest.actorPlayerId,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                selectedX);
            if (hasLeylineOption)
            {
                tryResumeC022DollWarLeylineReduction(gameState, actionChainState);
            }
            else
            {
                clearC022DollWarState(actionChainState, clearPendingContinuation: gameState.currentInputContext is null && gameState.currentResponseWindow is null);
            }

            return;
        }

        if (hasLeylineOption)
        {
            tryResumeC022DollWarLeylineReduction(gameState, actionChainState);
        }
        else
        {
            clearC022DollWarState(actionChainState);
        }
    }

    private void tryResumeC022DollWarLeylineReduction(GameState.GameState gameState, ActionChainState actionChainState)
    {
        if (gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC022DollWarPendingLeylineReduction, out var rawAmount) ||
            !int.TryParse(rawAmount, out var amount) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC022DollWarTargetPlayerId, out var rawTargetPlayerId) ||
            !long.TryParse(rawTargetPlayerId, out var targetPlayerNumericId))
        {
            return;
        }

        var targetPlayerId = new PlayerId(targetPlayerNumericId);
        if (gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) &&
            gameState.teams.TryGetValue(targetPlayerState.teamId, out var targetTeamState))
        {
            targetTeamState.leyline = Math.Max(0, targetTeamState.leyline - amount);
        }

        clearC022DollWarState(actionChainState);
    }

    private static bool isC022SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC022ReturnToVoidDefenseCard, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC022GrandGuignolTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC022DollWarSelectX, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC022DollWarTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC022DollWarTargetOptions, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC022DollWarTargetDiscard, StringComparison.Ordinal);
    }

    private static List<string> collectC022DefensePlacedCardChoiceKeys(
        GameState.GameState gameState,
        GameState.PlayerState actorPlayerState)
    {
        var choiceKeys = new List<string>();
        foreach (var opponentPlayerState in gameState.players.Values.OrderBy(player => player.playerId.Value))
        {
            if (opponentPlayerState.teamId == actorPlayerState.teamId ||
                !gameState.zones.TryGetValue(opponentPlayerState.fieldZoneId, out var fieldZoneState) ||
                !tryFindAliveOpponentTeammate(gameState, actorPlayerState, opponentPlayerState, out _))
            {
                continue;
            }

            foreach (var cardInstanceId in fieldZoneState.cardInstanceIds)
            {
                if (!gameState.cardInstances.TryGetValue(cardInstanceId, out var cardInstance) ||
                    !cardInstance.isDefensePlacedOnField)
                {
                    continue;
                }

                choiceKeys.Add(ChoiceKeyC022DefenseCardPrefix + cardInstanceId.Value);
            }
        }

        return choiceKeys;
    }

    private static bool tryFindAliveOpponentTeammate(
        GameState.GameState gameState,
        GameState.PlayerState actorPlayerState,
        GameState.PlayerState defenseOwnerPlayerState,
        out CharacterInstance characterInstance)
    {
        characterInstance = null!;
        foreach (var playerState in gameState.players.Values.OrderBy(player => player.playerId.Value))
        {
            if (playerState.playerId == defenseOwnerPlayerState.playerId ||
                playerState.teamId != defenseOwnerPlayerState.teamId ||
                playerState.teamId == actorPlayerState.teamId)
            {
                continue;
            }

            return tryFindAliveInPlayCharacterInstanceByOwner(gameState, playerState.playerId, out characterInstance);
        }

        return false;
    }

    private static List<string> collectC022DollAmountChoiceKeys(CharacterInstance sourceCharacterInstance)
    {
        var choiceKeys = new List<string>();
        var markerCount = MarkerRuntime.getMarkerCount(sourceCharacterInstance, MarkerTypeDoll);
        for (var amount = 0; amount <= markerCount; amount++)
        {
            choiceKeys.Add(ChoiceKeyC022DollPrefix + amount);
        }

        return choiceKeys;
    }

    private static int parseC022DollChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(ChoiceKeyC022DollPrefix, StringComparison.Ordinal) ||
            !int.TryParse(choiceKey[ChoiceKeyC022DollPrefix.Length..], out var amount))
        {
            throw new InvalidOperationException("C022 doll choice requires doll:{amount} format.");
        }

        return amount;
    }

    private static int getSavedC022DollWarX(ActionChainState actionChainState)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC022DollWarX, out var rawAmount) ||
            !int.TryParse(rawAmount, out var amount))
        {
            throw new InvalidOperationException("C022:4 requires saved X.");
        }

        return amount;
    }

    private static PlayerId getSavedC022DollWarTargetPlayerId(ActionChainState actionChainState)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC022DollWarTargetPlayerId, out var rawPlayerId) ||
            !long.TryParse(rawPlayerId, out var playerNumericId))
        {
            throw new InvalidOperationException("C022:4 requires saved target player.");
        }

        return new PlayerId(playerNumericId);
    }

    private static void clearC022DollWarState(ActionChainState actionChainState, bool clearPendingContinuation = true)
    {
        actionChainState.localState.Remove(LocalStateKeyC022DollWarX);
        actionChainState.localState.Remove(LocalStateKeyC022DollWarTargetPlayerId);
        actionChainState.localState.Remove(LocalStateKeyC022DollWarPendingLeylineReduction);
        actionChainState.localState.Remove(LocalStateKeyC022DollWarPendingDirectDamage);
        if (clearPendingContinuation)
        {
            actionChainState.pendingContinuationKey = null;
        }
    }

    private List<CardInstance> drawCardsForPlayerReturningCards(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        GameState.PlayerState playerState,
        int count,
        long requestId)
    {
        var drawnCards = new List<CardInstance>();
        for (var drawIndex = 0; drawIndex < count; drawIndex++)
        {
            if (!gameState.zones.TryGetValue(playerState.deckZoneId, out var deckZoneState) ||
                !gameState.zones.TryGetValue(playerState.discardZoneId, out var discardZoneState) ||
                !gameState.zones.ContainsKey(playerState.handZoneId))
            {
                throw new InvalidOperationException("Draw effect requires player deck, discard, and hand zones.");
            }

            if (deckZoneState.cardInstanceIds.Count == 0 && discardZoneState.cardInstanceIds.Count > 0)
            {
                var discardCardIdsInCurrentOrder = PlayerDeckRuntime.createShuffledCardInstanceIds(discardZoneState.cardInstanceIds);
                foreach (var cardInstanceId in discardCardIdsInCurrentOrder)
                {
                    var discardedCardInstance = gameState.cardInstances[cardInstanceId];
                    actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                        gameState,
                        discardedCardInstance,
                        playerState.deckZoneId,
                        CardMoveReason.returnToSource,
                        actionChainState.actionChainId,
                        requestId));
                }
            }

            if (deckZoneState.cardInstanceIds.Count <= 0)
            {
                break;
            }

            var topCardInstance = gameState.cardInstances[deckZoneState.cardInstanceIds[0]];
            actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                gameState,
                topCardInstance,
                playerState.handZoneId,
                CardMoveReason.draw,
                actionChainState.actionChainId,
                requestId));
            drawnCards.Add(topCardInstance);
        }

        return drawnCards;
    }

    private void continueC031SkillInput(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request,
        string continuationKey)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState) ||
            !gameState.characterInstances.TryGetValue(useSkillActionRequest.characterInstanceId, out var sourceCharacterInstance))
        {
            throw new InvalidOperationException("C031 skill continuation requires a valid UseSkillActionRequest root.");
        }

        if (string.Equals(continuationKey, ContinuationKeyC031FarPunchSelectLeyline, StringComparison.Ordinal))
        {
            var consumedLeyline = parseLeylineChoiceKey(request.choiceKey);
            var actorTeamState = gameState.teams[actorPlayerState.teamId];
            if (consumedLeyline > actorTeamState.leyline)
            {
                throw new InvalidOperationException("C031:2 requires selected leyline cost to be available.");
            }

            actorTeamState.leyline -= consumedLeyline;
            actionChainState.localState[LocalStateKeyC031FarPunchConsumedLeyline] = consumedLeyline.ToString();
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC031TargetPlayer,
                ContextKeyC031FarPunchTarget,
                ContinuationKeyC031FarPunchTarget,
                collectOpponentPlayerChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC031FarPunchTarget, StringComparison.Ordinal))
        {
            var consumedLeyline = consumeSavedC031Leyline(actionChainState);
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                targetPlayerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, targetPlayerId, out var targetCharacterInstance))
            {
                throw new InvalidOperationException("C031:2 requires selected player to be an alive opponent.");
            }

            actionChainState.pendingContinuationKey = null;
            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstance.characterInstanceId,
                3 + consumedLeyline,
                DamageTypeKeyPhysical);
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC031AlleyDeclare, StringComparison.Ordinal))
        {
            var declaredDefinitionId = parseDeclareCardNameChoiceKey(request.choiceKey);
            actionChainState.localState[LocalStateKeyC031AlleyDeclaredDefinitionId] = declaredDefinitionId;
            openCharacterSkillChoiceInput(
                gameState,
                actionChainState,
                request.requestId,
                useSkillActionRequest.actorPlayerId,
                InputTypeKeyC031TargetPlayer,
                ContextKeyC031AlleyTarget,
                ContinuationKeyC031AlleyTarget,
                collectOpponentPlayerWithNonEmptyDeckChoiceKeys(gameState, actorPlayerState));
            return;
        }

        if (string.Equals(continuationKey, ContinuationKeyC031AlleyTarget, StringComparison.Ordinal))
        {
            var targetPlayerId = parsePlayerChoiceKey(request.choiceKey);
            var declaredDefinitionId = consumeSavedC031DeclaredDefinitionId(actionChainState);
            var revealedCardInstance = getOpponentDeckTopCardOrThrow(gameState, actorPlayerState, targetPlayerId, "C031:3");
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState))
            {
                throw new InvalidOperationException("C031:3 requires target player to exist.");
            }

            actionChainState.producedEvents.Add(new CardRevealedEvent
            {
                eventId = request.requestId,
                eventTypeKey = "cardRevealed",
                sourceActionChainId = actionChainState.actionChainId,
                cardInstanceId = revealedCardInstance.cardInstanceId,
                ownerPlayerId = revealedCardInstance.ownerPlayerId,
                definitionId = revealedCardInstance.definitionId,
                revealReasonKey = RevealReasonKeyC031AlleyAlliance,
            });

            actionChainState.pendingContinuationKey = null;
            if (string.Equals(revealedCardInstance.definitionId, declaredDefinitionId, StringComparison.Ordinal))
            {
                var actorTeamState = gameState.teams[actorPlayerState.teamId];
                actorTeamState.leyline = Math.Min(5, actorTeamState.leyline + 1);
                return;
            }

            appendDrawOneCardEffectEventsForPlayer(
                gameState,
                actionChainState,
                actorPlayerState,
                request.requestId);
            appendDrawOneCardEffectEventsForPlayer(
                gameState,
                actionChainState,
                targetPlayerState,
                request.requestId);
            return;
        }

        throw new InvalidOperationException("C031 skill continuation key is unsupported.");
    }

    private static bool isC031SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC031FarPunchSelectLeyline, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC031FarPunchTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC031AlleyDeclare, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC031AlleyTarget, StringComparison.Ordinal);
    }

    private static List<string> collectLeylineAmountChoiceKeys(
        GameState.GameState gameState,
        GameState.PlayerState actorPlayerState)
    {
        var leyline = gameState.teams[actorPlayerState.teamId].leyline;
        var choiceKeys = new List<string>();
        for (var amount = 0; amount <= leyline; amount++)
        {
            choiceKeys.Add(ChoiceKeyLeylinePrefix + amount);
        }

        return choiceKeys;
    }

    private static int parseLeylineChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(ChoiceKeyLeylinePrefix, StringComparison.Ordinal) ||
            !int.TryParse(choiceKey.Substring(ChoiceKeyLeylinePrefix.Length), out var amount) ||
            amount < 0)
        {
            throw new InvalidOperationException("Leyline choice requires leyline:{amount} format.");
        }

        return amount;
    }

    private static int consumeSavedC031Leyline(ActionChainState actionChainState)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC031FarPunchConsumedLeyline, out var rawAmount) ||
            !int.TryParse(rawAmount, out var amount) ||
            amount < 0)
        {
            throw new InvalidOperationException("C031:2 requires saved leyline amount.");
        }

        actionChainState.localState.Remove(LocalStateKeyC031FarPunchConsumedLeyline);
        return amount;
    }

    private static List<string> collectDeclarableCardNameChoiceKeys()
    {
        var choiceKeys = new List<string>();
        foreach (var definitionId in TreasureDefinitionRepository.getDeclarableTreasureDefinitionIds())
        {
            choiceKeys.Add(TreasureOnPlayEffectRuntime.ChoiceKeyDeclareCardNamePrefix + definitionId);
        }

        return choiceKeys;
    }

    private static string parseDeclareCardNameChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(TreasureOnPlayEffectRuntime.ChoiceKeyDeclareCardNamePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Declare card name choice requires declareCardName:{definitionId} format.");
        }

        var definitionId = choiceKey.Substring(TreasureOnPlayEffectRuntime.ChoiceKeyDeclareCardNamePrefix.Length);
        if (!TreasureDefinitionRepository.getDeclarableTreasureDefinitionIds().Contains(definitionId))
        {
            throw new InvalidOperationException("Declare card name choice requires a declarable treasure definitionId.");
        }

        return definitionId;
    }

    private static string consumeSavedC031DeclaredDefinitionId(ActionChainState actionChainState)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC031AlleyDeclaredDefinitionId, out var definitionId) ||
            string.IsNullOrWhiteSpace(definitionId))
        {
            throw new InvalidOperationException("C031:3 requires saved declared definitionId.");
        }

        actionChainState.localState.Remove(LocalStateKeyC031AlleyDeclaredDefinitionId);
        return definitionId;
    }

    private static void applyC031DepletionGarden(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        UseSkillActionRequest useSkillActionRequest)
    {
        for (var index = gameState.statusInstances.Count - 1; index >= 0; index--)
        {
            var statusInstance = gameState.statusInstances[index];
            if (!string.Equals(
                    StatusPolicyTable.normalizeStatusKey(statusInstance.statusKey),
                    StatusKeyBarrier,
                    StringComparison.Ordinal))
            {
                continue;
            }

            actionChainState.producedEvents.Add(new StatusChangedEvent
            {
                eventId = useSkillActionRequest.requestId,
                eventTypeKey = "statusChanged",
                sourceActionChainId = actionChainState.actionChainId,
                statusKey = StatusKeyBarrier,
                targetPlayerId = statusInstance.targetPlayerId,
                targetCharacterInstanceId = statusInstance.targetCharacterInstanceId,
                targetCardInstanceId = statusInstance.targetCardInstanceId,
                isApplied = false,
            });
            gameState.statusInstances.RemoveAt(index);
        }

        foreach (var characterInstance in gameState.characterInstances.Values)
        {
            if (!characterInstance.isAlive ||
                !characterInstance.isInPlay ||
                characterInstance.currentHp <= 1)
            {
                continue;
            }

            var hpBefore = characterInstance.currentHp;
            characterInstance.currentHp = 1;
            actionChainState.producedEvents.Add(new HpChangedEvent
            {
                eventId = useSkillActionRequest.requestId,
                eventTypeKey = "hpChanged",
                sourceActionChainId = actionChainState.actionChainId,
                targetPlayerId = characterInstance.ownerPlayerId,
                targetCharacterInstanceId = characterInstance.characterInstanceId,
                hpBefore = hpBefore,
                hpAfter = 1,
                delta = 1 - hpBefore,
            });
        }
    }

    private static List<string> collectOpponentPlayerWithNonEmptyDeckChoiceKeys(
        GameState.GameState gameState,
        GameState.PlayerState actorPlayerState)
    {
        var choiceKeys = new List<string>();
        foreach (var playerId in getPlayersInSeatOrder(gameState))
        {
            if (!gameState.players.TryGetValue(playerId, out var playerState) ||
                playerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(gameState, playerId, out _) ||
                !gameState.zones.TryGetValue(playerState.deckZoneId, out var deckZoneState) ||
                deckZoneState.cardInstanceIds.Count == 0)
            {
                continue;
            }

            choiceKeys.Add(ChoiceKeyPlayerPrefix + playerId.Value);
        }

        return choiceKeys;
    }

    private static CardInstance getOpponentDeckTopCardOrThrow(
        GameState.GameState gameState,
        GameState.PlayerState actorPlayerState,
        PlayerId targetPlayerId,
        string effectKey)
    {
        if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
            targetPlayerState.teamId == actorPlayerState.teamId ||
            !gameState.zones.TryGetValue(targetPlayerState.deckZoneId, out var targetDeckZoneState) ||
            targetDeckZoneState.cardInstanceIds.Count == 0)
        {
            throw new InvalidOperationException($"{effectKey} requires selected opponent to have a non-empty deck.");
        }

        var topCardInstanceId = targetDeckZoneState.cardInstanceIds[0];
        if (!gameState.cardInstances.TryGetValue(topCardInstanceId, out var topCardInstance))
        {
            throw new InvalidOperationException($"{effectKey} requires target deck top card to exist.");
        }

        return topCardInstance;
    }

    private static CharacterInstance getAliveInPlayCharacterByOwnerOrThrow(
        GameState.GameState gameState,
        PlayerId ownerPlayerId,
        string effectKey)
    {
        if (!tryFindAliveInPlayCharacterInstanceByOwner(gameState, ownerPlayerId, out var characterInstance))
        {
            throw new InvalidOperationException($"{effectKey} requires selected target to control an alive character.");
        }

        return characterInstance;
    }

    private void appendDirectDamageFromCharacter(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CharacterInstanceId sourceCharacterInstanceId,
        CharacterInstanceId targetCharacterInstanceId,
        int baseDamageValue)
    {
        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(requestId),
            sourcePlayerId = sourcePlayerId,
            sourceCharacterInstanceId = sourceCharacterInstanceId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = baseDamageValue,
            damageType = DamageTypeKeyDirect,
        };

        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, damageContext));
    }

    private void tryResumeC013CriminalTakeRevealed(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (!actionChainState.localState.ContainsKey(LocalStateKeyC013CriminalRevealedCardInstanceId) ||
            gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            return;
        }

        if (!actionChainState.localState.TryGetValue(LocalStateKeyC013CriminalTargetPlayerId, out var targetPlayerRaw) ||
            !long.TryParse(targetPlayerRaw, out var targetPlayerNumericId) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC013CriminalRevealedCardInstanceId, out var cardRaw) ||
            !long.TryParse(cardRaw, out var cardNumericId))
        {
            clearC013CriminalRevealedState(actionChainState);
            throw new InvalidOperationException("C013:4 revealed-card state is invalid.");
        }

        var targetPlayerId = new PlayerId(targetPlayerNumericId);
        var revealedCardInstanceId = new CardInstanceId(cardNumericId);
        if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
            !gameState.zones.TryGetValue(targetPlayerState.deckZoneId, out var targetDeckZoneState) ||
            targetDeckZoneState.cardInstanceIds.Count == 0 ||
            targetDeckZoneState.cardInstanceIds[0] != revealedCardInstanceId ||
            !gameState.cardInstances.ContainsKey(revealedCardInstanceId))
        {
            clearC013CriminalRevealedState(actionChainState);
            return;
        }

        openCharacterSkillChoiceInput(
            gameState,
            actionChainState,
            eventId,
            targetPlayerId,
            InputTypeKeyC013TakeRevealed,
            ContextKeyC013CriminalTakeRevealed,
            ContinuationKeyC013CriminalTakeRevealed,
            new List<string> { ChoiceKeyC013KeepRevealed, ChoiceKeyC013TakeRevealed });
    }

    private void continueC013CriminalTakeRevealed(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest request)
    {
        if (!actionChainState.localState.TryGetValue(LocalStateKeyC013CriminalTargetPlayerId, out var targetPlayerRaw) ||
            !long.TryParse(targetPlayerRaw, out var targetPlayerNumericId) ||
            !actionChainState.localState.TryGetValue(LocalStateKeyC013CriminalRevealedCardInstanceId, out var cardRaw) ||
            !long.TryParse(cardRaw, out var cardNumericId))
        {
            clearC013CriminalRevealedState(actionChainState);
            throw new InvalidOperationException("C013:4 take-revealed continuation requires saved revealed-card state.");
        }

        var targetPlayerId = new PlayerId(targetPlayerNumericId);
        var revealedCardInstanceId = new CardInstanceId(cardNumericId);
        if (request.actorPlayerId != targetPlayerId)
        {
            throw new InvalidOperationException("C013:4 take-revealed choice must be submitted by the target player.");
        }

        if (string.Equals(request.choiceKey, ChoiceKeyC013TakeRevealed, StringComparison.Ordinal))
        {
            if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState) ||
                !gameState.zones.TryGetValue(targetPlayerState.deckZoneId, out var targetDeckZoneState) ||
                targetDeckZoneState.cardInstanceIds.Count == 0 ||
                targetDeckZoneState.cardInstanceIds[0] != revealedCardInstanceId ||
                !gameState.cardInstances.TryGetValue(revealedCardInstanceId, out var revealedCardInstance))
            {
                throw new InvalidOperationException("C013:4 take-revealed card must remain on target deck top.");
            }

            actionChainState.producedEvents.Add(zoneMovementService.moveCard(
                gameState,
                revealedCardInstance,
                targetPlayerState.handZoneId,
                CardMoveReason.draw,
                actionChainState.actionChainId,
                request.requestId));
        }

        clearC013CriminalRevealedState(actionChainState);
        actionChainState.pendingContinuationKey = null;
    }

    private static void clearC013CriminalRevealedState(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(LocalStateKeyC013CriminalTargetPlayerId);
        actionChainState.localState.Remove(LocalStateKeyC013CriminalRevealedCardInstanceId);
    }

    private static bool isC011SkillContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyC011MysticEyesTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC011TrueAncestorCombatTarget, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyC011RealityMarbleSelectSummon, StringComparison.Ordinal);
    }

    private void applyCharmStatusToTarget(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        PlayerId applierPlayerId,
        CharacterInstanceId applierCharacterInstanceId,
        PlayerId targetPlayerId,
        CharacterInstanceId targetCharacterInstanceId)
    {
        var appliedStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = StatusKeyCharm,
                applierPlayerId = applierPlayerId,
                applierCharacterInstanceId = applierCharacterInstanceId,
                targetPlayerId = targetPlayerId,
                targetCharacterInstanceId = targetCharacterInstanceId,
                stackCount = 1,
            });
        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = eventId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = appliedStatus.statusKey,
            targetPlayerId = appliedStatus.targetPlayerId,
            targetCharacterInstanceId = appliedStatus.targetCharacterInstanceId,
            isApplied = true,
        });
    }

    private static void applyStatusFromCharacterSkill(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        PlayerId applierPlayerId,
        CharacterInstanceId applierCharacterInstanceId,
        string statusKey,
        PlayerId targetPlayerId,
        CharacterInstanceId targetCharacterInstanceId,
        string? durationTypeKey = null)
    {
        var appliedStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = statusKey,
                applierPlayerId = applierPlayerId,
                applierCharacterInstanceId = applierCharacterInstanceId,
                targetPlayerId = targetPlayerId,
                targetCharacterInstanceId = targetCharacterInstanceId,
                stackCount = 1,
                durationTypeKey = durationTypeKey ?? string.Empty,
            });
        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = eventId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = appliedStatus.statusKey,
            targetPlayerId = appliedStatus.targetPlayerId,
            targetCharacterInstanceId = appliedStatus.targetCharacterInstanceId,
            isApplied = true,
        });
    }

    private void tryApplyC021BlackKeyFreeManaAfterDamage(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !string.Equals(useSkillActionRequest.skillKey, SkillKeyC021_2, StringComparison.Ordinal) ||
            actionChainState.localState.ContainsKey(LocalStateKeyC021BlackKeyFreeManaGranted) ||
            !gameState.players.ContainsKey(useSkillActionRequest.actorPlayerId) ||
            !gameState.characterInstances.TryGetValue(useSkillActionRequest.characterInstanceId, out var sourceCharacterInstance) ||
            !sourceCharacterInstance.isAlive ||
            !sourceCharacterInstance.isInPlay)
        {
            return;
        }

        var appliedStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = StatusRuntime.StatusKeyC021BlackKeyProjectionFreeMana,
                applierPlayerId = useSkillActionRequest.actorPlayerId,
                applierCharacterInstanceId = useSkillActionRequest.characterInstanceId,
                targetPlayerId = useSkillActionRequest.actorPlayerId,
                stackCount = 1,
                durationTypeKey = "turnEnd",
            });
        actionChainState.localState[LocalStateKeyC021BlackKeyFreeManaGranted] = "true";
        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = eventId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = appliedStatus.statusKey,
            targetPlayerId = appliedStatus.targetPlayerId,
            targetCharacterInstanceId = appliedStatus.targetCharacterInstanceId,
            isApplied = true,
        });
    }

    private static void consumeC021BlackKeyFreeManaStatus(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId playerId,
        long eventId)
    {
        var removedStatuses = StatusRuntime.removeStatusesOnPlayer(
            gameState,
            playerId,
            StatusRuntime.StatusKeyC021BlackKeyProjectionFreeMana);
        foreach (var removedStatus in removedStatuses)
        {
            actionChainState.producedEvents.Add(new StatusChangedEvent
            {
                eventId = eventId,
                eventTypeKey = "statusChanged",
                sourceActionChainId = actionChainState.actionChainId,
                statusKey = removedStatus.statusKey,
                targetPlayerId = removedStatus.targetPlayerId,
                targetCharacterInstanceId = removedStatus.targetCharacterInstanceId,
                isApplied = false,
            });
        }
    }

    private void tryApplyC011MysticEyesCharmAfterFullDefense(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        ResponseWindowState responseWindowState,
        DamageContext damageContext,
        long eventId)
    {
        if (!actionChainState.localState.TryGetValue(
                LocalStateKeyC011MysticEyesPendingFullDefenseCharm,
                out var pendingTargetCharacterIdRaw))
        {
            return;
        }

        actionChainState.localState.Remove(LocalStateKeyC011MysticEyesPendingFullDefenseCharm);
        if (!long.TryParse(pendingTargetCharacterIdRaw, out var pendingTargetCharacterNumericId))
        {
            throw new InvalidOperationException("C011:1 full-defense charm marker contains an invalid target character id.");
        }

        var pendingTargetCharacterInstanceId = new CharacterInstanceId(pendingTargetCharacterNumericId);
        if (responseWindowState.pendingDamageTargetCharacterInstanceId != pendingTargetCharacterInstanceId ||
            responseWindowState.pendingDamageDefenseDeclarationKey is null ||
            !tryParseCardDefenseDeclarationKey(
                responseWindowState.pendingDamageDefenseDeclarationKey,
                out _,
                out _) ||
            damageContext.didDealDamage ||
            !responseWindowState.pendingDamageSourcePlayerId.HasValue ||
            !responseWindowState.pendingDamageSourceCharacterInstanceId.HasValue ||
            !gameState.characterInstances.TryGetValue(pendingTargetCharacterInstanceId, out var targetCharacterInstance) ||
            !targetCharacterInstance.isAlive ||
            !targetCharacterInstance.isInPlay)
        {
            return;
        }

        applyCharmStatusToTarget(
            gameState,
            actionChainState,
            eventId,
            responseWindowState.pendingDamageSourcePlayerId.Value,
            responseWindowState.pendingDamageSourceCharacterInstanceId.Value,
            targetCharacterInstance.ownerPlayerId,
            targetCharacterInstance.characterInstanceId);
    }

    private void continueC011RealityMarbleSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            return;
        }

        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest)
        {
            actionChainState.localState.Remove(LocalStateKeyC011RealityMarbleRemainingCount);
            return;
        }

        if (!actionChainState.localState.TryGetValue(
                LocalStateKeyC011RealityMarbleRemainingCount,
                out var remainingCountRaw) ||
            !int.TryParse(remainingCountRaw, out var remainingCount) ||
            remainingCount <= 0)
        {
            actionChainState.localState.Remove(LocalStateKeyC011RealityMarbleRemainingCount);
            return;
        }

        var choiceKeys = collectSummonZoneCardChoiceKeys(gameState);
        if (choiceKeys.Count == 0)
        {
            actionChainState.localState.Remove(LocalStateKeyC011RealityMarbleRemainingCount);
            return;
        }

        openCharacterSkillChoiceInput(
            gameState,
            actionChainState,
            eventId,
            useSkillActionRequest.actorPlayerId,
            InputTypeKeyC011SelectSummon,
            ContextKeyC011RealityMarbleSelectSummon,
            ContinuationKeyC011RealityMarbleSelectSummon,
            choiceKeys);
    }

    private void continueC011RealityMarbleSelectedSummon(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest request)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest ||
            !gameState.players.TryGetValue(useSkillActionRequest.actorPlayerId, out var actorPlayerState) ||
            gameState.publicState is null ||
            !gameState.zones.TryGetValue(gameState.publicState.summonZoneId, out var summonZoneState))
        {
            throw new InvalidOperationException("C011:4 continuation requires summonZone and actor player state.");
        }

        var selectedCardInstanceId = parseCardChoiceKey(request.choiceKey, ChoiceKeySummonZoneCardPrefix);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance) ||
            selectedCardInstance.zoneId != gameState.publicState.summonZoneId ||
            !summonZoneState.cardInstanceIds.Contains(selectedCardInstanceId))
        {
            throw new InvalidOperationException("C011:4 requires selected card to remain in summonZone.");
        }

        var movedEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            actorPlayerState.handZoneId,
            CardMoveReason.returnToSource,
            actionChainState.actionChainId,
            request.requestId);
        actionChainState.producedEvents.Add(movedEvent);
        selectedCardInstance.ownerPlayerId = useSkillActionRequest.actorPlayerId;

        var remainingCount = 0;
        if (actionChainState.localState.TryGetValue(
                LocalStateKeyC011RealityMarbleRemainingCount,
                out var remainingCountRaw) &&
            int.TryParse(remainingCountRaw, out var parsedRemainingCount))
        {
            remainingCount = Math.Max(0, parsedRemainingCount - 1);
        }

        if (remainingCount <= 0)
        {
            actionChainState.localState.Remove(LocalStateKeyC011RealityMarbleRemainingCount);
        }
        else
        {
            actionChainState.localState[LocalStateKeyC011RealityMarbleRemainingCount] = remainingCount.ToString();
        }

        actionChainState.pendingContinuationKey = null;
        treasureArrivalEffectRuntime.tryRefillSummonZoneFromPublicTreasureDeck(
            gameState,
            actionChainState,
            request.requestId);
    }

    private void tryResumeC011RealityMarbleSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (!actionChainState.localState.ContainsKey(LocalStateKeyC011RealityMarbleRemainingCount) ||
            gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            return;
        }

        continueC011RealityMarbleSequence(gameState, actionChainState, eventId);
    }

    private static List<string> collectSummonZoneCardChoiceKeys(GameState.GameState gameState)
    {
        var choiceKeys = new List<string>();
        if (gameState.publicState is null ||
            !gameState.zones.TryGetValue(gameState.publicState.summonZoneId, out var summonZoneState))
        {
            return choiceKeys;
        }

        foreach (var cardInstanceId in summonZoneState.cardInstanceIds)
        {
            choiceKeys.Add(ChoiceKeySummonZoneCardPrefix + cardInstanceId.Value);
        }

        return choiceKeys;
    }

    private void startC008ExcaliburSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        UseSkillActionRequest useSkillActionRequest,
        GameState.PlayerState actorPlayerState)
    {
        var targetCharacterInstanceIds = new List<CharacterInstanceId>();
        foreach (var playerId in getPlayersInSeatOrder(gameState))
        {
            var playerState = gameState.players[playerId];
            if (playerState.teamId == actorPlayerState.teamId ||
                !tryFindAliveInPlayCharacterInstanceByOwner(
                    gameState,
                    playerId,
                    out var targetCharacterInstance))
            {
                continue;
            }

            targetCharacterInstanceIds.Add(targetCharacterInstance.characterInstanceId);
        }

        actionChainState.localState[LocalStateKeyC008ExcaliburSequence] = C008ExcaliburSequenceCollecting;
        actionChainState.localState[LocalStateKeyC008QueuedDamageContexts] = string.Empty;
        saveC008RemainingTargetCharacterIds(actionChainState, targetCharacterInstanceIds);
        continueC008ExcaliburSequence(gameState, actionChainState, useSkillActionRequest.requestId);
    }

    private void tryResumeC008ExcaliburSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (!actionChainState.localState.ContainsKey(LocalStateKeyC008ExcaliburSequence) ||
            gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            return;
        }

        continueC008ExcaliburSequence(gameState, actionChainState, eventId);
    }

    private void continueC008ExcaliburSequence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (actionChainState.rootActionRequest is not UseSkillActionRequest useSkillActionRequest)
        {
            clearC008ExcaliburSequence(actionChainState);
            return;
        }

        if (actionChainState.localState.TryGetValue(
                LocalStateKeyC008ExcaliburSequence,
                out var sequenceState) &&
            string.Equals(sequenceState, C008ExcaliburSequenceFinalizing, StringComparison.Ordinal))
        {
            continueC008ExcaliburFinalDamage(gameState, actionChainState, eventId);
            return;
        }

        while (gameState.matchState == GameState.MatchState.running &&
               gameState.currentInputContext is null &&
               gameState.currentResponseWindow is null &&
               string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            var remainingTargetCharacterIds = loadC008RemainingTargetCharacterIds(actionChainState);
            if (remainingTargetCharacterIds.Count == 0)
            {
                actionChainState.localState[LocalStateKeyC008ExcaliburSequence] = C008ExcaliburSequenceFinalizing;
                continueC008ExcaliburFinalDamage(gameState, actionChainState, eventId);
                return;
            }

            var targetCharacterInstanceId = remainingTargetCharacterIds[0];
            remainingTargetCharacterIds.RemoveAt(0);
            saveC008RemainingTargetCharacterIds(actionChainState, remainingTargetCharacterIds);
            if (!gameState.characterInstances.TryGetValue(targetCharacterInstanceId, out var targetCharacterInstance) ||
                !targetCharacterInstance.isAlive ||
                !targetCharacterInstance.isInPlay)
            {
                continue;
            }

            openDamageResponseWindowWithPendingDamage(
                gameState,
                actionChainState,
                eventId,
                useSkillActionRequest.actorPlayerId,
                null,
                useSkillActionRequest.characterInstanceId,
                targetCharacterInstanceId,
                6,
                DamageTypeKeySpell);
        }
    }

    private static void saveC008RemainingTargetCharacterIds(
        ActionChainState actionChainState,
        List<CharacterInstanceId> characterInstanceIds)
    {
        var numericIds = new List<string>();
        foreach (var characterInstanceId in characterInstanceIds)
        {
            numericIds.Add(characterInstanceId.Value.ToString());
        }

        actionChainState.localState[LocalStateKeyC008RemainingTargetCharacterIds] =
            string.Join(",", numericIds);
    }

    private static List<CharacterInstanceId> loadC008RemainingTargetCharacterIds(ActionChainState actionChainState)
    {
        var result = new List<CharacterInstanceId>();
        if (!actionChainState.localState.TryGetValue(
                LocalStateKeyC008RemainingTargetCharacterIds,
                out var serializedIds) ||
            string.IsNullOrWhiteSpace(serializedIds))
        {
            return result;
        }

        foreach (var segment in serializedIds.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!long.TryParse(segment, out var numericId))
            {
                throw new InvalidOperationException("C008:4 continuation contains an invalid target character id.");
            }

            result.Add(new CharacterInstanceId(numericId));
        }

        return result;
    }

    private void queueOrResolveStagedDamage(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        DamageContext damageContext,
        long eventId,
        int producedEventsStartIndex)
    {
        if (shouldQueueC008ExcaliburDamage(actionChainState))
        {
            enqueueC008QueuedDamageContext(actionChainState, damageContext);
            actionChainState.pendingContinuationKey = null;
            tryResumeC008ExcaliburSequence(gameState, actionChainState, eventId);
            return;
        }

        if (shouldQueueC014RippleDamage(actionChainState))
        {
            enqueueC014QueuedDamageContext(actionChainState, damageContext);
            actionChainState.pendingContinuationKey = null;
            tryResumeC014RippleSequence(gameState, actionChainState, eventId);
            return;
        }

        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, damageContext));
        tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
            gameState,
            actionChainState,
            eventId,
            producedEventsStartIndex);
        tryResumeCharacterSkillSequences(gameState, actionChainState, eventId);
    }

    private static bool shouldQueueC008ExcaliburDamage(ActionChainState actionChainState)
    {
        return actionChainState.localState.TryGetValue(
                   LocalStateKeyC008ExcaliburSequence,
                   out var sequenceState) &&
               string.Equals(sequenceState, C008ExcaliburSequenceCollecting, StringComparison.Ordinal);
    }

    private static bool shouldQueueC014RippleDamage(ActionChainState actionChainState)
    {
        return actionChainState.localState.TryGetValue(
                   LocalStateKeyC014RippleSequence,
                   out var sequenceState) &&
               string.Equals(sequenceState, C014RippleSequenceCollecting, StringComparison.Ordinal);
    }

    private void continueC008ExcaliburFinalDamage(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        while (gameState.matchState == GameState.MatchState.running &&
               gameState.currentInputContext is null &&
               gameState.currentResponseWindow is null &&
               string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            var queuedDamageContexts = loadC008QueuedDamageContexts(actionChainState);
            if (queuedDamageContexts.Count == 0)
            {
                clearC008ExcaliburSequence(actionChainState);
                return;
            }

            var damageContext = queuedDamageContexts[0];
            queuedDamageContexts.RemoveAt(0);
            saveC008QueuedDamageContexts(actionChainState, queuedDamageContexts);

            if (!damageContext.targetCharacterInstanceId.HasValue ||
                !gameState.characterInstances.TryGetValue(
                    damageContext.targetCharacterInstanceId.Value,
                    out var targetCharacterInstance) ||
                !targetCharacterInstance.isAlive ||
                !targetCharacterInstance.isInPlay)
            {
                continue;
            }

            var producedEventsStartIndex = actionChainState.producedEvents.Count;
            actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, damageContext));
            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                eventId,
                producedEventsStartIndex);
        }

        if (gameState.matchState != GameState.MatchState.running)
        {
            clearC008ExcaliburSequence(actionChainState);
        }
    }

    private static void enqueueC008QueuedDamageContext(
        ActionChainState actionChainState,
        DamageContext damageContext)
    {
        var queuedDamageContexts = loadC008QueuedDamageContexts(actionChainState);
        queuedDamageContexts.Add(damageContext);
        saveC008QueuedDamageContexts(actionChainState, queuedDamageContexts);
    }

    private static void enqueueC014QueuedDamageContext(
        ActionChainState actionChainState,
        DamageContext damageContext)
    {
        var queuedDamageContexts = loadC014QueuedDamageContexts(actionChainState);
        queuedDamageContexts.Add(damageContext);
        saveC014QueuedDamageContexts(actionChainState, queuedDamageContexts);
    }

    private static void saveC008QueuedDamageContexts(
        ActionChainState actionChainState,
        List<DamageContext> damageContexts)
    {
        saveQueuedDamageContexts(actionChainState, LocalStateKeyC008QueuedDamageContexts, damageContexts);
    }

    private static void saveC014QueuedDamageContexts(
        ActionChainState actionChainState,
        List<DamageContext> damageContexts)
    {
        saveQueuedDamageContexts(actionChainState, LocalStateKeyC014RippleQueuedDamageContexts, damageContexts);
    }

    private static void saveQueuedDamageContexts(
        ActionChainState actionChainState,
        string localStateKey,
        List<DamageContext> damageContexts)
    {
        var serializedContexts = new List<string>();
        foreach (var damageContext in damageContexts)
        {
            serializedContexts.Add(string.Join(
                ";",
                damageContext.damageContextId.Value,
                serializeNullableId(damageContext.sourcePlayerId?.Value),
                serializeNullableId(damageContext.sourceCardInstanceId?.Value),
                serializeNullableId(damageContext.sourceCharacterInstanceId?.Value),
                serializeNullableId(damageContext.killCreditPlayerIdOverride?.Value),
                serializeNullableId(damageContext.targetPlayerId?.Value),
                serializeNullableId(damageContext.targetCardInstanceId?.Value),
                serializeNullableId(damageContext.targetCharacterInstanceId?.Value),
                damageContext.baseDamageValue,
                damageContext.damageType,
                damageContext.defenseDeclarationKey ?? string.Empty,
                damageContext.hasAppliedSourceDamageBonuses,
                damageContext.suppressT029DamageImmunityPrompt,
                damageContext.forcePenetrate,
                damageContext.isPrevented,
                damageContext.isImmune));
        }

        actionChainState.localState[localStateKey] = string.Join("|", serializedContexts);
    }

    private static List<DamageContext> loadC008QueuedDamageContexts(ActionChainState actionChainState)
    {
        return loadQueuedDamageContexts(
            actionChainState,
            LocalStateKeyC008QueuedDamageContexts,
            "C008:4 queued damage context is malformed.");
    }

    private static List<DamageContext> loadC014QueuedDamageContexts(ActionChainState actionChainState)
    {
        return loadQueuedDamageContexts(
            actionChainState,
            LocalStateKeyC014RippleQueuedDamageContexts,
            "C014:4 queued damage context is malformed.");
    }

    private static List<DamageContext> loadQueuedDamageContexts(
        ActionChainState actionChainState,
        string localStateKey,
        string malformedMessage)
    {
        var result = new List<DamageContext>();
        if (!actionChainState.localState.TryGetValue(
                localStateKey,
                out var serializedContexts) ||
            string.IsNullOrWhiteSpace(serializedContexts))
        {
            return result;
        }

        foreach (var serializedContext in serializedContexts.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var segments = serializedContext.Split(';');
            if (segments.Length != 16 ||
                !long.TryParse(segments[0], out var damageContextNumericId) ||
                !int.TryParse(segments[8], out var baseDamageValue) ||
                !bool.TryParse(segments[11], out var hasAppliedSourceDamageBonuses) ||
                !bool.TryParse(segments[12], out var suppressT029DamageImmunityPrompt) ||
                !bool.TryParse(segments[13], out var forcePenetrate) ||
                !bool.TryParse(segments[14], out var isPrevented) ||
                !bool.TryParse(segments[15], out var isImmune))
            {
                throw new InvalidOperationException(malformedMessage);
            }

            var damageContext = new DamageContext
            {
                damageContextId = new DamageContextId(damageContextNumericId),
                baseDamageValue = baseDamageValue,
                damageType = segments[9],
                defenseDeclarationKey = string.IsNullOrWhiteSpace(segments[10]) ? null : segments[10],
                hasAppliedSourceDamageBonuses = hasAppliedSourceDamageBonuses,
                suppressT029DamageImmunityPrompt = suppressT029DamageImmunityPrompt,
                forcePenetrate = forcePenetrate,
                isPrevented = isPrevented,
                isImmune = isImmune,
            };

            if (tryDeserializeNullableId(segments[1], out var sourcePlayerId))
            {
                damageContext.sourcePlayerId = new PlayerId(sourcePlayerId);
            }

            if (tryDeserializeNullableId(segments[2], out var sourceCardInstanceId))
            {
                damageContext.sourceCardInstanceId = new CardInstanceId(sourceCardInstanceId);
            }

            if (tryDeserializeNullableId(segments[3], out var sourceCharacterInstanceId))
            {
                damageContext.sourceCharacterInstanceId = new CharacterInstanceId(sourceCharacterInstanceId);
            }

            if (tryDeserializeNullableId(segments[4], out var killCreditPlayerIdOverride))
            {
                damageContext.killCreditPlayerIdOverride = new PlayerId(killCreditPlayerIdOverride);
            }

            if (tryDeserializeNullableId(segments[5], out var targetPlayerId))
            {
                damageContext.targetPlayerId = new PlayerId(targetPlayerId);
            }

            if (tryDeserializeNullableId(segments[6], out var targetCardInstanceId))
            {
                damageContext.targetCardInstanceId = new CardInstanceId(targetCardInstanceId);
            }

            if (tryDeserializeNullableId(segments[7], out var targetCharacterInstanceId))
            {
                damageContext.targetCharacterInstanceId = new CharacterInstanceId(targetCharacterInstanceId);
            }

            result.Add(damageContext);
        }

        return result;
    }

    private static string serializeNullableId(long? value)
    {
        return value.HasValue ? value.Value.ToString() : "-";
    }

    private static bool tryDeserializeNullableId(string value, out long numericId)
    {
        numericId = 0;
        return !string.Equals(value, "-", StringComparison.Ordinal) &&
               long.TryParse(value, out numericId);
    }

    private static void clearC008ExcaliburSequence(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(LocalStateKeyC008ExcaliburSequence);
        actionChainState.localState.Remove(LocalStateKeyC008RemainingTargetCharacterIds);
        actionChainState.localState.Remove(LocalStateKeyC008QueuedDamageContexts);
    }

    private static void clearC014RippleSequence(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(LocalStateKeyC014RippleRemainingDamages);
        actionChainState.localState.Remove(LocalStateKeyC014RipplePendingSelfDamage);
        actionChainState.localState.Remove(LocalStateKeyC014RippleSequence);
        actionChainState.localState.Remove(LocalStateKeyC014RippleQueuedDamageContexts);
        actionChainState.localState.Remove(LocalStateKeyC014RippleBeforeDefenseResponsesConsumed);
        actionChainState.localState.Remove(LocalStateKeyC014RippleDamageBonus);
        actionChainState.localState.Remove(LocalStateKeyC014RippleForcePenetrate);
    }

    private void tryResumeCharacterSkillSequences(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        tryResumeC003RedNightHealSequence(gameState, actionChainState, eventId);
        tryResumeC018SkillSequence(gameState, actionChainState, eventId);
        tryResumeC008ExcaliburSequence(gameState, actionChainState, eventId);
        tryResumeC004BlazingCometSequence(gameState, actionChainState, eventId);
        tryResumeC005SakuraSparkSequence(gameState, actionChainState, eventId);
        tryResumeC011TrueAncestorCombatDeactivateAfterDamage(gameState, actionChainState, eventId);
        tryResumeC011RealityMarbleSequence(gameState, actionChainState, eventId);
        tryResumeC013CriminalTakeRevealed(gameState, actionChainState, eventId);
        tryResumeC014RippleSequence(gameState, actionChainState, eventId);
        tryResumeC022DollWarLeylineReduction(gameState, actionChainState);
        turnTransitionProcessor.tryResumeTurnStartResponseQueue(gameState, actionChainState, eventId);
    }

    private static void tryResumeC011TrueAncestorCombatDeactivateAfterDamage(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId)
    {
        if (!actionChainState.localState.TryGetValue(
                LocalStateKeyC011TrueAncestorCombatDeactivateAfterDamage,
                out var serializedCharacterInstanceId))
        {
            return;
        }

        actionChainState.localState.Remove(LocalStateKeyC011TrueAncestorCombatDeactivateAfterDamage);
        if (!long.TryParse(serializedCharacterInstanceId, out var characterInstanceNumericId))
        {
            throw new InvalidOperationException("C011:2 deactivate-after-damage marker contains an invalid characterInstanceId.");
        }

        var characterInstanceId = new CharacterInstanceId(characterInstanceNumericId);
        if (!gameState.characterInstances.TryGetValue(characterInstanceId, out var characterInstance))
        {
            throw new InvalidOperationException("C011:2 deactivate-after-damage marker requires characterInstanceId to exist.");
        }

        if (!characterInstance.isActivated)
        {
            return;
        }

        actionChainState.producedEvents.Add(CharacterActivationRuntime.setActivated(
            gameState,
            characterInstanceId,
            false,
            actionChainState.actionChainId,
            eventId));
    }

    private void appendDrawOneCardEffectEventsForPlayer(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        GameState.PlayerState playerState,
        long requestId)
    {
        if (!gameState.zones.TryGetValue(playerState.deckZoneId, out var deckZoneState))
        {
            throw new InvalidOperationException("UseSkillActionRequest skill effect requires player deckZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(playerState.handZoneId))
        {
            throw new InvalidOperationException("UseSkillActionRequest skill effect requires player handZoneId to exist in gameState.zones.");
        }

        if (!gameState.zones.TryGetValue(playerState.discardZoneId, out var discardZoneState))
        {
            throw new InvalidOperationException("UseSkillActionRequest skill effect requires player discardZoneId to exist in gameState.zones.");
        }

        if (deckZoneState.cardInstanceIds.Count == 0 && discardZoneState.cardInstanceIds.Count > 0)
        {
            var discardCardIdsInCurrentOrder = PlayerDeckRuntime.createShuffledCardInstanceIds(discardZoneState.cardInstanceIds);
            foreach (var cardInstanceId in discardCardIdsInCurrentOrder)
            {
                var discardedCardInstance = gameState.cardInstances[cardInstanceId];
                var rebuildEvent = zoneMovementService.moveCard(
                    gameState,
                    discardedCardInstance,
                    playerState.deckZoneId,
                    CardMoveReason.returnToSource,
                    actionChainState.actionChainId,
                    requestId);
                actionChainState.producedEvents.Add(rebuildEvent);
            }
        }

        if (deckZoneState.cardInstanceIds.Count <= 0)
        {
            return;
        }

        var topCardInstanceId = deckZoneState.cardInstanceIds[0];
        var topCardInstance = gameState.cardInstances[topCardInstanceId];
        var drawEvent = zoneMovementService.moveCard(
            gameState,
            topCardInstance,
            playerState.handZoneId,
            CardMoveReason.draw,
            actionChainState.actionChainId,
            requestId);
        actionChainState.producedEvents.Add(drawEvent);
    }

    private static bool tryFindAliveInPlayCharacterInstanceByOwner(
        GameState.GameState gameState,
        PlayerId ownerPlayerId,
        out CharacterInstance characterInstance)
    {
        CharacterInstance? selectedCharacter = null;
        long selectedCharacterNumericId = long.MaxValue;
        foreach (var candidateCharacterInstance in gameState.characterInstances.Values)
        {
            if (candidateCharacterInstance.ownerPlayerId != ownerPlayerId ||
                !candidateCharacterInstance.isAlive ||
                !candidateCharacterInstance.isInPlay)
            {
                continue;
            }

            if (candidateCharacterInstance.characterInstanceId.Value >= selectedCharacterNumericId)
            {
                continue;
            }

            selectedCharacterNumericId = candidateCharacterInstance.characterInstanceId.Value;
            selectedCharacter = candidateCharacterInstance;
        }

        if (selectedCharacter is null)
        {
            characterInstance = null!;
            return false;
        }

        characterInstance = selectedCharacter;
        return true;
    }

    private static void appendHealOnCharacter(
        ActionChainState actionChainState,
        long requestId,
        CharacterInstance characterInstance,
        int healAmount)
    {
        if (healAmount <= 0)
        {
            return;
        }

        var hpBefore = characterInstance.currentHp;
        var hpAfter = Math.Min(characterInstance.maxHp, hpBefore + healAmount);
        if (hpAfter == hpBefore)
        {
            return;
        }

        characterInstance.currentHp = hpAfter;
        actionChainState.producedEvents.Add(new HpChangedEvent
        {
            eventId = requestId,
            eventTypeKey = "hpChanged",
            sourceActionChainId = actionChainState.actionChainId,
            targetPlayerId = characterInstance.ownerPlayerId,
            targetCharacterInstanceId = characterInstance.characterInstanceId,
            hpBefore = hpBefore,
            hpAfter = hpAfter,
            delta = hpAfter - hpBefore,
        });
    }

    private void applyT014OnDefensePostResolution(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        GameState.PlayerState defenderPlayerState,
        CardInstance defenseCardInstance,
        int producedEventsStartIndexForDamageResolution,
        CharacterInstanceId? defenderActiveCharacterInstanceIdBeforeResolution)
    {
        var defenderWasKilledInThisResolution = false;
        if (defenderActiveCharacterInstanceIdBeforeResolution.HasValue)
        {
            for (var eventIndex = Math.Max(0, producedEventsStartIndexForDamageResolution);
                 eventIndex < actionChainState.producedEvents.Count;
                 eventIndex++)
            {
                if (actionChainState.producedEvents[eventIndex] is not KillRecordedEvent killRecordedEvent)
                {
                    continue;
                }

                if (killRecordedEvent.killedCharacterInstanceId == defenderActiveCharacterInstanceIdBeforeResolution.Value)
                {
                    defenderWasKilledInThisResolution = true;
                    break;
                }
            }
        }

        if (!defenderWasKilledInThisResolution &&
            defenderActiveCharacterInstanceIdBeforeResolution.HasValue &&
            gameState.characterInstances.TryGetValue(
                defenderActiveCharacterInstanceIdBeforeResolution.Value,
                out var defenderCharacterInstance) &&
            defenderCharacterInstance.isAlive &&
            defenderCharacterInstance.isInPlay)
        {
            appendHealOnCharacter(
                actionChainState,
                requestId,
                defenderCharacterInstance,
                healAmount: 4);
        }

        if (defenseCardInstance.zoneId == defenderPlayerState.fieldZoneId &&
            defenseCardInstance.isDefensePlacedOnField)
        {
            var movedEvent = zoneMovementService.moveCard(
                gameState,
                defenseCardInstance,
                defenderPlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                requestId);
            actionChainState.producedEvents.Add(movedEvent);
            defenseCardInstance.isDefensePlacedOnField = false;
        }
    }

    private void applyT015OnDefensePostResolution(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        GameState.PlayerState defenderPlayerState,
        PlayerId damageSourcePlayerId,
        CardInstance defenseCardInstance,
        int producedEventsStartIndexForDamageResolution,
        CharacterInstanceId? defenderActiveCharacterInstanceIdBeforeResolution)
    {
        var defenderWasKilledInThisResolution = false;
        if (defenderActiveCharacterInstanceIdBeforeResolution.HasValue)
        {
            for (var eventIndex = Math.Max(0, producedEventsStartIndexForDamageResolution);
                 eventIndex < actionChainState.producedEvents.Count;
                 eventIndex++)
            {
                if (actionChainState.producedEvents[eventIndex] is not KillRecordedEvent killRecordedEvent)
                {
                    continue;
                }

                if (killRecordedEvent.killedCharacterInstanceId == defenderActiveCharacterInstanceIdBeforeResolution.Value)
                {
                    defenderWasKilledInThisResolution = true;
                    break;
                }
            }
        }

        if (!defenderWasKilledInThisResolution &&
            defenderActiveCharacterInstanceIdBeforeResolution.HasValue &&
            gameState.characterInstances.TryGetValue(
                defenderActiveCharacterInstanceIdBeforeResolution.Value,
                out var defenderCharacterInstance) &&
            defenderCharacterInstance.isAlive &&
            defenderCharacterInstance.isInPlay &&
            tryFindAliveInPlayCharacterInstanceByOwner(
                gameState,
                damageSourcePlayerId,
                out var damageSourceCharacterInstance))
        {
            var directDamageContext = new DamageContext
            {
                damageContextId = new DamageContextId(requestId),
                sourcePlayerId = defenderPlayerState.playerId,
                sourceCardInstanceId = defenseCardInstance.cardInstanceId,
                targetPlayerId = damageSourcePlayerId,
                targetCharacterInstanceId = damageSourceCharacterInstance.characterInstanceId,
                baseDamageValue = 1,
                damageType = DamageTypeKeyDirect,
            };
            actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, directDamageContext));
        }

        if (defenseCardInstance.zoneId == defenderPlayerState.fieldZoneId &&
            defenseCardInstance.isDefensePlacedOnField)
        {
            var movedEvent = zoneMovementService.moveCard(
                gameState,
                defenseCardInstance,
                defenderPlayerState.discardZoneId,
                CardMoveReason.discard,
                actionChainState.actionChainId,
                requestId);
            actionChainState.producedEvents.Add(movedEvent);
            defenseCardInstance.isDefensePlacedOnField = false;
        }
    }

    private void moveDefensePlacedCardToDiscardPostResolution(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        GameState.PlayerState defenderPlayerState,
        CardInstance defenseCardInstance)
    {
        if (defenseCardInstance.zoneId != defenderPlayerState.fieldZoneId ||
            !defenseCardInstance.isDefensePlacedOnField)
        {
            return;
        }

        var movedEvent = zoneMovementService.moveCard(
            gameState,
            defenseCardInstance,
            defenderPlayerState.discardZoneId,
            CardMoveReason.discard,
            actionChainState.actionChainId,
            requestId);
        actionChainState.producedEvents.Add(movedEvent);
        defenseCardInstance.isDefensePlacedOnField = false;
    }

    private List<GameEvent> processEnterActionPhaseActionRequest(
        GameState.GameState gameState,
        EnterActionPhaseActionRequest enterActionPhaseActionRequest)
    {
        return turnTransitionProcessor.processEnterActionPhaseActionRequest(
            gameState,
            enterActionPhaseActionRequest);
    }

    private List<GameEvent> processEnterSummonPhaseActionRequest(
        GameState.GameState gameState,
        EnterSummonPhaseActionRequest enterSummonPhaseActionRequest)
    {
        return turnTransitionProcessor.processEnterSummonPhaseActionRequest(
            gameState,
            enterSummonPhaseActionRequest);
    }

    private List<GameEvent> processEnterEndPhaseActionRequest(
        GameState.GameState gameState,
        EnterEndPhaseActionRequest enterEndPhaseActionRequest)
    {
        return endPhaseProcessor.processEnterEndPhaseActionRequest(
            gameState,
            enterEndPhaseActionRequest);
    }

    private List<GameEvent> processStartNextTurnActionRequest(
        GameState.GameState gameState,
        StartNextTurnActionRequest startNextTurnActionRequest)
    {
        return turnTransitionProcessor.processStartNextTurnActionRequest(
            gameState,
            startNextTurnActionRequest);
    }

    private List<GameEvent> processTryResolveAnomalyActionRequest(
        GameState.GameState gameState,
        TryResolveAnomalyActionRequest tryResolveAnomalyActionRequest)
    {
        var producedEvents = anomalyProcessor.processTryResolveAnomalyActionRequest(
            gameState,
            tryResolveAnomalyActionRequest);
        if (gameState.currentActionChain is not null)
        {
            tryApplyTreasureBanishEffectsFromProducedEvents(
                gameState,
                gameState.currentActionChain,
                tryResolveAnomalyActionRequest.requestId,
                producedEventsStartIndex: 0);
            tryOpenTreasureArrivalEffectsFromProducedEvents(
                gameState,
                gameState.currentActionChain,
                tryResolveAnomalyActionRequest.requestId,
                producedEventsStartIndex: 0);
        }

        return producedEvents;
    }

    private static (int manaCost, int leylineCost) resolveUseSkillResourceCost(
        string characterDefinitionId,
        string skillKey)
    {
        return CharacterSkillCostResolver.resolveSkillCost(characterDefinitionId, skillKey);
    }

    private static bool isOncePerTurnSkill(string skillKey)
    {
        return string.Equals(skillKey, SkillKeyC018_2, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC001_4, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC011_2, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC021_1, StringComparison.Ordinal);
    }

    private static bool isSequencedSkill(string skillKey)
    {
        return string.Equals(skillKey, SkillKeyC001_1, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC003_1, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC003_3, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC004_1, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC004_4, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC005_3, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC005_4, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC011_1, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC011_2, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC011_4, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC018_2, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC018_3, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC008_4, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC014_3, StringComparison.Ordinal) ||
               string.Equals(skillKey, SkillKeyC014_4, StringComparison.Ordinal) ||
                string.Equals(skillKey, SkillKeyC021_1, StringComparison.Ordinal) ||
                string.Equals(skillKey, SkillKeyC021_2, StringComparison.Ordinal) ||
                string.Equals(skillKey, SkillKeyC021_4, StringComparison.Ordinal) ||
                string.Equals(skillKey, SkillKeyC022_4, StringComparison.Ordinal) ||
                string.Equals(skillKey, SkillKeyC031_2, StringComparison.Ordinal) ||
                string.Equals(skillKey, SkillKeyC031_3, StringComparison.Ordinal);
    }

    private static bool requiresOpponentTarget(string skillKey)
    {
        return false;
    }

    private static bool requiresAllyTarget(string skillKey)
    {
        return false;
    }

    private static bool tryGetMarkerCost(string skillKey, out string markerCostType, out int markerCostAmount)
    {
        if (string.Equals(skillKey, SkillKeyC007_3, StringComparison.Ordinal))
        {
            markerCostType = MarkerTypeDream;
            markerCostAmount = 3;
            return true;
        }

        if (string.Equals(skillKey, SkillKeyC022_2, StringComparison.Ordinal))
        {
            markerCostType = MarkerTypeDoll;
            markerCostAmount = 1;
            return true;
        }

        if (string.Equals(skillKey, SkillKeyC022_3, StringComparison.Ordinal))
        {
            markerCostType = MarkerTypeDoll;
            markerCostAmount = 2;
            return true;
        }

        markerCostType = string.Empty;
        markerCostAmount = 0;
        return false;
    }

    private static void ensurePlayTreasureCardActionRequestGuard(
        GameState.GameState gameState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires gameState.turnState to be initialized.");
        }

        if (playTreasureCardActionRequest.actorPlayerId != gameState.turnState.currentPlayerId)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest actorPlayerId must equal gameState.turnState.currentPlayerId.");
        }

        if (gameState.turnState.currentPhase != GameState.TurnPhase.action)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires gameState.turnState.currentPhase to be action.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires gameState.currentInputContext to be null.");
        }

        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires gameState.currentResponseWindow to be null.");
        }

        var cardInstance = gameState.cardInstances[playTreasureCardActionRequest.cardInstanceId];
        if (cardInstance.ownerPlayerId != playTreasureCardActionRequest.actorPlayerId)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires cardInstance.ownerPlayerId to equal actorPlayerId.");
        }

        var actorPlayerState = gameState.players[playTreasureCardActionRequest.actorPlayerId];
        if (cardInstance.zoneId != actorPlayerState.handZoneId)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires cardInstance.zoneId to equal actor player's handZoneId.");
        }

        var sourceZoneState = gameState.zones[cardInstance.zoneId];
        if (sourceZoneState.zoneType != ZoneKey.hand)
        {
            throw new InvalidOperationException("PlayTreasureCardActionRequest requires sourceZoneState.zoneType to be hand.");
        }
    }

    private void appendScriptedOnPlayEffectEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest,
        CardInstance cardInstance)
    {
        if (treasureOnPlayEffectRuntime.tryOpenOnPlayInputContext(
                gameState,
                actionChainState,
                playTreasureCardActionRequest,
                cardInstance))
        {
            return;
        }

        var onPlayProbeEffectKind = TemporaryOnPlayProbeResolver.resolveOnPlayProbeEffectKind(cardInstance.definitionId);
        switch (onPlayProbeEffectKind)
        {
            case TemporaryOnPlayProbeResolver.ProbeEffectKind.immediateDamage1:
            {
                var targetCharacterInstanceId = findDeterministicScriptedDamageTarget(
                    gameState,
                    playTreasureCardActionRequest.actorPlayerId);

                appendResponseWindowAndDamageEventsToExistingActionChain(
                    gameState,
                    actionChainState,
                    playTreasureCardActionRequest.requestId,
                    playTreasureCardActionRequest.actorPlayerId,
                    playTreasureCardActionRequest.cardInstanceId,
                    targetCharacterInstanceId,
                    1);
                return;
            }

            case TemporaryOnPlayProbeResolver.ProbeEffectKind.waitResponseDamage1:
            {
                var targetCharacterInstanceId = findDeterministicScriptedDamageTarget(
                    gameState,
                    playTreasureCardActionRequest.actorPlayerId);

                openDamageResponseWindowWithPendingDamage(
                    gameState,
                    actionChainState,
                    playTreasureCardActionRequest.requestId,
                    playTreasureCardActionRequest.actorPlayerId,
                    playTreasureCardActionRequest.cardInstanceId,
                    null,
                    targetCharacterInstanceId,
                    1,
                    DamageTypeKeyPhysical);
                return;
            }

            case TemporaryOnPlayProbeResolver.ProbeEffectKind.chooseDamage1:
            {
                if (gameState.currentInputContext is not null)
                {
                    throw new InvalidOperationException("Scripted on-play choose damage requires currentInputContext to be null.");
                }

                var inputContextId = new InputContextId(Interlocked.Increment(ref nextInputContextNumericId));
                var inputContextState = new InputContextState
                {
                    inputContextId = inputContextId,
                    requiredPlayerId = playTreasureCardActionRequest.actorPlayerId,
                    sourceActionChainId = actionChainState.actionChainId,
                    inputTypeKey = TemporaryOnPlayProbeResolver.ScriptedOnPlayChoiceInputTypeKey,
                    contextKey = TemporaryOnPlayProbeResolver.ScriptedOnPlayChooseDamageContextKey,
                };
                inputContextState.choiceKeys.Add(TemporaryOnPlayProbeResolver.ScriptedOnPlayDeal1ChoiceKey);
                gameState.currentInputContext = inputContextState;
                actionChainState.pendingContinuationKey = TemporaryOnPlayProbeResolver.ContinuationKeyInputChoiceDamage;

                actionChainState.producedEvents.Add(new InteractionWindowEvent
                {
                    eventId = playTreasureCardActionRequest.requestId,
                    eventTypeKey = "inputContextOpened",
                    sourceActionChainId = actionChainState.actionChainId,
                    windowKindKey = "inputContext",
                    inputContextId = inputContextId,
                    isOpened = true,
                });
                return;
            }

            case TemporaryOnPlayProbeResolver.ProbeEffectKind.none:
            default:
                return;
        }
    }

    private CharacterInstanceId findDeterministicScriptedDamageTarget(
        GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        CharacterInstanceId? selectedTargetCharacterInstanceId = null;
        long selectedTargetNumericId = long.MaxValue;

        foreach (var characterInstance in gameState.characterInstances.Values)
        {
            if (!characterInstance.isAlive || !characterInstance.isInPlay)
            {
                continue;
            }

            if (characterInstance.ownerPlayerId == actorPlayerId)
            {
                continue;
            }

            var currentCharacterNumericId = characterInstance.characterInstanceId.Value;
            if (currentCharacterNumericId < selectedTargetNumericId)
            {
                selectedTargetNumericId = currentCharacterNumericId;
                selectedTargetCharacterInstanceId = characterInstance.characterInstanceId;
            }
        }

        if (!selectedTargetCharacterInstanceId.HasValue)
        {
            throw new InvalidOperationException("Scripted on-play damage requires an alive in-play enemy character target.");
        }

        return selectedTargetCharacterInstanceId.Value;
    }

    private void appendResponseWindowAndDamageEventsToExistingActionChain(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        CharacterInstanceId targetCharacterInstanceId,
        int baseDamageValue)
    {
        var responseWindowState = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(Interlocked.Increment(ref nextResponseWindowNumericId)),
            originType = ResponseWindowOriginType.chain,
            windowTypeKey = "damageResponse",
            sourceActionChainId = actionChainState.actionChainId,
        };
        gameState.currentResponseWindow = responseWindowState;

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = requestId,
            eventTypeKey = "responseWindowOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "responseWindow",
            responseWindowId = responseWindowState.responseWindowId,
            responseWindowOriginType = responseWindowState.originType,
            isOpened = true,
        });

        gameState.currentResponseWindow = null;

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = requestId,
            eventTypeKey = "responseWindowClosed",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "responseWindow",
            responseWindowId = responseWindowState.responseWindowId,
            responseWindowOriginType = responseWindowState.originType,
            isOpened = false,
        });

        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(requestId),
            sourcePlayerId = sourcePlayerId,
            sourceCardInstanceId = sourceCardInstanceId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = baseDamageValue,
            damageType = DamageTypeKeyDirect,
        };

        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, damageContext));
    }

    private void appendDamageEventsToExistingActionChain(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        CharacterInstanceId targetCharacterInstanceId,
        int baseDamageValue)
    {
        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(requestId),
            sourcePlayerId = sourcePlayerId,
            sourceCardInstanceId = sourceCardInstanceId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = baseDamageValue,
            damageType = DamageTypeKeyDirect,
        };

        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(gameState, damageContext));
    }

    private void openDamageResponseWindowWithPendingDamage(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        CharacterInstanceId? sourceCharacterInstanceId,
        CharacterInstanceId targetCharacterInstanceId,
        int baseDamageValue,
        string? damageTypeKey,
        bool hasAppliedSourceDamageBonuses = false,
        bool suppressC003Gungnir = false,
        bool suppressC021Funeral = false,
        bool suppressC014AndThen = false,
        bool forcePenetrate = false)
    {
        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("Staged damage response window cannot open while currentResponseWindow is active.");
        }

        if (!gameState.characterInstances.TryGetValue(targetCharacterInstanceId, out var pendingDamageTargetCharacter))
        {
            throw new InvalidOperationException("Staged damage response window requires pending damage target character to exist in gameState.characterInstances.");
        }

        var pendingDamageDefenderPlayerId = pendingDamageTargetCharacter.ownerPlayerId;
        var stagedDamageContext = new DamageContext
        {
            damageContextId = new DamageContextId(requestId),
            sourcePlayerId = sourcePlayerId,
            sourceCardInstanceId = sourceCardInstanceId,
            sourceCharacterInstanceId = sourceCharacterInstanceId,
            targetPlayerId = pendingDamageDefenderPlayerId,
            targetCharacterInstanceId = targetCharacterInstanceId,
            baseDamageValue = baseDamageValue,
            damageType = normalizePendingDamageTypeKey(damageTypeKey),
            hasAppliedSourceDamageBonuses = hasAppliedSourceDamageBonuses,
            forcePenetrate = forcePenetrate,
        };
        actionChainState.producedEvents.AddRange(
            damageProcessor.applySourceMatchingDamageBonuses(gameState, stagedDamageContext));
        tryApplyC005SwordAuraBeforePhysicalDamage(
            gameState,
            actionChainState,
            stagedDamageContext,
            requestId);
        if (!suppressC021Funeral &&
            tryOpenC021FuneralBeforeCharmAndBarrier(
                gameState,
                actionChainState,
                requestId,
                stagedDamageContext))
        {
            return;
        }

        if (!suppressC014AndThen &&
            tryOpenC014AndThenBeforeDefense(gameState, actionChainState, requestId, stagedDamageContext))
        {
            return;
        }

        var wasCharmConsumedBeforeDefense = damageProcessor.tryConsumeCharmBeforeDefense(
            gameState,
            stagedDamageContext,
            out var charmPreDefenseEvents);
        actionChainState.producedEvents.AddRange(charmPreDefenseEvents);
        if (damageProcessor.tryResolveBarrierBeforeDefense(
                gameState,
                stagedDamageContext,
                out var barrierPreventionEvents))
        {
            var producedEventsStartIndex = actionChainState.producedEvents.Count;
            actionChainState.producedEvents.AddRange(barrierPreventionEvents);
            tryApplyC021BlackKeyFreeManaAfterDamage(
                gameState,
                actionChainState,
                requestId);
            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                requestId,
                producedEventsStartIndex);
            tryResumeCharacterSkillSequences(gameState, actionChainState, requestId);
            return;
        }

        if (wasCharmConsumedBeforeDefense)
        {
            var producedEventsStartIndex = actionChainState.producedEvents.Count;
            queueOrResolveStagedDamage(
                gameState,
                actionChainState,
                stagedDamageContext,
                requestId,
                producedEventsStartIndex);
            tryApplyC021BlackKeyFreeManaAfterDamage(
                gameState,
                actionChainState,
                requestId);
            return;
        }

        if (tryOpenBeforeDefenseCharacterResponses(
                gameState,
                actionChainState,
                requestId,
                stagedDamageContext,
                suppressC014AndThen: true,
                suppressC003Gungnir))
        {
            return;
        }

        var responseWindowState = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(Interlocked.Increment(ref nextResponseWindowNumericId)),
            originType = ResponseWindowOriginType.flow,
            windowTypeKey = "damageResponse",
            sourceActionChainId = actionChainState.actionChainId,
            pendingDamageTargetCharacterInstanceId = targetCharacterInstanceId,
            pendingDamageBaseDamageValue = stagedDamageContext.baseDamageValue,
            pendingDamageSourcePlayerId = sourcePlayerId,
            pendingDamageSourceCardInstanceId = sourceCardInstanceId,
            pendingDamageSourceCharacterInstanceId = sourceCharacterInstanceId,
            pendingDamageTypeKey = stagedDamageContext.damageType,
            pendingDamageDefenseDeclarationKey = null,
            pendingDamageDefenderPlayerId = pendingDamageDefenderPlayerId,
        };
        setDamageResponseStage(
            responseWindowState,
            DamageResponseStage.awaitDefense,
            pendingDamageDefenderPlayerId);
        gameState.currentResponseWindow = responseWindowState;
        actionChainState.pendingContinuationKey = ContinuationKeyStagedResponseDamage;

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = requestId,
            eventTypeKey = "responseWindowOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "responseWindow",
            responseWindowId = responseWindowState.responseWindowId,
            responseWindowOriginType = responseWindowState.originType,
            isOpened = true,
        });
    }

    private List<GameEvent> processOpenDamageResponseWindowActionRequest(
        GameState.GameState gameState,
        OpenDamageResponseWindowActionRequest openDamageResponseWindowActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("OpenDamageResponseWindowActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("OpenDamageResponseWindowActionRequest requires gameState.turnState to be initialized.");
        }

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(openDamageResponseWindowActionRequest.requestId),
            actorPlayerId = openDamageResponseWindowActionRequest.actorPlayerId,
            rootActionRequest = openDamageResponseWindowActionRequest,
            isCompleted = false,
            currentFrameIndex = 0,
        };

        var effectFrame = new EffectFrame
        {
            effectKey = "openDamageResponseWindow",
            sourcePlayerId = openDamageResponseWindowActionRequest.actorPlayerId,
            sourceCardInstanceId = openDamageResponseWindowActionRequest.sourceCardInstanceId,
            sourceCharacterInstanceId = openDamageResponseWindowActionRequest.sourceCharacterInstanceId,
            targetCharacterInstanceId = openDamageResponseWindowActionRequest.targetCharacterInstanceId,
            contextKey = "stagedDamageResponse",
        };

        actionChainState.effectFrames.Add(effectFrame);
        gameState.currentActionChain = actionChainState;

        openDamageResponseWindowWithPendingDamage(
            gameState,
            actionChainState,
            openDamageResponseWindowActionRequest.requestId,
            openDamageResponseWindowActionRequest.actorPlayerId,
            openDamageResponseWindowActionRequest.sourceCardInstanceId,
            openDamageResponseWindowActionRequest.sourceCharacterInstanceId,
            openDamageResponseWindowActionRequest.targetCharacterInstanceId,
            openDamageResponseWindowActionRequest.baseDamageValue,
            openDamageResponseWindowActionRequest.damageTypeKey);

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);
        return actionChainState.producedEvents;
    }

    private List<GameEvent> processSubmitResponseActionRequest(
        GameState.GameState gameState,
        SubmitResponseActionRequest submitResponseActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest requires gameState.turnState to be initialized.");
        }

        var actionChainState = gameState.currentActionChain;
        if (actionChainState is null)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest requires an active currentActionChain.");
        }

        var responseWindowState = gameState.currentResponseWindow;
        if (responseWindowState is null)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest requires an active currentResponseWindow.");
        }

        if (submitResponseActionRequest.responseWindowId != responseWindowState.responseWindowId)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest responseWindowId mismatch.");
        }

        if (!responseWindowState.currentResponderPlayerId.HasValue)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest requires currentResponseWindow.currentResponderPlayerId to be set.");
        }

        if (submitResponseActionRequest.actorPlayerId != responseWindowState.currentResponderPlayerId.Value)
        {
            throw new InvalidOperationException("SubmitResponseActionRequest actorPlayerId does not match currentResponseWindow.currentResponderPlayerId.");
        }

        if (responseWindowState.windowTypeKey == "damageResponse")
        {
            if (submitResponseActionRequest.shouldRespond &&
                StatusRuntime.hasStatusOnPlayer(gameState, submitResponseActionRequest.actorPlayerId, StatusKeySilence))
            {
                throw new InvalidOperationException("SubmitResponseActionRequest actorPlayerId cannot respond while Silence status is active.");
            }

            if (submitResponseActionRequest.shouldRespond || submitResponseActionRequest.responseKey is not null)
            {
                throw new NotSupportedException("SubmitResponseActionRequest currently only supports no-response continuation (shouldRespond=false, responseKey=null).");
            }

            if (actionChainState.pendingContinuationKey != ContinuationKeyStagedResponseDamage)
            {
                throw new InvalidOperationException("SubmitResponseActionRequest requires currentActionChain.pendingContinuationKey to be continuation:stagedResponseDamage.");
            }

            var pendingDamageTargetCharacterInstanceId = responseWindowState.pendingDamageTargetCharacterInstanceId;
            if (!pendingDamageTargetCharacterInstanceId.HasValue)
            {
                throw new InvalidOperationException("SubmitResponseActionRequest requires pending damage target data in currentResponseWindow.");
            }

            var pendingDamageBaseDamageValue = responseWindowState.pendingDamageBaseDamageValue;
            if (!pendingDamageBaseDamageValue.HasValue)
            {
                throw new InvalidOperationException("SubmitResponseActionRequest requires pending damage value data in currentResponseWindow.");
            }

            var pendingDamageResponseStage = getDamageResponseStageOrThrow(
                responseWindowState,
                "SubmitResponseActionRequest requires valid pending damage response stage data in currentResponseWindow.");

            var pendingDamageDefenderPlayerId = responseWindowState.pendingDamageDefenderPlayerId;
            if (!pendingDamageDefenderPlayerId.HasValue)
            {
                throw new InvalidOperationException("SubmitResponseActionRequest requires pending damage defender data in currentResponseWindow.");
            }

            var pendingDamageSourcePlayerId = responseWindowState.pendingDamageSourcePlayerId;
            if (!pendingDamageSourcePlayerId.HasValue)
            {
                throw new InvalidOperationException("SubmitResponseActionRequest requires pending damage source player data in currentResponseWindow.");
            }

            ensureDamageResponseStateConsistency(
                responseWindowState,
                pendingDamageResponseStage,
                pendingDamageDefenderPlayerId.Value,
                pendingDamageSourcePlayerId.Value,
                "SubmitResponseActionRequest");

            var producedEventsStartIndex = actionChainState.producedEvents.Count;
            var producedEvents = closeDamageResponseWindowAndResolveDamage(
                gameState,
                actionChainState,
                responseWindowState,
                submitResponseActionRequest.requestId);
            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                submitResponseActionRequest.requestId,
                producedEventsStartIndex);
            actionChainState.isCompleted =
                gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);
            return producedEvents;
        }

        throw new NotSupportedException("SubmitResponseActionRequest currentResponseWindow.windowTypeKey is not supported.");
    }

    private List<GameEvent> processSubmitDefenseActionRequest(
        GameState.GameState gameState,
        SubmitDefenseActionRequest submitDefenseActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires gameState.turnState to be initialized.");
        }

        var actionChainState = gameState.currentActionChain;
        if (actionChainState is null)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires an active currentActionChain.");
        }

        var responseWindowState = gameState.currentResponseWindow;
        if (responseWindowState is null || responseWindowState.windowTypeKey != "damageResponse")
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires an active damageResponse currentResponseWindow.");
        }

        if (actionChainState.pendingContinuationKey != ContinuationKeyStagedResponseDamage)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires currentActionChain.pendingContinuationKey to be continuation:stagedResponseDamage.");
        }

        var pendingDamageResponseStage = getDamageResponseStageOrThrow(
            responseWindowState,
            "SubmitDefenseActionRequest requires currentResponseWindow.pendingDamageResponseStageKey to be awaitDefense.");
        if (pendingDamageResponseStage != DamageResponseStage.awaitDefense)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires currentResponseWindow.pendingDamageResponseStageKey to be awaitDefense.");
        }

        var pendingDamageDefenderPlayerId = responseWindowState.pendingDamageDefenderPlayerId;
        if (!pendingDamageDefenderPlayerId.HasValue)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires pending damage defender data in currentResponseWindow.");
        }

        if (submitDefenseActionRequest.actorPlayerId != pendingDamageDefenderPlayerId.Value)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest actorPlayerId must equal pending damage defender player.");
        }

        if (!gameState.players.TryGetValue(submitDefenseActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires actorPlayerId to exist in gameState.players.");
        }
        var defenderActiveCharacterInstanceIdBeforeResolution = actorPlayerState.activeCharacterInstanceId;
        if (!defenderActiveCharacterInstanceIdBeforeResolution.HasValue)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires defender player to have an active character.");
        }

        var pendingDamageSourcePlayerId = responseWindowState.pendingDamageSourcePlayerId;
        if (!pendingDamageSourcePlayerId.HasValue)
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest requires pending damage source player data in currentResponseWindow.");
        }

        ensureDamageResponseStateConsistency(
            responseWindowState,
            pendingDamageResponseStage,
            pendingDamageDefenderPlayerId.Value,
            pendingDamageSourcePlayerId.Value,
            "SubmitDefenseActionRequest");

        if (StatusRuntime.hasStatusOnPlayer(gameState, submitDefenseActionRequest.actorPlayerId, "Charm"))
        {
            throw new InvalidOperationException("SubmitDefenseActionRequest actorPlayerId cannot defend while Charm status is active.");
        }

        CardInstance? formalDefenseCardInstance = null;
        var shouldApplyT014OnDefensePostResolution = false;
        var shouldApplyT015OnDefensePostResolution = false;
        var shouldMoveDefenseCardToDiscardPostResolution = false;
        var shouldOpenT025ExtraDiscardInputContext = false;
        var producedEventsCountBeforeDamageResolution = actionChainState.producedEvents.Count;
        if (submitDefenseActionRequest.defenseTypeKey == DefenseTypeKeyFixedReduce1)
        {
            responseWindowState.pendingDamageDefenseDeclarationKey = DefenseTypeKeyFixedReduce1;
        }
        else
        {
            if (!isFormalDefenseTypeKey(submitDefenseActionRequest.defenseTypeKey))
            {
                throw new NotSupportedException("SubmitDefenseActionRequest currently only supports defenseTypeKey=fixedReduce1.");
            }

            if (!gameState.cardInstances.TryGetValue(submitDefenseActionRequest.defenseCardInstanceId, out var defenseCardInstance))
            {
                throw new InvalidOperationException("SubmitDefenseActionRequest requires defenseCardInstanceId to exist in gameState.cardInstances.");
            }
            formalDefenseCardInstance = defenseCardInstance;
            shouldApplyT014OnDefensePostResolution =
                string.Equals(defenseCardInstance.definitionId, TreasureDefinitionIdT014, StringComparison.Ordinal);
            shouldApplyT015OnDefensePostResolution =
                string.Equals(defenseCardInstance.definitionId, TreasureDefinitionIdT015, StringComparison.Ordinal);
            shouldMoveDefenseCardToDiscardPostResolution =
                string.Equals(defenseCardInstance.definitionId, TreasureDefinitionIdT020, StringComparison.Ordinal);
            shouldOpenT025ExtraDiscardInputContext =
                string.Equals(defenseCardInstance.definitionId, TreasureDefinitionIdT025, StringComparison.Ordinal);

            if (defenseCardInstance.ownerPlayerId != submitDefenseActionRequest.actorPlayerId)
            {
                throw new InvalidOperationException("SubmitDefenseActionRequest requires defenseCardInstance.ownerPlayerId to equal actorPlayerId.");
            }

            var isDefenseCardInActorHand = defenseCardInstance.zoneId == actorPlayerState.handZoneId;
            var isDefenseCardAlreadyDefensePlacedOnField =
                defenseCardInstance.zoneId == actorPlayerState.fieldZoneId &&
                defenseCardInstance.isDefensePlacedOnField;
            if (!isDefenseCardInActorHand && !isDefenseCardAlreadyDefensePlacedOnField)
            {
                throw new InvalidOperationException(
                    "SubmitDefenseActionRequest requires defense card to be in actor hand zone, or already defense-placed in actor field zone.");
            }

            var definitionDefenseValue = DefenseValueRuntimeResolver.resolveEffectiveDefenseValue(
                gameState,
                defenseCardInstance);
            var definitionDefenseTypeKey = TreasureResourceValueResolver.resolveDefenseTypeKey(defenseCardInstance.definitionId);
            if (!definitionDefenseValue.HasValue || string.IsNullOrWhiteSpace(definitionDefenseTypeKey))
            {
                throw new InvalidOperationException("SubmitDefenseActionRequest requires defense card definition to provide defenseValue and defenseTypeKey.");
            }
            var supportsDeclaredDefenseType = isCardDefinitionDefenseTypeSupportingDeclaredDefenseType(
                definitionDefenseTypeKey,
                submitDefenseActionRequest.defenseTypeKey);
            var effectiveDeclaredDefenseValue = supportsDeclaredDefenseType
                ? definitionDefenseValue.Value
                : 0;

            if (isDefenseCardInActorHand)
            {
                var defensePlaceMovedEvent = zoneMovementService.moveCard(
                    gameState,
                    defenseCardInstance,
                    actorPlayerState.fieldZoneId,
                    CardMoveReason.defensePlace,
                    actionChainState.actionChainId,
                    submitDefenseActionRequest.requestId);
                actionChainState.producedEvents.Add(defensePlaceMovedEvent);
                defenseCardInstance.isDefensePlacedOnField = true;
            }

            responseWindowState.pendingDamageDefenseDeclarationKey = createCardDefenseDeclarationKey(
                submitDefenseActionRequest.defenseTypeKey,
                effectiveDeclaredDefenseValue);
        }

        if (shouldOpenT025ExtraDiscardInputContext && formalDefenseCardInstance is not null)
        {
            treasureDefenseEffectRuntime.openT025ExtraDiscardInputContext(
                gameState,
                actionChainState,
                submitDefenseActionRequest.requestId,
                actorPlayerState,
                formalDefenseCardInstance.cardInstanceId);
            return actionChainState.producedEvents;
        }

        if (formalDefenseCardInstance is not null &&
            tryOpenC013AbnormalTasteAfterDefense(
                gameState,
                actionChainState,
                responseWindowState,
                submitDefenseActionRequest.requestId,
                formalDefenseCardInstance,
                actorPlayerState,
                defenderActiveCharacterInstanceIdBeforeResolution.Value,
                producedEventsCountBeforeDamageResolution))
        {
            return actionChainState.producedEvents;
        }

        var producedEvents = closeDamageResponseWindowAndResolveDamage(
            gameState,
            actionChainState,
            responseWindowState,
            submitDefenseActionRequest.requestId);
        if (shouldApplyT014OnDefensePostResolution && formalDefenseCardInstance is not null)
        {
            applyT014OnDefensePostResolution(
                gameState,
                actionChainState,
                submitDefenseActionRequest.requestId,
                actorPlayerState,
                formalDefenseCardInstance,
                producedEventsCountBeforeDamageResolution,
                defenderActiveCharacterInstanceIdBeforeResolution);
        }

        if (shouldApplyT015OnDefensePostResolution && formalDefenseCardInstance is not null)
        {
            applyT015OnDefensePostResolution(
                gameState,
                actionChainState,
                submitDefenseActionRequest.requestId,
                actorPlayerState,
                pendingDamageSourcePlayerId.Value,
                formalDefenseCardInstance,
                producedEventsCountBeforeDamageResolution,
                defenderActiveCharacterInstanceIdBeforeResolution);
        }

        if (shouldMoveDefenseCardToDiscardPostResolution && formalDefenseCardInstance is not null)
        {
            moveDefensePlacedCardToDiscardPostResolution(
                gameState,
                actionChainState,
                submitDefenseActionRequest.requestId,
                actorPlayerState,
                formalDefenseCardInstance);
        }

        tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
            gameState,
            actionChainState,
            submitDefenseActionRequest.requestId,
            producedEventsCountBeforeDamageResolution);
        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);

        return producedEvents;
    }

    private List<GameEvent> processSubmitDamageCounterActionRequest(
        GameState.GameState gameState,
        SubmitDamageCounterActionRequest submitDamageCounterActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires gameState.turnState to be initialized.");
        }

        var actionChainState = gameState.currentActionChain;
        if (actionChainState is null)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires an active currentActionChain.");
        }

        var responseWindowState = gameState.currentResponseWindow;
        if (responseWindowState is null || responseWindowState.windowTypeKey != "damageResponse")
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires an active damageResponse currentResponseWindow.");
        }

        if (submitDamageCounterActionRequest.responseWindowId != responseWindowState.responseWindowId)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest responseWindowId mismatch.");
        }

        if (actionChainState.pendingContinuationKey != ContinuationKeyStagedResponseDamage)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires currentActionChain.pendingContinuationKey to be continuation:stagedResponseDamage.");
        }

        var pendingDamageResponseStage = getDamageResponseStageOrThrow(
            responseWindowState,
            "SubmitDamageCounterActionRequest requires currentResponseWindow.pendingDamageResponseStageKey to be awaitCounter.");
        if (pendingDamageResponseStage != DamageResponseStage.awaitCounter)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires currentResponseWindow.pendingDamageResponseStageKey to be awaitCounter.");
        }

        if (!responseWindowState.currentResponderPlayerId.HasValue)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires currentResponseWindow.currentResponderPlayerId to be set.");
        }

        if (submitDamageCounterActionRequest.actorPlayerId != responseWindowState.currentResponderPlayerId.Value)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest actorPlayerId does not match currentResponseWindow.currentResponderPlayerId.");
        }

        var pendingDamageSourcePlayerId = responseWindowState.pendingDamageSourcePlayerId;
        if (!pendingDamageSourcePlayerId.HasValue)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires pending damage source player data in currentResponseWindow.");
        }

        var pendingDamageDefenderPlayerId = responseWindowState.pendingDamageDefenderPlayerId;
        if (!pendingDamageDefenderPlayerId.HasValue)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires pending damage defender data in currentResponseWindow.");
        }

        ensureDamageResponseStateConsistency(
            responseWindowState,
            pendingDamageResponseStage,
            pendingDamageDefenderPlayerId.Value,
            pendingDamageSourcePlayerId.Value,
            "SubmitDamageCounterActionRequest");

        if (submitDamageCounterActionRequest.actorPlayerId != pendingDamageSourcePlayerId.Value)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest actorPlayerId must equal pending damage source player.");
        }

        if (submitDamageCounterActionRequest.counterTypeKey != DamageCounterTypeKeyCancelFixedReduce1)
        {
            throw new NotSupportedException("SubmitDamageCounterActionRequest currently only supports counterTypeKey=cancelFixedReduce1.");
        }

        if (responseWindowState.pendingDamageDefenseDeclarationKey != DefenseTypeKeyFixedReduce1)
        {
            throw new InvalidOperationException("SubmitDamageCounterActionRequest requires currentResponseWindow.pendingDamageDefenseDeclarationKey to be fixedReduce1.");
        }

        responseWindowState.pendingDamageDefenseDeclarationKey = null;
        return closeDamageResponseWindowAndResolveDamage(
            gameState,
            actionChainState,
            responseWindowState,
            submitDamageCounterActionRequest.requestId);
    }

    private List<GameEvent> closeDamageResponseWindowAndResolveDamage(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        ResponseWindowState responseWindowState,
        long requestId)
    {
        var pendingDamageTargetCharacterInstanceId = responseWindowState.pendingDamageTargetCharacterInstanceId;
        if (!pendingDamageTargetCharacterInstanceId.HasValue)
        {
            throw new InvalidOperationException("Damage response resolution requires pending damage target data in currentResponseWindow.");
        }

        var pendingDamageBaseDamageValue = responseWindowState.pendingDamageBaseDamageValue;
        if (!pendingDamageBaseDamageValue.HasValue)
        {
            throw new InvalidOperationException("Damage response resolution requires pending damage value data in currentResponseWindow.");
        }

        var pendingDamageTypeKey = normalizePendingDamageTypeKey(responseWindowState.pendingDamageTypeKey);
        var defenseReduction = resolveDamageReductionFromWindowState(responseWindowState, pendingDamageTypeKey);
        var effectiveDamage = Math.Max(0, pendingDamageBaseDamageValue.Value - defenseReduction);

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = requestId,
            eventTypeKey = "responseWindowClosed",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "responseWindow",
            responseWindowId = responseWindowState.responseWindowId,
            responseWindowOriginType = responseWindowState.originType,
            isOpened = false,
        });

        gameState.currentResponseWindow = null;

        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(requestId),
            sourcePlayerId = responseWindowState.pendingDamageSourcePlayerId,
            sourceCardInstanceId = responseWindowState.pendingDamageSourceCardInstanceId,
            sourceCharacterInstanceId = responseWindowState.pendingDamageSourceCharacterInstanceId,
            killCreditPlayerIdOverride = responseWindowState.pendingDamageKillCreditPlayerIdOverride,
            targetCharacterInstanceId = pendingDamageTargetCharacterInstanceId.Value,
            baseDamageValue = effectiveDamage,
            damageType = pendingDamageTypeKey,
            defenseDeclarationKey = responseWindowState.pendingDamageDefenseDeclarationKey,
            hasAppliedSourceDamageBonuses = true,
        };

        var producedEventsStartIndex = actionChainState.producedEvents.Count;
        var isQueuedAoeDamage =
            shouldQueueC008ExcaliburDamage(actionChainState) ||
            shouldQueueC014RippleDamage(actionChainState);
        queueOrResolveStagedDamage(
            gameState,
            actionChainState,
            damageContext,
            requestId,
            producedEventsStartIndex);
        tryApplyC021BlackKeyFreeManaAfterDamage(
            gameState,
            actionChainState,
            requestId);
        if (!isQueuedAoeDamage)
        {
            tryApplyC011MysticEyesCharmAfterFullDefense(
                gameState,
                actionChainState,
                responseWindowState,
                damageContext,
                requestId);
            tryOpenC005SixRealmsAfterFullDefense(
                gameState,
                actionChainState,
                responseWindowState,
                damageContext,
                producedEventsStartIndex,
                requestId);
            tryOpenC004StardustCarnivalAfterDefense(
                gameState,
                actionChainState,
                responseWindowState,
                requestId);
        }
        var clearedStagedDamageContinuation = false;
        if (gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.Equals(
                actionChainState.pendingContinuationKey,
                ContinuationKeyStagedResponseDamage,
                StringComparison.Ordinal))
        {
            actionChainState.pendingContinuationKey = null;
            clearedStagedDamageContinuation = true;
        }

        if (clearedStagedDamageContinuation &&
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            tryResumeCharacterSkillSequences(gameState, actionChainState, requestId);
        }

        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);

        return actionChainState.producedEvents;
    }

    private static DamageResponseStage getDamageResponseStageOrThrow(
        ResponseWindowState responseWindowState,
        string invalidStageMessage)
    {
        return responseWindowState.pendingDamageResponseStageKey switch
        {
            DamageResponseStageAwaitDefense => DamageResponseStage.awaitDefense,
            DamageResponseStageAwaitCounter => DamageResponseStage.awaitCounter,
            _ => throw new InvalidOperationException(invalidStageMessage),
        };
    }

    private static void setDamageResponseStage(
        ResponseWindowState responseWindowState,
        DamageResponseStage stage,
        PlayerId responderPlayerId)
    {
        responseWindowState.pendingDamageResponseStageKey = stage switch
        {
            DamageResponseStage.awaitDefense => DamageResponseStageAwaitDefense,
            DamageResponseStage.awaitCounter => DamageResponseStageAwaitCounter,
            _ => throw new InvalidOperationException("Unsupported damageResponse stage."),
        };
        responseWindowState.currentResponderPlayerId = responderPlayerId;
    }

    private static void ensureDamageResponseStateConsistency(
        ResponseWindowState responseWindowState,
        DamageResponseStage stage,
        PlayerId pendingDamageDefenderPlayerId,
        PlayerId pendingDamageSourcePlayerId,
        string actionRequestTypeKey)
    {
        if (!responseWindowState.currentResponderPlayerId.HasValue)
        {
            throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.currentResponderPlayerId to be set.");
        }

        var pendingDamageDefenseDeclarationKey = responseWindowState.pendingDamageDefenseDeclarationKey;
        if (pendingDamageDefenseDeclarationKey is not null &&
            pendingDamageDefenseDeclarationKey != DefenseTypeKeyFixedReduce1 &&
            !tryParseCardDefenseDeclarationKey(
                pendingDamageDefenseDeclarationKey,
                out _,
                out _))
        {
            throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.pendingDamageDefenseDeclarationKey to be null, fixedReduce1, or a supported card defense declaration.");
        }

        if (stage == DamageResponseStage.awaitDefense)
        {
            if (responseWindowState.currentResponderPlayerId.Value != pendingDamageDefenderPlayerId)
            {
                throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.currentResponderPlayerId to match pending damage defender while stage is awaitDefense.");
            }

            if (pendingDamageDefenseDeclarationKey is not null)
            {
                throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.pendingDamageDefenseDeclarationKey to be null while stage is awaitDefense.");
            }

            return;
        }

        if (responseWindowState.currentResponderPlayerId.Value != pendingDamageSourcePlayerId)
        {
            throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.currentResponderPlayerId to match pending damage source player while stage is awaitCounter.");
        }

        if (pendingDamageDefenseDeclarationKey is null)
        {
            throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.pendingDamageDefenseDeclarationKey to be initialized while stage is awaitCounter.");
        }

        if (pendingDamageDefenseDeclarationKey != DefenseTypeKeyFixedReduce1 &&
            !tryParseCardDefenseDeclarationKey(
                pendingDamageDefenseDeclarationKey,
                out _,
                out _))
        {
            throw new InvalidOperationException($"{actionRequestTypeKey} requires currentResponseWindow.pendingDamageDefenseDeclarationKey to be fixedReduce1 or a supported card defense declaration while stage is awaitCounter.");
        }
    }

    private static int resolveDamageReductionFromWindowState(
        ResponseWindowState responseWindowState,
        string damageTypeKey)
    {
        var defenseDeclarationKey = responseWindowState.pendingDamageDefenseDeclarationKey;
        if (defenseDeclarationKey is null)
        {
            return 0;
        }

        if (defenseDeclarationKey == DefenseTypeKeyFixedReduce1)
        {
            return 1;
        }

        if (!tryParseCardDefenseDeclarationKey(
                defenseDeclarationKey,
                out var defenseTypeKey,
                out var defenseValue))
        {
            throw new InvalidOperationException("Damage response resolution only supports pendingDamageDefenseDeclarationKey null, fixedReduce1, or a supported card defense declaration.");
        }

        if (!isDefenseTypeMatchingDamageType(damageTypeKey, defenseTypeKey))
        {
            return 0;
        }

        return defenseValue;
    }

    private static bool isDefenseTypeMatchingDamageType(string damageTypeKey, string defenseTypeKey)
    {
        if (damageTypeKey == DamageTypeKeyPhysical)
        {
            return defenseTypeKey == DefenseTypeKeyPhysical || defenseTypeKey == DefenseTypeKeyDual;
        }

        if (damageTypeKey == DamageTypeKeySpell)
        {
            return defenseTypeKey == DefenseTypeKeySpell || defenseTypeKey == DefenseTypeKeyDual;
        }

        return false;
    }

    private static string normalizePendingDamageTypeKey(string? damageTypeKey)
    {
        if (string.IsNullOrWhiteSpace(damageTypeKey))
        {
            return DamageTypeKeyPhysical;
        }

        return damageTypeKey;
    }

    private static bool isFormalDefenseTypeKey(string defenseTypeKey)
    {
        return defenseTypeKey == DefenseTypeKeyPhysical ||
               defenseTypeKey == DefenseTypeKeySpell ||
               defenseTypeKey == DefenseTypeKeyDual;
    }

    private static bool isCardDefinitionDefenseTypeSupportingDeclaredDefenseType(
        string cardDefinitionDefenseTypeKey,
        string declaredDefenseTypeKey)
    {
        if (!isFormalDefenseTypeKey(declaredDefenseTypeKey))
        {
            return false;
        }

        if (cardDefinitionDefenseTypeKey == DefenseTypeKeyDual)
        {
            return true;
        }

        return cardDefinitionDefenseTypeKey == declaredDefenseTypeKey;
    }

    private static string createCardDefenseDeclarationKey(string defenseTypeKey, int defenseValue)
    {
        return $"{DefenseDeclarationPrefixCard}{defenseTypeKey}:{defenseValue}";
    }

    private static bool tryParseCardDefenseDeclarationKey(
        string defenseDeclarationKey,
        out string defenseTypeKey,
        out int defenseValue)
    {
        defenseTypeKey = string.Empty;
        defenseValue = 0;

        if (!defenseDeclarationKey.StartsWith(DefenseDeclarationPrefixCard, StringComparison.Ordinal))
        {
            return false;
        }

        var payload = defenseDeclarationKey.Substring(DefenseDeclarationPrefixCard.Length);
        var separatorIndex = payload.LastIndexOf(':');
        if (separatorIndex <= 0 || separatorIndex >= payload.Length - 1)
        {
            return false;
        }

        var parsedDefenseTypeKey = payload.Substring(0, separatorIndex);
        var parsedDefenseValueText = payload.Substring(separatorIndex + 1);
        if (!isFormalDefenseTypeKey(parsedDefenseTypeKey))
        {
            return false;
        }

        if (!int.TryParse(parsedDefenseValueText, out var parsedDefenseValue))
        {
            return false;
        }

        if (parsedDefenseValue < 0)
        {
            return false;
        }

        defenseTypeKey = parsedDefenseTypeKey;
        defenseValue = parsedDefenseValue;
        return true;
    }

    private List<GameEvent> processOpenInputContextActionRequest(
        GameState.GameState gameState,
        OpenInputContextActionRequest openInputContextActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("OpenInputContextActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("OpenInputContextActionRequest requires gameState.turnState to be initialized.");
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("OpenInputContextActionRequest cannot open while currentInputContext is active.");
        }

        var actionChainState = new ActionChainState
        {
            actionChainId = new ActionChainId(openInputContextActionRequest.requestId),
            actorPlayerId = openInputContextActionRequest.actorPlayerId,
            rootActionRequest = openInputContextActionRequest,
            isCompleted = false,
            currentFrameIndex = 0,
        };

        var effectFrame = new EffectFrame
        {
            effectKey = "openInputContext",
            sourcePlayerId = openInputContextActionRequest.actorPlayerId,
            contextKey = openInputContextActionRequest.contextKey,
        };

        actionChainState.effectFrames.Add(effectFrame);
        gameState.currentActionChain = actionChainState;

        var inputContextId = new InputContextId(Interlocked.Increment(ref nextInputContextNumericId));
        var inputContextState = new InputContextState
        {
            inputContextId = inputContextId,
            requiredPlayerId = openInputContextActionRequest.actorPlayerId,
            sourceActionChainId = actionChainState.actionChainId,
            inputTypeKey = openInputContextActionRequest.inputTypeKey,
            contextKey = openInputContextActionRequest.contextKey,
        };
        inputContextState.choiceKeys.AddRange(openInputContextActionRequest.choiceKeys);

        gameState.currentInputContext = inputContextState;

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = openInputContextActionRequest.requestId,
            eventTypeKey = "inputContextOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextId,
            isOpened = true,
        });

        actionChainState.currentFrameIndex = actionChainState.effectFrames.Count;
        return actionChainState.producedEvents;
    }

    private List<GameEvent> processSubmitInputChoiceActionRequest(
        GameState.GameState gameState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (gameState.matchState != GameState.MatchState.running)
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest requires gameState.matchState to be running.");
        }

        if (gameState.turnState is null)
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest requires gameState.turnState to be initialized.");
        }

        var actionChainState = gameState.currentActionChain;
        if (actionChainState is null)
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest requires an active currentActionChain.");
        }

        var inputContextState = gameState.currentInputContext;

        if (inputContextState is null)
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest requires an active currentInputContext.");
        }

        if (submitInputChoiceActionRequest.inputContextId != inputContextState.inputContextId)
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest inputContextId mismatch.");
        }

        var continuationState = new SubmitInputChoiceContinuationState(actionChainState.pendingContinuationKey);
        ensureSubmitInputChoiceActorAllowed(inputContextState, submitInputChoiceActionRequest.actorPlayerId);

        if (continuationState.isTreasureArrivalContinuation &&
            treasureArrivalEffectRuntime.isParallelTreasureArrivalInputContext(inputContextState))
        {
            var producedEventsCountBeforeParallelArrivalContinuation = actionChainState.producedEvents.Count;
            if (!treasureArrivalEffectRuntime.tryContinueOnSubmitInputChoice(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest treasure arrival continuation could not be resolved.");
            }

            tryApplyTreasureBanishEffectsFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeParallelArrivalContinuation);
            tryResumeCharacterSkillSequences(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);
            anomalyProcessor.tryFinalizeDeferredA005ResolveAfterExternalEffects(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);

            actionChainState.isCompleted =
                gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                actionChainState.pendingContinuationKey is null;
            return actionChainState.producedEvents;
        }

        if (continuationState.isAnomalyContinuation &&
            anomalyProcessor.isParallelAnomalyInputContext(inputContextState))
        {
            var producedEventsCountBeforeParallelAnomalyContinuation = actionChainState.producedEvents.Count;
            if (!anomalyProcessor.tryContinueParallelAnomalyInputChoice(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest,
                    continuationState.pendingContinuationKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest anomaly parallel continuation could not be resolved.");
            }

            tryApplyTreasureBanishEffectsFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeParallelAnomalyContinuation);
            tryOpenTreasureArrivalEffectsFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeParallelAnomalyContinuation);
            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeParallelAnomalyContinuation);
            tryResumeCharacterSkillSequences(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);

            actionChainState.isCompleted =
                gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                actionChainState.pendingContinuationKey is null;
            return actionChainState.producedEvents;
        }

        if (continuationState.isMechanicalJadeContinuation)
        {
            ensureValidSubmitInputChoiceByContinuationGroup(
                gameState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState);

            actionChainState.producedEvents.Add(new InteractionWindowEvent
            {
                eventId = submitInputChoiceActionRequest.requestId,
                eventTypeKey = "inputContextClosed",
                sourceActionChainId = actionChainState.actionChainId,
                windowKindKey = "inputContext",
                inputContextId = inputContextState.inputContextId,
                isOpened = false,
            });
            gameState.currentInputContext = null;
            if (!mechanicalJadeRuntime.tryContinueOnSubmitInputChoice(
                    gameState,
                    actionChainState,
                    submitInputChoiceActionRequest))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest Mechanical Jade continuation could not be resolved.");
            }
            tryResumeCharacterSkillSequences(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);

            actionChainState.isCompleted =
                gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                actionChainState.pendingContinuationKey is null;
            return actionChainState.producedEvents;
        }

        if (continuationState.isTreasureDefenseContinuation)
        {
            ensureValidSubmitInputChoiceByContinuationGroup(
                gameState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState);

            actionChainState.producedEvents.Add(new InteractionWindowEvent
            {
                eventId = submitInputChoiceActionRequest.requestId,
                eventTypeKey = "inputContextClosed",
                sourceActionChainId = actionChainState.actionChainId,
                windowKindKey = "inputContext",
                inputContextId = inputContextState.inputContextId,
                isOpened = false,
            });
            gameState.currentInputContext = null;

            continueT025ExtraDiscardDefenseAndResolveDamage(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest);
            tryResumeCharacterSkillSequences(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);

            actionChainState.isCompleted =
                gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                actionChainState.pendingContinuationKey is null;
            return actionChainState.producedEvents;
        }

        if (continuationState.isT029DamageImmunityContinuation)
        {
            ensureValidT029DamageImmunityChoiceRequest(inputContextState, submitInputChoiceActionRequest);

            var producedEventsCountBeforeT029Continuation = actionChainState.producedEvents.Count;
            actionChainState.producedEvents.Add(new InteractionWindowEvent
            {
                eventId = submitInputChoiceActionRequest.requestId,
                eventTypeKey = "inputContextClosed",
                sourceActionChainId = actionChainState.actionChainId,
                windowKindKey = "inputContext",
                inputContextId = inputContextState.inputContextId,
                isOpened = false,
            });
            inputContextState.selectedChoiceKey = submitInputChoiceActionRequest.choiceKey;
            gameState.currentInputContext = null;

            continueT029DamageImmunityChoice(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);

            tryApplyTreasureBanishEffectsFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeT029Continuation);
            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeT029Continuation);
            tryResumeCharacterSkillSequences(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);

            actionChainState.isCompleted =
                gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                actionChainState.pendingContinuationKey is null;
            return actionChainState.producedEvents;
        }

        var shouldSkipSelectedChoiceKeyAssignmentForAnomalyContinuation =
            ensureValidSubmitInputChoiceByContinuationGroup(
                gameState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState);

        if (continuationState.isC014SkillContinuation &&
            string.Equals(continuationState.pendingContinuationKey, ContinuationKeyC014AndThenDiscard, StringComparison.Ordinal) &&
            submitInputChoiceActionRequest.choiceKeys.Count > 0)
        {
            inputContextState.selectedChoiceKey = string.Join(",", submitInputChoiceActionRequest.choiceKeys);
        }
        else if (!continuationState.isTurnStartShackleDiscardContinuation &&
            !(continuationState.isAnomalyContinuation &&
              shouldSkipSelectedChoiceKeyAssignmentForAnomalyContinuation))
        {
            inputContextState.selectedChoiceKey = submitInputChoiceActionRequest.choiceKey;
        }

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = submitInputChoiceActionRequest.requestId,
            eventTypeKey = "inputContextClosed",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextState.inputContextId,
            isOpened = false,
        });
        var producedEventsCountBeforeContinuation = actionChainState.producedEvents.Count;

        var shouldContinueScriptedOnPlayChooseDamage =
            continuationState.isInputChoiceDamageContinuation &&
            submitInputChoiceActionRequest.choiceKey == TemporaryOnPlayProbeResolver.ScriptedOnPlayDeal1ChoiceKey;

        gameState.currentInputContext = null;

        if (tryContinueSubmitInputChoiceByContinuationGroup(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState))
        {
            tryApplyTreasureBanishEffectsFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeContinuation);
            tryOpenTreasureArrivalEffectsFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeContinuation);
            tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                producedEventsCountBeforeContinuation);
            if (isA010ContinuationThatMayHaveDeferredMechanicalJade(continuationState.pendingContinuationKey))
            {
                tryOpenMechanicalJadeOverlayOnlyFromProducedEvents(
                    gameState,
                    actionChainState,
                    submitInputChoiceActionRequest.requestId,
                    0);
            }
            tryResumeCharacterSkillSequences(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);
            anomalyProcessor.tryFinalizeDeferredA005ResolveAfterExternalEffects(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);
            actionChainState.isCompleted =
                gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                actionChainState.pendingContinuationKey is null;
            return actionChainState.producedEvents;
        }

        if (continuationState.isTreasureArrivalContinuation)
        {
            if (!treasureArrivalEffectRuntime.tryContinueOnSubmitInputChoice(
                    gameState,
                    actionChainState,
                    inputContextState,
                    submitInputChoiceActionRequest))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest treasure arrival continuation could not be resolved.");
            }
        }
        else if (continuationState.isTreasureOnPlayContinuation)
        {
            if (!treasureOnPlayEffectRuntime.tryContinueOnSubmitInputChoice(
                    gameState,
                    actionChainState,
                    submitInputChoiceActionRequest))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest treasure on-play continuation could not be resolved.");
            }
        }

        if (shouldContinueScriptedOnPlayChooseDamage)
        {
            var targetCharacterInstanceId = findDeterministicScriptedDamageTarget(
                gameState,
                submitInputChoiceActionRequest.actorPlayerId);
            appendDamageEventsToExistingActionChain(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                null,
                targetCharacterInstanceId,
                1);
            actionChainState.pendingContinuationKey = null;
        }

        tryApplyTreasureBanishEffectsFromProducedEvents(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            producedEventsCountBeforeContinuation);
        tryOpenTreasureArrivalEffectsFromProducedEvents(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            producedEventsCountBeforeContinuation);
        tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            producedEventsCountBeforeContinuation);
        tryResumeCharacterSkillSequences(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId);
        anomalyProcessor.tryFinalizeDeferredA005ResolveAfterExternalEffects(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId);

        actionChainState.isCompleted =
            gameState.currentInputContext is null &&
            gameState.currentResponseWindow is null &&
            actionChainState.pendingContinuationKey is null;
        return actionChainState.producedEvents;
    }

    private void tryOpenMechanicalJadeOverlayAfterKillFromProducedEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        int producedEventsStartIndex)
    {
        if (anomalyProcessor.tryOpenA010KillBanishSetAsideInputFromProducedEvents(
                gameState,
                actionChainState,
                eventId,
                producedEventsStartIndex))
        {
            return;
        }

        mechanicalJadeRuntime.tryOpenOverlayAfterKillInputContextFromProducedEvents(
            gameState,
            actionChainState,
            eventId,
            producedEventsStartIndex);
    }

    private static bool isA010ContinuationThatMayHaveDeferredMechanicalJade(string? pendingContinuationKey)
    {
        return string.Equals(
                   pendingContinuationKey,
                   AnomalyProcessor.ContinuationKeyA010KillBanishSetAside,
                   StringComparison.Ordinal) ||
               string.Equals(
                   pendingContinuationKey,
                   AnomalyProcessor.ContinuationKeyA010RewardChooseTwo,
                   StringComparison.Ordinal) ||
               string.Equals(
                   pendingContinuationKey,
                   AnomalyProcessor.ContinuationKeyA010RewardSelectSummonToHand,
                   StringComparison.Ordinal);
    }

    private void tryOpenMechanicalJadeOverlayOnlyFromProducedEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        int producedEventsStartIndex)
    {
        mechanicalJadeRuntime.tryOpenOverlayAfterKillInputContextFromProducedEvents(
            gameState,
            actionChainState,
            eventId,
            producedEventsStartIndex);
    }

    private void tryApplyTreasureBanishEffectsFromProducedEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        int producedEventsStartIndex)
    {
        treasureBanishEffectRuntime.applyBanishEffectsFromProducedEvents(
            gameState,
            actionChainState,
            eventId,
            producedEventsStartIndex);
    }

    private void continueT025ExtraDiscardDefenseAndResolveDamage(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var responseWindowState = gameState.currentResponseWindow;
        if (responseWindowState is null || responseWindowState.windowTypeKey != "damageResponse")
        {
            throw new InvalidOperationException("T025 extra-discard defense continuation requires an active damageResponse currentResponseWindow.");
        }

        if (!tryParseCardDefenseDeclarationKey(
                responseWindowState.pendingDamageDefenseDeclarationKey ?? string.Empty,
                out var defenseTypeKey,
                out var defenseValue))
        {
            throw new InvalidOperationException("T025 extra-discard defense continuation requires a pending card defense declaration.");
        }

        var extraDiscardCount = treasureDefenseEffectRuntime.continueT025ExtraDiscard(
            gameState,
            actionChainState,
            inputContextState,
            submitInputChoiceActionRequest);

        responseWindowState.pendingDamageDefenseDeclarationKey = createCardDefenseDeclarationKey(
            defenseTypeKey,
            defenseValue + extraDiscardCount);

        closeDamageResponseWindowAndResolveDamage(
            gameState,
            actionChainState,
            responseWindowState,
            submitInputChoiceActionRequest.requestId);
    }

    private void continueT029DamageImmunityChoice(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var damageContext = loadT029PendingDamageContextOrThrow(actionChainState);
        clearT029PendingDamageContext(actionChainState);

        if (!string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                DamageProcessor.ChoiceKeyT029Decline,
                StringComparison.Ordinal))
        {
            var t029CardInstanceId = parseT029BanishChoiceKey(submitInputChoiceActionRequest.choiceKey);
            if (gameState.publicState is null)
            {
                throw new InvalidOperationException("T029 damage immunity continuation requires gameState.publicState.");
            }

            if (!gameState.cardInstances.TryGetValue(t029CardInstanceId, out var t029CardInstance))
            {
                throw new InvalidOperationException("T029 damage immunity continuation requires selected cardInstanceId to exist.");
            }

            if (!gameState.players.TryGetValue(submitInputChoiceActionRequest.actorPlayerId, out var actorPlayerState))
            {
                throw new InvalidOperationException("T029 damage immunity continuation requires actorPlayerId to exist.");
            }

            if (t029CardInstance.ownerPlayerId != submitInputChoiceActionRequest.actorPlayerId ||
                t029CardInstance.zoneId != actorPlayerState.handZoneId ||
                !string.Equals(t029CardInstance.definitionId, "T029", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("T029 damage immunity continuation requires selected T029 to be in actor hand zone.");
            }

            var movedEvent = zoneMovementService.moveCard(
                gameState,
                t029CardInstance,
                gameState.publicState.gapZoneId,
                CardMoveReason.banish,
                actionChainState.actionChainId,
                submitInputChoiceActionRequest.requestId);
            actionChainState.producedEvents.Add(movedEvent);
            damageContext.isPrevented = true;
            damageContext.isImmune = true;
        }

        damageContext.suppressT029DamageImmunityPrompt = true;
        var producedEventsStartIndex = actionChainState.producedEvents.Count;
        actionChainState.pendingContinuationKey = null;
        queueOrResolveStagedDamage(
            gameState,
            actionChainState,
            damageContext,
            submitInputChoiceActionRequest.requestId,
            producedEventsStartIndex);
    }

    private static DamageContext loadT029PendingDamageContextOrThrow(ActionChainState actionChainState)
    {
        var damageContext = new DamageContext
        {
            damageContextId = new DamageContextId(loadLongLocalState(actionChainState, DamageProcessor.LocalStateKeyT029DamageContextId)),
            targetCharacterInstanceId = new CharacterInstanceId(loadLongLocalState(actionChainState, DamageProcessor.LocalStateKeyT029TargetCharacterInstanceId)),
            baseDamageValue = (int)loadLongLocalState(actionChainState, DamageProcessor.LocalStateKeyT029BaseDamageValue),
            damageType = loadStringLocalState(actionChainState, DamageProcessor.LocalStateKeyT029DamageType),
            hasAppliedSourceDamageBonuses =
                actionChainState.localState.TryGetValue(
                    DamageProcessor.LocalStateKeyT029HasAppliedSourceDamageBonuses,
                    out var hasAppliedSourceDamageBonusesRaw) &&
                bool.TryParse(hasAppliedSourceDamageBonusesRaw, out var hasAppliedSourceDamageBonuses) &&
                hasAppliedSourceDamageBonuses,
        };

        if (tryLoadLongLocalState(actionChainState, DamageProcessor.LocalStateKeyT029SourcePlayerId, out var sourcePlayerNumericId))
        {
            damageContext.sourcePlayerId = new PlayerId(sourcePlayerNumericId);
        }

        if (tryLoadLongLocalState(actionChainState, DamageProcessor.LocalStateKeyT029SourceCardInstanceId, out var sourceCardNumericId))
        {
            damageContext.sourceCardInstanceId = new CardInstanceId(sourceCardNumericId);
        }

        if (tryLoadLongLocalState(actionChainState, DamageProcessor.LocalStateKeyT029SourceCharacterInstanceId, out var sourceCharacterNumericId))
        {
            damageContext.sourceCharacterInstanceId = new CharacterInstanceId(sourceCharacterNumericId);
        }

        if (actionChainState.localState.TryGetValue(
                DamageProcessor.LocalStateKeyT029DefenseDeclarationKey,
                out var defenseDeclarationKey))
        {
            damageContext.defenseDeclarationKey = defenseDeclarationKey;
        }

        return damageContext;
    }

    private static void clearT029PendingDamageContext(ActionChainState actionChainState)
    {
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029DamageContextId);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029SourcePlayerId);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029SourceCardInstanceId);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029SourceCharacterInstanceId);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029TargetCharacterInstanceId);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029BaseDamageValue);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029DamageType);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029DefenseDeclarationKey);
        actionChainState.localState.Remove(DamageProcessor.LocalStateKeyT029HasAppliedSourceDamageBonuses);
    }

    private static long loadLongLocalState(ActionChainState actionChainState, string key)
    {
        if (!tryLoadLongLocalState(actionChainState, key, out var value))
        {
            throw new InvalidOperationException($"T029 damage immunity continuation requires localState[{key}].");
        }

        return value;
    }

    private static bool tryLoadLongLocalState(ActionChainState actionChainState, string key, out long value)
    {
        value = 0;
        return actionChainState.localState.TryGetValue(key, out var serializedValue) &&
               long.TryParse(serializedValue, out value);
    }

    private static int loadOptionalIntLocalState(ActionChainState actionChainState, string key)
    {
        return actionChainState.localState.TryGetValue(key, out var serializedValue) &&
               int.TryParse(serializedValue, out var value)
            ? value
            : 0;
    }

    private static string loadStringLocalState(ActionChainState actionChainState, string key)
    {
        if (!actionChainState.localState.TryGetValue(key, out var value) ||
            string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"T029 damage immunity continuation requires localState[{key}].");
        }

        return value;
    }

    private static CardInstanceId parseT029BanishChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(DamageProcessor.ChoiceKeyT029BanishPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T029 damage immunity continuation requires choiceKey to be T029:decline or T029:banish:{cardId}.");
        }

        var cardIdSegment = choiceKey.Substring(DamageProcessor.ChoiceKeyT029BanishPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId) || cardNumericId <= 0)
        {
            throw new InvalidOperationException("T029 damage immunity continuation requires numeric T029 cardInstanceId.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private void tryOpenTreasureArrivalEffectsFromProducedEvents(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        int producedEventsStartIndex)
    {
        if (producedEventsStartIndex < 0 ||
            producedEventsStartIndex >= actionChainState.producedEvents.Count)
        {
            return;
        }

        if (gameState.currentInputContext is not null ||
            gameState.currentResponseWindow is not null ||
            !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey))
        {
            return;
        }

        var enteredSummonZoneCardInstanceIds = new List<CardInstanceId>();
        for (var eventIndex = producedEventsStartIndex; eventIndex < actionChainState.producedEvents.Count; eventIndex++)
        {
            if (actionChainState.producedEvents[eventIndex] is not CardMovedEvent cardMovedEvent)
            {
                continue;
            }

            if (cardMovedEvent.toZoneKey != ZoneKey.summonZone)
            {
                continue;
            }

            enteredSummonZoneCardInstanceIds.Add(cardMovedEvent.cardInstanceId);
        }

        if (enteredSummonZoneCardInstanceIds.Count == 0)
        {
            return;
        }

        treasureArrivalEffectRuntime.tryStartArrivalEffectsForEnteredSummonZoneCards(
            gameState,
            actionChainState,
            eventId,
            enteredSummonZoneCardInstanceIds);
    }

    private bool tryOpenTreasureArrivalEffectsFromProducedEventsForAnomaly(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long eventId,
        int producedEventsStartIndex)
    {
        tryOpenTreasureArrivalEffectsFromProducedEvents(
            gameState,
            actionChainState,
            eventId,
            producedEventsStartIndex);
        return gameState.currentInputContext is not null ||
               gameState.currentResponseWindow is not null ||
               !string.IsNullOrWhiteSpace(actionChainState.pendingContinuationKey);
    }

    private bool ensureValidSubmitInputChoiceByContinuationGroup(
        GameState.GameState gameState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        SubmitInputChoiceContinuationState continuationState)
    {
        if (continuationState.isTurnStartShackleDiscardContinuation)
        {
            if (!TurnTransitionProcessor.isValidTurnStartShackleDiscardChoiceRequest(
                    inputContextState,
                    submitInputChoiceActionRequest))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest requires either choiceKey=shackle:decline, or choiceKeys to contain exactly four unique values from currentInputContext.choiceKeys for continuation:turnStartShackleDiscard.");
            }

            return false;
        }

        if (continuationState.isTurnStartC001BarrierContinuation)
        {
            if (!TurnTransitionProcessor.isValidTurnStartC001BarrierChoiceRequest(
                    inputContextState,
                    submitInputChoiceActionRequest))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest requires choiceKey to be one of currentInputContext.choiceKeys for continuation:turnStartC001Barrier.");
            }

            return false;
        }

        if (continuationState.isTurnStartC013MechanicalJadeContinuation)
        {
            if (!TurnTransitionProcessor.isValidTurnStartC013MechanicalJadeChoiceRequest(
                    inputContextState,
                    submitInputChoiceActionRequest))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C013 turn-start choice must decline or select exactly two hand cards.");
            }

            return false;
        }

        if (continuationState.isC001SkillContinuation)
        {
            if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C001 target choice must be one of currentInputContext.choiceKeys.");
            }

            return false;
        }

        if (continuationState.isC003SkillContinuation)
        {
            if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C003 choice must be one of currentInputContext.choiceKeys.");
            }

            return false;
        }

        if (continuationState.isC004SkillContinuation)
        {
            if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C004 choice must be one of currentInputContext.choiceKeys.");
            }

            return false;
        }

        if (continuationState.isC005SkillContinuation)
        {
            if (!isValidC005ChoiceRequest(inputContextState, submitInputChoiceActionRequest, continuationState.pendingContinuationKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C005 choice must match the current C005 input context.");
            }

            return false;
        }

        if (continuationState.isC005SixRealmsContinuation)
        {
            if (!string.Equals(inputContextState.contextKey, ContextKeyC005SixRealmsChoice, StringComparison.Ordinal) ||
                !inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey) ||
                (submitInputChoiceActionRequest.choiceKey != ChoiceKeyDecline &&
                 submitInputChoiceActionRequest.choiceKey != ChoiceKeyC005SixRealmsUse) ||
                submitInputChoiceActionRequest.choiceKeys.Count != 0)
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C005:2 choice must be decline or c005SixRealms:use.");
            }

            return false;
        }

        if (continuationState.isC007SkillContinuation)
        {
            if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C007 target choice must be one of currentInputContext.choiceKeys.");
            }

            return false;
        }

        if (continuationState.isC008SkillContinuation)
        {
            if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C008 choice must be one of currentInputContext.choiceKeys.");
            }

            return false;
        }

        if (continuationState.isC011SkillContinuation)
        {
            if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C011 choice must be one of currentInputContext.choiceKeys.");
            }

            return false;
        }

        if (continuationState.isC013SkillContinuation)
        {
            if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C013 choice must be one of currentInputContext.choiceKeys.");
            }

            return false;
        }

        if (continuationState.isC021SkillContinuation)
        {
            if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C021 choice must be one of currentInputContext.choiceKeys.");
            }

            return false;
        }

        if (continuationState.isC022SkillContinuation)
        {
            if (!isValidC022ChoiceRequest(
                    inputContextState,
                    submitInputChoiceActionRequest,
                    continuationState.pendingContinuationKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C022 choice must match the current C022 input context.");
            }

            return false;
        }

        if (continuationState.isC014SkillContinuation)
        {
            if (string.Equals(continuationState.pendingContinuationKey, ContinuationKeyC014AndThenDiscard, StringComparison.Ordinal))
            {
                var selectedChoiceKeys = submitInputChoiceActionRequest.choiceKeys.Count > 0
                    ? submitInputChoiceActionRequest.choiceKeys
                    : new List<string> { submitInputChoiceActionRequest.choiceKey };
                if (selectedChoiceKeys.Count == 0 ||
                    selectedChoiceKeys.Any(choiceKey => !inputContextState.choiceKeys.Contains(choiceKey)))
                {
                    throw new InvalidOperationException("SubmitInputChoiceActionRequest C014 multi-discard choices must be current input choices.");
                }
            }
            else if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C014 choice must be one of currentInputContext.choiceKeys.");
            }

            return false;
        }

        if (continuationState.isC031SkillContinuation)
        {
            if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
            {
                throw new InvalidOperationException("SubmitInputChoiceActionRequest C031 choice must be one of currentInputContext.choiceKeys.");
            }

            return false;
        }

        if (continuationState.isAnomalyContinuation)
        {
            return anomalyProcessor.ensureValidAnomalyContinuationBeforeClosingInputContext(
                gameState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey);
        }

        if (TreasureOnPlayEffectRuntime.isT021OverlayCardsContinuationKey(continuationState.pendingContinuationKey))
        {
            TreasureOnPlayEffectRuntime.ensureValidT021OverlayChoiceRequest(
                inputContextState,
                submitInputChoiceActionRequest);
            return false;
        }

        if (continuationState.isMechanicalJadeContinuation)
        {
            MechanicalJadeRuntime.ensureValidOverlayAfterKillChoiceRequest(
                inputContextState,
                submitInputChoiceActionRequest);
            return false;
        }

        if (continuationState.isTreasureDefenseContinuation)
        {
            TreasureDefenseEffectRuntime.ensureValidT025ExtraDiscardChoiceRequest(
                inputContextState,
                submitInputChoiceActionRequest);
            return false;
        }

        if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
        {
            throw new InvalidOperationException("SubmitInputChoiceActionRequest choiceKey is not allowed by currentInputContext.choiceKeys.");
        }

        return false;
    }

    private static void ensureValidT029DamageImmunityChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!string.Equals(
                inputContextState.contextKey,
                DamageProcessor.ContextKeyT029DamageImmunity,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T029 damage immunity continuation requires currentInputContext.contextKey to match T029 immunity context.");
        }

        if (inputContextState.requiredPlayerId != submitInputChoiceActionRequest.actorPlayerId)
        {
            throw new InvalidOperationException("T029 damage immunity continuation requires actorPlayerId to match currentInputContext.requiredPlayerId.");
        }

        if (!inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey))
        {
            throw new InvalidOperationException("T029 damage immunity continuation requires choiceKey to be one of currentInputContext.choiceKeys.");
        }
    }

    private static bool isValidC022ChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        string? continuationKey)
    {
        if (string.Equals(continuationKey, ContinuationKeyC022DollWarTargetOptions, StringComparison.Ordinal) ||
            string.Equals(continuationKey, ContinuationKeyC022DollWarTargetDiscard, StringComparison.Ordinal))
        {
            var selectedChoiceKeys = submitInputChoiceActionRequest.choiceKeys.Count > 0
                ? submitInputChoiceActionRequest.choiceKeys
                : string.IsNullOrWhiteSpace(submitInputChoiceActionRequest.choiceKey)
                    ? new List<string>()
                    : new List<string> { submitInputChoiceActionRequest.choiceKey };

            return selectedChoiceKeys.Count > 0 &&
                   selectedChoiceKeys.Distinct(StringComparer.Ordinal).Count() == selectedChoiceKeys.Count &&
                   selectedChoiceKeys.All(choiceKey => inputContextState.choiceKeys.Contains(choiceKey));
        }

        return submitInputChoiceActionRequest.choiceKeys.Count == 0 &&
               inputContextState.choiceKeys.Contains(submitInputChoiceActionRequest.choiceKey);
    }

    private static void ensureSubmitInputChoiceActorAllowed(
        InputContextState inputContextState,
        PlayerId actorPlayerId)
    {
        if (inputContextState.requiredPlayerIds.Count > 0)
        {
            for (var index = 0; index < inputContextState.requiredPlayerIds.Count; index++)
            {
                if (inputContextState.requiredPlayerIds[index] == actorPlayerId)
                {
                    return;
                }
            }

            throw new InvalidOperationException("SubmitInputChoiceActionRequest actorPlayerId is not in currentInputContext.requiredPlayerIds.");
        }

        if (inputContextState.requiredPlayerId.HasValue &&
            actorPlayerId == inputContextState.requiredPlayerId.Value)
        {
            return;
        }

        throw new InvalidOperationException("SubmitInputChoiceActionRequest actorPlayerId does not match currentInputContext.requiredPlayerId.");
    }

    private bool tryContinueSubmitInputChoiceByContinuationGroup(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        SubmitInputChoiceContinuationState continuationState)
    {
        if (continuationState.isEndPhaseHandDiscardContinuation)
        {
            endPhaseProcessor.continueEndPhaseHandDiscardContinuation(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (continuationState.isTurnStartShackleDiscardContinuation)
        {
            turnTransitionProcessor.continueTurnStartShackleDiscardContinuation(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (continuationState.isTurnStartC001BarrierContinuation)
        {
            turnTransitionProcessor.continueTurnStartC001BarrierContinuation(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (continuationState.isTurnStartC013MechanicalJadeContinuation)
        {
            turnTransitionProcessor.continueTurnStartC013MechanicalJadeContinuation(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (continuationState.isC001SkillContinuation)
        {
            continueC001SkillInput(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isC003SkillContinuation)
        {
            continueC003SkillInput(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isC004SkillContinuation)
        {
            continueC004SkillInput(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isC005SkillContinuation)
        {
            continueC005SkillInput(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isC005SixRealmsContinuation)
        {
            continueC005SixRealmsChoice(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (continuationState.isC007SkillContinuation)
        {
            continueC007SkillInput(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isC008SkillContinuation)
        {
            continueC008SkillInput(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isC011SkillContinuation)
        {
            continueC011SkillInput(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isC013SkillContinuation)
        {
            continueC013SkillInput(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isC021SkillContinuation)
        {
            continueC021SkillInput(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isC022SkillContinuation)
        {
            continueC022SkillInput(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest,
                inputContextState,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isC014SkillContinuation)
        {
            continueC014SkillInput(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isC031SkillContinuation)
        {
            continueC031SkillInput(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey!);
            return true;
        }

        if (continuationState.isAnomalyContinuation)
        {
            anomalyProcessor.continueAnomalyContinuation(
                gameState,
                actionChainState,
                inputContextState,
                submitInputChoiceActionRequest,
                continuationState.pendingContinuationKey);
            if (string.Equals(
                    continuationState.pendingContinuationKey,
                    AnomalyProcessor.ContinuationKeyA010ArrivalSetAside,
                    StringComparison.Ordinal))
            {
                treasureArrivalEffectRuntime.resumeArrivalQueueAfterExternalContinuation(
                    gameState,
                    actionChainState,
                    submitInputChoiceActionRequest.requestId);
            }

            anomalyProcessor.tryFinalizeDeferredA005ResolveAfterExternalEffects(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId);

            return true;
        }

        return false;
    }
}

