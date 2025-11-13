using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class SceneLoader : MonoBehaviour {
    const string LevelThreeQuality = "LvL3";
    const string DefaultQuality = "Ultra";
    public void Load(string sceneName) => SceneManager.LoadScene(sceneName);
    public void Quit() => Application.Quit();

    void SetQualitySafely(string qualityName) {
        Debug.Log($"[SceneLoader] Requesting quality '{qualityName}'");
        if (string.IsNullOrEmpty(qualityName)) return;
        var names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++) {
            if (names[i] == qualityName) {
                if (QualitySettings.GetQualityLevel() != i) {
                    QualitySettings.SetQualityLevel(i, true);
                    Debug.Log($"Quality switched to {qualityName} (index {i}).");
                } else {
                    Debug.Log($"Quality already set to {qualityName}.");
                }
                var pipeline = GraphicsSettings.currentRenderPipeline != null ? GraphicsSettings.currentRenderPipeline.name : "null";
                Debug.Log($"Active pipeline: {pipeline}");
                return;
            }
        }
        Debug.LogWarning($"Quality level '{qualityName}' not found. Available: {string.Join(", ", names)}");
    }

    void RestoreDefaultQuality() => SetQualitySafely(DefaultQuality);

    // NEW: convenience methods for Home buttons
    public void LoadLevel1Tutorial() {
        RestoreDefaultQuality();
        GameMode.SetTutorial(true);
        SceneManager.LoadScene("LvL1");
    }
    public void LoadLevel1() {
        RestoreDefaultQuality();
        GameMode.SetTutorial(false);
        SceneManager.LoadScene("LvL1");
    }
    public void LoadLevel2()
    {
        RestoreDefaultQuality();
        if (!ProgressManager.IsLevelUnlocked(2))
        {
            Debug.LogWarning("Tried to load Level 2 before Level 1 was complete.");
            return;
        }
        SceneManager.LoadScene("LvL2");
    }
    public void LoadLevel3() {
        SetQualitySafely(LevelThreeQuality);
        SceneManager.LoadScene("LvL3");
    }

    public void LoadLevel4() {
        RestoreDefaultQuality();
        SceneManager.LoadScene("LvL4");
    }

    public void LoadHome() {
        RestoreDefaultQuality();
        GameMode.SetTutorial(false);
        SceneManager.LoadScene("Home", LoadSceneMode.Single);
    }
}
