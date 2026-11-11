using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class MapWheelZoom : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform map, viewport;
    public Func<bool> canZoom;
    public Action onPanStart;
    public Func<float> maxZoom;
    const float MinZoom = 1f;
    float pinchDistance;
    bool panning;
    Vector2 previous;
    Vector2 ClampPosition(Vector2 position, float scale)
    {
        float width = map.rect.width, height = map.rect.height;
        float marginX = width * 0.08f, marginY = height * 0.05f;
        position.x = Mathf.Clamp(position.x, width * (1f - scale) - marginX, marginX);
        position.y = Mathf.Clamp(position.y, height * (1f - scale) - marginY, marginY);
        return position;
    }
    void ZoomAt(Vector2 screenPoint, float next)
    {
        float current = map.localScale.x;
        next = Mathf.Clamp(next, MinZoom, maxZoom != null ? maxZoom() : 2.5f);
        if (Mathf.Approximately(next, current)) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(map, screenPoint, null, out var local)) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, null, out var desired)) return;
        map.localScale = Vector3.one * next;
        Vector2 actual = viewport.InverseTransformPoint(map.TransformPoint(local));
        map.anchoredPosition = ClampPosition(map.anchoredPosition + desired - actual, next);
    }
    public void OnScroll(PointerEventData e)
    {
        if (!map || !viewport || (canZoom != null && !canZoom()) || Mathf.Approximately(e.scrollDelta.y, 0)) return;
        ZoomAt(e.position, map.localScale.x * Mathf.Pow(1.12f, e.scrollDelta.y));
    }
    void Update()
    {
        if (Input.touchCount != 2 || !map || !viewport || (canZoom != null && !canZoom())) { pinchDistance = 0; return; }
        Touch a = Input.GetTouch(0), b = Input.GetTouch(1);
        float distance = Vector2.Distance(a.position, b.position);
        if (pinchDistance > 1f && distance > 1f)
            ZoomAt((a.position + b.position) * 0.5f, map.localScale.x * distance / pinchDistance);
        else { panning = false; onPanStart?.Invoke(); }
        pinchDistance = distance;
    }
    public void OnBeginDrag(PointerEventData e)
    {
        if (!map || !viewport || Input.touchCount >= 2 || (canZoom != null && !canZoom())) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, null, out previous)) return;
        panning = true; onPanStart?.Invoke();
    }
    public void OnDrag(PointerEventData e)
    {
        if (!panning || (canZoom != null && !canZoom())) return;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, null, out var point)) return;
        map.anchoredPosition = ClampPosition(map.anchoredPosition + point - previous, map.localScale.x);
        previous = point;
    }
    public void OnEndDrag(PointerEventData e) { panning = false; }
    void OnDisable() { panning = false; pinchDistance = 0; }
}
