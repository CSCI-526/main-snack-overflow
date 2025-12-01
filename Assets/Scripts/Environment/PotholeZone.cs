using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class PotholeZone : MonoBehaviour
{
    const string DefaultStuckSoundPath = "Audio/potholestuck_sound";
    static AudioClip cachedDefaultStuckSound;
    readonly HashSet<PlayerMover> playersInside = new();
    [Min(0f)]
    public float stunDuration = 3f;
    [Header("Audio")]
    [SerializeField] AudioClip stuckSound;
    [Range(0f, 2f)]
    [SerializeField] float stuckSoundVolume = 1.35f;

    void Awake() => EnsureStuckSoundAssigned();

    void Reset()
    {
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
        EnsureStuckSoundAssigned();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerMover mover) && playersInside.Add(mover))
        {
            bool stunned = mover.ApplyStun(stunDuration);
            if (stunned && PlayerMover.IsActivePlayer(mover))
            {
                ShowPotholeMessage();
                PlayStuckSound();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out PlayerMover mover))
            playersInside.Remove(mover);
    }


    void ShowPotholeMessage()
    {
        HazardHintManager.TryShowPotholeHint(stunDuration);
    }

    void PlayStuckSound()
    {
        if (!stuckSound)
            return;

        var go = new GameObject("PotholeStuckSoundTemp");
        go.transform.position = transform.position;
        var source = go.AddComponent<AudioSource>();
        source.clip = stuckSound;
        source.volume = Mathf.Clamp(stuckSoundVolume, 0f, 2f);
        source.spatialBlend = 0f; // play as 2D so it stays loud regardless of camera distance
        source.playOnAwake = false;
        source.loop = false;
        source.Play();
        Destroy(go, stuckSound.length + 0.1f);
    }

    void EnsureStuckSoundAssigned()
    {
        if (stuckSound)
            return;

        if (!cachedDefaultStuckSound)
            cachedDefaultStuckSound = Resources.Load<AudioClip>(DefaultStuckSoundPath);

        stuckSound = cachedDefaultStuckSound;
    }
}
