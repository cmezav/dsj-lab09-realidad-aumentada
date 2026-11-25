using System;
using System.IO;
using TMPro;
using Image = UnityEngine.UI.Image;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Vuforia;

namespace SolarARControls.Editor
{
    [InitializeOnLoad]
    public static class SolarARControlsInstaller
    {
        private const string Request = "Assets/SolarARControls/ApplyControls.request.txt";
        static SolarARControlsInstaller() { EditorApplication.update += AutoApply; }
        private static void AutoApply()
        {
            if (!File.Exists(Request)) { EditorApplication.update -= AutoApply; return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (FindInfo() == null) return;
            EditorApplication.update -= AutoApply;
            Apply();
        }
        private static ARModelInfo FindInfo()
        {
            foreach (var info in UnityEngine.Object.FindObjectsByType<ARModelInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (info.gameObject.scene.IsValid() && info.imageTarget != null && info.informationText != null) return info;
            return null;
        }
        [MenuItem("Tools/Solar AR/Agregar estado y control de tamano")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var info = FindInfo();
            if (info == null) { EditorUtility.DisplayDialog("Solar AR", "Abre SolarAR con su panel informativo.", "Aceptar"); return; }
            try {
                var canvas = info.GetComponentInParent<Canvas>();
                var observer = info.imageTarget.GetComponent<ObserverBehaviour>();
                if (canvas == null || observer == null) throw new InvalidOperationException("Falta Canvas o el Observer de Vuforia en ImageTarget.");
                var holder = Child(canvas.transform, "ControlesExperienciaAR");
                holder.anchorMin = Vector2.zero; holder.anchorMax = Vector2.one;
                holder.offsetMin = Vector2.zero; holder.offsetMax = Vector2.zero;
                var controls = holder.GetComponent<SolarARExperienceControls>();
                if (controls == null) controls = Undo.AddComponent<SolarARExperienceControls>(holder.gameObject);
                Undo.RecordObject(controls, "Configurar controles AR");
                controls.imageTarget = info.imageTarget;
                controls.observer = observer;
                controls.minimumFactor = 0.5f;
                controls.maximumFactor = 2f;
                var statusPanel = Child(holder, "EstadoReconocimiento");
                statusPanel.anchorMin = new Vector2(0f, 1f); statusPanel.anchorMax = Vector2.one;
                statusPanel.pivot = new Vector2(0.5f, 1f);
                statusPanel.sizeDelta = new Vector2(-64f, 82f);
                statusPanel.anchoredPosition = new Vector2(0f, -28f);
                Background(statusPanel);
                var status = Text(statusPanel, "TextoEstado", info.informationText.font, "Apunta a la imagen", 28f);
                Stretch(status.rectTransform, 12f);
                controls.statusText = status;
                var sizePanel = Child(holder, "TamanoModelo");
                sizePanel.anchorMin = sizePanel.anchorMax = new Vector2(0.5f, 0f);
                sizePanel.pivot = new Vector2(0.5f, 0.5f);
                sizePanel.sizeDelta = new Vector2(620f, 72f);
                sizePanel.anchoredPosition = new Vector2(0f, 170f);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/SolarARUpgrade/Generated/SolarButtons.asset");
                if (font == null) font = info.informationText.font;
                controls.smallerButton = Button(sizePanel, "Reducir", "-", -235f, 130f, font);
                controls.resetButton = Button(sizePanel, "Restablecer", "RESTABLECER", 0f, 300f, font);
                controls.largerButton = Button(sizePanel, "Ampliar", "+", 235f, 130f, font);
                controls.smallerButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                controls.largerButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                controls.resetButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                UnityEventTools.AddPersistentListener(controls.smallerButton.onClick, controls.DecreaseSize);
                UnityEventTools.AddPersistentListener(controls.largerButton.onClick, controls.IncreaseSize);
                UnityEventTools.AddPersistentListener(controls.resetButton.onClick, controls.ResetSize);
                var panel = info.GetComponent<RectTransform>();
                Undo.RecordObject(panel, "Separar informacion y controles");
                panel.anchoredPosition = new Vector2(0f, 325f);
                var help = panel.Find("AyudaExploracion");
                if (help != null) {
                    var text = help.GetComponent<TMP_Text>();
                    if (text != null) { Undo.RecordObject(text, "Ayuda de teclado"); text.text = "IZQ / DER: MODELO    ARRIBA / ABAJO: TAMANO"; }
                }
                foreach (var graphic in holder.GetComponentsInChildren<Graphic>(true)) EditorUtility.SetDirty(graphic);
                EditorUtility.SetDirty(controls);
                EditorUtility.SetDirty(controls.smallerButton);
                EditorUtility.SetDirty(controls.largerButton);
                EditorUtility.SetDirty(controls.resetButton);
                EditorSceneManager.MarkSceneDirty(info.gameObject.scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(info.gameObject.scene)) throw new IOException("No se pudo guardar la escena.");
                if (File.Exists(Request)) AssetDatabase.DeleteAsset(Request);
                EditorUtility.DisplayDialog("Solar AR listo", "Estado y tamano instalados y guardados. Play: arriba amplia; abajo reduce. Haz clic dentro de Game para usar el teclado. Restablecer recupera el tamano original del modelo seleccionado.", "Aceptar");
            }
            catch (Exception error) {
                Debug.LogException(error);
                EditorUtility.DisplayDialog("Solar AR", "Revisa Console: " + error.Message, "Aceptar");
            }
        }
        private static RectTransform Child(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.GetComponent<RectTransform>();
            var obj = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(obj, "Agregar controles AR");
            obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false);
            return obj.GetComponent<RectTransform>();
        }
        private static Image Background(RectTransform rect)
        {
            var image = rect.GetComponent<Image>();
            if (image == null) image = Undo.AddComponent<Image>(rect.gameObject);
            image.color = new Color(0.04f, 0.09f, 0.16f, 0.88f);
            image.raycastTarget = false;
            return image;
        }
        private static TextMeshProUGUI Text(Transform parent, string name, TMP_FontAsset font, string value, float size)
        {
            var rect = Child(parent, name);
            var text = rect.GetComponent<TextMeshProUGUI>();
            if (text == null) text = Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            text.font = font; text.text = value; text.fontSize = size;
            text.enableAutoSizing = true; text.fontSizeMin = 16f; text.fontSizeMax = size;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white; text.raycastTarget = false;
            return text;
        }
        private static UnityEngine.UI.Button Button(Transform parent, string name, string label, float x, float width, TMP_FontAsset font)
        {
            var rect = Child(parent, name);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f); rect.sizeDelta = new Vector2(width, 66f);
            var image = Background(rect); image.raycastTarget = true;
            image.color = new Color(0.08f, 0.19f, 0.3f, 0.96f);
            var button = rect.GetComponent<UnityEngine.UI.Button>();
            if (button == null) button = Undo.AddComponent<UnityEngine.UI.Button>(rect.gameObject);
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var text = Text(rect, "Texto", font, label, label.Length > 1 ? 24f : 36f);
            Stretch(text.rectTransform, 8f);
            return button;
        }
        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * padding; rect.offsetMax = Vector2.one * -padding;
        }
    }
}
