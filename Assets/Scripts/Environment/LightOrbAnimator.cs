using UnityEngine;

/// <summary>
/// Simple bobbing + pulsing animation that keeps collectible light orbs feeling alive.
/// </summary>
[DisallowMultipleComponent]
public class LightOrbAnimator : MonoBehaviour
{
    [Header("Hover")]
    public float hoverAmplitude = 0.25f;
    public float hoverFrequency = 0.5f; // cycles per second

    [Header("Rotation")]
    public float rotationSpeed = 35f;

    [Header("Light Flicker")]
    public float intensityPulseAmplitude = 0.35f;
    public float intensityPulseFrequency = 0.65f;

    Vector3 initialLocalPosition;
    float phaseOffset;
    Light cachedLight;
    float baseIntensity;

    void Awake()
    {
        CacheState();
    }

    void OnEnable()
    {
        CacheState();
    }

    void CacheState()
    {
        initialLocalPosition = transform.localPosition;
        phaseOffset = Random.value * Mathf.PI * 2f;
        cachedLight = GetComponentInChildren<Light>();
        baseIntensity = cachedLight ? cachedLight.intensity : 0f;
    }

    void Update()
    {
        float t = (Time.time + phaseOffset);

        if (hoverAmplitude > 0f && hoverFrequency > 0f)
        {
            float hover = Mathf.Sin(t * hoverFrequency * Mathf.PI * 2f) * hoverAmplitude;
            transform.localPosition = initialLocalPosition + Vector3.up * hover;
        }

        if (!Mathf.Approximately(rotationSpeed, 0f))
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        if (cachedLight && intensityPulseAmplitude > 0f && intensityPulseFrequency > 0f)
        {
            float pulse = Mathf.Sin(t * intensityPulseFrequency * Mathf.PI * 2f) * intensityPulseAmplitude;
            cachedLight.intensity = Mathf.Max(0f, baseIntensity * (1f + pulse));
        }
    }
}
