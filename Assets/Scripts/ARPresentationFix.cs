using UnityEngine;

public class ARPresentationFix : MonoBehaviour
{
    [Header("Personajes")]
    public bool faceCamera = true;
    public float characterHeight = 0.055f;
    public float characterYaw = 0f;

    private Transform arCamera;

    private readonly string[] planetNames =
    {
        "Sun", "Mercury", "Venus", "Earth", "Mars",
        "Jupiter", "Saturn", "Uranus", "Neptune"
    };

    private void Start()
    {
        if (Camera.main != null)
            arCamera = Camera.main.transform;

        // Coloca todos los planetas en el mismo punto del marcador.
        foreach (string planetName in planetNames)
        {
            Transform planet = transform.Find(planetName);

            if (planet != null)
                planet.localPosition = new Vector3(0f, 0.055f, 0f);
        }

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("AR_"))
            {
                child.localPosition =
                    new Vector3(0f, characterHeight, 0f);
            }
        }
    }

    private void LateUpdate()
    {
        if (!faceCamera)
            return;

        if (arCamera == null)
        {
            if (Camera.main == null)
                return;

            arCamera = Camera.main.transform;
        }

        foreach (Transform child in transform)
        {
            if (!child.name.StartsWith("AR_") ||
                !child.gameObject.activeInHierarchy)
                continue;

            Vector3 direction = arCamera.position - child.position;

            if (direction.sqrMagnitude < 0.000001f)
                continue;

            child.rotation =
                Quaternion.LookRotation(direction, arCamera.up) *
                Quaternion.Euler(0f, characterYaw, 0f);
        }
    }
}