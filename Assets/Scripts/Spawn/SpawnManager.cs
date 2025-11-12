// using System.Collections.Generic;
// using UnityEngine;

// public class SpawnManager : MonoBehaviour
// {
//     public NPCColorPalette palette;
// public bool impostorsFollowPaths = true;   // keep current behavior

//     [Header("Player")]
//     public GameObject playerPrefab;
//     public Transform playerSpawn;

//     [Header("NPCs")]
//     public GameObject npcPrefab;
//     public int civilianCount = 30;
//     public int impostorCount = 4;

//     public Transform[] npcSpawns;
//     public BoxCollider movementBounds;     // Bounds3D
//     public PathShape[] impostorPaths;

//     [Header("Parents")]
//     public Transform npcsParent;
//     public Transform playerParent;
//     readonly List<Vector3> usedSpawnPositions = new List<Vector3>();

//     const float minimumSpawnSpacing = 1.1f;
//     const float spacingRelaxFactor = 0.7f;
//     const int spacingRelaxIterations = 4;
//     const int maxAttemptsPerIteration = 24;
//     const float defaultPathScale = 0.55f;

//     // void Start(){ SpawnPlayer(); SpawnCivilians(); SpawnImpostors(); }
//     public void StartSpawning()
// {
//     SpawnPlayer();
//     SpawnCivilians();
//     SpawnImpostors();
// }


//     void SpawnPlayer()
//     {
//         Vector3 p = playerSpawn ? playerSpawn.position : Vector3.zero; p.y = 0f;
//         Instantiate(playerPrefab, p, Quaternion.identity, playerParent);
//         usedSpawnPositions.Add(p);
//     }
    
//     PathShape FindPathByShape(PathShape.ShapeType t)
// {
//     if (impostorPaths == null) return null;
//     for (int i = 0; i < impostorPaths.Length; i++)
//     {
//         var p = impostorPaths[i];
//         if (p && p.shape == t) return p;
//     }
//     return null;
// }


//     void SpawnCivilians()
// {
//     int spawnCount = npcSpawns != null ? npcSpawns.Length : 0;
//     for (int i = 0; i < civilianCount; i++)
//     {
//         Transform sp = spawnCount > 0 ? npcSpawns[Random.Range(0, spawnCount)] : null;
//         Vector3 pos = SampleSpawnPosition(sp);
//         var npc = Instantiate(npcPrefab, pos, Quaternion.identity, npcsParent);

//         // choose a random strategy for decoy type:
//         // 0: same shape (from impostor paths), different color
//         // 1: same color (one of the two), different shape
//         // 2: neither (random wanderer)
//         int decoyType = Random.Range(0, 3);

//         PathShape.ShapeType shapeType = PathShape.ShapeType.Square; // default
//         int colorId = Random.Range(0, palette.Count);

//         var pairs = GameRoundState.Instance ? GameRoundState.Instance.allowedPairs : null;

//         if (decoyType == 0 && impostorPaths.Length > 0)
// {
//     // same shape as a random impostor path, but color != allowed
//     var anyShape = impostorPaths[Random.Range(0, impostorPaths.Length)].shape;
//     shapeType = anyShape;

//     int colA = pairs[0].colorId, colB = pairs[1].colorId;
//     do { colorId = Random.Range(0, palette.Count); } while (colorId == colA || colorId == colB);

//     var path = FindPathByShape(anyShape);
//     if (path != null)
//     {
//         var f = npc.AddComponent<PathFollower>();
//         f.pathShape = path;
//     }
//     else
//     {
//         // fallback: wander
//         var w = npc.AddComponent<NPCWander>();
//         w.movementBounds = movementBounds;
//     }
// }

//         else if (decoyType == 1)
//         {
//             // same color as one allowed, but different shape
//             int which = Random.Range(0, 2);
//             colorId = pairs[which].colorId;

//             // pick a shape not equal to the paired shape
//             PathShape.ShapeType avoid = pairs[which].shape;
//             shapeType = (PathShape.ShapeType)Random.Range(0, 4);
//             int guard = 0;
//             while (shapeType == avoid && guard++<8)
//                 shapeType = (PathShape.ShapeType)Random.Range(0, 4);

//             // Wanderer or path of different shape (optional)
//             var w = npc.AddComponent<NPCWander>();
//             w.movementBounds = movementBounds;
//         }
//         else
//         {
//             // neither matches (pure wanderer, random non-allowed color)
//             int colA = pairs[0].colorId, colB = pairs[1].colorId;
//             do { colorId = Random.Range(0, palette.Count); } while (colorId == colA || colorId == colB);
//             shapeType = (PathShape.ShapeType)Random.Range(0, 4);

//             var w = npc.AddComponent<NPCWander>();
//             w.movementBounds = movementBounds;
//         }

//         ApplyNPCIdentity(npc, false, shapeType, colorId);
//     }
// }


//   void SpawnImpostors()
// {
//     if (GameRoundState.Instance == null) return;
//     var pairs = GameRoundState.Instance.allowedPairs;
//     if (pairs == null || pairs.Length == 0) return;

//     // Build a plan that guarantees at least one impostor per pair,
//     // then fill up to impostorCount, and shuffle.
//     var plan = new List<GameRoundState.CluePair>(impostorCount);

//     // ensure coverage
//     for (int i = 0; i < pairs.Length && plan.Count < impostorCount; i++)
//         plan.Add(pairs[i]);

//     // fill the rest by cycling the pairs
//     int idx = 0;
//     while (plan.Count < impostorCount)
//     {
//         plan.Add(pairs[idx]);
//         idx = (idx + 1) % pairs.Length;
//     }

//     // shuffle so they don't always spawn in the same order
//     Shuffle(plan);

//     int spawnCount = npcSpawns != null ? npcSpawns.Length : 0;

//     for (int i = 0; i < plan.Count; i++)
//     {
//         var pair = plan[i];
//         var path = FindPathByShape(pair.shape);
//         if (path == null)
//         {
//             Debug.LogWarning($"No PathShape found in impostorPaths for {pair.shape}. Add one to the array.");
//             continue;
//         }

//         Transform sp = spawnCount > 0 ? npcSpawns[Random.Range(0, spawnCount)] : null;
//         Vector3 pos = SampleImpostorSpawnPosition(sp, path);

//         var npc = Instantiate(npcPrefab, pos, Quaternion.identity, npcsParent);
//         ApplyNPCIdentity(npc, true, pair.shape, pair.colorId);

//         if (impostorsFollowPaths)
//         {
//             var f = npc.AddComponent<PathFollower>();
//             f.pathShape = path;
//         }
//         else
//         {
//             var w = npc.AddComponent<NPCWander>();
//             w.movementBounds = movementBounds;
//         }
//     }
// }

// // Fisher–Yates shuffle
// void Shuffle<T>(IList<T> list)
// {
//     for (int i = list.Count - 1; i > 0; i--)
//     {
//         int j = Random.Range(0, i + 1);
//         (list[i], list[j]) = (list[j], list[i]);
//     }
// }



    
//     void ApplyNPCIdentity(GameObject npc, bool isImpostor, PathShape.ShapeType shapeType, int colorId)
// {
//     var id = npc.GetComponent<NPCIdentity>();
//     if (!id) id = npc.AddComponent<NPCIdentity>();
//     id.isImpostor = isImpostor;
//     id.shapeType = shapeType;
//     id.colorId = colorId;

//     var rends = npc.GetComponentsInChildren<Renderer>();
//     id.ApplyColor(palette, rends);
// }


//     Vector3 SampleSpawnPosition(Transform fallback)
//     {
//         if (TrySampleWithinBounds(out Vector3 position))
//         {
//             return position;
//         }

//         Vector3 basePos = fallback ? new Vector3(fallback.position.x, 0f, fallback.position.z) : Vector3.zero;
//         if (TrySampleAround(basePos, 0.9f, out position))
//         {
//             return position;
//         }

//         position = basePos + RandomHorizontalOffset(0.45f);
//         usedSpawnPositions.Add(position);
//         return position;
//     }

//     Vector3 SampleImpostorSpawnPosition(Transform fallback, PathShape path)
//     {
//         if (path)
//         {
//             var points = path.GetPoints();
//             if (points != null && points.Length > 0)
//             {
//                 Vector3 center = path.transform.position;
//                 float scale = Mathf.Clamp(defaultPathScale, 0.15f, 1f);
//                 float spacing = minimumSpawnSpacing;
//                 for (int relax = 0; relax < spacingRelaxIterations; relax++)
//                 {
//                     for (int attempt = 0; attempt < maxAttemptsPerIteration; attempt++)
//                     {
//                         int pointIndex = Random.Range(0, points.Length);
//                         Vector3 start = Vector3.Lerp(center, points[pointIndex], scale);
//                         if (IsFarEnough(start, spacing))
//                         {
//                             usedSpawnPositions.Add(start);
//                             return start;
//                         }
//                     }
//                     spacing *= spacingRelaxFactor;
//                 }
//             }
//         }

//         return SampleSpawnPosition(fallback);
//     }

//     bool TrySampleWithinBounds(out Vector3 position)
//     {
//         position = default;
//         if (!movementBounds) return false;

//         Bounds b = movementBounds.bounds;
//         float spacing = minimumSpawnSpacing;
//         for (int relax = 0; relax < spacingRelaxIterations; relax++)
//         {
//             for (int attempt = 0; attempt < maxAttemptsPerIteration; attempt++)
//             {
//                 float x = Random.Range(b.min.x, b.max.x);
//                 float z = Random.Range(b.min.z, b.max.z);
//                 Vector3 candidate = new Vector3(x, 0f, z);
//                 if (IsFarEnough(candidate, spacing))
//                 {
//                     usedSpawnPositions.Add(candidate);
//                     position = candidate;
//                     return true;
//                 }
//             }
//             spacing *= spacingRelaxFactor;
//         }

//         return false;
//     }

//     bool TrySampleAround(Vector3 center, float radius, out Vector3 position)
//     {
//         position = default;
//         float spacing = minimumSpawnSpacing;
//         for (int relax = 0; relax < spacingRelaxIterations; relax++)
//         {
//             for (int attempt = 0; attempt < maxAttemptsPerIteration; attempt++)
//             {
//                 Vector3 candidate = center + RandomHorizontalOffset(radius);
//                 if (IsFarEnough(candidate, spacing))
//                 {
//                     usedSpawnPositions.Add(candidate);
//                     position = candidate;
//                     return true;
//                 }
//             }
//             spacing *= spacingRelaxFactor;
//         }

//         return false;
//     }

//     bool IsFarEnough(Vector3 candidate, float spacing)
//     {
//         foreach (var pos in usedSpawnPositions)
//         {
//             if ((pos - candidate).sqrMagnitude < spacing * spacing)
//                 return false;
//         }
//         return true;
//     }

//     static Vector3 RandomHorizontalOffset(float radius)
//     {
//         Vector2 offset2D = Random.insideUnitCircle * radius;
//         return new Vector3(offset2D.x, 0f, offset2D.y);
//     }
// }


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class SpawnManager : MonoBehaviour
{
    public NPCColorPalette palette;
    public bool impostorsFollowPaths = true;   // keep current behavior

    [Header("Player")]
    public GameObject playerPrefab;
    public Transform playerSpawn;

    [Header("NPCs")]
    public GameObject npcPrefab;
    public int civilianCount = 35;
    public int impostorCount = 4;

    public Transform[] npcSpawns;
    public BoxCollider movementBounds;     // Bounds3D
    public PathShape[] impostorPaths;

    [Header("Distribution")]
    [Range(0.6f, 1f)]
    public float spawnSpreadFactor = 1f;
    [Range(0f, 0.3f)]
    public float spawnEdgePadding = 0.08f;

    [Header("Dynamic Density")]
    [Tooltip("Minimum civilians per 100 square units of navigable area.")]
    public float civiliansPerHundredSquareUnits = 1.15f;
    [Tooltip("Minimum impostors per 100 square units of navigable area (unless red focus overrides).")]
    public float impostorsPerHundredSquareUnits = 0.2f;
    [Tooltip("Clamp range for auto-resolved civilian counts.")]
    public Vector2Int civilianAutoCountRange = new Vector2Int(45, 140);
    [Tooltip("Clamp range for auto-resolved impostor counts.")]
    public Vector2Int impostorAutoCountRange = new Vector2Int(4, 14);
    [Tooltip("Scales the inspector civilian count before density is applied.")]
    [Range(1f, 2.5f)]
    public float civilianCountMultiplier = 1.45f;
    [Tooltip("Boosts density-derived civilian counts so large arenas feel full.")]
    [Range(1f, 2f)]
    public float civilianDensityMultiplier = 1.2f;

    [Header("Parents")]
    public Transform npcsParent;
    public Transform playerParent;

    [Header("HUD")]
    [Tooltip("Optional root object (e.g., HUD canvas) that contains the ImpostorColorIndicator. " +
             "If assigned, it will be activated automatically when a color-cycle level (LvL2/LvL3) begins.")]
    public GameObject colorCycleHUDRoot;

    static readonly string[] RedFocusScenes = { "LvL1", "LvL2" };
    static readonly string[] RedFocusWarmColorKeywords = { "raspberry", "carmine", "tomato", "pink", "magenta", "orange" };

    const string LevelTwoSceneName = "LvL2";
    const string LevelThreeSceneName = "LvL3";
    const float LevelTwoColorShiftSeconds = 15f;
    const float LevelThreeColorShiftSeconds = 15f;
    static readonly string[] ColorCycleKeywords = { "red", "orange", "pink", "yellow", "green" };

    int[] redFocusCivilianColorIdsCache;
    int redFocusImpostorColorIdCache = -2;
    readonly List<NPCIdentity> spawnedImpostors = new List<NPCIdentity>();
    readonly List<NPCIdentity> spawnedCivilians = new List<NPCIdentity>();
    Coroutine colorCycleRoutine;
    float currentColorCycleShiftSeconds = LevelTwoColorShiftSeconds;

    readonly List<Vector3> usedSpawnPositions = new List<Vector3>();

    const float minimumSpawnSpacing = 3.5f;     // was 1.1f
    const float spacingRelaxFactor = 0.85f;     // was 0.7f
    const int spacingRelaxIterations = 6;       // was 4
    const int maxAttemptsPerIteration = 50;     // was 24
    const float defaultPathScale = 0.55f;

    [Header("Grounding (for impostor starts)")]
    public LayerMask groundMask = ~0;
    public float groundProbeHeight = 4f;
    public float groundProbeDistance = 12f;

    // -----------------------------
    // Delayed spawn (called after memory phase)
    // -----------------------------
    public void StartSpawning()
    {
        redFocusCivilianColorIdsCache = null;
        redFocusImpostorColorIdCache = -2;
        spawnedImpostors.Clear();
        spawnedCivilians.Clear();
        StopColorCycle();
        GameRoundState.Instance?.ClearImpostorColorOverride();
        usedSpawnPositions.Clear();
        SpawnPlayer();
        SpawnCivilians();
        SpawnImpostors();
        if (TryGetColorCycleShift(out float shiftSeconds))
            StartColorCycle(shiftSeconds);
    }

    void SpawnPlayer()
    {
        Vector3 p = playerSpawn ? playerSpawn.position : Vector3.zero;
        p.y = 0f;

        var existing = FindObjectOfType<PlayerMover>();
        if (existing != null)
        {
            existing.transform.position = p;
            if (playerParent != null && existing.transform.parent != playerParent)
                existing.transform.SetParent(playerParent);
            usedSpawnPositions.Add(p);
            return;
        }

        Instantiate(playerPrefab, p, Quaternion.identity, playerParent);
        usedSpawnPositions.Add(p);
    }

    void OnDisable()
    {
        StopColorCycle();
    }

    void SpawnCivilians()
    {
        int targetCivilianCount = ResolveCivilianCount();
        for (int i = 0; i < targetCivilianCount; i++)
        {
            // Pick random spawn position within movement bounds
            Vector3 pos = GetRandomPointInMovementBounds();

            // Instantiate NPC
            var npc = Instantiate(npcPrefab, pos, Quaternion.identity, npcsParent);

            // Random shape and color
            PathShape.ShapeType shapeType = (PathShape.ShapeType)Random.Range(0, 4);
            int colorId = PickCivilianColor();

            // Create a unique path at the NPC’s spawn location
            var path = CreateRuntimePath(shapeType, pos);

            // Assign PathFollower to follow this path
            var follower = npc.AddComponent<PathFollower>();
            follower.pathShape = path;

            // Apply civilian identity (false = not impostor)
            var identity = ApplyNPCIdentity(npc, false, shapeType, colorId);
            if (identity != null)
                spawnedCivilians.Add(identity);
        }
    }

    void SpawnImpostors()
    {
        if (GameRoundState.Instance == null || GameRoundState.Instance.allowedPairs == null)
            return;

        var pairs = GameRoundState.Instance.allowedPairs;
        if (pairs.Length == 0) return;

        bool forceRedRules = IsRedFocusScene();
        int impostorsToSpawn = ResolveImpostorCount(forceRedRules);
        int impostorColorId = forceRedRules ? DetermineRedFocusImpostorColorId() : -1;

        for (int i = 0; i < impostorsToSpawn; i++)
        {
            // Randomly pick one allowed impostor pair (shape + color)
            var pair = pairs[Random.Range(0, pairs.Length)];

            // Spawn randomly within the movement bounds (using your fixed GetRandomPointInMovementBounds)
            Vector3 pos = GetRandomPointInMovementBounds();

            // Create the impostor NPC at that position
            var npc = Instantiate(npcPrefab, pos, Quaternion.identity, npcsParent);
            ImpostorTracker.Instance?.RegisterImpostor();

            // Assign impostor identity (true = impostor)
            int appliedColor = forceRedRules && impostorColorId >= 0 ? impostorColorId : pair.colorId;
            var identity = ApplyNPCIdentity(npc, true, pair.shape, appliedColor);
            if (identity != null)
                spawnedImpostors.Add(identity);

            // Create a unique runtime path for this impostor
            var path = CreateRuntimePath(pair.shape, pos);

            // Make impostor follow its unique path
            var follower = npc.AddComponent<PathFollower>();
            follower.pathShape = path;
        }
    }

    int ResolveCivilianCount()
    {
        int baseline = Mathf.RoundToInt(Mathf.Max(0f, civilianCount) * civilianCountMultiplier);
        float densityRaw = GetMovementPlaneArea() / 100f * civiliansPerHundredSquareUnits;
        int densityCount = Mathf.RoundToInt(densityRaw * civilianDensityMultiplier);
        int resolved = Mathf.Max(baseline, densityCount);
        resolved = Mathf.Clamp(resolved, civilianAutoCountRange.x, civilianAutoCountRange.y);
        return resolved;
    }

    int ResolveImpostorCount(bool forceRedRules)
    {
        int densityCount = Mathf.RoundToInt(GetMovementPlaneArea() / 100f * impostorsPerHundredSquareUnits);
        int resolved = Mathf.Max(impostorCount, densityCount);
        if (forceRedRules)
            resolved = Mathf.Max(resolved, 10);
        resolved = Mathf.Clamp(resolved, impostorAutoCountRange.x, impostorAutoCountRange.y);
        return resolved;
    }

    float GetMovementPlaneArea()
    {
        if (!movementBounds)
            return 0f;

        Vector3 size = movementBounds.size;
        return Mathf.Abs(size.x * size.y);
    }


    NPCIdentity ApplyNPCIdentity(GameObject npc, bool isImpostor, PathShape.ShapeType shapeType, int colorId)
    {
        var id = npc.GetComponent<NPCIdentity>();
        if (!id) id = npc.AddComponent<NPCIdentity>();
        id.isImpostor = isImpostor;
        id.shapeType = shapeType;
        id.colorId = colorId;

        var rends = npc.GetComponentsInChildren<Renderer>();
        id.ApplyColor(palette, rends);
        return id;
    }

    void StartColorCycle(float shiftSeconds)
    {
        StopColorCycle();

        currentColorCycleShiftSeconds = shiftSeconds;
        EnsureColorCycleHUDActive();

        var sequence = BuildColorCycleSequence();
        if (sequence.Length == 0)
            return;

        ApplyImpostorColor(sequence[0], false);
        ApplyCycleCivilianColors(sequence, sequence[0]);

        if (sequence.Length == 1)
            return;

        colorCycleRoutine = StartCoroutine(ColorCycleRoutine(sequence, 0));
    }

    void StopColorCycle()
    {
        if (colorCycleRoutine != null)
        {
            StopCoroutine(colorCycleRoutine);
            colorCycleRoutine = null;
        }
    }

    void EnsureColorCycleHUDActive()
    {
        if (colorCycleHUDRoot && !colorCycleHUDRoot.activeSelf)
            colorCycleHUDRoot.SetActive(true);

        var indicator = ImpostorColorIndicator.Instance;
        if (!indicator)
        {
            if (colorCycleHUDRoot)
                indicator = colorCycleHUDRoot.GetComponentInChildren<ImpostorColorIndicator>(true);
            if (!indicator)
                indicator = FindObjectOfType<ImpostorColorIndicator>(true);
        }

        if (indicator && !indicator.gameObject.activeSelf)
            indicator.gameObject.SetActive(true);
    }

    IEnumerator ColorCycleRoutine(int[] sequence, int currentIndex)
    {
        if (sequence == null || sequence.Length <= 1)
            yield break;

        int index = currentIndex;

        while (true)
        {
            yield return new WaitForSeconds(currentColorCycleShiftSeconds);
            index = (index + 1) % sequence.Length;
            ApplyImpostorColor(sequence[index], true);
            ApplyCycleCivilianColors(sequence, sequence[index]);
        }
    }

    void ApplyImpostorColor(int colorId, bool announce)
    {
        if (colorId < 0)
            return;

        var roundState = GameRoundState.Instance;
        roundState?.SetImpostorColorOverride(colorId);
        ApplyColorToImpostors(colorId);
        ImpostorColorIndicator.Instance?.SetColor(palette, colorId);

        if (announce)
            AnnounceColorChange(colorId);
    }

    void ApplyColorToImpostors(int colorId)
    {
        if (!palette)
            return;

        spawnedImpostors.RemoveAll(id => id == null);

        foreach (var id in spawnedImpostors)
        {
            if (!id) continue;
            id.colorId = colorId;
            var rends = id.GetComponentsInChildren<Renderer>();
            id.ApplyColor(palette, rends);
        }
    }

    void ApplyCycleCivilianColors(int[] sequence, int impostorColorId)
    {
        if (!palette)
            return;

        spawnedCivilians.RemoveAll(id => id == null);
        if (spawnedCivilians.Count == 0)
            return;

        var pool = new List<int>();
        if (sequence != null)
        {
            for (int i = 0; i < sequence.Length; i++)
            {
                int id = sequence[i];
                if (id == impostorColorId || pool.Contains(id))
                    continue;
                pool.Add(id);
            }
        }

        if (pool.Count == 0)
        {
            for (int i = 0; i < palette.Count; i++)
            {
                if (i == impostorColorId)
                    continue;
                pool.Add(i);
            }
        }

        if (pool.Count == 0)
            return;

        int offset = Random.Range(0, pool.Count);
        for (int i = 0; i < spawnedCivilians.Count; i++)
        {
            var civ = spawnedCivilians[i];
            if (!civ)
                continue;

            int colorId = pool[(offset + i) % pool.Count];
            civ.colorId = colorId;
            var rends = civ.GetComponentsInChildren<Renderer>();
            civ.ApplyColor(palette, rends);
        }
    }

    void AnnounceColorChange(int impostorColorId)
    {
        if (!palette)
            return;

        string impostorName = ResolveColorName(impostorColorId);
        KillTextController.Instance?.Show($"Impostors are now {impostorName}!");
    }

    string ResolveColorName(int colorId)
    {
        if (!palette)
            return "a new color";

        string colorName = palette.GetName(colorId);
        return string.IsNullOrEmpty(colorName) ? "a new color" : colorName;
    }

    int[] BuildColorCycleSequence()
    {
        if (!palette || palette.Count == 0)
            return Array.Empty<int>();

        var ids = new List<int>();
        foreach (var keyword in ColorCycleKeywords)
        {
            int id = FindColorIdByKeyword(keyword);
            if (id >= 0 && !ids.Contains(id))
                ids.Add(id);
        }

        if (ids.Count > 0 && ids.Count < ColorCycleKeywords.Length)
        {
            Debug.LogWarning($"[SpawnManager] Only found {ids.Count} of {ColorCycleKeywords.Length} requested color-cycle palette entries.");
        }

        if (ids.Count == 0)
        {
            for (int i = 0; i < palette.Count; i++)
                ids.Add(i);
        }

        return ids.ToArray();
    }

    int FindColorIdByKeyword(string keyword)
    {
        if (!palette || string.IsNullOrEmpty(keyword))
            return -1;

        for (int i = 0; i < palette.Count; i++)
        {
            string name = palette.GetName(i);
            if (NameMatchesKeyword(name, keyword))
                return i;
        }
        return -1;
    }

    int PickCivilianColor()
    {
        if (!palette || palette.Count == 0)
            return 0;

        if (!IsRedFocusScene())
            return Random.Range(0, palette.Count);

        var warmColors = GetRedFocusCivilianColorIds();
        if (warmColors.Length == 0)
            return Random.Range(0, palette.Count);

        return warmColors[Random.Range(0, warmColors.Length)];
    }

    int DetermineRedFocusImpostorColorId()
    {
        if (redFocusImpostorColorIdCache != -2)
            return redFocusImpostorColorIdCache;

        if (!palette || palette.Count == 0)
        {
            redFocusImpostorColorIdCache = -1;
            return redFocusImpostorColorIdCache;
        }

        for (int i = 0; i < palette.Count; i++)
        {
            if (NameMatchesKeyword(palette.GetName(i), "red"))
            {
                redFocusImpostorColorIdCache = i;
                return redFocusImpostorColorIdCache;
            }
        }

        redFocusImpostorColorIdCache = 0;
        return redFocusImpostorColorIdCache;
    }

    int[] GetRedFocusCivilianColorIds()
    {
        if (redFocusCivilianColorIdsCache != null)
            return redFocusCivilianColorIdsCache;

        if (!palette || palette.Count == 0)
        {
            redFocusCivilianColorIdsCache = Array.Empty<int>();
            return redFocusCivilianColorIdsCache;
        }

        var matches = new List<int>();
        for (int i = 0; i < palette.Count; i++)
        {
            string name = palette.GetName(i);
            if (NameMatchesAnyKeyword(name, RedFocusWarmColorKeywords))
                matches.Add(i);
        }

        redFocusCivilianColorIdsCache = matches.ToArray();
        return redFocusCivilianColorIdsCache;
    }

    bool IsRedFocusScene()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return false;
        var currentName = scene.name;
        for (int i = 0; i < RedFocusScenes.Length; i++)
        {
            if (string.Equals(currentName, RedFocusScenes[i], StringComparison.Ordinal))
                return true;
        }
        return false;
    }

    bool TryGetColorCycleShift(out float shiftSeconds)
    {
        shiftSeconds = 0f;
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid()) return false;

        switch (scene.name)
        {
            case LevelTwoSceneName:
                shiftSeconds = LevelTwoColorShiftSeconds;
                return true;
            case LevelThreeSceneName:
                shiftSeconds = LevelThreeColorShiftSeconds;
                return true;
            default:
                return false;
        }
    }

    static bool NameMatchesAnyKeyword(string name, string[] keywords)
    {
        if (string.IsNullOrEmpty(name) || keywords == null || keywords.Length == 0)
            return false;

        foreach (var keyword in keywords)
        {
            if (NameMatchesKeyword(name, keyword))
                return true;
        }
        return false;
    }

    static bool NameMatchesKeyword(string name, string keyword)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(keyword))
            return false;

        return name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    Vector3 GetRandomPointInMovementBounds()
    {
        if (!movementBounds)
        {
            Debug.LogError("MovementBounds not assigned on SpawnManager!");
            return Vector3.zero;
        }

        float spacing = minimumSpawnSpacing;
        for (int relax = 0; relax < spacingRelaxIterations; relax++)
        {
            for (int attempt = 0; attempt < maxAttemptsPerIteration; attempt++)
            {
                Vector3 candidate = ProjectToGround(SamplePointWithinBounds());
                if (IsFarEnoughFromUsed(candidate, spacing))
                {
                    usedSpawnPositions.Add(candidate);
                    return candidate;
                }
            }

            spacing *= spacingRelaxFactor;
        }

        Vector3 fallback = ProjectToGround(SamplePointWithinBounds());
        usedSpawnPositions.Add(fallback);
        return fallback;
    }

    Vector3 SamplePointWithinBounds()
    {
        var box = movementBounds;
        Vector3 halfSize = box.size * 0.5f;
        float spread = Mathf.Clamp(spawnSpreadFactor, 0.1f, 1f);
        float padPercent = Mathf.Clamp01(spawnEdgePadding);

        float spanX = halfSize.x * spread;
        float spanY = halfSize.y * spread;
        float padX = spanX * padPercent;
        float padY = spanY * padPercent;

        float minX = -spanX + padX;
        float maxX = spanX - padX;
        float minY = -spanY + padY;
        float maxY = spanY - padY;

        float x = Random.Range(minX, maxX);
        float y = Random.Range(minY, maxY);

        Vector3 localPoint = new Vector3(x, y, 0f);
        return box.transform.TransformPoint(box.center + localPoint);
    }

    bool IsFarEnoughFromUsed(Vector3 candidate, float spacing)
    {
        if (spacing <= 0f || usedSpawnPositions.Count == 0)
            return true;

        float sqrSpacing = spacing * spacing;
        for (int i = 0; i < usedSpawnPositions.Count; i++)
        {
            Vector3 existing = usedSpawnPositions[i];
            Vector2 delta = new Vector2(existing.x - candidate.x, existing.z - candidate.z);
            if (delta.sqrMagnitude < sqrSpacing)
                return false;
        }

        return true;
    }

    // Teammate's grounding helper for placing impostors on terrain/meshes
    Vector3 ProjectToGround(Vector3 position)
    {
        Vector3 origin = position + Vector3.up * groundProbeHeight;
        float maxDistance = groundProbeHeight + groundProbeDistance;

        if (Physics.Raycast(origin, Vector3.down, out var hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            position.y = hit.point.y;
        }
        else
        {
            position.y = 0f;
        }
        return position;
    }

    PathShape CreateRuntimePath(PathShape.ShapeType shapeType, Vector3 center)
{
    // Create a unique GameObject to host this NPC’s personal path
    var go = new GameObject($"Path_{shapeType}_Instance");
    go.transform.position = center;

    // Optional: nudge center slightly so the loop isn’t centered exactly on the spawn point
    Vector2 jitter = Random.insideUnitCircle * 1.25f;
    go.transform.position += new Vector3(jitter.x, 0f, jitter.y);

    // Random orientation for variety; PathShape uses rotationDegY (not transform.rotation) for points
    float rotY = Random.Range(0f, 360f);
    go.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

    // Add PathShape and set ONLY existing fields from your PathShape.cs
    var ps = go.AddComponent<PathShape>();
    ps.shape = shapeType;            // Triangle, Square, Pentagon, Circle
    ps.radius = Random.Range(2.8f, 5.5f);
    ps.rotationDegY = rotY;
    ps.circlePoints = 20;            // used only if shape == Circle
    ps.closed = true;

    // Do NOT reference impostorPaths here; do NOT call FindPathByShape here.
    // Do NOT call a non-existent build method. PathFollower will call GetPoints().

    // Leave this visible in Hierarchy while you test:
    // go.hideFlags = HideFlags.HideInHierarchy;

    return ps;
}



}
