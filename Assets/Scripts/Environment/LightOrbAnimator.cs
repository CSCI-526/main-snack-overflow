using UnityEngine;

/// <summary>
/// Simple bob + spin for collectible light orbs so they read at a glance.
/// </summary>
public class LightOrbAnimator : MonoBehaviour
{
    public float bobAmplitude = 0.25f;
    public float bobFrequency = 1.5f;
    public float rotationSpeed = 55f;

    Vector3 baseLocalPosition;

    void OnEnable()
    {
        baseLocalPosition = transform.localPosition;
    }

    void Update()
    {
        float bobOffset = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        Vector3 next = baseLocalPosition;
        next.y += bobOffset;
        transform.localPosition = next;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
    }
}
