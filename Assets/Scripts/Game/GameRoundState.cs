using UnityEngine;

public class GameRoundState : MonoBehaviour
{
    public static GameRoundState Instance { get; private set; }

    [System.Serializable]
    public struct CluePair { public PathShape.ShapeType shape; public int colorId; }

    [Header("Round Clues (2)")]
    public CluePair[] allowedPairs = new CluePair[2];  // filled by MemoryBar at start

    [Header("Palette")]
    public NPCColorPalette palette;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool MatchesAllowed(PathShape.ShapeType s, int colorId)
    {
        for (int i = 0; i < allowedPairs.Length; i++)
            if (allowedPairs[i].shape == s && allowedPairs[i].colorId == colorId)
                return true;
        return false;
    }

}
