using UnityEngine;
using UnityEngine.UI;

public class HomeLevelButtons : MonoBehaviour {
    public Button level1Btn, level2Btn;
    public GameObject level2LockIcon;

    void Start() {
        bool l2 = ProgressManager.IsLevelUnlocked(2);
        level2Btn.interactable = l2;
        if (level2LockIcon) level2LockIcon.SetActive(!l2);
    }
}
