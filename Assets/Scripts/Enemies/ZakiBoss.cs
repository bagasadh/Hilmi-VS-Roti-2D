using System;
using System.Collections;
using HilmiVSRoti.Player;
using UnityEngine;

namespace HilmiVSRoti.Enemies
{
    /// <summary>
    /// Final Boss: Zaki Raksasa (versi besar dari jek).
    /// Memiliki 3 pola serangan: Menerjang, Tebas, dan Lompat Menghantam Tanah (Ground Slam).
    /// </summary>
    public class ZakiBoss : Enemy
    {
        [Header("Arena & Pengaturan Boss")]
        [SerializeField] private float arenaMinX = 122f;
        [SerializeField] private float arenaMaxX = 146f;
        [SerializeField] private float activationRange = 14f;
        [SerializeField] private float decisionInterval = 1.4f;
        [SerializeField] private float hitCooldown = 0.6f;

        [Header("Serangan 1: Menerjang")]
        [SerializeField] private float chargeSpeed = 9f;
        [SerializeField] private float chargeDuration = 0.9f;

        [Header("Serangan 2: Tebas")]
        [SerializeField] private float slashRange = 3.2f;
        [SerializeField] private float slashWaveSpeed = 8f;

        [Header("Serangan 3: Lompat Hantam Tanah")]
        [SerializeField] private float slamJumpForceY = 16f;
        [SerializeField] private float slamHorizontalSpeed = 6f;
        [SerializeField] private float slamFallGravity = 6f;
        [SerializeField] private float shockwaveSpeed = 9f;
        [SerializeField] private BossShockwave shockwavePrefab;

        private enum BossState { Idle, Charging, Slashing, JumpingUp, SlammingDown }

        private BossState state = BossState.Idle;
        private float stateTimer;
        private float chargeStuckTimer;
        private float hitCooldownTimer;
        private float defaultGravity;
        private float activeDir = -1f;
        private int attackIndex;
        private bool isActivated;

        private static readonly int HashIsMoving = Animator.StringToHash("IsMoving");

        public event Action<int, int> OnBossHealthChanged;
        public event Action OnBossActivated;
        public event Action OnBossDefeated;

        protected override void Awake()
        {
            base.Awake();
            defaultGravity = rb.gravityScale;
            stateTimer = decisionInterval;

            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.sharedMaterial = new PhysicsMaterial2D("BossNoFriction")
                {
                    friction = 0f,
                    bounciness = 0f
                };
            }
        }

        protected override void Start()
        {
            base.Start();
            OnBossHealthChanged?.Invoke(currentHp, maxHp);
        }

        protected override void UpdateBehavior()
        {
            if (hitCooldownTimer > 0f)
                hitCooldownTimer -= Time.deltaTime;

            if (player == null)
                return;

            if (!isActivated)
            {
                if (DistanceToPlayer() <= activationRange)
                {
                    isActivated = true;
                    OnBossActivated?.Invoke();
                }
                return;
            }

            switch (state)
            {
                case BossState.Idle:
                    FaceDirection(player.position.x - transform.position.x);
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                        ChooseNextAttack();
                    break;

                case BossState.Charging:
                    stateTimer -= Time.deltaTime;
                    if (Mathf.Abs(rb.linearVelocity.x) < 0.6f && Mathf.Abs(rb.linearVelocity.y) < 0.1f)
                    {
                        chargeStuckTimer += Time.deltaTime;
                        if (chargeStuckTimer >= 0.08f)
                        {
                            chargeStuckTimer = 0f;
                            rb.linearVelocity = new Vector2(activeDir * chargeSpeed, 17.5f);
                        }
                    }
                    else
                    {
                        chargeStuckTimer = 0f;
                    }

                    bool isGrounded = Mathf.Abs(rb.linearVelocity.y) < 0.1f;
                    if ((stateTimer <= 0f && isGrounded) || stateTimer <= -0.8f || HitArenaBoundary(activeDir))
                        EndAttack();
                    break;

                case BossState.Slashing:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                        EndAttack();
                    break;

                case BossState.JumpingUp:
                    stateTimer += Time.deltaTime;
                    if (rb.linearVelocity.y <= 0f)
                    {
                        state = BossState.SlammingDown;
                        stateTimer = 0f;
                        rb.gravityScale = slamFallGravity;
                    }
                    break;

                case BossState.SlammingDown:
                    stateTimer += Time.deltaTime;
                    if (Mathf.Abs(rb.linearVelocity.y) < 0.05f && stateTimer > 0.08f)
                    {
                        SpawnShockwave(Vector2.left, shockwaveSpeed);
                        SpawnShockwave(Vector2.right, shockwaveSpeed);
                        rb.gravityScale = defaultGravity;
                        EndAttack();
                    }
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (isDead)
                return;

            if (state == BossState.Charging && !HitArenaBoundary(activeDir))
                rb.linearVelocity = new Vector2(activeDir * chargeSpeed, rb.linearVelocity.y);
            else if ((state == BossState.JumpingUp || state == BossState.SlammingDown) && !HitArenaBoundary(activeDir))
                rb.linearVelocity = new Vector2(activeDir * slamHorizontalSpeed, rb.linearVelocity.y);
            else
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (anim != null)
                anim.SetBool(HashIsMoving, Mathf.Abs(rb.linearVelocity.x) > 0.1f);
        }

        private void ChooseNextAttack()
        {
            activeDir = player.position.x >= transform.position.x ? 1f : -1f;
            FaceDirection(activeDir);

            // Jika pemain berada di atas platform bata, prioritaskan lompat ke atas
            if (player.position.y > transform.position.y + 0.9f && attackIndex % 2 == 0)
            {
                StartGroundSlam();
            }
            // Jika pemain sangat dekat, prioritaskan tebasan
            else if (DistanceToPlayer() <= slashRange && attackIndex % 2 == 0)
            {
                StartSlash();
            }
            else
            {
                int pattern = attackIndex % 3;
                if (pattern == 0)
                    StartCharge();
                else if (pattern == 1)
                    StartSlash();
                else
                    StartGroundSlam();
            }

            attackIndex++;
        }

        private void StartCharge()
        {
            state = BossState.Charging;
            stateTimer = chargeDuration;
            chargeStuckTimer = 0f;
        }

        private void StartSlash()
        {
            state = BossState.Slashing;
            stateTimer = 0.5f;
            SpawnShockwave(new Vector2(activeDir, 0f), slashWaveSpeed, -0.35f);
        }

        private void StartGroundSlam()
        {
            state = BossState.JumpingUp;
            stateTimer = 0f;
            rb.linearVelocity = new Vector2(activeDir * slamHorizontalSpeed, slamJumpForceY);
        }

        private void EndAttack()
        {
            state = BossState.Idle;
            stateTimer = decisionInterval;
        }

        private void SpawnShockwave(Vector2 dir, float speed, float offsetY = -1.0f)
        {
            if (shockwavePrefab == null)
                return;

            Vector3 spawnPos = new Vector3(transform.position.x + dir.x * 0.8f, transform.position.y + offsetY, 0f);
            BossShockwave wave = Instantiate(shockwavePrefab, spawnPos, Quaternion.identity);
            wave.Initialize(dir, speed);
        }

        private bool HitArenaBoundary(float dir)
        {
            return (dir < 0f && transform.position.x <= arenaMinX) ||
                   (dir > 0f && transform.position.x >= arenaMaxX);
        }

        public override void TakeDamage(int amount)
        {
            if (isDead || hitCooldownTimer > 0f)
                return;

            hitCooldownTimer = hitCooldown;
            currentHp = Mathf.Max(0, currentHp - amount);
            OnBossHealthChanged?.Invoke(currentHp, maxHp);
            StartCoroutine(FlashRed());

            if (currentHp <= 0)
                Die();
        }

        private IEnumerator FlashRed()
        {
            sr.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            if (!isDead)
                sr.color = Color.white;
        }

        protected override void Die()
        {
            isDead = true;
            OnBossDefeated?.Invoke();
            Destroy(gameObject);
        }

        protected override void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDead || !collision.gameObject.CompareTag("Player"))
                return;

            var playerController = collision.gameObject.GetComponent<PlayerController>();
            var playerHealth = collision.gameObject.GetComponent<PlayerHealth>();

            if (playerController != null && playerController.IsRamModeActive)
            {
                playerController.Bounce(stompBounceForce);
                TakeDamage(1);
                return;
            }

            bool stomped = false;
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.45f)
                {
                    stomped = true;
                    break;
                }
            }

            if (stomped)
            {
                if (playerController != null)
                    playerController.Bounce(stompBounceForce);
                TakeDamage(1);
                return;
            }

            if (playerHealth != null)
                playerHealth.TakeDamage(contactDamage, transform.position);
        }
    }
}

