using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    // ---------- 动态「山海铁道驿报」与本地「山海群雄榜」 ----------
    sealed class BulletinEntry
    {
        public string tag, title, date, body, actionLabel;
        public Color accentColor;
        public int focusStation = -1;
        public Action onAction;
    }

    bool HasUnreadBulletins()
    {
        return lastReadNoticeDay < day;
    }

    void RefreshNoticeUnreadDot()
    {
        if (noticeUnreadDot) noticeUnreadDot.SetActive(HasUnreadBulletins());
    }

    void OpenSolarTermQuickPopup()
    {
        var term = CurrentSolarTerm();
        var next = NextSolarTerm();
        string body = "「" + term.poem + "」\n\n"
            + "当期物候加成：" + term.effectHint + "\n"
            + "剩余天数：尚余 " + SolarTermDaysRemaining() + " 日\n"
            + "下一节候：【" + next.name + "】" + next.effectHint;
        float textH = Mathf.Clamp(MeasureTextHeight(body, 26, 740) + 20f, 160f, 420f);
        var p = OpenPopup("时令【" + term.name + "】· " + term.season, textH + 220f, 820f, "solar_term_quick");
        ScrollableText(p.transform, body, 26, InkSoft, 40f, 126f, 740f, textH);
        var calBtn = Btn(p.transform, "OpenSolarJournal", "打开节气风物历", 25, 40f, 30f, 360f, 76f, Matcha, () => {
            ClosePanel(true);
            OpenCultureJournalModal(2);
        });
        TInk(calBtn, Color.white);
        var closeBtn = Btn(p.transform, "CloseSolar", "关闭", 25, 420f, 30f, 360f, 76f, Paper2, ClosePanel);
        TInk(closeBtn, Ink);
    }

    void OpenCongestionAlertQuickPopup()
    {
        var hotSegs = CollectTopCongestedSegments(3);
        if (hotSegs.Count == 0)
        {
            var pEmpty = OpenPopup("全网卡点急报", 340f, 760f, "congestion_alert_quick");
            Txt(pEmpty.transform, "全网路网当前运行通畅，暂无满载率超过 78% 的高负荷区段。", 26, InkSoft, 40f, 130f, 680f, 110f, TextAnchor.MiddleLeft);
            var okBtn = Btn(pEmpty.transform, "CloseAlertEmpty", "知道了", 25, 220f, 30f, 320f, 74f, Indigo, ClosePanel);
            TInk(okBtn, Color.white);
            return;
        }
        string segLines = "";
        for (int hs = 0; hs < hotSegs.Count; hs++)
        {
            var cg = hotSegs[hs];
            int pctLoad = Mathf.RoundToInt(cg.loadRatio * 100f);
            segLines += (hs > 0 ? "\n" : "") + (hs + 1) + ". " + DisplayPlaceAliases(cg.region) + " · " + cg.line.name
                + "（" + StationDisplayName(cg.stA, cg.region) + " ↔ " + StationDisplayName(cg.stB, cg.region) + "）满载 " + pctLoad + "%";
        }
        segLines += "\n\n建议开启热力图或直接跳转对应区段快速增班/调入备车。";
        float textH = Mathf.Clamp(MeasureTextHeight(segLines, 26, 740) + 20f, 160f, 440f);
        var firstSeg = hotSegs[0];
        var p = OpenPopup("全网卡点急报（" + hotSegs.Count + " 处高负荷）", textH + 220f, 820f, "congestion_alert_quick");
        ScrollableText(p.transform, segLines, 26, InkSoft, 40f, 126f, 740f, textH);
        var jumpBtn = Btn(p.transform, "JumpWorstSeg", "直飞最堵区段", 25, 40f, 30f, 360f, 76f, Vermilion, () => {
            ClosePanel(true);
            FocusAndBoostSegment(firstSeg);
        });
        TInk(jumpBtn, Color.white);
        var closeBtn = Btn(p.transform, "CloseAlert", "关闭", 25, 420f, 30f, 360f, 76f, Paper2, ClosePanel);
        TInk(closeBtn, Ink);
    }

    List<BulletinEntry> BuildLiveBulletins()
    {
        var list = new List<BulletinEntry>();

        // 0. 督办考成（节气与急报已拆至地图左侧快捷入口）
        int readyPlaques = CountClaimablePlaques();
        if (readyPlaques > 0)
        {
            list.Add(new BulletinEntry
            {
                tag = "考成",
                title = "督办考成：" + readyPlaques + " 方牌匾待领",
                date = "可请颁",
                accentColor = Gold,
                body = "总办考核目标已达成！请前往「山海舆图志 · 钦颁匾额」请领赏银、科研点与永久经营加成。",
                actionLabel = "前往请匾",
                onAction = () => OpenCultureJournalModal(0)
            });
        }

        // 1. 跨区联运与年度评比动态
        int links = CrossBaySynergyLinkCount();
        int awardCycle = (day / 15) * 15;
        if (day >= 15 && lastAwardDay < awardCycle)
        {
            list.Add(new BulletinEntry
            {
                tag = "嘉奖",
                title = "第 " + awardCycle + " 日山海年度铁道大赏已揭晓",
                date = "第 " + day + " 日",
                accentColor = Gold,
                body = "评审团已完成本周期路网评估，贺礼现已备妥，请前往「公司」页签领赏。",
                actionLabel = "前往领赏",
                onAction = () => { ClosePanel(true); OpenCompanyScreen(); }
            });
        }

        // 2. 当前车站突发客流/施工事件（带直达定位按钮）
        var eventStations = new List<int>();
        for (int i = 0; i < stations.Count; i++)
        {
            if (stations[i].closed || stations[i].trafficEvent <= 0) continue;
            if (!RegionUnlocked(stations[i].region)) continue;
            eventStations.Add(i);
        }
        eventStations.Sort((a, b) =>
        {
            int builtCmp = stations[b].built.CompareTo(stations[a].built);
            if (builtCmp != 0) return builtCmp;
            return StationBaselineDemand(b).CompareTo(StationBaselineDemand(a));
        });
        int shownEvents = Mathf.Min(4, eventStations.Count);
        for (int k = 0; k < shownEvents; k++)
        {
            int stIdx = eventStations[k];
            var st = stations[stIdx];
            int ev = Mathf.Clamp(st.trafficEvent, 0, TrafficEventNames.Length - 1);
            int pct = Mathf.RoundToInt((TrafficEventFactors[ev] - 1f) * 100f);
            bool surge = pct >= 0;
            string pctStr = (pct >= 0 ? "+" : "") + pct + "%";
            string stName = StationDisplayName(stIdx, st.region);
            string regName = DisplayPlaceAliases(st.region);
            list.Add(new BulletinEntry
            {
                tag = surge ? "客潮" : "预警",
                title = regName + " · " + stName + "「" + TrafficEventNames[ev] + "」",
                date = "今日",
                accentColor = surge ? Matcha : Vermilion,
                focusStation = stIdx,
                actionLabel = "定位站台",
                body = stName + "站周边今日出现「" + TrafficEventNames[ev] + "」，客流需求波动 " + pctStr + "。\n"
                     + (surge
                         ? (st.built ? "建议留意该站候车拥挤度，必要时加密途经线路班次。" : "尚未接通该站，正是趁热延线扩站的好时机！")
                         : "该站客流短期回落，可适当调减富余车次节省维保成本。")
            });
        }

        // 3. 山海行风周报
        int weekNum = ((day - 1) / 7) + 1;
        list.Add(new BulletinEntry
        {
            tag = "周报",
            title = "《山海行风周报》第 " + weekNum + " 周 · " + EconomyCycleNames[economyCycle],
            date = "还剩 " + CycleDaysRemaining() + " 日",
            accentColor = Indigo,
            body = "本周态势：" + EconomyCycleDescs[economyCycle] + "\n下周风向预告：" + EconomyCycleNames[nextEconomyCycle] + " — " + EconomyCycleDescs[nextEconomyCycle]
        });

        // 4. 三地口岸联运简报
        list.Add(new BulletinEntry
        {
            tag = "联运",
            title = links > 0 ? "三地口岸联运生效中（" + links + "/3 走廊）" : "三域口岸联运开通指引",
            date = "实时",
            accentColor = Matcha,
            body = links > 0
                ? CrossBaySynergySummary() + "\n继续贯通其余跨区门户可进一步叠加口岸补贴与全网客流吸引力。"
                : "当汐港、镜屿与榕川的运营线路在「北桥关 / 东湾（西渡）」「青藤（北关）/ 琴海口」「跨海虹桥」等门户两侧同时接通时，即可激活每日跨区联运补贴与客流加成。"
        });

        // 5. 常规社务公告
        list.Add(new BulletinEntry
        {
            tag = "社务",
            title = companyName + " · 站务手帐编年备忘",
            date = "常态",
            accentColor = InkSoft,
            body = "· 长按地图任意站点拖拽可实时预览走廊客需（翠绿→琥珀→赤红）与预计新增日客流。\n"
                 + "· 点击底部负荷图例可切换客流热力图，点选繁忙路段即可快捷调班或加车。"
        });

        return list;
    }

    void OpenNoticeBoard()
    {
        lastReadNoticeDay = day;
        RefreshNoticeUnreadDot();
        SaveGame();
        PlayPaperFlipSfx();

        var items = BuildLiveBulletins();
        int total = items.Count;

        const float popupW = 880f;
        const float popupH = 980f;
        var panel = OpenPopup("山海铁道驿报", popupH, popupW, "notice_board");
        JournalTape(panel.transform, popupW * 0.5f - 82f, popupH - 18f, 164f);
        StampBotanicalSprig(panel.transform, "orn_cloud_fret", 10f, 10f, 38f, 38f, 0.35f, false, true);
        StampBotanicalSprig(panel.transform, "orn_cloud_fret", popupW - 48f, 10f, 38f, 38f, 0.35f, true, true);
        Txt(panel.transform, "第 " + day + " 日 · 沿线动态与运营简报（共 " + total + " 则）", 23, InkSoft, 28, popupH - 108, popupW - 56, 32, TextAnchor.MiddleLeft);
        Img(Rect("NoticeHeaderLine", panel.transform, 28, popupH - 120, popupW - 56, 2), new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.16f)).raycastTarget = false;

        float viewW = popupW - 56f;
        float viewH = popupH - 152f;
        var view = Rect("NoticeScrollView", panel.transform, 28, 24, viewW, viewH);
        Img(view, new Color(1, 1, 1, 0)).raycastTarget = true;
        view.AddComponent<RectMask2D>();
        var content = Rect("NoticeListContent", view.transform, 0, 0, viewW, viewH);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(0, 1);
        contentRt.pivot = new Vector2(0, 1); contentRt.anchoredPosition = Vector2.zero;
        var scroll = view.AddComponent<ScrollRect>();
        scroll.viewport = view.GetComponent<RectTransform>();
        scroll.content = contentRt;
        scroll.horizontal = false; scroll.vertical = true;
        scroll.scrollSensitivity = 95f; scroll.movementType = ScrollRect.MovementType.Elastic;

        int expandedIndex = total > 0 ? 0 : -1;
        bool scrollBound = false;

        void RebuildNoticeList()
        {
            if (scrollBound) RememberScrollPosition("notice:board", contentRt.anchoredPosition.y, contentRt.rect.height, viewH);
            for (int c = content.transform.childCount - 1; c >= 0; c--)
                Destroy(content.transform.GetChild(c).gameObject);

            const float headerH = 92f;
            const float gap = 16f;
            float cursorY = 8f;

            for (int i = 0; i < total; i++)
            {
                int idx = i;
                var item = items[i];
                bool isExpanded = idx == expandedIndex;
                bool hasAction = item.focusStation >= 0 || item.onAction != null;
                float bodyTextW = viewW - 84f;
                float bodyH = 0f;
                if (isExpanded)
                {
                    float rawH = MeasureTextHeight(item.body, 25, bodyTextW) * 1.32f + 26f;
                    bodyH = Mathf.Max(120f, rawH) + (hasAction ? 74f : 0f);
                }
                float cardH = headerH + (isExpanded ? bodyH + 28f : 0f);
                cursorY += cardH;

                var card = Rect("NoticeItem_" + idx, content.transform, 0, -cursorY, viewW, cardH);
                var cardRt = card.GetComponent<RectTransform>();
                cardRt.anchorMin = new Vector2(0, 1); cardRt.anchorMax = new Vector2(0, 1);
                cardRt.pivot = new Vector2(0, 0);
                Rounded(card, isExpanded ? new Color(1f, 0.995f, 0.97f, 0.99f) : new Color(Paper2.r, Paper2.g, Paper2.b, 0.74f));
                if (isExpanded) SoftShadow(card);

                var ribbon = Rect("Ribbon", card.transform, 0, 8, 6, cardH - 16);
                Img(ribbon, item.accentColor, spriteRound).raycastTarget = false;

                var headerGo = Rect("Header", card.transform, 0, cardH - headerH, viewW, headerH);
                var headerBg = Img(headerGo, new Color(1, 1, 1, 0));
                var headerBtn = headerGo.AddComponent<Button>();
                headerBtn.targetGraphic = headerBg;
                headerBtn.onClick.AddListener(() => {
                    PlayPaperFlipSfx();
                    expandedIndex = (expandedIndex == idx) ? -1 : idx;
                    RebuildNoticeList();
                });

                float titleX = 28f;
                var tagPill = Rect("NoticeTag", headerGo.transform, titleX, (headerH - 40f) * 0.5f, 84f, 40f);
                Rounded(tagPill, new Color(item.accentColor.r, item.accentColor.g, item.accentColor.b, 0.14f));
                var tagTxt = Txt(tagPill.transform, item.tag, 21, item.accentColor, 0, 0, 84f, 40f, TextAnchor.MiddleCenter);
                tagTxt.fontStyle = FontStyle.Normal;
                titleX += 98f;

                var titleTxt = Txt(headerGo.transform, item.title, 28, Ink, titleX, 0, viewW - titleX - 208f, headerH, TextAnchor.MiddleLeft);
                titleTxt.fontStyle = FontStyle.Normal;
                FitSingleLine(titleTxt, 28);

                if (!string.IsNullOrEmpty(item.date))
                    Txt(headerGo.transform, item.date, 22, InkSoft, viewW - 204f, 0, 146f, headerH, TextAnchor.MiddleRight);
                Txt(headerGo.transform, isExpanded ? "▾" : "▸", 26, isExpanded ? Ink : InkSoft, viewW - 56f, 0, 36f, headerH, TextAnchor.MiddleCenter);

                if (isExpanded)
                {
                    Img(Rect("Sep", card.transform, 28, cardH - headerH - 2f, viewW - 56f, 2f), new Color(InkSoft.r, InkSoft.g, InkSoft.b, 0.14f)).raycastTarget = false;
                    var bodyBg = Rect("BodyPaper", card.transform, 24, 16, viewW - 48f, bodyH);
                    Rounded(bodyBg, new Color(0.965f, 0.945f, 0.905f, 0.72f));
                    bodyBg.GetComponent<Image>().raycastTarget = false;
                    float textBottom = hasAction ? 78f : 10f;
                    var bodyTxt = Txt(bodyBg.transform, item.body, 25, Ink, 20, textBottom, bodyTextW, bodyH - textBottom - 10f, TextAnchor.UpperLeft);
                    bodyTxt.lineSpacing = 1.32f;

                    if (hasAction)
                    {
                        int focusIdx = item.focusStation;
                        Action act = item.onAction ?? (() => { ClosePanel(true); FocusMapOnStation(focusIdx); });
                        var goBtn = Btn(bodyBg.transform, "NoticeAction", item.actionLabel ?? "前往查看", 23, viewW - 48f - 236f, 12f, 216f, 56f, Indigo, act);
                        TInk(goBtn, Color.white);
                    }
                }

                cursorY += gap;
            }

            contentRt.sizeDelta = new Vector2(viewW, Mathf.Max(viewH, cursorY + 8f));
            if (!scrollBound) { BindScrollPosition(scroll, "notice:board"); scrollBound = true; }
            else RestoreScrollPosition(scroll, "notice:board");
        }

        RebuildNoticeList();
    }

    sealed class LeaderboardOperator
    {
        public string name, tag, motto;
        public int stations, linesCount, dailyProfit, score;
        public bool isPlayer, isCloudPlayer;
    }

    int CalculatePlayerRailScore()
    {
        int s = DailyConnectedCount() * 180
              + lines.Count * 260
              + Mathf.Max(0, Mathf.RoundToInt(income))
              + CrossBaySynergyLinkCount() * 900
              + FoundCount() * 340
              + (techDone.Count + companyTechDone.Count) * 85;
        return Mathf.Max(100, s);
    }

    List<LeaderboardOperator> BuildBayLeaderboard()
    {
        int d = Mathf.Max(1, day);
        int playerScore = CalculatePlayerRailScore();
        var entries = new List<LeaderboardOperator>
        {
            new LeaderboardOperator {
                name = companyName + PlayerPlaqueHonorTag(),
                tag = CompanyTitle() + " · 票册 " + earnedMemorialTickets.Count + "/6",
                motto = CrossBaySynergyLinkCount() > 0 ? "三地口岸联运中 · " + DisplayPlaceAliases(currentRegion) + "主导" : "当前重心：" + DisplayPlaceAliases(currentRegion),
                stations = DailyConnectedCount(),
                linesCount = lines.Count,
                dailyProfit = Mathf.RoundToInt(income),
                score = playerScore,
                isPlayer = true
            }
        };

        // 合并 TapTap 真实云端排行榜玩家记录（若已从 clientCloud:GetRankList 拉取成功）
        for (int i = 0; i < cloudRankEntries.Count; i++)
        {
            var cr = cloudRankEntries[i];
            if (cr == null || cr.isMe) continue;
            string displayName = !string.IsNullOrEmpty(cr.companyName) && !string.IsNullOrEmpty(cr.nickname)
                ? $"{cr.nickname} · {cr.companyName}"
                : (!string.IsNullOrEmpty(cr.nickname) ? cr.nickname : (!string.IsNullOrEmpty(cr.companyName) ? cr.companyName : "山海同行 #" + cr.rank));
            entries.Add(new LeaderboardOperator {
                name = displayName,
                tag = "TapTap 真实路局 · 第 " + cr.day + " 日",
                motto = string.IsNullOrEmpty(cr.region) ? "云端实名排行玩家" : ("主营版图：" + cr.region + (cr.dailyRidership > 0 ? $" · 日客流 {cr.dailyRidership:N0}" : "")),
                stations = Mathf.Max(1, cr.stationsBuilt),
                linesCount = Mathf.Max(1, cr.linesCount),
                dailyProfit = cr.score / 5,
                score = cr.score,
                isCloudPlayer = true
            });
        }

        // 经典湾区同行作为参照标尺
        entries.AddRange(new[] {
            new LeaderboardOperator {
                name = "汐港电铁局",
                tag = "老牌港岛干线",
                motto = "青湾南岸高密度通勤网络",
                stations = 4 + Mathf.Min(18, d / 3),
                linesCount = 1 + Mathf.Min(6, d / 7),
                dailyProfit = 320 + d * 42,
                score = 1100 + d * 195
            },
            new LeaderboardOperator {
                name = "榕川快轨集团",
                tag = "南溟城际联营",
                motto = "主打百花—东湾城—莞香跨城大站快线",
                stations = 3 + Mathf.Min(26, d / 2),
                linesCount = 1 + Mathf.Min(8, d / 6),
                dailyProfit = 240 + d * 56,
                score = 920 + d * 245
            },
            new LeaderboardOperator {
                name = "镜屿轻轨社",
                tag = "海湾文旅专线",
                motto = "深耕半岛与连汀海湾接驳",
                stations = 2 + Mathf.Min(12, d / 4),
                linesCount = 1 + Mathf.Min(4, d / 9),
                dailyProfit = 190 + d * 26,
                score = 720 + d * 120
            },
            new LeaderboardOperator {
                name = "沧海客运总社",
                tag = "百年商埠车队",
                motto = "老街商业与站前物业稳健派",
                stations = 5 + Mathf.Min(20, d / 3),
                linesCount = 2 + Mathf.Min(7, d / 7),
                dailyProfit = 380 + d * 38,
                score = 1480 + d * 185
            },
            new LeaderboardOperator {
                name = "山海星铁联营",
                tag = "高速智行先驱",
                motto = "瞄准新纪元山海湾环形快网",
                stations = 6 + Mathf.Min(32, d / 2),
                linesCount = 2 + Mathf.Min(10, d / 5),
                dailyProfit = 460 + d * 68,
                score = 1850 + d * 280
            }
        });
        entries.Sort((a, b) => b.score.CompareTo(a.score));
        return entries;
    }

    void RefreshLeaderboardFromSdk(bool triggerReload = true)
    {
        SubmitTapTapLeaderboardScore(true);
        FetchTapTapLeaderboard(() => {
            if (triggerReload && pageRoot && activePageKey == "排行榜")
            {
                RenderLeaderboardPageContent();
            }
        });
    }

    void OpenLeaderboard()
    {
        RenderLeaderboardPageContent();
        RefreshLeaderboardFromSdk(true);
    }

    void RenderLeaderboardPageContent()
    {
        var page = OpenPage("排行榜", "查看 TapTap 云端实时路局排行与本社建设编年史。", "PageArt/album_watercolor");
        Txt(page.transform, "山海群雄榜", 38, Ink, 36, PgH - 255, 500, 54, TextAnchor.MiddleLeft);
        var refreshBtn = Btn(page.transform, "RefreshCloudRank", cloudRankLoading ? "云榜同步中..." : "↻ 刷新 TapTap 云榜", 22, W - 310f, PgH - 256f, 274f, 54f, Indigo, () => {
            RefreshLeaderboardFromSdk(true);
            RenderLeaderboardPageContent();
        });
        TInk(refreshBtn, Color.white);

        var list = BuildBayLeaderboard();
        int playerRank = 1;
        for (int i = 0; i < list.Count; i++) if (list[i].isPlayer) { playerRank = i + 1; break; }

        float contentH = 445f + list.Count * 158f + 360f;
        var content = FleetScroll(page.transform, 36f, PgH - 306f, contentH, scrollKey: "leaderboard");
        float topY = content.sizeDelta.y;
        float cardW = content.sizeDelta.x;
        float innerW = cardW - 8f;

        // 1. 顶部霓虹海报与本社排名总览卡
        float heroH = 405f;
        float y = topY - 12f - heroH;
        var hero = Rect("RankingHeroCard", content, 4f, y, innerW, heroH);
        Rounded(hero, CardBg); SoftShadow(hero); JournalTape(hero.transform, innerW * 0.5f - 72f, heroH - 17f, 144f);
        int palette = currentRegion == "镜屿" ? 2 : currentRegion == "榕川" ? 1 : 0;
        var posterTex = EraPosterGenerator.GetCustomPoster("rank_" + currentRegion, "RAILWAY", palette, 4, 768, 220, 1);
        var posterFrame = CreateNeonPosterFrame(hero.transform, "RankingNeonPoster", 24f, 159f, innerW - 48f, 214f, posterTex, 9f);
        var badge = Rect("RegionTag", posterFrame.transform, 18f, 16f, 240f, 38f);
        Rounded(badge, new Color(0.04f, 0.04f, 0.08f, 0.78f)); badge.GetComponent<Image>().raycastTarget = false;
        Txt(badge.transform, "版图 · " + DisplayPlaceAliases(currentRegion), 21, new Color(0.96f, 0.97f, 1f, 0.95f), 0f, 0f, 240f, 38f, TextAnchor.MiddleCenter).raycastTarget = false;

        string rankHeadline = cloudMyRank > 0
            ? $"{companyName} · 云端 #{cloudMyRank}（综合 #{playerRank}）"
            : $"{companyName} · 当前第 {playerRank} 名";
        Txt(hero.transform, rankHeadline, 32, Ink, 28f, 98f, innerW - 290f, 46f, TextAnchor.MiddleLeft);
        Txt(hero.transform, "综合评分 " + CalculatePlayerRailScore().ToString("N0"), 28, Matcha, innerW - 270f, 99f, 242f, 44f, TextAnchor.MiddleRight);
        Txt(hero.transform, "已连 " + DailyConnectedCount() + " 站  ·  运营 " + lines.Count + " 线  ·  预期日利 " + FormatSignedCoins(income) + "  ·  口岸走廊 " + CrossBaySynergyLinkCount() + "/3",
            22, InkSoft, 28f, 54f, innerW - 56f, 36f, TextAnchor.MiddleLeft);
        Txt(hero.transform, cloudRankStatusText, 21, cloudRankFromSdk ? Matcha : Indigo, 28f, 18f, innerW - 56f, 32f, TextAnchor.MiddleLeft);

        // 2. 山海同行阶梯榜
        for (int i = 0; i < list.Count; i++)
        {
            var op = list[i];
            y -= 16f + 142f;
            var row = Rect("RankRow_" + i, content, 4f, y, innerW, 142f);
            Color rowBg = op.isPlayer
                ? Color.Lerp(CardBg, Matcha, 0.14f)
                : (op.isCloudPlayer ? Color.Lerp(CardBg, Indigo, 0.09f) : CardBg);
            Rounded(row, rowBg);
            SoftShadow(row);

            Color rankCol = i == 0 ? Gold : i == 1 ? Indigo : i == 2 ? new Color(0.72f, 0.45f, 0.24f) : InkSoft;
            var rankBadge = Rect("RankNum", row.transform, 22f, 37f, 68f, 68f);
            Rounded(rankBadge, op.isPlayer ? Matcha : op.isCloudPlayer ? Indigo : new Color(rankCol.r, rankCol.g, rankCol.b, 0.16f));
            Txt(rankBadge.transform, "#" + (i + 1), 28, (op.isPlayer || op.isCloudPlayer) ? Color.white : rankCol, 0f, 0f, 68f, 68f, TextAnchor.MiddleCenter);

            string titleBadge = op.isPlayer ? " ★" : op.isCloudPlayer ? " [TapTap]" : "";
            Txt(row.transform, op.name + titleBadge, 29, Ink, 108f, 82f, 540f, 42f, TextAnchor.MiddleLeft);
            Txt(row.transform, op.tag + " · " + op.motto, 21, InkSoft, 108f, 44f, 560f, 32f, TextAnchor.MiddleLeft);
            string thirdLine = op.isCloudPlayer
                ? ("连通 " + op.stations + " 站  ·  " + op.linesCount + " 线  ·  TapTap 实时云榜")
                : ("连通 " + op.stations + " 站  ·  " + op.linesCount + " 线  ·  日利 " + FormatSignedCoins(op.dailyProfit));
            Txt(row.transform, thirdLine, 21, op.isPlayer ? Matcha : Indigo, 108f, 12f, 560f, 30f, TextAnchor.MiddleLeft);
            Txt(row.transform, op.score.ToString("N0") + " 分", 28, op.isPlayer ? Matcha : Ink, innerW - 220f, 48f, 194f, 44f, TextAnchor.MiddleRight);
        }

        // 3. 公司荣誉编年史
        y -= 22f + 318f;
        var chron = Rect("CompanyChronicle", content, 4f, y, innerW, 318f);
        Rounded(chron, CardBg); SoftShadow(chron);
        Txt(chron.transform, "公司史册 · 里程碑碑林", 30, Ink, 28f, 256f, innerW - 56f, 44f, TextAnchor.MiddleLeft);
        string[] milestones = {
            (lines.Count >= 1 ? "✓ " : "○ ") + "青湾初笛：开通首条商业线路",
            (FoundCount() >= 3 ? "✓ " : "○ ") + "山海留影：累计收录 3 处以上名胜风光（当前 " + FoundCount() + " 处）",
            (macauUnlocked ? "✓ " : "○ ") + "越海镜屿：解锁并通车镜屿版图",
            (guangdongUnlocked ? "✓ " : "○ ") + "北上榕川：解锁并贯通榕川路网",
            (CrossBaySynergyLinkCount() >= 1 ? "✓ " : "○ ") + "山海直通：打通跨区口岸联运走廊（" + CrossBaySynergyLinkCount() + "/3）",
            (income >= 2000f ? "✓ " : "○ ") + "金字招牌：全网预期日利润突破 ¥2,000（当前 " + FormatSignedCoins(income) + "）"
        };
        for (int m = 0; m < milestones.Length; m++)
        {
            bool done = milestones[m].StartsWith("✓");
            Txt(chron.transform, milestones[m], 22, done ? Matcha : InkSoft, 28f, 206f - m * 38f, innerW - 56f, 34f, TextAnchor.MiddleLeft);
        }
    }
}
