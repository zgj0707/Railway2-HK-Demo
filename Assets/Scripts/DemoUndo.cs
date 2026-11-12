using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    sealed class UndoLine
    {
        public Line line; public string name; public List<int> path; public int[] dispatch; public int trains; public float fare;
    }
    sealed class UndoVehicle { public ResearchVehicle vehicle; public int lineId; }
    sealed class UndoState
    {
        public string label; public float coinsBefore, diamondsBefore, rpBefore, coinsDelta, diamondsDelta, rpDelta;
        public int nextId, speed; public bool macau, guangdong;
        public List<UndoLine> lines = new List<UndoLine>();
        public int[] levels; public bool[] built, closed, sceneryUnlocked, sceneryFound;
        public List<string> technologies; public List<UndoVehicle> vehicles = new List<UndoVehicle>();
    }
    UndoState pendingUndo, lastUndo;
    Button undoButton;
    void BuildUndoButton()
    {
        undoButton = Btn(root, "Undo", "↶\n撤销", 23, 970, 190, 90, 90, new Color(1,0.99f,0.96f,0.94f), UndoLastAction);
        TInk(undoButton, InkSoft); SoftShadow(undoButton.gameObject); RefreshUndoButton();
    }
    void RefreshUndoButton()
    {
        if (!undoButton) return;
        undoButton.GetComponent<Image>().color = lastUndo == null ? new Color(0.87f,0.85f,0.81f,0.9f) : new Color(1,0.99f,0.96f,0.96f);
    }
    void BeginUndo(string label)
    {
        var state = new UndoState { label=label, coinsBefore=coins, diamondsBefore=diamonds, rpBefore=rp, nextId=nextLineId,
            macau=macauUnlocked, guangdong=guangdongUnlocked, speed=timeSpeed, levels=new int[stations.Count], built=new bool[stations.Count], closed=new bool[stations.Count],
            sceneryUnlocked=new bool[sceneries.Count], sceneryFound=new bool[sceneries.Count], technologies=new List<string>(techDone) };
        foreach (var line in lines) state.lines.Add(new UndoLine { line=line, name=line.name, path=new List<int>(line.st), dispatch=(int[])line.dispatch.Clone(), trains=line.trains, fare=line.fare });
        for (int i=0;i<stations.Count;i++) { state.levels[i]=stations[i].level; state.built[i]=stations[i].built; state.closed[i]=stations[i].closed; }
        for (int i=0;i<sceneries.Count;i++) { state.sceneryUnlocked[i]=sceneries[i].unlocked; state.sceneryFound[i]=sceneries[i].found; }
        foreach (var item in researchVehicles) state.vehicles.Add(new UndoVehicle { vehicle=item, lineId=item.lineId });
        pendingUndo=state;
    }
    void EndUndo()
    {
        if (pendingUndo == null) return;
        pendingUndo.coinsDelta=coins-pendingUndo.coinsBefore; pendingUndo.diamondsDelta=diamonds-pendingUndo.diamondsBefore; pendingUndo.rpDelta=rp-pendingUndo.rpBefore;
        lastUndo=pendingUndo; pendingUndo=null; RefreshUndoButton();
    }
    void UndoLastAction()
    {
        if (dragging || activePanel || pageRoot || sceneryViewer) return;
        if (lastUndo == null) { Toast("没有可撤销的操作"); return; }
        var state=lastUndo; lastUndo=null; pendingUndo=null; CloseLineBubble(); CancelDraft();
        coins-=state.coinsDelta; diamonds-=state.diamondsDelta; rp-=state.rpDelta; nextLineId=state.nextId; timeSpeed=state.speed; UpdateTimeText();
        lines.Clear(); foreach (var record in state.lines)
        { record.line.name=record.name; record.line.st=new List<int>(record.path); record.line.dispatch=(int[])record.dispatch.Clone(); record.line.trains=record.trains; record.line.fare=record.fare; lines.Add(record.line); }
        for (int i=0;i<stations.Count;i++) { stations[i].level=state.levels[i]; stations[i].built=state.built[i]; stations[i].closed=state.closed[i]; }
        for (int i=0;i<sceneries.Count;i++) { sceneries[i].unlocked=state.sceneryUnlocked[i]; sceneries[i].found=state.sceneryFound[i]; }
        techDone.Clear(); techDone.AddRange(state.technologies);
        researchVehicles.Clear(); foreach (var item in state.vehicles) { item.vehicle.lineId=item.lineId; researchVehicles.Add(item.vehicle); }
        macauUnlocked=state.macau; guangdongUnlocked=state.guangdong;
        pendingCheckin.Clear(); foreach (var sc in sceneries) if (sc.unlocked && !sc.found && TryGetSceneryContent(sc,out _,out _)) pendingCheckin.Add(sc);
        RefreshSceneryViews(); RefreshPendingBadge(); CreateLineVisuals(); RefreshStationDots(); RecalcIncome();
        if (!RegionUnlocked(currentRegion)) SwitchRegion("香港");
        RefreshHUD(); RefreshUndoButton(); Toast("已撤销：" + state.label);
    }
}
