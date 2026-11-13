using HilmiVSRoti.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace HilmiVSRoti.UI
{
    /// <summary>
    /// Mengelola Main Menu di awal permainan serta menu jeda (Pause) saat menekan Esc.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Referensi UI")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Text startButtonLabel;
        [SerializeField] private Text soundButtonLabel;
        [SerializeField] private GameObject endScreenPanel;

        [Header("Pengaturan")]
        [SerializeField] private bool showOnStart = true;

        private bool isMenuOpen;
        private bool hasStartedOnce;

        private void Start()
        {
            if (showOnStart)
                OpenMenu(isPause: false);
            else
                CloseMenu();

            RefreshSoundLabel();
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            // Jangan buka menu jeda jika layar Menang/Kalah sedang tampil
            if (endScreenPanel != null && endScreenPanel.activeSelf)
                return;

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (isMenuOpen && hasStartedOnce)
                    CloseMenu();
                else if (!isMenuOpen)
                    OpenMenu(isPause: true);
            }
            else if (isMenuOpen && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
            {
                StartOrResumeGame();
            }
        }

        public void StartOrResumeGame()
        {
            hasStartedOnce = true;
            CloseMenu();
        }

        public void ToggleSound()
        {
            if (GameAudioManager.Instance != null)
                GameAudioManager.Instance.ToggleMute();

            RefreshSoundLabel();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OpenMenu(bool isPause)
        {
            isMenuOpen = true;
            Time.timeScale = 0f;

            if (menuPanel != null)
                menuPanel.SetActive(true);

            if (startButtonLabel != null)
                startButtonLabel.text = isPause ? "LANJUT MAIN" : "MULAI MAIN";
        }

        private void CloseMenu()
        {
            isMenuOpen = false;
            Time.timeScale = 1f;

            if (menuPanel != null)
                menuPanel.SetActive(false);
        }

        private void RefreshSoundLabel()
        {
            if (soundButtonLabel == null)
                return;

            bool muted = GameAudioManager.Instance != null && GameAudioManager.Instance.IsMuted;
            soundButtonLabel.text = muted ? "SUARA SFX: MATI" : "SUARA SFX: NYALA";
        }
    }
}

