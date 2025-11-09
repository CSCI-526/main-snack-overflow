using UnityEngine;
using UnityEngine.UI;  // Make sure to include this for RawImage
using UnityEngine.SceneManagement;

public class VisionMaskController : MonoBehaviour
{
    public static VisionMaskController Instance;

    const string LevelThreeSceneName = "LvL3";

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

    [Header("Passive Shrink")]
    [Tooltip("Radius will slowly decay toward this value after it has been increased.")]
    public float passiveShrinkFloor = 0.26f;
    [Tooltip("Units per second the radius shrinks when above the floor.")]
    public float passiveShrinkPerSecond = 0.008f;

    bool maskSuppressed;

    void Awake()
    {
        Instance = this;

        if (visionMaskImage == null)
        {
            visionMaskImage = GetComponent<RawImage>(); // Automatically find it if not set
        }

        visionMaskMaterial = visionMaskImage ? visionMaskImage.material : null;

        group = GetComponent<CanvasGroup>();
        if (group == null)
            group = gameObject.AddComponent<CanvasGroup>();

        maskSuppressed = IsLevelThreeScene();

        currentRadius = initialRadius;
        ResetRadius();

        if (maskSuppressed)
        {
            if (visionMaskImage) visionMaskImage.enabled = false;
            HideMask();
        }
        else
        {
            if (visionMaskImage) visionMaskImage.enabled = true;
            HideMask(); // start invisible
        }
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
        if (maskSuppressed || visionMaskMaterial == null)
            return;
        visionMaskMaterial.SetFloat("_Radius", currentRadius);
    }

    public RectTransform MaskRect => visionMaskImage ? visionMaskImage.rectTransform : null;

    public void ShowMask()
    {
        if (maskSuppressed) return;
        group.alpha = 1f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    public void HideMask()
    {
        if (maskSuppressed)
        {
            group.alpha = 0f;
            return;
        }
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    void Update()
    {
        if (maskSuppressed)
            return;

        if (passiveShrinkPerSecond <= 0f)
            return;

        if (currentRadius <= passiveShrinkFloor)
            return;

        float delta = passiveShrinkPerSecond * Time.deltaTime;
        float next = Mathf.Max(passiveShrinkFloor, currentRadius - delta);
        if (!Mathf.Approximately(next, currentRadius))
            UpdateRadius(next);
    }



    bool IsLevelThreeScene()
    {
        var scene = SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == LevelThreeSceneName;
    }
}
