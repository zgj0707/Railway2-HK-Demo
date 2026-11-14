using System;
using UnityEngine;

public partial class DemoBootstrap
{
    // ---------- 三地口岸联运协同（深港 / 澳珠 / 港珠澳） ----------
    static readonly string[] CrossBayCorridorNames = { "深港双枢纽走廊", "澳珠口岸走廊", "港珠澳跨海走廊" };
    static readonly string[] CrossBayCorridorDescs = {
        "香港与南粤线路同时接入「罗湖」或「深圳（西九龙）」",
        "濠江与南粤线路同时接入「珠海（关闸）」「珠澳工业区」或「横琴口岸」",
        "「港珠澳」门户在任意两地以上均有运行线路接驳"
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
                int luo = FindStationIndexByExactName("罗湖");
                int sz = FindStationIndexByExactName("深圳（西九龙）");
                bool hkSide = HasPortalServiceInRegion(luo, "香港") || HasPortalServiceInRegion(sz, "香港");
                bool gdSide = HasPortalServiceInRegion(luo, "广东") || HasPortalServiceInRegion(sz, "广东");
                return hkSide && gdSide;
            }
            case 1:
            {
                int zh = FindStationIndexByExactName("珠海（关闸）");
                int zha = FindStationIndexByExactName("珠澳工业区");
                int hq = FindStationIndexByExactName("横琴口岸");
                bool moSide = HasPortalServiceInRegion(zh, "澳门") || HasPortalServiceInRegion(zha, "澳门") || HasPortalServiceInRegion(hq, "澳门");
                bool gdSide = HasPortalServiceInRegion(zh, "广东") || HasPortalServiceInRegion(zha, "广东") || HasPortalServiceInRegion(hq, "广东");
                return moSide && gdSide;
            }
            case 2:
            {
                int hzm = FindStationIndexByExactName("港珠澳");
                int regions = (HasPortalServiceInRegion(hzm, "香港") ? 1 : 0)
                            + (HasPortalServiceInRegion(hzm, "澳门") ? 1 : 0)
                            + (HasPortalServiceInRegion(hzm, "广东") ? 1 : 0);
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
        return links * perLink * CoTechRevenueMult();
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
        return $"已贯通 {links} / 3 条湾区走廊 · 每日联运补贴 +¥{Mathf.RoundToInt(bonus)} · 全网需求 +{links * 4}%（口岸 +{links * 12}%）";
    }
}
