using UnityEngine;
using UnityEngine.SceneManagement;

public static class HazardHintManager {
    const string LevelTwoSceneName = "LvL2";
    const string LevelThreeSceneName = "LvL3";
    const string LevelFourSceneName = "LvL4";

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
        if (!IsHazardHintLevel())
            return;
        const float cooldown = 4f;
        if (Time.unscaledTime < nextMudHintTime)
            return;
        nextMudHintTime = Time.unscaledTime + cooldown;
        KillTextController.Instance?.ShowHazard("Eww—mud bath!\nSpeed temporarily reduced!");
    }

    public static void TryShowPotholeHint(float duration) {
        EnsureInitialized();
        if (!IsHazardHintLevel())
            return;
        const float cooldown = 6f;
        if (Time.unscaledTime < nextPotholeHintTime)
            return;
        nextPotholeHintTime = Time.unscaledTime + cooldown;
        KillTextController.Instance?.ShowHazard("Ouch! Straight into a pothole!\nTimeout: 3 seconds!");
    }

    static bool IsHazardHintLevel() {
        string sceneName = SceneManager.GetActiveScene().name;
        return sceneName == LevelTwoSceneName
            || sceneName == LevelThreeSceneName
            || sceneName == LevelFourSceneName;
    }
}
