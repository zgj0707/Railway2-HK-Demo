using System;
using UnityEngine;

public enum MetroPresetTheme
{
    MinimalWhite = 0,   // 极简白（经典港铁/现代风）
    CyberNight = 1,     // 深空暗夜（高对比霓虹壁纸风）
    VintagePaper = 2,   // 复古手绘（暖黄纸纹）
    Blueprint = 3,      // 工程蓝图（群青网格）
    LingnanPorcelain = 4 // 岭南青瓷（淡雅青白）
}

public enum MetroStationShape
{
    Ring = 0,       // 空心圆环
    SolidDot = 1,   // 实心圆点
    Square = 2      // 圆角方徽
}

public enum MetroLegendCorner
{
    TopLeft = 0,
    BottomLeft = 1,
    TopRight = 2,
    BottomRight = 3,
    Hidden = 4
}

[Serializable]
public class MetroStyleProfile
{
    public MetroPresetTheme preset = MetroPresetTheme.MinimalWhite;
    public string themeName = "极简白";

    public Color bgTopColor = new Color(0.975f, 0.978f, 0.985f, 1f);
    public Color bgBottomColor = new Color(0.935f, 0.945f, 0.960f, 1f);
    public bool showGrid = true;
    public Color gridColor = new Color(0.82f, 0.85f, 0.90f, 0.45f);
    public float gridSpacing = 72f;

    public float lineWidth = 10f;
    public float parallelGap = 5f;
    public float cornerRadius = 22f;
    public bool useOctilinear = true;
    public bool drawLineHalo = true;
    public Color haloColor = new Color(1f, 1f, 1f, 0.90f);

    public MetroStationShape stationShape = MetroStationShape.Ring;
    public float stationRadius = 11f;
    public Color stationFillColor = Color.white;
    public Color stationStrokeColor = new Color(0.16f, 0.18f, 0.22f, 1f);
    public Color transferRingColor = new Color(0.12f, 0.14f, 0.18f, 1f);

    public Color labelColor = new Color(0.14f, 0.16f, 0.20f, 1f);
    public Color labelShadowColor = new Color(1f, 1f, 1f, 0.85f);
    public int labelFontSize = 22;

    public Color cardBgColor = new Color(1f, 1f, 1f, 0.90f);
    public Color cardBorderColor = new Color(0.78f, 0.81f, 0.86f, 0.90f);
    public MetroLegendCorner legendCorner = MetroLegendCorner.TopLeft;

    public bool overrideLineColors = false;
    public Color[] paletteOverrides = Array.Empty<Color>();

    public static MetroStyleProfile CreatePreset(MetroPresetTheme theme)
    {
        switch (theme)
        {
            case MetroPresetTheme.CyberNight:
                return new MetroStyleProfile
                {
                    preset = theme,
                    themeName = "深空暗夜",
                    bgTopColor = new Color(0.055f, 0.075f, 0.125f, 1f),
                    bgBottomColor = new Color(0.025f, 0.035f, 0.065f, 1f),
                    showGrid = true,
                    gridColor = new Color(0.18f, 0.30f, 0.48f, 0.30f),
                    gridSpacing = 80f,
                    lineWidth = 10f,
                    parallelGap = 5f,
                    cornerRadius = 22f,
                    useOctilinear = true,
                    drawLineHalo = true,
                    haloColor = new Color(0.04f, 0.06f, 0.10f, 0.92f),
                    stationShape = MetroStationShape.Ring,
                    stationRadius = 11f,
                    stationFillColor = new Color(0.06f, 0.09f, 0.15f, 1f),
                    stationStrokeColor = new Color(0.88f, 0.94f, 1f, 1f),
                    transferRingColor = new Color(0.32f, 0.86f, 0.98f, 1f),
                    labelColor = new Color(0.90f, 0.95f, 1f, 1f),
                    labelShadowColor = new Color(0.02f, 0.03f, 0.06f, 0.92f),
                    labelFontSize = 22,
                    cardBgColor = new Color(0.08f, 0.11f, 0.18f, 0.88f),
                    cardBorderColor = new Color(0.24f, 0.46f, 0.72f, 0.75f),
                    legendCorner = MetroLegendCorner.TopLeft
                };

            case MetroPresetTheme.VintagePaper:
                return new MetroStyleProfile
                {
                    preset = theme,
                    themeName = "复古手绘",
                    bgTopColor = new Color(0.955f, 0.918f, 0.845f, 1f),
                    bgBottomColor = new Color(0.905f, 0.855f, 0.765f, 1f),
                    showGrid = true,
                    gridColor = new Color(0.68f, 0.60f, 0.48f, 0.28f),
                    gridSpacing = 84f,
                    lineWidth = 11f,
                    parallelGap = 5f,
                    cornerRadius = 26f,
                    useOctilinear = true,
                    drawLineHalo = true,
                    haloColor = new Color(0.95f, 0.91f, 0.83f, 0.92f),
                    stationShape = MetroStationShape.Ring,
                    stationRadius = 11.5f,
                    stationFillColor = new Color(0.98f, 0.95f, 0.89f, 1f),
                    stationStrokeColor = new Color(0.24f, 0.19f, 0.15f, 1f),
                    transferRingColor = new Color(0.68f, 0.25f, 0.18f, 1f),
                    labelColor = new Color(0.20f, 0.16f, 0.13f, 1f),
                    labelShadowColor = new Color(0.97f, 0.94f, 0.87f, 0.90f),
                    labelFontSize = 22,
                    cardBgColor = new Color(0.97f, 0.94f, 0.87f, 0.92f),
                    cardBorderColor = new Color(0.66f, 0.56f, 0.44f, 0.85f),
                    legendCorner = MetroLegendCorner.TopLeft
                };

            case MetroPresetTheme.Blueprint:
                return new MetroStyleProfile
                {
                    preset = theme,
                    themeName = "工程蓝图",
                    bgTopColor = new Color(0.085f, 0.215f, 0.415f, 1f),
                    bgBottomColor = new Color(0.055f, 0.155f, 0.325f, 1f),
                    showGrid = true,
                    gridColor = new Color(0.62f, 0.82f, 1f, 0.22f),
                    gridSpacing = 64f,
                    lineWidth = 9f,
                    parallelGap = 5f,
                    cornerRadius = 16f,
                    useOctilinear = true,
                    drawLineHalo = true,
                    haloColor = new Color(0.06f, 0.18f, 0.36f, 0.90f),
                    stationShape = MetroStationShape.Square,
                    stationRadius = 10.5f,
                    stationFillColor = new Color(0.08f, 0.21f, 0.40f, 1f),
                    stationStrokeColor = new Color(0.92f, 0.97f, 1f, 1f),
                    transferRingColor = new Color(1f, 0.86f, 0.42f, 1f),
                    labelColor = new Color(0.93f, 0.97f, 1f, 1f),
                    labelShadowColor = new Color(0.04f, 0.12f, 0.25f, 0.90f),
                    labelFontSize = 21,
                    cardBgColor = new Color(0.07f, 0.18f, 0.35f, 0.88f),
                    cardBorderColor = new Color(0.64f, 0.82f, 1f, 0.72f),
                    legendCorner = MetroLegendCorner.TopLeft,
                    overrideLineColors = true,
                    paletteOverrides = new[]
                    {
                        new Color(0.95f, 0.98f, 1.00f, 1f),
                        new Color(0.45f, 0.88f, 0.98f, 1f),
                        new Color(1.00f, 0.82f, 0.36f, 1f),
                        new Color(0.60f, 0.95f, 0.72f, 1f),
                        new Color(0.98f, 0.62f, 0.72f, 1f),
                        new Color(0.76f, 0.72f, 1.00f, 1f)
                    }
                };

            case MetroPresetTheme.LingnanPorcelain:
                return new MetroStyleProfile
                {
                    preset = theme,
                    themeName = "岭南青瓷",
                    bgTopColor = new Color(0.915f, 0.952f, 0.945f, 1f),
                    bgBottomColor = new Color(0.845f, 0.905f, 0.895f, 1f),
                    showGrid = true,
                    gridColor = new Color(0.42f, 0.58f, 0.56f, 0.20f),
                    gridSpacing = 76f,
                    lineWidth = 10f,
                    parallelGap = 5f,
                    cornerRadius = 24f,
                    useOctilinear = true,
                    drawLineHalo = true,
                    haloColor = new Color(0.92f, 0.96f, 0.95f, 0.92f),
                    stationShape = MetroStationShape.Ring,
                    stationRadius = 11f,
                    stationFillColor = new Color(0.97f, 0.99f, 0.98f, 1f),
                    stationStrokeColor = new Color(0.14f, 0.30f, 0.34f, 1f),
                    transferRingColor = new Color(0.12f, 0.45f, 0.48f, 1f),
                    labelColor = new Color(0.11f, 0.24f, 0.27f, 1f),
                    labelShadowColor = new Color(0.95f, 0.98f, 0.97f, 0.90f),
                    labelFontSize = 22,
                    cardBgColor = new Color(0.96f, 0.98f, 0.98f, 0.90f),
                    cardBorderColor = new Color(0.46f, 0.64f, 0.62f, 0.80f),
                    legendCorner = MetroLegendCorner.TopLeft
                };

            case MetroPresetTheme.MinimalWhite:
            default:
                return new MetroStyleProfile
                {
                    preset = theme,
                    themeName = "极简白",
                    bgTopColor = Color.white,
                    bgBottomColor = new Color(0.992f, 0.994f, 0.998f, 1f),
                    showGrid = false,
                    gridColor = new Color(0.76f, 0.80f, 0.86f, 0.35f),
                    gridSpacing = 72f,
                    lineWidth = 8.5f,
                    parallelGap = 4.0f,
                    cornerRadius = 22f,
                    useOctilinear = true,
                    drawLineHalo = true,
                    haloColor = new Color(1f, 1f, 1f, 0.94f),
                    stationShape = MetroStationShape.Ring,
                    stationRadius = 8.0f,
                    stationFillColor = Color.white,
                    stationStrokeColor = new Color(0.16f, 0.18f, 0.22f, 1f),
                    transferRingColor = new Color(0.10f, 0.11f, 0.14f, 1f),
                    labelColor = new Color(0.10f, 0.11f, 0.14f, 1f),
                    labelShadowColor = new Color(1f, 1f, 1f, 0.90f),
                    labelFontSize = 20,
                    cardBgColor = new Color(1f, 1f, 1f, 0.92f),
                    cardBorderColor = new Color(0.78f, 0.81f, 0.86f, 0.90f),
                    legendCorner = MetroLegendCorner.TopLeft
                };
        }
    }

    public MetroStyleProfile Clone()
    {
        var copy = (MetroStyleProfile)MemberwiseClone();
        if (paletteOverrides != null)
        {
            copy.paletteOverrides = (Color[])paletteOverrides.Clone();
        }
        return copy;
    }
}
