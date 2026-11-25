using UnityEngine;

public class PlanetRotation : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 15f;

    private void Update()
    {
        transform.Rotate(
            Vector3.up,
            degreesPerSecond * Time.deltaTime,
            Space.Self
        );
    }
}