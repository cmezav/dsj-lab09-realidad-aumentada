using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SolarARSpaceExpansion.Editor
{
    [InitializeOnLoad]
    public static class SpaceExpansionInstaller
    {
        private const string Root = "Assets/SolarARSpaceExpansion";
        private const string Request = Root + "/ApplyExpansion.request.txt";
        private static readonly string[] Models = { "craft_speederA", "craft_cargoA", "rover", "satelliteDish_detailed", "meteor_detailed", "Robot" };
        private static readonly string[] Names = { "Space_Nave_Exploradora", "Space_Nave_Carga", "Space_Rover", "Space_Antena", "Space_Meteorito", "AR_Robot_Auxiliar" };
        static SpaceExpansionInstaller() { EditorApplication.update += AutoApply; }
        private static MultiTargetVuforia FindTarget()
        {
            foreach (var selector in UnityEngine.Object.FindObjectsByType<MultiTargetVuforia>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (selector.gameObject.scene.IsValid()) return selector;
            return null;
        }
        private static void AutoApply()
        {
            if (!File.Exists(Request)) { EditorApplication.update -= AutoApply; return; }
            if (EditorApplication.isUpdating || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (FindTarget() == null) return;
            EditorApplication.update -= AutoApply; Apply();
        }
        [MenuItem("Tools/Solar AR/Agregar seis assets espaciales nuevos")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var target = FindTarget();
            if (target == null) { EditorUtility.DisplayDialog("Solar AR", "Abre la escena SolarAR.", "Aceptar"); return; }
            try {
                if (Shader.Find("Standard") == null) throw new InvalidOperationException("Este instalador utiliza el shader Standard del pipeline Built-In.");
                if (!AssetDatabase.IsValidFolder(Root + "/Generated")) AssetDatabase.CreateFolder(Root, "Generated");
                for (int i = 0; i < Models.Length; i++) {
                    string path = Root + "/Models/" + Models[i] + ".fbx";
                    if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) throw new InvalidOperationException("Falta " + path);
                }
                var robotController = RobotController();
                int added = 0;
                for (int i = 0; i < Models.Length; i++) {
                    if (target.transform.Find(Names[i]) != null) continue;
                    var wrapper = new GameObject(Names[i]);
                    Undo.RegisterCreatedObjectUndo(wrapper, "Agregar asset espacial");
                    wrapper.transform.SetParent(target.transform, false);
                    wrapper.transform.localPosition = new Vector3(0f, 0.055f, 0f);
                    var pivot = new GameObject("Movimiento");
                    pivot.transform.SetParent(wrapper.transform, false);
                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/" + Models[i] + ".fbx");
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(source, pivot.transform);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;
                    model.transform.localScale = Vector3.one;
                    foreach (var renderer in model.GetComponentsInChildren<Renderer>(true)) {
                        var slots = renderer.sharedMaterials;
                        for (int j = 0; j < slots.Length; j++) {
                            string materialName = slots[j] != null ? slots[j].name : "metal";
                            slots[j] = MaterialFor(materialName);
                        }
                        renderer.sharedMaterials = slots;
                    }
                    if (i == 5) {
                        var animator = model.GetComponent<Animator>();
                        if (animator == null) animator = model.AddComponent<Animator>();
                        animator.runtimeAnimatorController = robotController;
                        animator.applyRootMotion = false;
                        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    } else {
                        pivot.AddComponent<SpaceExhibitMotion>().motionType = i;
                    }
                    var renderers = model.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length > 0) {
                        Bounds bounds = renderers[0].bounds;
                        foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                        Vector3 center = model.transform.InverseTransformPoint(bounds.center);
                        float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                        if (longest > 0.00001f) {
                            float factor = 0.12f / longest;
                            model.transform.localScale = Vector3.one * factor;
                            model.transform.localPosition = -center * factor;
                        }
                    }
                    wrapper.SetActive(false);
                    added++;
                }
                EditorSceneManager.MarkSceneDirty(target.gameObject.scene);
                AssetDatabase.SaveAssets();
                if (!EditorSceneManager.SaveScene(target.gameObject.scene)) throw new IOException("No se pudo guardar SolarAR.");
                if (File.Exists(Request)) AssetDatabase.DeleteAsset(Request);
                EditorUtility.DisplayDialog("Solar AR listo", "Agregados: " + added + ". Guarda cualquier otro cambio y pulsa Play. Recorre las ultimas seis entradas con los botones o las flechas. El robot saluda; los otros modelos se animan mediante codigo.", "Aceptar");
            }
            catch (Exception error) { Debug.LogException(error); EditorUtility.DisplayDialog("Solar AR", error.Message, "Aceptar"); }
        }
        private static AnimatorController RobotController()
        {
            const string modelPath = Root + "/Models/Robot.fbx";
            const string controllerPath = Root + "/Generated/RobotAuxiliar.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller != null) return controller;
            var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("Falta el importador del robot.");
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
            var clipsConfig = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            foreach (var clip in clipsConfig) clip.loopTime = clip.name.EndsWith("Robot_Wave", StringComparison.Ordinal) || clip.name.EndsWith("Robot_Idle", StringComparison.Ordinal) || clip.name.EndsWith("Robot_Dance", StringComparison.Ordinal);
            importer.clipAnimations = clipsConfig;
            importer.SaveAndReimport();
            var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            if (!clips.Any(c => c.name.EndsWith("Robot_Wave", StringComparison.Ordinal))) throw new InvalidOperationException("No se importo el clip Robot_Wave del robot.");
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            for (int i = 0; i < clips.Length; i++) {
                string name = clips[i].name.Substring(clips[i].name.LastIndexOf('|') + 1);
                var state = machine.AddState(name, new Vector3(250f + i % 4 * 220f, 80f + i / 4 * 80f));
                state.motion = clips[i];
                if (name == "Robot_Wave") machine.defaultState = state;
            }
            EditorUtility.SetDirty(controller);
            return controller;
        }
        private static Material MaterialFor(string originalName)
        {
            string name = originalName.Replace(" (Instance)", "").Replace(".", "_");
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
            string path = Root + "/Generated/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var material = new Material(Shader.Find("Standard"));
            material.name = name;
            switch (name) {
                case "metal": material.color = new Color(0.8431373f, 0.8705882f, 0.9098039f); break;
                case "metalRed": material.color = new Color(1f, 0.6285242f, 0.2028302f); break;
                case "metalDark": material.color = new Color(0.6750623f, 0.7100219f, 0.7735849f); break;
                case "dark": material.color = new Color(0.2745098f, 0.2980392f, 0.3411765f); break;
                case "rockTrack": material.color = new Color(0.9811321f, 0.5809683f, 0.4489142f); break;
                case "Main": material.color = new Color(0.15f, 0.8f, 0.69f); break;
                case "Grey": material.color = new Color(0.6f, 0.65f, 0.7f); break;
                case "Black": material.color = new Color(0.055f, 0.09f, 0.12f); break;
                default: material.color = Color.white; break;
            }
            material.SetFloat("_Metallic", 0.1f);
            material.SetFloat("_Glossiness", 0.25f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
    }
}
