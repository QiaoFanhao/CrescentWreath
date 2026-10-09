using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CrescentWreath.Client.Net;
using TMPro;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class SkillPresentationTimeline : MonoBehaviour
{
    private const string SettingsResourcesPath = "EffectPresentation/SkillPresentationSettings";
    private const string SfxCatalogResourcesPath = "EffectPresentation/DefaultEffectSfxCatalog";
    private const int PresentedEventHistoryLimit = 2048;

    [SerializeField] private SkillPresentationSettings? settings;

    private readonly Queue<SkillPresentationStep> pendingSteps = new();
    private readonly HashSet<long> presentedSkillEventIds = new();
    private readonly Queue<long> presentedSkillEventOrder = new();
    private SkillPresentationOverlay? overlay;
    private EffectSfxPlayer? sfxPlayer;
    private EffectSfxCatalog? sfxCatalog;
    private bool isPresenting;

    public bool IsPresenting => isPresenting;
    public int PendingStepCount => pendingSteps.Count;

    public void Configure(Canvas overlayCanvas, TMP_FontAsset? primaryFont)
    {
        settings ??= Resources.Load<SkillPresentationSettings>(SettingsResourcesPath) ??
                     SkillPresentationSettings.CreateRuntimeDefault();
        sfxCatalog ??= Resources.Load<EffectSfxCatalog>(SfxCatalogResourcesPath);
        sfxPlayer ??= GetComponent<EffectSfxPlayer>() ?? gameObject.AddComponent<EffectSfxPlayer>();
        if (overlay is null)
        {
            var overlayObject = new GameObject("SkillPresentationOverlay", typeof(RectTransform));
            overlay = overlayObject.AddComponent<SkillPresentationOverlay>();
            overlay.Configure(overlayCanvas, primaryFont);
        }
    }

    public void PresentProjection(ProjectionViewModel projection)
    {
        if (projection is null || !projection.hasStateProjection || overlay is null)
        {
            return;
        }

        foreach (var skillEvent in projection.skillPresentationEvents)
        {
            if (skillEvent.eventNumericId <= 0 || !presentedSkillEventIds.Add(skillEvent.eventNumericId))
            {
                continue;
            }

            rememberPresentedEvent(skillEvent.eventNumericId);
            pendingSteps.Enqueue(buildStep(projection, skillEvent));
        }

        if (!isPresenting && pendingSteps.Count > 0)
        {
            StartCoroutine(playQueue());
        }
    }

    private IEnumerator playQueue()
    {
        isPresenting = true;
        while (pendingSteps.Count > 0)
        {
            var step = pendingSteps.Dequeue();
            var activeSettings = settings ?? SkillPresentationSettings.CreateRuntimeDefault();
            var duration = activeSettings.ResolveDuration(step.skillEvent.triggerTypeKey);
            BattlefieldInteractionPresentationGate.DelayFor(
                duration,
                "skill:" + step.skillEvent.skillKey,
                presentationStep: "skillPresentation");

            overlay!.Bind(
                step.skillEvent,
                step.playerName,
                step.characterName,
                step.skillName,
                step.skillTypeRaw,
                activeSettings.ResolveVfxKey(step.skillEvent.triggerTypeKey),
                activeSettings.dimAlpha);
            playSfx(activeSettings.ResolveSfxKey(step.skillEvent.triggerTypeKey));

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var enterProgress = activeSettings.enterDuration <= 0f
                    ? 1f
                    : Mathf.Clamp01(elapsed / activeSettings.enterDuration);
                var exitStart = Mathf.Max(activeSettings.enterDuration, duration - activeSettings.exitDuration);
                var exitProgress = activeSettings.exitDuration <= 0f
                    ? 0f
                    : Mathf.Clamp01((elapsed - exitStart) / activeSettings.exitDuration);
                var alpha = Mathf.Min(enterProgress, 1f - exitProgress);
                overlay.SetVisual(alpha, enterProgress, exitProgress, activeSettings.cardScale);
                yield return null;
            }

            overlay.Hide();
        }

        isPresenting = false;
    }

    private void playSfx(string sfxKey)
    {
        if (sfxCatalog is not null && sfxCatalog.TryGetEntry(sfxKey, out var entry))
        {
            sfxPlayer?.Play(sfxKey, entry);
        }
    }

    private static SkillPresentationStep buildStep(
        ProjectionViewModel projection,
        ProjectionSkillPresentationEventViewModel skillEvent)
    {
        var characterDefinition = projection.characterDefinitions.FirstOrDefault(
            definition => string.Equals(
                definition.definitionId,
                skillEvent.characterDefinitionId,
                System.StringComparison.Ordinal));
        var skillDefinition = characterDefinition?.skills.FirstOrDefault(
            skill => string.Equals(skill.skillKey, skillEvent.skillKey, System.StringComparison.Ordinal));
        return new SkillPresentationStep(
            skillEvent,
            $"玩家 P{skillEvent.actorPlayerNumericId}",
            characterDefinition?.characterName ?? skillEvent.characterDefinitionId,
            skillDefinition?.skillName ?? skillEvent.skillKey,
            skillDefinition?.skillTypeRaw ?? string.Empty);
    }

    private void rememberPresentedEvent(long eventNumericId)
    {
        presentedSkillEventOrder.Enqueue(eventNumericId);
        while (presentedSkillEventOrder.Count > PresentedEventHistoryLimit)
        {
            presentedSkillEventIds.Remove(presentedSkillEventOrder.Dequeue());
        }
    }

    private sealed class SkillPresentationStep
    {
        public SkillPresentationStep(
            ProjectionSkillPresentationEventViewModel skillEvent,
            string playerName,
            string characterName,
            string skillName,
            string skillTypeRaw)
        {
            this.skillEvent = skillEvent;
            this.playerName = playerName;
            this.characterName = characterName;
            this.skillName = skillName;
            this.skillTypeRaw = skillTypeRaw;
        }

        public readonly ProjectionSkillPresentationEventViewModel skillEvent;
        public readonly string playerName;
        public readonly string characterName;
        public readonly string skillName;
        public readonly string skillTypeRaw;
    }
}
}
