using System.Collections;
using UnityEngine;

namespace StoryToys.DragDrop
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class BlinkController : MonoBehaviour
    {
        [Header("Anchoring (two eyes)")]
        [Tooltip("Eye center anchors. If empty, two default anchors (L/R) will be created from sprite bounds.")]
        public Transform[] eyeAnchors;
        [Tooltip("Per-eye offsets from anchors (world units)")]
        public Vector2[] eyeOffsets;

        [Header("Eyelid visuals")]
        public Color eyelidColor = Color.black;
        [Tooltip("Width and closed height of eyelid in world units")] public Vector2 closedSize = new Vector2(0.35f, 0.12f);
        [Tooltip("Open height of eyelid in world units (0 = fully hidden)")] public float openHeight = 0.0f;

        [Header("Timing")]
        [Min(0f)] public float closeTime = 0.06f;
        [Min(0f)] public float holdClosedTime = 0.06f;
        [Min(0f)] public float openTime = 0.08f;
        [Min(0f)] public float intervalMin = 2.0f;
        [Min(0f)] public float intervalMax = 6.0f;

        [Header("Sorting")]
        public int sortingOrderOffset = 20; // above robot, below clothing

        private SpriteRenderer robotSR;
        private SpriteRenderer[] eyelidSRs;

        private void Awake()
        {
            robotSR = GetComponent<SpriteRenderer>();

            // Ensure anchors (L/R) exist
            if (eyeAnchors == null || eyeAnchors.Length == 0)
            {
                var bounds = robotSR.bounds;
                var xOff = bounds.size.x * 0.18f; // horizontal separation
                var yOff = bounds.size.y * 0.15f; // vertical position
                var aL = new GameObject("EyeAnchor_L").transform;
                aL.SetParent(transform, false);
                aL.localPosition = new Vector3(-xOff, yOff, 0f);
                var aR = new GameObject("EyeAnchor_R").transform;
                aR.SetParent(transform, false);
                aR.localPosition = new Vector3( xOff, yOff, 0f);
                eyeAnchors = new[] { aL, aR };
                eyeOffsets = new[] { Vector2.zero, Vector2.zero };
            }
            else if (eyeOffsets == null || eyeOffsets.Length != eyeAnchors.Length)
            {
                eyeOffsets = EnumerableRepeat(Vector2.zero, eyeAnchors.Length);
            }

            eyelidSRs = new SpriteRenderer[eyeAnchors.Length];
            for (int i = 0; i < eyeAnchors.Length; i++)
            {
                var go = new GameObject(i == 0 ? "Eyelid_L" : "Eyelid_R");
                go.transform.SetParent(transform, worldPositionStays: false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreatePixelSprite();
                sr.color = eyelidColor;
                sr.sortingLayerID = robotSR.sortingLayerID;
                sr.sortingOrder = robotSR.sortingOrder + sortingOrderOffset;
                eyelidSRs[i] = sr;
            }

            UpdateEyelidTransform(openHeight);
            StartCoroutine(BlinkLoop());
        }

        private void UpdateEyelidTransform(float currentHeight)
        {
            for (int i = 0; i < eyelidSRs.Length; i++)
            {
                var sr = eyelidSRs[i]; if (sr == null) continue;
                var anchor = (eyeAnchors != null && i < eyeAnchors.Length && eyeAnchors[i] != null) ? eyeAnchors[i] : transform;
                var offset = (eyeOffsets != null && i < eyeOffsets.Length) ? eyeOffsets[i] : Vector2.zero;
                var anchorPos = (Vector2)(anchor ? anchor.position : transform.position) + offset;
                // Top-anchored eyelid: position at top, height extends downward; when opening, retracts upward to 0
                sr.transform.position = new Vector3(anchorPos.x, anchorPos.y - currentHeight * 0.5f, transform.position.z);
                var w = Mathf.Max(0.0001f, closedSize.x);
                var h = Mathf.Max(0.0001f, currentHeight);
                sr.transform.localScale = new Vector3(w, h, 1f);
            }
        }

        private IEnumerator BlinkLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(intervalMin, intervalMax));
                yield return LerpHeight(openHeight, closedSize.y, closeTime);
                yield return new WaitForSeconds(holdClosedTime);
                yield return LerpHeight(closedSize.y, openHeight, openTime);
            }
        }

        private IEnumerator LerpHeight(float from, float to, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / Mathf.Max(0.0001f, duration));
                float cur = Mathf.Lerp(from, to, k);
                UpdateEyelidTransform(cur);
                yield return null;
            }
            UpdateEyelidTransform(to);
        }

        private Sprite CreatePixelSprite()
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }

        private static Vector2[] EnumerableRepeat(Vector2 value, int count)
        {
            var arr = new Vector2[count];
            for (int i = 0; i < count; i++) arr[i] = value;
            return arr;
        }
    }
}
