using System;
using UnityEngine;

public partial class DemoBootstrap
{
    // ---------- 三域口岸联运协同 ----------
    static readonly string[] CrossBayCorridorNames = { "双渡口双枢纽走廊", "汀屿北关走廊", "三屿跨海虹桥走廊" };
    static readonly string[] CrossBayCorridorDescs = {
        "青屿与锦川线路同时接入「界桥关」或「鹏湾（西渡）」",
        "镜汀与锦川线路同时接入「珠屿（北关）」「西岸工坊区」或「玉琴关」",
        "「三屿虹桥」门户在任意两域以上均有运行线路接驳"
    };

    bool HasPortalServiceInRegion(int portalIdx, string region)
    {
        if (portalIdx < 0 || portalIdx >= stations.Count) return false;
        foreach (var line in lines)
        {
            if (RunningTrains(line) <= 0 || !line.st.Contains(portalIdx)) continue;
            foreach (int stIdx in line.st)
            {
                if (stIdx == portalIdx || stIdx < 0 || stIdx >= stations.Count) continue;
                if (!stations[stIdx].closed && stations[stIdx].region == region) return true;
            }
        }
        return false;
    }

    int FindStationIndexByExactName(string exactName)
    {
        for (int i = 0; i < stations.Count; i++)
            if (stations[i].name == exactName) return i;
        return -1;
    }

    bool CrossBayCorridorActive(int index)
    {
        switch (index)
        {
            case 0:
            {
                int luo = FindStationIndexByExactName("界桥关");
                int sz = FindStationIndexByExactName("鹏湾（西渡）");
                bool hkSide = HasPortalServiceInRegion(luo, "青屿") || HasPortalServiceInRegion(sz, "青屿");
                bool gdSide = HasPortalServiceInRegion(luo, "锦川") || HasPortalServiceInRegion(sz, "锦川");
                return hkSide && gdSide;
            }
            case 1:
            {
                int zh = FindStationIndexByExactName("珠屿（北关）");
                int zha = FindStationIndexByExactName("西岸工坊区");
                int hq = FindStationIndexByExactName("玉琴关");
                bool moSide = HasPortalServiceInRegion(zh, "镜汀") || HasPortalServiceInRegion(zha, "镜汀") || HasPortalServiceInRegion(hq, "镜汀");
                bool gdSide = HasPortalServiceInRegion(zh, "锦川") || HasPortalServiceInRegion(zha, "锦川") || HasPortalServiceInRegion(hq, "锦川");
                return moSide && gdSide;
            }
            case 2:
            {
                int hzm = FindStationIndexByExactName("三屿虹桥");
                int regions = (HasPortalServiceInRegion(hzm, "青屿") ? 1 : 0)
                            + (HasPortalServiceInRegion(hzm, "镜汀") ? 1 : 0)
                            + (HasPortalServiceInRegion(hzm, "锦川") ? 1 : 0);
                return regions >= 2;
            }
            default:
                return false;
        }
    }

    int CrossBaySynergyLinkCount()
    {
        int count = 0;
        for (int i = 0; i < 3; i++) if (CrossBayCorridorActive(i)) count++;
        return count;
    }

    float CrossBaySynergyDailyBonus()
    {
        int links = CrossBaySynergyLinkCount();
        if (links <= 0) return 0f;
        float perLink = 260f + Mathf.Min(360f, DailyConnectedCount() * 10f);
        return links * perLink * CoTechRevenueMult() * PlaqueBayBonusMult();
    }

    float CrossBayDemandMult(int stationIdx)
    {
        int links = CrossBaySynergyLinkCount();
        if (links <= 0 || stationIdx < 0 || stationIdx >= stations.Count) return 1f;
        return stations[stationIdx].portal ? (1f + links * 0.12f) : (1f + links * 0.04f);
    }

    string CrossBaySynergySummary()
    {
        int links = CrossBaySynergyLinkCount();
        if (links <= 0) return "尚未贯通跨区联运走廊（接入任一口岸两地线路即享联运补贴与客流加成）";
        float bonus = CrossBaySynergyDailyBonus();
        return $"已贯通 {links} / 3 条跨海走廊 · 每日联运补贴 +¥{Mathf.RoundToInt(bonus)} · 全网需求 +{links * 4}%（口岸 +{links * 12}%）";
    }
}
