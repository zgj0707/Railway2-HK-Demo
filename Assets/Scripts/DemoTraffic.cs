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
        public Dictionary<Line, float> lineRevenue = new Dictionary<Line, float>();
        public Dictionary<Line, float> linePeakDemand = new Dictionary<Line, float>();
        public float[] sectionDemand, sectionServed, sectionCapacity, originBudget, originDemand;
        public float totalPassengers, totalRevenue;
    }
    sealed class TrafficEdge { public int target, section; public float cost, distance; }
    sealed class TrafficTrip { public float amount, fare, bonus; public int[] stations, sections; public Dictionary<int,float> lineDistance; }
    sealed class TrafficHeap
    {
        struct Entry { public int node; public float cost; public Entry(int n,float c){node=n;cost=c;} }
        readonly List<Entry> heap = new List<Entry>();
        public int Count { get { return heap.Count; } }
        public void Push(int node,float cost)
        {
            int i=heap.Count; heap.Add(new Entry(node,cost));
            while(i>0) { int parent=(i-1)/2; if(heap[parent].cost<=cost) break; heap[i]=heap[parent]; i=parent; }
            heap[i]=new Entry(node,cost);
        }
        public int Pop()
        {
            int node=heap[0].node, last=heap.Count-1; var tail=heap[last]; heap.RemoveAt(last);
            if(last>0) { int i=0; while(true) { int left=i*2+1; if(left>=last)break; int right=left+1, child=right<last && heap[right].cost<heap[left].cost ? right : left; if(heap[child].cost>=tail.cost)break; heap[i]=heap[child]; i=child; } heap[i]=tail; }
            return node;
        }
    }
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
        float hour = dayClock / GameDaySeconds * 24f;
        float morning = Mathf.Exp(-Mathf.Pow((hour - 8.5f) / 2f, 2f));
        float evening = Mathf.Exp(-Mathf.Pow((hour - 18f) / 2.4f, 2f));
        float leisure = Mathf.Exp(-Mathf.Pow((hour - 14f) / 4f, 2f));
        return StationBaselineDemand(i) * (0.72f + 0.30f * morning + 0.30f * evening + 0.14f * leisure) * TrafficEventFactors[s.trafficEvent] * ResearchFactor(ResearchEffect.Demand);
    }
    int StationKind(int i)
    {
        string name = stations[i].name;
        switch (name)
        {
            case "沙田": case "大围": case "天水围": case "元朗": case "上水": case "粉岭": case "荃湾": case "屯门": case "氹仔": case "青洲": case "石排湾": return 1;
            case "中环": case "金钟": case "尖沙咀": case "旺角": case "铜锣湾": case "广州": case "深圳（西九龙）": return 2;
        }
        foreach (var sc in sceneries) if (sc.triggerStation == name && sc.region == stations[i].region) return 3;
        return stations[i].portal ? 4 : 0;
    }
    string StationAreaHint(int i)
    {
        switch (StationKind(i)) {
            case 1: return "该站附近多为居住区，客流早出晚归";
            case 2: return "周边商业与办公集中，早高峰到达、晚高峰出发较多";
            case 3: return "周边景点较多，白天游客较集中";
            case 4: return "区域交界枢纽，适合衔接跨区线路";
            default: return "周边以日常出行为主，客流取决于线路连接";
        }
    }
    float DirectionFactor(int index, bool origin)
    {
        float hour = dayClock / GameDaySeconds * 24f;
        float morning = Mathf.Exp(-Mathf.Pow((hour - 8f) / 2f, 2f));
        float evening = Mathf.Exp(-Mathf.Pow((hour - 18f) / 2f, 2f));
        float noon = Mathf.Exp(-Mathf.Pow((hour - 13f) / 4f, 2f));
        int kind = StationKind(index);
        if (kind == 1) return 0.72f + (origin ? 0.9f * morning + 0.12f * evening : 0.12f * morning + 0.9f * evening);
        if (kind == 2) return 0.72f + (origin ? 0.12f * morning + 0.9f * evening : 0.9f * morning + 0.12f * evening);
        if (kind == 3) return 0.65f + 0.6f * noon;
        return 0.85f + 0.25f * (morning + evening);
    }
    int DispatchSlot() { float h = dayClock / GameDaySeconds * 24f; return h >= 6f && h < 10f ? 0 : h >= 10f && h < 16f ? 1 : h >= 16f && h < 20f ? 2 : 3; }
    int RunningTrains(Line line) { return Mathf.Clamp(line.dispatch[DispatchSlot()], 0, line.trains); }
    // 默认车为经济型；研究实物按特性影响维护、运力与长线收益。
    static readonly float[] FleetMaintenanceTraits = {0.75f,0.85f,0.9f,1f,1.12f,1.05f,1.1f,1.3f,4.5f};
    static readonly float[] FleetCapacityTraits = {0.9f,1.15f,1.25f,1.55f,1.8f,1.8f,2f,2.8f,2.2f};
    static readonly float[] FleetSpeedTraits = {0.65f,0.72f,0.76f,0.85f,1.05f,0.95f,1f,1.1f,1.65f};
    float FleetTrait(Line line, int trait, bool runningOnly = false)
    {
        int needed = runningOnly ? RunningTrains(line) : line.trains;
        if (needed <= 0) return 1f;
        float total = 0f; int count = 0;
        // 已配属的研究机车优先运行；不足的名额由线路基础车辆补足。
        foreach (var v in researchVehicles) if (v.lineId == line.id && v.node >= 0 && v.node < LocoNames.Length && count < needed) {
            total += trait == 0 ? FleetMaintenanceTraits[v.node] : trait == 1 ? FleetCapacityTraits[v.node] : FleetSpeedTraits[v.node]; count++;
        }
        while (count < needed) { total += 1f; count++; }
        return total / needed * (trait == 2 ? ResearchFactor(ResearchEffect.Speed) : 1f);
    }
    void SettleRemovedLineTax(Line line)
    {
        float profit=line.todayGross-line.todayMaintenance-line.todayTrack;
        coins-=Mathf.Max(0f,profit)*0.1f;
    }
    void ReleaseLineFleet(Line line) { foreach (var v in researchVehicles) if (v.lineId == line.id) v.lineId = 0; }
    void SyncLineFleet(Line line)
    {
        int count = 1; foreach (var v in researchVehicles) if (v.lineId == line.id) count++;
        line.trains = count;
        for (int i=0;i<line.dispatch.Length;i++) line.dispatch[i]=Mathf.Min(line.dispatch[i],count);
    }
    float StationCapacity(int i) { return (180f + (stations[i].level - 1) * 100f) * PassengerScale * ResearchFactor(ResearchEffect.Station); }
    // 单方向单区段的日化运力；不同区段可由不同乘客复用。
    float FleetCapacity(Line line) { return 4000f * RunningTrains(line) * FleetTrait(line, 1, true) * ResearchFactor(ResearchEffect.Capacity); }
    float LineCapacity(Line line) { return FleetCapacity(line); }
    Vector2 EconomicPosition(int index)
    {
        var station=stations[index];
        if (StationInRegion(index,"广东")) return PosInRegion(index,"广东");
        if (station.region == "香港") {
            var anchor=stations.FindIndex(x=>x.name=="深圳（西九龙）");
            Vector2 origin=anchor>=0 ? PosInRegion(anchor,"广东") : new Vector2(615,900);
            Vector2 localAnchor=anchor>=0 ? PosInRegion(anchor,"香港") : new Vector2(590,865);
            return origin+(PosInRegion(index,"香港")-localAnchor)*0.4f;
        }
        if (station.region == "澳门") {
            var anchor=stations.FindIndex(x=>x.name=="珠海（关闸）");
            Vector2 origin=anchor>=0 ? PosInRegion(anchor,"广东") : new Vector2(540,850);
            Vector2 localAnchor=anchor>=0 ? PosInRegion(anchor,"澳门") : new Vector2(540,900);
            return origin+(PosInRegion(index,"澳门")-localAnchor)*0.4f;
        }
        return Pos(index);
    }
    float EconomicSegmentDistance(int a,int b,string region)
    {
        float scale = region == "广东" ? 1f : 0.4f;
        return Vector2.Distance(PosInRegion(a,region),PosInRegion(b,region))*scale;
    }
    float EconomicPathLength(List<int> path)
    {
        float total=0; for(int i=0;i<path.Count-1;i++) {string region=SegmentRegion(path[i],path[i+1]); if(region!=null) total+=EconomicSegmentDistance(path[i],path[i+1],region);}
        return total;
    }
    void RollTrafficEvents()
    {
        foreach (var station in stations)
            station.trafficEvent = trafficRandom.NextDouble() < 0.16 ? trafficRandom.Next(1, TrafficEventNames.Length) : 0;
    }
    TrafficResult CalculateTraffic(Line replacement = null, List<int> replacementPath = null, Line addition = null)
    {
        var serviceLines = new List<Line>(lines); if (addition != null) serviceLines.Add(addition);
        int count=stations.Count, lineCount=serviceLines.Count;
        var result=new TrafficResult { demand=new float[count], served=new float[count], originBudget=new float[count], originDemand=new float[count] };
        int[,] node=new int[lineCount,count]; for(int l=0;l<lineCount;l++)for(int i=0;i<count;i++)node[l,i]=-1;
        var nodeStation=new List<int>(); var nodeLine=new List<int>(); var edges=new List<List<TrafficEdge>>();
        var capacity=new List<float>(); var sectionOwners=new List<int>();
        for(int l=0;l<lineCount;l++)
        {
            var line=serviceLines[l]; var path=line==replacement?replacementPath:line.st;
            result.lineDemand[line]=0; result.lineServed[line]=0; result.lineRevenue[line]=0; result.linePeakDemand[line]=0;
            if(RunningTrains(line)==0) continue;
            foreach(int st in path) if(node[l,st]<0) {node[l,st]=edges.Count;nodeStation.Add(st);nodeLine.Add(l);edges.Add(new List<TrafficEdge>());}
            for(int k=0;k<path.Count-1;k++) {
                int a=path[k],b=path[k+1]; string region=SegmentRegion(a,b); if(region==null)continue;
                float distance=Mathf.Max(1f,EconomicSegmentDistance(a,b,region));
                float travel=distance/(58f*FleetTrait(line,2)) + distance * 0.002f * 0.3f; int na=node[l,a],nb=node[l,b];
                int forward=capacity.Count; capacity.Add(FleetCapacity(line)); sectionOwners.Add(l); int reverse=capacity.Count; capacity.Add(FleetCapacity(line)); sectionOwners.Add(l);
                edges[na].Add(new TrafficEdge{target=nb,cost=travel,section=forward,distance=distance});
                edges[nb].Add(new TrafficEdge{target=na,cost=travel,section=reverse,distance=distance});
            }
        }
        for(int st=0;st<count;st++) if(!stations[st].closed)
            for(int a=0;a<lineCount;a++)if(node[a,st]>=0)
                for(int b=a+1;b<lineCount;b++)if(node[b,st]>=0) {
                    float wait=2f/Mathf.Max(1,RunningTrains(serviceLines[b]));
                    float reverse=2f/Mathf.Max(1,RunningTrains(serviceLines[a]));
                    edges[node[a,st]].Add(new TrafficEdge{target=node[b,st],cost=(2f+wait)*ResearchCostFactor(ResearchEffect.Transfer)+serviceLines[b].fare*0.4f,section=-1});
                    edges[node[b,st]].Add(new TrafficEdge{target=node[a,st],cost=(2f+reverse)*ResearchCostFactor(ResearchEffect.Transfer)+serviceLines[a].fare*0.4f,section=-1});
                }
        result.sectionCapacity=capacity.ToArray(); result.sectionDemand=new float[capacity.Count]; result.sectionServed=new float[capacity.Count];
        int n=edges.Count; var trips=new List<TrafficTrip>(); var baseDemand=new float[count]; var stationCap=new float[count]; var positions=new Vector2[count];
        for(int i=0;i<count;i++){baseDemand[i]=StationBaseDemand(i);stationCap[i]=StationCapacity(i);positions[i]=EconomicPosition(i);}
        for(int origin=0;origin<count;origin++)
        {
            if(stations[origin].closed)continue;
            var dist=new float[n];var prev=new int[n];var prevSection=new int[n];var prevDistance=new float[n];var done=new bool[n];
            for(int k=0;k<n;k++){dist[k]=float.PositiveInfinity;prev[k]=-1;prevSection[k]=-1;}
            var heap=new TrafficHeap();
            for(int l=0;l<lineCount;l++)if(node[l,origin]>=0){int source=node[l,origin];dist[source]=2f/Mathf.Max(1,RunningTrains(serviceLines[l])) + serviceLines[l].fare*0.4f;heap.Push(source,dist[source]);}
            if(heap.Count==0)continue;
            while(heap.Count>0){int u=heap.Pop();if(done[u])continue;done[u]=true;foreach(var e in edges[u])if(!done[e.target]&&dist[u]+e.cost<dist[e.target]){dist[e.target]=dist[u]+e.cost;prev[e.target]=u;prevSection[e.target]=e.section;prevDistance[e.target]=e.distance;heap.Push(e.target,dist[e.target]);}}
            // 起点的总出行预算固定，目的地权重包括不可达站点，不能靠添加站点生成无限乘客。
            float weightTotal=0f; var weights=new float[count];
            for(int dest=0;dest<count;dest++)if(dest!=origin&&!stations[dest].closed){float direct=Vector2.Distance(positions[origin],positions[dest]);weights[dest]=baseDemand[dest]*DirectionFactor(dest,false)/(50f+direct);weightTotal+=weights[dest];}
            if(weightTotal<=0)continue;
            float originBudget=baseDemand[origin]*PassengerScale*3.1f*DirectionFactor(origin,true);
            result.originBudget[origin]=originBudget;
            for(int dest=0;dest<count;dest++)
            {
                if(weights[dest]<=0)continue; int target=-1;float best=float.PositiveInfinity;
                for(int l=0;l<lineCount;l++)if(node[l,dest]>=0&&dist[node[l,dest]]<best){target=node[l,dest];best=dist[target];}
                if(target<0)continue;
                var sections=new List<int>(); var stationSet=new HashSet<int>{origin,dest}; var lineDist=new Dictionary<int,float>();float pathDistance=0;
                for(int u=target;prev[u]>=0;u=prev[u]) {
                    int v=prev[u]; int section=prevSection[u];
                    if(section>=0){sections.Add(section);int l=nodeLine[u];float d=prevDistance[u];lineDist[l]=lineDist.TryGetValue(l,out var prevD)?prevD+d:d;pathDistance+=d;}
                    else if(nodeStation[u]==nodeStation[v]) stationSet.Add(nodeStation[u]);
                }
                if(pathDistance<=0||lineDist.Count==0)continue;
                float directDistance=Vector2.Distance(positions[origin],positions[dest]);
                float detour=pathDistance/Mathf.Max(30f,directDistance);
                float detourFactor=detour<=1.25f?1f:Mathf.Clamp(1f-1.2f*(detour-1.25f),0.15f,1f);
                float fareBase=0f,speed=0f;
                foreach(var part in lineDist){float share=part.Value/pathDistance;fareBase+=serviceLines[part.Key].fare*share;speed+=FleetTrait(serviceLines[part.Key],2)*share;}
                // 票价基于起终点直达距离，绕远不能凭空增加票款。
                float fare=fareBase+0.002f*directDistance;
                float priceFactor=Mathf.Clamp(0.65f*Mathf.Exp(-0.85f*(fare/(1.5f+0.002f*directDistance)-1f)),0.1f,1f);
                float amount=originBudget*weights[dest]/weightTotal*detourFactor*priceFactor/(1f+best*0.07f);
                result.originDemand[origin]+=amount;
                if(amount<=0.001f)continue;
                float bonus=1f+Mathf.Max(0f,speed-1f)*Mathf.Clamp01(directDistance/1200f)*0.5f;
                var stationArray=new int[stationSet.Count];stationSet.CopyTo(stationArray);
                foreach(int st in stationArray)result.demand[st]+=amount;
                foreach(int section in sections)result.sectionDemand[section]+=amount;
                foreach(var part in lineDist)result.lineDemand[serviceLines[part.Key]]+=amount;
                trips.Add(new TrafficTrip{amount=amount,fare=fare,bonus=bonus,stations=stationArray,sections=sections.ToArray(),lineDistance=lineDist});
            }
        }
        for(int section=0;section<sectionOwners.Count;section++) {
            var line=serviceLines[sectionOwners[section]];
            result.linePeakDemand[line]=Mathf.Max(result.linePeakDemand[line],result.sectionDemand[section]);
        }
        float revenueMultiplier = TechMult();
        foreach(var trip in trips)
        {
            float factor=1f;
            foreach(int st in trip.stations)if(result.demand[st]>0)factor=Mathf.Min(factor,stationCap[st]/result.demand[st]);
            foreach(int section in trip.sections)if(result.sectionDemand[section]>0)factor=Mathf.Min(factor,result.sectionCapacity[section]/result.sectionDemand[section]);
            float served=trip.amount*Mathf.Clamp01(factor); if(served<=0)continue;
            foreach(int st in trip.stations)result.served[st]+=served;
            foreach(int section in trip.sections)result.sectionServed[section]+=served;
            float distance=0;foreach(float d in trip.lineDistance.Values)distance+=d;
            float revenue=served*trip.fare/10f*trip.bonus*revenueMultiplier;
            foreach(var part in trip.lineDistance){var line=serviceLines[part.Key];result.lineServed[line]+=served;result.lineRevenue[line]+=revenue*part.Value/distance;}
            result.totalPassengers+=served;result.totalRevenue+=revenue;
        }
        return result;
    }
    float StationPassengerFlow(int index) { return traffic == null ? 0 : traffic.served[index]; }
    float StationRequestedFlow(int index) { return traffic == null ? 0 : traffic.demand[index]; }
    float LinePassengerFlow(Line line) { return traffic != null && traffic.lineServed.TryGetValue(line,out float value) ? value : 0f; }
    float LineRequestedFlow(Line line) { return traffic != null && traffic.lineDemand.TryGetValue(line,out float value) ? value : 0f; }
    float LinePeakSectionDemand(Line line) { return traffic != null && traffic.linePeakDemand.TryGetValue(line,out float value) ? value : 0f; }
    sealed class LineForecastData { public float gross, vehicle, track, tax; public float Profit { get { return gross - vehicle - track - tax; } } }
    readonly Dictionary<Line, LineForecastData> lineForecasts = new Dictionary<Line, LineForecastData>();
    readonly TrafficResult[] trafficSlots = new TrafficResult[4];
    int forecastSignature; bool hasForecastSignature;
    int ForecastSignature()
    {
        unchecked {
            int hash = 17;
            foreach (var line in lines) {
                hash=hash*31+line.GetHashCode(); hash=hash*31+line.trains; hash=hash*31+line.fare.GetHashCode();
                foreach (int v in line.st) hash=hash*31+v;
                foreach (int v in line.dispatch) hash=hash*31+v;
            }
            foreach (var station in stations) { hash=hash*31+station.level; hash=hash*31+(station.closed?1:0); hash=hash*31+station.trafficEvent; }
            foreach (var v in researchVehicles) { hash=hash*31+v.node; hash=hash*31+v.lineId; }
            foreach (string key in techDone) hash=hash*31+key.GetHashCode();
            return hash;
        }
    }
    void RebuildForecasts()
    {
        InitHistoryTree(); SyncResearchEffects();
        int signature=ForecastSignature();
        if (hasForecastSignature && signature==forecastSignature) return;
        lineForecasts.Clear(); foreach (var line in lines) lineForecasts[line] = new LineForecastData();
        float savedClock = dayClock;
        float[] hours = { 8f, 13f, 18f, 1f }, fractions = { 4f/24f, 6f/24f, 4f/24f, 10f/24f };
        try {
            for (int slot=0; slot<4; slot++) {
                dayClock = hours[slot] * GameDaySeconds / 24f;
                var projected = CalculateTraffic(); trafficSlots[slot] = projected;
                foreach (var line in lines) {
                    var f = lineForecasts[line]; f.gross += TrafficRevenue(line,projected)*fractions[slot];
                    f.vehicle += VehicleMaintenance(line)*fractions[slot]; f.track += TrackMaintenance(line)*fractions[slot];
                }
            }
        } finally { dayClock = savedClock; }
        foreach (var f in lineForecasts.Values) f.tax = Mathf.Max(0f,f.gross-f.vehicle-f.track)*0.1f;
        forecastSignature=signature; hasForecastSignature=true;
    }
    LineForecastData ForecastFor(Line line) { return lineForecasts.TryGetValue(line,out var f) ? f : new LineForecastData(); }
    float VehicleMaintenance(Line line) { return (line.trains * 4f + RunningTrains(line) * 24f) * FleetTrait(line, 0, false) * ResearchCostFactor(ResearchEffect.Vehicle); }
    float TrackMaintenance(Line line) { return (40f + EconomicPathLength(line.st) * 0.25f + UniqueCount(line.st) * 5f) * ResearchCostFactor(ResearchEffect.Track); }
    float LineProfitRate(Line line, TrafficResult result)
    {
        float beforeTax = TrafficRevenue(line,result) - VehicleMaintenance(line) - TrackMaintenance(line);
        return beforeTax - Mathf.Max(0f,beforeTax) * 0.1f;
    }
    float TrafficRevenue(Line line, TrafficResult result)
    {
        return result != null && result.lineRevenue.TryGetValue(line,out float value) ? value : 0f;
    }
}
