using System.Collections;
using HilmiVSRoti.Enemies;
using HilmiVSRoti.Level;
using HilmiVSRoti.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HilmiVSRoti.UI
{
    /// <summary>
    /// Mengatur UI HP Final Boss dan layar Menang / Kalah lengkap dengan tombol Restart.
    /// </summary>
    public class BossAndEndUI : MonoBehaviour
    {
        [Header("Referensi")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private ZakiBoss finalBoss;
        [SerializeField] private FinishLine finishLine;

        [Header("UI Elemen")]
        [SerializeField] private Text bossHealthText;
        [SerializeField] private GameObject bossBarRoot;
        [SerializeField] private Image bossHealthFill;
        [SerializeField] private Text noticeText;
        [SerializeField] private GameObject endPanel;
        [SerializeField] private Text endTitleText;

        private void OnEnable()
        {
            if (playerHealth != null)
                playerHealth.OnPlayerDied += ShowLoseScreen;

            if (finalBoss != null)
            {
                finalBoss.OnBossActivated += ShowBossBar;
                finalBoss.OnBossHealthChanged += UpdateBossBar;
                finalBoss.OnBossDefeated += HandleBossDefeated;
            }

            if (finishLine != null)
            {
                finishLine.OnFinishReached += ShowWinScreen;
                finishLine.OnFinishLockedAttempt += ShowLockedNotice;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
                playerHealth.OnPlayerDied -= ShowLoseScreen;

            if (finalBoss != null)
            {
                finalBoss.OnBossActivated -= ShowBossBar;
                finalBoss.OnBossHealthChanged -= UpdateBossBar;
                finalBoss.OnBossDefeated -= HandleBossDefeated;
            }

            if (finishLine != null)
            {
                finishLine.OnFinishReached -= ShowWinScreen;
                finishLine.OnFinishLockedAttempt -= ShowLockedNotice;
            }
        }

        private void Start()
        {
            if (bossBarRoot != null)
                bossBarRoot.SetActive(false);
            if (bossHealthText != null)
                bossHealthText.gameObject.SetActive(false);
            if (noticeText != null)
                noticeText.gameObject.SetActive(false);
            if (endPanel != null)
                endPanel.SetActive(false);
        }

        private void ShowBossBar()
        {
            if (bossBarRoot != null)
                bossBarRoot.SetActive(true);
            if (bossHealthText != null)
                bossHealthText.gameObject.SetActive(true);
        }

        private void UpdateBossBar(int hp, int maxHp)
        {
            if (bossHealthText != null)
                bossHealthText.text = $"ZAKI RAKSASA  {hp}/{maxHp}";
            if (bossHealthFill != null && maxHp > 0)
                bossHealthFill.fillAmount = Mathf.Clamp01((float)hp / maxHp);
        }

        private void HandleBossDefeated()
        {
            if (bossBarRoot != null)
                bossBarRoot.SetActive(false);
            if (bossHealthText != null)
                bossHealthText.gameObject.SetActive(false);
            StartCoroutine(ShowTempNotice("Zaki Raksasa Kalah! Menuju Garis Finish!"));
        }

        private void ShowLockedNotice()
        {
            StartCoroutine(ShowTempNotice("Kalahkan Zaki Raksasa dulu sebelum Finish!"));
        }

        private IEnumerator ShowTempNotice(string msg)
        {
            if (noticeText == null)
                yield break;

            noticeText.text = msg;
            noticeText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2.5f);
            noticeText.gameObject.SetActive(false);
        }

        private void ShowWinScreen()
        {
            Time.timeScale = 0f;
            if (endPanel != null)
                endPanel.SetActive(true);
            if (endTitleText != null)
            {
                endTitleText.text = "MENANG!\nHilmi Berhasil Melewati Stage!";
                endTitleText.color = new Color(0.3f, 1f, 0.45f, 1f);
            }
        }

        private void ShowLoseScreen()
        {
            if (endPanel != null)
                endPanel.SetActive(true);
            if (endTitleText != null)
            {
                endTitleText.text = "KALAH!\nHilmi Kehabisan Nyawa";
                endTitleText.color = new Color(1f, 0.35f, 0.35f, 1f);
            }
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}

