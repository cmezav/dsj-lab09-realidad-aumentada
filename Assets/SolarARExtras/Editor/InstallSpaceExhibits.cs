using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SolarARExtras.Editor
{
    public static class InstallSpaceExhibits
    {
        private const string Root = "Assets/SolarARExtras";
        private static readonly string[] Models = {
            "Astronaut_FinnTheFrog", "Astronaut_RaeTheRedPanda",
            "Mech_BarbaraTheBee", "Enemy_Flying"
        };
        private static readonly string[] Labels = {
            "AR_Astronauta_Finn", "AR_Astronauta_Rae",
            "AR_Mech_Barbara", "AR_Alien_Volador"
        };
        private static readonly string[] Defaults = { "Wave", "Idle", "Dance", "Flying_Idle" };

        [MenuItem("Tools/Solar AR/Agregar 4 modelos animados al ImageTarget")]
        private static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) {
                EditorUtility.DisplayDialog("Solar AR", "Sal de Play antes de agregar los modelos.", "Aceptar");
                return;
            }
            GameObject target = Selection.activeGameObject;
            if (target == null || target.GetComponent("ImageTargetBehaviour") == null) {
                EditorUtility.DisplayDialog("Solar AR", "Selecciona el ImageTarget en la Hierarchy y vuelve a ejecutar este menu.", "Aceptar");
                return;
            }
            try {
                EnsureFolder(Root + "/Generated");
                Material material = PrepareMaterial();
                var prefabs = new List<GameObject>();
                var controllers = new List<AnimatorController>();
                for (int i = 0; i < Models.Length; i++) {
                    string path = Root + "/Models/" + Models[i] + ".fbx";
                    PrepareImport(path);
                    var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (model == null) throw new InvalidOperationException("Falta el modelo: " + path);
                    var clips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                        .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
                        .OrderBy(c => c.name).ToArray();
                    if (clips.Length == 0) throw new InvalidOperationException("No se importaron animaciones en " + Models[i]);
                    prefabs.Add(model);
                    controllers.Add(PrepareController(Models[i], Defaults[i], clips));
                }
                Undo.IncrementCurrentGroup();
                int undoGroup = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("Agregar exposiciones espaciales AR");
                int count = 0;
                for (int i = 0; i < Models.Length; i++) {
                    if (target.transform.Find(Labels[i]) != null) continue;
                    var exhibit = new GameObject(Labels[i]);
                    Undo.RegisterCreatedObjectUndo(exhibit, "Crear modelo AR");
                    exhibit.transform.SetParent(target.transform, false);
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[i], exhibit.transform);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;
                    model.transform.localScale = Vector3.one;
                    foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true)) {
                        var materials = renderer.sharedMaterials;
                        for (int slot = 0; slot < materials.Length; slot++) materials[slot] = material;
                        renderer.sharedMaterials = materials;
                    }
                    Animator animator = model.GetComponent<Animator>();
                    if (animator == null) animator = model.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controllers[i];
                    animator.applyRootMotion = false;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length > 0) {
                        Bounds bounds = renderers[0].bounds;
                        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                        float dimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                        if (dimension > 0.00001f) {
                            float factor = 0.12f / dimension;
                            Vector3 center = model.transform.InverseTransformPoint(bounds.center);
                            Vector3 bottom = model.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.min.y, bounds.center.z));
                            model.transform.localScale = Vector3.one * factor;
                            model.transform.localPosition = new Vector3(-center.x * factor, -bottom.y * factor, -center.z * factor);
                        }
                    }
                    exhibit.transform.localPosition = new Vector3(0, i == 3 ? 0.055f : 0.005f, 0);
                    // El selector existente activa solo el modelo elegido al iniciar.
                    exhibit.SetActive(false);
                    count++;
                }
                Undo.CollapseUndoOperations(undoGroup);
                EditorSceneManager.MarkSceneDirty(target.scene);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("Solar AR", "Agregados: " + count + ". Los modelos quedan inactivos hasta seleccionarlos con Previo/Siguiente. Guarda la escena con Ctrl+S y prueba. Si ya existian, no se duplicaron.", "Aceptar");
            }
            catch (Exception exception) {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Solar AR", "No se completo la instalacion. Revisa Console: " + exception.Message, "Aceptar");
            }
        }

        private static void PrepareImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null) throw new InvalidOperationException("No se encontro: " + path);
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.SaveAndReimport();
            var clips = importer.clipAnimations;
            if (clips.Length == 0) clips = importer.defaultClipAnimations;
            foreach (var clip in clips) {
                string name = ShortName(clip.name);
                clip.loopTime = name == "Idle" || name == "Wave" || name == "Dance" || name == "Flying_Idle";
            }
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimatorController PrepareController(string name, string preferred, AnimationClip[] clips)
        {
            string path = Root + "/Generated/" + name + ".controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null) return existing;
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            AnimatorState chosen = null;
            for (int i = 0; i < clips.Length; i++) {
                string stateName = ShortName(clips[i].name);
                var state = machine.AddState(stateName, new Vector3(300 + (i % 4) * 230, 100 + (i / 4) * 80));
                state.motion = clips[i];
                if (stateName == preferred) chosen = state;
            }
            if (chosen == null) throw new InvalidOperationException("No existe el clip " + preferred + " en " + name);
            machine.defaultState = chosen;
            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static string ShortName(string name)
        {
            int separator = name.LastIndexOf('|');
            return separator >= 0 ? name.Substring(separator + 1) : name;
        }

        private static Material PrepareMaterial()
        {
            string path = Root + "/Generated/SpaceAtlas.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Se requiere el pipeline 3D Built-In (shader Standard).");
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/Atlas.png");
            if (atlas == null) throw new InvalidOperationException("Falta Textures/Atlas.png");
            material = new Material(shader);
            material.name = "SpaceAtlas";
            material.mainTexture = atlas;
            material.color = Color.white;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.2f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            string parent = path.Substring(0, split);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(split + 1));
        }
    }
}
