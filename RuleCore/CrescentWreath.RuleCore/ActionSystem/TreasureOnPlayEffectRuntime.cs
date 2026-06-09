using System;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.RuleCore.DamageSystem;
using CrescentWreath.RuleCore.Definitions;
using CrescentWreath.RuleCore.EffectSystem;
using CrescentWreath.RuleCore.Entities;
using CrescentWreath.RuleCore.Events;
using CrescentWreath.RuleCore.GameState;
using CrescentWreath.RuleCore.Ids;
using CrescentWreath.RuleCore.ResponseSystem;
using CrescentWreath.RuleCore.StatusSystem;
using CrescentWreath.RuleCore.Zones;

namespace CrescentWreath.RuleCore.ActionSystem;

public sealed class TreasureOnPlayEffectRuntime
{
    public const string InputTypeKeyTreasureOnPlayTargetPlayerChoice = "treasureOnPlayTargetPlayerChoice";
    public const string InputTypeKeyTreasureOnPlayOptionalBanishChoice = "treasureOnPlayOptionalBanishChoice";
    public const string InputTypeKeyTreasureOnPlayDiscardCardChoice = "treasureOnPlayDiscardCardChoice";
    public const string InputTypeKeyTreasureOnPlaySummonZoneBanishChoice = "treasureOnPlaySummonZoneBanishChoice";
    public const string InputTypeKeyTreasureOnPlayOptionalMoveToHandChoice = "treasureOnPlayOptionalMoveToHandChoice";
    public const string InputTypeKeyTreasureOnPlayDeclareCardNameChoice = "treasureOnPlayDeclareCardNameChoice";
    public const string InputTypeKeyTreasureOnPlayOverlayCardsChoice = "treasureOnPlayOverlayCardsChoice";
    public const string InputTypeKeyTreasureOnPlayTargetCharacterChoice = "treasureOnPlayTargetCharacterChoice";
    public const string InputTypeKeyTreasureOnPlaySummonZoneCardChoice = "treasureOnPlaySummonZoneCardChoice";
    public const string InputTypeKeyTreasureOnPlayMarkerGrantChoice = "treasureOnPlayMarkerGrantChoice";
    public const string ChoiceKeyPlayerPrefix = "player:";
    public const string ChoiceKeyCharacterPrefix = "character:";
    public const string ChoiceKeyDiscardCardPrefix = "discardCard:";
    public const string ChoiceKeyBanishCardPrefix = "banishCard:";
    public const string ChoiceKeySummonZoneCardPrefix = "summonZoneCard:";
    public const string ChoiceKeyDeclareCardNamePrefix = "declareCardName:";
    public const string ChoiceKeyOverlayCardPrefix = "overlayCard:";
    public const string ChoiceKeyDeclineOptionalBanish = "banish:decline";
    public const string ChoiceKeyDeclineDiscard = "discard:decline";
    public const string ChoiceKeyDeclineOverlay = "overlay:decline";
    public const string ChoiceKeyMarkerGrantPrefix = "markerGrant:";
    public const string ChoiceKeyDeclineMarkerGrant = "markerGrant:decline";
    public const string ChoiceKeyT007GiveToAllyPrefix = "giveToAlly:";
    public const string ChoiceKeyT007AllyDiscardForManaPrefix = "allyDiscardForMana:";
    public const string ChoiceKeyAcceptMoveToHand = "moveToHand:accept";
    public const string ChoiceKeyDeclineMoveToHand = "moveToHand:decline";

    public const string ContinuationKeyT002OnPlayTargetHeal1 = "continuation:treasureOnPlay:T002:onPlayTargetHeal1";
    public const string ContinuationKeyT008OnPlayTargetDirectDamage1 = "continuation:treasureOnPlay:T008:onPlayTargetDirectDamage1";
    public const string ContinuationKeyT009OnPlayTargetOpponentSeal = "continuation:treasureOnPlay:T009:onPlayTargetOpponentSeal";
    public const string ContinuationKeyT022OnPlayOptionalBanishThenSelfShackle = "continuation:treasureOnPlay:T022:onPlayOptionalBanishThenSelfShackle";
    public const string ContinuationKeyT027OnPlayDraw2Discard2Step1 = "continuation:treasureOnPlay:T027:onPlayDraw2Discard2Step1";
    public const string ContinuationKeyT027OnPlayDraw2Discard2Step2 = "continuation:treasureOnPlay:T027:onPlayDraw2Discard2Step2";
    public const string ContinuationKeyT001OnPlayTargetOpponentSilenceAndDraw1 = "continuation:treasureOnPlay:T001:onPlayTargetOpponentSilenceAndDraw1";
    public const string ContinuationKeyT003OnPlayOptionalBanishStep = "continuation:treasureOnPlay:T003:onPlayOptionalBanishStep";
    public const string ContinuationKeyT003OnPlayTargetOpponentPhysicalDamage2 = "continuation:treasureOnPlay:T003:onPlayTargetOpponentPhysicalDamage2";
    public const string ContinuationKeyT004OnPlayTargetRemoveShackleOrSealStep = "continuation:treasureOnPlay:T004:onPlayTargetRemoveShackleOrSealStep";
    public const string ContinuationKeyT004OnPlayTargetOpponentPhysicalDamage3 = "continuation:treasureOnPlay:T004:onPlayTargetOpponentPhysicalDamage3";
    public const string ContinuationKeyT006OnPlayOptionalBanishFromSummonZone = "continuation:treasureOnPlay:T006:onPlayOptionalBanishFromSummonZone";
    public const string ContinuationKeyT011OnPlayTargetOpponentSpellDamage3 = "continuation:treasureOnPlay:T011:onPlayTargetOpponentSpellDamage3";
    public const string ContinuationKeyT012OnPlayOptionalBanishFromSummonZone = "continuation:treasureOnPlay:T012:onPlayOptionalBanishFromSummonZone";
    public const string ContinuationKeyT005OnPlaySelectTargetOpponentDiscard = "continuation:treasureOnPlay:T005:onPlaySelectTargetOpponentDiscard";
    public const string ContinuationKeyT005OnPlayTargetOpponentDiscardCard = "continuation:treasureOnPlay:T005:onPlayTargetOpponentDiscardCard";
    public const string ContinuationKeyT010OnPlaySelectDiscardToDeckTop = "continuation:treasureOnPlay:T010:onPlaySelectDiscardToDeckTop";
    public const string ContinuationKeyT010OnPlayOptionalMoveSelectedToHand = "continuation:treasureOnPlay:T010:onPlayOptionalMoveSelectedToHand";
    public const string ContinuationKeyT018OnPlayGainSkillPointThenBanish1 = "continuation:treasureOnPlay:T018:onPlayGainSkillPointThenBanish1";
    public const string ContinuationKeyT019OnPlayTargetOpponentSpellDamage2 = "continuation:treasureOnPlay:T019:onPlayTargetOpponentSpellDamage2";
    public const string ContinuationKeyT014OnPlayTargetFriendlyBarrier = "continuation:treasureOnPlay:T014:onPlayTargetFriendlyBarrier";
    public const string ContinuationKeyT015OnPlayTargetOpponentSilence = "continuation:treasureOnPlay:T015:onPlayTargetOpponentSilence";
    public const string ContinuationKeyT007OnPlayChooseAllyMode = "continuation:treasureOnPlay:T007:onPlayChooseAllyMode";
    public const string ContinuationKeyT007OnPlayTargetAllyDiscardCard = "continuation:treasureOnPlay:T007:onPlayTargetAllyDiscardCard";
    public const string ContinuationKeyT013OnPlayDeclareCardName = "continuation:treasureOnPlay:T013:onPlayDeclareCardName";
    public const string ContinuationKeyT013OnPlayTargetOpponentCharm = "continuation:treasureOnPlay:T013:onPlayTargetOpponentCharm";
    public const string ContinuationKeyT021OnPlayOverlayCardsForMana = "continuation:treasureOnPlay:T021:onPlayOverlayCardsForMana";
    public const string ContinuationKeyT023OnPlayTargetHumanHeal2 = "continuation:treasureOnPlay:T023:onPlayTargetHumanHeal2";
    public const string ContinuationKeyT023OnPlayTargetEnemyNonHumanDirectDamage1 = "continuation:treasureOnPlay:T023:onPlayTargetEnemyNonHumanDirectDamage1";
    public const string ContinuationKeyT017OnPlaySelectSummonZoneCard = "continuation:treasureOnPlay:T017:onPlaySelectSummonZoneCard";
    public const string ContinuationKeyT017OnPlayTargetFriendlyDeckTop = "continuation:treasureOnPlay:T017:onPlayTargetFriendlyDeckTop";
    public const string ContinuationKeyT026OnPlayGrantMarker = "continuation:treasureOnPlay:T026:onPlayGrantMarker";

    private const string ContextKeyT002OnPlayTargetHeal1 = "treasureOnPlay:T002:onPlayTargetHeal1";
    private const string ContextKeyT008OnPlayTargetDirectDamage1 = "treasureOnPlay:T008:onPlayTargetDirectDamage1";
    private const string ContextKeyT009OnPlayTargetOpponentSeal = "treasureOnPlay:T009:onPlayTargetOpponentSeal";
    private const string ContextKeyT022OnPlayOptionalBanishThenSelfShackle = "treasureOnPlay:T022:onPlayOptionalBanishThenSelfShackle";
    private const string ContextKeyT027OnPlayDraw2Discard2Step1 = "treasureOnPlay:T027:onPlayDraw2Discard2Step1";
    private const string ContextKeyT027OnPlayDraw2Discard2Step2 = "treasureOnPlay:T027:onPlayDraw2Discard2Step2";
    private const string ContextKeyT001OnPlayTargetOpponentSilenceAndDraw1 = "treasureOnPlay:T001:onPlayTargetOpponentSilenceAndDraw1";
    private const string ContextKeyT003OnPlayOptionalBanishStep = "treasureOnPlay:T003:onPlayOptionalBanishStep";
    private const string ContextKeyT003OnPlayTargetOpponentPhysicalDamage2 = "treasureOnPlay:T003:onPlayTargetOpponentPhysicalDamage2";
    private const string ContextKeyT004OnPlayTargetRemoveShackleOrSealStep = "treasureOnPlay:T004:onPlayTargetRemoveShackleOrSealStep";
    private const string ContextKeyT004OnPlayTargetOpponentPhysicalDamage3 = "treasureOnPlay:T004:onPlayTargetOpponentPhysicalDamage3";
    private const string ContextKeyT006OnPlayOptionalBanishFromSummonZone = "treasureOnPlay:T006:onPlayOptionalBanishFromSummonZone";
    private const string ContextKeyT011OnPlayTargetOpponentSpellDamage3 = "treasureOnPlay:T011:onPlayTargetOpponentSpellDamage3";
    private const string ContextKeyT012OnPlayOptionalBanishFromSummonZone = "treasureOnPlay:T012:onPlayOptionalBanishFromSummonZone";
    private const string ContextKeyT005OnPlaySelectTargetOpponentDiscard = "treasureOnPlay:T005:onPlaySelectTargetOpponentDiscard";
    private const string ContextKeyT005OnPlayTargetOpponentDiscardCard = "treasureOnPlay:T005:onPlayTargetOpponentDiscardCard";
    private const string ContextKeyT010OnPlaySelectDiscardToDeckTop = "treasureOnPlay:T010:onPlaySelectDiscardToDeckTop";
    private const string ContextKeyT010OnPlayOptionalMoveSelectedToHand = "treasureOnPlay:T010:onPlayOptionalMoveSelectedToHand";
    private const string ContextKeyT018OnPlayGainSkillPointThenBanish1 = "treasureOnPlay:T018:onPlayGainSkillPointThenBanish1";
    private const string ContextKeyT019OnPlayTargetOpponentSpellDamage2 = "treasureOnPlay:T019:onPlayTargetOpponentSpellDamage2";
    private const string ContextKeyT014OnPlayTargetFriendlyBarrier = "treasureOnPlay:T014:onPlayTargetFriendlyBarrier";
    private const string ContextKeyT015OnPlayTargetOpponentSilence = "treasureOnPlay:T015:onPlayTargetOpponentSilence";
    private const string ContextKeyT007OnPlayChooseAllyMode = "treasureOnPlay:T007:onPlayChooseAllyMode";
    private const string ContextKeyT007OnPlayTargetAllyDiscardCard = "treasureOnPlay:T007:onPlayTargetAllyDiscardCard";
    private const string ContextKeyT013OnPlayDeclareCardName = "treasureOnPlay:T013:onPlayDeclareCardName";
    private const string ContextKeyT013OnPlayTargetOpponentCharm = "treasureOnPlay:T013:onPlayTargetOpponentCharm";
    private const string ContextKeyT021OnPlayOverlayCardsForMana = "treasureOnPlay:T021:onPlayOverlayCardsForMana";
    private const string ContextKeyT023OnPlayTargetHumanHeal2 = "treasureOnPlay:T023:onPlayTargetHumanHeal2";
    private const string ContextKeyT023OnPlayTargetEnemyNonHumanDirectDamage1 = "treasureOnPlay:T023:onPlayTargetEnemyNonHumanDirectDamage1";
    private const string ContextKeyT017OnPlaySelectSummonZoneCard = "treasureOnPlay:T017:onPlaySelectSummonZoneCard";
    private const string ContextKeyT017OnPlayTargetFriendlyDeckTop = "treasureOnPlay:T017:onPlayTargetFriendlyDeckTop";
    private const string ContextKeyT026OnPlayGrantMarker = "treasureOnPlay:T026:onPlayGrantMarker";
    private const string LocalStateKeyT010SelectedDeckTopCardInstanceId = "treasureOnPlay:T010:selectedDeckTopCardInstanceId";
    private const string LocalStateKeyT017SelectedSummonZoneCardInstanceId = "treasureOnPlay:T017:selectedSummonZoneCardInstanceId";
    private const string DamageTypeKeyDirect = "direct";
    private const string DamageTypeKeyPhysical = "physical";
    private const string DamageTypeKeySpell = "spell";
    private const string StatusKeySeal = "Seal";
    private const string StatusKeyShackle = "Shackle";
    private const string StatusKeySilence = "Silence";
    private const string StatusKeyBarrier = "Barrier";
    private const string StatusKeyCharm = "Charm";
    private const string StatusKeyPenetrate = "Penetrate";
    private const string RaceTagHuman = "human";
    private const string RaceTagNonHuman = "nonHuman";
    private const string RevealReasonKeyT013DeclareCardName = "treasureOnPlay:T013:declareCardName";
    private const string ResponseWindowTypeDamageResponse = "damageResponse";
    private const int T027DrawCount = 2;
    private const int T027DiscardCount = 2;
    private const int T021MaxOverlayCount = 2;
    private const int T021ManaGainPerOverlayCard = 2;

    private readonly Func<long> nextInputContextIdSupplier;
    private readonly Func<long> nextResponseWindowIdSupplier;
    private readonly ZoneMovementService zoneMovementService;
    private readonly DamageProcessor damageProcessor;
    private readonly Action<GameState.GameState, ActionChainState, PlayerId, long> forceResolveCurrentAnomalyFromExternalEffect;

    private enum OnPlayEffectKind
    {
        none = 0,
        t002TargetHeal1 = 1,
        t008TargetDirectDamage1 = 2,
        t009TargetOpponentSeal = 3,
        t022OptionalBanishThenSelfShackle = 4,
        t027Draw2Discard2 = 5,
        t001TargetOpponentSilenceAndDraw1 = 6,
        t003OptionalBanishThenTargetOpponentPhysicalDamage2 = 7,
        t004TargetRemoveShackleOrSealThenOpponentPhysicalDamage3 = 8,
        t006SelfDirectDamage1ThenOptionalBanishSummonZone = 9,
        t011SkillPointPlus1ThenTargetOpponentSpellDamage3 = 10,
        t012Draw2ThenOptionalBanishSummonZone = 11,
        t005TargetOpponentDiscard1AndGainSkillPoint1 = 12,
        t010SelectDiscardToDeckTopThenOptionalMoveToHand = 13,
        t018SkillPointPlus1ThenBanish1 = 14,
        t019TargetOpponentSpellDamage2 = 15,
        t014TargetFriendlyBarrier = 16,
        t015TargetOpponentSilence = 17,
        t007ChooseAllyMode = 18,
        t013DeclareCardNameThenMaybeCharmOpponent = 19,
        t021OverlayUpTo2HandCardsForMana = 20,
        t028SelfNextDamagePenetrate = 21,
        t023LeylinePlus1HealHumanThenDamageEnemyNonHuman = 22,
        t017MoveSummonZoneCardToFriendlyDeckTop = 23,
        t020ResolveCurrentAnomalyThenBanishSelf = 24,
        t026TargetPlayerGrantAllowedMarker = 25,
    }

    public TreasureOnPlayEffectRuntime(
        Func<long> nextInputContextIdSupplier,
        Func<long> nextResponseWindowIdSupplier,
        ZoneMovementService zoneMovementService,
        DamageProcessor damageProcessor,
        Action<GameState.GameState, ActionChainState, PlayerId, long> forceResolveCurrentAnomalyFromExternalEffect)
    {
        this.nextInputContextIdSupplier = nextInputContextIdSupplier;
        this.nextResponseWindowIdSupplier = nextResponseWindowIdSupplier;
        this.zoneMovementService = zoneMovementService;
        this.damageProcessor = damageProcessor;
        this.forceResolveCurrentAnomalyFromExternalEffect = forceResolveCurrentAnomalyFromExternalEffect;
    }

    public static bool isTreasureOnPlayContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyT002OnPlayTargetHeal1, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT008OnPlayTargetDirectDamage1, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT009OnPlayTargetOpponentSeal, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT022OnPlayOptionalBanishThenSelfShackle, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT027OnPlayDraw2Discard2Step1, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT027OnPlayDraw2Discard2Step2, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT001OnPlayTargetOpponentSilenceAndDraw1, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT003OnPlayOptionalBanishStep, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT003OnPlayTargetOpponentPhysicalDamage2, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT004OnPlayTargetRemoveShackleOrSealStep, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT004OnPlayTargetOpponentPhysicalDamage3, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT006OnPlayOptionalBanishFromSummonZone, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT011OnPlayTargetOpponentSpellDamage3, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT012OnPlayOptionalBanishFromSummonZone, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT005OnPlaySelectTargetOpponentDiscard, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT005OnPlayTargetOpponentDiscardCard, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT010OnPlaySelectDiscardToDeckTop, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT010OnPlayOptionalMoveSelectedToHand, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT018OnPlayGainSkillPointThenBanish1, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT019OnPlayTargetOpponentSpellDamage2, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT014OnPlayTargetFriendlyBarrier, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT015OnPlayTargetOpponentSilence, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT007OnPlayChooseAllyMode, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT007OnPlayTargetAllyDiscardCard, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT013OnPlayDeclareCardName, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT013OnPlayTargetOpponentCharm, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT021OnPlayOverlayCardsForMana, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT023OnPlayTargetHumanHeal2, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT023OnPlayTargetEnemyNonHumanDirectDamage1, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT017OnPlaySelectSummonZoneCard, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT017OnPlayTargetFriendlyDeckTop, StringComparison.Ordinal) ||
               string.Equals(continuationKey, ContinuationKeyT026OnPlayGrantMarker, StringComparison.Ordinal);
    }

    public static bool isT021OverlayCardsContinuationKey(string? continuationKey)
    {
        return string.Equals(continuationKey, ContinuationKeyT021OnPlayOverlayCardsForMana, StringComparison.Ordinal);
    }

    public bool tryOpenOnPlayInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest,
        CardInstance cardInstance)
    {
        var effectKind = resolveOnPlayEffectKind(cardInstance.definitionId);
        if (effectKind == OnPlayEffectKind.none)
        {
            return false;
        }

        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("Treasure on-play target selection requires currentInputContext to be null.");
        }

        if (effectKind == OnPlayEffectKind.t027Draw2Discard2)
        {
            openT027Draw2Discard2InputContext(
                gameState,
                actionChainState,
                playTreasureCardActionRequest);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t022OptionalBanishThenSelfShackle)
        {
            openT022OptionalBanishInputContextOrApplySelfShackle(
                gameState,
                actionChainState,
                playTreasureCardActionRequest,
                cardInstance.cardInstanceId);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t003OptionalBanishThenTargetOpponentPhysicalDamage2)
        {
            openT003OptionalBanishThenTargetOpponentDamageInputContext(
                gameState,
                actionChainState,
                playTreasureCardActionRequest);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t004TargetRemoveShackleOrSealThenOpponentPhysicalDamage3)
        {
            openT004RemoveStatusThenTargetOpponentDamageInputContext(
                gameState,
                actionChainState,
                playTreasureCardActionRequest);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t006SelfDirectDamage1ThenOptionalBanishSummonZone)
        {
            openT006SelfDirectDamageThenOptionalSummonZoneBanish(
                gameState,
                actionChainState,
                playTreasureCardActionRequest,
                cardInstance.cardInstanceId);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t012Draw2ThenOptionalBanishSummonZone)
        {
            openT012Draw2ThenOptionalSummonZoneBanish(
                gameState,
                actionChainState,
                playTreasureCardActionRequest);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t010SelectDiscardToDeckTopThenOptionalMoveToHand)
        {
            openT010SelectDiscardToDeckTopInputContext(
                gameState,
                actionChainState,
                playTreasureCardActionRequest);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t007ChooseAllyMode)
        {
            openT007ChooseAllyModeInputContext(
                gameState,
                actionChainState,
                playTreasureCardActionRequest);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t013DeclareCardNameThenMaybeCharmOpponent)
        {
            openT013DeclareCardNameInputContext(
                gameState,
                actionChainState,
                playTreasureCardActionRequest);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t021OverlayUpTo2HandCardsForMana)
        {
            openT021OverlayCardsInputContextIfPossible(
                gameState,
                actionChainState,
                playTreasureCardActionRequest);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t023LeylinePlus1HealHumanThenDamageEnemyNonHuman)
        {
            applyT023LeylineAndOpenNextInputContextIfNeeded(
                gameState,
                actionChainState,
                playTreasureCardActionRequest.requestId,
                playTreasureCardActionRequest.actorPlayerId);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t028SelfNextDamagePenetrate)
        {
            applyT028SelfNextDamagePenetrate(
                gameState,
                actionChainState,
                playTreasureCardActionRequest.requestId,
                playTreasureCardActionRequest.actorPlayerId,
                cardInstance.cardInstanceId);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t017MoveSummonZoneCardToFriendlyDeckTop)
        {
            openT017SelectSummonZoneCardInputContextIfPossible(
                gameState,
                actionChainState,
                playTreasureCardActionRequest);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t020ResolveCurrentAnomalyThenBanishSelf)
        {
            applyT020ResolveCurrentAnomalyThenBanishSelf(
                gameState,
                actionChainState,
                playTreasureCardActionRequest.requestId,
                playTreasureCardActionRequest.actorPlayerId,
                cardInstance.cardInstanceId);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t026TargetPlayerGrantAllowedMarker)
        {
            openT026GrantMarkerInputContextIfPossible(
                gameState,
                actionChainState,
                playTreasureCardActionRequest);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t018SkillPointPlus1ThenBanish1)
        {
            gameState.players[playTreasureCardActionRequest.actorPlayerId].skillPoint += 1;
            openT018BanishInputContextIfPossible(
                gameState,
                actionChainState,
                playTreasureCardActionRequest);
            return true;
        }

        if (effectKind == OnPlayEffectKind.t011SkillPointPlus1ThenTargetOpponentSpellDamage3)
        {
            gameState.players[playTreasureCardActionRequest.actorPlayerId].skillPoint += 1;
        }
        else if (effectKind == OnPlayEffectKind.t005TargetOpponentDiscard1AndGainSkillPoint1)
        {
            gameState.players[playTreasureCardActionRequest.actorPlayerId].skillPoint += 1;
        }

        var choiceKeys = createChoiceKeysForEffect(gameState, playTreasureCardActionRequest.actorPlayerId, effectKind);
        if (choiceKeys.Count == 0)
        {
            return true;
        }

        openInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayTargetPlayerChoice,
            resolveContextKey(effectKind),
            resolveContinuationKey(effectKind),
            choiceKeys);

        return true;
    }

    public bool tryContinueOnSubmitInputChoice(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var continuationKey = actionChainState.pendingContinuationKey;
        if (!isTreasureOnPlayContinuationKey(continuationKey))
        {
            return false;
        }

        var sourceCardInstanceId = tryResolveSourceCardInstanceId(actionChainState);

        if (string.Equals(continuationKey, ContinuationKeyT002OnPlayTargetHeal1, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueT002TargetHeal1(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                selectedTargetPlayerId);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT008OnPlayTargetDirectDamage1, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueT008TargetDirectDamage1(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                sourceCardInstanceId,
                selectedTargetPlayerId);
            if (gameState.currentInputContext is null &&
                gameState.currentResponseWindow is null &&
                string.Equals(
                    actionChainState.pendingContinuationKey,
                    ContinuationKeyT008OnPlayTargetDirectDamage1,
                    StringComparison.Ordinal))
            {
                actionChainState.pendingContinuationKey = null;
            }

            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT009OnPlayTargetOpponentSeal, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueT009TargetOpponentSeal(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                sourceCardInstanceId,
                selectedTargetPlayerId);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT022OnPlayOptionalBanishThenSelfShackle, StringComparison.Ordinal))
        {
            continueT022OptionalBanishThenSelfShackle(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest,
                sourceCardInstanceId);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT027OnPlayDraw2Discard2Step1, StringComparison.Ordinal))
        {
            continueT027Draw2Discard2Step1(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT027OnPlayDraw2Discard2Step2, StringComparison.Ordinal))
        {
            continueT027Draw2Discard2Step2(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT001OnPlayTargetOpponentSilenceAndDraw1, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueT001TargetOpponentSilenceAndDraw1(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                sourceCardInstanceId,
                selectedTargetPlayerId);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT003OnPlayOptionalBanishStep, StringComparison.Ordinal))
        {
            continueT003OptionalBanishStep(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT003OnPlayTargetOpponentPhysicalDamage2, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueTargetDamage(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                sourceCardInstanceId,
                selectedTargetPlayerId,
                baseDamageValue: 2,
                damageTypeKey: DamageTypeKeyPhysical,
                continuationKey: ContinuationKeyT003OnPlayTargetOpponentPhysicalDamage2,
                requireOpponentTarget: true,
                isDirectDamage: false);
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT004OnPlayTargetRemoveShackleOrSealStep, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueT004TargetRemoveShackleOrSealStep(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                sourceCardInstanceId,
                selectedTargetPlayerId);
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT004OnPlayTargetOpponentPhysicalDamage3, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueTargetDamage(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                sourceCardInstanceId,
                selectedTargetPlayerId,
                baseDamageValue: 3,
                damageTypeKey: DamageTypeKeyPhysical,
                continuationKey: ContinuationKeyT004OnPlayTargetOpponentPhysicalDamage3,
                requireOpponentTarget: true,
                isDirectDamage: false);
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT006OnPlayOptionalBanishFromSummonZone, StringComparison.Ordinal))
        {
            continueOptionalSummonZoneBanish(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest,
                ContinuationKeyT006OnPlayOptionalBanishFromSummonZone);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT011OnPlayTargetOpponentSpellDamage3, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueTargetDamage(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                sourceCardInstanceId,
                selectedTargetPlayerId,
                baseDamageValue: 3,
                damageTypeKey: DamageTypeKeySpell,
                continuationKey: ContinuationKeyT011OnPlayTargetOpponentSpellDamage3,
                requireOpponentTarget: true,
                isDirectDamage: false);
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT012OnPlayOptionalBanishFromSummonZone, StringComparison.Ordinal))
        {
            continueOptionalSummonZoneBanish(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest,
                ContinuationKeyT012OnPlayOptionalBanishFromSummonZone);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT010OnPlaySelectDiscardToDeckTop, StringComparison.Ordinal))
        {
            continueT010SelectDiscardToDeckTop(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT010OnPlayOptionalMoveSelectedToHand, StringComparison.Ordinal))
        {
            continueT010OptionalMoveSelectedToHand(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT018OnPlayGainSkillPointThenBanish1, StringComparison.Ordinal))
        {
            continueT018BanishFromHandOrDiscard(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT019OnPlayTargetOpponentSpellDamage2, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueTargetDamage(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                sourceCardInstanceId,
                selectedTargetPlayerId,
                baseDamageValue: 2,
                damageTypeKey: DamageTypeKeySpell,
                continuationKey: ContinuationKeyT019OnPlayTargetOpponentSpellDamage2,
                requireOpponentTarget: true,
                isDirectDamage: false);
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT005OnPlaySelectTargetOpponentDiscard, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueT005SelectTargetOpponentDiscard(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                selectedTargetPlayerId);
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT005OnPlayTargetOpponentDiscardCard, StringComparison.Ordinal))
        {
            continueT005TargetOpponentDiscardCard(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT014OnPlayTargetFriendlyBarrier, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueT014TargetFriendlyBarrier(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                sourceCardInstanceId,
                selectedTargetPlayerId);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT015OnPlayTargetOpponentSilence, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueT015TargetOpponentSilence(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                sourceCardInstanceId,
                selectedTargetPlayerId);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT007OnPlayChooseAllyMode, StringComparison.Ordinal))
        {
            continueT007ChooseAllyMode(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT007OnPlayTargetAllyDiscardCard, StringComparison.Ordinal))
        {
            continueT007TargetAllyDiscardCard(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT013OnPlayDeclareCardName, StringComparison.Ordinal))
        {
            continueT013DeclareCardName(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT013OnPlayTargetOpponentCharm, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueT013TargetOpponentCharm(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                sourceCardInstanceId,
                selectedTargetPlayerId);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT021OnPlayOverlayCardsForMana, StringComparison.Ordinal))
        {
            continueT021OverlayCardsForMana(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest,
                sourceCardInstanceId);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT023OnPlayTargetHumanHeal2, StringComparison.Ordinal))
        {
            var selectedTargetCharacterInstanceId = parseCharacterChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueT023TargetHumanHeal2(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                selectedTargetCharacterInstanceId);
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT023OnPlayTargetEnemyNonHumanDirectDamage1, StringComparison.Ordinal))
        {
            var selectedTargetCharacterInstanceId = parseCharacterChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueT023TargetEnemyNonHumanDirectDamage1(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                sourceCardInstanceId,
                selectedTargetCharacterInstanceId);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT017OnPlaySelectSummonZoneCard, StringComparison.Ordinal))
        {
            continueT017SelectSummonZoneCard(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT017OnPlayTargetFriendlyDeckTop, StringComparison.Ordinal))
        {
            var selectedTargetPlayerId = parsePlayerChoiceKey(submitInputChoiceActionRequest.choiceKey);
            continueT017TargetFriendlyDeckTop(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                selectedTargetPlayerId);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        if (string.Equals(continuationKey, ContinuationKeyT026OnPlayGrantMarker, StringComparison.Ordinal))
        {
            continueT026GrantMarker(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest);
            actionChainState.pendingContinuationKey = null;
            return true;
        }

        return false;
    }

    private static OnPlayEffectKind resolveOnPlayEffectKind(string? definitionId)
    {
        return definitionId switch
        {
            "T002" => OnPlayEffectKind.t002TargetHeal1,
            "T008" => OnPlayEffectKind.t008TargetDirectDamage1,
            "T009" => OnPlayEffectKind.t009TargetOpponentSeal,
            "T022" => OnPlayEffectKind.t022OptionalBanishThenSelfShackle,
            "T027" => OnPlayEffectKind.t027Draw2Discard2,
            "T001" => OnPlayEffectKind.t001TargetOpponentSilenceAndDraw1,
            "T003" => OnPlayEffectKind.t003OptionalBanishThenTargetOpponentPhysicalDamage2,
            "T004" => OnPlayEffectKind.t004TargetRemoveShackleOrSealThenOpponentPhysicalDamage3,
            "T006" => OnPlayEffectKind.t006SelfDirectDamage1ThenOptionalBanishSummonZone,
            "T011" => OnPlayEffectKind.t011SkillPointPlus1ThenTargetOpponentSpellDamage3,
            "T012" => OnPlayEffectKind.t012Draw2ThenOptionalBanishSummonZone,
            "T005" => OnPlayEffectKind.t005TargetOpponentDiscard1AndGainSkillPoint1,
            "T010" => OnPlayEffectKind.t010SelectDiscardToDeckTopThenOptionalMoveToHand,
            "T018" => OnPlayEffectKind.t018SkillPointPlus1ThenBanish1,
            "T019" => OnPlayEffectKind.t019TargetOpponentSpellDamage2,
            "T014" => OnPlayEffectKind.t014TargetFriendlyBarrier,
            "T015" => OnPlayEffectKind.t015TargetOpponentSilence,
            "T007" => OnPlayEffectKind.t007ChooseAllyMode,
            "T013" => OnPlayEffectKind.t013DeclareCardNameThenMaybeCharmOpponent,
            "T021" => OnPlayEffectKind.t021OverlayUpTo2HandCardsForMana,
            "T028" => OnPlayEffectKind.t028SelfNextDamagePenetrate,
            "T023" => OnPlayEffectKind.t023LeylinePlus1HealHumanThenDamageEnemyNonHuman,
            "T017" => OnPlayEffectKind.t017MoveSummonZoneCardToFriendlyDeckTop,
            "T020" => OnPlayEffectKind.t020ResolveCurrentAnomalyThenBanishSelf,
            "T026" => OnPlayEffectKind.t026TargetPlayerGrantAllowedMarker,
            _ => OnPlayEffectKind.none,
        };
    }

    private static string resolveContinuationKey(OnPlayEffectKind effectKind)
    {
        return effectKind switch
        {
            OnPlayEffectKind.t002TargetHeal1 => ContinuationKeyT002OnPlayTargetHeal1,
            OnPlayEffectKind.t008TargetDirectDamage1 => ContinuationKeyT008OnPlayTargetDirectDamage1,
            OnPlayEffectKind.t009TargetOpponentSeal => ContinuationKeyT009OnPlayTargetOpponentSeal,
            OnPlayEffectKind.t022OptionalBanishThenSelfShackle => ContinuationKeyT022OnPlayOptionalBanishThenSelfShackle,
            OnPlayEffectKind.t027Draw2Discard2 => ContinuationKeyT027OnPlayDraw2Discard2Step1,
            OnPlayEffectKind.t001TargetOpponentSilenceAndDraw1 => ContinuationKeyT001OnPlayTargetOpponentSilenceAndDraw1,
            OnPlayEffectKind.t003OptionalBanishThenTargetOpponentPhysicalDamage2 => ContinuationKeyT003OnPlayOptionalBanishStep,
            OnPlayEffectKind.t004TargetRemoveShackleOrSealThenOpponentPhysicalDamage3 => ContinuationKeyT004OnPlayTargetRemoveShackleOrSealStep,
            OnPlayEffectKind.t006SelfDirectDamage1ThenOptionalBanishSummonZone => ContinuationKeyT006OnPlayOptionalBanishFromSummonZone,
            OnPlayEffectKind.t011SkillPointPlus1ThenTargetOpponentSpellDamage3 => ContinuationKeyT011OnPlayTargetOpponentSpellDamage3,
            OnPlayEffectKind.t012Draw2ThenOptionalBanishSummonZone => ContinuationKeyT012OnPlayOptionalBanishFromSummonZone,
            OnPlayEffectKind.t005TargetOpponentDiscard1AndGainSkillPoint1 => ContinuationKeyT005OnPlaySelectTargetOpponentDiscard,
            OnPlayEffectKind.t010SelectDiscardToDeckTopThenOptionalMoveToHand => ContinuationKeyT010OnPlaySelectDiscardToDeckTop,
            OnPlayEffectKind.t018SkillPointPlus1ThenBanish1 => ContinuationKeyT018OnPlayGainSkillPointThenBanish1,
            OnPlayEffectKind.t019TargetOpponentSpellDamage2 => ContinuationKeyT019OnPlayTargetOpponentSpellDamage2,
            OnPlayEffectKind.t014TargetFriendlyBarrier => ContinuationKeyT014OnPlayTargetFriendlyBarrier,
            OnPlayEffectKind.t015TargetOpponentSilence => ContinuationKeyT015OnPlayTargetOpponentSilence,
            OnPlayEffectKind.t007ChooseAllyMode => ContinuationKeyT007OnPlayChooseAllyMode,
            OnPlayEffectKind.t013DeclareCardNameThenMaybeCharmOpponent => ContinuationKeyT013OnPlayDeclareCardName,
            OnPlayEffectKind.t021OverlayUpTo2HandCardsForMana => ContinuationKeyT021OnPlayOverlayCardsForMana,
            OnPlayEffectKind.t026TargetPlayerGrantAllowedMarker => ContinuationKeyT026OnPlayGrantMarker,
            _ => string.Empty,
        };
    }

    private static string resolveContextKey(OnPlayEffectKind effectKind)
    {
        return effectKind switch
        {
            OnPlayEffectKind.t002TargetHeal1 => ContextKeyT002OnPlayTargetHeal1,
            OnPlayEffectKind.t008TargetDirectDamage1 => ContextKeyT008OnPlayTargetDirectDamage1,
            OnPlayEffectKind.t009TargetOpponentSeal => ContextKeyT009OnPlayTargetOpponentSeal,
            OnPlayEffectKind.t022OptionalBanishThenSelfShackle => ContextKeyT022OnPlayOptionalBanishThenSelfShackle,
            OnPlayEffectKind.t027Draw2Discard2 => ContextKeyT027OnPlayDraw2Discard2Step1,
            OnPlayEffectKind.t001TargetOpponentSilenceAndDraw1 => ContextKeyT001OnPlayTargetOpponentSilenceAndDraw1,
            OnPlayEffectKind.t003OptionalBanishThenTargetOpponentPhysicalDamage2 => ContextKeyT003OnPlayOptionalBanishStep,
            OnPlayEffectKind.t004TargetRemoveShackleOrSealThenOpponentPhysicalDamage3 => ContextKeyT004OnPlayTargetRemoveShackleOrSealStep,
            OnPlayEffectKind.t006SelfDirectDamage1ThenOptionalBanishSummonZone => ContextKeyT006OnPlayOptionalBanishFromSummonZone,
            OnPlayEffectKind.t011SkillPointPlus1ThenTargetOpponentSpellDamage3 => ContextKeyT011OnPlayTargetOpponentSpellDamage3,
            OnPlayEffectKind.t012Draw2ThenOptionalBanishSummonZone => ContextKeyT012OnPlayOptionalBanishFromSummonZone,
            OnPlayEffectKind.t005TargetOpponentDiscard1AndGainSkillPoint1 => ContextKeyT005OnPlaySelectTargetOpponentDiscard,
            OnPlayEffectKind.t010SelectDiscardToDeckTopThenOptionalMoveToHand => ContextKeyT010OnPlaySelectDiscardToDeckTop,
            OnPlayEffectKind.t018SkillPointPlus1ThenBanish1 => ContextKeyT018OnPlayGainSkillPointThenBanish1,
            OnPlayEffectKind.t019TargetOpponentSpellDamage2 => ContextKeyT019OnPlayTargetOpponentSpellDamage2,
            OnPlayEffectKind.t014TargetFriendlyBarrier => ContextKeyT014OnPlayTargetFriendlyBarrier,
            OnPlayEffectKind.t015TargetOpponentSilence => ContextKeyT015OnPlayTargetOpponentSilence,
            OnPlayEffectKind.t007ChooseAllyMode => ContextKeyT007OnPlayChooseAllyMode,
            OnPlayEffectKind.t013DeclareCardNameThenMaybeCharmOpponent => ContextKeyT013OnPlayDeclareCardName,
            OnPlayEffectKind.t021OverlayUpTo2HandCardsForMana => ContextKeyT021OnPlayOverlayCardsForMana,
            OnPlayEffectKind.t017MoveSummonZoneCardToFriendlyDeckTop => ContextKeyT017OnPlayTargetFriendlyDeckTop,
            OnPlayEffectKind.t026TargetPlayerGrantAllowedMarker => ContextKeyT026OnPlayGrantMarker,
            _ => string.Empty,
        };
    }

    private static List<string> createChoiceKeysForEffect(
        GameState.GameState gameState,
        PlayerId actorPlayerId,
        OnPlayEffectKind effectKind)
    {
        var choiceKeys = new List<string>();
        var actorTeamId = gameState.players[actorPlayerId].teamId;
        var requiresOpponentTarget =
            effectKind == OnPlayEffectKind.t001TargetOpponentSilenceAndDraw1 ||
            effectKind == OnPlayEffectKind.t008TargetDirectDamage1 ||
            effectKind == OnPlayEffectKind.t009TargetOpponentSeal ||
            effectKind == OnPlayEffectKind.t003OptionalBanishThenTargetOpponentPhysicalDamage2 ||
            effectKind == OnPlayEffectKind.t004TargetRemoveShackleOrSealThenOpponentPhysicalDamage3 ||
            effectKind == OnPlayEffectKind.t011SkillPointPlus1ThenTargetOpponentSpellDamage3 ||
            effectKind == OnPlayEffectKind.t005TargetOpponentDiscard1AndGainSkillPoint1 ||
            effectKind == OnPlayEffectKind.t019TargetOpponentSpellDamage2 ||
            effectKind == OnPlayEffectKind.t015TargetOpponentSilence ||
            effectKind == OnPlayEffectKind.t013DeclareCardNameThenMaybeCharmOpponent;
        var requiresFriendlyTarget =
            effectKind == OnPlayEffectKind.t014TargetFriendlyBarrier ||
            effectKind == OnPlayEffectKind.t017MoveSummonZoneCardToFriendlyDeckTop;

        foreach (var playerState in gameState.players.Values)
        {
            if (requiresOpponentTarget && playerState.teamId == actorTeamId)
            {
                continue;
            }

            if (requiresFriendlyTarget && playerState.teamId != actorTeamId)
            {
                continue;
            }

            if (!hasAliveInPlayActiveCharacter(gameState, playerState.playerId))
            {
                continue;
            }

            choiceKeys.Add(createPlayerChoiceKey(playerState.playerId));
        }

        return choiceKeys;
    }

    private static List<string> createStatusRemovalTargetChoiceKeys(GameState.GameState gameState)
    {
        var choiceKeys = new List<string>();
        foreach (var playerState in gameState.players.Values)
        {
            if (!hasAliveInPlayActiveCharacter(gameState, playerState.playerId))
            {
                continue;
            }

            if (!gameState.players.TryGetValue(playerState.playerId, out var targetPlayerState) ||
                !targetPlayerState.activeCharacterInstanceId.HasValue)
            {
                continue;
            }

            var targetCharacterInstanceId = targetPlayerState.activeCharacterInstanceId.Value;
            if (!StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterInstanceId, StatusKeyShackle) &&
                !StatusRuntime.hasStatusOnCharacter(gameState, targetCharacterInstanceId, StatusKeySeal))
            {
                continue;
            }

            choiceKeys.Add(createPlayerChoiceKey(playerState.playerId));
        }

        return choiceKeys;
    }

    private void openT027Draw2Discard2InputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        drawCardsForPlayer(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.actorPlayerId,
            playTreasureCardActionRequest.requestId,
            T027DrawCount);

        var discardChoiceKeys = createDiscardChoiceKeysFromHand(gameState, playTreasureCardActionRequest.actorPlayerId);
        var discardCountRequired = Math.Min(T027DiscardCount, discardChoiceKeys.Count);
        if (discardCountRequired <= 0)
        {
            return;
        }

        var continuationKey = discardCountRequired == 1
            ? ContinuationKeyT027OnPlayDraw2Discard2Step2
            : ContinuationKeyT027OnPlayDraw2Discard2Step1;
        var contextKey = discardCountRequired == 1
            ? ContextKeyT027OnPlayDraw2Discard2Step2
            : ContextKeyT027OnPlayDraw2Discard2Step1;

        openInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayDiscardCardChoice,
            contextKey,
            continuationKey,
            discardChoiceKeys);
    }

    private void openT013DeclareCardNameInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        var choiceKeys = new List<string>();
        foreach (var definitionId in TreasureDefinitionRepository.getDeclarableTreasureDefinitionIds())
        {
            choiceKeys.Add(ChoiceKeyDeclareCardNamePrefix + definitionId);
        }

        if (choiceKeys.Count == 0)
        {
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayDeclareCardNameChoice,
            ContextKeyT013OnPlayDeclareCardName,
            ContinuationKeyT013OnPlayDeclareCardName,
            choiceKeys);
    }

    private void openT021OverlayCardsInputContextIfPossible(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        var choiceKeys = collectT021OverlayChoiceKeysFromHand(gameState, playTreasureCardActionRequest.actorPlayerId);
        if (choiceKeys.Count <= 1)
        {
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayOverlayCardsChoice,
            ContextKeyT021OnPlayOverlayCardsForMana,
            ContinuationKeyT021OnPlayOverlayCardsForMana,
            choiceKeys);
    }

    private void openT022OptionalBanishInputContextOrApplySelfShackle(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest,
        CardInstanceId sourceCardInstanceId)
    {
        var optionalBanishChoiceKeys = collectT022OptionalBanishChoiceKeys(gameState, playTreasureCardActionRequest.actorPlayerId);
        if (optionalBanishChoiceKeys.Count <= 1)
        {
            applySelfShackle(
                gameState,
                actionChainState,
                playTreasureCardActionRequest.requestId,
                playTreasureCardActionRequest.actorPlayerId,
                sourceCardInstanceId);
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayOptionalBanishChoice,
            ContextKeyT022OnPlayOptionalBanishThenSelfShackle,
            ContinuationKeyT022OnPlayOptionalBanishThenSelfShackle,
            optionalBanishChoiceKeys);
    }

    private void openT003OptionalBanishThenTargetOpponentDamageInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        var optionalBanishChoiceKeys = collectT022OptionalBanishChoiceKeys(gameState, playTreasureCardActionRequest.actorPlayerId);
        if (optionalBanishChoiceKeys.Count > 1)
        {
            openInputContext(
                gameState,
                actionChainState,
                playTreasureCardActionRequest.requestId,
                playTreasureCardActionRequest.actorPlayerId,
                InputTypeKeyTreasureOnPlayOptionalBanishChoice,
                ContextKeyT003OnPlayOptionalBanishStep,
                ContinuationKeyT003OnPlayOptionalBanishStep,
                optionalBanishChoiceKeys);
            return;
        }

        var damageTargetChoiceKeys = createChoiceKeysForEffect(
            gameState,
            playTreasureCardActionRequest.actorPlayerId,
            OnPlayEffectKind.t003OptionalBanishThenTargetOpponentPhysicalDamage2);
        if (damageTargetChoiceKeys.Count <= 0)
        {
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayTargetPlayerChoice,
            ContextKeyT003OnPlayTargetOpponentPhysicalDamage2,
            ContinuationKeyT003OnPlayTargetOpponentPhysicalDamage2,
            damageTargetChoiceKeys);
    }

    private void openT004RemoveStatusThenTargetOpponentDamageInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        var removeStatusTargetChoiceKeys = createStatusRemovalTargetChoiceKeys(gameState);
        if (removeStatusTargetChoiceKeys.Count > 0)
        {
            openInputContext(
                gameState,
                actionChainState,
                playTreasureCardActionRequest.requestId,
                playTreasureCardActionRequest.actorPlayerId,
                InputTypeKeyTreasureOnPlayTargetPlayerChoice,
                ContextKeyT004OnPlayTargetRemoveShackleOrSealStep,
                ContinuationKeyT004OnPlayTargetRemoveShackleOrSealStep,
                removeStatusTargetChoiceKeys);
            return;
        }

        openT004TargetOpponentDamageInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId);
    }

    private void openT004TargetOpponentDamageInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId actorPlayerId)
    {
        var damageTargetChoiceKeys = createChoiceKeysForEffect(
            gameState,
            actorPlayerId,
            OnPlayEffectKind.t004TargetRemoveShackleOrSealThenOpponentPhysicalDamage3);
        if (damageTargetChoiceKeys.Count <= 0)
        {
            actionChainState.pendingContinuationKey = null;
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            requestId,
            actorPlayerId,
            InputTypeKeyTreasureOnPlayTargetPlayerChoice,
            ContextKeyT004OnPlayTargetOpponentPhysicalDamage3,
            ContinuationKeyT004OnPlayTargetOpponentPhysicalDamage3,
            damageTargetChoiceKeys);
    }

    private void openT006SelfDirectDamageThenOptionalSummonZoneBanish(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest,
        CardInstanceId sourceCardInstanceId)
    {
        var actorCharacterInstance = resolveAliveInPlayActiveCharacter(
            gameState,
            playTreasureCardActionRequest.actorPlayerId,
            ContinuationKeyT006OnPlayOptionalBanishFromSummonZone);

        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(
            gameState,
            new DamageContext
            {
                damageContextId = new DamageContextId(playTreasureCardActionRequest.requestId),
                sourcePlayerId = playTreasureCardActionRequest.actorPlayerId,
                sourceCardInstanceId = sourceCardInstanceId,
                targetPlayerId = playTreasureCardActionRequest.actorPlayerId,
                targetCharacterInstanceId = actorCharacterInstance.characterInstanceId,
                baseDamageValue = 1,
                damageType = DamageTypeKeyDirect,
            }));

        openOptionalSummonZoneBanishInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId,
            ContextKeyT006OnPlayOptionalBanishFromSummonZone,
            ContinuationKeyT006OnPlayOptionalBanishFromSummonZone);
    }

    private void openT012Draw2ThenOptionalSummonZoneBanish(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        drawCardsForPlayer(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.actorPlayerId,
            playTreasureCardActionRequest.requestId,
            drawCount: 2);

        openOptionalSummonZoneBanishInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId,
            ContextKeyT012OnPlayOptionalBanishFromSummonZone,
            ContinuationKeyT012OnPlayOptionalBanishFromSummonZone);
    }

    private void openOptionalSummonZoneBanishInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId actorPlayerId,
        string contextKey,
        string continuationKey)
    {
        var choiceKeys = collectOptionalBanishChoiceKeysFromSummonZone(gameState);
        if (choiceKeys.Count <= 1)
        {
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            requestId,
            actorPlayerId,
            InputTypeKeyTreasureOnPlaySummonZoneBanishChoice,
            contextKey,
            continuationKey,
            choiceKeys);
    }

    private void openT010SelectDiscardToDeckTopInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        var discardChoiceKeys = createDiscardChoiceKeysFromDiscard(gameState, playTreasureCardActionRequest.actorPlayerId);
        if (discardChoiceKeys.Count <= 0)
        {
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayDiscardCardChoice,
            ContextKeyT010OnPlaySelectDiscardToDeckTop,
            ContinuationKeyT010OnPlaySelectDiscardToDeckTop,
            discardChoiceKeys);
    }

    private void openT007ChooseAllyModeInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        var choiceKeys = createT007AllyModeChoiceKeys(gameState, playTreasureCardActionRequest.actorPlayerId);
        if (choiceKeys.Count <= 0)
        {
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayTargetPlayerChoice,
            ContextKeyT007OnPlayChooseAllyMode,
            ContinuationKeyT007OnPlayChooseAllyMode,
            choiceKeys);
    }

    private void openT018BanishInputContextIfPossible(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        var banishChoiceKeys = collectT022OptionalBanishChoiceKeys(
            gameState,
            playTreasureCardActionRequest.actorPlayerId);
        if (banishChoiceKeys.Count <= 1)
        {
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayOptionalBanishChoice,
            ContextKeyT018OnPlayGainSkillPointThenBanish1,
            ContinuationKeyT018OnPlayGainSkillPointThenBanish1,
            banishChoiceKeys);
    }

    private void continueT027Draw2Discard2Step1(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        discardSelectedCardFromActorHand(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            submitInputChoiceActionRequest.actorPlayerId,
            submitInputChoiceActionRequest.choiceKey,
            ContinuationKeyT027OnPlayDraw2Discard2Step1);

        var remainingDiscardChoiceKeys = createDiscardChoiceKeysFromHand(gameState, submitInputChoiceActionRequest.actorPlayerId);
        if (remainingDiscardChoiceKeys.Count <= 0)
        {
            actionChainState.pendingContinuationKey = null;
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            submitInputChoiceActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayDiscardCardChoice,
            ContextKeyT027OnPlayDraw2Discard2Step2,
            ContinuationKeyT027OnPlayDraw2Discard2Step2,
            remainingDiscardChoiceKeys);
    }

    private void continueT027Draw2Discard2Step2(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        discardSelectedCardFromActorHand(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            submitInputChoiceActionRequest.actorPlayerId,
            submitInputChoiceActionRequest.choiceKey,
            ContinuationKeyT027OnPlayDraw2Discard2Step2);
    }

    private void continueT022OptionalBanishThenSelfShackle(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        CardInstanceId? sourceCardInstanceId)
    {
        if (!string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                ChoiceKeyDeclineOptionalBanish,
                StringComparison.Ordinal))
        {
            var selectedCardInstanceId = parseCardChoiceKey(
                submitInputChoiceActionRequest.choiceKey,
                ChoiceKeyBanishCardPrefix,
                "T022 optional banish choice");
            banishSelectedCardFromActorHandOrDiscard(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                selectedCardInstanceId,
                ContinuationKeyT022OnPlayOptionalBanishThenSelfShackle);
        }

        applySelfShackle(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            submitInputChoiceActionRequest.actorPlayerId,
            sourceCardInstanceId);
    }

    private void continueT003OptionalBanishStep(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                ChoiceKeyDeclineOptionalBanish,
                StringComparison.Ordinal))
        {
            var selectedCardInstanceId = parseCardChoiceKey(
                submitInputChoiceActionRequest.choiceKey,
                ChoiceKeyBanishCardPrefix,
                "T003 optional banish choice");
            banishSelectedCardFromActorHandOrDiscard(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                selectedCardInstanceId,
                ContinuationKeyT003OnPlayOptionalBanishStep);
        }

        var damageTargetChoiceKeys = createChoiceKeysForEffect(
            gameState,
            submitInputChoiceActionRequest.actorPlayerId,
            OnPlayEffectKind.t003OptionalBanishThenTargetOpponentPhysicalDamage2);
        if (damageTargetChoiceKeys.Count <= 0)
        {
            actionChainState.pendingContinuationKey = null;
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            submitInputChoiceActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayTargetPlayerChoice,
            ContextKeyT003OnPlayTargetOpponentPhysicalDamage2,
            ContinuationKeyT003OnPlayTargetOpponentPhysicalDamage2,
            damageTargetChoiceKeys);
    }

    private void continueOptionalSummonZoneBanish(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        string continuationKey)
    {
        if (string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                ChoiceKeyDeclineOptionalBanish,
                StringComparison.Ordinal))
        {
            return;
        }

        var selectedCardInstanceId = parseCardChoiceKey(
            submitInputChoiceActionRequest.choiceKey,
            ChoiceKeyBanishCardPrefix,
            "summonZone optional banish choice");
        banishSelectedCardFromSummonZone(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            selectedCardInstanceId,
            continuationKey);
    }

    private void continueT010SelectDiscardToDeckTop(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var selectedCardInstanceId = parseCardChoiceKey(
            submitInputChoiceActionRequest.choiceKey,
            ChoiceKeyDiscardCardPrefix,
            "T010 discard-to-deck-top choice");
        var actorPlayerState = gameState.players[submitInputChoiceActionRequest.actorPlayerId];
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException($"{ContinuationKeyT010OnPlaySelectDiscardToDeckTop} requires selected discard card instance to exist.");
        }

        if (selectedCardInstance.ownerPlayerId != submitInputChoiceActionRequest.actorPlayerId)
        {
            throw new InvalidOperationException($"{ContinuationKeyT010OnPlaySelectDiscardToDeckTop} requires selected discard card to be owned by actor player.");
        }

        if (selectedCardInstance.zoneId != actorPlayerState.discardZoneId)
        {
            throw new InvalidOperationException($"{ContinuationKeyT010OnPlaySelectDiscardToDeckTop} requires selected discard card to be in actor discard zone.");
        }

        var movedEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            actorPlayerState.deckZoneId,
            CardMoveReason.returnToSource,
            actionChainState.actionChainId,
            submitInputChoiceActionRequest.requestId);
        actionChainState.producedEvents.Add(movedEvent);

        var actorDeckZoneState = gameState.zones[actorPlayerState.deckZoneId];
        actorDeckZoneState.cardInstanceIds.Remove(selectedCardInstanceId);
        actorDeckZoneState.cardInstanceIds.Insert(0, selectedCardInstanceId);

        var selectedCardSummonCost = TemporaryTreasureDefinitionResolver
            .resolveDefinition(selectedCardInstance.definitionId)
            .summonSigilCost;
        if (selectedCardSummonCost.HasValue && selectedCardSummonCost.Value < 5)
        {
            actionChainState.localState[LocalStateKeyT010SelectedDeckTopCardInstanceId] =
                selectedCardInstanceId.Value.ToString();
            openInputContext(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                InputTypeKeyTreasureOnPlayOptionalMoveToHandChoice,
                ContextKeyT010OnPlayOptionalMoveSelectedToHand,
                ContinuationKeyT010OnPlayOptionalMoveSelectedToHand,
                createMoveToHandChoiceKeys());
            return;
        }

        actionChainState.localState.Remove(LocalStateKeyT010SelectedDeckTopCardInstanceId);
        actionChainState.pendingContinuationKey = null;
    }

    private void continueT010OptionalMoveSelectedToHand(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                ChoiceKeyAcceptMoveToHand,
                StringComparison.Ordinal))
        {
            actionChainState.localState.Remove(LocalStateKeyT010SelectedDeckTopCardInstanceId);
            return;
        }

        if (!actionChainState.localState.TryGetValue(LocalStateKeyT010SelectedDeckTopCardInstanceId, out var serializedCardInstanceId) ||
            !long.TryParse(serializedCardInstanceId, out var selectedCardNumericId) ||
            selectedCardNumericId <= 0)
        {
            throw new InvalidOperationException($"{ContinuationKeyT010OnPlayOptionalMoveSelectedToHand} requires selected deck-top card state.");
        }

        var selectedCardInstanceId = new CardInstanceId(selectedCardNumericId);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException($"{ContinuationKeyT010OnPlayOptionalMoveSelectedToHand} requires selected deck-top card to exist.");
        }

        var actorPlayerState = gameState.players[submitInputChoiceActionRequest.actorPlayerId];
        if (selectedCardInstance.ownerPlayerId != submitInputChoiceActionRequest.actorPlayerId)
        {
            throw new InvalidOperationException($"{ContinuationKeyT010OnPlayOptionalMoveSelectedToHand} requires selected deck-top card to be owned by actor player.");
        }

        if (selectedCardInstance.zoneId != actorPlayerState.deckZoneId)
        {
            throw new InvalidOperationException($"{ContinuationKeyT010OnPlayOptionalMoveSelectedToHand} requires selected card to remain in actor deck zone.");
        }

        var movedEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            actorPlayerState.handZoneId,
            CardMoveReason.draw,
            actionChainState.actionChainId,
            submitInputChoiceActionRequest.requestId);
        actionChainState.producedEvents.Add(movedEvent);
        actionChainState.localState.Remove(LocalStateKeyT010SelectedDeckTopCardInstanceId);
    }

    private void continueT018BanishFromHandOrDiscard(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                ChoiceKeyDeclineOptionalBanish,
                StringComparison.Ordinal))
        {
            return;
        }

        var selectedCardInstanceId = parseCardChoiceKey(
            submitInputChoiceActionRequest.choiceKey,
            ChoiceKeyBanishCardPrefix,
            "T018 banish choice");
        banishSelectedCardFromActorHandOrDiscard(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            submitInputChoiceActionRequest.actorPlayerId,
            selectedCardInstanceId,
            ContinuationKeyT018OnPlayGainSkillPointThenBanish1);
    }

    private void continueT005SelectTargetOpponentDiscard(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        PlayerId targetPlayerId)
    {
        var sourcePlayerState = gameState.players[sourcePlayerId];
        var targetPlayerState = gameState.players[targetPlayerId];
        if (sourcePlayerState.teamId == targetPlayerState.teamId)
        {
            throw new InvalidOperationException("T005 on-play continuation requires selected target player to be an opponent.");
        }

        var discardChoiceKeys = createDiscardChoiceKeysFromHand(gameState, targetPlayerId);
        if (discardChoiceKeys.Count <= 0)
        {
            actionChainState.pendingContinuationKey = null;
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            requestId,
            targetPlayerId,
            InputTypeKeyTreasureOnPlayDiscardCardChoice,
            ContextKeyT005OnPlayTargetOpponentDiscardCard,
            ContinuationKeyT005OnPlayTargetOpponentDiscardCard,
            discardChoiceKeys);
    }

    private void continueT005TargetOpponentDiscardCard(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        discardSelectedCardFromActorHand(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            submitInputChoiceActionRequest.actorPlayerId,
            submitInputChoiceActionRequest.choiceKey,
            ContinuationKeyT005OnPlayTargetOpponentDiscardCard);
    }

    private void continueT007ChooseAllyMode(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (submitInputChoiceActionRequest.choiceKey.StartsWith(ChoiceKeyT007GiveToAllyPrefix, StringComparison.Ordinal))
        {
            var targetAllyPlayerId = parsePlayerChoiceKeyWithPrefix(
                submitInputChoiceActionRequest.choiceKey,
                ChoiceKeyT007GiveToAllyPrefix,
                "T007 give-to-ally choice");
            ensureT007TargetIsLivingTeammate(
                gameState,
                submitInputChoiceActionRequest.actorPlayerId,
                targetAllyPlayerId,
                ContinuationKeyT007OnPlayChooseAllyMode);

            var sourceCardInstanceId = tryResolveSourceCardInstanceId(actionChainState);
            if (!sourceCardInstanceId.HasValue)
            {
                throw new InvalidOperationException($"{ContinuationKeyT007OnPlayChooseAllyMode} requires source card instance id.");
            }

            moveT007SourceCardToAllyHand(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                submitInputChoiceActionRequest.actorPlayerId,
                sourceCardInstanceId.Value,
                targetAllyPlayerId);
            actionChainState.pendingContinuationKey = null;
            return;
        }

        if (submitInputChoiceActionRequest.choiceKey.StartsWith(ChoiceKeyT007AllyDiscardForManaPrefix, StringComparison.Ordinal))
        {
            var targetAllyPlayerId = parsePlayerChoiceKeyWithPrefix(
                submitInputChoiceActionRequest.choiceKey,
                ChoiceKeyT007AllyDiscardForManaPrefix,
                "T007 ally-discard choice");
            ensureT007TargetIsLivingTeammate(
                gameState,
                submitInputChoiceActionRequest.actorPlayerId,
                targetAllyPlayerId,
                ContinuationKeyT007OnPlayChooseAllyMode);

            var discardChoiceKeys = createDiscardChoiceKeysFromHand(gameState, targetAllyPlayerId);
            discardChoiceKeys.Insert(0, ChoiceKeyDeclineDiscard);
            openInputContext(
                gameState,
                actionChainState,
                submitInputChoiceActionRequest.requestId,
                targetAllyPlayerId,
                InputTypeKeyTreasureOnPlayDiscardCardChoice,
                ContextKeyT007OnPlayTargetAllyDiscardCard,
                ContinuationKeyT007OnPlayTargetAllyDiscardCard,
                discardChoiceKeys);
            return;
        }

        throw new InvalidOperationException($"{ContinuationKeyT007OnPlayChooseAllyMode} requires a T007 choiceKey.");
    }

    private void continueT007TargetAllyDiscardCard(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (string.Equals(submitInputChoiceActionRequest.choiceKey, ChoiceKeyDeclineDiscard, StringComparison.Ordinal))
        {
            return;
        }

        discardSelectedCardFromActorHand(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            submitInputChoiceActionRequest.actorPlayerId,
            submitInputChoiceActionRequest.choiceKey,
            ContinuationKeyT007OnPlayTargetAllyDiscardCard);

        var sourcePlayerId = resolveRootActorPlayerId(actionChainState, ContinuationKeyT007OnPlayTargetAllyDiscardCard);
        gameState.players[sourcePlayerId].mana += 3;
    }

    private static List<string> collectT022OptionalBanishChoiceKeys(
        GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        var actorPlayerState = gameState.players[actorPlayerId];
        var choiceKeys = new List<string> { ChoiceKeyDeclineOptionalBanish };

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

    private static List<string> collectOptionalBanishChoiceKeysFromSummonZone(
        GameState.GameState gameState)
    {
        var choiceKeys = new List<string> { ChoiceKeyDeclineOptionalBanish };
        if (gameState.publicState is null)
        {
            return choiceKeys;
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.summonZoneId, out var summonZoneState))
        {
            return choiceKeys;
        }

        foreach (var cardInstanceId in summonZoneState.cardInstanceIds)
        {
            choiceKeys.Add(ChoiceKeyBanishCardPrefix + cardInstanceId.Value);
        }

        return choiceKeys;
    }

    private static List<string> createDiscardChoiceKeysFromHand(
        GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        var actorPlayerState = gameState.players[actorPlayerId];
        var handZoneState = gameState.zones[actorPlayerState.handZoneId];
        var choiceKeys = new List<string>(handZoneState.cardInstanceIds.Count);
        foreach (var cardInstanceId in handZoneState.cardInstanceIds)
        {
            choiceKeys.Add(ChoiceKeyDiscardCardPrefix + cardInstanceId.Value);
        }

        return choiceKeys;
    }

    private static List<string> collectT021OverlayChoiceKeysFromHand(
        GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        var actorPlayerState = gameState.players[actorPlayerId];
        var handZoneState = gameState.zones[actorPlayerState.handZoneId];
        var choiceKeys = new List<string>(handZoneState.cardInstanceIds.Count + 1)
        {
            ChoiceKeyDeclineOverlay,
        };

        foreach (var cardInstanceId in handZoneState.cardInstanceIds)
        {
            choiceKeys.Add(ChoiceKeyOverlayCardPrefix + cardInstanceId.Value);
        }

        return choiceKeys;
    }

    private static List<string> createT007AllyModeChoiceKeys(
        GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        var actorTeamId = gameState.players[actorPlayerId].teamId;
        var choiceKeys = new List<string>();
        foreach (var playerState in gameState.players.Values)
        {
            if (playerState.playerId == actorPlayerId ||
                playerState.teamId != actorTeamId ||
                !hasAliveInPlayActiveCharacter(gameState, playerState.playerId))
            {
                continue;
            }

            choiceKeys.Add(ChoiceKeyT007GiveToAllyPrefix + playerState.playerId.Value);
            choiceKeys.Add(ChoiceKeyT007AllyDiscardForManaPrefix + playerState.playerId.Value);
        }

        return choiceKeys;
    }

    private static List<string> createDiscardChoiceKeysFromDiscard(
        GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        var actorPlayerState = gameState.players[actorPlayerId];
        var discardZoneState = gameState.zones[actorPlayerState.discardZoneId];
        var choiceKeys = new List<string>(discardZoneState.cardInstanceIds.Count);
        foreach (var cardInstanceId in discardZoneState.cardInstanceIds)
        {
            choiceKeys.Add(ChoiceKeyDiscardCardPrefix + cardInstanceId.Value);
        }

        return choiceKeys;
    }

    private static List<string> createMoveToHandChoiceKeys()
    {
        return new List<string>
        {
            ChoiceKeyAcceptMoveToHand,
            ChoiceKeyDeclineMoveToHand,
        };
    }

    private void drawCardsForPlayer(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId playerId,
        long requestId,
        int drawCount)
    {
        _ = drawCardsForPlayerAndReturnDrawnCardInstanceIds(
            gameState,
            actionChainState,
            playerId,
            requestId,
            drawCount);
    }

    private List<CardInstanceId> drawCardsForPlayerAndReturnDrawnCardInstanceIds(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayerId playerId,
        long requestId,
        int drawCount)
    {
        var drawnCardInstanceIds = new List<CardInstanceId>();
        if (drawCount <= 0)
        {
            return drawnCardInstanceIds;
        }

        var playerState = gameState.players[playerId];
        var deckZoneState = gameState.zones[playerState.deckZoneId];
        var discardZoneState = gameState.zones[playerState.discardZoneId];

        for (var drawIndex = 0; drawIndex < drawCount; drawIndex++)
        {
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
                break;
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
            drawnCardInstanceIds.Add(topCardInstanceId);
        }

        return drawnCardInstanceIds;
    }

    private void discardSelectedCardFromActorHand(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId actorPlayerId,
        string choiceKey,
        string continuationKey)
    {
        var selectedCardInstanceId = parseCardChoiceKey(choiceKey, ChoiceKeyDiscardCardPrefix, "T027 discard choice");
        var actorPlayerState = gameState.players[actorPlayerId];
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException($"{continuationKey} requires selected discard card instance to exist.");
        }

        if (selectedCardInstance.ownerPlayerId != actorPlayerId)
        {
            throw new InvalidOperationException($"{continuationKey} requires selected discard card to be owned by actor player.");
        }

        if (selectedCardInstance.zoneId != actorPlayerState.handZoneId)
        {
            throw new InvalidOperationException($"{continuationKey} requires selected discard card to be in actor hand zone.");
        }

        var discardEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            actorPlayerState.discardZoneId,
            CardMoveReason.discard,
            actionChainState.actionChainId,
            requestId);
        actionChainState.producedEvents.Add(discardEvent);
    }

    private void continueT021OverlayCardsForMana(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest,
        CardInstanceId? sourceCardInstanceId)
    {
        if (!sourceCardInstanceId.HasValue)
        {
            throw new InvalidOperationException("T021 overlay continuation requires sourceCardInstanceId.");
        }

        if (!gameState.cardInstances.TryGetValue(sourceCardInstanceId.Value, out var sourceCardInstance))
        {
            throw new InvalidOperationException("T021 overlay continuation requires source card instance to exist.");
        }

        if (!gameState.players.TryGetValue(submitInputChoiceActionRequest.actorPlayerId, out var actorPlayerState))
        {
            throw new InvalidOperationException("T021 overlay continuation requires actor player to exist.");
        }

        if (sourceCardInstance.ownerPlayerId != submitInputChoiceActionRequest.actorPlayerId ||
            sourceCardInstance.zoneId != actorPlayerState.fieldZoneId ||
            !string.Equals(sourceCardInstance.definitionId, "T021", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T021 overlay continuation requires source T021 card to remain in actor field zone.");
        }

        var selectedChoiceKeys = collectT021SelectedOverlayChoiceKeys(submitInputChoiceActionRequest);
        var overlaidCount = 0;
        foreach (var selectedChoiceKey in selectedChoiceKeys)
        {
            var selectedCardInstanceId = parseCardChoiceKey(
                selectedChoiceKey,
                ChoiceKeyOverlayCardPrefix,
                "T021 overlay choice");
            if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
            {
                throw new InvalidOperationException("T021 overlay continuation requires selected card instance to exist.");
            }

            if (selectedCardInstance.ownerPlayerId != submitInputChoiceActionRequest.actorPlayerId)
            {
                throw new InvalidOperationException("T021 overlay continuation requires selected overlay card to be owned by actor player.");
            }

            if (selectedCardInstance.zoneId != actorPlayerState.handZoneId)
            {
                throw new InvalidOperationException("T021 overlay continuation requires selected overlay card to still be in actor hand zone.");
            }

            var overlayEvent = OverlayRuntime.overlayCardUnderContainer(
                gameState,
                zoneMovementService,
                selectedCardInstance,
                sourceCardInstance,
                actionChainState.actionChainId,
                submitInputChoiceActionRequest.requestId);
            actionChainState.producedEvents.Add(overlayEvent);
            overlaidCount += 1;
        }

        if (overlaidCount > 0)
        {
            actorPlayerState.mana += overlaidCount * T021ManaGainPerOverlayCard;
        }
    }

    private void banishSelectedCardFromActorHandOrDiscard(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId actorPlayerId,
        CardInstanceId selectedCardInstanceId,
        string continuationKey)
    {
        if (gameState.publicState is null)
        {
            throw new InvalidOperationException($"{continuationKey} requires gameState.publicState to be initialized.");
        }

        var actorPlayerState = gameState.players[actorPlayerId];
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException($"{continuationKey} requires selected banish card instance to exist.");
        }

        if (selectedCardInstance.ownerPlayerId != actorPlayerId)
        {
            throw new InvalidOperationException($"{continuationKey} requires selected banish card to be owned by actor player.");
        }

        var isFromActorHand = selectedCardInstance.zoneId == actorPlayerState.handZoneId;
        var isFromActorDiscard = selectedCardInstance.zoneId == actorPlayerState.discardZoneId;
        if (!isFromActorHand && !isFromActorDiscard)
        {
            throw new InvalidOperationException($"{continuationKey} requires selected banish card to be in actor hand or discard zone.");
        }

        var banishEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            gameState.publicState.gapZoneId,
            CardMoveReason.banish,
            actionChainState.actionChainId,
            requestId);
        actionChainState.producedEvents.Add(banishEvent);
    }

    private void banishSelectedCardFromSummonZone(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        CardInstanceId selectedCardInstanceId,
        string continuationKey)
    {
        if (gameState.publicState is null)
        {
            throw new InvalidOperationException($"{continuationKey} requires gameState.publicState to be initialized.");
        }

        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException($"{continuationKey} requires selected summonZone card instance to exist.");
        }

        if (selectedCardInstance.zoneId != gameState.publicState.summonZoneId)
        {
            throw new InvalidOperationException($"{continuationKey} requires selected banish card to be in summonZone.");
        }

        var banishEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            gameState.publicState.gapZoneId,
            CardMoveReason.banish,
            actionChainState.actionChainId,
            requestId);
        actionChainState.producedEvents.Add(banishEvent);

        // SummonZone cards are face-up offers; when one leaves the zone, refill from
        // publicTreasureDeck top immediately if possible.
        refillSummonZoneFromPublicTreasureDeckIfAvailable(
            gameState,
            actionChainState,
            requestId,
            continuationKey);
    }

    private void continueT017SelectSummonZoneCard(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var selectedCardInstanceId = parseCardChoiceKey(
            submitInputChoiceActionRequest.choiceKey,
            ChoiceKeySummonZoneCardPrefix,
            "T017 summonZone card choice");

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("T017 on-play continuation requires gameState.publicState.");
        }

        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("T017 on-play continuation requires selected summonZone card to exist.");
        }

        if (selectedCardInstance.zoneId != gameState.publicState.summonZoneId)
        {
            throw new InvalidOperationException("T017 on-play continuation requires selected card to be in summonZone.");
        }

        actionChainState.localState[LocalStateKeyT017SelectedSummonZoneCardInstanceId] =
            selectedCardInstanceId.Value.ToString();

        var friendlyChoiceKeys = createChoiceKeysForEffect(
            gameState,
            submitInputChoiceActionRequest.actorPlayerId,
            OnPlayEffectKind.t017MoveSummonZoneCardToFriendlyDeckTop);
        if (friendlyChoiceKeys.Count <= 0)
        {
            actionChainState.pendingContinuationKey = null;
            actionChainState.localState.Remove(LocalStateKeyT017SelectedSummonZoneCardInstanceId);
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            submitInputChoiceActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayTargetPlayerChoice,
            ContextKeyT017OnPlayTargetFriendlyDeckTop,
            ContinuationKeyT017OnPlayTargetFriendlyDeckTop,
            friendlyChoiceKeys);
    }

    private void continueT017TargetFriendlyDeckTop(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId actorPlayerId,
        PlayerId targetPlayerId)
    {
        if (!actionChainState.localState.TryGetValue(
                LocalStateKeyT017SelectedSummonZoneCardInstanceId,
                out var selectedCardIdText) ||
            !long.TryParse(selectedCardIdText, out var selectedCardNumericId))
        {
            throw new InvalidOperationException("T017 on-play target continuation requires selected summonZone card local state.");
        }

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("T017 on-play target continuation requires gameState.publicState.");
        }

        var selectedCardInstanceId = new CardInstanceId(selectedCardNumericId);
        if (!gameState.cardInstances.TryGetValue(selectedCardInstanceId, out var selectedCardInstance))
        {
            throw new InvalidOperationException("T017 on-play target continuation requires selected card instance to exist.");
        }

        if (selectedCardInstance.zoneId != gameState.publicState.summonZoneId)
        {
            throw new InvalidOperationException("T017 on-play target continuation requires selected card to still be in summonZone.");
        }

        if (!gameState.players.TryGetValue(actorPlayerId, out var actorPlayerState) ||
            !gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState))
        {
            throw new InvalidOperationException("T017 on-play target continuation requires actor and target players to exist.");
        }

        if (targetPlayerState.teamId != actorPlayerState.teamId)
        {
            throw new InvalidOperationException("T017 on-play target continuation requires target player to be friendly.");
        }

        if (!hasAliveInPlayActiveCharacter(gameState, targetPlayerId))
        {
            throw new InvalidOperationException("T017 on-play target continuation requires target player to have an alive in-play active character.");
        }

        selectedCardInstance.ownerPlayerId = targetPlayerId;
        selectedCardInstance.isFaceUp = false;
        var movedEvent = zoneMovementService.moveCard(
            gameState,
            selectedCardInstance,
            targetPlayerState.deckZoneId,
            CardMoveReason.returnToSource,
            actionChainState.actionChainId,
            requestId);
        actionChainState.producedEvents.Add(movedEvent);

        var targetDeckZoneState = gameState.zones[targetPlayerState.deckZoneId];
        targetDeckZoneState.cardInstanceIds.Remove(selectedCardInstanceId);
        targetDeckZoneState.cardInstanceIds.Insert(0, selectedCardInstanceId);

        actionChainState.localState.Remove(LocalStateKeyT017SelectedSummonZoneCardInstanceId);
        refillSummonZoneFromPublicTreasureDeckIfAvailable(
            gameState,
            actionChainState,
            requestId,
            ContinuationKeyT017OnPlayTargetFriendlyDeckTop);
    }

    private void openT026GrantMarkerInputContextIfPossible(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        var choiceKeys = collectT026MarkerGrantChoiceKeys(gameState);
        if (choiceKeys.Count <= 1)
        {
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayMarkerGrantChoice,
            ContextKeyT026OnPlayGrantMarker,
            ContinuationKeyT026OnPlayGrantMarker,
            choiceKeys);
    }

    private static void continueT026GrantMarker(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (string.Equals(
                submitInputChoiceActionRequest.choiceKey,
                ChoiceKeyDeclineMarkerGrant,
                StringComparison.Ordinal))
        {
            return;
        }

        var (targetPlayerId, markerTypeKey) = parseMarkerGrantChoiceKey(submitInputChoiceActionRequest.choiceKey);
        if (!tryResolveAliveInPlayActiveCharacter(gameState, targetPlayerId, out var targetCharacterInstance))
        {
            throw new InvalidOperationException("T026 marker grant continuation requires target player to have an alive in-play active character.");
        }

        if (!MarkerRuntime.canAddMarker(targetCharacterInstance, markerTypeKey))
        {
            throw new InvalidOperationException("T026 marker grant continuation requires selected markerTypeKey to be allowed and below cap.");
        }

        actionChainState.producedEvents.Add(MarkerRuntime.addMarker(
            targetCharacterInstance,
            markerTypeKey,
            delta: 1,
            actionChainState.actionChainId,
            submitInputChoiceActionRequest.requestId));
    }

    private static List<string> collectT026MarkerGrantChoiceKeys(GameState.GameState gameState)
    {
        var choiceKeys = new List<string>
        {
            ChoiceKeyDeclineMarkerGrant,
        };

        foreach (var playerState in gameState.players.Values.OrderBy(player => player.playerId.Value))
        {
            if (!tryResolveAliveInPlayActiveCharacter(gameState, playerState.playerId, out var characterInstance))
            {
                continue;
            }

            var characterDefinition = CharacterDefinitionRepository.resolveByDefinitionId(characterInstance.definitionId);
            foreach (var markerTypeKey in characterDefinition.allowedMarkerTypes.OrderBy(marker => marker, StringComparer.Ordinal))
            {
                if (!MarkerRuntime.canAddMarker(characterInstance, markerTypeKey))
                {
                    continue;
                }

                choiceKeys.Add(createMarkerGrantChoiceKey(playerState.playerId, markerTypeKey));
            }
        }

        return choiceKeys;
    }

    private static string createMarkerGrantChoiceKey(PlayerId targetPlayerId, string markerTypeKey)
    {
        return ChoiceKeyMarkerGrantPrefix + targetPlayerId.Value + ":" + markerTypeKey;
    }

    private static (PlayerId targetPlayerId, string markerTypeKey) parseMarkerGrantChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(ChoiceKeyMarkerGrantPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T026 marker grant choice requires choiceKey prefix markerGrant:.");
        }

        var segments = choiceKey.Substring(ChoiceKeyMarkerGrantPrefix.Length)
            .Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 2 ||
            !long.TryParse(segments[0], out var playerNumericId) ||
            playerNumericId <= 0 ||
            string.IsNullOrWhiteSpace(segments[1]))
        {
            throw new InvalidOperationException("T026 marker grant choice requires markerGrant:{playerNumericId}:{markerTypeKey}.");
        }

        return (new PlayerId(playerNumericId), segments[1]);
    }

    private void refillSummonZoneFromPublicTreasureDeckIfAvailable(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        string continuationKey)
    {
        if (gameState.publicState is null)
        {
            throw new InvalidOperationException($"{continuationKey} requires gameState.publicState to be initialized.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.publicTreasureDeckZoneId, out var publicTreasureDeckZoneState))
        {
            throw new InvalidOperationException($"{continuationKey} requires publicTreasureDeck zone to exist in gameState.zones.");
        }

        if (!gameState.zones.ContainsKey(gameState.publicState.summonZoneId))
        {
            throw new InvalidOperationException($"{continuationKey} requires summonZone to exist in gameState.zones.");
        }

        if (publicTreasureDeckZoneState.cardInstanceIds.Count <= 0)
        {
            return;
        }

        var topPublicTreasureCardInstanceId = publicTreasureDeckZoneState.cardInstanceIds[0];
        var topPublicTreasureCardInstance = gameState.cardInstances[topPublicTreasureCardInstanceId];
        var refillEvent = zoneMovementService.moveCard(
            gameState,
            topPublicTreasureCardInstance,
            gameState.publicState.summonZoneId,
            CardMoveReason.reveal,
            actionChainState.actionChainId,
            requestId);
        actionChainState.producedEvents.Add(refillEvent);
    }

    private static CardInstanceId parseCardChoiceKey(
        string choiceKey,
        string requiredPrefix,
        string choiceName)
    {
        if (!choiceKey.StartsWith(requiredPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{choiceName} requires choiceKey to start with {requiredPrefix}.");
        }

        var cardIdSegment = choiceKey.Substring(requiredPrefix.Length);
        if (!long.TryParse(cardIdSegment, out var cardNumericId) || cardNumericId <= 0)
        {
            throw new InvalidOperationException($"{choiceName} requires choiceKey numeric segment to be a positive integer.");
        }

        return new CardInstanceId(cardNumericId);
    }

    private static List<string> collectT021SelectedOverlayChoiceKeys(
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (submitInputChoiceActionRequest.choiceKeys.Count > 0)
        {
            return new List<string>(submitInputChoiceActionRequest.choiceKeys);
        }

        if (!string.IsNullOrWhiteSpace(submitInputChoiceActionRequest.choiceKey) &&
            !string.Equals(submitInputChoiceActionRequest.choiceKey, ChoiceKeyDeclineOverlay, StringComparison.Ordinal))
        {
            return new List<string> { submitInputChoiceActionRequest.choiceKey };
        }

        return new List<string>();
    }

    private static PlayerId parsePlayerChoiceKeyWithPrefix(
        string choiceKey,
        string requiredPrefix,
        string choiceName)
    {
        if (!choiceKey.StartsWith(requiredPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{choiceName} requires choiceKey to start with {requiredPrefix}.");
        }

        var playerIdSegment = choiceKey.Substring(requiredPrefix.Length);
        if (!long.TryParse(playerIdSegment, out var playerNumericId) || playerNumericId <= 0)
        {
            throw new InvalidOperationException($"{choiceName} requires player id segment to be a positive integer.");
        }

        return new PlayerId(playerNumericId);
    }

    private static PlayerId resolveRootActorPlayerId(
        ActionChainState actionChainState,
        string continuationKey)
    {
        if (actionChainState.rootActionRequest is PlayTreasureCardActionRequest playTreasureCardActionRequest)
        {
            return playTreasureCardActionRequest.actorPlayerId;
        }

        throw new InvalidOperationException($"{continuationKey} requires rootActionRequest to be PlayTreasureCardActionRequest.");
    }

    private static void ensureT007TargetIsLivingTeammate(
        GameState.GameState gameState,
        PlayerId sourcePlayerId,
        PlayerId targetAllyPlayerId,
        string continuationKey)
    {
        if (!gameState.players.TryGetValue(sourcePlayerId, out var sourcePlayerState))
        {
            throw new InvalidOperationException($"{continuationKey} requires source player to exist.");
        }

        if (!gameState.players.TryGetValue(targetAllyPlayerId, out var targetPlayerState))
        {
            throw new InvalidOperationException($"{continuationKey} requires selected target ally player to exist.");
        }

        if (targetAllyPlayerId == sourcePlayerId || targetPlayerState.teamId != sourcePlayerState.teamId)
        {
            throw new InvalidOperationException($"{continuationKey} requires selected target player to be a teammate.");
        }

        if (!hasAliveInPlayActiveCharacter(gameState, targetAllyPlayerId))
        {
            throw new InvalidOperationException($"{continuationKey} requires selected target teammate to have an alive in-play active character.");
        }
    }

    private void moveT007SourceCardToAllyHand(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId sourceCardInstanceId,
        PlayerId targetAllyPlayerId)
    {
        if (!gameState.cardInstances.TryGetValue(sourceCardInstanceId, out var sourceCardInstance))
        {
            throw new InvalidOperationException($"{ContinuationKeyT007OnPlayChooseAllyMode} requires source card instance to exist.");
        }

        if (!gameState.players.TryGetValue(sourcePlayerId, out var sourcePlayerState) ||
            !gameState.players.TryGetValue(targetAllyPlayerId, out var targetAllyPlayerState))
        {
            throw new InvalidOperationException($"{ContinuationKeyT007OnPlayChooseAllyMode} requires source and target player states to exist.");
        }

        if (sourceCardInstance.ownerPlayerId != sourcePlayerId)
        {
            throw new InvalidOperationException($"{ContinuationKeyT007OnPlayChooseAllyMode} requires source card to be owned by source player.");
        }

        if (sourceCardInstance.zoneId != sourcePlayerState.fieldZoneId)
        {
            throw new InvalidOperationException($"{ContinuationKeyT007OnPlayChooseAllyMode} requires source card to remain in source player field.");
        }

        sourceCardInstance.ownerPlayerId = targetAllyPlayerId;
        sourceCardInstance.isDefensePlacedOnField = false;
        var movedEvent = zoneMovementService.moveCard(
            gameState,
            sourceCardInstance,
            targetAllyPlayerState.handZoneId,
            CardMoveReason.returnToSource,
            actionChainState.actionChainId,
            requestId);
        actionChainState.producedEvents.Add(movedEvent);
    }

    private void applySelfShackle(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId actorPlayerId,
        CardInstanceId? sourceCardInstanceId)
    {
        var actorCharacterInstance = resolveAliveInPlayActiveCharacter(
            gameState,
            actorPlayerId,
            ContinuationKeyT022OnPlayOptionalBanishThenSelfShackle);

        var appliedStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = StatusKeyShackle,
                applierPlayerId = actorPlayerId,
                applierCardInstanceId = sourceCardInstanceId,
                targetCharacterInstanceId = actorCharacterInstance.characterInstanceId,
                stackCount = 1,
            });

        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = requestId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = appliedStatus.statusKey,
            targetPlayerId = actorCharacterInstance.ownerPlayerId,
            targetCharacterInstanceId = actorCharacterInstance.characterInstanceId,
            isApplied = true,
        });
    }

    private static void applyT028SelfNextDamagePenetrate(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId actorPlayerId,
        CardInstanceId sourceCardInstanceId)
    {
        var appliedStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = StatusKeyPenetrate,
                applierPlayerId = actorPlayerId,
                applierCardInstanceId = sourceCardInstanceId,
                targetPlayerId = actorPlayerId,
                stackCount = 1,
                durationTypeKey = StatusRuntime.DurationTypeKeyNextDamageAttempt,
            });

        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = requestId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = appliedStatus.statusKey,
            targetPlayerId = appliedStatus.targetPlayerId,
            isApplied = true,
        });
    }

    public static void ensureValidT021OverlayChoiceRequest(
        InputContextState inputContextState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        if (!string.Equals(inputContextState.contextKey, ContextKeyT021OnPlayOverlayCardsForMana, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T021 overlay continuation requires currentInputContext.contextKey to be treasureOnPlay:T021:onPlayOverlayCardsForMana.");
        }

        if (string.Equals(submitInputChoiceActionRequest.choiceKey, ChoiceKeyDeclineOverlay, StringComparison.Ordinal))
        {
            if (submitInputChoiceActionRequest.choiceKeys.Count > 0)
            {
                throw new InvalidOperationException("T021 overlay continuation requires choiceKeys to be empty when choiceKey is overlay:decline.");
            }

            return;
        }

        var selectedChoiceKeys = collectT021SelectedOverlayChoiceKeys(submitInputChoiceActionRequest);
        if (selectedChoiceKeys.Count <= 0)
        {
            throw new InvalidOperationException("T021 overlay continuation requires overlay:decline, one overlayCard choiceKey, or one to two overlayCard choiceKeys.");
        }

        if (selectedChoiceKeys.Count > T021MaxOverlayCount)
        {
            throw new InvalidOperationException("T021 overlay continuation supports at most two selected overlayCard choiceKeys.");
        }

        var uniqueChoiceKeys = new HashSet<string>(selectedChoiceKeys, StringComparer.Ordinal);
        if (uniqueChoiceKeys.Count != selectedChoiceKeys.Count)
        {
            throw new InvalidOperationException("T021 overlay continuation requires selected overlayCard choiceKeys to be unique.");
        }

        foreach (var selectedChoiceKey in selectedChoiceKeys)
        {
            if (!selectedChoiceKey.StartsWith(ChoiceKeyOverlayCardPrefix, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("T021 overlay continuation requires selected choiceKeys to use overlayCard:{id} format.");
            }

            if (!inputContextState.choiceKeys.Contains(selectedChoiceKey))
            {
                throw new InvalidOperationException("T021 overlay continuation requires every selected overlayCard choiceKey to be one of currentInputContext.choiceKeys.");
            }
        }
    }

    private void openInputContext(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId requiredPlayerId,
        string inputTypeKey,
        string contextKey,
        string continuationKey,
        List<string> choiceKeys)
    {
        if (gameState.currentInputContext is not null)
        {
            throw new InvalidOperationException("Treasure on-play input context opening requires currentInputContext to be null.");
        }

        var inputContextId = new InputContextId(nextInputContextIdSupplier());
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

        actionChainState.producedEvents.Add(new InteractionWindowEvent
        {
            eventId = requestId,
            eventTypeKey = "inputContextOpened",
            sourceActionChainId = actionChainState.actionChainId,
            windowKindKey = "inputContext",
            inputContextId = inputContextId,
            isOpened = true,
        });
    }

    private static bool hasAliveInPlayActiveCharacter(
        GameState.GameState gameState,
        PlayerId playerId)
    {
        var playerState = gameState.players[playerId];
        if (!playerState.activeCharacterInstanceId.HasValue)
        {
            return false;
        }

        if (!gameState.characterInstances.TryGetValue(playerState.activeCharacterInstanceId.Value, out var activeCharacterInstance))
        {
            return false;
        }

        return activeCharacterInstance.isAlive && activeCharacterInstance.isInPlay;
    }

    private static List<string> createT023HumanCharacterChoiceKeys(GameState.GameState gameState)
    {
        var choiceKeys = new List<string>();
        foreach (var playerState in gameState.players.Values.OrderBy(player => player.playerId.Value))
        {
            if (!tryResolveAliveInPlayActiveCharacter(gameState, playerState.playerId, out var activeCharacterInstance))
            {
                continue;
            }

            if (!hasRaceTag(activeCharacterInstance, RaceTagHuman))
            {
                continue;
            }

            choiceKeys.Add(createCharacterChoiceKey(activeCharacterInstance.characterInstanceId));
        }

        return choiceKeys;
    }

    private static List<string> createT023EnemyNonHumanCharacterChoiceKeys(
        GameState.GameState gameState,
        PlayerId actorPlayerId)
    {
        var choiceKeys = new List<string>();
        var actorTeamId = gameState.players[actorPlayerId].teamId;
        foreach (var playerState in gameState.players.Values.OrderBy(player => player.playerId.Value))
        {
            if (playerState.teamId == actorTeamId)
            {
                continue;
            }

            if (!tryResolveAliveInPlayActiveCharacter(gameState, playerState.playerId, out var activeCharacterInstance))
            {
                continue;
            }

            if (!hasRaceTag(activeCharacterInstance, RaceTagNonHuman))
            {
                continue;
            }

            choiceKeys.Add(createCharacterChoiceKey(activeCharacterInstance.characterInstanceId));
        }

        return choiceKeys;
    }

    private static bool tryResolveAliveInPlayActiveCharacter(
        GameState.GameState gameState,
        PlayerId playerId,
        out CharacterInstance activeCharacterInstance)
    {
        activeCharacterInstance = null!;
        if (!gameState.players.TryGetValue(playerId, out var playerState) ||
            !playerState.activeCharacterInstanceId.HasValue)
        {
            return false;
        }

        if (!gameState.characterInstances.TryGetValue(playerState.activeCharacterInstanceId.Value, out activeCharacterInstance))
        {
            return false;
        }

        return activeCharacterInstance.isAlive && activeCharacterInstance.isInPlay;
    }

    private static CharacterInstance resolveAliveInPlayActiveCharacter(
        GameState.GameState gameState,
        PlayerId targetPlayerId,
        string continuationKey)
    {
        if (!gameState.players.TryGetValue(targetPlayerId, out var targetPlayerState))
        {
            throw new InvalidOperationException($"{continuationKey} requires selected target player to exist.");
        }

        if (!targetPlayerState.activeCharacterInstanceId.HasValue)
        {
            throw new InvalidOperationException($"{continuationKey} requires selected target player to have an active character.");
        }

        if (!gameState.characterInstances.TryGetValue(targetPlayerState.activeCharacterInstanceId.Value, out var targetCharacterInstance))
        {
            throw new InvalidOperationException($"{continuationKey} requires selected active character instance to exist.");
        }

        if (!targetCharacterInstance.isAlive || !targetCharacterInstance.isInPlay)
        {
            throw new InvalidOperationException($"{continuationKey} requires selected active character to be alive and in play.");
        }

        return targetCharacterInstance;
    }

    private static CharacterInstance resolveAliveInPlayCharacterByInstanceId(
        GameState.GameState gameState,
        CharacterInstanceId targetCharacterInstanceId,
        string continuationKey)
    {
        if (!gameState.characterInstances.TryGetValue(targetCharacterInstanceId, out var targetCharacterInstance))
        {
            throw new InvalidOperationException($"{continuationKey} requires selected target character instance to exist.");
        }

        if (!targetCharacterInstance.isAlive || !targetCharacterInstance.isInPlay)
        {
            throw new InvalidOperationException($"{continuationKey} requires selected target character to be alive and in play.");
        }

        return targetCharacterInstance;
    }

    private static bool hasRaceTag(CharacterInstance characterInstance, string raceTag)
    {
        return characterInstance.raceTags.Any(
            existingRaceTag => string.Equals(existingRaceTag, raceTag, StringComparison.Ordinal));
    }

    private static CardInstanceId? tryResolveSourceCardInstanceId(ActionChainState actionChainState)
    {
        if (actionChainState.rootActionRequest is PlayTreasureCardActionRequest playTreasureCardActionRequest)
        {
            return playTreasureCardActionRequest.cardInstanceId;
        }

        return null;
    }

    private static string createPlayerChoiceKey(PlayerId playerId)
    {
        return ChoiceKeyPlayerPrefix + playerId.Value;
    }

    private static PlayerId parsePlayerChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(ChoiceKeyPlayerPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Treasure on-play input choice requires choiceKey to use player:{id} format.");
        }

        var playerIdSegment = choiceKey.Substring(ChoiceKeyPlayerPrefix.Length);
        if (!long.TryParse(playerIdSegment, out var playerNumericId) || playerNumericId <= 0)
        {
            throw new InvalidOperationException("Treasure on-play input choice player id segment must be a positive integer.");
        }

        return new PlayerId(playerNumericId);
    }

    private static string createCharacterChoiceKey(CharacterInstanceId characterInstanceId)
    {
        return ChoiceKeyCharacterPrefix + characterInstanceId.Value;
    }

    private static CharacterInstanceId parseCharacterChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(ChoiceKeyCharacterPrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Treasure on-play input choice requires choiceKey to use character:{id} format.");
        }

        var characterIdSegment = choiceKey.Substring(ChoiceKeyCharacterPrefix.Length);
        if (!long.TryParse(characterIdSegment, out var characterNumericId) || characterNumericId <= 0)
        {
            throw new InvalidOperationException("Treasure on-play input choice character id segment must be a positive integer.");
        }

        return new CharacterInstanceId(characterNumericId);
    }

    private static string parseDeclareCardNameChoiceKey(string choiceKey)
    {
        if (!choiceKey.StartsWith(ChoiceKeyDeclareCardNamePrefix, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("T013 declare card name choice requires choiceKey to use declareCardName:{definitionId} format.");
        }

        var definitionId = choiceKey.Substring(ChoiceKeyDeclareCardNamePrefix.Length);
        if (string.IsNullOrWhiteSpace(definitionId))
        {
            throw new InvalidOperationException("T013 declare card name choice requires a non-empty definition id.");
        }

        return definitionId;
    }

    private static void continueT002TargetHeal1(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId targetPlayerId)
    {
        var targetCharacterInstance = resolveAliveInPlayActiveCharacter(
            gameState,
            targetPlayerId,
            ContinuationKeyT002OnPlayTargetHeal1);

        var hpBefore = targetCharacterInstance.currentHp;
        var hpAfter = Math.Min(targetCharacterInstance.maxHp, hpBefore + 1);
        if (hpAfter == hpBefore)
        {
            return;
        }

        targetCharacterInstance.currentHp = hpAfter;
        actionChainState.producedEvents.Add(new HpChangedEvent
        {
            eventId = requestId,
            eventTypeKey = "hpChanged",
            sourceActionChainId = actionChainState.actionChainId,
            targetPlayerId = targetCharacterInstance.ownerPlayerId,
            targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
            hpBefore = hpBefore,
            hpAfter = hpAfter,
            delta = hpAfter - hpBefore,
        });
    }

    private void continueT001TargetOpponentSilenceAndDraw1(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        PlayerId targetPlayerId)
    {
        if (gameState.players[sourcePlayerId].teamId == gameState.players[targetPlayerId].teamId)
        {
            throw new InvalidOperationException("T001 on-play continuation requires selected target player to be an opponent.");
        }

        var targetCharacterInstance = resolveAliveInPlayActiveCharacter(
            gameState,
            targetPlayerId,
            ContinuationKeyT001OnPlayTargetOpponentSilenceAndDraw1);

        var appliedStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = StatusKeySilence,
                applierPlayerId = sourcePlayerId,
                applierCardInstanceId = sourceCardInstanceId,
                targetPlayerId = targetPlayerId,
                stackCount = 1,
            });

        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = requestId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = appliedStatus.statusKey,
            targetPlayerId = targetPlayerId,
            targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
            isApplied = true,
        });

        drawCardsForPlayer(
            gameState,
            actionChainState,
            sourcePlayerId,
            requestId,
            drawCount: 1);
    }

    private void continueT013DeclareCardName(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        SubmitInputChoiceActionRequest submitInputChoiceActionRequest)
    {
        var declaredDefinitionId = parseDeclareCardNameChoiceKey(submitInputChoiceActionRequest.choiceKey);
        if (!TreasureDefinitionRepository.getDeclarableTreasureDefinitionIds().Contains(declaredDefinitionId))
        {
            throw new InvalidOperationException("T013 on-play declaration requires declared definition id to be declarable.");
        }

        var drawnCardInstanceIds = drawCardsForPlayerAndReturnDrawnCardInstanceIds(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.actorPlayerId,
            submitInputChoiceActionRequest.requestId,
            drawCount: 1);
        if (drawnCardInstanceIds.Count == 0)
        {
            actionChainState.pendingContinuationKey = null;
            return;
        }

        var revealedCardInstance = gameState.cardInstances[drawnCardInstanceIds[0]];
        actionChainState.producedEvents.Add(new CardRevealedEvent
        {
            eventId = submitInputChoiceActionRequest.requestId,
            eventTypeKey = "cardRevealed",
            sourceActionChainId = actionChainState.actionChainId,
            cardInstanceId = revealedCardInstance.cardInstanceId,
            ownerPlayerId = revealedCardInstance.ownerPlayerId,
            definitionId = revealedCardInstance.definitionId,
            revealReasonKey = RevealReasonKeyT013DeclareCardName,
        });

        if (!string.Equals(revealedCardInstance.definitionId, declaredDefinitionId, StringComparison.Ordinal))
        {
            actionChainState.pendingContinuationKey = null;
            return;
        }

        var opponentChoiceKeys = createChoiceKeysForEffect(
            gameState,
            submitInputChoiceActionRequest.actorPlayerId,
            OnPlayEffectKind.t013DeclareCardNameThenMaybeCharmOpponent);
        if (opponentChoiceKeys.Count == 0)
        {
            actionChainState.pendingContinuationKey = null;
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            submitInputChoiceActionRequest.requestId,
            submitInputChoiceActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlayTargetPlayerChoice,
            ContextKeyT013OnPlayTargetOpponentCharm,
            ContinuationKeyT013OnPlayTargetOpponentCharm,
            opponentChoiceKeys);
    }

    private void continueT008TargetDirectDamage1(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        PlayerId targetPlayerId)
    {
        var sourcePlayerState = gameState.players[sourcePlayerId];
        var targetPlayerState = gameState.players[targetPlayerId];
        if (sourcePlayerState.teamId == targetPlayerState.teamId)
        {
            throw new InvalidOperationException("T008 on-play continuation requires selected target player to be an opponent.");
        }

        var targetCharacterInstance = resolveAliveInPlayActiveCharacter(
            gameState,
            targetPlayerId,
            ContinuationKeyT008OnPlayTargetDirectDamage1);

        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(
            gameState,
            new DamageContext
            {
                damageContextId = new DamageContextId(requestId),
                sourcePlayerId = sourcePlayerId,
                sourceCardInstanceId = sourceCardInstanceId,
                targetPlayerId = targetPlayerId,
                targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                baseDamageValue = 1,
                damageType = DamageTypeKeyDirect,
            }));
    }

    private void applyT023LeylineAndOpenNextInputContextIfNeeded(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId actorPlayerId)
    {
        var actorTeamId = gameState.players[actorPlayerId].teamId;
        if (!gameState.teams.TryGetValue(actorTeamId, out var actorTeamState))
        {
            throw new InvalidOperationException("T023 on-play effect requires actor team state to exist.");
        }

        actorTeamState.leyline += 1;
        openT023HumanHealInputContextOrContinue(
            gameState,
            actionChainState,
            requestId,
            actorPlayerId);
    }

    private void openT023HumanHealInputContextOrContinue(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId actorPlayerId)
    {
        var choiceKeys = createT023HumanCharacterChoiceKeys(gameState);
        if (choiceKeys.Count <= 0)
        {
            openT023EnemyNonHumanDamageInputContextIfNeeded(
                gameState,
                actionChainState,
                requestId,
                actorPlayerId);
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            requestId,
            actorPlayerId,
            InputTypeKeyTreasureOnPlayTargetCharacterChoice,
            ContextKeyT023OnPlayTargetHumanHeal2,
            ContinuationKeyT023OnPlayTargetHumanHeal2,
            choiceKeys);
    }

    private void openT017SelectSummonZoneCardInputContextIfPossible(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        PlayTreasureCardActionRequest playTreasureCardActionRequest)
    {
        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("T017 on-play requires gameState.publicState to be initialized.");
        }

        if (!gameState.zones.TryGetValue(gameState.publicState.summonZoneId, out var summonZoneState))
        {
            throw new InvalidOperationException("T017 on-play requires summonZone to exist in gameState.zones.");
        }

        var choiceKeys = new List<string>();
        for (var index = 0; index < summonZoneState.cardInstanceIds.Count; index++)
        {
            choiceKeys.Add(ChoiceKeySummonZoneCardPrefix + summonZoneState.cardInstanceIds[index].Value);
        }

        if (choiceKeys.Count <= 0)
        {
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            playTreasureCardActionRequest.requestId,
            playTreasureCardActionRequest.actorPlayerId,
            InputTypeKeyTreasureOnPlaySummonZoneCardChoice,
            ContextKeyT017OnPlaySelectSummonZoneCard,
            ContinuationKeyT017OnPlaySelectSummonZoneCard,
            choiceKeys);
    }

    private void applyT020ResolveCurrentAnomalyThenBanishSelf(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId actorPlayerId,
        CardInstanceId sourceCardInstanceId)
    {
        forceResolveCurrentAnomalyFromExternalEffect(gameState, actionChainState, actorPlayerId, requestId);

        if (gameState.publicState is null)
        {
            throw new InvalidOperationException("T020 on-play requires gameState.publicState to be initialized.");
        }

        if (!gameState.cardInstances.TryGetValue(sourceCardInstanceId, out var sourceCardInstance))
        {
            throw new InvalidOperationException("T020 on-play requires source card instance to exist.");
        }

        if (sourceCardInstance.zoneKey == ZoneKey.gapZone)
        {
            return;
        }

        var banishEvent = zoneMovementService.moveCard(
            gameState,
            sourceCardInstance,
            gameState.publicState.gapZoneId,
            CardMoveReason.banish,
            actionChainState.actionChainId,
            requestId);
        actionChainState.producedEvents.Add(banishEvent);
    }

    private void openT023EnemyNonHumanDamageInputContextIfNeeded(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId actorPlayerId)
    {
        var choiceKeys = createT023EnemyNonHumanCharacterChoiceKeys(gameState, actorPlayerId);
        if (choiceKeys.Count <= 0)
        {
            actionChainState.pendingContinuationKey = null;
            return;
        }

        openInputContext(
            gameState,
            actionChainState,
            requestId,
            actorPlayerId,
            InputTypeKeyTreasureOnPlayTargetCharacterChoice,
            ContextKeyT023OnPlayTargetEnemyNonHumanDirectDamage1,
            ContinuationKeyT023OnPlayTargetEnemyNonHumanDirectDamage1,
            choiceKeys);
    }

    private void continueT023TargetHumanHeal2(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId actorPlayerId,
        CharacterInstanceId targetCharacterInstanceId)
    {
        var targetCharacterInstance = resolveAliveInPlayCharacterByInstanceId(
            gameState,
            targetCharacterInstanceId,
            ContinuationKeyT023OnPlayTargetHumanHeal2);
        if (!hasRaceTag(targetCharacterInstance, RaceTagHuman))
        {
            throw new InvalidOperationException("T023 heal continuation requires selected target character to have human race tag.");
        }

        var hpBefore = targetCharacterInstance.currentHp;
        var hpAfter = Math.Min(targetCharacterInstance.maxHp, hpBefore + 2);
        if (hpAfter != hpBefore)
        {
            targetCharacterInstance.currentHp = hpAfter;
            actionChainState.producedEvents.Add(new HpChangedEvent
            {
                eventId = requestId,
                eventTypeKey = "hpChanged",
                sourceActionChainId = actionChainState.actionChainId,
                targetPlayerId = targetCharacterInstance.ownerPlayerId,
                targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                hpBefore = hpBefore,
                hpAfter = hpAfter,
                delta = hpAfter - hpBefore,
            });
        }

        openT023EnemyNonHumanDamageInputContextIfNeeded(
            gameState,
            actionChainState,
            requestId,
            actorPlayerId);
    }

    private void continueT023TargetEnemyNonHumanDirectDamage1(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        CharacterInstanceId targetCharacterInstanceId)
    {
        var targetCharacterInstance = resolveAliveInPlayCharacterByInstanceId(
            gameState,
            targetCharacterInstanceId,
            ContinuationKeyT023OnPlayTargetEnemyNonHumanDirectDamage1);
        if (!hasRaceTag(targetCharacterInstance, RaceTagNonHuman))
        {
            throw new InvalidOperationException("T023 damage continuation requires selected target character to have nonHuman race tag.");
        }

        var sourcePlayerState = gameState.players[sourcePlayerId];
        var targetPlayerState = gameState.players[targetCharacterInstance.ownerPlayerId];
        if (targetPlayerState.teamId == sourcePlayerState.teamId)
        {
            throw new InvalidOperationException("T023 damage continuation requires selected target character owner to be an opponent.");
        }

        actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(
            gameState,
            new DamageContext
            {
                damageContextId = new DamageContextId(requestId),
                sourcePlayerId = sourcePlayerId,
                sourceCardInstanceId = sourceCardInstanceId,
                targetPlayerId = targetCharacterInstance.ownerPlayerId,
                targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                baseDamageValue = 1,
                damageType = DamageTypeKeyDirect,
            }));
    }

    private static void continueT009TargetOpponentSeal(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        PlayerId targetPlayerId)
    {
        var sourcePlayerState = gameState.players[sourcePlayerId];
        var targetPlayerState = gameState.players[targetPlayerId];
        if (sourcePlayerState.teamId == targetPlayerState.teamId)
        {
            throw new InvalidOperationException("T009 on-play continuation requires selected target player to be an opponent.");
        }

        var targetCharacterInstance = resolveAliveInPlayActiveCharacter(
            gameState,
            targetPlayerId,
            ContinuationKeyT009OnPlayTargetOpponentSeal);

        var appliedStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = StatusKeySeal,
                applierPlayerId = sourcePlayerId,
                applierCardInstanceId = sourceCardInstanceId,
                targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                stackCount = 1,
            });

        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = requestId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = appliedStatus.statusKey,
            targetPlayerId = targetCharacterInstance.ownerPlayerId,
            targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
            isApplied = true,
        });
    }

    private static void continueT013TargetOpponentCharm(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        PlayerId targetPlayerId)
    {
        var sourcePlayerState = gameState.players[sourcePlayerId];
        var targetPlayerState = gameState.players[targetPlayerId];
        if (sourcePlayerState.teamId == targetPlayerState.teamId)
        {
            throw new InvalidOperationException("T013 on-play continuation requires selected target player to be an opponent.");
        }

        var targetCharacterInstance = resolveAliveInPlayActiveCharacter(
            gameState,
            targetPlayerId,
            ContinuationKeyT013OnPlayTargetOpponentCharm);

        var appliedStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = StatusKeyCharm,
                applierPlayerId = sourcePlayerId,
                applierCardInstanceId = sourceCardInstanceId,
                targetPlayerId = targetPlayerId,
                stackCount = 1,
            });

        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = requestId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = appliedStatus.statusKey,
            targetPlayerId = targetPlayerId,
            targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
            isApplied = true,
        });
    }

    private static void continueT014TargetFriendlyBarrier(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        PlayerId targetPlayerId)
    {
        var sourcePlayerState = gameState.players[sourcePlayerId];
        var targetPlayerState = gameState.players[targetPlayerId];
        if (sourcePlayerState.teamId != targetPlayerState.teamId)
        {
            throw new InvalidOperationException("T014 on-play continuation requires selected target player to be a friendly player.");
        }

        var targetCharacterInstance = resolveAliveInPlayActiveCharacter(
            gameState,
            targetPlayerId,
            ContinuationKeyT014OnPlayTargetFriendlyBarrier);

        var appliedStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = StatusKeyBarrier,
                applierPlayerId = sourcePlayerId,
                applierCardInstanceId = sourceCardInstanceId,
                targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                stackCount = 1,
            });

        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = requestId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = appliedStatus.statusKey,
            targetPlayerId = targetCharacterInstance.ownerPlayerId,
            targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
            isApplied = true,
        });
    }

    private static void continueT015TargetOpponentSilence(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        PlayerId targetPlayerId)
    {
        var sourcePlayerState = gameState.players[sourcePlayerId];
        var targetPlayerState = gameState.players[targetPlayerId];
        if (sourcePlayerState.teamId == targetPlayerState.teamId)
        {
            throw new InvalidOperationException("T015 on-play continuation requires selected target player to be an opponent.");
        }

        var targetCharacterInstance = resolveAliveInPlayActiveCharacter(
            gameState,
            targetPlayerId,
            ContinuationKeyT015OnPlayTargetOpponentSilence);

        var appliedStatus = StatusRuntime.applyStatus(
            gameState,
            new StatusInstance
            {
                statusKey = StatusKeySilence,
                applierPlayerId = sourcePlayerId,
                applierCardInstanceId = sourceCardInstanceId,
                targetPlayerId = targetPlayerId,
                stackCount = 1,
            });

        actionChainState.producedEvents.Add(new StatusChangedEvent
        {
            eventId = requestId,
            eventTypeKey = "statusChanged",
            sourceActionChainId = actionChainState.actionChainId,
            statusKey = appliedStatus.statusKey,
            targetPlayerId = targetPlayerId,
            targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
            isApplied = true,
        });
    }

    private void continueT004TargetRemoveShackleOrSealStep(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        PlayerId targetPlayerId)
    {
        var targetCharacterInstance = resolveAliveInPlayActiveCharacter(
            gameState,
            targetPlayerId,
            ContinuationKeyT004OnPlayTargetRemoveShackleOrSealStep);

        var removedStatus = removeOneShackleOrSeal(
            gameState,
            targetCharacterInstance.characterInstanceId);
        if (removedStatus is not null)
        {
            actionChainState.producedEvents.Add(new StatusChangedEvent
            {
                eventId = requestId,
                eventTypeKey = "statusChanged",
                sourceActionChainId = actionChainState.actionChainId,
                statusKey = removedStatus.statusKey,
                targetPlayerId = targetCharacterInstance.ownerPlayerId,
                targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                isApplied = false,
            });
        }

        openT004TargetOpponentDamageInputContext(
            gameState,
            actionChainState,
            requestId,
            sourcePlayerId);
    }

    private void continueTargetDamage(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        PlayerId targetPlayerId,
        int baseDamageValue,
        string damageTypeKey,
        string continuationKey,
        bool requireOpponentTarget,
        bool isDirectDamage)
    {
        if (requireOpponentTarget &&
            gameState.players[sourcePlayerId].teamId == gameState.players[targetPlayerId].teamId)
        {
            throw new InvalidOperationException($"{continuationKey} requires selected target player to be an opponent.");
        }

        var targetCharacterInstance = resolveAliveInPlayActiveCharacter(
            gameState,
            targetPlayerId,
            continuationKey);

        if (isDirectDamage)
        {
            actionChainState.producedEvents.AddRange(damageProcessor.resolveDamage(
                gameState,
                new DamageContext
                {
                    damageContextId = new DamageContextId(requestId),
                    sourcePlayerId = sourcePlayerId,
                    sourceCardInstanceId = sourceCardInstanceId,
                    targetPlayerId = targetPlayerId,
                    targetCharacterInstanceId = targetCharacterInstance.characterInstanceId,
                    baseDamageValue = baseDamageValue,
                    damageType = damageTypeKey,
                }));
            if (gameState.currentInputContext is null &&
                string.Equals(actionChainState.pendingContinuationKey, continuationKey, StringComparison.Ordinal))
            {
                actionChainState.pendingContinuationKey = null;
            }

            return;
        }

        openStagedDamageResponseWindow(
            gameState,
            actionChainState,
            requestId,
            sourcePlayerId,
            sourceCardInstanceId,
            targetCharacterInstance.characterInstanceId,
            baseDamageValue,
            damageTypeKey);
    }

    private void openStagedDamageResponseWindow(
        GameState.GameState gameState,
        ActionChainState actionChainState,
        long requestId,
        PlayerId sourcePlayerId,
        CardInstanceId? sourceCardInstanceId,
        CharacterInstanceId targetCharacterInstanceId,
        int baseDamageValue,
        string damageTypeKey)
    {
        if (gameState.currentResponseWindow is not null)
        {
            throw new InvalidOperationException("Treasure on-play staged damage requires currentResponseWindow to be null.");
        }

        if (!gameState.characterInstances.TryGetValue(targetCharacterInstanceId, out var targetCharacterInstance))
        {
            throw new InvalidOperationException("Treasure on-play staged damage requires target character instance to exist.");
        }

        var pendingDamageDefenderPlayerId = targetCharacterInstance.ownerPlayerId;
        var responseWindowState = new ResponseWindowState
        {
            responseWindowId = new ResponseWindowId(nextResponseWindowIdSupplier()),
            originType = ResponseWindowOriginType.flow,
            windowTypeKey = ResponseWindowTypeDamageResponse,
            sourceActionChainId = actionChainState.actionChainId,
            pendingDamageTargetCharacterInstanceId = targetCharacterInstanceId,
            pendingDamageBaseDamageValue = baseDamageValue,
            pendingDamageSourcePlayerId = sourcePlayerId,
            pendingDamageSourceCardInstanceId = sourceCardInstanceId,
            pendingDamageTypeKey = damageTypeKey,
            pendingDamageResponseStageKey = ActionRequestProcessor.DamageResponseStageAwaitDefense,
            pendingDamageDefenseDeclarationKey = null,
            pendingDamageDefenderPlayerId = pendingDamageDefenderPlayerId,
            currentResponderPlayerId = pendingDamageDefenderPlayerId,
        };
        responseWindowState.responderPlayerIds.Add(pendingDamageDefenderPlayerId);

        gameState.currentResponseWindow = responseWindowState;
        actionChainState.pendingContinuationKey = ActionRequestProcessor.ContinuationKeyStagedResponseDamage;
        actionChainState.isCompleted = false;

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

    private static StatusInstance? removeOneShackleOrSeal(
        GameState.GameState gameState,
        CharacterInstanceId targetCharacterInstanceId)
    {
        var removedShackleStatuses = StatusRuntime.removeStatusesOnCharacter(
            gameState,
            targetCharacterInstanceId,
            StatusKeyShackle);
        if (removedShackleStatuses.Count > 0)
        {
            return removedShackleStatuses[0];
        }

        var removedSealStatuses = StatusRuntime.removeStatusesOnCharacter(
            gameState,
            targetCharacterInstanceId,
            StatusKeySeal);
        if (removedSealStatuses.Count > 0)
        {
            return removedSealStatuses[0];
        }

        return null;
    }
}
