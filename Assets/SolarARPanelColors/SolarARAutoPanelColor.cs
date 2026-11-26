using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SolarARPanelColors
{
    public sealed class SolarARAutoPanelColor : MonoBehaviour
    {
        public ARModelInfo info;
        public Image panel;
        private Image[] surfaces;
        private TMP_Text title;
        private Outline outline;
        private Transform previous;
        private Color goal;
        private Color accent;
        private bool ready;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() { Attach(); }
        private static void OnLoaded(Scene scene, LoadSceneMode mode) { Attach(); }
        private static void Attach()
        {
            foreach (var source in Object.FindObjectsByType<ARModelInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                var effect = source.GetComponent<SolarARAutoPanelColor>();
                if (effect == null) effect = source.gameObject.AddComponent<SolarARAutoPanelColor>();
                effect.info = source;
                if (effect.panel == null) effect.panel = source.GetComponent<Image>();
                effect.Initialize();
            }
        }
        private void Awake() { Initialize(); }
        private void OnEnable() { Initialize(); }
        private void Initialize()
        {
            if (info == null) info = GetComponent<ARModelInfo>();
            if (panel == null) panel = GetComponent<Image>();
            if (info == null || panel == null) return;
            title = info.titleText;
            outline = panel.GetComponent<Outline>();
            var list = new List<Image>();
            var canvas = info.GetComponentInParent<Canvas>();
            if (canvas != null) {
                foreach (var button in canvas.GetComponentsInChildren<Button>(true)) {
                    var image = button.targetGraphic as Image;
                    if (image != null && image != panel && !list.Contains(image)) list.Add(image);
                }
                foreach (var image in canvas.GetComponentsInChildren<Image>(true))
                    if (image.name == "EstadoReconocimiento" && !list.Contains(image)) list.Add(image);
            }
            surfaces = list.ToArray();
            previous = null;
            goal = panel.color;
            accent = Color.white;
            ready = true;
        }
        private void LateUpdate()
        {
            if (!ready) Initialize();
            if (!ready || info == null || info.imageTarget == null || panel == null) return;
            Transform selected = null;
            for (int i = 0; i < info.imageTarget.childCount; i++) {
                var child = info.imageTarget.GetChild(i);
                if (child.gameObject.activeSelf) { selected = child; break; }
            }
            if (selected != null && selected != previous) {
                previous = selected;
                goal = Palette(selected.name);
                goal.a = 0.92f;
                accent = Color.Lerp(goal, Color.white, 0.65f);
                accent.a = 1f;
            }
            float blend = 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime);
            panel.color = Color.Lerp(panel.color, goal, blend);
            if (title != null) title.color = Color.Lerp(title.color, accent, blend);
            if (outline != null) {
                Color border = accent; border.a = 0.8f;
                outline.effectColor = Color.Lerp(outline.effectColor, border, blend);
            }
            if (surfaces != null) foreach (var image in surfaces) {
                if (image == null) continue;
                Color tint = Color.Lerp(goal, Color.black, image.name == "EstadoReconocimiento" ? 0.6f : 0.18f);
                tint.a = 0.94f;
                image.color = Color.Lerp(image.color, tint, blend);
            }
        }
        public static Color Palette(string modelName)
        {
            string hex;
            switch (modelName) {
                case "Sun": hex = "#A96513"; break;
                case "Mercury": hex = "#55535D"; break;
                case "Venus": hex = "#98632E"; break;
                case "Earth": hex = "#205F83"; break;
                case "Mars": hex = "#9E3E32"; break;
                case "Jupiter": hex = "#86513C"; break;
                case "Saturn": hex = "#89702D"; break;
                case "Uranus": hex = "#24787A"; break;
                case "Neptune": hex = "#344C98"; break;
                case "AR_Astronauta_Finn": hex = "#42732D"; break;
                case "AR_Astronauta_Rae": hex = "#9D4E2F"; break;
                case "AR_Mech_Barbara": hex = "#82701E"; break;
                case "AR_Alien_Volador": hex = "#73418B"; break;
                case "Space_Nave_Exploradora": hex = "#96602C"; break;
                case "Space_Nave_Carga": hex = "#875037"; break;
                case "Space_Rover": hex = "#8B702D"; break;
                case "Space_Antena": hex = "#426873"; break;
                case "Space_Meteorito": hex = "#985344"; break;
                case "AR_Robot_Auxiliar": hex = "#28776C"; break;

                default: hex = "#48424D"; break;
            }
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}
