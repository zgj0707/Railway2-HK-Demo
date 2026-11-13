using System;
using System.Collections.Generic;
using UnityEngine;

public partial class DemoBootstrap
{
    const string SaveKey = "railway2_local_v1";
    [Serializable] sealed class SavedStation { public string key; public int level, trafficEvent; public bool built, closed; public float riderBonus; public int facLevel; }
    [Serializable] sealed class SavedScenery { public string key; public bool found, unlocked; }
    [Serializable] sealed class SavedLine { public int id, trains, todayRevenue; public int skin = -1; public string name; public Color color; public List<string> stations; public int[] dispatch; public float fare, todayGross, todayMaintenance, todayTrack; public List<int> recent; }
    [Serializable] sealed class SavedVehicle { public int node, lineId; }
    [Serializable] sealed class SaveData {
        public int version=1, day, speed, nextId, dailyBaseline, theme; public float coins, diamonds, rp, clock, researchClock;
        public bool macau, guangdong, loan, music; public string region;
        public List<SavedStation> stations = new List<SavedStation>(); public List<SavedScenery> scenery = new List<SavedScenery>();
        public List<SavedLine> lines = new List<SavedLine>(); public List<SavedVehicle> vehicles = new List<SavedVehicle>();
        public List<string> tech = new List<string>(), story = new List<string>(); public List<int> skins = new List<int>();
        public List<int> themes = new List<int>();
        public List<string> adKeys = new List<string>(); public List<int> adCounts = new List<int>();
        public List<int> trialThemes = new List<int>(); public List<long> trialEnds = new List<long>();
        public string companyName = "湾区铁道社";
        public int companySeal, companyPolicy, bondDaysLeft, lastPolicyDay = -1, lastAdSubsidyDay = -1, dailyTaskClaimedDay = -1;
        public int bondPrincipal, bondDailyDue, bondsRepaid, consultantId = -1, consultantDaysLeft, economyCycle, nextEconomyCycle = 1, turboDaysLeft, autoDispatchDaysLeft, lastAwardDay;
        public bool bondSubsidized;
        public List<int> boardPartners = new List<int>();
        public List<MunicipalContract> contracts = new List<MunicipalContract>();
    }
    bool saveReady, saveBlocked; float autosaveTimer;
    string StationSaveKey(Station station) { return station.region + ":" + station.name; }
    string ScenerySaveKey(Scenery scenery) { return scenery.region + ":" + scenery.name; }
    bool RestoreGame()
    {
        if (!PlayerPrefs.HasKey(SaveKey)) return false;
        if (stations == null || stations.Count == 0) BuildData();
        try {
            var data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
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
            companyName = string.IsNullOrWhiteSpace(data.companyName) ? "湾区铁道社" : data.companyName.Trim();
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
            EnsureCompanyInitialized();
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
            var data=new SaveData { coins=coins, diamonds=diamonds, rp=rp, day=day, clock=dayClock, researchClock=researchClock, speed=timeSpeed, nextId=nextLineId, dailyBaseline=dailyStationBaseline, macau=macauUnlocked, guangdong=guangdongUnlocked, loan=loanOutstanding, music=musicEnabled, region=currentRegion, theme=currentTheme, companyName=companyName, companySeal=companySeal, companyPolicy=companyPolicy, bondDaysLeft=bondDaysLeft, bondPrincipal=bondPrincipal, bondDailyDue=bondDailyDue, bondsRepaid=bondsRepaid, bondSubsidized=bondSubsidized, consultantId=consultantId, consultantDaysLeft=consultantDaysLeft, economyCycle=economyCycle, nextEconomyCycle=nextEconomyCycle, turboDaysLeft=turboDaysLeft, autoDispatchDaysLeft=autoDispatchDaysLeft, lastAwardDay=lastAwardDay, lastPolicyDay=lastPolicyDay, lastAdSubsidyDay=lastAdSubsidyDay, dailyTaskClaimedDay=dailyTaskClaimedDay };
            data.boardPartners.AddRange(boardPartners);
            data.contracts.AddRange(activeContracts);
            foreach(var s in stations) data.stations.Add(new SavedStation { key=StationSaveKey(s), level=s.level, trafficEvent=s.trafficEvent, built=s.built, closed=s.closed, riderBonus=s.riderBonus, facLevel=s.facLevel });
            foreach(var s in sceneries) data.scenery.Add(new SavedScenery { key=ScenerySaveKey(s), found=s.found, unlocked=s.unlocked });
            foreach(var l in lines) { var record=new SavedLine { id=l.id, name=l.name, color=l.color, skin=l.skin, trains=l.trains, dispatch=(int[])l.dispatch.Clone(), fare=l.fare, todayGross=l.todayGross, todayMaintenance=l.todayMaintenance, todayTrack=l.todayTrack, todayRevenue=l.todayRevenue, recent=new List<int>(l.recent), stations=new List<string>() }; foreach(int i in l.st) record.stations.Add(StationSaveKey(stations[i])); data.lines.Add(record); }
            foreach(var v in researchVehicles) data.vehicles.Add(new SavedVehicle { node=v.node, lineId=v.lineId });
            data.tech.AddRange(techDone); data.story.AddRange(storySeen); data.skins.AddRange(ownedLineSkins);
            data.themes.AddRange(ownedThemes);
            foreach (var kv in adWatchCounts) { data.adKeys.Add(kv.Key); data.adCounts.Add(kv.Value); }
            foreach (var kv in themeTrialUntil) { data.trialThemes.Add(kv.Key); data.trialEnds.Add(kv.Value); }
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(data)); PlayerPrefs.Save();
        } catch(Exception e) { Debug.LogError("本地存档保存失败：" + e.Message); }
    }
    void TickAutosave() { autosaveTimer += Time.unscaledDeltaTime; if (autosaveTimer >= 5f) { autosaveTimer=0; SaveGame(); } }
    void OnApplicationPause(bool paused) { if(paused) SaveGame(); }
    void OnApplicationFocus(bool focused) { if (!focused) { if (lineInsertActive) CancelLineInsert(); SaveGame(); } }
    void OnApplicationQuit() { SaveGame(); }
    void OnDisable() { SaveGame(); }
}
