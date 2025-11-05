using UnityEngine;

public class VisionMaskController : MonoBehaviour
{
    public static VisionMaskController Instance;

    private CanvasGroup group;

    void Awake()
    {
        Instance = this;

        // Add or find a CanvasGroup so we can control visibility easily
        group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        HideMask(); // start invisible
    }

    public void ShowMask()
    {
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    public void HideMask()
    {
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }
}
