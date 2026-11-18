using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public partial class DemoBootstrap
{
    GameObject sceneryViewer;
    readonly Dictionary<Scenery, GameObject> sceneryMarkerBadges = new Dictionary<Scenery, GameObject>();

    bool TryGetSceneryContent(Scenery sc, out string imagePath, out string description)
    {
        switch (sc.name)
        {
            case "帆影港":
                imagePath = "Scenery/victoria_harbour";
                description = "帆影港夹于汐港本岛与九曲岸之间，入夜后两岸灯火映照海面，展现汐港独特的港湾风景。";
                return true;
            case "望海峰":
                imagePath = "Scenery/victoria_peak";
                description = "望海峰俯瞰汐港本岛与帆影港，山径和观景点展现山海相连的海湾景色。";
                return true;
            case "卧狮岩":
                imagePath = "Scenery/lion_rock";
                description = "卧狮岩位于九曲岸与北郊之间，山脊形似伏狮，是汐港的山水地标之一。";
                return true;
            case "灯笼夜市":
                imagePath = "Scenery/temple_street_clean";
                description = "入夜后的千灯街灯火渐亮，摊档、招牌与骑楼街巷交织，展现汐港热闹的市井风情。";
                return true;
            case "云钟古阁":
                imagePath = "Scenery/tian_tan_buddha_clean";
                description = "云钟古阁坐落于苍岚岛山巅，云雾群山环绕，是汐港著名的古迹人文景观。";
                return true;
            case "清波湾":
                imagePath = "Scenery/repulse_bay_clean";
                description = "清波湾位于汐港本岛南部，海湾与青翠山坡相映，沿岸可以欣赏悠闲的海滨风光。";
                return true;
            case "白鹤古观":
                imagePath = "Scenery/baihe_ancient_temple";
                description = "白鹤古观依山而建，飞檐黛瓦隐于松柏烟岚之间，是汐港北郊颇具古韵的人文胜景。";
                return true;
            case "飞鹭芦滩":
                imagePath = "Scenery/egret_reed_marsh";
                description = "飞鹭芦滩水网交错、芦苇连绵，暮色时分常有成群白鹭掠水归巢，尽显海湾湿地生机。";
                return true;
            case "碧海石崖":
                imagePath = "Scenery/emerald_sea_cliff";
                description = "碧海石崖矗立于汐港东岸，六角柱状海蚀峭壁直面苍茫碧浪，是观海听涛的绝佳去处。";
                return true;
            case "石门古坊":
                imagePath = "Scenery/stone_arch_gate";
                description = "石门古坊雄踞镜屿老城石阶之上，雕花石壁历经海风洗礼，见证数百年海港市集变迁。";
                return true;
            case "望海灯楼":
                imagePath = "Scenery/sea_beacon_pavilion";
                description = "望海灯楼临海岬而立，古树垂荫、石阶蜿蜒，自古为镜屿南岸归航渔船指引航向。";
                return true;
            case "饼家老街":
                imagePath = "Scenery/bakery_old_street";
                description = "饼家老街青石铺路、骑楼相连，入夜后灯笼暖光与饼香飘散街巷，满载镜屿烟火气。";
                return true;
            case "墨沙海滩":
                imagePath = "Scenery/ink_sand_beach";
                description = "墨沙海滩以细腻乌亮的黑沙闻名镜屿南端，松林环抱新月海湾，潮汐拍岸清幽宁静。";
                return true;
            case "白塔山":
                imagePath = "Scenery/white_pagoda_hill";
                description = "白塔山高踞镜屿翠岭之上，素雅白塔远眺木船湾与海天一色，是镜屿最醒目的登高地标。";
                return true;
            default:
                imagePath = null; description = null;
                return false;
        }
    }

    void BuildSceneryMarker(Scenery sc)
    {
        if (sc.region != currentRegion) return;
        if (!sc.unlocked || !TryGetSceneryContent(sc, out _, out _))
        {
            var dot = Rect("Sc_" + sc.name, stationLayer, MAP_X + sc.x * MAP_W, MAP_Y + sc.y * MAP_H, 40, 40);
            dot.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            var dotImage = Img(dot, new Color(0.72f, 0.80f, 0.70f, 0.9f), spriteCircle);
            var dotLabel = Txt(dot.transform, sc.name, 22, new Color(0.22f, 0.30f, 0.22f, 0.95f), -60, -36, 180, 32, TextAnchor.MiddleCenter);
            TextShadow(dotLabel, new Color(1, 1, 1, 0.85f));
            var dotButton = dot.AddComponent<Button>(); dotButton.targetGraphic = dotImage;
            dotButton.onClick.AddListener(() => Toast(sc.name + (sc.unlocked ? " · 大图筹备中" : " · 连通" + sc.triggerStation + "站解锁")));
            return;
        }
        var marker = Rect("Sc_" + sc.name, stationLayer, MAP_X + sc.x * MAP_W, MAP_Y + sc.y * MAP_H, 68, 68);
        marker.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
        var background = Img(marker, new Color(0.98f, 0.96f, 0.87f, 0.97f), spriteCircle);
        SoftShadow(marker);
        var texture = Resources.Load<Texture2D>(sc.natural ? "MapIcons/scenery_nature" : "MapIcons/scenery_culture");
        if (texture)
        {
            var icon = Rect("CategoryIcon", marker.transform, 5, 5, 58, 58).AddComponent<RawImage>();
            icon.texture = texture; icon.raycastTarget = false;
        }
        var badge = Rect("UnvisitedBadge", marker.transform, 51, 51, 20, 20);
        Img(badge, new Color(0.98f, 0.96f, 0.87f, 1f), spriteCircle).raycastTarget = false;
        Img(Rect("RedDot", badge.transform, 3, 3, 14, 14), new Color(0.85f, 0.23f, 0.20f, 1f), spriteCircle).raycastTarget = false;
        badge.SetActive(!sc.found);
        sceneryMarkerBadges[sc] = badge;
        var label = Txt(marker.transform, sc.name, 22, new Color(0.22f, 0.30f, 0.22f, 0.98f), -65, -32, 198, 32, TextAnchor.MiddleCenter);
        TextShadow(label, new Color(1, 1, 1, 0.9f));
        var button = marker.AddComponent<Button>(); button.targetGraphic = background;
        button.onClick.AddListener(() => OpenSceneryViewer(sc));
    }

    void RefreshSceneryViews()
    {
        if (!stationLayer) return;
        foreach (Transform child in stationLayer)
            if (child.name.StartsWith("Sc_")) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        sceneryMarkerBadges.Clear();
        foreach (var scenery in sceneries) BuildSceneryMarker(scenery);
    }

    void RefreshSceneryMarkerBadge(Scenery sc)
    {
        if (sceneryMarkerBadges.TryGetValue(sc, out var badge) && badge) badge.SetActive(!sc.found);
    }

    void OpenSceneryViewer(Scenery sc, System.Action after = null)
    {
        if (sc == null || !sc.unlocked || !TryGetSceneryContent(sc, out string imagePath, out string description)) return;
        var texture = Resources.Load<Texture2D>(imagePath);
        if (!texture) { Toast(sc.name + " · 图片暂不可用"); return; }
        bool newlyCheckedIn = CompleteSceneryCheckin(sc);
        if (sceneryViewer) Destroy(sceneryViewer);
        CloseLineBubble();
        sceneryViewer = new GameObject("SceneryViewer", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster), typeof(SceneryViewerLayout));
        SyncRegionalMusic(false);
        var viewerCanvas = sceneryViewer.GetComponent<Canvas>();
        viewerCanvas.renderMode = RenderMode.ScreenSpaceOverlay; viewerCanvas.overrideSorting = true; viewerCanvas.sortingOrder = 100;
        var backdrop = Rect("Backdrop", sceneryViewer.transform, 0, 0, 0, 0);
        var backRt = backdrop.GetComponent<RectTransform>(); backRt.anchorMin = Vector2.zero; backRt.anchorMax = Vector2.one;
        backRt.offsetMin = backRt.offsetMax = Vector2.zero;
        Img(backdrop, new Color(0.06f, 0.08f, 0.07f, 1));
        var stage = Rect("Landscape", sceneryViewer.transform, 0, 0, 1920, 1080);
        var stageRt = stage.GetComponent<RectTransform>();
        stageRt.anchorMin = stageRt.anchorMax = new Vector2(0.5f, 0.5f); stageRt.pivot = new Vector2(0.5f, 0.5f); stageRt.anchoredPosition = Vector2.zero;
        sceneryViewer.GetComponent<SceneryViewerLayout>().stage = stageRt;
        var picture = Rect("Picture", stage.transform, 0, 0, 1920, 1080);
        var raw = picture.AddComponent<RawImage>(); raw.texture = texture; raw.color = Color.white;
        var controls = Rect("Controls", stage.transform, 0, 0, 1920, 1080);
        var reveal = picture.AddComponent<Button>(); reveal.targetGraphic = raw;
        reveal.onClick.AddListener(() => controls.SetActive(true));
        var info = Rect("Info", controls.transform, 1330, 220, 520, 590);
        Rounded(info, new Color(0.10f, 0.14f, 0.13f, 0.76f));
        Txt(info.transform, sc.region + (sc.natural ? " · 自然景观" : " · 人文景观"), 25, new Color(0.85f, 0.91f, 0.83f), 36, 500, 448, 40, TextAnchor.MiddleLeft);
        Txt(info.transform, sc.name, 58, Color.white, 36, 405, 448, 80, TextAnchor.MiddleLeft);
        Txt(info.transform, description, 30, new Color(0.96f, 0.96f, 0.91f), 36, 160, 448, 220, TextAnchor.UpperLeft);
        if (newlyCheckedIn)
        {
            var notification = Rect("CheckinToast", stage.transform, 560, 850, 800, 88);
            Rounded(notification, new Color(0.10f, 0.13f, 0.12f, 0.88f));
            Txt(notification.transform, "打卡成功 · 钻石 +20", 32, Color.white, 20, 10, 760, 68, TextAnchor.MiddleCenter);
            notification.GetComponent<Image>().raycastTarget = false;
            Destroy(notification, 3f);
        }
        var back = Btn(controls.transform, "Back", "←", 40, 24, 958, 94, 94, new Color(0.12f, 0.16f, 0.15f, 0.75f), () => { Destroy(sceneryViewer); sceneryViewer = null; RequestResourceTrim(false); after?.Invoke(); if (pageRoot == null) SyncRegionalMusic(false); });
        TInk(back, Color.white);
        var home = Btn(controls.transform, "Home", "主页", 24, 134, 958, 104, 94, new Color(0.12f, 0.16f, 0.15f, 0.75f), GoHome);
        TInk(home, Color.white);
        var heading = Txt(controls.transform, sc.name, 44, Color.white, 254, 975, 800, 60, TextAnchor.MiddleLeft);
        heading.resizeTextForBestFit = true; heading.resizeTextMinSize = 24; heading.resizeTextMaxSize = ScaledFontSize(44);
        var clear = Btn(controls.transform, "Clear", "清屏", 30, 1690, 44, 180, 70, new Color(0.12f, 0.16f, 0.15f, 0.75f), () => controls.SetActive(false));
        sceneryViewer.GetComponent<SceneryViewerLayout>().clear = clear.GetComponent<RectTransform>();
        TInk(clear, Color.white);
    }
}

public class SceneryViewerLayout : MonoBehaviour
{
    public RectTransform stage, clear;
    int lastWidth = -1, lastHeight = -1;
    void LateUpdate()
    {
        if (!stage) return;
        if (Screen.width == lastWidth && Screen.height == lastHeight) return;
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        bool portrait = lastHeight > lastWidth;
        float width = portrait ? lastHeight : lastWidth;
        float height = portrait ? lastWidth : lastHeight;
        float scale = Mathf.Min(width / 1920f, height / 1080f);
        stage.localScale = Vector3.one * scale;
        stage.localRotation = Quaternion.Euler(0, 0, portrait ? 90 : 0);
        if (clear) clear.anchoredPosition = new Vector2(portrait ? 1370 : 1690, 44);
    }
}
