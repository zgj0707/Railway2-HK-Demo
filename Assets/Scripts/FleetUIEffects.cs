using UnityEngine;
using UnityEngine.EventSystems;

public class FleetCardEntrance : MonoBehaviour
{
    public float delay;
    CanvasGroup group;
    RectTransform rect;
    Vector2 original;
    float elapsed;
    void Start()
    {
        rect = (RectTransform)transform; original = rect.anchoredPosition;
        group = gameObject.AddComponent<CanvasGroup>(); group.alpha = 0;
        rect.anchoredPosition = original + new Vector2(0, -18);
    }
    void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01((elapsed - delay) / 0.22f);
        group.alpha = t;
        rect.anchoredPosition = original + new Vector2(0, -18 * (1 - Mathf.SmoothStep(0, 1, t)));
        if (t >= 1) Destroy(this);
    }
}

public class FleetPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    bool pressed;
    Vector3 original;
    void Awake() { original = transform.localScale; }
    public void OnPointerDown(PointerEventData e) { pressed = true; }
    public void OnPointerUp(PointerEventData e) { pressed = false; }
    public void OnPointerExit(PointerEventData e) { pressed = false; }
    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, original * (pressed ? 0.96f : 1f), 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime));
    }
    void OnDisable() { transform.localScale = original; pressed = false; }
}

public class FleetBottomEdge : MonoBehaviour
{
    RectTransform rect, parent;
    void Awake() { rect = (RectTransform)transform; parent = rect.parent as RectTransform; }
    void LateUpdate()
    {
        if (!parent) return;
        var screen = new Vector2(Screen.width * 0.5f, Screen.safeArea.yMin);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, null, out var local))
            rect.anchoredPosition = new Vector2(0, local.y);
    }
}

public class FleetBackgroundFit : MonoBehaviour
{
    public RectTransform page;
    public UnityEngine.UI.RawImage image;
    void LateUpdate()
    {
        if (!page || !image || !image.texture) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(page, Vector2.zero, null, out var min) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(page, new Vector2(Screen.width, Screen.height), null, out var max)) return;
        float width = Mathf.Abs(max.x - min.x), height = Mathf.Abs(max.y - min.y);
        if (width < 1 || height < 1) return;
        var rt = image.rectTransform;
        rt.anchoredPosition = new Vector2(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y)) - page.rect.min;
        rt.sizeDelta = new Vector2(width, height);
        float imageAspect = (float)image.texture.width / image.texture.height;
        float screenAspect = width / height;
        if (imageAspect > screenAspect)
        {
            float visible = screenAspect / imageAspect;
            image.uvRect = new Rect((1f - visible) / 2f, 0, visible, 1);
        }
        else
        {
            float visible = imageAspect / screenAspect;
            image.uvRect = new Rect(0, (1f - visible) / 2f, 1, visible);
        }
    }
}
