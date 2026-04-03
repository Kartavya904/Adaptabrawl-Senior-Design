using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Networking;
using Unity.Netcode;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// In-scene pause overlay (not a separate scene). Local: Esc or any gamepad Options/Start pauses immediately.
    /// Online (<see cref="OnlineMutualPauseCoordinator"/>): both players must press pause before time freezes and the menu appears.
    /// </summary>
    public class MatchPauseController : MonoBehaviour
    {
        public static MatchPauseController Instance { get; private set; }

        [Header("Roots")]
        [SerializeField] private GameObject pauseOverlayRoot;
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private GameObject pauseRequestPanel;

        [Header("Copy")]
        [SerializeField] private TextMeshProUGUI pauseRequestMessage;
        [SerializeField] private TextMeshProUGUI pausedTitleText;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Networking (optional — OnlineGameScene)")]
        [SerializeField] private OnlineMutualPauseCoordinator netCoordinator;

        private float _previousTimeScale = 1f;
        private bool _frozenLocally;
        private OnlineMutualPauseCoordinator _coordinator;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (_frozenLocally)
                Time.timeScale = Mathf.Approximately(_previousTimeScale, 0f) ? 1f : _previousTimeScale;
        }

        private void Start()
        {
            if (netCoordinator != null)
                _coordinator = netCoordinator;
            else
                _coordinator = FindFirstObjectByType<OnlineMutualPauseCoordinator>();

            WireButtons();
            HideAll();

            if (pausedTitleText != null)
                pausedTitleText.text = "Paused";
        }

        private void WireButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);
            if (settingsButton != null)
                settingsButton.onClick.AddListener(OpenSettings);
            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        private void Update()
        {
            if (!IsGameplayScene())
                return;

            if (IsOnlineGameListening())
            {
                if (_coordinator == null)
                    _coordinator = FindFirstObjectByType<OnlineMutualPauseCoordinator>();
                if (_coordinator != null && _coordinator.IsSpawned)
                {
                    HandleOnlineInput();
                    return;
                }

                return;
            }

            HandleLocalInput();
        }

        private static bool IsGameplayScene()
        {
            var n = SceneManager.GetActiveScene().name;
            return n == "GameScene" || n == "OnlineGameScene" || n == "TestCharacter";
        }

        private static bool IsOnlineGameListening()
        {
            if (SceneManager.GetActiveScene().name != "OnlineGameScene")
                return false;
            return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        }

        private bool IsOnlineMutualReady()
        {
            return IsOnlineGameListening() && _coordinator != null && _coordinator.IsSpawned;
        }

        private void HandleLocalInput()
        {
            if (PausePressedThisFrame())
            {
                if (_frozenLocally)
                    ResumeLocalFreeze();
                else
                    EnterLocalPause();
            }
        }

        private void HandleOnlineInput()
        {
            if (!PausePressedThisFrame())
                return;

            if (_coordinator.MenuOpen)
            {
                _coordinator.RequestResumeFromLocalPlayer();
                return;
            }

            _coordinator.TogglePauseIntentFromLocalPlayer();
        }

        private static bool PausePressedThisFrame()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                return true;

            foreach (var pad in Gamepad.all)
            {
                if (pad != null && pad.startButton.wasPressedThisFrame)
                    return true;
            }

            return false;
        }

        private void EnterLocalPause()
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _frozenLocally = true;
            if (pauseOverlayRoot != null)
                pauseOverlayRoot.SetActive(true);
            if (pauseRequestPanel != null)
                pauseRequestPanel.SetActive(false);
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(true);
        }

        private void ResumeLocalFreeze()
        {
            Time.timeScale = Mathf.Approximately(_previousTimeScale, 0f) ? 1f : _previousTimeScale;
            _frozenLocally = false;
            HideAll();
        }

        private void OnResumeClicked()
        {
            if (IsOnlineMutualReady())
            {
                _coordinator.RequestResumeFromLocalPlayer();
                return;
            }

            ResumeLocalFreeze();
        }

        /// <summary>Called by <see cref="OnlineMutualPauseCoordinator"/> when mutual pause UI should update.</summary>
        public void ApplyOnlineCoordinatorState(bool menuOpen, byte pauseRequests)
        {
            if (pauseOverlayRoot == null)
                return;

            if (menuOpen)
            {
                pauseOverlayRoot.SetActive(true);
                if (pauseRequestPanel != null)
                    pauseRequestPanel.SetActive(false);
                if (pauseMenuPanel != null)
                    pauseMenuPanel.SetActive(true);

                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                _frozenLocally = true;
                return;
            }

            if (pauseRequests != 0)
            {
                pauseOverlayRoot.SetActive(true);
                if (pauseMenuPanel != null)
                    pauseMenuPanel.SetActive(false);
                if (pauseRequestPanel != null)
                    pauseRequestPanel.SetActive(true);

                if (pauseRequestMessage != null)
                    pauseRequestMessage.text = BuildPendingMessage(pauseRequests);

                if (_frozenLocally)
                {
                    Time.timeScale = Mathf.Approximately(_previousTimeScale, 0f) ? 1f : _previousTimeScale;
                    _frozenLocally = false;
                }
                return;
            }

            if (_frozenLocally)
            {
                Time.timeScale = Mathf.Approximately(_previousTimeScale, 0f) ? 1f : _previousTimeScale;
                _frozenLocally = false;
            }

            HideAll();
        }

        private static string BuildPendingMessage(byte pauseRequests)
        {
            bool p1 = (pauseRequests & 1) != 0;
            bool p2 = (pauseRequests & 2) != 0;
            var lobby = LobbyContext.Instance;
            string n1 = lobby != null ? lobby.p1Name : "Player 1";
            string n2 = lobby != null ? lobby.p2Name : "Player 2";

            if (p1 && !p2)
                return $"{n1} is requesting a pause.\n\nThe other player must press Esc or the controller Options/Start button to confirm.";
            if (p2 && !p1)
                return $"{n2} is requesting a pause.\n\nThe other player must press Esc or the controller Options/Start button to confirm.";
            return "";
        }

        private void HideAll()
        {
            if (pauseOverlayRoot != null)
                pauseOverlayRoot.SetActive(false);
            if (pauseMenuPanel != null)
                pauseMenuPanel.SetActive(false);
            if (pauseRequestPanel != null)
                pauseRequestPanel.SetActive(false);
        }

        private void OpenSettings()
        {
            PlayerPrefs.SetString("PreviousScene", SceneManager.GetActiveScene().name);
            PlayerPrefs.SetInt("WasPaused", _frozenLocally ? 1 : 0);
            Time.timeScale = 1f;
            SceneManager.LoadScene("SettingsScene");
        }

        private void ReturnToMainMenu()
        {
            Time.timeScale = 1f;
            _frozenLocally = false;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                var lobby = FindFirstObjectByType<LobbyManager>();
                if (lobby != null)
                    lobby.Disconnect();
                else
                    NetworkManager.Singleton.Shutdown();
            }

            SceneManager.LoadScene("StartScene");
        }
    }
}
