using System.Collections.Generic;
using UnityEngine;

public class CarHitFeedback : MonoBehaviour
{
    [Header("Impact particle")]
    [SerializeField] ParticleSystem impactParticle;
    [Tooltip("Move the particle to the contact point when one is given.")]
    [SerializeField] bool moveToHitPoint = true;

    [Header("Red flash")]
    [Tooltip("Body meshes to flash. Leave empty to use every mesh under this object.")]
    [SerializeField] Renderer[] renderers;
    [SerializeField] Color hitColor = new Color(1f, 0.1f, 0.1f);
    [SerializeField] float flashDuration = 0.4f;
    [SerializeField] int flashPulses = 2;

    [Header("Camera shake")]
    [SerializeField] float shakeStrength = 0.35f;
    [SerializeField] float shakeDuration = 0.4f;

    [Header("General")]
    [Tooltip("Ignore new hits for this many seconds.")]
    [SerializeField] float cooldown = 0.25f;

    [Tooltip("Leave empty to find the HealthSystem automatically.")]
    [SerializeField] HealthSystem health;

    struct Slot
    {
        public Renderer renderer;
        public int index;
        public int colorId;
        public Color original;
    }

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    readonly List<Slot> slots = new List<Slot>();
    MaterialPropertyBlock block;
    float flashTimer = -1f;
    float lastHitTime = -100f;

    void Awake()
    {
        block = new MaterialPropertyBlock();

        if (renderers == null || renderers.Length == 0)
        {
            var found = new List<Renderer>();
            foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
                if (r is MeshRenderer || r is SkinnedMeshRenderer) found.Add(r);
            renderers = found.ToArray();
        }

        foreach (Renderer r in renderers)
        {
            if (r == null) continue;

            Material[] mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;

                int id = m.HasProperty(BaseColorId) ? BaseColorId : m.HasProperty(ColorId) ? ColorId : -1;
                if (id < 0) continue;

                slots.Add(new Slot { renderer = r, index = i, colorId = id, original = m.GetColor(id) });
            }
        }
    }

    [System.Obsolete]
    void OnEnable()
    {
        if (health == null) health = GetComponentInParent<HealthSystem>();
        if (health == null) health = FindFirstObjectByType<HealthSystem>();

        if (health != null) health.OnDamaged += OnDamaged;
        else Debug.LogWarning("CarHitFeedback: no HealthSystem found.", this);
    }

    void OnDisable()
    {
        if (health != null) health.OnDamaged -= OnDamaged;
    }

    void OnDamaged() => OnHit();

    public void OnHit() => OnHit(transform.position);

    public void OnHit(Vector3 point)
    {
        if (Time.unscaledTime - lastHitTime < cooldown) return;
        lastHitTime = Time.unscaledTime;

        if (impactParticle != null)
        {
            if (moveToHitPoint) impactParticle.transform.position = point;
            impactParticle.Play();
        }

        flashTimer = 0f;

        if (CameraShake.Instance != null) CameraShake.Instance.Shake(shakeStrength, shakeDuration);
        else Debug.LogWarning("CarHitFeedback: no CameraShake on the camera, add it to your camera for the shake.", this);
    }

    void Update()
    {
        if (flashTimer < 0f) return;

        flashTimer += Time.unscaledDeltaTime;
        float t = flashDuration > 0f ? Mathf.Clamp01(flashTimer / flashDuration) : 1f;
        float amount = t >= 1f ? 0f : Mathf.Abs(Mathf.Cos(t * Mathf.PI * Mathf.Max(1, flashPulses))) * (1f - t);

        foreach (Slot s in slots)
        {
            if (s.renderer == null) continue;

            s.renderer.GetPropertyBlock(block, s.index);
            block.SetColor(s.colorId, Color.Lerp(s.original, hitColor, amount));
            s.renderer.SetPropertyBlock(block, s.index);
        }

        if (t >= 1f) flashTimer = -1f;
    }
}