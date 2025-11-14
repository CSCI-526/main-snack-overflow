using UnityEngine;

public static class ProgressManager {
    const string L1_COMPLETE = "L1_COMPLETE";                    // legacy key for backwards compatibility
    const string HIGHEST_UNLOCKED_LEVEL = "HIGHEST_UNLOCKED_LVL"; // new key that tracks progression
    const int DefaultUnlockedLevel = 1;
    const int MaxLevel = 4;

    public static void SetLevel1Complete() => MarkLevelComplete(1);

    public static void MarkLevelComplete(int level) {
        if (level < 1) level = 1;

        int targetUnlockedLevel = Mathf.Clamp(level + 1, DefaultUnlockedLevel, MaxLevel);
        int currentHighest = GetHighestUnlockedLevel();
        bool updated = false;

        if (targetUnlockedLevel > currentHighest) {
            PlayerPrefs.SetInt(HIGHEST_UNLOCKED_LEVEL, targetUnlockedLevel);
            updated = true;
        }

        if (level == 1 && PlayerPrefs.GetInt(L1_COMPLETE, 0) != 1) {
            PlayerPrefs.SetInt(L1_COMPLETE, 1);
            updated = true;
        }

        if (updated) PlayerPrefs.Save();
    }

    public static bool IsLevel1Complete() => GetHighestUnlockedLevel() >= 2;

    public static bool IsLevelUnlocked(int level)
        => level <= Mathf.Clamp(GetHighestUnlockedLevel(), DefaultUnlockedLevel, MaxLevel);

    static int GetHighestUnlockedLevel() {
        int stored = PlayerPrefs.GetInt(HIGHEST_UNLOCKED_LEVEL, 0);
        if (stored >= DefaultUnlockedLevel)
            return Mathf.Clamp(stored, DefaultUnlockedLevel, MaxLevel);

        // Legacy fallback: players that already cleared Level 1 only had this key set.
        return PlayerPrefs.GetInt(L1_COMPLETE, 0) == 1 ? 2 : DefaultUnlockedLevel;
    }

    public static void ResetAllProgress() {
        PlayerPrefs.DeleteKey(L1_COMPLETE);
        PlayerPrefs.DeleteKey(HIGHEST_UNLOCKED_LEVEL);
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR
    static bool editorAutoResetApplied;
    public static void TryAutoResetInEditorSession() {
        if (editorAutoResetApplied) return;
        editorAutoResetApplied = true;
        ResetAllProgress();
    }
#endif
}
