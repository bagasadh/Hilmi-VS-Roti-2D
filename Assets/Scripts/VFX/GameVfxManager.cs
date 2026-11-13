using HilmiVSRoti.Enemies;
using HilmiVSRoti.Items;
using HilmiVSRoti.Level;
using HilmiVSRoti.Player;
using UnityEngine;

namespace HilmiVSRoti.VFX
{
    /// <summary>
    /// Mengelola efek partikel pixel-art (debu lompat, percikan injak/hit, kilau roti, dan selebrasi menang).
    /// </summary>
    public class GameVfxManager : MonoBehaviour
    {
        private PlayerController playerController;
        private PlayerHealth playerHealth;
        private ZakiBoss zakiBoss;
        private FinishLine finishLine;
        private Sprite pixelSprite;
        private Material particleMaterial;
        private int lastPlayerHp = -1;
        private int lastBossHp = -1;

        private void Awake()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            Color[] pixels = new Color[16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            pixelSprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 16f);
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
                particleMaterial = new Material(shader);
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
                playerHealth.OnShieldBroken += HandlePlayerHit;
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
                playerHealth.OnShieldBroken -= HandlePlayerHit;
            }

            if (zakiBoss != null)
            {
                zakiBoss.OnBossHealthChanged -= HandleBossHealthChanged;
                zakiBoss.OnBossDefeated -= HandleBossDefeated;
            }

            if (finishLine != null)
                finishLine.OnFinishReached -= HandleWin;
        }

        private void HandleJump()
        {
            if (playerController == null)
                return;

            SpawnBurst(playerController.transform.position + new Vector3(0f, -0.75f, 0f),
                new Color(0.92f, 0.92f, 0.86f, 0.9f), 8, 2.2f, 0.35f, 0.14f);
        }

        private void HandleStomp()
        {
            if (playerController == null)
                return;

            SpawnBurst(playerController.transform.position + new Vector3(0f, -0.6f, 0f),
                new Color(1f, 0.85f, 0.25f, 1f), 14, 4.2f, 0.4f, 0.18f);
        }

        private void HandlePlayerHit()
        {
            if (playerHealth == null)
                return;

            SpawnBurst(playerHealth.transform.position,
                new Color(1f, 0.28f, 0.28f, 1f), 14, 4.5f, 0.45f, 0.2f);
        }

        private void HandlePlayerHealthChanged(int current, int max)
        {
            if (lastPlayerHp != -1 && current < lastPlayerHp)
                HandlePlayerHit();
            lastPlayerHp = current;
        }

        private void HandleBreadPickup(BreadType type)
        {
            if (playerController == null)
                return;

            Color c = type == BreadType.RotiJamuran
                ? new Color(0.65f, 0.25f, 0.9f, 1f)
                : new Color(0.35f, 1f, 0.55f, 1f);

            SpawnBurst(playerController.transform.position, c, 16, 3.8f, 0.5f, 0.18f);
        }

        private void HandleCheckpoint()
        {
            if (playerController == null)
                return;

            SpawnBurst(playerController.transform.position + Vector3.up * 0.4f,
                new Color(0.3f, 1f, 0.45f, 1f), 18, 4.5f, 0.6f, 0.2f);
        }

        private void HandleBossHealthChanged(int current, int max)
        {
            if (lastBossHp != -1 && current < lastBossHp && zakiBoss != null)
                SpawnBurst(zakiBoss.transform.position, new Color(1f, 0.55f, 0.15f, 1f), 20, 5.5f, 0.5f, 0.26f);
            lastBossHp = current;
        }

        private void HandleBossDefeated()
        {
            if (zakiBoss != null)
                SpawnBurst(zakiBoss.transform.position, new Color(1f, 0.85f, 0.2f, 1f), 36, 7f, 0.85f, 0.3f);
        }

        private void HandleWin()
        {
            if (finishLine != null)
                SpawnBurst(finishLine.transform.position + Vector3.up, new Color(1f, 0.88f, 0.25f, 1f), 40, 6.5f, 1.0f, 0.28f);
        }

        private void SpawnBurst(Vector3 pos, Color color, int count, float speed, float lifetime, float size)
        {
            var go = new GameObject("VfxBurst");
            go.transform.position = pos;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.duration = lifetime;
            main.loop = false;
            main.startLifetime = lifetime;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.45f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = color;
            main.gravityModifier = 0.8f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.enabled = true;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;

            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Sprites;
            tsa.AddSprite(pixelSprite);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (particleMaterial != null)
                renderer.material = particleMaterial;
            renderer.sortingOrder = 12;

            ps.Play();
        }
    }
}

