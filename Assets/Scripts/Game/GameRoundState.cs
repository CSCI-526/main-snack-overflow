using System;
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

    const string ImpostorColorKeyword = "red";
    int impostorColorIdCache = -2;
    int impostorColorOverride = -1;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        impostorColorIdCache = -2;
        impostorColorOverride = -1;
    }

    public bool MatchesAllowed(PathShape.ShapeType s, int colorId)
    {
        return IsImpostorColor(colorId);
    }

    public bool IsImpostorColor(int colorId)
    {
        int impostorColorId = GetImpostorColorId();
        return impostorColorId >= 0 && colorId == impostorColorId;
    }

    public int GetImpostorColorId()
    {
        if (impostorColorOverride >= 0)
            return impostorColorOverride;

        if (impostorColorIdCache != -2)
            return impostorColorIdCache;

        impostorColorIdCache = FindImpostorColorId();
        return impostorColorIdCache;
    }

    public void SetImpostorColorOverride(int colorId)
    {
        impostorColorOverride = colorId;
    }

    public void ClearImpostorColorOverride()
    {
        impostorColorOverride = -1;
        impostorColorIdCache = -2;
    }

    int FindImpostorColorId()
    {
        if (!palette || palette.Count == 0)
            return -1;

        for (int i = 0; i < palette.Count; i++)
        {
            string name = palette.GetName(i);
            if (!string.IsNullOrEmpty(name) &&
                name.IndexOf(ImpostorColorKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                return i;
        }

        return -1;
    }
}
