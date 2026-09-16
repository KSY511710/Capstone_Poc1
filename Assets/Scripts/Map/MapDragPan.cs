using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 화면을 마우스로 드래그하면 target을 세로로만 움직여서 맵을 위아래로 스크롤한다
/// </summary>
public class MapDragPan : MonoBehaviour, IDragHandler
{
    private RectTransform target;
    private Canvas canvas;

    /// <summary>
    /// 드래그로 움직일 대상과, 스크린 픽셀 → 로컬 좌표 보정에 쓸 Canvas를 지정한다.
    /// </summary>
    public void Initialize(RectTransform target, Canvas canvas)
    {
        this.target = target;
        this.canvas = canvas;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float scale = canvas != null ? canvas.scaleFactor : 1f;
        Vector2 pos = target.anchoredPosition;
        pos.y += eventData.delta.y / scale;
        target.anchoredPosition = pos;
    }
}
