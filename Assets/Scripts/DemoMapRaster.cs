using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 运行时用代码绘制的底图（不依赖任何贴图资源）：
/// 海面深浅渐变 + 浪纹、岸线加深、山体阴影、等高线、城区块。
/// </summary>
public partial class DemoBootstrap
{
    void BuildRasterMap(Transform parent)
    {
        var go = Rect("BaseMap", parent, 0, 0, W, H);
        var raw = go.AddComponent<RawImage>();
        raw.texture = MakeMapTexture2(1080, 1920);
        raw.color = Color.white;
        raw.raycastTarget = false;
    }

    // 广东地图为手绘风格示意，不绘制行政边界。
    GameObject BuildGuangdongMap(Transform parent)
    {
        var go = Rect("GuangdongArt", parent, 0, 0, W, H);
        var raw = go.AddComponent<RawImage>(); raw.raycastTarget = false;
        raw.texture = Resources.Load<Texture2D>("MapArt/guangdong_illustrated");
        var rt = go.GetComponent<RectTransform>();
        Label(rt, "广 东", 34, 0.48f, 0.84f, new Color(0.40f,0.38f,0.32f,0.70f), 200);
        var hint = Txt(go.transform, "放大地图查看县级站点", 19, new Color(0.28f,0.34f,0.31f,0.75f), 600, 230, 430, 32, TextAnchor.MiddleRight);
        hint.raycastTarget = false;
        return go;
    }

    // 澳门示意轮廓参考 OpenStreetMap 并经过简化，见 docs/map-data。
    GameObject BuildMacauMap(Transform parent)
    {
        var go = Rect("MacauArt", parent, 0, 0, W, H);
        var raw = go.AddComponent<RawImage>(); raw.raycastTarget = false;
        raw.texture = Resources.Load<Texture2D>("MapArt/macau_osm");
        var rt = go.GetComponent<RectTransform>();
        var labelColor = new Color(0.40f,0.38f,0.32f,0.70f);
        Label(rt, "澳门半岛", 25, MacauX(113.542f), MacauY(22.224f), labelColor, 180);
        Label(rt, "氹仔", 24, MacauX(113.550f), MacauY(22.171f), labelColor, 150);
        Label(rt, "路氹", 24, MacauX(113.566f), MacauY(22.136f), labelColor, 150);
        Label(rt, "路环", 24, MacauX(113.564f), MacauY(22.105f), labelColor, 150);
        var credit = Txt(go.transform, "© OpenStreetMap contributors", 18, new Color(0.22f,0.30f,0.31f,0.70f), 570, 188, 490, 28, TextAnchor.MiddleRight);
        credit.raycastTarget = false;
        return go;
    }

    Texture2D MakeMapTexture2(int w, int h)
    {
        Vector2[][] polys = new Vector2[][] {
            new []{ new Vector2(0.50f,0.225f), new Vector2(0.56f,0.215f), new Vector2(0.63f,0.22f), new Vector2(0.70f,0.215f), new Vector2(0.77f,0.235f), new Vector2(0.83f,0.23f), new Vector2(0.90f,0.24f), new Vector2(0.945f,0.27f), new Vector2(0.94f,0.30f), new Vector2(0.90f,0.33f), new Vector2(0.84f,0.335f), new Vector2(0.78f,0.35f), new Vector2(0.71f,0.34f), new Vector2(0.65f,0.35f), new Vector2(0.59f,0.335f), new Vector2(0.54f,0.32f), new Vector2(0.50f,0.29f) },
            new []{ new Vector2(0.52f,0.395f), new Vector2(0.60f,0.385f), new Vector2(0.68f,0.39f), new Vector2(0.74f,0.40f), new Vector2(0.80f,0.42f), new Vector2(0.845f,0.46f), new Vector2(0.86f,0.52f), new Vector2(0.83f,0.56f), new Vector2(0.77f,0.595f), new Vector2(0.70f,0.60f), new Vector2(0.64f,0.575f), new Vector2(0.58f,0.555f), new Vector2(0.53f,0.52f), new Vector2(0.50f,0.47f) },
            new []{ new Vector2(0.18f,0.56f), new Vector2(0.24f,0.60f), new Vector2(0.30f,0.585f), new Vector2(0.36f,0.61f), new Vector2(0.43f,0.59f), new Vector2(0.50f,0.605f), new Vector2(0.57f,0.59f), new Vector2(0.64f,0.615f), new Vector2(0.71f,0.63f), new Vector2(0.78f,0.66f), new Vector2(0.84f,0.69f), new Vector2(0.90f,0.72f), new Vector2(0.96f,0.77f), new Vector2(1.0f,0.82f), new Vector2(1.0f,1.0f), new Vector2(0.30f,1.0f), new Vector2(0.24f,0.93f), new Vector2(0.20f,0.85f), new Vector2(0.17f,0.76f), new Vector2(0.16f,0.68f) },
            new []{ new Vector2(0.04f,0.20f), new Vector2(0.10f,0.17f), new Vector2(0.17f,0.16f), new Vector2(0.24f,0.14f), new Vector2(0.31f,0.155f), new Vector2(0.37f,0.19f), new Vector2(0.40f,0.245f), new Vector2(0.38f,0.30f), new Vector2(0.34f,0.36f), new Vector2(0.28f,0.40f), new Vector2(0.21f,0.415f), new Vector2(0.14f,0.40f), new Vector2(0.08f,0.36f), new Vector2(0.04f,0.30f), new Vector2(0.03f,0.25f) },
            new []{ new Vector2(0.58f,0.115f), new Vector2(0.63f,0.11f), new Vector2(0.66f,0.145f), new Vector2(0.63f,0.175f), new Vector2(0.58f,0.165f), new Vector2(0.56f,0.14f) },
            new []{ new Vector2(0.36f,0.19f), new Vector2(0.395f,0.185f), new Vector2(0.41f,0.215f), new Vector2(0.385f,0.235f), new Vector2(0.355f,0.225f) },
            new []{ new Vector2(0.455f,0.235f), new Vector2(0.48f,0.23f), new Vector2(0.49f,0.255f), new Vector2(0.47f,0.27f), new Vector2(0.445f,0.26f) },
            new []{ new Vector2(0.75f,0.60f), new Vector2(0.79f,0.57f), new Vector2(0.88f,0.565f), new Vector2(0.975f,0.58f), new Vector2(0.98f,0.67f), new Vector2(0.93f,0.735f), new Vector2(0.84f,0.73f), new Vector2(0.76f,0.68f) },
            new []{ new Vector2(0.36f,0.52f), new Vector2(0.37f,0.575f), new Vector2(0.41f,0.59f), new Vector2(0.44f,0.575f), new Vector2(0.44f,0.535f), new Vector2(0.41f,0.515f) }
        };
        Color[] landCols = {
            new Color(0.85f,0.87f,0.72f), new Color(0.87f,0.86f,0.74f), new Color(0.84f,0.86f,0.70f),
            new Color(0.85f,0.87f,0.73f), new Color(0.86f,0.87f,0.74f), new Color(0.86f,0.87f,0.74f), new Color(0.86f,0.87f,0.74f),
            new Color(0.84f,0.86f,0.70f), new Color(0.85f,0.87f,0.72f)
        };

        bool[] land = new bool[w * h];
        int[] reg = new int[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x + 0.5f) / w, v = ((y + 0.5f) / h - BAND_T) / BAND_H;
                int r = -1;
                for (int k = 0; k < polys.Length; k++) if (PtInPoly(u, v, polys[k])) { r = k; break; }
                land[y * w + x] = r >= 0; reg[y * w + x] = r;
            }

        // 离岸距离：浅海 -> 深海渐变 + 浪花
        int[] dist = new int[w * h]; const int INF = 9999;
        for (int i = 0; i < w * h; i++) dist[i] = land[i] ? 0 : INF;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            int i = y * w + x; if (land[i]) continue;
            int d = dist[i];
            if (x > 0) d = Mathf.Min(d, dist[i - 1] + 1);
            if (y > 0) d = Mathf.Min(d, dist[i - w] + 1);
            dist[i] = d;
        }
        for (int y = h - 1; y >= 0; y--) for (int x = w - 1; x >= 0; x--)
        {
            int i = y * w + x; if (land[i]) continue;
            int d = dist[i];
            if (x < w - 1) d = Mathf.Min(d, dist[i + 1] + 1);
            if (y < h - 1) d = Mathf.Min(d, dist[i + w] + 1);
            dist[i] = d;
        }

        Color[] px = new Color[w * h];
        Color deep = new Color(0.50f, 0.69f, 0.75f), shallow = new Color(0.71f, 0.84f, 0.85f), foam = new Color(0.83f, 0.90f, 0.88f);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            float u = (x + 0.5f) / w, v = ((y + 0.5f) / h - BAND_T) / BAND_H;
            float n = Hash(x, y, 1) * 0.06f - 0.03f;
            if (land[i])
            {
                Color c = landCols[reg[i]];
                float e = Mathf.Sin(u * 72f) + Mathf.Sin(v * 54f) + Mathf.Sin((u + v) * 41f);
                if (Mathf.Abs(e) < 0.06f) c = Color.Lerp(c, new Color(0.60f, 0.64f, 0.52f), 0.20f);
                px[i] = c * (1f + n);
            }
            else
            {
                float depth = Mathf.Clamp01(dist[i] / 60f);
                Color c = Color.Lerp(shallow, deep, depth);
                if (dist[i] < 7) c = Color.Lerp(foam, c, dist[i] / 7f * 0.8f);
                px[i] = c * (1f + n);
            }
        }

        // 海面浪纹
        System.Random rnd = new System.Random(23);
        for (int k = 0; k < 1100; k++)
        {
            int x = rnd.Next(2, w - 14), y = rnd.Next(2, h - 2);
            int i = y * w + x;
            if (land[i] || dist[i] < 10) continue;
            Color c = Color.Lerp(px[i], new Color(0.40f, 0.58f, 0.64f), 0.22f);
            int len = rnd.Next(4, 12);
            for (int t = 0; t < len && x + t < w; t++) px[i + t] = c;
        }

        // 每个车站周围的城区块
        for (int s = 0; s < stations.Count; s++)
        {
            if (stations[s].portal || stations[s].region != "香港") continue;
            int cx = (int)(stations[s].x * w), cy = (int)((BAND_T + stations[s].y * BAND_H) * h);
            int r = Mathf.Max(12, (int)(0.038f * w));
            for (int k = 0; k < 90; k++)
            {
                int ox = rnd.Next(-r, r), oy = rnd.Next(-r, r);
                if (ox * ox + oy * oy > r * r) continue;
                int x = cx + ox, y = cy + oy;
                if (x < 1 || y < 1 || x >= w - 1 || y >= h - 1) continue;
                int i = y * w + x;
                if (!land[i]) continue;
                Color c = Color.Lerp(px[i], new Color(0.52f, 0.47f, 0.41f), 0.20f);
                px[i] = c; if (x + 1 < w) px[i + 1] = c; if (y + 1 < h) px[i + w] = c;
            }
        }

        // 岸线加深 + 一线白浪
        for (int y = 1; y < h - 1; y++) for (int x = 1; x < w - 1; x++)
        {
            int i = y * w + x;
            if (land[i])
            {
                if (!land[i - 1] || !land[i + 1] || !land[i - w] || !land[i + w])
                    px[i] = Color.Lerp(px[i], new Color(0.34f, 0.42f, 0.38f), 0.75f);
            }
            else if (dist[i] == 1)
                px[i] = Color.Lerp(px[i], new Color(0.46f, 0.56f, 0.54f), 0.5f);
        }

        // 地形：分层色带 + 等高线（纯代码绘制，不用贴图）
        Color tint = new Color(0.47f, 0.56f, 0.40f);
        DrawHillBands(px, land, w, h, 0.530f, 0.258f, 0.085f, 0.050f, 1, tint);
        DrawHillBands(px, land, w, h, 0.565f, 0.248f, 0.055f, 0.035f, 2, tint);
        DrawHillBands(px, land, w, h, 0.760f, 0.600f, 0.080f, 0.050f, 3, tint);
        DrawHillBands(px, land, w, h, 0.560f, 0.790f, 0.120f, 0.070f, 4, tint);
        DrawHillBands(px, land, w, h, 0.620f, 0.800f, 0.075f, 0.050f, 5, tint);
        DrawHillBands(px, land, w, h, 0.240f, 0.292f, 0.075f, 0.045f, 6, tint);
        DrawHillBands(px, land, w, h, 0.860f, 0.680f, 0.070f, 0.040f, 7, tint);

        DrawHillContours(px, land, w, h, 0.530f, 0.258f, 0.085f, 0.050f, 1);
        DrawHillContours(px, land, w, h, 0.565f, 0.248f, 0.055f, 0.035f, 2);
        DrawHillContours(px, land, w, h, 0.760f, 0.600f, 0.080f, 0.050f, 3);
        DrawHillContours(px, land, w, h, 0.560f, 0.790f, 0.120f, 0.070f, 4);
        DrawHillContours(px, land, w, h, 0.620f, 0.800f, 0.075f, 0.050f, 5);
        DrawHillContours(px, land, w, h, 0.240f, 0.292f, 0.075f, 0.045f, 6);
        DrawHillContours(px, land, w, h, 0.860f, 0.680f, 0.070f, 0.040f, 7);

        var tex = new Texture2D(w, h);
        tex.SetPixels(px);
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        return tex;
    }

    static float Hash(int x, int y, int s)
    {
        int n = x * 374761393 + y * 668265263 + s * 1442695041;
        n = (n ^ (n >> 13)) * 1274126177;
        n = n ^ (n >> 16);
        return (n & 0x7fffffff) / (float)0x7fffffff;
    }

    // 山体：由外向内 5 层椭圆色带，读起来就是等高线地形
    void DrawHillBands(Color[] px, bool[] land, int w, int h, float cx, float cy, float rx, float ry, int seed, Color tint)
    {
        int x0 = Mathf.Max(0, (int)((cx - rx * 1.1f) * w)), x1 = Mathf.Min(w - 1, (int)((cx + rx * 1.1f) * w));
        int y0 = Mathf.Max(0, (int)((BAND_T + (cy - ry * 1.1f) * BAND_H) * h)), y1 = Mathf.Min(h - 1, (int)((BAND_T + (cy + ry * 1.1f) * BAND_H) * h));
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
        {
            int i = y * w + x;
            if (!land[i]) continue;
            float u = (x + 0.5f) / w, v = ((y + 0.5f) / h - BAND_T) / BAND_H;
            float ex = (u - cx) / rx, ey = (v - cy) / ry;
            float r = Mathf.Sqrt(ex * ex + ey * ey);
            if (r > 1f) continue;
            float a = Mathf.Atan2(ey, ex);
            float wob = 1f + 0.045f * Mathf.Sin(a * 3f + seed * 1.7f) + 0.02f * Mathf.Sin(a * 7f + seed);
            float rr = r / wob;
            if (rr > 1f) continue;
            int band = Mathf.Clamp(Mathf.FloorToInt((1f - rr) / 0.2f), 0, 4);
            float k = band / 4f;
            px[i] = Color.Lerp(px[i], tint, 0.05f + k * 0.085f);
        }
    }

    // 等高线：每个山头画 4 圈带轻微抖动的闭合线，遇海裁剪
    void DrawHillContours(Color[] px, bool[] land, int w, int h, float cx, float cy, float rx, float ry, int seed)
    {
        for (int k = 1; k <= 4; k++)
        {
            float t = k * 0.20f;
            int steps = 132;
            float prevX = 0f, prevY = 0f; bool has = false;
            for (int i = 0; i <= steps; i++)
            {
                float a = i / (float)steps * Mathf.PI * 2f;
                float wob = 1f + 0.045f * Mathf.Sin(a * 3f + seed * 1.7f + k) + 0.02f * Mathf.Sin(a * 7f + seed);
                float u = cx + Mathf.Cos(a) * rx * t * wob;
                float v = cy + Mathf.Sin(a) * ry * t * wob;
                float fx = u * w, fy = (BAND_T + v * BAND_H) * h;
                if (has) DrawMapLine(px, land, w, h, prevX, prevY, fx, fy, new Color(0.47f, 0.53f, 0.41f, 0.045f));
                prevX = fx; prevY = fy; has = true;
            }
        }
    }

    void DrawMapLine(Color[] px, bool[] land, int w, int h, float x0, float y0, float x1, float y1, Color c)
    {
        float dx = x1 - x0, dy = y1 - y0;
        int steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) * 1.6f));
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            int x = Mathf.RoundToInt(x0 + dx * t);
            int y = Mathf.RoundToInt(y0 + dy * t);
            for (int oy = 0; oy <= 1; oy++) for (int ox = 0; ox <= 1; ox++)
            {
                int xx = x + ox, yy = y + oy;
                if (xx < 1 || yy < 1 || xx >= w - 1 || yy >= h - 1) continue;
                int idx = yy * w + xx;
                if (!land[idx]) continue;
                px[idx] = Color.Lerp(px[idx], c, c.a);
            }
        }
    }
}
