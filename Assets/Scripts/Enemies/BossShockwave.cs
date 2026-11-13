using HilmiVSRoti.Player;
using UnityEngine;

namespace HilmiVSRoti.Enemies
{
    /// <summary>
    /// Gelombang kejut atau tebasan yang bergerak mendatar dan melukai Hilmi jika tersentuh.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossShockwave : MonoBehaviour
    {
        [SerializeField] private int damage = 1;
        [SerializeField] private float lifeTime = 1.6f;

        private Vector2 moveDir;
        private float speed;

        public void Initialize(Vector2 direction, float moveSpeed)
        {
            moveDir = direction.normalized;
            speed = moveSpeed;
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            transform.Translate(moveDir * (speed * Time.deltaTime), Space.World);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            var health = other.GetComponent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(damage, transform.position);

            Destroy(gameObject);
        }
    }
}

