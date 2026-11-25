using UnityEngine;

public class PlanetRotation : MonoBehaviour
{
    public float degreesPerSecond = 30f;

    private void Update()
    {
        transform.Rotate(
            0f,
            degreesPerSecond * Time.deltaTime,
            0f,
            Space.Self
        );
    }
}