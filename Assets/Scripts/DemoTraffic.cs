using System;
using System.Collections.Generic;
using UnityEngine;

public partial class DemoBootstrap
{
    // 聚合式出行分配：按线路与换乘搜索可达路径，再按站点和线路运力约束服务量。
    sealed class TrafficResult
    {
        public float[] demand, served;
        public Dictionary<Line, float> lineDemand = new Dictionary<Line, float>();
        public Dictionary<Line, float> lineServed = new Dictionary<Line, float>();
    }
    sealed class TrafficEdge { public int target; public float cost; }
    sealed class TrafficTrip { public float amount; public HashSet<int> stations, lines; }
    TrafficResult traffic;
    readonly System.Random trafficRandom = new System.Random();
    static readonly string[] TrafficEventNames = { "日常运营", "社区活动", "景点活动", "道路施工", "暴雨出行减少" };
    static readonly float[] TrafficEventFactors = { 1f, 1.25f, 1.35f, 0.80f, 0.70f };

    // 每个模拟客流单位对应 100 人次；收入仍使用原经济尺度。
    const float PassengerScale = 100f;
    float CityDemandWeight(Station station)
    {
        if (station.portal) return 1.6f;
        if (station.region == "香港") return 1.25f;
        if (station.region == "澳门") return 0.75f;
        switch (station.city)
        {
            case "广州市": case "深圳市": return station.county ? 1.1f : 1.7f;
            case "佛山市": case "东莞市": return station.county ? 0.95f : 1.35f;
            case "惠州市": case "珠海市": case "中山市": case "江门市": return station.county ? 0.8f : 1.1f;
            default: return station.county ? 0.65f : 0.9f;
        }
    }
    float StationBaselineDemand(int i)
    {
        var s = stations[i];
        float baseValue = 80f;
        switch (s.name)
        {
            case "沙田": case "大围": case "天水围": case "元朗": case "上水": case "粉岭": case "荃湾": case "屯门": case "氹仔": case "青洲": case "石排湾": baseValue *= 1.25f; break;
            case "中环": case "金钟": case "尖沙咀": case "旺角": case "铜锣湾": case "大三巴": case "妈阁": case "澳门机场": case "深圳": case "广州": baseValue *= 1.30f; break;
        }
        foreach (var sc in sceneries) if (sc.triggerStation == s.name && s.region == sc.region) { baseValue *= 1.15f; break; }
        return baseValue * CityDemandWeight(s);
    }
    float StationBaseDemand(int i)
    {
        var s = stations[i];
        float hour = Mathf.Repeat(8f + dayClock / 60f * 24f, 24f);
        float morning = Mathf.Exp(-Mathf.Pow((hour - 8.5f) / 2f, 2f));
        float evening = Mathf.Exp(-Mathf.Pow((hour - 18f) / 2.4f, 2f));
        float leisure = Mathf.Exp(-Mathf.Pow((hour - 14f) / 4f, 2f));
        return StationBaselineDemand(i) * (0.72f + 0.30f * morning + 0.30f * evening + 0.14f * leisure) * TrafficEventFactors[s.trafficEvent];
    }
    float StationCapacity(int i) { return (180f + (stations[i].level - 1) * 100f) * PassengerScale; }
    float LineCapacity(Line line) { return PassengerScale * 260f * Mathf.Max(1, line.trains) * (1f + 0.35f * line.up[1]) * (1f + 0.25f * line.up[2]); }
    void RollTrafficEvents()
    {
        foreach (var station in stations)
            station.trafficEvent = trafficRandom.NextDouble() < 0.16 ? trafficRandom.Next(1, TrafficEventNames.Length) : 0;
    }
    TrafficResult CalculateTraffic(Line replacement = null, List<int> replacementPath = null, Line addition = null)
    {
        var serviceLines = new List<Line>(lines); if (addition != null) serviceLines.Add(addition);
        int count = stations.Count, lineCount = serviceLines.Count;
        var result = new TrafficResult { demand = new float[count], served = new float[count] };
        int[,] node = new int[lineCount, count];
        for (int l=0; l<lineCount; l++) for (int i=0; i<count; i++) node[l,i] = -1;
        var nodeStation = new List<int>(); var nodeLine = new List<int>(); var edges = new List<List<TrafficEdge>>();
        for (int l=0; l<lineCount; l++)
        {
            var path = serviceLines[l] == replacement ? replacementPath : serviceLines[l].st;
            foreach (int st in path) if (node[l,st] < 0)
            { node[l,st]=edges.Count; nodeStation.Add(st); nodeLine.Add(l); edges.Add(new List<TrafficEdge>()); }
            for (int k=0; k<path.Count-1; k++)
            {
                int a=path[k], b=path[k+1]; string region=SegmentRegion(a,b); if (region == null) continue;
                float distance=Vector2.Distance(PosInRegion(a,region),PosInRegion(b,region));
                float travel=distance / (58f + 8f*serviceLines[l].up[0]);
                int na=node[l,a], nb=node[l,b];
                edges[na].Add(new TrafficEdge { target=nb, cost=travel }); edges[nb].Add(new TrafficEdge { target=na, cost=travel });
            }
            result.lineDemand[serviceLines[l]]=0; result.lineServed[serviceLines[l]]=0;
        }
        for (int st=0; st<count; st++) if (!stations[st].closed)
            for (int a=0; a<lineCount; a++) if (node[a,st]>=0)
                for (int b=a+1; b<lineCount; b++) if (node[b,st]>=0)
                {
                    float wait=2f / (Mathf.Max(1,serviceLines[b].trains) * (1f+0.25f*serviceLines[b].up[2]));
                    float reverse=2f / (Mathf.Max(1,serviceLines[a].trains) * (1f+0.25f*serviceLines[a].up[2]));
                    edges[node[a,st]].Add(new TrafficEdge{target=node[b,st],cost=2f+wait});
                    edges[node[b,st]].Add(new TrafficEdge{target=node[a,st],cost=2f+reverse});
                }
        int n=edges.Count; var trips=new List<TrafficTrip>();
        var baseDemand = new float[count];
        for (int i = 0; i < count; i++) baseDemand[i] = StationBaseDemand(i);
        for (int origin=0; origin<count; origin++)
        {
            if (stations[origin].closed) continue;
            var dist=new float[n]; var prev=new int[n]; var done=new bool[n];
            for (int k=0;k<n;k++) { dist[k]=float.PositiveInfinity; prev[k]=-1; }
            bool starts=false;
            for (int l=0;l<lineCount;l++) if (node[l,origin]>=0)
            { dist[node[l,origin]]=2f/(Mathf.Max(1,serviceLines[l].trains)*(1f+0.25f*serviceLines[l].up[2])); starts=true; }
            if (!starts) continue;
            for (int step=0; step<n; step++)
            {
                int u=-1; float best=float.PositiveInfinity;
                for (int k=0;k<n;k++) if (!done[k] && dist[k]<best) { u=k; best=dist[k]; }
                if (u<0) break; done[u]=true;
                foreach (var e in edges[u]) if (dist[u]+e.cost<dist[e.target]) { dist[e.target]=dist[u]+e.cost; prev[e.target]=u; }
            }
            for (int dest=0;dest<count;dest++)
            {
                if (dest==origin || stations[dest].closed) continue;
                int target=-1; float best=float.PositiveInfinity;
                for (int l=0;l<lineCount;l++) if (node[l,dest]>=0 && dist[node[l,dest]]<best) {target=node[l,dest];best=dist[target];}
                if (target<0) continue;
                float amount=PassengerScale*0.58f*baseDemand[origin]*baseDemand[dest]/80f/(1f+best*0.07f);
                var route=new List<int>(); for (int u=target;u>=0;u=prev[u]) route.Add(u);
                var seenStations=new HashSet<int>(); var seenLines=new HashSet<int>();
                foreach (int u in route) { if (!stations[nodeStation[u]].closed) seenStations.Add(nodeStation[u]); seenLines.Add(nodeLine[u]); }
                foreach (int st in seenStations) result.demand[st]+=amount;
                foreach (int l in seenLines) result.lineDemand[serviceLines[l]]+=amount;
                trips.Add(new TrafficTrip { amount=amount, stations=seenStations, lines=seenLines });
            }
        }
        foreach (var trip in trips)
        {
            float factor=1f;
            foreach (int st in trip.stations) if (result.demand[st]>0) factor=Mathf.Min(factor, StationCapacity(st)/result.demand[st]);
            foreach (int l in trip.lines) if (result.lineDemand[serviceLines[l]]>0) factor=Mathf.Min(factor, LineCapacity(serviceLines[l])/result.lineDemand[serviceLines[l]]);
            float served=trip.amount*Mathf.Clamp01(factor);
            foreach (int st in trip.stations) result.served[st]+=served;
            foreach (int l in trip.lines) result.lineServed[serviceLines[l]]+=served;
        }
        return result;
    }
    float StationPassengerFlow(int index) { return traffic == null ? 0 : traffic.served[index]; }
    float StationRequestedFlow(int index) { return traffic == null ? 0 : traffic.demand[index]; }
    float LinePassengerFlow(Line line) { return traffic != null && traffic.lineServed.TryGetValue(line,out float value) ? value : 0f; }
    float TrafficRevenue(Line line, TrafficResult result)
    {
        return result.lineServed.TryGetValue(line,out float served) ? served / PassengerScale * 1.5f * TechMult() : 0f;
    }
}
