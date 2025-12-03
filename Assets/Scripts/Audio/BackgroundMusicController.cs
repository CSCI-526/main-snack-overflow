using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BackgroundMusicController : MonoBehaviour
{
    public static BackgroundMusicController Instance { get; private set; }

    [Tooltip("Resource path to the AudioClip (drop it under Assets/Resources).")]
    public string clipResourcePath = "Audio/BackgroundMusic";

    [Tooltip("Scene names where this music should play.")]
    public string[] scenesUsingClip = { "Landing", "Home", "LvL1", "LvL2" };

    [Range(0f, 1f)] public float volume = 1f;
    public bool loop = true;

    [System.Serializable]
    public class SceneMusicOverride
    {
        public string clipResourcePath = "Audio/BackgroundMusic";
        [Range(0f, 1f)] public float volume = 1f;
        public bool loop = true;
        [Tooltip("Optional offset in seconds to skip the beginning of the clip.")]
        public float startTimeSeconds = 0f;
        public string[] scenes;
    }

    [Tooltip("Optional overrides for specific scenes that need a different track.")]
    public SceneMusicOverride[] sceneOverrides;

    AudioSource audioSource;
    HashSet<string> sceneSet = new HashSet<string>();
    readonly Dictionary<string, SceneMusicOverride> overrideLookup = new Dictionary<string, SceneMusicOverride>();
    readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        BuildSceneSet();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void OnValidate() => BuildSceneSet();

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var config = GetConfigForScene(scene.name);
        if (config == null)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
            return;
        }

        var clip = LoadClip(config.clipResourcePath);
        if (!clip)
        {
            Debug.LogWarning($"[BackgroundMusicController] AudioClip not found at Resources/{config.clipResourcePath}.");
            return;
        }

        bool clipChanged = audioSource.clip != clip;
        if (clipChanged)
            audioSource.clip = clip;

        float targetVolume = Mathf.Max(config.volume, volume);
        audioSource.volume = Mathf.Clamp01(targetVolume);
        audioSource.loop = config.loop;

        if (clipChanged || !audioSource.isPlaying)
        {
            float startTime = Mathf.Max(0f, Mathf.Min(config.startTimeSeconds, clip.length > 0f ? clip.length - 0.01f : 0f));
            if (clip.length > 0f)
                audioSource.time = startTime;
            audioSource.Play();
        }
    }

    void BuildSceneSet()
    {
        sceneSet.Clear();
        overrideLookup.Clear();

        if (scenesUsingClip != null)
        {
            foreach (var scene in scenesUsingClip)
            {
                if (!string.IsNullOrWhiteSpace(scene))
                    sceneSet.Add(scene.Trim());
            }
        }

        if (sceneOverrides == null)
            return;

        foreach (var entry in sceneOverrides)
        {
            if (entry?.scenes == null)
                continue;
            foreach (var scene in entry.scenes)
            {
                if (string.IsNullOrWhiteSpace(scene))
                    continue;
                overrideLookup[scene.Trim()] = entry;
            }
        }
    }

    SceneMusicOverride GetConfigForScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return null;

        if (overrideLookup.TryGetValue(sceneName, out var overrideConfig))
            return overrideConfig;

        if (!sceneSet.Contains(sceneName))
            return null;

        return new SceneMusicOverride
        {
            clipResourcePath = clipResourcePath,
            volume = volume,
            loop = loop,
            startTimeSeconds = 0f
        };
    }

    AudioClip LoadClip(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        if (clipCache.TryGetValue(path, out var cached) && cached)
            return cached;

        var clip = Resources.Load<AudioClip>(path);
        if (clip)
            clipCache[path] = clip;
        return clip;
    }
}
