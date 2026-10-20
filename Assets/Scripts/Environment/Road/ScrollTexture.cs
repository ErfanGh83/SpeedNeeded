using UnityEngine;

public class ScrollTexture : MonoBehaviour
{
    [SerializeField] Renderer targetRenderer;

    // "_BaseMap" for URP/HDRP Lit, "_MainTex" for Built-in
    [SerializeField] string textureProperty = "_BaseMap";

    [Tooltip("Length of the road mesh in world units (e.g. 200).")]
    [SerializeField] float roadLength = 200f;

    [Tooltip("The material's Tiling Y.")]
    [SerializeField] float tilingY = 20f;

    [Tooltip("Flip to 1 if the road moves the wrong way.")]
    [SerializeField] float direction = -1f;

    [Header("Speed")]
    [Tooltip("Constant scroll speed (world units/s) added on top of the game speed.")]
    [SerializeField] float baseScrollSpeed = 5f;

    [Tooltip("Multiplies the game-speed part. Raise it above 1 if the road looks slower than the traffic.")]
    [SerializeField] float speedMultiplier = 1f;

    Material mat;
    float offsetY;

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponent<Renderer>();
        mat = targetRenderer.material;
        mat.SetTextureScale(textureProperty, new Vector2(1f, tilingY));
    }

    void Update()
    {
        if (!GameManager.IsPlaying) return;

        // World units per second the road surface moves
        float scrollSpeed = baseScrollSpeed + WorldSpeed.Value * speedMultiplier;

        // One texture repeat = roadLength / tilingY world units
        float offsetPerUnit = tilingY / roadLength;

        offsetY = Mathf.Repeat(offsetY + direction * scrollSpeed * offsetPerUnit * Time.deltaTime, 1f);
        mat.SetTextureOffset(textureProperty, new Vector2(0f, offsetY));
    }
}