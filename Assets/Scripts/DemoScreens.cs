using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    GameObject pageRoot;
    Action pageBack;

    GameObject OpenPage(string title, string help, string backgroundPath = null)
    {
        ClosePage();
        pageBack = null;
        CloseLineBubble();
        pageRoot = Rect("Page", root, 0, 0, W, H);
        Img(pageRoot, Paper);
        Img(Rect("PTop", pageRoot.transform, 0, H-160, W, 160), new Color(0.952f,0.922f,0.870f));
        Img(Rect("PDiv", pageRoot.transform, 0, H-160, W, 3), new Color(0.55f,0.48f,0.38f,0.14f));
        var bb = Btn(pageRoot.transform, "PBack", "←", 40, 24, 0, 94, 94, new Color(1,1,1,0.94f), GoBackPage);
        TInk(bb, Ink); SoftShadow(bb.gameObject);
        var home = Btn(pageRoot.transform, "PHome", "主页", 24, 134, 0, 104, 94, new Color(1,1,1,0.94f), GoHome);
        TInk(home, Ink); SoftShadow(home.gameObject);
        var heading = Txt(pageRoot.transform, title, 44, Ink, 0, 0, 600, 60, TextAnchor.MiddleLeft);
        heading.resizeTextForBestFit = true; heading.resizeTextMinSize = 26; heading.resizeTextMaxSize = 44;
        RectTransform helpRect = null;
        if (!string.IsNullOrEmpty(help))
        {
            var hb = Btn(pageRoot.transform, "PHelp", "?", 32, 0, 0, 64, 64, new Color(1,1,1,0.94f), () => HelpPopup(help));
            TInk(hb, InkSoft); SoftShadow(hb.gameObject); helpRect = hb.GetComponent<RectTransform>();
        }
        if (!string.IsNullOrEmpty(backgroundPath)) AddPageBackground(pageRoot, backgroundPath);
        var layout = pageRoot.AddComponent<PageHeaderLayout>();
        layout.page = pageRoot.GetComponent<RectTransform>(); layout.back = bb.GetComponent<RectTransform>();
        layout.home = home.GetComponent<RectTransform>(); layout.title = heading.rectTransform; layout.help = helpRect;
        return pageRoot;
    }
    void AddPageBackground(GameObject page, string path)
    {
        var texture = Resources.Load<Texture2D>(path);
        if (!texture) return;
        var background = Rect("PageBackground", page.transform, 0, 0, W, H);
        var art = background.AddComponent<RawImage>(); art.texture = texture;
        art.color = new Color(1, 1, 1, 0.26f); art.raycastTarget = false;
        background.transform.SetAsFirstSibling();
        var fit = background.AddComponent<FleetBackgroundFit>(); fit.page = page.GetComponent<RectTransform>(); fit.image = art;
        var header = page.transform.Find("PTop").GetComponent<Image>();
        var headerColor = header.color; headerColor.a = 0.35f; header.color = headerColor;
    }
    void GoBackPage()
    {
        var b = pageBack; pageBack = null;
        ClosePage();
        if (b != null) b();
    }
    void ClosePage() { if (pageRoot) Destroy(pageRoot); pageRoot = null; }
    void GoHome()
    {
        pageBack = null; fleetBack = null;
        if (sceneryViewer) Destroy(sceneryViewer);
        sceneryViewer = null;
        panelClosed = null;
        ClosePanel(true); CloseLineBubble(); ClosePage();
    }

    void HelpPopup(string text)
    {
        var p = OpenBubble("说明", 560, 640, new Vector2(W - 140, H - 200));
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

    // ---------- 区域选择 ----------
    void OpenRegionScreen()
    {
        var pg = OpenPage("切换区域", "测试期间香港、澳门、广东均已开放。红色圆心是跨区域交界站，可用于延长线路。", "PageArt/line_watercolor");
        string[] regions = { "香港", "澳门", "广东" };
        for (int i = 0; i < regions.Length; i++)
        {
            string region = regions[i]; bool available = RegionUnlocked(region); bool active = region == currentRegion;
            var card = Rect("Region_" + region, pg.transform, 28, H - 450 - i * 250, W - 56, 220);
            Rounded(card, new Color(1, 1, 1, 0.95f)); SoftShadow(card);
            Img(Rect("Accent", card.transform, 0, 30, 7, 160), active ? Gold : available ? Matcha : InkSoft, spriteRound).raycastTarget = false;
            Txt(card.transform, region, 42, Ink, 32, 142, 380, 55, TextAnchor.MiddleLeft);
            string condition = active ? "当前区域" : available ? "已开放" : region == "澳门"
                ? "解锁条件：每日收入达到 ¥600"
                : "解锁条件：开放澳门，连通澳门 2 座普通站，每日收入达到 ¥1200";
            Txt(card.transform, condition, 24, InkSoft, 32, 48, 720, 72, TextAnchor.MiddleLeft);
            var button = Btn(card.transform, "Select", active ? "当前" : available ? "前往" : "未解锁", 26,
                W - 56 - 225, 65, 190, 90, active ? Paper2 : available ? Matcha : new Color(0.78f,0.76f,0.71f), () => {
                    if (RegionUnlocked(region)) SwitchRegion(region); else Toast("尚未达到" + region + "的解锁条件");
                });
            TInk(button, active ? Ink : Color.white);
        }
    }

    // ---------- 线路 ----------
    void OpenLinesScreen()
    {
        var pg = OpenPage("线路", "长按地图车站，拖到下一站开线；松手确认。从已有线路的车站起手，默认延长该线，也可另开新线。最多 6 条线路。", "PageArt/line_watercolor");
        StatBox(pg.transform, "每日收入", Mathf.RoundToInt(income) + "", 28, H-400, 320, 210, Gold);
        StatBox(pg.transform, "已连通", ConnectedCount() + "/" + stations.Count, 380, H-400, 320, 210, Matcha);
        StatBox(pg.transform, "钻石", Mathf.FloorToInt(diamonds).ToString(), 732, H-400, 320, 210, Sakura);
        float top = H-700;
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
        var pg = OpenPage(line.name, "客流由可达站点、线路、换乘与运力共同决定；速度、编组和班次会影响出行与运输能力。", "PageArt/line_watercolor");
        pageBack = back ?? OpenLinesScreen;
        StatBox(pg.transform, "客流", Mathf.RoundToInt(LinePassengerFlow(line)) + "", 28, H-440, 320, 200, Matcha);
        StatBox(pg.transform, "运力", Mathf.RoundToInt(LineCapacity(line)) + " 人/日", 380, H-440, 320, 200, Indigo);
        StatBox(pg.transform, "每日收入", Mathf.RoundToInt(LineDaily(line)) + "", 732, H-440, 320, 200, Gold);
        string[] ups = { "车速", "编组", "班次" };
        for (int k = 0; k < 3; k++)
        {
            int kk = k;
            var row = Rect("UR"+k, pg.transform, 28, H-730 - k*150, W-56, 140);
            Rounded(row, new Color(1,1,1,0.94f)); SoftShadow(row);
            Txt(row.transform, ups[k], 32, Ink, 28, 84, 300, 44, TextAnchor.MiddleLeft);
            Txt(row.transform, "Lv." + line.up[k], 30, Indigo, 28, 34, 300, 40, TextAnchor.MiddleLeft);
            int lv = line.up[k];
            int cost = Mathf.RoundToInt((136 + k*70) * Mathf.Pow(1.6f, lv));
            Btn(row.transform, "Buy", "升级  ¥" + cost, 28, W-56-28-230, 26, 230, 88, Matcha, () => {
                if (coins < cost) { Toast("金币不足"); return; }
                BeginUndo("升级线路"); coins -= cost; line.up[kk]++; RecalcIncome(); EndUndo(); RefreshHUD();
                ClosePage(); OpenLineScreen(id, back);
            });
        }
        Txt(pg.transform, "沿线", 28, InkSoft, 28, H-1230, 160, 38, TextAnchor.MiddleLeft);
        Txt(pg.transform, UniquePathText(line.st), 26, Ink, 28, H-1290, W-56, 60, TextAnchor.UpperLeft);
        Btn(pg.transform, "Add", "机车商店", 30, 28, 56, 490, 124, Indigo, () => { fleetBack = () => OpenLineScreen(id, back); OpenLocoShop(); });
        Btn(pg.transform, "Del", "拆除线路", 30, 544, 56, 508, 124, Vermilion, () => {
            Confirm("拆除 " + line.name, "退还 ¥" + Mathf.RoundToInt(120f*(UniqueCount(line.st)-1)*0.6f), () => {
                BeginUndo("拆除整条线路"); coins += 120f*(UniqueCount(line.st)-1)*0.6f; lines.Remove(line);
                CreateLineVisuals(); RefreshStationDots(); RecalcIncome(); EndUndo(); RefreshHUD(); ClosePanel();
                ClosePage(); OpenLinesScreen();
            });
        });
    }

    // ---------- 调度 / 车库 / 商店 ----------
    int fleetPowerFilter, fleetUseFilter, fleetRegionFilter;
    Action fleetBack;
    static readonly string[] FleetPowerFilters = { "全部动力", "蒸汽", "内燃", "电力", "氢能", "双动力", "其他" };
    static readonly string[] FleetUseFilters = { "全部用途", "客运优先", "货运优先", "客货兼顾" };
    static readonly string[] FleetRegionFilters = { "全部地区", "英国", "美国", "德国", "印度" };
    // 分类沿用一代目录的用途划分；地区表示目录展示地区。
    static readonly int[] FleetUses = { 3, 2, 3, 3, 3, 2, 3, 3, 1 };
    static readonly int[] FleetRegions = { 1, 1, 1, 1, 1, 2, 3, 4, 2 };
    void OpenGarageScreen() { fleetBack = null; OpenFleetTab(0); }
    void OpenResearchInventory() { OpenFleetTab(1); }
    void OpenLocoShop() { OpenFleetTab(2); }
    bool FleetMatches(int index)
    {
        return (fleetPowerFilter == 0 || fleetPowerFilter == 1) &&
               (fleetUseFilter == 0 || FleetUses[index] == fleetUseFilter) &&
               (fleetRegionFilter == 0 || FleetRegions[index] == fleetRegionFilter);
    }
    void OpenFleetTab(int tab)
    {
        tab = Mathf.Clamp(tab, 0, 2);
        string[] titles = { "调度", "车库", "商店" };
        string[] help = {
            "管理线路并升级车速、编组和班次。车辆配置可在车库中调整。",
            "按动力、用途和地区筛选已拥有的机车，并将机车配置给线路。当前配置仅作展示。",
            "按动力、用途和地区筛选车型。机车须先在科技树解锁；研究完成赠送一辆。"
        };
        string[] backgrounds = { "dispatch_watercolor", "garage_watercolor", "shop_watercolor" };
        var pg = OpenPage(titles[tab], help[tab], "FleetArt/" + backgrounds[tab]);
        pageBack = fleetBack;
        if (tab == 0) BuildFleetDispatch(pg.transform);
        else BuildFleetCollection(pg.transform, tab);
        BuildFleetNavigation(pg.transform, tab);
    }
    void BuildFleetNavigation(Transform page, int selected)
    {
        var bar = Rect("FleetBottomNav", page, 0, 0, W, 154);
        Rounded(bar, new Color(0.985f, 0.975f, 0.95f, 1f)); SoftShadow(bar);
        bar.AddComponent<FleetBottomEdge>();
        string[] labels = { "调度", "车库", "商店" };
        string[] subtitles = { "DISPATCH", "GARAGE", "SHOP" };
        for (int i = 0; i < labels.Length; i++)
        {
            int target = i;
            var item = Rect("Nav" + i, bar.transform, i * 360, 0, 360, 154);
            var hit = Img(item, new Color(1, 1, 1, 0));
            if (i == selected) Img(Rect("Selected", item.transform, 105, 144, 150, 6), Gold, spriteRound).raycastTarget = false;
            Txt(item.transform, labels[i], 34, i == selected ? Ink : InkSoft, 0, 57, 360, 52, TextAnchor.MiddleCenter);
            Txt(item.transform, subtitles[i], 15, InkSoft, 0, 28, 360, 24, TextAnchor.MiddleCenter);
            var button = item.AddComponent<Button>(); button.targetGraphic = hit;
            button.onClick.AddListener(() => { if (target != selected) OpenFleetTab(target); });
            item.AddComponent<FleetPressFeedback>();
        }
    }
    RectTransform FleetScroll(Transform page, float bottom, float top, float requiredHeight)
    {
        float viewHeight = top - bottom;
        var view = Rect("FleetViewport", page, 28, bottom, W - 56, viewHeight);
        Img(view, new Color(1, 1, 1, 0.01f));
        var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true;
        scroll.inertia = true; scroll.decelerationRate = 0.12f; scroll.scrollSensitivity = 110f; scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.viewport = view.GetComponent<RectTransform>();
        float height = Mathf.Max(viewHeight, requiredHeight);
        var content = Rect("FleetContent", view.transform, 0, 0, W - 56, height);
        var rt = content.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1); rt.anchoredPosition = Vector2.zero; scroll.content = rt;
        return rt;
    }
    void BuildFleetDispatch(Transform page)
    {
        var content = FleetScroll(page, 172, H - 182, Mathf.Max(1, lines.Count) * 360f + 24);
        float height = content.sizeDelta.y;
        if (lines.Count == 0) Txt(content, "还没有运营中的线路", 30, InkSoft, 20, height - 180, W - 96, 80, TextAnchor.MiddleCenter);
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var card = Rect("Dispatch" + i, content, 0, height - 350 - i * 360, W - 56, 340);
            Rounded(card, new Color(1, 1, 1, 0.97f)); SoftShadow(card);
            Img(Rect("Dot", card.transform, 28, 262, 38, 38), line.color, spriteCircle).raycastTarget = false;
            Txt(card.transform, line.name, 36, Ink, 84, 260, 700, 48, TextAnchor.MiddleLeft);
            var lineStats = Txt(card.transform, UniqueCount(line.st) + " 站 · " + line.trains + "/8 列 · 每日收入 " + Mathf.RoundToInt(LineDaily(line)), 24, InkSoft, 28, 205, 940, 38, TextAnchor.MiddleLeft);
            var assigned = researchVehicles.Find(v => v.lineId == line.id);
            Txt(card.transform, "配置：" + (assigned == null ? "默认机车" : LocoNames[assigned.node]), 24, InkSoft, 28, 156, 940, 34, TextAnchor.MiddleLeft);
            string[] labels = { "车速", "编组", "班次" };
            for (int k = 0; k < 3; k++)
            {
                int upgrade = k;
                int cost = Mathf.RoundToInt((136 + k * 70) * Mathf.Pow(1.6f, line.up[k]));
                var btn = Btn(card.transform, "Upgrade" + k, labels[k] + " Lv." + line.up[k] + "  ¥" + cost, 24,
                    28 + k * 316, 38, 292, 88, Matcha, () => {
                        int amount = Mathf.RoundToInt((136 + upgrade * 70) * Mathf.Pow(1.6f, line.up[upgrade]));
                        if (coins < amount) { Toast("金币不足"); return; }
                        BeginUndo("升级线路"); coins -= amount; line.up[upgrade]++; RecalcIncome(); EndUndo(); RefreshHUD();
                        lineStats.text = UniqueCount(line.st) + " 站 · " + line.trains + "/8 列 · 每日收入 " + Mathf.RoundToInt(LineDaily(line));
                        int next = Mathf.RoundToInt((136 + upgrade * 70) * Mathf.Pow(1.6f, line.up[upgrade]));
                        var buttonText = card.transform.Find("Upgrade" + upgrade).GetComponentInChildren<Text>();
                        buttonText.text = labels[upgrade] + " Lv." + line.up[upgrade] + "  ¥" + next;
                        Toast(line.name + " · " + labels[upgrade] + "升级成功");
                    });
                btn.gameObject.AddComponent<FleetPressFeedback>();
            }
            var entry = card.AddComponent<FleetCardEntrance>(); entry.delay = Mathf.Min(i, 5) * 0.035f;
        }
    }
    void BuildFleetFilters(Transform page, int tab)
    {
        Button power = null, use = null, region = null;
        power = Btn(page, "PowerFilter", "动力：" + FleetPowerFilters[fleetPowerFilter] + "  ▾", 23, 28, H - 285, 326, 72,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(page, power.transform, FleetPowerFilters, fleetPowerFilter, value => { fleetPowerFilter = value; OpenFleetTab(tab); }));
        use = Btn(page, "UseFilter", "用途：" + FleetUseFilters[fleetUseFilter] + "  ▾", 23, 377, H - 285, 326, 72,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(page, use.transform, FleetUseFilters, fleetUseFilter, value => { fleetUseFilter = value; OpenFleetTab(tab); }));
        region = Btn(page, "RegionFilter", "地区：" + FleetRegionFilters[fleetRegionFilter] + "  ▾", 23, 726, H - 285, 326, 72,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(page, region.transform, FleetRegionFilters, fleetRegionFilter, value => { fleetRegionFilter = value; OpenFleetTab(tab); }));
        TInk(power, Ink); TInk(use, Ink); TInk(region, Ink);
        power.gameObject.AddComponent<FleetPressFeedback>(); use.gameObject.AddComponent<FleetPressFeedback>(); region.gameObject.AddComponent<FleetPressFeedback>();
    }
    void ShowFleetDropdown(Transform page, Transform source, string[] options, int selected, Action<int> choose)
    {
        var old = page.Find("FleetDropdownOverlay");
        if (old) Destroy(old.gameObject);
        var sourceRect = (RectTransform)source;
        var corners = new Vector3[4]; sourceRect.GetWorldCorners(corners);
        var screen = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)page, screen, null, out var point)) return;
        var overlay = Rect("FleetDropdownOverlay", page, 0, 0, W, H);
        var overlayImage = Img(overlay, new Color(0, 0, 0, 0));
        var dismiss = overlay.AddComponent<Button>(); dismiss.targetGraphic = overlayImage;
        dismiss.transition = Selectable.Transition.None; dismiss.onClick.AddListener(() => Destroy(overlay));
        float width = 326, rowHeight = 70, height = options.Length * rowHeight + 16;
        float x = Mathf.Clamp(point.x, 12, W - width - 12);
        float y = Mathf.Clamp(point.y - height - 8, 160, H - height - 150);
        var list = Rect("Options", overlay.transform, x, y, width, height);
        Rounded(list, new Color(0.99f, 0.98f, 0.95f, 1)); SoftShadow(list);
        for (int i = 0; i < options.Length; i++)
        {
            int index = i;
            var color = i == selected ? new Color(0.91f, 0.91f, 0.83f, 1) : new Color(1, 1, 1, 0.98f);
            var button = Btn(list.transform, "Option" + i, options[i] + (i == selected ? "  ✓" : ""), 24,
                8, height - 8 - (i + 1) * rowHeight, width - 16, rowHeight - 4, color, () => { Destroy(overlay); choose(index); });
            TInk(button, Ink); button.gameObject.AddComponent<FleetPressFeedback>();
        }
    }
    void BuildFleetCollection(Transform page, int tab)
    {
        BuildFleetFilters(page, tab);
        var visibleItems = new List<ResearchVehicle>();
        var visibleModels = new List<int>();
        if (tab == 1)
        {
            foreach (var item in researchVehicles) if (FleetMatches(item.node)) visibleItems.Add(item);
        }
        else for (int i = 0; i < LocoNames.Length; i++) if (FleetMatches(i)) visibleModels.Add(i);
        int count = tab == 1 ? visibleItems.Count : visibleModels.Count;
        Txt(page, tab == 1 ? "已拥有  " + count + " 辆" : "车型  " + count + " 款", 24, InkSoft, 28, H - 338, 900, 34, TextAnchor.MiddleLeft);
        var content = FleetScroll(page, 172, H - 354, Mathf.Max(1, count) * 218f + 20);
        float height = content.sizeDelta.y;
        if (count == 0)
        {
            Txt(content, tab == 1 ? "暂无符合条件的机车\n解锁科技后可获得机车" : "暂无符合条件的车型", 29, InkSoft, 40, height - 240, W - 136, 150, TextAnchor.MiddleCenter);
            return;
        }
        for (int i = 0; i < count; i++)
        {
            var item = tab == 1 ? visibleItems[i] : null;
            int index = tab == 1 ? item.node : visibleModels[i];
            bool unlocked = techDone.Contains(LocoKey(index));
            var assigned = item == null ? null : lines.Find(l => l.id == item.lineId);
            if (item != null && assigned == null) item.lineId = 0;
            var card = Rect("Vehicle" + i, content, 0, height - 208 - i * 218, W - 56, 198);
            Rounded(card, new Color(1, 1, 1, 0.97f)); SoftShadow(card);
            var art = Rect("TrainArt", card.transform, 16, 35, 270, 116).AddComponent<RawImage>();
            art.texture = Resources.Load<Texture2D>("Research/Train/" + LocoArt[index]); art.raycastTarget = false;
            if (art.texture)
            {
                float ratio = (float)art.texture.width / art.texture.height;
                float aw = Mathf.Min(270, 116 * ratio), ah = aw / ratio;
                art.rectTransform.sizeDelta = new Vector2(aw, ah);
                art.rectTransform.anchoredPosition = new Vector2(16 + (270 - aw) / 2, 35 + (116 - ah) / 2);
                art.color = tab == 2 && !unlocked ? new Color(0.65f, 0.65f, 0.65f, 0.75f) : Color.white;
            }
            Txt(card.transform, LocoNames[index], 31, Ink, 304, 135, 430, 42, TextAnchor.MiddleLeft);
            Txt(card.transform, LocoYears[index] + " · 蒸汽 · " + FleetUseFilters[FleetUses[index]], 21, InkSoft, 304, 96, 430, 30, TextAnchor.MiddleLeft);
            string state = tab == 1 ? (assigned == null ? "未配置" : "已配置至 " + assigned.name) : unlocked ? "已解锁" : "科技树未解锁";
            Txt(card.transform, state, 22, InkSoft, 304, 48, 400, 32, TextAnchor.MiddleLeft);
            var button = Btn(card.transform, "Action", tab == 1 ? "配置线路" : unlocked ? "购买 ¥" + LocoPrice(index) : "未解锁", 23,
                758, 54, 234, 90, new Color(0.92f, 0.89f, 0.83f), () => {
                    if (tab == 1) ChooseResearchVehicleLine(item);
                    else PurchaseLoco(index);
                });
            TInk(button, Ink); button.gameObject.AddComponent<FleetPressFeedback>();
            var entry = card.AddComponent<FleetCardEntrance>(); entry.delay = Mathf.Min(i, 5) * 0.035f;
        }
    }
    void ChooseResearchVehicleLine(ResearchVehicle item)
    {
        float h = 230 + lines.Count * 78;
        var bubble = OpenBubble("配置 " + LocoNames[item.node], h, 760, new Vector2(W / 2, H / 2));
        if (lines.Count == 0) Txt(bubble.transform, "请先开通线路", 27, InkSoft, 24, 80, 710, 50, TextAnchor.MiddleCenter);
        for (int i = 0; i < lines.Count; i++)
        {
            var target = lines[i];
            var button = Btn(bubble.transform, "Line" + target.id, target.name, 25, 30, h - 154 - i * 78, 700, 66, new Color(0.92f, 0.89f, 0.83f), () => {
                BeginUndo("配置机车"); foreach (var owned in researchVehicles) if (owned != item && owned.lineId == target.id) owned.lineId = 0;
                item.lineId = target.id; EndUndo(); ClosePanel(); OpenResearchInventory(); Toast("已配置到 " + target.name);
            });
            TInk(button, Ink);
        }
    }

    // ---------- 公司 ----------
    void OpenCompanyScreen()
    {
        OpenPage("公司", "公司系统筹备中：总部、员工、评级与全球区域将在这里展开。", "PageArt/company_watercolor");
    }

    // ---------- 图鉴 ----------
    int sceneryCategory;
    static readonly string[] SceneryCategories = { "全部名胜", "自然景观", "人文景观" };
    void OpenSceneryScreen()
    {
        var pg = OpenPage("图鉴", "连通名胜绑定的车站即可解锁；有大图的名胜首次打开大图时打卡。", "PageArt/album_watercolor");
        Button filter = null;
        filter = Btn(pg.transform, "SceneryFilter", "分类：" + SceneryCategories[sceneryCategory] + "  ▾", 25, 28, H-275, 326, 72,
            new Color(0.93f,0.90f,0.84f), () => ShowFleetDropdown(pg.transform, filter.transform, SceneryCategories, sceneryCategory, value => { sceneryCategory = value; OpenSceneryScreen(); }));
        TInk(filter, Ink); filter.gameObject.AddComponent<FleetPressFeedback>();
        int count = 0;
        foreach (var item in sceneries) if (sceneryCategory == 0 || (sceneryCategory == 1 && item.natural) || (sceneryCategory == 2 && !item.natural)) count++;
        var content = FleetScroll(pg.transform, 72, H - 300, Mathf.CeilToInt(count / 2f) * 300f + 16);
        float contentHeight = content.sizeDelta.y;
        int visible = 0;
        for (int i = 0; i < sceneries.Count; i++)
        {
            var sc = sceneries[i];
            if (sceneryCategory == 1 && !sc.natural || sceneryCategory == 2 && sc.natural) continue;
            bool pending = pendingCheckin.Contains(sc);
            bool hasPicture = TryGetSceneryContent(sc, out _, out _);
            var card = Rect("EC"+i, content, (visible%2)*516, contentHeight - 280 - (visible/2)*300, 500, 270);
            visible++;
            Rounded(card, sc.found ? new Color(0.91f,0.95f,0.85f,0.97f) : pending ? new Color(0.99f,0.91f,0.87f,0.98f) : new Color(1,1,1,0.96f)); SoftShadow(card);
            Txt(card.transform, sc.found ? "★" : pending ? "●" : "○", 44, sc.found ? Gold : pending ? Vermilion : InkSoft, 24, 176, 70, 56, TextAnchor.MiddleLeft);
            Txt(card.transform, sc.name, 36, Ink, 98, 178, 380, 50, TextAnchor.MiddleLeft);
            Txt(card.transform, sc.region + (sc.natural ? " · 自然景观" : " · 人文景观"), 26, InkSoft, 98, 138, 380, 34, TextAnchor.MiddleLeft);
            Txt(card.transform, sc.found ? "已打卡 · 钻石 +20" : !RegionUnlocked(sc.region) ? "先解锁" + sc.region + "区域" : !sc.unlocked ? "连通" + sc.triggerStation + "站解锁" : hasPicture ? "点击查看大图并打卡" : "已解锁 · 大图筹备中", 24, sc.found ? Matcha : pending ? Vermilion : InkSoft, 24, 50, 450, 34, TextAnchor.MiddleLeft);
            if (sc.unlocked && hasPicture)
            {
                var scLocal = sc;
                var btn = card.AddComponent<Button>(); btn.targetGraphic = card.GetComponent<Image>();
                btn.onClick.AddListener(() => OpenSceneryViewer(scLocal, () => { ClosePage(); OpenSceneryScreen(); }));
            }
        }
    }
}
