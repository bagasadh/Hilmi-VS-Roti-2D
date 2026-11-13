using System;
using HilmiVSRoti.Enemies;
using UnityEngine;

namespace HilmiVSRoti.Level
{
    /// <summary>
    /// Garis Finish di ujung stage. Terkunci sampai Final Boss (Zaki Raksasa) dikalahkan.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class FinishLine : MonoBehaviour
    {
        [SerializeField] private ZakiBoss finalBoss;
        [SerializeField] private Color lockedColor = new Color(0.55f, 0.25f, 0.25f, 1f);
        [SerializeField] private Color unlockedColor = new Color(1f, 0.85f, 0.2f, 1f);

        private SpriteRenderer sr;
        private bool isUnlocked;

        public event Action OnFinishReached;
        public event Action OnFinishLockedAttempt;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            GetComponent<BoxCollider2D>().isTrigger = true;
            isUnlocked = finalBoss == null;
            UpdateVisual();
        }

        private void OnEnable()
        {
            if (finalBoss != null)
                finalBoss.OnBossDefeated += UnlockFinish;
        }

        private void OnDisable()
        {
            if (finalBoss != null)
                finalBoss.OnBossDefeated -= UnlockFinish;
        }

        private void UnlockFinish()
        {
            isUnlocked = true;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (sr != null)
                sr.color = isUnlocked ? unlockedColor : lockedColor;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            if (isUnlocked)
                OnFinishReached?.Invoke();
            else
                OnFinishLockedAttempt?.Invoke();
        }
    }
}

