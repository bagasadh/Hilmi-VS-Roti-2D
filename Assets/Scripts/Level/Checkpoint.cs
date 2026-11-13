using HilmiVSRoti.Player;
using UnityEngine;

namespace HilmiVSRoti.Level
{
    /// <summary>
    /// Titik checkpoint di stage. Saat disentuh Hilmi, warna bendera berubah
    /// dan posisi respawn diperbarui.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] private Color inactiveColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField] private Color activeColor = new Color(0.2f, 0.9f, 0.35f, 1f);

        private SpriteRenderer sr;
        private bool isActivated;

        public static event System.Action OnAnyCheckpointActivated;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = inactiveColor;

            var col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isActivated || !other.CompareTag("Player"))
                return;

            var health = other.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.SetCheckpoint(transform.position);
                isActivated = true;
                if (sr != null)
                    sr.color = activeColor;
                OnAnyCheckpointActivated?.Invoke();
                Debug.Log($"[Checkpoint] Aktif di {gameObject.name}");
            }
        }
    }
}

