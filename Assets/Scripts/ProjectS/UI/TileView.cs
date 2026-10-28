using UnityEngine;

namespace ProjectS.UI
{
    /// <summary>
    /// 타일 1칸의 표시(프리팹: Assets/Prefabs/TileView.prefab). 바닥 스프라이트 + 하이라이트 오버레이.
    /// 스프라이트 크기와 무관하게 칸 크기에 맞춰 늘린다.
    /// </summary>
    public sealed class TileView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private SpriteRenderer highlightRenderer;

        public void Configure(Sprite sprite, Color tint, float cellSize)
        {
            if (sprite != null) baseRenderer.sprite = sprite;
            baseRenderer.color = tint;
            baseRenderer.sortingOrder = PrototypeVisuals.TileOrder;
            FitToCell(baseRenderer, cellSize * (sprite != null ? 1f : 0.95f)); // 흰 사각형은 틈으로 격자선 표현

            highlightRenderer.sortingOrder = PrototypeVisuals.HighlightOrder;
            FitToCell(highlightRenderer, cellSize * 0.95f);
            SetHighlight(null);
        }

        public void SetHighlight(Color? color)
        {
            highlightRenderer.enabled = color.HasValue;
            if (color.HasValue) highlightRenderer.color = color.Value;
        }

        public static void FitToCell(SpriteRenderer renderer, float size)
        {
            if (renderer.sprite == null) return;
            var s = renderer.sprite.bounds.size;
            renderer.transform.localScale = new Vector3(size / s.x, size / s.y, 1f);
        }
    }
}
