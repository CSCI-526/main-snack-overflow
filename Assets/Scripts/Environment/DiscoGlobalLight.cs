using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Light2D))]
public class DiscoGlobalLight : MonoBehaviour
{
    const string LevelFourSceneName = "LvL4";

    [SerializeField] Light2D targetLight;
    [SerializeField, Min(0.1f)] float cycleDurationSeconds = 8f;
    [SerializeField] bool restrictToLevelFour = true;
    [SerializeField] Color[] paletteOverrides;

    static readonly Color[] DefaultPalette = new[]
    {
        new Color(0.96f, 0.31f, 0.56f),
        new Color(0.52f, 0.36f, 0.98f),
        new Color(0.18f, 0.78f, 0.98f),
        new Color(0.20f, 0.98f, 0.58f),
        new Color(0.99f, 0.87f, 0.31f),
    };

    float randomOffset;

    void Reset() => CacheComponents();

    void Awake()
    {
        CacheComponents();
        randomOffset = Random.value * cycleDurationSeconds;
    }

    void OnValidate() => CacheComponents();

    void CacheComponents()
    {
        if (!targetLight)
            targetLight = GetComponent<Light2D>();
        if (cycleDurationSeconds < 0.1f)
            cycleDurationSeconds = 0.1f;
    }

    void Update()
    {
        if (!targetLight || (restrictToLevelFour && !IsLevelFour()))
            return;

        float normalized = Mathf.Repeat(Time.time + randomOffset, cycleDurationSeconds) / cycleDurationSeconds;
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
