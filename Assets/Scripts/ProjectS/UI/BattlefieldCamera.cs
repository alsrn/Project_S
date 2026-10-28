using UnityEngine;

namespace ProjectS.UI
{
    /// <summary>
    /// 전장 카메라. UI 가 차지하는 좌/우 열·상/하단 띠(UiLayout)를 피해 전장이 가운데 남는 영역에 꽉 차도록
    /// 직교 크기와 위치를 맞춘다. 화면 크기가 바뀌면 다시 맞춘다.
    /// TODO(P1): 팬 / 제한된 줌. 전투 행동 상태와 무관하게 동작해야 한다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BattlefieldCamera : MonoBehaviour
    {
        private const float WorldMargin = 0.3f;

        private Camera cam;
        private Rect worldBounds;
        private Canvas uiCanvas;
        private Vector2Int lastScreen;
        private float lastScale;

        public void FitTo(Rect bounds, Canvas canvas)
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PrototypeVisuals.Background;
            worldBounds = bounds;
            uiCanvas = canvas;
            Refit();
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            float scale = uiCanvas != null ? uiCanvas.scaleFactor : 1f;
            if (Screen.width != lastScreen.x || Screen.height != lastScreen.y || !Mathf.Approximately(scale, lastScale))
                Refit();
        }

        private void Refit()
        {
            float scale = uiCanvas != null ? uiCanvas.scaleFactor : Screen.height / UiLayout.ReferenceResolution.y;
            lastScreen = new Vector2Int(Screen.width, Screen.height);
            lastScale = scale;

            // UI 를 뺀 화면 영역(픽셀)
            float left = UiLayout.SideColumnWidth * scale;
            float right = Screen.width - UiLayout.SideColumnWidth * scale;
            float bottom = UiLayout.BottomBarHeight * scale;
            float top = Screen.height - UiLayout.TopBarHeight * scale;
            float availW = Mathf.Max(1f, right - left);
            float availH = Mathf.Max(1f, top - bottom);

            float worldW = worldBounds.width + WorldMargin * 2f;
            float worldH = worldBounds.height + WorldMargin * 2f;
            float unitsPerPixel = Mathf.Max(worldW / availW, worldH / availH);
            cam.orthographicSize = Screen.height * unitsPerPixel * 0.5f;

            // 가용 영역 중심이 전장 중심에 오도록 카메라를 옮긴다
            var areaCenterPx = new Vector2((left + right) * 0.5f, (bottom + top) * 0.5f);
            var offsetPx = areaCenterPx - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var center = worldBounds.center - offsetPx * unitsPerPixel;
            transform.position = new Vector3(center.x, center.y, -10f);
        }
    }
}
