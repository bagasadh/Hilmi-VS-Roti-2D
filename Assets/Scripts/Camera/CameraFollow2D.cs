using UnityEngine;

namespace HilmiVSRoti.CameraSystem
{
    /// <summary>
    /// Mengatur kamera 2D ala Mario: mengikuti posisi X secara halus,
    /// menjaga tanah/jurang tetap terlihat saat melompat (vertical deadzone),
    /// dan memperluas jarak pandang kamera.
    /// </summary>
    public class CameraFollow2D : MonoBehaviour
    {
        [Header("Target & Posisi")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(2.5f, 0.5f, -10f);
        [SerializeField] private float smoothTimeX = 0.12f;
        [SerializeField] private float smoothTimeY = 0.25f;
        [SerializeField] private float orthographicSize = 6.5f;

        [Header("Kamera Vertikal ala Mario")]
        [SerializeField] private float baseCameraY = 1.6f;
        [SerializeField] private float verticalDeadzone = 4.0f;

        [Header("Batas Kamera (Clamp)")]
        [SerializeField] private bool useBounds = true;
        [SerializeField] private float minX = 0f;
        [SerializeField] private float maxX = 145f;
        [SerializeField] private float minY = 1.6f;
        [SerializeField] private float maxY = 10f;

        private float velX;
        private float velY;
        private Camera cam;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam != null && cam.orthographic)
                cam.orthographicSize = orthographicSize;
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            float desiredX = target.position.x + offset.x;

            // Saat lompat biasa, kamera tetap di baseCameraY agar tanah, duri, dan jurang selalu kelihatan
            float desiredY = baseCameraY;
            if (target.position.y > baseCameraY + verticalDeadzone)
                desiredY = target.position.y - verticalDeadzone + offset.y;

            if (useBounds)
            {
                desiredX = Mathf.Clamp(desiredX, minX, maxX);
                desiredY = Mathf.Clamp(desiredY, minY, maxY);
            }

            float newX = Mathf.SmoothDamp(transform.position.x, desiredX, ref velX, smoothTimeX);
            float newY = Mathf.SmoothDamp(transform.position.y, desiredY, ref velY, smoothTimeY);
            transform.position = new Vector3(newX, newY, offset.z);
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
