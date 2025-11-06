using UnityEngine;

/// <summary>
/// Provides a gentle in-place drift for tutorial NPCs so they feel alive without wandering far.
/// </summary>
public class TutorialDrifter : MonoBehaviour
{
    Vector3 origin;
    Vector3 axisX;
    Vector3 axisZ;
    float speed;
    float phaseOffset;

    public void Initialise(float radius, float speed)
    {
        origin = transform.position;
        float rx = Mathf.Clamp(radius * Random.Range(0.45f, 0.9f), 0.05f, radius);
        float rz = Mathf.Clamp(radius * Random.Range(0.35f, 0.75f), 0.05f, radius);
        axisX = new Vector3(rx, 0f, 0f);
        axisZ = new Vector3(0f, 0f, rz);
        this.speed = Mathf.Max(0.05f, speed);
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void OnEnable()
    {
        origin = transform.position;
    }

    void Update()
    {
        float t = (Time.time + phaseOffset) * speed * Mathf.PI * 2f;
        transform.position = origin + axisX * Mathf.Sin(t) + axisZ * Mathf.Cos(t * 0.72f);
    }
}
