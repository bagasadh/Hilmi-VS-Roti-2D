using System;
using HilmiVSRoti.Enemies;
using HilmiVSRoti.Items;
using HilmiVSRoti.Level;
using HilmiVSRoti.Player;
using UnityEngine;

namespace HilmiVSRoti.Audio
{
    /// <summary>
    /// Mengelola efek suara (SFX) permainan secara terpusat.
    /// Jika AudioClip di Inspector kosong, otomatis membuat suara retro 8-bit prosedural.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class GameAudioManager : MonoBehaviour
    {
        public static GameAudioManager Instance { get; private set; }

        [Header("Volume & Status")]
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.65f;

        [Header("Audio Clips Opsional (Kosong = Pakai Suara Retro Otomatis)")]
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip stompClip;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip breadGoodClip;
        [SerializeField] private AudioClip breadBadClip;
        [SerializeField] private AudioClip checkpointClip;
        [SerializeField] private AudioClip bossHitClip;
        [SerializeField] private AudioClip winClip;
        [SerializeField] private AudioClip loseClip;

        private AudioSource audioSource;
        private PlayerController playerController;
        private PlayerHealth playerHealth;
        private ZakiBoss zakiBoss;
        private FinishLine finishLine;
        private int lastPlayerHp = -1;
        private int lastBossHp = -1;

        public bool IsMuted { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;

            EnsureProceduralClips();
        }

        private void OnEnable()
        {
            Checkpoint.OnAnyCheckpointActivated += HandleCheckpoint;
            BreadPickup.OnAnyBreadPickedUp += HandleBreadPickup;

            playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                playerController.OnJumped += HandleJump;
                playerController.OnBounced += HandleStomp;
            }

            playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth != null)
            {
                lastPlayerHp = playerHealth.CurrentHealth;
                playerHealth.OnHealthChanged += HandlePlayerHealthChanged;
                playerHealth.OnShieldBroken += HandleHit;
                playerHealth.OnPlayerDied += HandleLose;
            }

            zakiBoss = FindFirstObjectByType<ZakiBoss>();
            if (zakiBoss != null)
            {
                zakiBoss.OnBossHealthChanged += HandleBossHealthChanged;
                zakiBoss.OnBossDefeated += HandleBossDefeated;
            }

            finishLine = FindFirstObjectByType<FinishLine>();
            if (finishLine != null)
                finishLine.OnFinishReached += HandleWin;
        }

        private void OnDisable()
        {
            Checkpoint.OnAnyCheckpointActivated -= HandleCheckpoint;
            BreadPickup.OnAnyBreadPickedUp -= HandleBreadPickup;

            if (playerController != null)
            {
                playerController.OnJumped -= HandleJump;
                playerController.OnBounced -= HandleStomp;
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandlePlayerHealthChanged;
                playerHealth.OnShieldBroken -= HandleHit;
                playerHealth.OnPlayerDied -= HandleLose;
            }

            if (zakiBoss != null)
            {
                zakiBoss.OnBossHealthChanged -= HandleBossHealthChanged;
                zakiBoss.OnBossDefeated -= HandleBossDefeated;
            }

            if (finishLine != null)
                finishLine.OnFinishReached -= HandleWin;
        }

        public void ToggleMute()
        {
            IsMuted = !IsMuted;
            AudioListener.volume = IsMuted ? 0f : 1f;
        }

        public void PlayClip(AudioClip clip, float volumeScale = 1f)
        {
            if (IsMuted || clip == null || audioSource == null)
                return;

            audioSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }

        private void HandleJump() => PlayClip(jumpClip, 0.7f);
        private void HandleStomp() => PlayClip(stompClip, 0.9f);
        private void HandleHit() => PlayClip(hitClip, 0.9f);
        private void HandleCheckpoint() => PlayClip(checkpointClip, 0.85f);
        private void HandleBossDefeated() => PlayClip(winClip, 0.95f);
        private void HandleWin() => PlayClip(winClip, 1f);
        private void HandleLose() => PlayClip(loseClip, 0.95f);

        private void HandlePlayerHealthChanged(int current, int max)
        {
            if (lastPlayerHp != -1 && current < lastPlayerHp && current > 0)
                PlayClip(hitClip, 0.9f);
            lastPlayerHp = current;
        }

        private void HandleBossHealthChanged(int current, int max)
        {
            if (lastBossHp != -1 && current < lastBossHp && current > 0)
                PlayClip(bossHitClip, 1f);
            lastBossHp = current;
        }

        private void HandleBreadPickup(BreadType type)
        {
            PlayClip(type == BreadType.RotiJamuran ? breadBadClip : breadGoodClip, 0.85f);
        }

        private void EnsureProceduralClips()
        {
            if (jumpClip == null) jumpClip = CreateSweepClip("SFX_Jump", 320f, 680f, 0.12f, 0.35f);
            if (stompClip == null) stompClip = CreateSweepClip("SFX_Stomp", 260f, 110f, 0.11f, 0.45f);
            if (hitClip == null) hitClip = CreateSweepClip("SFX_Hit", 200f, 75f, 0.18f, 0.5f);
            if (breadGoodClip == null) breadGoodClip = CreateTwoToneClip("SFX_BreadGood", 523.25f, 783.99f, 0.08f, 0.35f);
            if (breadBadClip == null) breadBadClip = CreateTwoToneClip("SFX_BreadBad", 260f, 175f, 0.12f, 0.4f);
            if (checkpointClip == null) checkpointClip = CreateTwoToneClip("SFX_Checkpoint", 587.33f, 880f, 0.10f, 0.35f);
            if (bossHitClip == null) bossHitClip = CreateSweepClip("SFX_BossHit", 160f, 65f, 0.22f, 0.55f);
            if (winClip == null) winClip = CreateFanfareClip("SFX_Win", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.10f, 0.4f);
            if (loseClip == null) loseClip = CreateFanfareClip("SFX_Lose", new[] { 392f, 349.23f, 311.13f, 261.63f }, 0.13f, 0.4f);
        }

        private static AudioClip CreateSweepClip(string clipName, float startFreq, float endFreq, float duration, float amplitude)
        {
            const int sampleRate = 22050;
            int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
            float[] data = new float[sampleCount];
            float phase = 0f;

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleCount;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                phase += 2f * Mathf.PI * freq / sampleRate;
                float envelope = 1f - t;
                float wave = Mathf.Sign(Mathf.Sin(phase));
                data[i] = wave * amplitude * envelope;
            }

            AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateTwoToneClip(string clipName, float freq1, float freq2, float noteDuration, float amplitude)
        {
            return CreateFanfareClip(clipName, new[] { freq1, freq2 }, noteDuration, amplitude);
        }

        private static AudioClip CreateFanfareClip(string clipName, float[] notes, float noteDuration, float amplitude)
        {
            const int sampleRate = 22050;
            int samplesPerNote = Mathf.Max(1, Mathf.RoundToInt(sampleRate * noteDuration));
            int totalSamples = samplesPerNote * notes.Length;
            float[] data = new float[totalSamples];

            for (int n = 0; n < notes.Length; n++)
            {
                float freq = notes[n];
                for (int i = 0; i < samplesPerNote; i++)
                {
                    float t = (float)i / samplesPerNote;
                    float envelope = 1f - t * 0.7f;
                    float wave = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate);
                    data[n * samplesPerNote + i] = wave * amplitude * envelope;
                }
            }

            AudioClip clip = AudioClip.Create(clipName, totalSamples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

