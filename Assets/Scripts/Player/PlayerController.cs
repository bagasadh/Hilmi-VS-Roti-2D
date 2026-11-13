using UnityEngine;
using UnityEngine.InputSystem;

namespace HilmiVSRoti.Player
{
    /// <summary>
    /// Mengatur pergerakan 2D ala Mario untuk Hilmi:
    /// gerak kiri-kanan, lompat dengan ketinggian variabel, coyote time, jump buffer, dan animasi.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Pergerakan Horizontal")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float acceleration = 60f;
        [SerializeField] private float deceleration = 70f;

        [Header("Lompat & Gravitasi")]
        [SerializeField] private float jumpForce = 16.5f;
        [SerializeField, Range(0.1f, 1f)] private float jumpCutMultiplier = 0.45f;
        [SerializeField] private float fallGravityMultiplier = 2.2f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Cek Tanah (Ground Check)")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.45f, 0.12f);
        [SerializeField] private LayerMask groundLayer;

        // Komponen internal
        private Rigidbody2D rb;
        private Animator anim;
        private SpriteRenderer sr;

        // Input actions
        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction jumpAction;

        // State pergerakan
        private float moveInputX;
        private float baseGravityScale;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private bool isFacingRight = true;

        // Hash parameter Animator agar lebih efisien
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int HashVerticalVelocity = Animator.StringToHash("VerticalVelocity");

        // Multiplier untuk sistem power-up / debuff Roti (Tahap 6)
        public float SpeedMultiplier { get; set; } = 1f;
        public float JumpMultiplier { get; set; } = 1f;
        public bool IsRamModeActive { get; set; }
        public bool IsGrounded { get; private set; }

        public event System.Action OnJumped;
        public event System.Action OnBounced;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            anim = GetComponent<Animator>();
            sr = GetComponent<SpriteRenderer>();
            baseGravityScale = rb.gravityScale;

            if (inputActions != null)
            {
                playerMap = inputActions.FindActionMap("Player", throwIfNotFound: true);
                moveAction = playerMap.FindAction("Move", throwIfNotFound: true);
                jumpAction = playerMap.FindAction("Jump", throwIfNotFound: true);
            }
            else
            {
                Debug.LogWarning("[PlayerController] InputActionAsset belum di-assign di Inspector!");
            }
        }

        private void OnEnable()
        {
            if (playerMap != null)
                playerMap.Enable();

            if (jumpAction != null)
            {
                jumpAction.started += OnJumpStarted;
                jumpAction.canceled += OnJumpCanceled;
            }
        }

        private void OnDisable()
        {
            if (jumpAction != null)
            {
                jumpAction.started -= OnJumpStarted;
                jumpAction.canceled -= OnJumpCanceled;
            }

            if (playerMap != null)
                playerMap.Disable();
        }

        private void Update()
        {
            // Baca input horizontal dari InputActionAsset, dengan fallback Keyboard.current (New Input System)
            moveInputX = moveAction != null ? moveAction.ReadValue<Vector2>().x : 0f;

            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (Mathf.Abs(moveInputX) < 0.01f)
                {
                    float right = (kb.dKey.isPressed || kb.rightArrowKey.isPressed) ? 1f : 0f;
                    float left = (kb.aKey.isPressed || kb.leftArrowKey.isPressed) ? 1f : 0f;
                    moveInputX = right - left;
                }

                // Fallback jika Jump action belum terpanggil
                if (jumpAction == null || !jumpAction.enabled)
                {
                    if (kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame)
                        jumpBufferTimer = jumpBufferTime;
                    if ((kb.spaceKey.wasReleasedThisFrame || kb.wKey.wasReleasedThisFrame || kb.upArrowKey.wasReleasedThisFrame) && rb.linearVelocity.y > 0f)
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
                }
            }

            CheckGround();
            UpdateTimers();
            TryExecuteJump();
            HandleFlip();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            ApplyHorizontalMovement();
            ApplyFallGravity();
        }

        private void CheckGround()
        {
            Vector2 checkPos = groundCheck != null ? (Vector2)groundCheck.position : (Vector2)transform.position;
            IsGrounded = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer) != null;
        }

        private void UpdateTimers()
        {
            if (IsGrounded)
                coyoteTimer = coyoteTime;
            else
                coyoteTimer -= Time.deltaTime;

            if (jumpBufferTimer > 0f)
                jumpBufferTimer -= Time.deltaTime;
        }

        private void OnJumpStarted(InputAction.CallbackContext ctx)
        {
            jumpBufferTimer = jumpBufferTime;
        }

        private void OnJumpCanceled(InputAction.CallbackContext ctx)
        {
            // Variable jump height: potong kecepatan naik jika tombol lompat dilepas lebih cepat
            if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y * jumpCutMultiplier
                );
            }
        }

        private void TryExecuteJump()
        {
            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce * JumpMultiplier);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                OnJumped?.Invoke();
            }
        }

        private void ApplyHorizontalMovement()
        {
            float targetSpeed = moveInputX * moveSpeed * SpeedMultiplier;
            float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;
            float newVelX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, rate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newVelX, rb.linearVelocity.y);
        }

        private void ApplyFallGravity()
        {
            // Gravitasi lebih kuat saat jatuh agar terasa responsif seperti Mario
            rb.gravityScale = rb.linearVelocity.y < -0.05f
                ? baseGravityScale * fallGravityMultiplier
                : baseGravityScale;
        }

        private void HandleFlip()
        {
            if (moveInputX > 0.05f && !isFacingRight)
                Flip();
            else if (moveInputX < -0.05f && isFacingRight)
                Flip();
        }

        private void Flip()
        {
            isFacingRight = !isFacingRight;
            sr.flipX = !isFacingRight;
        }

        private void UpdateAnimator()
        {
            anim.SetFloat(HashSpeed, Mathf.Abs(rb.linearVelocity.x));
            anim.SetBool(HashIsGrounded, IsGrounded);
            anim.SetFloat(HashVerticalVelocity, rb.linearVelocity.y);
        }

        /// <summary>
        /// Dipakai saat menginjak musuh dari atas agar Hilmi memantul.
        /// </summary>
        public void Bounce(float force)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
            OnBounced?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
                return;

            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}

