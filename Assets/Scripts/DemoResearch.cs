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
    Text historyInfo, historyHeading;
    GameObject historyInfoPanel, historyControls;
    readonly HashSet<string> historyAnimated = new HashSet<string>();
    readonly Dictionary<string, Vector2> historyPositions = new Dictionary<string, Vector2>();
    readonly Dictionary<string, RectTransform> historyNodeRects = new Dictionary<string, RectTransform>();
    float historyZoom = 1f;
    HistoryTooltipFollow historyTooltipFollow;
    string historyJustUnlocked;
    GameObject historyPage;
    Sprite historyCircleOutline;
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
        switch(node.key) {
            case "dispatch_basic": return 250; case "dispatch_peak": return 600; case "fare_policy": return 500;
            case "network_expand1": return 1000; case "network_expand2": return 3000;
        }
        if (node.loco == 0) return 300;
        float price = 100f * Mathf.Pow(1.35f, node.depth) * (node.loco >= 0 ? 1.8f : 1f);
        return Mathf.Max(100, Mathf.RoundToInt(price / 10f) * 10);
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
        historyInfoPanel.SetActive(true);
        if (historyTooltipFollow && historyNodeRects.TryGetValue(key, out var target)) historyTooltipFollow.target = target;
        historyHeading.text = node.name;
        string effect = HistoryEffectText(node);
        string missing = "";
        foreach (var parent in node.parents) if (!techDone.Contains(parent)) missing += (missing.Length == 0 ? "" : "、") + historyByKey[parent].name;
        string state = techDone.Contains(node.key) ? "已解锁" : missing.Length > 0 ? "尚需：" + missing : "点击解锁";
        string estimate = "";
        if (!techDone.Contains(key) && node.loco < 0 && node.effect != ResearchEffect.None) {
            float delta = EstimateResearchProfit(node);
            estimate = "\n当前线网预计日利润 " + (delta >= 0 ? "+" : "") + delta.ToString("0.0") + " 金币（按当前运营估算）";
        }
        historyInfo.text = "年代：" + HistoryPeriod(node) + " · 研究费用 ¥" + HistoryCost(node) + "\n" + node.description + "\n" + effect + " · " + state + estimate;
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
        int cost=HistoryCost(node);
        if(coins<cost) { Toast("金币不足，研究需要 ¥" + cost + "，还差 ¥" + Mathf.CeilToInt(cost-coins)); return; }
        historyAnimated.Clear();
        foreach (var candidate in historyNodes) if (HistoryVisible(candidate)) historyAnimated.Add(candidate.key);
        BeginUndo("解锁科技");
        coins -= cost; techDone.Add(key);
        if (node.loco >= 0) {
            researchVehicles.Add(new ResearchVehicle { node = node.loco });
            if(node.loco==0 || node.loco==4 || node.loco==8) { diamonds+=50; Toast("机车里程碑：钻石 +50"); }
        }
        historyJustUnlocked = key;
        Haptic.Vibrate(20);
        RecalcIncome(); EndUndo(); RefreshHUD(); OpenTechScreen();
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
    void OpenTechScreen(int unused = 0)
    {
        InitHistoryTree();
        ClosePage(); pageBack = null; CloseLineBubble();
        var page = Rect("HistoryFullScreen", canvas.transform, 0, 0, 0, 0);
        var pageRt = page.GetComponent<RectTransform>(); pageRt.anchorMin = Vector2.zero; pageRt.anchorMax = Vector2.one; pageRt.offsetMin = pageRt.offsetMax = Vector2.zero;
        Img(page, new Color(0.99f, 0.985f, 0.97f)).raycastTarget = false;
        pageRoot = page; historyPage = page;
        var view = Rect("HistoryViewport", page.transform, 0, 0, 0, 0);
        var viewRt = view.GetComponent<RectTransform>(); viewRt.anchorMin = Vector2.zero; viewRt.anchorMax = Vector2.one;
        viewRt.offsetMin = Vector2.zero; viewRt.offsetMax = new Vector2(0, -160);
        Img(view, new Color(0.99f, 0.985f, 0.97f));
        var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = true;
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
        if (!historyCircleOutline) historyCircleOutline = MakeRing(512, 2f);
        string[] eras = { "1800年代", "1810年代", "1820年代", "1830—1849", "1850—1869", "1870—1899" };
        for (int i = 5; i >= 0; i--)
        {
            float radius = 270 + i * 230;
            var circle = Rect("EraCircle" + i, content.transform, 4000 - radius, 4000 - radius, radius * 2, radius * 2);
            Img(circle, new Color(0.62f, 0.51f, 0.39f, 0.18f), historyCircleOutline).raycastTarget = false;
            Txt(content.transform, eras[i], 25, new Color(0.49f,0.42f,0.34f,0.85f), 4010, 4000 + radius + 12, 260, 36, TextAnchor.MiddleLeft);
        }
        var edgeGo = Rect("HistoryEdges", content.transform, 0, 0, cw, ch);
        var edges = edgeGo.AddComponent<HistoryGraphEdges>(); edges.lineSprite = spriteWhite;
        var layout = content.AddComponent<HistoryGraphLayout>();
        layout.positions = historyPositions; layout.edges = edges;
        layout.updatesPerSecond = qualityTier == 0 ? 15f : qualityTier == 1 ? 30f : 60f;
        var graphRects = new Dictionary<string, RectTransform>();
        historyNodeRects.Clear();
        foreach (var node in historyNodes)
        {
            if (!HistoryVisible(node)) continue;
            Vector2 point = HistoryPosition(node.key);
            bool done = techDone.Contains(node.key), ready = HistoryAvailable(node);
            var card = Rect(node.key, content.transform, 4000 + point.x - 100, 4000 + point.y - 60, 200, 120);
            var cardRect = card.GetComponent<RectTransform>(); graphRects[node.key] = cardRect; historyNodeRects[node.key] = cardRect;
            layout.Add(node.key, cardRect, node.key == "pressure" ? -1 : HistoryEra(node.key), node.parents);
            Color categoryColor = HistoryEffectColor(node);
            Rounded(card, done ? Color.Lerp(categoryColor, Color.white, 0.60f) : Color.white);
            if (!done) {
                var outline = Rect("UnresearchedOutline", card.transform, 0, 0, 200, 120);
                var outlineRt = outline.GetComponent<RectTransform>(); outlineRt.anchorMin = Vector2.zero; outlineRt.anchorMax = Vector2.one;
                outlineRt.offsetMin = outlineRt.offsetMax = Vector2.zero;
                var dashed = outline.AddComponent<HistoryNodeDashedOutline>(); dashed.color = categoryColor; dashed.raycastTarget = false;
            }
            var marker = Rect("Category", card.transform, 8, 90, 17, 17);
            Img(marker, categoryColor, spriteCircle).raycastTarget = false;
            var name = Txt(card.transform, node.name, 24, Ink, 6, node.loco >= 0 ? 83 : 62, 188, node.loco >= 0 ? 32 : 48, TextAnchor.MiddleCenter);
            name.resizeTextForBestFit = true; name.resizeTextMinSize = 17; name.resizeTextMaxSize = 24;
            if (node.loco >= 0)
            {
                var art = Rect("Locomotive", card.transform, 12, 36, 176, 46).AddComponent<RawImage>();
                art.texture = Resources.Load<Texture2D>("Research/Train/" + LocoArt[node.loco]); art.raycastTarget = false;
                if (art.texture)
                {
                    float ratio = (float)art.texture.width / art.texture.height;
                    float iw = Mathf.Min(176, 46 * ratio), ih = iw / ratio;
                    art.rectTransform.sizeDelta = new Vector2(iw, ih); art.rectTransform.anchoredPosition = new Vector2((200 - iw) / 2, 36 + (46 - ih) / 2);
                }
            }
            string label = node.loco >= 0 ? "机车" : node.effect == ResearchEffect.None ? "运营解锁" : HistoryEffectText(node).Replace(" 基础值（同类收益递减）", "");
            var labelText = Txt(card.transform, label + (done ? " · 已解锁" : ready ? " · 可解锁" : " · 前置不足"), 17, InkSoft, 6, 20, 188, 30, TextAnchor.MiddleCenter);
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
            foreach (string parent in node.parents)
                if (graphRects.TryGetValue(parent, out var parentRect))
                    edges.Add(parentRect, child, techDone.Contains(parent));
        }
        edges.Refresh();
        if (qualityTier >= 2 && historyJustUnlocked != null) { var shake = page.AddComponent<HistoryCameraShake>(); shake.target = rt; }
        historyJustUnlocked = null;
        historyControls = Rect("HistoryControls", page.transform, 0, 0, 0, 0);
        var controlsRt = historyControls.GetComponent<RectTransform>(); controlsRt.anchorMin = Vector2.zero; controlsRt.anchorMax = Vector2.one; controlsRt.offsetMin = controlsRt.offsetMax = Vector2.zero;
        var back = Btn(historyControls.transform, "Back", "←", 40, 24, 0, 94, 94, new Color(1,1,1,0.94f), GoBackPage); TInk(back, Ink);
        var backRt = back.GetComponent<RectTransform>(); backRt.anchorMin = backRt.anchorMax = new Vector2(0,1); backRt.pivot = new Vector2(0,1); backRt.anchoredPosition = new Vector2(24,-28);
        var home = Btn(historyControls.transform, "Home", "主页", 24, 0, 0, 104, 94, new Color(1,1,1,0.94f), GoHome); TInk(home, Ink);
        var homeRt = home.GetComponent<RectTransform>(); homeRt.anchorMin = homeRt.anchorMax = new Vector2(0,1); homeRt.pivot = new Vector2(0,1); homeRt.anchoredPosition = new Vector2(134,-28);
        var title = Txt(historyControls.transform, "科技树", 44, Ink, 0, 0, 320, 60, TextAnchor.MiddleLeft);
        var titleRt = title.rectTransform; titleRt.anchorMin = titleRt.anchorMax = new Vector2(0,1); titleRt.pivot = new Vector2(0,1); titleRt.anchoredPosition = new Vector2(254,-45);
        backRt.anchorMin = backRt.anchorMax = Vector2.zero;
        homeRt.anchorMin = homeRt.anchorMax = Vector2.zero;
        titleRt.anchorMin = titleRt.anchorMax = Vector2.zero;
        var headerLayout = historyControls.AddComponent<PageHeaderLayout>();
        headerLayout.page = controlsRt; headerLayout.back = backRt; headerLayout.home = homeRt; headerLayout.title = titleRt;
        var progress = Txt(historyControls.transform, "研究进度  " + techDone.Count + " / " + historyNodes.Count, 24, InkSoft, 0, 0, 330, 50, TextAnchor.MiddleRight);
        var prt = progress.rectTransform; prt.anchorMin = prt.anchorMax = new Vector2(0,1); prt.pivot = new Vector2(0,1); prt.anchoredPosition = new Vector2(24,-140); progress.alignment = TextAnchor.MiddleLeft;
        var legend = Rect("ResearchLegend", historyControls.transform, 0, 0, 1032, 104);
        var legendRt = legend.GetComponent<RectTransform>(); legendRt.anchorMin = legendRt.anchorMax = new Vector2(0.5f, 0); legendRt.pivot = new Vector2(0.5f, 0); legendRt.anchoredPosition = new Vector2(0, 20);
        Rounded(legend, new Color(1f, 0.99f, 0.96f, 0.94f));
        string[] legendNames = { "收入", "客流", "运力", "速度", "站点吞吐", "车辆维护", "线路维护", "换乘", "机车", "运营解锁" };
        ResearchEffect[] legendKinds = { ResearchEffect.Revenue, ResearchEffect.Demand, ResearchEffect.Capacity, ResearchEffect.Speed, ResearchEffect.Station, ResearchEffect.Vehicle, ResearchEffect.Track, ResearchEffect.Transfer, ResearchEffect.None, ResearchEffect.None };
        for (int i = 0; i < legendNames.Length; i++) {
            float x = 16 + (i % 5) * 204, y = i < 5 ? 62 : 18;
            var sample = new HistoryNode { effect = legendKinds[i], loco = i == 8 ? 0 : -1 };
            var dot = Rect("LegendDot" + i, legend.transform, x, y + 6, 18, 18); Img(dot, HistoryEffectColor(sample), spriteCircle).raycastTarget = false;
            Txt(legend.transform, legendNames[i], 22, InkSoft, x + 24, y, 174, 32, TextAnchor.MiddleLeft);
        }
        var info = Rect("HistoryInfo", page.transform, 0, 0, 900, 310); Rounded(info, new Color(0.96f,0.93f,0.87f,0.98f)); SoftShadow(info);
        var irt = info.GetComponent<RectTransform>(); irt.anchorMin = irt.anchorMax = new Vector2(0.5f,0.5f); irt.pivot = new Vector2(0.5f,0); irt.anchoredPosition = Vector2.zero;
        historyTooltipFollow = info.AddComponent<HistoryTooltipFollow>(); historyTooltipFollow.page = pageRt;
        historyInfoPanel = info;
        historyHeading = Txt(info.transform, "", 34, Ink, 28, 250, 844, 45, TextAnchor.MiddleLeft);
        historyInfo = Txt(info.transform, "", 25, InkSoft, 28, 24, 844, 210, TextAnchor.UpperLeft);
        historyInfoPanel.SetActive(false);
    }
    int LocoPrice(int index) { return 500 + index * 350; }
    void PurchaseLoco(int index)
    {
        if (index < 0 || index >= LocoNames.Length || !techDone.Contains(LocoKey(index))) { Toast("请先解锁对应机车节点"); return; }
        int price = LocoPrice(index);
        if (coins < price) { Toast("金币不足，还差 ¥" + Mathf.CeilToInt(price - coins)); return; }
        BeginUndo("购买机车"); coins -= price; researchVehicles.Add(new ResearchVehicle { node = index }); EndUndo(); RefreshHUD(); OpenLocoShop(); Toast("已购买 " + LocoNames[index] + " ×1");
    }
}
