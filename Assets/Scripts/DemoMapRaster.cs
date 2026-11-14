using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 三个区域共用清新手绘风格的地图底图。
/// </summary>
public partial class DemoBootstrap
{
    void BuildRasterMap(Transform parent)
    {
        var go = Rect("BaseMap", parent, 0, 0, W, H);
        var raw = go.AddComponent<RawImage>();
        if (qualityTier > 0 || currentRegion == "香港")
            raw.texture = Resources.Load<Texture2D>("MapArt/hongkong_illustrated");
        raw.color = Themes[Mathf.Clamp(currentTheme, 0, Themes.Length - 1)].mapTint;
        raw.raycastTarget = false;
        hkMapRaw = raw;
    }

    // 广东地图为手绘风格示意，不绘制行政边界。
    GameObject BuildGuangdongMap(Transform parent)
    {
        var go = Rect("GuangdongArt", parent, 0, 0, W, H);
        var raw = go.AddComponent<RawImage>(); raw.raycastTarget = false;
        if (qualityTier > 0 || currentRegion == "广东")
            raw.texture = Resources.Load<Texture2D>("MapArt/guangdong_illustrated");
        raw.color = Themes[Mathf.Clamp(currentTheme, 0, Themes.Length - 1)].mapTint;
        guangdongMapRaw = raw;
        var rt = go.GetComponent<RectTransform>();
        Label(rt, "广 东", 34, 0.48f, 0.84f, new Color(0.40f,0.38f,0.32f,0.70f), 200);
        var hint = Txt(go.transform, "放大地图查看县级站点", 22, new Color(0.28f,0.34f,0.31f,0.78f), 580, 230, 450, 36, TextAnchor.MiddleRight);
        hint.raycastTarget = false;
        return go;
    }

    // 澳门示意轮廓参考 OpenStreetMap 并经过简化，见 docs/map-data。
    GameObject BuildMacauMap(Transform parent)
    {
        var go = Rect("MacauArt", parent, 0, 0, W, H);
        var raw = go.AddComponent<RawImage>(); raw.raycastTarget = false;
        if (qualityTier > 0 || currentRegion == "澳门")
            raw.texture = Resources.Load<Texture2D>("MapArt/macau_osm");
        raw.color = Themes[Mathf.Clamp(currentTheme, 0, Themes.Length - 1)].mapTint;
        macauMapRaw = raw;
        var rt = go.GetComponent<RectTransform>();
        var labelColor = new Color(0.40f,0.38f,0.32f,0.70f);
        Label(rt, "澳门半岛", 25, MacauX(113.542f), MacauY(22.224f), labelColor, 180);
        Label(rt, "氹仔", 24, MacauX(113.550f), MacauY(22.171f), labelColor, 150);
        Label(rt, "路氹", 24, MacauX(113.566f), MacauY(22.136f), labelColor, 150);
        Label(rt, "路环", 24, MacauX(113.564f), MacauY(22.105f), labelColor, 150);
        var credit = Txt(go.transform, "© OpenStreetMap contributors", 20, new Color(0.22f,0.30f,0.31f,0.72f), 540, 188, 520, 32, TextAnchor.MiddleRight);
        credit.raycastTarget = false;
        return go;
    }

    // 省电/低显存模式仅在显存保留当前区地图纹理，中/高档常驻三区实现切区瞬开。
    void SyncRegionMapTextureMemory()
    {
        bool lowMem = qualityTier <= 0;
        if (hkMapRaw)
        {
            if (lowMem && currentRegion != "香港") hkMapRaw.texture = null;
            else if (!hkMapRaw.texture) hkMapRaw.texture = Resources.Load<Texture2D>("MapArt/hongkong_illustrated");
        }
        if (guangdongMapRaw)
        {
            if (lowMem && currentRegion != "广东") guangdongMapRaw.texture = null;
            else if (!guangdongMapRaw.texture) guangdongMapRaw.texture = Resources.Load<Texture2D>("MapArt/guangdong_illustrated");
        }
        if (macauMapRaw)
        {
            if (lowMem && currentRegion != "澳门") macauMapRaw.texture = null;
            else if (!macauMapRaw.texture) macauMapRaw.texture = Resources.Load<Texture2D>("MapArt/macau_osm");
        }
    }
}
