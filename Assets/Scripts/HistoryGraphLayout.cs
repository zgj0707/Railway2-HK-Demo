using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 一张网格绘制整圈虚线，避免每个节点创建大量 UI 对象。
public class HistoryNodeDashedOutline : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect r = rectTransform.rect;
        const float width = 5f, dash = 14f, gap = 9f;
        for (float x = r.xMin; x < r.xMax; x += dash + gap) {
            float end = Mathf.Min(x + dash, r.xMax);
            AddQuad(mesh, x, r.yMin, end, r.yMin + width);
            AddQuad(mesh, x, r.yMax - width, end, r.yMax);
        }
        for (float y = r.yMin + width; y < r.yMax - width; y += dash + gap) {
            float end = Mathf.Min(y + dash, r.yMax - width);
            AddQuad(mesh, r.xMin, y, r.xMin + width, end);
            AddQuad(mesh, r.xMax - width, y, r.xMax, end);
        }
    }
    void AddQuad(VertexHelper mesh, float x0, float y0, float x1, float y1)
    {
        int start = mesh.currentVertCount;
        mesh.AddVert(new Vector3(x0, y0), color, Vector2.zero);
        mesh.AddVert(new Vector3(x0, y1), color, Vector2.zero);
        mesh.AddVert(new Vector3(x1, y1), color, Vector2.zero);
        mesh.AddVert(new Vector3(x1, y0), color, Vector2.zero);
        mesh.AddTriangle(start, start + 1, start + 2);
        mesh.AddTriangle(start, start + 2, start + 3);
    }
}

public class HistoryGraphLayout : MonoBehaviour
{
    class Entry { public string key; public RectTransform rect; public int era; public string[] parents; public Vector2 velocity; }
    readonly List<Entry> entries = new List<Entry>();
    readonly Dictionary<string, Entry> byKey = new Dictionary<string, Entry>();
    [NonSerialized] public Dictionary<string, Vector2> positions;
    public HistoryGraphEdges edges;
    readonly Dictionary<string, float> angleTargets = new Dictionary<string, float>();
    readonly Dictionary<Vector2Int, List<int>> nearby = new Dictionary<Vector2Int, List<int>>();
    float nextAngleUpdate;
    public float updatesPerSecond = 60f;
    float accumulatedTime;
    public void Add(string key, RectTransform rect, int era, string[] parents)
    {
        var e = new Entry { key = key, rect = rect, era = era, parents = parents };
        entries.Add(e); byKey.Add(key, e);
    }
    void UpdateAngleTargets()
    {
        angleTargets.Clear();
        for (int era = 0; era < 6; era++)
        {
            var group = entries.FindAll(e => e.era == era);
            if (group.Count < 2) continue;
            group.Sort((a, b) => Mathf.Atan2(positions[a.key].y, positions[a.key].x).CompareTo(Mathf.Atan2(positions[b.key].y, positions[b.key].x)));
            float step = 2f * Mathf.PI / group.Count, sumSin = 0, sumCos = 0;
            for (int i = 0; i < group.Count; i++)
            {
                Vector2 p = positions[group[i].key];
                float offset = Mathf.Atan2(p.y, p.x) - i * step;
                sumSin += Mathf.Sin(offset); sumCos += Mathf.Cos(offset);
            }
            float baseAngle = Mathf.Atan2(sumSin, sumCos);
            for (int i = 0; i < group.Count; i++) angleTargets[group[i].key] = baseAngle + i * step;
        }
    }
    void Update()
    {
        if (positions == null || entries.Count == 0) return;
        accumulatedTime += Time.unscaledDeltaTime;
        if (accumulatedTime < 1f / Mathf.Max(1f, updatesPerSecond)) return;
        float dt = Mathf.Min(accumulatedTime, 0.1f); accumulatedTime = 0f;
        if (Time.unscaledTime >= nextAngleUpdate) { UpdateAngleTargets(); nextAngleUpdate = Time.unscaledTime + 0.25f; }
        foreach (var cell in nearby.Values) cell.Clear();
        for (int i = 0; i < entries.Count; i++) {
            Vector2 p = positions[entries[i].key];
            var cell = new Vector2Int(Mathf.FloorToInt(p.x / 300f), Mathf.FloorToInt(p.y / 300f));
            if (!nearby.TryGetValue(cell, out var bucket)) nearby[cell] = bucket = new List<int>();
            bucket.Add(i);
        }
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i]; Vector2 p = positions[e.key];
            if (e.era < 0) { positions[e.key] = Vector2.zero; e.velocity = Vector2.zero; continue; }
            Vector2 force = Vector2.zero;
            float radius = p.magnitude;
            Vector2 outward = radius > 0.01f ? p / radius : Vector2.right;
            float targetRadius = 270f + e.era * 230f;
            force += outward * (targetRadius - radius) * 1.2f;
            if (angleTargets.TryGetValue(e.key, out float desired))
            {
                float current = Mathf.Atan2(p.y, p.x);
                float error = Mathf.DeltaAngle(current * Mathf.Rad2Deg, desired * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                Vector2 tangent = new Vector2(-outward.y, outward.x);
                force += tangent * Mathf.Clamp(error * targetRadius * 0.7f, -220f, 220f);
            }
            var cell = new Vector2Int(Mathf.FloorToInt(p.x / 300f), Mathf.FloorToInt(p.y / 300f));
            for (int dx = -1; dx <= 1; dx++) for (int dy = -1; dy <= 1; dy++) {
                if (!nearby.TryGetValue(new Vector2Int(cell.x + dx, cell.y + dy), out var bucket)) continue;
                foreach (int j in bucket) {
                    if (i == j) continue;
                    Vector2 delta = p - positions[entries[j].key]; float distance = delta.magnitude;
                    if (distance < 300f) {
                        Vector2 direction = distance > 0.1f ? delta / distance : new Vector2(Mathf.Cos(i * 2.4f), Mathf.Sin(i * 2.4f));
                        force += direction * (300f - distance) * 3f;
                    }
                }
            }
            foreach (var parent in e.parents)
            {
                if (!byKey.ContainsKey(parent)) continue;
                Vector2 delta = positions[parent] - p; float distance = delta.magnitude;
                if (distance > 1f) force += delta / distance * (distance - 260f) * 0.65f;
            }
            e.velocity = (e.velocity + force * dt) * Mathf.Exp(-3.5f * dt);
        }
        foreach (var e in entries)
        {
            if (e.era >= 0) positions[e.key] += e.velocity * dt;
            var rect = e.rect; Vector2 center = new Vector2(4000, 4000) + positions[e.key];
            rect.anchoredPosition = center - Vector2.Scale(new Vector2(0.5f, 0.5f) - rect.pivot, rect.sizeDelta);
        }
        if (edges) edges.Refresh();
    }
}

public class HistoryGraphEdges : MonoBehaviour
{
    class Link { public RectTransform from, to, line; }
    readonly List<Link> links = new List<Link>();
    public Sprite lineSprite;
    public void Add(RectTransform from, RectTransform to, bool active)
    {
        var go = new GameObject("Link", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = Vector2.zero; rect.pivot = new Vector2(0, 0.5f);
        var image = go.GetComponent<Image>(); image.sprite = lineSprite;
        image.color = active ? new Color(0.27f, 0.46f, 0.60f, 0.95f) : new Color(0.66f, 0.47f, 0.37f, 0.82f);
        image.raycastTarget = false;
        links.Add(new Link { from = from, to = to, line = rect });
    }
    Vector2 Center(RectTransform r) { return r.anchoredPosition + Vector2.Scale(new Vector2(0.5f, 0.5f) - r.pivot, r.sizeDelta); }
    public void Refresh()
    {
        foreach (var link in links)
        {
            if (!link.from || !link.to || !link.line) continue;
            Vector2 a = Center(link.from), b = Center(link.to), delta = b - a;
            link.line.anchoredPosition = a;
            link.line.sizeDelta = new Vector2(delta.magnitude, 3f);
            link.line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }
    }
}
