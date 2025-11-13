using System.Collections;
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
    public float maxIntensity = 500f;
    [Tooltip("Used only when linkIntensityToRange is disabled.")]
    public float perCollectibleIntensityIncrease = 100f;
    [Tooltip("Used only when linkIntensityToRange is disabled.")]
    public float passiveIntensityDecayPerSecond = 5f;

    [Header("Pickup Feedback - Light")]
    public float pickupFlashMultiplier = 1.08f;
    public float pickupFlashDuration = 0.8f;

    [Header("Pickup Feedback - Player Glow")]
    public Renderer[] playerGlowRenderers;
    public Color playerGlowColor = new Color(1f, 0.87f, 0.54f);
    public float playerGlowIntensity = 1.1f;
    public float playerGlowDuration = 1.2f;

    public bool onlyActiveInLevelThree = true;

    bool activeInScene;
    bool boundsInitialized;
    float minRangeRuntime;
    float maxRangeRuntime;
    float minIntensityRuntime;
    float maxIntensityRuntime;
    float baseIntensityValue;
    Coroutine pickupFlashRoutine;
    float pickupFlashBlend;
    Coroutine playerGlowRoutine;
    float playerGlowBlend;
    MaterialPropertyBlock[] glowBlocks;
    Color[] glowBaseEmission;

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
        CachePlayerGlowData();

        RefreshActivation(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDestroy()
    {
        if (pickupFlashRoutine != null)
            StopCoroutine(pickupFlashRoutine);
        if (playerGlowRoutine != null)
            StopCoroutine(playerGlowRoutine);
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
            SetIntensity(baseIntensityValue + amount * perCollectibleIntensityIncrease);
        TriggerPickupFlash();
        TriggerPlayerGlow();
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
        baseIntensityValue = Mathf.Clamp(value, IntensityMin, IntensityMax);
        ApplyFinalIntensity();
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

    void ApplyFinalIntensity()
    {
        if (!targetLight)
            return;
        float flashFactor = Mathf.Lerp(1f, Mathf.Max(1f, pickupFlashMultiplier), pickupFlashBlend);
        targetLight.intensity = baseIntensityValue * flashFactor;
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
        baseIntensityValue = Mathf.Clamp(targetLight.intensity, IntensityMin, IntensityMax);
        boundsInitialized = true;
    }

    void TriggerPickupFlash()
    {
        if (!targetLight)
            return;

        if (pickupFlashDuration <= 0f || pickupFlashMultiplier <= 1f)
            return;

        pickupFlashBlend = 1f;
        if (pickupFlashRoutine != null)
            StopCoroutine(pickupFlashRoutine);
        pickupFlashRoutine = StartCoroutine(PickupFlashCoroutine());
    }

    IEnumerator PickupFlashCoroutine()
    {
        float timer = 0f;
        while (timer < pickupFlashDuration)
        {
            float normalized = Mathf.Clamp01(timer / Mathf.Max(0.0001f, pickupFlashDuration));
            pickupFlashBlend = 1f - Mathf.SmoothStep(0f, 1f, normalized);
            ApplyFinalIntensity();
            timer += Time.deltaTime;
            yield return null;
        }
        pickupFlashBlend = 0f;
        ApplyFinalIntensity();
        pickupFlashRoutine = null;
    }

    void CachePlayerGlowData()
    {
        if (playerGlowRenderers == null || playerGlowRenderers.Length == 0)
            return;

        glowBlocks = new MaterialPropertyBlock[playerGlowRenderers.Length];
        glowBaseEmission = new Color[playerGlowRenderers.Length];

        for (int i = 0; i < playerGlowRenderers.Length; i++)
        {
            var rend = playerGlowRenderers[i];
            if (!rend)
                continue;
            if (glowBlocks[i] == null)
                glowBlocks[i] = new MaterialPropertyBlock();
            var shared = rend.sharedMaterial;
            Color baseEmission = Color.black;
            if (shared && shared.HasProperty("_EmissionColor"))
            {
                baseEmission = shared.GetColor("_EmissionColor");
                shared.EnableKeyword("_EMISSION");
            }
            glowBaseEmission[i] = baseEmission;
        }
        ApplyPlayerGlow(0f);
    }

    void TriggerPlayerGlow()
    {
        if (playerGlowRenderers == null || playerGlowRenderers.Length == 0)
            return;
        if (playerGlowDuration <= 0f || playerGlowIntensity <= 0f)
            return;

        playerGlowBlend = 1f;
        if (playerGlowRoutine != null)
            StopCoroutine(playerGlowRoutine);
        playerGlowRoutine = StartCoroutine(PlayerGlowCoroutine());
        ApplyPlayerGlow(playerGlowBlend);
    }

    IEnumerator PlayerGlowCoroutine()
    {
        float timer = 0f;
        while (timer < playerGlowDuration)
        {
            float normalized = Mathf.Clamp01(timer / Mathf.Max(0.0001f, playerGlowDuration));
            float eased = 1f - Mathf.SmoothStep(0f, 1f, normalized);
            playerGlowBlend = eased;
            ApplyPlayerGlow(playerGlowBlend);
            timer += Time.deltaTime;
            yield return null;
        }
        playerGlowBlend = 0f;
        ApplyPlayerGlow(0f);
        playerGlowRoutine = null;
    }

    void ApplyPlayerGlow(float blend)
    {
        if (playerGlowRenderers == null || glowBlocks == null)
            return;
        float blendClamped = Mathf.Clamp01(blend);
        float scale = Mathf.Lerp(1f, Mathf.Max(1f, playerGlowIntensity), blendClamped);
        for (int i = 0; i < playerGlowRenderers.Length; i++)
        {
            var rend = playerGlowRenderers[i];
            if (!rend)
                continue;
            var block = glowBlocks[i];
            if (block == null)
                block = glowBlocks[i] = new MaterialPropertyBlock();
            Color baseEmission = glowBaseEmission != null && i < glowBaseEmission.Length ? glowBaseEmission[i] : Color.black;
            Color additive = playerGlowColor * playerGlowIntensity * blendClamped;
            Color targetEmission = baseEmission * scale + additive;
            block.SetColor("_EmissionColor", targetEmission);
            rend.SetPropertyBlock(block);
        }
    }

    float RangeMin => minRangeRuntime;
    float RangeMax => maxRangeRuntime;
    float IntensityMin => minIntensityRuntime;
    float IntensityMax => maxIntensityRuntime;
}
