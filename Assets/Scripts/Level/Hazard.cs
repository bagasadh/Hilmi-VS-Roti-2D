using HilmiVSRoti.Player;
using UnityEngine;

namespace HilmiVSRoti.Level
{
    /// <summary>
    /// Menangani rintangan berbahaya seperti duri (spike) atau jurang (death zone).
    /// Terhubung langsung ke PlayerHealth untuk mengurangi nyawa atau respawn di checkpoint.
    /// </summary>
    public class Hazard : MonoBehaviour
    {
        [Header("Pengaturan Bahaya")]
        [SerializeField] private int damage = 1;
        [SerializeField] private bool instantKill = false;
        [SerializeField] private Vector3 fallbackRespawnPos = new Vector3(0f, 2f, 0f);

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandlePlayerHit(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandlePlayerHit(collision.gameObject);
        }

        private void HandlePlayerHit(GameObject target)
        {
            if (!target.CompareTag("Player"))
                return;

            var health = target.GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(damage, transform.position, isPitFall: instantKill);
                return;
            }

            // Fallback jika PlayerHealth belum terpasang
            var rb = target.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            target.transform.SetParent(null);
            target.transform.position = fallbackRespawnPos;
        }
    }
}

