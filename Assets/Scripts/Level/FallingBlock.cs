using System.Collections;
using UnityEngine;

namespace HilmiVSRoti.Level
{
    /// <summary>
    /// Balok yang bergetar lalu jatuh setelah diinjak pemain, dan muncul kembali setelah beberapa detik.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class FallingBlock : MonoBehaviour
    {
        [Header("Pengaturan Jatuh")]
        [SerializeField] private float shakeDuration = 0.65f;
        [SerializeField] private float shakeMagnitude = 0.06f;
        [SerializeField] private float fallGravityScale = 3f;
        [SerializeField] private float destroyOrHideDelay = 1.2f;
        [SerializeField] private float respawnDelay = 1.8f;

        private Rigidbody2D rb;
        private BoxCollider2D col;
        private SpriteRenderer sr;
        private Vector3 initialPos;
        private bool isTriggered;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<BoxCollider2D>();
            sr = GetComponent<SpriteRenderer>();
            initialPos = transform.position;
            ResetBlock();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isTriggered || !collision.gameObject.CompareTag("Player"))
                return;

            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f)
                {
                    StartCoroutine(FallSequence());
                    break;
                }
            }
        }

        private IEnumerator FallSequence()
        {
            isTriggered = true;
            float elapsed = 0f;

            // Fase bergetar sebagai tanda peringatan
            while (elapsed < shakeDuration)
            {
                float offsetX = Random.Range(-shakeMagnitude, shakeMagnitude);
                transform.position = initialPos + new Vector3(offsetX, 0f, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Mulai jatuh
            transform.position = initialPos;
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = fallGravityScale;

            yield return new WaitForSeconds(destroyOrHideDelay);

            // Sembunyikan sementara lalu respawn agar tidak soft-lock
            col.enabled = false;
            if (sr != null) sr.enabled = false;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;

            yield return new WaitForSeconds(respawnDelay);
            ResetBlock();
        }

        private void ResetBlock()
        {
            isTriggered = false;
            transform.position = initialPos;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            col.enabled = true;
            if (sr != null) sr.enabled = true;
        }
    }
}

