
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public partial class DemoBootstrap : MonoBehaviour
{
    // ---------- design ----------
    const float W = 1080f, H = 1920f;
    const float MAP_X = 0f, MAP_Y = 173f, MAP_W = 1080f, MAP_H = 1574f;
    const float BAND_T = 0.09f, BAND_H = 0.82f;
    static readonly Color Paper = new Color(0.965f, 0.937f, 0.890f);
    static readonly Color Paper2 = new Color(0.925f, 0.886f, 0.816f);
    static readonly Color Ink = new Color(0.184f, 0.165f, 0.149f);
    static readonly Color InkSoft = new Color(0.42f, 0.39f, 0.35f);
    static readonly Color Vermilion = new Color(0.784f, 0.333f, 0.239f);
    static readonly Color Indigo = new Color(0.231f, 0.353f, 0.549f);
    static readonly Color Matcha = new Color(0.435f, 0.627f, 0.353f);
    static readonly Color Gold = new Color(0.851f, 0.663f, 0.306f);
    static readonly Color Sakura = new Color(0.910f, 0.627f, 0.706f);
    static readonly Color Sea = new Color(0.663f, 0.788f, 0.808f);
    static readonly Color[] LineColors = {
        new Color(0.78f,0.33f,0.24f), new Color(0.30f,0.62f,0.47f),
        new Color(0.24f,0.45f,0.72f), new Color(0.88f,0.52f,0.20f),
        new Color(0.52f,0.36f,0.65f), new Color(0.45f,0.60f,0.72f)
    };

    [Serializable] public class Station {
        public string name; public float x, y; public int level;
        public bool portal; public string portalTo;
        public float riderBonus; public int facLevel;
        public Station(string n,float px,float py){name=n;x=px;y=py;level=1;}
    }
    [Serializable] public class Scenery { public string name; public float x,y; public bool natural; public bool found; }
    public class Line {
        public int id; public string name; public Color color; public List<int> st = new List<int>();
        public int[] up = new int[3]; public int trains = 1;
        public List<int> recent = new List<int>(); // 最近几个结算日的实收
        public int todayRevenue;
    }

    // ---------- state ----------
    Font font;
    Sprite spriteRound, spriteCircle, spriteWhite, spriteDiamond, spriteRing;
    Canvas canvas; RectTransform root, mapRect, lineLayer, stationLayer, trainLayer, previewLayer;
    GraphicRaycaster raycaster;
    float coins = 2378f, diamonds = 20f, rp = 60f, progress = 0.08f;
    float income = 0f;
    List<Station> stations = new List<Station>();
    List<Scenery> sceneries = new List<Scenery>();
    List<Line> lines = new List<Line>();
    Dictionary<int, Image> stationImgs = new Dictionary<int, Image>();
    Dictionary<int, LineVisual> lineVisuals = new Dictionary<int, LineVisual>();
    List<string> techDone = new List<string>();
    Text coinTxt, incomeTxt, diamondTxt, progTxt, rpTxt;
    RectTransform progFill;
    GameObject activePanel, scrim, toastGo, panelShadow;
    int panelOpenedFrame = -1;
    Text toastTxt; float toastTimer;
    GameObject buildBar; Text buildInfo; bool buildMode; bool dragging;
    List<int> draft = new List<int>(); int draftCost; Image previewImg;
    int nextLineId = 2;
    List<Scenery> pendingCheckin = new List<Scenery>();
    GameObject sceneryBadge;

    class LineVisual { public List<GameObject> segs = new List<GameObject>(); public GameObject train; public float t; public int lastSt = -1; public float credit; }

    // ---------- boot ----------
    void Start()
    {
        font = Resources.Load<Font>("Fonts/LXGWWenKai-Regular");
        if (font == null) { Debug.LogError("FONT MISSING"); font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        spriteWhite = MakeWhite();
        spriteRound = MakeRounded(64, 18);
        spriteCircle = MakeCircle(64);
        spriteDiamond = MakeDiamond(64);
        spriteRing = MakeRing(96, 6f);
        BuildData();
        BuildUI();
        RecalcIncome();
        RefreshHUD();
        InitMobile();
        Debug.Log("HK DEMO READY");
    }

    void BuildData()
    {
        Add("中环", 0.575f, 0.285f); Add("金钟", 0.64f, 0.315f); Add("铜锣湾", 0.715f, 0.30f); Add("北角", 0.795f, 0.275f);
        Add("尖沙咀", 0.59f, 0.418f); Add("红磡", 0.725f, 0.455f); Add("旺角", 0.64f, 0.505f); Add("九龙塘", 0.755f, 0.552f);
        Add("沙田", 0.815f, 0.635f); Add("大埔墟", 0.825f, 0.735f); Add("荃湾", 0.46f, 0.585f); Add("屯门", 0.30f, 0.585f);
        Add("东涌", 0.24f, 0.305f); Add("机场", 0.13f, 0.34f); Add("迪士尼", 0.315f, 0.24f);
        var luo = new Station("罗湖", 0.93f, 0.87f); luo.portal = true; luo.portalTo = "深圳"; stations.Add(luo);
        var hz = new Station("港珠澳", 0.105f, 0.455f); hz.portal = true; hz.portalTo = "珠海/澳门"; stations.Add(hz);

        sceneries.Add(new Scenery{name="太平山", x=0.478f,y=0.238f,natural=true});
        sceneries.Add(new Scenery{name="维多利亚港", x=0.665f,y=0.375f,natural=true});
        sceneries.Add(new Scenery{name="天坛大佛", x=0.20f,y=0.235f,natural=false});
        sceneries.Add(new Scenery{name="庙街夜市", x=0.545f,y=0.475f,natural=false});
        sceneries.Add(new Scenery{name="浅水湾", x=0.855f,y=0.185f,natural=true});
        sceneries.Add(new Scenery{name="狮子山", x=0.76f,y=0.60f,natural=true});

        var l1 = new Line { id = 1, name = "1 号线", color = LineColors[0] };
        l1.st.Add(0); l1.st.Add(1); l1.st.Add(2);
        lines.Add(l1);
    }
    void Add(string n, float x, float y) { stations.Add(new Station(n, x, y)); }

    void BuildUI()
    {
        EnsureCamera();
        var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // 画布自适应：宽屏按高度、窄屏按宽度，保证 1080x1920 设计内容永远完整可见
        cgo.AddComponent<CanvasLetterbox>().designSize = new Vector2(W, H);
        raycaster = cgo.GetComponent<GraphicRaycaster>();
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // 铺满整屏的海面底，letterbox 区域不露相机纯色
        var backdrop = Rect("Backdrop", cgo.transform, 0, 0, 0, 0);
        var backdropRt = backdrop.GetComponent<RectTransform>();
        backdropRt.anchorMin = Vector2.zero; backdropRt.anchorMax = Vector2.one;
        backdropRt.pivot = new Vector2(0.5f, 0.5f);
        backdropRt.offsetMin = Vector2.zero; backdropRt.offsetMax = Vector2.zero;
        backdrop.AddComponent<MapBackdropGraphic>().raycastTarget = false;

        // 设计画布 1080x1920 居中；所有 UI 都挂在它下面，内部坐标不用改
        var stage = Rect("Stage", cgo.transform, 0, 0, W, H);
        var stageRt = stage.GetComponent<RectTransform>();
        stageRt.anchorMin = stageRt.anchorMax = new Vector2(0.5f, 0.5f);
        stageRt.pivot = new Vector2(0.5f, 0.5f);
        stageRt.anchoredPosition = Vector2.zero;
        stageRt.sizeDelta = new Vector2(W, H);
        root = stageRt;

        // 画布边缘淡投影，宽屏下让纸面像浮在海面上的图
        var stageShadow = Rect("StageShadow", root, -26, -26, W + 52, H + 52);
        Rounded(stageShadow, new Color(0.10f, 0.14f, 0.16f, 0.22f));
        stageShadow.GetComponent<Image>().raycastTarget = false;

        var bg = Rect("Paper", root, 0, 0, W, H); Img(bg, Paper);

        BuildMap();
        BuildStationViews();
        BuildSceneryViews();
        BuildTopBar();
        BuildLeftRail();
        BuildRightRail();
        BuildBottomNav();
        CreateLineVisuals();
        RefreshAllLines();
        InvokeRepeating(nameof(RPTick), 6f, 6f);
    }

    void EnsureCamera()
    {
        if (Camera.main != null) return;
        var camGo = new GameObject("UICamera", typeof(Camera));
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(transform, false);
        var cam = camGo.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.66f, 0.79f, 0.81f);
        cam.cullingMask = 0;
        cam.orthographic = true;
        cam.depth = -10;
        if (UnityEngine.Object.FindFirstObjectByType<AudioListener>() == null)
            camGo.AddComponent<AudioListener>();
    }

    void BuildTopBar()
    {
        var bar = Rect("Top", root, 0, 1640, W, 280); Img(bar, new Color(1,1,1,0));

        var c1 = Rect("Coin", bar.transform, 24, 46, 318, 92); Rounded(c1, new Color(1,0.99f,0.96f,0.95f)); SoftShadow(c1);
        var coinIcon = Rect("Icon", c1.transform, 12, 18, 56, 56); Img(coinIcon, Gold, spriteCircle);
        coinTxt = Txt(c1.transform, "2378", 46, Ink, 80, 38, 220, 54, TextAnchor.MiddleLeft);
        incomeTxt = Txt(c1.transform, "每日收入 240", 24, InkSoft, 82, 8, 226, 30, TextAnchor.MiddleLeft);

        var cap = Rect("Cap", bar.transform, 354, 26, 400, 162); Rounded(cap, new Color(1,0.985f,0.95f,0.62f)); SoftShadow(cap);
        var title = Txt(cap.transform, "香 港", 54, Ink, 0, 94, 400, 64, TextAnchor.MiddleCenter);
        title.gameObject.AddComponent<Shadow>().effectColor = new Color(1,0.99f,0.96f,0.8f);
        timeTxt = Txt(cap.transform, "第 1 天  08:00", 24, InkSoft, 46, 54, 250, 32, TextAnchor.MiddleCenter);
        timeTxt.gameObject.AddComponent<Shadow>().effectColor = new Color(1,0.99f,0.96f,0.85f);
        var sp = Btn(cap.transform, "Speed", "1×", 26, 300, 48, 88, 52, new Color(0.92f,0.89f,0.83f,0.9f), CycleSpeed);
        TInk(sp, Ink); speedTxt = sp.GetComponentInChildren<Text>();

        var c3 = Rect("Dia", bar.transform, 768, 46, 226, 92); Rounded(c3, new Color(1,0.99f,0.96f,0.95f)); SoftShadow(c3);
        var diaIcon = Rect("Icon", c3.transform, 12, 18, 52, 52); Img(diaIcon, Sakura, spriteDiamond);
        diamondTxt = Txt(c3.transform, "20", 42, Ink, 74, 38, 96, 54, TextAnchor.MiddleLeft);
        Btn(c3.transform, "Add", "＋", 30, 168, 23, 52, 52, Matcha, () => { diamonds += 5; Toast("钻石 +5"); RefreshHUD(); });

        var gb = Btn(bar.transform, "Gear", "设", 26, 1004, 64, 60, 60, new Color(1,0.99f,0.96f,0.94f), () => Toast("设置（demo）"));
        gb.GetComponentInChildren<Text>().color = Ink;
        var hb = Btn(bar.transform, "Help", "?", 30, 1004, 132, 60, 60, new Color(1,0.99f,0.96f,0.94f), () => HelpPopup("长按车站并拖到下一站开线，松手确认；拖回已连接的站点会撤销后段。若起点已在某条线路上，默认延长该线，也可另开新线；起点有多条线路时逐条选择。点线路可拆该段、拆整线或看运营详情。景观打卡改为图鉴红点，进入图鉴点击领取。时间 60 秒为一天，每日收入即一天进账；1× 可切换 2×/3×，只放大收入。底栏进入线路 / 车库 / 公司 / 科技。"));
        hb.GetComponentInChildren<Text>().color = InkSoft;
    }

    void BuildMap()
    {
        var map = Rect("Map", root, 0, 0, W, H);
        mapRect = map.GetComponent<RectTransform>();
        BuildRasterMap(map.transform);
        lineLayer = Rect("Lines", mapRect, 0, 0, W, H).GetComponent<RectTransform>();
        previewLayer = Rect("Preview", mapRect, 0, 0, W, H).GetComponent<RectTransform>();
        var pv = Rect("PreviewLine", previewLayer, 0, 0, 1, 12);
        pv.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
        previewImg = Img(pv, new Color(0.95f,0.58f,0.32f,0.85f), spriteRound);
        previewImg.raycastTarget = false;
        pv.SetActive(false);
        var tipGo = Rect("DragTip", previewLayer, 0, 0, 34, 34);
        tipGo.GetComponent<RectTransform>().pivot = new Vector2(0.5f,0.5f);
        dragTip = Img(tipGo, new Color(0.95f,0.45f,0.28f,0.95f), spriteCircle);
        dragTip.raycastTarget = false; tipGo.SetActive(false);
        stationLayer = Rect("Stations", mapRect, 0, 0, W, H).GetComponent<RectTransform>();
        trainLayer = Rect("Trains", mapRect, 0, 0, W, H).GetComponent<RectTransform>();

        Label(mapRect, "新 界", 34, 0.55f, 0.90f, new Color(0.40f,0.38f,0.32f,0.75f), 180);
        Label(mapRect, "香港岛", 26, 0.50f, 0.170f, new Color(0.40f,0.38f,0.32f,0.72f), 150);
        Label(mapRect, "大屿山", 24, 0.20f, 0.105f, new Color(0.40f,0.38f,0.32f,0.70f), 140);
        Label(mapRect, "维多利亚港", 18, 0.40f, 0.360f, new Color(0.33f,0.48f,0.53f,0.80f), 190);
        PortalLabel(mapRect, 0.80f, 0.925f, "→ 深圳");
        PortalLabel(mapRect, 0.25f, 0.505f, "→ 珠海 / 澳门");

        // 顶部/底部奶油渐隐，让悬浮 UI 压得住地图
        var fade = MakeFadeTexture();
        var top = Rect("TopFade", root, 0, H-320, W, 320);
        var tri = top.AddComponent<RawImage>(); tri.texture = fade; tri.color = new Color(0.985f,0.97f,0.93f,0.92f); tri.raycastTarget = false;
        var bot = Rect("BotFade", root, 0, 0, W, 240);
        var bri = bot.AddComponent<RawImage>(); bri.texture = fade; bri.color = new Color(0.985f,0.97f,0.93f,0.85f); bri.raycastTarget = false;
        bri.uvRect = new Rect(0, 1, 1, -1);
    }

    void BuildStationViews()
    {
        for (int i = 0; i < stations.Count; i++)
        {
            var s = stations[i];
            var go = Rect("S_" + s.name, stationLayer, MAP_X + s.x*MAP_W, MAP_Y + s.y*MAP_H, 56, 56);
            go.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            var hit = Img(go, new Color(1,1,1,0)); hit.raycastTarget = true;
            var ringGo = Rect("Ring", go.transform, -14, -14, 84, 84);
            var ring = Img(ringGo, new Color(1,1,1,0), spriteRing); ring.raycastTarget = false;
            stationRings.Add(ring);
            var outline = Rect("DotRing", go.transform, 12, 12, 32, 32);
            Img(outline, new Color(0.20f,0.19f,0.16f,0.85f), spriteRing).raycastTarget = false;
            var dotGo = Rect("DotFill", go.transform, 18, 18, 20, 20);
            var img = Img(dotGo, new Color(0.985f,0.965f,0.925f,0.98f), s.portal ? spriteDiamond : spriteCircle);
            img.raycastTarget = false;
            var sp = go.AddComponent<StationPress>(); sp.index = i; sp.boot = this;
            var label = Txt(go.transform, s.name, s.portal ? 20 : 24, new Color(0.17f,0.15f,0.13f,0.95f), -94, -46, 220, 34, TextAnchor.MiddleCenter);
            label.gameObject.AddComponent<Shadow>().effectColor = new Color(1,0.99f,0.96f,0.95f);
            if (s.portal)
            {
                var pl = Txt(go.transform, s.portalTo, 18, new Color(0.38f,0.35f,0.31f,0.9f), -90, -76, 200, 26, TextAnchor.MiddleCenter);
                pl.gameObject.AddComponent<Shadow>().effectColor = new Color(1,0.99f,0.96f,0.95f);
            }
            stationImgs[i] = img;
        }
    }

    void BuildSceneryViews()
    {
        foreach (var sc in sceneries)
        {
            var go = Rect("Sc_" + sc.name, stationLayer, MAP_X + sc.x*MAP_W, MAP_Y + sc.y*MAP_H, 40, 40);
            go.GetComponent<RectTransform>().pivot = new Vector2(0.5f,0.5f);
            var img = Img(go, new Color(0.72f,0.80f,0.70f,0.9f), spriteCircle);
            var label = Txt(go.transform, sc.name, 22, new Color(0.22f,0.30f,0.22f,0.95f), -60, -36, 180, 32, TextAnchor.MiddleCenter);
            label.gameObject.AddComponent<Shadow>().effectColor = new Color(1,1,1,0.85f);
            var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
            var local = sc;
            btn.onClick.AddListener(() => Toast(local.name + (local.found ? " · 已打卡" : " · 让线路经过它来打卡")));
        }
    }

    void BuildLeftRail()
    {
        string[] names = { "任务", "图鉴", "排行" };
        for (int i = 0; i < 3; i++)
        {
            int k = i;
            var b = Rect("L" + i, root, 10, 1070 - i*112, 86, 100);
            Rounded(b, new Color(1,0.99f,0.96f,0.88f)); SoftShadow(b);
            Txt(b.transform, names[i], 24, Ink, 0, 54, 86, 34, TextAnchor.MiddleCenter);
            Txt(b.transform, "◇", 24, LineColors[i], 0, 14, 86, 30, TextAnchor.MiddleCenter);
            if (i == 1)
            {
                Img(Rect("BadgeRing", b.transform, 62, 76, 24, 24), new Color(1,0.98f,0.94f,0.98f), spriteCircle).raycastTarget = false;
                var dotGo = Rect("Badge", b.transform, 65, 79, 18, 18); Img(dotGo, Vermilion, spriteCircle).raycastTarget = false;
                sceneryBadge = dotGo;
                dotGo.SetActive(false);
            }
            var btn = b.AddComponent<Button>(); btn.targetGraphic = b.GetComponent<Image>();
            btn.onClick.AddListener(() => {
                if (k == 1) OpenSceneryScreen();
                else if (k == 0) Toast("每日任务：连通 2 座城市（0/2）");
                else Toast("排行榜：本机最佳 · 香港 8%");
            });
        }
    }

    void BuildRightRail()
    {
        string[] labels = { "收入×2", "加速", "领金币" };
        for (int i = 0; i < 3; i++)
        {
            int k = i;
            var b = Rect("R" + i, root, 980, 1050 - i*118, 88, 104);
            Rounded(b, new Color(1,0.99f,0.96f,0.90f)); SoftShadow(b);
            Txt(b.transform, labels[i], 20, Ink, 0, 58, 88, 32, TextAnchor.MiddleCenter);
            Txt(b.transform, "▶", 20, Indigo, 0, 18, 88, 30, TextAnchor.MiddleCenter);
            var btn = b.AddComponent<Button>(); btn.targetGraphic = b.GetComponent<Image>();
            btn.onClick.AddListener(() => {
                if (k == 0) { coins += income; Toast("广告奖励：收入 ×2（+¥" + Mathf.RoundToInt(income) + "）"); }
                else if (k == 1) Toast("广告位：施工加速（demo 暂无施工计时）");
                else { coins += 120; Toast("广告奖励：金币 +120"); }
                RefreshHUD();
            });
        }
    }

    void BuildBottomNav()
    {
        dock = Rect("Dock", root, 22, 16, 1036, 156);
        Rounded(dock, new Color(1,0.99f,0.96f,0.96f)); SoftShadow(dock);
        string[] names = { "线路", "列车", "公司", "科技" };
        string[] ens = { "LINE", "TRAIN", "COMPANY", "TECH" };
        Color[] cols = { Vermilion, Indigo, Matcha, Gold };
        for (int i = 0; i < 4; i++)
        {
            int k = i;
            var item = Rect("Tab" + i, dock.transform, 14 + i*254, 12, 240, 132);
            Img(item, new Color(1,1,1,0));
            var dot = Rect("Dot", item.transform, 110, 104, 20, 20); Img(dot, cols[i], spriteCircle);
            Txt(item.transform, names[i], 36, Ink, 0, 44, 240, 48, TextAnchor.MiddleCenter);
            Txt(item.transform, ens[i], 15, InkSoft, 0, 14, 240, 24, TextAnchor.MiddleCenter);
            var btn = item.AddComponent<Button>(); btn.targetGraphic = item.GetComponent<Image>();
            btn.onClick.AddListener(() => OnTab(k));
        }
        Img(Rect("Sep1", dock.transform, 268, 30, 1, 96), new Color(0.45f,0.40f,0.33f,0.14f));
        Img(Rect("Sep2", dock.transform, 522, 30, 1, 96), new Color(0.45f,0.40f,0.33f,0.14f));
        Img(Rect("Sep3", dock.transform, 776, 30, 1, 96), new Color(0.45f,0.40f,0.33f,0.14f));
    }

    void BuildBuildBar()
    {
        buildBar = Rect("BuildBar", root, 26, 20, 1028, 172);
        Rounded(buildBar, new Color(0.22f,0.20f,0.17f,0.94f));
        buildInfo = Txt(buildBar.transform, "建线模式：按住车站拖到下一站", 27, new Color(1,0.97f,0.90f), 24, 104, 980, 54, TextAnchor.MiddleLeft);
        Btn(buildBar.transform, "Undo", "撤销", 28, 24, 20, 132, 76, new Color(0.42f,0.39f,0.35f), UndoDraft);
        Btn(buildBar.transform, "Done", "完成", 32, 172, 20, 190, 76, Matcha, FinishDraft);
        Btn(buildBar.transform, "Cancel", "取消", 28, 378, 20, 156, 76, Vermilion, CancelBuild);
        Btn(buildBar.transform, "Exit", "×", 34, 550, 20, 76, 76, new Color(0.42f,0.39f,0.35f), CancelBuild);
        Txt(buildBar.transform, "拖过车站会自动连接", 21, new Color(1,0.95f,0.86f,0.75f), 660, 30, 350, 56, TextAnchor.MiddleLeft);
        buildBar.SetActive(false);
    }

    // ---------- tabs ----------
    void OnTab(int k)
    {
        if (k == 0) OpenLinesScreen();
        else if (k == 1) OpenGarageScreen();
        else if (k == 2) OpenCompanyScreen();
        else OpenTechScreen(0);
    }

    // ---------- long press draw ----------
    public void OnStationDown(int idx) { BeginPress(idx); }
    public void OnStationUp(int idx) { }
    bool TryConnect(int a, int b)
    {
        if (a == b) return false;
        if (draft.Contains(b)) { Toast("同一条线不能重复经过同一站"); return false; }
        float d = Vector2.Distance(Pos(a), Pos(b));
        int cost = Mathf.RoundToInt(30 + d * 0.75f);
        draftCost += cost; draft.Add(b);
        Haptic.Vibrate(12); PlayTone(true);
        return true;
    }
    void UndoDraft()
    {
        if (draft.Count == 0) return;
        if (draft.Count >= 2)
        {
            int a = draft[draft.Count-2], b = draft[draft.Count-1];
            float d = Vector2.Distance(Pos(a), Pos(b));
            draftCost -= Mathf.RoundToInt(30 + d*0.75f);
        }
        draft.RemoveAt(draft.Count-1);
        UpdateBuildBar();
    }
    void CancelBuild() { CancelDraft(); }
    void FinishDraft()
    {
        if (draft.Count < 2) { CancelDraft(); return; }
        if (draftCost > coins) { Toast("金币不足：还差 ¥" + Mathf.RoundToInt(draftCost - coins)); CancelDraft(); return; }
        int cost = draftCost;
        var snapshot = new List<int>(draft);
        int from = snapshot[0];
        var candidates = ValidExtendCandidates(from, snapshot);
        if (candidates.Count == 0 && lines.Count >= 6) { Toast("线路已满 6 条"); CancelDraft(); return; }
        if (candidates.Count == 0)
        {
            int inc = IncomeOfStations(snapshot.Count);
            Confirm("开通 " + nextLineId + " 号线", "共 " + snapshot.Count + " 站 · 造价 ¥" + cost + " · +" + inc + "/天", () => CommitNewLine(snapshot, cost), CancelDraft);
            return;
        }
        ShowConnectPopup(from, snapshot, cost, 0);
    }

    void ClearDraftState()
    {
        draft.Clear(); draftCost = 0;
        ClearDraftSegs(); ResetBuildVisuals();
    }

    void CommitNewLine(List<int> snapshot, int cost)
    {
        if (lines.Count >= 6) { Toast("线路已满 6 条"); return; }
        ClosePanel(true);
        var line = new Line { id = nextLineId, name = nextLineId + " 号线", color = LineColors[(nextLineId-1) % LineColors.Length] };
        line.st = new List<int>(snapshot);
        lines.Add(line); coins -= cost;
        ClearDraftState();
        CreateLineVisuals(); RefreshAllLines(); RefreshStationDots(); RecalcIncome(); RefreshHUD();
        nextLineId++;
        CheckSceneryOnLine(snapshot);
    }

    void CommitExtend(Line line, List<int> newPath, List<int> draftStations, int cost)
    {
        ClosePanel(true);
        line.st = new List<int>(newPath);
        coins -= cost;
        ClearDraftState();
        CreateLineVisuals(); RefreshAllLines(); RefreshStationDots(); RecalcIncome(); RefreshHUD();
        CheckSceneryOnLine(draftStations);
    }

    List<int> BuildExtendPath(Line line, int from, List<int> draftStations)
    {
        var tail = draftStations.GetRange(1, draftStations.Count - 1);
        if (tail.Count == 0) return null;
        int i = line.st.IndexOf(from);
        if (i < 0) return null;
        var res = new List<int>();
        if (i == 0)
        {
            for (int k = tail.Count - 1; k >= 0; k--) res.Add(tail[k]);
            res.AddRange(line.st);
        }
        else if (i == line.st.Count - 1)
        {
            res.AddRange(line.st);
            res.AddRange(tail);
        }
        else
        {
            for (int k = 0; k <= i; k++) res.Add(line.st[k]);
            res.AddRange(tail);
            for (int k = tail.Count - 1; k >= 0; k--) res.Add(tail[k]);
            res.Add(from);
            for (int k = i + 1; k < line.st.Count; k++) res.Add(line.st[k]);
        }
        return res;
    }

    int UniqueCount(List<int> st)
    {
        var set = new HashSet<int>();
        foreach (var i in st) set.Add(i);
        return set.Count;
    }

    float DailyOf(Line line, List<int> st)
    {
        int n = Mathf.Max(1, UniqueCount(st) - 1);
        float upMult = 1f + 0.05f*line.up[0] + 0.08f*line.up[1] + 0.12f*line.up[2];
        return 120f * n * upMult;
    }

    string UniquePathText(List<int> st)
    {
        var seen = new HashSet<int>(); string s = "";
        foreach (var i in st)
        {
            if (!seen.Add(i)) continue;
            s += (s.Length == 0 ? "" : " · ") + stations[i].name;
        }
        return s;
    }

    List<Line> ValidExtendCandidates(int from, List<int> snapshot)
    {
        var res = new List<Line>();
        foreach (var l in lines)
        {
            if (!l.st.Contains(from)) continue;
            bool overlap = false;
            for (int k = 1; k < snapshot.Count; k++) if (l.st.Contains(snapshot[k])) { overlap = true; break; }
            if (!overlap) res.Add(l);
        }
        return res;
    }

    class ConnectOpt { public bool isNew; public Line line; public List<int> path; }

    void ShowConnectPopup(int from, List<int> snapshot, int cost, int selected)
    {
        try { ShowConnectPopupInternal(from, snapshot, cost, selected); }
        catch (Exception e) { Debug.LogError("EXT FAIL " + e); Toast("延长弹窗出错"); CancelDraft(); }
    }

    void ShowConnectPopupInternal(int from, List<int> snapshot, int cost, int selected)
    {
        var opts = new List<ConnectOpt>();
        foreach (var l in ValidExtendCandidates(from, snapshot))
        {
            var path = BuildExtendPath(l, from, snapshot);
            if (path != null) opts.Add(new ConnectOpt { isNew = false, line = l, path = path });
        }
        if (lines.Count < 6) opts.Add(new ConnectOpt { isNew = true });
        if (opts.Count == 0) { Toast("线路已满 6 条"); CancelDraft(); return; }
        selected = Mathf.Clamp(selected, 0, opts.Count - 1);
        var sel = opts[selected];
        string title = sel.isNew ? ("开通 " + nextLineId + " 号线") : ("延长 " + sel.line.name);
        float h = 360f + opts.Count * 112f;
        var p = OpenPopup(title, h, 920);
        Txt(p.transform, "从 " + stations[from].name + " 起 · 新增 " + (snapshot.Count - 1) + " 站 · 造价 ¥" + cost, 22, InkSoft, 30, h - 118, 820, 32, TextAnchor.MiddleLeft);
        for (int i = 0; i < opts.Count; i++)
        {
            var opt = opts[i]; int k = i;
            var row = Rect("Opt"+i, p.transform, 24, h - 212 - i*112, 872, 100);
            Rounded(row, i == selected ? new Color(0.985f,0.948f,0.862f,1f) : new Color(1,1,1,0.94f));
            SoftShadow(row);
            Color accent = opt.isNew ? Vermilion : opt.line.color;
            Img(Rect("Bar", row.transform, 0, 22, 7, 56), accent, spriteRound);
            string t1 = opt.isNew ? "开通新线路" : ("延长 " + opt.line.name);
            int gain = Mathf.RoundToInt(opt.isNew ? IncomeOfStations(snapshot.Count) : (DailyOf(opt.line, opt.path) - DailyOf(opt.line, opt.line.st)));
            string t2 = opt.isNew
                ? (nextLineId + " 号线 · " + snapshot.Count + " 站 · +" + gain + "/天")
                : (UniquePathText(opt.line.st) + "  +" + (snapshot.Count - 1) + " 站 · +" + gain + "/天");
            Txt(row.transform, t1, 30, Ink, 28, 56, 520, 40, TextAnchor.MiddleLeft);
            Txt(row.transform, t2, 20, InkSoft, 28, 20, 740, 30, TextAnchor.MiddleLeft);
            if (i == selected) Txt(row.transform, "✓", 34, Gold, 800, 30, 48, 44, TextAnchor.MiddleCenter);
            var btn = row.AddComponent<Button>(); btn.targetGraphic = row.GetComponent<Image>();
            btn.onClick.AddListener(() => ShowConnectPopup(from, snapshot, cost, k));
        }
        var ok = Btn(p.transform, "Ok", "确 认", 32, 24, 28, 424, 92, Vermilion, () => {
            if (sel.isNew) CommitNewLine(snapshot, cost);
            else CommitExtend(sel.line, sel.path, snapshot, cost);
        });
        TInk(ok, Color.white);
        var no = Btn(p.transform, "No", "取 消", 32, 472, 28, 424, 92, new Color(0.92f,0.89f,0.83f), CancelDraft);
        TInk(no, Ink);
    }
    void ShowPreview(int fromIdx)
    {
        previewImg.gameObject.SetActive(true);
        UpdatePreview(fromIdx);
    }
    void UpdatePreview(int fromIdx) { UpdatePreviewInternal(fromIdx); }

    // ---------- draft drag visuals ----------
    List<GameObject> draftSegs = new List<GameObject>();
    void ClearDraftSegs()
    {
        foreach (var g in draftSegs) if (g) Destroy(g);
        draftSegs.Clear();
    }
    void AddDraftSeg(int a, int b)
    {
        var p1 = Pos(a); var p2 = Pos(b); var d = p2 - p1;
        var go = Rect("DSeg", lineLayer, p1.x, p1.y, d.magnitude, 16);
        go.GetComponent<RectTransform>().pivot = new Vector2(0, 0.5f);
        Img(go, new Color(0.95f,0.52f,0.30f,0.90f), spriteRound);
        go.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0,0, Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
        draftSegs.Add(go);
    }
    void RedrawDraft()
    {
        ClearDraftSegs();
        for (int i = 0; i < draft.Count-1; i++) AddDraftSeg(draft[i], draft[i+1]);
    }

    // ---------- line visuals ----------
    void CreateLineVisuals()
    {
        foreach (var v in lineVisuals.Values) { foreach (var g in v.segs) if (g) Destroy(g); if (v.train) Destroy(v.train); }
        lineVisuals.Clear();
        foreach (var line in lines)
        {
            var lv = new LineVisual();
            for (int i = 0; i < line.st.Count - 1; i++)
            {
                var casing = AddSeg(line.st[i], line.st[i+1], new Color(0.19f,0.18f,0.14f,0.82f), 10, lineLayer);
                var segBody = AddSeg(line.st[i], line.st[i+1], line.color, 6, lineLayer);
                var hit = AddSeg(line.st[i], line.st[i+1], new Color(1,1,1,0f), 34, lineLayer);
                var lt = hit.AddComponent<LineTapPress>(); lt.boot = this;
                lv.segs.Add(casing); lv.segs.Add(segBody); lv.segs.Add(hit);
            }
            var t = Rect("Train" + line.id, trainLayer, 0, 0, 34, 34);
            t.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            Img(t, new Color(0.20f,0.18f,0.14f,0.85f), spriteCircle).raycastTarget = false;
            var body = Rect("Body", t.transform, 3, 3, 28, 28); Img(body, Gold, spriteCircle).raycastTarget = false;
            var nose = Rect("Nose", t.transform, 23, 14, 7, 7); Img(nose, new Color(0.99f,0.95f,0.85f), spriteCircle).raycastTarget = false;
            lv.train = t;
            lineVisuals[line.id] = lv;
        }
    }
    GameObject AddSeg(int a, int b, Color c, float width, RectTransform layer)
    {
        var p1 = Pos(a); var p2 = Pos(b);
        var d = p2 - p1;
        var go = Rect("Seg", layer, p1.x, p1.y, d.magnitude, width);
        var rt = go.GetComponent<RectTransform>(); rt.pivot = new Vector2(0, 0.5f);
        Img(go, c, spriteWhite);
        rt.localRotation = Quaternion.Euler(0,0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        return go;
    }
    Vector2 Pos(int i) { return new Vector2(MAP_X + stations[i].x*MAP_W, MAP_Y + stations[i].y*MAP_H); }

    void RefreshAllLines()
    {
        CreateLineVisuals();
        RefreshStationDots();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        TickPress(dt);
        TickClock(dt);
        TickFloaters(dt);
        UpdateTrains(dt);
        RefreshHUD();
        if (pulseTimer > 0)
        {
            pulseTimer -= dt;
            if (pulseIdx >= 0 && stationImgs.ContainsKey(pulseIdx))
            {
                float k = 1f - Mathf.Clamp01(pulseTimer / 0.24f);
                float sc = 1f + 0.35f * Mathf.Sin(k * Mathf.PI);
                stationImgs[pulseIdx].rectTransform.localScale = new Vector3(sc, sc, 1);
            }
            if (pulseTimer <= 0)
            {
                if (pulseIdx >= 0 && stationImgs.ContainsKey(pulseIdx)) stationImgs[pulseIdx].rectTransform.localScale = Vector3.one;
                pulseIdx = -1;
            }
        }
        if (dragging && draft.Count > 0 && previewImg)
        {
            if (dragFrom < 0) dragFrom = draft[draft.Count-1];
            Vector2 local; RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, Input.mousePosition, null, out local);
            const float snapR = 44f;
            int near = NearestStationAny(local, snapR);
            bool backtracked = false;
            if (near >= 0 && draft.Contains(near) && near != dragFrom)
                backtracked = BacktrackDraft(near);
            int hov = near >= 0 && !draft.Contains(near) ? near : -1;
            Vector2 end = backtracked ? Pos(dragFrom) : (hov >= 0 ? Pos(hov) : local);
            var p1 = Pos(dragFrom); var d = end - p1;
            previewImg.rectTransform.anchoredPosition = p1;
            previewImg.rectTransform.sizeDelta = new Vector2(Mathf.Max(1, d.magnitude), 16);
            previewImg.rectTransform.localRotation = Quaternion.Euler(0,0, Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
            if (dragTip) dragTip.rectTransform.anchoredPosition = end;
            hoverStation = hov;
            if (!backtracked && hov >= 0 && hov != dragFrom && TryConnect(dragFrom, hov))
            {
                dragFrom = hov; hoverStation = -1;
                RedrawDraft();
                PulseStation(hov);
                previewImg.rectTransform.anchoredPosition = Pos(hov);
            }
            UpdateRings();
        }
        else if (!dragging) UpdateRings();
        if (Input.GetMouseButtonUp(0) && dragging) EndDraw();
        if (toastTimer > 0) { toastTimer -= dt; if (toastTimer <= 0 && toastGo) toastGo.SetActive(false); }
    }
    void UpdatePreviewInternal(int fromIdx) { }

    int NearestStation(Vector2 p, float radius)
    {
        int best = -1; float bd = radius;
        for (int i = 0; i < stations.Count; i++)
        {
            if (draft.Contains(i)) continue;
            float d = Vector2.Distance(p, Pos(i));
            if (d < bd) { bd = d; best = i; }
        }
        return best;
    }
    int NearestStationAny(Vector2 p, float radius)
    {
        int best = -1; float bd = radius;
        for (int i = 0; i < stations.Count; i++)
        {
            float d = Vector2.Distance(p, Pos(i));
            if (d < bd) { bd = d; best = i; }
        }
        return best;
    }
    int DraftCostOf(List<int> path)
    {
        int c = 0;
        for (int i = 0; i < path.Count-1; i++)
            c += Mathf.RoundToInt(30 + Vector2.Distance(Pos(path[i]), Pos(path[i+1])) * 0.75f);
        return c;
    }
    bool BacktrackDraft(int station)
    {
        int idx = draft.IndexOf(station);
        if (idx < 0 || idx >= draft.Count-1) return false;
        draft.RemoveRange(idx+1, draft.Count-idx-1);
        dragFrom = station; hoverStation = -1;
        draftCost = DraftCostOf(draft);
        RedrawDraft(); UpdateBuildBar(); UpdateRings();
        PulseStation(station); PlayTone(false);
        return true;
    }
    void UpdateRings()
    {
        for (int i = 0; i < stationRings.Count; i++)
        {
            bool active = dragging && (i == dragFrom || i == hoverStation);
            bool inDraft = dragging && draft.Contains(i);
            stationRings[i].color = active ? new Color(0.95f,0.42f,0.26f,0.95f)
                : inDraft ? new Color(0.91f,0.63f,0.71f,0.60f) : new Color(1,1,1,0);
        }
    }
    void PulseStation(int i) { pulseIdx = i; pulseTimer = 0.24f; }
    void ResetBuildVisuals()
    {
        dragging = false; dragFrom = -1; hoverStation = -1;
        if (previewImg) previewImg.gameObject.SetActive(false);
        if (dragTip) dragTip.gameObject.SetActive(false);
        UpdateRings();
    }

    void UpdateTrains(float dt)
    {
        foreach (var line in lines)
        {
            if (!lineVisuals.ContainsKey(line.id)) continue;
            var lv = lineVisuals[line.id];
            if (line.st.Count < 2) continue;
            float total = 0; var pts = new List<Vector2>();
            for (int i = 0; i < line.st.Count; i++) pts.Add(Pos(line.st[i]));
            for (int i = 0; i < pts.Count-1; i++) total += Vector2.Distance(pts[i], pts[i+1]);
            if (total < 1f) continue;
            float speed = 58f + 8f*line.up[0];
            lv.t += dt * speed / total;
            if (lv.t >= 2f) lv.t -= 2f;
            float phase = lv.t <= 1f ? lv.t : 2f - lv.t;
            float target = phase * total; float acc = 0; Vector2 pos = pts[0]; float ang = 0;
            for (int i = 0; i < pts.Count-1; i++)
            {
                float seg = Vector2.Distance(pts[i], pts[i+1]);
                if (target <= acc + seg) { float f = (target-acc)/Mathf.Max(0.01f,seg); pos = Vector2.Lerp(pts[i], pts[i+1], f); ang = Mathf.Atan2(pts[i+1].y-pts[i].y, pts[i+1].x-pts[i].x)*Mathf.Rad2Deg; break; }
                acc += seg; pos = pts[i+1];
            }
            if (lv.train)
            {
                var trt = lv.train.GetComponent<RectTransform>();
                trt.anchoredPosition = pos;
                float dir = lv.t <= 1f ? ang : ang + 180f;
                trt.localRotation = Quaternion.Euler(0,0,dir);
            }
            lv.credit += LineDaily(line) / 60f * dt * timeSpeed;
            int near = -1; float best = 34f;
            for (int i = 0; i < line.st.Count; i++)
            {
                float dd = Vector2.Distance(pos, Pos(line.st[i]));
                if (dd < best) { best = dd; near = i; }
            }
            if (near >= 0 && near != lv.lastSt)
            {
                lv.lastSt = near;
                int amt = Mathf.RoundToInt(lv.credit);
                if (amt > 0) { lv.credit -= amt; coins += amt; line.todayRevenue += amt; SpawnFloater(amt, Pos(line.st[near]) + new Vector2(0, 34)); }
            }
        }
    }

    int RaycastStation(Vector2 screenPos)
    {
        if (EventSystem.current == null) return -1;
        var ped = new PointerEventData(EventSystem.current); ped.position = screenPos;
        var results = new List<RaycastResult>();
        raycaster.Raycast(ped, results);
        foreach (var r in results)
        {
            var sp = r.gameObject.GetComponent<StationPress>();
            if (sp != null) return sp.index;
        }
        return -1;
    }

    // ---------- income ----------
    int IncomeOfStations(int n) { return 120 * Mathf.Max(1, n-1); }
    float TechMult() { return 1f + 0.04f * techDone.Count; }
    void RecalcIncome()
    {
        float total = 0;
        foreach (var line in lines)
        {
            float baseInc = 120f * Mathf.Max(1, UniqueCount(line.st)-1);
            float upMult = 1f + 0.05f*line.up[0] + 0.08f*line.up[1] + 0.12f*line.up[2];
            total += baseInc * upMult;
        }
        foreach (var s in stations) total += s.riderBonus;
        total *= TechMult();
        income = total;
        progress = Mathf.Clamp01(lines.Count / 6f * 0.5f + FoundCount() / 6f * 0.5f);
    }
    int FoundCount() { int n = 0; foreach (var s in sceneries) if (s.found) n++; return n; }
    void RPTick() { rp += 1f; RefreshHUD(); }

    // ---------- HUD ----------
    void RefreshHUD()
    {
        if (coinTxt) coinTxt.text = Mathf.FloorToInt(coins).ToString();
        if (incomeTxt) incomeTxt.text = "每日收入 " + Mathf.RoundToInt(income);
        if (diamondTxt) diamondTxt.text = Mathf.FloorToInt(diamonds).ToString();
        if (progTxt) progTxt.text = Mathf.RoundToInt(progress*100) + "%";
        if (progFill) progFill.sizeDelta = new Vector2(180f*progress, 8f);
    }
    void UpdateBuildBar()
    {
        if (!buildInfo) return;
        string path = "新建线路";
        if (draft.Count > 0) path = stations[draft[0]].name;
        for (int i = 1; i < draft.Count; i++) path += " → " + stations[draft[i]].name;
        if (draft.Count >= 2)
            buildInfo.text = path + "  ·  ¥" + draftCost + "  ·  预计 +" + IncomeOfStations(draft.Count) + "/分";
        else buildInfo.text = draft.Count == 0 ? "建线模式：按住起点车站，拖向下一个站" : "继续拖动连接下一站 · 松手可暂停";
    }

    // ---------- panels ----------
    void TInk(Button b, Color c) { var t = b.GetComponentInChildren<Text>(); if (t) t.color = c; }

    GameObject OpenPanel(string title, float h)
    {
        CloseLineBubble();
        ClosePanel(true);
        scrim = Rect("Scrim", root, 0, 0, W, H); Img(scrim, new Color(0.12f,0.10f,0.09f,0.26f));
        var sb = scrim.AddComponent<Button>(); sb.transition = Selectable.Transition.None; sb.onClick.AddListener(ClosePanel);
        float w = W - 80f;
        panelShadow = Rect("PShadow", root, 44, 168, w, h); Rounded(panelShadow, new Color(0.12f,0.10f,0.08f,0.14f)); panelShadow.GetComponent<Image>().raycastTarget = false;
        activePanel = Rect("Panel", root, 40, 176, w, h);
        Rounded(activePanel, new Color(0.985f,0.972f,0.945f,0.95f));
        Img(Rect("Handle", activePanel.transform, (w-72)/2, h-16, 72, 5), new Color(0.5f,0.45f,0.38f,0.30f));
        Txt(activePanel.transform, title, 28, Ink, 26, h-66, 600, 40, TextAnchor.MiddleLeft);
        var cb = Btn(activePanel.transform, "Close", "×", 24, w-76, h-66, 52, 52, new Color(0.91f,0.88f,0.82f), ClosePanel);
        TInk(cb, Ink);
        panelOpenedFrame = Time.frameCount + 1;
        return activePanel;
    }

    GameObject OpenPopup(string title, float h, float w)
    {
        CloseLineBubble();
        ClosePanel(true);
        scrim = Rect("Scrim", root, 0, 0, W, H); Img(scrim, new Color(0.12f,0.10f,0.09f,0.30f));
        var sb = scrim.AddComponent<Button>(); sb.transition = Selectable.Transition.None; sb.onClick.AddListener(ClosePanel);
        float x = (W - w) / 2f, y = (H - h) / 2f;
        panelShadow = Rect("PShadow", root, x+8, y-10, w, h); Rounded(panelShadow, new Color(0.10f,0.08f,0.06f,0.16f)); panelShadow.GetComponent<Image>().raycastTarget = false;
        activePanel = Rect("Popup", root, x, y, w, h);
        Rounded(activePanel, new Color(0.99f,0.976f,0.95f,0.95f));
        Txt(activePanel.transform, title, 34, Ink, 26, h-64, w-110, 46, TextAnchor.MiddleLeft);
        var cb = Btn(activePanel.transform, "Close", "×", 28, w-72, h-68, 52, 52, new Color(0.92f,0.89f,0.83f), ClosePanel);
        TInk(cb, Ink);
        panelOpenedFrame = Time.frameCount + 1;
        return activePanel;
    }

    void ClosePanel() { ClosePanel(false); }

    void ClosePanel(bool force)
    {
        if (!force && Time.frameCount <= panelOpenedFrame) return;
        if (activePanel) Destroy(activePanel);
        if (scrim) Destroy(scrim);
        if (panelShadow) Destroy(panelShadow);
        activePanel = null; scrim = null; panelShadow = null;
        panelOpenedFrame = -1;
        if (!dragging && draft.Count > 0) CancelDraft();
    }

    void LegacyLineList()
    {
        if (buildMode) { Toast("先完成或取消当前建线"); return; }
        int h = Mathf.Clamp(340 + lines.Count*80, 420, 620);
        var p = OpenPanel("画 线", h);
        Txt(p.transform, "已建 " + lines.Count + "/6 条 · 已连通 " + ConnectedCount() + "/" + stations.Count + " 站", 19, InkSoft, 26, h-100, 900, 28, TextAnchor.MiddleLeft);
        if (lines.Count < 3) Txt(p.transform, "点击线路查看详情 · 新建线路可继续推高收入", 16, InkSoft, 26, 110, 900, 24, TextAnchor.MiddleLeft);
        for (int i = 0; i < lines.Count && i < 5; i++)
        {
            var line = lines[i]; int id = line.id;
            var row = Rect("Row"+i, p.transform, 24, h-186 - i*76, 952, 68); Rounded(row, new Color(1,1,1,0.90f));
            var dot = Rect("Dot", row.transform, 16, 22, 22, 22); Img(dot, line.color, spriteCircle);
            Txt(row.transform, line.name, 22, Ink, 52, 32, 150, 30, TextAnchor.MiddleLeft);
            Txt(row.transform, line.st.Count + " 站 · " + Mathf.RoundToInt(120f*(line.st.Count-1) * (1f+0.05f*line.up[0]+0.08f*line.up[1]+0.12f*line.up[2])) + "/分", 18, InkSoft, 210, 32, 520, 30, TextAnchor.MiddleLeft);
            var goBtn = Btn(row.transform, "Go", "查看", 17, 826, 15, 106, 38, new Color(0.92f,0.89f,0.83f), () => { ClosePanel(); LegacyLineDetail(id); });
            TInk(goBtn, Ink);
        }
        if (lines.Count > 5) Txt(p.transform, "更多线路可在车库中查看", 16, InkSoft, 26, 24, 600, 24, TextAnchor.MiddleLeft);
        Btn(p.transform, "New", "＋ 新建线路", 24, 24, 24, 952, 64, Vermilion, () => { ClosePanel(); StartBuild(); });
    }
    int ConnectedCount() { var set = new HashSet<int>(); foreach (var l in lines) foreach (var s in l.st) set.Add(s); return set.Count; }

    void StartBuild()
    {
        buildMode = true; draft.Clear(); draftCost = 0; dragging = false;
        buildBar.SetActive(true); UpdateBuildBar();
        if (dock) dock.SetActive(false);
        ResetBuildVisuals();
        Toast("按住车站拖到下一站，拖过车站会自动连接");
    }

    void LegacyLineDetail(int id)
    {
        var line = lines.Find(l => l.id == id); if (line == null) return;
        var p = OpenPanel(line.name, 520);
        var dot = Rect("Dot", p.transform, 26, 420, 26, 26); Img(dot, line.color, spriteCircle);
        Txt(p.transform, PathName(line), 19, InkSoft, 26, 380, 940, 30, TextAnchor.MiddleLeft);
        Card(p.transform, "客流", Mathf.RoundToInt(80f*(line.st.Count-1)) + "/分", 24, 272, 300, 88, Matcha);
        Card(p.transform, "运力", (70 + 6*line.up[1]) + "%", 350, 272, 300, 88, Indigo);
        Card(p.transform, "收入", Mathf.RoundToInt(120f*(line.st.Count-1)*(1f+0.05f*line.up[0]+0.08f*line.up[1]+0.12f*line.up[2])) + "/分", 676, 272, 300, 88, Gold);
        Txt(p.transform, "本线列车 " + line.trains + " 列 · 站数越多收入越高", 18, InkSoft, 26, 220, 900, 28, TextAnchor.MiddleLeft);
        Txt(p.transform, "升级车速 / 编组 / 班次，或加车提升运力", 17, InkSoft, 26, 190, 900, 26, TextAnchor.MiddleLeft);
        Btn(p.transform, "Up", "线路升级", 19, 24, 32, 250, 72, Matcha, () => { ClosePanel(); LegacyGarage(); });
        Btn(p.transform, "Del", "拆除线路", 19, 286, 32, 200, 72, Vermilion, () => {
            Confirm("拆除 " + line.name + "？", "退回 ¥" + Mathf.RoundToInt(120f*(line.st.Count-1)*0.6f) + "，整条线连同列车一起拆掉", () => {
                coins += 120f*(line.st.Count-1)*0.6f; lines.Remove(line); CreateLineVisuals(); RecalcIncome(); RefreshHUD(); ClosePanel(); Toast("已拆除 " + line.name);
            });
        });
        Btn(p.transform, "Add", "＋ 加车  ¥" + (289 + line.trains*120), 19, 500, 32, 476, 72, Indigo, () => {
            int cost = 289 + line.trains*120;
            if (coins < cost) { Toast("金币不足（看视频立减 demo 未接）"); return; }
            coins -= cost; line.trains++; RecalcIncome(); RefreshHUD(); Toast("加车成功：运力 +20%"); ClosePanel(); LegacyLineDetail(id);
        });
    }
    string PathName(Line l) { string s = ""; foreach (var i in l.st) s += (s==""?"":" · ") + stations[i].name; return s; }
    void Card(Transform p, string label, string value, float x, float y, float w, float h, Color c)
    {
        var g = Rect("Card", p, x, y, w, h); Rounded(g, new Color(1,1,1,0.90f));
        var bar = Rect("Bar", g.transform, 0, 16, 4, h-32); Img(bar, c, spriteRound);
        Txt(g.transform, label, 17, InkSoft, 18, h-38, 120, 24, TextAnchor.MiddleLeft);
        Txt(g.transform, value, 24, Ink, 18, 14, w-36, 36, TextAnchor.MiddleLeft);
    }

    void LegacyGarage()
    {
        var p = OpenPanel("列车 · 车库", 380);
        Txt(p.transform, "按线路升级车辆 · 升级立刻影响收入", 18, InkSoft, 26, 288, 900, 28, TextAnchor.MiddleLeft);
        if (lines.Count < 2) Txt(p.transform, "新建线路后，可在这里升级第二列车", 17, InkSoft, 24, 96, 900, 24, TextAnchor.MiddleLeft);
        for (int i = 0; i < lines.Count && i < 2; i++)
        {
            var line = lines[i];
            var row = Rect("Row"+i, p.transform, 24, 176 - i*106, 952, 100); Rounded(row, new Color(1,1,1,0.90f));
            var dot = Rect("Dot", row.transform, 16, 38, 24, 24); Img(dot, line.color, spriteCircle);
            Txt(row.transform, line.name + " · " + line.st.Count + " 站 · " + line.trains + "/8 列", 19, Ink, 50, 56, 380, 28, TextAnchor.MiddleLeft);
            string[] labels = { "车速", "编组", "班次" };
            for (int k = 0; k < 3; k++)
            {
                int kk = k; int lv = line.up[k];
                int cost = Mathf.RoundToInt((136 + k*70) * Mathf.Pow(1.6f, lv));
                Btn(row.transform, "U"+k, labels[k] + " Lv." + lv + "  ¥" + cost, 15, 450 + k*160, 22, 150, 56, Matcha, () => {
                    if (coins < cost) { Toast("金币不足"); return; }
                    coins -= cost; line.up[kk]++; RecalcIncome(); RefreshHUD(); Toast(line.name + " " + labels[kk] + " 升级成功"); ClosePanel(); LegacyGarage();
                });
            }
        }
    }

    void LegacyStationList()
    {
        var p = OpenPanel("车 站", 740);
        Txt(p.transform, "点车站查看设施与周边景观", 19, InkSoft, 26, 648, 900, 28, TextAnchor.MiddleLeft);
        for (int i = 0; i < stations.Count; i++)
        {
            int id = i; var s = stations[i];
            var row = Rect("Row"+i, p.transform, 24 + (i%3)*322, 540 - (i/3)*100, 300, 86); Rounded(row, new Color(1,1,1,0.90f));
            Txt(row.transform, s.name, 21, Ink, 16, 44, 200, 30, TextAnchor.MiddleLeft);
            Txt(row.transform, s.portal ? "跨区口" : ("Lv." + s.level), 16, InkSoft, 16, 14, 200, 24, TextAnchor.MiddleLeft);
            var btn = row.AddComponent<Button>(); btn.targetGraphic = row.GetComponent<Image>();
            btn.onClick.AddListener(() => { ClosePanel(); LegacyStationDetail(id); });
        }
    }

    void LegacyStationDetail(int idx)
    {
        var s = stations[idx];
        if (s.portal) { Toast(s.name + "：跨区接口 → " + s.portalTo + "（下一区域，demo 未开放）"); return; }
        var p = OpenPanel(s.name, 680);
        Txt(p.transform, "周边景观：" + NearbyScenery(s), 19, InkSoft, 26, 584, 900, 28, TextAnchor.MiddleLeft);
        Card(p.transform, "客流", Mathf.RoundToInt((60 + s.riderBonus*4)) + "/分", 24, 472, 300, 88, Matcha);
        Card(p.transform, "等级", "Lv." + s.level, 350, 472, 300, 88, Indigo);
        Card(p.transform, "设施", s.facLevel + " 项", 676, 472, 300, 88, Gold);
        string[] fac = { "站台扩容", "商业设施", "研发中心" };
        for (int k = 0; k < 3; k++)
        {
            int kk = k; int cost = 150 + k*120 + s.facLevel*60;
            Btn(p.transform, "F"+k, fac[k] + "   ¥" + cost, 19, 24, 356 - k*98, 952, 76, k == 0 ? Matcha : (k == 1 ? Indigo : Gold), () => {
                if (coins < cost) { Toast("金币不足"); return; }
                coins -= cost; s.facLevel++;
                if (kk == 0) { s.level++; s.riderBonus += 8; }
                if (kk == 1) { s.riderBonus += 14; }
                if (kk == 2) { rp += 20; }
                RecalcIncome(); RefreshHUD(); Toast(s.name + " · " + fac[kk] + " 完成"); ClosePanel(); LegacyStationDetail(idx);
            });
        }
        var back = Btn(p.transform, "Back", "返回地图", 19, 24, 48, 952, 64, new Color(0.92f,0.89f,0.83f), ClosePanel);
        TInk(back, Ink);
    }
    string NearbyScenery(Station s)
    {
        string r = "";
        foreach (var sc in sceneries)
        {
            float d = Vector2.Distance(new Vector2(s.x,s.y), new Vector2(sc.x,sc.y));
            if (d < 0.16f) r += (r==""?"":"、") + sc.name + (sc.found?"（已打卡）":"");
        }
        return r == "" ? "暂无" : r;
    }

    void LegacyTech()
    {
        var p = OpenPanel("科 技 树", 1120);
        Txt(p.transform, "研究点 RP " + Mathf.FloorToInt(rp) + " · 每 6 秒 +1", 19, InkSoft, 26, 1008, 900, 28, TextAnchor.MiddleLeft);
        string[] branches = { "机车", "车辆", "基建", "电气化", "经营" };
        Color[] cols = { Vermilion, Sakura, Matcha, Indigo, Gold };
        for (int b = 0; b < 5; b++)
        {
            int bb = b;
            Txt(p.transform, branches[b], 20, cols[b], 24, 888 - b*158, 100, 30, TextAnchor.MiddleLeft);
            for (int n = 0; n < 3; n++)
            {
                string key = b + "-" + n; int cost = 20 + n*20;
                bool done = techDone.Contains(key);
                var node = Rect("N"+b+"_"+n, p.transform, 130 + n*242, 840 - b*158, 214, 128);
                Rounded(node, done ? new Color(0.92f,0.95f,0.88f,0.95f) : new Color(1,1,1,0.90f));
                Txt(node.transform, branches[b] + " T" + (n+1), 18, Ink, 10, 92, 194, 26, TextAnchor.MiddleCenter);
                Txt(node.transform, done ? "已研发" : ("RP " + cost + "  +4%"), 16, done ? Matcha : InkSoft, 10, 64, 194, 22, TextAnchor.MiddleCenter);
                if (!done)
                {
                    Btn(node.transform, "R", "研究", 16, 32, 14, 150, 40, Matcha, () => {
                        if (rp < cost) { Toast("研究点不足（还差 " + (cost - Mathf.FloorToInt(rp)) + "）"); return; }
                        rp -= cost; techDone.Add(key); RecalcIncome(); RefreshHUD(); Toast("研究完成：" + branches[bb] + " T" + (n+1) + "（收入 +4%）"); ClosePanel(); LegacyTech();
                    });
                }
            }
        }
    }

    void LegacyScenery()
    {
        var p = OpenPanel("图 鉴 · 景观", 690);
        Txt(p.transform, "香港 · 已打卡 " + FoundCount() + "/" + sceneries.Count, 19, InkSoft, 26, 598, 800, 28, TextAnchor.MiddleLeft);
        for (int i = 0; i < sceneries.Count; i++)
        {
            var sc = sceneries[i];
            var card = Rect("C"+i, p.transform, 24 + (i%2)*480, 407 - (i/2)*182, 460, 168);
            Rounded(card, sc.found ? new Color(0.93f,0.96f,0.87f,0.95f) : new Color(1,1,1,0.90f));
            Txt(card.transform, sc.found ? "★" : "？", 30, sc.found ? Gold : InkSoft, 16, 108, 60, 40, TextAnchor.MiddleLeft);
            Txt(card.transform, sc.name, 22, Ink, 78, 112, 350, 30, TextAnchor.MiddleLeft);
            Txt(card.transform, sc.natural ? "自然景观" : "人文景观", 16, InkSoft, 78, 84, 350, 22, TextAnchor.MiddleLeft);
            Txt(card.transform, sc.found ? "客流 +15% · 钻石 +20" : "让线路经过即可打卡", 16, sc.found?Matcha:InkSoft, 16, 34, 420, 24, TextAnchor.MiddleLeft);
        }
    }

    // ---------- scenery ----------
    void CheckSceneryOnLine(List<int> st)
    {
        for (int i = 0; i < st.Count-1; i++)
        {
            var a = new Vector2(MAP_X + stations[st[i]].x*MAP_W, MAP_Y + stations[st[i]].y*MAP_H);
            var b = new Vector2(MAP_X + stations[st[i+1]].x*MAP_W, MAP_Y + stations[st[i+1]].y*MAP_H);
            foreach (var sc in sceneries)
            {
                if (sc.found || pendingCheckin.Contains(sc)) continue;
                var p = new Vector2(MAP_X + sc.x*MAP_W, MAP_Y + sc.y*MAP_H);
                if (DistToSeg(p,a,b) < 80f) pendingCheckin.Add(sc);
            }
        }
        RefreshPendingBadge();
    }
    void RefreshPendingBadge()
    {
        if (sceneryBadge) sceneryBadge.SetActive(pendingCheckin.Count > 0);
    }
    float DistToSeg(Vector2 p, Vector2 a, Vector2 b)
    {
        var ab = b-a; float t = Mathf.Clamp01(Vector2.Dot(p-a,ab)/Mathf.Max(0.001f,ab.sqrMagnitude));
        return Vector2.Distance(p, a + ab*t);
    }
    void ShowCheckin(Scenery sc, Action after = null)
    {
        if (sc == null || sc.found) { if (after != null) after(); return; }
        sc.found = true; diamonds += 20;
        pendingCheckin.Remove(sc);
        RecalcIncome(); RefreshHUD(); RefreshPendingBadge();
        var p = OpenPopup("打卡成功", 500, 660);
        Txt(p.transform, "★", 72, Gold, 0, 350, 660, 90, TextAnchor.MiddleCenter);
        Txt(p.transform, sc.name, 40, Ink, 0, 274, 600, 56, TextAnchor.MiddleCenter);
        Txt(p.transform, (sc.natural?"自然景观":"人文景观") + " · 已收入明信片册", 22, InkSoft, 0, 236, 600, 32, TextAnchor.MiddleCenter);
        Txt(p.transform, "附近车站客流 +15%  ·  钻石 +20", 24, Ink, 0, 152, 600, 36, TextAnchor.MiddleCenter);
        Btn(p.transform, "Ok", "收 下", 32, 40, 36, 580, 92, Vermilion, () => {
            ClosePanel();
            if (after != null) after();
        });
    }

    // ---------- helpers ----------
    void Toast(string msg)
    {
        if (toastGo) Destroy(toastGo);
        toastGo = Rect("Toast", root, 230, 1572, 620, 68);
        Rounded(toastGo, new Color(0.17f,0.15f,0.13f,0.88f)); SoftShadow(toastGo);
        toastTxt = Txt(toastGo.transform, msg, 24, new Color(1,0.97f,0.9f), 16, 8, 588, 52, TextAnchor.MiddleCenter);
        toastTimer = 2.2f;
    }
    void Confirm(string title, string body, Action ok, Action cancel = null)
    {
        var p = OpenPopup(title, 420, 860);
        Txt(p.transform, body, 26, InkSoft, 34, 160, 792, 150, TextAnchor.UpperLeft);
        Btn(p.transform, "Ok", "确 认", 32, 34, 36, 380, 92, Vermilion, () => { ClosePanel(); ok(); });
        var no = Btn(p.transform, "No", "取 消", 32, 446, 36, 380, 92, new Color(0.92f,0.89f,0.83f), () => { ClosePanel(); if (cancel != null) cancel(); });
        TInk(no, Ink);
    }

    GameObject Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0,0); rt.pivot = new Vector2(0,0);
        rt.anchoredPosition = new Vector2(x,y); rt.sizeDelta = new Vector2(w,h);
        return go;
    }
    Image Img(GameObject go, Color c, Sprite sp = null)
    {
        var im = go.AddComponent<Image>(); im.color = c; im.sprite = sp;
        return im;
    }
    void Rounded(GameObject go, Color c) { var im = Img(go, c, spriteRound); im.type = Image.Type.Sliced; }
    Text Txt(Transform p, string s, int size, Color c, float x, float y, float w, float h, TextAnchor a)
    {
        var go = Rect("T", p, x, y, w, h);
        var t = go.AddComponent<Text>(); t.font = font; t.fontSize = size; t.color = c; t.text = s;
        t.alignment = a; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }
    Button Btn(Transform p, string name, string label, int size, float x, float y, float w, float h, Color c, Action onClick)
    {
        var go = Rect(name, p, x, y, w, h);
        var im = Img(go, c, spriteRound); im.type = Image.Type.Sliced;
        var t = Txt(go.transform, label, size, new Color(1,0.98f,0.94f), 0, 0, w, h, TextAnchor.MiddleCenter);
        var b = go.AddComponent<Button>(); b.targetGraphic = im; b.onClick.AddListener(() => onClick());
        return b;
    }
    void SoftShadow(GameObject go)
    {
        var sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0.16f,0.13f,0.10f,0.16f);
        sh.effectDistance = new Vector2(0, -5);
    }
    void Label(RectTransform p, string s, int size, float x, float y, Color c, float w)
    {
        var t = Txt(p, s, size, c, MAP_X + x*MAP_W - w/2, MAP_Y + y*MAP_H, w, 40, TextAnchor.MiddleCenter);
        t.gameObject.AddComponent<Shadow>().effectColor = new Color(1,1,1,0.7f);
    }
    void PortalLabel(RectTransform p, float x, float y, string s)
    {
        var t = Txt(p, s, 22, Vermilion, MAP_X + x*MAP_W - 110, MAP_Y + y*MAP_H, 220, 32, TextAnchor.MiddleCenter);
        t.gameObject.AddComponent<Shadow>().effectColor = new Color(1,1,1,0.85f);
    }

    // ---------- sprites / texture ----------
    Sprite MakeWhite()
    {
        var t = new Texture2D(4,4); var px = new Color[16]; for (int i=0;i<16;i++) px[i]=Color.white;
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0,0,4,4), new Vector2(0,0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(0,0,0,0));
    }
    Sprite MakeRounded(int s, int r)
    {
        var t = new Texture2D(s,s); var px = new Color[s*s];
        for (int y=0;y<s;y++) for (int x=0;x<s;x++)
        {
            float dx = Mathf.Max(0, Mathf.Max(r-x, x-(s-1-r)));
            float dy = Mathf.Max(0, Mathf.Max(r-y, y-(s-1-r)));
            bool inside = dx*dx+dy*dy <= r*r;
            px[y*s+x] = inside ? Color.white : new Color(1,1,1,0);
        }
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0,0,s,s), new Vector2(0.5f,0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(10,10,10,10));
    }
    Sprite MakeCircle(int s)
    {
        var t = new Texture2D(s,s); var px = new Color[s*s];
        for (int y=0;y<s;y++) for (int x=0;x<s;x++)
        {
            float d = Vector2.Distance(new Vector2(x,y), new Vector2(s/2f-0.5f,s/2f-0.5f));
            px[y*s+x] = d < s/2f-1 ? Color.white : new Color(1,1,1,0);
        }
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0,0,s,s), new Vector2(0.5f,0.5f), 100f);
    }
    Sprite MakeRing(int s, float th)
    {
        var t = new Texture2D(s,s); var px = new Color[s*s];
        float outer = s/2f - 0.5f, inner = outer - th;
        for (int y=0;y<s;y++) for (int x=0;x<s;x++)
        {
            float d = Vector2.Distance(new Vector2(x,y), new Vector2(s/2f-0.5f,s/2f-0.5f));
            float a = Mathf.Clamp01(outer - d + 0.6f) * Mathf.Clamp01(d - inner + 0.6f);
            px[y*s+x] = new Color(1,1,1,a);
        }
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0,0,s,s), new Vector2(0.5f,0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(0,0,0,0));
    }
    Sprite MakeDiamond(int s)
    {
        var t = new Texture2D(s,s); var px = new Color[s*s];
        for (int y=0;y<s;y++) for (int x=0;x<s;x++)
        {
            float dx = Mathf.Abs(x - (s-1)/2f), dy = Mathf.Abs(y - (s-1)/2f);
            px[y*s+x] = (dx+dy < s*0.45f) ? Color.white : new Color(1,1,1,0);
        }
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0,0,s,s), new Vector2(0.5f,0.5f), 100f);
    }

    Texture2D MakeFadeTexture()
    {
        int h = 64; var t = new Texture2D(1, h); var px = new Color[h];
        for (int y = 0; y < h; y++)
        {
            float a = Mathf.Pow(y / (float)(h - 1), 1.15f);
            px[y] = new Color(1f, 1f, 1f, a);
        }
        t.SetPixels(px); t.Apply(); t.wrapMode = TextureWrapMode.Clamp;
        return t;
    }

    bool PtInPoly(float x, float y, Vector2[] poly)
    {
        bool inside = false;
        for (int i=0,j=poly.Length-1;i<poly.Length;j=i++)
        {
            if (((poly[i].y>y)!=(poly[j].y>y)) && (x < (poly[j].x-poly[i].x)*(y-poly[i].y)/(poly[j].y-poly[i].y)+poly[i].x)) inside = !inside;
        }
        return inside;
    }
}

public class StationPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public int index; public DemoBootstrap boot;
    public void OnPointerDown(PointerEventData e) { boot.OnStationDown(index); }
    public void OnPointerUp(PointerEventData e) { boot.OnStationUp(index); }
}
