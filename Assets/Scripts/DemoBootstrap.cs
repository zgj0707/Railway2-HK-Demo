
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
        public string name; public float x, y; public int level; public bool built, closed; public int trafficEvent;
        public bool portal, county; public string portalTo, portalTo2, city; public int administrativeCode; public string region = "香港"; public float otherX, otherY, thirdX, thirdY;
        public float riderBonus; public int facLevel;
        public Station(string n,float px,float py){name=n;x=px;y=py;level=1;}
    }
    [Serializable] public class Scenery { public string name; public float x,y; public bool natural; public bool found, unlocked; public string triggerStation; public string region = "香港"; }
    [Serializable] public class Line {
        public int id; public string name; public Color color; public List<int> st = new List<int>();
        public int trains = 1;
        public int[] dispatch = { 1, 1, 1, 1 }; public float fare = 1.5f;
        public float todayGross, todayMaintenance, todayTrack;
        public List<int> recent = new List<int>(); // 最近几个结算日的实收
        public int todayRevenue;
    }

    // ---------- state ----------
    Font font;
    Sprite spriteRound, spriteCircle, spriteWhite, spriteDiamond, spriteRing, spriteStationRing;
    Canvas canvas; RectTransform root, mapRect, lineLayer, stationLayer, trainLayer, floaterLayer, previewLayer;
    MapWheelZoom mapWheelZoom;
    GraphicRaycaster raycaster;
    float coins = 2378f, diamonds = 20f, rp = 60f, progress = 0.08f;
    float income = 0f;
    List<Station> stations = new List<Station>();
    List<Scenery> sceneries = new List<Scenery>();
    List<Line> lines = new List<Line>();
    Dictionary<int, Image> stationImgs = new Dictionary<int, Image>();
    readonly Dictionary<int, Text> stationLabels = new Dictionary<int, Text>();
    readonly Dictionary<int, Text> stationDestinations = new Dictionary<int, Text>();
    readonly Dictionary<int, Image> stationLeaders = new Dictionary<int, Image>();
    readonly Dictionary<int, Image> pressRings = new Dictionary<int, Image>();
    Dictionary<int, LineVisual> lineVisuals = new Dictionary<int, LineVisual>();
    List<string> techDone = new List<string>();
    Text coinTxt, incomeTxt, diamondTxt, progTxt, rpTxt, dayTxt;
    string currentRegion = "香港"; Text regionTitle; GameObject hkMapArt, macauMapArt, guangdongMapArt;
    bool macauUnlocked, guangdongUnlocked;
    const float MacauIncomeRequired = 600f, GuangdongIncomeRequired = 1200f;
    RectTransform progFill;
    GameObject activePanel, scrim, toastGo, panelShadow, zoomNotice;
    int stationFilter; Text stationFilterText;
    Text zoomNoticeTxt;
    Action panelClosed;
    int panelOpenedFrame = -1;
    Text toastTxt; float toastTimer;
    bool dragging;
    List<int> draft = new List<int>(); int draftCost; Image previewImg;
    int nextLineId = 2;
    List<Scenery> pendingCheckin = new List<Scenery>();
    GameObject sceneryBadge;

    class LineSegmentVisual
    {
        public int from, to;
        public GameObject casing, body, core, hit;
        public void SetVisible(bool visible)
        {
            if (casing && casing.activeSelf != visible) casing.SetActive(visible);
            if (body && body.activeSelf != visible) body.SetActive(visible);
            if (core && core.activeSelf != visible) core.SetActive(visible);
            if (hit && hit.activeSelf != visible) hit.SetActive(visible);
        }
    }
    class LineVisual { public List<LineSegmentVisual> segments = new List<LineSegmentVisual>(); public GameObject train; public float t; public int lastSt = -1; public float credit; }
    // 地图放大时要按 1/zoom 反向补偿厚度的矩形（线路段、点击热区、草稿线）
    class ZoomRect { public RectTransform rt; public float baseWidth; public ZoomRect(RectTransform rt, float baseWidth) { this.rt = rt; this.baseWidth = baseWidth; } }
    readonly List<ZoomRect> zoomRects = new List<ZoomRect>();
    float overlayZoom = 1f;

    // ---------- boot ----------
    void OnEnable()
    {
        if (mapRect) mapWheelZoom = mapRect.GetComponent<MapWheelZoom>();
        // 关键防御：应对 Unity 热重载 (Domain Reload)
        if (stations == null || stations.Count == 0)
        {
            BuildData();
        }
        if (lines == null || lines.Count == 0)
        {
            RestoreGame();
        }
        saveBlocked = false;
        saveReady = true;
        if (canvas != null && lines != null && lines.Count > 0)
        {
            if (lineVisuals == null) lineVisuals = new Dictionary<int, LineVisual>();
            CreateLineVisuals();
            RefreshAllLines();
        }
    }
    void Start()
    {
        qualityTier = Mathf.Clamp(PlayerPrefs.GetInt("visual_quality", 1), 0, 2);
        selectedFrameRate = PlayerPrefs.GetInt("target_fps", 60);
        if (selectedFrameRate != 30 && selectedFrameRate != 60 && selectedFrameRate != 120) selectedFrameRate = 60;
        font = Resources.Load<Font>("Fonts/LXGWWenKai-Regular");
        if (font == null) { Debug.LogError("FONT MISSING"); font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        spriteWhite = MakeWhite();
        spriteRound = MakeRounded(64, 18);
        spriteCircle = MakeCircle(64);
        spriteDiamond = MakeDiamond(64);
        spriteRing = MakeRing(96, 6f);
        spriteStationRing = MakeHandDrawnRing(96, 7f);
        BuildData();
        bool restored = RestoreGame();
        if (!restored) dailyStationBaseline = DailyConnectedCount();
        BuildUI();
        ApplyVisualQuality();
        CheckSceneryOnLine(null);
        RecalcIncome();
        RefreshHUD();
        InitMobile();
        if (!string.IsNullOrEmpty(savedRegion) && savedRegion != currentRegion && RegionUnlocked(savedRegion)) SwitchRegion(savedRegion);
        saveReady = true;
        if (saveBlocked) Toast("存档读取失败，原存档已保留");
        Debug.Log("HK DEMO READY");
    }

    void BuildData()
    {
        stations.Clear();
        sceneries.Clear();
        Add("中环", 0.575f, 0.285f); Add("金钟", 0.64f, 0.315f); Add("铜锣湾", 0.715f, 0.30f); Add("北角", 0.795f, 0.275f);
        Add("尖沙咀", 0.59f, 0.418f); Add("红磡", 0.725f, 0.455f); Add("旺角", 0.64f, 0.505f); Add("九龙塘", 0.755f, 0.552f);
        Add("沙田", 0.815f, 0.635f); Add("大埔墟", 0.825f, 0.735f); Add("荃湾", 0.45f, 0.61f); Add("屯门", 0.30f, 0.61f);
        Add("东涌", 0.24f, 0.305f); Add("机场", 0.13f, 0.34f); Add("迪士尼", 0.315f, 0.24f);
        var luo = new Station("罗湖", 0.93f, 0.87f); luo.portal = true; luo.portalTo = "广东"; luo.otherX = 0.70f; luo.otherY = 0.25f; stations.Add(luo);
        var hz = new Station("港珠澳", 0.13f, 0.38f); hz.portal = true; hz.portalTo = "广东"; hz.otherX = 0.535f; hz.otherY = 0.400f; hz.portalTo2 = "澳门"; hz.thirdX = MacauX(113.576074f); hz.thirdY = MacauY(22.200356f); stations.Add(hz);

        // 追加车站，保留原有车站索引和既有线路。
        Add("元朗", 0.38f, 0.70f); Add("天水围", 0.27f, 0.73f);
        Add("锦上路", 0.48f, 0.67f); Add("上水", 0.78f, 0.86f);
        Add("粉岭", 0.70f, 0.81f); Add("太和", 0.76f, 0.76f);
        Add("大学", 0.86f, 0.70f); Add("大围", 0.70f, 0.63f);
        Add("马鞍山", 0.91f, 0.66f); Add("西贡", 0.94f, 0.60f);
        Add("葵芳", 0.51f, 0.64f); Add("青衣", 0.40f, 0.56f);
        Add("深圳", 0.69f, 0.34f, "广东"); Add("福田", 0.57f, 0.33f, "广东");
        Add("宝安", 0.47f, 0.42f, "广东"); Add("东莞", 0.58f, 0.55f, "广东");
        Add("惠州", 0.82f, 0.55f, "广东"); Add("广州", 0.40f, 0.72f, "广东");
        Add("佛山", 0.23f, 0.66f, "广东"); Add("中山", 0.30f, 0.48f, "广东");
        Add("珠海", 0.23f, 0.34f, "广东");
        Add("浅水湾", 0.83f, 0.255f);
        AddMacau("大三巴", 113.540845f, 22.197455f);
        AddMacau("外港", 113.557664f, 22.197471f); AddMacau("氹仔", 113.556615f, 22.153493f);
        AddMacau("路氹", 113.560200f, 22.142700f); AddMacau("路环", 113.552522f, 22.117962f);
        AddMacau("青洲", 113.537554f, 22.211585f); AddMacau("新口岸", 113.550720f, 22.188997f);
        AddMacau("妈阁", 113.529413f, 22.183668f); AddMacau("议事亭", 113.539900f, 22.193650f);
        AddMacau("海洋", 113.541297f, 22.157843f); AddMacau("排角", 113.554546f, 22.150870f);
        AddMacau("澳门机场", 113.574648f, 22.159403f); AddMacau("莲花", 113.559813f, 22.139520f);
        AddMacau("石排湾", 113.561542f, 22.131421f); AddMacau("黑沙", 113.569345f, 22.118802f);

        ApplyGuangdongStations();
        var zhuhai = stations.Find(s => s.region == "广东" && s.name == "珠海");
        if (zhuhai != null)
        {
            zhuhai.name = "珠海（关闸）"; zhuhai.portal = true; zhuhai.portalTo = "澳门";
            zhuhai.otherX = MacauX(113.549205f); zhuhai.otherY = MacauY(22.215939f);
        }
        AddMacauGateway("珠澳工业区", 113.534457f, 22.212157f, 0.466f, 0.416f);
        AddMacauGateway("横琴口岸", 113.544850f, 22.140669f, 0.474f, 0.402f);
        luo.otherX = 0.585f; luo.otherY = 0.466f; luo.administrativeCode = 440303; luo.city = "深圳市";
        // 深圳和西九龙是同一跨区节点，广东侧保留原深圳站坐标。
        var shenzhen = stations.Find(s => s.region == "广东" && s.name == "深圳");
        if (shenzhen != null)
        {
            shenzhen.name = "深圳（西九龙）"; shenzhen.portal = true; shenzhen.portalTo = "香港";
            shenzhen.otherX = 0.548f; shenzhen.otherY = 0.440f;
        }

        sceneries.Add(new Scenery{name="太平山", x=0.478f,y=0.238f,natural=true,triggerStation="中环"});
        sceneries.Add(new Scenery{name="维多利亚港", x=0.665f,y=0.375f,natural=true,triggerStation="尖沙咀"});
        sceneries.Add(new Scenery{name="天坛大佛", x=0.20f,y=0.235f,natural=false,triggerStation="东涌"});
        sceneries.Add(new Scenery{name="庙街夜市", x=0.545f,y=0.475f,natural=false,triggerStation="旺角"});
        sceneries.Add(new Scenery{name="浅水湾", x=0.855f,y=0.185f,natural=true,triggerStation="浅水湾"});
        sceneries.Add(new Scenery{name="狮子山", x=0.76f,y=0.60f,natural=true,triggerStation="九龙塘"});

        sceneries.Add(new Scenery{name="黄大仙祠", x=0.78f,y=0.52f,natural=false,triggerStation="九龙塘"});
        sceneries.Add(new Scenery{name="香港湿地公园", x=0.23f,y=0.78f,natural=true,triggerStation="天水围"});
        sceneries.Add(new Scenery{name="西贡海岸", x=0.98f,y=0.57f,natural=true,triggerStation="西贡"});

        sceneries.Add(new Scenery{name="大三巴牌坊", x=0.31345f,y=0.74189f,natural=false,triggerStation="大三巴",region="澳门"});
        sceneries.Add(new Scenery{name="妈阁庙", x=0.30253f,y=0.65772f,natural=false,triggerStation="妈阁",region="澳门"});
        sceneries.Add(new Scenery{name="官也街", x=0.55992f,y=0.42471f,natural=false,triggerStation="氹仔",region="澳门"});
        sceneries.Add(new Scenery{name="黑沙海滩", x=0.68345f,y=0.11927f,natural=true,triggerStation="黑沙",region="澳门"});
        sceneries.Add(new Scenery{name="路环圣方济各圣堂", x=0.42496f,y=0.12521f,natural=false,triggerStation="路环",region="澳门"});

        var l1 = new Line { id = 1, name = "1 号线", color = LineColors[0] };
        l1.st.Add(0); l1.st.Add(1); l1.st.Add(2);
        lines.Add(l1);
        foreach (int index in l1.st) stations[index].built = true;
    }
    void Add(string n, float x, float y, string region = "香港") { stations.Add(new Station(n, x, y) { region = region }); }
    // 澳门底图范围：经度 113.505–113.605，纬度 22.100–22.235。
    float MacauX(float longitude) { return (longitude - 113.505f) / 0.100f; }
    float MacauY(float latitude) { return (latitude - 22.100f) / 0.135f; }
    void AddMacau(string n, float longitude, float latitude) { Add(n, MacauX(longitude), MacauY(latitude), "澳门"); }

    [Serializable] sealed class GuangdongStationList { public GuangdongStationRecord[] stations; }
    [Serializable] sealed class GuangdongStationRecord { public string name, city; public int code; public float x, y; public bool legacy; }
    void ApplyGuangdongStations()
    {
        var file = Resources.Load<TextAsset>("MapData/guangdong_stations");
        if (!file) { Debug.LogError("广东县级站点数据缺失"); return; }
        var data = JsonUtility.FromJson<GuangdongStationList>(file.text);
        foreach (var record in data.stations)
        {
            var station = record.legacy ? stations.Find(s => s.region == "广东" && s.name == record.name) : null;
            if (station == null) { station = new Station(record.name, record.x, record.y) { region = "广东" }; stations.Add(station); }
            station.x = record.x; station.y = record.y; station.city = record.city;
            station.county = !record.legacy; station.administrativeCode = record.code;
        }
    }
    void SetMacauGateway(string name, float x, float y)
    {
        var station = stations.Find(s => s.region == "澳门" && s.name == name);
        if (station == null) return;
        station.portal = true; station.portalTo = "广东"; station.otherX = x; station.otherY = y;
    }
    void AddMacauGateway(string name, float longitude, float latitude, float x, float y)
    {
        AddMacau(name, longitude, latitude); SetMacauGateway(name, x, y);
    }
    // 玩家界面统一使用别称；内部地名仍作为既有存档和跨区域连接的稳定标识。
    static readonly string[,] PlaceAliases = {
        { "香 港", "香 江" }, { "澳 门", "濠 江" }, { "广 东", "南 粤" },
        { "香港", "香江" }, { "澳门", "濠江" }, { "广东", "南粤" },
        { "广州", "羊城" }, { "深圳", "鹏城" }, { "珠海", "百岛之市" },
        { "汕头", "鮀城" }, { "佛山", "禅城" }, { "韶关", "韶州" },
        { "河源", "槎城" }, { "梅州", "嘉应" }, { "惠州", "鹅城" },
        { "汕尾", "善美之城" }, { "东莞", "莞邑" }, { "中山", "香山" },
        { "江门", "五邑" }, { "阳江", "鼍城" }, { "湛江", "港城" },
        { "茂名", "油城" }, { "肇庆", "端州" }, { "清远", "清城" },
        { "潮州", "凤城" }, { "揭阳", "榕城" }, { "云浮", "石都" }
    };
    static string DisplayPlaceAliases(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        for (int i = 0; i < PlaceAliases.GetLength(0); i++) {
            string original = PlaceAliases[i, 0], alias = PlaceAliases[i, 1];
            text = text.Replace(original + "市", alias).Replace(original + "省", alias).Replace(original, alias);
        }
        return text;
    }
    string StationDisplayName(int index, string region)
    {
        var station = stations[index];
        return DisplayPlaceAliases(station.name == "深圳（西九龙）" && region == "香港" ? "西九龙（深圳）" : station.name);
    }
    string PortalSubtitle(Station station, string region)
    {
        if (station.name == "深圳（西九龙）") return region == "香港" ? "深圳" : "香港";
        if (station.name == "港珠澳")
            return region == "澳门" ? "内地/港" : region == "香港" ? "内地/澳" : "港/澳";
        if (station.region == "澳门") return region == "澳门" ? "内地" : "澳门";
        return region == station.region ? station.portalTo : station.region;
    }

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

        var bg = Rect("Paper", root, 0, 0, W, H); Img(bg, Sea);

        BuildMap();
        BuildStationViews();
        BuildSceneryViews();
        BuildTopBar();
        BuildZoomNotice();
        BuildLeftRail();
        BuildStationFilterButton();
        BuildRightRail();
        BuildBottomNav();
        BuildUndoButton();
        BuildDragCostBadge();
        CreateLineVisuals();
        RefreshAllLines();

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
        var bar = Rect("Top", root, 0, 1610, W, 280); Img(bar, new Color(1,1,1,0));

        var c1 = Rect("Coin", bar.transform, 24, 46, 318, 92); Rounded(c1, new Color(0.99f,0.985f,0.955f,0.96f)); SoftShadow(c1);
        var coinRing = Rect("CoinRing", c1.transform, 12, 18, 56, 56); Img(coinRing, new Color(0.25f,0.21f,0.17f,0.88f), spriteStationRing).raycastTarget = false;
        var coinIcon = Rect("Icon", coinRing.transform, 4, 4, 48, 48); Img(coinIcon, Gold, spriteCircle);
        coinTxt = Txt(c1.transform, "2378", 46, Ink, 80, 38, 220, 54, TextAnchor.MiddleLeft);
        incomeTxt = Txt(c1.transform, "每日收入 240", 24, InkSoft, 82, 8, 226, 30, TextAnchor.MiddleLeft);

        var cap = Rect("Cap", bar.transform, 354, 26, 400, 162); Rounded(cap, new Color(0.99f,0.985f,0.955f,0.96f)); SoftShadow(cap);
        var tape = Rect("CapTape", cap.transform, 130, 150, 140, 24);
        var tapeIm = Img(tape, new Color(0.88f, 0.78f, 0.62f, 0.75f), spriteRound); tapeIm.type = Image.Type.Sliced; tapeIm.raycastTarget = false;
        tape.transform.localRotation = Quaternion.Euler(0, 0, -1.5f);
        var title = Txt(cap.transform, "香 港", 48, Ink, 0, 106, 400, 52, TextAnchor.MiddleCenter);
        regionTitle = title;
        var regionEntry = cap.AddComponent<Button>(); regionEntry.targetGraphic = cap.GetComponent<Image>();
        regionEntry.onClick.AddListener(OpenRegionScreen);
        TextShadow(title, new Color(1,0.99f,0.96f,0.8f));
        timeTxt = Txt(cap.transform, "08:00", 26, InkSoft, 0, 60, 400, 36, TextAnchor.MiddleCenter);
        dayTxt = Txt(cap.transform, "第 1 日", 23, InkSoft, 0, 20, 400, 32, TextAnchor.MiddleCenter);

        var c3 = Rect("Dia", bar.transform, 768, 46, 226, 92); Rounded(c3, new Color(1,0.99f,0.96f,0.95f)); SoftShadow(c3);
        var diaIcon = Rect("Icon", c3.transform, 12, 18, 52, 52); Img(diaIcon, Sakura, spriteDiamond);
        diamondTxt = Txt(c3.transform, "20", 42, Ink, 66, 19, 100, 54, TextAnchor.MiddleCenter);
        Btn(c3.transform, "DiamondInfo", "?", 30, 168, 23, 52, 52, Matcha, () => Toast("钻石可通过名胜打卡和机车里程碑获得"));

        var gb = Btn(bar.transform, "Gear", "设", 26, 24, -24, 60, 60, new Color(1,0.99f,0.96f,0.94f), OpenAudioSettings);
        gb.GetComponentInChildren<Text>().color = Ink;
        var hb = Btn(bar.transform, "Help", "?", 30, 94, -24, 60, 60, new Color(1,0.99f,0.96f,0.94f), () => HelpPopup("长按站点拖动铺线。\n从线路端点出发可延长，从中间站出发可新建；拖回起点可成环。\n长按线路拖到新站，可插入途经站。\n交界站可衔接区域，点击线路查看运营。\n在图鉴打开名胜大图即可打卡。\n右侧可调倍速，1 倍速下 6 分钟为一天。"));
        hb.GetComponentInChildren<Text>().color = InkSoft;
    }

    void BuildZoomNotice()
    {
        // 顶部轻提示：不占画面中央，只做状态说明
        zoomNotice = Rect("ZoomNotice", root, 300, 1458, 480, 54);
        var nbg = Img(zoomNotice, new Color(0.99f, 0.975f, 0.94f, 0.95f), spriteRound);
        nbg.type = Image.Type.Sliced;
        nbg.raycastTarget = false;
        SoftShadow(zoomNotice);
        zoomNoticeTxt = Txt(zoomNotice.transform, "已隐藏 0 个站点 · 双指放大", 22, Ink, 8, 4, 464, 46, TextAnchor.MiddleCenter);
        zoomNoticeTxt.raycastTarget = false;
        zoomNotice.SetActive(false);
    }

    void BuildMap()
    {
        var map = Rect("Map", root, 0, 0, W, H);
        mapRect = map.GetComponent<RectTransform>();
        Img(map, new Color(1, 1, 1, 0));
        var mapZoom = map.AddComponent<MapWheelZoom>();
        mapWheelZoom = mapZoom;
        mapZoom.map = mapRect; mapZoom.viewport = root;
        mapZoom.canZoom = () => !dragging && !lineInsertActive && pageRoot == null && activePanel == null && sceneryViewer == null;
        mapZoom.maxZoom = () => currentRegion == "广东" ? 12f : 2.5f;
        mapZoom.onPanStart = () => { pressing = false; pressIdx = -1; HidePressRing(); };
        hkMapArt = Rect("HongKongArt", map.transform, 0, 0, W, H);
        BuildRasterMap(hkMapArt.transform);
        guangdongMapArt = BuildGuangdongMap(map.transform);
        macauMapArt = BuildMacauMap(map.transform);
        guangdongMapArt.SetActive(false); macauMapArt.SetActive(false);
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
        floaterLayer = Rect("IncomeFloaters", mapRect, 0, 0, W, H).GetComponent<RectTransform>();

        Label(hkMapArt.GetComponent<RectTransform>(), "新 界", 34, 0.55f, 0.90f, new Color(0.40f,0.38f,0.32f,0.75f), 180);
        Label(hkMapArt.GetComponent<RectTransform>(), "香港岛", 26, 0.50f, 0.170f, new Color(0.40f,0.38f,0.32f,0.72f), 150);
        Label(hkMapArt.GetComponent<RectTransform>(), "大屿山", 24, 0.20f, 0.105f, new Color(0.40f,0.38f,0.32f,0.70f), 140);
        Label(hkMapArt.GetComponent<RectTransform>(), "维多利亚港", 18, 0.40f, 0.360f, new Color(0.33f,0.48f,0.53f,0.80f), 190);
        PortalLabel(hkMapArt.GetComponent<RectTransform>(), 0.80f, 0.925f, "→ 深圳");
        PortalLabel(hkMapArt.GetComponent<RectTransform>(), 0.25f, 0.505f, "→ 珠海 / 澳门");

        // 顶部/底部奶油渐隐，让悬浮 UI 压得住地图
        var fade = MakeFadeTexture();
        var top = Rect("TopFade", root, 0, H-320, W, 320);
        var tri = top.AddComponent<RawImage>(); tri.texture = fade; tri.color = new Color(0.985f,0.97f,0.93f,0.92f); tri.raycastTarget = false;
        var bot = Rect("BotFade", root, 0, 0, W, 240);
        var bri = bot.AddComponent<RawImage>(); bri.texture = fade; bri.color = new Color(0.985f,0.97f,0.93f,0.85f); bri.raycastTarget = false;
        bri.uvRect = new Rect(0, 1, 1, -1);
    }

    void SwitchRegion(string destination)
    {
        if (dragging || activePanel || sceneryViewer || !RegionUnlocked(destination)) return;
        if (destination == currentRegion) { ClosePage(); return; }
        FreezeRegionTraffic();
        ClosePage(); CloseLineBubble(); CancelDraft(); ClearFloaters(); currentRegion = destination;
        hkMapArt.SetActive(currentRegion == "香港"); macauMapArt.SetActive(currentRegion == "澳门");
        guangdongMapArt.SetActive(currentRegion == "广东");
        mapRect.localScale = Vector3.one; mapRect.anchoredPosition = Vector2.zero;
        foreach (var kv in stationImgs)
        {
            int i = kv.Key; var marker = kv.Value.transform.parent;
            marker.gameObject.SetActive(StationInRegion(i, currentRegion));
            marker.localScale = Vector3.one;
            marker.GetComponent<RectTransform>().anchoredPosition = Pos(i);
            if (stationLabels.TryGetValue(i, out var nameLabel) && nameLabel) nameLabel.text = StationDisplayName(i, currentRegion);
            if (stations[i].portal)
            {
                var texts = marker.GetComponentsInChildren<Text>(true);
                if (texts.Length > 1) texts[1].text = DisplayPlaceAliases(PortalSubtitle(stations[i], currentRegion));
            }
        }
        RefreshSceneryViews();
        LayoutStationLabels();
        UpdateRegionalMarkers(true);
        regionTitle.text = DisplayPlaceAliases(currentRegion == "香港" ? "香 港" : currentRegion == "澳门" ? "澳 门" : "广 东");
        CreateLineVisuals(); RefreshStationDots();
        RecalcIncome(); RefreshHUD();
        Toast("已切换至" + currentRegion);
    }

    bool RegionUnlocked(string region)
    {
        return region == "香港" || (region == "澳门" && macauUnlocked) || (region == "广东" && guangdongUnlocked);
    }
    int ConnectedOrdinaryStations(string region)
    {
        var found = new HashSet<int>();
        foreach (var line in lines) foreach (int i in line.st)
            if (stations[i].region == region && !stations[i].portal && !stations[i].closed) found.Add(i);
        return found.Count;
    }
    void UpdateRegionUnlocks()
    {
        if (!macauUnlocked && income >= MacauIncomeRequired && ConnectedOrdinaryStations("香港") >= 4)
        { macauUnlocked = true; Toast("澳门区域已解锁"); QueueStory("macau_open", "海的另一边", "澳门发来一封简短的邀请：客流已经准备好了，缺的只是一条可靠的线路。\n下一站，去海的另一边。"); }
        if (macauUnlocked && !guangdongUnlocked && income >= GuangdongIncomeRequired && ConnectedOrdinaryStations("澳门") >= 2)
        { guangdongUnlocked = true; Toast("广东区域已解锁"); QueueStory("guangdong_open", "向北的邀请", "澳门的站台刚刚热闹起来，广东的合作邀请便送到了桌上。\n这一次，你要把一座城市的故事，接到更远的地方。"); }
    }

    void BuildStationViews()
    {
        for (int i = 0; i < stations.Count; i++)
        {
            var s = stations[i];
            Vector2 stationPosition = Pos(i);
            var go = Rect("S_" + s.name, stationLayer, stationPosition.x, stationPosition.y, 56, 56);
            go.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            var hit = Img(go, new Color(1,1,1,0)); hit.raycastTarget = true;
            var ringGo = Rect("Ring", go.transform, -14, -14, 84, 84);
            var ring = Img(ringGo, new Color(1,1,1,0), spriteRing); ring.raycastTarget = false;
            stationRings.Add(ring);
            // 长按 0.42s 的进度环：只显示正在长按的站点
            var pressGo = Rect("PressRing", go.transform, -14, -14, 84, 84);
            var press = Img(pressGo, new Color(0.95f, 0.42f, 0.26f, 0.95f), spriteRing);
            press.type = Image.Type.Filled;
            press.fillMethod = Image.FillMethod.Radial360;
            press.fillOrigin = (int)Image.Origin360.Top;
            press.fillClockwise = true;
            press.fillAmount = 0f;
            press.raycastTarget = false;
            pressGo.SetActive(false);
            pressRings[i] = press;
            // 圆点按级别分级：门户最大、县区最小，缩小时画面才有层次
            float dotSize = s.portal ? 24f : s.county ? 13f : 20f;
            float ringSize = dotSize + 8f;
            var outline = Rect("DotRing", go.transform, (56f - ringSize) * 0.5f, (56f - ringSize) * 0.5f, ringSize, ringSize);
            Color ringColor = s.portal ? new Color(0.82f,0.28f,0.22f,0.92f) : new Color(0.24f,0.21f,0.18f,0.85f);
            Img(outline, ringColor, spriteStationRing).raycastTarget = false;
            var dotGo = Rect("DotFill", go.transform, (56f - dotSize) * 0.5f, (56f - dotSize) * 0.5f, dotSize, dotSize);
            var img = Img(dotGo, s.portal ? Vermilion : new Color(0.985f,0.97f,0.94f,0.98f), spriteCircle);
            img.raycastTarget = false;
            if (s.portal)
            {
                var innerDot = Rect("PortalCore", dotGo.transform, dotSize * 0.28f, dotSize * 0.28f, dotSize * 0.44f, dotSize * 0.44f);
                Img(innerDot, Color.white, spriteCircle).raycastTarget = false;
            }
            var sp = go.AddComponent<StationPress>(); sp.index = i; sp.boot = this;
            var label = Txt(go.transform, StationDisplayName(i, currentRegion), s.portal ? 20 : 24, new Color(0.16f,0.14f,0.11f,0.96f), -94, -46, 220, 34, TextAnchor.MiddleCenter);
            TextShadow(label, new Color(0.99f,0.98f,0.94f,0.95f));
            if (s.county) { label.resizeTextForBestFit = true; label.resizeTextMinSize = 14; label.resizeTextMaxSize = 24; label.verticalOverflow = VerticalWrapMode.Truncate; }
            stationLabels[i] = label;
            var leader = Img(Rect("NameLeader", go.transform, 0, 0, 1, 2), new Color(0.15f, 0.14f, 0.13f, 0.32f), spriteWhite);
            leader.raycastTarget = false; leader.gameObject.SetActive(false); stationLeaders[i] = leader;
            if (s.portal)
            {
                var pl = Txt(go.transform, PortalSubtitle(s, currentRegion), 18, new Color(0.38f,0.35f,0.31f,0.9f), -90, -76, 200, 26, TextAnchor.MiddleCenter);
                TextShadow(pl, new Color(1,0.99f,0.96f,0.95f));
                stationDestinations[i] = pl;
            }
            stationImgs[i] = img;
            go.SetActive(StationInRegion(i, currentRegion));
        }
        LayoutStationLabels();
    }

    // 站名摆放：优先贴近圆点，挤不下就往外挪并用引导线连回去。
    // 只要圆点露出来就保留名字；拥挤时选择冲突最少的位置。
    readonly List<Rect> labelRects = new List<Rect>();

    bool PlaceStationLabel(int i, List<Rect> occupied)
    {
        if (!stationLabels.TryGetValue(i, out var label) || !label) return false;
        float scale = mapRect ? mapRect.localScale.x : 1f;
        Vector2 pan = mapRect ? mapRect.anchoredPosition : Vector2.zero;
        Vector2 center = pan + Pos(i) * scale;
        float subtitleWidth = stationDestinations.TryGetValue(i, out var subtitle) ? subtitle.preferredWidth + 12f : 0f;
        float width = Mathf.Clamp(Mathf.Max(label.preferredWidth + 12f, subtitleWidth), 52f, 180f);
        const float height = 30f;
        float destHeight = stationDestinations.ContainsKey(i) ? 21f : 0f;
        // 16 个候选位：前 8 个贴着圆点，后 8 个更远，用引导线连回
        var offsets = new List<Vector2> {
            new Vector2(-width/2, -48-destHeight), new Vector2(-width/2, 22),
            new Vector2(27, -height/2), new Vector2(-width-27, -height/2),
            new Vector2(42, 24), new Vector2(-width-42, 24),
            new Vector2(42, -52-destHeight), new Vector2(-width-42, -52-destHeight),
            new Vector2(-width/2, -84-destHeight), new Vector2(-width/2, 58),
            new Vector2(74, -height/2), new Vector2(-width-74, -height/2),
            new Vector2(96, 58), new Vector2(-width-96, 58),
            new Vector2(96, -92-destHeight), new Vector2(-width-96, -92-destHeight)
        };
        // 密集区域扩大搜索范围，优先找不重叠的位置。
        for (int radius = 130; radius <= 310; radius += 90)
            for (int direction = 0; direction < 8; direction++)
            {
                float angle = direction * Mathf.PI / 4f;
                offsets.Add(new Vector2(Mathf.Cos(angle) * radius - width / 2f,
                    Mathf.Sin(angle) * radius - (height + destHeight) / 2f));
            }
        // 屏幕边缘的圆点也尽量把名称放进画面。
        offsets.Add(new Vector2(Mathf.Clamp(center.x - width / 2f, 4f, W - width - 4f) - center.x,
            Mathf.Clamp(center.y + 22f, 4f, H - height - destHeight - 4f) - center.y));
        float best = float.PositiveInfinity; int chosen = 0;
        for (int k = 0; k < offsets.Count; k++)
        {
            var rect = new Rect(center.x + offsets[k].x, center.y + offsets[k].y, width, height + destHeight);
            float penalty = k * 2f;
            if (rect.xMin < 4 || rect.xMax > W-4 || rect.yMin < 4 || rect.yMax > H-4) penalty += 10000;
            for (int q = 0; q < occupied.Count; q++) if (rect.Overlaps(occupied[q])) penalty += 1000;
            for (int j = 0; j < stations.Count; j++)
            {
                if (i == j || !StationInRegion(j, currentRegion) || !IsStationShown(j)) continue;
                Vector2 point = pan + Pos(j) * scale;
                if (rect.Overlaps(new Rect(point.x-20, point.y-20, 40, 40))) penalty += 1000;
            }
            if (penalty < best) { best = penalty; chosen = k; }
            if (penalty == k * 2f) break; // 最靠近的无冲突位置已找到。
        }
        label.gameObject.SetActive(true);
        stationLeaders.TryGetValue(i, out var lead);
        if (lead) lead.gameObject.SetActive(false);

        Vector2 offset = offsets[chosen];
        var rt = label.rectTransform; rt.anchoredPosition = new Vector2(28 + offset.x, 28 + offset.y + destHeight);
        rt.sizeDelta = new Vector2(width, height);
        if (stationDestinations.TryGetValue(i, out var destination))
        {
            destination.rectTransform.anchoredPosition = new Vector2(28 + offset.x, 28 + offset.y);
            destination.rectTransform.sizeDelta = new Vector2(width, destHeight);
        }
        occupied.Add(new Rect(center.x + offset.x, center.y + offset.y, width, height + destHeight));

        bool displaced = chosen >= 2;
        if (lead)
        {
            lead.gameObject.SetActive(displaced);
            if (displaced)
            {
                Vector2 edge = new Vector2(Mathf.Clamp(0, offset.x, offset.x+width), Mathf.Clamp(0, offset.y, offset.y+height+destHeight));
                Vector2 start = edge.normalized * 18f, delta = edge - start;
                var lr = lead.rectTransform; lr.pivot = new Vector2(0, 0.5f); lr.anchoredPosition = new Vector2(28,28) + start;
                lr.sizeDelta = new Vector2(delta.magnitude, 2f); lr.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg);
            }
        }
        return true;
    }

    void LayoutStationLabels()
    {
        bool haveVisibility = stationVisible.Length == stations.Count;
        labelRects.Clear();
        for (int pass = 0; pass < 5; pass++)
            for (int i = 0; i < stations.Count; i++)
            {
                if (!StationInRegion(i, currentRegion)) continue;
                if (haveVisibility && !stationVisible[i]) continue;   // 圆点没露出来就不用排站名
                if (MarkerRank(i) != pass - 2) continue;              // 重要的先占近位
                PlaceStationLabel(i, labelRects);
            }
    }

    float regionalMarkerScale = -1f;
    // 地图放大时，线路 / 火车 / 草稿线按 1/zoom 反向补偿，和站点一样保持屏幕尺寸恒定。
    // 只改厚度与图标缩放，不改长度与位置——长度必须留在地图坐标系里。
    void ApplyZoomToOverlays(float scale)
    {
        float k = 1f / Mathf.Max(0.01f, scale);
        overlayZoom = k;
        int live = 0;
        for (int i = 0; i < zoomRects.Count; i++)
        {
            var zr = zoomRects[i];
            if (!zr.rt) continue;
            zr.rt.sizeDelta = new Vector2(zr.rt.sizeDelta.x, zr.baseWidth * k);
            zoomRects[live++] = zr;
        }
        if (live < zoomRects.Count) zoomRects.RemoveRange(live, zoomRects.Count - live);
        foreach (var lv in lineVisuals.Values) if (lv.train) lv.train.transform.localScale = Vector3.one * k;
        if (previewImg) previewImg.rectTransform.sizeDelta = new Vector2(previewImg.rectTransform.sizeDelta.x, 16f * k);
        if (dragTip) dragTip.rectTransform.localScale = Vector3.one * k;
    }

    bool[] stationVisible = new bool[0];
    Vector2 regionalMarkerPos = new Vector2(float.NaN, float.NaN);
    readonly List<int> markerOrder = new List<int>();
    readonly List<Vector2> takenDots = new List<Vector2>();

    // 拖线站点优先，其次是深港交界站，再是其他交界站和已开站点。
    int MarkerRank(int i)
    {
        if (dragging && (i == dragFrom || i == hoverStation)) return -2;
        if (currentRegion == "广东" && stations[i].name == "深圳（西九龙）") return -1;
        if (IsOpen(i) || stations[i].portal) return 0;
        return stations[i].county ? 2 : 1;
    }

    bool MatchesStationFilter(int i)
    {
        var station = stations[i];
        // 港澳使用站区而非县级行政区；保留所有交界站与本地站点。
        if (currentRegion != "广东" || station.portal || stationFilter == 0) return true;
        int code = station.administrativeCode;
        bool prefecture = code > 0 && code % 100 == 0;
        bool countyCity = code > 0 && code % 100 >= 81;
        return prefecture || (stationFilter == 2 && countyCity);
    }

    void CycleStationFilter()
    {
        if (dragging) { Toast("请先完成当前线路"); return; }
        stationFilter = (stationFilter + 1) % 3;
        string[] names = { "全部城市", "地级市以上", "除市辖区" };
        if (stationFilterText) stationFilterText.text = names[stationFilter];
        UpdateRegionalMarkers(true);
        Toast("站点筛选：" + names[stationFilter]);
    }

    void UpdateRegionalMarkers(bool force)
    {
        if (!mapRect) return;
        float scale = mapRect.localScale.x;
        Vector2 pan = mapRect.anchoredPosition;
        bool zoomChanged = Mathf.Abs(scale - regionalMarkerScale) >= 0.005f;
        // 缩放和解算平移都要触发重算：拖动地图后站点/线路的疏密会变
        if (!force && !zoomChanged && (pan - regionalMarkerPos).sqrMagnitude < 1f) return;
        regionalMarkerScale = scale;
        regionalMarkerPos = pan;
        ApplyZoomToOverlays(scale);
        if (stationImgs.Count == 0) { if (zoomNotice) zoomNotice.SetActive(false); return; }

        // 站点标记按 1/zoom 反向补偿，放大地图不会把图标放大到盖住屏幕
        foreach (var kv in stationImgs) kv.Value.transform.parent.localScale = Vector3.one / scale;

        // 屏幕空间碰撞过滤（1 代做法）：靠得太近的站点自动让位。
        // 拖线站点 > 深港交界站 > 其他交界站及已开站点 > 普通 > 县区
        if (stationVisible.Length != stations.Count) stationVisible = new bool[stations.Count];
        for (int i = 0; i < stationVisible.Length; i++) stationVisible[i] = false;

        markerOrder.Clear();
        for (int pass = 0; pass < 5; pass++)
            for (int i = 0; i < stations.Count; i++)
                if (StationInRegion(i, currentRegion) && MatchesStationFilter(i) && MarkerRank(i) == pass - 2) markerOrder.Add(i);

        // 已被筛选排除的站点不能留下旧图标或接受吸附。
        for (int i = 0; i < stations.Count; i++)
            if (StationInRegion(i, currentRegion) && !MatchesStationFilter(i))
                stationImgs[i].transform.parent.gameObject.SetActive(false);
        int hiddenCount = 0;
        takenDots.Clear();
        for (int k = 0; k < markerOrder.Count; k++)
        {
            int i = markerOrder[k];
            int rank = MarkerRank(i);
            Vector2 sp = pan + Pos(i) * scale;                 // 相对 Stage 左下角的屏幕坐标
            bool inView = sp.x >= -48f && sp.x <= W + 48f && sp.y >= -48f && sp.y <= H + 48f;
            // 间距与圆点尺寸匹配：主干 44、普通 34、县区 26（屏幕像素），避免两点相切成一片
            float gap = rank <= 0 ? 44f : rank == 2 ? 26f : 34f;
            bool show = inView;
            if (show)
                for (int q = 0; q < takenDots.Count; q++)
                    if (Mathf.Abs(sp.x - takenDots[q].x) < gap && Mathf.Abs(sp.y - takenDots[q].y) < gap) { show = false; break; }
            if (show) takenDots.Add(sp);
            if (!show && inView) hiddenCount++;

            var marker = stationImgs[i].transform.parent;
            if (marker.gameObject.activeSelf != show) marker.gameObject.SetActive(show);
            stationVisible[i] = show;
            if (!show) continue;

        }

        // 站名统一在这一步排：圆点露出来就给名字，挤不下就外移并用引导线连回
        LayoutStationLabels();

        // 状态驱动：只要有站点被疏密挤掉就提示，全部显示出来就自动收起
        if (zoomNotice)
        {
            if (hiddenCount > 0)
            {
                if (zoomNoticeTxt) zoomNoticeTxt.text = "已隐藏 " + hiddenCount + " 个站点 · 双指放大";
                if (!zoomNotice.activeSelf) zoomNotice.SetActive(true);
            }
            else if (zoomNotice.activeSelf) zoomNotice.SetActive(false);
        }

        // 线路段跟随端点：至少一端可见才画，避免线条连到看不见的点
        foreach (var lv in lineVisuals.Values)
        {
            foreach (var segment in lv.segments)
            {
                bool show = (segment.from >= 0 && segment.from < stationVisible.Length && stationVisible[segment.from])
                         || (segment.to >= 0 && segment.to < stationVisible.Length && stationVisible[segment.to]);
                segment.SetVisible(show);
            }
        }
    }

    void BuildSceneryViews()
    {
        foreach (var scenery in sceneries) BuildSceneryMarker(scenery);
    }

    void BuildLeftRail()
    {
        string[] names = { "任务", "图鉴", "排行" };
        string[] icons = { "手帐", "名胜", "番位" };
        Color[] accents = { new Color(0.85f,0.48f,0.28f,0.95f), new Color(0.38f,0.62f,0.45f,0.95f), new Color(0.32f,0.48f,0.68f,0.95f) };
        for (int i = 0; i < 3; i++)
        {
            int k = i;
            var b = Rect("L" + i, root, 0, 1070 - i*106, 92, 88);
            Rounded(b, new Color(0.985f, 0.975f, 0.94f, 0.95f)); SoftShadow(b);
            var ribbon = Rect("Ribbon", b.transform, 0, 0, 5, 88);
            Img(ribbon, accents[i], spriteWhite).raycastTarget = false;
            Txt(b.transform, names[i], 22, Ink, 6, 44, 82, 36, TextAnchor.MiddleCenter);
            var tag = Rect("Tag", b.transform, 18, 10, 60, 26);
            Rounded(tag, new Color(0.92f,0.90f,0.85f,0.60f));
            Txt(tag.transform, icons[i], 15, InkSoft, 0, 0, 60, 26, TextAnchor.MiddleCenter);
            if (i == 1)
            {
                Img(Rect("BadgeRing", b.transform, 68, 64, 22, 22), new Color(1,0.98f,0.94f,0.98f), spriteCircle).raycastTarget = false;
                var dotGo = Rect("Badge", b.transform, 70, 66, 18, 18); Img(dotGo, Vermilion, spriteCircle).raycastTarget = false;
                sceneryBadge = dotGo;
                dotGo.SetActive(false);
            }
            var btn = b.AddComponent<Button>(); btn.targetGraphic = b.GetComponent<Image>();
            btn.onClick.AddListener(() => {
                if (k == 1) OpenSceneryScreen();
                else if (k == 0) OpenDailyTasks();
                else OpenLeaderboard();
            });
        }
    }

    void BuildStationFilterButton()
    {
        var button = Rect("StationFilter", root, 0, 752, 92, 88);
        Rounded(button, new Color(0.985f, 0.975f, 0.94f, 0.95f)); SoftShadow(button);
        var ribbon = Rect("Ribbon", button.transform, 0, 0, 5, 88);
        Img(ribbon, new Color(0.55f,0.48f,0.38f,0.95f), spriteWhite).raycastTarget = false;
        Txt(button.transform, "筛选", 22, Ink, 6, 44, 82, 36, TextAnchor.MiddleCenter);
        var tag = Rect("FilterTag", button.transform, 8, 10, 76, 26);
        Rounded(tag, new Color(0.92f,0.90f,0.85f,0.60f));
        stationFilterText = Txt(tag.transform, "全部", 15, InkSoft, 0, 0, 76, 26, TextAnchor.MiddleCenter);
        stationFilterText.resizeTextForBestFit = true; stationFilterText.resizeTextMinSize = 13; stationFilterText.resizeTextMaxSize = 15;
        var control = button.AddComponent<Button>(); control.targetGraphic = button.GetComponent<Image>();
        control.onClick.AddListener(CycleStationFilter);
    }

    void BuildRightRail()
    {
        var go = Rect("SpeedBoard", root, 986, 1168, 86, 96);
        var shell = Img(go, new Color(0.24f, 0.20f, 0.17f, 0.88f), spriteRound); shell.type = Image.Type.Sliced;
        SoftShadow(go);
        var inner = Rect("Inner", go.transform, 3, 3, 80, 90);
        var innerIm = Img(inner, new Color(0.985f, 0.975f, 0.94f, 0.96f), spriteRound); innerIm.type = Image.Type.Sliced;
        var topBar = Rect("TopBar", inner.transform, 0, 68, 80, 22);
        Img(topBar, new Color(0.88f, 0.78f, 0.62f, 0.75f), spriteWhite).raycastTarget = false;
        Txt(inner.transform, "运行", 15, InkSoft, 0, 68, 80, 22, TextAnchor.MiddleCenter);
        speedTxt = Txt(inner.transform, "1×", 32, Ink, 0, 6, 80, 60, TextAnchor.MiddleCenter);
        var btn = go.AddComponent<Button>(); btn.targetGraphic = shell; btn.onClick.AddListener(CycleSpeed);
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

    // ---------- tabs ----------
    void OnTab(int k)
    {
        if (k == 0) OpenLinesScreen();
        else if (k == 1) OpenGarageScreen();
        else if (k == 2) Toast("尚未开放");
        else OpenTechScreen(0);
    }

    // ---------- long press draw ----------
    public void OnStationDown(int idx) { BeginPress(idx); }
    public void OnStationUp(int idx)
    {
        if (pressing && pressIdx == idx && pressTimer < 0.42f && !dragging)
        {
            pressing = false; pressIdx = -1;
            HidePressRing();
            ShowStationActions(idx);
        }
        else if (pressing)
        {
            pressing = false; pressIdx = -1;
            HidePressRing();
        }
    }
    float nextPortalEndHint;
    void ShowPortalEndHint()
    {
        if (Time.unscaledTime < nextPortalEndHint) return;
        nextPortalEndHint = Time.unscaledTime + 1.5f;
        Toast("交界站只能作为本区域线路的起点或终点");
    }
    bool TryConnect(int a, int b)
    {
        if (a == b || !StationInRegion(a, currentRegion) || !StationInRegion(b, currentRegion)) return false;
        // 到达交界站后本区域线路必须结束；从交界站开始画线仍然允许。
        if (stations[a].portal && draft.Count >= 2) { ShowPortalEndHint(); return false; }
        if (draft.Count >= 3 && b == draft[0] && stations[b].portal) { ShowPortalEndHint(); return false; }
        if (stations[a].portal && stations[b].portal) { Toast("请通过区域内普通站连接交界站"); return false; }
        bool closeLoop = draft.Count >= 3 && b == draft[0] && !IsLoop(draft);
        if (draft.Contains(b) && !closeLoop) { Toast("同一条线不能重复经过同一站"); return false; }
        draft.Add(b); draftCost = DraftCostOf(draft);
        Haptic.Vibrate(12); PlayTone(true);
        return true;
    }
    void FinishDraft()
    {
        if (draft.Count < 2) { CancelDraft(); return; }
        if (draftCost > coins) { Toast("金币不足：还差 ¥" + Mathf.RoundToInt(draftCost - coins)); CancelDraft(); return; }
        int cost = draftCost;
        var snapshot = new List<int>(draft);
        int from = snapshot[0];
        ShowConnectPopup(from, snapshot, cost, -1);
    }

    void ClearDraftState()
    {
        draft.Clear(); draftCost = 0;
        ClearDraftSegs(); ResetBuildVisuals();
    }

    int RegionStationCount(string region)
    {
        int count = 0;
        for (int i = 0; i < stations.Count; i++) if (StationInRegion(i, region)) count++;
        return count;
    }
    int RegionLineLimit(string region) { return RegionStationCount(region) / 2; }
    int LineLimit() { return RegionLineLimit(currentRegion); }
    bool PathTouchesRegion(List<int> path, string region)
    {
        for (int i = 0; i + 1 < path.Count; i++) if (SegmentRegion(path[i], path[i + 1]) == region) return true;
        return false;
    }
    int RegionLineCount(string region)
    {
        int count = 0; foreach (var line in lines) if (PathTouchesRegion(line.st, region)) count++;
        return count;
    }
    void CommitNewLine(List<int> snapshot, int cost)
    {
        if (RegionLineCount(currentRegion) >= LineLimit()) { Toast(currentRegion + "线路已达上限（" + LineLimit() + " 条）"); return; }
        BeginUndo("开通线路");
        ClosePanel(true);
        var line = new Line { id = nextLineId, name = nextLineId + " 号线", color = LineColors[(nextLineId-1) % LineColors.Length] };
        line.st = new List<int>(snapshot);
        lines.Add(line); coins -= cost;
        BuildStationsOnPath(snapshot);
        ClearDraftState();
        CreateLineVisuals(); RefreshAllLines(); RefreshStationDots(); RecalcIncome(); RefreshHUD();
        nextLineId++;
        CheckSceneryOnLine(snapshot);
        QueueStory("new_line", "不被看好的新线", "有人说这条线不会有乘客。首班车靠站时，等车的人却排到了街角。\n你把时刻表贴上墙：从今天起，这座城市多了一条路。");
        CheckLineStory(line);
        EndUndo();
        SaveGame();
    }

    void CommitExtend(Line line, List<int> newPath, List<int> draftStations, int cost)
    {
        if (!PathTouchesRegion(line.st, currentRegion) && PathTouchesRegion(newPath, currentRegion) && RegionLineCount(currentRegion) >= LineLimit()) {
            Toast(currentRegion + "线路已达上限，暂不能跨区延长"); return;
        }
        BeginUndo("延长线路");
        ClosePanel(true);
        line.st = new List<int>(newPath);
        coins -= cost; BuildStationsOnPath(draftStations);
        ClearDraftState();
        CreateLineVisuals(); RefreshAllLines(); RefreshStationDots(); RecalcIncome(); RefreshHUD();
        CheckSceneryOnLine(draftStations);
        CheckLineStory(line);
        EndUndo();
        SaveGame();
    }

    void CheckLineStory(Line line)
    {
        if (UniqueCount(line.st) >= 5) QueueStory("five_stations", "第五站的灯", "第五座站台亮灯时，最早质疑你的人也挤进了人群。\n“原来那条线，真的能通到这里。”\n你笑了笑，把下一段线路图展开。");
        if (IsLoop(line.st)) QueueStory("first_loop", "首尾相连", "最后一段轨道合拢，列车没有掉头，沿着城市的脉络继续前行。\n曾经分散的街区，终于连成了一个完整的圆。");
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
        if (ReferenceEquals(st, line.st) && traffic != null) return TrafficRevenue(line, traffic);
        return TrafficRevenue(line, CalculateTraffic(line, st));
    }
    float EstimateNetworkDelta(Line line, List<int> path, bool isNew)
    {
        var candidate = isNew ? new Line { id = nextLineId, st = path, trains = 1 } : line;
        var oldPath = candidate.st;
        float oldClock = dayClock;
        var totals = new Dictionary<Line, LineForecastData>();
        foreach (var existing in lines) totals[existing] = new LineForecastData();
        if (isNew) totals[candidate] = new LineForecastData();
        float[] hours = {8f,13f,18f,1f}, weights = {4f/24f,6f/24f,4f/24f,10f/24f};
        try
        {
            candidate.st = path;
            for (int slot=0;slot<4;slot++)
            {
                dayClock = hours[slot] * GameDaySeconds / 24f;
                var projected = CalculateTraffic(addition: isNew ? candidate : null);
                foreach (var pair in totals)
                {
                    var item=pair.Key; var f=pair.Value;
                    f.gross += TrafficRevenue(item,projected)*weights[slot];
                    f.vehicle += VehicleMaintenance(item)*weights[slot];
                    f.track += TrackMaintenance(item)*weights[slot];
                }
            }
        }
        finally { dayClock = oldClock; candidate.st = oldPath; }
        float next=0f, old=0f;
        foreach (var f in totals.Values) { float pre=f.gross-f.vehicle-f.track; next += pre - Mathf.Max(0f,pre)*0.1f; }
        foreach (var existing in lines) old += ForecastFor(existing).Profit;
        return next-old;
    }

    string UniquePathText(List<int> st)
    {
        var seen = new HashSet<int>(); string s = "";
        foreach (var i in st)
        {
            if (!seen.Add(i)) continue;
            s += (s.Length == 0 ? "" : " · ") + StationDisplayName(i, currentRegion);
        }
        return s;
    }

    List<Line> ValidExtendCandidates(int from, List<int> snapshot)
    {
        var res = new List<Line>();
        if (IsLoop(snapshot)) return res;
        foreach (var l in lines)
        {
            if (!l.st.Contains(from) || IsLoop(l.st)) continue;
            bool overlap = false;
            int opposite = from == l.st[0] ? l.st[l.st.Count - 1] : from == l.st[l.st.Count - 1] ? l.st[0] : -1;
            for (int k = 1; k < snapshot.Count; k++)
                if (l.st.Contains(snapshot[k]) && !(k == snapshot.Count - 1 && snapshot[k] == opposite)) { overlap = true; break; }
            if (!overlap)
            {
                var proposed = BuildExtendPath(l, from, snapshot);
                if (proposed != null && PortalEndsAreValid(proposed)) res.Add(l);
            }
        }
        return res;
    }

    // 同一交界站可连接两个区域的线路段，但不能连接同一区域的两段。
    bool PortalEndsAreValid(List<int> path)
    {
        if (IsLoop(path) && stations[path[0]].portal) return false;
        for (int i = 1; i < path.Count - 1; i++)
        {
            if (!stations[path[i]].portal) continue;
            string before = SegmentRegion(path[i - 1], path[i]);
            string after = SegmentRegion(path[i], path[i + 1]);
            if (before == null || after == null || before == after) return false;
        }
        return true;
    }

    bool IsLoop(List<int> path) { return path.Count >= 4 && path[0] == path[path.Count - 1]; }

    class ConnectOpt { public bool isNew; public Line line; public List<int> path; }

    int DefaultConnectOption(int from, List<ConnectOpt> opts)
    {
        // 交汇站可能同时是多条线路的端点和中间站，优先匹配端点。
        for (int i = 0; i < opts.Count; i++)
        {
            var line = opts[i].line;
            if (!opts[i].isNew && line != null && line.st.Count > 0 &&
                (line.st[0] == from || line.st[line.st.Count - 1] == from)) return i;
        }
        for (int i = 0; i < opts.Count; i++) if (opts[i].isNew) return i;
        return 0; // 已达线路上限时，只提供仍可用的延长选项。
    }

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
            if (path != null && (PathTouchesRegion(l.st, currentRegion) || RegionLineCount(currentRegion) < LineLimit()))
                opts.Add(new ConnectOpt { isNew = false, line = l, path = path });
        }
        if (RegionLineCount(currentRegion) < LineLimit()) opts.Add(new ConnectOpt { isNew = true });
        if (opts.Count == 0) { Toast(currentRegion + "线路已达上限（" + LineLimit() + " 条）"); CancelDraft(); return; }
        if (selected < 0) selected = DefaultConnectOption(from, opts);
        selected = Mathf.Clamp(selected, 0, opts.Count - 1);
        var sel = opts[selected];
        string title = sel.isNew ? ("开通 " + nextLineId + " 号线") : ("延长 " + sel.line.name);
        float h = 360f + Mathf.Min(opts.Count, 5) * 112f;
        var p = OpenBubble(title, h, 920, Pos(snapshot[snapshot.Count - 1]));
        Txt(p.transform, "从 " + StationDisplayName(from, currentRegion) + " 起 · 新增 " + (UniqueCount(snapshot) - 1) + " 站 · 造价 ¥" + cost, 22, InkSoft, 30, h - 118, 820, 32, TextAnchor.MiddleLeft);
        float viewHeight = Mathf.Min(opts.Count, 5) * 112f;
        var view = Rect("OptionsViewport", p.transform, 24, 140, 872, viewHeight);
        Img(view, new Color(1,1,1,0.01f)); var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 100f;
        scroll.viewport = view.GetComponent<RectTransform>();
        var content = Rect("Options", view.transform, 0, 0, 872, opts.Count * 112f);
        var contentRt = content.GetComponent<RectTransform>(); contentRt.anchorMin = contentRt.anchorMax = new Vector2(0,1);
        contentRt.pivot = new Vector2(0,1); contentRt.anchoredPosition = Vector2.zero; scroll.content = contentRt;
        for (int i = 0; i < opts.Count; i++)
        {
            var opt = opts[i]; int k = i;
            var row = Rect("Opt"+i, content.transform, 0, opts.Count * 112f - 106 - i*112, 872, 100);
            Rounded(row, i == selected ? new Color(0.985f,0.948f,0.862f,1f) : new Color(1,1,1,0.94f));
            SoftShadow(row);
            Color accent = opt.isNew ? Vermilion : opt.line.color;
            Img(Rect("Bar", row.transform, 0, 22, 7, 56), accent, spriteRound);
            string t1 = opt.isNew ? "开通新线路" : ("延长 " + opt.line.name);
            string route = opt.isNew ? (nextLineId + " 号线 · " + UniqueCount(snapshot) + " 站") : (UniquePathText(opt.line.st) + "  +" + (snapshot.Count - 1) + " 站");
            int gain = i == selected ? Mathf.RoundToInt(EstimateNetworkDelta(opt.line, opt.isNew ? snapshot : opt.path, opt.isNew)) : 0;
            string t2 = route + (i == selected ? " · 预计 " + (gain >= 0 ? "+" : "") + gain + "/天" : " · 选择后查看预计收益");
            Txt(row.transform, t1, 30, Ink, 28, 56, 520, 40, TextAnchor.MiddleLeft);
            Txt(row.transform, t2, 20, InkSoft, 28, 20, 740, 30, TextAnchor.MiddleLeft);
            if (i == selected) Txt(row.transform, "✓", 34, Gold, 800, 30, 48, 44, TextAnchor.MiddleCenter);
            var btn = row.AddComponent<Button>(); btn.targetGraphic = row.GetComponent<Image>();
            btn.onClick.AddListener(() => ShowConnectPopup(from, snapshot, cost, k));
        }
        if (opts.Count > 5) scroll.verticalNormalizedPosition = 1f - Mathf.Clamp01((float)selected / (opts.Count - 5));
        var ok = Btn(p.transform, "Ok", "确 认", 28, 24, 28, 422, 92, Vermilion, () => {
            if (sel.isNew) CommitNewLine(snapshot, cost);
            else CommitExtend(sel.line, sel.path, snapshot, cost);
        });
        TInk(ok, Color.white);
        var no = Btn(p.transform, "No", "取 消", 28, 474, 28, 422, 92, new Color(0.92f,0.89f,0.83f), () => { ClosePanel(); CancelDraft(); });
        TInk(no, Ink);
    }
    // ---------- draft drag visuals ----------
    List<GameObject> draftSegs = new List<GameObject>();
    void ClearDraftSegs()
    {
        foreach (var g in draftSegs) if (g) Destroy(g);
        draftSegs.Clear();
    }
    void AddDraftSeg(int a, int b, bool animate = false)
    {
        var p1 = Pos(a); var p2 = Pos(b); var d = p2 - p1;
        var go = Rect("DSeg", lineLayer, p1.x, p1.y, d.magnitude, 16f * overlayZoom);
        var drt = go.GetComponent<RectTransform>(); drt.pivot = new Vector2(0, 0.5f);
        Img(go, new Color(0.95f,0.52f,0.30f,0.90f), spriteRound);
        drt.localRotation = Quaternion.Euler(0,0, Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
        zoomRects.Add(new ZoomRect(drt, 16f));
        draftSegs.Add(go);
        if (animate) go.AddComponent<DraftSegGrow>();
    }
    void RedrawDraft()
    {
        ClearDraftSegs();
        for (int i = 0; i < draft.Count-1; i++) AddDraftSeg(draft[i], draft[i+1], i == draft.Count - 2);
    }

    // ---------- line visuals ----------
    void CreateLineVisuals()
    {
        var oldVisuals = new Dictionary<int, LineVisual>(lineVisuals);
        foreach (var v in lineVisuals.Values) if (v.train) Destroy(v.train);
        if (lineLayer) { for (int i = lineLayer.childCount - 1; i >= 0; i--) Destroy(lineLayer.GetChild(i).gameObject); }
        if (trainLayer) { for (int i = trainLayer.childCount - 1; i >= 0; i--) Destroy(trainLayer.GetChild(i).gameObject); }
        lineVisuals.Clear();
        zoomRects.Clear();
        foreach (var line in lines)
        {
            var lv = new LineVisual();
            if (oldVisuals.TryGetValue(line.id, out var old)) { lv.t = old.t; lv.credit = old.credit; lv.lastSt = old.lastSt; }
            for (int i = 0; i < line.st.Count - 1; i++)
            {
                if (SegmentRegion(line.st[i], line.st[i+1]) != currentRegion) continue;
                var casing = AddSeg(line.st[i], line.st[i+1], new Color(0.20f,0.18f,0.15f,0.92f), 12f, lineLayer);
                var segBody = AddSeg(line.st[i], line.st[i+1], line.color, 7.5f, lineLayer);
                var trackCore = AddSeg(line.st[i], line.st[i+1], new Color(1f,0.98f,0.92f,0.65f), 2.2f, lineLayer);
                var hit = AddSeg(line.st[i], line.st[i+1], new Color(1,1,1,0f), 34, lineLayer);
                var lt = hit.AddComponent<LineTapPress>(); lt.boot = this; lt.lineId = line.id; lt.segment = i;
                lv.segments.Add(new LineSegmentVisual { from = line.st[i], to = line.st[i + 1], casing = casing, body = segBody, core = trackCore, hit = hit });
            }
            // 日式微缩铁道模型（硬朗修长车厢，规避肉感胶囊）
            var t = Rect("Train" + line.id, trainLayer, 0, 0, 34, 14);
            t.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            // 墨黑机械底盘与转向架轮轴
            var chassis = Img(t, new Color(0.18f,0.16f,0.13f,0.95f), spriteWhite); chassis.raycastTarget = false;
            // 线路涂装车身主体
            var body = Rect("Body", t.transform, 1, 1, 32, 12);
            var bimg = Img(body, line.color, spriteWhite); bimg.raycastTarget = false;
            // 车顶中央浅色肋带
            var roof = Rect("Roof", body.transform, 3, 2, 22, 8);
            Img(roof, Color.Lerp(line.color, Color.white, 0.40f), spriteWhite).raycastTarget = false;
            // 车顶受电弓 / 空调机罩
            var ac = Rect("AC", roof.transform, 8, 1, 8, 6);
            Img(ac, new Color(0.22f,0.20f,0.18f,0.85f), spriteWhite).raycastTarget = false;
            // 车头宽屏驾驶舱反光风挡玻璃
            var cab = Rect("Cab", body.transform, 27, 1, 3.5f, 10);
            Img(cab, new Color(0.20f,0.40f,0.55f,0.95f), spriteWhite).raycastTarget = false;
            // 车头左右双前照灯（明亮暖黄，彻底确立列车前脸）
            var lightL = Rect("LightL", body.transform, 30.5f, 1.5f, 1.5f, 2.5f);
            Img(lightL, new Color(1f,0.95f,0.45f,1f), spriteWhite).raycastTarget = false;
            var lightR = Rect("LightR", body.transform, 30.5f, 8f, 1.5f, 2.5f);
            Img(lightR, new Color(1f,0.95f,0.45f,1f), spriteWhite).raycastTarget = false;
            lv.train = t;
            lineVisuals[line.id] = lv;
        }
        ApplyZoomToOverlays(mapRect ? mapRect.localScale.x : 1f);
        UpdateRegionalMarkers(true);
    }
    GameObject AddSeg(int a, int b, Color c, float width, RectTransform layer)
    {
        var p1 = Pos(a); var p2 = Pos(b);
        var d = p2 - p1;
        var go = Rect("Seg", layer, p1.x, p1.y, d.magnitude, width);
        var rt = go.GetComponent<RectTransform>(); rt.pivot = new Vector2(0, 0.5f);
        Img(go, c, spriteWhite);
        rt.localRotation = Quaternion.Euler(0,0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        zoomRects.Add(new ZoomRect(rt, width));
        return go;
    }
    bool StationInRegion(int i, string region) { return stations[i].region == region || (stations[i].portal && (stations[i].portalTo == region || stations[i].portalTo2 == region)); }
    string SegmentRegion(int a, int b)
    {
        if (StationInRegion(a, "香港") && StationInRegion(b, "香港")) return "香港";
        if (StationInRegion(a, "澳门") && StationInRegion(b, "澳门")) return "澳门";
        if (StationInRegion(a, "广东") && StationInRegion(b, "广东")) return "广东";
        return null;
    }
    Vector2 PosInRegion(int i, string region)
    {
        var s = stations[i]; bool other = s.portal && s.portalTo == region;
        bool third = s.portal && s.portalTo2 == region;
        return new Vector2(MAP_X + (third ? s.thirdX : other ? s.otherX : s.x)*MAP_W,
            MAP_Y + (third ? s.thirdY : other ? s.otherY : s.y)*MAP_H);
    }
    Vector2 Pos(int i) { return PosInRegion(i, currentRegion); }

    void RefreshAllLines()
    {
        CreateLineVisuals();
        RefreshStationDots();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        TickAutosave();
        UpdateRegionalMarkers(false);
        TickPress(dt);
        if (lineInsertActive) { TickDragEdgeAutoPan(dt); UpdateLineInsert(Input.mousePosition); }
        TickClock(dt);
        TickStationTraffic(dt);
        TickStory();
        TickResearch(dt);
        TickFloaters(dt);
        UpdateTrains(dt * timeSpeed / 6f);
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

            // 拖拽开线到边缘自动推镜 / 平滑滚屏
            TickDragEdgeAutoPan(dt);

            Vector2 local; RectTransformUtility.ScreenPointToLocalPointInRectangle(mapRect, Input.mousePosition, null, out local);
            float snapR = (currentRegion == "广东" || currentRegion == "澳门") ? 44f / Mathf.Max(1f, mapRect.localScale.x) : 44f;
            int near = NearestStationAny(local, snapR);
            bool backtracked = HandleDraftRevisit(near);
            int hov = near >= 0 && !draft.Contains(near) && !IsLoop(draft) ? near : -1;
            bool stoppedAtPortal = stations[dragFrom].portal && draft.Count >= 2;
            Vector2 end = backtracked || stoppedAtPortal ? Pos(dragFrom) : (hov >= 0 ? Pos(hov) : local);
            var p1 = Pos(dragFrom); var d = end - p1;
            previewImg.rectTransform.anchoredPosition = p1;
            previewImg.rectTransform.sizeDelta = new Vector2(Mathf.Max(1, d.magnitude), 16f * overlayZoom);
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
            UpdateDragCostBadge(end, local, dt);
            UpdateRings();
        }
        else if (!dragging) UpdateRings();
        if (Input.GetMouseButtonUp(0) && dragging) EndDraw();
        if (Input.GetMouseButtonUp(0) && lineInsertActive) EndLineInsert(Input.mousePosition);
        if (toastTimer > 0) { toastTimer -= dt; if (toastTimer <= 0 && toastGo) toastGo.SetActive(false); }
    }

    void TickDragEdgeAutoPan(float dt)
    {
        if (!mapWheelZoom && mapRect) mapWheelZoom = mapRect.GetComponent<MapWheelZoom>();
        if (!mapWheelZoom || !mapRect || pageRoot != null || activePanel != null) return;

        Vector2 mouse = Input.mousePosition;
        Rect safe = Screen.safeArea;
        if (safe.width <= 0 || safe.height <= 0) return;

        // 边缘智能感应区缓冲区厚度 (水平约 110px，垂直约 140px，预留顶部栏与底栏)
        float marginX = Mathf.Min(110f, safe.width * 0.16f);
        float marginY = Mathf.Min(140f, safe.height * 0.16f);

        float dirX = 0f;
        float dirY = 0f;

        // 左边缘：视口向左探寻 -> 地图向右平移
        if (mouse.x < safe.xMin + marginX && mouse.x >= safe.xMin - 40f)
        {
            float f = Mathf.Clamp01((safe.xMin + marginX - mouse.x) / marginX);
            dirX = f * f; // 二次平滑推进曲线
        }
        // 右边缘：视口向右探寻 -> 地图向左平移
        else if (mouse.x > safe.xMax - marginX && mouse.x <= safe.xMax + 40f)
        {
            float f = Mathf.Clamp01((mouse.x - (safe.xMax - marginX)) / marginX);
            dirX = -f * f;
        }

        // 下边缘：视口向下探寻 -> 地图向上平移
        if (mouse.y < safe.yMin + marginY && mouse.y >= safe.yMin - 40f)
        {
            float f = Mathf.Clamp01((safe.yMin + marginY - mouse.y) / marginY);
            dirY = f * f;
        }
        // 上边缘：视口向上探寻 -> 地图向下平移
        else if (mouse.y > safe.yMax - marginY && mouse.y <= safe.yMax + 40f)
        {
            float f = Mathf.Clamp01((mouse.y - (safe.yMax - marginY)) / marginY);
            dirY = -f * f;
        }

        if (dirX != 0f || dirY != 0f)
        {
            // 基础巡航速度：460px/s，丝滑稳健，受 MapWheelZoom.ClampPosition 防穿帮锁保护
            float speed = 460f;
            Vector2 delta = new Vector2(dirX, dirY) * (speed * dt);
            mapWheelZoom.PanBy(delta);
        }
    }

    // 只吸附当前真正显示出来的站点；吸附半径按级别给，小点不该有大门户的吸附范围
    bool IsStationShown(int i)
    {
        if (i >= 0 && i < stationVisible.Length) return stationVisible[i];
        return stationImgs.TryGetValue(i, out var img) && img.transform.parent.gameObject.activeInHierarchy;
    }
    float SnapRadius(int i, float radius)
    {
        if (stations[i].county) return radius * 0.82f;
        if (stations[i].portal || IsOpen(i)) return radius * 1.1f;
        return radius;
    }
    int NearestStation(Vector2 p, float radius)
    {
        int best = -1; float bd = float.MaxValue;
        for (int i = 0; i < stations.Count; i++)
        {
            if (!StationInRegion(i, currentRegion) || !IsStationShown(i) || draft.Contains(i)) continue;
            float d = Vector2.Distance(p, Pos(i));
            if (d < SnapRadius(i, radius) && d < bd) { bd = d; best = i; }
        }
        return best;
    }
    int NearestStationAny(Vector2 p, float radius)
    {
        int best = -1; float bd = float.MaxValue;
        for (int i = 0; i < stations.Count; i++)
        {
            if (!StationInRegion(i, currentRegion) || !IsStationShown(i)) continue;
            float d = Vector2.Distance(p, Pos(i));
            if (d < SnapRadius(i, radius) && d < bd) { bd = d; best = i; }
        }
        return best;
    }
    int StationOpeningCost(int index)
    {
        var station = stations[index];
        if (station.built) return 0;
        bool cityHub = station.administrativeCode > 0 && station.administrativeCode % 100 == 0;
        int hubCost = station.portal ? 70 : cityHub ? 50 : 0;
        return Mathf.RoundToInt(40f + StationBaselineDemand(index) * 0.7f + hubCost);
    }
    int StationEstimatedDemand(int index)
    {
        return Mathf.RoundToInt(StationBaselineDemand(index) * PassengerScale);
    }
    void BuildStationsOnPath(List<int> path)
    {
        foreach (int index in path) stations[index].built = true;
    }
    int DraftCostOf(List<int> path)
    {
        int c = 0;
        var counted = new HashSet<int>();
        foreach (int index in path) if (counted.Add(index)) c += StationOpeningCost(index);
        for (int i = 0; i < path.Count-1; i++)
            c += Mathf.RoundToInt(30 + Vector2.Distance(Pos(path[i]), Pos(path[i+1])) * 0.75f);
        return c;
    }
    bool HandleDraftRevisit(int near)
    {
        if (near < 0 || near == dragFrom || !draft.Contains(near)) return false;
        if (near == draft[0] && draft.Count >= 3 && !IsLoop(draft))
        {
            if (TryConnect(dragFrom, near)) { dragFrom = near; RedrawDraft(); PulseStation(near); }
            return false;
        }
        return BacktrackDraft(near);
    }
    bool BacktrackDraft(int station)
    {
        int idx = draft.IndexOf(station);
        if (idx < 0 || idx >= draft.Count-1) return false;
        draft.RemoveRange(idx+1, draft.Count-idx-1);
        dragFrom = station; hoverStation = -1;
        draftCost = DraftCostOf(draft);
        RedrawDraft(); UpdateRings();
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
        if (dragCostBadge) dragCostBadge.SetActive(false);
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
            if (!PathTouchesRegion(line.st, currentRegion)) { if (lv.train && lv.train.activeSelf) lv.train.SetActive(false); continue; }
            if (line.st.Count < 2) continue;
            if (RunningTrains(line) <= 0) { if (lv.train) lv.train.SetActive(false); continue; }
            float total = 0;
            for (int i = 0; i < line.st.Count - 1; i++)
            {
                string r = SegmentRegion(line.st[i], line.st[i + 1]);
                if (r != null) total += Vector2.Distance(PosInRegion(line.st[i], r), PosInRegion(line.st[i + 1], r));
            }
            if (total < 1f) continue;
            lv.t += dt * 58f * FleetTrait(line, 2) / total;
            bool loop = IsLoop(line.st);
            lv.t = Mathf.Repeat(lv.t, loop ? 1f : 2f);
            float phase = loop ? lv.t : (lv.t <= 1f ? lv.t : 2f - lv.t);
            float target = phase * total, acc = 0, ang = 0;
            Vector2 pos = Vector2.zero; string segmentRegion = null; int segmentIndex = -1;
            for (int i = 0; i < line.st.Count - 1; i++)
            {
                string r = SegmentRegion(line.st[i], line.st[i + 1]); if (r == null) continue;
                Vector2 a = PosInRegion(line.st[i], r), b = PosInRegion(line.st[i + 1], r);
                float length = Vector2.Distance(a, b);
                if (target <= acc + length || i == line.st.Count - 2)
                {
                    pos = Vector2.Lerp(a, b, (target - acc) / Mathf.Max(0.01f, length));
                    ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                    segmentRegion = r; segmentIndex = i; break;
                }
                acc += length;
            }
            if (lv.train)
            {
                int trainA = segmentIndex >= 0 ? line.st[segmentIndex] : -1;
                int trainB = segmentIndex >= 0 ? line.st[segmentIndex + 1] : -1;
                bool segShown = (trainA >= 0 && trainA < stationVisible.Length && stationVisible[trainA])
                             || (trainB >= 0 && trainB < stationVisible.Length && stationVisible[trainB]);
                lv.train.SetActive(segmentRegion == currentRegion && segShown && RunningTrains(line) > 0);
                var trt = lv.train.GetComponent<RectTransform>(); trt.anchoredPosition = pos;
                float dir = loop || lv.t <= 1f ? ang : ang + 180f;
                trt.localRotation = Quaternion.Euler(0, 0, dir);
            }
            // 飘字只反馈已连续入账的票款，不再重复改变余额。
            if (traffic != null) lv.credit += TrafficRevenue(line, traffic) * dt / 60f;
            int near = -1; float best = 34f;
            if (segmentIndex >= 0) for (int i=segmentIndex; i<=segmentIndex+1; i++)
            {
                if (stations[line.st[i]].closed) continue;
                float distance = Vector2.Distance(pos, PosInRegion(line.st[i], segmentRegion));
                if (distance < best) { best = distance; near = i; }
            }
            if (near >= 0 && near != lv.lastSt)
            {
                lv.lastSt = near; int amount = Mathf.FloorToInt(lv.credit);
                if (amount > 0) { lv.credit -= amount; if (segmentRegion == currentRegion) SpawnFloater(amount, Pos(line.st[near]) + new Vector2(0, 34)); }
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
    float TechMult() { return Mathf.Max(0.1f, 1f + ResearchBonus()); }
    void RecalcIncome()
    {
        RebuildForecasts();
        traffic = trafficSlots[DispatchSlot()];
        RebuildOperatingRates();
        float total = 0;
        foreach (var line in lines) total += ForecastFor(line).Profit;
        foreach (var station in stations) if (!station.closed) total += station.riderBonus * TechMult();
        income = total;
        UpdateRegionUnlocks();
        progress = Mathf.Clamp01(lines.Count / 6f * 0.5f + FoundCount() / (float)Mathf.Max(1, sceneries.Count) * 0.5f);
    }
    int FoundCount() { int n = 0; foreach (var s in sceneries) if (s.found) n++; return n; }
    void RPTick() { rp += 1f; RefreshHUD(); }

    // ---------- HUD ----------
    int lastHudCoins = int.MinValue, lastHudIncome = int.MinValue, lastHudDiamonds = int.MinValue, lastHudProgress = int.MinValue;
    void RefreshHUD()
    {
        int money = Mathf.FloorToInt(coins), profit = Mathf.RoundToInt(income), gems = Mathf.FloorToInt(diamonds), pct = Mathf.RoundToInt(progress * 100);
        if (coinTxt && money != lastHudCoins) { coinTxt.text = money.ToString(); lastHudCoins = money; }
        if (incomeTxt && profit != lastHudIncome) { incomeTxt.text = "预计日利润 " + profit; lastHudIncome = profit; }
        if (diamondTxt && gems != lastHudDiamonds) { diamondTxt.text = gems.ToString(); lastHudDiamonds = gems; }
        if (pct != lastHudProgress) {
            if (progTxt) progTxt.text = pct + "%";
            if (progFill) progFill.sizeDelta = new Vector2(180f * progress, 8f);
            lastHudProgress = pct;
        }
    }
    // ---------- panels ----------
    void TInk(Button b, Color c) { var t = b.GetComponentInChildren<Text>(); if (t) t.color = c; }

    GameObject OpenBubble(string title, float h, float w, Vector2 anchor)
    {
        CloseLineBubble();
        ClosePanel(true);
        scrim = Rect("BubbleDismiss", root, 0, 0, W, H);
        var bg = Img(scrim, new Color(0, 0, 0, 0));
        var dismiss = scrim.AddComponent<Button>(); dismiss.targetGraphic = bg;
        dismiss.transition = Selectable.Transition.None; dismiss.onClick.AddListener(ClosePanel);
        float x = Mathf.Clamp(anchor.x - w / 2f, 20, W - w - 20);
        float y = anchor.y + 60;
        if (y + h > H - 230) y = anchor.y - h - 60;
        y = Mathf.Clamp(y, 190, H - h - 200);
        activePanel = Rect("Bubble", root, x, y, w, h);
        Rounded(activePanel, new Color(0.99f,0.976f,0.95f,0.98f)); SoftShadow(activePanel);
        var heading = Txt(activePanel.transform, title, 32, Ink, 26, h-64, w-110, 46, TextAnchor.MiddleLeft);
        FitSingleLine(heading, 32);
        var close = Btn(activePanel.transform, "Close", "×", 28, w-72, h-68, 52, 52, new Color(0.92f,0.89f,0.83f), ClosePanel);
        TInk(close, Ink); panelOpenedFrame = Time.frameCount + 1;
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
        var heading = Txt(activePanel.transform, title, 34, Ink, 26, h-64, w-110, 46, TextAnchor.MiddleLeft);
        FitSingleLine(heading, 34);
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
        var onClosed = panelClosed; panelClosed = null;
        onClosed?.Invoke();
    }

    int ConnectedCount() { var set = new HashSet<int>(); foreach (var l in lines) foreach (var station in l.st) set.Add(station); return set.Count; }

    // ---------- scenery ----------
    void CheckSceneryOnLine(List<int> changedPath)
    {
        // 解锁以绑定车站是否被任一线路连通为准；解锁后不会因拆线撤销。
        bool changed = false;
        foreach (var sc in sceneries)
        {
            if (sc.unlocked) continue;
            foreach (var line in lines)
            {
                bool linked = false;
                foreach (int index in line.st)
                    if (!stations[index].closed && stations[index].region == sc.region && stations[index].name == sc.triggerStation) { linked = true; break; }
                if (!linked) continue;
                sc.unlocked = true; changed = true;
                if (!sc.found && TryGetSceneryContent(sc, out _, out _)) pendingCheckin.Add(sc);
                break;
            }
        }
        if (changed) RefreshSceneryViews();
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
    bool CompleteSceneryCheckin(Scenery sc)
    {
        if (sc == null || !sc.unlocked || sc.found || !TryGetSceneryContent(sc, out _, out _)) return false;
        BeginUndo("名胜打卡");
        sc.found = true; diamonds += 20;
        RefreshSceneryMarkerBadge(sc);
        pendingCheckin.Remove(sc);
        RecalcIncome(); RefreshHUD(); RefreshPendingBadge();
        QueueStory("first_scenery", "窗外的第一处风景", "列车靠站，车窗外的风景被旅客拍成了明信片。\n你忽然发现，大家乘车去的不只是下一站，也是自己期待已久的地方。");
        EndUndo();
        return true;
    }
    // ---------- helpers ----------
    void Toast(string msg)
    {
        if (toastGo) Destroy(toastGo);
        toastGo = Rect("Toast", root, 230, zoomNotice && zoomNotice.activeSelf ? 1508 : 1572, 620, 68);
        var toastCanvas = toastGo.AddComponent<Canvas>(); toastCanvas.overrideSorting = true; toastCanvas.sortingOrder = 200;
        Rounded(toastGo, new Color(0.17f,0.15f,0.13f,0.88f)); SoftShadow(toastGo);
        toastGo.GetComponent<Image>().raycastTarget = false;
        toastTxt = Txt(toastGo.transform, msg, 24, new Color(1,0.97f,0.9f), 16, 8, 588, 52, TextAnchor.MiddleCenter);
        toastTimer = 2.2f;
    }
    void Confirm(string title, string body, Action ok, Action cancel = null)
    {
        float bodyHeight = Mathf.Clamp(MeasureTextHeight(body, 26, 792) + 10, 44, H - 440);
        var p = OpenBubble(title, bodyHeight + 240, 860, new Vector2(W / 2, H / 2));
        ScrollableText(p.transform, body, 26, InkSoft, 34, 150, 792, bodyHeight);
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
        var t = go.AddComponent<Text>(); t.font = font; t.fontSize = size; t.color = c; t.text = DisplayPlaceAliases(s);
        t.alignment = a; t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }
    float MeasureTextHeight(string value, int size, float width)
    {
        var settings = new TextGenerationSettings { font = font, fontSize = size, color = Color.white,
            scaleFactor = 1, lineSpacing = 1, richText = true, generationExtents = new Vector2(width, 0),
            horizontalOverflow = HorizontalWrapMode.Wrap, verticalOverflow = VerticalWrapMode.Overflow };
        return Mathf.Ceil(new TextGenerator().GetPreferredHeight(DisplayPlaceAliases(value), settings));
    }
    void FitSingleLine(Text text, int size)
    {
        text.resizeTextForBestFit = true; text.resizeTextMinSize = Mathf.Max(12, size - 10);
        text.resizeTextMaxSize = size; text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
    }
    Text ScrollableText(Transform parent, string value, int size, Color color, float x, float y, float width, float height)
    {
        var view = Rect("TextViewport", parent, x, y, width, height);
        var bg = Img(view, Color.clear); bg.raycastTarget = true;
        view.AddComponent<RectMask2D>();
        var content = Rect("TextContent", view.transform, 0, 0, width, Mathf.Max(height, MeasureTextHeight(value, size, width) + 8));
        var rt = content.GetComponent<RectTransform>(); rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1); rt.anchoredPosition = Vector2.zero;
        var text = Txt(content.transform, value, size, color, 0, 0, width, rt.sizeDelta.y, TextAnchor.UpperLeft);
        var scroll = view.AddComponent<ScrollRect>(); scroll.viewport = view.GetComponent<RectTransform>();
        scroll.content = rt; scroll.horizontal = false; scroll.vertical = true; scroll.scrollSensitivity = 70;
        return text;
    }
    Button Btn(Transform p, string name, string label, int size, float x, float y, float w, float h, Color c, Action onClick)
    {
        var go = Rect(name, p, x, y, w, h);
        var im = Img(go, c, spriteRound); im.type = Image.Type.Sliced;
        var t = Txt(go.transform, label, size, new Color(1,0.98f,0.94f), 0, 0, w, h, TextAnchor.MiddleCenter);
        t.resizeTextForBestFit = true; t.resizeTextMinSize = Mathf.Max(12, size - 8); t.resizeTextMaxSize = size;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        var b = go.AddComponent<Button>(); b.targetGraphic = im; b.onClick.AddListener(() => onClick());
        return b;
    }
    void SoftShadow(GameObject go)
    {
        var sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0.16f,0.13f,0.10f,0.16f);
        sh.effectDistance = new Vector2(0, -5);
        sh.enabled = qualityTier > 0;
    }
    void TextShadow(Text text, Color color)
    {
        var shadow = text.gameObject.AddComponent<Shadow>(); shadow.effectColor = color; shadow.enabled = qualityTier > 0;
    }
    void Label(RectTransform p, string s, int size, float x, float y, Color c, float w)
    {
        var t = Txt(p, s, size, c, MAP_X + x*MAP_W - w/2, MAP_Y + y*MAP_H, w, 40, TextAnchor.MiddleCenter);
        TextShadow(t, new Color(1,1,1,0.7f));
    }
    void PortalLabel(RectTransform p, float x, float y, string s)
    {
        var t = Txt(p, s, 22, Vermilion, MAP_X + x*MAP_W - 110, MAP_Y + y*MAP_H, 220, 32, TextAnchor.MiddleCenter);
        TextShadow(t, new Color(1,1,1,0.85f));
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
    Sprite MakeHandDrawnRing(int s, float th)
    {
        var t = new Texture2D(s, s); var px = new Color[s * s];
        Vector2 center = new Vector2(s / 2f - 0.5f, s / 2f - 0.5f);
        float baseOuter = s / 2f - 1.2f;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            Vector2 delta = new Vector2(x, y) - center;
            float d = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x);
            float jitter = Mathf.Sin(angle * 3f) * 0.85f + Mathf.Cos(angle * 7f) * 0.45f;
            float outer = baseOuter + jitter;
            float inner = outer - th - Mathf.Sin(angle * 5f) * 0.35f;
            float a = Mathf.Clamp01(outer - d + 0.65f) * Mathf.Clamp01(d - inner + 0.65f);
            px[y * s + x] = new Color(1f, 1f, 1f, a);
        }
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(0, 0, 0, 0));
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


}

public class StationPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public int index; public DemoBootstrap boot;
    public void OnPointerDown(PointerEventData e) { boot.OnStationDown(index); }
    public void OnPointerUp(PointerEventData e) { boot.OnStationUp(index); }
}
