using UnityEngine;
using UnityEngine.SceneManagement;

public static class TitleBootstrapper
{
    const string TitleSceneName = "Landing";
    static bool titleSceneEnsured;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void EnsureTitleSceneIsFirst()
    {
        if (titleSceneEnsured)
            return;

        titleSceneEnsured = true;

        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.name == TitleSceneName)
            return;

        SceneManager.LoadScene(TitleSceneName, LoadSceneMode.Single);
    }
}
