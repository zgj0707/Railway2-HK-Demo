using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapBackdropGraphic : MaskableGraphic
{
    public Color top = new Color(0.74f, 0.86f, 0.87f, 1f);
    public Color bottom = new Color(0.52f, 0.70f, 0.77f, 1f);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = GetPixelAdjustedRect();
        vh.AddVert(new Vector3(r.xMin, r.yMin), bottom, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMin), bottom, Vector2.zero);
        vh.AddVert(new Vector3(r.xMax, r.yMax), top, Vector2.zero);
        vh.AddVert(new Vector3(r.xMin, r.yMax), top, Vector2.zero);
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(0, 2, 3);
    }
}

public class MapPolygonGraphic : MaskableGraphic
{
    Vector2[][] polys;
    Color[] fills;

    public void SetData(Vector2[][] points, Color[] colors)
    {
        polys = points; fills = colors; SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (polys == null) return;
        for (int p = 0; p < polys.Length; p++)
        {
            var poly = polys[p];
            if (poly == null || poly.Length < 3) continue;
            var tris = new List<int>(poly.Length * 3);
            Triangulate(poly, tris);
            if (tris.Count < 3) continue;
            int baseIndex = vh.currentVertCount;
            Color c = fills != null && p < fills.Length ? fills[p] : color;
            for (int i = 0; i < poly.Length; i++) vh.AddVert(new Vector3(poly[i].x, poly[i].y, 0f), c, Vector2.zero);
            for (int i = 0; i < tris.Count; i += 3) vh.AddTriangle(baseIndex + tris[i], baseIndex + tris[i + 1], baseIndex + tris[i + 2]);
        }
    }

    static void Triangulate(Vector2[] poly, List<int> tris)
    {
        int n = poly.Length;
        var idx = new List<int>(n);
        float area = 0f;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = poly[i], b = poly[(i + 1) % n];
            area += a.x * b.y - b.x * a.y;
            idx.Add(i);
        }
        if (area < 0f) idx.Reverse();
        int guard = 0;
        while (idx.Count > 3 && guard++ < n * n)
        {
            bool clipped = false;
            int count = idx.Count;
            for (int i = 0; i < count; i++)
            {
                int ia = idx[(i - 1 + count) % count], ib = idx[i], ic = idx[(i + 1) % count];
                Vector2 a = poly[ia], b = poly[ib], c = poly[ic];
                if (Cross(b - a, c - b) <= 0f) continue;
                bool inside = false;
                for (int j = 0; j < count; j++)
                {
                    int ij = idx[j];
                    if (ij == ia || ij == ib || ij == ic) continue;
                    if (PointInTriangle(poly[ij], a, b, c)) { inside = true; break; }
                }
                if (inside) continue;
                tris.Add(ia); tris.Add(ib); tris.Add(ic);
                idx.RemoveAt(i);
                clipped = true;
                break;
            }
            if (!clipped) break;
        }
        if (idx.Count == 3) { tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]); }
    }

    static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }
    static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Cross(b - a, p - a), d2 = Cross(c - b, p - b), d3 = Cross(a - c, p - c);
        bool neg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool pos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(neg && pos);
    }
}

public class MapStrokeGraphic : MaskableGraphic
{
    public class Stroke
    {
        public List<Vector2> points = new List<Vector2>();
        public bool closed;
        public float width = 2f;
        public Color color = Color.black;
    }

    public List<Stroke> strokes = new List<Stroke>();

    public void Add(List<Vector2> points, bool closed, float width, Color color)
    {
        var s = new Stroke { points = points, closed = closed, width = width, color = color };
        strokes.Add(s);
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        for (int s = 0; s < strokes.Count; s++)
        {
            var st = strokes[s];
            if (st.points == null || st.points.Count < 2) continue;
            int count = st.points.Count;
            int segs = st.closed ? count : count - 1;
            for (int i = 0; i < segs; i++)
            {
                Vector2 a = st.points[i], b = st.points[(i + 1) % count];
                Vector2 d = b - a;
                if (d.sqrMagnitude < 0.0001f) continue;
                Vector2 n = new Vector2(-d.y, d.x).normalized * (st.width * 0.5f);
                int vi = vh.currentVertCount;
                vh.AddVert(new Vector3(a.x + n.x, a.y + n.y), st.color, Vector2.zero);
                vh.AddVert(new Vector3(a.x - n.x, a.y - n.y), st.color, Vector2.zero);
                vh.AddVert(new Vector3(b.x + n.x, b.y + n.y), st.color, Vector2.zero);
                vh.AddVert(new Vector3(b.x - n.x, b.y - n.y), st.color, Vector2.zero);
                vh.AddTriangle(vi, vi + 1, vi + 2);
                vh.AddTriangle(vi + 1, vi + 3, vi + 2);
            }
        }
    }
}
