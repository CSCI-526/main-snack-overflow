using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Light))]
public class DiscoSunLight : MonoBehaviour
{
    const string LevelFourSceneName = "LvL4";

    [SerializeField] Light targetLight;
    [SerializeField, Min(0.1f)] float cycleDurationSeconds = 8f;
    [SerializeField] bool restrictToLevelFour = true;
    [SerializeField] float intensity = 1.1f;
    [SerializeField] Color[] paletteOverrides;

    static readonly Color[] DefaultPalette = new[]
    {
        new Color(0.97f, 0.32f, 0.54f),
        new Color(0.52f, 0.39f, 1.0f),
        new Color(0.18f, 0.73f, 1.0f),
        new Color(0.20f, 0.95f, 0.61f),
        new Color(0.99f, 0.86f, 0.31f)
    };

    float cycleOffset;

    void Reset() => CacheComponents();

    void Awake()
    {
        CacheComponents();
        cycleOffset = Random.value * cycleDurationSeconds;
        ApplyIntensity();
    }

    void OnValidate()
    {
        CacheComponents();
        ApplyIntensity();
    }

    void CacheComponents()
    {
        if (!targetLight)
            targetLight = GetComponent<Light>();
        if (targetLight)
            targetLight.type = LightType.Directional;
        if (cycleDurationSeconds < 0.1f)
            cycleDurationSeconds = 0.1f;
    }

    void ApplyIntensity()
    {
        if (targetLight)
            targetLight.intensity = Mathf.Max(0.01f, intensity);
    }

    void Update()
    {
        if (!targetLight || (restrictToLevelFour && !IsLevelFour()))
            return;

        float normalized = Mathf.Repeat(Time.time + cycleOffset, cycleDurationSeconds) / cycleDurationSeconds;
        targetLight.color = EvaluatePalette(normalized);
    }

    bool IsLevelFour()
    {
        var scene = gameObject.scene;
        if (scene.IsValid())
            return scene.name == LevelFourSceneName;
        return SceneManager.GetActiveScene().name == LevelFourSceneName;
    }

    Color EvaluatePalette(float t)
    {
        var palette = paletteOverrides != null && paletteOverrides.Length >= 2 ? paletteOverrides : DefaultPalette;
        if (palette == null || palette.Length == 0)
            return Color.white;
        if (palette.Length == 1)
            return palette[0];

        float scaled = t * palette.Length;
        int index = Mathf.FloorToInt(scaled) % palette.Length;
        int next = (index + 1) % palette.Length;
        float lerp = scaled - Mathf.Floor(scaled);
        return Color.Lerp(palette[index], palette[next], Mathf.SmoothStep(0f, 1f, lerp));
    }
}
