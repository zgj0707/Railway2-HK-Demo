using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public static class Haptic
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void VibrateJS(int ms);
#endif
    public static void Vibrate(int ms)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        try { VibrateJS(ms); } catch { }
#elif UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#else
        _ = ms;
#endif
    }
}

public partial class DemoBootstrap
{
    // ---------- time / input / feedback ----------
    float dayClock = 0f; int day = 1; int timeSpeed = 1;
    Text timeTxt, speedTxt;
    int pressIdx = -1; float pressTimer; bool pressing; Vector2 pressStart;
    AudioSource sfx; AudioClip toneLo, toneHi;
    Image dragTip; int dragFrom = -1, hoverStation = -1;
    GameObject dock; List<Image> stationRings = new List<Image>();
    int pulseIdx = -1; float pulseTimer;

    class Floater { public Text txt; public RectTransform rt; public float age; }
    readonly List<Floater> floaters = new List<Floater>();

    void InitMobile()
    {
        var go = new GameObject("Sfx", typeof(AudioSource));
        go.transform.SetParent(transform, false);
        sfx = go.GetComponent<AudioSource>();
        toneLo = MakeTone(620f, 0.07f);
        toneHi = MakeTone(940f, 0.055f);
        RefreshStationDots();
        UpdateTimeText();
    }

    AudioClip MakeTone(float freq, float dur)
    {
        int rate = 44100; int n = Mathf.CeilToInt(rate * dur);
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)rate;
            float env = Mathf.Min(1f, t / 0.004f) * Mathf.Min(1f, (dur - t) / 0.02f);
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.30f;
        }
        var c = AudioClip.Create("tone", n, 1, rate, false);
        c.SetData(data, 0);
        return c;
    }
    void PlayTone(bool high) { if (sfx) sfx.PlayOneShot(high ? toneHi : toneLo); }

    // ---------- clock ----------
    void TickClock(float dt)
    {
        dayClock += dt;
        if (dayClock >= 60f) { dayClock -= 60f; day++; }
        UpdateTimeText();
    }
    void UpdateTimeText()
    {
        if (!timeTxt) return;
        int mins = 8 * 60 + Mathf.FloorToInt(dayClock / 60f * 1440f);
        int hh = (mins / 60) % 24, mm = mins % 60;
        timeTxt.text = "第 " + day + " 天  " + hh.ToString("00") + ":" + mm.ToString("00");
        if (speedTxt) speedTxt.text = timeSpeed + "×";
    }
    void CycleSpeed()
    {
        timeSpeed = timeSpeed >= 3 ? 1 : timeSpeed + 1;
        UpdateTimeText();
    }

    // ---------- income floaters ----------
    void SpawnFloater(int amount, Vector2 at)
    {
        if (amount <= 0) return;
        var go = Rect("Float", trainLayer, at.x - 60, at.y + 20, 160, 34);
        var t = go.AddComponent<Text>();
        t.font = font; t.fontSize = 27; t.alignment = TextAnchor.MiddleCenter;
        t.color = Gold; t.text = "+¥" + amount; t.raycastTarget = false;
        var sh = go.AddComponent<Shadow>(); sh.effectColor = new Color(1, 0.99f, 0.95f, 0.85f);
        var f = new Floater { txt = t, rt = go.GetComponent<RectTransform>(), age = 0f };
        floaters.Add(f);
    }
    void TickFloaters(float dt)
    {
        for (int i = floaters.Count - 1; i >= 0; i--)
        {
            var f = floaters[i]; f.age += dt;
            f.rt.anchoredPosition += new Vector2(0, dt * 38f);
            var c = f.txt.color;
            c.a = Mathf.Clamp01(1f - (f.age - 0.55f) / 0.7f);
            f.txt.color = c;
            if (c.a <= 0f) { Destroy(f.rt.gameObject); floaters.RemoveAt(i); }
        }
    }

    // ---------- station dots ----------
    bool[] stationOpen;
    void RefreshStationDots()
    {
        if (stationOpen == null || stationOpen.Length != stations.Count) stationOpen = new bool[stations.Count];
        for (int i = 0; i < stationOpen.Length; i++) stationOpen[i] = false;
        foreach (var l in lines) foreach (var i in l.st) if (i >= 0 && i < stationOpen.Length) stationOpen[i] = true;
        foreach (var kv in stationImgs)
        {
            if (stations[kv.Key].portal) continue;
            kv.Value.color = stationOpen[kv.Key] ? Gold : new Color(0.985f, 0.965f, 0.925f, 0.98f);
        }
    }
    bool IsOpen(int i) { return stationOpen != null && i >= 0 && i < stationOpen.Length && stationOpen[i]; }

    // ---------- long press / draw ----------
    void BeginPress(int idx)
    {
        if (dragging) return;
        pressIdx = idx; pressTimer = 0f; pressing = true; pressStart = Input.mousePosition;
    }
    void TickPress(float dt)
    {
        if (!pressing) return;
        if (!Input.GetMouseButton(0))
        {
            pressing = false; pressIdx = -1;
            return;
        }
        if (Vector2.Distance((Vector2)Input.mousePosition, pressStart) > 26f && pressTimer < 0.42f)
        {
            pressing = false; pressIdx = -1; return;
        }
        pressTimer += dt;
        if (pressTimer >= 0.42f)
        {
            int idx = pressIdx; pressing = false; pressIdx = -1;
            StartDraw(idx);
        }
    }
    void StartDraw(int idx)
    {
        if (dragging || idx < 0 || idx >= stations.Count) return;
        draft.Clear(); draft.Add(idx); draftCost = 0;
        dragFrom = idx; dragging = true; hoverStation = -1;
        RedrawDraft();
        if (previewImg) previewImg.gameObject.SetActive(true);
        if (dragTip) dragTip.gameObject.SetActive(true);
        UpdateRings();
        Haptic.Vibrate(22); PlayTone(false);
    }
    void EndDraw()
    {
        if (!dragging) return;
        dragging = false; hoverStation = -1;
        if (previewImg) previewImg.gameObject.SetActive(false);
        if (dragTip) dragTip.gameObject.SetActive(false);
        UpdateRings();
        if (draft.Count >= 2) FinishDraft();
        else CancelDraft();
    }
    void CancelDraft()
    {
        draft.Clear(); draftCost = 0;
        ClearDraftSegs(); ResetBuildVisuals();
    }
}
