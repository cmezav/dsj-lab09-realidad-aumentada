using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SolarARControls
{
    public sealed class SolarARExperienceControls : MonoBehaviour
    {
        public Transform imageTarget;
        public ObserverBehaviour observer;
        public TMP_Text statusText;
        public Button smallerButton;
        public Button largerButton;
        public Button resetButton;
        public float minimumFactor = 0.5f;
        public float maximumFactor = 2f;
        private const float Step = 1.15f;
        private readonly Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
        private readonly Dictionary<Transform, float> factors = new Dictionary<Transform, float>();
        private Transform selected;
        private bool everRecognized;
        private ObserverBehaviour subscribedObserver;

        private void Awake() { CacheModels(); }
        private void OnEnable()
        {
            if (observer == null && imageTarget != null) observer = imageTarget.GetComponent<ObserverBehaviour>();
            subscribedObserver = observer;
            if (subscribedObserver != null) {
                subscribedObserver.OnTargetStatusChanged += StatusChanged;
                DisplayStatus(subscribedObserver.TargetStatus);
            } else if (statusText != null) {
                statusText.text = "Apunta a la imagen";
                statusText.color = new Color(0.7f, 0.87f, 1f);
            }
        }
        private void OnDisable()
        {
            if (subscribedObserver != null) subscribedObserver.OnTargetStatusChanged -= StatusChanged;
            subscribedObserver = null;
        }
        private void StatusChanged(ObserverBehaviour source, TargetStatus status) { DisplayStatus(status); }
        private void DisplayStatus(TargetStatus status)
        {
            if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED) everRecognized = true;
            if (statusText == null) return;
            if (status.Status == Status.TRACKED) {
                statusText.text = "Imagen reconocida";
                statusText.color = new Color(0.45f, 1f, 0.69f);
            } else if (status.Status == Status.EXTENDED_TRACKED) {
                statusText.text = "Imagen reconocida\n<size=75%>Seguimiento extendido: vuelve a encuadrar la imagen</size>";
                statusText.color = new Color(1f, 0.85f, 0.45f);
            } else if (everRecognized) {
                statusText.text = "Seguimiento perdido";
                statusText.color = new Color(1f, 0.55f, 0.55f);
            } else {
                statusText.text = "Apunta a la imagen";
                statusText.color = new Color(0.7f, 0.87f, 1f);
            }
        }
        private void CacheModels()
        {
            if (imageTarget == null) return;
            foreach (Transform child in imageTarget) {
                if (originalScales.ContainsKey(child)) continue;
                originalScales.Add(child, child.localScale);
                factors.Add(child, 1f);
            }
        }
        private Transform FindSelected()
        {
            if (imageTarget == null) return null;
            for (int i = 0; i < imageTarget.childCount; i++) {
                Transform child = imageTarget.GetChild(i);
                if (child.gameObject.activeSelf) return child;
            }
            return null;
        }
        private void Update()
        {
            Transform active = FindSelected();
            if (active != selected) { selected = active; RefreshButtons(); }
            bool up = false, down = false;
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard != null) {
                up = keyboard.upArrowKey.wasPressedThisFrame;
                down = keyboard.downArrowKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            up = Input.GetKeyDown(KeyCode.UpArrow);
            down = Input.GetKeyDown(KeyCode.DownArrow);
#endif
            if (up != down) SetSize(up ? Step : 1f / Step);
        }
        public void IncreaseSize() { SetSize(Step); }
        public void DecreaseSize() { SetSize(1f / Step); }
        private void SetSize(float multiplier)
        {
            selected = FindSelected();
            if (selected == null) return;
            CacheModels();
            float factor = Mathf.Clamp(factors[selected] * multiplier, minimumFactor, maximumFactor);
            factors[selected] = factor;
            selected.localScale = originalScales[selected] * factor;
            RefreshButtons();
        }
        public void ResetSize()
        {
            selected = FindSelected();
            if (selected == null) return;
            CacheModels();
            factors[selected] = 1f;
            selected.localScale = originalScales[selected];
            RefreshButtons();
        }
        private void RefreshButtons()
        {
            CacheModels();
            float factor = selected != null && factors.ContainsKey(selected) ? factors[selected] : 1f;
            if (smallerButton != null) smallerButton.interactable = selected != null && factor > minimumFactor + 0.001f;
            if (largerButton != null) largerButton.interactable = selected != null && factor < maximumFactor - 0.001f;
            if (resetButton != null) resetButton.interactable = selected != null;
        }
    }
}
