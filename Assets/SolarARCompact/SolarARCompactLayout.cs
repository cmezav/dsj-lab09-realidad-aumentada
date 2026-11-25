using UnityEngine;

namespace SolarARCompact
{
    public sealed class SolarARCompactLayout : MonoBehaviour
    {
        private RectTransform panel;
        private RectTransform canvasRect;
        private RectTransform status;
        private float previousWidth = -1f;
        private void Start()
        {
            panel = GetComponent<RectTransform>();
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) {
                canvasRect = canvas.GetComponent<RectTransform>();
                foreach (var rect in canvas.GetComponentsInChildren<RectTransform>(true))
                    if (rect.name == "EstadoReconocimiento") { status = rect; break; }
            }
            Resize();
        }
        private void LateUpdate() { Resize(); }
        private void Resize()
        {
            if (panel == null || canvasRect == null) return;
            float width = canvasRect.rect.width;
            if (width <= 0f || Mathf.Abs(width - previousWidth) < 0.5f) return;
            previousWidth = width;
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(680f, width * 0.72f));
            if (status != null) status.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Min(560f, width * 0.72f));
        }
    }
}
