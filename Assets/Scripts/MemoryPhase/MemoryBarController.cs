// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;

// [DisallowMultipleComponent]
// public class MemoryBarController : MonoBehaviour
// {
//     [Header("Clue generation")]
// public NPCColorPalette palette;          // drag your NPCColorPalette asset
// public PathShape[] sourceShapes;         // drag the same PathShapes you use for impostors
// public bool randomizeOnEnable = true;    // pick new clues each time

// // We’ll use the same 2 Image slots you already have (shapeSlots[0..1])
// private GameRoundState.CluePair[] chosen = new GameRoundState.CluePair[2];


//     [Header("Color Clues")]
// public NPCColorPalette palette;
// public int clueColorIdA = 0;
// public int clueColorIdB = 1;
// public PathShape.ShapeType clueShapeA = PathShape.ShapeType.Triangle;
// public PathShape.ShapeType clueShapeB = PathShape.ShapeType.Square;
// public Image[] shapeTintSlots; // the same 2 UI Image slots you already have for shapes

//     [Header("Timing")]
//     public float showDurationSeconds = 5f;   // memory phase length (seconds)

//     [Header("Shape Slots (drag the 4 Image objects here)")]
//     public Image[] shapeSlots;

//     [Header("Sprites (assign in Inspector)")]
//     public Sprite squareSprite;
//     public Sprite circleSprite;
//     public Sprite triangleSprite;
//     public Sprite pentagonSprite;

//     [Header("UI")]
//     public TMP_Text timerText;
//     public GameObject rootBar;            // MemoryBar object (self if left empty)
//     public RectTransform layoutRoot;      // ShapesRow (for layout rebuild)
//     RectTransform barRect;                // cached
//     CanvasGroup canvasGroup;              // for fade

//     [Header("Blur / Pause (optional URP volume)")]
//     public GameObject blurVolume;         // your Global Volume (can be null)
//     public bool pauseGameDuringMemory = true;

//     [Header("Intro Animation (unscaled time)")]
//     public float introSlideY = 180f;      // starts this many px above, slides to current position
//     public float introFadeDuration = 0.35f;

//     [Header("Exit Animation (unscaled time)")]
//     public float slideDistanceY = 300f;   // moves down by this many pixels
//     public float animDuration = 0.6f;     // seconds (unscaled)
//     [Range(0f,1f)] public float fadeTo = 0f;

//     [Header("2D Blur (RenderTexture method)")]
//     public RawImage worldBlurRawImage;    // WorldBlur RawImage
//     public Camera mainCamera;             // Main Camera
//     public RenderTexture worldRT;         // WorldRT

//     [Header("Which shapes to show")]
//     public List<PathShape.ShapeType> shapesToShow = new()
//     {
//         PathShape.ShapeType.Square,
//         PathShape.ShapeType.Circle,
//         PathShape.ShapeType.Triangle,
//         PathShape.ShapeType.Pentagon
//     };

//     public event Action<List<PathShape.ShapeType>> OnMemoryPhaseComplete;

//     void Awake()
//     {
//         if (!rootBar) rootBar = this.gameObject;
//         barRect = rootBar.GetComponent<RectTransform>();
//         canvasGroup = rootBar.GetComponent<CanvasGroup>();
//         if (!canvasGroup) canvasGroup = rootBar.AddComponent<CanvasGroup>();

//         // Hidden by default; shown when BeginMemoryPhase() is called
//         canvasGroup.alpha = 0f;
//         rootBar.SetActive(false);
//     }

//     // ============ PUBLIC ENTRY ============
//     public void BeginMemoryPhase()
//     {
//         ApplyShapesToSlots();

//         // Blur + pause
//         if (blurVolume) blurVolume.SetActive(true);
//         if (worldBlurRawImage) worldBlurRawImage.gameObject.SetActive(true);
//         if (mainCamera && worldRT) mainCamera.targetTexture = worldRT;
//         if (pauseGameDuringMemory) Time.timeScale = 0f;

//         if (randomizeOnEnable)
// {
//     GenerateNewClues();
//     ApplyCluesToUI();
//     PublishClues();
// }


//         // Prepare intro pose
//         rootBar.SetActive(true);
//         canvasGroup.alpha = 0f;
//         var targetPos = barRect.anchoredPosition;
//         barRect.anchoredPosition = targetPos + new Vector2(0f, Mathf.Abs(introSlideY));

//         StopAllCoroutines();
//         StartCoroutine(IntroThenCountdownThenOutro(targetPos));
//     }

//     IEnumerator IntroThenCountdownThenOutro(Vector2 targetPos)
//     {
//         // INTRO (fade-in from top)
//         float t = 0f;
//         Vector2 from = barRect.anchoredPosition;
//         Vector2 to = targetPos;
//         while (t < introFadeDuration)
//         {
//             float u = t / introFadeDuration; u = u * u * (3f - 2f * u); // smoothstep
//             barRect.anchoredPosition = Vector2.Lerp(from, to, u);
//             canvasGroup.alpha = Mathf.Lerp(0f, 1f, u);
//             t += Time.unscaledDeltaTime;
//             yield return null;
//         }
//         barRect.anchoredPosition = to;
//         canvasGroup.alpha = 1f;

//         // COUNTDOWN (unscaled time while game is paused)
//         float remain = showDurationSeconds;
//         while (remain > 0f)
//         {
//             if (timerText) timerText.text = Mathf.CeilToInt(remain).ToString();
//             remain -= Time.unscaledDeltaTime;
//             yield return null;
//         }
//         if (timerText) timerText.text = "0";

//         // OUTRO (slide down + fade out)
//         Vector2 start = barRect.anchoredPosition;
//         Vector2 end = start + new Vector2(0f, -Mathf.Abs(slideDistanceY));
//         float startAlpha = canvasGroup.alpha;
//         float elapsed = 0f;

//         while (elapsed < animDuration)
//         {
//             float u = elapsed / animDuration; u = u * u * (3f - 2f * u);
//             barRect.anchoredPosition = Vector2.Lerp(start, end, u);
//             canvasGroup.alpha = Mathf.Lerp(startAlpha, fadeTo, u);
//             elapsed += Time.unscaledDeltaTime;
//             yield return null;
//         }
//         barRect.anchoredPosition = end;
//         canvasGroup.alpha = fadeTo;
//         rootBar.SetActive(false);

//         // Unblur + unpause
//         if (blurVolume) blurVolume.SetActive(false);
//         if (worldBlurRawImage) worldBlurRawImage.gameObject.SetActive(false);
//         if (mainCamera) mainCamera.targetTexture = null;
//         if (pauseGameDuringMemory) Time.timeScale = 1f;

//         if (GameRoundState.Instance)
// {
//     GameRoundState.Instance.allowedPairs = new GameRoundState.CluePair[] {
//         new GameRoundState.CluePair { shape = clueShapeA, colorId = clueColorIdA },
//         new GameRoundState.CluePair { shape = clueShapeB, colorId = clueColorIdB },
//     };
// }

//         // Notify gameplay can begin
//         OnMemoryPhaseComplete?.Invoke(new List<PathShape.ShapeType>(shapesToShow));
//     }

//     void ApplyShapesToSlots()
//     {
//         int n = Mathf.Min(shapeSlots.Length, shapesToShow.Count);
//         for (int i = 0; i < n; i++)
//         {
//             var img = shapeSlots[i];
//             if (!img) continue;
//             img.preserveAspect = true;
//             img.sprite = GetSpriteFor(shapesToShow[i]);
//             img.enabled = true;
//             img.color = Color.white;
//         }
//         for (int i = n; i < shapeSlots.Length; i++)
//             if (shapeSlots[i]) shapeSlots[i].enabled = true;

//         if (layoutRoot) LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
//     }
//     void GenerateNewClues()
// {
//     // 1) Build the shape pool
//     var pool = new List<PathShape.ShapeType>();
//     if (sourceShapes != null && sourceShapes.Length > 0)
//     {
//         foreach (var p in sourceShapes)
//             if (p) pool.Add(p.shape);
//     }
//     if (pool.Count == 0) // fallback to all shapes
//         pool.AddRange(new [] {
//             PathShape.ShapeType.Triangle,
//             PathShape.ShapeType.Square,
//             PathShape.ShapeType.Pentagon,
//             PathShape.ShapeType.Circle
//         });

//     // 2) Pick two DISTINCT shapes
//     var sA = pool[UnityEngine.Random.Range(0, pool.Count)];
//     pool.Remove(sA);
//     var sB = pool[UnityEngine.Random.Range(0, pool.Count)];

//     // 3) Pick two DISTINCT colors from palette
//     int cA = 0, cB = 0;
//     int n = palette ? palette.Count : 0;
//     if (n <= 0) { cA = 0; cB = 0; }                   // safe fallback
//     else if (n == 1) { cA = 0; cB = 0; }              // only one color in palette
//     else {
//         cA = UnityEngine.Random.Range(0, n);
//         cB = UnityEngine.Random.Range(0, n - 1);
//         if (cB >= cA) cB++;                           // ensure distinct
//     }

//     chosen[0] = new GameRoundState.CluePair { shape = sA, colorId = cA };
//     chosen[1] = new GameRoundState.CluePair { shape = sB, colorId = cB };
// }

// void ApplyCluesToUI()
// {
//     // IMPORTANT: your shape sprites should be WHITE so tinting shows palette color correctly.
//     // If your current sprites are blue, replace them with white versions.

//     // Slot 0
//     if (shapeSlots.Length > 0 && shapeSlots[0])
//     {
//         shapeSlots[0].sprite = GetSpriteFor(chosen[0].shape);
//         shapeSlots[0].color  = palette ? palette.Get(chosen[0].colorId) : Color.white;
//         shapeSlots[0].enabled = true;
//         shapeSlots[0].preserveAspect = true;
//     }

//     // Slot 1
//     if (shapeSlots.Length > 1 && shapeSlots[1])
//     {
//         shapeSlots[1].sprite = GetSpriteFor(chosen[1].shape);
//         shapeSlots[1].color  = palette ? palette.Get(chosen[1].colorId) : Color.white;
//         shapeSlots[1].enabled = true;
//         shapeSlots[1].preserveAspect = true;
//     }

//     if (layoutRoot)
//         LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
// }

// void PublishClues()
// {
//     if (GameRoundState.Instance)
//         GameRoundState.Instance.allowedPairs = new [] { chosen[0], chosen[1] };
// }


//     Sprite GetSpriteFor(PathShape.ShapeType shape)
//     {
//         switch (shape)
//         {
//             case PathShape.ShapeType.Square:   return squareSprite;
//             case PathShape.ShapeType.Circle:   return circleSprite;
//             case PathShape.ShapeType.Triangle: return triangleSprite;
//             case PathShape.ShapeType.Pentagon: return pentagonSprite;
//             default: return null;
//         }
//     }
// }


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public class MemoryBarController : MonoBehaviour
{
    
    [Header("Clue generation")]
    [Tooltip("Your ScriptableObject palette (indexes are color IDs).")]
    public NPCColorPalette palette;

    [Tooltip("PathShape objects to pick shapes from (leave empty to allow all 4).")]
    public PathShape[] sourceShapes;

    [Tooltip("Pick new random clues each time the memory phase begins.")]
    public bool randomizeOnEnable = true;

    
    private GameRoundState.CluePair[] chosen = new GameRoundState.CluePair[2];

    
    [Header("Timing")]
    [Tooltip("How long the clues are shown (seconds).")]
    public float showDurationSeconds = 5f;

    [Header("Shape Slots (exactly 2 Image objects)")]
    public Image[] shapeSlots;   

    [Header("Shape Sprites (white sprites recommended so tinting works)")]
    public Sprite squareSprite;
    public Sprite circleSprite;
    public Sprite triangleSprite;
    public Sprite pentagonSprite;

    [Header("UI")]
    public TMP_Text timerText;
    public GameObject rootBar;           
    public RectTransform layoutRoot;     

    RectTransform barRect;               
    CanvasGroup canvasGroup;            

    [Header("Blur / Pause (optional)")]
    [Tooltip("Optional: your URP Global Volume to enable while showing clues.")]
    public GameObject blurVolume;
    [Tooltip("Pause gameplay while the memory bar is visible.")]
    public bool pauseGameDuringMemory = true;

    [Header("Intro Animation (unscaled time)")]
    [Tooltip("Starts this many pixels above, slides to current position.")]
    public float introSlideY = 180f;
    public float introFadeDuration = 0.35f;

    [Header("Exit Animation (unscaled time)")]
    [Tooltip("Slides this many pixels downward when leaving.")]
    public float slideDistanceY = 300f;
    public float animDuration = 0.6f;
    [Range(0f, 1f)] public float fadeTo = 0f;

    [Header("2D Blur (RenderTexture method)")]
    [Tooltip("RawImage that shows the blurred world (toggle on during memory).")]
    public RawImage worldBlurRawImage;
    [Tooltip("Main Camera that renders to the target texture while showing clues.")]
    public Camera mainCamera;
    [Tooltip("RenderTexture the camera writes to (used by the RawImage).")]
    public RenderTexture worldRT;

    public event Action OnMemoryPhaseComplete; 


    void Awake()
    {
        if (!rootBar) rootBar = this.gameObject;
        barRect = rootBar.GetComponent<RectTransform>();
        canvasGroup = rootBar.GetComponent<CanvasGroup>();
        if (!canvasGroup) canvasGroup = rootBar.AddComponent<CanvasGroup>();

        
        canvasGroup.alpha = 0f;
        rootBar.SetActive(false);

        // Ensure the first frame renders the actual scene (WebGL would otherwise show black)
        // while keeping whatever blur GameObjects were configured in the scene.
        if (mainCamera) mainCamera.targetTexture = null;
    }

    
    public void BeginMemoryPhase()
    {
        
        if (randomizeOnEnable)
        {
            GenerateNewClues();
            ApplyCluesToUI();
            PublishClues();
        }

        
        if (blurVolume) blurVolume.SetActive(true);
        if (worldBlurRawImage) worldBlurRawImage.gameObject.SetActive(true);
        if (mainCamera && worldRT) mainCamera.targetTexture = worldRT;
        if (pauseGameDuringMemory) Time.timeScale = 0f;

        
        rootBar.SetActive(true);
        canvasGroup.alpha = 0f;
        var targetPos = barRect.anchoredPosition;
        barRect.anchoredPosition = targetPos + new Vector2(0f, Mathf.Abs(introSlideY));

        StopAllCoroutines();
        StartCoroutine(IntroThenCountdownThenOutro(targetPos));
    }

    IEnumerator IntroThenCountdownThenOutro(Vector2 targetPos)
    {
       
        float t = 0f;
        Vector2 from = barRect.anchoredPosition;
        Vector2 to = targetPos;

        while (t < introFadeDuration)
        {
            float u = t / introFadeDuration;
            u = u * u * (3f - 2f * u); 
            barRect.anchoredPosition = Vector2.Lerp(from, to, u);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, u);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        barRect.anchoredPosition = to;
        canvasGroup.alpha = 1f;

        float remain = showDurationSeconds;
        while (remain > 0f)
        {
            if (timerText) timerText.text = Mathf.CeilToInt(remain).ToString();
            remain -= Time.unscaledDeltaTime;
            yield return null;
        }
        if (timerText) timerText.text = "0";

        Vector2 start = barRect.anchoredPosition;
        Vector2 end = start + new Vector2(0f, -Mathf.Abs(slideDistanceY));
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            float u = elapsed / animDuration; u = u * u * (3f - 2f * u);
            barRect.anchoredPosition = Vector2.Lerp(start, end, u);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, fadeTo, u);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        barRect.anchoredPosition = end;
        canvasGroup.alpha = fadeTo;
        rootBar.SetActive(false);

if (blurVolume) blurVolume.SetActive(false);
if (worldBlurRawImage) worldBlurRawImage.gameObject.SetActive(false);
if (mainCamera) mainCamera.targetTexture = null;
if (pauseGameDuringMemory) Time.timeScale = 1f;

var timer = FindObjectOfType<TimerController>(true);
if (timer != null) timer.StartTimer(90f);

OnMemoryPhaseComplete?.Invoke();

    }


    void GenerateNewClues()
    {
        
        var pool = new List<PathShape.ShapeType>();
        if (sourceShapes != null && sourceShapes.Length > 0)
        {
            foreach (var p in sourceShapes)
                if (p) pool.Add(p.shape);
        }
        if (pool.Count == 0)
        {
            pool.AddRange(new[]
            {
                PathShape.ShapeType.Triangle,
                PathShape.ShapeType.Square,
                PathShape.ShapeType.Pentagon,
                PathShape.ShapeType.Circle
            });
        }

        
        var sA = pool[UnityEngine.Random.Range(0, pool.Count)];
        pool.Remove(sA);
        var sB = pool[UnityEngine.Random.Range(0, pool.Count)];

        
        int cA = 0, cB = 0;
        int n = palette ? palette.Count : 0;
        if (n <= 1)
        {
            cA = 0; cB = 0; 
        }
        else
        {
            cA = UnityEngine.Random.Range(0, n);
            cB = UnityEngine.Random.Range(0, n - 1);
            if (cB >= cA) cB++; 
        }

        chosen[0] = new GameRoundState.CluePair { shape = sA, colorId = cA };
        chosen[1] = new GameRoundState.CluePair { shape = sB, colorId = cB };
    }
    void EnsureSlotSize(Image img, float w = 220f, float h = 220f)
{
    if (!img) return;
    var rt = img.rectTransform;
    if (rt.rect.width < 10f || rt.rect.height < 10f)
        rt.sizeDelta = new Vector2(w, h);

    
    img.type = Image.Type.Simple;
    img.preserveAspect = true;
    var c = img.color; if (c.a < 0.99f) { c.a = 1f; img.color = c; }
}

    void ApplyCluesToUI()
    {
        
        
        if (shapeSlots == null || shapeSlots.Length < 2) return;

        
        if (shapeSlots[0])
        {
            shapeSlots[0].sprite = GetSpriteFor(chosen[0].shape);
            shapeSlots[0].color  = palette ? palette.Get(chosen[0].colorId) : Color.white;
            shapeSlots[0].preserveAspect = true;
            shapeSlots[0].enabled = true;
        }

        
        if (shapeSlots[1])
        {
            shapeSlots[1].sprite = GetSpriteFor(chosen[1].shape);
            shapeSlots[1].color  = palette ? palette.Get(chosen[1].colorId) : Color.white;
            shapeSlots[1].preserveAspect = true;
            shapeSlots[1].enabled = true;
        }

        if (layoutRoot)
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);

        EnsureSlotSize(shapeSlots[0]);
        EnsureSlotSize(shapeSlots[1]);

    }

    void PublishClues()
    {
        if (GameRoundState.Instance)
            GameRoundState.Instance.allowedPairs = new[] { chosen[0], chosen[1] };
    }

   

    Sprite GetSpriteFor(PathShape.ShapeType shape)
    {
        switch (shape)
        {
            case PathShape.ShapeType.Square:   return squareSprite;
            case PathShape.ShapeType.Circle:   return circleSprite;
            case PathShape.ShapeType.Triangle: return triangleSprite;
            case PathShape.ShapeType.Pentagon: return pentagonSprite;
            default: return null;
        }
    }
}
