using HilmiVSRoti.Player;
using UnityEngine;

namespace HilmiVSRoti.Enemies
{
    /// <summary>
    /// Base class untuk semua musuh (jek, labutit, KenyGK).
    /// Menangani HP, deteksi injakan dari atas (stomp), tabrakan mode Roti Keju, dan damage ke Hilmi.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public abstract class Enemy : MonoBehaviour
    {
        [Header("Stat Dasar Musuh")]
        [SerializeField] protected int maxHp = 1;
        [SerializeField] protected int contactDamage = 1;
        [SerializeField] protected float stompBounceForce = 11f;
        [SerializeField] protected float detectionRange = 8f;

        protected int currentHp;
        protected Rigidbody2D rb;
        protected SpriteRenderer sr;
        protected Animator anim;
        protected Transform player;
        protected bool isDead;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
            anim = GetComponent<Animator>();
            currentHp = maxHp;

            // Pastikan sesama musuh tidak saling bertabrakan/mendorong
            int enemyLayer = gameObject.layer;
            if (enemyLayer != 0)
                Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
        }

        protected virtual void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        protected virtual void Update()
        {
            if (isDead)
                return;

            UpdateBehavior();
        }

        /// <summary>
        /// Di-override oleh masing-masing musuh untuk logika AI-nya.
        /// </summary>
        protected abstract void UpdateBehavior();

        public virtual void TakeDamage(int amount)
        {
            if (isDead)
                return;

            currentHp -= amount;
            if (currentHp <= 0)
                Die();
        }

        protected virtual void Die()
        {
            isDead = true;
            Destroy(gameObject);
        }

        protected void FaceDirection(float dirX)
        {
            if (Mathf.Abs(dirX) < 0.01f)
                return;

            // Semua sprite default menghadap kanan; flipX = true jika menghadap kiri
            sr.flipX = dirX < 0f;
        }

        protected float DistanceToPlayer()
        {
            if (player == null)
                return float.MaxValue;
            return Vector2.Distance(transform.position, player.position);
        }

        protected virtual void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDead || !collision.gameObject.CompareTag("Player"))
                return;

            var playerController = collision.gameObject.GetComponent<PlayerController>();
            var playerHealth = collision.gameObject.GetComponent<PlayerHealth>();

            // 1. Jika Hilmi sedang dalam mode tabrak (Roti Keju), musuh langsung kalah
            if (playerController != null && playerController.IsRamModeActive)
            {
                Die();
                return;
            }

            // 2. Cek apakah Hilmi menginjak musuh dari atas
            bool stompedFromAbove = false;
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.45f)
                {
                    stompedFromAbove = true;
                    break;
                }
            }

            if (stompedFromAbove)
            {
                if (playerController != null)
                    playerController.Bounce(stompBounceForce);
                TakeDamage(1);
                return;
            }

            // 3. Jika kena dari samping/bawah, Hilmi menerima damage
            if (playerHealth != null)
                playerHealth.TakeDamage(contactDamage, transform.position);
        }
    }
}

