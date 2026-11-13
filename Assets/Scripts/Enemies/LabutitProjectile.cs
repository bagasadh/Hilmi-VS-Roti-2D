using HilmiVSRoti.Player;
using UnityEngine;

namespace HilmiVSRoti.Enemies
{
    /// <summary>
    /// Proyektil lemparan tangan/kaki dari Labutit dengan lintasan melengkung (gravitasi).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class LabutitProjectile : MonoBehaviour
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private float lifeTime = 4f;
        [SerializeField] private float spinSpeed = 360f;

        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            Destroy(gameObject, lifeTime);
        }

        public void Launch(Vector2 velocity)
        {
            rb.linearVelocity = velocity;
        }

        private void Update()
        {
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                var health = other.GetComponent<PlayerHealth>();
                if (health != null)
                    health.TakeDamage(damage, transform.position);
                Destroy(gameObject);
            }
            else if (other.gameObject.layer == LayerMask.NameToLayer("Ground") && rb.linearVelocity.y <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}

