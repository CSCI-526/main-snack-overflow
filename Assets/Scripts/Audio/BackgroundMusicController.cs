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

    [Range(0f, 1f)] public float volume = 0.5f;
    public bool loop = true;

    AudioSource audioSource;
    AudioClip loadedClip;
    HashSet<string> sceneSet = new HashSet<string>();

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
        bool shouldPlay = sceneSet.Contains(scene.name);
        if (!shouldPlay)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
            return;
        }

        if (!loadedClip && !string.IsNullOrEmpty(clipResourcePath))
            loadedClip = Resources.Load<AudioClip>(clipResourcePath);

        if (!loadedClip)
        {
            Debug.LogWarning($"[BackgroundMusicController] AudioClip not found at Resources/{clipResourcePath}.");
            return;
        }

        if (audioSource.clip != loadedClip)
            audioSource.clip = loadedClip;

        audioSource.volume = volume;
        audioSource.loop = loop;

        if (!audioSource.isPlaying)
            audioSource.Play();
    }

    void BuildSceneSet()
    {
        sceneSet.Clear();
        if (scenesUsingClip == null) return;
        foreach (var scene in scenesUsingClip)
        {
            if (!string.IsNullOrWhiteSpace(scene))
                sceneSet.Add(scene.Trim());
        }
    }
}
