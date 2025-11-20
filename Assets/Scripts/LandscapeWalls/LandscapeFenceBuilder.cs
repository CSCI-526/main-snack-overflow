using UnityEngine;

[ExecuteAlways]
public class LandscapeFenceBuilder : MonoBehaviour
{
    [Tooltip("Parent transform that represents the playable ground area.")]
    public Transform sourceRoot;
    [Tooltip("Optional material override for all generated fence pieces.")]
    public Material fenceMaterial;

    [Header("Dimensions")]
    public float fenceHeight = 3f;
    public float postWidth = 0.4f;
    public float railThickness = 0.18f;
    public float postSpacing = 4f;
    public float horizontalInset = 0.25f;
    public float baseHeightOffset = 0f;

    [Header("Rails")]
    [Min(1)] public int railCount = 3;
    public Vector2 railHeightRange = new Vector2(0.4f, 2.2f);

    const string VisualRootName = "FenceVisuals";

    static Mesh cachedCubeMesh;
    static Material fallbackMaterial;

    void OnEnable() => Rebuild();
    void OnValidate() => Rebuild();

    public void Rebuild()
    {
        if (!isActiveAndEnabled || !sourceRoot)
            return;

        var renderers = sourceRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Transform visualRoot = EnsureVisualRoot();
        ClearChildren(visualRoot);

        float minX = bounds.min.x - horizontalInset;
        float maxX = bounds.max.x + horizontalInset;
        float minZ = bounds.min.z - horizontalInset;
        float maxZ = bounds.max.z + horizontalInset;
        float groundY = bounds.min.y + baseHeightOffset;

        BuildSide("North", new Vector3(minX, groundY, maxZ), new Vector3(maxX, groundY, maxZ), visualRoot);
        BuildSide("South", new Vector3(maxX, groundY, minZ), new Vector3(minX, groundY, minZ), visualRoot);
        BuildSide("East", new Vector3(maxX, groundY, maxZ), new Vector3(maxX, groundY, minZ), visualRoot);
        BuildSide("West", new Vector3(minX, groundY, minZ), new Vector3(minX, groundY, maxZ), visualRoot);
    }

    Transform EnsureVisualRoot()
    {
        var visualRoot = transform.Find(VisualRootName);
        if (!visualRoot)
        {
            var go = new GameObject(VisualRootName);
            go.transform.SetParent(transform, false);
            visualRoot = go.transform;
        }
        return visualRoot;
    }

    void ClearChildren(Transform visualRoot)
    {
        for (int i = visualRoot.childCount - 1; i >= 0; i--)
        {
            var child = visualRoot.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }

    void BuildSide(string label, Vector3 start, Vector3 end, Transform root)
    {
        Vector3 delta = end - start;
        float length = delta.magnitude;
        if (length < 0.01f)
            return;

        Vector3 direction = delta / length;
        int segments = Mathf.Max(1, Mathf.CeilToInt(length / Mathf.Max(0.1f, postSpacing)));
        float step = length / segments;

        for (int i = 0; i <= segments; i++)
        {
            Vector3 position = start + direction * (i * step);
            CreatePost(root, $"{label}_Post_{i}", position + Vector3.up * (fenceHeight * 0.5f));
        }

        for (int i = 0; i < railCount; i++)
        {
            float t = railCount == 1 ? 0f : (float)i / (railCount - 1);
            float railHeight = Mathf.Lerp(railHeightRange.x, railHeightRange.y, t);
            Vector3 railStart = start + Vector3.up * railHeight;
            Vector3 railEnd = end + Vector3.up * railHeight;
            CreateRail(root, $"{label}_Rail_{i}", railStart, railEnd);
        }
    }

    void CreatePost(Transform parent, string name, Vector3 center)
    {
        var post = CreatePrimitive(name, parent);
        post.position = center;
        post.localScale = new Vector3(postWidth, fenceHeight, postWidth);
    }

    void CreateRail(Transform parent, string name, Vector3 start, Vector3 end)
    {
        Vector3 delta = end - start;
        float length = delta.magnitude;
        if (length < 0.01f)
            return;

        var rail = CreatePrimitive(name, parent);
        rail.position = start + delta * 0.5f;
        rail.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        rail.localScale = new Vector3(railThickness, railThickness, length + postWidth * 0.25f);
    }

    Transform CreatePrimitive(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = GetCubeMesh();

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = fenceMaterial ? fenceMaterial : GetFallbackMaterial();

        return go.transform;
    }

    static Mesh GetCubeMesh()
    {
        if (cachedCubeMesh)
            return cachedCubeMesh;

        var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cachedCubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            DestroyImmediate(temp);
        else
            Destroy(temp);
#else
        Object.Destroy(temp);
#endif
        return cachedCubeMesh;
    }

    static Material GetFallbackMaterial()
    {
        if (fallbackMaterial)
            return fallbackMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader)
            shader = Shader.Find("Standard");

        fallbackMaterial = new Material(shader)
        {
            color = new Color(0.58f, 0.38f, 0.21f)
        };
        fallbackMaterial.hideFlags = HideFlags.HideAndDontSave;
        return fallbackMaterial;
    }
}
