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
    public static bool DevTestSaveSyncEnabled
    {
        get { return UnityEditor.EditorPrefs.GetBool(DevTestSaveSyncPref, true); }
        set { UnityEditor.EditorPrefs.SetBool(DevTestSaveSyncPref, value); }
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
            File.WriteAllText(BundledTestSaveAssetPath, prettyJson, new System.Text.UTF8Encoding(false));
            DevTestSaveDirty = true;
        }
        catch (Exception e)
        {
            Debug.LogError("[Railway2] 写入开发测试存档文件失败：" + e.Message);
        }
    }
#endif
    [Serializable] sealed class SavedStation { public string key; public int level, trafficEvent; public bool built, closed; public float riderBonus; public int facLevel; }
    [Serializable] sealed class SavedScenery { public string key; public bool found, unlocked; }
    [Serializable] sealed class SavedLine { public int id, trains, todayRevenue; public int skin = -1; public string name; public Color color; public List<string> stations; public int[] dispatch; public float fare, todayGross, todayMaintenance, todayTrack; public List<int> recent; }
    [Serializable] sealed class SavedVehicle { public int node, lineId; }
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
    }
    bool saveReady, saveBlocked; float autosaveTimer;
    string StationSaveKey(Station station) { return station.region + ":" + station.name; }
    string ScenerySaveKey(Scenery scenery) { return scenery.region + ":" + scenery.name; }
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
                PlayerPrefs.SetString(SaveKey, diskJson);
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
            foreach(var record in data.stations) if(record == null || !byKey.ContainsKey(record.key)) continue;
            foreach(var record in data.vehicles) if(record == null || record.node < 0 || record.node >= LocoNames.Length || (record.lineId != 0 && !ids.Contains(record.lineId))) continue;
            foreach(var record in data.stations) {
                if (record != null && byKey.TryGetValue(record.key, out int sIdx)) {
                    var station=stations[sIdx]; station.level=record.level; station.trafficEvent=record.trafficEvent; station.built=record.built; station.closed=record.closed; station.riderBonus=record.riderBonus; station.facLevel=record.facLevel;
                }
            }
            var sceneryByKey = new Dictionary<string,Scenery>(); foreach(var item in sceneries) sceneryByKey[ScenerySaveKey(item)] = item;
            foreach(var record in data.scenery) if(record != null && sceneryByKey.TryGetValue(record.key,out var item)) { item.found=record.found; item.unlocked=record.unlocked; }
            lines.Clear(); lines.AddRange(restoredLines);
            foreach (var l in restoredLines) foreach (int stIdx in l.st) if (stIdx >= 0 && stIdx < stations.Count) stations[stIdx].built = true;
            techDone.Clear(); techDone.AddRange(data.tech);
            researchVehicles.Clear(); foreach(var item in data.vehicles) researchVehicles.Add(new ResearchVehicle { node=item.node, lineId=item.lineId });
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
            activeContracts.Clear();
            if (data.contracts != null)
            {
                foreach (var mc in data.contracts)
                    if (mc != null && !mc.claimed) { activeContracts.Add(mc); nextContractId = Mathf.Max(nextContractId, mc.id + 1); }
            }
            companyTechDone.Clear();
            if (data.coTech != null) foreach (string ct in data.coTech) if (!string.IsNullOrEmpty(ct) && !companyTechDone.Contains(ct)) companyTechDone.Add(ct);
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
            EnsureCompanyInitialized();
            EnsureTalentsInitialized();
            SyncStoryProgression(false);
            SyncCulturalCollection(false);
            lastPolicyDay = data.lastPolicyDay; lastAdSubsidyDay = data.lastAdSubsidyDay; dailyTaskClaimedDay = data.dailyTaskClaimedDay;
            savedRegion=data.region;
            saveBlocked = false;
            saveReady = true;
            return true;
        } catch (Exception e) { saveBlocked=true; Debug.LogError("本地存档读取失败，已保留原档：" + e.Message); return false; }
    }
    string savedRegion;
    void SaveGame()
    {
        if(!saveReady || saveBlocked) return;
        // 防毁灭安全阀：如果当前内存中的 lines 异常为空，而磁盘原本存有线路，绝不盲目覆写，而是紧急自愈拉回
        if (lines.Count == 0 && PlayerPrefs.HasKey(SaveKey))
        {
            try {
                var existing = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
                if (existing != null && existing.lines != null && existing.lines.Count > 0)
                {
                    RestoreGame();
                    return;
                }
            } catch {}
        }
        try {
            var data=new SaveData { coins=coins, diamonds=diamonds, rp=rp, day=day, clock=dayClock, researchClock=researchClock, speed=timeSpeed, nextId=nextLineId, dailyBaseline=dailyStationBaseline, macau=macauUnlocked, guangdong=guangdongUnlocked, loan=loanOutstanding, music=musicEnabled, region=currentRegion, theme=(previewRestoreTheme >= 0 ? previewRestoreTheme : currentTheme), companyName=companyName, companySeal=companySeal, companyPolicy=companyPolicy, bondDaysLeft=bondDaysLeft, bondPrincipal=bondPrincipal, bondDailyDue=bondDailyDue, bondsRepaid=bondsRepaid, bondSubsidized=bondSubsidized, consultantId=consultantId, consultantDaysLeft=consultantDaysLeft, economyCycle=economyCycle, nextEconomyCycle=nextEconomyCycle, turboDaysLeft=turboDaysLeft, autoDispatchDaysLeft=autoDispatchDaysLeft, lastAwardDay=lastAwardDay, lastPolicyDay=lastPolicyDay, lastAdSubsidyDay=lastAdSubsidyDay, dailyTaskClaimedDay=dailyTaskClaimedDay, storyStage=storyStage, tutorialSkipped=tutorialSkipped, nextTalentId=nextTalentId, lastTalentRefreshDay=lastTalentRefreshDay, lastReadNoticeDay=lastReadNoticeDay, retiredVehiclesCount=retiredVehiclesCount };
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
                string reg = string.IsNullOrEmpty(check.region) ? "青屿" : DisplayPlaceAliases(check.region);
                int lc = check.lines != null ? check.lines.Count : 0;
                int tc = check.tech != null ? check.tech.Count : 0;
                return "第 " + Mathf.Max(1, check.day) + " 日" + reg + "测试存档（" + lc + " 条线路 · " + tc + " 项科技 · " + FormatCoins(check.coins) + " 资金）";
            }
        } catch {}
        return "预置测试存档";
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

        ClosePanel(true); CloseLineBubble(); ClosePage(); CancelDraft(); ClearFloaters();
        EndThemePreview(false);
        ClearUndoHistory();
        techCollapseDone = true; techSessionUnlocked.Clear();
        lines.Clear();
        BuildData();
        PlayerPrefs.SetString(SaveKey, raw);
        PlayerPrefs.Save();
        saveBlocked = false;
        saveReady = false;
        if (!RestoreGame()) { Toast("测试存档载入失败"); return false; }

        EnsureCompanyInitialized();
        if (!IsThemeAvailable(currentTheme)) currentTheme = 0;
        string dest = (!string.IsNullOrEmpty(savedRegion) && RegionUnlocked(savedRegion)) ? savedRegion : "青屿";
        FreezeRegionTraffic();
        currentRegion = dest;
        if (hkMapArt) hkMapArt.SetActive(currentRegion == "青屿");
        if (macauMapArt) macauMapArt.SetActive(currentRegion == "镜汀");
        if (guangdongMapArt) guangdongMapArt.SetActive(currentRegion == "锦川");
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
        if (regionTitle) regionTitle.text = DisplayPlaceAliases(currentRegion == "青屿" ? "青 屿" : currentRegion == "镜汀" ? "镜 汀" : "锦 川");
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
