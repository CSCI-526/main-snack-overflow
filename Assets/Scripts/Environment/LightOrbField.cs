using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scatters light-orb collectibles across a box volume so the player can
/// gradually brighten the world by exploring.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public class LightOrbField : MonoBehaviour
{
    [Header("Orb Setup")]
    [Tooltip("Prefab that represents a single light orb. If left empty a simple glowing sphere will be generated at runtime.")]
    public GameObject orbPrefab;
    [Tooltip("How much light energy each orb awards.")]
    public float lightReward = 1.25f;

    [Header("Generated Orb Look (used when no prefab is supplied)")]
    [Min(0.1f)]
    public float generatedOrbScale = 2.1f;
    public Color generatedOrbColor = new Color(1f, 0.95f, 0.2f);
    [Tooltip("Adds a point light to each generated orb. Leave disabled for a flat glow without world lighting.")]
    public bool addPointLight = false;
    [Tooltip("Radius of the glow emitted by auto-generated orbs (when point light is enabled).")]
    public float generatedLightRange = 18f;
    [Tooltip("Base intensity of the glow emitted by auto-generated orbs (when point light is enabled).")]
    public float generatedLightIntensity = 4.25f;

    [Header("Distribution")]
    public int orbCount = 18;
    public float minSpacing = 3f;
    public float spawnYOffset = 0.35f;
    public float raycastHeight = 25f;
    public LayerMask groundLayers = ~0;
    public int maxPlacementAttempts = 96;
    [Tooltip("Prevent new orbs from spawning within this radius of the active player.")]
    public float playerAvoidRadius = 2.5f;

    [Header("Lifecycle")]
    public bool spawnOnEnable = true;
    [Tooltip("If true, the field automatically repopulates once every orb has been collected.")]
    public bool respawnWhenCleared = true;
    public float respawnDelaySeconds = 6f;
    [Header("Continuous Spawn")]
    [Tooltip("Keep spawning new orbs over time instead of waiting for the whole field to be empty.")]
    public bool continuousSpawn = true;
    [Tooltip("Seconds between spawn attempts while the active orb count is below the target.")]
    public Vector2 spawnIntervalRange = new Vector2(2f, 4.5f);

    [Header("Debug")]
    public bool drawVolumeGizmo = true;
    public Color gizmoColor = new Color(1f, 0.91f, 0.55f, 0.33f);

    readonly List<GameObject> spawnedInstances = new List<GameObject>();
    readonly List<Vector3> occupiedPositions = new List<Vector3>();

    BoxCollider volume;
    float respawnTimer;
    float nextSpawnTime;

    void Reset()
    {
        volume = GetComponent<BoxCollider>() ?? gameObject.AddComponent<BoxCollider>();
        volume.isTrigger = true;
    }

    void Awake()
    {
        CacheComponents();
    }

    void OnValidate()
    {
        CacheComponents();
        orbCount = Mathf.Max(0, orbCount);
        minSpacing = Mathf.Max(0f, minSpacing);
        maxPlacementAttempts = Mathf.Max(1, maxPlacementAttempts);
        spawnIntervalRange.x = Mathf.Clamp(spawnIntervalRange.x, 0.1f, 999f);
        spawnIntervalRange.y = Mathf.Clamp(spawnIntervalRange.y, spawnIntervalRange.x, 999f);
        playerAvoidRadius = Mathf.Max(0f, playerAvoidRadius);
        generatedOrbScale = Mathf.Max(0.1f, generatedOrbScale);
        generatedLightRange = Mathf.Max(0f, generatedLightRange);
        generatedLightIntensity = Mathf.Max(0f, generatedLightIntensity);
    }

    void CacheComponents()
    {
        if (volume == null)
            volume = GetComponent<BoxCollider>();
        if (volume != null)
            volume.isTrigger = true;
    }

    void OnEnable()
    {
        if (spawnOnEnable)
            RespawnImmediate();
        ScheduleNextSpawn();
    }

    void OnDisable()
    {
        ClearSpawnedImmediate();
        respawnTimer = 0f;
        nextSpawnTime = 0f;
    }

    void Update()
    {
        if (!Application.isPlaying)
            return;

        int aliveCount = CleanupDestroyedOrbs();

        if (continuousSpawn)
        {
            if (aliveCount < orbCount && Time.time >= nextSpawnTime)
            {
                if (TrySpawnSingleOrb())
                    aliveCount++;
                ScheduleNextSpawn();
            }
            else if (aliveCount >= orbCount)
            {
                ScheduleNextSpawn();
            }
        }
        else if (respawnWhenCleared && respawnDelaySeconds > 0f)
        {
            if (aliveCount > 0)
            {
                respawnTimer = respawnDelaySeconds;
                return;
            }

            if (respawnTimer <= 0f)
                respawnTimer = respawnDelaySeconds;

            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
                RespawnImmediate();
        }
    }

    [ContextMenu("Respawn Light Orbs")]
    public void RespawnImmediate()
    {
        ClearSpawnedImmediate();
        SpawnOrbs();
        respawnTimer = respawnDelaySeconds;
    }

    void ClearSpawnedImmediate()
    {
        occupiedPositions.Clear();
        for (int i = 0; i < spawnedInstances.Count; i++)
        {
            var inst = spawnedInstances[i];
            if (!inst)
                continue;

            if (Application.isPlaying)
                Destroy(inst);
            else
                DestroyImmediate(inst);
        }
        spawnedInstances.Clear();
    }

    void SpawnOrbs()
    {
        if (volume == null)
        {
            Debug.LogWarning("LightOrbField requires a BoxCollider to define its spawn volume.", this);
            return;
        }

        if (orbCount <= 0)
            return;

        occupiedPositions.Clear();

        for (int i = 0; i < orbCount; i++)
        {
            if (!TrySpawnSingleOrb())
                break;
        }
    }

    bool TrySpawnSingleOrb()
    {
        if (!TrySamplePosition(out Vector3 position))
            return false;

        var instance = CreateOrbInstance(position);
        if (!instance)
            return false;

        spawnedInstances.Add(instance);
        occupiedPositions.Add(position);
        return true;
    }

    bool TrySamplePosition(out Vector3 position)
    {
        for (int attempt = 0; attempt < maxPlacementAttempts; attempt++)
        {
            Vector3 local = new Vector3(
                Random.Range(-0.5f, 0.5f) * volume.size.x,
                Random.Range(-0.5f, 0.5f) * volume.size.y,
                Random.Range(-0.5f, 0.5f) * volume.size.z);

            Vector3 world = transform.TransformPoint(volume.center + local);
            Vector3 rayOrigin = world + Vector3.up * raycastHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastHeight * 2f, groundLayers, QueryTriggerInteraction.Ignore))
            {
                Vector3 candidate = hit.point + Vector3.up * spawnYOffset;
                bool spacingOk = true;
                for (int j = 0; j < occupiedPositions.Count; j++)
                {
                    if ((occupiedPositions[j] - candidate).sqrMagnitude < minSpacing * minSpacing)
                    {
                        spacingOk = false;
                        break;
                    }
                }

                if (!spacingOk)
                    continue;

                if (!IsOutsidePlayerRadius(candidate))
                    continue;

                position = candidate;
                return true;
            }
        }

        position = default;
        return false;
    }

    GameObject CreateOrbInstance(Vector3 position)
    {
        GameObject instance;
        if (orbPrefab)
        {
            instance = Application.isPlaying
                ? Instantiate(orbPrefab, position, Quaternion.identity, transform)
                : Instantiate(orbPrefab, transform);
            if (instance)
                instance.transform.position = position;
        }
        else
        {
            instance = CreateGeneratedOrb(position);
        }

        if (!instance)
            return null;

        var collectible = instance.GetComponent<LightCollectible>();
        if (!collectible)
            collectible = instance.AddComponent<LightCollectible>();
        collectible.lightReward = lightReward;
        collectible.destroyOnPickup = true;

        var collider = instance.GetComponent<Collider>();
        if (!collider)
            collider = instance.AddComponent<SphereCollider>();
        collider.isTrigger = true;

        if (!instance.GetComponent<LightOrbAnimator>())
            instance.AddComponent<LightOrbAnimator>();

        instance.transform.SetParent(transform);
        return instance;
    }

    GameObject CreateGeneratedOrb(Vector3 position)
    {
        var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "Light Orb (Generated)";
        orb.transform.SetParent(transform, false);
        orb.transform.position = position;
        orb.transform.localScale = Vector3.one * generatedOrbScale;

        var collider = orb.GetComponent<Collider>();
        if (collider)
            collider.isTrigger = true;

        var renderer = orb.GetComponent<MeshRenderer>();
        if (renderer)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader);
            material.SetColor("_BaseColor", generatedOrbColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", generatedOrbColor * 2.5f);
            }
            renderer.sharedMaterial = material;
        }

        if (addPointLight)
        {
            var light = orb.GetComponentInChildren<Light>();
            if (!light)
                light = orb.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = generatedLightRange;
            light.intensity = generatedLightIntensity;
            light.color = generatedOrbColor;
        }
        else
        {
            var existingLight = orb.GetComponentInChildren<Light>();
            if (existingLight)
            {
                if (Application.isPlaying)
                    Destroy(existingLight);
                else
                    DestroyImmediate(existingLight);
            }
        }

        return orb;
    }

    bool IsOutsidePlayerRadius(Vector3 candidate)
    {
        if (playerAvoidRadius <= 0f)
            return true;

        var player = PlayerMover.Active;
        if (!PlayerMover.IsActivePlayer(player))
            return true;

        Vector3 playerPos = player.transform.position;
        playerPos.y = candidate.y;
        return (playerPos - candidate).sqrMagnitude >= playerAvoidRadius * playerAvoidRadius;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawVolumeGizmo || volume == null)
            return;

        Gizmos.color = gizmoColor;
        Matrix4x4 prev = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.DrawCube(volume.center, volume.size);
        Gizmos.matrix = prev;
    }

    int CleanupDestroyedOrbs()
    {
        int alive = 0;
        for (int i = spawnedInstances.Count - 1; i >= 0; i--)
        {
            if (spawnedInstances[i] == null)
            {
                spawnedInstances.RemoveAt(i);
                if (i < occupiedPositions.Count)
                    occupiedPositions.RemoveAt(i);
            }
            else
            {
                alive++;
            }
        }
        return alive;
    }

    void ScheduleNextSpawn()
    {
        if (!continuousSpawn)
            return;

        float interval = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
        nextSpawnTime = Time.time + interval;
    }
}
