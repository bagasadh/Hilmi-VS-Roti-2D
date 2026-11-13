using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HilmiVSRoti.Player
{
    /// <summary>
    /// Mengatur nyawa Hilmi, efek kebal sementara (invincibility) saat kena hit,
    /// respawn di checkpoint, serta restart stage saat nyawa habis.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Pengaturan Nyawa")]
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private float invincibilityDuration = 1.2f;
        [SerializeField] private float blinkInterval = 0.1f;

        [Header("Knockback & Respawn")]
        [SerializeField] private float knockbackForceX = 5f;
        [SerializeField] private float knockbackForceY = 8f;
        [SerializeField] private Vector3 initialSpawnPoint = new Vector3(0f, 1.5f, 0f);
        [SerializeField] private float restartDelayOnDeath = 1.5f;

        private Rigidbody2D rb;
        private SpriteRenderer sr;
        private PlayerController controller;

        private int currentHealth;
        private Vector3 currentCheckpoint;
        private bool isInvincible;
        private bool isDead;

        // Untuk sistem perisai Roti Sobek (Tahap 6)
        public bool HasShield { get; set; }
        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;
        public bool IsInvincible => isInvincible;

        public event Action<int, int> OnHealthChanged;
        public event Action OnPlayerDied;
        public event Action OnShieldBroken;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sr = GetComponent<SpriteRenderer>();
            controller = GetComponent<PlayerController>();

            currentHealth = maxHealth;
            currentCheckpoint = initialSpawnPoint;
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void OnDestroy()
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
                Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, false);
        }

        public void SetCheckpoint(Vector3 checkpointPos)
        {
            currentCheckpoint = checkpointPos + Vector3.up * 0.5f;
        }

        /// <summary>
        /// Memberikan damage ke Hilmi. Jika jatuh ke jurang (isPitFall = true),
        /// Hilmi langsung respawn ke checkpoint terakhir selama nyawa masih ada.
        /// </summary>
        public void TakeDamage(int damage, Vector2 hitOrigin, bool isPitFall = false)
        {
            if (isDead)
                return;

            // Jika bukan jatuh ke jurang, cek invincibility dan perisai Roti Sobek
            if (!isPitFall)
            {
                if (isInvincible)
                    return;

                if (HasShield)
                {
                    HasShield = false;
                    OnShieldBroken?.Invoke();
                    ApplyKnockback(hitOrigin);
                    StartCoroutine(InvincibilityRoutine());
                    return;
                }
            }

            currentHealth = Mathf.Max(0, currentHealth - damage);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0)
            {
                Die();
                return;
            }

            if (isPitFall)
            {
                RespawnAtCheckpoint();
            }
            else
            {
                ApplyKnockback(hitOrigin);
                StartCoroutine(InvincibilityRoutine());
            }
        }

        private void RespawnAtCheckpoint()
        {
            transform.SetParent(null);
            rb.linearVelocity = Vector2.zero;
            transform.position = currentCheckpoint;
            StartCoroutine(InvincibilityRoutine());
        }

        private void ApplyKnockback(Vector2 hitOrigin)
        {
            float dirX = transform.position.x >= hitOrigin.x ? 1f : -1f;
            rb.linearVelocity = new Vector2(dirX * knockbackForceX, knockbackForceY);
        }

        private IEnumerator InvincibilityRoutine()
        {
            isInvincible = true;
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer != -1)
                Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, true);

            float elapsed = 0f;

            while (elapsed < invincibilityDuration)
            {
                sr.enabled = !sr.enabled;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            sr.enabled = true;
            if (enemyLayer != -1)
                Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, false);
            isInvincible = false;
        }

        private void Die()
        {
            isDead = true;
            if (controller != null)
                controller.enabled = false;

            rb.linearVelocity = new Vector2(0f, knockbackForceY);
            OnPlayerDied?.Invoke();
            StartCoroutine(RestartSceneRoutine());
        }

        private IEnumerator RestartSceneRoutine()
        {
            yield return new WaitForSeconds(restartDelayOnDeath);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

