using System.Collections.Generic;
using UnityEngine;

namespace CrescentWreath.Client.UI
{
public sealed class EffectSfxPlayer : MonoBehaviour
{
    private readonly Dictionary<string, float> lastPlayTimeByCategoryKey = new();
    private AudioSource? audioSource;
    private float categoryCooldownSeconds = 0.05f;

    public void Play(string categoryKey, EffectSfxCatalog.Entry? entry)
    {
        if (entry is null || entry.clips.Count <= 0)
        {
            Debug.LogWarning($"[EffectSfxPlayer] Missing SFX entry or clip for category={categoryKey}.");
            return;
        }

        var now = Time.unscaledTime;
        if (lastPlayTimeByCategoryKey.TryGetValue(categoryKey, out var lastPlayTime) &&
            now - lastPlayTime < categoryCooldownSeconds)
        {
            return;
        }

        lastPlayTimeByCategoryKey[categoryKey] = now;
        if (audioSource == null)
        {
            audioSource = getOrCreateAudioSource();
        }

        var clip = chooseClip(entry);
        if (clip is null)
        {
            return;
        }

        audioSource.pitch = Random.Range(entry.pitchMin, entry.pitchMax);
        audioSource.PlayOneShot(clip, entry.volume);
    }

    public void SetCategoryCooldown(float seconds)
    {
        categoryCooldownSeconds = Mathf.Max(0f, seconds);
    }

    private AudioSource getOrCreateAudioSource()
    {
        var source = GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        return source;
    }

    private static AudioClip? chooseClip(EffectSfxCatalog.Entry entry)
    {
        if (entry.clips.Count <= 0)
        {
            return null;
        }

        return entry.randomizeClip
            ? entry.clips[Random.Range(0, entry.clips.Count)]
            : entry.clips[0];
    }
}
}
