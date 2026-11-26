using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SolarARTouchUpgrade
{
    [DefaultExecutionOrder(11000)]
    public sealed class SolarARDragRotation : MonoBehaviour
    {
        public float degreesAcrossScreen = 240f;
        public float maximumPitch = 70f;
        private readonly Dictionary<Transform, Vector2> angles = new Dictionary<Transform, Vector2>();
        private readonly List<RaycastResult> hits = new List<RaycastResult>();
        private Transform selected;
        private Transform draggingModel;
        private Vector2 lastPosition;
        private bool holding;
        private bool blocked;

        private Transform Selected()
        {
            foreach (Transform child in transform)
                if (child.gameObject.activeSelf && child.GetComponent<SolarARModelPivot>() != null) return child;
            return null;
        }

        private bool OverUI(Vector2 position)
        {
            if (EventSystem.current == null) return false;
            hits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = position }, hits);
            foreach (var hit in hits)
                if (hit.module is GraphicRaycaster) return true;
            return false;
        }

        private void Update()
        {
            selected = Selected();
            if (selected == null) { holding = false; return; }
            bool down = false;
            bool multiTouch = false;
            Vector2 position = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            var screen = Touchscreen.current;
            if (screen != null && screen.primaryTouch.press.isPressed) {
                down = true;
                position = screen.primaryTouch.position.ReadValue();
                int count = 0;
                foreach (var touch in screen.touches) if (touch.press.isPressed) count++;
                multiTouch = count > 1;
            } else if (Mouse.current != null) {
                down = Mouse.current.leftButton.isPressed;
                position = Mouse.current.position.ReadValue();
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0) {
                var touch = Input.GetTouch(0);
                down = touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
                position = touch.position;
                multiTouch = Input.touchCount > 1;
            } else {
                down = Input.GetMouseButton(0);
                position = Input.mousePosition;
            }
#endif
            if (!down) { holding = false; return; }
            if (!holding) {
                holding = true;
                blocked = OverUI(position) || multiTouch;
                draggingModel = selected;
                lastPosition = position;
                return;
            }
            if (draggingModel != selected || multiTouch) blocked = true;
            Vector2 delta = position - lastPosition;
            lastPosition = position;
            if (blocked || OverUI(position)) return;
            angles.TryGetValue(selected, out Vector2 value);
            float sensitivity = degreesAcrossScreen / Mathf.Max(1f, Screen.width);
            value.x = Mathf.Repeat(value.x - delta.x * sensitivity, 360f);
            value.y = Mathf.Clamp(value.y + delta.y * sensitivity, -maximumPitch, maximumPitch);
            angles[selected] = value;
        }

        private void LateUpdate()
        {
            Transform model = Selected();
            if (model == null) return;
            angles.TryGetValue(model, out Vector2 value);
            Quaternion baseline = Quaternion.identity;
            Camera camera = Camera.main;
            if (model.name.StartsWith("AR_")) {
                // ARPresentationFix acaba de orientar el personaje hacia la camara.
                var presenter = GetComponent<ARPresentationFix>();
                if (camera != null && presenter != null && presenter.faceCamera) baseline = model.localRotation;
            } else if (model.name == "Saturn" && camera != null) {
                Vector3 direction = camera.transform.position - model.position;
                if (direction.sqrMagnitude > 0.000001f) {
                    Quaternion view = Quaternion.LookRotation(direction, camera.transform.up);
                    // Anillo en XZ: 25 grados desde el canto produce una elipse legible.
                    baseline = Quaternion.Inverse(transform.rotation) * view * Quaternion.Euler(25f, 0f, -12f);
                }
            }
            model.localRotation = baseline * Quaternion.Euler(value.y, value.x, 0f);
        }

        public void ResetCurrentRotation()
        {
            Transform model = Selected();
            if (model != null) angles[model] = Vector2.zero;
            holding = false;
        }
    }
}
