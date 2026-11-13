using HilmiVSRoti.Player;
using UnityEngine;

namespace HilmiVSRoti.Items
{
    /// <summary>
    /// Satu script untuk semua jenis roti (Tawar, Keju, Cokelat, Sobek, Jamuran).
    /// Tipe efek dan durasi dapat dipilih langsung melalui Inspector.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BreadPickup : MonoBehaviour
    {
        [Header("Pengaturan Roti")]
        [SerializeField] private BreadType breadType = BreadType.RotiTawar;
        [SerializeField] private float duration = 6.5f;

        [Header("Animasi Mengambang")]
        [SerializeField] private float bobAmplitude = 0.12f;
        [SerializeField] private float bobSpeed = 3f;

        private Vector3 startPos;

        public static event System.Action<BreadType> OnAnyBreadPickedUp;

        private void Awake()
        {
            startPos = transform.position;
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void Update()
        {
            // Mengambang ke atas (0..bobAmplitude) agar tidak pernah menembus tanah/batu di bawahnya
            float offsetY = (Mathf.Sin(Time.time * bobSpeed) + 1f) * 0.5f * bobAmplitude;
            transform.position = startPos + new Vector3(0f, offsetY, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            var effects = other.GetComponent<PlayerBreadEffects>();
            if (effects != null)
            {
                effects.ApplyBread(breadType, duration);
                OnAnyBreadPickedUp?.Invoke(breadType);
                Destroy(gameObject);
            }
        }
    }
}

