using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    GameObject pageRoot;
    Action pageBack;

    GameObject OpenPage(string title, string help)
    {
        ClosePage();
        CloseLineBubble();
        pageRoot = Rect("Page", root, 0, 0, W, H);
        Img(pageRoot, Paper);
        Img(Rect("PTop", pageRoot.transform, 0, H-260, W, 260), new Color(0.952f,0.922f,0.870f));
        Img(Rect("PDiv", pageRoot.transform, 0, H-260, W, 3), new Color(0.55f,0.48f,0.38f,0.14f));
        var bb = Btn(pageRoot.transform, "PBack", "←", 44, 26, H-222, 96, 96, new Color(1,1,1,0.94f), GoBackPage);
        TInk(bb, Ink); SoftShadow(bb.gameObject);
        Txt(pageRoot.transform, title, 52, Ink, 144, H-164, 700, 66, TextAnchor.MiddleLeft);
        if (!string.IsNullOrEmpty(help))
        {
            var hb = Btn(pageRoot.transform, "PHelp", "?", 32, W-108, H-218, 64, 64, new Color(1,1,1,0.94f), () => HelpPopup(help));
            TInk(hb, InkSoft); SoftShadow(hb.gameObject);
        }
        return pageRoot;
    }
    void GoBackPage()
    {
        var b = pageBack; pageBack = null;
        if (b != null) { b(); return; }
        ClosePage();
    }
    void ClosePage() { if (pageRoot) Destroy(pageRoot); pageRoot = null; }

    void HelpPopup(string text)
    {
        var p = OpenPopup("说明", 560, 640);
        Txt(p.transform, text, 26, InkSoft, 34, 104, 572, 300, TextAnchor.UpperLeft);
        var ok = Btn(p.transform, "Ok", "知道了", 30, 34, 28, 572, 80, new Color(0.92f,0.89f,0.83f), ClosePanel);
        TInk(ok, Ink);
    }

    void StatBox(Transform p, string label, string value, float x, float y, float w, float h, Color c)
    {
        var g = Rect("Stat", p, x, y, w, h); Rounded(g, new Color(1,1,1,0.95f)); SoftShadow(g);
        Img(Rect("Bar", g.transform, 0, 24, 7, h-48), c, spriteRound);
        Txt(g.transform, label, 26, InkSoft, 30, h-66, w-56, 34, TextAnchor.MiddleLeft);
        Txt(g.transform, value, 46, Ink, 30, 26, w-56, 54, TextAnchor.MiddleLeft);
    }

    // ---------- 线路 ----------
    void OpenLinesScreen()
    {
        var pg = OpenPage("线路", "长按地图车站，拖到下一站开线；松手确认。从已有线路的车站起手，默认延长该线，也可另开新线。最多 6 条线路。");
        StatBox(pg.transform, "每日收入", Mathf.RoundToInt(income) + "", 28, H-500, 320, 210, Gold);
        StatBox(pg.transform, "已连通", ConnectedCount() + "/" + stations.Count, 380, H-500, 320, 210, Matcha);
        StatBox(pg.transform, "钻石", Mathf.FloorToInt(diamonds).ToString(), 732, H-500, 320, 210, Sakura);
        float top = H-800;
        for (int i = 0; i < lines.Count && i < 6; i++)
        {
            var line = lines[i]; int id = line.id;
            var card = Rect("LC"+i, pg.transform, 28, top - i*188, W-56, 172);
            Rounded(card, new Color(1,1,1,0.95f)); SoftShadow(card);
            var dot = Rect("D", card.transform, 26, 64, 44, 44); Img(dot, line.color, spriteCircle);
            Txt(card.transform, line.name, 40, Ink, 92, 100, 340, 52, TextAnchor.MiddleLeft);
            Txt(card.transform, UniqueCount(line.st) + " 站 · " + Mathf.RoundToInt(LineDaily(line)) + "/天", 26, InkSoft, 94, 48, 440, 36, TextAnchor.MiddleLeft);
            var gb = Btn(card.transform, "Go", "查看", 32, W-56-236, 42, 196, 88, new Color(0.92f,0.89f,0.83f), () => OpenLineScreen(id, OpenLinesScreen));
            TInk(gb, Ink);
        }
    }

    float LineDaily(Line line) { return DailyOf(line, line.st); }

    // ---------- 线路详情 ----------
    void OpenLineScreen(int id, Action back = null)
    {
        var line = lines.Find(l => l.id == id);
        if (line == null) { OpenLinesScreen(); return; }
        var pg = OpenPage(line.name, "客流为每日客流；车速、编组、班次直接提升本线收入，加车提升运力。");
        StatBox(pg.transform, "客流", Mathf.RoundToInt(80f*Mathf.Max(0, UniqueCount(line.st)-1)) + "", 28, H-540, 320, 200, Matcha);
        StatBox(pg.transform, "运力", (70 + 6*line.up[1]) + "%", 380, H-540, 320, 200, Indigo);
        StatBox(pg.transform, "每日收入", Mathf.RoundToInt(LineDaily(line)) + "", 732, H-540, 320, 200, Gold);
        string[] ups = { "车速", "编组", "班次" };
        for (int k = 0; k < 3; k++)
        {
            int kk = k;
            var row = Rect("UR"+k, pg.transform, 28, H-830 - k*150, W-56, 140);
            Rounded(row, new Color(1,1,1,0.94f)); SoftShadow(row);
            Txt(row.transform, ups[k], 32, Ink, 28, 84, 300, 44, TextAnchor.MiddleLeft);
            Txt(row.transform, "Lv." + line.up[k], 30, Indigo, 28, 34, 300, 40, TextAnchor.MiddleLeft);
            int lv = line.up[k];
            int cost = Mathf.RoundToInt((136 + k*70) * Mathf.Pow(1.6f, lv));
            Btn(row.transform, "Buy", "升级  ¥" + cost, 28, W-56-28-230, 26, 230, 88, Matcha, () => {
                if (coins < cost) { Toast("金币不足"); return; }
                coins -= cost; line.up[kk]++; RecalcIncome(); RefreshHUD();
                ClosePage(); OpenLineScreen(id, back);
            });
        }
        Txt(pg.transform, "沿线", 28, InkSoft, 28, H-1330, 160, 38, TextAnchor.MiddleLeft);
        Txt(pg.transform, UniquePathText(line.st), 26, Ink, 28, H-1390, W-56, 60, TextAnchor.UpperLeft);
        Btn(pg.transform, "Add", "＋ 加车  ¥" + (289 + line.trains*120), 30, 28, 56, 490, 124, Indigo, () => {
            int cost = 289 + line.trains*120;
            if (coins < cost) { Toast("金币不足"); return; }
            coins -= cost; line.trains++; RecalcIncome(); RefreshHUD();
            ClosePage(); OpenLineScreen(id, back);
        });
        Btn(pg.transform, "Del", "拆除线路", 30, 544, 56, 508, 124, Vermilion, () => {
            Confirm("拆除 " + line.name, "退还 ¥" + Mathf.RoundToInt(120f*(UniqueCount(line.st)-1)*0.6f), () => {
                coins += 120f*(UniqueCount(line.st)-1)*0.6f; lines.Remove(line);
                CreateLineVisuals(); RefreshStationDots(); RecalcIncome(); RefreshHUD(); ClosePanel();
                ClosePage(); OpenLinesScreen();
            });
        });
    }

    // ---------- 车库 ----------
    void OpenGarageScreen()
    {
        var pg = OpenPage("车库", "每条线路可升级车速、编组、班次；升级立刻生效。");
        for (int i = 0; i < lines.Count && i < 2; i++)
        {
            var line = lines[i];
            var card = Rect("GC"+i, pg.transform, 28, H-680 - i*420, W-56, 390);
            Rounded(card, new Color(1,1,1,0.95f)); SoftShadow(card);
            var dot = Rect("D", card.transform, 28, 308, 44, 44); Img(dot, line.color, spriteCircle);
            Txt(card.transform, line.name + " · " + UniqueCount(line.st) + " 站 · " + line.trains + "/8 列", 38, Ink, 92, 300, 820, 52, TextAnchor.MiddleLeft);
            string[] labels = { "车速", "编组", "班次" };
            for (int k = 0; k < 3; k++)
            {
                int kk = k; int lv = line.up[k];
                int cost = Mathf.RoundToInt((136 + k*70) * Mathf.Pow(1.6f, lv));
                Btn(card.transform, "U"+k, labels[k] + " Lv." + lv + "  ¥" + cost, 28, 28 + k*316, 70, 292, 112, Matcha, () => {
                    if (coins < cost) { Toast("金币不足"); return; }
                    coins -= cost; line.up[kk]++; RecalcIncome(); RefreshHUD();
                    ClosePage(); OpenGarageScreen();
                });
            }
        }
    }

    // ---------- 公司 ----------
    void OpenCompanyScreen()
    {
        OpenPage("公司", "公司系统筹备中：总部、员工、评级与全球区域将在这里展开。");
    }

    // ---------- 科技树（HOI4 式，每科目一页） ----------
    static readonly string[][] TechNames = {
        new string[]{"蒸汽机车","内燃机车","电力机车","高速动车","磁浮列车","氢能机车","智能驾驶","超导磁浮","星际列车"},
        new string[]{"硬座客车","卧铺客车","双层客车","集装箱平车","冷藏货车","重载货车","智能列车","空铁车厢","模块化编组"},
        new string[]{"单线铁路","复线铁路","高架桥","隧道","自动闭塞","调度中心","智慧枢纽","海底隧道","跨海大桥"},
        new string[]{"接触网","变电站","再生制动","智能供电","储能装置","无线供电","光伏铁路","氢能供电","核聚变"},
        new string[]{"售票系统","广告位","联程票","会员体系","商业开发","全球联运","品牌联名","资本市场","跨国并购"}
    };
    static readonly string[] TechCats = { "机车", "车辆", "基建", "电气化", "经营" };
    void OpenTechScreen(int cat = 0)
    {
        var pg = OpenPage("科技树", "研究点 RP 每 6 秒 +1；每个科目独立一页，完成科技提升全线收入。");
        for (int c = 0; c < 5; c++)
        {
            int cc = c;
            var b = Btn(pg.transform, "Cat"+c, TechCats[c], 26, 28 + c*204, H-390, 196, 86,
                c == cat ? Ink : new Color(0.92f,0.89f,0.83f), () => { ClosePage(); OpenTechScreen(cc); });
            if (c != cat) TInk(b, Ink);
        }
        string[] years = { "2025", "2030", "2035" };
        for (int c = 0; c < 3; c++)
        {
            Txt(pg.transform, years[c], 24, InkSoft, 40 + c*330, H-446, 300, 34, TextAnchor.MiddleCenter);
        }
        for (int row = 0; row < 3; row++)
        {
            float y = H - 700 - row*250;
            for (int col = 0; col < 3; col++)
            {
                int idx = row*3 + col;
                string key = TechCats[cat] + idx;
                bool done = techDone.Contains(key);
                var card = Rect("T"+idx, pg.transform, 40 + col*330, y, 300, 200);
                Rounded(card, done ? new Color(0.90f,0.95f,0.86f,0.97f) : new Color(1,1,1,0.96f)); SoftShadow(card);
                Txt(card.transform, TechNames[cat][idx], 27, Ink, 20, 138, 260, 40, TextAnchor.MiddleLeft);
                Txt(card.transform, "RP " + 20*(idx+1), 22, InkSoft, 20, 106, 260, 30, TextAnchor.MiddleLeft);
                if (done)
                    Btn(card.transform, "R", "已完成", 26, 20, 20, 260, 64, new Color(0.78f,0.82f,0.72f), () => { });
                else
                {
                    int cost = 20*(idx+1); string kk = key;
                    Btn(card.transform, "R", "研 究", 28, 20, 20, 260, 64, Matcha, () => {
                        if (rp < cost) { Toast("研究点不足"); return; }
                        rp -= cost; techDone.Add(kk); RecalcIncome(); RefreshHUD();
                        ClosePage(); OpenTechScreen(cat);
                    });
                }
                if (col < 2)
                {
                    Img(Rect("Link", pg.transform, 340 + col*330, y+98, 30, 4), new Color(0.45f,0.40f,0.33f,0.40f));
                }
            }
        }
    }

    // ---------- 图鉴 ----------
    void OpenSceneryScreen()
    {
        var pg = OpenPage("图鉴", "线路经过景观后，图鉴会出现红点；点击待打卡卡片领取客流加成与钻石。");
        for (int i = 0; i < sceneries.Count; i++)
        {
            var sc = sceneries[i];
            bool pending = pendingCheckin.Contains(sc);
            var card = Rect("EC"+i, pg.transform, 28 + (i%2)*516, H-560 - (i/2)*300, 500, 270);
            Rounded(card, sc.found ? new Color(0.91f,0.95f,0.85f,0.97f) : pending ? new Color(0.99f,0.91f,0.87f,0.98f) : new Color(1,1,1,0.96f)); SoftShadow(card);
            Txt(card.transform, sc.found ? "★" : pending ? "●" : "○", 44, sc.found ? Gold : pending ? Vermilion : InkSoft, 24, 176, 70, 56, TextAnchor.MiddleLeft);
            Txt(card.transform, sc.name, 36, Ink, 98, 178, 380, 50, TextAnchor.MiddleLeft);
            Txt(card.transform, sc.natural ? "自然景观" : "人文景观", 26, InkSoft, 98, 138, 380, 34, TextAnchor.MiddleLeft);
            Txt(card.transform, sc.found ? "客流 +15% · 钻石 +20" : pending ? "待打卡 · 点击领取" : "线路经过后点亮", 24, sc.found ? Matcha : pending ? Vermilion : InkSoft, 24, 50, 450, 34, TextAnchor.MiddleLeft);
            if (pending)
            {
                var scLocal = sc;
                var btn = card.AddComponent<Button>(); btn.targetGraphic = card.GetComponent<Image>();
                btn.onClick.AddListener(() => ShowCheckin(scLocal, () => { ClosePage(); OpenSceneryScreen(); }));
            }
        }
    }
}
