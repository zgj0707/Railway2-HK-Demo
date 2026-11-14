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
        public int branch; // 0: 组织人力, 1: 商业地产, 2: 资本金融, 3: 运营品牌
        public int tier;   // 0..2
        public string title;
        public string desc;
        public int costCoins;
        public int costRp;
        public string[] parents;
    }

    readonly List<string> companyTechDone = new List<string>();
    readonly List<SavedTalent> talents = new List<SavedTalent>();
    readonly List<SavedTalent> talentPool = new List<SavedTalent>();
    int nextTalentId = 1;
    int lastTalentRefreshDay = -1;
    int techTreeTab = 0;       // 0: 工程技术史, 1: 企业治理树
    int companyBoardSubTab = 0; // 0: 董事会名宿, 1: 才俊招募与培养

    static readonly string[] CoTechBranchNames = { "组织人力", "商业地产", "资本金融", "运营品牌" };
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
        "曾在大屿山车厂担任见习调度员，手写时刻表从不出错一分。",
        "自小在骑楼老街长大，熟稔街坊商铺门道与早晚市客流规律。",
        "毕业于港大统计夜校，喜欢带着算盘与钢笔巡检每一段路轨。",
        "热衷拍摄湾区风物明信片，总能把普通站台办成旅途里的打卡地。",
        "出身三代铁路世家，箱底藏着一本泛黄的蒸汽机车保养札记。",
        "擅长协调跨区口岸站务，再拥挤的换乘通道也能理出秩序。"
    };

    static readonly CompanyTechNode[] CompanyTechNodes = {
        // 0: 组织人力
        new CompanyTechNode { key = "co_hr_1", branch = 0, tier = 0, title = "站务班组制", desc = "人才训练经验 +25%，并扩充 1 名全网人才编制上限。", costCoins = 400, costRp = 4, parents = Array.Empty<string>() },
        new CompanyTechNode { key = "co_hr_2", branch = 0, tier = 1, title = "董事议事厅", desc = "董事会常驻高管席位 +1，人才编制上限再 +1。", costCoins = 1200, costRp = 8, parents = new[] { "co_hr_1" } },
        new CompanyTechNode { key = "co_hr_3", branch = 0, tier = 1, title = "师徒传帮带", desc = "人才进修经费 -25%，每日自然实训经验 +50%。", costCoins = 2200, costRp = 12, parents = new[] { "co_hr_1" } },
        new CompanyTechNode { key = "co_hr_4", branch = 0, tier = 2, title = "名宿礼遇令", desc = "董事会高管负面特质削减 35%，常驻董事会席位再 +1。", costCoins = 5800, costRp = 20, parents = new[] { "co_hr_2", "co_hr_3" } },

        // 1: 商业地产
        new CompanyTechNode { key = "co_re_1", branch = 1, tier = 0, title = "月台商铺规划", desc = "全网新开车站与站前商业设施造价 -12%。", costCoins = 450, costRp = 4, parents = Array.Empty<string>() },
        new CompanyTechNode { key = "co_re_2", branch = 1, tier = 1, title = "骑楼连廊商圈", desc = "所有已落成站前物业的每日租金收益 +20%。", costCoins = 1600, costRp = 10, parents = new[] { "co_re_1" } },
        new CompanyTechNode { key = "co_re_3", branch = 1, tier = 1, title = "艺文茶室联名", desc = "名胜地标站点基础吸引客流 +10%。", costCoins = 2800, costRp = 14, parents = new[] { "co_re_1" } },
        new CompanyTechNode { key = "co_re_4", branch = 1, tier = 2, title = "湾区枢纽综合体", desc = "站前物业总租金再 +25%，全网车站吞吐上限 +10%。", costCoins = 7500, costRp = 22, parents = new[] { "co_re_2", "co_re_3" } },

        // 2: 资本金融
        new CompanyTechNode { key = "co_fin_1", branch = 2, tier = 0, title = "精算成本核查", desc = "全网铺轨造价 -8%，线路与车辆日常维保成本 -8%。", costCoins = 450, costRp = 4, parents = Array.Empty<string>() },
        new CompanyTechNode { key = "co_fin_2", branch = 2, tier = 1, title = "优质债券评级", desc = "企业基建债券可贷本金 +20%，每期还本付息压力 -10%。", costCoins = 1500, costRp = 9, parents = new[] { "co_fin_1" } },
        new CompanyTechNode { key = "co_fin_3", branch = 2, tier = 1, title = "市政招标快道", desc = "市政中标工程赏金 +20%，同时可挂牌标书上限 +1。", costCoins = 2600, costRp = 14, parents = new[] { "co_fin_1" } },
        new CompanyTechNode { key = "co_fin_4", branch = 2, tier = 2, title = "跨境银团授信", desc = "全网客票营收结算 +8%，线路与列车维保费再 -10%。", costCoins = 8000, costRp = 24, parents = new[] { "co_fin_2", "co_fin_3" } },

        // 3: 运营品牌
        new CompanyTechNode { key = "co_ops_1", branch = 3, tier = 0, title = "准点承诺徽章", desc = "全网机车平均运营车速 +6%，每日科研点产速 +15%。", costCoins = 500, costRp = 5, parents = Array.Empty<string>() },
        new CompanyTechNode { key = "co_ops_2", branch = 3, tier = 1, title = "统一导视系统", desc = "跨线换乘惩罚降低 12%，全网车票结算营收 +5%。", costCoins = 1500, costRp = 10, parents = new[] { "co_ops_1" } },
        new CompanyTechNode { key = "co_ops_3", branch = 3, tier = 1, title = "湾区假日套票", desc = "含名胜车站的线路客票收入额外 +15%。", costCoins = 2500, costRp = 14, parents = new[] { "co_ops_1" } },
        new CompanyTechNode { key = "co_ops_4", branch = 3, tier = 2, title = "金字铁道招牌", desc = "全网基础客流需求 +10%，大湾区年度铁道大赏赏金 +25%。", costCoins = 7200, costRp = 22, parents = new[] { "co_ops_2", "co_ops_3" } }
    };

    bool HasCoTech(string key) => companyTechDone.Contains(key);
    int CoTechTotalCount() => CompanyTechNodes.Length;
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
    float CoTechTrainXpMult() => (HasCoTech("co_hr_1") ? 1.25f : 1f) * (HasCoTech("co_hr_3") ? 1.50f : 1f);
    float CoTechTrainCostMult() => HasCoTech("co_hr_3") ? 0.75f : 1f;
    float CoTechOpenCostMult() => HasCoTech("co_re_1") ? 0.88f : 1f;
    float CoTechCommercialMult() => (HasCoTech("co_re_2") ? 1.20f : 1f) * (HasCoTech("co_re_4") ? 1.25f : 1f);
    float CoTechSceneryDemandMult() => HasCoTech("co_re_3") ? 1.10f : 1f;
    float CoTechStationCapMult() => HasCoTech("co_re_4") ? 1.10f : 1f;
    float CoTechTrackBuildMult() => HasCoTech("co_fin_1") ? 0.92f : 1f;
    float CoTechMaintMult() => (HasCoTech("co_fin_1") ? 0.92f : 1f) * (HasCoTech("co_fin_4") ? 0.90f : 1f);
    float CoTechBondCapMult() => HasCoTech("co_fin_2") ? 1.20f : 1f;
    float CoTechBondDueMult() => HasCoTech("co_fin_2") ? 0.90f : 1f;
    int CoTechContractCap() => HasCoTech("co_fin_3") ? 3 : 2;
    float CoTechContractBonusMult() => HasCoTech("co_fin_3") ? 1.20f : 1f;
    float CoTechSpeedMult() => HasCoTech("co_ops_1") ? 1.06f : 1f;
    float CoTechRpMult() => HasCoTech("co_ops_1") ? 1.15f : 1f;
    float CoTechTransferMult() => HasCoTech("co_ops_2") ? 0.88f : 1f;
    float CoTechRevenueMult() => (HasCoTech("co_ops_2") ? 1.05f : 1f) * (HasCoTech("co_fin_4") ? 1.08f : 1f);
    float CoTechTourismLineMult() => HasCoTech("co_ops_3") ? 1.15f : 1f;
    float CoTechBaseDemandMult() => HasCoTech("co_ops_4") ? 1.10f : 1f;
    float CoTechAwardMult() => HasCoTech("co_ops_4") ? 1.25f : 1f;

    float MitigateCon(float rawDelta)
    {
        // 例：rawDelta = 0.10f 表示负面增加 10%，减免 35% 后变为 +6.5%；若为 -0.05f 同样缩窄幅度
        return 1f + rawDelta * CoTechPartnerConMitigation();
    }

    // ---------- 人才编制、培养、加值计算 ----------
    int TalentRosterCapacity()
    {
        return 2 + (HasCoTech("co_hr_1") ? 1 : 0) + (HasCoTech("co_hr_2") ? 1 : 0) + (HasCoTech("co_hr_4") ? 1 : 0)
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

    // ---------- M2 UI A: 企业治理树（挂在科技屏的双页签中） ----------
    void OpenCompanyTechScreen()
    {
        techTreeTab = 1;
        var pg = OpenPage("企业治理树", "公司专属管理与商业科技树，提升董事会编制、物业租金、融资评级与全网效益。", "PageArt/company_watercolor", "company_tech");
        BuildTechModeSwitchBar(pg.transform, true);

        StatBox(pg.transform, "公司科研进度", companyTechDone.Count + " / " + CompanyTechNodes.Length, 28, PgH - 420, 320, 148, Matcha);
        StatBox(pg.transform, "可用经费", "¥" + FormatCoins(coins), 380, PgH - 420, 320, 148, Gold);
        StatBox(pg.transform, "科研点数", Mathf.FloorToInt(rp) + " RP", 732, PgH - 420, 320, 148, Indigo);

        float totalH = 2120f;
        var content = FleetScroll(pg.transform, 36f, PgH - 446f, totalH);
        float topY = content.sizeDelta.y;
        float cardW = W - 72f;
        Color[] branchCols = { Matcha, Gold, Indigo, Vermilion };

        float cursorY = topY - 12f;
        for (int b = 0; b < CoTechBranchNames.Length; b++)
        {
            Color bCol = branchCols[b];
            float secH = 486f;
            cursorY -= secH;
            var sec = Rect("CoBranch_" + b, content, 4f, cursorY, cardW - 8f, secH - 18f);
            Rounded(sec, CardBg); SoftShadow(sec);
            Img(Rect("Accent", sec.transform, 0, 26, 7, secH - 70), bCol, spriteRound).raycastTarget = false;
            Txt(sec.transform, "分支 " + (b + 1) + " · " + CoTechBranchNames[b], 30, Ink, 28f, secH - 76f, 600f, 44f, TextAnchor.MiddleLeft);

            var branchNodes = Array.FindAll(CompanyTechNodes, n => n.branch == b);
            float colW = (cardW - 80f) * 0.5f;
            // T0 (左上) -> T1a (右上) / T1b (左下) -> T2 (右下)
            Vector2[] slots = {
                new Vector2(28f, 222f),
                new Vector2(44f + colW, 222f),
                new Vector2(28f, 24f),
                new Vector2(44f + colW, 24f)
            };
            for (int nIdx = 0; nIdx < branchNodes.Length && nIdx < slots.Length; nIdx++)
            {
                var node = branchNodes[nIdx];
                bool done = HasCoTech(node.key);
                bool ready = CanUnlockCoTech(node);
                Vector2 sp = slots[nIdx];
                Color tileBg = done ? Color.Lerp(CardBg, bCol, 0.20f)
                    : ready ? Color.Lerp(CardBg, Gold, 0.08f)
                    : Color.Lerp(CardBg, Ink, 0.06f);

                var btn = Btn(sec.transform, "CoNode_" + node.key, "", 22, sp.x, sp.y, colW, 180f, tileBg, () => {
                    TapCompanyTechNode(node);
                });
                var nTitle = Txt(btn.transform, (node.tier == 2 ? "◆ " : "") + node.title, 25, done ? bCol : ready ? Ink : InkSoft, 16f, 130f, colW - 196f, 38f, TextAnchor.MiddleLeft);
                FitSingleLine(nTitle, 25); nTitle.raycastTarget = false;
                string badgeTxt = done ? "已掌握 ✓" : ready ? ("¥" + FormatCoins(node.costCoins) + " · " + node.costRp + "RP") : "前置不足";
                var nState = Txt(btn.transform, badgeTxt, 22, done ? Matcha : ready ? Vermilion : InkSoft, colW - 186f, 132f, 172f, 34f, TextAnchor.MiddleRight);
                FitSingleLine(nState, 22); nState.raycastTarget = false;
                var nDesc = Txt(btn.transform, node.desc, 22, InkSoft, 16f, 14f, colW - 32f, 108f, TextAnchor.UpperLeft);
                nDesc.raycastTarget = false;
            }
        }
    }

    void BuildTechModeSwitchBar(Transform parent, bool isCorporateActive)
    {
        float y = PgH - 256f;
        var bEng = Btn(parent, "SwitchEngTech", "工程技术史 (" + techDone.Count + "/" + historyNodes.Count + ")", 24, 28f, y, 496f, 68f,
            !isCorporateActive ? Indigo : CardBg, () => { techTreeTab = 0; OpenTechScreen(0); });
        TInk(bEng, !isCorporateActive ? Color.white : Ink);
        if (isCorporateActive) SoftShadow(bEng.gameObject);

        var bCo = Btn(parent, "SwitchCoTech", "企业治理树 (" + companyTechDone.Count + "/" + CompanyTechNodes.Length + ")", 24, 556f, y, 496f, 68f,
            isCorporateActive ? Matcha : CardBg, () => { OpenCompanyTechScreen(); });
        TInk(bCo, isCorporateActive ? Color.white : Ink);
        if (!isCorporateActive) SoftShadow(bCo.gameObject);
    }

    void TapCompanyTechNode(CompanyTechNode node)
    {
        if (node == null || HasCoTech(node.key)) { Toast("该企业治理科技已掌握"); return; }
        if (!CanUnlockCoTech(node))
        {
            PlayErrorSfx();
            Toast("请先研习该分支前置管理项");
            return;
        }
        if (coins < node.costCoins)
        {
            PlayErrorSfx();
            Toast("经费不足：需 ¥" + FormatCoins(node.costCoins));
            return;
        }
        if (rp < node.costRp)
        {
            PlayErrorSfx();
            Toast("科研点不足：需 " + node.costRp + " RP");
            return;
        }
        BeginUndo("解锁企业治理科技");
        coins -= node.costCoins;
        rp -= node.costRp;
        companyTechDone.Add(node.key);
        EnsureCompanyInitialized();
        hasForecastSignature = false;
        operatingRates.Clear();
        RecalcIncome();
        EndUndo();
        PlayTechSfx();
        TriggerHaptic(22);
        SpawnConfettiBurst(new Vector2(RootW * 0.5f, RootH * 0.55f), Gold, Matcha, 14);
        SaveGame();
        RefreshHUD();
        OpenCompanyTechScreen();
        Toast("企业治理升级：「" + node.title + "」生效！");
    }

    // ---------- M2 UI B: 人才招募与培养页签（嵌入公司->高管与人才页） ----------
    void BuildTalentManagementView(Transform page, float cardW)
    {
        EnsureTalentsInitialized();
        float poolH = 668f;
        float rosterH = Mathf.Max(280f, talents.Count * 246f + 130f);
        float totalH = poolH + rosterH + 60f;
        var content = FleetScroll(page, 36f, PgH - 350f, totalH);
        float topY = content.sizeDelta.y;
        float cardX = 4f, innerW = cardW - 8f, cW = innerW - 64f;

        // 1. 人才市场（每日免费刷新 / ¥150 即刻招募）
        float y1 = topY - 12f - poolH;
        var poolCard = Rect("TalentMarketCard", content, cardX, y1, innerW, poolH);
        Rounded(poolCard, CardBg); SoftShadow(poolCard); JournalTape(poolCard.transform, innerW * 0.5f - 72f, poolH - 17f, 144f);
        Txt(poolCard.transform, "湾区猎聘阁 · 今日待聘名才", 29, Ink, 32f, poolH - 64f, 560f, 44f, TextAnchor.MiddleLeft);
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
            float ry = poolH - 246f - i * 174f;
            var row = Rect("Cand_" + i, poolCard.transform, 32f, ry, cW, 160f);
            Rounded(row, Color.Lerp(CardBg, Ink, 0.04f));
            Color rCol = cand.rarity == 3 ? Gold : cand.rarity == 2 ? Indigo : Matcha;
            var badge = Rect("Rarity", row.transform, 16f, 38f, 84f, 84f);
            Rounded(badge, rCol);
            Txt(badge.transform, TalentRarityNames[cand.rarity] + "\n" + TalentRoleNames[cand.role].Substring(0, 2), 22, Color.white, 0f, 0f, 84f, 84f, TextAnchor.MiddleCenter);

            Txt(row.transform, $"{cand.name} · Lv.{cand.level} {TalentRoleNames[cand.role]}", 25, Ink, 114f, 112f, 560f, 38f, TextAnchor.MiddleLeft);
            Txt(row.transform, "✓ " + TalentRoleEffectSummary(cand), 23, Matcha, 114f, 70f, 580f, 36f, TextAnchor.MiddleLeft);
            Txt(row.transform, "• " + TalentFlawDescs[Mathf.Clamp(cand.flawIndex, 0, TalentFlawDescs.Length - 1)], 22, InkSoft, 114f, 14f, 580f, 50f, TextAnchor.UpperLeft);

            int hireCost = TalentHireCost(cand);
            bool full = talents.Count >= TalentRosterCapacity();
            var hBtn = Btn(row.transform, "HireTalent_" + i, full ? "编制已满" : ("签约 ¥" + hireCost), 24, cW - 230f, 39f, 212f, 82f,
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
        float h = Mathf.Clamp(220f + Mathf.Min(items.Count, 6) * rowH, 460f, 820f);
        var p = OpenPopup($"指派「{t.name}」({TalentRoleNames[t.role]})", h, 860f);
        string hint = lineMode ? "建议优先随线执勤；也可按需派驻核心大站。" : "建议优先驻守已开站的高客流站点或枢纽线。";
        Txt(p.transform, hint, 24, InkSoft, 34f, h - 118f, 792f, 38f, TextAnchor.MiddleLeft);

        float viewH = h - 220f;
        var content = FleetScroll(p.transform, 30f, 122f + viewH, Mathf.Max(viewH, items.Count * rowH));
        float topY = content.sizeDelta.y;
        for (int i = 0; i < items.Count; i++)
        {
            var entry = items[i];
            float y = topY - (i + 1) * rowH + 6f;
            bool isCur = (entry.kind == 0 && t.assignKind == 0)
                || (entry.kind == 2 && t.assignKind == 2 && t.assignLineId == entry.lineId)
                || (entry.kind == 1 && t.assignKind == 1 && t.assignStationKey == entry.stKey);
            var btn = Btn(content, "Slot_" + i, entry.label + (isCur ? "  ✓ 当前" : ""), 22, 4f, y, content.sizeDelta.x - 8f, rowH - 12f,
                isCur ? Indigo : CardBg, () => {
                    t.assignKind = entry.kind;
                    t.assignLineId = entry.lineId;
                    t.assignStationKey = entry.stKey;
                    hasForecastSignature = false; operatingRates.Clear(); RecalcIncome();
                    ClosePanel(true); SaveGame(); RefreshHUD(); OpenCompanyScreen();
                    Toast($"已将「{t.name}」派往：{entry.label}");
                });
            TInk(btn, isCur ? Color.white : Ink);
        }
        var cancel = Btn(p.transform, "Cancel", "取消", 24, 34f, 28f, 792f, 74f, Paper2, ClosePanel);
        TInk(cancel, Ink);
    }
}
