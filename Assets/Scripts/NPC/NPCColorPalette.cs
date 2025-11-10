using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(menuName = "Game/NPC Color Palette")]
public class NPCColorPalette : ScriptableObject
{
    [System.Serializable]
    public struct Entry { public string name; public Color color; }
    [Tooltip("Index = colorId. Keep these distinct and readable.")]
    public Entry[] entries;

    public int Count => entries != null ? entries.Length : 0;
    public Color Get(int id) =>
        (entries != null && id >= 0 && id < entries.Length) ? entries[id].color : Color.white;

    public Color GetForRole(int id, bool isImpostor)
    {
        Color baseColor = Get(id);
        if (!ShouldUseLevelOneRedFilter())
            return baseColor;

        return isImpostor && IsLevelOneImpostorColor(id)
            ? LevelOneImpostorColor
            : GetLevelOneShade(id);
    }
    public string GetName(int id) => (entries != null && id >= 0 && id < entries.Length) ? entries[id].name : "?";

    const string LevelOneSceneName = "LvL1";
    static readonly Color[] LevelOneRedShades =
    {
        new Color32(150, 0, 24, 255),
        // new Color32(255, 127, 80, 255),
        // new Color32(205, 92, 92, 255),
        new Color32(255, 99, 71, 255),
        new Color32(139, 0, 0, 255),
        new Color32(128, 0, 32, 255),
        new Color32(127, 23, 52, 255),
        // new Color32(0, 111, 92, 255),
    };
    static readonly Color LevelOneImpostorColor = new Color32(255, 0, 0, 255);

    static bool ShouldUseLevelOneRedFilter()
    {
        if (!Application.isPlaying)
            return false;

        var activeScene = SceneManager.GetActiveScene();
        return activeScene.IsValid() && activeScene.name == LevelOneSceneName;
    }

    static Color GetLevelOneShade(int colorId)
    {
        if (LevelOneRedShades == null || LevelOneRedShades.Length == 0)
            return new Color32(150, 12, 28, 255);

        int idx = Mathf.Abs(colorId);
        idx %= LevelOneRedShades.Length;
        return LevelOneRedShades[idx];
    }

    bool IsLevelOneImpostorColor(int colorId)
    {
        var roundState = GameRoundState.Instance;
        if (roundState != null)
        {
            int impostorId = roundState.GetImpostorColorId();
            if (impostorId >= 0)
                return impostorId == colorId;
        }

        if (entries == null || colorId < 0 || colorId >= entries.Length)
            return false;

        string name = entries[colorId].name;
        return !string.IsNullOrEmpty(name) &&
               name.IndexOf("red", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
