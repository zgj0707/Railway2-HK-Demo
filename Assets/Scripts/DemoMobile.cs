using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public static class Haptic
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void VibrateJS(int ms);
#endif
    public static void Vibrate(int ms)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { VibrateJS(ms); } catch { }
#elif UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#else
        _ = ms;
#endif
    }
}

public partial class DemoBootstrap
{
    // ---------- time / input / feedback ----------
    const float GameDaySeconds = 360f;
    float dayClock = 120f, researchClock; int day = 1; int timeSpeed = 1; int economicHour = -1;
    Text timeTxt, speedTxt;
    int pressIdx = -1; float pressTimer; bool pressing; Vector2 pressStart;
    AudioSource sfx, music; AudioClip toneLo, toneHi;
    bool musicEnabled = true;
    int qualityTier = 1;
    int selectedFrameRate = 60;
    static readonly int[] FrameRateOptions = { 30, 60, 120 };
    void ApplyFrameRate()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = selectedFrameRate;
    }
    void SetFrameRate(int fps)
    {
        if (fps != 30 && fps != 60 && fps != 120) return;
        selectedFrameRate = fps;
        PlayerPrefs.SetInt("target_fps", fps); PlayerPrefs.Save();
        ApplyFrameRate();
    }
    void ApplyVisualQuality()
    {
        ApplyFrameRate();
        foreach (var shadow in UnityEngine.Object.FindObjectsByType<Shadow>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (shadow && shadow.gameObject.scene.IsValid()) shadow.enabled = qualityTier > 0;
    }
    void SetVisualQuality(int tier)
    {
        qualityTier = Mathf.Clamp(tier, 0, 2);
        PlayerPrefs.SetInt("visual_quality", qualityTier); PlayerPrefs.Save();
        ApplyVisualQuality();
    }
    Image dragTip; int dragFrom = -1, hoverStation = -1;
    GameObject dock; List<Image> stationRings = new List<Image>();
    int pulseIdx = -1; float pulseTimer;

    class Floater { public Text txt; public RectTransform rt; public float age; }
    readonly List<Floater> floaters = new List<Floater>();

    void InitMobile()
    {
        var go = new GameObject("Sfx", typeof(AudioSource));
        go.transform.SetParent(transform, false);
        sfx = go.GetComponent<AudioSource>();
        var musicObject = new GameObject("BackgroundMusic", typeof(AudioSource));
        musicObject.transform.SetParent(transform, false);
        music = musicObject.GetComponent<AudioSource>();
        music.clip = Resources.Load<AudioClip>("Audio/Between_the_Bow_and_Breath");
        music.loop = true; music.playOnAwake = false; music.volume = 0.25f;
        if (music.clip && musicEnabled) music.Play();
        toneLo = MakeTone(620f, 0.07f);
        toneHi = MakeTone(940f, 0.055f);
        RefreshStationDots();
        UpdateTimeText();
    }
    void OpenAudioSettings()
    {
        var bubble = OpenBubble("设置", 680, 680, new Vector2(100, H - 250));
        Txt(bubble.transform, "画面质量", 27, InkSoft, 34, 515, 550, 44, TextAnchor.MiddleLeft);
        string[] levels = { "低", "中", "高" };
        for (int i = 0; i < levels.Length; i++) {
            int selected = i;
            var quality = Btn(bubble.transform, "Quality" + i, levels[i] + (qualityTier == i ? " · 当前" : ""), 26,
                34 + i * 205, 420, 190, 78, qualityTier == i ? Indigo : Paper2, () => { SetVisualQuality(selected); OpenAudioSettings(); });
            TInk(quality, qualityTier == i ? Color.white : Ink);
        }
        Txt(bubble.transform, "帧率上限", 27, InkSoft, 34, 350, 550, 44, TextAnchor.MiddleLeft);
        for (int i = 0; i < FrameRateOptions.Length; i++) {
            int fps = FrameRateOptions[i];
            var choice = Btn(bubble.transform, "FrameRate" + fps, fps + " FPS" + (selectedFrameRate == fps ? " · 当前" : ""), 24,
                34 + i * 205, 260, 190, 78, selectedFrameRate == fps ? Indigo : Paper2, () => { SetFrameRate(fps); OpenAudioSettings(); });
            TInk(choice, selectedFrameRate == fps ? Color.white : Ink);
        }
        Txt(bubble.transform, "实际帧率取决于屏幕刷新率和设备性能", 21, InkSoft, 34, 205, 612, 40, TextAnchor.MiddleLeft);
        var toggle = Btn(bubble.transform, "MusicToggle", musicEnabled ? "背景音乐：开" : "背景音乐：关", 30, 34, 54, 612, 92, Matcha, () => {
            musicEnabled = !musicEnabled;
            if (music) { if (musicEnabled && music.clip) music.Play(); else music.Stop(); }
            ClosePanel(); Toast(musicEnabled ? "背景音乐已开启" : "背景音乐已关闭");
        });
        TInk(toggle, Color.white);
    }

    AudioClip MakeTone(float freq, float dur)
    {
        int rate = 44100; int n = Mathf.CeilToInt(rate * dur);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Min(1f, t / 0.004f) * Mathf.Min(1f, (dur - t) / 0.02f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.30f;
        }
        var c = AudioClip.Create("tone", n, 1, rate, false);
        c.SetData(data, 0);
        return c;
    }
    void PlayTone(bool high) { if (sfx) sfx.PlayOneShot(high ? toneHi : toneLo); }

    // ---------- clock ----------
    void TickClock(float dt)
    {
        float remaining = Mathf.Max(0f, dt * timeSpeed);
        while (remaining > 0.0001f)
        {
            int hour = Mathf.FloorToInt(dayClock / 15f);
            if (economicHour != hour || traffic == null) { economicHour = hour; RecalcIncome(); }
            float step = Mathf.Min(remaining, (hour + 1) * 15f - dayClock);
            if (step <= 0.0001f) { dayClock = (hour + 1) * 15f; step = 0; }
            float fraction = step / GameDaySeconds;
            researchClock += step;
            while (researchClock >= 36f) { researchClock -= 36f; RPTick(); }
            foreach (var line in lines)
            {
                if (!operatingRates.TryGetValue(line, out var rate)) { RebuildOperatingRates(); rate = operatingRates[line]; }
                float gross = rate.gross * fraction;
                float vehicle = rate.vehicle * fraction, track = rate.track * fraction;
                line.todayGross += gross; line.todayMaintenance += vehicle; line.todayTrack += track;
                coins += gross - vehicle - track;
                line.todayRevenue = Mathf.RoundToInt(line.todayGross);
            }
            dayClock += step; remaining -= step;
            if (dayClock >= GameDaySeconds - 0.0001f)
            {
                dayClock = 0; day++; economicHour = -1;
                foreach (var line in lines)
                {
                    float beforeTax = line.todayGross - line.todayMaintenance - line.todayTrack;
                    float tax = Mathf.Max(0f, beforeTax) * 0.1f; coins -= tax;
                    line.recent.Add(Mathf.RoundToInt(beforeTax - tax));
                    if (line.recent.Count > 7) line.recent.RemoveAt(0);
                    line.todayGross = line.todayMaintenance = line.todayTrack = 0f; line.todayRevenue = 0;
                }
                dailyStationBaseline = DailyConnectedCount();
                RollTrafficEvents();
            }
        }
        UpdateTimeText();
    }
    int lastDisplayedMinute = -1, lastDisplayedDay = -1, lastDisplayedSpeed = -1;
    void UpdateTimeText()
    {
        if (!timeTxt) return;
        int mins = Mathf.FloorToInt(dayClock / GameDaySeconds * 1440f);
        if (mins != lastDisplayedMinute) { int hh = (mins / 60) % 24, mm = mins % 60; timeTxt.text = hh.ToString("00") + ":" + mm.ToString("00"); lastDisplayedMinute = mins; }
        if (dayTxt && day != lastDisplayedDay) { dayTxt.text = "第 " + day + " 日"; lastDisplayedDay = day; }
        if (speedTxt && timeSpeed != lastDisplayedSpeed) { speedTxt.text = timeSpeed == 0 ? "暂停" : timeSpeed + "×"; lastDisplayedSpeed = timeSpeed; }
    }
    void CycleSpeed()
    {
        BeginUndo("切换倍速"); timeSpeed = timeSpeed >= 3 ? 0 : timeSpeed + 1; EndUndo();
        UpdateTimeText();
    }

    // ---------- 客流与关键剧情 ----------
    float trafficRefresh;
    GameObject stationTrafficPanel;
    Text stationTrafficLabel;
    int stationTrafficIndex = -1;
    sealed class StoryEvent
    {
        public string title, text;
        // 预留立绘资源地址；当前只播放文字与 Unity 原生动效。
        public string portraitResource;
    }
    readonly Queue<StoryEvent> storyQueue = new Queue<StoryEvent>();
    readonly HashSet<string> storySeen = new HashSet<string>();
    GameObject storyCard;

    void TickStationTraffic(float dt)
    {
        trafficRefresh += dt;
        if (trafficRefresh < 2f) return;
        trafficRefresh = 0f;
        if (stationTrafficPanel && activePanel == stationTrafficPanel && stationTrafficLabel && stationTrafficIndex >= 0)
            {
            string description = StationTrafficDescription(stationTrafficIndex);
            stationTrafficLabel.text = DisplayPlaceAliases(description);
            var content = stationTrafficLabel.transform.parent.GetComponent<RectTransform>();
            float height = Mathf.Max(content.parent.GetComponent<RectTransform>().rect.height, MeasureTextHeight(description, 28, 884) + 8);
            content.sizeDelta = new Vector2(884, height); stationTrafficLabel.rectTransform.sizeDelta = new Vector2(884, height);
        }
        else { stationTrafficPanel = null; stationTrafficLabel = null; stationTrafficIndex = -1; }
    }
    int StationLineCount(int index)
    {
        int count = 0; foreach (var line in lines) if (line.st.Contains(index)) count++; return count;
    }
    string StationTrafficDescription(int index)
    {
        var station = stations[index]; int lineCount = StationLineCount(index);
        string state = !station.built ? "未开站 · 开站费用 ¥" + StationOpeningCost(index) : station.closed ? "已关停 · 列车通过不停站" : lineCount == 0 ? "已开站 · 等待线路接入" : "运营中 · " + lineCount + " 条线路";
        string lineNames = "";
        foreach (var line in lines) if (line.st.Contains(index)) lineNames += (lineNames.Length == 0 ? "" : "、") + line.name;
        float demand = StationRequestedFlow(index), served = StationPassengerFlow(index), cap = StationCapacity(index);
        string pressure = demand > cap ? "站点拥挤，升级可提高站内容量" : "站点容量充足";
        return state + (lineNames.Length > 0 ? "\n途经线路：" + lineNames : "") + "\n预估客流 " + StationEstimatedDemand(index) + " 人/日（潜在需求）" + "\n当前客流 " + Mathf.RoundToInt(served) + " / 日  ·  需求 " + Mathf.RoundToInt(demand) + " / 日\n" +
            "站内容量 " + Mathf.RoundToInt(cap) + " / 日 · " + pressure + "\n" +
            "流量事件：" + TrafficEventNames[station.trafficEvent] + "（需求 " +
            (TrafficEventFactors[station.trafficEvent] >= 1 ? "+" : "") + Mathf.RoundToInt((TrafficEventFactors[station.trafficEvent]-1)*100) + "%）\n" +
            StationAreaHint(index) + "\n线路覆盖、换乘、票价与班次决定可达客流。";
    }
    bool OpenStation(int index)
    {
        var station = stations[index];
        int cost = StationOpeningCost(index);
        if (coins < cost) { Toast("金币不足：还差 ¥" + Mathf.CeilToInt(cost - coins)); return false; }
        if (station.built && !station.closed) return true;
        BeginUndo(station.built ? "重新开站" : "开站");
        coins -= cost; station.built = true; station.closed = false;
        RefreshStationDots(); RecalcIncome(); CheckSceneryOnLine(null); EndUndo(); RefreshHUD();
        Toast(StationDisplayName(index, currentRegion) + " 已开站" + (cost > 0 ? " · ¥" + cost : ""));
        return true;
    }
    string StationIntroduction(int index)
    {
        var station = stations[index];
        switch (station.name)
        {
            case "广州": return "珠江之畔的商贸名城，街巷烟火与现代都市交融。";
            case "深圳（西九龙）": return "连接香江与鹏城的跨区域交通节点。";
            case "珠海（关闸）": return "海岛与海湾相映的滨海城市，邻接濠江。";
            case "东莞": return "水乡与制造业交织，城镇生活充满活力。";
            case "惠州": return "山海相依，湖光与海岸风景相伴。";
            case "佛山": return "岭南水乡与传统手艺相映的城市。";
            case "中山": return "水网密布的岭南侨乡，延续着香山文脉。";
            case "韶关": return "山水环绕的粤北门户，丹霞风光闻名。";
            case "汕头": return "滨海商埠，潮汕风味与侨乡记忆交织。";
            case "江门": return "五邑侨乡，骑楼与侨居故事相伴。";
            case "湛江": return "雷州半岛上的滨海港城，面海而兴。";
            case "茂名": return "粤西滨海城市，海岸与岭南果乡相伴。";
            case "肇庆": return "西江之畔，山水与端砚文脉相映。";
            case "梅州": return "山间客家之乡，围屋与乡村风物相连。";
            case "汕尾": return "依山面海的城市，渔港与海岸风光相伴。";
            case "河源": return "山水相依的东江上游城市。";
            case "阳江": return "滨海山城，以海景与传统刀剪闻名。";
            case "清远": return "北江穿城而过，山水与温泉相伴。";
            case "潮州": return "韩江之畔的古城，保留着潮州文化风韵。";
            case "揭阳": return "潮汕平原上的城市，榕江穿城而过。";
            case "云浮": return "群山之间的石材之乡，依西江而展。";
            case "罗湖": return "连接两地的口岸站，旅客往来频繁。";
            case "港珠澳": return "跨越海湾的交通节点，连接三地。";
            case "中环": return "海港边的商业街区，都市与山景相邻。";
            case "尖沙咀": return "临海的繁华街区，可远眺维多利亚港。";
            case "东涌": return "大屿山北岸的社区，邻近山海风景。";
            case "旺角": return "街市与商铺密集，生活气息浓厚。";
            case "大三巴": return "老城街巷环绕，留存着历史建筑风貌。";
            case "氹仔": return "海岛社区与街巷美食交融的区域。";
            case "路环": return "海岸与山林相伴，保留宁静的岛屿风貌。";
        }
        if (!string.IsNullOrEmpty(station.city))
            return "位于" + station.city + "，连接周边城镇与社区。";
        if (station.region == "澳门") return "濠江的沿线区域，中西风貌交融。";
        if (station.region == "香港") return "香江的沿线区域，山海与城市相伴。";
        return "沿线城镇与社区相连的地区。";
    }

    void ShowStationActions(int index)
    {
        if (index < 0 || index >= stations.Count || pageRoot || sceneryViewer) return;
        var station = stations[index];
        Vector3 world = mapRect.TransformPoint(Pos(index)); Vector2 anchor = root.InverseTransformPoint(world);
        string status = "预估客流 " + StationEstimatedDemand(index) + " 人/日";
        int lineCount = StationLineCount(index);
        if (!station.built) status += " · 开站 ¥" + StationOpeningCost(index);
        else if (station.closed) status += " · 已关停";
        else if (lineCount > 0) status += "\n当前客流 " + Mathf.RoundToInt(StationPassengerFlow(index)) + " 人/日";
        else status += " · 已开站";
        status += "\n" + StationAreaHint(index);
        float statusY = lineCount > 0 ? 166 : 92;
        float statusH = Mathf.Max(54, MeasureTextHeight(status, 20, 464) + 8);
        float introY = statusY + statusH + 8;
        float introH = Mathf.Max(30, MeasureTextHeight(StationIntroduction(index), 20, 464) + 4);
        var p = OpenBubble(StationDisplayName(index, currentRegion), introY + introH + 76, 520, anchor);
        var intro = Txt(p.transform, StationIntroduction(index), 20, InkSoft, 28, introY, 464, introH, TextAnchor.MiddleLeft);
        intro.name = "StationIntroduction";
        Txt(p.transform, status, 20, InkSoft, 28, statusY, 464, statusH, TextAnchor.UpperLeft);
        if (lineCount > 0) {
            var remove = Btn(p.transform, "RemoveWaystation", "移除线路途经站", 23, 28, 92, 464, 62, Paper2, () => OpenRemoveStationLines(index, 0));
            TInk(remove, Ink);
        }
        bool canOpen = !station.built || station.closed;
        if (canOpen)
        {
            var open = Btn(p.transform, "OpenStation", station.built ? "重新开站" : "开站 ¥" + StationOpeningCost(index), 23, 28, 18, 224, 62, Matcha, () => { if (OpenStation(index)) { ClosePanel(true); ShowStationActions(index); } });
            TInk(open, Color.white);
        }
        var detail = Btn(p.transform, "StationInfo", "站点详情", 24, canOpen ? 268 : 28, 18, canOpen ? 224 : 464, 62, Indigo, () => OpenStationInfo(index));
        TInk(detail, Color.white);
    }
    void OpenStationInfo(int index)
    {
        if (index < 0 || index >= stations.Count || pageRoot || sceneryViewer) return;
        var station = stations[index];
        string description = StationTrafficDescription(index);
        float contentHeight = Mathf.Clamp(MeasureTextHeight(description, 28, 884) + 16, 100, H - 620);
        var p = OpenPopup(StationDisplayName(index, currentRegion) + " · Lv." + station.level, contentHeight + 390, 960);
        stationTrafficPanel = p; stationTrafficIndex = index;
        stationTrafficLabel = ScrollableText(p.transform, description, 28, InkSoft, 38, 300, 884, contentHeight);
        int cost = Mathf.RoundToInt(100f * Mathf.Pow(1.65f, station.level - 1));
        var upgrade = Btn(p.transform, "UpgradeStation", station.level >= 10 ? "已满级" : "升级  ¥" + cost, 27, 38, 155, 420, 92, Matcha, () => {
            if (!station.built) { Toast("请先开站"); return; }
            if (station.level >= 10) { Toast("车站已满级"); return; }
            if (coins < cost) { Toast("金币不足"); return; }
            BeginUndo("升级车站"); coins -= cost; station.level++; RecalcIncome(); EndUndo(); RefreshHUD(); OpenStationInfo(index); Toast("车站升级成功");
        }); TInk(upgrade, Color.white);
        bool needsOpening = !station.built || station.closed;
        string toggleLabel = !station.built ? "开站 ¥" + StationOpeningCost(index) : station.closed ? "重新开站" : "关停";
        var toggle = Btn(p.transform, "ToggleStation", toggleLabel, 27, 500, 155, 420, 92, needsOpening ? Indigo : Vermilion, () => {
            if (needsOpening) { if (!OpenStation(index)) return; }
            else { BeginUndo("关停车站"); station.closed = true; RefreshStationDots(); RecalcIncome(); EndUndo(); RefreshHUD(); Toast(StationDisplayName(index, currentRegion) + " 已关停"); }
            OpenStationInfo(index);
        }); TInk(toggle, Color.white);
        var close = Btn(p.transform, "StationClose", "关闭", 26, 38, 36, 882, 80, new Color(0.92f,0.89f,0.83f), () => { ClosePanel(true); ShowStationActions(index); });
        TInk(close, Ink);
    }
    void QueueStory(string key, string title, string content)
    {
        if (!storySeen.Add(key)) return;
        storyQueue.Enqueue(new StoryEvent { title = title, text = content });
    }
    void TickStory()
    {
        if (storyCard || storyQueue.Count == 0 || pageRoot || activePanel || sceneryViewer || lineBubbleRoot || dragging) return;
        var story = storyQueue.Dequeue();
        storyCard = Rect("StoryCard", root, 40, 220, 1000, 360);
        Rounded(storyCard, new Color(0.99f, 0.975f, 0.94f, 0.98f)); SoftShadow(storyCard);
        var accent = Img(Rect("Accent", storyCard.transform, 0, 20, 8, 320), Gold, spriteRound); accent.raycastTarget = false;
        Txt(storyCard.transform, "沿途故事", 22, Matcha, 34, 308, 400, 32, TextAnchor.MiddleLeft);
        Txt(storyCard.transform, story.title, 36, Ink, 34, 254, 800, 50, TextAnchor.MiddleLeft);
        Txt(storyCard.transform, story.text, 27, Ink, 34, 100, 932, 140, TextAnchor.UpperLeft);
        var close = Btn(storyCard.transform, "Continue", "继续运营", 25, 740, 22, 226, 68, Matcha, () => { if (storyCard) Destroy(storyCard); storyCard = null; });
        TInk(close, Color.white);
        if (undoButton) undoButton.transform.SetAsLastSibling();
        var cg = storyCard.AddComponent<CanvasGroup>(); cg.alpha = 0;
        StartCoroutine(AnimateStory(storyCard.GetComponent<RectTransform>(), cg));
    }
    System.Collections.IEnumerator AnimateStory(RectTransform rect, CanvasGroup group)
    {
        Vector2 end = rect.anchoredPosition, start = end + new Vector2(0, -35); float elapsed = 0;
        while (rect && group && elapsed < 0.3f)
        {
            elapsed += Time.unscaledDeltaTime; float t = Mathf.Clamp01(elapsed / 0.3f); float ease = 1f - (1f-t)*(1f-t)*(1f-t);
            rect.anchoredPosition = Vector2.Lerp(start, end, ease); group.alpha = ease; yield return null;
        }
        if (rect && group) { rect.anchoredPosition = end; group.alpha = 1; }
    }

    // 拖线造价跟随指针，实际已吸附部分与待吸附段的预估分开标示。
    GameObject dragCostBadge;
    Text dragCostHeading, dragCostNumber;
    float dragCostDisplayed, dragCostPulse;
    int dragCostLastCommitted;
    void BuildDragCostBadge()
    {
        dragCostBadge = Rect("DragCostBadge", root, 0, 0, 250, 96);
        Rounded(dragCostBadge, new Color(0.99f,0.98f,0.95f,0.96f));
        dragCostBadge.GetComponent<Image>().raycastTarget = false; SoftShadow(dragCostBadge);
        dragCostHeading = Txt(dragCostBadge.transform, "线路造价", 20, InkSoft, 12, 62, 226, 26, TextAnchor.MiddleCenter);
        dragCostNumber = Txt(dragCostBadge.transform, "¥0", 38, Ink, 12, 12, 226, 49, TextAnchor.MiddleCenter);
        dragCostBadge.SetActive(false);
    }
    void UpdateDragCostBadge(Vector2 point, Vector2 pointer, float dt)
    {
        if (!dragCostBadge || draft.Count == 0) return;
        Vector2 current = Pos(draft[draft.Count - 1]);
        float remaining = Vector2.Distance(current, point);
        bool preview = !IsLoop(draft) && remaining > 24f;
        int value = draftCost + (preview ? Mathf.RoundToInt(30f + remaining * 0.75f) : 0);
        if (draftCost != dragCostLastCommitted) { dragCostLastCommitted = draftCost; dragCostPulse = 0.22f; }
        dragCostDisplayed = Mathf.Lerp(dragCostDisplayed, value, 1f - Mathf.Exp(-18f * dt));
        if (Mathf.Abs(dragCostDisplayed-value) < 0.5f) dragCostDisplayed = value;
        dragCostHeading.text = preview ? "预计造价 · 含拖拽段" : "已选造价";
        dragCostNumber.text = "¥" + Mathf.RoundToInt(dragCostDisplayed);
        dragCostNumber.color = value > coins ? Vermilion : Ink;
        dragCostPulse = Mathf.Max(0, dragCostPulse-dt);
        float pop = 1f + 0.12f * Mathf.Sin((dragCostPulse / 0.22f) * Mathf.PI);
        dragCostNumber.rectTransform.localScale = Vector3.one * pop;
        // 方向优先取「已确定的最后一段」，只有刚起步时跟鼠标；避免气泡随鼠标乱晃
        Vector2 dir;
        if (draft.Count >= 2) dir = Pos(draft[draft.Count - 1]) - Pos(draft[draft.Count - 2]);
        else dir = point - current;
        if (dir.sqrMagnitude < 4f) dir = new Vector2(0f, 1f);
        dir.Normalize();
        Vector2 at = (Vector2)root.InverseTransformPoint(mapRect.TransformPoint(pointer)) - root.rect.min;
        float gap = 132f + Mathf.Abs(dir.x) * 52f;  // 水平方向再多留一段，避免气泡压住拖拽点
        Vector2 anchor = at - dir * gap;            // 放在拖拽前进方向的反方向
        float x = Mathf.Clamp(anchor.x - 125f, 12f, W - 262f);
        float y = Mathf.Clamp(anchor.y - 48f, 180f, H - 301f);
        var badgeRt = dragCostBadge.GetComponent<RectTransform>();
        var target = new Vector2(x, y);
        if (dt >= 0.5f) badgeRt.anchoredPosition = target;   // 刚出现时直接落位
        else badgeRt.anchoredPosition = Vector2.Lerp(badgeRt.anchoredPosition, target, 1f - Mathf.Exp(-14f * dt));
    }

    // 与一代收益飘字共用深色调色板，保持浅色描边以便在地图上辨认。
    static readonly Color32[] FloaterColors = {
        new Color32(150, 100, 10, 255), new Color32(26, 100, 46, 255),
        new Color32(160, 44, 36, 255), new Color32(30, 70, 130, 255),
        new Color32(120, 70, 20, 255)
    };
    readonly System.Random floaterColorRandom = new System.Random();
    // ---------- income floaters ----------
    void SpawnFloater(int amount, Vector2 at)
    {
        if (qualityTier == 0) return;
        if (amount <= 0) return;
        var go = Rect("Float", floaterLayer, at.x - 60, at.y + 20, 160, 34);
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = 27; t.alignment = TextAnchor.MiddleCenter;
        t.color = FloaterColors[floaterColorRandom.Next(FloaterColors.Length)]; t.text = "+¥" + amount; t.raycastTarget = false;
        var sh = go.AddComponent<Shadow>(); sh.effectColor = new Color(1, 0.99f, 0.95f, 0.85f);
        var f = new Floater { txt = t, rt = go.GetComponent<RectTransform>(), age = 0f };
        floaters.Add(f);
    }
    void TickFloaters(float dt)
    {
        for (int i = floaters.Count - 1; i >= 0; i--)
        {
            var f = floaters[i];
            if (!f.rt || !f.txt) { floaters.RemoveAt(i); continue; }
            f.age += dt;
            f.rt.anchoredPosition += new Vector2(0, dt * 38f);
            var c = f.txt.color;
            c.a = Mathf.Clamp01(1f - (f.age - 0.55f) / 0.7f);
            f.txt.color = c;
            if (c.a <= 0f) { Destroy(f.rt.gameObject); floaters.RemoveAt(i); }
        }
    }

    void ClearFloaters()
    {
        foreach (var floater in floaters) if (floater.rt) Destroy(floater.rt.gameObject);
        floaters.Clear();
    }

    // ---------- station dots ----------
    bool[] stationOpen;
    void RefreshStationDots()
    {
        if (stationOpen == null || stationOpen.Length != stations.Count) stationOpen = new bool[stations.Count];
        for (int i = 0; i < stationOpen.Length; i++) stationOpen[i] = stations[i].built && !stations[i].closed;
        foreach (var kv in stationImgs)
        {
            if (stations[kv.Key].portal) continue;
            kv.Value.color = stationOpen[kv.Key] ? Gold : Color.white;
        }
        UpdateRegionalMarkers(true);
    }
    bool IsOpen(int i) { return stationOpen != null && i >= 0 && i < stationOpen.Length && stationOpen[i]; }

    // ---------- long press / draw ----------
    void BeginPress(int idx)
    {
        if (dragging) return;
        pressIdx = idx; pressTimer = 0f; pressing = true; pressStart = Input.mousePosition;
        HidePressRing();
    }
    void HidePressRing()
    {
        foreach (var kv in pressRings)
        {
            var ring = kv.Value;
            if (!ring) continue;
            if (ring.gameObject.activeSelf) ring.gameObject.SetActive(false);
            var rt = ring.rectTransform;
            rt.anchoredPosition = new Vector2(-14f, -14f);
            rt.localScale = Vector3.one;
        }
    }
    void TickPress(float dt)
    {
        if (!pressing) return;
        if (!Input.GetMouseButton(0))
        {
            pressing = false; pressIdx = -1;
            HidePressRing();
            return;
        }
        if (Vector2.Distance((Vector2)Input.mousePosition, pressStart) > 26f && pressTimer < 0.42f)
        {
            pressing = false; pressIdx = -1;
            HidePressRing();
            return;
        }
        pressTimer += dt;
        if (pressRings.TryGetValue(pressIdx, out var pressRing) && pressRing)
        {
            if (!pressRing.gameObject.activeSelf) pressRing.gameObject.SetActive(true);
            float p = Mathf.Clamp01(pressTimer / 0.42f);
            pressRing.fillAmount = p;
            pressRing.color = new Color(0.95f, 0.42f, 0.26f, Mathf.Lerp(0.62f, 0.98f, p));
            // 蓄力震颤：越接近长按完成，抖动越明显
            float t = Time.unscaledTime;
            float amp = Mathf.Lerp(0.5f, 2.8f, p * p);
            var rt = pressRing.rectTransform;
            rt.anchoredPosition = new Vector2(-14f + Mathf.Sin(t * 47f) * amp, -14f + Mathf.Cos(t * 61f) * amp);
            float sc = 1f + 0.035f * p * Mathf.Sin(t * 32f);
            rt.localScale = new Vector3(sc, sc, 1f);
        }
        if (pressTimer >= 0.42f)
        {
            HidePressRing();
            int idx = pressIdx; pressing = false; pressIdx = -1;
            StartDraw(idx);
        }
    }
    void StartDraw(int idx)
    {
        if (dragging || idx < 0 || idx >= stations.Count) return;
        // 线路已满时，从「不属于任何线路」的车站起手只能新开线 —— 在长按开始就拦住，别等拖完才说
        bool onLine = false;
        foreach (var line in lines) if (line.st.Contains(idx)) { onLine = true; break; }
        if (!onLine && RegionLineCount(currentRegion) >= LineLimit())
        {
            HidePressRing();
            Toast(currentRegion + "线路已达上限（" + LineLimit() + " 条）");
            return;
        }
        CloseLineBubble();
        draft.Clear(); draft.Add(idx); draftCost = DraftCostOf(draft);
        dragFrom = idx; dragging = true; hoverStation = -1;
        dragCostDisplayed = 0; dragCostLastCommitted = 0; dragCostPulse = 0;
        if (dragCostBadge) { dragCostBadge.SetActive(true); UpdateDragCostBadge(Pos(idx), Pos(idx), 1f); }
        RedrawDraft();
        if (previewImg) previewImg.gameObject.SetActive(true);
        if (dragTip) dragTip.gameObject.SetActive(true);
        UpdateRings();
        Haptic.Vibrate(22); PlayTone(false);
    }
    void EndDraw()
    {
        if (!dragging) return;
        dragging = false; hoverStation = -1;
        if (dragCostBadge) dragCostBadge.SetActive(false);
        if (previewImg) previewImg.gameObject.SetActive(false);
        if (dragTip) dragTip.gameObject.SetActive(false);
        UpdateRings();
        if (draft.Count >= 2) FinishDraft();
        else CancelDraft();
    }
    void CancelDraft()
    {
        draft.Clear(); draftCost = 0;
        if (dragCostBadge) dragCostBadge.SetActive(false);
        ClearDraftSegs(); ResetBuildVisuals();
    }
}
