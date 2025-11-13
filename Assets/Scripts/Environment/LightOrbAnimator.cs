using UnityEngine;

/// <summary>
/// Simple bob + spin for collectible light orbs so they read at a glance.
/// Adds a subtle magnet animation once the orb is being pulled toward the player.
/// </summary>
public class LightOrbAnimator : MonoBehaviour
{
    [Header("Idle Motion")]
    public float bobAmplitude = 0.25f;
    public float bobFrequency = 1.5f;
    public float rotationSpeed = 55f;

    [Header("Magnet Visuals")]
    public float magnetScaleMultiplier = 0.7f;
    public float magnetRotationMultiplier = 2f;
    public float magnetBobMultiplier = 1.6f;
    public float magnetLightIntensityMultiplier = 1.8f;
    public float magnetResponseSpeed = 6f;

    Vector3 lastBobOffset;
    Vector3 baseScale;
    Light orbLight;
    float baseLightIntensity;
    float baseLightRange;
    LightCollectible collectible;
    float magnetBlend;

    void OnEnable()
    {
        lastBobOffset = Vector3.zero;
        baseScale = transform.localScale;
        orbLight = GetComponent<Light>();
        if (orbLight)
        {
            baseLightIntensity = orbLight.intensity;
            baseLightRange = orbLight.range;
        }
        collectible = GetComponent<LightCollectible>();
        magnetBlend = 0f;
    }

    void Update()
    {
        bool magnetActive = collectible && collectible.MagnetActive;
        float targetBlend = magnetActive ? 1f : 0f;
        magnetBlend = Mathf.MoveTowards(magnetBlend, targetBlend, magnetResponseSpeed * Time.deltaTime);

        float currentBobAmplitude = Mathf.Lerp(bobAmplitude, bobAmplitude * magnetBobMultiplier, magnetBlend);
        float currentRotationSpeed = Mathf.Lerp(rotationSpeed, rotationSpeed * magnetRotationMultiplier, magnetBlend);
        float currentScaleMultiplier = Mathf.Lerp(1f, magnetScaleMultiplier, magnetBlend);

        Vector3 restLocalPosition = transform.localPosition - lastBobOffset;
        float bobOffset = Mathf.Sin(Time.time * bobFrequency) * currentBobAmplitude;
        Vector3 newBobOffset = new Vector3(0f, bobOffset, 0f);
        Vector3 targetLocal = restLocalPosition + newBobOffset;
        if (magnetBlend > 0f)
        {
            float followSpeed = Mathf.Max(0.01f, magnetResponseSpeed);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetLocal, followSpeed * Time.deltaTime);
        }
        else
        {
            transform.localPosition = targetLocal;
        }
        lastBobOffset = newBobOffset;

        transform.localScale = baseScale * currentScaleMultiplier;
        transform.Rotate(Vector3.up, currentRotationSpeed * Time.deltaTime, Space.World);

        if (orbLight)
        {
            orbLight.intensity = Mathf.Lerp(baseLightIntensity, baseLightIntensity * magnetLightIntensityMultiplier, magnetBlend);
            orbLight.range = Mathf.Lerp(baseLightRange, baseLightRange * magnetLightIntensityMultiplier, magnetBlend);
        }
    }
}
