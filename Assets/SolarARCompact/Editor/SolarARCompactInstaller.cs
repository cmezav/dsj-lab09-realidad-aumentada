using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using SolarARPanelColors;

namespace SolarARCompact.Editor
{
    [InitializeOnLoad]
    public static class SolarARCompactInstaller
    {
        private const string Request = "Assets/SolarARCompact/ApplyCompact.request.txt";
        static SolarARCompactInstaller() { EditorApplication.update += Auto; }
        private static void Auto()
        {
            if (!File.Exists(Request)) { EditorApplication.update -= Auto; return; }
            if (EditorApplication.isUpdating || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (Find() == null) return;
            EditorApplication.update -= Auto;
            Apply();
        }
        private static ARModelInfo Find()
        {
            foreach (var item in UnityEngine.Object.FindObjectsByType<ARModelInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (item.gameObject.scene.IsValid() && item.imageTarget != null) return item;
            return null;
        }
        [MenuItem("Tools/Solar AR/Compactar panel y recuperar colores")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var info = Find();
            if (info == null) { EditorUtility.DisplayDialog("Solar AR", "Abre la escena SolarAR.", "Aceptar"); return; }
            try {
                var panel = info.GetComponent<RectTransform>();
                var image = info.GetComponent<Image>();
                if (panel == null || image == null) throw new InvalidOperationException("Falta el panel informativo con Image.");
                var colors = info.GetComponent<SolarARAutoPanelColor>();
                if (colors == null) colors = Undo.AddComponent<SolarARAutoPanelColor>(info.gameObject);
                Undo.RecordObject(colors, "Conectar colores");
                colors.info = info; colors.panel = image; colors.enabled = true;
                Undo.RecordObject(panel, "Compactar panel");
                Bottom(panel, 0f, 310f, 680f, 185f);
                var responsive = info.GetComponent<SolarARCompactLayout>();
                if (responsive == null) responsive = Undo.AddComponent<SolarARCompactLayout>(info.gameObject);
                responsive.enabled = true;
                if (info.titleText != null) {
                    var title = info.titleText;
                    Undo.RecordObject(title, "Compactar titulo");
                    title.enableAutoSizing = true; title.fontSizeMin = 20f; title.fontSizeMax = 32f; title.fontSize = 32f;
                    var rect = title.rectTransform;
                    rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = Vector2.one;
                    rect.pivot = new Vector2(0.5f, 1f); rect.sizeDelta = new Vector2(-40f, 44f);
                    rect.anchoredPosition = new Vector2(0f, -12f);
                }
                if (info.informationText != null) {
                    var body = info.informationText;
                    Undo.RecordObject(body, "Descripcion compacta");
                    body.enableAutoSizing = true; body.fontSizeMin = 18f; body.fontSizeMax = 25f; body.fontSize = 25f;
                    body.color = Color.white;
                    body.rectTransform.anchorMin = Vector2.zero; body.rectTransform.anchorMax = Vector2.one;
                    body.rectTransform.offsetMin = new Vector2(24f, 18f); body.rectTransform.offsetMax = new Vector2(-24f, -64f);
                }
                var hint = panel.Find("AyudaExploracion");
                if (hint != null) { Undo.RecordObject(hint.gameObject, "Reducir texto accesorio"); hint.gameObject.SetActive(false); }
                var canvas = info.GetComponentInParent<Canvas>();
                if (canvas != null) {
                    foreach (var button in canvas.GetComponentsInChildren<Button>(true)) {
                        var rect = button.GetComponent<RectTransform>();
                        Undo.RecordObject(rect, "Botones compactos");
                        switch (button.name) {
                            case "ButtonPrevio": Bottom(rect, -145f, 65f, 260f, 72f); break;
                            case "ButtonSiguiente": Bottom(rect, 145f, 65f, 260f, 72f); break;
                            case "Reducir": Center(rect, -205f, 110f); break;
                            case "Ampliar": Center(rect, 205f, 110f); break;
                            case "Restablecer": Center(rect, 0f, 260f); break;
                        }
                    }
                    foreach (var rect in canvas.GetComponentsInChildren<RectTransform>(true)) {
                        if (rect.name == "TamanoModelo") Bottom(rect, 0f, 160f, 520f, 66f);
                        if (rect.name == "EstadoReconocimiento") {
                            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                            rect.pivot = new Vector2(0.5f, 1f); rect.sizeDelta = new Vector2(560f, 72f);
                            rect.anchoredPosition = new Vector2(0f, -20f);
                        }
                    }
                }
                string name = "Earth";
                foreach (Transform child in info.imageTarget) if (child.gameObject.activeSelf) { name = child.name; break; }
                var color = SolarARAutoPanelColor.Palette(name); color.a = 0.92f;
                image.color = color;
                EditorUtility.SetDirty(colors);
                EditorUtility.SetDirty(responsive);
                EditorSceneManager.MarkSceneDirty(info.gameObject.scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(info.gameObject.scene)) throw new IOException("No se pudo guardar SolarAR.");
                if (File.Exists(Request)) AssetDatabase.DeleteAsset(Request);
                EditorUtility.DisplayDialog("Solar AR listo", "Panel mas compacto y colores conectados y guardados. Prueba Marte, Saturno y Finn: rojo, dorado y verde. Botones y borde acompanian el color.", "Aceptar");
            }
            catch (Exception error) { Debug.LogException(error); EditorUtility.DisplayDialog("Solar AR", error.Message, "Aceptar"); }
        }
        private static void Bottom(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f); rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        }
        private static void Center(RectTransform rect, float x, float width)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, 0f); rect.sizeDelta = new Vector2(width, 62f);
        }
    }
}
