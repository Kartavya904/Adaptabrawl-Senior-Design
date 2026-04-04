using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Adaptabrawl.Gameplay;

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

        [Header("Controller / keyboard UI (optional overrides)")]
        [Tooltip("If empty, uses Play → Settings → Quit (main) and Local Play → Online → Back (play options).")]
        [SerializeField] private Selectable[] mainMenuFocusOrder;
        [SerializeField] private Selectable[] playOptionsFocusOrder;

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

            WireMenuNavigation();

            // Show main menu by default
            ShowMainMenu();
        }

        private void WireMenuNavigation()
        {
            Selectable[] main = mainMenuFocusOrder != null && mainMenuFocusOrder.Length > 0
                ? mainMenuFocusOrder
                : new Selectable[] { playButton, settingsButton, quitButton };

            Selectable[] play = playOptionsFocusOrder != null && playOptionsFocusOrder.Length > 0
                ? playOptionsFocusOrder
                : new Selectable[] { localPlayButton, onlineButton, backButton };

            MenuNavigationGroup.ApplyVerticalChain(main, wrap: true);
            MenuNavigationGroup.ApplyVerticalChain(play, wrap: true);
        }

        public void PlayLocal()
        {
            // Init persistent lobby context — carries player names, devices, fighters across scenes
            LobbyContext.EnsureExists().Init(true);
            if (PublicRoomLobbyContext.Instance != null)
                PublicRoomLobbyContext.Instance.SetLanRoomListActive(false);
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
            LobbyContext.EnsureExists().Init(false);
            PublicRoomLobbyContext.EnsureExists().SetLanRoomListActive(true);
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

            Selectable[] play = playOptionsFocusOrder != null && playOptionsFocusOrder.Length > 0
                ? playOptionsFocusOrder
                : new Selectable[] { localPlayButton, onlineButton, backButton };
            MenuNavigationGroup.SelectFirstAvailable(play);
        }

        private void ShowMainMenu()
        {
            if (playOptionsPanel != null)
                playOptionsPanel.SetActive(false);
            if (mainMenuPanel != null)
                mainMenuPanel.SetActive(true);

            Selectable[] main = mainMenuFocusOrder != null && mainMenuFocusOrder.Length > 0
                ? mainMenuFocusOrder
                : new Selectable[] { playButton, settingsButton, quitButton };
            MenuNavigationGroup.SelectFirstAvailable(main);
        }

        private void LateUpdate()
        {
            if (playOptionsPanel == null || !playOptionsPanel.activeSelf) return;
            if (!BackInputUtility.WasBackOrCancelPressedThisFrame()) return;
            if (BackInputUtility.IsTextInputFocused()) return;
            ShowMainMenu();
        }
    }
}
