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
    public int civilianCount = 12;
    public int impostorCount = 4;

    public Transform[] npcSpawns;
    public BoxCollider movementBounds;     // Bounds3D
    public PathShape[] impostorPaths;

    [Header("Parents")]
    public Transform npcsParent;
    public Transform playerParent;

    const string LevelOneSceneName = "LvL1";
    static readonly string[] LevelOneWarmColorKeywords = { "raspberry", "carmine", "tomato" };

    int[] levelOneCivilianColorIdsCache;
    int levelOneImpostorColorIdCache = -2;

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
        levelOneCivilianColorIdsCache = null;
        levelOneImpostorColorIdCache = -2;
        usedSpawnPositions.Clear();
        SpawnPlayer();
        SpawnCivilians();
        SpawnImpostors();
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

    void SpawnCivilians()
    {
        for (int i = 0; i < civilianCount; i++)
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
            ApplyNPCIdentity(npc, false, shapeType, colorId);
        }
    }

    void SpawnImpostors()
    {
        if (GameRoundState.Instance == null || GameRoundState.Instance.allowedPairs == null)
            return;

        var pairs = GameRoundState.Instance.allowedPairs;
        if (pairs.Length == 0) return;

        bool isLevelOne = IsLevelOneScene();
        int impostorsToSpawn = isLevelOne ? 10 : impostorCount;
        int impostorColorId = isLevelOne ? DetermineLevelOneImpostorColorId() : -1;

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
            int appliedColor = isLevelOne && impostorColorId >= 0 ? impostorColorId : pair.colorId;
            ApplyNPCIdentity(npc, true, pair.shape, appliedColor);

            // Create a unique runtime path for this impostor
            var path = CreateRuntimePath(pair.shape, pos);

            // Make impostor follow its unique path
            var follower = npc.AddComponent<PathFollower>();
            follower.pathShape = path;
        }
    }


    void ApplyNPCIdentity(GameObject npc, bool isImpostor, PathShape.ShapeType shapeType, int colorId)
    {
        var id = npc.GetComponent<NPCIdentity>();
        if (!id) id = npc.AddComponent<NPCIdentity>();
        id.isImpostor = isImpostor;
        id.shapeType = shapeType;
        id.colorId = colorId;

        var rends = npc.GetComponentsInChildren<Renderer>();
        id.ApplyColor(palette, rends);
    }

    int PickCivilianColor()
    {
        if (!palette || palette.Count == 0)
            return 0;

        if (!IsLevelOneScene())
            return Random.Range(0, palette.Count);

        var warmColors = GetLevelOneCivilianColorIds();
        if (warmColors.Length == 0)
            return Random.Range(0, palette.Count);

        return warmColors[Random.Range(0, warmColors.Length)];
    }

    int DetermineLevelOneImpostorColorId()
    {
        if (levelOneImpostorColorIdCache != -2)
            return levelOneImpostorColorIdCache;

        if (!palette || palette.Count == 0)
        {
            levelOneImpostorColorIdCache = -1;
            return levelOneImpostorColorIdCache;
        }

        for (int i = 0; i < palette.Count; i++)
        {
            if (NameMatchesKeyword(palette.GetName(i), "red"))
            {
                levelOneImpostorColorIdCache = i;
                return levelOneImpostorColorIdCache;
            }
        }

        levelOneImpostorColorIdCache = 0;
        return levelOneImpostorColorIdCache;
    }

    int[] GetLevelOneCivilianColorIds()
    {
        if (levelOneCivilianColorIdsCache != null)
            return levelOneCivilianColorIdsCache;

        if (!palette || palette.Count == 0)
        {
            levelOneCivilianColorIdsCache = Array.Empty<int>();
            return levelOneCivilianColorIdsCache;
        }

        var matches = new List<int>();
        for (int i = 0; i < palette.Count; i++)
        {
            string name = palette.GetName(i);
            if (NameMatchesAnyKeyword(name, LevelOneWarmColorKeywords))
                matches.Add(i);
        }

        levelOneCivilianColorIdsCache = matches.ToArray();
        return levelOneCivilianColorIdsCache;
    }

    bool IsLevelOneScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == LevelOneSceneName;
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
    if (movementBounds == null)
    {
        Debug.LogError("MovementBounds not assigned on SpawnManager!");
        return Vector3.zero;
    }

    BoxCollider box = movementBounds;

    float concentrationFactor = 0.5f;

    Vector3 localPoint = new Vector3(
        Random.Range(-0.5f, 0.5f) * box.size.x * concentrationFactor,
        Random.Range(-0.5f, 0.5f) * box.size.y * concentrationFactor,
        0f // thin axis since map is rotated -90° on X
    );

    // Convert to world coordinates
    Vector3 worldPoint = box.transform.TransformPoint(box.center + localPoint);

    // Project to ground (optional but helps with terrain unevenness)
    worldPoint = ProjectToGround(worldPoint);

    Debug.Log($"Spawned at: {worldPoint}");
    return worldPoint;
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
