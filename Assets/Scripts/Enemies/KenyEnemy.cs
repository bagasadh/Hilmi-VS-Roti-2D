using HilmiVSRoti.Level;
using UnityEngine;

namespace HilmiVSRoti.Enemies
{
    /// <summary>
    /// Musuh 3: KenyGK (Keni).
    /// Menyamar sebagai kotak/batu, lalu tiba-tiba muncul dan mengejar saat Hilmi mendekat.
    /// </summary>
    public class KenyEnemy : Enemy
    {
        [Header("Penyamaran & Kejar (KenyGK)")]
        [SerializeField] private Sprite disguiseSprite;
        [SerializeField] private Sprite revealedSprite;
        [SerializeField] private Material disguisedMaterial;
        [SerializeField] private Material revealedMaterial;
        [SerializeField] private float revealDistance = 4f;
        [SerializeField] private float revealHopForce = 6f;
        [SerializeField] private float chaseSpeed = 3.2f;

        [Header("Deteksi Jurang & Duri")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float edgeCheckOffsetX = 0.55f;
        [SerializeField] private float edgeCheckDistance = 1.6f;

        private bool isDisguised = true;

        protected override void Awake()
        {
            base.Awake();
            if (groundLayer == 0)
                groundLayer = LayerMask.GetMask("Ground");
            EnterDisguise();
        }

        private void EnterDisguise()
        {
            isDisguised = true;
            if (anim != null)
                anim.enabled = false;
            if (sr != null && disguiseSprite != null)
                sr.sprite = disguiseSprite;
            if (sr != null && disguisedMaterial != null)
                sr.sharedMaterial = disguisedMaterial;
        }

        private void Reveal()
        {
            if (!isDisguised)
                return;

            isDisguised = false;
            if (sr != null && revealedSprite != null)
                sr.sprite = revealedSprite;
            if (sr != null && revealedMaterial != null)
                sr.sharedMaterial = revealedMaterial;
            if (anim != null)
                anim.enabled = true;

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, revealHopForce);
        }

        protected override void UpdateBehavior()
        {
            if (player == null)
                return;

            if (isDisguised)
            {
                if (DistanceToPlayer() <= revealDistance)
                    Reveal();
                return;
            }

            // Setelah muncul, kejar Hilmi jika masih dalam jarak deteksi
            float dx = player.position.x - transform.position.x;
            FaceDirection(dx);
        }

        private void FixedUpdate()
        {
            if (isDead || isDisguised || player == null)
                return;

            if (DistanceToPlayer() <= detectionRange)
            {
                float dir = Mathf.Sign(player.position.x - transform.position.x);
                float vx = IsPathSafe(dir) ? dir * chaseSpeed : 0f;
                rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
        }

        private bool IsPathSafe(float dirX)
        {
            Vector2 origin = transform.position;
            Vector2 edgeOrigin = origin + new Vector2(dirX * edgeCheckOffsetX, 0f);
            if (Physics2D.Raycast(edgeOrigin, Vector2.down, edgeCheckDistance, groundLayer).collider == null)
                return false;

            RaycastHit2D[] hits = Physics2D.RaycastAll(origin + new Vector2(0f, -0.35f), new Vector2(dirX, 0f), edgeCheckOffsetX + 0.35f);
            foreach (var hit in hits)
            {
                if (hit.collider != null && hit.collider.GetComponent<Hazard>() != null)
                    return false;
            }
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, revealDistance);
        }
    }
}

