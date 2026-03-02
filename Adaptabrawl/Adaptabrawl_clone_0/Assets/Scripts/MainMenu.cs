using UnityEngine;
using UnityEngine.SceneManagement;

namespace Adaptabrawl.UI
{
    public class MainMenu : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject playOptionsPanel;

        [Header("Buttons")]
        [SerializeField] private UnityEngine.UI.Button playButton;
        [SerializeField] private UnityEngine.UI.Button onlineButton;
        [SerializeField] private UnityEngine.UI.Button settingsButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;
        [SerializeField] private UnityEngine.UI.Button backButton;
        [SerializeField] private UnityEngine.UI.Button localPlayButton;

        private void Start()
        {
            // Setup button listeners
            if (playButton != null)
                playButton.onClick.AddListener(ShowPlayOptions);

            if (onlineButton != null)
                onlineButton.onClick.AddListener(PlayOnline);

            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);

            if (quitButton != null)
                quitButton.onClick.AddListener(QuitGame);

            if (backButton != null)
                backButton.onClick.AddListener(ShowMainMenu);

            if (localPlayButton != null)
                localPlayButton.onClick.AddListener(PlayLocal);

            // Show main menu by default
            ShowMainMenu();
        }

        public void PlayLocal()
        {
            // Set flag for local match so Setup UI knows to unlock P2 controls
            CharacterSelectData.isLocalMatch = true;

            // Start local host
            if (Unity.Netcode.NetworkManager.Singleton != null)
            {
                Unity.Netcode.NetworkManager.Singleton.StartHost();
                Unity.Netcode.NetworkManager.Singleton.SceneManager.LoadScene("SetupScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                // Fallback if no network manager (unlikely)
                SceneManager.LoadScene("SetupScene");
            }
        }

        public void PlayOnline()
        {
            // Reset local match flag just in case it carried over
            CharacterSelectData.isLocalMatch = false;
            // Load lobby scene for online play
            SceneManager.LoadScene("LobbyScene");
        }

        public void OpenSettings()
        {
            SceneManager.LoadScene("SettingsScene");
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
        }

        private void ShowPlayOptions()
        {
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(false);
            if (playOptionsPanel != null)
                playOptionsPanel.SetActive(true);
        }

        private void ShowMainMenu()
        {
            if (playOptionsPanel != null)
                playOptionsPanel.SetActive(false);
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);
        }
    }
}
