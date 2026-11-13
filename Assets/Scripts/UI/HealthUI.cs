using HilmiVSRoti.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HilmiVSRoti.UI
{
    /// <summary>
    /// Menampilkan jumlah nyawa Hilmi dan pesan Game Over sederhana di layar.
    /// </summary>
    public class HealthUI : MonoBehaviour
    {
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private Text healthText;
        [SerializeField] private Text statusText;
        [SerializeField] private Image[] heartIcons;
        [SerializeField] private Sprite heartFullSprite;
        [SerializeField] private Sprite heartEmptySprite;

        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += UpdateHealthDisplay;
                playerHealth.OnPlayerDied += ShowGameOver;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= UpdateHealthDisplay;
                playerHealth.OnPlayerDied -= ShowGameOver;
            }
        }

        private void Start()
        {
            if (statusText != null)
                statusText.gameObject.SetActive(false);

            if (playerHealth != null)
                UpdateHealthDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }

        private void UpdateHealthDisplay(int current, int max)
        {
            if (heartIcons != null && heartIcons.Length > 0 && heartFullSprite != null && heartEmptySprite != null)
            {
                for (int i = 0; i < heartIcons.Length; i++)
                {
                    if (heartIcons[i] == null)
                        continue;
                    heartIcons[i].gameObject.SetActive(i < max);
                    heartIcons[i].sprite = i < current ? heartFullSprite : heartEmptySprite;
                }

                if (healthText != null)
                    healthText.text = "NYAWA";
                return;
            }

            if (healthText == null)
                return;

            string hearts = "";
            for (int i = 0; i < max; i++)
                hearts += i < current ? "♥ " : "♡ ";

            healthText.text = $"Nyawa: {hearts.TrimEnd()}";
        }

        private void ShowGameOver()
        {
            if (statusText != null)
            {
                statusText.text = "GAME OVER!\nRestarting...";
                statusText.gameObject.SetActive(true);
            }
        }
    }
}

