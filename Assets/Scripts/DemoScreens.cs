using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    GameObject pageRoot;
    RectTransform pageContentRect;
    Action pageBack;
    float PgH => pageContentRect ? Mathf.Max(1400f, pageContentRect.rect.height) : PageSafeHeight;

    GameObject OpenPage(string title, string help, string backgroundPath = null)
    {
        ClosePage();
        pageBack = null;
        CloseLineBubble();

        pageRoot = Rect("Page", root, 0, 0, RootW, RootH);
        var pageRootRt = pageRoot.GetComponent<RectTransform>();
        pageRootRt.anchorMin = Vector2.zero; pageRootRt.anchorMax = Vector2.one;
        pageRootRt.pivot = new Vector2(0.5f, 0.5f);
        pageRootRt.offsetMin = Vector2.zero; pageRootRt.offsetMax = Vector2.zero;
        Img(pageRoot, Paper);

        GetSafeMarginsInRoot(out float safeL, out float safeR, out float safeT, out float safeB);
        float safeW = Mathf.Max(320f, RootW - safeL - safeR);
        float safeH = Mathf.Max(1400f, RootH - safeT - safeB);
        float contentX = safeL + (safeW - W) * 0.5f;
        var pageContent = Rect("PageContent", pageRoot.transform, contentX, safeB, W, safeH);
        pageContentRect = pageContent.GetComponent<RectTransform>();

        var pTop = Rect("PTop", pageRoot.transform, 0, RootH - safeT - 160f, RootW, safeT + 160f);
        Img(pTop, HeaderBg);
        var pDiv = Rect("PDiv", pageRoot.transform, 0, RootH - safeT - 160f, RootW, 3);
        Img(pDiv, new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.16f)).raycastTarget = false;

        var bb = Btn(pageRoot.transform, "PBack", "←", 40, 24, 0, 94, 94, CardBg, GoBackPage);
        TInk(bb, Ink); SoftShadow(bb.gameObject);
        var home = Btn(pageRoot.transform, "PHome", "主页", 24, 134, 0, 104, 94, CardBg, GoHome);
        TInk(home, Ink); SoftShadow(home.gameObject);
        var heading = Txt(pageRoot.transform, title, 44, Ink, 0, 0, 600, 60, TextAnchor.MiddleLeft);
        heading.resizeTextForBestFit = true; heading.resizeTextMinSize = 26; heading.resizeTextMaxSize = 44;
        RectTransform helpRect = null;
        if (!string.IsNullOrEmpty(help))
        {
            var hb = Btn(pageRoot.transform, "PHelp", "?", 32, 0, 0, 64, 64, CardBg, () => HelpPopup(help));
            TInk(hb, InkSoft); SoftShadow(hb.gameObject); helpRect = hb.GetComponent<RectTransform>();
        }
        if (!string.IsNullOrEmpty(backgroundPath)) AddPageBackground(pageRoot, backgroundPath);
        var layout = pageRoot.AddComponent<PageHeaderLayout>();
        layout.page = pageRootRt;
        layout.topBarBg = pTop.GetComponent<RectTransform>();
        layout.topDivider = pDiv.GetComponent<RectTransform>();
        layout.back = bb.GetComponent<RectTransform>();
        layout.home = home.GetComponent<RectTransform>();
        layout.title = heading.rectTransform;
        layout.help = helpRect;
        EnsurePreviewBannerOnTop();
        return pageContent;
    }
    void AddPageBackground(GameObject page, string path)
    {
        var texture = Resources.Load<Texture2D>(path);
        if (!texture) return;
        var background = Rect("PageBackground", page.transform, 0, 0, RootW, RootH);
        var art = background.AddComponent<RawImage>(); art.texture = texture;
        art.color = IsNeonThemeActive() ? new Color(0.28f, 0.38f, 0.86f, 0.16f) : new Color(1, 1, 1, 0.26f);
        art.raycastTarget = false;
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
    void ClosePage() { if (pageRoot) Destroy(pageRoot); pageRoot = null; pageContentRect = null; }
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
        GetSafeMarginsInRoot(out float safeL, out _, out float safeT, out _);
        float bodyHeight = Mathf.Clamp(MeasureTextHeight(text, 27, 572) + 10, 50, Mathf.Max(300f, PgH - 420f));
        var p = OpenBubble("说明", bodyHeight + 220, 640, new Vector2(safeL + 130f, RootH - safeT - 240f));
        ScrollableText(p.transform, text, 27, InkSoft, 34, 128, 572, bodyHeight);
        var ok = Btn(p.transform, "Ok", "知道了", 30, 34, 28, 572, 80, new Color(0.92f,0.89f,0.83f), ClosePanel);
        TInk(ok, Ink);
    }

    void StatBox(Transform p, string label, string value, float x, float y, float w, float h, Color c)
    {
        var g = Rect("Stat", p, x, y, w, h); Rounded(g, CardBg); SoftShadow(g);
        Img(Rect("Bar", g.transform, 0, 24, 7, h-48), c, spriteRound);
        Txt(g.transform, label, 26, InkSoft, 30, h-66, w-56, 34, TextAnchor.MiddleLeft);
        Txt(g.transform, value, 46, Ink, 30, 26, w-56, 54, TextAnchor.MiddleLeft);
    }

    // ---------- 区域选择 ----------
    void OpenRegionScreen()
    {
        var pg = OpenPage("切换区域", "各区依次解锁。每区线路上限为站点数的一半；交界站计入所连地区。", "PageArt/line_watercolor");
        string[] regions = { "香港", "澳门", "广东" };
        var content = FleetScroll(pg.transform, 42f, PgH - 250f, 800f);
        float topY = content.sizeDelta.y, cardW = content.sizeDelta.x, cardX = 4f, innerW = cardW - 8f;
        for (int i = 0; i < regions.Length; i++)
        {
            string region = regions[i]; bool available = RegionUnlocked(region); bool active = region == currentRegion;
            float cy = topY - 12f - (i + 1) * 246f + 16f;
            var card = Rect("Region_" + region, content, cardX, cy, innerW, 224f);
            Rounded(card, new Color(1, 1, 1, 0.96f)); SoftShadow(card);
            Img(Rect("Accent", card.transform, 0, 30, 7, 164), active ? Gold : available ? Matcha : InkSoft, spriteRound).raycastTarget = false;
            Txt(card.transform, region, 42, Ink, 32, 142, 380, 55, TextAnchor.MiddleLeft);
            Txt(card.transform, "线路 " + RegionLineCount(region) + " / " + RegionLineLimit(region), 24, InkSoft, 450, 150, 280, 40, TextAnchor.MiddleLeft);
            string condition = active ? "当前区域" : available ? "已开放" : region == "澳门"
                ? "香港普通站 " + Mathf.Min(4, ConnectedOrdinaryStations("香港")) + "/4 · 预计日利润 ¥" + FormatCoins(income) + "/600"
                : "开放澳门 · 澳门普通站 " + Mathf.Min(2, ConnectedOrdinaryStations("澳门")) + "/2 · 预计日利润 ¥" + FormatCoins(income) + "/1,200";
            Txt(card.transform, condition, 23, InkSoft, 32, 42, innerW - 260f, 76, TextAnchor.MiddleLeft);
            var button = Btn(card.transform, "Select", active ? "当前" : available ? "前往" : "未解锁", 26,
                innerW - 216f, 67f, 186f, 90f, active ? Paper2 : available ? Matcha : new Color(0.78f,0.76f,0.71f), () => {
                    if (RegionUnlocked(region)) SwitchRegion(region); else Toast("尚未达到" + region + "的解锁条件");
                });
            TInk(button, active ? Ink : Color.white);
        }
    }

    // ---------- 线路 ----------
    void OpenLinesScreen()
    {
        var pg = OpenPage("线路", "在地图按住车站拖向下一站即可开线；自端点拖出为延线，自中间站拖出则开新线。", "PageArt/line_watercolor");
        StatBox(pg.transform, "预计日利润", FormatSignedNumber(income), 28, PgH-400, 320, 210, Gold);
        StatBox(pg.transform, "已连通", ConnectedCount() + "/" + stations.Count, 380, PgH-400, 320, 210, Matcha);
        StatBox(pg.transform, "钻石", Mathf.FloorToInt(diamonds).ToString(), 732, PgH-400, 320, 210, Sakura);
        var regionalLines = lines.FindAll(line => PathTouchesRegion(line.st, currentRegion));
        Txt(pg.transform, currentRegion + "线路 " + regionalLines.Count + " / " + LineLimit() + " · 当前时段日化旅次 " + Mathf.RoundToInt(traffic != null ? traffic.totalPassengers : 0f), 24, InkSoft, 28, PgH-465, 1000, 40, TextAnchor.MiddleLeft);
        var list = FleetScroll(pg.transform, 36, PgH - 490, Mathf.Max(1, regionalLines.Count) * 188f + 24);
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
        var pg = OpenPage(line.name, "沿线客流 · 运力配属 · 日盈亏总览", "PageArt/line_watercolor");
        pageBack = back ?? OpenLinesScreen;
        StatBox(pg.transform, "客流", Mathf.RoundToInt(LinePassengerFlow(line)) + "", 28, PgH-440, 320, 200, Matcha);
        StatBox(pg.transform, "单向区段运力", Mathf.RoundToInt(LineCapacity(line)) + " 人次/日", 380, PgH-440, 320, 200, Indigo);
        StatBox(pg.transform, "预计日利润", Mathf.RoundToInt(LineDaily(line)) + "", 732, PgH-440, 320, 200, Gold);
        var fleet = Rect("FleetSummary", pg.transform, 28, PgH-710, W-56, 190); Rounded(fleet, new Color(1,1,1,0.94f)); SoftShadow(fleet);
        Txt(fleet.transform, "配属 " + line.trains + " 列 · 平均速度 " + Mathf.RoundToInt(FleetTrait(line,2)*100) + "%", 28, Ink, 24, 115, 950, 45, TextAnchor.MiddleLeft);
        Txt(fleet.transform, "重联叠加运力，速度与养护成本按车队均值核算。", 21, InkSoft, 24, 70, 950, 35, TextAnchor.MiddleLeft);
        var fleetButton=Btn(fleet.transform,"Fleet","机车配属",25,24,8,450,55,Indigo,()=>OpenLineFleet(line));TInk(fleetButton,Color.white);
        var operations=Btn(fleet.transform,"Operations","票价与调度",25,495,8,450,55,Indigo,()=>OpenLineOperations(line));TInk(operations,Color.white);
        Txt(pg.transform, "沿线", 28, InkSoft, 28, PgH-800, 160, 38, TextAnchor.MiddleLeft);
        Txt(pg.transform, UniquePathText(line.st), 26, Ink, 28, PgH-870, W-56, 60, TextAnchor.UpperLeft);
        int curSkin = GetLineSkin(line);
        string skinBtnText = curSkin >= 0
            ? "线路涂装 · " + LineSkinNames[curSkin] + "（" + SkinCategoryName(curSkin) + " · " + SkinPerkSummary(curSkin) + "）"
            : "线路涂装 · 解锁外观与流派加成";
        var colors=Btn(pg.transform,"LineSkins",skinBtnText,24,28,PgH-990,W-56,78,Paper2,()=>OpenLineSkins(line));TInk(colors,Ink);
        Btn(pg.transform, "Add", "机车商店", 30, 28, 56, 490, 124, Indigo, () => { fleetBack = () => OpenLineScreen(id, back); OpenLocoShop(); });
        Btn(pg.transform, "Del", "拆除线路", 30, 544, 56, 508, 124, Vermilion, () => {
            int refund = Mathf.RoundToInt(120f * (UniqueCount(line.st) - 1) * TrackBuildCostFactor() * DemolishRefundRate());
            Confirm("拆除 " + line.name, "退还 ¥" + refund, () => {
                BeginUndo("拆除整条线路"); coins += refund; SettleRemovedLineTax(line); ReleaseLineFleet(line); lines.Remove(line);
                CreateLineVisuals(); RefreshStationDots(); RecalcIncome(); EndUndo(); RefreshHUD(); ClosePanel();
                ClosePage(); OpenLinesScreen();
            });
        });
    }

    // ---------- 调度 / 车库 / 商店 ----------
    int fleetPowerFilter, fleetUseFilter, fleetRegionFilter, shopCategory;
    Action fleetBack;
    static readonly string[] FleetPowerFilters = { "全部动力", "蒸汽", "内燃", "电力", "氢能", "双动力", "其他" };
    static readonly string[] FleetUseFilters = { "全部用途", "客运优先", "货运优先", "客货兼顾" };
    static readonly string[] FleetRegionFilters = { "全部地区", "英国", "美国", "德国", "印度" };
    // 分类沿用一代目录的用途划分；地区表示目录展示地区。
    static readonly int[] FleetUses = { 3, 2, 3, 3, 3, 2, 3, 3, 1 };
    static readonly int[] FleetRegions = { 1, 1, 1, 1, 1, 2, 3, 4, 2 };
    void OpenGarageScreen() { fleetBack = null; OpenFleetTab(0); }
    void OpenResearchInventory() { OpenFleetTab(1); }
    void OpenLocoShop() { shopCategory = 0; OpenFleetTab(2); }
    void OpenThemeShopScreen() { fleetBack = null; shopCategory = 2; OpenFleetTab(2); }
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
            "统筹全网各线配车与分时班次，优化客流运力比。",
            "管理在库机车资产，随时调拨或增配至干线。",
            "选购机车、复线涂装与全局手账主题；多款外观可在映像馆抢先试用。"
        };
        string[] backgrounds = { "dispatch_watercolor", "garage_watercolor", "shop_watercolor" };
        var pg = OpenPage(titles[tab], help[tab], "FleetArt/" + backgrounds[tab]);
        pageBack = fleetBack;
        if (tab == 0) BuildFleetDispatch(pg.transform);
        else if (tab == 1) BuildFleetCollection(pg.transform, tab);
        else
        {
            if (shopCategory == 0) BuildFleetCollection(pg.transform, 2);
            else if (shopCategory == 1) BuildShopLiveries(pg.transform);
            else BuildShopThemes(pg.transform);
            BuildShopSidebar(pg.transform);
        }
        BuildFleetNavigation(pg.transform, tab);
    }
    void BuildShopSidebar(Transform page)
    {
        var rail = Rect("ShopSidebar", page, 28, PgH - 550, 136, 304);
        Rounded(rail, CardBg); SoftShadow(rail);
        string[] cats = { "机车", "涂装", "风格" };
        Color activeBg = new Color(Matcha.r, Matcha.g, Matcha.b, 0.25f);
        for (int i = 0; i < cats.Length; i++)
        {
            int cat = i;
            bool selected = shopCategory == i;
            var btn = Btn(rail.transform, "ShopCat" + i, cats[i], 25, 6, 206 - i * 98, 124, 88,
                selected ? activeBg : new Color(Paper2.r, Paper2.g, Paper2.b, 0.72f),
                () => { shopCategory = cat; OpenFleetTab(2); });
            TInk(btn, selected ? Ink : InkSoft);
        }
    }
    void BuildFleetNavigation(Transform page, int selected)
    {
        Transform host = pageRoot ? pageRoot.transform : page;
        GetSafeMarginsInRoot(out float safeL, out float safeR, out _, out float safeB);
        float safeW = Mathf.Max(320f, RootW - safeL - safeR);
        float navSlotX = pageRoot ? (safeL + (safeW - W) * 0.5f) : 0f;
        float navBaseY = pageRoot ? safeB : 0f;

        var bar = Rect("FleetBottomNav", host, 0, navBaseY, RootW, 154);
        Rounded(bar, CardBg); SoftShadow(bar);
        if (navBaseY > 0.5f)
        {
            var skirt = Rect("BottomSafeSkirt", bar.transform, 0, -navBaseY, RootW, navBaseY + 12f);
            Img(skirt, CardBg).raycastTarget = false;
        }
        if (!pageRoot) bar.AddComponent<FleetBottomEdge>();
        string[] labels = { "调度", "车库", "商店" };
        string[] subtitles = { "DISPATCH", "GARAGE", "SHOP" };
        for (int i = 0; i < labels.Length; i++)
        {
            int target = i;
            var item = Rect("Nav" + i, bar.transform, navSlotX + i * 360, 0, 360, 154);
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
        var content = FleetScroll(page, 172, PgH - 182, Mathf.Max(1, lines.Count) * 360f + 24);
        float height = content.sizeDelta.y;
        if (lines.Count == 0) Txt(content, "还没有运营中的线路", 30, InkSoft, 20, height - 180, W - 96, 80, TextAnchor.MiddleCenter);
        for (int i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var card = Rect("Dispatch" + i, content, 0, height - 350 - i * 360, W - 56, 340);
            Rounded(card, CardBg); SoftShadow(card);
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
        power = Btn(page, "PowerFilter", "动力：" + FleetPowerFilters[fleetPowerFilter] + "  ▾", 22, left, PgH - 285, width, 72,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(page, power.transform, FleetPowerFilters, fleetPowerFilter, value => { fleetPowerFilter = value; OpenFleetTab(tab); }));
        use = Btn(page, "UseFilter", "用途：" + FleetUseFilters[fleetUseFilter] + "  ▾", 22, left + width + gap, PgH - 285, width, 72,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(page, use.transform, FleetUseFilters, fleetUseFilter, value => { fleetUseFilter = value; OpenFleetTab(tab); }));
        region = Btn(page, "RegionFilter", "地区：" + FleetRegionFilters[fleetRegionFilter] + "  ▾", 22, left + 2 * (width + gap), PgH - 285, width, 72,
            new Color(0.93f, 0.90f, 0.84f), () => ShowFleetDropdown(page, region.transform, FleetRegionFilters, fleetRegionFilter, value => { fleetRegionFilter = value; OpenFleetTab(tab); }));
        TInk(power, Ink); TInk(use, Ink); TInk(region, Ink);
        power.gameObject.AddComponent<FleetPressFeedback>(); use.gameObject.AddComponent<FleetPressFeedback>(); region.gameObject.AddComponent<FleetPressFeedback>();
    }
    void ShowFleetDropdown(Transform page, Transform source, string[] options, int selected, Action<int> choose)
    {
        var old = page.Find("FleetDropdownOverlay");
        if (old) Destroy(old.gameObject);
        var sourceRect = (RectTransform)source;
        var pageRect = (RectTransform)page;
        var corners = new Vector3[4]; sourceRect.GetWorldCorners(corners);
        var screen = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(pageRect, screen, null, out var localPt)) return;
        Vector2 point = localPt - pageRect.rect.min;
        float ph = Mathf.Max(PgH, pageRect.rect.height);
        float pw = Mathf.Max(W, pageRect.rect.width);
        var overlay = Rect("FleetDropdownOverlay", page, 0, 0, pw, ph);
        var overlayImage = Img(overlay, new Color(0, 0, 0, 0));
        var dismiss = overlay.AddComponent<Button>(); dismiss.targetGraphic = overlayImage;
        dismiss.transition = Selectable.Transition.None; dismiss.onClick.AddListener(() => Destroy(overlay));
        float width = 326, rowHeight = 70, height = options.Length * rowHeight + 16;
        float x = Mathf.Clamp(point.x, 12, pw - width - 12);
        float y = Mathf.Clamp(point.y - height - 8, 160, ph - height - 150);
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
        Txt(page, tab == 1 ? "已拥有  " + count + " 辆 · 点击车卡查看详情" : "车型  " + count + " 款 · 点击车卡查看详情", 24, InkSoft, 28 + inset, PgH - 338, 850 - inset, 34, TextAnchor.MiddleLeft);
        var content = FleetScroll(page, 172, PgH - 354, Mathf.Max(1, count) * 218f + 20, inset);
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
            Rounded(card, CardBg); SoftShadow(card);
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
        var page = OpenPage("票价与调度", "定价权衡客座率与营收；分时调车削减闲置损耗。", "PageArt/line_watercolor");
        pageBack = () => OpenLineScreen(line.id);
        var forecast = ForecastFor(line);
        Txt(page.transform, line.name + " · 配属 " + line.trains + " 列 · 当前开行 " + RunningTrains(line) + " 列", 28, Ink, 32, PgH-300, 1000, 45, TextAnchor.MiddleLeft);
        Txt(page.transform, "预计日收入 ¥" + Mathf.RoundToInt(forecast.gross) + "    成本 ¥" + Mathf.RoundToInt(forecast.vehicle+forecast.track+forecast.tax) + "    利润 ¥" + Mathf.RoundToInt(forecast.Profit), 24, InkSoft, 32, PgH-360, 1000, 45, TextAnchor.MiddleLeft);
        var fare = Rect("Fare",page.transform,28,PgH-535,W-56,145); Rounded(fare,new Color(1,1,1,0.94f));
        Txt(fare.transform, "起步票价  ¥" + line.fare.ToString("0.0") + " / 人", 30, Ink, 26, 72, 570, 44, TextAnchor.MiddleLeft);
        Txt(fare.transform, techDone.Contains("fare_policy") ? "起步价叠加跨站里程费 · 票款按 10:1 计入金币" : "研究「客运票价制度」后可自主调价", 20, InkSoft, 26, 25, 650, 34, TextAnchor.MiddleLeft);
        var fm=Btn(fare.transform,"FareMinus","−",35,720,30,110,80,Paper2,()=>ChangeLineFare(line,-0.1f));TInk(fm,Ink);
        var fp=Btn(fare.transform,"FarePlus","+",35,850,30,110,80,Paper2,()=>ChangeLineFare(line,0.1f));TInk(fp,Ink);
        bool basic=techDone.Contains("dispatch_basic"), peak=techDone.Contains("dispatch_peak");
        Txt(page.transform, !basic ? "研究「铁路时刻表」解锁统一调度" : !peak ? "统一调度已就绪；研究「分时列车计划」可分时段增减" : "分时调度 · 高峰与夜间独立发班", 23, InkSoft, 32, PgH-590, 1000, 36, TextAnchor.MiddleLeft);
        int rows=peak?4:1;
        for(int i=0;i<rows;i++) {
            int slot=i; var row=Rect("DispatchSlot"+i,page.transform,28,PgH-740-i*130,W-56,115); Rounded(row,new Color(1,1,1,0.94f));
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
        Txt(page.transform, "高阶机车优先上线；各时段数据按全日口径折算，税费于每日结算扣除。", 22, InkSoft, 32, Mathf.Max(70f, PgH-1320), 1000, 100, TextAnchor.UpperLeft);
    }
    void OpenLineFleet(Line line)
    {
        if (line == null || !lines.Contains(line)) { OpenLinesScreen(); return; }
        var page=OpenPage("机车配属", "统筹本线牵引动力与编制配属。", "FleetArt/garage_watercolor");
        pageBack=()=>OpenLineScreen(line.id);
        Txt(page.transform, line.name + " · 已配属 " + line.trains + " 列", 32, Ink, 32, PgH-300, 1000, 44, TextAnchor.MiddleLeft);
        Txt(page.transform, "当前单向区段运力 " + Mathf.RoundToInt(FleetCapacity(line)) + " 人次/日 · 中途上下车可复用座位", 24, InkSoft, 32, PgH-360, 1000, 45, TextAnchor.MiddleLeft);
        Txt(page.transform, "平均速度 " + Mathf.RoundToInt(FleetTrait(line,2)*100) + "% · 平均维护 " + Mathf.RoundToInt(FleetTrait(line,0)*100) + "%", 24, InkSoft, 32, PgH-410, 1000, 45, TextAnchor.MiddleLeft);
        Txt(page.transform,"配属车辆：线路基础车辆 ×1",27,Ink,32,PgH-500,1000,40,TextAnchor.MiddleLeft);
        float y=PgH-555; foreach(var v in researchVehicles) if(v.lineId==line.id) { Txt(page.transform,LocoNames[v.node] + " · 运力 " + Mathf.RoundToInt(FleetCapacityTraits[v.node]*100) + "% · 成本 " + Mathf.RoundToInt(FleetMaintenanceTraits[v.node]*100) + "% · 速度 " + Mathf.RoundToInt(FleetSpeedTraits[v.node]*100) + "%",22,InkSoft,48,y,970,36,TextAnchor.MiddleLeft); y-=46; }
        var add=Btn(page.transform,"AddVehicle","调入或购买机车",26,28,260,W-56,84,Indigo,()=>{if(line.trains>=8){Toast("该线路最多配属 8 列车");return;}OpenLineFleetSupply(line,-1,0);}); TInk(add,Color.white);
        Txt(page.transform,"研发新机车后可自车库调拨或直购增编。",22,InkSoft,32,195,1000,45,TextAnchor.MiddleLeft);
    }

    void OpenLineFleetSupply(Line line, int slot, int amount)
    {
        if (!PathTouchesRegion(line.st, currentRegion)) { Toast("请先切换到该线路所在地区调整机车"); return; }
        var page=OpenPage("配属机车", "从在库车队调拨或直购已研发车型。", "PageArt/line_watercolor");
        pageBack=()=> { if(slot<0) OpenLineFleet(line); else OpenLineOperations(line); };
        var free=new List<ResearchVehicle>(); foreach(var v in researchVehicles) if(v.lineId==0) free.Add(v);
        var models=new List<int>(); for(int i=0;i<LocoNames.Length;i++) if(techDone.Contains(LocoKey(i))) models.Add(i);
        int count=free.Count+models.Count; var content=FleetScroll(page.transform,72,PgH-235,Mathf.Max(180,count*128+20)); float h=content.sizeDelta.y; int rowIndex=0;
        if(count==0) Txt(content,"没有空闲或已解锁的机车，请先研究机车节点。",26,InkSoft,20,h-100,960,60,TextAnchor.MiddleCenter);
        foreach(var v in free) { var item=v; int row=rowIndex++; var button=Btn(content,"Free"+row,"调入  "+LocoNames[item.node],25,12,h-120-row*128,940,100,Paper2,()=>{
            BeginUndo("调入机车"); item.lineId=line.id; SyncLineFleet(line); if(slot>=0) { if(!techDone.Contains("dispatch_peak")) for(int k=0;k<4;k++)line.dispatch[k]=amount; else line.dispatch[slot]=amount; }
            RecalcIncome();EndUndo();RefreshHUD();if(slot<0)OpenLineFleet(line);else OpenLineOperations(line);Toast("机车已调入"); });TInk(button,Ink); }
        foreach(int n in models) { int model=n,row=rowIndex++; var button=Btn(content,"Buy"+row,"购买  "+LocoNames[model]+"  ¥"+LocoPrice(model),25,12,h-120-row*128,940,100,Paper2,()=>{
            int cost=LocoPrice(model);if(coins<cost){Toast("金币不足，还差 ¥"+FormatShortfall(cost-coins));return;}
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

    bool IsSkinFreeByScenery(int skin)
    {
        return (skin == 10 || skin == 11) && FoundCount() >= 1;
    }

    void ApplySkinToLine(Line line, int skin, bool payIfNeeded)
    {
        if (line == null || !lines.Contains(line) || skin < 0 || skin >= LineSkinNames.Length) return;
        bool alreadyOwned = ownedLineSkins.Contains(skin);
        bool freeByScenery = IsSkinFreeByScenery(skin);
        if (!alreadyOwned && payIfNeeded && !freeByScenery && coins < LineSkinPrices[skin])
        {
            Toast("金币不足，还差 ¥" + FormatShortfall(LineSkinPrices[skin] - coins));
            return;
        }
        BeginUndo("线路涂装");
        if (!alreadyOwned && payIfNeeded)
        {
            if (!freeByScenery) coins -= LineSkinPrices[skin];
            ownedLineSkins.Add(skin);
        }
        else ownedLineSkins.Add(skin);
        line.skin = skin;
        line.color = LineSkinColors[skin];
        CreateLineVisuals();
        RecalcIncome();
        EndUndo();
        RefreshHUD();
        Toast("已换装：" + LineSkinNames[skin] + "（" + SkinPerkSummary(skin) + "）");
    }

    void PromptRewardedAd(string adKey, int requiredCount, string itemName, Action onPermanentUnlock, Action onTrialUnlock = null)
    {
        int watched = adWatchCounts.TryGetValue(adKey, out int c) ? c : 0;
        string desc = requiredCount <= 1
            ? "观看 1 段赞助铁道宣传短片，即可免费永久解锁\n「" + itemName + "」"
            : "观看赞助宣传片可立即免费试用「" + itemName + "」24小时\n累计观影进度：" + watched + " / " + requiredCount + " 次（满次即永久拥有）";
        var bubble = OpenBubble("铁道映像馆 · 赞助放映", 400, 740, new Vector2(W / 2, H / 2));
        Txt(bubble.transform, desc, 25, InkSoft, 34, 146, 672, 160, TextAnchor.MiddleLeft);
        var watch = Btn(bubble.transform, "WatchAd", "观看赞助短片（领权益）", 26, 34, 34, 420, 84, Matcha, () => {
            ClosePanel(true);
            int next = (adWatchCounts.TryGetValue(adKey, out int cur) ? cur : 0) + 1;
            adWatchCounts[adKey] = next;
            PlayTone(true);
            SaveGame();
            if (next >= requiredCount) onPermanentUnlock?.Invoke();
            else onTrialUnlock?.Invoke();
        });
        TInk(watch, Color.white);
        var cancel = Btn(bubble.transform, "CancelAd", "暂不观看", 25, 474, 34, 232, 84, Paper2, ClosePanel);
        TInk(cancel, Ink);
    }

    void OpenLineSkins(Line line)
    {
        if (line == null || !lines.Contains(line)) return;
        const float h = 920f, rowHeight = 118f;
        var bubble = OpenBubble("线路涂装", h, 800, new Vector2(W / 2, H / 2));
        Txt(bubble.transform, "双色专属涂装 · 附带流派加成 · 全网通用", 22, InkSoft, 30, h - 126, 740, 36, TextAnchor.MiddleLeft);
        var view = Rect("SkinViewport", bubble.transform, 28, 36, 744, 725);
        Img(view, new Color(1, 1, 1, 0.01f)); var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true;
        scroll.scrollSensitivity = 100f; scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.viewport = view.GetComponent<RectTransform>();
        var content = Rect("SkinOptions", view.transform, 0, 0, 744, LineSkinNames.Length * rowHeight);
        var contentRt = content.GetComponent<RectTransform>(); contentRt.anchorMin = contentRt.anchorMax = new Vector2(0, 1);
        contentRt.pivot = new Vector2(0, 1); contentRt.anchoredPosition = Vector2.zero; scroll.content = contentRt;
        int activeSkin = GetLineSkin(line);
        for (int i = 0; i < LineSkinNames.Length; i++)
        {
            int skin = i;
            bool owned = ownedLineSkins.Contains(skin);
            bool selected = activeSkin == skin;
            bool freeScenery = !owned && IsSkinFreeByScenery(skin);
            var row = Rect("SkinRow" + i, content.transform, 0, LineSkinNames.Length * rowHeight - (i + 1) * rowHeight, 744, 108);
            Rounded(row, CardBg);
            Img(Rect("Swatch", row.transform, 18, 26, 56, 56), LineSkinColors[i], spriteCircle).raycastTarget = false;
            Img(Rect("SwatchTrim", row.transform, 33, 41, 26, 26), LineSkinTrimColors[i], spriteCircle).raycastTarget = false;
            Txt(row.transform, LineSkinNames[i] + " · " + SkinCategoryName(i), 25, Ink, 88, 56, 360, 38, TextAnchor.MiddleLeft);
            Txt(row.transform, SkinPerkSummary(i), 20, InkSoft, 88, 18, 360, 30, TextAnchor.MiddleLeft);

            if (owned)
            {
                var btn = Btn(row.transform, "Skin" + i, selected ? "使用中 ✓" : "换装", 24, 520, 18, 204, 72, selected ? Matcha : Paper2, () => {
                    if (selected) { Toast("当前已使用此涂装"); return; }
                    ApplySkinToLine(line, skin, false);
                    ClosePanel(true); OpenLineScreen(line.id);
                });
                TInk(btn, selected ? Color.white : Ink);
            }
            else
            {
                var buyBtn = Btn(row.transform, "SkinBuy" + i, freeScenery ? "名胜免费领" : "¥" + LineSkinPrices[i], 21, 444, 18, 148, 72, freeScenery ? Matcha : Paper2, () => {
                    ApplySkinToLine(line, skin, true);
                    if (ownedLineSkins.Contains(skin)) { ClosePanel(true); OpenLineScreen(line.id); }
                });
                TInk(buyBtn, freeScenery ? Color.white : Ink);
                var adBtn = Btn(row.transform, "SkinAd" + i, "看片领", 21, 602, 18, 122, 72, Indigo, () => {
                    PromptRewardedAd("skin_" + skin, 1, LineSkinNames[skin], () => {
                        ownedLineSkins.Add(skin);
                        ApplySkinToLine(line, skin, false);
                        OpenLineScreen(line.id);
                    });
                });
                TInk(adBtn, Color.white);
            }
        }
    }

    void ChooseLineForSkin(int skin)
    {
        if (lines.Count == 0) { Toast("请先修建一条铁路线路"); return; }
        float h = 230 + Mathf.Clamp(lines.Count, 1, 7) * 84f;
        var bubble = OpenBubble("将「" + LineSkinNames[skin] + "」应用于", h, 720, new Vector2(W / 2, H / 2));
        var view = Rect("LineViewport", bubble.transform, 28, 36, 664, h - 140);
        Img(view, new Color(1, 1, 1, 0.01f)); var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true;
        scroll.viewport = view.GetComponent<RectTransform>();
        var content = Rect("Lines", view.transform, 0, 0, 664, lines.Count * 84f);
        var crt = content.GetComponent<RectTransform>(); crt.anchorMin = crt.anchorMax = new Vector2(0, 1);
        crt.pivot = new Vector2(0, 1); crt.anchoredPosition = Vector2.zero; scroll.content = crt;
        for (int i = 0; i < lines.Count; i++)
        {
            var target = lines[i];
            int cur = GetLineSkin(target);
            string desc = target.name + (cur == skin ? "（已佩戴）" : cur >= 0 ? "（" + LineSkinNames[cur] + "）" : "");
            var row = Btn(content.transform, "ApplyLine" + i, desc, 24, 0, lines.Count * 84f - (i + 1) * 84f, 664, 72, Paper2, () => {
                ClosePanel(true);
                ApplySkinToLine(target, skin, false);
                OpenFleetTab(2);
            });
            TInk(row, Ink);
        }
    }

    void BuildShopLiveries(Transform page)
    {
        Txt(page, "线路涂装馆 · 15 款双色涂装（含流派增益 · 可看片免费领）", 22, InkSoft, 178, PgH - 235, 860, 36, TextAnchor.MiddleLeft);
        var content = FleetScroll(page, 172, PgH - 255, LineSkinNames.Length * 158f + 24, 150f);
        float height = content.sizeDelta.y;
        float cardWidth = content.sizeDelta.x;
        for (int i = 0; i < LineSkinNames.Length; i++)
        {
            int skin = i;
            bool owned = ownedLineSkins.Contains(skin);
            bool freeScenery = !owned && IsSkinFreeByScenery(skin);
            var card = Rect("LiveryShop" + i, content, 0, height - 148 - i * 158, cardWidth, 142);
            Rounded(card, CardBg); SoftShadow(card);
            Img(Rect("Swatch", card.transform, 20, 41, 60, 60), LineSkinColors[i], spriteCircle).raycastTarget = false;
            Img(Rect("Trim", card.transform, 36, 57, 28, 28), LineSkinTrimColors[i], spriteCircle).raycastTarget = false;
            Txt(card.transform, LineSkinNames[i] + "  ·  " + SkinCategoryName(i), 27, Ink, 98, 76, 430, 40, TextAnchor.MiddleLeft);
            Txt(card.transform, SkinPerkSummary(i) + (freeScenery ? "（已达成名胜打卡免费领）" : ""), 21, InkSoft, 98, 28, 430, 34, TextAnchor.MiddleLeft);
            if (owned)
            {
                var apply = Btn(card.transform, "Apply" + i, "装配到线路", 23, cardWidth - 220, 33, 196, 76, Matcha, () => ChooseLineForSkin(skin));
                TInk(apply, Color.white);
            }
            else
            {
                var buy = Btn(card.transform, "Buy" + i, freeScenery ? "免费领" : "¥" + LineSkinPrices[i], 22, cardWidth - 336, 33, 150, 76, freeScenery ? Matcha : Paper2, () => {
                    if (!freeScenery && coins < LineSkinPrices[skin]) { Toast("金币不足，还差 ¥" + FormatShortfall(LineSkinPrices[skin] - coins)); return; }
                    BeginUndo("解锁线路涂装");
                    if (!freeScenery) coins -= LineSkinPrices[skin];
                    ownedLineSkins.Add(skin);
                    EndUndo(); RefreshHUD(); OpenFleetTab(2); Toast("已解锁涂装「" + LineSkinNames[skin] + "」");
                });
                TInk(buy, freeScenery ? Color.white : Ink);
                var ad = Btn(card.transform, "Ad" + i, "看片解锁", 22, cardWidth - 174, 33, 150, 76, Indigo, () => {
                    PromptRewardedAd("skin_" + skin, 1, LineSkinNames[skin], () => {
                        ownedLineSkins.Add(skin); SaveGame(); OpenFleetTab(2); Toast("已永久解锁「" + LineSkinNames[skin] + "」");
                    });
                });
                TInk(ad, Color.white);
            }
        }
    }

    void BuildShopThemes(Transform page)
    {
        Txt(page, "全局界面风格馆 · 更换纸面、夜图与 UI 色系；购买前可免费实机预览", 22, InkSoft, 178, PgH - 235, 860, 36, TextAnchor.MiddleLeft);
        var content = FleetScroll(page, 172, PgH - 255, Themes.Length * 256f + 24, 150f);
        float height = content.sizeDelta.y;
        float cardWidth = content.sizeDelta.x;
        for (int i = 0; i < Themes.Length; i++)
        {
            int t = i;
            var spec = Themes[i];
            bool owned = ownedThemes.Contains(t);
            bool trial = !owned && IsThemeTrialActive(t);
            bool avail = owned || trial;
            bool active = currentTheme == t && avail && previewRestoreTheme < 0;

            var card = Rect("ThemeShop" + i, content, 0, height - 240 - i * 256, cardWidth, 232);
            Rounded(card, CardBg); SoftShadow(card);
            // 主题色条与4色调色板圆点预览
            Img(Rect("Accent", card.transform, 0, 22, 8, 188), spec.indigo, spriteRound).raycastTarget = false;
            Color[] chips = { spec.paper, spec.sea, spec.indigo, spec.matcha };
            for (int c = 0; c < chips.Length; c++)
                Img(Rect("Chip" + c, card.transform, 28 + c * 38, 152, 28, 28), chips[c], spriteCircle).raycastTarget = false;

            string statusTag = owned ? (active ? "使用中 ✓" : "已拥有") : trial ? ThemeTrialRemainingText(t) : "未解锁";
            Txt(card.transform, spec.name, 31, Ink, 200, 148, 340, 44, TextAnchor.MiddleLeft);

            if (spec.isNeon)
            {
                var miniTex = EraPosterGenerator.GetCustomPoster("shop_theme_3d", "3D", 0, 4, 256, 112, 0);
                CreateNeonPosterFrame(card.transform, "NeonThemeThumb", cardWidth - 224f, 100f, 196f, 88f, miniTex, 4f);
                Txt(card.transform, statusTag, 21, active ? Matcha : InkSoft, cardWidth - 450, 152, 210, 36, TextAnchor.MiddleRight);
                Txt(card.transform, spec.subtitle, 21, InkSoft, 28, 96, cardWidth - 266, 40, TextAnchor.MiddleLeft);
            }
            else
            {
                Txt(card.transform, statusTag, 22, active ? Matcha : InkSoft, cardWidth - 270, 152, 240, 36, TextAnchor.MiddleRight);
                Txt(card.transform, spec.subtitle, 22, InkSoft, 28, 96, cardWidth - 56, 40, TextAnchor.MiddleLeft);
            }

            if (avail)
            {
                float btnW = owned ? cardWidth - 56 : 360f;
                var useBtn = Btn(card.transform, "UseTheme" + i, active ? "当前正在使用" : "立即应用此风格", 24, 28, 18, btnW, 64, active ? Matcha : Indigo, () => {
                    if (active) { Toast("当前已经是该风格"); return; }
                    ApplyTheme(t); SaveGame(); OpenFleetTab(2); Toast("已切换为「" + spec.name + "」");
                });
                TInk(useBtn, Color.white);
            }

            if (!owned)
            {
                string adKey = "theme_" + t;
                int req = t >= 3 ? 5 : 3;
                int watched = adWatchCounts.TryGetValue(adKey, out int wc) ? wc : 0;

                if (!avail)
                {
                    float pvW = 176f;
                    var pvBtn = Btn(card.transform, "PreviewTheme" + i, "预览效果", 21, 28, 18, pvW, 64, new Color(0.12f, 0.50f, 0.64f), () => {
                        StartThemePreview(t);
                    });
                    TInk(pvBtn, Color.white);

                    float adX = 28f + pvW + 14f;
                    float adW = 344f;
                    string adTxt = "看片试用24h (" + watched + "/" + req + " 永久)";
                    var adBtn = Btn(card.transform, "AdTheme" + i, adTxt, 20, adX, 18, adW, 64, Indigo, () => {
                        PromptRewardedAd(adKey, req, spec.name,
                            () => {
                                ownedThemes.Add(t);
                                ApplyTheme(t); SaveGame(); OpenFleetTab(2);
                                Toast("已永久解锁「" + spec.name + "」");
                            },
                            () => {
                                themeTrialUntil[t] = NowUnixSeconds() + 86400;
                                ApplyTheme(t); SaveGame(); OpenFleetTab(2);
                                Toast("已激活「" + spec.name + "」24小时免费试用！");
                            });
                    });
                    TInk(adBtn, Color.white);

                    float altX = adX + adW + 14f;
                    float altW = cardWidth - altX - 28f;
                    bool techFree = t == 2 && techDone.Count >= 5;
                    int diamondCost = t == 4 ? 50 : 30;
                    string altLabel = t == 1 ? "¥800 解锁" : t == 2 ? (techFree ? "免费领(科技≥5)" : "¥800 或 5科技") : (diamondCost + " 钻解锁");
                    var altBtn = Btn(card.transform, "AltTheme" + i, altLabel, 20, altX, 18, altW, 64, techFree ? Matcha : Paper2, () => {
                        if (t == 1)
                        {
                            if (coins < 800) { Toast("金币不足，还差 ¥" + FormatShortfall(800 - coins)); return; }
                            BeginUndo("解锁界面风格"); coins -= 800; ownedThemes.Add(t); EndUndo();
                        }
                        else if (t == 2)
                        {
                            if (techDone.Count < 5)
                            {
                                if (coins < 800) { Toast("研究满 5 项科技可免费领，或花 ¥800 解锁"); return; }
                                BeginUndo("解锁界面风格"); coins -= 800; ownedThemes.Add(t); EndUndo();
                            }
                            else { ownedThemes.Add(t); }
                        }
                        else
                        {
                            if (diamonds < diamondCost) { Toast("钻石不足，还差 " + Mathf.CeilToInt(diamondCost - diamonds) + " 颗"); return; }
                            BeginUndo("解锁界面风格"); diamonds -= diamondCost; ownedThemes.Add(t); EndUndo();
                        }
                        ApplyTheme(t); RefreshHUD(); SaveGame(); OpenFleetTab(2);
                        Toast("已永久解锁「" + spec.name + "」");
                    });
                    TInk(altBtn, techFree ? Color.white : Ink);
                }
                else
                {
                    float adX = 404f;
                    float adW = cardWidth - 432f;
                    string adTxt = "看片续期/永久 (" + watched + "/" + req + ")";
                    var adBtn = Btn(card.transform, "AdTheme" + i, adTxt, 21, adX, 18, adW, 64, Indigo, () => {
                        PromptRewardedAd(adKey, req, spec.name,
                            () => {
                                ownedThemes.Add(t);
                                ApplyTheme(t); SaveGame(); OpenFleetTab(2);
                                Toast("已永久解锁「" + spec.name + "」");
                            },
                            () => {
                                themeTrialUntil[t] = NowUnixSeconds() + 86400;
                                ApplyTheme(t); SaveGame(); OpenFleetTab(2);
                                Toast("已续期「" + spec.name + "」24小时试用！");
                            });
                    });
                    TInk(adBtn, Color.white);
                }
            }
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
        var page = OpenPage("每日任务", "每日零点刷新；今日净增连通 2 站即可领奖。", "PageArt/album_watercolor");
        Txt(page.transform, "今日旅程", 38, Ink, 36, PgH-245, 600, 54, TextAnchor.MiddleLeft);
        Txt(page.transform, "DAY " + day + "  ·  每日零点刷新", 22, InkSoft, 38, PgH-287, 700, 34, TextAnchor.MiddleLeft);
        var card = Rect("DailyTaskCard", page.transform, 28, PgH-760, W-56, 420);
        Rounded(card, CardBg); SoftShadow(card); JournalTape(card.transform, 440, 403, 144);
        int progress = Mathf.Clamp(DailyConnectedCount() - dailyStationBaseline, 0, 2);
        bool claimedToday = dailyTaskClaimedDay == day;
        Txt(card.transform, "让旅程更远一点", 38, Ink, 38, 324, 760, 50, TextAnchor.MiddleLeft);
        Txt(card.transform, "今日净增连通 2 座车站（赏金：+¥600 · +12 RP · +2 钻）", 25, InkSoft, 40, 270, 940, 38, TextAnchor.MiddleLeft);
        Img(Rect("Track", card.transform, 40, 212, 780, 16), new Color(0.89f,0.87f,0.81f), spriteRound).raycastTarget = false;
        if (progress > 0) Img(Rect("Progress", card.transform, 40, 212, 780 * progress / 2f, 16), Matcha, spriteRound).raycastTarget = false;
        Txt(card.transform, progress + " / 2", 30, Ink, 840, 196, 140, 45, TextAnchor.MiddleRight);
        string statusText = claimedToday ? "今日任务已完成，明日零点再来" : progress >= 2 ? "目标达成！可领取建设补贴" : "向下一站延伸路网吧";
        Txt(card.transform, statusText, 24, (claimedToday || progress >= 2) ? Matcha : InkSoft, 40, 148, 900, 36, TextAnchor.MiddleLeft);
        if (claimedToday)
        {
            var doneBtn = Btn(card.transform, "ClaimedDone", "今日补贴已领取  ✓", 28, 40, 34, W-136, 90, new Color(0.84f, 0.85f, 0.81f), () => Toast("今日补贴已领取，明日零点刷新"));
            TInk(doneBtn, InkSoft);
        }
        else if (progress < 2)
        {
            var todoBtn = Btn(card.transform, "ClaimLocked", "再连通 " + (2 - progress) + " 座车站即可领奖", 27, 40, 34, W-136, 90, new Color(0.89f, 0.87f, 0.82f), () => Toast("还差 " + (2 - progress) + " 座新增连通车站"));
            TInk(todoBtn, InkSoft);
        }
        else
        {
            var claimBtn = Btn(card.transform, "ClaimNormal", "领取  +¥600 · +12 RP · +2 钻", 24, 40, 34, 444, 90, Matcha, () => {
                if (dailyTaskClaimedDay == day) return;
                coins += 600; rp += 12; diamonds += 2; dailyTaskClaimedDay = day;
                SaveGame(); RefreshHUD(); OpenDailyTasks();
                Toast("每日补贴到账 +¥600 · +12 RP · +2 钻");
            });
            TInk(claimBtn, Color.white);
            var dblBtn = Btn(card.transform, "ClaimDouble", "看片双倍  +¥1,200 · +24 RP · +4 钻", 22, 504, 34, 480, 90, new Color(0.76f, 0.50f, 0.16f), () => {
                PromptRewardedAd("daily_double_" + day, 1, "每日任务双倍补贴 (+¥1,200 · +24 RP · +4 钻)", () => {
                    if (dailyTaskClaimedDay == day) return;
                    coins += 1200; rp += 24; diamonds += 4; dailyTaskClaimedDay = day;
                    SaveGame(); RefreshHUD(); OpenDailyTasks();
                    Toast("双倍每日补贴到账 +¥1,200 · +24 RP · +4 钻");
                });
            });
            TInk(dblBtn, Color.white);
        }
    }
    GameObject CreateNeonPosterFrame(Transform parent, string name, float x, float y, float w, float h, Texture2D posterTex, float pad = 8f)
    {
        var frame = Rect(name, parent, x, y, w, h);
        Rounded(frame, new Color(0.025f, 0.025f, 0.045f, 1f));
        SoftShadow(frame);
        frame.GetComponent<Image>().raycastTarget = false;
        var rawGo = Rect("PosterRaw", frame.transform, pad, pad, w - pad * 2f, h - pad * 2f);
        var raw = rawGo.AddComponent<RawImage>();
        raw.texture = posterTex;
        raw.color = Color.white;
        raw.raycastTarget = false;
        return frame;
    }

    void OpenLeaderboard()
    {
        var page = OpenPage("排行榜", "查看湾区铁路建设规模与个人运营档案。", "PageArt/album_watercolor");
        Txt(page.transform, "路网名录", 38, Ink, 36, PgH - 245, 800, 54, TextAnchor.MiddleLeft);
        Txt(page.transform, "RAILWAY ARCHIVES", 22, InkSoft, 38, PgH - 287, 700, 34, TextAnchor.MiddleLeft);
        float cardW = W - 56f;
        var note = Rect("RankingNote", page.transform, 28, PgH - 880, cardW, 540);
        Rounded(note, CardBg); SoftShadow(note); JournalTape(note.transform, cardW * 0.5f - 72f, 523, 144);

        int palette = currentRegion == "澳门" ? 2 : currentRegion == "广东" ? 1 : 0;
        var posterTex = EraPosterGenerator.GetCustomPoster("rank_" + currentRegion, "RAILWAY", palette, 4, 768, 248, 1);
        var posterFrame = CreateNeonPosterFrame(note.transform, "RankingNeonPoster", 26f, 224f, cardW - 52f, 284f, posterTex, 9f);
        var badge = Rect("RegionTag", posterFrame.transform, 20f, 18f, 198f, 34f);
        Rounded(badge, new Color(0.04f, 0.04f, 0.08f, 0.78f));
        badge.GetComponent<Image>().raycastTarget = false;
        Txt(badge.transform, "版图 · " + DisplayPlaceAliases(currentRegion), 18, new Color(0.96f, 0.97f, 1f, 0.95f), 0f, 0f, 198f, 34f, TextAnchor.MiddleCenter).raycastTarget = false;

        Txt(note.transform, companyName + " · 荣誉档案", 36, Ink, 36, 152, cardW - 72f, 52, TextAnchor.MiddleLeft);
        Txt(note.transform, "全网榜单筹备中，每一次扩线与日利润增长都会记入公司史册。", 24, InkSoft, 38, 96, cardW - 76f, 40, TextAnchor.MiddleLeft);
        Txt(note.transform, "当前版图  " + DisplayPlaceAliases(currentRegion) + "   ·   已连通  " + DailyConnectedCount() + " 站   ·   预期日利  ¥" + Mathf.RoundToInt(income), 25, Matcha, 38, 34, cardW - 76f, 42, TextAnchor.MiddleLeft);
    }

    struct NoticeEntry
    {
        public string title;
        public string body;
        public string date;
    }

    // 公告数据列表（约定最新公告置于索引 0，打开时默认仅展开索引 0 且全局互斥同时只开一条）
    static readonly NoticeEntry[] Announcements =
    {
        new NoticeEntry
        {
            title = "1号公告",
            date = "2026年11月13日",
            body = "暂无公告\n特此公告\n2026年11月13日"
        }
    };

    void OpenNoticeBoard()
    {
        noticeReadThisSession = true;
        if (noticeUnreadDot) noticeUnreadDot.SetActive(false);

        const float popupW = 860f;
        const float popupH = 920f;
        var panel = OpenPopup("公司公告", popupH, popupW);
        int total = Announcements != null ? Announcements.Length : 0;
        Txt(panel.transform, "铁路运营部告示 · 共 " + total + " 则", 22, InkSoft, 28, popupH - 108, popupW - 56, 30, TextAnchor.MiddleLeft);
        Img(Rect("NoticeHeaderLine", panel.transform, 28, popupH - 118, popupW - 56, 2), new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.16f)).raycastTarget = false;

        float viewW = popupW - 56f;
        float viewH = popupH - 148f;
        var view = Rect("NoticeScrollView", panel.transform, 28, 22, viewW, viewH);
        Img(view, new Color(1, 1, 1, 0)).raycastTarget = true;
        view.AddComponent<RectMask2D>();
        var content = Rect("NoticeListContent", view.transform, 0, 0, viewW, viewH);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(0, 1);
        contentRt.pivot = new Vector2(0, 1); contentRt.anchoredPosition = Vector2.zero;
        var scroll = view.AddComponent<ScrollRect>();
        scroll.viewport = view.GetComponent<RectTransform>();
        scroll.content = contentRt;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.scrollSensitivity = 90f; scroll.movementType = ScrollRect.MovementType.Elastic;

        int expandedIndex = total > 0 ? 0 : -1;

        void RebuildNoticeList()
        {
            for (int c = content.transform.childCount - 1; c >= 0; c--)
                Destroy(content.transform.GetChild(c).gameObject);

            if (total == 0)
            {
                var emptyCard = Rect("NoticeEmpty", content.transform, 0, -140, viewW, 140);
                Rounded(emptyCard, new Color(Paper2.r, Paper2.g, Paper2.b, 0.55f));
                Txt(emptyCard.transform, "当前暂无公司公告", 26, InkSoft, 24, 0, viewW - 48, 140, TextAnchor.MiddleCenter);
                contentRt.sizeDelta = new Vector2(viewW, viewH);
                return;
            }

            const float headerH = 88f;
            const float gap = 16f;
            float cursorY = 8f;

            for (int i = 0; i < total; i++)
            {
                int idx = i;
                var item = Announcements[i];
                bool isExpanded = idx == expandedIndex;
                float bodyTextW = viewW - 84f;
                float bodyH = 0f;
                if (isExpanded)
                {
                    float rawH = MeasureTextHeight(item.body, 26, bodyTextW) * 1.32f + 28f;
                    bodyH = Mathf.Max(136f, rawH);
                }
                float cardH = headerH + (isExpanded ? bodyH + 28f : 0f);
                cursorY += cardH;

                var card = Rect("NoticeItem_" + idx, content.transform, 0, -cursorY, viewW, cardH);
                var cardRt = card.GetComponent<RectTransform>();
                cardRt.anchorMin = new Vector2(0, 1); cardRt.anchorMax = new Vector2(0, 1);
                cardRt.pivot = new Vector2(0, 0);
                Rounded(card, isExpanded ? new Color(1f, 0.995f, 0.97f, 0.99f) : new Color(Paper2.r, Paper2.g, Paper2.b, 0.72f));
                if (isExpanded) SoftShadow(card);

                var accentCol = idx == 0 ? Vermilion : (isExpanded ? Indigo : InkSoft);
                var ribbon = Rect("Ribbon", card.transform, 0, 8, 6, cardH - 16);
                Img(ribbon, accentCol, spriteRound).raycastTarget = false;

                var headerGo = Rect("Header", card.transform, 0, cardH - headerH, viewW, headerH);
                var headerBg = Img(headerGo, new Color(1, 1, 1, 0));
                var headerBtn = headerGo.AddComponent<Button>();
                headerBtn.targetGraphic = headerBg;
                headerBtn.onClick.AddListener(() => {
                    expandedIndex = (expandedIndex == idx) ? -1 : idx;
                    RebuildNoticeList();
                });

                float titleX = 28f;
                if (idx == 0)
                {
                    var latestPill = Rect("LatestTag", headerGo.transform, titleX, (headerH - 34f) * 0.5f, 70f, 34f);
                    Rounded(latestPill, new Color(Vermilion.r, Vermilion.g, Vermilion.b, 0.14f));
                    var latestTxt = Txt(latestPill.transform, "最新", 18, Vermilion, 0, 0, 70f, 34f, TextAnchor.MiddleCenter);
                    latestTxt.fontStyle = FontStyle.Normal;
                    titleX += 84f;
                }

                var titleTxt = Txt(headerGo.transform, item.title, 30, Ink, titleX, 0, viewW - titleX - 240f, headerH, TextAnchor.MiddleLeft);
                titleTxt.fontStyle = FontStyle.Normal;
                FitSingleLine(titleTxt, 30);

                if (!string.IsNullOrEmpty(item.date))
                {
                    Txt(headerGo.transform, item.date, 20, InkSoft, viewW - 235f, 0, 175f, headerH, TextAnchor.MiddleRight);
                }
                Txt(headerGo.transform, isExpanded ? "▾" : "▸", 26, isExpanded ? Ink : InkSoft, viewW - 56f, 0, 36f, headerH, TextAnchor.MiddleCenter);

                if (isExpanded)
                {
                    Img(Rect("Sep", card.transform, 28, cardH - headerH - 2f, viewW - 56f, 2f), new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.14f)).raycastTarget = false;
                    var bodyBg = Rect("BodyPaper", card.transform, 24, 16, viewW - 48f, bodyH);
                    Rounded(bodyBg, new Color(0.965f, 0.945f, 0.905f, 0.72f));
                    bodyBg.GetComponent<Image>().raycastTarget = false;
                    var bodyTxt = Txt(bodyBg.transform, item.body, 26, Ink, 20, 10, bodyTextW, bodyH - 20f, TextAnchor.UpperLeft);
                    bodyTxt.lineSpacing = 1.32f;
                }

                cursorY += gap;
            }

            contentRt.sizeDelta = new Vector2(viewW, Mathf.Max(viewH, cursorY + 8f));
        }

        RebuildNoticeList();
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
        EnsureCompanyInitialized();
        companyTab = Mathf.Clamp(companyTab, 0, 3);
        var pg = OpenPage("公司", "总社运营台 · 统筹董事任命、周期风向、债券招标与效能特权。", "PageArt/company_watercolor");

        string[] tabNames = { "总览 · 社务", "高管 · 董事会", "资本 · 招投标", "特权 · 效率厅" };
        for (int i = 0; i < tabNames.Length; i++)
        {
            int tabIdx = i;
            bool active = companyTab == tabIdx;
            var tBtn = Btn(pg.transform, "CompanyTab_" + i, tabNames[i], 23, 28f + i * 260f, PgH - 252f, 244f, 64f,
                active ? Indigo : CardBg, () => { companyTab = tabIdx; OpenCompanyScreen(); });
            TInk(tBtn, active ? Color.white : Ink);
            if (!active) SoftShadow(tBtn.gameObject);
        }

        float cardW = W - 56f;
        if (companyTab == 0) BuildCompanyOverviewTab(pg.transform, cardW);
        else if (companyTab == 1) BuildCompanyBoardTab(pg.transform, cardW);
        else if (companyTab == 2) BuildCompanyFinanceTab(pg.transform, cardW);
        else BuildCompanyPrivilegeTab(pg.transform, cardW);
    }

    void BuildCompanyOverviewTab(Transform page, float cardW)
    {
        float totalH = 1500f;
        var content = FleetScroll(page, 36f, PgH - 270f, totalH);
        float topY = content.sizeDelta.y;
        float cardX = 4f, innerW = cardW - 8f, cW = innerW - 64f;

        // 1. 品牌名片卡（朱红印章 + 头衔 + 在线改名 + 升阶坐标 + 核心损益）
        float y1 = topY - 12f - 366f;
        var brandCard = Rect("CompanyBrandCard", content, cardX, y1, innerW, 366f);
        Rounded(brandCard, CardBg); SoftShadow(brandCard); JournalTape(brandCard.transform, innerW * 0.5f - 72f, 349f, 144f);

        int sealIdx = Mathf.Clamp(companySeal, 0, CompanySealChars.Length - 1);
        Color sealBg = sealIdx >= 6 ? new Color(0.76f, 0.50f, 0.16f) : Vermilion;
        var sealBtn = Btn(brandCard.transform, "SealButton", CompanySealChars[sealIdx], 68, 32f, 182f, 138f, 138f, sealBg, OpenCompanySealPicker);
        TInk(sealBtn, Color.white);
        Txt(brandCard.transform, "换印", 20, sealBg, 32f, 144f, 138f, 32f, TextAnchor.MiddleCenter);

        Txt(brandCard.transform, "头衔 · " + CompanyTitle() + "  ·  " + CreditTierLabels[CompanyCreditTier()] + "信用（已连 " + DailyConnectedCount() + " 站）",
            22, Matcha, 196f, 298f, 788f, 36f, TextAnchor.MiddleLeft);

        var nameBox = Rect("CompanyNameInputBox", brandCard.transform, 196f, 222f, 620f, 66f);
        Rounded(nameBox, Color.Lerp(CardBg, Ink, 0.06f));
        var nameTxt = Txt(nameBox.transform, companyName, 31, Ink, 18f, 0f, 584f, 66f, TextAnchor.MiddleLeft);
        nameTxt.supportRichText = false;
        var nameInput = nameBox.AddComponent<InputField>();
        nameInput.textComponent = nameTxt;
        nameInput.lineType = InputField.LineType.SingleLine;
        nameInput.characterLimit = 12;
        nameInput.text = companyName;
        nameInput.onEndEdit.AddListener(val => {
            string trimmed = string.IsNullOrWhiteSpace(val) ? "湾区铁道社" : val.Trim();
            companyName = trimmed;
            nameInput.text = companyName;
            SaveGame();
            Toast("企业字号已更名为「" + companyName + "」");
        });
        var renameBtn = Btn(brandCard.transform, "RenameBtn", "改名", 24, 832f, 222f, 152f, 66f, Indigo, () => {
            nameInput.Select();
            nameInput.ActivateInputField();
        });
        TInk(renameBtn, Color.white);

        int activePartners = boardPartners.Count + (consultantDaysLeft > 0 ? 1 : 0);
        Txt(brandCard.transform, "高管 " + activePartners + " 人   ·   运营 " + lines.Count + " 线   ·   名胜 " + FoundCount() + " 处   ·   科技 " + techDone.Count + " 项",
            22, InkSoft, 196f, 160f, 788f, 38f, TextAnchor.MiddleLeft);
        Txt(brandCard.transform, NextCompanyRankHint(), 20, Indigo, 196f, 120f, 788f, 34f, TextAnchor.MiddleLeft);

        float halfStatW = (cW - 20f) * 0.5f;
        var statLeft = Rect("StatProfit", brandCard.transform, 32f, 24f, halfStatW, 68f);
        Rounded(statLeft, Color.Lerp(CardBg, Ink, 0.04f));
        Txt(statLeft.transform, "预期日利润  " + FormatSignedCoins(income), 24, income >= 0f ? Matcha : Vermilion, 16f, 0f, halfStatW - 32f, 68f, TextAnchor.MiddleLeft);

        var statRight = Rect("StatProperty", brandCard.transform, 32f + halfStatW + 20f, 24f, halfStatW, 68f);
        Rounded(statRight, Color.Lerp(CardBg, Ink, 0.04f));
        Txt(statRight.transform, "站前物业  " + DevelopedCommercialCount() + " 处（+¥" + FormatCoins(StationCommercialTotalDaily()) + "/日）", 23, Indigo, 16f, 0f, halfStatW - 32f, 68f, TextAnchor.MiddleLeft);

        // 2. 《湾区行风周报》周期剪报卡
        float y2 = y1 - 22f - 252f;
        var newsCard = Rect("EconomyCycleCard", content, cardX, y2, innerW, 252f);
        Rounded(newsCard, CardBg); SoftShadow(newsCard); JournalTape(newsCard.transform, innerW * 0.5f - 72f, 235f, 144f);
        int weekNum = ((day - 1) / 7) + 1;
        Txt(newsCard.transform, "《湾区行风周报》第 " + weekNum + " 周 · " + EconomyCycleNames[economyCycle], 28, Ink, 32f, 186f, 670f, 44f, TextAnchor.MiddleLeft);
        Txt(newsCard.transform, "下周切换还剩 " + CycleDaysRemaining() + " 日", 22, Vermilion, 704f, 188f, 280f, 38f, TextAnchor.MiddleRight);
        Txt(newsCard.transform, EconomyCycleDescs[economyCycle], 23, InkSoft, 32f, 114f, cW, 62f, TextAnchor.UpperLeft);

        var previewBar = Rect("NextCyclePreview", newsCard.transform, 32f, 22f, cW, 74f);
        Rounded(previewBar, Color.Lerp(CardBg, Indigo, 0.08f));
        Txt(previewBar.transform, "下周风向：" + EconomyCycleNames[nextEconomyCycle] + " — " + EconomyCycleDescs[nextEconomyCycle],
            20, Indigo, 16f, 6f, cW - 32f, 62f, TextAnchor.MiddleLeft);

        // 3. 社长经营方针（4 选 1 全局策略卡）
        float y3 = y2 - 22f - 436f;
        var policyCard = Rect("CompanyPolicyCard", content, cardX, y3, innerW, 436f);
        Rounded(policyCard, CardBg); SoftShadow(policyCard);
        Txt(policyCard.transform, "社长经营方针", 30, Ink, 32f, 372f, 420f, 44f, TextAnchor.MiddleLeft);
        bool policyLockedToday = lastPolicyDay == day;
        Txt(policyCard.transform, policyLockedToday ? "今日已定策 · 明日零点可调" : "每日限调 1 次", 22, policyLockedToday ? Vermilion : Matcha, 460f, 374f, 524f, 40f, TextAnchor.MiddleRight);

        for (int i = 0; i < CompanyPolicyNames.Length; i++)
        {
            int idx = i;
            bool active = companyPolicy == idx;
            float px = 32f + (i % 2) * (halfStatW + 20f);
            float py = (i < 2) ? 194f : 22f;
            Color pBg = active ? Color.Lerp(CardBg, Indigo, 0.18f) : Color.Lerp(CardBg, Ink, 0.05f);
            var pBtn = Btn(policyCard.transform, "Policy_" + idx, "", 20, px, py, halfStatW, 158f, pBg, () => {
                if (companyPolicy == idx) { Toast("当前正实行「" + CompanyPolicyNames[idx] + "」"); return; }
                if (lastPolicyDay == day) { Toast("经营方针每日限变更 1 次"); return; }
                BeginUndo("切换经营方针");
                companyPolicy = idx;
                lastPolicyDay = day;
                operatingRates.Clear();
                RecalcIncome();
                EndUndo(); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                Toast("已切换为「" + CompanyPolicyNames[idx] + "」方针");
            });
            var titleTxt = Txt(pBtn.transform, CompanyPolicyNames[idx] + (active ? "  ✓ 生效中" : ""), 26, active ? Indigo : Ink, 18f, 102f, halfStatW - 36f, 42f, TextAnchor.MiddleLeft);
            titleTxt.raycastTarget = false;
            var descTxt = Txt(pBtn.transform, CompanyPolicyDescs[idx], 20, InkSoft, 18f, 12f, halfStatW - 36f, 84f, TextAnchor.UpperLeft);
            descTxt.raycastTarget = false;
        }

        // 4. 年度铁道大赏 + 站前商业物业导览
        float y4 = y3 - 22f - 344f;
        var honorCard = Rect("CompanyHonorAndProp", content, cardX, y4, innerW, 344f);
        Rounded(honorCard, CardBg); SoftShadow(honorCard);

        int awardCycle = (day / 15) * 15;
        bool awardReady = day >= 15 && lastAwardDay < awardCycle;
        var awardRow = Rect("AnnualAwardRow", honorCard.transform, 32f, 212f, cW, 106f);
        Rounded(awardRow, awardReady ? Color.Lerp(CardBg, new Color(0.95f, 0.72f, 0.20f), 0.25f) : Color.Lerp(CardBg, Ink, 0.04f));
        Txt(awardRow.transform, "大湾区年度铁道大赏（每 15 日授勋）", 25, Ink, 18f, 58f, 620f, 36f, TextAnchor.MiddleLeft);
        string awardHint = awardReady
            ? "第 " + awardCycle + " 日大赏揭晓，贺礼待领取"
            : "距下届评比还有 " + (15 - (day % 15)) + " 日";
        Txt(awardRow.transform, awardHint, 20, awardReady ? Vermilion : InkSoft, 18f, 14f, 620f, 36f, TextAnchor.MiddleLeft);
        var awardBtn = Btn(awardRow.transform, "OpenAwardBtn", awardReady ? "立即领赏" : "查看评比", 22, cW - 280f, 17f, 260f, 72f,
            awardReady ? new Color(0.76f, 0.50f, 0.16f) : Indigo, OpenAnnualAwardPopup);
        TInk(awardBtn, Color.white);

        int devCount = DevelopedCommercialCount();
        Txt(honorCard.transform, "站前物业（已落成 " + devCount + " 处）", 27, Ink, 32f, 154f, 800f, 40f, TextAnchor.MiddleLeft);
        string propSummary = "";
        int shown = 0;
        for (int i = 0; i < stations.Count; i++)
        {
            var st = stations[i];
            if (st.facLevel <= 0) continue;
            if (shown < 3)
            {
                if (shown > 0) propSummary += "、";
                int lvl = Mathf.Clamp(st.facLevel, 0, StationFacilityNames.Length - 1);
                int baseRent = StationFacilityRents[lvl];
                propSummary += StationDisplayName(i, currentRegion) + "(" + StationFacilityNames[lvl] + " ¥" + Mathf.RoundToInt(baseRent * TechMult() * (HasPartner(1) ? 1.25f : 1f)) + "/日)";
            }
            shown++;
        }
        if (shown > 3) propSummary += " 等共 " + shown + " 处";
        if (shown == 0) propSummary = "暂未开发物业。点选地图车站「详情」可投资商铺、商业街或都会广场。";
        Txt(honorCard.transform, propSummary, 21, InkSoft, 32f, 94f, cW, 56f, TextAnchor.UpperLeft);

        var goMapBtn = Btn(honorCard.transform, "GoBuildCommercial", "返回地图挑选车站开发物业", 23, 32f, 20f, cW, 64f, Matcha, () => {
            ClosePage();
            Toast("点开任意已建车站「详情」即可投资站前物业");
        });
        TInk(goMapBtn, Color.white);
    }

    void BuildCompanyBoardTab(Transform page, float cardW)
    {
        float totalH = 1412f;
        var content = FleetScroll(page, 36f, PgH - 270f, totalH);
        float topY = content.sizeDelta.y;
        float cardX = 4f, innerW = cardW - 8f, cW = innerW - 64f;

        // 1. 董事会席位总览（1~3 常驻席 + 1 IAA 特聘席）
        float y1 = topY - 12f - 246f;
        var seatCard = Rect("BoardSeatSummary", content, cardX, y1, innerW, 246f);
        Rounded(seatCard, CardBg); SoftShadow(seatCard);
        int seats = BoardSeatCapacity();
        Txt(seatCard.transform, "湾区铁路董事会 · 常驻席位 " + boardPartners.Count + " / " + seats, 30, Ink, 32f, 182f, 580f, 44f, TextAnchor.MiddleLeft);
        Txt(seatCard.transform, "随公司头衔解锁第 2 / 第 3 席", 21, InkSoft, 594f, 186f, 390f, 38f, TextAnchor.MiddleRight);

        float slotW = (cW - 48f) * 0.25f;
        for (int i = 0; i < 4; i++)
        {
            float sx = 32f + i * (slotW + 16f);
            if (i < 3)
            {
                bool unlocked = i < seats;
                bool occupied = i < boardPartners.Count;
                int pid = occupied ? boardPartners[i] : -1;
                Color bg = !unlocked ? Color.Lerp(CardBg, Ink, 0.08f) : occupied ? Color.Lerp(CardBg, Indigo, 0.14f) : Color.Lerp(CardBg, Matcha, 0.10f);
                var sBtn = Btn(seatCard.transform, "SeatSlot_" + i, "", 20, sx, 22f, slotW, 140f, bg, () => {
                    if (!unlocked) { Toast(i == 1 ? "升至「城市干线局」（8站或3线）解锁" : "升至「跨海铁道社」（16站且日利润¥600）解锁"); return; }
                    if (occupied)
                    {
                        BeginUndo("卸任高管");
                        boardPartners.RemoveAt(i);
                        hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                        EndUndo(); SaveGame(); OpenCompanyScreen();
                        Toast(PartnerNames[pid] + " 已卸任");
                    }
                    else Toast("请在下方名宿档案选择任命人选");
                });
                string topT = !unlocked ? "未解锁" : occupied ? PartnerNames[pid] : "空缺常驻席";
                string subT = !unlocked ? (i == 1 ? "干线局解锁" : "跨海社解锁") : occupied ? PartnerTitles[pid] + "\n(点击卸任)" : "待从下方任命";
                var t1 = Txt(sBtn.transform, topT, 24, !unlocked ? InkSoft : occupied ? Indigo : Matcha, 10f, 82f, slotW - 20f, 40f, TextAnchor.MiddleCenter);
                t1.raycastTarget = false;
                var t2 = Txt(sBtn.transform, subT, 19, InkSoft, 10f, 14f, slotW - 20f, 64f, TextAnchor.UpperCenter);
                t2.raycastTarget = false;
            }
            else
            {
                bool hasCons = consultantDaysLeft > 0 && consultantId >= 0 && consultantId < PartnerNames.Length;
                Color cBg = hasCons ? Color.Lerp(CardBg, new Color(0.95f, 0.70f, 0.15f), 0.25f) : Color.Lerp(CardBg, new Color(0.76f, 0.50f, 0.16f), 0.12f);
                var cBtn = Btn(seatCard.transform, "ConsultantSlot", "", 20, sx, 22f, slotW, 140f, cBg, () => {
                    PromptRewardedAd("hire_huo_" + day, 1, "特聘传奇顾问·霍老督办（3日全能零负面）", () => {
                        consultantId = 6;
                        consultantDaysLeft += 3;
                        hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                        SaveGame(); OpenCompanyScreen();
                        Toast("霍老督办已赴第 4 特聘席（剩余 " + consultantDaysLeft + " 天）");
                    });
                });
                var ct1 = Txt(cBtn.transform, hasCons ? PartnerNames[consultantId] : "特聘第 4 席", 23, new Color(0.65f, 0.38f, 0.05f), 8f, 82f, slotW - 16f, 40f, TextAnchor.MiddleCenter);
                ct1.raycastTarget = false;
                var ct2 = Txt(cBtn.transform, hasCons ? "驻席加持中\n剩 " + consultantDaysLeft + " 天（点延）" : "看片空降霍老\n全增益 0 负面", 19, InkSoft, 8f, 14f, slotW - 16f, 64f, TextAnchor.UpperCenter);
                ct2.raycastTarget = false;
            }
        }

        // 2. 名宿合伙人列表（0~6）
        float y2 = y1 - 22f - 1114f;
        var listCard = Rect("PartnerRosterCard", content, cardX, y2, innerW, 1114f);
        Rounded(listCard, CardBg); SoftShadow(listCard);
        Txt(listCard.transform, "铁路名宿档案（按特质自由搭配）", 28, Ink, 32f, 1052f, 800f, 42f, TextAnchor.MiddleLeft);

        Color[] avatarColors = {
            Indigo, Matcha, new Color(0.72f, 0.45f, 0.22f),
            new Color(0.28f, 0.52f, 0.65f), Vermilion, new Color(0.55f, 0.36f, 0.62f),
            new Color(0.76f, 0.50f, 0.16f)
        };
        for (int i = 0; i < PartnerNames.Length; i++)
        {
            int pid = i;
            float ry = 900f - i * 146f;
            bool isSpecial = pid == 6;
            bool hired = isSpecial ? (consultantDaysLeft > 0 && consultantId == 6) : boardPartners.Contains(pid);
            var row = Rect("PartnerRow_" + pid, listCard.transform, 32f, ry, cW, 134f);
            Rounded(row, hired ? Color.Lerp(CardBg, Indigo, 0.10f) : Color.Lerp(CardBg, Ink, 0.04f));

            var avatar = Rect("Avatar", row.transform, 16f, 21f, 92f, 92f);
            Rounded(avatar, avatarColors[pid]);
            Txt(avatar.transform, PartnerNames[pid].Substring(0, 1), 44, Color.white, 0f, 0f, 92f, 92f, TextAnchor.MiddleCenter);

            Txt(row.transform, PartnerNames[pid] + " · " + PartnerTitles[pid], 24, Ink, 126f, 88f, 560f, 36f, TextAnchor.MiddleLeft);
            Txt(row.transform, "✓ " + PartnerPros[pid], 20, Matcha, 126f, 50f, 560f, 34f, TextAnchor.MiddleLeft);
            Txt(row.transform, "• " + PartnerCons[pid], 19, isSpecial ? Indigo : Vermilion, 126f, 14f, 560f, 32f, TextAnchor.MiddleLeft);

            if (isSpecial)
            {
                var pBtn = Btn(row.transform, "HireSpecial", hired ? "在岗 " + consultantDaysLeft + "天(延)" : "看片特聘 3 天", 21, cW - 252f, 31f, 232f, 72f, new Color(0.76f, 0.50f, 0.16f), () => {
                    PromptRewardedAd("hire_huo_" + day, 1, "特聘传奇顾问·霍老督办（3日全能零负面）", () => {
                        consultantId = 6;
                        consultantDaysLeft += 3;
                        hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                        SaveGame(); OpenCompanyScreen();
                        Toast("霍老督办已赴第 4 特聘席（剩余 " + consultantDaysLeft + " 天）");
                    });
                });
                TInk(pBtn, Color.white);
            }
            else
            {
                bool seatsFull = !hired && boardPartners.Count >= seats;
                string btnTxt = hired ? "卸任高管" : seatsFull ? "席位已满" : "任命上岗";
                Color bCol = hired ? Vermilion : seatsFull ? new Color(0.78f, 0.76f, 0.71f) : Matcha;
                var pBtn = Btn(row.transform, "Hire_" + pid, btnTxt, 22, cW - 252f, 31f, 232f, 72f, bCol, () => {
                    if (hired)
                    {
                        BeginUndo("卸任高管");
                        boardPartners.Remove(pid);
                        hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                        EndUndo(); SaveGame(); OpenCompanyScreen();
                        Toast(PartnerNames[pid] + " 已卸任");
                    }
                    else if (seatsFull) Toast("常驻席位已满（" + seats + " 席），可先卸任再任命或提升头衔");
                    else
                    {
                        BeginUndo("任命高管");
                        boardPartners.Add(pid);
                        hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                        EndUndo(); SaveGame(); OpenCompanyScreen();
                        Toast("已任命「" + PartnerNames[pid] + "」进入董事会");
                    }
                });
                TInk(pBtn, seatsFull ? InkSoft : Color.white);
            }
        }
    }

    void BuildCompanyFinanceTab(Transform page, float cardW)
    {
        float totalH = 1280f;
        var content = FleetScroll(page, 36f, PgH - 270f, totalH);
        float topY = content.sizeDelta.y;
        float cardX = 4f, innerW = cardW - 8f, cW = innerW - 64f;

        // 1. 企业信用评级与动态融资卡
        float y1 = topY - 12f - 548f;
        var finCard = Rect("CompanyCreditCard", content, cardX, y1, innerW, 548f);
        Rounded(finCard, CardBg); SoftShadow(finCard);

        int score = CompanyCreditScore();
        int tier = CompanyCreditTier();
        int maxBond = CurrentCreditBondAmount(tier);
        int stdDaily = CurrentCreditBondDaily(tier);

        Txt(finCard.transform, "企业信用评级：" + CreditTierLabels[tier] + "  （" + score + "/100）", 30, Ink, 32f, 484f, 670f, 44f, TextAnchor.MiddleLeft);
        Txt(finCard.transform, "已结清债券 " + bondsRepaid + " 笔", 22, Matcha, 704f, 486f, 280f, 38f, TextAnchor.MiddleRight);
        Txt(finCard.transform, "随连通站数、预期利润、站前物业、科技与履约记录成长；信用越优授信越高。",
            20, InkSoft, 32f, 424f, cW, 52f, TextAnchor.UpperLeft);

        // 1.1 动态基建债券区
        var bondBox = Rect("DynamicBondBox", finCard.transform, 32f, 166f, cW, 244f);
        Rounded(bondBox, Color.Lerp(CardBg, Indigo, 0.06f));
        if (bondDaysLeft > 0)
        {
            int curDaily = EffectiveBondDailyPayment();
            int redeemCost = BondEarlyRedeemCost();
            Txt(bondBox.transform, "在还基建债券（本金 ¥" + (bondPrincipal > 0 ? bondPrincipal : BondPrincipal) + "）", 26, Ink, 20f, 184f, 880f, 40f, TextAnchor.MiddleLeft);
            Txt(bondBox.transform, "剩 " + bondDaysLeft + " 期 · 每晚扣款 ¥" + curDaily + (bondSubsidized ? "（已享 IAA 贴息 85 折 ✓）" : "（余额不足将扣半延息顺延）"),
                21, bondSubsidized ? Matcha : InkSoft, 20f, 134f, 880f, 38f, TextAnchor.MiddleLeft);

            var redeemBtn = Btn(bondBox.transform, "RedeemBondBtn", "提前赎回 ¥" + redeemCost + "（省半息）", 22, 20f, 26f, 446f, 84f, Vermilion, () => {
                int cost = BondEarlyRedeemCost();
                if (coins < cost) { Toast("提前赎回需 ¥" + cost + "，金币不足"); return; }
                BeginUndo("提前赎回债券");
                coins -= cost;
                bondDaysLeft = 0; bondPrincipal = 0; bondDailyDue = 0; bondSubsidized = false; bondsRepaid++;
                EndUndo(); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                Toast("债券已提前全额赎回，信用评分提升");
            });
            TInk(redeemBtn, Color.white);

            var subBtn = Btn(bondBox.transform, "BondSubsidyBtn", bondSubsidized ? "已享 85 折贴息 ✓" : "看片获银行贴息 · 日供减 15%", 22, 486f, 26f, 446f, 84f,
                bondSubsidized ? new Color(0.84f, 0.85f, 0.81f) : new Color(0.76f, 0.50f, 0.16f), () => {
                    if (bondSubsidized) { Toast("当前债券已享 15% 银行贴息"); return; }
                    PromptRewardedAd("bond_sub_" + day + "_" + bondDaysLeft, 1, "银行贴息批文（剩余期数日供 -15%）", () => {
                        bondSubsidized = true;
                        SaveGame(); OpenCompanyScreen();
                        Toast("贴息批文生效：每期降至 ¥" + EffectiveBondDailyPayment());
                    });
                });
            TInk(subBtn, bondSubsidized ? InkSoft : Color.white);
        }
        else
        {
            Txt(bondBox.transform, "「" + CreditTierLabels[tier] + "」授信可发基建债券：¥" + maxBond, 28, Indigo, 20f, 178f, 880f, 44f, TextAnchor.MiddleLeft);
            Txt(bondBox.transform, "期限 10 日 · 每晚扣款 ¥" + stdDaily + "（按期履约可升 AAA 最高 ¥100,000）",
                21, InkSoft, 20f, 126f, 880f, 40f, TextAnchor.MiddleLeft);
            var issueBtn = Btn(bondBox.transform, "IssueCreditBondBtn", "发行 ¥" + maxBond + " 基建债券", 25, 20f, 24f, cW - 40f, 84f, Matcha, () => {
                BeginUndo("发行信用债券");
                coins += maxBond;
                bondPrincipal = maxBond;
                bondDailyDue = stdDaily;
                bondDaysLeft = BondTermDays;
                bondSubsidized = false;
                EndUndo(); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                Toast("已发行 " + CreditTierLabels[tier] + " 债券 +¥" + maxBond);
            });
            TInk(issueBtn, Color.white);
        }

        // 1.2 无息应急周转金
        var loanRow = Rect("LoanRow", finCard.transform, 32f, 28f, cW, 114f);
        Rounded(loanRow, Color.Lerp(CardBg, Ink, 0.04f));
        Txt(loanRow.transform, "无息应急周转金（破产保底）", 24, Ink, 18f, 64f, 620f, 36f, TextAnchor.MiddleLeft);
        Txt(loanRow.transform, loanOutstanding ? "尚欠周转金 ¥500（归还即恢复信用分）" : "金币低于 ¥100 且未盈利时可领 ¥500", 20, InkSoft, 18f, 16f, 636f, 40f, TextAnchor.MiddleLeft);
        var loanBtn = Btn(loanRow.transform, "LoanBtn", loanOutstanding ? "归还 ¥500" : "申请 ¥500", 23, cW - 282f, 21f, 262f, 72f, Indigo, () => {
            if (loanOutstanding)
            {
                if (coins < 500) { Toast("金币不足，暂无法还款"); return; }
                BeginUndo("归还周转金"); coins -= 500; loanOutstanding = false;
            }
            else
            {
                if (coins >= 100 || income > 0) { Toast("需金币低于 ¥100 且日利润未转正方可申请"); return; }
                BeginUndo("领取周转金"); coins += 500; loanOutstanding = true;
            }
            EndUndo(); SaveGame(); RefreshHUD(); OpenCompanyScreen();
            Toast(loanOutstanding ? "已领无息周转金 ¥500" : "周转金已还清，信用分恢复");
        });
        TInk(loanBtn, Color.white);

        // 2. 「市政特急招标合约」
        float y2 = y1 - 22f - 680f;
        var contractCard = Rect("ContractsCard", content, cardX, y2, innerW, 680f);
        Rounded(contractCard, CardBg); SoftShadow(contractCard);
        Txt(contractCard.transform, "市政特急招标合约", 30, Ink, 32f, 612f, 520f, 46f, TextAnchor.MiddleLeft);

        int rc = adWatchCounts.TryGetValue("refresh_cnt_" + day, out int rcVal) ? rcVal : 0;
        var refreshBtn = Btn(contractCard.transform, "RefreshContractsBtn", rc >= 2 ? "今日换标已上限" : "看片重投标书 ↻ (" + rc + "/2)", 22, 700f, 604f, 284f, 58f,
            rc >= 2 ? new Color(0.84f, 0.85f, 0.81f) : new Color(0.76f, 0.50f, 0.16f), () => {
                if (rc >= 2) { Toast("今日换标已达 2 次上限"); return; }
                PromptRewardedAd("refresh_cnt_" + day, 1, "重投 2 份市政招标书", () => {
                    activeContracts.Clear();
                    nextContractId += 2;
                    EnsureCompanyInitialized();
                    SaveGame(); OpenCompanyScreen();
                    Toast("已换取两份新市政标书");
                });
            });
        TInk(refreshBtn, rc >= 2 ? InkSoft : Color.white);

        for (int i = 0; i < activeContracts.Count && i < 2; i++)
        {
            var c = activeContracts[i];
            float cy = i == 0 ? 318f : 26f;
            bool ready = ContractIsReady(c);
            var box = Rect("ContractBox_" + i, contractCard.transform, 32f, cy, cW, 268f);
            Rounded(box, ready ? Color.Lerp(CardBg, Matcha, 0.12f) : Color.Lerp(CardBg, Ink, 0.04f));

            Txt(box.transform, ContractTitle(c), 26, Ink, 20f, 212f, 580f, 40f, TextAnchor.MiddleLeft);
            int daysRem = Mathf.Max(0, c.deadlineDay - day);
            Txt(box.transform, ready ? "✓ 待验收" : "剩 " + daysRem + " 天截止", 21, ready ? Matcha : Vermilion, 616f, 214f, 316f, 36f, TextAnchor.MiddleRight);
            Txt(box.transform, ContractDesc(c), 22, InkSoft, 20f, 162f, 900f, 38f, TextAnchor.MiddleLeft);

            float ratio = c.targetValue > 0 ? Mathf.Clamp01(ContractCurrentValue(c) / (float)c.targetValue) : 1f;
            Img(Rect("CTrack", box.transform, 20f, 134f, 912f, 14f), new Color(0.86f, 0.84f, 0.78f), spriteRound).raycastTarget = false;
            if (ratio > 0f) Img(Rect("CFill", box.transform, 20f, 134f, 912f * ratio, 14f), ready ? Matcha : Indigo, spriteRound).raycastTarget = false;

            Txt(box.transform, "中标赏金：+¥" + c.rewardCoins + " · +" + c.rewardRp + " RP · +" + c.rewardDiamonds + " 钻",
                21, Indigo, 20f, 90f, 900f, 34f, TextAnchor.MiddleLeft);

            if (ready)
            {
                var claimBtn = Btn(box.transform, "ClaimContract_" + c.id, "验收领奖", 23, 20f, 16f, 446f, 64f, Matcha, () => {
                    if (c.claimed) return;
                    c.claimed = true;
                    coins += c.rewardCoins; rp += c.rewardRp; diamonds += c.rewardDiamonds;
                    activeContracts.Remove(c);
                    EnsureCompanyInitialized();
                    SaveGame(); RefreshHUD(); OpenCompanyScreen();
                    Toast("中标赏金到账 +¥" + c.rewardCoins);
                });
                TInk(claimBtn, Color.white);

                int xCoins = Mathf.RoundToInt(c.rewardCoins * 1.5f);
                int xRp = Mathf.RoundToInt(c.rewardRp * 1.5f);
                int xGems = Mathf.RoundToInt(c.rewardDiamonds * 1.5f);
                var vipBtn = Btn(box.transform, "ClaimContractVip_" + c.id, "看片专访 · ×1.5 领 ¥" + xCoins, 22, 486f, 16f, 446f, 64f, new Color(0.76f, 0.50f, 0.16f), () => {
                    PromptRewardedAd("contract_vip_" + c.id, 1, "媒体头版专访（赏金/RP/钻石全额 ×1.5）", () => {
                        if (c.claimed) return;
                        c.claimed = true;
                        coins += xCoins; rp += xRp; diamonds += xGems;
                        activeContracts.Remove(c);
                        EnsureCompanyInitialized();
                        SaveGame(); RefreshHUD(); OpenCompanyScreen();
                        Toast("专访赏金到账 +¥" + xCoins + " · +" + xGems + "钻");
                    });
                });
                TInk(vipBtn, Color.white);
            }
            else
            {
                var todoBtn = Btn(box.transform, "ContractTodo_" + c.id, "工程推进中（达标后回来验收）", 21, 20f, 16f, 912f, 64f, new Color(0.86f, 0.85f, 0.80f), () => Toast("达成标书目标后即可在此验收领奖"));
                TInk(todoBtn, InkSoft);
            }
        }
    }

    void BuildCompanyPrivilegeTab(Transform page, float cardW)
    {
        float totalH = 1210f;
        var content = FleetScroll(page, 36f, PgH - 270f, totalH);
        float topY = content.sizeDelta.y;
        float cardX = 4f, innerW = cardW - 8f, cW = innerW - 64f;

        // 1. 效率特权一：AI 智能调度总控台 + 维保 -10%
        float y1 = topY - 12f - 246f;
        var dispCard = Rect("AutoDispatchPrivCard", content, cardX, y1, innerW, 246f);
        Rounded(dispCard, CardBg); SoftShadow(dispCard);
        bool hasAuto = autoDispatchDaysLeft > 0;
        Txt(dispCard.transform, "AI 智能调度总控台 · 一键黄金配班", 29, Ink, 32f, 184f, 640f, 42f, TextAnchor.MiddleLeft);
        Txt(dispCard.transform, hasAuto ? "剩余 " + autoDispatchDaysLeft + " 天" : "未激活", 22, hasAuto ? Matcha : Vermilion, 694f, 186f, 290f, 38f, TextAnchor.MiddleRight);
        Txt(dispCard.transform, "依据全网真实客流一键优化四时段配车，消灭空跑亏钱，并享全网维护费 -10%。",
            21, InkSoft, 32f, 114f, cW, 56f, TextAnchor.UpperLeft);

        if (hasAuto)
        {
            var runBtn = Btn(dispCard.transform, "RunAutoDispatch", "立即一键优化全线配班", 24, 32f, 22f, 610f, 76f, Matcha, () => {
                BeginUndo("AI一键黄金配班");
                int n = OptimizeAllLinesDispatch();
                EndUndo(); RefreshHUD();
                Toast(n > 0 ? "AI 已重新平衡 " + n + " 条线路配班，预期日利润 ¥" + Mathf.RoundToInt(income) : "当前全线已处于最优配班");
            });
            TInk(runBtn, Color.white);

            var extBtn = Btn(dispCard.transform, "ExtendAutoDispatch", "看片续期 +4 天", 22, 662f, 22f, 322f, 76f, Indigo, () => {
                PromptRewardedAd("priv_autodisp_" + day + "_" + autoDispatchDaysLeft, 1, "延长 AI 调度总控台 +4 天", () => {
                    autoDispatchDaysLeft += 4;
                    SaveGame(); OpenCompanyScreen();
                    Toast("AI 智能调度已续期至 " + autoDispatchDaysLeft + " 天");
                });
            });
            TInk(extBtn, Color.white);
        }
        else
        {
            var actBtn = Btn(dispCard.transform, "ActivateAutoDispatch", "看片解锁 4 天特权（一键优化 + 全网维保 -10%）", 23, 32f, 22f, cW, 76f, new Color(0.76f, 0.50f, 0.16f), () => {
                PromptRewardedAd("priv_autodisp_" + day, 1, "AI 智能调度总控台（4天 · 维保-10%）", () => {
                    autoDispatchDaysLeft += 4;
                    int n = OptimizeAllLinesDispatch();
                    SaveGame(); RefreshHUD(); OpenCompanyScreen();
                    Toast("AI 总控台已激活 4 天，已优化 " + n + " 条线路");
                });
            });
            TInk(actBtn, Color.white);
        }

        // 2. 效率特权二：4× 超极速推演 + 科研加速 +50%
        float y2 = y1 - 22f - 226f;
        var turboCard = Rect("TurboPrivCard", content, cardX, y2, innerW, 226f);
        Rounded(turboCard, CardBg); SoftShadow(turboCard);
        bool hasTurbo = turboDaysLeft > 0;
        Txt(turboCard.transform, "4× 极速推演牌照 · 科研加速 +50%", 29, Ink, 32f, 164f, 650f, 42f, TextAnchor.MiddleLeft);
        Txt(turboCard.transform, hasTurbo ? "剩余 " + turboDaysLeft + " 天" : "未激活", 22, hasTurbo ? Matcha : Vermilion, 694f, 166f, 290f, 38f, TextAnchor.MiddleRight);
        Txt(turboCard.transform, "开放顶栏「4× 极速」运行档位，并使科研点 RP 生成速率提升 50%。",
            21, InkSoft, 32f, 108f, cW, 44f, TextAnchor.MiddleLeft);

        var turboBtn = Btn(turboCard.transform, "TurboBtn",
            hasTurbo ? "看片续期 +4 天（顶栏 4× 档位已就绪）" : "看片开启 4 天极速推演（4× 倍速 + 科研+50%）",
            23, 32f, 22f, cW, 72f, hasTurbo ? Indigo : new Color(0.76f, 0.50f, 0.16f), () => {
                PromptRewardedAd("priv_turbo_" + day + "_" + turboDaysLeft, 1, "4× 极速推演牌照（科研+50% · 4天）", () => {
                    turboDaysLeft += 4;
                    timeSpeed = 4;
                    UpdateTimeText();
                    SaveGame(); OpenCompanyScreen();
                    Toast("4× 极速推演已开启（剩 " + turboDaysLeft + " 天）");
                });
            });
        TInk(turboBtn, Color.white);

        // 3. 财政补贴与传奇外援双卡（并排）
        float y3 = y2 - 22f - 230f;
        float halfW = (innerW - 20f) * 0.5f;

        var subCard = Rect("SubsidyPrivCard", content, cardX, y3, halfW, 230f);
        Rounded(subCard, CardBg); SoftShadow(subCard);
        int grant = AdTourismSubsidyAmount();
        bool adClaimed = lastAdSubsidyDay == day;
        Txt(subCard.transform, "每日文旅财政补贴", 25, Ink, 26f, 168f, halfW - 52f, 38f, TextAnchor.MiddleLeft);
        Txt(subCard.transform, "基础 ¥500 + 连通 " + DailyConnectedCount() + " 站×¥60（封顶 ¥5,000）\n今日可领 ¥" + grant, 20, InkSoft, 26f, 100f, halfW - 52f, 62f, TextAnchor.UpperLeft);
        var subBtn = Btn(subCard.transform, "SubsidyQuickBtn", adClaimed ? "今日补贴已领 ✓" : "看片立领 ¥" + grant, 22, 26f, 22f, halfW - 52f, 68f,
            adClaimed ? new Color(0.84f, 0.85f, 0.81f) : Matcha, () => {
                if (lastAdSubsidyDay == day) { Toast("今日补贴已领，明日再来"); return; }
                PromptRewardedAd("company_subsidy_" + day, 1, "文旅专项财政补贴 ¥" + grant, () => {
                    if (lastAdSubsidyDay == day) return;
                    coins += grant; lastAdSubsidyDay = day;
                    SaveGame(); RefreshHUD(); OpenCompanyScreen();
                    Toast("文旅专项补贴到账 +¥" + grant);
                });
            });
        TInk(subBtn, adClaimed ? InkSoft : Color.white);

        var huoCard = Rect("HuoPrivCard", content, cardX + halfW + 20f, y3, halfW, 230f);
        Rounded(huoCard, CardBg); SoftShadow(huoCard);
        bool hasHuo = consultantDaysLeft > 0 && consultantId == 6;
        Txt(huoCard.transform, "特聘传奇 · 霍老督办", 25, Ink, 26f, 168f, halfW - 52f, 38f, TextAnchor.MiddleLeft);
        Txt(huoCard.transform, "独立第 4 高管席：客流+10% · 车速+8%\n建线-10% · 维保-10%（零负面特质）", 20, InkSoft, 26f, 100f, halfW - 52f, 62f, TextAnchor.UpperLeft);
        var huoBtn = Btn(huoCard.transform, "HuoQuickBtn", hasHuo ? "已在岗(剩" + consultantDaysLeft + "天)点延" : "看片特聘 3 天", 22, 26f, 22f, halfW - 52f, 68f,
            new Color(0.76f, 0.50f, 0.16f), () => {
                PromptRewardedAd("hire_huo_" + day, 1, "特聘传奇顾问·霍老督办（3日全能零负面）", () => {
                    consultantId = 6; consultantDaysLeft += 3;
                    hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                    SaveGame(); OpenCompanyScreen();
                    Toast("霍老督办已赴第 4 特聘席");
                });
            });
        TInk(huoBtn, Color.white);

        // 4. 外观定制工坊直通车
        float y4 = y3 - 22f - 398f;
        var lookCard = Rect("LookPrivCard", content, cardX, y4, innerW, 398f);
        Rounded(lookCard, CardBg); SoftShadow(lookCard);
        Txt(lookCard.transform, "外观定制工坊 · 社徽与线网涂装", 28, Ink, 32f, 336f, 920f, 42f, TextAnchor.MiddleLeft);
        Txt(lookCard.transform, "共轨复线支持双色涂装并排呈现；典藏御印、线网涂装与手账主题均支持映像馆解锁。",
            21, InkSoft, 32f, 276f, cW, 48f, TextAnchor.MiddleLeft);

        float halfLookW = (cW - 20f) * 0.5f;
        for (int s = 6; s <= 7; s++)
        {
            int sealIndex = s;
            bool owned = IsSealUnlocked(sealIndex);
            float sx = 32f + (s - 6) * (halfLookW + 20f);
            Color scBg = Color.Lerp(CardBg, new Color(0.76f, 0.50f, 0.16f), 0.12f);
            var sBtn = Btn(lookCard.transform, "SealVipBtn_" + s, "", 20, sx, 130f, halfLookW, 124f, scBg, () => {
                if (owned)
                {
                    companySeal = sealIndex; SaveGame(); companyTab = 0; OpenCompanyScreen();
                    Toast("已换用印章「" + CompanySealNames[sealIndex] + "」");
                    return;
                }
                PromptRewardedAd("seal_" + sealIndex, 2, "典藏御印「" + CompanySealNames[sealIndex] + "」", () => {
                    companySeal = sealIndex; SaveGame(); OpenCompanyScreen();
                    Toast("典藏御印「" + CompanySealNames[sealIndex] + "」已解锁并佩戴");
                }, () => {
                    OpenCompanyScreen();
                    Toast("「" + CompanySealNames[sealIndex] + "」观影进度 1/2");
                });
            });
            var stamp = Rect("Stamp", sBtn.transform, 16f, 18f, 88f, 88f);
            Rounded(stamp, new Color(0.76f, 0.50f, 0.16f));
            Txt(stamp.transform, CompanySealChars[sealIndex], 46, Color.white, 0f, 0f, 88f, 88f, TextAnchor.MiddleCenter).raycastTarget = false;
            Txt(sBtn.transform, CompanySealNames[sealIndex] + (companySeal == sealIndex ? " ✓" : ""), 24, Ink, 122f, 68f, halfLookW - 140f, 38f, TextAnchor.MiddleLeft).raycastTarget = false;
            Txt(sBtn.transform, owned ? "已永久解锁 · 点此佩戴" : SealUnlockHint(sealIndex), 20, owned ? Matcha : Vermilion, 122f, 22f, halfLookW - 140f, 36f, TextAnchor.MiddleLeft).raycastTarget = false;
        }

        var liveryBtn = Btn(lookCard.transform, "GoLiveryShop", "进入「涂装工坊」（15 款双色复线涂装）", 22, 32f, 24f, halfLookW, 82f, Indigo, () => {
            ClosePage();
            fleetBack = () => { companyTab = 3; OpenCompanyScreen(); };
            shopCategory = 1;
            OpenFleetTab(2);
        });
        TInk(liveryBtn, Color.white);

        var themeBtn = Btn(lookCard.transform, "GoThemeShop", "进入「主题工坊」（" + Themes.Length + " 套全局风格）", 22, 32f + halfLookW + 20f, 24f, halfLookW, 82f, Vermilion, () => {
            ClosePage();
            fleetBack = () => { companyTab = 3; OpenCompanyScreen(); };
            shopCategory = 2;
            OpenFleetTab(2);
        });
        TInk(themeBtn, Color.white);
    }

    void OpenAnnualAwardPopup()
    {
        int awardCycle = (day / 15) * 15;
        bool canClaim = day >= 15 && lastAwardDay < awardCycle;
        ComputeAnnualAward(out string medalName, out string reason, out int grantCoins, out int grantDiamonds);

        var p = OpenPopup("大湾区年度铁道大赏", 760f, 920f);
        string posterTitle = awardCycle >= 15 ? ("DAY" + awardCycle) : "AWARD";
        int awardPalette = (awardCycle / 15) % 4;
        var awardTex = EraPosterGenerator.GetCustomPoster("annual_" + posterTitle, posterTitle, awardPalette, 3, 768, 248, 0);
        var posterFrame = CreateNeonPosterFrame(p.transform, "AwardNeonPoster", 36f, 396f, 848f, 258f, awardTex, 8f);
        var cornerTag = Rect("AwardCyclePill", posterFrame.transform, 598f, 16f, 232f, 34f);
        Rounded(cornerTag, new Color(0.04f, 0.04f, 0.08f, 0.78f));
        cornerTag.GetComponent<Image>().raycastTarget = false;
        Txt(cornerTag.transform, awardCycle >= 15 ? ("第 " + awardCycle + " 日纪念刊") : "首届大赏筹备", 18, new Color(1f, 0.95f, 0.86f, 0.96f), 0f, 0f, 232f, 34f, TextAnchor.MiddleCenter).raycastTarget = false;

        var banner = Rect("AwardBanner", p.transform, 36f, 286f, 848f, 96f);
        Rounded(banner, Color.Lerp(CardBg, new Color(0.95f, 0.72f, 0.20f), 0.16f));
        Img(Rect("Accent", banner.transform, 0, 12, 8, 72), new Color(0.86f, 0.56f, 0.12f), spriteRound).raycastTarget = false;
        Txt(banner.transform, "本期拟授勋：" + medalName, 30, Ink, 24f, 48f, 800f, 40f, TextAnchor.MiddleLeft);
        Txt(banner.transform, reason, 21, InkSoft, 24f, 10f, 800f, 36f, TextAnchor.MiddleLeft);

        Txt(p.transform, "评比贺礼：+¥" + grantCoins + "   ·   +" + grantDiamonds + " 钻", 25, Indigo, 36f, 230f, 848f, 40f, TextAnchor.MiddleCenter);
        Txt(p.transform, canClaim
            ? "第 " + awardCycle + " 日大赏名单已揭晓，可领发展金或接受专访翻倍。"
            : (day < 15 ? "第 15 日正式开启首次授勋（当前第 " + day + " 日）。" : "第 " + awardCycle + " 日贺礼已发，下届第 " + (awardCycle + 15) + " 日再会。"),
            22, InkSoft, 36f, 140f, 848f, 80f, TextAnchor.UpperCenter);

        if (canClaim)
        {
            var normal = Btn(p.transform, "ClaimAwardNormal", "直接领赏 +¥" + grantCoins, 24, 36f, 38f, 408f, 86f, Matcha, () => {
                if (lastAwardDay >= awardCycle) return;
                lastAwardDay = awardCycle;
                coins += grantCoins; diamonds += grantDiamonds;
                ClosePanel(true); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                Toast("获颁「" + medalName + "」+¥" + grantCoins + " · +" + grantDiamonds + "钻");
            });
            TInk(normal, Color.white);

            var vip = Btn(p.transform, "ClaimAwardVip", "看片专访 · 双倍 +¥" + (grantCoins * 2), 24, 476f, 38f, 408f, 86f, new Color(0.76f, 0.50f, 0.16f), () => {
                PromptRewardedAd("annual_award_" + awardCycle, 1, "铁道大赏头版专访（赏金与钻石双倍）", () => {
                    if (lastAwardDay >= awardCycle) return;
                    lastAwardDay = awardCycle;
                    coins += grantCoins * 2; diamonds += grantDiamonds * 2;
                    ClosePanel(true); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                    Toast("专访贺礼到账 +¥" + (grantCoins * 2) + " · +" + (grantDiamonds * 2) + "钻");
                });
            });
            TInk(vip, Color.white);
        }
        else
        {
            var ok = Btn(p.transform, "CloseAward", "返回公司总览", 25, 36f, 38f, 848f, 86f, Indigo, ClosePanel);
            TInk(ok, Color.white);
        }
    }

    void OpenCompanySealPicker()
    {
        var panel = OpenPopup("选择公司朱红印章", 720f, 960f);
        Txt(panel.transform, "随公司成就或激励视频解锁 8 枚专属印章，点选立即生效", 23, InkSoft, 34f, 616f, 892f, 38f, TextAnchor.MiddleLeft);
        for (int i = 0; i < CompanySealChars.Length; i++)
        {
            int idx = i;
            int col = i % 4;
            int row = i / 4;
            float x = 34f + col * 226f;
            float y = row == 0 ? 336f : 64f;
            bool unlocked = IsSealUnlocked(idx);
            bool selected = companySeal == idx;
            bool isVip = idx >= 6;
            Color sealColor = unlocked ? (isVip ? new Color(0.76f, 0.50f, 0.16f) : Vermilion) : new Color(0.65f, 0.63f, 0.59f);
            Color cardColor = selected ? Color.Lerp(CardBg, sealColor, 0.16f) : (unlocked ? CardBg : Color.Lerp(CardBg, Ink, 0.08f));
            var btn = Btn(panel.transform, "SealChoice_" + idx, "", 20, x, y, 212f, 248f, cardColor, () => {
                if (!unlocked)
                {
                    if (isVip)
                    {
                        PromptRewardedAd("seal_" + idx, 2, "典藏御印「" + CompanySealNames[idx] + "」", () => {
                            companySeal = idx; SaveGame(); ClosePanel(true); OpenCompanyScreen();
                            Toast("已永久解锁并佩戴「" + CompanySealNames[idx] + "」");
                        }, () => {
                            OpenCompanySealPicker();
                            Toast("「" + CompanySealNames[idx] + "」观影进度 1/2，再看 1 次即可解锁！");
                        });
                    }
                    else Toast("未解锁：" + SealUnlockHint(idx));
                    return;
                }
                companySeal = idx;
                SaveGame();
                ClosePanel(true);
                OpenCompanyScreen();
                Toast("已换用印章「" + CompanySealNames[idx] + "」");
            });
            var stamp = Rect("Stamp", btn.transform, 52f, 114f, 108f, 108f);
            Rounded(stamp, sealColor);
            Txt(stamp.transform, CompanySealChars[idx], 56, Color.white, 0f, 0f, 108f, 108f, TextAnchor.MiddleCenter).raycastTarget = false;
            Txt(btn.transform, CompanySealNames[idx] + (selected ? " ✓" : ""), 22, unlocked ? Ink : InkSoft, 8f, 60f, 196f, 38f, TextAnchor.MiddleCenter).raycastTarget = false;
            Txt(btn.transform, unlocked ? (selected ? "当前印章" : "点击选用") : SealUnlockHint(idx), 18, unlocked ? (selected ? sealColor : Matcha) : (isVip ? new Color(0.76f, 0.50f, 0.16f) : InkSoft), 8f, 14f, 196f, 40f, TextAnchor.MiddleCenter).raycastTarget = false;
        }
    }

    // ---------- 图鉴 ----------
    int sceneryCategory;
    int sceneryRegionFilter;
    static readonly string[] SceneryCategories = { "全部名胜", "自然景观", "人文景观" };
    static readonly string[] SceneryRegions = { "全部", "香港", "澳门", "广东" };
    void OpenSceneryScreen()
    {
        var pg = OpenPage("旅行手帐", "铁路通达风物所在站即入画；展开大图留存沿途印记。", "PageArt/album_watercolor");
        Txt(pg.transform, "沿途记忆", 36, Ink, 34, PgH-238, 440, 52, TextAnchor.MiddleLeft);
        int regionTotal = 0, regionFound = 0;
        foreach (var item in sceneries) if (sceneryRegionFilter == 0 || item.region == SceneryRegions[sceneryRegionFilter]) { regionTotal++; if (item.found) regionFound++; }
        Txt(pg.transform, "TRAVEL JOURNAL  ·  " + regionFound + " / " + regionTotal, 21, InkSoft, 36, PgH-278, 410, 32, TextAnchor.MiddleLeft);
        if (regionTotal > 0 && regionFound == regionTotal)
        {
            var clearTex = EraPosterGenerator.GetCustomPoster("scenery_clear_" + sceneryRegionFilter, "CLEAR!", sceneryRegionFilter % 4, 5, 512, 184, 0);
            var clearSeal = CreateNeonPosterFrame(pg.transform, "SceneryClearSeal", 456f, PgH - 274f, 214f, 78f, clearTex, 4f);
            clearSeal.transform.localRotation = Quaternion.Euler(0, 0, -3.5f);
        }
        Button filter = null;
        filter = Btn(pg.transform, "SceneryFilter", SceneryCategories[sceneryCategory] + "  ▾", 25, 694, PgH-264, 330, 72,
            new Color(0.91f,0.88f,0.80f,0.98f), () => ShowFleetDropdown(pg.transform, filter.transform, SceneryCategories, sceneryCategory, value => { sceneryCategory = value; OpenSceneryScreen(); }));
        TInk(filter, Ink); filter.gameObject.AddComponent<FleetPressFeedback>();
        for (int i = 0; i < SceneryRegions.Length; i++) {
            int region = i;
            var tab = Btn(pg.transform, "SceneryRegion" + i, SceneryRegions[i], 24, 28 + i * 258, PgH - 360, 246, 60,
                i == sceneryRegionFilter ? new Color(0.75f,0.82f,0.68f,0.98f) : new Color(0.96f,0.94f,0.88f,0.98f),
                () => { sceneryRegionFilter = region; OpenSceneryScreen(); });
            TInk(tab, Ink);
        }
        int count = 0;
        foreach (var item in sceneries)
            if ((sceneryRegionFilter == 0 || item.region == SceneryRegions[sceneryRegionFilter]) &&
                (sceneryCategory == 0 || (sceneryCategory == 1 && item.natural) || (sceneryCategory == 2 && !item.natural))) count++;
        var content = FleetScroll(pg.transform, 72, PgH - 390, Mathf.CeilToInt(count / 2f) * 440f + 20);
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
