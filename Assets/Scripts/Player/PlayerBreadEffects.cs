using System;
using System.Collections.Generic;
using HilmiVSRoti.Items;
using UnityEngine;

namespace HilmiVSRoti.Player
{
    /// <summary>
    /// Mengelola efek aktif dari Roti (buff & debuff), durasi waktu,
    /// serta memberi tahu UI setiap kali timer berubah.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(PlayerHealth))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerBreadEffects : MonoBehaviour
    {
        [Header("Pengaturan Multiplier Efek Roti")]
        [SerializeField] private float rotiTawarJumpMultiplier = 1.4f;
        [SerializeField] private float rotiCokelatSpeedMultiplier = 1.5f;
        [SerializeField] private float rotiJamuranSpeedMultiplier = 0.6f;
        [SerializeField] private float rotiJamuranJumpMultiplier = 0.7f;

        private PlayerController controller;
        private PlayerHealth health;
        private SpriteRenderer sr;

        private readonly Dictionary<BreadType, float> activeTimers = new Dictionary<BreadType, float>();
        private readonly List<BreadType> keysBuffer = new List<BreadType>();

        public event Action OnEffectsUpdated;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
            health = GetComponent<PlayerHealth>();
            sr = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            if (health != null)
                health.OnShieldBroken += HandleShieldBroken;
        }

        private void OnDisable()
        {
            if (health != null)
                health.OnShieldBroken -= HandleShieldBroken;
        }

        private void Update()
        {
            if (activeTimers.Count == 0)
                return;

            keysBuffer.Clear();
            foreach (var key in activeTimers.Keys)
                keysBuffer.Add(key);

            bool changed = false;
            foreach (BreadType type in keysBuffer)
            {
                activeTimers[type] -= Time.deltaTime;
                if (activeTimers[type] <= 0f)
                {
                    activeTimers.Remove(type);
                    changed = true;
                }
            }

            if (changed)
                RecalculateModifiers();

            OnEffectsUpdated?.Invoke();
        }

        public void ApplyBread(BreadType type, float duration)
        {
            activeTimers[type] = duration;
            if (type == BreadType.RotiSobek && health != null)
                health.HasShield = true;

            RecalculateModifiers();
            OnEffectsUpdated?.Invoke();
        }

        private void HandleShieldBroken()
        {
            if (activeTimers.Remove(BreadType.RotiSobek))
            {
                RecalculateModifiers();
                OnEffectsUpdated?.Invoke();
            }
        }

        private void RecalculateModifiers()
        {
            float speedMult = 1f;
            float jumpMult = 1f;

            if (activeTimers.ContainsKey(BreadType.RotiTawar))
                jumpMult *= rotiTawarJumpMultiplier;

            if (activeTimers.ContainsKey(BreadType.RotiCokelat))
                speedMult *= rotiCokelatSpeedMultiplier;

            if (activeTimers.ContainsKey(BreadType.RotiJamuran))
            {
                speedMult *= rotiJamuranSpeedMultiplier;
                jumpMult *= rotiJamuranJumpMultiplier;
            }

            bool ramActive = activeTimers.ContainsKey(BreadType.RotiKeju);

            controller.SpeedMultiplier = speedMult;
            controller.JumpMultiplier = jumpMult;
            controller.IsRamModeActive = ramActive;

            if (health != null)
                health.HasShield = activeTimers.ContainsKey(BreadType.RotiSobek);

            UpdateVisualTint(ramActive);
        }

        private void UpdateVisualTint(bool ramActive)
        {
            if (ramActive)
                sr.color = new Color(1f, 0.9f, 0.35f, 1f); // Emas (Mode Tabrak)
            else if (activeTimers.ContainsKey(BreadType.RotiSobek))
                sr.color = new Color(0.55f, 0.9f, 1f, 1f); // Biru perisai
            else if (activeTimers.ContainsKey(BreadType.RotiJamuran))
                sr.color = new Color(0.65f, 0.95f, 0.55f, 1f); // Kehijauan (Debuff)
            else
                sr.color = Color.white;
        }

        public IReadOnlyDictionary<BreadType, float> GetActiveEffects() => activeTimers;
    }
}

