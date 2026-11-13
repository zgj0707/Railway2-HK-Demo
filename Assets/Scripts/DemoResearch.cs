using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public partial class DemoBootstrap
{
    class ResearchVehicle { public int node; public int lineId; }
    class HistoryNode
    {
        public string key, name, description; public int column, depth, incomePercent, loco = -1;
        public ResearchEffect effect = ResearchEffect.Revenue;
        public int year;
        public string[] parents;
    }
    enum ResearchEffect { Revenue, Demand, Capacity, Speed, Station, Vehicle, Track, Transfer, None }
    readonly List<ResearchVehicle> researchVehicles = new List<ResearchVehicle>();
    readonly List<HistoryNode> historyNodes = new List<HistoryNode>();
    readonly Dictionary<string, HistoryNode> historyByKey = new Dictionary<string, HistoryNode>();
    static readonly string[] LocoNames = { "潘尼达伦", "萨拉曼卡", "喷气比利", "旅行者一号", "火箭号", "约翰牛", "鹰号", "仙女皇后", "纽约中央 999" };
    static readonly string[] LocoYears = { "1804", "1812", "1813", "1825", "1829", "1831", "1835", "1855", "1893" };
    static readonly string[] LocoArt = { "art_train_penydarren_1804", "art_train_salamanca_1812", "art_train_puffing_billy_1813", "art_train_locomotion_no1_1825", "art_train_rocket_1829", "art_train_john_bull_1831", "art_train_adler_1835", "art_train_fairy_queen_1855", "art_train_nyc_999_1893" };
    string historySelected;
    float historyScrollX = 0.5f, historyScrollY = 0.5f;
    RectTransform historyInfoRect, rpBadgeRect;
    Text historyInfo, historyHeading, historyRpText;
    GameObject historyInfoPanel, historyControls, eraJournalModal;
    bool techCollapseDone = true;
    readonly HashSet<string> techSessionUnlocked = new HashSet<string>();
    bool IsCollapsedIntoHub(string key, bool collapsed) { return collapsed && techDone.Contains(key) && !techSessionUnlocked.Contains(key); }
    TechCoinWidget historyCoinWidget;
    readonly HashSet<string> historyAnimated = new HashSet<string>();
    readonly Dictionary<string, Vector2> historyPositions = new Dictionary<string, Vector2>();
    readonly Dictionary<string, RectTransform> historyNodeRects = new Dictionary<string, RectTransform>();
    float historyZoom = 1f;
    HistoryTooltipFollow historyTooltipFollow;
    string historyJustUnlocked;
    GameObject historyPage;
    Sprite historyCircleOutline, historyHubDiscSprite;
    static Sprite MakeSmoothCircle(int s)
    {
        var t = new Texture2D(s, s);
        t.wrapMode = TextureWrapMode.Clamp;
        var px = new Color[s * s];
        float c = s * 0.5f - 0.5f;
        float r = s * 0.5f - 1.5f;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
            float a = Mathf.Clamp01(r - d + 0.75f);
            px[y * s + x] = new Color(1f, 1f, 1f, a);
        }
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f);
    }
    string LocoKey(int n) { return "loco19:" + n; }
    void AddHistory(string key, string name, int column, int depth, int effect, string description, params string[] parents)
    {
        var node = new HistoryNode { key = key, name = name, column = column, depth = depth, incomePercent = effect, description = description, parents = parents };
        historyNodes.Add(node); historyByKey.Add(key, node);
    }
    void AddHistoryLoco(int n, int column, int depth, string description, params string[] parents)
    {
        AddHistory(LocoKey(n), LocoNames[n], column, depth, 0, description, parents);
        historyByKey[LocoKey(n)].loco = n;
    }
    void InitHistoryTree()
    {
        if (historyNodes.Count > 0) return;
        AddHistory("dispatch_basic", "铁路时刻表", 0, 3, 0, "约 1820 年代，铁路开始按时刻组织运行。解锁统一车辆调度。", "pressure");
        AddHistory("dispatch_peak", "分时列车计划", 0, 8, 0, "19 世纪公共铁路发展推动分时安排列车。解锁早晚高峰与日夜分时调度。", "dispatch_basic", "public");
        AddHistory("fare_policy", "客运票价制度", 1, 9, 0, "公共铁路客运建立票价制度。解锁线路票价调整。", "dispatch_basic", "public");
        AddHistory("network_expand1", "铁路网络扩建", 0, 11, 3, "铁路由单线逐渐发展为网络，更多出行选择提升旅客对铁路的需求。", "public");
        AddHistory("network_expand2", "铁路运营组织", 0, 14, 4, "铁路公司扩大网络并协调各线接续，降低旅客换乘阻力。", "network_expand1", "manufacture");
        AddHistory("pressure", "高压蒸汽", 3, 0, 3, "高压蒸汽使动力装置更紧凑，为可移动蒸汽机提供了条件。", Array.Empty<string>());
        AddHistory("rail", "早期铸铁轨", 1, 1, -2, "早期轨道承重有限，重型机车可能压坏铁轨；发展初期须为维修付出代价。", "pressure");
        AddHistory("piston", "活塞与传动", 3, 1, 2, "汽缸内的蒸汽推动活塞，经机械传动带动车轮。", "pressure");
        AddHistory("valve", "锅炉安全阀", 5, 1, 2, "安全阀帮助释放过高的锅炉压力，降低蒸汽动力运行的风险。", "pressure");
        AddHistoryLoco(0, 3, 2, "1804 年的潘尼达伦机车是早期轨道蒸汽牵引的重要里程碑。完成后赠送一辆并解锁购买。", "piston", "rail");
        AddHistory("rack", "齿轨牵引", 1, 3, 2, "早期工程师曾借助齿轮与齿轨传递牵引力，以解决对轮轨黏着力不足的担忧。", LocoKey(0));
        AddHistory("adhesion", "轮轨黏着", 5, 3, 3, "利用车轮与轨道之间的摩擦传递牵引力，为不依赖齿轨的机车提供路径。", LocoKey(0));
        AddHistory("damage", "轨道受损", 0, 4, -2, "早期机车重量与脆弱轨道之间的矛盾造成轨道损坏，显示新技术需要配套改进。", "rack");
        AddHistoryLoco(1, 1, 4, "1812 年萨拉曼卡采用齿轨传动，是早期商业蒸汽机车的代表之一。", "rack");
        AddHistory("coupled", "多轮分担载重", 4, 4, 2, "通过多轮及传动安排分担重量，改善早期机车对脆弱轨道的适应。", "adhesion");
        AddHistoryLoco(2, 5, 5, "喷气比利于 1813—1814 年在怀勒姆煤矿使用，是现存最古老的蒸汽机车之一。", "adhesion", "coupled");
        AddHistory("twocyl", "双汽缸驱动", 2, 5, 3, "双汽缸布置改善动力传递，帮助机车从不同曲柄位置起动。", LocoKey(1), "piston");
        AddHistory("rods", "连杆驱动", 4, 6, 3, "连杆把汽缸往复运动转化为车轮旋转，并可带动多个动轮。", "twocyl", "coupled");
        AddHistoryLoco(3, 3, 7, "旅行者一号在 1825 年斯托克顿—达灵顿铁路开通时牵引列车。", "rods", LocoKey(2));
        AddHistory("public", "公共铁路运营", 1, 8, 3, "蒸汽牵引进入公共铁路，客货运输开始形成更广阔的运营场景。", LocoKey(3));
        AddHistory("blast", "排汽引风", 3, 8, 3, "将排汽引向烟囱能够加强炉膛通风；这一做法在早期机车的发展中不断改进。", LocoKey(3));
        AddHistory("tubes", "多烟管锅炉", 5, 8, 4, "多根烟管增加受热面积，提升锅炉产汽能力，成为火箭号的重要设计特征。", LocoKey(3), "valve");
        AddHistoryLoco(4, 4, 9, "1829 年火箭号参加雷恩希尔机车竞赛，多烟管锅炉与引风设计影响了后来的机车。", "blast", "tubes");
        AddHistory("speedrisk", "速度与安全", 1, 10, -3, "速度增加也放大制动和轨道不平顺带来的风险，技术进步伴随着新的运行代价。", "public", LocoKey(4));
        AddHistory("guide", "导轮与曲线", 3, 10, 3, "约翰牛在美国运行时增设前导轮，以改善机车通过曲线和不平顺轨道的表现。", LocoKey(4), "rods");
        AddHistoryLoco(5, 4, 11, "约翰牛于 1831 年抵达美国，后续增设导轮以减少脱轨问题。", "guide");
        AddHistoryLoco(6, 6, 11, "鹰号是 1835 年德国首条铁路运营的早期蒸汽机车代表。", LocoKey(4), "public");
        AddHistory("manufacture", "机车工厂", 1, 12, 2, "机车工厂和维修体系的发展有助于制造、维护并持续改进机车。", "public", LocoKey(5));
        AddHistory("braking", "连续制动探索", 3, 12, 2, "列车长度和速度增加后，如何让整列车更可靠地制动成为重要问题。", "speedrisk", "guide");
        AddHistory("feed", "锅炉给水", 5, 12, 2, "可靠地向锅炉补水是持续运行蒸汽机车的基础。", "tubes", LocoKey(6));
        AddHistoryLoco(7, 6, 13, "仙女皇后是一台 1855 年建造、服务于印度铁路的早期蒸汽机车。", "feed", "manufacture");
        AddHistory("injector", "蒸汽注水器", 5, 14, 3, "蒸汽注水器利用蒸汽将水送入锅炉，简化了锅炉给水装置。", "feed", LocoKey(7));
        AddHistory("straightbrake", "直通空气制动", 2, 14, 2, "直通空气制动将制动力传向整列车，但管路破损时存在失去制动力的风险。", "braking");
        AddHistory("brakefault", "制动管破损", 1, 15, -2, "直通空气制动的管路若破损，制动空气可能流失；这一局限促使工程师继续改良。", "straightbrake");
        AddHistory("autobrake", "自动空气制动", 3, 16, 4, "自动空气制动利用管路压力变化控制制动，针对断管等问题提供更可靠的制动方式。", "straightbrake", "brakefault");
        AddHistory("running", "高速走行", 5, 16, 3, "改进车轮、车架与导向布置，有助于高速运行；安全制动仍然是必要配套。", "guide", "manufacture");
        AddHistoryLoco(8, 4, 17, "纽约中央铁路 999 号是 19 世纪末快速蒸汽机车的代表之一。", "running", "autobrake", "injector");
        AddHistory("steamengine", "紧凑型蒸汽机", 2, 2, 2, "高压蒸汽有助于缩小动力装置，为机车上安装蒸汽机创造条件。", "pressure");
        AddHistory("minehaul", "矿山蒸汽运输", 0, 3, 2, "早期蒸汽机车在矿山运输中与马匹牵引并存，逐渐积累轨道运输经验。", LocoKey(0));
        AddHistory("walking", "机械步行机车", 0, 5, -2, "布伦顿的机械步行机车试图以机械腿推动机车；1815 年其锅炉爆炸造成严重伤亡。", "rack");
        AddHistory("cylinder", "汽缸与锅炉布置", 3, 4, 2, "早期设计将汽缸与锅炉结构结合，利用锅炉支承汽缸和相关作用力。", "piston", "steamengine");
        AddHistory("firetube", "单烟道锅炉", 3, 5, 2, "早期机车采用单一烟道输送热气，受热面积有限，后续设计继续扩大换热面积。", "cylinder");
        AddHistory("returnflue", "回程烟道", 3, 8, 2, "通过回程烟道增加热气经过锅炉的路程和受热面积，是多烟管普及前的改进方向之一。", "firetube", LocoKey(3));
        AddHistory("pressureloss", "蒸汽消耗", 4, 6, -1, "蒸汽动力的燃料和水消耗增加了早期运输成本，也推动锅炉效率持续改进。", "firetube");
        AddHistory("boilerburst", "旅行者号锅炉事故", 0, 9, -3, "1828 年旅行者一号锅炉爆炸，司机约翰·克里遇难；事故显示安全操作的重要性。", LocoKey(3), "valve");
        AddHistory("rebuild", "事故后的重建", 0, 10, 2, "锅炉爆炸后机车得到重建，运行经验推动早期铁路继续改进设备与维护。", "boilerburst");
        AddHistory("rainhill", "雷恩希尔试验", 5, 10, 2, "1829 年的雷恩希尔机车竞赛为铁路比较机车方案提供了公开试验场。", LocoKey(4));
        AddHistory("twinreturn", "双回程烟道", 4, 9, 2, "旅行者一号后来的双回程烟道扩大了受热面积，体现了持续改造的工程实践。", "returnflue");
        AddHistory("locoworks", "斯蒂芬森机车厂", 0, 8, 2, "1823 年设立的机车制造厂为铁路机车生产与工程积累提供了组织基础。", "rods");
        AddHistory("leadingtruck", "美国导向轮改装", 4, 12, 2, "约翰牛为适应曲线和不平顺轨道增加前导轮，反映出不同铁路条件下的改装。", LocoKey(5), "guide");
        AddHistory("linkmotion", "联杆配汽机构", 2, 13, 3, "1842 年的机车工作图纸记录了联杆配汽机构；这类机构用于控制蒸汽进排及反向。", "rods", "manufacture");
        AddHistory("firebox", "火箱受热面", 4, 13, 2, "19 世纪中叶的工程图比较不同机车火箱和锅炉受热面积，帮助工程师研究蒸汽发生能力。", "tubes", "feed");
        AddHistory("ramsvalve", "弹簧式安全阀", 1, 14, 3, "1856 年拉姆斯博顿设计的安全阀采用弹簧加压，旨在减少人为调整安全阀的风险。", "valve", "boilerburst");
        AddHistory("giffard", "吉法尔注水器", 6, 15, 3, "1858 年的吉法尔注水器利用蒸汽向运行中的锅炉供水。", "injector");
        AddHistory("directair", "空气制动推广", 1, 15, 2, "1869 年西屋公司成立，制造空气制动装置；制动技术随后经历多次改良。", "straightbrake");
        AddHistory("tripvalve", "自动制动控制", 2, 17, 3, "自动空气制动通过列车管压力变化控制制动，使断管等情况下的制动更可靠。", "autobrake");
        AddHistory("cylinderdrain", "汽缸排水与泄压", 4, 16, 2, "霍尔特 1878 年的汽缸排水与泄压阀将两类安全功能合并，以应对汽缸内的积水和超压。", "piston", "ramsvalve");
        AddHistory("safetyact", "铁路安全装置", 0, 18, 2, "1893 年美国铁路安全装置法推动空气制动器与自动车钩等安全设备的应用。", "autobrake", "manufacture");

        ConfigureResearchEffects();
        AddOperatingStudies();

    }
    bool HistoryVisible(HistoryNode node)
    {
        if (node.parents.Length == 0) return true;
        foreach (string parent in node.parents) if (techDone.Contains(parent)) return true;
        return false;
    }
    bool HistoryAvailable(HistoryNode node)
    {
        foreach (string parent in node.parents) if (!techDone.Contains(parent)) return false;
        return true;
    }
    readonly float[] researchEffects = new float[9];
    void SetResearchEffect(ResearchEffect effect, params string[] keys)
    {
        foreach (string key in keys) historyByKey[key].effect = effect;
    }
    void ConfigureResearchEffects()
    {
        SetResearchEffect(ResearchEffect.Speed, "pressure", "piston", "twocyl", "rods", "blast", "tubes", "guide", "running", "steamengine", "cylinder", "firetube", "returnflue", "twinreturn", "linkmotion", "firebox");
        SetResearchEffect(ResearchEffect.Capacity, "adhesion", "coupled", "rack", "braking", "straightbrake", "autobrake", "tripvalve", "leadingtruck");
        SetResearchEffect(ResearchEffect.Vehicle, "valve", "feed", "injector", "giffard", "manufacture", "ramsvalve", "cylinderdrain", "pressureloss", "boilerburst", "walking");
        SetResearchEffect(ResearchEffect.Track, "rail", "damage", "speedrisk", "rebuild", "safetyact", "brakefault");
        SetResearchEffect(ResearchEffect.Demand, "public", "minehaul", "rainhill", "network_expand1");
        SetResearchEffect(ResearchEffect.Transfer, "network_expand2");
        SetResearchEffect(ResearchEffect.Station, "locoworks", "directair");
        SetResearchEffect(ResearchEffect.None, "dispatch_basic", "dispatch_peak", "fare_policy");
    }
    void SyncResearchEffects()
    {
        Array.Clear(researchEffects, 0, researchEffects.Length);
        foreach (string key in techDone)
            if (historyByKey.TryGetValue(key, out var node) && node.loco < 0)
                researchEffects[(int)node.effect] += node.incomePercent * 0.01f;
    }
    float ResearchAmount(ResearchEffect effect)
    {
        float value = researchEffects[(int)effect];
        // 正向收益逐渐接近 50%，避免后期节点突然失效；负面影响单独限制在 25%。
        return value >= 0f ? 0.5f * value / (0.5f + value) : Mathf.Max(-0.25f, value);
    }
    float ResearchFactor(ResearchEffect effect) { return 1f + ResearchAmount(effect); }
    float ResearchCostFactor(ResearchEffect effect) { return 1f - ResearchAmount(effect); }
    float ResearchBonus() { return ResearchAmount(ResearchEffect.Revenue); }
    int HistoryCost(HistoryNode node)
    {
        if (node == null || node.key == "pressure") return 0;
        switch(node.key) {
            case "dispatch_basic": return 800; case "dispatch_peak": return 4000; case "fare_policy": return 3000;
            case "network_expand1": return 6000; case "network_expand2": return 28000;
        }
        if (node.loco == 0) return 260;
        // 单节点消费上限 100 万，从 1899 年向 1800 年按年代平滑递减
        int year = HistoryYear(node.key);
        float t = Mathf.Clamp01((year - 1800f) / 99f);
        float raw = 180f * Mathf.Pow(5500f, t) * (node.loco >= 0 ? 1.25f : 1f);
        int step = raw >= 10000f ? 1000 : raw >= 1000f ? 100 : 10;
        return Mathf.Clamp(Mathf.RoundToInt(raw / step) * step, 100, 1000000);
    }
    int HistoryRpCost(HistoryNode node)
    {
        if (node == null || node.key == "pressure") return 0;
        int year = HistoryYear(node.key);
        float t = Mathf.Clamp01((year - 1800f) / 99f);
        return Mathf.Clamp(Mathf.RoundToInt(1f + t * 14f), 1, 15);
    }
    string HistoryEffectText(HistoryNode node)
    {
        if (node.loco >= 0) return "赠送机车 ×1，解锁购买；配属后影响该线路运力、速度与维护费";
        switch (node.key) {
            case "dispatch_basic": return "解锁全天统一调度";
            case "dispatch_peak": return "解锁四时段列车调度";
            case "fare_policy": return "解锁线路票价调整";
        }
        string label;
        switch (node.effect) {
            case ResearchEffect.Demand: label = "潜在客流"; break;
            case ResearchEffect.Capacity: label = "列车区段运力"; break;
            case ResearchEffect.Speed: label = "列车速度"; break;
            case ResearchEffect.Station: label = "站点吞吐上限"; break;
            case ResearchEffect.Vehicle: label = "车辆维护费"; break;
            case ResearchEffect.Track: label = "线路维护费"; break;
            case ResearchEffect.Transfer: label = "换乘阻力"; break;
            default: label = "票务收入"; break;
        }
        int value = node.incomePercent;
        if (node.effect == ResearchEffect.Vehicle || node.effect == ResearchEffect.Track || node.effect == ResearchEffect.Transfer) value = -value;
        return label + (value >= 0 ? " +" : " ") + value + "% 基础值（同类收益递减）";
    }
    Color HistoryEffectColor(HistoryNode node)
    {
        if (node.loco >= 0) return new Color(0.79f, 0.49f, 0.18f);
        switch (node.effect) {
            case ResearchEffect.Revenue: return new Color(0.84f, 0.63f, 0.25f);
            case ResearchEffect.Demand: return new Color(0.82f, 0.41f, 0.35f);
            case ResearchEffect.Capacity: return new Color(0.30f, 0.49f, 0.72f);
            case ResearchEffect.Speed: return new Color(0.58f, 0.43f, 0.72f);
            case ResearchEffect.Station: return new Color(0.28f, 0.64f, 0.64f);
            case ResearchEffect.Vehicle: return new Color(0.39f, 0.62f, 0.37f);
            case ResearchEffect.Track: return new Color(0.61f, 0.48f, 0.37f);
            case ResearchEffect.Transfer: return new Color(0.78f, 0.43f, 0.61f);
            default: return new Color(0.43f, 0.48f, 0.56f);
        }
    }
    float EstimateResearchProfit(HistoryNode node)
    {
        if (node.loco >= 0 || node.effect == ResearchEffect.None || techDone.Contains(node.key)) return 0;
        RebuildForecasts(); float before = 0;
        foreach (var line in lines) before += ForecastFor(line).Profit;
        techDone.Add(node.key);
        try {
            RebuildForecasts(); float after = 0;
            foreach (var line in lines) after += ForecastFor(line).Profit;
            return after - before;
        } finally {
            techDone.Remove(node.key); RebuildForecasts(); traffic = trafficSlots[DispatchSlot()];
        }
    }
    void HoldHistoryNode(string key)
    {
        historySelected = key;
        if (!historyInfoPanel || !historyByKey.TryGetValue(key, out var node)) return;
        if (historyInfoRect) historyInfoRect.sizeDelta = new Vector2(900, 310);
        historyInfoPanel.SetActive(true);
        if (historyTooltipFollow && historyNodeRects.TryGetValue(key, out var target)) historyTooltipFollow.target = target;
        historyHeading.text = DisplayPlaceAliases(node.name);
        string effect = HistoryEffectText(node);
        string missing = "";
        foreach (var parent in node.parents) if (!techDone.Contains(parent)) missing += (missing.Length == 0 ? "" : "、") + historyByKey[parent].name;
        int rpNeed = HistoryRpCost(node);
        string costPart = "¥" + FormatCoins(HistoryCost(node)) + (rpNeed > 0 ? " · " + rpNeed + " RP" : "");
        string state = techDone.Contains(node.key) ? "已解锁" : missing.Length > 0 ? "前置：" + missing : "点击立研";
        string estimate = "";
        if (!techDone.Contains(key) && node.loco < 0 && node.effect != ResearchEffect.None) {
            float delta = EstimateResearchProfit(node);
            estimate = "\n当前线网日利润预计 " + FormatSignedNumber(delta);
        }
        historyInfo.text = DisplayPlaceAliases(HistoryPeriod(node) + " · 研发 " + costPart + "\n" + node.description + "\n" + effect + " · " + state + estimate);
    }
    void HoldRpRules()
    {
        if (!historyInfoPanel) return;
        if (historyInfoRect) historyInfoRect.sizeDelta = new Vector2(900, 340);
        historyInfoPanel.SetActive(true);
        if (historyTooltipFollow && rpBadgeRect) historyTooltipFollow.target = rpBadgeRect;
        int connected = DailyConnectedCount();
        int scalePct = Mathf.RoundToInt(ResearchScaleBonusRatio() * 100f);
        string turboLine = turboDaysLeft > 0 ? " · 4×牌照 +50% 生效中" : "";
        string dailyRate = EstimatedDailyRpRate().ToString("0.0");
        historyHeading.text = "◆ 科研点 (RP) · 获取规则与产能 ◆";
        historyInfo.text =
            "当前推演产能：约 " + dailyRate + " RP / 游戏日（规模加成 +" + scalePct + "%" + turboLine + "）\n" +
            "1. 时间积累：营业时每 36 游戏秒固定产出 1 RP；全网每连通 15 站额外提速 +15%（上限 +60%，当前 " + connected + " 站）。\n" +
            "2. 每日扩张补贴：公司页面每日净增 2 站领 +12 RP（看片双倍 +24 RP）。\n" +
            "3. 市政标书与特权：完成招标每次获 +14~+22 RP（专访 ×1.5）；公司「4× 极速牌照」可提速 +50%。";
    }
    void HideHistoryInfo() { if (historyInfoPanel) historyInfoPanel.SetActive(false); }
    void SetHistoryDragging(bool dragging)
    {
        if (historyControls) historyControls.SetActive(!dragging);
        if (dragging) HideHistoryInfo();
    }
    void TapHistoryNode(string key)
    {
        if (!historyByKey.TryGetValue(key, out var node) || techDone.Contains(key)) return;
        if (!HistoryAvailable(node)) { HoldHistoryNode(key); Invoke(nameof(HideHistoryInfo), 2f); return; }
        int cost = HistoryCost(node);
        int rpCost = HistoryRpCost(node);
        if (coins < cost) {
            Toast("研发经费不足：还差 ¥" + FormatShortfall(cost - coins));
            if (historyCoinWidget) historyCoinWidget.Shake();
            return;
        }
        if (rp < rpCost) {
            Toast("科研点不足：需要 " + rpCost + " RP（还差 " + Mathf.CeilToInt(rpCost - rp) + "，推进运营或接公司标书可获 RP）");
            return;
        }
        historyAnimated.Clear();
        foreach (var candidate in historyNodes) if (HistoryVisible(candidate)) historyAnimated.Add(candidate.key);
        BeginUndo("解锁科技");
        coins -= cost; rp -= rpCost; techDone.Add(key); techSessionUnlocked.Add(key);
        if (node.loco >= 0) {
            researchVehicles.Add(new ResearchVehicle { node = node.loco });
            if(node.loco==0 || node.loco==4 || node.loco==8) { diamonds+=50; Toast("机车里程碑：钻石 +50"); }
        }
        historyJustUnlocked = key;
        Haptic.Vibrate(20);
        RecalcIncome(); EndUndo(); RefreshHUD(); OpenTechScreen(1);
    }
    int HistoryYear(string key)
    {
        if (historyByKey.TryGetValue(key, out var node)) {
            if (node.loco >= 0) return int.Parse(LocoYears[node.loco]);
            if (node.year > 0) return node.year;
        }
        switch (key)
        {
            case "pressure": case "rail": case "piston": case "valve": case "steamengine": case "minehaul": return 1804;
            case "rack": case "adhesion": case "damage": case "coupled": case "twocyl": case "rods": case "walking": case "cylinder": case "firetube": case "pressureloss": return 1815;
            case "dispatch_basic": return 1825;
            case "dispatch_peak": case "fare_policy": case "network_expand1": return 1830;
            case "network_expand2": return 1850;
            case "public": case "blast": case "tubes": case "returnflue": case "locoworks": case "boilerburst": case "rebuild": case "rainhill": case "twinreturn": return 1829;
            case "speedrisk": case "guide": case "manufacture": case "leadingtruck": return 1835;
            case "linkmotion": return 1842;
            case "braking": case "feed": case "firebox": return 1855;
            case "ramsvalve": return 1856;
            case "injector": case "giffard": return 1858;
            case "straightbrake": case "directair": case "brakefault": return 1869;
            case "autobrake": case "tripvalve": return 1872;
            case "cylinderdrain": return 1878;
            case "running": case "safetyact": return 1893;
            default: return 1800;
        }
    }
    string HistoryPeriod(HistoryNode node)
    {
        int year = HistoryYear(node.key);
        if (node.loco >= 0) return year + " 年 · 机车年代";
        switch (node.key)
        {
            case "boilerburst": return "1828 年 · 历史事件";
            case "walking": return "1815 年 · 历史事件";
            case "rainhill": return "1829 年 · 历史事件";
            case "locoworks": return "1823 年 · 历史事件";
            case "linkmotion": return "1842 年 · 馆藏图纸";
            case "ramsvalve": return "1856 年 · 安全阀设计";
            case "injector": case "giffard": return "1858 年 · 注水器";
            case "straightbrake": case "directair": return "1869 年 · 空气制动发展";
            case "autobrake": case "tripvalve": return "1870 年代 · 自动制动";
            case "cylinderdrain": return "1878 年 · 专利装置";
            case "safetyact": return "1893 年 · 安全法规";
            default: return (year / 10 * 10) + " 年代前后 · 发展时期";
        }
    }
    int HistoryEra(string key)
    {
        int year = HistoryYear(key);
        if (year < 1810) return 0;
        if (year < 1820) return 1;
        if (year < 1830) return 2;
        if (year < 1850) return 3;
        if (year < 1870) return 4;
        return 5;
    }
    Vector2 HistoryPosition(string key)
    {
        if (historyPositions.TryGetValue(key, out var position)) return position;
        var node = historyByKey[key];
        if (node.parents.Length == 0) return historyPositions[key] = Vector2.zero;
        Vector2 origin = Vector2.zero; int count = 0;
        foreach (var parent in node.parents)
            if (historyPositions.TryGetValue(parent, out var p)) { origin += p; count++; }
        if (count > 0) origin /= count;
        int index = historyNodes.IndexOf(node);
        float angle = index * 2.399963f;
        return historyPositions[key] = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 160f;
    }
    void TickResearch(float dt) { }
    void OpenTechScreen(int mode = 0)
    {
        // mode == 0：重新进入科技树界面时清空本次解锁暂留名单并自动收纳全部已解锁节点；mode == 1：停留在页面内刷新，本次解锁节点留在外面
        if (mode == 0)
        {
            techCollapseDone = true;
            techSessionUnlocked.Clear();
        }
        InitHistoryTree();
        ClosePage(); pageBack = null; CloseLineBubble(); eraJournalModal = null;
        var page = Rect("HistoryFullScreen", canvas.transform, 0, 0, 0, 0);
        var pageRt = page.GetComponent<RectTransform>(); pageRt.anchorMin = Vector2.zero; pageRt.anchorMax = Vector2.one; pageRt.offsetMin = pageRt.offsetMax = Vector2.zero;
        Img(page, new Color(0.972f, 0.955f, 0.922f)).raycastTarget = false;
        pageRoot = page; historyPage = page;

        // 远景景深视差层（随拖拽 0.42× 慢速位移，营造裸眼 3D 立体纵深）
        var farLayer = Rect("FarParallaxLayer", page.transform, 0, 0, 0, 0);
        var farRt = farLayer.GetComponent<RectTransform>(); farRt.anchorMin = Vector2.zero; farRt.anchorMax = Vector2.one; farRt.offsetMin = farRt.offsetMax = Vector2.zero;
        if (!historyCircleOutline) historyCircleOutline = MakeRing(512, 2f);
        if (!historyHubDiscSprite) historyHubDiscSprite = MakeSmoothCircle(256);
        for (int rIdx = 0; rIdx < 3; rIdx++)
        {
            float dr = 420f + rIdx * 360f;
            var deepRing = Rect("DeepCompass_" + rIdx, farLayer.transform, 0, 0, dr * 2f, dr * 2f);
            var drt = deepRing.GetComponent<RectTransform>(); drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f); drt.pivot = new Vector2(0.5f, 0.5f); drt.anchoredPosition = Vector2.zero;
            Img(deepRing, new Color(0.24f, 0.34f, 0.48f, 0.075f - rIdx * 0.015f), historyCircleOutline).raycastTarget = false;
        }
        var crossH = Rect("CompassAxisH", farLayer.transform, 0, 0, 2600, 2);
        var chRt = crossH.GetComponent<RectTransform>(); chRt.anchorMin = chRt.anchorMax = new Vector2(0.5f, 0.5f); chRt.pivot = new Vector2(0.5f, 0.5f);
        Img(crossH, new Color(0.42f, 0.35f, 0.27f, 0.09f)).raycastTarget = false;
        var crossV = Rect("CompassAxisV", farLayer.transform, 0, 0, 2, 2600);
        var cvRt = crossV.GetComponent<RectTransform>(); cvRt.anchorMin = cvRt.anchorMax = new Vector2(0.5f, 0.5f); cvRt.pivot = new Vector2(0.5f, 0.5f);
        Img(crossV, new Color(0.42f, 0.35f, 0.27f, 0.09f)).raycastTarget = false;

        var view = Rect("HistoryViewport", page.transform, 0, 0, 0, 0);
        var viewRt = view.GetComponent<RectTransform>(); viewRt.anchorMin = Vector2.zero; viewRt.anchorMax = Vector2.one;
        viewRt.offsetMin = Vector2.zero; viewRt.offsetMax = Vector2.zero;
        Img(view, new Color(0.972f, 0.955f, 0.922f, 0.01f));
        var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>(); scroll.horizontal = true; scroll.vertical = true; scroll.scrollSensitivity = 0; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.viewport = viewRt;
        const float cw = 8000, ch = 8000;
        var content = Rect("HistoryTree", view.transform, 0, 0, cw, ch);
        var rt = content.GetComponent<RectTransform>(); rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1); rt.anchoredPosition = Vector2.zero; scroll.content = rt;
        rt.localScale = Vector3.one * historyZoom;
        scroll.horizontalNormalizedPosition = historyScrollX; scroll.verticalNormalizedPosition = historyScrollY;
        scroll.onValueChanged.AddListener(v => { historyScrollX = v.x; historyScrollY = v.y; });
        var zoom = view.AddComponent<HistoryGraphZoom>(); zoom.viewport = viewRt; zoom.content = rt; zoom.scroll = scroll;
        zoom.scale = historyZoom; zoom.onScaleChanged = value => historyZoom = value; zoom.onPinch = SetHistoryDragging;
        var drag = view.AddComponent<HistoryDragFeedback>(); drag.callback = SetHistoryDragging;
        var parallax = view.AddComponent<HistoryParallaxLayer>(); parallax.content = rt; parallax.target = farRt;

        string[] eras = { "1800s · 蒸汽萌芽", "1810s · 矿道初试", "1820s · 公共铁路", "1830—1849 · 制式确立", "1850—1869 · 干线时代", "1870—1899 · 电气前夜" };
        for (int i = 5; i >= 0; i--)
        {
            float radius = 270 + i * 230;
            var circle = Rect("EraCircle" + i, content.transform, 4000 - radius, 4000 - radius, radius * 2, radius * 2);
            Img(circle, new Color(0.58f, 0.45f, 0.30f, 0.20f), historyCircleOutline).raycastTarget = false;
            var outerHair = Rect("EraCircleHair" + i, content.transform, 4000 - (radius + 6), 4000 - (radius + 6), (radius + 6) * 2, (radius + 6) * 2);
            Img(outerHair, new Color(0.58f, 0.45f, 0.30f, 0.08f), historyCircleOutline).raycastTarget = false;
            var eraLbl = Txt(content.transform, "◆  " + eras[i], 25, new Color(0.42f, 0.33f, 0.23f, 0.92f), 4012, 4000 + radius + 12, 360, 38, TextAnchor.MiddleLeft);
            eraLbl.fontStyle = FontStyle.Normal; eraLbl.raycastTarget = false;
        }
        var edgeGo = Rect("HistoryEdges", content.transform, 0, 0, cw, ch);
        var edges = edgeGo.AddComponent<HistoryGraphEdges>(); edges.lineSprite = spriteWhite;
        var layout = content.AddComponent<HistoryGraphLayout>();
        layout.positions = historyPositions; layout.edges = edges;
        layout.updatesPerSecond = qualityTier == 0 ? 15f : 60f;
        var graphRects = new Dictionary<string, RectTransform>();
        historyNodeRects.Clear();

        int hubCollapsedCount = 0;
        if (techCollapseDone) foreach (var k in techDone) if (!techSessionUnlocked.Contains(k)) hubCollapsedCount++;
        bool collapsed = techCollapseDone && hubCollapsedCount > 0;
        const string HubKey = "__completed_hub__";
        if (collapsed)
        {
            historyPositions[HubKey] = Vector2.zero;
            const float hubSize = 370f;
            var hubGo = Rect("CompletedHub", content.transform, 4000f - hubSize * 0.5f, 4000f - hubSize * 0.5f, hubSize, hubSize);
            var hubRt = hubGo.GetComponent<RectTransform>();
            graphRects[HubKey] = hubRt;
            layout.Add(HubKey, hubRt, -1, Array.Empty<string>());

            // 多层典藏徽章圆盘视觉（使用高分抗锯齿实心圆底盘 + 立体浮雕暗底托）
            var hub3dShadow = Rect("Hub3DBase", hubGo.transform, -10, -12, hubSize + 4, hubSize + 4);
            Img(hub3dShadow, new Color(0.14f, 0.17f, 0.26f, 0.34f), historyHubDiscSprite).raycastTarget = false;
            var outerDisc = Rect("HubOuter", hubGo.transform, 0, 0, hubSize, hubSize);
            Img(outerDisc, new Color(0.76f, 0.56f, 0.25f, 0.98f), historyHubDiscSprite).raycastTarget = false;
            var midDisc = Rect("HubMid", hubGo.transform, 10, 10, hubSize - 20, hubSize - 20);
            Img(midDisc, new Color(0.27f, 0.22f, 0.17f, 0.95f), historyHubDiscSprite).raycastTarget = false;
            var innerDisc = Rect("HubInner", hubGo.transform, 18, 18, hubSize - 36, hubSize - 36);
            Img(innerDisc, new Color(0.985f, 0.968f, 0.928f), historyHubDiscSprite).raycastTarget = true;

            string subLabel = techSessionUnlocked.Count > 0 ? "本次新解 " + techSessionUnlocked.Count + " 项外留" : "— XIX CENTURY —";
            var hubSub = Txt(hubGo.transform, subLabel, 17, new Color(0.56f, 0.42f, 0.22f), 24, 260, hubSize - 48, 28, TextAnchor.MiddleCenter);
            hubSub.fontStyle = FontStyle.Normal; hubSub.raycastTarget = false;
            var hubTitle = Txt(hubGo.transform, "已研典藏图谱", 32, Ink, 24, 208, hubSize - 48, 46, TextAnchor.MiddleCenter);
            hubTitle.fontStyle = FontStyle.Normal; hubTitle.raycastTarget = false;
            var hubCount = Txt(hubGo.transform, hubCollapsedCount + " / " + historyNodes.Count, 48, new Color(0.74f, 0.48f, 0.16f), 24, 136, hubSize - 48, 64, TextAnchor.MiddleCenter);
            hubCount.fontStyle = FontStyle.Normal; hubCount.raycastTarget = false;
            bool allDone = hubCollapsedCount >= historyNodes.Count;
            var hubHintBg = Rect("HubHintChip", hubGo.transform, 68, 64, hubSize - 136, 44);
            Rounded(hubHintBg, new Color(0.24f, 0.34f, 0.48f, 0.12f));
            hubHintBg.GetComponent<Image>().raycastTarget = false;
            var hubHint = Txt(hubHintBg.transform, allDone ? "全卷圆满 · 点击检视" : "点击展开全部节点 ▸", 20, Indigo, 6, 0, hubSize - 148, 44, TextAnchor.MiddleCenter);
            hubHint.fontStyle = FontStyle.Normal; hubHint.raycastTarget = false;

            var hubBtn = hubGo.AddComponent<Button>();
            hubBtn.targetGraphic = innerDisc.GetComponent<Image>();
            hubBtn.onClick.AddListener(() => { techCollapseDone = false; OpenTechScreen(1); });
        }

        foreach (var node in historyNodes)
        {
            if (!HistoryVisible(node)) continue;
            bool done = techDone.Contains(node.key), ready = HistoryAvailable(node);
            if (IsCollapsedIntoHub(node.key, collapsed)) continue;

            Vector2 point = HistoryPosition(node.key);
            var card = Rect(node.key, content.transform, 4000 + point.x - 100, 4000 + point.y - 60, 200, 120);
            var cardRect = card.GetComponent<RectTransform>(); graphRects[node.key] = cardRect; historyNodeRects[node.key] = cardRect;

            string[] resolvedParents = node.parents;
            if (collapsed)
            {
                var mapped = new List<string>();
                bool linkedHub = false;
                foreach (string parent in node.parents)
                {
                    if (IsCollapsedIntoHub(parent, collapsed)) { if (!linkedHub) { mapped.Add(HubKey); linkedHub = true; } }
                    else mapped.Add(parent);
                }
                resolvedParents = mapped.ToArray();
            }
            layout.Add(node.key, cardRect, node.key == "pressure" ? -1 : HistoryEra(node.key), resolvedParents);
            Color categoryColor = HistoryEffectColor(node);
            bool locked = !done && !ready; // 前置不足

            // 卡片底色与立体浮雕底座：可解锁节点增加左下方偏置深靛硬影，营造裸眼 3D 悬浮卡片质感并保纯墨可读性
            Color cardBg = done ? Color.Lerp(categoryColor, Color.white, 0.60f)
                         : locked ? new Color(1f, 1f, 1f, 0.45f)
                         : new Color(0.996f, 0.992f, 0.976f);
            if (ready)
            {
                var card3dBase = Rect("Card3DBase", card.transform, -6, -7, 200, 120);
                Rounded(card3dBase, new Color(categoryColor.r * 0.45f, categoryColor.g * 0.45f, categoryColor.b * 0.52f, 0.38f));
                card3dBase.GetComponent<Image>().raycastTarget = false;
                var cardBody = Rect("CardBody", card.transform, 0, 0, 200, 120);
                Rounded(cardBody, cardBg);
                cardBody.GetComponent<Image>().raycastTarget = false;
            }
            Rounded(card, ready ? new Color(1f, 1f, 1f, 0.01f) : cardBg);

            // 分类圆标：前置不足低透明度弱化，其余恢复原样饱和色
            var marker = Rect("Category", card.transform, 8, 90, 17, 17);
            Color dotColor = locked ? new Color(categoryColor.r, categoryColor.g, categoryColor.b, 0.35f) : categoryColor;
            Img(marker, dotColor, spriteCircle).raycastTarget = false;

            // 标题文字：单一字重下使用 FontStyle.Normal 避免伪粗体发糊，改以字号与墨色深浅区分层级
            Color nameColor = locked ? new Color(Ink.r, Ink.g, Ink.b, 0.48f) : Ink;
            int nameMaxSize = ready ? 25 : 23;
            var name = Txt(card.transform, node.name, nameMaxSize, nameColor, 6, node.loco >= 0 ? 83 : 62, 188, node.loco >= 0 ? 32 : 48, TextAnchor.MiddleCenter);
            name.fontStyle = FontStyle.Normal;
            name.resizeTextForBestFit = true; name.resizeTextMinSize = 18; name.resizeTextMaxSize = nameMaxSize;

            if (node.loco >= 0)
            {
                var art = Rect("Locomotive", card.transform, 12, 36, 176, 46).AddComponent<RawImage>();
                art.texture = Resources.Load<Texture2D>("Research/Train/" + LocoArt[node.loco]); art.raycastTarget = false;
                if (locked) art.color = new Color(1f, 1f, 1f, 0.35f); // 前置不足机车图低透明度
                if (art.texture)
                {
                    float ratio = (float)art.texture.width / art.texture.height;
                    float iw = Mathf.Min(176, 46 * ratio), ih = iw / ratio;
                    art.rectTransform.sizeDelta = new Vector2(iw, ih); art.rectTransform.anchoredPosition = new Vector2((200 - iw) / 2, 36 + (46 - ih) / 2);
                }
            }

            // 状态标签文本：前置不足半透明，其余恢复原样排版与 InkSoft 字色
            string label = node.loco >= 0 ? "机车" : node.effect == ResearchEffect.None ? "运营解锁" : HistoryEffectText(node).Replace(" 基础值（同类收益递减）", "");
            Color labelColor = locked ? new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.45f) : InkSoft;
            var labelText = Txt(card.transform, label + (done ? " · 已解锁" : ready ? " · 可解锁" : " · 前置不足"), 17, labelColor, 6, 20, 188, 30, TextAnchor.MiddleCenter);
            labelText.resizeTextForBestFit = true; labelText.resizeTextMinSize = 12; labelText.resizeTextMaxSize = 17;
            var touch = card.AddComponent<HistoryNodeTouch>(); string key = node.key;
            touch.onTap = () => TapHistoryNode(key); touch.onHold = () => HoldHistoryNode(key); touch.onRelease = HideHistoryInfo;
            bool newlyVisible = historyJustUnlocked != null && !historyAnimated.Contains(key);
            if (qualityTier > 0 && (newlyVisible || historyJustUnlocked == key))
            {
                var fx = card.AddComponent<HistoryNodeReveal>(); fx.startDelay = newlyVisible ? 0.18f : 0f;
                var halo = Rect("UnlockHalo", card.transform, -30, -70, 260, 260);
                var haloImage = Img(halo, new Color(0.87f, 0.65f, 0.32f, 0.85f), historyCircleOutline); haloImage.raycastTarget = false;
                halo.transform.SetAsFirstSibling(); fx.halo = halo.GetComponent<RectTransform>();
                fx.halo.pivot = new Vector2(0.5f, 0.5f); fx.halo.anchoredPosition = new Vector2(100, 60);
                int sparkCount = qualityTier == 1 ? 6 : 16;
                for (int i = 0; i < sparkCount; i++)
                {
                    var spark = Rect("Spark", card.transform, 96, 56, 9, 9);
                    Img(spark, i % 2 == 0 ? Indigo : Gold, spriteCircle).raycastTarget = false;
                    fx.sparks.Add(spark.GetComponent<RectTransform>());
                }
            }
        }
        foreach (var node in historyNodes)
        {
            if (!graphRects.TryGetValue(node.key, out var child)) continue;
            bool linkedHub = false;
            foreach (string parent in node.parents)
            {
                if (IsCollapsedIntoHub(parent, collapsed))
                {
                    if (!linkedHub && graphRects.TryGetValue(HubKey, out var hubRect))
                    {
                        edges.Add(hubRect, child, true);
                        linkedHub = true;
                    }
                }
                else if (graphRects.TryGetValue(parent, out var parentRect))
                {
                    edges.Add(parentRect, child, techDone.Contains(parent));
                }
            }
        }
        edges.Refresh();
        if (qualityTier >= 2 && historyJustUnlocked != null) { var shake = page.AddComponent<HistoryCameraShake>(); shake.target = rt; }
        historyJustUnlocked = null;

        // 屏幕最外缘深色景深画框（类似透视黑框裁切效果：边框之外沉暗，中心明朗，顶栏与胶囊浮跃框前）
        Color matteEdge = new Color(0.14f, 0.13f, 0.16f, 0.88f);
        Color brassLine = new Color(0.76f, 0.58f, 0.30f, 0.72f);
        var frameLayer = Rect("DepthGalleryFrame", page.transform, 0, 0, 0, 0);
        var frt = frameLayer.GetComponent<RectTransform>(); frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
        Action<string, Vector2, Vector2, Vector2, Vector2> addBar = (bName, aMin, aMax, oMin, oMax) => {
            var g = Rect(bName, frameLayer.transform, 0, 0, 0, 0);
            var gr = g.GetComponent<RectTransform>(); gr.anchorMin = aMin; gr.anchorMax = aMax; gr.offsetMin = oMin; gr.offsetMax = oMax;
            Img(g, matteEdge).raycastTarget = false;
        };
        addBar("MatteLeft", new Vector2(0,0), new Vector2(0,1), new Vector2(0,0), new Vector2(10,0));
        addBar("MatteRight", new Vector2(1,0), new Vector2(1,1), new Vector2(-10,0), new Vector2(0,0));
        addBar("MatteTop", new Vector2(0,1), new Vector2(1,1), new Vector2(0,-10), new Vector2(0,0));
        addBar("MatteBot", new Vector2(0,0), new Vector2(1,0), new Vector2(0,0), new Vector2(0,10));
        var innerRim = Rect("BrassRim", frameLayer.transform, 0, 0, 0, 0);
        var irim = innerRim.GetComponent<RectTransform>(); irim.anchorMin = Vector2.zero; irim.anchorMax = Vector2.one; irim.offsetMin = new Vector2(10,10); irim.offsetMax = new Vector2(-10,-10);
        var rimImg = Img(innerRim, new Color(1,1,1,0)); rimImg.raycastTarget = false;
        var outRim = innerRim.AddComponent<Outline>(); outRim.effectColor = brassLine; outRim.effectDistance = new Vector2(2f, -2f);

        historyControls = Rect("HistoryControls", page.transform, 0, 0, 0, 0);
        var controlsRt = historyControls.GetComponent<RectTransform>(); controlsRt.anchorMin = Vector2.zero; controlsRt.anchorMax = Vector2.one; controlsRt.offsetMin = controlsRt.offsetMax = Vector2.zero;
        var back = Btn(historyControls.transform, "Back", "←", 40, 24, 0, 94, 94, new Color(1,1,1,0.95f), GoBackPage); TInk(back, Ink); SoftShadow(back.gameObject);
        var backRt = back.GetComponent<RectTransform>(); backRt.anchorMin = backRt.anchorMax = new Vector2(0,1); backRt.pivot = new Vector2(0,1); backRt.anchoredPosition = new Vector2(24,-28);
        var home = Btn(historyControls.transform, "Home", "主页", 24, 0, 0, 104, 94, new Color(1,1,1,0.95f), GoHome); TInk(home, Ink); SoftShadow(home.gameObject);
        var homeRt = home.GetComponent<RectTransform>(); homeRt.anchorMin = homeRt.anchorMax = new Vector2(0,1); homeRt.pivot = new Vector2(0,1); homeRt.anchoredPosition = new Vector2(134,-28);
        var titleCard = Rect("TechTitleBadge", historyControls.transform, 0, 0, 380, 60);
        var titleCardBg = Img(titleCard, new Color(1f, 0.99f, 0.96f, 0.96f), spriteRound);
        titleCardBg.type = Image.Type.Sliced; titleCardBg.raycastTarget = false;
        SoftShadow(titleCard);
        var titleRt = titleCard.GetComponent<RectTransform>(); titleRt.anchorMin = titleRt.anchorMax = new Vector2(0,1); titleRt.pivot = new Vector2(0,1); titleRt.anchoredPosition = new Vector2(254,-45);
        var title = Txt(titleCard.transform, "铁路典籍 · 科技树", 34, Ink, 0, 0, 0, 0, TextAnchor.MiddleLeft);
        title.fontStyle = FontStyle.Normal; title.raycastTarget = false;
        title.resizeTextForBestFit = true; title.resizeTextMinSize = 20; title.resizeTextMaxSize = 34;
        var titleTextRt = title.rectTransform; titleTextRt.anchorMin = Vector2.zero; titleTextRt.anchorMax = Vector2.one;
        titleTextRt.offsetMin = new Vector2(20, 4); titleTextRt.offsetMax = new Vector2(-148, -4);
        var titleSub = Txt(titleCard.transform, "XIX · ARCHIVE", 15, new Color(0.58f, 0.45f, 0.28f, 0.85f), 0, 0, 0, 0, TextAnchor.MiddleRight);
        titleSub.fontStyle = FontStyle.Normal; titleSub.raycastTarget = false;
        var titleSubRt = titleSub.rectTransform; titleSubRt.anchorMin = Vector2.zero; titleSubRt.anchorMax = Vector2.one;
        titleSubRt.offsetMin = new Vector2(16, 4); titleSubRt.offsetMax = new Vector2(-18, -4);
        backRt.anchorMin = backRt.anchorMax = Vector2.zero;
        homeRt.anchorMin = homeRt.anchorMax = Vector2.zero;
        titleRt.anchorMin = titleRt.anchorMax = Vector2.zero;

        // 顶栏实时金币展示与不足震颤提示组件
        var coinBadge = Rect("TechCoinBadge", historyControls.transform, 0, 0, 224, 94);
        var coinBadgeRt = coinBadge.GetComponent<RectTransform>();
        coinBadgeRt.anchorMin = coinBadgeRt.anchorMax = Vector2.zero;
        var coinShakeTarget = Rect("ShakeTarget", coinBadge.transform, 0, 0, 224, 94);
        var coinBgImg = Img(coinShakeTarget, new Color(1f, 0.99f, 0.97f, 0.96f), spriteRound);
        coinBgImg.type = Image.Type.Sliced;
        SoftShadow(coinShakeTarget);
        var coinRing = Rect("CoinRing", coinShakeTarget.transform, 16, 19, 56, 56);
        Img(coinRing, new Color(0.25f, 0.21f, 0.17f, 0.88f), spriteStationRing).raycastTarget = false;
        var coinIcon = Rect("Icon", coinRing.transform, 4, 4, 48, 48);
        Img(coinIcon, Gold, spriteCircle).raycastTarget = false;
        var coinText = Txt(coinShakeTarget.transform, FormatCoins(coins), 36, Ink, 82, 18, 126, 58, TextAnchor.MiddleLeft);
        coinText.resizeTextForBestFit = true; coinText.resizeTextMinSize = 18; coinText.resizeTextMaxSize = 36;

        var widget = coinBadge.AddComponent<TechCoinWidget>();
        widget.shakeTarget = coinShakeTarget.GetComponent<RectTransform>();
        widget.coinText = coinText;
        widget.bgImage = coinBgImg;
        widget.normalTextColor = Ink;
        widget.normalBgColor = new Color(1f, 0.99f, 0.97f, 0.96f);
        widget.alertColor = Vermilion;
        widget.getCoins = () => coins;
        historyCoinWidget = widget;

        var headerLayout = historyControls.AddComponent<PageHeaderLayout>();
        headerLayout.page = controlsRt; headerLayout.back = backRt; headerLayout.home = homeRt; headerLayout.title = titleRt;
        headerLayout.coinBadge = coinBadgeRt;
        var progressBadge = Rect("ProgressBadge", historyControls.transform, 24, 125, 248, 50);
        var pbg = Img(progressBadge, new Color(1f, 0.99f, 0.97f, 0.99f), spriteRound); pbg.type = Image.Type.Sliced; SoftShadow(progressBadge);
        var prt = progressBadge.GetComponent<RectTransform>(); prt.anchorMin = prt.anchorMax = new Vector2(0,1); prt.pivot = new Vector2(0,1); prt.anchoredPosition = new Vector2(24,-138);
        Txt(progressBadge.transform, "进度  " + techDone.Count + " / " + historyNodes.Count, 22, Ink, 16, 8, 218, 34, TextAnchor.MiddleLeft);

        var rpBadge = Rect("RPBadge", historyControls.transform, 286, 125, 290, 50);
        var rbg = Img(rpBadge, new Color(1f, 0.99f, 0.97f, 0.99f), spriteRound); rbg.type = Image.Type.Sliced; rbg.raycastTarget = true; SoftShadow(rpBadge);
        var rrt = rpBadge.GetComponent<RectTransform>(); rrt.anchorMin = rrt.anchorMax = new Vector2(0,1); rrt.pivot = new Vector2(0,1); rrt.anchoredPosition = new Vector2(286,-138);
        rpBadgeRect = rrt;
        var rpDot = Rect("RPDot", rpBadge.transform, 16, 16, 18, 18); Img(rpDot, Indigo, spriteCircle).raycastTarget = false;
        historyRpText = Txt(rpBadge.transform, "科研点  " + Mathf.FloorToInt(rp) + " RP", 22, Indigo, 42, 8, 206, 34, TextAnchor.MiddleLeft);
        historyRpText.resizeTextForBestFit = true; historyRpText.resizeTextMinSize = 16; historyRpText.resizeTextMaxSize = 22; historyRpText.raycastTarget = false;
        var rpHelpHint = Txt(rpBadge.transform, "?", 20, InkSoft, 250, 8, 28, 34, TextAnchor.MiddleCenter);
        rpHelpHint.raycastTarget = false;
        var rpTouch = rpBadge.AddComponent<HistoryNodeTouch>();
        rpTouch.onHold = HoldRpRules;
        rpTouch.onRelease = HideHistoryInfo;
        rpTouch.onTap = () => Toast("长按科研点徽章查看获取规则（当前日产 " + EstimatedDailyRpRate().ToString("0.0") + " RP）");

        var eraBadge = Btn(historyControls.transform, "EraRoadmapBadge", "19世纪 · 蒸汽与轨交  ▾", 22, 24, 0, 298, 44, new Color(1f, 0.99f, 0.96f, 0.99f), () => OpenEraJournalModal(page.transform));
        eraBadge.GetComponentInChildren<Text>().fontStyle = FontStyle.Normal;
        TInk(eraBadge, new Color(0.45f, 0.31f, 0.16f));
        SoftShadow(eraBadge.gameObject);
        Img(Rect("EraAccent", eraBadge.transform, 0, 8, 6, 28), Gold, spriteRound).raycastTarget = false;
        var ert = eraBadge.GetComponent<RectTransform>(); ert.anchorMin = ert.anchorMax = new Vector2(0, 1); ert.pivot = new Vector2(0, 1); ert.anchoredPosition = new Vector2(24, -198);

        if ((!techCollapseDone || techSessionUnlocked.Count > 0) && techDone.Count > 0)
        {
            var collapseBtn = Btn(historyControls.transform, "CollapseDoneBtn", "收纳已研节点 ⟲", 20, 336, 0, 204, 44, new Color(0.95f, 0.92f, 0.85f, 0.98f), () => { techSessionUnlocked.Clear(); techCollapseDone = true; OpenTechScreen(1); });
            TInk(collapseBtn, Indigo);
            SoftShadow(collapseBtn.gameObject);
            var crt2 = collapseBtn.GetComponent<RectTransform>(); crt2.anchorMin = crt2.anchorMax = new Vector2(0, 1); crt2.pivot = new Vector2(0, 1); crt2.anchoredPosition = new Vector2(336, -198);
        }

        var legend = Rect("ResearchLegend", historyControls.transform, 0, 0, 1032, 104);
        var legendRt = legend.GetComponent<RectTransform>(); legendRt.anchorMin = legendRt.anchorMax = new Vector2(0.5f, 0); legendRt.pivot = new Vector2(0.5f, 0); legendRt.anchoredPosition = new Vector2(0, 20);
        Rounded(legend, new Color(1f, 0.99f, 0.96f, 0.97f)); SoftShadow(legend);
        string[] legendNames = { "收入", "客流", "运力", "速度", "站点吞吐", "车辆维护", "线路维护", "换乘", "机车", "运营解锁" };
        ResearchEffect[] legendKinds = { ResearchEffect.Revenue, ResearchEffect.Demand, ResearchEffect.Capacity, ResearchEffect.Speed, ResearchEffect.Station, ResearchEffect.Vehicle, ResearchEffect.Track, ResearchEffect.Transfer, ResearchEffect.None, ResearchEffect.None };
        for (int i = 0; i < legendNames.Length; i++) {
            float x = 16 + (i % 5) * 204, y = i < 5 ? 62 : 18;
            var sample = new HistoryNode { effect = legendKinds[i], loco = i == 8 ? 0 : -1 };
            var dot = Rect("LegendDot" + i, legend.transform, x, y + 6, 18, 18); Img(dot, HistoryEffectColor(sample), spriteCircle).raycastTarget = false;
            Txt(legend.transform, legendNames[i], 22, InkSoft, x + 24, y, 174, 32, TextAnchor.MiddleLeft);
        }
        var info = Rect("HistoryInfo", page.transform, 0, 0, 900, 310); Rounded(info, new Color(0.97f,0.94f,0.88f,0.99f)); SoftShadow(info);
        var irt = info.GetComponent<RectTransform>(); irt.anchorMin = irt.anchorMax = new Vector2(0.5f,0.5f); irt.pivot = new Vector2(0.5f,0); irt.anchoredPosition = Vector2.zero;
        var infoBorder = Rect("InfoInnerFrame", info.transform, 10, 10, 0, 0);
        var infoFrameRt = infoBorder.GetComponent<RectTransform>(); infoFrameRt.anchorMin = Vector2.zero; infoFrameRt.anchorMax = Vector2.one; infoFrameRt.offsetMin = new Vector2(10, 10); infoFrameRt.offsetMax = new Vector2(-10, -10);
        Img(infoBorder, new Color(0.68f, 0.52f, 0.28f, 0.14f), spriteRound).raycastTarget = false;
        var tapeGo = Rect("InfoTape", info.transform, 0, 0, 148, 22);
        var tapeRt = tapeGo.GetComponent<RectTransform>(); tapeRt.anchorMin = tapeRt.anchorMax = new Vector2(0.5f, 1f); tapeRt.pivot = new Vector2(0.5f, 0.5f); tapeRt.anchoredPosition = new Vector2(0, 2);
        tapeGo.transform.localRotation = Quaternion.Euler(0, 0, -1.5f);
        var tapeIm = Img(tapeGo, new Color(0.88f, 0.78f, 0.62f, 0.78f), spriteRound); tapeIm.type = Image.Type.Sliced; tapeIm.raycastTarget = false;
        historyInfoRect = irt;
        historyTooltipFollow = info.AddComponent<HistoryTooltipFollow>(); historyTooltipFollow.page = pageRt;
        historyInfoPanel = info;
        historyHeading = Txt(info.transform, "", 32, Ink, 28, 0, 844, 46, TextAnchor.MiddleLeft);
        historyHeading.fontStyle = FontStyle.Normal;
        var hrt = historyHeading.rectTransform; hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1); hrt.pivot = new Vector2(0.5f, 1); hrt.offsetMin = new Vector2(28, -62); hrt.offsetMax = new Vector2(-28, -16);
        historyInfo = Txt(info.transform, "", 24, InkSoft, 28, 24, 844, 210, TextAnchor.UpperLeft);
        var infoBodyRt = historyInfo.rectTransform; infoBodyRt.anchorMin = Vector2.zero; infoBodyRt.anchorMax = Vector2.one; infoBodyRt.offsetMin = new Vector2(28, 20); infoBodyRt.offsetMax = new Vector2(-28, -68);
        historyInfo.resizeTextForBestFit = true; historyInfo.resizeTextMinSize = 18; historyInfo.resizeTextMaxSize = 24;
        historyInfoPanel.SetActive(false);
    }

    void OpenEraJournalModal(Transform parent)
    {
        if (eraJournalModal) Destroy(eraJournalModal);
        var modal = Rect("EraJournalModal", parent, 0, 0, 0, 0);
        var mrt = modal.GetComponent<RectTransform>(); mrt.anchorMin = Vector2.zero; mrt.anchorMax = Vector2.one; mrt.offsetMin = mrt.offsetMax = Vector2.zero;
        eraJournalModal = modal;

        var scrimBg = Img(modal, new Color(0.06f, 0.07f, 0.11f, 0.68f));
        var closeBtn = modal.AddComponent<Button>(); closeBtn.targetGraphic = scrimBg; closeBtn.transition = Selectable.Transition.None;
        closeBtn.onClick.AddListener(() => { if (eraJournalModal) Destroy(eraJournalModal); eraJournalModal = null; });

        const float cardW = 900f, cardH = 1140f;
        var sheet = Rect("JournalSheet", modal.transform, 0, 0, cardW, cardH);
        var srt = sheet.GetComponent<RectTransform>(); srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f); srt.pivot = new Vector2(0.5f, 0.5f); srt.anchoredPosition = Vector2.zero;
        Rounded(sheet, new Color(0.972f, 0.948f, 0.895f, 0.99f));
        SoftShadow(sheet);

        var frameOuter = Rect("JournalFrame", sheet.transform, 18, 18, cardW - 36, cardH - 36);
        Rounded(frameOuter, new Color(0.65f, 0.50f, 0.28f, 0.14f));
        frameOuter.GetComponent<Image>().raycastTarget = false;
        var frameInner = Rect("JournalInner", sheet.transform, 24, 24, cardW - 48, cardH - 48);
        Rounded(frameInner, new Color(0.988f, 0.974f, 0.942f));
        frameInner.GetComponent<Image>().raycastTarget = false;
        JournalTape(sheet.transform, cardW * 0.5f - 95f, cardH - 18f, 190f);

        var enHeader = Txt(sheet.transform, "CHRONICLES OF RAILWAY  ·  XIX — XXI CENTURY", 19, new Color(0.55f, 0.42f, 0.25f), 44, cardH - 86, cardW - 88, 28, TextAnchor.MiddleCenter);
        enHeader.fontStyle = FontStyle.Normal;
        var cnHeader = Txt(sheet.transform, "— ◆ 铁道百年 · 时代手帐 ◆ —", 38, Ink, 44, cardH - 144, cardW - 88, 52, TextAnchor.MiddleCenter);
        cnHeader.fontStyle = FontStyle.Normal;
        var subHeader = Txt(sheet.transform, "手绘霓虹色彩视差纪元卷册 · 点击进入或查阅详情", 22, InkSoft, 44, cardH - 184, cardW - 88, 32, TextAnchor.MiddleCenter);

        var xClose = Btn(sheet.transform, "CloseJournal", "×", 34, cardW - 96, cardH - 96, 62, 62, new Color(0.92f, 0.87f, 0.78f), () => {
            if (eraJournalModal) Destroy(eraJournalModal); eraJournalModal = null;
        });
        TInk(xClose, Ink);

        string[] titles = { "十九世纪 · 蒸汽奠基", "二十世纪 · 内燃电气", "二十一世纪 · 高速智行" };
        string[] subStatus = {
            "现行研习 · 已收编 " + techDone.Count + " / " + historyNodes.Count + " 项典籍  ›",
            "待启卷册 · 敬请期待",
            "远景档案 · 敬请期待"
        };
        Color[] accentColors = {
            new Color(1.00f, 0.34f, 0.72f),
            new Color(0.24f, 0.96f, 0.88f),
            new Color(1.00f, 0.52f, 0.26f)
        };

        float itemW = cardW - 92f;
        const float itemH = 266f;
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            bool activeEra = i == 0;
            float cy = cardH - 480f - i * 286f;

            // 外层极黑影院遮幅画框：复刻参考图纯黑上下边衬托中心炽烈彩色霓虹海报
            var vol = Rect("EraVolume_" + i, sheet.transform, 46, cy, itemW, itemH);
            Rounded(vol, new Color(0.015f, 0.015f, 0.025f, 1f));
            SoftShadow(vol);

            // 中间铺满程序化矢量手绘「19Century / 20Century / 21Century」海报贴图（上黑边 12px，下黑边 38px）
            var posterGo = Rect("NeonPoster", vol.transform, 10, 38, itemW - 20f, itemH - 50f);
            var posterRaw = posterGo.AddComponent<RawImage>();
            posterRaw.texture = EraPosterGenerator.GetPoster(i);
            posterRaw.raycastTarget = false;

            // 下黑边安静承接时代中文卷名与当前状态，绝不破坏画面主体霓虹艺术纯文字
            var leftLabel = Txt(vol.transform, titles[i], 19, new Color(0.92f, 0.94f, 0.98f, 0.92f), 20, 4, 420, 32, TextAnchor.MiddleLeft);
            leftLabel.raycastTarget = false;
            var rightLabel = Txt(vol.transform, subStatus[i], 18, accentColors[i], itemW - 440f, 4, 420, 32, TextAnchor.MiddleRight);
            rightLabel.raycastTarget = false;

            var cardTap = vol.AddComponent<Button>();
            cardTap.targetGraphic = vol.GetComponent<Image>();
            cardTap.onClick.AddListener(() => {
                if (activeEra) {
                    if (eraJournalModal) Destroy(eraJournalModal);
                    eraJournalModal = null;
                } else {
                    Toast("「" + titles[idx] + "」典籍尚在编纂整理中");
                }
            });
        }
    }
    static readonly int[] LocoPrices = { 300, 800, 1500, 3200, 6800, 14000, 28000, 65000, 280000 };
    int LocoPrice(int index) { return LocoPrices[Mathf.Clamp(index, 0, LocoPrices.Length - 1)]; }
    void PurchaseLoco(int index)
    {
        if (index < 0 || index >= LocoNames.Length || !techDone.Contains(LocoKey(index))) { Toast("请先解锁对应机车节点"); return; }
        int price = LocoPrice(index);
        if (coins < price) { Toast("金币不足，还差 ¥" + FormatShortfall(price - coins)); return; }
        BeginUndo("购买机车"); coins -= price; researchVehicles.Add(new ResearchVehicle { node = index }); EndUndo(); RefreshHUD(); OpenLocoShop(); Toast("已购买 " + LocoNames[index] + " ×1");
    }
}
