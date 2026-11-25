using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace SolarARUpgrade.Editor
{
    [InitializeOnLoad]
    public static class SolarARUpgradeInstaller
    {
        private const string Root = "Assets/SolarARUpgrade";
        private const string Request = Root + "/ApplyUpgrade.request.txt";
        static SolarARUpgradeInstaller() { EditorApplication.update += TryAutomatic; }
        private static void TryAutomatic()
        {
            if (!File.Exists(Request)) { EditorApplication.update -= TryAutomatic; return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (FindSelector() == null) return;
            EditorApplication.update -= TryAutomatic;
            Apply();
        }
        private static MultiTargetVuforia FindSelector()
        {
            foreach (var item in UnityEngine.Object.FindObjectsByType<MultiTargetVuforia>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (item.gameObject.scene.IsValid() && item.gameObject.scene.isLoaded) return item;
            return null;
        }
        [MenuItem("Tools/Solar AR/Aplicar ajustes finales")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorUtility.DisplayDialog("Solar AR", "Sal de Play antes de aplicar.", "Aceptar"); return;
            }
            var selector = FindSelector();
            if (selector == null) {
                EditorUtility.DisplayDialog("Solar AR", "Abre la escena SolarAR con su ImageTarget.", "Aceptar"); return;
            }
            try {
                TMP_FontAsset titleFont = FontAsset("Spacesky/Spacesky PERSONAL USE ONLY!.ttf", "SolarTitle");
                TMP_FontAsset buttonFont = FontAsset("PlanetEstyle/PLANE___.TTF", "SolarButtons");
                var presenter = selector.GetComponent<ARPresentationFix>();
                if (presenter == null) presenter = Undo.AddComponent<ARPresentationFix>(selector.gameObject);
                Undo.RecordObject(presenter, "Configurar presentacion AR");
                presenter.enabled = true;
                presenter.faceCamera = true;
                presenter.characterYaw = 180f;
                presenter.characterHeight = 0.055f;
                presenter.planetHeight = 0.055f;
                foreach (Transform model in selector.transform) {
                    if (!ARPresentationFix.IsPlanet(model.name) && !model.name.StartsWith("AR_")) continue;
                    Undo.RecordObject(model, "Centrar modelo AR");
                    model.localPosition = new Vector3(0f, 0.055f, 0f);
                    if (model.name.StartsWith("AR_")) model.localRotation = Quaternion.identity;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(model);
                }
                ARModelInfo info = null;
                foreach (var candidate in UnityEngine.Object.FindObjectsByType<ARModelInfo>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (candidate.imageTarget == selector.transform) { info = candidate; break; }
                if (info == null || info.informationText == null)
                    throw new InvalidOperationException("No se encontro ARModelInfo con sus referencias. Revisa el PanelInformacion.");
                RectTransform panel = info.GetComponent<RectTransform>();
                var background = info.GetComponent<Image>();
                if (panel == null || background == null) throw new InvalidOperationException("ARModelInfo debe estar en PanelInformacion (con Image).");
                Undo.RecordObject(panel, "Disenar panel espacial");
                panel.anchorMin = new Vector2(0f, 0f);
                panel.anchorMax = new Vector2(1f, 0f);
                panel.pivot = new Vector2(0.5f, 0.5f);
                panel.sizeDelta = new Vector2(-64f, 205f);
                panel.anchoredPosition = new Vector2(0f, 240f);
                Undo.RecordObject(background, "Color panel");
                background.color = new Color(0.05f, 0.09f, 0.16f, 0.88f);
                background.raycastTarget = false;
                var outline = panel.GetComponent<Outline>();
                if (outline == null) outline = Undo.AddComponent<Outline>(panel.gameObject);
                outline.effectColor = new Color(0.3f, 0.8f, 1f, 0.65f);
                outline.effectDistance = new Vector2(1f, -1f);
                TextMeshProUGUI title = ChildText(panel, "TituloEspacial");
                title.font = titleFont;
                title.fontStyle = FontStyles.Normal;
                title.fontSize = 36f;
                title.enableAutoSizing = true;
                title.fontSizeMin = 20f;
                title.fontSizeMax = 36f;
                title.color = Color.white;
                title.alignment = TextAlignmentOptions.Center;
                title.raycastTarget = false;
                SetTop(title.rectTransform, 18f, 28f, 56f);
                title.text = "EXPLORACION ESPACIAL";
                Undo.RecordObject(info, "Asignar titulo espacial");
                info.titleText = title;
                var body = info.informationText;
                Undo.RecordObject(body, "Estilo descripcion");
                body.color = new Color(0.94f, 0.96f, 1f, 1f);
                body.fontStyle = FontStyles.Normal;
                body.alignment = TextAlignmentOptions.Center;
                body.fontSize = 28f;
                body.enableAutoSizing = true;
                body.fontSizeMin = 20f;
                body.fontSizeMax = 28f;
                body.raycastTarget = false;
                body.rectTransform.anchorMin = Vector2.zero;
                body.rectTransform.anchorMax = Vector2.one;
                body.rectTransform.offsetMin = new Vector2(28f, 38f);
                body.rectTransform.offsetMax = new Vector2(-28f, -78f);
                var hint = ChildText(panel, "AyudaExploracion");
                hint.font = buttonFont;
                hint.text = "EXPLORA CON LOS BOTONES O LAS FLECHAS DEL TECLADO";
                hint.fontSize = 16f;
                hint.enableAutoSizing = true;
                hint.fontSizeMin = 10f;
                hint.fontSizeMax = 16f;
                hint.alignment = TextAlignmentOptions.Center;
                hint.color = new Color(0.65f, 0.85f, 1f, 1f);
                hint.raycastTarget = false;
                hint.rectTransform.anchorMin = new Vector2(0f, 0f);
                hint.rectTransform.anchorMax = new Vector2(1f, 0f);
                hint.rectTransform.pivot = new Vector2(0.5f, 0f);
                hint.rectTransform.sizeDelta = new Vector2(-48f, 26f);
                hint.rectTransform.anchoredPosition = new Vector2(0f, 6f);
                int buttons = 0;
                foreach (Button button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                    if (button.gameObject.scene != selector.gameObject.scene) continue;
                    if (button.name == "ButtonPrevio") { StyleButton(button, "< VOLVER", -1, selector, buttonFont); buttons++; }
                    if (button.name == "ButtonSiguiente") { StyleButton(button, "EXPLORAR >", 1, selector, buttonFont); buttons++; }
                }
                if (buttons != 2) Debug.LogWarning("Solar AR: se esperaban ButtonPrevio y ButtonSiguiente. Revisa los nombres si no se estilizaron.");
                EditorUtility.SetDirty(info);
                EditorUtility.SetDirty(presenter);
                EditorSceneManager.MarkSceneDirty(selector.gameObject.scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(selector.gameObject.scene)) throw new IOException("No se pudo guardar SolarAR.");
                if (File.Exists(Request)) AssetDatabase.DeleteAsset(Request);
                Debug.Log("Solar AR: centrado, orientacion, flechas y fuentes aplicados y guardados.");
                EditorUtility.DisplayDialog("Solar AR listo", "Se guardaron los ajustes. Pulsa Play y haz clic dentro de Game para probar las flechas. Los botones tactiles siguen funcionando.\n\nSi un modelo aun muestra la espalda, ajusta Character Yaw de ImageTarget entre 180 y 0; no cambies la rotacion de ARCamera.", "Aceptar");
            }
            catch (Exception error) {
                Debug.LogException(error);
                EditorUtility.DisplayDialog("Solar AR", "No se completo el ajuste. Revisa Console: " + error.Message, "Aceptar");
            }
        }
        private static void StyleButton(Button button, string label, int step, MultiTargetVuforia selector, TMP_FontAsset font)
        {
            Undo.RecordObject(button, "Configurar boton");
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddIntPersistentListener(button.onClick, selector.ChangeARModel, step);
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.72f, 0.9f, 1f);
            colors.pressedColor = new Color(0.45f, 0.7f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
            var rect = button.GetComponent<RectTransform>();
            Undo.RecordObject(rect, "Colocar boton");
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(step * 190f, 80f);
            rect.sizeDelta = new Vector2(340f, 82f);
            var image = button.GetComponent<Image>();
            if (image != null) {
                Undo.RecordObject(image, "Color boton");
                image.color = new Color(0.08f, 0.19f, 0.3f, 0.96f);
                image.raycastTarget = true;
                button.targetGraphic = image;
            }
            var text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null) {
                Undo.RecordObject(text, "Fuente boton");
                text.text = label;
                text.font = font;
                text.fontStyle = FontStyles.Normal;
                text.fontSize = 28f;
                text.enableAutoSizing = true;
                text.fontSizeMin = 18f;
                text.fontSizeMax = 28f;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
                text.raycastTarget = false;
                text.rectTransform.anchorMin = Vector2.zero;
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.offsetMin = new Vector2(12f, 8f);
                text.rectTransform.offsetMax = new Vector2(-12f, -8f);
            }
            EditorUtility.SetDirty(button);
        }
        private static TextMeshProUGUI ChildText(RectTransform panel, string name)
        {
            var child = panel.Find(name);
            if (child != null) return child.GetComponent<TextMeshProUGUI>();
            var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            Undo.RegisterCreatedObjectUndo(obj, "Crear texto espacial");
            obj.layer = panel.gameObject.layer;
            obj.transform.SetParent(panel, false);
            return obj.GetComponent<TextMeshProUGUI>();
        }
        private static void SetTop(RectTransform rect, float top, float sides, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(-sides * 2f, height);
            rect.anchoredPosition = new Vector2(0f, -top);
        }
        private static TMP_FontAsset FontAsset(string source, string name)
        {
            if (!AssetDatabase.IsValidFolder(Root + "/Generated")) AssetDatabase.CreateFolder(Root, "Generated");
            string path = Root + "/Generated/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/" + source);
            if (font == null) throw new InvalidOperationException("Falta la fuente " + source);
            var asset = TMP_FontAsset.CreateFontAsset(font, 72, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (asset == null) throw new InvalidOperationException("No se pudo crear TMP para " + source);
            asset.name = name;
            var characters = new StringBuilder();
            for (int c = 32; c <= 126; c++) characters.Append((char)c);
            characters.Append("áéíóúüñÁÉÍÓÚÜÑ¿¡");
            asset.TryAddCharacters(characters.ToString());
            asset.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(asset, path);
            if (asset.material != null && !AssetDatabase.Contains(asset.material)) AssetDatabase.AddObjectToAsset(asset.material, asset);
            foreach (var atlas in asset.atlasTextures)
                if (atlas != null && !AssetDatabase.Contains(atlas)) AssetDatabase.AddObjectToAsset(atlas, asset);
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
