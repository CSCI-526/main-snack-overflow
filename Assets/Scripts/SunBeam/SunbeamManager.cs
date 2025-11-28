using UnityEngine;
using UnityEngine.SceneManagement;

public class SunbeamManager : MonoBehaviour
{
    public static SunbeamManager Instance { get; private set; }
    public GameObject sunbeamPrefab; // assign the Sunbeam prefab in Inspector
    const string LevelThreeSceneName = "LvL3";
    public const string DefaultImpostorMessage = "Imposter Down!\nEyes Sharp, Feet Faster!";
    public const string DefaultCivilianMessage = "Civilian Down!\nSpeed Drop, Vision Blur!";
    public const string LevelThreeImpostorMessage = "Impostor down!\nFeet faster";
    public const string LevelThreeCivilianMessage = "Civilian down!\nSpeed drop";

    [Header("Audio")]
    [Tooltip("Resource path (under Resources/) for the correct-kill sound.")]
    public string correctKillClipPath = "Audio/CorrectKillSound";
    [Tooltip("Seconds to skip before the trimmed snippet begins.")]
    public float correctKillTrimOffset = 1f;
    [Tooltip("Seconds of audio to keep after the trim offset.")]
    public float correctKillClipDuration = 2f;
    [Range(0f, 1f)] public float correctKillVolume = 0.9f;

    [Tooltip("Resource path for the wrong-kill sound.")]
    public string wrongKillClipPath = "Audio/WrongKillSound";
    [Tooltip("Seconds to skip before using the wrong-kill clip.")]
    public float wrongKillTrimOffset = 1f;
    [Tooltip("Pitch multiplier applied to the wrong-kill clip (2 = double speed).")]
    public float wrongKillPitch = 2f;
    [Range(0f, 1f)] public float wrongKillVolume = 1f;

    AudioSource sfxSource;
    AudioClip trimmedCorrectKillClip;
    AudioClip spedWrongKillClip;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        sfxSource = GetComponent<AudioSource>();
        if (!sfxSource)
            sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
    }

    public void Smite(NPCDeath npcDeath)
    {
        if (npcDeath == null) return;

        var identity = npcDeath.GetComponent<NPCIdentity>();
        bool isImpostor = identity != null && identity.isImpostor;

        var go = Instantiate(sunbeamPrefab);
        var beam = go.GetComponent<Sunbeam>();

        void OnBeamHit()
        {
            bool lvlThree = SceneManager.GetActiveScene().name == LevelThreeSceneName;
            if (isImpostor)
            {
                string message = lvlThree ? LevelThreeImpostorMessage : DefaultImpostorMessage;
                KillTextController.Instance?.Show(message);
                PlayCorrectKillSound();
            }
            else
            {
                string message = lvlThree ? LevelThreeCivilianMessage : DefaultCivilianMessage;
                KillTextController.Instance?.Show(message);
                PlayWrongKillSound();
            }

            npcDeath.DieCute();
        }

        beam.Init(npcDeath.transform, OnBeamHit, isImpostor);
    }

    void PlayCorrectKillSound()
    {
        if (!sfxSource)
            return;

        var clip = GetTrimmedCorrectKillClip();
        if (!clip)
            return;

        sfxSource.PlayOneShot(clip, correctKillVolume);
    }

    void PlayWrongKillSound()
    {
        if (!sfxSource)
            return;

        var clip = GetSpedWrongKillClip();
        if (!clip)
            return;

        sfxSource.PlayOneShot(clip, wrongKillVolume);
    }

    AudioClip GetTrimmedCorrectKillClip()
    {
        if (trimmedCorrectKillClip)
            return trimmedCorrectKillClip;
        if (string.IsNullOrEmpty(correctKillClipPath))
            return null;

        var sourceClip = Resources.Load<AudioClip>(correctKillClipPath);
        if (!sourceClip)
        {
            Debug.LogWarning($"[SunbeamManager] Correct kill sound not found at Resources/{correctKillClipPath}.");
            return null;
        }
        if (!EnsureClipData(sourceClip))
            return null;

        trimmedCorrectKillClip = TrimClipRange(sourceClip, correctKillTrimOffset, correctKillClipDuration);
        return trimmedCorrectKillClip ?? sourceClip;
    }

    AudioClip GetSpedWrongKillClip()
    {
        if (spedWrongKillClip)
            return spedWrongKillClip;
        if (string.IsNullOrEmpty(wrongKillClipPath))
            return null;

        var sourceClip = Resources.Load<AudioClip>(wrongKillClipPath);
        if (!sourceClip)
        {
            Debug.LogWarning($"[SunbeamManager] Wrong kill sound not found at Resources/{wrongKillClipPath}.");
            return null;
        }
        if (!EnsureClipData(sourceClip))
            return null;

        var trimmed = TrimClipRange(sourceClip, wrongKillTrimOffset, sourceClip.length - wrongKillTrimOffset);
        var baseClip = trimmed ?? sourceClip;
        spedWrongKillClip = CreatePitchShiftedClip(baseClip, wrongKillPitch);
        return spedWrongKillClip ?? baseClip;
    }

    AudioClip TrimClipRange(AudioClip source, float startSeconds, float durationSeconds)
    {
        if (!source)
            return null;
        if (!EnsureClipData(source))
            return null;

        float start = Mathf.Clamp(startSeconds, 0f, source.length);
        float available = Mathf.Max(0f, source.length - start);
        float trimmedDuration = durationSeconds > 0f ? Mathf.Min(durationSeconds, available) : available;
        if (trimmedDuration <= 0.01f)
            return null;

        int startSampleFrame = Mathf.Clamp(Mathf.FloorToInt(start * source.frequency), 0, source.samples - 1);
        int sampleFrames = Mathf.Clamp(Mathf.FloorToInt(trimmedDuration * source.frequency), 1, source.samples - startSampleFrame);
        int channels = source.channels;

        float[] data = new float[sampleFrames * channels];
        source.GetData(data, startSampleFrame);

        var trimmed = AudioClip.Create(source.name + "_trimmed", sampleFrames, channels, source.frequency, false);
        trimmed.SetData(data, 0);
        return trimmed;
    }

    AudioClip CreatePitchShiftedClip(AudioClip source, float pitchMultiplier)
    {
        if (!source || pitchMultiplier <= 0f)
            return null;
        if (!EnsureClipData(source))
            return null;

        int totalSamples = source.samples;
        if (totalSamples <= 0)
            return null;

        int channels = source.channels;
        float[] data = new float[totalSamples * channels];
        source.GetData(data, 0);

        int targetSamples = Mathf.Max(1, Mathf.FloorToInt(totalSamples / pitchMultiplier));
        float[] resampled = new float[targetSamples * channels];

        for (int i = 0; i < targetSamples; i++)
        {
            float srcIndex = i * pitchMultiplier;
            int baseIndex = Mathf.Clamp(Mathf.FloorToInt(srcIndex), 0, totalSamples - 1);
            float frac = srcIndex - baseIndex;
            int nextIndex = Mathf.Min(baseIndex + 1, totalSamples - 1);

            for (int ch = 0; ch < channels; ch++)
            {
                int srcBase = baseIndex * channels + ch;
                int srcNext = nextIndex * channels + ch;
                float sample = Mathf.Lerp(data[srcBase], data[srcNext], frac);
                resampled[i * channels + ch] = sample;
            }
        }

        var shifted = AudioClip.Create(source.name + "_sped", targetSamples, channels, source.frequency, false);
        shifted.SetData(resampled, 0);
        return shifted;
    }

    bool EnsureClipData(AudioClip clip)
    {
        if (!clip)
            return false;

        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();

        return clip.loadState == AudioDataLoadState.Loaded;
    }
}
