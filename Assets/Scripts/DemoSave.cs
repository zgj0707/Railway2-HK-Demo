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
    [Serializable] sealed class SavedStation { public string key, customName; public int level, trafficEvent; public bool built, closed; public float riderBonus; public int facLevel; }
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
        { "青屿", "汐港" },
        { "镜汀", "镜屿" },
        { "锦川", "榕川" },
        { "海晏埠", "海晏台" },
        { "鸣钟里", "鸣钟街" },
        { "铜鼓湾", "铜环湾" },
        { "望北咀", "望海咀" },
        { "翠沙咀", "翠竹咀" },
        { "丹崖口", "丹石口" },
        { "熙市角", "旺市角" },
        { "龙潭塘", "龙尾塘" },
        { "白沙坪", "白沙围" },
        { "青墟镇", "南步墟" },
        { "香兰湾", "兰溪湾" },
        { "守望门", "镇海门" },
        { "东溪涌", "东山涌" },
        { "云翼空港", "翔翼坪" },
        { "星梦乐园", "幻彩湾" },
        { "界桥关", "北桥关" },
        { "三屿虹桥", "跨海虹桥" },
        { "丰朗墟", "丰年墟" },
        { "云水围", "芦花围" },
        { "绣岭路", "织布埗" },
        { "清溪上", "清泉围" },
        { "松烟岭", "松岭围" },
        { "安和里", "和安里" },
        { "书院坡", "石梯坡" },
        { "翠围乡", "翠林澳" },
        { "青鞍麓", "鞍马山" },
        { "东溟渔港", "渔光澳" },
        { "向阳里", "向阳涌" },
        { "碧衫洲", "青衫洲" },
        { "清波湾", "清波湾" },
        { "望海奇峰", "望海峰" },
        { "银潮百帆港", "帆影港" },
        { "云顶禅刹", "云钟古阁" },
        { "千灯夜市", "灯笼夜市" },
        { "伏狮翠岭", "卧狮岩" },
        { "松鹤古观", "白鹤古观" },
        { "芦苇飞鹭苑", "飞鹭芦滩" },
        { "东溟碧崖", "碧海石崖" },
        { "古坊石街", "古石围" },
        { "外海渡口", "东帆码头" },
        { "汀屿里", "莲心仔" },
        { "连汀湾", "翠环村" },
        { "松风渔村", "木船厂" },
        { "青芦洲", "芦苇洲" },
        { "新潮岸", "新海堤" },
        { "海神阁", "望海阁" },
        { "钟楼广场", "钟楼市集" },
        { "潮音苑", "白墙巷" },
        { "石角街", "杏仁街" },
        { "镜汀空港", "莲东空港" },
        { "青莲渡", "双莲渡" },
        { "叠石湾", "叠石澳" },
        { "玄沙湾", "墨沙湾" },
        { "西岸工坊区", "西堤关" },
        { "玉琴关", "琴海口" },
        { "古坊石门楼", "石门古坊" },
        { "海神望海阁", "望海灯楼" },
        { "青石风味坊", "饼家老街" },
        { "玄沙听涛滩", "墨沙海滩" },
        { "海角钟楼堂", "白塔山" },
        { "百花城", "南羊城" },
        { "百花府", "南羊府" },
        { "鹏湾（西渡）", "东湾（西渡）" },
        { "鹏湾府", "东湾府" },
        { "鹏湾", "东湾城" },
        { "福坪", "稻田墟" },
        { "安湾", "吉安墟" },
        { "禅溪府", "明珠府" },
        { "禅溪", "明珠镇" },
        { "珠屿（北关）", "青藤（北关）" },
        { "珠屿府", "青藤府" },
        { "珠屿", "青藤湾" },
        { "莞香坡", "百川镇" },
        { "莞香府", "百川府" },
        { "鹅湖镇", "鹅山墟" },
        { "鹅湖府", "鹅山府" },
        { "翠香原", "锦溪墟" },
        { "翠香府", "锦溪府" },
        { "韶溪镇", "五岭口" },
        { "韶溪府", "五岭府" },
        { "鮀海港", "潮风港" },
        { "鮀海府", "潮风府" },
        { "五津镇", "银江墟" },
        { "五津府", "银江府" },
        { "湛溟港", "雷海埠" },
        { "湛溟府", "雷海府" },
        { "茂林城", "荔枝城" },
        { "茂林府", "荔枝府" },
        { "端溪城", "砚山墟" },
        { "端溪府", "砚山府" },
        { "嘉应驿", "寒香镇" },
        { "善浦港", "白浪港" },
        { "善浦府", "白浪府" },
        { "槎溪城", "东溪埠" },
        { "槎溪府", "东溪府" },
        { "鼍阳港", "银滩港" },
        { "鼍阳府", "银滩府" },
        { "清溪镇", "北山墟" },
        { "清溪府", "北山府" },
        { "凤岭城", "金凤城" },
        { "凤岭府", "金凤府" },
        { "榕浦城", "榕荫墟" },
        { "榕浦府", "榕荫府" },
        { "石泉城", "石鼓墟" },
        { "石泉府", "石鼓府" },
        { "云龙岗", "青岗墟" },
        { "兴龙坪", "平山围" },
        { "晨曦墟", "南渡墟" },
        { "鹅湖坊", "桥头埠" },
        { "沐阳镇", "甘水墟" },
        { "青萝墟", "青萝墟" },
        { "晴波湾", "稻香湾" },
        { "荔风坊", "西水涌" },
        { "翠微里", "东坡口" },
        { "海镜洲", "芦湾村" },
        { "银河坪", "浅沙墟" },
        { "晴云墟", "江边墟" },
        { "黄木港", "黄沙埠" },
        { "翠禺渡", "石阶墟" },
        { "锦花坡", "茶花墟" },
        { "银沙湾", "细沙湾" },
        { "青莲墟", "丰穗墟" },
        { "古禅坊", "陶窑坊" },
        { "南溟墟", "南村墟" },
        { "顺水坊", "良田墟" },
        { "三川汇", "西桥涌" },
        { "香澜洲", "前坡墟" },
        { "星斗门", "渔斗门" },
        { "金潮湾", "三石湾" },
        { "武溪渡", "双溪渡" },
        { "明溪里", "清溪埠" },
        { "曲水坪", "马鞍墟" },
        { "启明镇", "兴隆墟" },
        { "栖霞坡", "和顺墟" },
        { "安昌墟", "安乐坪" },
        { "南岭驿", "南坡墟" },
        { "青龙湖", "碧海墟" },
        { "镜潮湾", "浪花埠" },
        { "向阳汀", "木棉墟" },
        { "南潮港", "观海门" },
        { "蓬莱渡", "葵乡墟" },
        { "滨海湾", "平川埠" },
        { "新津墟", "望台墟" },
        { "望台镇", "踏水墟" },
        { "碉楼墟", "红土墟" },
        { "白鹤坪", "白鹤墟" },
        { "沐恩温泉", "泉水墟" },
        { "晚霞山", "晚霞埠" },
        { "青樟里", "麻石墟" },
        { "翠溪坪", "甘溪墟" },
        { "南角渡", "海角渡" },
        { "清廉渡", "清泉墟" },
        { "青雷墟", "响雷墟" },
        { "吴湾口", "渔帆埠" },
        { "茂林南", "东水湾" },
        { "白沙角", "白沙墟" },
        { "高岭城", "高坡墟" },
        { "橘香坡", "橘香渡" },
        { "玉都坪", "竹山墟" },
        { "墨砚里", "石坑口" },
        { "青鼎湖", "青鼎墟" },
        { "高岩镇", "石牌渡" },
        { "翠竹乡", "翠竹墟" },
        { "竹影坪", "山谷墟" },
        { "四水汇", "四水墟" },
        { "寒梅渡", "寒梅墟" },
        { "梅林坪", "芭蕉坪" },
        { "青瓷镇", "青瓷墟" },
        { "丰水潭", "温泉汤" },
        { "五峰山", "五峰墟" },
        { "宁川坊", "宁川围" },
        { "善湾坊", "丰海墟" },
        { "海晏墟", "丰陆墟" },
        { "玄武滩", "玄石湾" },
        { "源水阁", "水源埠" },
        { "紫竹坪", "紫竹墟" },
        { "青龙潭", "青龙渡" },
        { "青阳坡", "太和墟" },
        { "千翠湖", "连翠塘" },
        { "银溪城", "春山墟" },
        { "东海滩", "东海埠" },
        { "西海岸", "银浪湾" },
        { "春山墟", "春风墟" },
        { "清风阁", "茶香墟" },
        { "茶山坪", "采茶墟" },
        { "凤凰坡", "金羽墟" },
        { "白鹭港", "白鹭埠" },
        { "古榕坊", "古榕东" },
        { "榕东坪", "榕荫渡" },
        { "灵泉港", "青梅墟" },
        { "金沙镇", "灵泉墟" },
        { "云岫里", "云岫墟" },
        { "石帆渡", "石帆渡" },
        { "云谷禅乡", "禅林墟" },
        { "翠林墟", "翠林墟" },
        { "泷水城", "泷水埠" },
        { "石板街", "海晏台" },
        { "兵房山", "鸣钟街" },
        { "鹅颈桥", "铜环湾" },
        { "炮台山", "望海咀" },
        { "码头咀", "翠竹咀" },
        { "鲤鱼门", "丹石口" },
        { "芒角墟", "旺市角" },
        { "龙城寨", "龙尾塘" },
        { "沥源围", "白沙围" },
        { "大步墟", "南步墟" },
        { "浅湾", "兰溪湾" },
        { "青山湾", "镇海门" },
        { "东涌口", "东山涌" },
        { "启德坪", "翔翼坪" },
        { "竹篙湾", "幻彩湾" },
        { "桥头关", "北桥关" },
        { "伶仃桥", "跨海虹桥" },
        { "元朗墟", "丰年墟" },
        { "米埔围", "芦花围" },
        { "深水埗", "织布埗" },
        { "上水围", "清泉围" },
        { "粉岭围", "松岭围" },
        { "沙头角", "和安里" },
        { "西贡墟", "石梯坡" },
        { "将军澳", "翠林澳" },
        { "马鞍山", "鞍马山" },
        { "塔门澳", "渔光澳" },
        { "葵涌", "向阳涌" },
        { "青衣岛", "青衫洲" },
        { "浅水湾", "清波湾" },
        { "扯旗山", "望海峰" },
        { "鲤鱼门海角", "帆影港" },
        { "宝莲寺", "云钟古阁" },
        { "榕树头", "灯笼夜市" },
        { "狮子山", "卧狮岩" },
        { "黄大仙祠", "白鹤古观" },
        { "米埔芦滩", "飞鹭芦滩" },
        { "破边洲", "碧海石崖" },
        { "茨林围", "古石围" },
        { "外港码头", "东帆码头" },
        { "潭仔", "莲心仔" },
        { "龙环村", "翠环村" },
        { "荔枝碗", "木船厂" },
        { "青洲", "芦苇洲" },
        { "新口岸", "新海堤" },
        { "阿妈阁", "望海阁" },
        { "议事亭", "钟楼市集" },
        { "望厦村", "白墙巷" },
        { "官也街", "杏仁街" },
        { "北安港", "莲东空港" },
        { "路氹口", "双莲渡" },
        { "石排湾", "叠石澳" },
        { "黑沙湾", "墨沙湾" },
        { "拱北口", "西堤关" },
        { "莲花口", "琴海口" },
        { "大三巴", "石门古坊" },
        { "妈阁庙", "望海灯楼" },
        { "官也老街", "饼家老街" },
        { "黑沙滩", "墨沙海滩" },
        { "东望洋塔", "白塔山" },
        { "五羊府", "南羊府" },
        { "新安府", "东湾府" },
        { "莞城府", "百川府" },
        { "惠州府", "鹅山府" },
        { "祖庙府", "明珠府" },
        { "香山府", "青藤府" },
        { "岐江府", "锦溪府" },
        { "曲江府", "五岭府" },
        { "潮海府", "潮风府" },
        { "五邑府", "银江府" },
        { "雷州府", "雷海府" },
        { "高凉府", "荔枝府" },
        { "端州府", "砚山府" },
        { "嘉应府", "寒香府" },
        { "海陆府", "白浪府" },
        { "循州府", "东溪府" },
        { "阳江府", "银滩府" },
        { "清城府", "北山府" },
        { "潮州府", "金凤府" },
        { "揭阳府", "榕荫府" },
        { "泷水府", "石鼓府" },
        { "五羊城", "南羊城" },
        { "新安（西渡）", "东湾（西渡）" },
        { "西渡（新安城）", "西渡（东湾城）" },
        { "新安城", "东湾城" },
        { "石龙镇", "百川镇" },
        { "西湖墟", "鹅山墟" },
        { "祖庙镇", "明珠镇" },
        { "香山（北关）", "青藤（北关）" },
        { "香山湾", "青藤湾" },
        { "石岐墟", "锦溪墟" },
        { "曲江口", "五岭口" },
        { "樟林港", "潮风港" },
        { "蓬江墟", "银江墟" },
        { "赤坎埠", "雷海埠" },
        { "高凉城", "荔枝城" },
        { "端州墟", "砚山墟" },
        { "松口镇", "寒香镇" },
        { "汕尾港", "白浪港" },
        { "东江埠", "东溪埠" },
        { "海陵港", "银滩港" },
        { "清远墟", "北山墟" },
        { "牌坊街", "金凤城" },
        { "榕城墟", "榕荫墟" },
        { "罗定墟", "石鼓墟" },
        { "福田墟", "稻田墟" },
        { "宝安墟", "吉安墟" },
        { "西关涌", "西水涌" },
        { "东山口", "东坡口" },
        { "沥滘村", "芦湾村" },
        { "沙河墟", "浅沙墟" },
        { "江村墟", "江边墟" },
        { "黄埔埠", "黄沙埠" },
        { "石基墟", "石阶墟" },
        { "花山墟", "茶花墟" },
        { "沙湾墟", "细沙湾" },
        { "增城墟", "丰穗墟" },
        { "武江渡", "双溪渡" },
        { "浈江埠", "清溪埠" },
        { "马坝墟", "马鞍墟" },
        { "始兴墟", "兴隆墟" },
        { "仁化墟", "和顺墟" },
        { "乐昌坪", "安乐坪" },
        { "南雄墟", "南坡墟" },
        { "龙岗墟", "青岗墟" },
        { "坪山围", "平山围" },
        { "南头墟", "南渡墟" },
        { "前山墟", "前坡墟" },
        { "斗门墟", "渔斗门" },
        { "三灶湾", "三石湾" },
        { "澄海墟", "碧海墟" },
        { "达濠埠", "浪花埠" },
        { "棉城墟", "木棉墟" },
        { "海门湾", "观海门" },
        { "石湾窑", "陶窑坊" },
        { "南庄墟", "南村墟" },
        { "大良墟", "良田墟" },
        { "西南涌", "西桥涌" },
        { "新会墟", "葵乡墟" },
        { "江海埠", "平川埠" },
        { "台城墟", "望台墟" },
        { "水步墟", "踏水墟" },
        { "赤坎墟", "红土墟" },
        { "鹤山墟", "白鹤墟" },
        { "恩平墟", "泉水墟" },
        { "霞山埠", "晚霞埠" },
        { "麻章墟", "麻石墟" },
        { "遂溪墟", "甘溪墟" },
        { "徐闻渡", "海角渡" },
        { "廉江墟", "清泉墟" },
        { "雷城墟", "响雷墟" },
        { "吴川埠", "渔帆埠" },
        { "水东湾", "东水湾" },
        { "电白墟", "白沙墟" },
        { "高州墟", "高坡墟" },
        { "化州渡", "橘香渡" },
        { "信宜墟", "竹山墟" },
        { "水坑口", "石坑口" },
        { "鼎湖墟", "青鼎墟" },
        { "高要渡", "石牌渡" },
        { "广宁墟", "翠竹墟" },
        { "怀集墟", "山谷墟" },
        { "四会墟", "四水墟" },
        { "桥东埠", "桥头埠" },
        { "淡水墟", "甘水墟" },
        { "博罗墟", "青萝墟" },
        { "稔山湾", "稻香湾" },
        { "梅县墟", "寒梅墟" },
        { "蕉岭坪", "芭蕉坪" },
        { "大埔墟", "青瓷墟" },
        { "丰顺汤", "温泉汤" },
        { "五华墟", "五峰墟" },
        { "兴宁围", "宁川围" },
        { "海丰墟", "丰海墟" },
        { "陆丰墟", "丰陆墟" },
        { "碣石湾", "玄石湾" },
        { "源城埠", "水源埠" },
        { "紫金墟", "紫竹墟" },
        { "龙川渡", "青龙渡" },
        { "和平墟", "太和墟" },
        { "连平塘", "连翠塘" },
        { "阳春墟", "春山墟" },
        { "东平埠", "东海埠" },
        { "闸坡湾", "银浪湾" },
        { "春湾墟", "春风墟" },
        { "英德墟", "茶香墟" },
        { "茶山墟", "采茶墟" },
        { "凤凰墟", "金羽墟" },
        { "饶平埠", "白鹭埠" },
        { "揭东墟", "古榕东" },
        { "榕江渡", "榕荫渡" },
        { "普宁墟", "青梅墟" },
        { "惠来墟", "灵泉墟" },
        { "云城墟", "云岫墟" },
        { "南江渡", "石帆渡" },
        { "新兴墟", "禅林墟" },
        { "郁南墟", "翠林墟" },
        { "泷江埠", "泷水埠" },
        { "香埠", "汐港" },
        { "莲澳", "镜屿" },
        { "粤海", "榕川" },
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
        if (region == "榕川" && name == "东湾城") name = "东湾（西渡）";
        if (region == "榕川" && name == "青藤湾") name = "青藤（北关）";
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
                var station=stations[sIdx]; station.level=record.level; station.trafficEvent=record.trafficEvent; station.built=record.built; station.closed=record.closed; station.riderBonus=record.riderBonus; station.facLevel=record.facLevel; station.customName=string.IsNullOrWhiteSpace(record.customName) ? "" : record.customName.Trim();
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
        foreach (var c in activeContracts) data.contracts.Add(new MunicipalContract {
            id=c.id, type=c.type, startValue=c.startValue, targetValue=c.targetValue,
            rewardCoins=c.rewardCoins, rewardRp=c.rewardRp, rewardDiamonds=c.rewardDiamonds,
            deadlineDay=c.deadlineDay, claimed=c.claimed });
        data.coTech.AddRange(companyTechDone);
        foreach (var t in talents) data.talents.Add(CopySaveTalent(t));
        foreach (var t in talentPool) data.talentPool.Add(CopySaveTalent(t));
        foreach(var s in stations) data.stations.Add(new SavedStation { key=StationSaveKey(s), customName=string.IsNullOrEmpty(s.customName) ? "" : s.customName, level=s.level, trafficEvent=s.trafficEvent, built=s.built, closed=s.closed, riderBonus=s.riderBonus, facLevel=s.facLevel });
        foreach(var s in sceneries) data.scenery.Add(new SavedScenery { key=ScenerySaveKey(s), found=s.found, unlocked=s.unlocked });
        foreach(var l in lines) { var record=new SavedLine { id=l.id, name=l.name, color=l.color, skin=l.skin, trains=l.trains, dispatch=(int[])l.dispatch.Clone(), fare=l.fare, todayGross=l.todayGross, todayMaintenance=l.todayMaintenance, todayTrack=l.todayTrack, todayRevenue=l.todayRevenue, recent=new List<int>(l.recent), stations=new List<string>() }; foreach(int i in l.st) record.stations.Add(StationSaveKey(stations[i])); data.lines.Add(record); }
        foreach(var v in researchVehicles) data.vehicles.Add(new SavedVehicle { node=v.node, lineId=v.lineId });
        data.tech.AddRange(techDone); data.story.AddRange(storySeen); data.skins.AddRange(ownedLineSkins);
        data.themes.AddRange(ownedThemes);
        foreach (var kv in adWatchCounts) { data.adKeys.Add(kv.Key); data.adCounts.Add(kv.Value); }
        foreach (var kv in themeTrialUntil) { data.trialThemes.Add(kv.Key); data.trialEnds.Add(kv.Value); }
        return data;
    }
    static SavedTalent CopySaveTalent(SavedTalent t)
    {
        return new SavedTalent { id=t.id, name=t.name, role=t.role, rarity=t.rarity,
            level=t.level, xp=t.xp, assignKind=t.assignKind, assignLineId=t.assignLineId,
            assignStationKey=t.assignStationKey, flawIndex=t.flawIndex,
            flawCleansed=t.flawCleansed, bio=t.bio };
    }
    sealed class AutosavePayload { public string json, pretty; }
    System.Threading.Tasks.Task<AutosavePayload> autosaveSerialization;
    AutosavePayload pendingAutosavePayload;
    int saveRevision, autosaveRevision;
    float saveIdleSeconds;
    bool saveDirty;
    float saveDebounceTimer;
    void RequestSaveGame(float delay = 0.85f)
    {
        if (!saveReady || saveBlocked) return;
        unchecked { saveRevision++; }
        saveDirty = true;
        saveDebounceTimer = delay;
    }
    void SaveGame()
    {
        if(!saveReady || saveBlocked) return;
        unchecked { saveRevision++; }
        pendingAutosavePayload = null;
        saveDirty = false;
        saveDebounceTimer = 0f;
        autosaveTimer = 0f;
        try {
            var data = CaptureSaveData();
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data)); PlayerPrefs.Save();
#if UNITY_EDITOR
            if (DevTestSaveSyncEnabled) WriteDevTestSaveToDisk(data);
#endif
            TryAutoSyncTapTapCloudSave(false);
        } catch(Exception e) { Debug.LogError("本地存档保存失败：" + e.Message); }
    }
    void TickAutosave()
    {
        if (!saveReady || saveBlocked) return;
        float dt = Time.unscaledDeltaTime;
        bool interacting = draft.Count > 0 || lineInsertActive || Input.GetMouseButton(0) || Input.touchCount > 0;
        saveIdleSeconds = interacting ? 0f : saveIdleSeconds + dt;
        bool busy = interacting || saveIdleSeconds < 1f || pendingForecastMask != 0;
        saveDebounceTimer -= dt;
        autosaveTimer += dt;
        if (autosaveSerialization != null)
        {
            if (!autosaveSerialization.IsCompleted) return;
            if (autosaveSerialization.IsFaulted)
                Debug.LogError("本地存档序列化失败：" + autosaveSerialization.Exception.GetBaseException().Message);
            else if (autosaveRevision == saveRevision) pendingAutosavePayload = autosaveSerialization.Result;
            autosaveSerialization = null;
            return; // Do not combine snapshot/serialization/flush in one frame.
        }
        if (pendingAutosavePayload != null)
        {
            if (autosaveRevision != saveRevision) { pendingAutosavePayload = null; return; }
            if (busy) return;
            try
            {
                PlayerPrefs.SetString(SaveKey, pendingAutosavePayload.json);
                PlayerPrefs.Save();
#if UNITY_EDITOR
                if (DevTestSaveSyncEnabled)
                {
                    WriteSaveJsonAtomically(BundledTestSaveAssetPath, pendingAutosavePayload.pretty);
                    DevTestSaveDirty = true;
                }
#endif
                saveDirty = false; saveDebounceTimer = 0f; autosaveTimer = 0f;
                TryAutoSyncTapTapCloudSave(false);
            }
            catch (Exception e) { Debug.LogError("本地存档保存失败：" + e.Message); }
            pendingAutosavePayload = null;
            return;
        }
        if (busy || (saveDirty ? saveDebounceTimer > 0f : autosaveTimer < 5f)) return;
        try
        {
            // Capture on the main thread; WebGL runs without managed worker threads.
            var data = CaptureSaveData();
            autosaveRevision = saveRevision;
#if UNITY_WEBGL && !UNITY_EDITOR
            pendingAutosavePayload = new AutosavePayload {
                json = JsonUtility.ToJson(data), pretty = null
            };
#else
            bool needPretty = false;
#if UNITY_EDITOR
            needPretty = DevTestSaveSyncEnabled;
#endif
            autosaveSerialization = System.Threading.Tasks.Task.Run(() => new AutosavePayload {
                json = JsonUtility.ToJson(data), pretty = needPretty ? JsonUtility.ToJson(data, true) : null
            });
#endif
        }
        catch (Exception e) { Debug.LogError("本地存档快照失败：" + e.Message); }
    }
    string ReadBundledTestSaveRaw()
    {
#if UNITY_EDITOR
        string diskJson = ReadDevTestSaveFromDisk();
        if (!string.IsNullOrEmpty(diskJson)) return diskJson;
#endif
#if UNITY_EDITOR || TRAILER_PREVIEW
        var asset = Resources.Load<TextAsset>(BundledTestSaveResource);
        return asset ? asset.text : null;
#else
        return null;
#endif
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
                string reg = string.IsNullOrEmpty(check.region) ? "汐港" : DisplayPlaceAliases(check.region);
                int lc = check.lines != null ? check.lines.Count : 0;
                int tc = check.tech != null ? check.tech.Count : 0;
                return "第 " + Mathf.Max(1, check.day) + " 日" + reg + "测试存档（" + lc + " 条线路 · " + tc + " 项科技 · " + FormatCoins(check.coins) + " 资金）";
            }
        } catch {}
        return "预置测试存档";
    }
    void ResetLoadedSessionState()
    {
        unchecked { saveRevision++; }
        pendingAutosavePayload = null;
        saveDirty = false; saveDebounceTimer = 0f;
        if (forecastWork != null) { forecastWork.Dispose(); forecastWork = null; }
        pendingForecastMask = 0; hasStagedTopoSig = false; hasTrafficTopologySig = false;
        for (int slot = 0; slot < 4; slot++)
        {
            if (stagedSlots != null && slot < stagedSlots.Length) stagedSlots[slot] = null;
            if (trafficSlots != null && slot < trafficSlots.Length) trafficSlots[slot] = null;
        }
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
    bool ApplyLoadedSaveJson(string raw, string sourceLabel, bool fromBundledTestSave)
    {
        if (string.IsNullOrEmpty(raw)) { Toast("未找到" + sourceLabel); return false; }
        try {
            var check = JsonUtility.FromJson<SaveData>(raw);
            if (check == null || check.version != 1 || check.lines == null || check.stations == null)
            {
                Toast(sourceLabel + "格式校验失败");
                return false;
            }
        } catch (Exception e) {
            Toast(sourceLabel + "解析失败：" + e.Message);
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
        if (fromBundledTestSave)
        {
            // Loading the shared test run explicitly resumes shared test-save synchronization.
            DevTestSaveSyncEnabled = true;
        }
#endif
        lines.Clear();
        BuildData();
        PlayerPrefs.SetString(SaveKey, raw);
        PlayerPrefs.Save();
        saveBlocked = false;
        saveReady = false;
        if (!RestoreGame()) { Toast(sourceLabel + "载入失败"); return false; }

        EnsureCompanyInitialized();
        if (!IsThemeAvailable(currentTheme)) currentTheme = 0;
        string dest = (!string.IsNullOrEmpty(savedRegion) && RegionUnlocked(savedRegion)) ? savedRegion : "汐港";
        FreezeRegionTraffic();
        currentRegion = dest;
        if (hkMapArt) hkMapArt.SetActive(currentRegion == "汐港");
        if (macauMapArt) macauMapArt.SetActive(currentRegion == "镜屿");
        if (guangdongMapArt) guangdongMapArt.SetActive(currentRegion == "榕川");
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
        if (regionTitle) regionTitle.text = DisplayPlaceAliases(currentRegion == "汐港" ? "汐 港" : currentRegion == "镜屿" ? "镜 屿" : "榕 川");
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
        Toast("已载入" + sourceLabel + "：第 " + day + " 日 · " + DisplayPlaceAliases(currentRegion));
        return true;
    }

    bool LoadBundledTestSave()
    {
        return ApplyLoadedSaveJson(ReadBundledTestSaveRaw(), "测试存档", true);
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


