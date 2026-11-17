using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    const string SaveKey = "railway2_local_v1";
    const string BundledTestSaveResource = "TestSaves/railway2_local_v1";
#if UNITY_EDITOR
    const string DevTestSaveSyncPref = "Railway2.DevTestSaveSync";
    const string BundledTestSaveAssetPath = "Assets/Resources/TestSaves/railway2_local_v1.json";
    static bool DevLocalOnlyRun
    {
        get { return UnityEditor.EditorPrefs.GetBool("Railway2.DevLocalOnlyRun", false); }
        set { UnityEditor.EditorPrefs.SetBool("Railway2.DevLocalOnlyRun", value); }
    }
    public static bool DevTestSaveSyncEnabled
    {
        get { return !DevLocalOnlyRun && UnityEditor.EditorPrefs.GetBool(DevTestSaveSyncPref, true); }
        set { DevLocalOnlyRun = false; UnityEditor.EditorPrefs.SetBool(DevTestSaveSyncPref, value); }
    }
    public static bool DevTestSaveDirty;

    static string ReadDevTestSaveFromDisk()
    {
        try
        {
            if (File.Exists(BundledTestSaveAssetPath))
                return File.ReadAllText(BundledTestSaveAssetPath, System.Text.Encoding.UTF8);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Railway2] 读取开发测试存档文件失败：" + e.Message);
        }
        return null;
    }

    static void WriteDevTestSaveToDisk(SaveData data)
    {
        if (data == null) return;
        try
        {
            string prettyJson = JsonUtility.ToJson(data, true) + "\n";
            string dir = Path.GetDirectoryName(BundledTestSaveAssetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(BundledTestSaveAssetPath))
            {
                string prev = File.ReadAllText(BundledTestSaveAssetPath, System.Text.Encoding.UTF8);
                if (prev == prettyJson) return;
            }
            WriteSaveJsonAtomically(BundledTestSaveAssetPath, prettyJson);
            DevTestSaveDirty = true;
            if (!Application.isPlaying) UnityEditor.AssetDatabase.ImportAsset(BundledTestSaveAssetPath);
            else UnityEditor.EditorApplication.delayCall += () => {
                if (!Application.isPlaying && File.Exists(BundledTestSaveAssetPath))
                    UnityEditor.AssetDatabase.ImportAsset(BundledTestSaveAssetPath);
            };
        }
        catch (Exception e)
        {
            Debug.LogError("[Railway2] 写入开发测试存档文件失败：" + e.Message);
        }
    }
#endif
    static void WriteSaveJsonAtomically(string path, string json)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        string temporary = path + ".tmp";
        try
        {
            File.WriteAllText(temporary, json, new System.Text.UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    [Serializable] sealed class SavedStation { public string key; public int level, trafficEvent; public bool built, closed; public float riderBonus; public int facLevel; }
    [Serializable] sealed class SavedScenery { public string key; public bool found, unlocked; }
    [Serializable] sealed class SavedLine { public int id, trains, todayRevenue; public int skin = -1; public string name; public Color color; public List<string> stations; public int[] dispatch; public float fare, todayGross, todayMaintenance, todayTrack; public List<int> recent; }
    [Serializable] sealed class SavedVehicle { public int node, lineId; }
    [Serializable] sealed class SavedFrozenLine { public int lineId; public float[] revenue, demand, served, peak; public bool[] valid; }
    [Serializable] sealed class SaveData {
        public int version=1, day, speed, nextId, dailyBaseline, theme; public double coins; public float diamonds, rp, clock, researchClock;
        public bool macau, guangdong, loan, music; public string region;
        public List<SavedStation> stations = new List<SavedStation>(); public List<SavedScenery> scenery = new List<SavedScenery>();
        public List<SavedLine> lines = new List<SavedLine>(); public List<SavedVehicle> vehicles = new List<SavedVehicle>();
        public List<string> tech = new List<string>(), story = new List<string>(); public List<int> skins = new List<int>();
        public List<int> themes = new List<int>();
        public List<string> adKeys = new List<string>(); public List<int> adCounts = new List<int>();
        public List<int> trialThemes = new List<int>(); public List<long> trialEnds = new List<long>();
        public string companyName = "山海铁道社";
        public int companySeal, companyPolicy, bondDaysLeft, lastPolicyDay = -1, lastAdSubsidyDay = -1, dailyTaskClaimedDay = -1;
        public int bondPrincipal, bondDailyDue, bondsRepaid, consultantId = -1, consultantDaysLeft, economyCycle, nextEconomyCycle = 1, turboDaysLeft, autoDispatchDaysLeft, lastAwardDay;
        public bool bondSubsidized;
        public List<int> boardPartners = new List<int>();
        public List<MunicipalContract> contracts = new List<MunicipalContract>();
        // M1-M3 新增向后兼容字段
        public int storyStage;
        public bool tutorialSkipped;
        public List<string> coTech = new List<string>();
        public List<SavedTalent> talents = new List<SavedTalent>();
        public List<SavedTalent> talentPool = new List<SavedTalent>();
        public int nextTalentId = 1, lastTalentRefreshDay = -1;
        public int lastReadNoticeDay = -1;
        public List<string> memorialPlaques = new List<string>();
        public List<string> memorialTickets = new List<string>();
        public int retiredVehiclesCount;
        public int lastDisplayedSolarTerm = -1, nextContractId = 1;
        public bool hasYesterdayReport;
        public DailyReportSnapshot yesterdayReport;
        public List<SavedFrozenLine> frozenLines = new List<SavedFrozenLine>();
    }
    bool saveReady, saveBlocked; float autosaveTimer;
    string StationSaveKey(Station station) { return station.region + ":" + station.name; }
    string ScenerySaveKey(Scenery scenery) { return scenery.region + ":" + scenery.name; }
    static readonly Dictionary<string, string> LegacyPlaceNames = new Dictionary<string, string> {
        { "青屿", "香埠" },
        { "镜汀", "莲澳" },
        { "锦川", "粤海" },
        { "海晏埠", "石板街" },
        { "鸣钟里", "兵房山" },
        { "铜鼓湾", "鹅颈桥" },
        { "望北咀", "炮台山" },
        { "翠沙咀", "码头咀" },
        { "丹崖口", "鲤鱼门" },
        { "熙市角", "芒角墟" },
        { "龙潭塘", "龙城寨" },
        { "白沙坪", "沥源围" },
        { "青墟镇", "大步墟" },
        { "香兰湾", "浅湾" },
        { "守望门", "青山湾" },
        { "东溪涌", "东涌口" },
        { "云翼空港", "启德坪" },
        { "星梦乐园", "竹篙湾" },
        { "界桥关", "桥头关" },
        { "三屿虹桥", "伶仃桥" },
        { "丰朗墟", "元朗墟" },
        { "云水围", "米埔围" },
        { "绣岭路", "深水埗" },
        { "清溪上", "上水围" },
        { "松烟岭", "粉岭围" },
        { "安和里", "沙头角" },
        { "书院坡", "西贡墟" },
        { "翠围乡", "将军澳" },
        { "青鞍麓", "马鞍山" },
        { "东溟渔港", "塔门澳" },
        { "向阳里", "葵涌" },
        { "碧衫洲", "青衣岛" },
        { "清波湾", "浅水湾" },
        { "望海奇峰", "扯旗山" },
        { "银潮百帆港", "鲤鱼门海角" },
        { "云顶禅刹", "宝莲寺" },
        { "千灯夜市", "榕树头" },
        { "伏狮翠岭", "狮子山" },
        { "松鹤古观", "黄大仙祠" },
        { "芦苇飞鹭苑", "米埔芦滩" },
        { "东溟碧崖", "破边洲" },
        { "古坊石街", "茨林围" },
        { "外海渡口", "外港码头" },
        { "汀屿里", "潭仔" },
        { "连汀湾", "龙环村" },
        { "松风渔村", "荔枝碗" },
        { "青芦洲", "青洲" },
        { "新潮岸", "新口岸" },
        { "海神阁", "阿妈阁" },
        { "钟楼广场", "议事亭" },
        { "潮音苑", "望厦村" },
        { "石角街", "官也街" },
        { "镜汀空港", "北安港" },
        { "青莲渡", "路氹口" },
        { "叠石湾", "石排湾" },
        { "玄沙湾", "黑沙湾" },
        { "西岸工坊区", "拱北口" },
        { "玉琴关", "莲花口" },
        { "古坊石门楼", "大三巴" },
        { "海神望海阁", "妈阁庙" },
        { "青石风味坊", "官也老街" },
        { "玄沙听涛滩", "黑沙滩" },
        { "海角钟楼堂", "东望洋塔" },
        { "百花城", "五羊城" },
        { "百花府", "五羊府" },
        { "鹏湾（西渡）", "新安（西渡）" },
        { "鹏湾府", "新安府" },
        { "鹏湾", "新安城" },
        { "福坪", "福田墟" },
        { "安湾", "宝安墟" },
        { "禅溪府", "祖庙府" },
        { "禅溪", "祖庙镇" },
        { "珠屿（北关）", "香山（北关）" },
        { "珠屿府", "香山府" },
        { "珠屿", "香山湾" },
        { "莞香坡", "石龙镇" },
        { "莞香府", "莞城府" },
        { "鹅湖镇", "西湖墟" },
        { "鹅湖府", "惠州府" },
        { "翠香原", "石岐墟" },
        { "翠香府", "岐江府" },
        { "韶溪镇", "曲江口" },
        { "韶溪府", "曲江府" },
        { "鮀海港", "樟林港" },
        { "鮀海府", "潮海府" },
        { "五津镇", "蓬江墟" },
        { "五津府", "五邑府" },
        { "湛溟港", "赤坎埠" },
        { "湛溟府", "雷州府" },
        { "茂林城", "高凉城" },
        { "茂林府", "高凉府" },
        { "端溪城", "端州墟" },
        { "端溪府", "端州府" },
        { "嘉应驿", "松口镇" },
        { "善浦港", "汕尾港" },
        { "善浦府", "海陆府" },
        { "槎溪城", "东江埠" },
        { "槎溪府", "循州府" },
        { "鼍阳港", "海陵港" },
        { "鼍阳府", "阳江府" },
        { "清溪镇", "清远墟" },
        { "清溪府", "清城府" },
        { "凤岭城", "牌坊街" },
        { "凤岭府", "潮州府" },
        { "榕浦城", "榕城墟" },
        { "榕浦府", "揭阳府" },
        { "石泉城", "罗定墟" },
        { "石泉府", "泷水府" },
        { "云龙岗", "龙岗墟" },
        { "兴龙坪", "坪山围" },
        { "晨曦墟", "南头墟" },
        { "鹅湖坊", "桥东埠" },
        { "沐阳镇", "淡水墟" },
        { "青萝墟", "博罗墟" },
        { "晴波湾", "稔山湾" },
        { "荔风坊", "西关涌" },
        { "翠微里", "东山口" },
        { "海镜洲", "沥滘村" },
        { "银河坪", "沙河墟" },
        { "晴云墟", "江村墟" },
        { "黄木港", "黄埔埠" },
        { "翠禺渡", "石基墟" },
        { "锦花坡", "花山墟" },
        { "银沙湾", "沙湾墟" },
        { "青莲墟", "增城墟" },
        { "古禅坊", "石湾窑" },
        { "南溟墟", "南庄墟" },
        { "顺水坊", "大良墟" },
        { "三川汇", "西南涌" },
        { "香澜洲", "前山墟" },
        { "星斗门", "斗门墟" },
        { "金潮湾", "三灶湾" },
        { "武溪渡", "武江渡" },
        { "明溪里", "浈江埠" },
        { "曲水坪", "马坝墟" },
        { "启明镇", "始兴墟" },
        { "栖霞坡", "仁化墟" },
        { "安昌墟", "乐昌坪" },
        { "南岭驿", "南雄墟" },
        { "青龙湖", "澄海墟" },
        { "镜潮湾", "达濠埠" },
        { "向阳汀", "棉城墟" },
        { "南潮港", "海门湾" },
        { "蓬莱渡", "新会墟" },
        { "滨海湾", "江海埠" },
        { "新津墟", "台城墟" },
        { "望台镇", "水步墟" },
        { "碉楼墟", "赤坎墟" },
        { "白鹤坪", "鹤山墟" },
        { "沐恩温泉", "恩平墟" },
        { "晚霞山", "霞山埠" },
        { "青樟里", "麻章墟" },
        { "翠溪坪", "遂溪墟" },
        { "南角渡", "徐闻渡" },
        { "清廉渡", "廉江墟" },
        { "青雷墟", "雷城墟" },
        { "吴湾口", "吴川埠" },
        { "茂林南", "水东湾" },
        { "白沙角", "电白墟" },
        { "高岭城", "高州墟" },
        { "橘香坡", "化州渡" },
        { "玉都坪", "信宜墟" },
        { "墨砚里", "水坑口" },
        { "青鼎湖", "鼎湖墟" },
        { "高岩镇", "高要渡" },
        { "翠竹乡", "广宁墟" },
        { "竹影坪", "怀集墟" },
        { "四水汇", "四会墟" },
        { "寒梅渡", "梅县墟" },
        { "梅林坪", "蕉岭坪" },
        { "青瓷镇", "大埔墟" },
        { "丰水潭", "丰顺汤" },
        { "五峰山", "五华墟" },
        { "宁川坊", "兴宁围" },
        { "善湾坊", "海丰墟" },
        { "海晏墟", "陆丰墟" },
        { "玄武滩", "碣石湾" },
        { "源水阁", "源城埠" },
        { "紫竹坪", "紫金墟" },
        { "青龙潭", "龙川渡" },
        { "青阳坡", "和平墟" },
        { "千翠湖", "连平塘" },
        { "银溪城", "阳春墟" },
        { "东海滩", "东平埠" },
        { "西海岸", "闸坡湾" },
        { "春山墟", "春湾墟" },
        { "清风阁", "英德墟" },
        { "茶山坪", "茶山墟" },
        { "凤凰坡", "凤凰墟" },
        { "白鹭港", "饶平埠" },
        { "古榕坊", "揭东墟" },
        { "榕东坪", "榕江渡" },
        { "灵泉港", "普宁墟" },
        { "金沙镇", "惠来墟" },
        { "云岫里", "云城墟" },
        { "石帆渡", "南江渡" },
        { "云谷禅乡", "新兴墟" },
        { "翠林墟", "郁南墟" },
        { "泷水城", "泷江埠" },
    };
    static string NormalizePlaceName(string value)
    {
        return value != null && LegacyPlaceNames.TryGetValue(value, out var current) ? current : value;
    }
    static string NormalizePlaceKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        int colon = key.IndexOf(':');
        if (colon < 0) return NormalizePlaceName(key);
        string region = NormalizePlaceName(key.Substring(0, colon));
        string name = NormalizePlaceName(key.Substring(colon + 1));
        // Older saves may predate the gateway suffix added during BuildData.
        if (region == "粤海" && name == "新安城") name = "新安（西渡）";
        if (region == "粤海" && name == "香山湾") name = "香山（北关）";
        return region + ":" + name;
    }
    static void NormalizeSavedPlaces(SaveData data)
    {
        data.region = NormalizePlaceName(data.region);
        if (data.stations != null) foreach (var item in data.stations) if (item != null) item.key = NormalizePlaceKey(item.key);
        if (data.scenery != null) foreach (var item in data.scenery) if (item != null) item.key = NormalizePlaceKey(item.key);
        if (data.lines != null) foreach (var line in data.lines)
            if (line != null && line.stations != null) for (int i = 0; i < line.stations.Count; i++) line.stations[i] = NormalizePlaceKey(line.stations[i]);
        if (data.talents != null) foreach (var item in data.talents) if (item != null) item.assignStationKey = NormalizePlaceKey(item.assignStationKey);
        if (data.talentPool != null) foreach (var item in data.talentPool) if (item != null) item.assignStationKey = NormalizePlaceKey(item.assignStationKey);
    }
    bool RestoreGame()
    {
        string rawJson = null;
#if UNITY_EDITOR
        if (DevTestSaveSyncEnabled)
        {
            string diskJson = ReadDevTestSaveFromDisk();
            if (!string.IsNullOrEmpty(diskJson))
            {
                rawJson = diskJson;

            }
        }
#endif
        if (string.IsNullOrEmpty(rawJson))
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return false;
            rawJson = PlayerPrefs.GetString(SaveKey);
        }
        if (stations == null || stations.Count == 0) BuildData();
        try {
            var data = JsonUtility.FromJson<SaveData>(rawJson);
            if (data == null || data.version != 1 || data.stations == null || data.lines == null || data.scenery == null || data.tech == null || data.vehicles == null || data.story == null || data.skins == null) throw new Exception("invalid save");
            ApplySaveData(data);
            EnsureCompanyInitialized();
            EnsureTalentsInitialized();
            SyncStoryProgression(false);
            SyncCulturalCollection(false);
            lastPolicyDay = data.lastPolicyDay; lastAdSubsidyDay = data.lastAdSubsidyDay; dailyTaskClaimedDay = data.dailyTaskClaimedDay;
            savedRegion=data.region;
            PlayerPrefs.SetString(SaveKey, rawJson);
            PlayerPrefs.Save();
            saveBlocked = false;
            saveReady = true;
            return true;
        } catch (Exception e) { saveBlocked=true; Debug.LogError("本地存档读取失败，已保留原档：" + e.Message); return false; }
    }
    // Data restoration is shared by disk loading and the managed round-trip audit.
    void ApplySaveData(SaveData data)
    {
        NormalizeSavedPlaces(data);
        var byKey = new Dictionary<string,int>(); for (int i=0;i<stations.Count;i++) byKey[StationSaveKey(stations[i])] = i;
        var restoredLines = new List<Line>(); var ids = new HashSet<int>();
        foreach (var record in data.lines) {
            if(record == null || record.stations == null || record.stations.Count < 2 || record.dispatch == null || record.dispatch.Length != 4 || !ids.Add(record.id)) continue;
            var line = new Line { id=record.id, name=record.name, color=record.color, skin=record.skin, trains=record.trains, dispatch=record.dispatch, fare=record.fare, todayRevenue=record.todayRevenue, todayGross=record.todayGross, todayMaintenance=record.todayMaintenance, todayTrack=record.todayTrack, recent=record.recent ?? new List<int>() };
            bool valid = true;
            foreach (string key in record.stations) {
                if(!byKey.TryGetValue(key,out int index)) { valid = false; break; }
                line.st.Add(index);
            }
            if (valid && line.st.Count >= 2) restoredLines.Add(line);
        }
        foreach(var record in data.stations) {
            if (record != null && byKey.TryGetValue(record.key, out int sIdx)) {
                var station=stations[sIdx]; station.level=record.level; station.trafficEvent=record.trafficEvent; station.built=record.built; station.closed=record.closed; station.riderBonus=record.riderBonus; station.facLevel=record.facLevel;
            }
        }
        var sceneryByKey = new Dictionary<string,Scenery>(); foreach(var item in sceneries) sceneryByKey[ScenerySaveKey(item)] = item;
        foreach(var record in data.scenery) if(record != null && sceneryByKey.TryGetValue(record.key,out var item)) { item.found=record.found; item.unlocked=record.unlocked; }
        pendingCheckin.Clear();
        foreach(var sc in sceneries) if(sc.unlocked && !sc.found && TryGetSceneryContent(sc, out _, out _)) pendingCheckin.Add(sc);
        lines.Clear(); lines.AddRange(restoredLines);
        frozenTraffic.Clear();
        if (data.frozenLines != null)
        {
            foreach (var fl in data.frozenLines)
            {
                if (fl == null) continue;
                var line = restoredLines.Find(l => l.id == fl.lineId);
                if (line == null) continue;
                var ft = new FrozenLineTraffic();
                if (fl.revenue != null && fl.revenue.Length == 4) Array.Copy(fl.revenue, ft.revenue, 4);
                if (fl.demand != null && fl.demand.Length == 4) Array.Copy(fl.demand, ft.demand, 4);
                if (fl.served != null && fl.served.Length == 4) Array.Copy(fl.served, ft.served, 4);
                if (fl.peak != null && fl.peak.Length == 4) Array.Copy(fl.peak, ft.peak, 4);
                if (fl.valid != null && fl.valid.Length == 4) Array.Copy(fl.valid, ft.valid, 4);
                frozenTraffic[line] = ft;
            }
        }
        foreach (var l in restoredLines) foreach (int stIdx in l.st) if (stIdx >= 0 && stIdx < stations.Count) stations[stIdx].built = true;
        techDone.Clear(); techDone.AddRange(data.tech);
        researchVehicles.Clear(); foreach(var item in data.vehicles)
            if (item != null && item.node >= 0 && item.node < LocoNames.Length)
                researchVehicles.Add(new ResearchVehicle { node=item.node, lineId=restoredLines.Exists(l => l.id == item.lineId) ? item.lineId : 0 });
        storySeen.Clear(); foreach(var item in data.story) storySeen.Add(item); ownedLineSkins.Clear(); foreach(int item in data.skins) ownedLineSkins.Add(item);
        ownedThemes.Clear(); ownedThemes.Add(0);
        if (data.themes != null) foreach (int item in data.themes) if (item >= 0 && item < Themes.Length) ownedThemes.Add(item);
        adWatchCounts.Clear();
        if (data.adKeys != null && data.adCounts != null)
            for (int i = 0; i < Mathf.Min(data.adKeys.Count, data.adCounts.Count); i++)
                if (!string.IsNullOrEmpty(data.adKeys[i]) && data.adCounts[i] > 0) adWatchCounts[data.adKeys[i]] = data.adCounts[i];
        themeTrialUntil.Clear();
        if (data.trialThemes != null && data.trialEnds != null)
            for (int i = 0; i < Mathf.Min(data.trialThemes.Count, data.trialEnds.Count); i++)
                if (data.trialEnds[i] > 0) themeTrialUntil[data.trialThemes[i]] = data.trialEnds[i];
        currentTheme = Mathf.Clamp(data.theme, 0, Themes.Length - 1);
        if (!IsThemeAvailable(currentTheme)) currentTheme = 0;
        coins=data.coins; diamonds=data.diamonds; rp=data.rp; day=Mathf.Max(1,data.day); dayClock=data.clock; researchClock=data.researchClock; timeSpeed=data.speed;
        nextLineId=data.nextId; dailyStationBaseline=data.dailyBaseline; macauUnlocked=data.macau; guangdongUnlocked=data.guangdong; loanOutstanding=data.loan; musicEnabled=data.music;
        companyName = string.IsNullOrWhiteSpace(data.companyName) ? "山海铁道社" : data.companyName.Trim();
        companySeal = Mathf.Clamp(data.companySeal, 0, CompanySealChars.Length - 1);
        companyPolicy = Mathf.Clamp(data.companyPolicy, 0, CompanyPolicyNames.Length - 1);
        bondDaysLeft = Mathf.Max(0, data.bondDaysLeft);
        bondPrincipal = bondDaysLeft > 0 ? (data.bondPrincipal > 0 ? data.bondPrincipal : BondPrincipal) : 0;
        bondDailyDue = bondDaysLeft > 0 ? (data.bondDailyDue > 0 ? data.bondDailyDue : BondDailyPayment) : 0;
        bondsRepaid = Mathf.Max(0, data.bondsRepaid);
        bondSubsidized = data.bondSubsidized;
        consultantId = data.consultantDaysLeft > 0 ? Mathf.Clamp(data.consultantId, 0, PartnerNames.Length - 1) : -1;
        consultantDaysLeft = Mathf.Max(0, data.consultantDaysLeft);
        economyCycle = Mathf.Clamp(data.economyCycle, 0, EconomyCycleNames.Length - 1);
        nextEconomyCycle = Mathf.Clamp(data.nextEconomyCycle, 0, EconomyCycleNames.Length - 1);
        turboDaysLeft = Mathf.Max(0, data.turboDaysLeft);
        autoDispatchDaysLeft = Mathf.Max(0, data.autoDispatchDaysLeft);
        lastAwardDay = Mathf.Max(0, data.lastAwardDay);
        boardPartners.Clear();
        if (data.boardPartners != null) foreach (int pid in data.boardPartners) if (pid >= 0 && pid < 6 && !boardPartners.Contains(pid)) boardPartners.Add(pid);
        nextContractId = Mathf.Max(1, data.nextContractId);
        activeContracts.Clear();
        if (data.contracts != null)
        {
            foreach (var mc in data.contracts)
                if (mc != null && !mc.claimed) { activeContracts.Add(mc); nextContractId = Mathf.Max(nextContractId, mc.id + 1); }
        }
        companyTechDone.Clear();
        if (data.coTech != null) foreach (string ct in data.coTech) if (!string.IsNullOrEmpty(ct) && !companyTechDone.Contains(ct)) companyTechDone.Add(ct);
        EnsureCompanyTechMigrated();
        talents.Clear();
        if (data.talents != null) foreach (var t in data.talents) if (t != null && !string.IsNullOrEmpty(t.name)) talents.Add(t);
        talentPool.Clear();
        if (data.talentPool != null) foreach (var tp in data.talentPool) if (tp != null && !string.IsNullOrEmpty(tp.name)) talentPool.Add(tp);
        nextTalentId = Mathf.Max(1, data.nextTalentId);
        lastTalentRefreshDay = data.lastTalentRefreshDay;
        lastReadNoticeDay = data.lastReadNoticeDay;
        earnedPlaques.Clear();
        if (data.memorialPlaques != null) foreach (string pk in data.memorialPlaques) if (!string.IsNullOrEmpty(pk) && !earnedPlaques.Contains(pk)) earnedPlaques.Add(pk);
        earnedMemorialTickets.Clear();
        if (data.memorialTickets != null) foreach (string tk in data.memorialTickets) if (!string.IsNullOrEmpty(tk) && !earnedMemorialTickets.Contains(tk)) earnedMemorialTickets.Add(tk);
        retiredVehiclesCount = Mathf.Max(0, data.retiredVehiclesCount);
        storyStage = Mathf.Max(0, data.storyStage);
        tutorialSkipped = data.tutorialSkipped;
        lastDisplayedSolarTerm = data.lastDisplayedSolarTerm;
        hasYesterdayReport = data.hasYesterdayReport && data.yesterdayReport != null;
        yesterdayReport = hasYesterdayReport ? CopyDailyReport(data.yesterdayReport) : null;
        lastPolicyDay = data.lastPolicyDay; lastAdSubsidyDay = data.lastAdSubsidyDay; dailyTaskClaimedDay = data.dailyTaskClaimedDay;
        savedRegion = data.region;
    }
    string savedRegion;
    static DailyReportSnapshot CopyDailyReport(DailyReportSnapshot src)
    {
        if (src == null) return null;
        return new DailyReportSnapshot {
            settledDay = src.settledDay, bottleneckStation = src.bottleneckStation,
            gross = src.gross, commercialRent = src.commercialRent, maintAndTax = src.maintAndTax, netProfit = src.netProfit, passengers = src.passengers,
            bestLineNet = src.bestLineNet, worstLineNet = src.worstLineNet, crowdLost = src.crowdLost, unreachedLost = src.unreachedLost, elasticLost = src.elasticLost,
            bestLineName = src.bestLineName, worstLineName = src.worstLineName, bottleneckNote = src.bottleneckNote
        };
    }
    SaveData CaptureSaveData()
    {
        var data=new SaveData { coins=coins, diamonds=diamonds, rp=rp, day=day, clock=dayClock, researchClock=researchClock, speed=timeSpeed, nextId=nextLineId, dailyBaseline=dailyStationBaseline, macau=macauUnlocked, guangdong=guangdongUnlocked, loan=loanOutstanding, music=musicEnabled, region=currentRegion, theme=(previewRestoreTheme >= 0 ? previewRestoreTheme : currentTheme), companyName=companyName, companySeal=companySeal, companyPolicy=companyPolicy, bondDaysLeft=bondDaysLeft, bondPrincipal=bondPrincipal, bondDailyDue=bondDailyDue, bondsRepaid=bondsRepaid, bondSubsidized=bondSubsidized, consultantId=consultantId, consultantDaysLeft=consultantDaysLeft, economyCycle=economyCycle, nextEconomyCycle=nextEconomyCycle, turboDaysLeft=turboDaysLeft, autoDispatchDaysLeft=autoDispatchDaysLeft, lastAwardDay=lastAwardDay, lastPolicyDay=lastPolicyDay, lastAdSubsidyDay=lastAdSubsidyDay, dailyTaskClaimedDay=dailyTaskClaimedDay, storyStage=storyStage, tutorialSkipped=tutorialSkipped, nextTalentId=nextTalentId, lastTalentRefreshDay=lastTalentRefreshDay, lastReadNoticeDay=lastReadNoticeDay, retiredVehiclesCount=retiredVehiclesCount, lastDisplayedSolarTerm=lastDisplayedSolarTerm, nextContractId=nextContractId, hasYesterdayReport=(hasYesterdayReport && yesterdayReport != null), yesterdayReport=(hasYesterdayReport && yesterdayReport != null ? CopyDailyReport(yesterdayReport) : null) };
        foreach (var kv in frozenTraffic)
        {
            if (kv.Key == null || kv.Value == null) continue;
            data.frozenLines.Add(new SavedFrozenLine {
                lineId = kv.Key.id,
                revenue = (float[])kv.Value.revenue.Clone(),
                demand = (float[])kv.Value.demand.Clone(),
                served = (float[])kv.Value.served.Clone(),
                peak = (float[])kv.Value.peak.Clone(),
                valid = (bool[])kv.Value.valid.Clone()
            });
        }
        data.memorialPlaques.AddRange(earnedPlaques);
        data.memorialTickets.AddRange(earnedMemorialTickets);
        data.boardPartners.AddRange(boardPartners);
        data.contracts.AddRange(activeContracts);
        data.coTech.AddRange(companyTechDone);
        data.talents.AddRange(talents);
        data.talentPool.AddRange(talentPool);
        foreach(var s in stations) data.stations.Add(new SavedStation { key=StationSaveKey(s), level=s.level, trafficEvent=s.trafficEvent, built=s.built, closed=s.closed, riderBonus=s.riderBonus, facLevel=s.facLevel });
        foreach(var s in sceneries) data.scenery.Add(new SavedScenery { key=ScenerySaveKey(s), found=s.found, unlocked=s.unlocked });
        foreach(var l in lines) { var record=new SavedLine { id=l.id, name=l.name, color=l.color, skin=l.skin, trains=l.trains, dispatch=(int[])l.dispatch.Clone(), fare=l.fare, todayGross=l.todayGross, todayMaintenance=l.todayMaintenance, todayTrack=l.todayTrack, todayRevenue=l.todayRevenue, recent=new List<int>(l.recent), stations=new List<string>() }; foreach(int i in l.st) record.stations.Add(StationSaveKey(stations[i])); data.lines.Add(record); }
        foreach(var v in researchVehicles) data.vehicles.Add(new SavedVehicle { node=v.node, lineId=v.lineId });
        data.tech.AddRange(techDone); data.story.AddRange(storySeen); data.skins.AddRange(ownedLineSkins);
        data.themes.AddRange(ownedThemes);
        foreach (var kv in adWatchCounts) { data.adKeys.Add(kv.Key); data.adCounts.Add(kv.Value); }
        foreach (var kv in themeTrialUntil) { data.trialThemes.Add(kv.Key); data.trialEnds.Add(kv.Value); }
        return data;
    }
    void SaveGame()
    {
        if(!saveReady || saveBlocked) return;
        try {
            var data = CaptureSaveData();
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data)); PlayerPrefs.Save();
#if UNITY_EDITOR
            if (DevTestSaveSyncEnabled) WriteDevTestSaveToDisk(data);
#endif
        } catch(Exception e) { Debug.LogError("本地存档保存失败：" + e.Message); }
    }
    void TickAutosave() { autosaveTimer += Time.unscaledDeltaTime; if (autosaveTimer >= 5f) { autosaveTimer=0; SaveGame(); } }
    string ReadBundledTestSaveRaw()
    {
#if UNITY_EDITOR
        string diskJson = ReadDevTestSaveFromDisk();
        if (!string.IsNullOrEmpty(diskJson)) return diskJson;
#endif
        var asset = Resources.Load<TextAsset>(BundledTestSaveResource);
        return asset ? asset.text : null;
    }
    bool HasBundledTestSave() { return !string.IsNullOrEmpty(ReadBundledTestSaveRaw()); }
    string DescribeBundledTestSave()
    {
        string raw = ReadBundledTestSaveRaw();
        if (string.IsNullOrEmpty(raw)) return "预置测试存档";
        try {
            var check = JsonUtility.FromJson<SaveData>(raw);
            if (check != null && check.version == 1)
            {
                string reg = string.IsNullOrEmpty(check.region) ? "香埠" : DisplayPlaceAliases(check.region);
                int lc = check.lines != null ? check.lines.Count : 0;
                int tc = check.tech != null ? check.tech.Count : 0;
                return "第 " + Mathf.Max(1, check.day) + " 日" + reg + "测试存档（" + lc + " 条线路 · " + tc + " 项科技 · " + FormatCoins(check.coins) + " 资金）";
            }
        } catch {}
        return "预置测试存档";
    }
    void ResetLoadedSessionState()
    {
        techCollapseDone = true; techSessionUnlocked.Clear(); historyJustUnlocked = null;
        coTechCollapseDone = true; coTechSessionUnlocked.Clear(); coTechJustUnlocked = null;
        researchProfitEstimates.Clear(); hasResearchProfitSignature = false;
        hasYesterdayReport = false; yesterdayReport = null;
        estimatingResearchProfit = false; hasForecastSignature = false;
        frozenTraffic.Clear(); lineForecasts.Clear(); operatingRates.Clear();
        pendingCheckin.Clear(); storyQueue.Clear();
        scrollPositions.Clear(); scrollViews.Clear();
        // Keep generations monotonic so an old same-frame callback cannot match a new binding.
        foreach (var key in new List<string>(scrollBindingVersions.Keys)) NextScrollBinding(key);
        lineFleetBackAction = null; lineFleetOriginFleetBack = null; lineFleetShopReturn = null;
        lineFleetLineId = 0; lineFleetSlot = -1; lineFleetAmount = 0;
        nextContractId = 1; hasYesterdayReport = false; yesterdayReport = null;
        autosaveTimer = 0f;
    }
    bool LoadBundledTestSave()
    {
        string raw = ReadBundledTestSaveRaw();
        if (string.IsNullOrEmpty(raw)) { Toast("未找到内置测试存档"); return false; }
        try {
            var check = JsonUtility.FromJson<SaveData>(raw);
            if (check == null || check.version != 1 || check.lines == null || check.stations == null)
            {
                Toast("内置测试存档格式校验失败");
                return false;
            }
        } catch (Exception e) {
            Toast("内置测试存档解析失败：" + e.Message);
            return false;
        }

        saveReady = false;
        ClosePanel(true); CloseLineBubble(); ClosePage(); CancelDraft(); ClearFloaters();
        EndThemePreview(false);
        ClearUndoHistory();
        ResetLoadedSessionState();
        if (storyCard) { Destroy(storyCard); storyCard = null; }
        storyCheckTimer = 0f;
#if UNITY_EDITOR
        // Loading the shared test run explicitly resumes shared test-save synchronization.
        DevTestSaveSyncEnabled = true;
#endif
        lines.Clear();
        BuildData();
        PlayerPrefs.SetString(SaveKey, raw);
        PlayerPrefs.Save();
        saveBlocked = false;
        saveReady = false;
        if (!RestoreGame()) { Toast("测试存档载入失败"); return false; }

        EnsureCompanyInitialized();
        if (!IsThemeAvailable(currentTheme)) currentTheme = 0;
        string dest = (!string.IsNullOrEmpty(savedRegion) && RegionUnlocked(savedRegion)) ? savedRegion : "香埠";
        FreezeRegionTraffic();
        currentRegion = dest;
        if (hkMapArt) hkMapArt.SetActive(currentRegion == "香埠");
        if (macauMapArt) macauMapArt.SetActive(currentRegion == "莲澳");
        if (guangdongMapArt) guangdongMapArt.SetActive(currentRegion == "粤海");
        if (mapRect)
        {
            mapRect.localScale = Vector3.one;
            mapRect.anchoredPosition = mapWheelZoom ? mapWheelZoom.CenterPosition(1f) : Vector2.zero;
        }
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
        if (regionTitle) regionTitle.text = DisplayPlaceAliases(currentRegion == "香埠" ? "香 埠" : currentRegion == "莲澳" ? "莲 澳" : "粤 海");
        RefreshSceneryViews();
        UpdateStationLabelSizes(1f);
        LayoutStationLabels();
        ApplyTheme(currentTheme, false);
        CreateLineVisuals();
        RefreshAllLines();
        RefreshStationDots();
        CheckSceneryOnLine(null);
        RecalcIncome();
        UpdateRegionalMarkers(true);
        UpdateTimeText();
        RefreshHUD();
        RefreshPendingBadge();
        RefreshNoticeUnreadDot();
        RefreshUndoButton();
        SyncStoryProgression(false);
        UpdateStoryObjectiveBanner();
        SyncRegionalMusic(true);
        saveReady = true;
        SaveGame();
        Toast("已载入测试存档：第 " + day + " 日 · " + DisplayPlaceAliases(currentRegion));
        return true;
    }
    void OnApplicationPause(bool paused) { if(paused) SaveGame(); }
    void OnApplicationFocus(bool focused)
    {
        if (!focused) { if (lineInsertActive) CancelLineInsert(); SaveGame(); }
        else ApplyFrameRate();
    }
    void OnApplicationQuit() { SaveGame(); }
    void OnDisable() { SaveGame(); }
}


