using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerLightController : MonoBehaviour
{
    public static PlayerLightController Instance { get; private set; }

    const string LevelThreeSceneName = "LvL3";

    [Header("Light Settings")]
    public Light targetLight;
    public float minRange = 6f;
    public float maxRange = 32f;
    [Tooltip("Each point of light energy increases the range by this much.")]
    public float perCollectibleIncrease = 4f;
    [Tooltip("Extra multiplier applied to the reward value passed in by the collectible. Bump this for dramatic growth.")]
    public float collectibleBurstMultiplier = 3f;
    public float passiveDecayPerSecond = 0.5f;
    public bool onlyActiveInLevelThree = false;
    [Tooltip("Force the light to begin at its minimum range so orb pickups always feel impactful.")]
    public bool startAtMinRange = true;
    [Tooltip("Allow the torch radius to grow when the player collects light orbs. Disable to keep a constant lantern and only adjust the sun.")]
    public bool growTorchWithCollectibles = false;

    [Header("Range Response")]
    [Tooltip("Units per second the visible light will move toward its target range. Lower = smoother growth.")]
    public float rangeLerpSpeed = 8f;

    [Header("Sun Sync")]
    [Tooltip("When enabled, collecting light energy will also brighten the directional sun light.")]
    public bool brightenSunOnCollect = true;
    [Tooltip("Multiplier applied to the collectible's reward before it is forwarded to the sun controller.")]
    public float sunBurstMultiplier = 1.5f;

    [Header("Light Intensity Link")]
    [Tooltip("Scale the point light intensity alongside its range.")]
    public bool scaleIntensityWithRange = true;
    public float intensityAtMinRange = 4f;
    public float intensityAtMaxRange = 12f;

    [Header("Vision Mask Link")] 
    [Tooltip("When enabled, the vision mask radius scales with the player's light range so collecting orbs reveals more of the map.")]
    public bool syncVisionMaskRadius = true;
    [Tooltip("Vision mask radius to use while the light is at its minimum range.")]
    public float visionRadiusAtMinRange = 0.26f;
    [Tooltip("Vision mask radius to use while the light is at its maximum range.")]
    public float visionRadiusAtMaxRange = 0.55f;
    [Tooltip("Optional curve applied after normalizing the light range, letting you fine-tune how quickly the vision radius grows.")]
    public AnimationCurve rangeToRadiusCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    float defaultIntensity;
    bool activeInScene;
    float targetRange;

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
            defaultIntensity = targetLight.intensity;
            float clamped = Mathf.Clamp(targetLight.range, minRange, maxRange);
            targetRange = startAtMinRange ? minRange : clamped;
            ApplyRangeInstant(targetRange);
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
            targetRange = Mathf.Clamp(targetRange, minRange, maxRange);
            if (startAtMinRange && growTorchWithCollectibles)
                targetRange = minRange;

            if (growTorchWithCollectibles)
                ApplyRangeInstant(targetRange);
            else
                MaintainStaticTorch();
        }
    }

    void Update()
    {
        if (!activeInScene || !targetLight)
            return;

        if (!growTorchWithCollectibles)
        {
            MaintainStaticTorch();
            return;
        }

        if (passiveDecayPerSecond > 0f && targetRange > minRange)
        {
            float delta = passiveDecayPerSecond * Time.deltaTime;
            targetRange = Mathf.Max(minRange, targetRange - delta);
        }

        float currentRange = targetLight.range;
        if (!Mathf.Approximately(currentRange, targetRange))
        {
            float next = rangeLerpSpeed > 0f
                ? Mathf.MoveTowards(currentRange, targetRange, rangeLerpSpeed * Time.deltaTime)
                : targetRange;
            ApplyRangeInstant(next);
        }
    }

    public void HandleLightOrbPickup(float lightReward)
    {
        AddLightEnergy(lightReward);

        if (brightenSunOnCollect && SunBrightnessController.Instance != null)
            SunBrightnessController.Instance.AddSunEnergy(lightReward * sunBurstMultiplier);
    }

    void AddLightEnergy(float amount)
    {
        if (!activeInScene || !targetLight || amount <= 0f)
            return;
        if (growTorchWithCollectibles)
        {
            float baseline = targetLight ? Mathf.Max(targetLight.range, targetRange) : targetRange;
            SetRange(baseline + amount * perCollectibleIncrease * collectibleBurstMultiplier);
        }
    }

    void SetRange(float value)
    {
        if (!growTorchWithCollectibles)
            return;
        targetRange = Mathf.Clamp(value, minRange, maxRange);
        if (!Application.isPlaying || rangeLerpSpeed <= 0f)
            ApplyRangeInstant(targetRange);
    }

    void ApplyRangeInstant(float value)
    {
        if (!targetLight)
            return;
        targetLight.range = Mathf.Clamp(value, minRange, maxRange);
        UpdateVisionMask();
        UpdateLightIntensity();
    }

    void MaintainStaticTorch()
    {
        if (!targetLight)
            return;

        targetRange = Mathf.Clamp(targetRange, minRange, maxRange);
        if (!Mathf.Approximately(targetLight.range, targetRange))
            targetLight.range = targetRange;

        UpdateVisionMask();
        UpdateLightIntensity();
    }

    void UpdateVisionMask()
    {
        if (!syncVisionMaskRadius || !targetLight)
            return;

        var mask = VisionMaskController.Instance;
        if (mask == null)
            return;

        float normalized = Mathf.InverseLerp(minRange, maxRange, targetLight.range);
        float curveFactor = rangeToRadiusCurve != null ? rangeToRadiusCurve.Evaluate(Mathf.Clamp01(normalized)) : normalized;
        float newRadius = Mathf.Lerp(visionRadiusAtMinRange, visionRadiusAtMaxRange, Mathf.Clamp01(curveFactor));
        mask.UpdateRadius(newRadius);
    }

    void UpdateLightIntensity()
    {
        if (!scaleIntensityWithRange || !targetLight)
            return;

        float normalized = Mathf.InverseLerp(minRange, maxRange, targetLight.range);
        float newIntensity = Mathf.Lerp(intensityAtMinRange, intensityAtMaxRange, Mathf.Clamp01(normalized));
        targetLight.intensity = newIntensity;
    }
}
