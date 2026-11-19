mergeInto(LibraryManager.library, {
  TapTapJS_IsAvailable: function () {
    if (typeof window === "undefined") return 0;
    var hasAd = typeof window.ShowRewardVideoAd === "function" ||
      (window.tap && typeof window.tap.showRewardVideoAd === "function") ||
      (window.TapSDK && typeof window.TapSDK.showRewardVideoAd === "function");
    var hasCloud = !!window.clientCloud ||
      (window.tap && !!window.tap.cloud) ||
      (window.TapSDK && !!window.TapSDK.cloud);
    return (hasAd || hasCloud) ? 1 : 0;
  },

  TapTapJS_GetUserId: function () {
    var uid = "";
    try {
      if (typeof window !== "undefined") {
        if (typeof window.GetUserId === "function") {
          var v = window.GetUserId();
          if (v !== undefined && v !== null) uid = String(v);
        } else if (window.clientCloud && window.clientCloud.userId !== undefined && window.clientCloud.userId !== null) {
          uid = String(window.clientCloud.userId);
        } else if (window.tap && window.tap.userId) {
          uid = String(window.tap.userId);
        }
      }
    } catch (e) {}
    var bufferSize = lengthBytesUTF8(uid) + 1;
    var buffer = _malloc(bufferSize);
    stringToUTF8(uid, buffer, bufferSize);
    return buffer;
  },

  TapTapJS_ShowRewardVideoAd: function (requestIdPtr, adKeyPtr, itemNamePtr) {
    var readStr = typeof UTF8ToString === "function" ? UTF8ToString : Pointer_stringify;
    var requestId = readStr(requestIdPtr);
    var adKey = readStr(adKeyPtr);
    var itemName = readStr(itemNamePtr);
    var settled = false;

    function sendResult(success, manualClose, errorCode, message) {
      if (settled) return;
      settled = true;
      var payload = JSON.stringify({
        requestId: requestId,
        adKey: adKey,
        success: !!success,
        manualClose: !!manualClose,
        errorCode: String(errorCode || ""),
        message: String(message || "")
      });
      if (typeof SendMessage === "function") {
        SendMessage("TapTapSdkBridgeHost", "OnJsAdCallback", payload);
      }
    }

    function resolveAdFn() {
      if (typeof window === "undefined") return null;
      if (typeof window.ShowRewardVideoAd === "function") return window.ShowRewardVideoAd;
      if (window.tap && typeof window.tap.showRewardVideoAd === "function") {
        return function (opts) { return window.tap.showRewardVideoAd(opts); };
      }
      if (window.TapSDK && typeof window.TapSDK.showRewardVideoAd === "function") {
        return function (opts) { return window.TapSDK.showRewardVideoAd(opts); };
      }
      return null;
    }

    var adFn = resolveAdFn();
    if (adFn) {
      try {
        adFn({
          adKey: adKey,
          onSuccess: function (res) {
            var msg = res && res.message ? String(res.message) : "";
            if (msg === "embed manual close") {
              sendResult(false, true, "MANUAL_CLOSE", msg);
              return;
            }
            if (!res || res.success === true) {
              sendResult(true, false, "", msg || "ok");
            } else {
              sendResult(false, false, res.code || "FAILED", msg || "ad failed");
            }
          },
          onError: function (res) {
            var msg = res && res.message ? String(res.message) : (res ? String(res) : "ad error");
            var code = res && res.code ? String(res.code) : "AD_ERROR";
            var isManual = msg === "embed manual close" || code === "MANUAL_CLOSE";
            sendResult(false, isManual, code, msg);
          }
        });
      } catch (e) {
        sendResult(false, false, "EXCEPTION", e && e.message ? e.message : "ad invoke exception");
      }
      return;
    }

    // 浏览器预览环境未注入宿主 TapTap SDK 时，弹出仿真激励短片倒计时框（支持正常看完或中途关闭）
    try {
      if (typeof document !== "undefined" && document.body) {
        var existing = document.getElementById("tt-preview-ad-overlay");
        if (existing && existing.parentNode) existing.parentNode.removeChild(existing);

        var overlay = document.createElement("div");
        overlay.id = "tt-preview-ad-overlay";
        overlay.style.cssText = "position:fixed;inset:0;z-index:999999;background:rgba(16,20,28,0.88);display:flex;align-items:center;justify-content:center;font-family:sans-serif;color:#f7f4ec;";

        var card = document.createElement("div");
        card.style.cssText = "width:min(88vw,420px);background:#1e2633;border:1px solid rgba(255,255,255,0.16);border-radius:16px;padding:24px;box-sizing:border-box;text-align:center;box-shadow:0 20px 50px rgba(0,0,0,0.5);";

        var title = document.createElement("div");
        title.style.cssText = "font-size:18px;font-weight:700;margin-bottom:8px;";
        title.textContent = "TapTap 激励视频预览";

        var sub = document.createElement("div");
        sub.style.cssText = "font-size:14px;color:#b8c3d4;margin-bottom:18px;line-height:1.5;";
        sub.textContent = "奖励项目：" + (itemName || adKey) + "（当前为离线预览通道）";

        var timerEl = document.createElement("div");
        timerEl.style.cssText = "font-size:28px;font-weight:700;color:#7cc47f;margin-bottom:18px;";
        var remain = 3;
        timerEl.textContent = "还剩 " + remain + " 秒可领取奖励";

        var btnRow = document.createElement("div");
        btnRow.style.cssText = "display:flex;gap:12px;justify-content:center;";

        var claimBtn = document.createElement("button");
        claimBtn.disabled = true;
        claimBtn.style.cssText = "flex:1;padding:10px 14px;border:none;border-radius:10px;background:#49617a;color:#cbd5e1;font-size:14px;font-weight:600;cursor:not-allowed;";
        claimBtn.textContent = "请稍候...";

        var closeBtn = document.createElement("button");
        closeBtn.style.cssText = "padding:10px 16px;border:none;border-radius:10px;background:#593232;color:#ffd6d6;font-size:14px;cursor:pointer;";
        closeBtn.textContent = "提前关闭";

        function cleanup() {
          if (intervalId) { clearInterval(intervalId); intervalId = 0; }
          if (overlay && overlay.parentNode) overlay.parentNode.removeChild(overlay);
        }

        closeBtn.onclick = function () {
          cleanup();
          sendResult(false, true, "MANUAL_CLOSE", "embed manual close");
        };

        claimBtn.onclick = function () {
          if (claimBtn.disabled) return;
          cleanup();
          sendResult(true, false, "", "preview ad completed");
        };

        btnRow.appendChild(claimBtn);
        btnRow.appendChild(closeBtn);
        card.appendChild(title);
        card.appendChild(sub);
        card.appendChild(timerEl);
        card.appendChild(btnRow);
        overlay.appendChild(card);
        document.body.appendChild(overlay);

        var intervalId = setInterval(function () {
          remain -= 1;
          if (remain <= 0) {
            clearInterval(intervalId);
            intervalId = 0;
            timerEl.textContent = "已达成完整观看条件";
            claimBtn.disabled = false;
            claimBtn.style.background = "#589e5c";
            claimBtn.style.color = "#ffffff";
            claimBtn.style.cursor = "pointer";
            claimBtn.textContent = "领取奖励";
          } else {
            timerEl.textContent = "还剩 " + remain + " 秒可领取奖励";
          }
        }, 1000);
        return;
      }
    } catch (e) {}

    sendResult(false, false, "NO_HOST", "TapTap Ad SDK unavailable");
  },

  TapTapJS_CloudSaveUpload: function (requestIdPtr, saveJsonPtr, day, timestamp, railScore, stationsBuilt, linesCount, ridership, companyMetaPtr) {
    var readStr = typeof UTF8ToString === "function" ? UTF8ToString : Pointer_stringify;
    var requestId = readStr(requestIdPtr);
    var saveJson = readStr(saveJsonPtr);
    var companyMeta = readStr(companyMetaPtr);

    function finish(ok, reason) {
      var payload = JSON.stringify({
        requestId: requestId,
        ok: !!ok,
        day: day,
        timestamp: timestamp,
        reason: String(reason || "")
      });
      if (typeof SendMessage === "function") {
        SendMessage("TapTapSdkBridgeHost", "OnJsCloudSaveUploadCallback", payload);
      }
    }

    var cc = typeof window !== "undefined" ? (window.clientCloud || (window.tap && window.tap.cloud) || (window.TapSDK && window.TapSDK.cloud)) : null;
    if (cc && typeof cc.BatchSet === "function") {
      try {
        cc.BatchSet()
          .Set("railway2_cloud_save_v1", saveJson)
          .Set("company_meta", companyMeta)
          .SetInt("railway2_save_day", day)
          .SetInt("railway2_save_ts", timestamp)
          .SetInt("rail_score", railScore)
          .SetInt("stations_built", stationsBuilt)
          .SetInt("lines_count", linesCount)
          .SetInt("daily_ridership", ridership)
          .Save("自动云存档与排行榜同步", {
            ok: function () { finish(true, "clientCloud ok"); },
            error: function (code, reason) { finish(false, String(code || "") + ":" + String(reason || "")); }
          });
      } catch (e) {
        finish(false, e && e.message ? e.message : "cloud upload exception");
      }
      return;
    }

    // 未注入 TapTap clientCloud 时，镜像写入浏览器隔离本地云缓存槽以便前端功能测试与断点恢复
    try {
      if (typeof window !== "undefined" && window.localStorage) {
        var mirror = {
          saveJson: saveJson,
          companyMeta: companyMeta,
          day: day,
          timestamp: timestamp,
          railScore: railScore,
          stationsBuilt: stationsBuilt,
          linesCount: linesCount,
          ridership: ridership
        };
        window.localStorage.setItem("railway2_taptap_cloud_mirror_v1", JSON.stringify(mirror));
        setTimeout(function () { finish(true, "local mirror synced"); }, 10);
        return;
      }
    } catch (e) {}
    finish(false, "clientCloud unavailable");
  },

  TapTapJS_CloudSaveFetch: function (requestIdPtr) {
    var readStr = typeof UTF8ToString === "function" ? UTF8ToString : Pointer_stringify;
    var requestId = readStr(requestIdPtr);

    function finish(ok, saveJson, day, timestamp, reason) {
      var payload = JSON.stringify({
        requestId: requestId,
        ok: !!ok,
        saveJson: saveJson ? String(saveJson) : "",
        day: Number(day || 0),
        timestamp: Number(timestamp || 0),
        reason: String(reason || "")
      });
      if (typeof SendMessage === "function") {
        SendMessage("TapTapSdkBridgeHost", "OnJsCloudSaveFetchCallback", payload);
      }
    }

    var cc = typeof window !== "undefined" ? (window.clientCloud || (window.tap && window.tap.cloud) || (window.TapSDK && window.TapSDK.cloud)) : null;
    if (cc && typeof cc.BatchGet === "function") {
      try {
        cc.BatchGet()
          .Key("railway2_cloud_save_v1")
          .Key("railway2_save_day")
          .Key("railway2_save_ts")
          .Fetch({
            ok: function (values, iscores) {
              var raw = values && values.railway2_cloud_save_v1 ? values.railway2_cloud_save_v1 : "";
              if (raw && typeof raw !== "string") {
                try { raw = JSON.stringify(raw); } catch (e) {}
              }
              var d = iscores && iscores.railway2_save_day ? Number(iscores.railway2_save_day) : 0;
              var ts = iscores && iscores.railway2_save_ts ? Number(iscores.railway2_save_ts) : 0;
              finish(true, raw || "", d, ts, raw ? "ok" : "empty cloud save");
            },
            error: function (code, reason) {
              finish(false, "", 0, 0, String(code || "") + ":" + String(reason || ""));
            }
          });
      } catch (e) {
        finish(false, "", 0, 0, e && e.message ? e.message : "cloud fetch exception");
      }
      return;
    }

    try {
      if (typeof window !== "undefined" && window.localStorage) {
        var txt = window.localStorage.getItem("railway2_taptap_cloud_mirror_v1");
        if (txt) {
          var parsed = JSON.parse(txt);
          setTimeout(function () {
            finish(true, parsed.saveJson || "", parsed.day || 0, parsed.timestamp || 0, "local mirror fetched");
          }, 10);
          return;
        }
        setTimeout(function () { finish(true, "", 0, 0, "empty cloud mirror"); }, 10);
        return;
      }
    } catch (e) {}
    finish(false, "", 0, 0, "clientCloud unavailable");
  },

  TapTapJS_SubmitScore: function (requestIdPtr, railScore, stationsBuilt, linesCount, ridership, day, companyMetaPtr) {
    var readStr = typeof UTF8ToString === "function" ? UTF8ToString : Pointer_stringify;
    var requestId = readStr(requestIdPtr);
    var companyMeta = readStr(companyMetaPtr);

    function finish(ok, reason) {
      var payload = JSON.stringify({
        requestId: requestId,
        ok: !!ok,
        reason: String(reason || "")
      });
      if (typeof SendMessage === "function") {
        SendMessage("TapTapSdkBridgeHost", "OnJsSubmitScoreCallback", payload);
      }
    }

    var cc = typeof window !== "undefined" ? (window.clientCloud || (window.tap && window.tap.cloud) || (window.TapSDK && window.TapSDK.cloud)) : null;
    if (cc && typeof cc.BatchSet === "function") {
      try {
        cc.BatchSet()
          .SetInt("rail_score", railScore)
          .SetInt("stations_built", stationsBuilt)
          .SetInt("lines_count", linesCount)
          .SetInt("daily_ridership", ridership)
          .SetInt("railway2_save_day", day)
          .Set("company_meta", companyMeta)
          .Save("更新排行榜评分", {
            ok: function () { finish(true, "ok"); },
            error: function (code, reason) { finish(false, String(code || "") + ":" + String(reason || "")); }
          });
      } catch (e) {
        finish(false, e && e.message ? e.message : "submit score exception");
      }
      return;
    }
    setTimeout(function () { finish(false, "clientCloud unavailable"); }, 10);
  },

  TapTapJS_FetchLeaderboard: function (requestIdPtr, topN) {
    var readStr = typeof UTF8ToString === "function" ? UTF8ToString : Pointer_stringify;
    var requestId = readStr(requestIdPtr);
    var count = topN > 0 ? topN : 20;

    function finish(ok, entries, myRank, totalPlayers, fromSdk, reason) {
      var payload = JSON.stringify({
        requestId: requestId,
        ok: !!ok,
        fromSdk: !!fromSdk,
        myRank: Number(myRank || 0),
        totalPlayers: Number(totalPlayers || 0),
        reason: String(reason || ""),
        entries: entries || []
      });
      if (typeof SendMessage === "function") {
        SendMessage("TapTapSdkBridgeHost", "OnJsLeaderboardCallback", payload);
      }
    }

    var cc = typeof window !== "undefined" ? (window.clientCloud || (window.tap && window.tap.cloud) || (window.TapSDK && window.TapSDK.cloud)) : null;
    if (!cc || typeof cc.GetRankList !== "function") {
      setTimeout(function () { finish(false, [], 0, 0, false, "clientCloud unavailable"); }, 10);
      return;
    }

    var myUserId = "";
    try {
      if (typeof window.GetUserId === "function") myUserId = String(window.GetUserId() || "");
      else if (cc.userId !== undefined && cc.userId !== null) myUserId = String(cc.userId);
    } catch (e) {}

    var entries = [];
    var userIds = [];
    var myRank = 0;
    var totalPlayers = 0;
    var pending = 3;

    function checkDone() {
      pending -= 1;
      if (pending > 0) return;
      if (userIds.length > 0 && typeof window.GetUserNickname === "function") {
        try {
          window.GetUserNickname({
            userIds: userIds,
            onSuccess: function (nicknames) {
              var map = {};
              if (nicknames && nicknames.length) {
                for (var j = 0; j < nicknames.length; j++) {
                  var info = nicknames[j];
                  if (info) map[String(info.userId)] = info.nickname || "";
                }
              }
              for (var k = 0; k < entries.length; k++) {
                var e = entries[k];
                if (map[e.userId]) e.nickname = map[e.userId];
              }
              finish(true, entries, myRank, totalPlayers, true, "ok");
            },
            onError: function () {
              finish(true, entries, myRank, totalPlayers, true, "ok without nickname");
            }
          });
          return;
        } catch (e) {}
      }
      finish(true, entries, myRank, totalPlayers, true, "ok");
    }

    try {
      cc.GetRankList("rail_score", 0, count, {
        ok: function (rankList) {
          if (rankList && rankList.length) {
            for (var i = 0; i < rankList.length; i++) {
              var item = rankList[i] || {};
              var isc = item.iscore || {};
              var sc = item.score || {};
              var uid = item.userId !== undefined && item.userId !== null
                ? String(item.userId)
                : (item.player !== undefined && item.player !== null ? String(item.player) : "");
              var meta = sc.company_meta;
              if (typeof meta === "string") {
                try { meta = JSON.parse(meta); } catch (e) { meta = null; }
              }
              entries.push({
                rank: i + 1,
                userId: uid,
                nickname: "",
                companyName: meta && meta.companyName ? String(meta.companyName) : "",
                seal: meta && typeof meta.seal === "number" ? meta.seal : 0,
                region: meta && meta.region ? String(meta.region) : "",
                score: Number(isc.rail_score || 0),
                stationsBuilt: Number(isc.stations_built || 0),
                linesCount: Number(isc.lines_count || 0),
                dailyRidership: Number(isc.daily_ridership || 0),
                day: Number(isc.railway2_save_day || 1),
                isMe: !!myUserId && uid === myUserId
              });
              if (uid) userIds.push(uid);
            }
          }
          checkDone();
        },
        error: function () {
          checkDone();
        }
      }, "stations_built", "lines_count", "daily_ridership", "railway2_save_day", "company_meta");

      if (typeof cc.GetUserRank === "function" && myUserId) {
        cc.GetUserRank(cc.userId !== undefined ? cc.userId : myUserId, "rail_score", {
          ok: function (rank) { myRank = Number(rank || 0); checkDone(); },
          error: function () { checkDone(); }
        });
      } else {
        checkDone();
      }

      if (typeof cc.GetRankTotal === "function") {
        cc.GetRankTotal("rail_score", {
          ok: function (total) { totalPlayers = Number(total || 0); checkDone(); },
          error: function () { checkDone(); }
        });
      } else {
        checkDone();
      }
    } catch (e) {
      finish(false, [], 0, 0, false, e && e.message ? e.message : "leaderboard exception");
    }
  }
});
