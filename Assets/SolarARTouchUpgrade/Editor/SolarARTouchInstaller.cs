using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace SolarARTouchUpgrade.Editor
{
    [InitializeOnLoad]
    public static class SolarARTouchInstaller
    {
        private const string Root = "Assets/SolarARTouchUpgrade";
        private const string Request = Root + "/Apply.request.txt";
        static SolarARTouchInstaller() { EditorApplication.update += AutoApply; }

        private static MultiTargetVuforia FindTarget()
        {
            return UnityEngine.Object.FindObjectsByType<MultiTargetVuforia>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.gameObject.scene.IsValid() && t.gameObject.scene.isLoaded);
        }
        private static void AutoApply()
        {
            if (!File.Exists(Request)) { EditorApplication.update -= AutoApply; return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (FindTarget() == null) return;
            EditorApplication.update -= AutoApply;
            Apply();
        }

        [MenuItem("Tools/Solar AR/Aplicar Saturno recorrido y rotacion tactil")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var selector = FindTarget();
            if (selector == null) { EditorUtility.DisplayDialog("Solar AR", "Abre la escena SolarAR y vuelve a ejecutar este menu.", "Aceptar"); return; }
            try {
                var scene = selector.gameObject.scene;
                string backup = Root + "/Backups";
                Directory.CreateDirectory(backup);
                if (!string.IsNullOrEmpty(scene.path)) File.Copy(scene.path, backup + "/SolarAR_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".unity.bak", false);
                Undo.IncrementCurrentGroup();
                int undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Mejoras tactiles Solar AR");
                var serialized = new SerializedObject(selector);
                var start = serialized.FindProperty("startModel");
                if (start == null) throw new InvalidOperationException("No se encontro Start Model en MultiTargetVuforia.");
                var initial = start.objectReferenceValue as GameObject;
                Transform[] originals = selector.transform.Cast<Transform>().ToArray();
                foreach (Transform original in originals) {
                    if (original.GetComponent<SolarARModelPivot>() != null) continue;
                    // Desconecta solo la instancia, nunca modifica el prefab fuente.
                    if (PrefabUtility.IsPartOfPrefabInstance(original.gameObject)) {
                        var instance = PrefabUtility.GetOutermostPrefabInstanceRoot(original.gameObject);
                        if (instance == original.gameObject)
                            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    }
                    string name = original.name;
                    int index = original.GetSiblingIndex();
                    bool active = original.gameObject.activeSelf;
                    Vector3 position = original.localPosition;
                    Vector3 scale = original.localScale;
                    Quaternion rotation = original.localRotation;
                    var wrapper = new GameObject(name);
                    Undo.RegisterCreatedObjectUndo(wrapper, "Crear contenedor tactil");
                    wrapper.transform.SetParent(selector.transform, false);
                    wrapper.transform.SetSiblingIndex(index);
                    wrapper.transform.localPosition = position;
                    wrapper.transform.localScale = scale;
                    wrapper.transform.localRotation = Quaternion.identity;
                    Undo.RecordObject(original.gameObject, "Renombrar contenido");
                    Undo.SetTransformParent(original, wrapper.transform, "Conservar modelo");
                    Undo.RecordObject(original, "Conservar transformacion");
                    original.name = "Contenido";
                    original.localPosition = Vector3.zero;
                    original.localScale = Vector3.one;
                    original.localRotation = (name == "Saturn" || name.StartsWith("AR_")) ? Quaternion.identity : rotation;
                    original.gameObject.SetActive(true);
                    wrapper.SetActive(active);
                    Undo.AddComponent<SolarARModelPivot>(wrapper).content = original;
                    if (initial == original.gameObject) {
                        start.objectReferenceValue = wrapper;
                        serialized.ApplyModifiedProperties();
                    }
                }
                var drag = selector.GetComponent<SolarARDragRotation>();
                if (drag == null) drag = Undo.AddComponent<SolarARDragRotation>(selector.gameObject);
                var saturn = selector.transform.Find("Saturn");
                if (saturn != null) {
                    var content = saturn.GetComponent<SolarARModelPivot>().content;
                    content.localRotation = Quaternion.identity;
                    // Vista previa oblicua. En Play se orienta respecto a ARCamera.
                    saturn.localRotation = Quaternion.Euler(25f, 0f, -12f);
                    EditorUtility.SetDirty(saturn);
                }
                var rover = selector.transform.Find("Space_Rover");
                if (rover != null) AddRoverRoute(rover);
                foreach (var controls in UnityEngine.Object.FindObjectsByType<SolarARControls.SolarARExperienceControls>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                    if (controls.imageTarget != selector.transform || controls.resetButton == null) continue;
                    Button button = controls.resetButton;
                    bool exists = false;
                    for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                        if (button.onClick.GetPersistentTarget(i) == drag && button.onClick.GetPersistentMethodName(i) == nameof(SolarARDragRotation.ResetCurrentRotation)) exists = true;
                    if (!exists) {
                        Undo.RecordObject(button, "Restablecer tambien orientacion");
                        UnityEventTools.AddPersistentListener(button.onClick, drag.ResetCurrentRotation);
                        EditorUtility.SetDirty(button);
                    }
                }
                EditorUtility.SetDirty(selector);
                Undo.CollapseUndoOperations(undoGroup);
                EditorSceneManager.MarkSceneDirty(scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("No se pudo guardar la escena.");
                if (File.Exists(Request)) AssetDatabase.DeleteAsset(Request);
                EditorUtility.DisplayDialog("Solar AR listo", "Saturno inclinado, rover con recorrido visible y giro con un dedo. En el editor arrastra con el boton izquierdo fuera de los paneles. Restablecer recupera tamano y orientacion. Prueba antes de actualizar el APK.", "Aceptar");
            } catch (Exception error) {
                Debug.LogException(error);
                EditorUtility.DisplayDialog("Solar AR", error.Message + "\nLa copia de la escena esta en Assets/SolarARTouchUpgrade/Backups.", "Aceptar");
            }
        }

        private static void AddRoverRoute(Transform rover)
        {
            Transform route = rover.Find("Ruta_Rover");
            if (route != null) return;
            const string path = Root + "/RutaRover.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) throw new InvalidOperationException("No se encontro Sprites/Default para la ruta.");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            var obj = new GameObject("Ruta_Rover");
            Undo.RegisterCreatedObjectUndo(obj, "Dibujar recorrido rover");
            obj.transform.SetParent(rover, false);
            var line = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 96;
            line.widthMultiplier = 0.0012f;
            line.sharedMaterial = material;
            line.startColor = line.endColor = new Color(0.35f, 0.95f, 0.95f, 0.6f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            for (int i = 0; i < 96; i++) {
                float a = i * Mathf.PI * 2f / 96f;
                line.SetPosition(i, new Vector3(Mathf.Sin(a) * 0.065f, -0.025f, Mathf.Cos(a) * 0.04f));
            }
        }
    }
}
