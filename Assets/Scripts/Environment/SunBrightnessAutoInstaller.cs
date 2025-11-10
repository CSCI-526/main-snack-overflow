using UnityEngine;

/// <summary>
/// Makes sure a SunBrightnessController exists even if no scene or prefab provides one.
/// </summary>
static class SunBrightnessAutoInstaller
{
    const string AutoObjectName = "__SunBrightnessController";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (SunBrightnessController.Instance != null)
            return;

        var existing = GameObject.Find(AutoObjectName);
        var host = existing ? existing : new GameObject(AutoObjectName);
        Object.DontDestroyOnLoad(host);

        if (!host.TryGetComponent(out SunBrightnessController controller))
            controller = host.AddComponent<SunBrightnessController>();

        if (!controller.sunLight)
            controller.sunLight = SunBrightnessController.FindDirectionalSun();
    }
}
