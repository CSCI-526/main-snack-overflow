using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerLightController : MonoBehaviour
{
    public static PlayerLightController Instance { get; private set; }

    const string LevelThreeSceneName = "LvL3";

    [Header("Light Settings")]
    public Light targetLight;
    public float minRange = 6f;
    public float maxRange = 16f;
    public float perCollectibleIncrease = 1.25f;
    public float passiveDecayPerSecond = 0.5f;
    public bool onlyActiveInLevelThree = true;

    float defaultIntensity;
    bool activeInScene;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (!targetLight)
            targetLight = GetComponentInChildren<Light>();
        if (targetLight)
        {
            defaultIntensity = targetLight.intensity;
            targetLight.range = Mathf.Clamp(targetLight.range, minRange, maxRange);
        }
        RefreshActivation(SceneManager.GetActiveScene());
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshActivation(scene);
    }

    void RefreshActivation(Scene scene)
    {
        activeInScene = !onlyActiveInLevelThree || (scene.IsValid() && scene.name == LevelThreeSceneName);
        if (!targetLight)
            return;

        if (!activeInScene)
        {
            targetLight.enabled = false;
        }
        else
        {
            targetLight.enabled = true;
            targetLight.range = Mathf.Clamp(targetLight.range, minRange, maxRange);
        }
    }

    void Update()
    {
        if (!activeInScene || !targetLight)
            return;

        if (passiveDecayPerSecond > 0f && targetLight.range > minRange)
        {
            float delta = passiveDecayPerSecond * Time.deltaTime;
            SetRange(targetLight.range - delta);
        }
    }

    public void AddLightEnergy(float amount)
    {
        if (!activeInScene || !targetLight || amount <= 0f)
            return;
        SetRange(targetLight.range + amount * perCollectibleIncrease);
    }

    void SetRange(float value)
    {
        targetLight.range = Mathf.Clamp(value, minRange, maxRange);
    }
}
