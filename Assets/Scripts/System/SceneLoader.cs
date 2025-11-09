using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour {
    public void Load(string sceneName) => SceneManager.LoadScene(sceneName);
    public void Quit() => Application.Quit();

    // NEW: convenience methods for Home buttons
    public void LoadLevel1Tutorial() {
        GameMode.SetTutorial(true);
        SceneManager.LoadScene("LvL1");
    }
    public void LoadLevel1() {
        GameMode.SetTutorial(false);
        SceneManager.LoadScene("LvL1");
    }
    public void LoadLevel2()
    {
        if (!ProgressManager.IsLevelUnlocked(2))
        {
            Debug.LogWarning("Tried to load Level 2 before Level 1 was complete.");
            return;
        }
        SceneManager.LoadScene("LvL2");
    }
    public void LoadLevel3() {
        SceneManager.LoadScene("LvL3");
    }

    public void LoadHome() {
        GameMode.SetTutorial(false);
        SceneManager.LoadScene("Home", LoadSceneMode.Single);
    }
}
