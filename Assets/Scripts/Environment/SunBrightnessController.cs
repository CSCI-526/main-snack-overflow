using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls the global "sun" brightness so the entire scene brightens smoothly as the player collects light orbs.
/// </summary>
[DisallowMultipleComponent]
public class SunBrightnessController : MonoBehaviour
{
    public static SunBrightnessController Instance { get; private set; }

    [Tooltip("Directional or point light that represents the sun.")]
    public Light sunLight;

    [Header("Intensity Settings")]
    public float minIntensity = 0.3f;
    public float maxIntensity = 6f;
    [Tooltip("How much the sun brightens for each point of light energy supplied by the player.")]
    public float perEnergyIncrease = 0.13f;
    [Tooltip("How quickly the stored brightness target drains when no new light is collected.")]
    public float passiveDecayPerSecond = 0.05f;

    [Header("Smoothing")]
    [Tooltip("Units per second the sun intensity rises toward a brighter target.")]
    public float brightenLerpPerSecond = 0.4f;
    [Tooltip("Units per second the sun intensity falls when the stored target decreases.")]
    public float darkenLerpPerSecond = 1.2f;

    [Header("Ambient Lighting")]
    public bool adjustAmbientLight = true;
    public Color ambientColorAtMin = new Color(0.03f, 0.03f, 0.05f);
    public Color ambientColorAtMax = new Color(0.55f, 0.55f, 0.6f);

    float targetIntensity;
    float appliedIntensity;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;

        AcquireSunIfNeeded();
        if (!sunLight)
        {
            Debug.LogWarning("SunBrightnessController could not find a directional light. Global brightness boosts will fall back to ambient only.");
        }

        ResetToMinimum();
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AcquireSunIfNeeded();
        ResetToMinimum();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void Update()
    {
        if (passiveDecayPerSecond > 0f && targetIntensity > minIntensity)
        {
            float delta = passiveDecayPerSecond * Time.deltaTime;
            targetIntensity = Mathf.Max(minIntensity, targetIntensity - delta);
        }

        if (!Mathf.Approximately(appliedIntensity, targetIntensity))
        {
            float speed = targetIntensity > appliedIntensity ? brightenLerpPerSecond : darkenLerpPerSecond;
            if (speed <= 0f)
                speed = brightenLerpPerSecond <= 0f ? darkenLerpPerSecond : speed;
            float next = speed > 0f
                ? Mathf.MoveTowards(appliedIntensity, targetIntensity, speed * Time.deltaTime)
                : targetIntensity;
            appliedIntensity = next;
            ApplyIntensity();
        }
    }

    public void AddSunEnergy(float amount)
    {
        if (amount <= 0f)
            return;

        AcquireSunIfNeeded();
        targetIntensity = Mathf.Clamp(targetIntensity + amount * perEnergyIncrease, minIntensity, maxIntensity);

        if (brightenLerpPerSecond <= 0f)
        {
            appliedIntensity = targetIntensity;
            ApplyIntensity();
        }
    }

    public void ResetToMinimum()
    {
        targetIntensity = minIntensity;
        appliedIntensity = minIntensity;
        ApplyIntensity();
    }

    void AcquireSunIfNeeded()
    {
        if (sunLight && sunLight.gameObject.scene.isLoaded)
            return;

        if (RenderSettings.sun)
        {
            sunLight = RenderSettings.sun;
            return;
        }

        sunLight = FindDirectionalSun();
    }

    public static Light FindDirectionalSun()
    {
        var lights = Object.FindObjectsOfType<Light>();
        Light fallback = null;
        for (int i = 0; i < lights.Length; i++)
        {
            var l = lights[i];
            if (!l || !l.isActiveAndEnabled)
                continue;

            if (l.type == LightType.Directional)
                return l;

            if (fallback == null)
                fallback = l;
        }
        return fallback;
    }

    void ApplyIntensity()
    {
        float normalized = Mathf.Approximately(maxIntensity, minIntensity)
            ? 1f
            : Mathf.InverseLerp(minIntensity, maxIntensity, appliedIntensity);

        if (sunLight)
            sunLight.intensity = appliedIntensity;

        if (adjustAmbientLight)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(ambientColorAtMin, ambientColorAtMax, normalized);
        }
    }
}
