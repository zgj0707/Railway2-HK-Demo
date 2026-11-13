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
        float bodyHeight = Mathf.Clamp(MeasureTextHeight(text, 27, 572) + 10, 50, H - 420);
        var p = OpenBubble("说明", bodyHeight + 220, 640, new Vector2(130, H - 240));
        ScrollableText(p.transform, text, 27, InkSoft, 34, 128, 572, bodyHeight);
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
        var pg = OpenPage("切换区域", "地区依次解锁。每区线路上限为该区站点总数的一半，向下取整；交界站计入所连接的地区，跨区线路在经过的地区各占一个名额。", "PageArt/line_watercolor");
        string[] regions = { "香港", "澳门", "广东" };
        for (int i = 0; i < regions.Length; i++)
        {
            string region = regions[i]; bool available = RegionUnlocked(region); bool active = region == currentRegion;
            var card = Rect("Region_" + region, pg.transform, 28, H - 450 - i * 250, W - 56, 220);
            Rounded(card, new Color(1, 1, 1, 0.95f)); SoftShadow(card);
            Img(Rect("Accent", card.transform, 0, 30, 7, 160), active ? Gold : available ? Matcha : InkSoft, spriteRound).raycastTarget = false;
            Txt(card.transform, region, 42, Ink, 32, 142, 380, 55, TextAnchor.MiddleLeft);
            Txt(card.transform, "线路 " + RegionLineCount(region) + " / " + RegionLineLimit(region), 24, InkSoft, 450, 150, 280, 40, TextAnchor.MiddleLeft);
            string condition = active ? "当前区域" : available ? "已开放" : region == "澳门"
                ? "香港普通站 " + Mathf.Min(4, ConnectedOrdinaryStations("香港")) + "/4 · 预计日利润 ¥" + Mathf.RoundToInt(income) + "/600"
                : "开放澳门 · 澳门普通站 " + Mathf.Min(2, ConnectedOrdinaryStations("澳门")) + "/2 · 预计日利润 ¥" + Mathf.RoundToInt(income) + "/1200";
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
        var pg = OpenPage("线路", "长按地图车站，拖到下一站开线；松手确认。从已有线路的车站起手，端点默认延长，中间站默认新建。每个地区的线路上限为当地站点总数的一半。", "PageArt/line_watercolor");
        StatBox(pg.transform, "预计日利润", Mathf.RoundToInt(income) + "", 28, H-400, 320, 210, Gold);
        StatBox(pg.transform, "已连通", ConnectedCount() + "/" + stations.Count, 380, H-400, 320, 210, Matcha);
        StatBox(pg.transform, "钻石", Mathf.FloorToInt(diamonds).ToString(), 732, H-400, 320, 210, Sakura);
        var regionalLines = lines.FindAll(line => PathTouchesRegion(line.st, currentRegion));
        Txt(pg.transform, currentRegion + "线路 " + regionalLines.Count + " / " + LineLimit() + " · 当前时段日化旅次 " + Mathf.RoundToInt(traffic != null ? traffic.totalPassengers : 0f), 24, InkSoft, 28, H-465, 1000, 40, TextAnchor.MiddleLeft);
        var list = FleetScroll(pg.transform, 36, H - 490, Mathf.Max(1, regionalLines.Count) * 188f + 24);
        float top = list.sizeDelta.y - 184f;
        for (int i = 0; i < regionalLines.Count; i++)
        {
            var line = regionalLines[i]; int id = line.id;
            var card = Rect("LC"+i, list, 0, top - i*188, W-56, 172);
            Rounded(card, new Color(1,1,1,0.95f)); SoftShadow(card);
            var dot = Rect("D", card.transform, 26, 64, 44, 44); Img(dot, line.color, spriteCircle);
            Txt(card.transform, line.name, 40, Ink, 92, 100, 340, 52, TextAnchor.MiddleLeft);
            Txt(card.transform, UniqueCount(line.st) + " 站 · 预计利润 " + Mathf.RoundToInt(LineDaily(line)) + "/天", 26, InkSoft, 94, 48, 440, 36, TextAnchor.MiddleLeft);
            var gb = Btn(card.transform, "Go", "查看", 32, W-56-236, 42, 196, 88, new Color(0.92f,0.89f,0.83f), () => OpenLineScreen(id, OpenLinesScreen));
            TInk(gb, Ink);
        }
    }

    float LineDaily(Line line) { return ForecastFor(line).Profit; }

    // ---------- 线路详情 ----------
    void OpenLineScreen(int id, Action back = null)
    {
        var line = lines.Find(l => l.id == id);
        if (line == null) { OpenLinesScreen(); return; }
        var pg = OpenPage(line.name, "客流由可达站点、线路、换乘与运力共同决定；车辆性能与时段调度会影响出行和运输能力。", "PageArt/line_watercolor");
        pageBack = back ?? OpenLinesScreen;
        StatBox(pg.transform, "客流", Mathf.RoundToInt(LinePassengerFlow(line)) + "", 28, H-440, 320, 200, Matcha);
        StatBox(pg.transform, "单向区段运力", Mathf.RoundToInt(LineCapacity(line)) + " 人次/日", 380, H-440, 320, 200, Indigo);
        StatBox(pg.transform, "预计日利润", Mathf.RoundToInt(LineDaily(line)) + "", 732, H-440, 320, 200, Gold);
        var fleet = Rect("FleetSummary", pg.transform, 28, H-710, W-56, 190); Rounded(fleet, new Color(1,1,1,0.94f)); SoftShadow(fleet);
        Txt(fleet.transform, "配属 " + line.trains + " 列 · 平均速度 " + Mathf.RoundToInt(FleetTrait(line,2)*100) + "%", 28, Ink, 24, 115, 950, 45, TextAnchor.MiddleLeft);
        Txt(fleet.transform, "单区段运力相加；乘客上下车可复用座位，速度和机车成本取平均。", 21, InkSoft, 24, 70, 950, 35, TextAnchor.MiddleLeft);
        var fleetButton=Btn(fleet.transform,"Fleet","机车配属",25,24,8,450,55,Indigo,()=>OpenLineFleet(line));TInk(fleetButton,Color.white);
        var operations=Btn(fleet.transform,"Operations","票价与调度",25,495,8,450,55,Indigo,()=>OpenLineOperations(line));TInk(operations,Color.white);
        Txt(pg.transform, "沿线", 28, InkSoft, 28, H-800, 160, 38, TextAnchor.MiddleLeft);
        Txt(pg.transform, UniquePathText(line.st), 26, Ink, 28, H-870, W-56, 60, TextAnchor.UpperLeft);
        var colors=Btn(pg.transform,"LineSkins","线路涂装 · 金币",25,28,H-990,W-56,78,Paper2,()=>OpenLineSkins(line));TInk(colors,Ink);
        Btn(pg.transform, "Add", "机车商店", 30, 28, 56, 490, 124, Indigo, () => { fleetBack = () => OpenLineScreen(id, back); OpenLocoShop(); });
        Btn(pg.transform, "Del", "拆除线路", 30, 544, 56, 508, 124, Vermilion, () => {
            Confirm("拆除 " + line.name, "退还 ¥" + Mathf.RoundToInt(120f*(UniqueCount(line.st)-1)*0.6f), () => {
                BeginUndo("拆除整条线路"); coins += 120f*(UniqueCount(line.st)-1)*0.6f; SettleRemovedLineTax(line); ReleaseLineFleet(line); lines.Remove(line);
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
            "管理线路机车配属与分时调度；车辆性能由配属机车决定。",
            "筛选并查看已拥有的机车，也可将它们配置给线路。",
            "点击车型查看详情。机车须先在科技树解锁；研究完成赠送一辆。"
        };
        string[] backgrounds = { "dispatch_watercolor", "garage_watercolor", "shop_watercolor" };
        var pg = OpenPage(titles[tab], help[tab], "FleetArt/" + backgrounds[tab]);
        pageBack = fleetBack;
        if (tab == 0) BuildFleetDispatch(pg.transform);
        else BuildFleetCollection(pg.transform, tab);
        if (tab == 2) BuildShopSidebar(pg.transform);
        BuildFleetNavigation(pg.transform, tab);
    }
    void BuildShopSidebar(Transform page)
    {
        var rail = Rect("ShopSidebar", page, 28, H - 560, 136, 206);
        Rounded(rail, new Color(1f,0.985f,0.95f,0.92f)); SoftShadow(rail);
        var loco = Btn(rail.transform, "Locomotives", "机车", 26, 6, 108, 124, 88, new Color(0.86f,0.88f,0.79f,0.98f), () => { }); TInk(loco, Ink);
        var skin = Btn(rail.transform, "Liveries", "涂装", 26, 6, 10, 124, 88, new Color(0.96f,0.94f,0.88f,0.98f), () => Toast("尚未开放")); TInk(skin, InkSoft);
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
    RectTransform FleetScroll(Transform page, float bottom, float top, float requiredHeight, float leftInset = 0f)
    {
        float viewHeight = top - bottom;
        float width = W - 56 - leftInset;
        var view = Rect("FleetViewport", page, 28 + leftInset, bottom, width, viewHeight);
        Img(view, new Color(1, 1, 1, 0.01f));
        var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true;
        scroll.inertia = true; scroll.decelerationRate = 0.12f; scroll.scrollSensitivity = 110f; scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.viewport = view.GetComponent<RectTransform>();
        float height = Mathf.Max(viewHeight, requiredHeight);
        var content = Rect("FleetContent", view.transform, 0, 0, width, height);
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
            var lineStats = Txt(card.transform, UniqueCount(line.st) + " 站 · " + line.trains + " 列 · 预计日利润 " + Mathf.RoundToInt(LineDaily(line)), 24, InkSoft, 28, 205, 940, 38, TextAnchor.MiddleLeft);
            var assigned = researchVehicles.Find(v => v.lineId == line.id);
            Txt(card.transform, "配置：" + (assigned == null ? "默认机车" : LocoNames[assigned.node]), 24, InkSoft, 28, 156, 940, 34, TextAnchor.MiddleLeft);
            var fleetButton = Btn(card.transform, "Fleet", "机车配属", 25, 28, 38, 450, 88, Indigo, () => OpenLineFleet(line)); TInk(fleetButton, Color.white);
            var operations = Btn(card.transform, "Operations", "票价与调度", 25, 495, 38, 450, 88, Indigo, () => OpenLineOperations(line)); TInk(operations, Color.white);
            if (qualityTier > 0) { var entry = card.AddComponent<FleetCardEntrance>(); entry.delay = Mathf.Min(i, 5) * 0.035f; }
        }
    }
    void BuildFleetFilters(Transform page, int tab)
    {
        Button power = null, use = null, region = null;
        float left = tab == 2 ? 178 : 28, width = tab == 2 ? 282 : 326, gap = tab == 2 ? 14 : 23;
        power = Btn(page, "PowerFilter", "动力：" + FleetPowerFilters[fleetPowerFilter] + "  ▾", 22, left, H - 285, width, 72,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(page, power.transform, FleetPowerFilters, fleetPowerFilter, value => { fleetPowerFilter = value; OpenFleetTab(tab); }));
        use = Btn(page, "UseFilter", "用途：" + FleetUseFilters[fleetUseFilter] + "  ▾", 22, left + width + gap, H - 285, width, 72,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(page, use.transform, FleetUseFilters, fleetUseFilter, value => { fleetUseFilter = value; OpenFleetTab(tab); }));
        region = Btn(page, "RegionFilter", "地区：" + FleetRegionFilters[fleetRegionFilter] + "  ▾", 22, left + 2 * (width + gap), H - 285, width, 72,
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
        float inset = tab == 2 ? 150 : 0;
        Txt(page, tab == 1 ? "已拥有  " + count + " 辆 · 点击车卡查看详情" : "车型  " + count + " 款 · 点击车卡查看详情", 24, InkSoft, 28 + inset, H - 338, 850 - inset, 34, TextAnchor.MiddleLeft);
        var content = FleetScroll(page, 172, H - 354, Mathf.Max(1, count) * 218f + 20, inset);
        float height = content.sizeDelta.y;
        float cardWidth = content.sizeDelta.x, artWidth = tab == 2 ? 210f : 270f;
        float textX = tab == 2 ? 240f : 304f, actionWidth = tab == 2 ? 180f : 234f;
        float actionX = cardWidth - actionWidth - 32f, infoWidth = actionX - textX - 12f;
        if (count == 0)
        {
            Txt(content, tab == 1 ? "暂无符合条件的机车\n解锁科技后可获得机车" : "暂无符合条件的车型", 29, InkSoft, 40, height - 240, cardWidth - 80, 150, TextAnchor.MiddleCenter);
            return;
        }
        for (int i = 0; i < count; i++)
        {
            var item = tab == 1 ? visibleItems[i] : null;
            int index = tab == 1 ? item.node : visibleModels[i];
            bool unlocked = techDone.Contains(LocoKey(index));
            var assigned = item == null ? null : lines.Find(l => l.id == item.lineId);
            if (item != null && assigned == null) item.lineId = 0;
            var card = Rect("Vehicle" + i, content, 0, height - 208 - i * 218, cardWidth, 198);
            Rounded(card, new Color(1, 1, 1, 0.97f)); SoftShadow(card);
            var cardButton = card.AddComponent<Button>(); cardButton.targetGraphic = card.GetComponent<Image>();
            cardButton.onClick.AddListener(() => OpenVehicleDetails(index));
            var art = Rect("TrainArt", card.transform, 16, 35, artWidth, 116).AddComponent<RawImage>();
            art.texture = Resources.Load<Texture2D>("Research/Train/" + LocoArt[index]); art.raycastTarget = false;
            if (art.texture)
            {
                float ratio = (float)art.texture.width / art.texture.height;
                float aw = Mathf.Min(artWidth, 116 * ratio), ah = aw / ratio;
                art.rectTransform.sizeDelta = new Vector2(aw, ah);
                art.rectTransform.anchoredPosition = new Vector2(16 + (artWidth - aw) / 2, 35 + (116 - ah) / 2);
                art.color = tab == 2 && !unlocked ? new Color(0.65f, 0.65f, 0.65f, 0.75f) : Color.white;
            }
            var name = Txt(card.transform, LocoNames[index], 31, Ink, textX, 135, infoWidth, 42, TextAnchor.MiddleLeft); name.resizeTextForBestFit = true; name.resizeTextMinSize = 20; name.resizeTextMaxSize = 31;
            var stats = Txt(card.transform, LocoYears[index] + " · 运力" + Mathf.RoundToInt(FleetCapacityTraits[index]*100) + "% · 维护" + Mathf.RoundToInt(FleetMaintenanceTraits[index]*100) + "% · 速度" + Mathf.RoundToInt(FleetSpeedTraits[index]*100) + "%", 19, InkSoft, textX, 96, infoWidth, 30, TextAnchor.MiddleLeft);
            stats.resizeTextForBestFit = true; stats.resizeTextMinSize = 13; stats.resizeTextMaxSize = 19;
            string state = tab == 1 ? (assigned == null ? "未配置" : "已配置至 " + assigned.name) : unlocked ? "已解锁" : "科技树未解锁";
            Txt(card.transform, state, 22, InkSoft, textX, 48, infoWidth, 32, TextAnchor.MiddleLeft);
            var button = Btn(card.transform, "Action", tab == 1 ? "配置线路" : unlocked ? "购买 ¥" + LocoPrice(index) : "未解锁", 23,
                actionX, 54, actionWidth, 90, new Color(0.92f, 0.89f, 0.83f), () => {
                    if (tab == 1) ChooseResearchVehicleLine(item);
                    else PurchaseLoco(index);
                });
            TInk(button, Ink); button.gameObject.AddComponent<FleetPressFeedback>();
            if (qualityTier > 0) { var entry = card.AddComponent<FleetCardEntrance>(); entry.delay = Mathf.Min(i, 5) * 0.035f; }
        }
    }
    void ChooseResearchVehicleLine(ResearchVehicle item)
    {
        var availableLines = lines.FindAll(line => PathTouchesRegion(line.st, currentRegion));
        int rows = availableLines.Count + (item.lineId != 0 ? 1 : 0);
        float h = 230 + Mathf.Max(1, Mathf.Min(rows, 8)) * 78;
        var bubble = OpenBubble("配置 " + LocoNames[item.node], h, 760, new Vector2(W / 2, H / 2));
        var view = Rect("LineViewport", bubble.transform, 30, 70, 700, h - 200);
        Img(view, new Color(1,1,1,0.01f)); var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 90f;
        scroll.viewport = view.GetComponent<RectTransform>();
        float contentHeight = Mathf.Max(h - 200, rows * 78f);
        var content = Rect("LineOptions", view.transform, 0, 0, 700, contentHeight);
        var contentRt = content.GetComponent<RectTransform>(); contentRt.anchorMin = contentRt.anchorMax = new Vector2(0,1);
        contentRt.pivot = new Vector2(0,1); contentRt.anchoredPosition = Vector2.zero; scroll.content = contentRt;
        if (item.lineId != 0)
        {
            var unassign = Btn(content.transform, "Unassign", "返回车库待命", 25, 0, contentHeight - (availableLines.Count + 1) * 78, 700, 66, Paper2, () => {
                var previous = lines.Find(x => x.id == item.lineId);
                if (previous != null && !PathTouchesRegion(previous.st, currentRegion)) { Toast("请先切换到该线路所在地区调整机车"); return; }
                BeginUndo("机车返回车库");
                item.lineId = 0; if (previous != null) SyncLineFleet(previous); RecalcIncome(); EndUndo();
                ClosePanel(); OpenResearchInventory(); Toast("机车已返回车库，相关调度已调整");
            }); TInk(unassign, Ink);
        }
        if (availableLines.Count == 0) Txt(content.transform, "当前地区还没有线路", 27, InkSoft, 0, contentHeight - 65, 700, 50, TextAnchor.MiddleCenter);
        for (int i = 0; i < availableLines.Count; i++)
        {
            var target = availableLines[i];
            var button = Btn(content.transform, "Line" + target.id, target.name, 25, 0, contentHeight - (i + 1) * 78, 700, 66, new Color(0.92f, 0.89f, 0.83f), () => {
                if (item.lineId != target.id && target.trains >= 8) { Toast("该线路最多配属 8 列车"); return; }
                var previous = lines.Find(x => x.id == item.lineId);
                if (!PathTouchesRegion(target.st, currentRegion) || (previous != null && !PathTouchesRegion(previous.st, currentRegion))) { Toast("请先切换到相关线路所在地区调整机车"); return; }
                BeginUndo("配置机车");
                item.lineId = target.id; if (previous != null) SyncLineFleet(previous); SyncLineFleet(target);
                RecalcIncome(); EndUndo(); ClosePanel(); OpenResearchInventory(); Toast("已配置到 " + target.name);
            });
            TInk(button, Ink);
        }
    }

    static readonly string[] DispatchNames = { "早高峰 06–10", "日间 10–16", "晚高峰 16–20", "夜间 20–06" };
    void SetLineDispatch(Line line, int slot, int amount)
    {
        if (!PathTouchesRegion(line.st, currentRegion)) { Toast("请先切换到该线路所在地区调整调度"); return; }
        if (!techDone.Contains("dispatch_basic")) { Toast("请先研究铁路时刻表"); return; }
        amount = Mathf.Clamp(amount, 0, 8);
        if (amount > line.trains) { OpenLineFleetSupply(line, slot, amount); return; }
        BeginUndo("调整调度");
        if (!techDone.Contains("dispatch_peak")) for (int i=0;i<4;i++) line.dispatch[i]=amount;
        else line.dispatch[slot]=amount;
        RecalcIncome(); EndUndo(); RefreshHUD(); OpenLineOperations(line);
    }
    void ChangeLineFare(Line line, float delta)
    {
        if (!PathTouchesRegion(line.st, currentRegion)) { Toast("请先切换到该线路所在地区调整票价"); return; }
        if (!techDone.Contains("fare_policy")) { Toast("请先研究客运票价制度"); return; }
        BeginUndo("调整票价"); line.fare = Mathf.Clamp(Mathf.Round((line.fare + delta)*10f)/10f,0.5f,3f);
        RecalcIncome(); EndUndo(); RefreshHUD(); OpenLineOperations(line);
    }
    void OpenLineOperations(Line line)
    {
        if (line == null || !lines.Contains(line)) { OpenLinesScreen(); return; }
        var page = OpenPage("票价与调度", "票价影响需求；按时段投放车辆，闲置车辆仍有基础保养费。", "PageArt/line_watercolor");
        pageBack = () => OpenLineScreen(line.id);
        var forecast = ForecastFor(line);
        Txt(page.transform, line.name + " · 配属 " + line.trains + " 列 · 当前开行 " + RunningTrains(line) + " 列", 28, Ink, 32, H-300, 1000, 45, TextAnchor.MiddleLeft);
        Txt(page.transform, "预计日收入 ¥" + Mathf.RoundToInt(forecast.gross) + "    成本 ¥" + Mathf.RoundToInt(forecast.vehicle+forecast.track+forecast.tax) + "    利润 ¥" + Mathf.RoundToInt(forecast.Profit), 24, InkSoft, 32, H-360, 1000, 45, TextAnchor.MiddleLeft);
        var fare = Rect("Fare",page.transform,28,H-535,W-56,145); Rounded(fare,new Color(1,1,1,0.94f));
        Txt(fare.transform, "起步票价  ¥" + line.fare.ToString("0.0") + " / 人", 30, Ink, 26, 72, 570, 44, TextAnchor.MiddleLeft);
        Txt(fare.transform, techDone.Contains("fare_policy") ? "票价另加里程费；票款每 10 元折合 1 游戏金币" : "研究「客运票价制度」解锁调价", 20, InkSoft, 26, 25, 650, 34, TextAnchor.MiddleLeft);
        var fm=Btn(fare.transform,"FareMinus","−",35,720,30,110,80,Paper2,()=>ChangeLineFare(line,-0.1f));TInk(fm,Ink);
        var fp=Btn(fare.transform,"FarePlus","+",35,850,30,110,80,Paper2,()=>ChangeLineFare(line,0.1f));TInk(fp,Ink);
        bool basic=techDone.Contains("dispatch_basic"), peak=techDone.Contains("dispatch_peak");
        Txt(page.transform, !basic ? "研究「铁路时刻表」解锁统一调度" : !peak ? "统一调度已解锁；研究「分时列车计划」可分别设置" : "分时调度 · 高峰与夜间可分别设置", 23, InkSoft, 32, H-590, 1000, 36, TextAnchor.MiddleLeft);
        int rows=peak?4:1;
        for(int i=0;i<rows;i++) {
            int slot=i; var row=Rect("DispatchSlot"+i,page.transform,28,H-740-i*130,W-56,115); Rounded(row,new Color(1,1,1,0.94f));
            Txt(row.transform,peak?DispatchNames[i]:"全天统一",26,Ink,24,68,400,36,TextAnchor.MiddleLeft);
            Txt(row.transform,line.dispatch[i]+" 列",30,Indigo,510,44,130,40,TextAnchor.MiddleCenter);
            int sampleSlot = peak ? i : DispatchSlot();
            float oldClock = dayClock;
            float[] sampleHours = {8f,13f,18f,1f};
            try {
                dayClock = sampleHours[sampleSlot] * GameDaySeconds / 24f;
                var sample = trafficSlots[sampleSlot];
                float requested = sample != null && sample.linePeakDemand.TryGetValue(line, out var d) ? d : 0f;
                float gross = sample != null ? TrafficRevenue(line,sample) : 0f;
                Txt(row.transform, "最高区段需求 " + Mathf.RoundToInt(requested) + " · 运力 " + Mathf.RoundToInt(LineCapacity(line)) + " · 收入 ¥" + Mathf.RoundToInt(gross) + " · 运维 ¥" + Mathf.RoundToInt(VehicleMaintenance(line)+TrackMaintenance(line)), 18, InkSoft, 24, 12, 670, 34, TextAnchor.MiddleLeft);
            } finally { dayClock = oldClock; }
            var minus=Btn(row.transform,"Minus","−",34,720,18,110,80,Paper2,()=>SetLineDispatch(line,slot,line.dispatch[slot]-1)); TInk(minus,Ink);
            var plus=Btn(row.transform,"Plus","+",34,850,18,110,80,Paper2,()=>SetLineDispatch(line,slot,line.dispatch[slot]+1)); TInk(plus,Ink);
        }
        Txt(page.transform, "研究机车优先运行；速度和机车成本按全线配属车辆平均。\n各时段数据按全天折算；运维不含税，税在每日结算。", 22, InkSoft, 32, H-1320, 1000, 100, TextAnchor.UpperLeft);
    }
    void OpenLineFleet(Line line)
    {
        if (line == null || !lines.Contains(line)) { OpenLinesScreen(); return; }
        var page=OpenPage("机车配属", "不同机车的运力相加；速度和维护成本按线路配属车辆平均计算。", "FleetArt/garage_watercolor");
        pageBack=()=>OpenLineScreen(line.id);
        Txt(page.transform, line.name + " · 已配属 " + line.trains + " 列", 32, Ink, 32, H-300, 1000, 44, TextAnchor.MiddleLeft);
        Txt(page.transform, "当前单向区段运力 " + Mathf.RoundToInt(FleetCapacity(line)) + " 人次/日 · 中途上下车可复用座位", 24, InkSoft, 32, H-360, 1000, 45, TextAnchor.MiddleLeft);
        Txt(page.transform, "平均速度 " + Mathf.RoundToInt(FleetTrait(line,2)*100) + "% · 平均维护 " + Mathf.RoundToInt(FleetTrait(line,0)*100) + "%", 24, InkSoft, 32, H-410, 1000, 45, TextAnchor.MiddleLeft);
        Txt(page.transform,"配属车辆：线路基础车辆 ×1",27,Ink,32,H-500,1000,40,TextAnchor.MiddleLeft);
        float y=H-555; foreach(var v in researchVehicles) if(v.lineId==line.id) { Txt(page.transform,LocoNames[v.node] + " · 运力 " + Mathf.RoundToInt(FleetCapacityTraits[v.node]*100) + "% · 成本 " + Mathf.RoundToInt(FleetMaintenanceTraits[v.node]*100) + "% · 速度 " + Mathf.RoundToInt(FleetSpeedTraits[v.node]*100) + "%",22,InkSoft,48,y,970,36,TextAnchor.MiddleLeft); y-=46; }
        var add=Btn(page.transform,"AddVehicle","调入或购买机车",26,28,260,W-56,84,Indigo,()=>{if(line.trains>=8){Toast("该线路最多配属 8 列车");return;}OpenLineFleetSupply(line,-1,0);}); TInk(add,Color.white);
        Txt(page.transform,"基础车辆运力较低；先研究机车，再从车库调入或购买。",22,InkSoft,32,195,1000,45,TextAnchor.MiddleLeft);
    }

    void OpenLineFleetSupply(Line line, int slot, int amount)
    {
        if (!PathTouchesRegion(line.st, currentRegion)) { Toast("请先切换到该线路所在地区调整机车"); return; }
        var page=OpenPage("配属机车", "可调入空闲车辆，或购买科技树已解锁的机车。", "PageArt/line_watercolor");
        pageBack=()=> { if(slot<0) OpenLineFleet(line); else OpenLineOperations(line); };
        var free=new List<ResearchVehicle>(); foreach(var v in researchVehicles) if(v.lineId==0) free.Add(v);
        var models=new List<int>(); for(int i=0;i<LocoNames.Length;i++) if(techDone.Contains(LocoKey(i))) models.Add(i);
        int count=free.Count+models.Count; var content=FleetScroll(page.transform,72,H-235,Mathf.Max(180,count*128+20)); float h=content.sizeDelta.y; int rowIndex=0;
        if(count==0) Txt(content,"没有空闲或已解锁的机车，请先研究机车节点。",26,InkSoft,20,h-100,960,60,TextAnchor.MiddleCenter);
        foreach(var v in free) { var item=v; int row=rowIndex++; var button=Btn(content,"Free"+row,"调入  "+LocoNames[item.node],25,12,h-120-row*128,940,100,Paper2,()=>{
            BeginUndo("调入机车"); item.lineId=line.id; SyncLineFleet(line); if(slot>=0) { if(!techDone.Contains("dispatch_peak")) for(int k=0;k<4;k++)line.dispatch[k]=amount; else line.dispatch[slot]=amount; }
            RecalcIncome();EndUndo();RefreshHUD();if(slot<0)OpenLineFleet(line);else OpenLineOperations(line);Toast("机车已调入"); });TInk(button,Ink); }
        foreach(int n in models) { int model=n,row=rowIndex++; var button=Btn(content,"Buy"+row,"购买  "+LocoNames[model]+"  ¥"+LocoPrice(model),25,12,h-120-row*128,940,100,Paper2,()=>{
            int cost=LocoPrice(model);if(coins<cost){Toast("金币不足，还差 ¥"+Mathf.CeilToInt(cost-coins));return;}
            BeginUndo("购买并配属机车"); coins-=cost; researchVehicles.Add(new ResearchVehicle{node=model,lineId=line.id}); SyncLineFleet(line);
            if(slot>=0) { if(!techDone.Contains("dispatch_peak")) for(int k=0;k<4;k++)line.dispatch[k]=amount; else line.dispatch[slot]=amount; }
            RecalcIncome();EndUndo();RefreshHUD();if(slot<0)OpenLineFleet(line);else OpenLineOperations(line);Toast("机车已购买并配属"); });TInk(button,Ink); }
    }

    readonly HashSet<int> ownedLineSkins = new HashSet<int>();
    bool loanOutstanding;
    static readonly string[] LineSkinNames = { "海风蓝", "樱花粉", "金色晨光", "松林绿", "朱砂红", "暮云紫", "湖水青", "焦糖橙", "靛青", "草木绿", "青瓷绿", "珊瑚红", "雾霭灰", "墨棕", "浅紫苑" };
    static readonly Color[] LineSkinColors = {
        new Color(0.31f,0.58f,0.68f), new Color(0.84f,0.52f,0.58f), new Color(0.82f,0.63f,0.29f),
        new Color(0.32f,0.52f,0.40f), new Color(0.75f,0.29f,0.25f), new Color(0.53f,0.39f,0.63f),
        new Color(0.29f,0.64f,0.60f), new Color(0.80f,0.48f,0.26f), new Color(0.24f,0.38f,0.60f),
        new Color(0.53f,0.63f,0.32f), new Color(0.42f,0.65f,0.51f), new Color(0.85f,0.40f,0.36f),
        new Color(0.48f,0.54f,0.56f), new Color(0.48f,0.37f,0.30f), new Color(0.64f,0.50f,0.70f)
    };
    static readonly int[] LineSkinPrices = { 180,220,260,200,200,240,200,220,220,200,240,220,180,180,240 };
    void OpenLineSkins(Line line)
    {
        if(line==null||!lines.Contains(line))return;
        const float h = 850f, rowHeight = 100f;
        var bubble = OpenBubble("线路涂装", h, 760, new Vector2(W/2,H/2));
        Txt(bubble.transform, "金币解锁 · 所有线路可重复使用", 23, InkSoft, 30, h-130, 700, 38, TextAnchor.MiddleLeft);
        var view = Rect("SkinViewport", bubble.transform, 28, 38, 704, 650);
        Img(view, new Color(1,1,1,0.01f)); var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true;
        scroll.scrollSensitivity = 100f; scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.viewport = view.GetComponent<RectTransform>();
        var content = Rect("SkinOptions", view.transform, 0, 0, 704, LineSkinNames.Length * rowHeight);
        var contentRt = content.GetComponent<RectTransform>(); contentRt.anchorMin = contentRt.anchorMax = new Vector2(0,1);
        contentRt.pivot = new Vector2(0,1); contentRt.anchoredPosition = Vector2.zero; scroll.content = contentRt;
        for (int i = 0; i < LineSkinNames.Length; i++) {
            int skin = i; bool owned = ownedLineSkins.Contains(skin); bool selected = line.color == LineSkinColors[skin];
            var row = Rect("SkinRow" + i, content.transform, 0, LineSkinNames.Length * rowHeight - (i + 1) * rowHeight, 704, 90);
            Rounded(row, new Color(1f,0.99f,0.96f,0.96f));
            Img(Rect("Swatch", row.transform, 20, 19, 52, 52), LineSkinColors[i], spriteCircle).raycastTarget = false;
            Txt(row.transform, LineSkinNames[i], 27, Ink, 88, 24, 330, 42, TextAnchor.MiddleLeft);
            var button = Btn(row.transform, "Skin" + i, selected && owned ? "使用中" : owned ? "使用" : "¥" + LineSkinPrices[i], 25, 486, 10, 196, 70, Paper2, () => {
                if (ownedLineSkins.Contains(skin) && line.color == LineSkinColors[skin]) { Toast("当前已使用此颜色"); return; }
                if (!ownedLineSkins.Contains(skin) && coins < LineSkinPrices[skin]) { Toast("金币不足，还差 ¥" + Mathf.CeilToInt(LineSkinPrices[skin] - coins)); return; }
                BeginUndo("线路涂装"); if (ownedLineSkins.Add(skin)) coins -= LineSkinPrices[skin];
                line.color = LineSkinColors[skin]; CreateLineVisuals(); EndUndo(); RefreshHUD();
                ClosePanel(true); OpenLineScreen(line.id); Toast("涂装已应用");
            }); TInk(button, Ink);
        }
    }
    int dailyStationBaseline;
    int DailyConnectedCount()
    {
        var connected = new HashSet<int>();
        foreach (var line in lines) foreach (int index in line.st)
            if (index >= 0 && index < stations.Count && stations[index].built && !stations[index].closed) connected.Add(index);
        return connected.Count;
    }
    void JournalTape(Transform parent, float x, float y, float width)
    {
        var tape = Rect("Tape", parent, x, y, width, 25);
        Img(tape, new Color(0.82f,0.73f,0.54f,0.55f), spriteRound).raycastTarget = false;
        tape.transform.localRotation = Quaternion.Euler(0,0,-3);
    }
    void OpenDailyTasks()
    {
        var page = OpenPage("每日任务", "每日零点刷新进度；统计当天新增连通的站点数。奖励功能尚未开放。", "PageArt/album_watercolor");
        Txt(page.transform, "今日旅程", 38, Ink, 36, H-245, 600, 54, TextAnchor.MiddleLeft);
        Txt(page.transform, "DAY " + day + "  ·  每日零点刷新", 22, InkSoft, 38, H-287, 700, 34, TextAnchor.MiddleLeft);
        var card = Rect("DailyTaskCard", page.transform, 28, H-660, W-56, 320);
        Rounded(card, new Color(1f,0.985f,0.95f,0.97f)); SoftShadow(card); JournalTape(card.transform, 440, 303, 144);
        int progress = Mathf.Clamp(DailyConnectedCount() - dailyStationBaseline, 0, 2);
        Txt(card.transform, "让旅程更远一点", 38, Ink, 38, 224, 760, 50, TextAnchor.MiddleLeft);
        Txt(card.transform, "今日净增连通 2 个站点", 28, InkSoft, 40, 170, 800, 38, TextAnchor.MiddleLeft);
        Img(Rect("Track", card.transform, 40, 112, 780, 16), new Color(0.89f,0.87f,0.81f), spriteRound).raycastTarget = false;
        if (progress > 0) Img(Rect("Progress", card.transform, 40, 112, 780 * progress / 2f, 16), Matcha, spriteRound).raycastTarget = false;
        Txt(card.transform, progress + " / 2", 30, Ink, 840, 96, 140, 45, TextAnchor.MiddleRight);
        Txt(card.transform, progress >= 2 ? "今日目标已达成" : "每一站，都是新的相遇", 24, progress >= 2 ? Matcha : InkSoft, 40, 36, 700, 36, TextAnchor.MiddleLeft);
        Txt(page.transform, "奖励功能尚未开放", 25, InkSoft, 40, H-720, 900, 38, TextAnchor.MiddleLeft);
    }
    void OpenLeaderboard()
    {
        var page = OpenPage("排行榜", "排行榜服务尚未接入；这里先展示你的当前运营记录。", "PageArt/album_watercolor");
        Txt(page.transform, "旅途中的同行者", 38, Ink, 36, H-245, 800, 54, TextAnchor.MiddleLeft);
        Txt(page.transform, "RAILWAY NOTES", 22, InkSoft, 38, H-287, 700, 34, TextAnchor.MiddleLeft);
        var note = Rect("RankingNote", page.transform, 28, H-660, W-56, 320);
        Rounded(note, new Color(1f,0.985f,0.95f,0.97f)); SoftShadow(note); JournalTape(note.transform, 440, 303, 144);
        Txt(note.transform, "排行榜尚未开放", 38, Ink, 40, 214, 800, 52, TextAnchor.MiddleLeft);
        Txt(note.transform, "先把沿途的风景与成绩，记在这里。", 27, InkSoft, 42, 150, 880, 42, TextAnchor.MiddleLeft);
        Txt(note.transform, "当前区域  " + currentRegion + "     全网已连通  " + DailyConnectedCount() + " 站", 28, Matcha, 42, 72, 900, 42, TextAnchor.MiddleLeft);
    }
    void OpenVehicleDetails(int index)
    {
        if (index < 0 || index >= LocoNames.Length) return;
        InitHistoryTree();
        string intro = historyByKey.TryGetValue(LocoKey(index), out var node) ? node.description : "暂无车型简介。";
        float bodyHeight = Mathf.Clamp(MeasureTextHeight(intro, 25, 888) + 10, 54, H - 1100);
        float artY = 340 + bodyHeight;
        var panel = OpenPopup(LocoNames[index] + " · " + LocoYears[index], artY + 580, 960);
        var backdrop = Rect("VehicleArtFrame", panel.transform, 30, artY, 900, 490);
        Rounded(backdrop, new Color(0.93f,0.92f,0.85f,0.75f));
        var art = Rect("VehicleArt", backdrop.transform, 40, 40, 820, 410).AddComponent<RawImage>();
        art.texture = Resources.Load<Texture2D>("Research/Train/" + LocoArt[index]); art.raycastTarget = false;
        FitVehicleImage(art, 820, 410);
        Txt(panel.transform, "车型简介", 31, Ink, 36, 290 + bodyHeight, 860, 44, TextAnchor.MiddleLeft);
        ScrollableText(panel.transform, intro, 25, InkSoft, 36, 280, 888, bodyHeight);
        Txt(panel.transform, "运力 " + Mathf.RoundToInt(FleetCapacityTraits[index] * 100) + "%    维护 " + Mathf.RoundToInt(FleetMaintenanceTraits[index] * 100) + "%    速度 " + Mathf.RoundToInt(FleetSpeedTraits[index] * 100) + "%", 24, InkSoft, 36, 230, 888, 42, TextAnchor.MiddleLeft);
        var fullscreen = Btn(panel.transform, "Fullscreen", "横屏查看大图", 30, 36, 46, 888, 90, Indigo, () => OpenVehicleImage(index)); TInk(fullscreen, Color.white);
    }
    void FitVehicleImage(RawImage image, float width, float height)
    {
        if (!image.texture) return;
        float ratio = (float)image.texture.width / image.texture.height;
        float w = Mathf.Min(width, height * ratio), h = w / ratio;
        image.rectTransform.sizeDelta = new Vector2(w, h);
        image.rectTransform.anchoredPosition = new Vector2(40f + (width - w) / 2f, 40f + (height - h) / 2f);
    }
    void OpenVehicleImage(int index)
    {
        var texture = Resources.Load<Texture2D>("Research/Train/" + LocoArt[index]);
        if (!texture) { Toast("车型图片暂不可用"); return; }
        ClosePanel(true);
        if (sceneryViewer) Destroy(sceneryViewer);
        sceneryViewer = new GameObject("VehicleViewer", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(SceneryViewerLayout));
        var viewerCanvas = sceneryViewer.GetComponent<Canvas>(); viewerCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        viewerCanvas.overrideSorting = true; viewerCanvas.sortingOrder = 100;
        var bg = Rect("Backdrop", sceneryViewer.transform, 0, 0, 0, 0);
        var bgRt = bg.GetComponent<RectTransform>(); bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        Img(bg, new Color(0.94f,0.92f,0.87f));
        var stage = Rect("Landscape", sceneryViewer.transform, 0, 0, 1920, 1080);
        var stageRt = stage.GetComponent<RectTransform>(); stageRt.anchorMin = stageRt.anchorMax = new Vector2(0.5f,0.5f); stageRt.pivot = new Vector2(0.5f,0.5f);
        stageRt.anchoredPosition = Vector2.zero; sceneryViewer.GetComponent<SceneryViewerLayout>().stage = stageRt;
        var art = Rect("VehicleImage", stage.transform, 95, 160, 1730, 760).AddComponent<RawImage>(); art.texture = texture; art.raycastTarget = false;
        float ratio = (float)texture.width / texture.height, width = Mathf.Min(1730, 760 * ratio), height = width / ratio;
        art.rectTransform.sizeDelta = new Vector2(width,height); art.rectTransform.anchoredPosition = new Vector2(95 + (1730-width)/2f,160 + (760-height)/2f);
        var back = Btn(stage.transform, "Back", "←", 40, 24, 958, 94, 94, Color.white, () => { Destroy(sceneryViewer); sceneryViewer = null; OpenVehicleDetails(index); }); TInk(back, Ink);
        var home = Btn(stage.transform, "Home", "主页", 24, 134, 958, 104, 94, Color.white, GoHome); TInk(home, Ink);
        Txt(stage.transform, LocoNames[index] + " · " + LocoYears[index], 44, Ink, 254, 975, 800, 60, TextAnchor.MiddleLeft);
    }
    void OpenCompanyScreen()
    {
        var pg=OpenPage("公司", "资金紧张时可申请一笔无息周转金；归还后才能再次申请。", "PageArt/company_watercolor");
        Txt(pg.transform,"周转金",34,Ink,32,H-310,800,45,TextAnchor.MiddleLeft);
        Txt(pg.transform,loanOutstanding?"已有未还周转金 ¥500":"尚无周转金；金币低于 ¥100 且预计利润不为正时可申请。",24,InkSoft,32,H-390,980,80,TextAnchor.UpperLeft);
        var loan=Btn(pg.transform,"Loan",loanOutstanding?"归还 ¥500":"申请 ¥500",28,32,H-540,950,96,Indigo,()=>{
            if(loanOutstanding){if(coins<500){Toast("金币不足，暂无法还款");return;}BeginUndo("归还周转金");coins-=500;loanOutstanding=false;}
            else {if(coins>=100||income>0){Toast("金币低于 ¥100 且预计利润不为正时可申请");return;}BeginUndo("领取周转金");coins+=500;loanOutstanding=true;}
            EndUndo();RefreshHUD();OpenCompanyScreen();Toast(loanOutstanding?"已领取无息周转金":"周转金已还清");
        });TInk(loan,Color.white);
    }

    // ---------- 图鉴 ----------
    int sceneryCategory;
    int sceneryRegionFilter;
    static readonly string[] SceneryCategories = { "全部名胜", "自然景观", "人文景观" };
    static readonly string[] SceneryRegions = { "全部", "香港", "澳门", "广东" };
    void OpenSceneryScreen()
    {
        var pg = OpenPage("旅行手帐", "连通名胜绑定的车站即可解锁；有大图的名胜首次打开大图时打卡。", "PageArt/album_watercolor");
        Txt(pg.transform, "沿途记忆", 36, Ink, 34, H-238, 440, 52, TextAnchor.MiddleLeft);
        int regionTotal = 0, regionFound = 0;
        foreach (var item in sceneries) if (sceneryRegionFilter == 0 || item.region == SceneryRegions[sceneryRegionFilter]) { regionTotal++; if (item.found) regionFound++; }
        Txt(pg.transform, "TRAVEL JOURNAL  ·  " + regionFound + " / " + regionTotal, 21, InkSoft, 36, H-278, 500, 32, TextAnchor.MiddleLeft);
        Button filter = null;
        filter = Btn(pg.transform, "SceneryFilter", SceneryCategories[sceneryCategory] + "  ▾", 25, 694, H-264, 330, 72,
            new Color(0.91f,0.88f,0.80f,0.98f), () => ShowFleetDropdown(pg.transform, filter.transform, SceneryCategories, sceneryCategory, value => { sceneryCategory = value; OpenSceneryScreen(); }));
        TInk(filter, Ink); filter.gameObject.AddComponent<FleetPressFeedback>();
        for (int i = 0; i < SceneryRegions.Length; i++) {
            int region = i;
            var tab = Btn(pg.transform, "SceneryRegion" + i, SceneryRegions[i], 24, 28 + i * 258, H - 360, 246, 60,
                i == sceneryRegionFilter ? new Color(0.75f,0.82f,0.68f,0.98f) : new Color(0.96f,0.94f,0.88f,0.98f),
                () => { sceneryRegionFilter = region; OpenSceneryScreen(); });
            TInk(tab, Ink);
        }
        int count = 0;
        foreach (var item in sceneries)
            if ((sceneryRegionFilter == 0 || item.region == SceneryRegions[sceneryRegionFilter]) &&
                (sceneryCategory == 0 || (sceneryCategory == 1 && item.natural) || (sceneryCategory == 2 && !item.natural))) count++;
        var content = FleetScroll(pg.transform, 72, H - 390, Mathf.CeilToInt(count / 2f) * 440f + 20);
        float contentHeight = content.sizeDelta.y;
        int visible = 0;
        if (count == 0) Txt(content, "这里还没有旅行记忆\n换个分类看看吧", 30, InkSoft, 40, contentHeight - 240, W - 136, 150, TextAnchor.MiddleCenter);
        for (int i = 0; i < sceneries.Count; i++)
        {
            var sc = sceneries[i];
            if (sceneryRegionFilter != 0 && sc.region != SceneryRegions[sceneryRegionFilter]) continue;
            if (sceneryCategory == 1 && !sc.natural || sceneryCategory == 2 && sc.natural) continue;
            bool pending = pendingCheckin.Contains(sc);
            bool hasPicture = TryGetSceneryContent(sc, out string picturePath, out _);
            var card = Rect("EC"+i, content, (visible%2)*516, contentHeight - 420 - (visible/2)*440, 500, 410);
            visible++;
            Rounded(card, new Color(1f,0.985f,0.95f,0.97f)); SoftShadow(card);
            var photo = Rect("PostcardPhoto", card.transform, 20, 168, 460, 218);
            var photoBg = Img(photo, new Color(0.88f,0.91f,0.84f,1f), spriteRound); photoBg.type = Image.Type.Sliced; photoBg.raycastTarget = false;
            if (sc.found && hasPicture) {
                var image = Rect("CheckedInPhoto", photo.transform, 6, 6, 448, 206).AddComponent<RawImage>();
                image.color = Color.clear; image.raycastTarget = false;
                var lazy = image.gameObject.AddComponent<JournalLazyPhoto>(); lazy.image = image;
                lazy.viewport = content.parent as RectTransform; lazy.resourcePath = picturePath;
            } else {
                var wash = Rect("Watercolor", photo.transform, 6, 6, 448, 206).AddComponent<RawImage>();
                wash.texture = Resources.Load<Texture2D>("PageArt/album_watercolor"); wash.color = new Color(1,1,1,sc.unlocked ? 0.38f : 0.18f); wash.raycastTarget = false;
                var icon = Rect("Illustration", photo.transform, 170, 53, 120, 112).AddComponent<RawImage>();
                icon.texture = Resources.Load<Texture2D>(sc.natural ? "MapIcons/scenery_nature" : "MapIcons/scenery_culture");
                icon.color = sc.unlocked ? Color.white : new Color(0.62f,0.68f,0.58f,0.72f); icon.raycastTarget = false;
                Txt(photo.transform, sc.unlocked ? "沿途风景" : "待收集", 23, InkSoft, 90, 16, 280, 34, TextAnchor.MiddleCenter);
            }
            var tape = Rect("Tape", card.transform, 190, 379, 120, 25); Img(tape, new Color(0.82f,0.73f,0.54f,0.55f), spriteRound).raycastTarget = false;
            tape.transform.localRotation = Quaternion.Euler(0, 0, (i % 2 == 0) ? -4 : 4);
            Txt(card.transform, sc.name, 34, Ink, 24, 115, 330, 44, TextAnchor.MiddleLeft);
            Txt(card.transform, sc.region + (sc.natural ? "  ·  自然" : "  ·  人文"), 22, InkSoft, 26, 79, 310, 30, TextAnchor.MiddleLeft);
            string status = sc.found ? "已打卡" : !RegionUnlocked(sc.region) ? "先解锁" + sc.region : !sc.unlocked ? "连通" + sc.triggerStation + "站" : hasPicture ? "点击打卡" : "图片筹备中";
            Txt(card.transform, status, 22, sc.found ? Matcha : pending ? Vermilion : InkSoft, 24, 28, 440, 35, TextAnchor.MiddleLeft);
            var stamp = Rect("Stamp", card.transform, 364, 89, 102, 58); Rounded(stamp, sc.found ? new Color(0.72f,0.82f,0.67f,0.65f) : pending ? new Color(0.96f,0.78f,0.72f,0.75f) : new Color(0.89f,0.87f,0.81f,0.85f));
            stamp.GetComponent<Image>().raycastTarget = false; stamp.transform.localRotation = Quaternion.Euler(0,0,7);
            Txt(stamp.transform, sc.found ? "已到访" : pending ? "待打卡" : "未到访", 20, sc.found ? Matcha : pending ? Vermilion : InkSoft, 0, 0, 102, 58, TextAnchor.MiddleCenter);
            if (sc.unlocked && hasPicture)
            {
                var scLocal = sc;
                var btn = card.AddComponent<Button>(); btn.targetGraphic = card.GetComponent<Image>();
                btn.onClick.AddListener(() => OpenSceneryViewer(scLocal, () => { ClosePage(); OpenSceneryScreen(); }));
            }
            if (qualityTier > 0) { var entry = card.AddComponent<FleetCardEntrance>(); entry.delay = Mathf.Min(visible - 1, 4) * 0.025f; }
        }
    }
}
