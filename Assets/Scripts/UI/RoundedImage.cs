using UnityEngine;
using UnityEngine.UI;

namespace StoryToys.DragDrop
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Rounded Image")]
    public class RoundedImage : Graphic
    {
        [SerializeField, Min(0f)] private float radius = 18f;
        [SerializeField, Range(2, 32)] private int cornerSegments = 8;

        public float Radius
        {
            get => radius;
            set { radius = Mathf.Max(0f, value); SetVerticesDirty(); }
        }

        public int CornerSegments
        {
            get => cornerSegments;
            set { cornerSegments = Mathf.Clamp(value, 2, 32); SetVerticesDirty(); }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            var rect = GetPixelAdjustedRect();
            float width = rect.width;
            float height = rect.height;

            float r = Mathf.Clamp(radius, 0f, 0.5f * Mathf.Min(width, height));
            int seg = Mathf.Clamp(cornerSegments, 2, 32);

            Vector2 bl = new(rect.xMin, rect.yMin);
            Vector2 br = new(rect.xMax, rect.yMin);
            Vector2 tl = new(rect.xMin, rect.yMax);
            Vector2 tr = new(rect.xMax, rect.yMax);

            // Center quad
            AddQuad(vh,
                new Vector2(bl.x + r, bl.y + r),
                new Vector2(tr.x - r, tr.y - r),
                color);

            // Edge quads
            // Top
            AddQuad(vh,
                new Vector2(tl.x + r, tr.y - r),
                new Vector2(tr.x - r, tr.y),
                color);
            // Bottom
            AddQuad(vh,
                new Vector2(bl.x + r, bl.y),
                new Vector2(br.x - r, bl.y + r),
                color);
            // Left
            AddQuad(vh,
                new Vector2(bl.x, bl.y + r),
                new Vector2(tl.x + r, tl.y - r),
                color);
            // Right
            AddQuad(vh,
                new Vector2(br.x - r, br.y + r),
                new Vector2(tr.x, tr.y - r),
                color);

            // Corner fans (quarter circles)
            AddCorner(vh, new Vector2(tl.x + r, tl.y - r), 180f, 270f, r, seg, color); // Top-Left
            AddCorner(vh, new Vector2(tr.x - r, tr.y - r), 270f, 360f, r, seg, color); // Top-Right
            AddCorner(vh, new Vector2(br.x - r, br.y + r),   0f,  90f, r, seg, color); // Bottom-Right
            AddCorner(vh, new Vector2(bl.x + r, bl.y + r),  90f, 180f, r, seg, color); // Bottom-Left
        }

        private static void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Color32 col)
        {
            int start = vh.currentVertCount;

            UIVertex v0 = UIVertex.simpleVert; v0.position = new Vector3(min.x, min.y); v0.color = col; v0.uv0 = new Vector2(0, 0);
            UIVertex v1 = UIVertex.simpleVert; v1.position = new Vector3(min.x, max.y); v1.color = col; v1.uv0 = new Vector2(0, 1);
            UIVertex v2 = UIVertex.simpleVert; v2.position = new Vector3(max.x, max.y); v2.color = col; v2.uv0 = new Vector2(1, 1);
            UIVertex v3 = UIVertex.simpleVert; v3.position = new Vector3(max.x, min.y); v3.color = col; v3.uv0 = new Vector2(1, 0);

            vh.AddVert(v0);
            vh.AddVert(v1);
            vh.AddVert(v2);
            vh.AddVert(v3);

            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        private static void AddCorner(VertexHelper vh, Vector2 center, float startAngleDeg, float endAngleDeg, float r, int segments, Color32 col)
        {
            int start = vh.currentVertCount;

            // Center vertex of the fan
            UIVertex vc = UIVertex.simpleVert;
            vc.position = center;
            vc.color = col;
            vc.uv0 = new Vector2(0.5f, 0.5f);
            vh.AddVert(vc);

            float startRad = startAngleDeg * Mathf.Deg2Rad;
            float endRad = endAngleDeg * Mathf.Deg2Rad;
            float step = (endRad - startRad) / segments;

            // Perimeter vertices
            for (int i = 0; i <= segments; i++)
            {
                float a = startRad + step * i;
                Vector2 p = center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                UIVertex v = UIVertex.simpleVert;
                v.position = p;
                v.color = col;
                // Rough UVs for completeness
                v.uv0 = new Vector2(0.5f + Mathf.Cos(a) * 0.5f, 0.5f + Mathf.Sin(a) * 0.5f);
                vh.AddVert(v);
            }

            // Triangles
            for (int i = 0; i < segments; i++)
            {
                vh.AddTriangle(start, start + 1 + i, start + 2 + i);
            }
        }
    }
}
