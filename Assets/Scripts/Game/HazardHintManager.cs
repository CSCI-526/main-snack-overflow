using UnityEngine;
using UnityEngine.SceneManagement;

public static class HazardHintManager {
    static bool initialized;
    static float nextMudHintTime;
    static float nextPotholeHintTime;

    static void EnsureInitialized() {
        if (initialized) return;
        initialized = true;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        nextMudHintTime = 0f;
        nextPotholeHintTime = 0f;
    }

    public static void TryShowMudHint() {
        EnsureInitialized();
        if (!IsLevelTwo())
            return;
        const float cooldown = 4f;
        if (Time.unscaledTime < nextMudHintTime)
            return;
        nextMudHintTime = Time.unscaledTime + cooldown;
        KillTextController.Instance?.ShowHazard("Eww—mud bath!\nSpeed temporarily reduced!");
    }

    public static void TryShowPotholeHint(float duration) {
        EnsureInitialized();
        if (!IsLevelTwo())
            return;
        const float cooldown = 6f;
        if (Time.unscaledTime < nextPotholeHintTime)
            return;
        nextPotholeHintTime = Time.unscaledTime + cooldown;
        string seconds = duration % 1f == 0f
            ? Mathf.RoundToInt(duration).ToString()
            : duration.ToString("0.0");
        KillTextController.Instance?.ShowHazard($"Ouch! Straight into a pothole!\nRecovering in {seconds}s!");
    }

    static bool IsLevelTwo() => SceneManager.GetActiveScene().name == "LvL2";
}
