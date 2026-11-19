using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public sealed class TapTapSdkBridgeHost : MonoBehaviour
{
    public static TapTapSdkBridgeHost Instance { get; private set; }
    public DemoBootstrap Owner { get; set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OnJsAdCallback(string json)
    {
        Owner?.HandleTapTapAdCallback(json);
    }

    public void OnJsCloudSaveUploadCallback(string json)
    {
        Owner?.HandleTapTapCloudUploadCallback(json);
    }

    public void OnJsCloudSaveFetchCallback(string json)
    {
        Owner?.HandleTapTapCloudFetchCallback(json);
    }

    public void OnJsSubmitScoreCallback(string json)
    {
        Owner?.HandleTapTapSubmitScoreCallback(json);
    }

    public void OnJsLeaderboardCallback(string json)
    {
        Owner?.HandleTapTapLeaderboardCallback(json);
    }
}

public partial class DemoBootstrap
{
    const string CloudMirrorPrefsKey = "railway2_taptap_cloud_mirror_v1";
    const string CloudMirrorMetaPrefsKey = "railway2_taptap_cloud_mirror_meta_v1";
    const float CloudAutoUploadIntervalSeconds = 60f;
    const float AdWatchdogTimeoutSeconds = 18f;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern int TapTapJS_IsAvailable();
    [DllImport("__Internal")] static extern string TapTapJS_GetUserId();
    [DllImport("__Internal")] static extern void TapTapJS_ShowRewardVideoAd(string requestId, string adKey, string itemName);
    [DllImport("__Internal")] static extern void TapTapJS_CloudSaveUpload(string requestId, string saveJson, int day, int timestamp, int railScore, int stationsBuilt, int linesCount, int ridership, string companyMetaJson);
    [DllImport("__Internal")] static extern void TapTapJS_CloudSaveFetch(string requestId);
    [DllImport("__Internal")] static extern void TapTapJS_SubmitScore(string requestId, int railScore, int stationsBuilt, int linesCount, int ridership, int day, string companyMetaJson);
    [DllImport("__Internal")] static extern void TapTapJS_FetchLeaderboard(string requestId, int topN);
#endif

    [Serializable]
    sealed class JsAdResult
    {
        public string requestId, adKey, errorCode, message;
        public bool success, manualClose;
    }

    [Serializable]
    sealed class JsCloudUploadResult
    {
        public string requestId, reason;
        public bool ok;
        public int day, timestamp;
    }

    [Serializable]
    sealed class JsCloudFetchResult
    {
        public string requestId, saveJson, reason;
        public bool ok;
        public int day, timestamp;
    }

    [Serializable]
    sealed class JsScoreSubmitResult
    {
        public string requestId, reason;
        public bool ok;
    }

    [Serializable]
    sealed class JsRankItem
    {
        public int rank, seal, score, stationsBuilt, linesCount, dailyRidership, day;
        public string userId, nickname, companyName, region;
        public bool isMe;
    }

    [Serializable]
    sealed class JsLeaderboardResult
    {
        public string requestId, reason;
        public bool ok, fromSdk;
        public int myRank, totalPlayers;
        public JsRankItem[] entries;
    }

    [Serializable]
    sealed class CloudCompanyMeta
    {
        public string companyName, region;
        public int seal, dailyProfit;
    }

    [Serializable]
    sealed class LocalCloudMirrorMeta
    {
        public int day, timestamp, railScore;
    }

    sealed class CloudRankEntry
    {
        public int rank, seal, score, stationsBuilt, linesCount, dailyRidership, day;
        public string userId, nickname, companyName, region;
        public bool isMe;
    }

    TapTapSdkBridgeHost sdkHost;
    int sdkRequestSeq;

    // 激励视频状态与单次结算守卫
    bool adInFlight, adSettled, adMutedBgmTemporarily;
    string activeAdRequestId = "";
    float adWatchdogTimer;
    Action pendingAdGrantedCallback;

    // 云存档状态
    bool cloudSaveUploadInFlight, cloudSaveFetchInFlight, cloudStartupChecked;
    float lastCloudUploadRealtime = -999f;
    int lastCloudSyncedDay = -1;
    string cloudSaveStatusText = "未同步（本地档保护中）";
    string activeCloudUploadReqId = "", activeCloudFetchReqId = "";
    Action<bool, string> pendingCloudUploadCallback;
    Action<bool, string, int, long> pendingCloudFetchCallback;

    // 排行榜状态
    bool cloudRankLoading, cloudRankFromSdk;
    int cloudMyRank, cloudTotalPlayers;
    string cloudRankStatusText = "尚未拉取云端排行";
    string activeRankFetchReqId = "";
    readonly List<CloudRankEntry> cloudRankEntries = new List<CloudRankEntry>();
    Action pendingLeaderboardUiRefresh;

    void EnsureTapTapSdkBridge()
    {
        if (sdkHost == null)
        {
            var existing = GameObject.Find("TapTapSdkBridgeHost");
            if (existing != null) sdkHost = existing.GetComponent<TapTapSdkBridgeHost>() ?? existing.AddComponent<TapTapSdkBridgeHost>();
            if (sdkHost == null)
            {
                var go = new GameObject("TapTapSdkBridgeHost");
                sdkHost = go.AddComponent<TapTapSdkBridgeHost>();
            }
        }
        sdkHost.Owner = this;
    }

    string NextSdkRequestId(string prefix)
    {
        sdkRequestSeq++;
        return prefix + "_" + sdkRequestSeq;
    }

    bool IsTapTapHostSdkAvailable()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { return TapTapJS_IsAvailable() != 0; } catch { return false; }
#else
        return false;
#endif
    }

    string CurrentTapTapUserId()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { return TapTapJS_GetUserId() ?? ""; } catch { return ""; }
#else
        return "local_dev_uid";
#endif
    }

    string BuildCompanyMetaJson()
    {
        return JsonUtility.ToJson(new CloudCompanyMeta {
            companyName = string.IsNullOrWhiteSpace(companyName) ? "山海铁道社" : companyName.Trim(),
            region = DisplayPlaceAliases(currentRegion),
            seal = companySeal,
            dailyProfit = Mathf.RoundToInt(income)
        });
    }

    int CurrentDailyRidership()
    {
        if (traffic != null && traffic.totalPassengers > 0f) return Mathf.RoundToInt(traffic.totalPassengers);
        if (hasYesterdayReport && yesterdayReport != null && yesterdayReport.passengers > 0f) return Mathf.RoundToInt(yesterdayReport.passengers);
        return Mathf.Max(0, DailyConnectedCount() * 120);
    }

    // ---------- 1. TapTap 激励视频广告调度 ----------
    void TickTapTapSdkBridge()
    {
        if (adInFlight && !adSettled)
        {
            adWatchdogTimer -= Time.unscaledDeltaTime;
            if (adWatchdogTimer <= 0f)
            {
                adSettled = true;
                adInFlight = false;
                RestoreAudioAfterRewardedAd();
                pendingAdGrantedCallback = null;
                Toast("广告响应超时，请稍后再试");
            }
        }
    }

    void PauseAudioForRewardedAd()
    {
        adMutedBgmTemporarily = false;
        try
        {
            if (music && music.isPlaying)
            {
                music.Pause();
                adMutedBgmTemporarily = true;
            }
            if (musicB && musicB.isPlaying)
            {
                musicB.Pause();
                adMutedBgmTemporarily = true;
            }
        }
        catch {}
    }

    void RestoreAudioAfterRewardedAd()
    {
        if (!adMutedBgmTemporarily) return;
        adMutedBgmTemporarily = false;
        try { SyncRegionalMusic(true); } catch {}
    }

    void RequestTapTapRewardedAd(string adKey, string itemName, Action onGranted)
    {
        EnsureTapTapSdkBridge();
        if (adInFlight)
        {
            Toast("赞助短片正在加载中，请稍候");
            return;
        }
        adInFlight = true;
        adSettled = false;
        adWatchdogTimer = AdWatchdogTimeoutSeconds;
        activeAdRequestId = NextSdkRequestId("ad");
        pendingAdGrantedCallback = onGranted;
        PauseAudioForRewardedAd();

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            TapTapJS_ShowRewardVideoAd(activeAdRequestId, adKey ?? "", itemName ?? "");
            return;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[TapTapSDK] 调用 WebGL 激励广告失败：" + e.Message);
        }
#endif
        // Editor 或原生非 WebGL 环境下弹出统一预览确认通道（同样受单次结算守卫控制）
        string editorJson = JsonUtility.ToJson(new JsAdResult {
            requestId = activeAdRequestId,
            adKey = adKey ?? "",
            success = true,
            manualClose = false,
            errorCode = "",
            message = "editor/standalone fallback"
        });
        HandleTapTapAdCallback(editorJson);
    }

    public void HandleTapTapAdCallback(string json)
    {
        if (adSettled || !adInFlight) return;
        JsAdResult res = null;
        try { res = JsonUtility.FromJson<JsAdResult>(json); } catch {}
        if (res != null && !string.IsNullOrEmpty(activeAdRequestId) && !string.IsNullOrEmpty(res.requestId) && res.requestId != activeAdRequestId)
            return;

        adSettled = true;
        adInFlight = false;
        RestoreAudioAfterRewardedAd();

        var granted = pendingAdGrantedCallback;
        pendingAdGrantedCallback = null;

        if (res != null && res.success && !res.manualClose && res.message != "embed manual close")
        {
            granted?.Invoke();
            TryAutoSyncTapTapCloudSave(true);
            return;
        }

        if (res != null && (res.manualClose || res.message == "embed manual close"))
        {
            Toast("未完整观看短片，暂未发放奖励");
            return;
        }

        string errHint = res != null && !string.IsNullOrEmpty(res.message) ? res.message : "赞助短片暂无填充";
        Toast("广告加载失败：" + errHint);
    }

    // ---------- 2. TapTap 排行榜上报与拉取 ----------
    void SubmitTapTapLeaderboardScore(bool silent = true)
    {
        EnsureTapTapSdkBridge();
        if (!saveReady || lines == null) return;

        int score = CalculatePlayerRailScore();
        int built = DailyConnectedCount();
        int lc = lines.Count;
        int ridership = CurrentDailyRidership();
        string metaJson = BuildCompanyMetaJson();
        string reqId = NextSdkRequestId("score");

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            TapTapJS_SubmitScore(reqId, score, built, lc, ridership, Mathf.Max(1, day), metaJson);
            return;
        }
        catch (Exception e)
        {
            if (!silent) Debug.LogWarning("[TapTapSDK] 上报排行榜分数异常：" + e.Message);
        }
#endif
        if (!silent) Toast("已记录本地评分：" + score.ToString("N0"));
    }

    public void HandleTapTapSubmitScoreCallback(string json)
    {
        try
        {
            var res = JsonUtility.FromJson<JsScoreSubmitResult>(json);
            if (res != null && res.ok)
            {
                Debug.Log("[TapTapSDK] 排行榜分数已实时同步");
            }
        }
        catch {}
    }

    void FetchTapTapLeaderboard(Action onUpdated = null)
    {
        EnsureTapTapSdkBridge();
        if (onUpdated != null) pendingLeaderboardUiRefresh = onUpdated;
        if (cloudRankLoading) return;

        cloudRankLoading = true;
        cloudRankStatusText = "正在连接 TapTap 云端榜单...";
        activeRankFetchReqId = NextSdkRequestId("rank");

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            TapTapJS_FetchLeaderboard(activeRankFetchReqId, 20);
            return;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[TapTapSDK] 拉取排行榜异常：" + e.Message);
        }
#endif
        cloudRankLoading = false;
        cloudRankFromSdk = false;
        cloudRankStatusText = IsTapTapHostSdkAvailable() ? "云端榜单暂未返回" : "离线/本地模式（已展示本地战绩与同行榜）";
        var cb = pendingLeaderboardUiRefresh;
        pendingLeaderboardUiRefresh = null;
        cb?.Invoke();
    }

    public void HandleTapTapLeaderboardCallback(string json)
    {
        cloudRankLoading = false;
        JsLeaderboardResult res = null;
        try { res = JsonUtility.FromJson<JsLeaderboardResult>(json); } catch {}
        if (res == null || (!string.IsNullOrEmpty(activeRankFetchReqId) && !string.IsNullOrEmpty(res.requestId) && res.requestId != activeRankFetchReqId))
        {
            var cbFail = pendingLeaderboardUiRefresh;
            pendingLeaderboardUiRefresh = null;
            cbFail?.Invoke();
            return;
        }

        cloudRankEntries.Clear();
        cloudRankFromSdk = res.ok && res.fromSdk;
        cloudMyRank = res.myRank;
        cloudTotalPlayers = res.totalPlayers;

        if (res.ok && res.entries != null)
        {
            for (int i = 0; i < res.entries.Length; i++)
            {
                var item = res.entries[i];
                if (item == null || item.score <= 0) continue;
                cloudRankEntries.Add(new CloudRankEntry {
                    rank = item.rank > 0 ? item.rank : (i + 1),
                    userId = item.userId ?? "",
                    nickname = string.IsNullOrEmpty(item.nickname) ? ("路局 #" + (item.userId ?? "?")) : item.nickname,
                    companyName = string.IsNullOrEmpty(item.companyName) ? "" : item.companyName,
                    region = string.IsNullOrEmpty(item.region) ? "" : item.region,
                    seal = item.seal,
                    score = item.score,
                    stationsBuilt = item.stationsBuilt,
                    linesCount = item.linesCount,
                    dailyRidership = item.dailyRidership,
                    day = Mathf.Max(1, item.day),
                    isMe = item.isMe
                });
            }
            cloudRankStatusText = cloudTotalPlayers > 0
                ? $"TapTap 云端实时榜 · 共 {cloudTotalPlayers} 位路局参与" + (cloudMyRank > 0 ? $" · 我的排名 #{cloudMyRank}" : "")
                : (cloudRankEntries.Count > 0 ? $"TapTap 云端实时榜 · 已拉取 {cloudRankEntries.Count} 席" : "TapTap 云端榜已连通（暂无玩家上榜，冲榜中）");
        }
        else
        {
            cloudRankStatusText = res.fromSdk ? "云端排行榜拉取失败，当前展示本地同行榜" : "TapTap 云环境未就绪，已切换本地同行对比榜";
        }

        var cb = pendingLeaderboardUiRefresh;
        pendingLeaderboardUiRefresh = null;
        cb?.Invoke();
    }

    // ---------- 3. TapTap 云存档上传与恢复 ----------
    string CloudSaveStatusSummary()
    {
        return string.IsNullOrEmpty(cloudSaveStatusText) ? "未同步" : cloudSaveStatusText;
    }

    void TryAutoSyncTapTapCloudSave(bool force = false)
    {
        if (!saveReady || saveBlocked) return;
        float now = Time.realtimeSinceStartup;
        if (!force && (now - lastCloudUploadRealtime < CloudAutoUploadIntervalSeconds && day <= lastCloudSyncedDay))
            return;
        UploadTapTapCloudSave(false);
    }

    void UploadTapTapCloudSave(bool manual, Action<bool, string> onDone = null)
    {
        EnsureTapTapSdkBridge();
        if (!saveReady || saveBlocked)
        {
            onDone?.Invoke(false, "当前会话尚未就绪");
            return;
        }
        if (cloudSaveUploadInFlight)
        {
            if (manual) Toast("正在上传云端存档，请稍候");
            onDone?.Invoke(false, "正在上传中");
            return;
        }

        string rawJson;
        try
        {
            var data = CaptureSaveData();
            rawJson = JsonUtility.ToJson(data);
        }
        catch (Exception e)
        {
            if (manual) Toast("序列化存档失败：" + e.Message);
            onDone?.Invoke(false, e.Message);
            return;
        }

        int ts = (int)Mathf.Clamp(NowUnixSeconds(), 0L, int.MaxValue);
        int score = CalculatePlayerRailScore();
        int built = DailyConnectedCount();
        int lc = lines.Count;
        int ridership = CurrentDailyRidership();
        string metaJson = BuildCompanyMetaJson();

        cloudSaveUploadInFlight = true;
        activeCloudUploadReqId = NextSdkRequestId("csu");
        pendingCloudUploadCallback = onDone;
        lastCloudUploadRealtime = Time.realtimeSinceStartup;
        cloudSaveStatusText = "正在同步至 TapTap 云端...";

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            TapTapJS_CloudSaveUpload(activeCloudUploadReqId, rawJson, Mathf.Max(1, day), ts, score, built, lc, ridership, metaJson);
            return;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[TapTapSDK] WebGL 云存档上传失败：" + e.Message);
        }
#endif
        try
        {
            PlayerPrefs.SetString(CloudMirrorPrefsKey, rawJson);
            PlayerPrefs.SetString(CloudMirrorMetaPrefsKey, JsonUtility.ToJson(new LocalCloudMirrorMeta {
                day = Mathf.Max(1, day), timestamp = ts, railScore = score
            }));
            PlayerPrefs.Save();
            HandleTapTapCloudUploadCallback(JsonUtility.ToJson(new JsCloudUploadResult {
                requestId = activeCloudUploadReqId,
                ok = true,
                day = Mathf.Max(1, day),
                timestamp = ts,
                reason = "local mirror"
            }));
        }
        catch (Exception e)
        {
            HandleTapTapCloudUploadCallback(JsonUtility.ToJson(new JsCloudUploadResult {
                requestId = activeCloudUploadReqId,
                ok = false,
                day = Mathf.Max(1, day),
                timestamp = ts,
                reason = e.Message
            }));
        }
    }

    public void HandleTapTapCloudUploadCallback(string json)
    {
        cloudSaveUploadInFlight = false;
        JsCloudUploadResult res = null;
        try { res = JsonUtility.FromJson<JsCloudUploadResult>(json); } catch {}
        if (res == null || (!string.IsNullOrEmpty(activeCloudUploadReqId) && !string.IsNullOrEmpty(res.requestId) && res.requestId != activeCloudUploadReqId))
            return;

        if (res.ok)
        {
            lastCloudSyncedDay = Mathf.Max(lastCloudSyncedDay, res.day > 0 ? res.day : day);
            cloudSaveStatusText = "云端已同步（第 " + lastCloudSyncedDay + " 日 · " + DateTime.Now.ToString("HH:mm") + "）";
        }
        else
        {
            cloudSaveStatusText = "云端同步待重试（" + (string.IsNullOrEmpty(res.reason) ? "离线" : res.reason) + "）";
        }

        var cb = pendingCloudUploadCallback;
        pendingCloudUploadCallback = null;
        cb?.Invoke(res.ok, res.reason ?? "");
    }

    void FetchTapTapCloudSave(Action<bool, string, int, long> onFetched)
    {
        EnsureTapTapSdkBridge();
        if (cloudSaveFetchInFlight)
        {
            onFetched?.Invoke(false, "", 0, 0);
            return;
        }
        cloudSaveFetchInFlight = true;
        activeCloudFetchReqId = NextSdkRequestId("csf");
        pendingCloudFetchCallback = onFetched;

#if UNITY_WEBGL && !UNITY_EDITOR
        try
        {
            TapTapJS_CloudSaveFetch(activeCloudFetchReqId);
            return;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[TapTapSDK] WebGL 拉取云存档失败：" + e.Message);
        }
#endif
        string cached = PlayerPrefs.GetString(CloudMirrorPrefsKey, "");
        int cachedDay = 0, cachedTs = 0;
        string metaStr = PlayerPrefs.GetString(CloudMirrorMetaPrefsKey, "");
        if (!string.IsNullOrEmpty(metaStr))
        {
            try
            {
                var m = JsonUtility.FromJson<LocalCloudMirrorMeta>(metaStr);
                if (m != null) { cachedDay = m.day; cachedTs = m.timestamp; }
            }
            catch {}
        }
        HandleTapTapCloudFetchCallback(JsonUtility.ToJson(new JsCloudFetchResult {
            requestId = activeCloudFetchReqId,
            ok = true,
            saveJson = cached,
            day = cachedDay,
            timestamp = cachedTs,
            reason = string.IsNullOrEmpty(cached) ? "empty" : "local mirror"
        }));
    }

    public void HandleTapTapCloudFetchCallback(string json)
    {
        cloudSaveFetchInFlight = false;
        JsCloudFetchResult res = null;
        try { res = JsonUtility.FromJson<JsCloudFetchResult>(json); } catch {}
        if (res == null || (!string.IsNullOrEmpty(activeCloudFetchReqId) && !string.IsNullOrEmpty(res.requestId) && res.requestId != activeCloudFetchReqId))
            return;

        var cb = pendingCloudFetchCallback;
        pendingCloudFetchCallback = null;
        cb?.Invoke(res.ok, res.saveJson ?? "", res.day, res.timestamp);
    }

    void CheckTapTapCloudSaveOnStartup(bool hadLocalSave)
    {
        if (cloudStartupChecked) return;
        cloudStartupChecked = true;
        FetchTapTapCloudSave((ok, rawJson, cloudDay, cloudTs) => {
            if (!ok || string.IsNullOrEmpty(rawJson))
            {
                cloudSaveStatusText = hadLocalSave ? "本地已就绪（云端尚无档案）" : "新社创立（将自动备份云端）";
                if (hadLocalSave) TryAutoSyncTapTapCloudSave(true);
                return;
            }
            SaveData cloudData = null;
            try { cloudData = JsonUtility.FromJson<SaveData>(rawJson); } catch {}
            if (cloudData == null || cloudData.version != 1 || cloudData.lines == null || cloudData.stations == null) return;

            int actualCloudDay = Mathf.Max(1, Mathf.Max(cloudDay, cloudData.day));
            if (!hadLocalSave)
            {
                if (ApplyLoadedSaveJson(rawJson, "TapTap 云端存档", false))
                {
                    cloudSaveStatusText = "已从 TapTap 云端自动续接（第 " + actualCloudDay + " 日）";
                }
                return;
            }

            if (actualCloudDay > day || (actualCloudDay == day && cloudData.lines.Count > lines.Count))
            {
                cloudSaveStatusText = "云端发现更新进度（第 " + actualCloudDay + " 日）";
                Confirm("发现云端新进度",
                    $"TapTap 云端存有第 {actualCloudDay} 日·{cloudData.lines.Count} 条线路的经营档案（当前本机为第 {day} 日·{lines.Count} 条线路）。是否拉取覆盖本机？",
                    () => {
                        if (ApplyLoadedSaveJson(rawJson, "TapTap 云端存档", false))
                        {
                            cloudSaveStatusText = "已恢复云端进度（第 " + actualCloudDay + " 日）";
                        }
                    });
            }
            else
            {
                cloudSaveStatusText = "本地与云端已核对（第 " + Mathf.Max(day, actualCloudDay) + " 日）";
            }
        });
    }
}
