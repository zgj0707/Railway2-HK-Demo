using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class DemoBootstrap
{
    // ---------- M1: 轻量化手帐转场、微动效与粒子反馈系统 ----------
    GameObject regionCurtainGo;
    Coroutine bubbleAnimCo, pageAnimCo;

    void StopBubbleAnim()
    {
        if (bubbleAnimCo != null) { StopCoroutine(bubbleAnimCo); bubbleAnimCo = null; }
    }

    void StopPageAnim()
    {
        if (pageAnimCo != null) { StopCoroutine(pageAnimCo); pageAnimCo = null; }
    }

    void AnimateBubblePop(GameObject panel, GameObject shadow = null)
    {
        StopBubbleAnim();
        if (!panel) return;
        var rt = panel.GetComponent<RectTransform>();
        if (!rt) return;
        var cg = panel.GetComponent<CanvasGroup>();
        if (!cg) cg = panel.AddComponent<CanvasGroup>();
        Vector3 targetScale = rt.localScale;
        cg.alpha = 0f;
        rt.localScale = targetScale * (qualityTier >= 2 ? 0.88f : 0.94f);
        CanvasGroup shadowCg = null;
        if (shadow)
        {
            shadowCg = shadow.GetComponent<CanvasGroup>();
            if (!shadowCg) shadowCg = shadow.AddComponent<CanvasGroup>();
            shadowCg.alpha = 0f;
            shadowCg.blocksRaycasts = false;
        }
        bubbleAnimCo = StartCoroutine(CoBubblePop(rt, cg, shadowCg, targetScale, qualityTier >= 2));
    }

    IEnumerator CoBubblePop(RectTransform rt, CanvasGroup cg, CanvasGroup shadowCg, Vector3 targetScale, bool springOvershoot)
    {
        float dur = springOvershoot ? 0.19f : 0.14f;
        float t = 0f;
        while (rt && cg && t < dur)
        {
            t += Mathf.Clamp(Time.unscaledDeltaTime, 0.002f, 0.03f);
            float p = Mathf.Clamp01(t / dur);
            float sc;
            if (springOvershoot)
            {
                const float c1 = 1.70158f, c3 = c1 + 1f;
                float u = p - 1f;
                float back = 1f + c3 * u * u * u + c1 * u * u;
                sc = Mathf.LerpUnclamped(0.88f, 1f, back);
            }
            else
            {
                float ease = 1f - Mathf.Pow(1f - p, 3f);
                sc = Mathf.Lerp(0.94f, 1f, ease);
            }
            rt.localScale = targetScale * sc;
            float alpha = Mathf.Clamp01(p * 1.55f);
            cg.alpha = alpha;
            if (shadowCg) shadowCg.alpha = alpha;
            yield return null;
        }
        if (rt && cg)
        {
            rt.localScale = targetScale;
            cg.alpha = 1f;
        }
        if (shadowCg) shadowCg.alpha = 1f;
        bubbleAnimCo = null;
    }

    void AnimatePageEnter(GameObject page, bool slideUp = true)
    {
        StopPageAnim();
        if (!page) return;
        var cg = page.GetComponent<CanvasGroup>();
        if (!cg) cg = page.AddComponent<CanvasGroup>();
        var rt = page.GetComponent<RectTransform>();
        cg.alpha = 0f;
        float startY = qualityTier >= 2 ? -32f : -22f;
        if (rt && slideUp)
        {
            rt.offsetMin = new Vector2(0f, startY);
            rt.offsetMax = new Vector2(0f, startY);
        }
        pageAnimCo = StartCoroutine(CoPageEnter(rt, cg, slideUp, startY));
    }

    IEnumerator CoPageEnter(RectTransform rt, CanvasGroup cg, bool slideUp, float dy)
    {
        const float dur = 0.18f;
        float t = 0f;
        while (cg && t < dur)
        {
            t += Mathf.Clamp(Time.unscaledDeltaTime, 0.002f, 0.03f);
            float p = Mathf.Clamp01(t / dur);
            float ease = 1f - Mathf.Pow(1f - p, 3f);
            cg.alpha = ease;
            if (rt && slideUp)
            {
                float off = Mathf.Lerp(dy, 0f, ease);
                rt.offsetMin = new Vector2(0f, off);
                rt.offsetMax = new Vector2(0f, off);
            }
            yield return null;
        }
        if (cg) cg.alpha = 1f;
        if (rt && slideUp)
        {
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        pageAnimCo = null;
    }

    void PlayRegionCurtain(string title)
    {
        if (!root) return;
        if (regionCurtainGo) Destroy(regionCurtainGo);
        regionCurtainGo = Rect("RegionCurtain", root, 0f, 0f, RootW, RootH);
        var rt = regionCurtainGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

        var bg = Img(regionCurtainGo, Paper);
        bg.raycastTarget = false;

        var badge = Rect("Stamp", regionCurtainGo.transform, (RootW - 460f) * 0.5f, (RootH - 170f) * 0.5f, 460f, 170f);
        var badgeRt = badge.GetComponent<RectTransform>();
        badgeRt.pivot = new Vector2(0.5f, 0.5f);
        badgeRt.anchoredPosition = new Vector2(RootW * 0.5f, RootH * 0.5f);
        Rounded(badge, CardBg);
        badge.GetComponent<Image>().raycastTarget = false;
        SoftShadow(badge);
        Txt(badge.transform, "前往版图", 24, InkSoft, 0f, 108f, 460f, 38f, TextAnchor.MiddleCenter).raycastTarget = false;
        Txt(badge.transform, DisplayPlaceAliases(title), 54, Ink, 0f, 24f, 460f, 82f, TextAnchor.MiddleCenter).raycastTarget = false;

        var cg = regionCurtainGo.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.alpha = 0f;
        StartCoroutine(CoRegionCurtain(regionCurtainGo, cg, badgeRt));
    }

    IEnumerator CoRegionCurtain(GameObject go, CanvasGroup cg, RectTransform badgeRt)
    {
        float inDur = 0.14f, holdDur = 0.22f, outDur = 0.34f;
        float t = 0f;
        while (go && cg && t < inDur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / inDur);
            cg.alpha = p;
            if (badgeRt && qualityTier >= 2)
                badgeRt.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, 1f - Mathf.Pow(1f - p, 3f));
            yield return null;
        }
        if (cg) cg.alpha = 1f;
        if (badgeRt) badgeRt.localScale = Vector3.one;
        float h = 0f;
        while (go && h < holdDur) { h += Time.unscaledDeltaTime; yield return null; }
        t = 0f;
        while (go && cg && t < outDur)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / outDur);
            yield return null;
        }
        if (go) Destroy(go);
        if (regionCurtainGo == go) regionCurtainGo = null;
    }

    void SpawnMapRipple(Vector2 mapPos, Color col, float maxSize = 140f)
    {
        if (!floaterLayer || !spriteRing) return;
        var go = Rect("MapRipple", floaterLayer, mapPos.x - 16f, mapPos.y - 16f, 32f, 32f);
        var rt = go.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = mapPos;
        var img = Img(go, col, spriteRing);
        img.raycastTarget = false;
        StartCoroutine(CoMapRipple(go, rt, img, col, maxSize, 0f));
        // 高画质附加一层金色共振外环波纹，强化触感反馈。
        if (qualityTier >= 2)
        {
            var echoGo = Rect("MapRippleEcho", floaterLayer, mapPos.x - 12f, mapPos.y - 12f, 24f, 24f);
            var echoRt = echoGo.GetComponent<RectTransform>();
            echoRt.pivot = new Vector2(0.5f, 0.5f);
            echoRt.anchoredPosition = mapPos;
            var echoCol = Color.Lerp(col, Gold, 0.6f);
            var echoImg = Img(echoGo, new Color(echoCol.r, echoCol.g, echoCol.b, 0f), spriteRing);
            echoImg.raycastTarget = false;
            StartCoroutine(CoMapRipple(echoGo, echoRt, echoImg, echoCol, maxSize * 1.28f, 0.08f));
        }
    }

    IEnumerator CoMapRipple(GameObject go, RectTransform rt, Image img, Color baseCol, float maxSize, float delay)
    {
        if (delay > 0f)
        {
            float d = 0f;
            while (go && d < delay) { d += Time.unscaledDeltaTime; yield return null; }
        }
        float dur = 0.52f, t = 0f;
        while (go && rt && img && t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / dur);
            float ease = 1f - Mathf.Pow(1f - p, 2.5f);
            float size = Mathf.Lerp(24f, maxSize, ease);
            rt.sizeDelta = new Vector2(size, size);
            img.color = new Color(baseCol.r, baseCol.g, baseCol.b, (1f - p) * 0.85f);
            yield return null;
        }
        if (go) Destroy(go);
    }

    void SpawnConfettiBurst(Vector2 rootPos, Color c1, Color c2, int count = 14)
    {
        if (!root || !spriteCircle) return;
        int n = qualityTier >= 2 ? Mathf.RoundToInt(count * 1.7f)
              : qualityTier == 1 ? count
              : Mathf.Max(6, count / 2);
        StartCoroutine(CoConfettiBurst(rootPos, c1, c2, n));
    }

    IEnumerator CoConfettiBurst(Vector2 origin, Color c1, Color c2, int count)
    {
        var holder = Rect("ConfettiBurst", root, 0f, 0f, 10f, 10f);
        var hrt = holder.GetComponent<RectTransform>();
        hrt.anchoredPosition = origin;
        var items = new RectTransform[count];
        var imgs = new Image[count];
        var vel = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            float ang = (i / (float)count) * Mathf.PI * 2f + UnityEngine.Random.Range(-0.2f, 0.2f);
            float spd = UnityEngine.Random.Range(150f, qualityTier >= 2 ? 390f : 340f);
            vel[i] = new Vector2(Mathf.Cos(ang) * spd, Mathf.Sin(ang) * spd + 120f);
            float sz = UnityEngine.Random.Range(10f, 19f);
            var pGo = Rect("P_" + i, holder.transform, 0f, 0f, sz, sz);
            items[i] = pGo.GetComponent<RectTransform>();
            items[i].pivot = new Vector2(0.5f, 0.5f);
            items[i].anchoredPosition = Vector2.zero;
            Color col = i % 3 == 0 ? c1 : i % 3 == 1 ? c2 : Gold;
            imgs[i] = Img(pGo, col, i % 2 == 0 ? spriteDiamond : spriteCircle);
            imgs[i].raycastTarget = false;
        }

        float dur = qualityTier >= 2 ? 0.86f : 0.72f, t = 0f;
        while (holder && t < dur)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            float p = Mathf.Clamp01(t / dur);
            for (int i = 0; i < count; i++)
            {
                if (!items[i]) continue;
                vel[i].y -= 520f * dt;
                items[i].anchoredPosition += vel[i] * dt;
                items[i].localRotation = Quaternion.Euler(0f, 0f, t * 240f * (i % 2 == 0 ? 1f : -1f));
                Color c = imgs[i].color;
                imgs[i].color = new Color(c.r, c.g, c.b, 1f - p * p);
            }
            yield return null;
        }
        if (holder) Destroy(holder);
    }

    void PlayStationPathSparkles(System.Collections.Generic.List<int> path, Color col)
    {
        if (path == null || path.Count == 0) return;
        int stride = qualityTier >= 2 ? 1 : Mathf.Max(1, path.Count / 5);
        for (int i = 0; i < path.Count; i += stride)
        {
            int idx = path[i];
            if (idx >= 0 && idx < stations.Count && StationInRegion(idx, currentRegion))
                SpawnMapRipple(Pos(idx), i % 2 == 0 ? col : Color.Lerp(col, Gold, 0.45f), 130f);
        }
        if (path.Count > 0)
        {
            int last = path[path.Count - 1];
            if (last >= 0 && last < stations.Count && StationInRegion(last, currentRegion))
                SpawnMapRipple(Pos(last), Gold, 175f);
        }
    }
}
