using UnityEngine;

public class PageHeaderLayout : MonoBehaviour
{
    public RectTransform page, back, home, title, help;
    void LateUpdate()
    {
        if (!page) return;
        Rect safe = Screen.safeArea;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(page, new Vector2(safe.xMin, safe.yMax), null, out var left) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(page, new Vector2(safe.xMax, safe.yMax), null, out var right)) return;
        Vector2 origin = page.rect.min;
        if (back) { back.pivot = new Vector2(0, 1); back.anchoredPosition = left - origin + new Vector2(24, -28); }
        if (home) { home.pivot = new Vector2(0, 1); home.anchoredPosition = left - origin + new Vector2(134, -28); }
        if (title)
        {
            title.pivot = new Vector2(0, 1);
            float titleX = help ? 330 : 254;
            title.anchoredPosition = left - origin + new Vector2(titleX, -45);
            title.sizeDelta = new Vector2(Mathf.Max(1, right.x - left.x - titleX - 280), 60);
        }
        if (help) { help.pivot = new Vector2(0, 1); help.anchoredPosition = left - origin + new Vector2(254, -43); }
    }
}
