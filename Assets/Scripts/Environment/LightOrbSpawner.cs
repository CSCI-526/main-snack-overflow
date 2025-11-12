using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Drops a configured set of glowing orbs into the scene at runtime.
/// Each orb becomes a LightCollectible that boosts the player lantern.
/// </summary>
public class LightOrbSpawner : MonoBehaviour
{
    [Tooltip("Local positions for each orb (Y is overridden by orbHeight).")]
    public Vector3[] orbPositions;
    public float orbHeight = 1.4f;
    public float orbScale = 0.9f;
    public float orbReward = 1f;

    [Header("Hint Light")]
    [Tooltip("Subtle light to help players spot the orb without flooding the map.")]
    public float orbLightRange = 2.0f;
    public float orbLightIntensity = 2.0f;
    public Color orbLightColor = new Color(1f, 0.88f, 0.4f);

    public Material orbMaterial;
    public float orbBobAmplitude = 0.35f;
    public float orbBobFrequency = 1.65f;
    public float orbRotationSpeed = 65f;
    public bool spawnOnStart = true;
    public string orbName = "Light Orb";

    readonly List<GameObject> spawnedOrbs = new List<GameObject>();

    void Start()
    {
        if (spawnOnStart)
            SpawnOrbs();
    }

    public void SpawnOrbs()
    {
        if (orbPositions == null || orbPositions.Length == 0)
            return;

        if (spawnedOrbs.Count > 0)
            return;

        for (int i = 0; i < orbPositions.Length; i++)
        {
            Vector3 local = orbPositions[i];
            local.y = orbHeight;
            SpawnSingle(local, i);
        }
    }

    void SpawnSingle(Vector3 localPosition, int index)
    {
        var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = $"{orbName} {index + 1}";
        orb.transform.SetParent(transform, false);
        orb.transform.localPosition = localPosition;
        orb.transform.localScale = Vector3.one * orbScale;

        var col = orb.GetComponent<Collider>();
        if (col) col.isTrigger = true;

        var renderer = orb.GetComponent<MeshRenderer>();
        if (renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            if (orbMaterial)
                renderer.sharedMaterial = orbMaterial;
        }

        var light = orb.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = orbLightRange;
        light.intensity = orbLightIntensity;
        light.color = orbLightColor;
        light.shadows = LightShadows.None;

        var collectible = orb.AddComponent<LightCollectible>();
        collectible.lightReward = orbReward;

        var animator = orb.AddComponent<LightOrbAnimator>();
        animator.bobAmplitude = orbBobAmplitude;
        animator.bobFrequency = orbBobFrequency;
        animator.rotationSpeed = orbRotationSpeed;

        spawnedOrbs.Add(orb);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (orbPositions == null)
            return;

        Gizmos.color = new Color(orbLightColor.r, orbLightColor.g, orbLightColor.b, 0.6f);
        for (int i = 0; i < orbPositions.Length; i++)
        {
            Vector3 local = orbPositions[i];
            local.y = orbHeight;
            Gizmos.DrawWireSphere(transform.TransformPoint(local), orbScale * 0.5f);
        }
    }
#endif
}
