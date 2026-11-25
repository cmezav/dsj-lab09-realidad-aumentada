using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(10000)]
public class ARPresentationFix : MonoBehaviour
{
    [Header("Presentacion sobre el marcador")]
    public bool faceCamera = true;
    public float characterHeight = 0.055f;
    public float characterYaw = 180f;
    public float planetHeight = 0.055f;
    private Transform arCamera;
    private Transform[] models;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsurePresenters() { AttachMissing(); }
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) { AttachMissing(); }
    private static void AttachMissing()
    {
        foreach (var selector in Object.FindObjectsByType<MultiTargetVuforia>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (selector.GetComponent<ARPresentationFix>() == null)
                selector.gameObject.AddComponent<ARPresentationFix>();
    }
    private void Awake() { RefreshModels(); }
    private void Start() { RefreshModels(); }
    private void RefreshModels()
    {
        models = new Transform[transform.childCount];
        for (int i = 0; i < models.Length; i++) {
            models[i] = transform.GetChild(i);
            Center(models[i]);
        }
    }
    public static bool IsPlanet(string modelName)
    {
        switch (modelName) {
            case "Sun": case "Mercury": case "Venus": case "Earth":
            case "Mars": case "Jupiter": case "Saturn": case "Uranus": case "Neptune": return true;
            default: return false;
        }
    }
    private void Center(Transform model)
    {
        if (IsPlanet(model.name)) model.localPosition = new Vector3(0f, planetHeight, 0f);
        else if (model.name.StartsWith("AR_")) model.localPosition = new Vector3(0f, characterHeight, 0f);
    }
    private void LateUpdate()
    {
        if (models == null || models.Length != transform.childCount) RefreshModels();
        if (arCamera == null && Camera.main != null) arCamera = Camera.main.transform;
        foreach (Transform model in models) {
            if (model == null || !model.gameObject.activeInHierarchy) continue;
            Center(model);
            if (!faceCamera || arCamera == null || !model.name.StartsWith("AR_")) continue;
            Vector3 direction = arCamera.position - model.position;
            if (direction.sqrMagnitude < 0.000001f) continue;
            // Solo se orienta el contenedor; los huesos y el Animator quedan intactos.
            model.rotation = Quaternion.LookRotation(direction, arCamera.up)
                * Quaternion.Euler(0f, characterYaw, 0f);
        }
    }
    public void FlipCharacterFacing() { characterYaw = Mathf.Repeat(characterYaw + 180f, 360f); }
}
