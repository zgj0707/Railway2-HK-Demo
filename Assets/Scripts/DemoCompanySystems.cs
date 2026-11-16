using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    // ---------- M2: 公司经营科技树（企业治理树）与人才招募培养系统 ----------
    [Serializable]
    public sealed class SavedTalent
    {
        public int id;
        public string name;
        public int role;       // 0: 站务长, 1: 王牌车长, 2: 精算幕僚, 3: 文旅专员
        public int rarity;     // 1: 良才, 2: 精英, 3: 卓异
        public int level = 1;  // 1..5
        public int xp;
        public int assignKind; // 0: 待命, 1: 驻站, 2: 随线
        public int assignLineId;
        public string assignStationKey = "";
        public int flawIndex;
        public bool flawCleansed;
        public string bio = "";
    }

    sealed class CompanyTechNode
    {
        public string key;
        public int eraGroup; // 0: 19世纪 (开放), 1: 20世纪 (暂未开放), 2: 21世纪 (暂未开放)
        public int year;
        public int branch; // 0: 组织人力, 1: 商业地产, 2: 资本金融, 3: 运营品牌, 4: 商海教训
        public string title;
        public string periodLabel;
        public string desc;
        public string effectSummary;
        public int costCoins;
        public int costRp;
        public string[] parents;
    }

    readonly List<string> companyTechDone = new List<string>();
    readonly Dictionary<string, CompanyTechNode> coTechByKey = new Dictionary<string, CompanyTechNode>();
    readonly List<SavedTalent> talents = new List<SavedTalent>();
    readonly List<SavedTalent> talentPool = new List<SavedTalent>();
    int nextTalentId = 1;
    int lastTalentRefreshDay = -1;
    int techTreeTab = 0;       // 0: 工程技术史, 1: 企业治理树
    int companyBoardSubTab = 0; // 0: 董事会名宿, 1: 才俊招募与培养

    static readonly string[] CoTechBranchNames = { "组织人力", "商业地产", "资本金融", "运营品牌", "商海教训" };
    static readonly Color[] CoTechBranchColors = {
        new Color(0.39f, 0.62f, 0.37f), // 组织人力（松绿）
        new Color(0.84f, 0.63f, 0.25f), // 商业地产（鎏金）
        new Color(0.30f, 0.49f, 0.72f), // 资本金融（靛蓝）
        new Color(0.82f, 0.41f, 0.35f), // 运营品牌（朱砂）
        new Color(0.55f, 0.34f, 0.28f)  // 商海教训（赭褐）
    };
    static readonly string[] TalentRoleNames = { "站务长", "王牌车长", "精算幕僚", "文旅专员" };
    static readonly string[] TalentRarityNames = { "", "良才", "精英", "卓异" };
    static readonly string[] TalentFlawDescs = {
        "初任磨合：随线或驻站日耗略增（维保 +5%，Lv.3或淬炼可除）",
        "排班保守：发车节奏偏稳（车速 -3%，Lv.3或淬炼可除）",
        "文书繁琐：开站筹备略慢（站点费 +4%，Lv.3或淬炼可除）",
        "慢热性格：需配合老线磨合（初期收益 -3%，Lv.3或淬炼可除）"
    };
    static readonly string[] TalentSurnames = { "沈", "顾", "许", "陆", "温", "唐", "谢", "苏", "江", "何", "潘", "叶", "宋", "钟" };
    static readonly string[] TalentGivenNames = { "明远", "清嘉", "怀瑾", "予安", "景行", "思齐", "知微", "承泽", "若水", "闻轩", "星桥", "舒雅", "致远", "青晏" };
    static readonly string[] TalentBios = {
        "曾在苍岚岛车厂担任见习调度员，手写时刻表从不出错一分。",
        "自小在骑楼老街长大，熟稔街坊商铺门道与早晚市客流规律。",
        "毕业于海湾统计夜校，喜欢带着算盘与钢笔巡检每一段路轨。",
        "热衷拍摄山海风物明信片，总能把普通站台办成旅途里的打卡地。",
        "出身三代铁路世家，箱底藏着一本泛黄的蒸汽机车保养札记。",
        "擅长协调跨区口岸站务，再拥挤的换乘通道也能理出秩序。"
    };

    static readonly CompanyTechNode[] CompanyTechNodes = {
        // 环 0：1800—1829 · 矿轨契约与特许立社
        new CompanyTechNode {
            key = "co_charter_1801", eraGroup = 0, year = 1801, branch = 1,
            title = "萨里包运契约", periodLabel = "1801 年 · 公共收费铁路雏形",
            desc = "1801 年英国萨里铁道获议会特许作为世界首条公共收费轨道运输社运营，奠定“集资修路、按程收费”的商业起点。",
            effectSummary = "开站与商用筹备造价 -3%", costCoins = 0, costRp = 0, parents = Array.Empty<string>()
        },
        new CompanyTechNode {
            key = "co_stock_1821", eraGroup = 0, year = 1821, branch = 2,
            title = "议会筑路特许案", periodLabel = "1821 年 · 斯托克顿—达灵顿法案",
            desc = "1821 年英国首条公共蒸汽铁路历经国会辩论取得法定筑路与征地特许状，确立近代铁路公司合股筹资法规。",
            effectSummary = "全网铺轨造价 -4%", costCoins = 220, costRp = 2, parents = new[] { "co_charter_1801" }
        },
        new CompanyTechNode {
            key = "co_shares_1825", eraGroup = 0, year = 1825, branch = 0,
            title = "合股募股章程", periodLabel = "1825 年 · 公开招股与经理制",
            desc = "1825 年通车前夕铁路公司向公众发售股份并聘任专职总监管账，确立资合制铁路社的初代治理与人才框架。",
            effectSummary = "全网人才编制上限 +1", costCoins = 320, costRp = 3, parents = new[] { "co_stock_1821" }
        },
        new CompanyTechNode {
            key = "co_booth_1826", eraGroup = 0, year = 1826, branch = 1,
            title = "初代站台票房", periodLabel = "1826 年 · 驿站木屋售票处",
            desc = "早期公共客运在道旁搭设兼办售票、寄存与小卖部的木屋站房，让车站首次具备稳定商铺租金价值。",
            effectSummary = "新开车站与站前商用设施造价 -5%", costCoins = 340, costRp = 3, parents = new[] { "co_charter_1801" }
        },

        // 环 1：1830—1845 · 清算萌芽与标准时刻
        new CompanyTechNode {
            key = "co_fin_1", eraGroup = 0, year = 1830, branch = 2,
            title = "复式分账精算", periodLabel = "1830 年 · 利物浦—曼彻斯特会计制",
            desc = "1830 年利曼铁路严格区隔工程资本支出与煤耗、维护等日常营运账目，开创铁路精算成本核查传统。",
            effectSummary = "铺轨造价再 -6%，线路与车辆日常维保 -6%", costCoins = 450, costRp = 4, parents = new[] { "co_stock_1821" }
        },
        new CompanyTechNode {
            key = "co_hr_1", eraGroup = 0, year = 1835, branch = 0,
            title = "站务班组规章", periodLabel = "1835 年 · 制服班组与轮值表",
            desc = "干线客运兴盛后，各站配备着装整洁、照表交接的站长、扳道员与行包班组，员工培养自成章法。",
            effectSummary = "人才训练经验 +25%，全网人才编制上限再 +1", costCoins = 480, costRp = 4, parents = new[] { "co_shares_1825" }
        },
        new CompanyTechNode {
            key = "co_re_1", eraGroup = 0, year = 1838, branch = 1,
            title = "月台书报茶座", periodLabel = "1838 年 · 车站特许商铺兴起",
            desc = "1830 年代末旅途报刊与茶点需求激增，书报商铺率先批量入驻大站月台，形成持续稳健的特许经营租金流。",
            effectSummary = "开站与站前商业设施造价再 -7%，物业租金 +6%", costCoins = 560, costRp = 5, parents = new[] { "co_booth_1826" }
        },
        new CompanyTechNode {
            key = "co_ops_1", eraGroup = 0, year = 1840, branch = 3,
            title = "铁路统一标准时", periodLabel = "1840 年 · 大西部铁路标准时",
            desc = "1840 年大西部铁路率先废除沿线各镇地方日晷时间、全网统一按伦敦标准时运行，塑造准点金字口碑。",
            effectSummary = "全网机车平均运营车速 +6%，每日科研产速 +15%", costCoins = 640, costRp = 5, parents = new[] { "co_hr_1", "co_booth_1826" }
        },
        new CompanyTechNode {
            key = "co_rch_1842", eraGroup = 0, year = 1842, branch = 3,
            title = "伦敦铁路清算所", periodLabel = "1842 年 · 跨社联运清分机构",
            desc = "1842 年铁路清算所（RCH）设立，各家铁路公司据此实行联程一票直达并按里程自动清分票款。",
            effectSummary = "跨线换乘阻力降低 8%", costCoins = 980, costRp = 6, parents = new[] { "co_ops_1", "co_fin_1" }
        },
        new CompanyTechNode {
            key = "co_fare_act_1844", eraGroup = 0, year = 1844, branch = 3,
            title = "分级客票条例", periodLabel = "1844 年 · 格莱斯顿铁路法案",
            desc = "1844 年议会法案规定各干线须每日开行每英里一便士的平价三等车厢，促成大众平价出行时代与分级票价制度。",
            effectSummary = "全网基础客流需求 +4%，客票营收结算 +3%", costCoins = 1150, costRp = 7, parents = new[] { "co_rch_1842" }
        },
        new CompanyTechNode {
            key = "co_mania_1845", eraGroup = 0, year = 1845, branch = 4,
            title = "铁路狂热泡沫", periodLabel = "1845 年 · 历史商海教训",
            desc = "1845—1846 年间上百家空壳路社扎堆募股引爆投机股灾，虽短期拖高合规审计开销，却倒逼出严格公司债法制。",
            effectSummary = "历史教训：线路维保短期 +3%（债券与规范审计前置）", costCoins = 850, costRp = 4, parents = new[] { "co_fin_1" }
        },

        // 环 2：1846—1864 · 干线整并与基建融资
        new CompanyTechNode {
            key = "co_fin_2", eraGroup = 0, year = 1849, branch = 2,
            title = "铁路债券与优先股", periodLabel = "1849 年 · 长期公司债发行制度",
            desc = "投机泡沫退潮后，规范发行固定息票的公司债券与优先股取代高息短拆，为长周期铁路建设提供低息长期本金。",
            effectSummary = "基建债券可贷本金 +20%，每期还本付息压力 -10%", costCoins = 1500, costRp = 9, parents = new[] { "co_mania_1845" }
        },
        new CompanyTechNode {
            key = "co_ops_3", eraGroup = 0, year = 1851, branch = 3,
            title = "万国博览假日套票", periodLabel = "1851 年 · 托马斯·库克专列",
            desc = "1851 年伦敦万国博览会期间，旅行包租专列与风景区往返套票爆红，开创“铁道+文旅”爆款营销先河。",
            effectSummary = "含名胜车站的线路客票收入额外 +15%", costCoins = 1750, costRp = 10, parents = new[] { "co_fare_act_1844" }
        },
        new CompanyTechNode {
            key = "co_hotel_1854", eraGroup = 0, year = 1854, branch = 1,
            title = "终点站大酒店", periodLabel = "1854 年 · 站城一体物业先驱",
            desc = "1854 年帕丁顿大西部酒店直通终点站月台开业，集住宿、宴会与商铺于一体，使铁路社从买路修桥迈向站前造城。",
            effectSummary = "站前物业每日租金 +12%，名胜地标站点吸客 +5%", costCoins = 2100, costRp = 11, parents = new[] { "co_re_1" }
        },
        new CompanyTechNode {
            key = "co_ops_2", eraGroup = 0, year = 1858, branch = 3,
            title = "电报总社联调", periodLabel = "1858 年 · 电报协同导视制",
            desc = "十九世纪中叶电报线沿路轨铺通，总社调度房与大站导视牌实时报信接续，极大减少了乘客跨线换乘等待。",
            effectSummary = "跨线换乘惩罚再降 8%，全网车票营收结算 +5%", costCoins = 2300, costRp = 11, parents = new[] { "co_rch_1842" }
        },
        new CompanyTechNode {
            key = "co_hr_3", eraGroup = 0, year = 1862, branch = 0,
            title = "机务师徒传帮带", periodLabel = "1862 年 · 机车城夜校与工坊",
            desc = "克鲁、斯温顿等铁路重镇兴办技校与工坊师徒制，老车长与大司炉言传身教，大幅摊薄人才进修耗费。",
            effectSummary = "人才进修经费 -25%，每日自然实训经验 +50%", costCoins = 2200, costRp = 12, parents = new[] { "co_hr_1" }
        },

        // 环 3：1865—1879 · 总社科层与远东初探
        new CompanyTechNode {
            key = "co_hr_2", eraGroup = 0, year = 1866, branch = 0,
            title = "总社董事议事厅", periodLabel = "1866 年 · 麦卡勒姆职能组织图",
            desc = "丹尼尔·麦卡勒姆绘制史上首张现代公司职能分部架构图，划分工务、机务、票务与督办董事常驻议事席。",
            effectSummary = "董事会常驻高管席位 +1，人才编制上限再 +1", costCoins = 2900, costRp = 13, parents = new[] { "co_hr_3", "co_ops_2" }
        },
        new CompanyTechNode {
            key = "co_pension_1868", eraGroup = 0, year = 1868, branch = 0,
            title = "员工互助抚恤金", periodLabel = "1868 年 · 司乘互助保障社",
            desc = "大型干线社自 1860 年代末普遍成立养老与伤残互助基金，骨干队伍高度稳定，日常实训事半功倍。",
            effectSummary = "人才进修经费再 -10%，人才训练经验再 +10%", costCoins = 3400, costRp = 14, parents = new[] { "co_hr_2" }
        },
        new CompanyTechNode {
            key = "co_waybill_1871", eraGroup = 0, year = 1871, branch = 2,
            title = "联运提单押汇", periodLabel = "1871 年 · 路行票据结算制度",
            desc = "铁路货运单据纳入银行贴现押汇体系，货主与商家可凭路单融资，铁路社流动资金周转效率成倍加快。",
            effectSummary = "线路与车辆日常维保费再 -5%", costCoins = 3800, costRp = 14, parents = new[] { "co_fin_2" }
        },
        new CompanyTechNode {
            key = "co_woosung_1876", eraGroup = 0, year = 1876, branch = 4,
            title = "浅滩商办教训", periodLabel = "1876 年 · 滨海商路教训",
            desc = "1876 年海隅浅滩窄轨未获妥善征地公文即仓促通车，终被督署拆撤；警示海湾开站必先理顺市政契约。",
            effectSummary = "历史教训：开站费短期 +3%（市政招标与官督商办前置）", costCoins = 4200, costRp = 12, parents = new[] { "co_waybill_1871", "co_hotel_1854" }
        },
        new CompanyTechNode {
            key = "co_fin_3", eraGroup = 0, year = 1878, branch = 2,
            title = "市政招标规范道", periodLabel = "1878 年 · 公私合营工程标书",
            desc = "汲取早期征地纠葛教训，确立明文路权、分期工程赏金与履约验收条款的市政公开招标体系。",
            effectSummary = "市政中标工程赏金 +20%，同时可挂牌标书上限 +1", costCoins = 4600, costRp = 15, parents = new[] { "co_woosung_1876" }
        },

        // 环 4：1880—1889 · 港埠骑楼与独立年审
        new CompanyTechNode {
            key = "co_kaiping_1881", eraGroup = 0, year = 1881, branch = 3,
            title = "官督商办路政约", periodLabel = "1881 年 · 矿埠自办商路先声",
            desc = "1881 年海北矿务局自建矿区铁轨并首创商民自营商业运务，探索出兼顾督管与商股分红的本土章法。",
            effectSummary = "全网基础客流需求 +4%，每日科研点产速 +10%", costCoins = 5200, costRp = 16, parents = new[] { "co_woosung_1876" }
        },
        new CompanyTechNode {
            key = "co_re_2", eraGroup = 0, year = 1883, branch = 1,
            title = "骑楼连廊商圈", periodLabel = "1883 年 · 南岸港埠站前铺面",
            desc = "十九世纪八十年代三域埠岸兴建遮阳避雨的骑楼商廊，把下车旅客自然引入茶室银庄与杂货行。",
            effectSummary = "所有已落成站前物业的每日租金收益 +20%", costCoins = 5600, costRp = 16, parents = new[] { "co_hotel_1854", "co_kaiping_1881" }
        },
        new CompanyTechNode {
            key = "co_audit_1885", eraGroup = 0, year = 1885, branch = 2,
            title = "特许会计师年审", periodLabel = "1885 年 · 独立路账年审制度",
            desc = "大型铁路公司延请外部特许会计师审计轨料折旧与客货票款，大幅增进银行信用并压缩筑路虚耗。",
            effectSummary = "铺轨造价再 -5%，基建债券每期还款压力再 -5%", costCoins = 6000, costRp = 17, parents = new[] { "co_fin_3" }
        },
        new CompanyTechNode {
            key = "co_rateact_1887", eraGroup = 0, year = 1887, branch = 3,
            title = "跨埠公平运价约", periodLabel = "1887 年 · 公开透明运价条例",
            desc = "参照 1887 年公平运价法案禁止暗箱歧视加价与恶性割价竞争，稳定了长途与跨埠干线的合理利润率。",
            effectSummary = "全网客票营收结算 +4%", costCoins = 6400, costRp = 17, parents = new[] { "co_audit_1885", "co_ops_3" }
        },
        new CompanyTechNode {
            key = "co_re_3", eraGroup = 0, year = 1888, branch = 1,
            title = "望海奇峰缆车特许", periodLabel = "1888 年 · 青屿山顶缆车通车",
            desc = "1888 年青屿山顶缆车通车，缔造观光交通与半山地产共荣典范，名胜车站由此成为全网客流高地。",
            effectSummary = "名胜地标站点基础吸引客流再 +10%", costCoins = 6800, costRp = 18, parents = new[] { "co_re_2" }
        },

        // 环 5：1890—1899 · 海湾口岸与现代集团
        new CompanyTechNode {
            key = "co_hr_4", eraGroup = 0, year = 1891, branch = 0,
            title = "名宿礼遇与委任状", periodLabel = "1891 年 · 资深董事授衔章程",
            desc = "世纪末大型铁路集团向历任高管颁授礼遇与独立权责委任状，大幅化解部门本位掣肘与磨合内耗。",
            effectSummary = "董事会高管负面特质削减 35%，常驻董事会席位再 +1", costCoins = 7400, costRp = 19, parents = new[] { "co_rateact_1887", "co_hr_2" }
        },
        new CompanyTechNode {
            key = "co_ops_4", eraGroup = 0, year = 1893, branch = 3,
            title = "山海旅行手账刊", periodLabel = "1893 年 · 版画沿线风物指南",
            desc = "随车附赠精致的港口风光版画手册与名胜盖章本，使铁路品牌深入人心，屡夺年度行业评比桂冠。",
            effectSummary = "全网基础客流需求再 +8%，年度铁道大赏赏金 +25%", costCoins = 7800, costRp = 20, parents = new[] { "co_re_3" }
        },
        new CompanyTechNode {
            key = "co_re_4", eraGroup = 0, year = 1898, branch = 1,
            title = "跨湾枢纽综合体", periodLabel = "1898 年 · 跨湾干线草约与总站规划",
            desc = "1898 年锦川至青屿干线勘线草约签订，规划海港大枢纽与周边货栈商业街，打造跨城站城一体化枢纽。",
            effectSummary = "站前物业总租金再 +25%，全网车站吞吐上限 +10%", costCoins = 8600, costRp = 21, parents = new[] { "co_re_2", "co_re_3" }
        },
        new CompanyTechNode {
            key = "co_fin_4", eraGroup = 0, year = 1899, branch = 2,
            title = "跨境银团授信", periodLabel = "1899 年 · 十九世纪末银团联贷协议",
            desc = "世纪之交多家海湾银号结成跨境基础设施银团授信，以极低长期成本护航三域铁路总社跨入新纪元。",
            effectSummary = "全网客票营收结算再 +8%，线路与列车维保费再 -10%", costCoins = 9800, costRp = 24, parents = new[] { "co_audit_1885", "co_hr_4", "co_re_4" }
        }
    };

    void EnsureCoTechIndexBuilt()
    {
        if (coTechByKey.Count > 0) return;
        for (int i = 0; i < CompanyTechNodes.Length; i++)
        {
            var n = CompanyTechNodes[i];
            coTechByKey[n.key] = n;
        }
    }

    void EnsureCompanyTechMigrated()
    {
        EnsureCoTechIndexBuilt();
        if (companyTechDone.Count == 0) return;
        // 兼容 16 节点旧档：若玩家已解锁旧节点，为其平滑补齐新前置链，避免图谱出现悬空孤岛
        bool added;
        do
        {
            added = false;
            var snapshot = new List<string>(companyTechDone);
            foreach (string key in snapshot)
            {
                if (!coTechByKey.TryGetValue(key, out var node) || node.parents == null) continue;
                foreach (string parent in node.parents)
                {
                    if (!companyTechDone.Contains(parent))
                    {
                        companyTechDone.Add(parent);
                        added = true;
                    }
                }
            }
        } while (added);
    }

    int CoTechEraNodeCount(int era)
    {
        int c = 0;
        for (int i = 0; i < CompanyTechNodes.Length; i++) if (CompanyTechNodes[i].eraGroup == era) c++;
        return c;
    }

    int CoTechEraDoneCount(int era)
    {
        EnsureCompanyTechMigrated();
        int c = 0;
        for (int i = 0; i < CompanyTechNodes.Length; i++)
            if (CompanyTechNodes[i].eraGroup == era && companyTechDone.Contains(CompanyTechNodes[i].key)) c++;
        return c;
    }

    int CoTechRingIndex(CompanyTechNode node)
    {
        if (node == null) return 0;
        if (node.year < 1830) return 0;
        if (node.year <= 1845) return 1;
        if (node.year < 1865) return 2;
        if (node.year < 1880) return 3;
        if (node.year < 1890) return 4;
        return 5;
    }

    bool IsCoTechVisible(CompanyTechNode node)
    {
        if (node == null) return false;
        if (node.parents == null || node.parents.Length == 0) return true;
        foreach (string p in node.parents) if (companyTechDone.Contains(p)) return true;
        return false;
    }

    bool HasCoTech(string key) => companyTechDone.Contains(key);
    int CoTechTotalCount() => CoTechEraNodeCount(0);
    bool CanUnlockCoTech(CompanyTechNode node)
    {
        if (node == null || HasCoTech(node.key)) return false;
        if (node.parents == null) return true;
        foreach (string p in node.parents) if (!HasCoTech(p)) return false;
        return true;
    }

    // ---------- 公司科技全局乘区（纯读取、与已有模型无缝相乘） ----------
    int CoTechExtraSeats() => (HasCoTech("co_hr_2") ? 1 : 0) + (HasCoTech("co_hr_4") ? 1 : 0);
    float CoTechPartnerConMitigation() => HasCoTech("co_hr_4") ? 0.65f : 1f;
    float CoTechTrainXpMult() => (HasCoTech("co_hr_1") ? 1.25f : 1f) * (HasCoTech("co_hr_3") ? 1.50f : 1f) * (HasCoTech("co_pension_1868") ? 1.10f : 1f);
    float CoTechTrainCostMult() => (HasCoTech("co_hr_3") ? 0.75f : 1f) * (HasCoTech("co_pension_1868") ? 0.90f : 1f);
    float CoTechOpenCostMult() => (HasCoTech("co_charter_1801") ? 0.97f : 1f) * (HasCoTech("co_booth_1826") ? 0.95f : 1f) * (HasCoTech("co_re_1") ? 0.93f : 1f) * (HasCoTech("co_woosung_1876") ? 1.03f : 1f);
    float CoTechCommercialMult() => (HasCoTech("co_re_1") ? 1.06f : 1f) * (HasCoTech("co_hotel_1854") ? 1.12f : 1f) * (HasCoTech("co_re_2") ? 1.20f : 1f) * (HasCoTech("co_re_4") ? 1.25f : 1f);
    float CoTechSceneryDemandMult() => (HasCoTech("co_hotel_1854") ? 1.05f : 1f) * (HasCoTech("co_re_3") ? 1.10f : 1f);
    float CoTechStationCapMult() => HasCoTech("co_re_4") ? 1.10f : 1f;
    float CoTechTrackBuildMult() => (HasCoTech("co_stock_1821") ? 0.96f : 1f) * (HasCoTech("co_fin_1") ? 0.94f : 1f) * (HasCoTech("co_audit_1885") ? 0.95f : 1f);
    float CoTechMaintMult() => (HasCoTech("co_fin_1") ? 0.94f : 1f) * (HasCoTech("co_mania_1845") ? 1.03f : 1f) * (HasCoTech("co_waybill_1871") ? 0.95f : 1f) * (HasCoTech("co_fin_4") ? 0.90f : 1f);
    float CoTechBondCapMult() => HasCoTech("co_fin_2") ? 1.20f : 1f;
    float CoTechBondDueMult() => (HasCoTech("co_fin_2") ? 0.90f : 1f) * (HasCoTech("co_audit_1885") ? 0.95f : 1f);
    int CoTechContractCap() => HasCoTech("co_fin_3") ? 3 : 2;
    float CoTechContractBonusMult() => HasCoTech("co_fin_3") ? 1.20f : 1f;
    float CoTechSpeedMult() => HasCoTech("co_ops_1") ? 1.06f : 1f;
    float CoTechRpMult() => (HasCoTech("co_ops_1") ? 1.15f : 1f) * (HasCoTech("co_kaiping_1881") ? 1.10f : 1f);
    float CoTechTransferMult() => (HasCoTech("co_rch_1842") ? 0.92f : 1f) * (HasCoTech("co_ops_2") ? 0.92f : 1f);
    float CoTechRevenueMult() => (HasCoTech("co_fare_act_1844") ? 1.03f : 1f) * (HasCoTech("co_ops_2") ? 1.05f : 1f) * (HasCoTech("co_rateact_1887") ? 1.04f : 1f) * (HasCoTech("co_fin_4") ? 1.08f : 1f);
    float CoTechTourismLineMult() => HasCoTech("co_ops_3") ? 1.15f : 1f;
    float CoTechBaseDemandMult() => (HasCoTech("co_fare_act_1844") ? 1.04f : 1f) * (HasCoTech("co_kaiping_1881") ? 1.04f : 1f) * (HasCoTech("co_ops_4") ? 1.08f : 1f);
    float CoTechAwardMult() => HasCoTech("co_ops_4") ? 1.25f : 1f;

    float MitigateCon(float rawDelta)
    {
        // 例：rawDelta = 0.10f 表示负面增加 10%，减免 35% 后变为 +6.5%；若为 -0.05f 同样缩窄幅度
        return 1f + rawDelta * CoTechPartnerConMitigation();
    }

    // ---------- 人才编制、培养、加值计算 ----------
    int TalentRosterCapacity()
    {
        return 2 + (HasCoTech("co_shares_1825") ? 1 : 0) + (HasCoTech("co_hr_1") ? 1 : 0) + (HasCoTech("co_hr_2") ? 1 : 0)
            + Mathf.Min(3, DailyConnectedCount() / 10);
    }

    int TalentNextLevelXp(int level) => 40 + Mathf.Clamp(level, 1, 5) * 35;

    float TalentPowerScale(SavedTalent t)
    {
        if (t == null) return 0f;
        float rMult = t.rarity == 3 ? 1.45f : t.rarity == 2 ? 1.20f : 1.0f;
        return (0.04f + (t.level - 1) * 0.025f) * rMult;
    }

    bool TalentHasActiveFlaw(SavedTalent t) => t != null && !t.flawCleansed && t.level < 3;

    string TalentRoleEffectSummary(SavedTalent t)
    {
        if (t == null) return "";
        int pctMain = Mathf.RoundToInt(TalentPowerScale(t) * 100f);
        int pctSub = Mathf.Max(2, Mathf.RoundToInt(TalentPowerScale(t) * 65f));
        switch (t.role)
        {
            case 0: return $"驻站吞吐 +{pctMain}% · 站前物业租金 +{pctMain + 4}%";
            case 1: return $"随线列车速度 +{pctMain}% · 编组运力 +{pctSub}%";
            case 2: return $"维保支出 -{pctMain}% · 客票综合净利 +{pctSub}%";
            default: return $"沿线名胜吸引力 +{pctMain + 3}% · 票务营收 +{pctSub}%";
        }
    }

    string TalentAssignmentLabel(SavedTalent t)
    {
        if (t == null) return "待命中";
        if (t.assignKind == 1 && !string.IsNullOrEmpty(t.assignStationKey))
        {
            int stIdx = FindStationIndexByKey(t.assignStationKey);
            return stIdx >= 0 ? "驻站 · " + stations[stIdx].name : "驻站 · " + t.assignStationKey;
        }
        if (t.assignKind == 2 && t.assignLineId > 0)
        {
            var line = lines.Find(l => l.id == t.assignLineId);
            return line != null ? "随线 · " + line.name : "待命中";
        }
        return "总社待命";
    }

    int FindStationIndexByKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return -1;
        for (int i = 0; i < stations.Count; i++)
            if (StationSaveKey(stations[i]) == key) return i;
        return -1;
    }

    void EnsureTalentsInitialized(bool forceMarketRefresh = false)
    {
        // 自动修正失效的派驻目标
        foreach (var t in talents)
        {
            if (t == null) continue;
            t.level = Mathf.Clamp(t.level, 1, 5);
            if (t.assignKind == 2 && lines.Find(l => l.id == t.assignLineId) == null)
            { t.assignKind = 0; t.assignLineId = 0; }
            if (t.assignKind == 1 && FindStationIndexByKey(t.assignStationKey) < 0)
            { t.assignKind = 0; t.assignStationKey = ""; }
        }
        if (forceMarketRefresh || talentPool.Count == 0 || lastTalentRefreshDay != day)
            RefreshTalentMarket();
    }

    void RefreshTalentMarket()
    {
        talentPool.Clear();
        lastTalentRefreshDay = day;
        int seedBase = Mathf.Abs(day * 131 + DailyConnectedCount() * 17 + nextTalentId * 29 + (int)Mathf.Round((float)(coins % 97d)));
        for (int i = 0; i < 3; i++)
        {
            int h = seedBase + i * 73;
            int roll = h % 100;
            int rarity = roll >= 88 ? 3 : roll >= 52 ? 2 : 1;
            int role = (h / 7 + i) % TalentRoleNames.Length;
            string fullName = TalentSurnames[(h / 3) % TalentSurnames.Length] + TalentGivenNames[(h / 11 + i * 3) % TalentGivenNames.Length];
            talentPool.Add(new SavedTalent {
                id = 0,
                name = fullName,
                role = role,
                rarity = rarity,
                level = rarity >= 3 ? 2 : 1,
                xp = 0,
                assignKind = 0,
                flawIndex = (h / 5) % TalentFlawDescs.Length,
                flawCleansed = false,
                bio = TalentBios[(h / 13 + i) % TalentBios.Length]
            });
        }
    }

    int TalentHireCost(SavedTalent cand)
    {
        if (cand == null) return 300;
        return cand.rarity == 3 ? 1280 : cand.rarity == 2 ? 680 : 320;
    }

    int TalentTrainCoinCost(SavedTalent t)
    {
        int raw = (240 + t.level * 180) * t.rarity;
        return Mathf.Max(80, Mathf.RoundToInt(raw * CoTechTrainCostMult() / 10f) * 10);
    }

    int TalentTrainRpCost(SavedTalent t) => Mathf.Clamp(2 + t.level * 2, 2, 12);

    void GrantTalentXp(SavedTalent t, int rawXp, bool notify)
    {
        if (t == null || t.level >= 5) return;
        t.xp += Mathf.Max(1, Mathf.RoundToInt(rawXp * CoTechTrainXpMult()));
        bool leveled = false;
        while (t.level < 5 && t.xp >= TalentNextLevelXp(t.level))
        {
            t.xp -= TalentNextLevelXp(t.level);
            t.level++;
            leveled = true;
            if (t.level >= 3) t.flawCleansed = true;
        }
        if (t.level >= 5) t.xp = 0;
        if (leveled && notify) Toast($"人才晋升：「{t.name}」升至 Lv.{t.level}" + (t.level == 3 ? "（磨合期结束！）" : ""));
    }

    void TickDailyTalents()
    {
        if (talents.Count == 0) return;
        bool anyChange = false;
        foreach (var t in talents)
        {
            if (t == null || t.level >= 5) continue;
            int dailyXp = t.assignKind != 0 ? 15 : 6;
            int prevLvl = t.level;
            GrantTalentXp(t, dailyXp, true);
            if (t.level != prevLvl) anyChange = true;
        }
        if (anyChange) hasForecastSignature = false;
    }

    // ---------- 人才乘区注入 ----------
    float LineTalentSpeedMult(Line line)
    {
        if (line == null) return 1f;
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 2 || t.assignLineId != line.id) continue;
            if (t.role == 1) m *= 1f + TalentPowerScale(t);
            if (TalentHasActiveFlaw(t) && t.flawIndex == 1) m *= 0.97f;
        }
        return m;
    }

    float LineTalentCapMult(Line line)
    {
        if (line == null) return 1f;
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 2 || t.assignLineId != line.id) continue;
            if (t.role == 1) m *= 1f + TalentPowerScale(t) * 0.65f;
        }
        return m;
    }

    float LineTalentMaintMult(Line line)
    {
        if (line == null) return 1f;
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 2 || t.assignLineId != line.id) continue;
            if (t.role == 2) m *= Mathf.Max(0.65f, 1f - TalentPowerScale(t));
            if (TalentHasActiveFlaw(t) && t.flawIndex == 0) m *= 1.05f;
        }
        return m;
    }

    float LineTalentRevenueMult(Line line)
    {
        if (line == null) return 1f;
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 2 || t.assignLineId != line.id) continue;
            if (t.role == 2 || t.role == 3) m *= 1f + TalentPowerScale(t) * 0.65f;
            if (TalentHasActiveFlaw(t) && t.flawIndex == 3) m *= 0.97f;
        }
        return m;
    }

    float StationTalentCapMult(int stationIdx)
    {
        if (stationIdx < 0 || stationIdx >= stations.Count) return 1f;
        string key = StationSaveKey(stations[stationIdx]);
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 1 || t.assignStationKey != key) continue;
            if (t.role == 0) m *= 1f + TalentPowerScale(t);
        }
        return m;
    }

    float StationTalentDemandMult(int stationIdx)
    {
        if (stationIdx < 0 || stationIdx >= stations.Count) return 1f;
        string key = StationSaveKey(stations[stationIdx]);
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 1 || t.assignStationKey != key) continue;
            if (t.role == 3) m *= 1f + TalentPowerScale(t) + 0.03f;
        }
        return m;
    }

    float StationTalentCommercialMult(int stationIdx)
    {
        if (stationIdx < 0 || stationIdx >= stations.Count) return 1f;
        string key = StationSaveKey(stations[stationIdx]);
        float m = 1f;
        foreach (var t in talents)
        {
            if (t == null || t.assignKind != 1 || t.assignStationKey != key) continue;
            if (t.role == 0) m *= 1f + TalentPowerScale(t) + 0.04f;
            if (t.role == 2) m *= 1f + TalentPowerScale(t) * 0.5f;
        }
        return m;
    }

    // ---------- M2 UI A: 企业治理树（共享同心圆历史图谱引擎，XIX世纪史实经营节点） ----------
    readonly HashSet<string> coTechSessionUnlocked = new HashSet<string>();
    bool coTechCollapseDone = true;
    string coTechJustUnlocked;

    void OpenCompanyTechScreen(int mode = 0)
    {
        EnsureCompanyTechMigrated();
        OpenConcentricTechScreen(true, mode);
    }

    void TapCompanyTechNode(CompanyTechNode node)
    {
        if (node == null) return;
        if (HasCoTech(node.key))
        {
            HoldCoTechNode(node.key);
            return;
        }
        if (!CanUnlockCoTech(node))
        {
            HoldCoTechNode(node.key);
            Invoke(nameof(HideHistoryInfo), 2f);
            return;
        }
        if (coins < node.costCoins)
        {
            PlayErrorSfx();
            Toast("研发经费不足：还差 ¥" + FormatShortfall(node.costCoins - coins));
            if (historyCoinWidget) historyCoinWidget.Shake();
            return;
        }
        if (rp < node.costRp)
        {
            PlayErrorSfx();
            Toast("科研点不足：需要 " + node.costRp + " RP（还差 " + Mathf.CeilToInt(node.costRp - rp) + " RP）");
            return;
        }

        historyAnimated.Clear();
        for (int i = 0; i < CompanyTechNodes.Length; i++)
        {
            var cand = CompanyTechNodes[i];
            if (cand.eraGroup == 0 && IsCoTechVisible(cand)) historyAnimated.Add(cand.key);
        }

        BeginUndo("解锁企业治理科技");
        coins -= node.costCoins;
        rp -= node.costRp;
        companyTechDone.Add(node.key);
        coTechSessionUnlocked.Add(node.key);
        coTechJustUnlocked = node.key;
        EnsureCompanyInitialized();
        hasForecastSignature = false;
        operatingRates.Clear();
        RecalcIncome();
        EndUndo();
        PlayTechSfx();
        TriggerHaptic(22);
        SaveGame();
        RefreshHUD();
        OpenCompanyTechScreen(1);
        Toast("企业治理升级：「" + node.title + "」已生效！");
    }

    // ---------- M2 UI B: 人才招募与培养页签（嵌入公司->高管与人才页） ----------
    void BuildTalentManagementView(Transform page, float cardW)
    {
        EnsureTalentsInitialized();
        float poolH = 708f;
        float rosterH = Mathf.Max(280f, talents.Count * 246f + 130f);
        float totalH = poolH + rosterH + 60f;
        var content = FleetScroll(page, 36f, PgH - 350f, totalH);
        float topY = content.sizeDelta.y;
        float cardX = 4f, innerW = cardW - 8f, cW = innerW - 64f;

        // 1. 人才市场（每日免费刷新 / ¥150 即刻招募）
        float y1 = topY - 12f - poolH;
        var poolCard = Rect("TalentMarketCard", content, cardX, y1, innerW, poolH);
        Rounded(poolCard, CardBg); SoftShadow(poolCard); JournalTape(poolCard.transform, innerW * 0.5f - 72f, poolH - 17f, 144f);
        Txt(poolCard.transform, "山海猎聘阁 · 今日待聘名才", 29, Ink, 32f, poolH - 64f, 560f, 44f, TextAnchor.MiddleLeft);
        var refreshBtn = Btn(poolCard.transform, "RefreshMarket", "刷新名录 ¥150", 23, innerW - 276f, poolH - 72f, 244f, 58f, Paper2, () => {
            if (coins < 150) { PlayErrorSfx(); Toast("刷新猎聘名录需 ¥150"); return; }
            coins -= 150;
            RefreshTalentMarket();
            SaveGame(); RefreshHUD(); OpenCompanyScreen();
            Toast("已更新今日待聘人才名录");
        });
        TInk(refreshBtn, Indigo);

        for (int i = 0; i < talentPool.Count; i++)
        {
            var cand = talentPool[i];
            int idx = i;
            float ry = poolH - 258f - i * 184f;
            var row = Rect("Cand_" + i, poolCard.transform, 32f, ry, cW, 170f);
            Rounded(row, Color.Lerp(CardBg, Ink, 0.04f));
            Color rCol = cand.rarity == 3 ? Gold : cand.rarity == 2 ? Indigo : Matcha;
            var badge = Rect("Rarity", row.transform, 16f, 43f, 84f, 84f);
            Rounded(badge, rCol);
            Txt(badge.transform, TalentRarityNames[cand.rarity] + "\n" + TalentRoleNames[cand.role].Substring(0, 2), 22, Color.white, 0f, 0f, 84f, 84f, TextAnchor.MiddleCenter);

            Txt(row.transform, $"{cand.name} · Lv.{cand.level} {TalentRoleNames[cand.role]}", 25, Ink, 114f, 120f, 560f, 38f, TextAnchor.MiddleLeft);
            Txt(row.transform, "✓ " + TalentRoleEffectSummary(cand), 23, Matcha, 114f, 78f, 580f, 36f, TextAnchor.MiddleLeft);
            var flawTxt = Txt(row.transform, "• " + TalentFlawDescs[Mathf.Clamp(cand.flawIndex, 0, TalentFlawDescs.Length - 1)], 20, InkSoft, 114f, 16f, 580f, 56f, TextAnchor.UpperLeft);
            flawTxt.resizeTextForBestFit = true; flawTxt.resizeTextMinSize = 15; flawTxt.resizeTextMaxSize = ScaledFontSize(20);

            int hireCost = TalentHireCost(cand);
            bool full = talents.Count >= TalentRosterCapacity();
            var hBtn = Btn(row.transform, "HireTalent_" + i, full ? "编制已满" : ("签约 ¥" + hireCost), 24, cW - 230f, 44f, 212f, 82f,
                full ? new Color(0.82f, 0.80f, 0.75f) : Matcha, () => {
                    if (talents.Count >= TalentRosterCapacity()) { PlayErrorSfx(); Toast("在籍人才已满上限（" + TalentRosterCapacity() + "），可在「企业治理树」扩充编制"); return; }
                    if (coins < hireCost) { PlayErrorSfx(); Toast("签约金币不足，还差 ¥" + FormatShortfall(hireCost - coins)); return; }
                    coins -= hireCost;
                    cand.id = nextTalentId++;
                    talents.Add(cand);
                    talentPool.RemoveAt(idx);
                    SaveGame(); RefreshHUD();
                    PlayCoinSfx();
                    SpawnConfettiBurst(new Vector2(RootW * 0.5f, RootH * 0.5f), Matcha, Gold, 12);
                    OpenCompanyScreen();
                    Toast($"欢迎「{cand.name}」加入{companyName}！");
                });
            TInk(hBtn, full ? InkSoft : Color.white);
        }

        // 2. 在籍人才名册与培养中心
        float y2 = y1 - 22f - rosterH;
        var rosterCard = Rect("TalentRosterCard", content, cardX, y2, innerW, rosterH);
        Rounded(rosterCard, CardBg); SoftShadow(rosterCard);
        Txt(rosterCard.transform, $"在籍人才档案与培养（{talents.Count} / {TalentRosterCapacity()} 人）", 29, Ink, 32f, rosterH - 64f, 880f, 44f, TextAnchor.MiddleLeft);

        if (talents.Count == 0)
        {
            Txt(rosterCard.transform, "尚未签约专业人才。\n从上方猎聘阁签约后，可指派其【驻守车站】或【随线执勤】，亦可投入金币与科研点进修晋升至 Lv.5。",
                24, InkSoft, 32f, 40f, cW, rosterH - 130f, TextAnchor.MiddleCenter);
            return;
        }

        for (int i = 0; i < talents.Count; i++)
        {
            var t = talents[i];
            float ry = rosterH - 316f - i * 246f;
            var card = Rect("TalentItem_" + t.id, rosterCard.transform, 32f, ry, cW, 228f);
            Rounded(card, Color.Lerp(CardBg, Indigo, 0.05f));

            string xpStr = t.level >= 5 ? "已满级" : $"经验 {t.xp}/{TalentNextLevelXp(t.level)}";
            Txt(card.transform, $"{t.name} · {TalentRarityNames[t.rarity]}{TalentRoleNames[t.role]}  Lv.{t.level}  ({xpStr})", 25, Ink, 20f, 170f, 650f, 42f, TextAnchor.MiddleLeft);
            Txt(card.transform, TalentAssignmentLabel(t), 22, t.assignKind == 0 ? InkSoft : Indigo, 660f, 172f, cW - 680f, 38f, TextAnchor.MiddleRight);
            Txt(card.transform, "✓ " + TalentRoleEffectSummary(t), 23, Matcha, 20f, 128f, cW - 40f, 36f, TextAnchor.MiddleLeft);
            string flawLine = t.flawCleansed || t.level >= 3
                ? "✓ 初任磨合已突破，全特质纯正生效"
                : "• " + TalentFlawDescs[Mathf.Clamp(t.flawIndex, 0, TalentFlawDescs.Length - 1)];
            Txt(card.transform, flawLine, 22, t.flawCleansed || t.level >= 3 ? Matcha : Vermilion, 20f, 90f, cW - 40f, 34f, TextAnchor.MiddleLeft);

            float bw = (cW - 60f) * 0.25f;
            var assignBtn = Btn(card.transform, "Assign_" + t.id, "指派岗位", 22, 20f, 14f, bw, 66f, Indigo, () => OpenTalentAssignPopup(t));
            TInk(assignBtn, Color.white);

            bool maxLv = t.level >= 5;
            int trC = TalentTrainCoinCost(t), trRp = TalentTrainRpCost(t);
            var trainBtn = Btn(card.transform, "Train_" + t.id, maxLv ? "Lv.5 圆满" : $"进修 ¥{trC}/{trRp}RP", 22, 20f + bw + 12f, 14f, bw, 66f,
                maxLv ? Paper2 : Matcha, () => {
                    if (t.level >= 5) { Toast("该人才已修业至最高 Lv.5"); return; }
                    if (coins < trC) { PlayErrorSfx(); Toast("进修经费不足：需 ¥" + trC); return; }
                    if (rp < trRp) { PlayErrorSfx(); Toast("科研点不足：需 " + trRp + " RP"); return; }
                    coins -= trC; rp -= trRp;
                    GrantTalentXp(t, TalentNextLevelXp(t.level), false);
                    hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                    PlayTechSfx();
                    SaveGame(); RefreshHUD(); OpenCompanyScreen();
                    Toast($"「{t.name}」进修顺利，晋升为 Lv.{t.level}！");
                });
            TInk(trainBtn, maxLv ? InkSoft : Color.white);

            bool cleanable = TalentHasActiveFlaw(t);
            var cleanseBtn = Btn(card.transform, "Cleanse_" + t.id, cleanable ? "淬炼除弊 5RP" : "传记履历", 22, 20f + (bw + 12f) * 2f, 14f, bw, 66f,
                cleanable ? Gold : Paper2, () => {
                    if (cleanable)
                    {
                        if (rp < 5f) { PlayErrorSfx(); Toast("淬炼需 5 RP 科研指导点数"); return; }
                        rp -= 5f; t.flawCleansed = true;
                        hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                        PlayTechSfx(); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                        Toast($"已为「{t.name}」解除初任磨合短板！");
                    }
                    else OpenTalentBioPopup(t);
                });
            TInk(cleanseBtn, cleanable ? Color.white : Ink);

            var fireBtn = Btn(card.transform, "Fire_" + t.id, "礼送解聘", 22, 20f + (bw + 12f) * 3f, 14f, bw, 66f, Vermilion, () => {
                Confirm("礼送解聘", $"确定礼送「{t.name}」离任吗？", () => {
                    talents.Remove(t);
                    hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                    SaveGame(); OpenCompanyScreen();
                    Toast("已与「" + t.name + "」友好解约");
                });
            });
            TInk(fireBtn, Color.white);
        }
    }

    void OpenTalentBioPopup(SavedTalent t)
    {
        if (t == null) return;
        var p = OpenPopup(t.name + " · 人物小传", 520f, 820f);
        Txt(p.transform, $"{TalentRarityNames[t.rarity]} {TalentRoleNames[t.role]} · 当前 Lv.{t.level}\n岗前履历：{TalentAssignmentLabel(t)}", 24, Indigo, 36f, 368f, 748f, 70f, TextAnchor.UpperLeft);
        Txt(p.transform, t.bio, 24, Ink, 36f, 220f, 748f, 130f, TextAnchor.UpperLeft);
        Txt(p.transform, "专长效用：" + TalentRoleEffectSummary(t), 23, Matcha, 36f, 136f, 748f, 52f, TextAnchor.MiddleLeft);
        var ok = Btn(p.transform, "Ok", "合上档案", 25, 36f, 30f, 748f, 78f, Paper2, ClosePanel);
        TInk(ok, Ink);
    }

    void OpenTalentAssignPopup(SavedTalent t)
    {
        if (t == null) return;
        bool lineMode = t.role == 1 || t.role == 2; // 车长/精算师偏随线，也可手动切换
        var items = new List<(string label, int kind, int lineId, string stKey)>();
        items.Add(("返回总社待命", 0, 0, ""));
        foreach (var l in lines)
            items.Add(($"随线执勤 · {l.name}（{l.st.Count} 站）", 2, l.id, ""));
        for (int i = 0; i < stations.Count && items.Count < 42; i++)
            if (stations[i].built && !stations[i].closed)
                items.Add(($"驻守车站 · {stations[i].name}（{stations[i].region}）", 1, 0, StationSaveKey(stations[i])));

        float rowH = 84f;
        const float popW = 860f, scrollX = 28f, scrollW = 804f, scrollBottom = 114f;
        float h = Mathf.Clamp(252f + Mathf.Min(items.Count, 6) * rowH, 460f, 820f);
        var p = OpenPopup($"指派「{t.name}」({TalentRoleNames[t.role]})", h, popW, "talent_assign_" + t.id);
        string hint = lineMode ? "建议优先随线执勤；也可按需派驻核心大站。" : "建议优先驻守已开站的高客流站点或枢纽线。";
        Txt(p.transform, hint, 23, InkSoft, 34f, h - 122f, 792f, 38f, TextAnchor.MiddleLeft);

        float viewH = Mathf.Max(140f, h - 134f - scrollBottom);
        var view = Rect("AssignViewport", p.transform, scrollX, scrollBottom, scrollW, viewH);
        Img(view, new Color(1f, 1f, 1f, 0.01f));
        var mask = view.AddComponent<Mask>(); mask.showMaskGraphic = false;
        var scroll = view.AddComponent<ScrollRect>();
        scroll.horizontal = false; scroll.vertical = true;
        scroll.inertia = true; scroll.decelerationRate = 0.12f;
        scroll.scrollSensitivity = 96f; scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.viewport = view.GetComponent<RectTransform>();

        float contentH = Mathf.Max(viewH, items.Count * rowH + 8f);
        var contentGo = Rect("AssignContent", view.transform, 0f, 0f, scrollW, contentH);
        var content = contentGo.GetComponent<RectTransform>();
        content.anchorMin = content.anchorMax = new Vector2(0f, 1f);
        content.pivot = new Vector2(0f, 1f);
        content.anchoredPosition = Vector2.zero;
        scroll.content = content;

        for (int i = 0; i < items.Count; i++)
        {
            var entry = items[i];
            float y = contentH - (i + 1) * rowH + 4f;
            bool isCur = (entry.kind == 0 && t.assignKind == 0)
                || (entry.kind == 2 && t.assignKind == 2 && t.assignLineId == entry.lineId)
                || (entry.kind == 1 && t.assignKind == 1 && t.assignStationKey == entry.stKey);
            var btn = Btn(content, "Slot_" + i, entry.label + (isCur ? "  ✓ 当前" : ""), 22, 4f, y, scrollW - 8f, rowH - 12f,
                isCur ? Indigo : Paper2, () => {
                    t.assignKind = entry.kind;
                    t.assignLineId = entry.lineId;
                    t.assignStationKey = entry.stKey;
                    hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                    ClosePanel(true); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                    Toast($"已将「{t.name}」派往：{entry.label}");
                });
            TInk(btn, isCur ? Color.white : Ink);
        }
        var cancel = Btn(p.transform, "Cancel", "取消", 24, 34f, 26f, 792f, 74f, Paper2, ClosePanel);
        TInk(cancel, Ink);
    }
}
