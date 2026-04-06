using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Networking;
using Unity.Netcode;
using Adaptabrawl.Settings;

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
        [SerializeField] private Button restartButton;
        [SerializeField] private Button changeCharactersButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Controller / keyboard menu")]
        [Tooltip("Vertical focus order. If empty: Resume → Restart → Change Characters → Settings → Main Menu.")]
        [SerializeField] private Selectable[] pauseMenuFocusOrder;

        [Header("Networking (optional — online matches)")]
        [SerializeField] private OnlineMutualPauseCoordinator netCoordinator;

        private float _previousTimeScale = 1f;
        private bool _frozenLocally;
        private OnlineMutualPauseCoordinator _coordinator;
        private bool _pauseMenuWasOpen;

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

        private void LateUpdate()
        {
            if (!IsGameplayScene()) return;

            bool menuOpen = pauseMenuPanel != null && pauseMenuPanel.activeSelf;
            if (menuOpen)
            {
                RestorePauseMenuFocusIfNeeded();
            }
            else if (_pauseMenuWasOpen)
            {
                _pauseMenuWasOpen = false;
            }
        }

        private void RestorePauseMenuFocusIfNeeded()
        {
            if (pauseMenuPanel == null || !pauseMenuPanel.activeSelf) return;
            var es = EventSystem.current;
            if (es == null) return;

            var selected = es.currentSelectedGameObject;
            if (selected != null && selected.transform.IsChildOf(pauseMenuPanel.transform))
                return;

            SetupPauseMenuFocus();
        }

        private void SetupPauseMenuFocus()
        {
            Selectable[] order = pauseMenuFocusOrder != null && pauseMenuFocusOrder.Length > 0
                ? pauseMenuFocusOrder
                : new Selectable[] { resumeButton, restartButton, changeCharactersButton, settingsButton, mainMenuButton };

            order = order.Where(s => s != null).ToArray();
            if (order.Length == 0) return;

            MenuNavigationGroup.ApplyVerticalChain(order, wrap: false);
            MenuNavigationGroup.SelectFirstAvailable(order);
            _pauseMenuWasOpen = true;
        }

        private void WireButtons()
        {
            if (resumeButton != null)
                resumeButton.onClick.AddListener(OnResumeClicked);
            if (restartButton != null)
                restartButton.onClick.AddListener(RestartMatch);
            if (changeCharactersButton != null)
                changeCharactersButton.onClick.AddListener(ChangeCharacters);
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
            return IsGameplayScene()
                && NetworkManager.Singleton != null
                && NetworkManager.Singleton.IsListening
                && !LobbyContext.CurrentMatchIsLocal();
        }

        private bool IsOnlineMutualReady()
        {
            return IsOnlineGameListening() && _coordinator != null && _coordinator.IsSpawned;
        }

        private void HandleLocalInput()
        {
            bool menuVisible = _frozenLocally && pauseMenuPanel != null && pauseMenuPanel.activeSelf;
            if (!PauseOrUnpausePressedThisFrame(menuVisible))
                return;

            if (_frozenLocally)
                ResumeLocalFreeze();
            else
                EnterLocalPause();
        }

        private void HandleOnlineInput()
        {
            bool menuOpen = _coordinator != null && _coordinator.MenuOpen;
            if (!PauseOrUnpausePressedThisFrame(menuOpen))
                return;

            if (_coordinator.MenuOpen)
            {
                _coordinator.RequestResumeFromLocalPlayer();
                return;
            }

            _coordinator.TogglePauseIntentFromLocalPlayer();
        }

        /// <summary>
        /// Escape / Start always; Circle/B only while the pause menu is open so in-match Circle stays combat (dodge).
        /// </summary>
        private static bool PauseOrUnpausePressedThisFrame(bool pauseMenuIsOpen)
        {
            var bindings = ControlBindingsContext.EnsureExists();
            if (bindings.WasActionPressedThisFrame(ControlProfileId.GlobalKeyboardPlayer1, ControlActionId.Pause))
                return true;

            var lobby = LobbyContext.Instance;
            if (lobby != null
                && LobbyContext.IsDualKeyboardMode(lobby.p1InputDevice, lobby.p2InputDevice)
                && bindings.WasActionPressedThisFrame(ControlProfileId.GlobalKeyboardPlayer2, ControlActionId.Pause))
                return true;

            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                if (Gamepad.all[i] == null)
                    continue;

                if (bindings.WasActionPressedThisFrame(ControlProfileId.GlobalController, ControlActionId.Pause, i))
                    return true;

                if (pauseMenuIsOpen && bindings.WasActionPressedThisFrame(ControlProfileId.GlobalController, ControlActionId.BackCancel, i))
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
            {
                pauseMenuPanel.SetActive(true);
                StartCoroutine(FixLayoutNextFrame(pauseMenuPanel));
            }

            SetupPauseMenuFocus();
        }

        private void ResumeLocalFreeze()
        {
            Time.timeScale = Mathf.Approximately(_previousTimeScale, 0f) ? 1f : _previousTimeScale;
            _frozenLocally = false;
            HideAll();
        }

        private System.Collections.IEnumerator FixLayoutNextFrame(GameObject panel)
        {
            if (panel != null)
            {
                var rt = panel.GetComponent<RectTransform>();
                if (rt != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                
                var hovers = panel.GetComponentsInChildren<ButtonHoverFeedback>(true);
                foreach (var h in hovers) h.RecaptureBasePosition();
            }

            yield return new WaitForEndOfFrame();

            if (panel != null)
            {
                var rt = panel.GetComponent<RectTransform>();
                if (rt != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                
                var hovers = panel.GetComponentsInChildren<ButtonHoverFeedback>(true);
                foreach (var h in hovers) h.RecaptureBasePosition();
            }
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

        private void RestartMatch()
        {
            if (IsOnlineMutualReady())
            {
                _coordinator.RequestRestartFromLocalPlayer();
                return;
            }

            TransitionTo(SceneManager.GetActiveScene().name);
        }

        private void ChangeCharacters()
        {
            if (IsOnlineMutualReady())
            {
                _coordinator.RequestChangeCharactersFromLocalPlayer();
                return;
            }

            MatchResultsData.rematchSkipToCharacterSelect = true;
            TransitionTo("SetupScene");
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
                {
                    pauseMenuPanel.SetActive(true);
                    StartCoroutine(FixLayoutNextFrame(pauseMenuPanel));
                }

                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                _frozenLocally = true;
                SetupPauseMenuFocus();
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
                return $"{n1} is requesting a pause.\n\nThe other player must press {GetPauseBindingLabel(2)} to confirm.";
            if (p2 && !p1)
                return $"{n2} is requesting a pause.\n\nThe other player must press {GetPauseBindingLabel(1)} to confirm.";
            return "";
        }

        private static string GetPauseBindingLabel(int playerNumber)
        {
            var bindings = ControlBindingsContext.EnsureExists();
            var lobby = LobbyContext.Instance;
            int p1Device = lobby != null ? lobby.p1InputDevice : 0;
            int p2Device = lobby != null ? lobby.p2InputDevice : 0;
            bool dualKeyboard = LobbyContext.IsDualKeyboardMode(p1Device, p2Device);

            if (playerNumber == 1 && p1Device == 1)
                return bindings.GetDefaultBindingsLabel(ControlProfileId.GlobalController, ControlActionId.Pause);

            if (playerNumber == 2 && p2Device == 1)
                return bindings.GetDefaultBindingsLabel(ControlProfileId.GlobalController, ControlActionId.Pause);

            return bindings.GetDefaultBindingsLabel(
                ControlBindingProfileResolver.ResolveGlobalKeyboardProfile(playerNumber, dualKeyboard),
                ControlActionId.Pause);
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
            QuickMatchSession.Instance?.ClearSession();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                var lobby = FindFirstObjectByType<LobbyManager>();
                if (lobby != null)
                    lobby.Disconnect();
                else
                    NetworkManager.Singleton.Shutdown();
            }

            TransitionTo("StartScene");
        }

        private void TransitionTo(string sceneName)
        {
            Time.timeScale = 1f;
            _frozenLocally = false;
            HideAll();

            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionToScene(sceneName);
            else
                SceneManager.LoadScene(sceneName);
        }
    }
}
