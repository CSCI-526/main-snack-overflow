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
    public void LoadLevel2() {
        // You can add your unlock check here later if you want
        SceneManager.LoadScene("LvL2");
    }
}
