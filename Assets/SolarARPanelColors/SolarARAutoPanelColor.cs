using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SolarARPanelColors
{
    // Paleta definida por modelo. No analiza la imagen de la webcam ni lee texturas.
    // Se instala al iniciar Play o cargar una escena, sin editar la escena guardada.
    public sealed class SolarARAutoPanelColor : MonoBehaviour
    {
        private ARModelInfo info;
        private Image panel;
        private Transform previous;
        private Color goal;
        private float panelAlpha;
        private bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() { Attach(); }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { Attach(); }

        private static void Attach()
        {
            var panels = Object.FindObjectsByType<ARModelInfo>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var source in panels)
            {
                Image background = source.GetComponent<Image>();
                if (background == null) background = source.GetComponentInParent<Image>();
                if (background == null) continue;
                var effect = source.GetComponent<SolarARAutoPanelColor>();
                if (effect == null) effect = source.gameObject.AddComponent<SolarARAutoPanelColor>();
                effect.Configure(source, background);
            }
        }

        private void Configure(ARModelInfo source, Image background)
        {
            if (initialized) return;
            info = source;
            panel = background;
            panelAlpha = background.color.a;
            if (panelAlpha < 0.05f) panelAlpha = 0.82f;
            goal = background.color;
            goal.a = panelAlpha;
            initialized = true;
        }

        private void LateUpdate()
        {
            if (!initialized || info == null || info.imageTarget == null || panel == null) return;
            Transform selected = null;
            for (int i = 0; i < info.imageTarget.childCount; i++)
            {
                Transform child = info.imageTarget.GetChild(i);
                if (child.gameObject.activeSelf) { selected = child; break; }
            }
            if (selected != null && selected != previous)
            {
                previous = selected;
                goal = Palette(selected.name);
                goal.a = panelAlpha;
            }
            // Transicion breve, independiente de la pausa de las animaciones.
            float blend = 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
            if (((Vector4)panel.color - (Vector4)goal).sqrMagnitude > 0.000001f)
                panel.color = Color.Lerp(panel.color, goal, blend);
        }

        private static Color Palette(string modelName)
        {
            string hex;
            switch (modelName)
            {
                case "Sun": hex = "#805019"; break;
                case "Mercury": hex = "#494752"; break;
                case "Venus": hex = "#74532E"; break;
                case "Earth": hex = "#174F74"; break;
                case "Mars": hex = "#843B2E"; break;
                case "Jupiter": hex = "#75503C"; break;
                case "Saturn": hex = "#76622F"; break;
                case "Uranus": hex = "#206672"; break;
                case "Neptune": hex = "#293D86"; break;
                case "AR_Astronauta_Finn": hex = "#3B652D"; break;
                case "AR_Astronauta_Rae": hex = "#82452F"; break;
                case "AR_Mech_Barbara": hex = "#746018"; break;
                case "AR_Alien_Volador": hex = "#5D397E"; break;
                default: hex = "#293E5A"; break;
            }
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return color;
        }
    }
}