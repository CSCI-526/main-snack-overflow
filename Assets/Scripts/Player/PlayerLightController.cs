using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerLightController : MonoBehaviour
{
    public static PlayerLightController Instance { get; private set; }

    const string LevelThreeSceneName = "LvL3";

    [Header("Light Settings")]
    public Light targetLight;
    public float minRange = 6f;
    public float maxRange = 16f;
    public float perCollectibleIncrease = 1.25f;
    public float passiveDecayPerSecond = 0.5f;

    [Header("Intensity Settings")]
    public bool linkIntensityToRange = true;
    public float minIntensity = 80f;
    public float maxIntensity = 220f;
    [Tooltip("Used only when linkIntensityToRange is disabled.")]
    public float perCollectibleIntensityIncrease = 15f;
    [Tooltip("Used only when linkIntensityToRange is disabled.")]
    public float passiveIntensityDecayPerSecond = 5f;

    public bool onlyActiveInLevelThree = true;

    bool activeInScene;
    bool boundsInitialized;
    float minRangeRuntime;
    float maxRangeRuntime;
    float minIntensityRuntime;
    float maxIntensityRuntime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (!targetLight)
            targetLight = GetComponentInChildren<Light>();

        if (targetLight)
        {
            InitializeRuntimeBounds();
            targetLight.range = Mathf.Clamp(targetLight.range, RangeMin, RangeMax);
            if (linkIntensityToRange)
                SyncIntensityToRange();
            else
                SetIntensity(targetLight.intensity);
        }

        RefreshActivation(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshActivation(scene);
    }

    void RefreshActivation(Scene scene)
    {
        activeInScene = !onlyActiveInLevelThree || (scene.IsValid() && scene.name == LevelThreeSceneName);
        if (!targetLight)
            return;

        if (!activeInScene)
        {
            targetLight.enabled = false;
        }
        else
        {
            targetLight.enabled = true;
            InitializeRuntimeBounds();
            targetLight.range = Mathf.Clamp(targetLight.range, RangeMin, RangeMax);
            if (linkIntensityToRange)
                SyncIntensityToRange();
            else
                SetIntensity(targetLight.intensity);
        }
    }

    void Update()
    {
        if (!activeInScene || !targetLight)
            return;

        InitializeRuntimeBounds();
        if (passiveDecayPerSecond > 0f && targetLight.range > RangeMin)
        {
            float delta = passiveDecayPerSecond * Time.deltaTime;
            SetRange(targetLight.range - delta);
        }

        if (!linkIntensityToRange && passiveIntensityDecayPerSecond > 0f && targetLight.intensity > IntensityMin)
        {
            float delta = passiveIntensityDecayPerSecond * Time.deltaTime;
            SetIntensity(targetLight.intensity - delta);
        }
    }

    public void AddLightEnergy(float amount)
    {
        if (!activeInScene || !targetLight || amount <= 0f)
            return;

        InitializeRuntimeBounds();
        SetRange(targetLight.range + amount * perCollectibleIncrease);
        if (!linkIntensityToRange)
            SetIntensity(targetLight.intensity + amount * perCollectibleIntensityIncrease);
    }

    void SetRange(float value)
    {
        InitializeRuntimeBounds();
        targetLight.range = Mathf.Clamp(value, RangeMin, RangeMax);
        if (linkIntensityToRange)
            SyncIntensityToRange();
    }

    void SetIntensity(float value)
    {
        InitializeRuntimeBounds();
        targetLight.intensity = Mathf.Clamp(value, IntensityMin, IntensityMax);
    }

    void SyncIntensityToRange()
    {
        if (!targetLight)
            return;
        InitializeRuntimeBounds();

        float rangeSpan = RangeMax - RangeMin;
        float lerp = rangeSpan > 0.001f ? Mathf.InverseLerp(RangeMin, RangeMax, targetLight.range) : 1f;
        float next = Mathf.Lerp(IntensityMin, IntensityMax, lerp);
        SetIntensity(next);
    }

    void InitializeRuntimeBounds()
    {
        if (boundsInitialized || !targetLight)
            return;

        float currentRange = targetLight.range;
        if (minRange > 0f)
            minRangeRuntime = Mathf.Min(minRange, currentRange);
        else
            minRangeRuntime = currentRange;

        float maxRangeCandidate = maxRange > 0f ? maxRange : currentRange;
        maxRangeRuntime = Mathf.Max(minRangeRuntime, maxRangeCandidate);

        float currentIntensity = targetLight.intensity;
        if (minIntensity > 0f)
            minIntensityRuntime = Mathf.Min(minIntensity, currentIntensity);
        else
            minIntensityRuntime = currentIntensity;

        float maxIntensityCandidate = maxIntensity > 0f ? maxIntensity : currentIntensity;
        maxIntensityRuntime = Mathf.Max(minIntensityRuntime, maxIntensityCandidate);

        boundsInitialized = true;
    }

    float RangeMin => minRangeRuntime;
    float RangeMax => maxRangeRuntime;
    float IntensityMin => minIntensityRuntime;
    float IntensityMax => maxIntensityRuntime;
}
