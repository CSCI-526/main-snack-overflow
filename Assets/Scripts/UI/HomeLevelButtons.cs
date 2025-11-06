using UnityEngine;
using UnityEngine.UI;

public class HomeLevelButtons : MonoBehaviour {
    public Button level1Btn, level2Btn;
    public GameObject level2LockIcon;
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
        if (!level2Btn) return;

        bool level2Unlocked = ProgressManager.IsLevelUnlocked(2);
        level2Btn.interactable = level2Unlocked;
        if (level2LockIcon) level2LockIcon.SetActive(!level2Unlocked);
    }
}
