using UnityEngine;

namespace ProjectS.UI
{
    /// <summary>
    /// 전장 카메라. 현재는 전장 전체가 보이도록 맞추기만 한다.
    /// TODO(P1): 팬 / 제한된 줌. 전투 행동 상태와 무관하게 동작해야 한다.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class BattlefieldCamera : MonoBehaviour
    {
        private const float Margin = 1f;

        public void FitTo(Rect worldBounds)
        {
            var cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = PrototypeVisuals.Background;

            var center = worldBounds.center;
            transform.position = new Vector3(center.x, center.y, -10f);

            float byHeight = worldBounds.height * 0.5f + Margin;
            float byWidth = (worldBounds.width * 0.5f + Margin) / cam.aspect;
            cam.orthographicSize = Mathf.Max(byHeight, byWidth);
        }
    }
}
