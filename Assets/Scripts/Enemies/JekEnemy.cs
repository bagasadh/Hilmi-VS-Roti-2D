using HilmiVSRoti.Level;
using UnityEngine;

namespace HilmiVSRoti.Enemies
{
    /// <summary>
    /// Musuh 1: jek (bebek kuning pegang piso).
    /// Berjalan bolak-balik sambil menghindari jurang & duri, lalu ancang-ancang sebentar
    /// dan menerjang lurus saat Hilmi masuk jarak deteksi.
    /// </summary>
    public class JekEnemy : Enemy
    {
        [Header("Patrol & Terjang (jek)")]
        [SerializeField] private float patrolSpeed = 2.2f;
        [SerializeField] private float patrolDistance = 3f;
        [SerializeField] private float chargeSpeed = 7.2f;
        [SerializeField] private float windUpDuration = 0.25f;
        [SerializeField] private float chargeDuration = 0.75f;
        [SerializeField] private float chargeCooldown = 1.3f;
        [SerializeField] private float verticalDetectTolerance = 2.5f;

        [Header("Deteksi Jurang, Dinding & Duri")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float edgeCheckOffsetX = 0.65f;
        [SerializeField] private float edgeCheckDistance = 1.6f;
        [SerializeField] private float obstacleCheckDistance = 0.75f;

        private enum State { Patrol, WindUp, Charge }

        private Vector3 startPos;
        private int patrolDir = 1;
        private State currentState = State.Patrol;
        private float stateTimer;
        private float cooldownTimer;
        private float chargeDir = 1f;

        private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");

        protected override void Awake()
        {
            base.Awake();
            startPos = transform.position;
            if (groundLayer == 0)
                groundLayer = LayerMask.GetMask("Ground");
        }

        protected override void UpdateBehavior()
        {
            if (cooldownTimer > 0f)
                cooldownTimer -= Time.deltaTime;

            switch (currentState)
            {
                case State.WindUp:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                    {
                        currentState = State.Charge;
                        stateTimer = chargeDuration;
                    }
                    break;

                case State.Charge:
                    stateTimer -= Time.deltaTime;
                    // Hentikan terjangan jika di depan ada jurang, duri, atau dinding
                    if (stateTimer <= 0f || !IsPathSafe(chargeDir))
                    {
                        currentState = State.Patrol;
                        cooldownTimer = chargeCooldown;
                    }
                    break;

                case State.Patrol:
                    // Cek apakah Hilmi masuk jarak deteksi dan jalur di depan aman
                    if (player != null && cooldownTimer <= 0f)
                    {
                        float distX = Mathf.Abs(player.position.x - transform.position.x);
                        float distY = Mathf.Abs(player.position.y - transform.position.y);
                        float dirToPlayer = player.position.x >= transform.position.x ? 1f : -1f;

                        if (distX <= detectionRange && distY <= verticalDetectTolerance && IsPathSafe(dirToPlayer))
                        {
                            currentState = State.WindUp;
                            stateTimer = windUpDuration;
                            chargeDir = dirToPlayer;
                            FaceDirection(chargeDir);
                            return;
                        }
                    }

                    // Balik arah jika melewati batas patrol atau ada jurang/duri/dinding
                    if (transform.position.x >= startPos.x + patrolDistance || (patrolDir > 0 && !IsPathSafe(1f)))
                        patrolDir = -1;
                    else if (transform.position.x <= startPos.x - patrolDistance || (patrolDir < 0 && !IsPathSafe(-1f)))
                        patrolDir = 1;

                    FaceDirection(patrolDir);
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (isDead)
                return;

            float vx = 0f;
            if (currentState == State.Patrol && IsPathSafe(patrolDir))
                vx = patrolDir * patrolSpeed;
            else if (currentState == State.Charge && IsPathSafe(chargeDir))
                vx = chargeDir * chargeSpeed;

            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

            if (anim != null)
                anim.SetBool(HashIsMoving, Mathf.Abs(vx) > 0.1f);
        }

        /// <summary>
        /// Mengembalikan true jika di depan masih ada pijakan tanah, tidak ada dinding, dan tidak ada duri (Hazard).
        /// </summary>
        private bool IsPathSafe(float dirX)
        {
            Vector2 origin = transform.position;

            // 1. Cek apakah masih ada tanah di depan (hindari jatuh ke jurang)
            Vector2 edgeOrigin = origin + new Vector2(dirX * edgeCheckOffsetX, 0f);
            RaycastHit2D groundHit = Physics2D.Raycast(edgeOrigin, Vector2.down, edgeCheckDistance, groundLayer);
            if (groundHit.collider == null)
                return false;

            // 2. Cek apakah ada dinding di depan
            RaycastHit2D wallHit = Physics2D.Raycast(origin, new Vector2(dirX, 0f), obstacleCheckDistance, groundLayer);
            if (wallHit.collider != null)
                return false;

            // 3. Cek apakah ada duri/Hazard di depan (agar tidak menerobos spike)
            RaycastHit2D[] aheadHits = Physics2D.RaycastAll(origin + new Vector2(0f, -0.35f), new Vector2(dirX, 0f), obstacleCheckDistance + 0.35f);
            foreach (var hit in aheadHits)
            {
                if (hit.collider != null && hit.collider.GetComponent<Hazard>() != null)
                    return false;
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 center = Application.isPlaying ? startPos : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(center + Vector3.left * patrolDistance, center + Vector3.right * patrolDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
        }
    }
}
