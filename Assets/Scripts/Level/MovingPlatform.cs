using UnityEngine;

namespace HilmiVSRoti.Level
{
    /// <summary>
    /// Platform bergerak bolak-balik (misalnya naik-turun atau kiri-kanan).
    /// Pemain otomatis ikut terbawa saat berdiri di atasnya.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Pergerakan")]
        [SerializeField] private Vector3 moveOffset = new Vector3(0f, 3.5f, 0f);
        [SerializeField] private float speed = 2.5f;
        [SerializeField] private float waitTimeAtEnds = 0.4f;

        private Vector3 startPos;
        private Vector3 targetPos;
        private Vector3 currentDestination;
        private float waitTimer;

        private void Awake()
        {
            startPos = transform.position;
            targetPos = startPos + moveOffset;
            currentDestination = targetPos;
        }

        private void FixedUpdate()
        {
            if (waitTimer > 0f)
            {
                waitTimer -= Time.fixedDeltaTime;
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, currentDestination, speed * Time.fixedDeltaTime);

            if (Vector3.Distance(transform.position, currentDestination) < 0.01f)
            {
                transform.position = currentDestination;
                currentDestination = currentDestination == targetPos ? startPos : targetPos;
                waitTimer = waitTimeAtEnds;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag("Player"))
                return;

            // Pastikan pemain mendarat dari atas platform
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f)
                {
                    collision.transform.SetParent(transform);
                    break;
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player") && collision.transform.parent == transform)
            {
                collision.transform.SetParent(null);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 from = Application.isPlaying ? startPos : transform.position;
            Vector3 to = from + moveOffset;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(from, to);
            Gizmos.DrawWireCube(to, transform.localScale);
        }
    }
}

