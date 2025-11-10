using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Automatically drops a LightOrbField into scenes that don't already
/// contain placed light collectibles, making sure the landscape always
/// has light-energy pickups without any manual scene wiring.
/// </summary>
static class LightOrbFieldBootstrap
{
    const string AutoFieldName = "Auto Light Orb Field";
    const float AreaPerOrb = 70f;          // larger number => fewer orbs
    const int MinOrbCount = 12;
    const int MaxOrbCount = 64;
    static bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (initialized)
            return;

        initialized = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureLightOrbs(SceneManager.GetActiveScene());
    }

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureLightOrbs(scene);
    }

    static void EnsureLightOrbs(Scene scene)
    {
        if (!Application.isPlaying || !scene.IsValid())
            return;

        if (SceneAlreadyHasLightCollectibles(scene))
            return;

        if (!TryFindLandscapeBounds(scene, out Bounds bounds))
        {
            Debug.LogWarning($"[LightOrbFieldBootstrap] Unable to determine landscape bounds in scene '{scene.name}'. Place a LightOrbField manually so light orbs can spawn.");
            return;
        }

        CreateAutoField(bounds, scene);
    }

    static bool SceneAlreadyHasLightCollectibles(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<LightOrbField>(true) != null)
                return true;
            if (root.GetComponentInChildren<LightCollectible>(true) != null)
                return true;
        }
        return false;
    }

    static bool TryFindLandscapeBounds(Scene scene, out Bounds bounds)
    {
        bounds = default;

        var builders = Object.FindObjectsOfType<WorldBoundsBuilder>();
        for (int i = 0; i < builders.Length; i++)
        {
            var builder = builders[i];
            if (!builder || !builder.sourceRoot)
                continue;

            if (builder.gameObject.scene != scene && builder.sourceRoot.gameObject.scene != scene)
                continue;

            if (TryCollectRendererBounds(builder.sourceRoot, out bounds))
            {
                ExpandBounds(ref bounds, 1f);
                return true;
            }
        }

        foreach (var candidate in EnumerateLikelyLandscapeRoots(scene))
        {
            if (TryCollectRendererBounds(candidate, out bounds))
            {
                ExpandBounds(ref bounds, 1f);
                return true;
            }
        }

        return TryCollectAllRendererBounds(scene, out bounds);
    }

    static IEnumerable<Transform> EnumerateLikelyLandscapeRoots(Scene scene)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            string name = root.name.ToLowerInvariant();
            if (name.Contains("ground") || name.Contains("land") || name.Contains("terrain") || name.Contains("floor"))
                yield return root.transform;
        }
    }

    static bool TryCollectRendererBounds(Transform root, out Bounds bounds)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        bounds = default;
        if (renderers == null || renderers.Length == 0)
            return false;

        bool initialized = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r || r is TrailRenderer || r is LineRenderer)
                continue;

            if (!initialized)
            {
                bounds = r.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }
        return initialized;
    }

    static bool TryCollectAllRendererBounds(Scene scene, out Bounds bounds)
    {
        bounds = default;
        bool initialized = false;
        var renderers = Object.FindObjectsOfType<Renderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            if (!r || r.gameObject.scene != scene)
                continue;

            if (r is TrailRenderer || r is LineRenderer)
                continue;

            if (!initialized)
            {
                bounds = r.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }
        if (initialized)
            ExpandBounds(ref bounds, 1f);
        return initialized;
    }

    static void ExpandBounds(ref Bounds bounds, float padding)
    {
        bounds.Expand(new Vector3(padding, 0f, padding));
    }

    static void CreateAutoField(Bounds bounds, Scene scene)
    {
        var autoRoot = new GameObject(AutoFieldName);
        autoRoot.SetActive(false);

        var collider = autoRoot.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.center = bounds.center;
        collider.size = new Vector3(bounds.size.x, Mathf.Max(2f, bounds.size.y + 2f), bounds.size.z);

        var field = autoRoot.AddComponent<LightOrbField>();
        field.spawnOnEnable = false;
        field.orbCount = ComputeOrbCount(bounds);
        field.minSpacing = Mathf.Clamp(Mathf.Sqrt(Mathf.Max(1f, bounds.size.x * bounds.size.z) / field.orbCount) * 0.85f, 2.5f, 6.5f);
        field.spawnYOffset = 0.6f;
        field.raycastHeight = Mathf.Max(20f, bounds.extents.y + 12f);
        field.lightReward = 1.5f;
        field.generatedLightRange = 9f;
        field.generatedLightIntensity = 3.8f;
        field.respawnDelaySeconds = 10f;

        SceneManager.MoveGameObjectToScene(autoRoot, scene);
        autoRoot.SetActive(true);
        field.RespawnImmediate();
    }

    static int ComputeOrbCount(Bounds bounds)
    {
        float area = Mathf.Max(1f, bounds.size.x * bounds.size.z);
        int count = Mathf.RoundToInt(area / AreaPerOrb);
        return Mathf.Clamp(count, MinOrbCount, MaxOrbCount);
    }
}
