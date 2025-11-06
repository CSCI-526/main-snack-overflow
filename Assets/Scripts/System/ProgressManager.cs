using UnityEngine;

public static class ProgressManager {
    const string L1_COMPLETE = "L1_COMPLETE";
    public static void SetLevel1Complete() => PlayerPrefs.SetInt(L1_COMPLETE, 1);
    public static bool IsLevel1Complete()   => PlayerPrefs.GetInt(L1_COMPLETE, 0) == 1;
    public static bool IsLevelUnlocked(int level)
        => level <= 1 || (level == 2 && IsLevel1Complete());

    public static void ResetAllProgress() {
        PlayerPrefs.DeleteKey(L1_COMPLETE);
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
