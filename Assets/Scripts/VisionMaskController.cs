using UnityEngine;
using UnityEngine.UI;  // Make sure to include this for RawImage

public class VisionMaskController : MonoBehaviour
{
    public static VisionMaskController Instance;

    private CanvasGroup group;
    private Material visionMaskMaterial;

    // Reference to RawImage (Vision Mask Image)
    public RawImage visionMaskImage;

    // Adjustable radius limits for penalties
    public float maxRadius = 1f;
    public float minRadius = 0f;

    [Header("Radius")]
    public float initialRadius = 0.26f;
    public float currentRadius = 0.26f;
    [Tooltip("When the player still has lives left, keep at least this radius to avoid total darkness.")]
    public float minRadiusWhileAlive = 0.08f;

    void Awake()
    {
        Instance = this;

        if (visionMaskImage == null)
        {
            visionMaskImage = GetComponent<RawImage>(); // Automatically find it if not set
        }

        visionMaskMaterial = visionMaskImage.material;

        group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        currentRadius = initialRadius;
        ResetRadius();
        HideMask(); // start invisible
    }

    public void ResetRadius()
    {
        UpdateRadius(initialRadius);
    }

    // Update the radius of the vision mask
    public void UpdateRadius(float newRadius)
    {
        float minClamp = minRadius;
        var lives = LivesManager.Instance;
        if (lives != null && lives.Current > 0)
            minClamp = Mathf.Max(minClamp, minRadiusWhileAlive);

        currentRadius = Mathf.Clamp(newRadius, minClamp, maxRadius);
        visionMaskMaterial.SetFloat("_Radius", currentRadius);
    }

    public RectTransform MaskRect => visionMaskImage ? visionMaskImage.rectTransform : null;

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
