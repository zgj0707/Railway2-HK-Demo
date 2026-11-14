using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    sealed class UndoLine
    {
        public Line line; public string name; public Color color; public int skin; public List<int> path; public int[] dispatch; public int trains; public float fare;
    }
    sealed class UndoVehicle { public ResearchVehicle vehicle; public int lineId; }
    sealed class UndoState
    {
        public string label, region; public double coinsBefore, coinsDelta; public float diamondsBefore, rpBefore, diamondsDelta, rpDelta;
        public int nextId, speed, storyStage; public bool macau, guangdong, loanOutstanding, tutorialSkipped;
        public List<UndoLine> lines = new List<UndoLine>();
        public int companyPolicy, bondDaysLeft, lastPolicyDay, bondPrincipal, bondDailyDue, bondsRepaid;
        public bool bondSubsidized;
        public List<int> boardPartners;
        public int[] levels, facLevels; public float[] riderBonuses; public bool[] built, closed, sceneryUnlocked, sceneryFound;
        public List<int> ownedSkins; public List<string> technologies, companyTech = new List<string>(), storySeen = new List<string>();
        public List<UndoVehicle> vehicles = new List<UndoVehicle>();
        public Dictionary<Line, FrozenLineTraffic> frozen = new Dictionary<Line, FrozenLineTraffic>();
    }
    const int MaxUndoSteps = 5;
    UndoState pendingUndo;
    readonly List<UndoState> undoStack = new List<UndoState>();
    Button undoButton;
    Text undoButtonText;
    void ClearUndoHistory()
    {
        pendingUndo = null;
        undoStack.Clear();
        RefreshUndoButton();
    }
    void BuildUndoButton()
    {
        undoButton = Btn(root, "Undo", "↶\n撤销", 26, 962, 190, 108, 98, CardBg, UndoLastAction);
        undoButtonText = undoButton.GetComponentInChildren<Text>();
        TInk(undoButton, InkSoft); SoftShadow(undoButton.gameObject); RefreshUndoButton();
    }
    void RefreshUndoButton()
    {
        if (!undoButton) return;
        undoButton.GetComponent<Image>().color = undoStack.Count == 0 ? new Color(Paper2.r, Paper2.g, Paper2.b, 0.90f) : CardBg;
        if (undoButtonText) undoButtonText.text = undoStack.Count > 1 ? "↶\n撤销(" + undoStack.Count + ")" : "↶\n撤销";
    }
    void BeginUndo(string label)
    {
        var state = new UndoState { label=label, region=currentRegion, coinsBefore=coins, diamondsBefore=diamonds, rpBefore=rp, nextId=nextLineId,
            macau=macauUnlocked, guangdong=guangdongUnlocked, loanOutstanding=loanOutstanding, ownedSkins=new List<int>(ownedLineSkins), speed=timeSpeed,
            storyStage=storyStage, tutorialSkipped=tutorialSkipped,
            levels=new int[stations.Count], built=new bool[stations.Count], closed=new bool[stations.Count],
            companyPolicy=companyPolicy, bondDaysLeft=bondDaysLeft, lastPolicyDay=lastPolicyDay, bondPrincipal=bondPrincipal, bondDailyDue=bondDailyDue, bondsRepaid=bondsRepaid, bondSubsidized=bondSubsidized, boardPartners=new List<int>(boardPartners),
            facLevels=new int[stations.Count], riderBonuses=new float[stations.Count],
            sceneryUnlocked=new bool[sceneries.Count], sceneryFound=new bool[sceneries.Count],
            technologies=new List<string>(techDone), companyTech=new List<string>(companyTechDone), storySeen=new List<string>(storySeen) };
        foreach (var line in lines) state.lines.Add(new UndoLine { line=line, name=line.name, color=line.color, skin=line.skin, path=new List<int>(line.st), dispatch=(int[])line.dispatch.Clone(), trains=line.trains, fare=line.fare });
        for (int i=0;i<stations.Count;i++) { state.levels[i]=stations[i].level; state.built[i]=stations[i].built; state.closed[i]=stations[i].closed; }
        for (int i=0;i<sceneries.Count;i++) { state.sceneryUnlocked[i]=sceneries[i].unlocked; state.sceneryFound[i]=sceneries[i].found; }
        for (int i=0;i<stations.Count;i++) { state.facLevels[i]=stations[i].facLevel; state.riderBonuses[i]=stations[i].riderBonus; }
        foreach (var item in researchVehicles) state.vehicles.Add(new UndoVehicle { vehicle=item, lineId=item.lineId });
        foreach (var item in frozenTraffic) state.frozen[item.Key] = item.Value.Copy();
        pendingUndo=state;
    }
    void EndUndo()
    {
        if (pendingUndo == null) return;
        pendingUndo.coinsDelta=coins-pendingUndo.coinsBefore; pendingUndo.diamondsDelta=diamonds-pendingUndo.diamondsBefore; pendingUndo.rpDelta=rp-pendingUndo.rpBefore;
        undoStack.Add(pendingUndo);
        if (undoStack.Count > MaxUndoSteps) undoStack.RemoveAt(0);
        pendingUndo=null; RefreshUndoButton(); SaveGame();
    }
    void UndoLastAction()
    {
        if (dragging || activePanel || pageRoot || sceneryViewer) return;
        if (undoStack.Count == 0) { Toast("没有可撤销的操作"); return; }
        var state = undoStack[undoStack.Count - 1];
        if (state.region != currentRegion) { Toast("请先回到" + DisplayPlaceAliases(state.region) + "撤销「" + state.label + "」"); return; }
        undoStack.RemoveAt(undoStack.Count - 1);
        pendingUndo=null; CloseLineBubble(); CancelDraft();
        coins-=state.coinsDelta; diamonds-=state.diamondsDelta; rp-=state.rpDelta; nextLineId=state.nextId; timeSpeed=state.speed; UpdateTimeText();
        lines.Clear(); foreach (var record in state.lines)
        { record.line.name=record.name; record.line.color=record.color; record.line.skin=record.skin; record.line.st=new List<int>(record.path); record.line.dispatch=(int[])record.dispatch.Clone(); record.line.trains=record.trains; record.line.fare=record.fare; lines.Add(record.line); }
        for (int i=0;i<stations.Count;i++) { stations[i].level=state.levels[i]; stations[i].built=state.built[i]; stations[i].closed=state.closed[i]; }
        for (int i=0;i<sceneries.Count;i++) { sceneries[i].unlocked=state.sceneryUnlocked[i]; sceneries[i].found=state.sceneryFound[i]; }
        if (state.facLevels != null && state.riderBonuses != null)
            for (int i=0;i<stations.Count && i<state.facLevels.Length && i<state.riderBonuses.Length;i++) { stations[i].facLevel=state.facLevels[i]; stations[i].riderBonus=state.riderBonuses[i]; }
        companyPolicy=state.companyPolicy; bondDaysLeft=state.bondDaysLeft; lastPolicyDay=state.lastPolicyDay;
        bondPrincipal=state.bondPrincipal; bondDailyDue=state.bondDailyDue; bondsRepaid=state.bondsRepaid; bondSubsidized=state.bondSubsidized;
        boardPartners.Clear(); if (state.boardPartners != null) boardPartners.AddRange(state.boardPartners);
        techDone.Clear(); techDone.AddRange(state.technologies);
        companyTechDone.Clear(); if (state.companyTech != null) companyTechDone.AddRange(state.companyTech);
        storyStage = state.storyStage; tutorialSkipped = state.tutorialSkipped;
        storySeen.Clear(); if (state.storySeen != null) storySeen.UnionWith(state.storySeen);
        frozenTraffic.Clear(); foreach (var item in state.frozen) frozenTraffic[item.Key] = item.Value.Copy();
        researchVehicles.Clear(); foreach (var item in state.vehicles) { item.vehicle.lineId=item.lineId; researchVehicles.Add(item.vehicle); }
        macauUnlocked=state.macau; guangdongUnlocked=state.guangdong; loanOutstanding=state.loanOutstanding;
        ownedLineSkins.Clear(); foreach (int skin in state.ownedSkins) ownedLineSkins.Add(skin);
        pendingCheckin.Clear(); foreach (var sc in sceneries) if (sc.unlocked && !sc.found && TryGetSceneryContent(sc,out _,out _)) pendingCheckin.Add(sc);
        hasForecastSignature = false;
        RefreshSceneryViews(); RefreshPendingBadge(); RefreshFeatureGateVisuals(); UpdateStoryObjectiveBanner();
        CreateLineVisuals(); RefreshStationDots(); RecalcIncome();
        if (!RegionUnlocked(currentRegion)) SwitchRegion("香港");
        RefreshHUD(); RefreshUndoButton(); SaveGame(); Toast("已撤销：" + state.label);
    }
}
