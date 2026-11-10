using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public partial class DemoBootstrap
{
    class LineHit { public Line line; public int seg; public float dist; }
    GameObject lineBubbleRoot, lineBubbleScrim;

    public void OnLineTap(Vector2 screen)
    {
        if (dragging || buildMode || pageRoot != null || activePanel != null) return;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, screen, null, out local)) return;
        var hits = FindLineHits(local, 24f);
        if (hits.Count == 0) { Toast("点在线路上可以拆除或查看运营"); return; }
        ShowLineBubbles(local, hits);
    }

    List<LineHit> FindLineHits(Vector2 point, float tolerance)
    {
        var best = new Dictionary<int, LineHit>();
        foreach (var line in lines)
        {
            if (line.st == null || line.st.Count < 2) continue;
            for (int i = 0; i < line.st.Count - 1; i++)
            {
                float d = DistToSeg(point, Pos(line.st[i]), Pos(line.st[i+1]));
                if (d > tolerance) continue;
                LineHit h;
                if (!best.TryGetValue(line.id, out h) || d < h.dist)
                    best[line.id] = new LineHit { line = line, seg = i, dist = d };
            }
        }
        var list = new List<LineHit>(best.Values);
        list.Sort((a, b) => a.line.id.CompareTo(b.line.id));
        return list;
    }

    void ShowLineBubbles(Vector2 at, List<LineHit> hits)
    {
        CloseLineBubble();
        lineBubbleScrim = Rect("LineScrim", root, 0, 0, W, H);
        var simg = Img(lineBubbleScrim, new Color(0.10f,0.09f,0.08f,0.035f));
        var sb = lineBubbleScrim.AddComponent<Button>();
        sb.transition = Selectable.Transition.None; sb.targetGraphic = simg;
        sb.onClick.AddListener(CloseLineBubble);
        lineBubbleRoot = Rect("LineBubbles", lineBubbleScrim.transform, 0, 0, W, H);
        lineBubbleRoot.transform.SetAsLastSibling();

        const float bw = 460f, bh = 156f, gap = 12f;
        float total = hits.Count * bh + (hits.Count - 1) * gap;
        float x = Mathf.Clamp(at.x - bw / 2f, 16f, W - bw - 16f);
        float above = at.y + 24f;
        float groupY = above + total <= H - 250f ? above : at.y - total - 24f;
        groupY = Mathf.Clamp(groupY, 210f, H - 240f - total);
        float top = groupY + total;

        for (int i = 0; i < hits.Count; i++)
        {
            var hit = hits[i];
            var line = hit.line;
            if (!lines.Contains(line)) continue;
            int a = line.st[hit.seg], b = line.st[hit.seg + 1];
            float y = top - bh - i * (bh + gap);
            var card = Rect("LBubble" + line.id, lineBubbleRoot.transform, x, y, bw, bh);
            Rounded(card, new Color(1f, 0.985f, 0.95f, 0.985f)); SoftShadow(card);
            Img(Rect("Accent", card.transform, 0, 12, 6, bh - 24), line.color, spriteRound).raycastTarget = false;
            Txt(card.transform, line.name, 25, Ink, 18, bh - 50, 150, 34, TextAnchor.MiddleLeft);
            Txt(card.transform, stations[a].name + " ⇄ " + stations[b].name, 18, InkSoft, 168, bh - 48, 280, 30, TextAnchor.MiddleLeft);

            var b1 = Btn(card.transform, "CutSeg", "拆该段", 21, 18, 16, 128, 64, new Color(0.965f,0.895f,0.85f), () => {
                CloseLineBubble();
                Confirm("拆除该段", SegmentConfirmText(line, hit.seg), () => DoDemolishSegment(line, hit.seg));
            });
            TInk(b1, Vermilion);
            var b2 = Btn(card.transform, "CutLine", "拆整线", 21, 158, 16, 128, 64, new Color(0.86f,0.38f,0.29f), () => {
                CloseLineBubble();
                Confirm("拆除 " + line.name, LineConfirmText(line), () => DoDemolishLine(line));
            });
            TInk(b2, Color.white);
            var b3 = Btn(card.transform, "Info", "详 情", 21, 298, 16, 128, 64, Indigo, () => {
                CloseLineBubble();
                OpenLineInfo(line, hit.seg);
            });
            TInk(b3, Color.white);
        }
    }

    string SegmentConfirmText(Line line, int seg)
    {
        if (line == null || seg < 0 || seg >= line.st.Count - 1) return "";
        int a = line.st[seg], b = line.st[seg + 1];
        float d = Vector2.Distance(Pos(a), Pos(b));
        int refund = Mathf.RoundToInt((30f + d * 0.75f) * 0.6f);
        string tail = line.st.Count <= 2 ? "\n该段拆除后，这条线路会消失。" : "\n拆除后退还 ¥" + refund + "。";
        return stations[a].name + " ⇄ " + stations[b].name + tail;
    }
    string LineConfirmText(Line line)
    {
        if (line == null) return "";
        int refund = Mathf.RoundToInt(120f * (UniqueCount(line.st) - 1) * 0.6f);
        return line.st.Count + " 站 · " + line.trains + " 列车\n拆除后退还 ¥" + refund + "。";
    }

    void DoDemolishSegment(Line line, int seg)
    {
        if (line == null || !lines.Contains(line) || seg < 0 || seg >= line.st.Count - 1) return;
        int n = line.st.Count;
        if (n > 3 && seg > 0 && seg < n - 2 && lines.Count >= 6)
        {
            Toast("线路已满 6 条，无法拆分该段；可拆除整线");
            return;
        }
        int a = line.st[seg], b = line.st[seg + 1];
        float d = Vector2.Distance(Pos(a), Pos(b));
        int refund = Mathf.RoundToInt((30f + d * 0.75f) * 0.6f);
        string msg;

        if (n <= 2)
        {
            lines.Remove(line);
            coins += refund;
            msg = "已拆除 " + line.name;
        }
        else if (seg == 0)
        {
            line.st.RemoveAt(0);
            coins += refund;
            msg = "已拆除一段：" + stations[a].name + " ⇄ " + stations[b].name;
        }
        else if (seg == n - 2)
        {
            line.st.RemoveAt(n - 1);
            coins += refund;
            msg = "已拆除一段：" + stations[a].name + " ⇄ " + stations[b].name;
        }
        else
        {
            var tail = line.st.GetRange(seg + 1, n - seg - 1);
            line.st.RemoveRange(seg + 1, n - seg - 1);
            coins += refund;
            var nl = new Line {
                id = nextLineId,
                name = nextLineId + " 号线",
                color = LineColors[(nextLineId - 1) % LineColors.Length]
            };
            nl.st = tail;
            lines.Add(nl);
            nextLineId++;
            msg = "线路拆分为 " + line.name + " / " + nl.name;
        }

        CreateLineVisuals(); RefreshStationDots(); RecalcIncome(); RefreshHUD();
        Toast(msg);
    }

    void DoDemolishLine(Line line)
    {
        if (line == null || !lines.Contains(line)) return;
        float refund = 120f * (UniqueCount(line.st) - 1) * 0.6f;
        coins += refund; lines.Remove(line);
        CreateLineVisuals(); RefreshStationDots(); RecalcIncome(); RefreshHUD();
        Toast("已拆除 " + line.name);
    }

    void OpenLineInfo(Line line, int seg)
    {
        if (line == null || !lines.Contains(line)) return;
        CloseLineBubble();
        float h = 1180f;
        var p = OpenPopup(line.name + " · 运营详情", h, 960);
        Img(Rect("LineColor", p.transform, 36, h - 124, 888, 8), line.color, spriteRound).raycastTarget = false;

        int unique = UniqueCount(line.st);
        int recentSum = 0;
        for (int i = 0; i < line.recent.Count; i++) recentSum += line.recent[i];

        StatBox(p.transform, "每日收入", Mathf.RoundToInt(LineDaily(line)) + "", 34, h - 300, 430, 150, Gold);
        StatBox(p.transform, "今日实收", line.todayRevenue + "", 496, h - 300, 430, 150, Matcha);
        StatBox(p.transform, "客 流", Mathf.RoundToInt(80f * Mathf.Max(0, unique - 1)) + "", 34, h - 470, 430, 150, Indigo);
        StatBox(p.transform, "运 力", (70 + 6 * line.up[1]) + "%", 496, h - 470, 430, 150, Sakura);

        Txt(p.transform, "近 7 日实收", 24, Ink, 36, 675, 420, 34, TextAnchor.MiddleLeft);
        if (line.recent.Count == 0)
        {
            for (int i = 0; i < 7; i++)
            {
                var ghost = Rect("GhostBar", p.transform, 60 + i * 120, 505, 64, 135);
                Img(ghost, new Color(0.90f, 0.87f, 0.81f, 0.42f), spriteRound).raycastTarget = false;
            }
            Img(Rect("ChartBase", p.transform, 36, 500, 888, 2), new Color(0.55f,0.50f,0.42f,0.28f)).raycastTarget = false;
            Txt(p.transform, "首日运营中 · 今日 ¥" + line.todayRevenue + "\n日结后生成近 7 日营收曲线", 21, InkSoft, 36, 545, 860, 70, TextAnchor.UpperLeft);
        }
        else
        {
            Txt(p.transform, "已结算 " + line.recent.Count + " 日 · 日均 ¥" + (recentSum / line.recent.Count), 19, InkSoft, 480, 678, 430, 30, TextAnchor.MiddleRight);
            int max = 1;
            foreach (int v in line.recent) if (v > max) max = v;
            int start = 7 - line.recent.Count;
            for (int i = 0; i < 7; i++)
            {
                float bx = 60 + i * 120;
                Img(Rect("BarBg", p.transform, bx, 505, 64, 135), new Color(0.90f, 0.87f, 0.81f, 0.55f), spriteRound).raycastTarget = false;
                if (i >= start)
                {
                    int v = line.recent[i - start];
                    float bh = Mathf.Max(3f, 135f * v / max);
                    Img(Rect("Bar", p.transform, bx, 505, 64, bh), line.color, spriteRound).raycastTarget = false;
                }
            }
        }

        Txt(p.transform, "运 营", 24, Ink, 36, 470, 300, 34, TextAnchor.MiddleLeft);
        Txt(p.transform, "车站 " + unique + " · 列车 " + line.trains + "/8 · 线路长度 " + Mathf.RoundToInt(PathLength(line.st)) + "m", 21, Ink, 36, 420, 860, 34, TextAnchor.MiddleLeft);
        Txt(p.transform, "车速 Lv." + line.up[0] + " · 编组 Lv." + line.up[1] + " · 班次 Lv." + line.up[2], 21, InkSoft, 36, 384, 860, 30, TextAnchor.MiddleLeft);
        if (seg >= 0 && seg < line.st.Count - 1)
            Txt(p.transform, "选中段：" + stations[line.st[seg]].name + " ⇄ " + stations[line.st[seg + 1]].name, 19, line.color, 36, 344, 860, 30, TextAnchor.MiddleLeft);
        Txt(p.transform, "沿线 " + UniquePathText(line.st), 19, InkSoft, 36, 296, 860, 48, TextAnchor.UpperLeft);

        Btn(p.transform, "LineDetail", "线路详情 / 升级", 26, 34, 34, 430, 82, Indigo, () => {
            ClosePanel(); OpenLineScreen(line.id);
        });
        var close = Btn(p.transform, "InfoClose", "关 闭", 26, 496, 34, 430, 82, new Color(0.92f,0.89f,0.83f), ClosePanel);
        TInk(close, Ink);
    }

    float PathLength(List<int> path)
    {
        float total = 0;
        for (int i = 0; i < path.Count - 1; i++) total += Vector2.Distance(Pos(path[i]), Pos(path[i+1]));
        return total;
    }

    void CloseLineBubble()
    {
        if (lineBubbleRoot) Destroy(lineBubbleRoot);
        if (lineBubbleScrim) Destroy(lineBubbleScrim);
        lineBubbleRoot = null; lineBubbleScrim = null;
    }
}

public class LineTapPress : MonoBehaviour, IPointerClickHandler
{
    public DemoBootstrap boot;
    public void OnPointerClick(PointerEventData e) { if (boot) boot.OnLineTap(e.position); }
}
