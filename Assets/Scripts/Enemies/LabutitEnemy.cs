using UnityEngine;

namespace HilmiVSRoti.Enemies
{
    /// <summary>
    /// Musuh 2: labutit (Labu).
    /// Diam di tempat dan melempar proyektil dengan lintasan melengkung ke arah Hilmi.
    /// </summary>
    public class LabutitEnemy : Enemy
    {
        [Header("Lemparan Labutit")]
        [SerializeField] private LabutitProjectile projectilePrefab;
        [SerializeField] private Transform throwOrigin;
        [SerializeField] private float throwInterval = 2.4f;
        [SerializeField] private float throwArcHeight = 3.6f;
        [SerializeField] private float maxThrowSpeedX = 11f;

        private float throwTimer;

        protected override void Awake()
        {
            base.Awake();
            throwTimer = throwInterval * 0.5f;
            // Labutit diam di tempat agar tidak terdorong ke duri atau jurang
            rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }

        protected override void UpdateBehavior()
        {
            if (player == null)
                return;

            float dirX = player.position.x - transform.position.x;
            float dirY = player.position.y - transform.position.y;
            FaceDirection(dirX);

            // Miringkan Labutit menengadah ke atas saat Hilmi berada di platform atas
            float targetAngle = dirY > 0.8f ? (dirX >= 0f ? 22f : -22f) : 0f;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, targetAngle), Time.deltaTime * 8f);

            if (DistanceToPlayer() > detectionRange)
                return;

            throwTimer -= Time.deltaTime;
            if (throwTimer <= 0f)
            {
                ThrowProjectile();
                throwTimer = throwInterval;
            }
        }

        private void ThrowProjectile()
        {
            if (projectilePrefab == null || player == null)
                return;

            float aimDirX = player.position.x >= transform.position.x ? 1f : -1f;
            Vector3 spawnPos = throwOrigin != null ? throwOrigin.position : transform.position + new Vector3(aimDirX * 0.45f, 0.35f, 0f);
            LabutitProjectile proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            // Hitung lintasan balistik: datar/rendah jika Hilmi di bawah, melambung tinggi jika Hilmi di atas
            float dx = player.position.x - spawnPos.x;
            float dy = player.position.y - spawnPos.y;
            float gravity = Mathf.Abs(Physics2D.gravity.y) * 1.8f;

            float vy = dy > 0.6f
                ? Mathf.Sqrt(2f * gravity * (dy + 1.6f))
                : throwArcHeight;

            float tUp = vy / gravity;
            float peak = (vy * vy) / (2f * gravity);
            float tDown = Mathf.Sqrt(2f * Mathf.Max(0.1f, peak - dy) / gravity);
            float flightTime = Mathf.Max(0.25f, tUp + tDown);
            float vx = Mathf.Clamp(dx / flightTime, -maxThrowSpeedX, maxThrowSpeedX);
            proj.Launch(new Vector2(vx, vy));
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}

