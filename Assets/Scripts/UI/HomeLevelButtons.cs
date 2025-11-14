using UnityEngine;
using UnityEngine.UI;

public class HomeLevelButtons : MonoBehaviour {
    public Button level1Btn, level2Btn, level3Btn, level4Btn;
    public GameObject level2LockIcon, level3LockIcon, level4LockIcon;
#if UNITY_EDITOR
    [SerializeField] bool resetProgressEachPlayInEditor = true;
#endif

    void Awake() {
#if UNITY_EDITOR
        if (Application.isPlaying && resetProgressEachPlayInEditor)
            ProgressManager.TryAutoResetInEditorSession();
#endif
    }

    void OnEnable() => RefreshLevelLocks();
    void Start() => RefreshLevelLocks();

    void RefreshLevelLocks() {
        UpdateLevelLock(level2Btn, level2LockIcon, 2);
        UpdateLevelLock(level3Btn, level3LockIcon, 3);
        UpdateLevelLock(level4Btn, level4LockIcon, 4);
    }

    void UpdateLevelLock(Button button, GameObject lockIcon, int level) {
        if (!button) return;

        bool unlocked = ProgressManager.IsLevelUnlocked(level);
        button.interactable = unlocked;
        if (lockIcon) lockIcon.SetActive(!unlocked);
    }
}
