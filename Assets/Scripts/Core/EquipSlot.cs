using UnityEngine;

namespace StoryToys.DragDrop
{
    // Valid drop area for item (e.g., robot torso). Handles highlight visuals.
    [RequireComponent(typeof(Collider2D))]
    public class EquipSlot : MonoBehaviour
    {
        [Header("Anchor (optional)")]
        [Tooltip("If set, items will snap to this anchor instead of the slot's transform position.")]
        public Transform anchor;

        [Header("Tint Highlight (fallback)")]
        [SerializeField] private bool enableTintFallback = false;
        [SerializeField] private SpriteRenderer[] renderersToTint;
        [SerializeField] private Color tintColor = new Color(1f, 0.92f, 0.25f, 0.6f);
        [SerializeField, Range(0f, 1f)] private float tintIntensity = 0.6f;

        [Header("Outline Overlay")]
        [SerializeField] private Sprite glowSprite;
        [SerializeField] private Material glowMaterial; // Sprite_Outline shader material or auto-loaded
        [SerializeField] private bool autoLoadGlowMaterial = true;
        [SerializeField] private Color glowColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField] private Vector3 glowLocalOffset = Vector3.zero;
        [SerializeField] private int glowSortingOrderOffset = 50;
        [SerializeField] private bool inheritSortingLayer = true;
        [SerializeField] private OutlineSettings outlineSettings;

        private const string OutlineShaderPath = "Shader Graphs/Sprite_Outline";

        private SpriteRenderer overlay;
        private float currentLevel = 0f;
        private static Material sharedOutlineMat;
        private MaterialPropertyBlock mpb;
        private int baseSortingOrder;
        private int baseSortingLayerID;
        private Color[] originalTintColors;

        private void Awake()
        {
            // pick a renderer to tint if none provided
            if (renderersToTint == null || renderersToTint.Length == 0)
            {
                var sr = GetComponentInParent<SpriteRenderer>() ?? GetComponent<SpriteRenderer>();
                if (sr != null) renderersToTint = new[] { sr };
            }
            if (renderersToTint != null && renderersToTint.Length > 0)
            {
                originalTintColors = new Color[renderersToTint.Length];
                for (int i = 0; i < renderersToTint.Length; i++)
                    if (renderersToTint[i] != null) originalTintColors[i] = renderersToTint[i].color;
            }

            // create overlay
            if (overlay == null)
            {
                var baseSr = (renderersToTint != null && renderersToTint.Length > 0) ? renderersToTint[0] : GetComponentInParent<SpriteRenderer>();
                if (baseSr != null)
                {
                    var go = new GameObject("HighlightOverlay");
                    go.transform.SetParent(transform, false);
                    overlay = go.AddComponent<SpriteRenderer>();
                    overlay.sprite = glowSprite != null ? glowSprite : baseSr.sprite;
                    var mat = ResolveOutlineMaterial();
                    if (mat != null) overlay.sharedMaterial = mat;
                    overlay.color = glowColor;
                    if (inheritSortingLayer) overlay.sortingLayerID = baseSr.sortingLayerID;
                    baseSortingLayerID = overlay.sortingLayerID;
                    baseSortingOrder = baseSr.sortingOrder + glowSortingOrderOffset;
                    overlay.sortingOrder = baseSortingOrder;
                    overlay.enabled = false;
                    overlay.transform.localPosition = glowLocalOffset;
                    ApplyOutlineProperties();
                }
            }
            ValidateOverlayMaterial();
        }

        public void SetHighlight(bool on) => SetHighlightLevel(on ? 1f : 0f);

        public void SetHighlightLevel(float level)
        {
            currentLevel = Mathf.Clamp01(level);

            if (overlay != null)
            {
                if (currentLevel <= 0f)
                {
                    overlay.enabled = false;
                }
                else
                {
                    // Only drive alpha; keep RGB = 1 so shader _OutlineColor defines the color exactly
                    float a = Mathf.Clamp01(glowColor.a * currentLevel);
                    overlay.color = new Color(1f, 1f, 1f, a);
                    overlay.enabled = true;
                    ApplyOutlineProperties();
                }
            }

            if (enableTintFallback && overlay == null && renderersToTint != null)
            {
                for (int i = 0; i < renderersToTint.Length; i++)
                {
                    var r = renderersToTint[i]; if (r == null) continue;
                    var baseCol = originalTintColors != null && originalTintColors.Length > i ? originalTintColors[i] : r.color;
                    r.color = Color.Lerp(baseCol, tintColor, tintIntensity * currentLevel);
                }
            }
            else if (currentLevel <= 0f && renderersToTint != null && originalTintColors != null)
            {
                // restore original tint when hiding
                for (int i = 0; i < renderersToTint.Length && i < originalTintColors.Length; i++)
                {
                    if (renderersToTint[i] != null)
                        renderersToTint[i].color = originalTintColors[i];
                }
            }
        }

        public void SetDrawOnTop(bool on, SpriteRenderer referenceRenderer = null, int boost = 100)
        {
            if (overlay == null) return;
            if (on)
            {
                if (referenceRenderer != null)
                {
                    overlay.sortingLayerID = referenceRenderer.sortingLayerID;
                    overlay.sortingOrder = referenceRenderer.sortingOrder + boost;
                }
                else
                {
                    overlay.sortingOrder = baseSortingOrder + boost;
                }
            }
            else
            {
                overlay.sortingLayerID = baseSortingLayerID;
                overlay.sortingOrder = baseSortingOrder;
            }
        }

        private Material ResolveOutlineMaterial()
        {
            if (glowMaterial != null) return glowMaterial;
            if (!autoLoadGlowMaterial) return null;
            if (sharedOutlineMat != null) return sharedOutlineMat;
            // Prefer Resources material to avoid shader stripping and instantiation at runtime
            var res = Resources.Load<Material>("Materials/Sprite_Outline");
            if (res != null)
            {
                sharedOutlineMat = res;
                return sharedOutlineMat;
            }
            var shader = Shader.Find(OutlineShaderPath);
            if (shader == null)
            {
                Debug.LogWarning($"[EquipSlot] Outline shader not found: {OutlineShaderPath}. Falling back to tint mode.");
                enableTintFallback = true;
                return null;
            }
            sharedOutlineMat = new Material(shader);
            return sharedOutlineMat;
        }

        private void ApplyOutlineProperties()
        {
            if (overlay == null) return;
            if (mpb == null) mpb = new MaterialPropertyBlock();
            overlay.GetPropertyBlock(mpb);
            if (outlineSettings != null)
            {
                var col = outlineSettings.outlineColor; col.a = 1f; // alpha via overlay.color
                mpb.SetColor("_OutlineColor", col);
                mpb.SetFloat("_ThicknessPx", outlineSettings.thicknessPx);
                mpb.SetFloat("_Softness", outlineSettings.softness);
                mpb.SetFloat("_AlphaThreshold", outlineSettings.alphaThreshold);
            }
            else
            {
                // sane defaults
                mpb.SetFloat("_ThicknessPx", 1f);
                mpb.SetFloat("_Softness", 0f);
                mpb.SetFloat("_AlphaThreshold", 0.5f);
            }
            overlay.SetPropertyBlock(mpb);
        }

        private void ValidateOverlayMaterial()
        {
            if (overlay == null) return;
            var mat = overlay.sharedMaterial;
            if (mat == null || mat.shader == null || mat.shader.name != OutlineShaderPath)
            {
                var resolved = ResolveOutlineMaterial();
                if (resolved != null)
                {
                    overlay.sharedMaterial = resolved;
                }
            }
        }

        [ContextMenu("Rebind Outline Material")]
        private void RebindOutlineMaterial()
        {
            var resolved = ResolveOutlineMaterial();
            if (resolved != null && overlay != null)
            {
                overlay.sharedMaterial = resolved;
                ApplyOutlineProperties();
            }
        }
    }
}
