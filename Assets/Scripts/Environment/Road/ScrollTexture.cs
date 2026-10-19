using UnityEngine;

public class ScrollTexture : MonoBehaviour
{
    [SerializeField] Renderer targetRenderer;
    [SerializeField] Vector2 scrollSpeed = new Vector2(0f, -1f);

    // Use "_BaseMap" for URP/HDRP Lit, "_MainTex" for Built-in
    [SerializeField] string textureProperty = "_BaseMap";

    [SerializeField] Vector2 tiling = new Vector2(1f, 20f);

    Material mat;
    Vector2 offset;

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        mat = targetRenderer.material;
        mat.SetTextureScale(textureProperty, tiling);
    }

    void Update()
    {
        offset += scrollSpeed * Time.deltaTime;
        offset.x %= 1f; // keep values small to avoid precision loss over time
        offset.y %= 1f;
        mat.SetTextureOffset(textureProperty, offset);
    }
}