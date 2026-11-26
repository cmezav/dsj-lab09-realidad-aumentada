using UnityEngine;

namespace SolarARSpaceExpansion
{
    public sealed class SpaceExhibitMotion : MonoBehaviour
    {
        public int motionType;
        private float elapsed;
        private void OnEnable() { elapsed = 0f; }
        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed;
            switch (motionType) {
                case 0:
                    transform.localPosition = new Vector3(0f, Mathf.Sin(t * 1.8f) * 0.004f, 0f);
                    transform.localRotation = Quaternion.Euler(0f, t * 12f, Mathf.Sin(t) * 5f);
                    break;
                case 1:
                    transform.localPosition = new Vector3(0f, Mathf.Sin(t * 1.2f) * 0.003f, 0f);
                    transform.localRotation = Quaternion.Euler(0f, t * 8f, 0f);
                    break;
                case 2:
                    // Recorrido eliptico visible (13 x 8 cm a escala normal), no giro en el sitio.
                    float angle = t * 0.65f;
                    transform.localPosition = new Vector3(Mathf.Sin(angle) * 0.065f, 0f, Mathf.Cos(angle) * 0.04f);
                    Vector3 tangent = new Vector3(Mathf.Cos(angle) * 0.065f, 0f, -Mathf.Sin(angle) * 0.04f);
                    transform.localRotation = Quaternion.LookRotation(tangent, Vector3.up);
                    break;
                case 3:
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.65f) * 45f, 0f);
                    break;
                case 4:
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.Euler(t * 12f, t * 20f, t * 8f);
                    break;
            }
        }
    }
}
