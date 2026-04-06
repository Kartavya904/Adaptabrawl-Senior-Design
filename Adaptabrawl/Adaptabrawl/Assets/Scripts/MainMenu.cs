using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Adaptabrawl.Fighters;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Networking;
using System.Collections;

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
        [SerializeField] private UnityEngine.UI.Button quickMatchButton;

        [Header("Controller / keyboard UI (optional overrides)")]
        [Tooltip("If empty, uses Play → Settings → Quit (main) and Local Play → Online → Back (play options).")]
        [SerializeField] private Selectable[] mainMenuFocusOrder;
        [SerializeField] private Selectable[] playOptionsFocusOrder;

        private bool playOnlinePreflightInProgress;

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

            if (quickMatchButton != null)
                quickMatchButton.onClick.AddListener(OpenQuickMatch);

            ApplyStartSceneShadowSilhouettes();
            WireMenuNavigation();

            // Show main menu by default
            ShowMainMenu();
        }

        private static void ApplyStartSceneShadowSilhouettes()
        {
            var decorativePlayers = FindObjectsByType<PlayerController_Platform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var playerController in decorativePlayers)
            {
                if (playerController == null)
                    continue;

                GameObject fighterRoot = playerController.transform.root.gameObject;
                if (fighterRoot == null || !fighterRoot.name.StartsWith("Player_"))
                    continue;

                var shadowVisual = fighterRoot.GetComponent<ShadowSilhouetteVisual>();
                if (shadowVisual == null)
                    shadowVisual = fighterRoot.AddComponent<ShadowSilhouetteVisual>();

                shadowVisual.Configure(FighterFactory.DefaultShadowSilhouetteColor, disableParticleSystems: false, disableTrails: false);
            }
        }

        private void WireMenuNavigation()
        {
            Selectable[] main = mainMenuFocusOrder != null && mainMenuFocusOrder.Length > 0
                ? mainMenuFocusOrder
                : new Selectable[] { playButton, settingsButton, quitButton };

            Selectable[] play = playOptionsFocusOrder != null && playOptionsFocusOrder.Length > 0
                ? playOptionsFocusOrder
                : new Selectable[] { quickMatchButton, localPlayButton, onlineButton, backButton };

            MenuNavigationGroup.ApplyVerticalChain(main, wrap: true);
            MenuNavigationGroup.ApplyVerticalChain(play, wrap: true);
        }

        public void PlayLocal()
        {
            ShutdownAnyExistingOnlineSession();
            QuickMatchSession.Instance?.ClearSession();

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

        private static void ShutdownAnyExistingOnlineSession()
        {
            var lobbyManager = FindFirstObjectByType<LobbyManager>();
            if (lobbyManager != null)
            {
                lobbyManager.Disconnect();
                return;
            }

            if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsListening)
                Unity.Netcode.NetworkManager.Singleton.Shutdown();
        }

        /// <summary>LAN / online party flow (auto-host + join-by-code). Must stay in Build Settings.</summary>
        public const string OnlinePartyRoomSceneName = "OnlinePartyRoomScene";
        public const string QuickMatchSceneName = "QuickMatchScene";

        public void PlayOnline()
        {
            if (playOnlinePreflightInProgress)
                return;

            StartCoroutine(CoPlayOnlineWithLanPreflight());
        }

        private IEnumerator CoPlayOnlineWithLanPreflight()
        {
            playOnlinePreflightInProgress = true;
            SetOnlineButtonState(interactable: false, label: "TESTING...");

            var task = LanConnectivitySelfTest.RunAsync(waitForWindowsFirewallConfirmation: false);
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
                Debug.LogException(task.Exception);
            else
            {
                Debug.Log($"[MainMenu] LAN preflight: {task.Result.Summary} {task.Result.Details}");

                if (ShouldRequestFirewallApproval(task.Result))
                {
                    SetOnlineButtonState(interactable: false, label: "APPROVE...");
                    var approvalTask = LanConnectivitySelfTest.TryEnsureWindowsFirewallAccessAsync();
                    while (!approvalTask.IsCompleted)
                        yield return null;

                    if (!approvalTask.IsFaulted && !approvalTask.IsCanceled && approvalTask.Result)
                    {
                        SetOnlineButtonState(interactable: false, label: "RECHECK...");
                        var rerunTask = LanConnectivitySelfTest.RunAsync(waitForWindowsFirewallConfirmation: false);
                        while (!rerunTask.IsCompleted)
                            yield return null;

                        if (rerunTask.IsFaulted)
                            Debug.LogException(rerunTask.Exception);
                        else
                            Debug.Log($"[MainMenu] LAN preflight after firewall rule update: {rerunTask.Result.Summary} {rerunTask.Result.Details}");
                    }
                    else
                    {
                        SetOnlineButtonState(interactable: true, label: "Play Online");
                        playOnlinePreflightInProgress = false;
                        yield break;
                    }
                }
            }

            SetOnlineButtonState(interactable: true, label: "Play Online");
            playOnlinePreflightInProgress = false;

            QuickMatchSession.Instance?.ClearSession();
            LobbyContext.EnsureExists().Init(false);
            PublicRoomLobbyContext.EnsureExists().SetLanRoomListActive(true);
            CharacterSelectData.isLocalMatch = false;

            if (TryGetBuildIndexBySceneName(OnlinePartyRoomSceneName) >= 0)
                SceneManager.LoadScene(OnlinePartyRoomSceneName);
            else
            {
                Debug.LogWarning(
                    "[MainMenu] OnlinePartyRoomScene is not in Build Settings — falling back to LobbyScene. " +
                    "Run Tools → Adaptabrawl → Setup Online Party Room Scene.");
                SceneManager.LoadScene("LobbyScene");
            }
        }

        private static bool ShouldRequestFirewallApproval(LanConnectivityTestResult result)
        {
            return result.State == LanConnectivityTestState.Warning &&
                   result.FirewallCheckSupported &&
                   !result.PrivateNetworkAllowed;
        }

        private void SetOnlineButtonState(bool interactable, string label)
        {
            if (onlineButton != null)
                onlineButton.interactable = interactable;

            if (onlineButton == null)
                return;

            if (onlineButton.GetComponentInChildren<TextMeshProUGUI>(true) is TextMeshProUGUI tmpText)
            {
                tmpText.text = label;
                return;
            }

            if (onlineButton.GetComponentInChildren<Text>(true) is Text legacyText)
                legacyText.text = label;
        }

        private static int TryGetBuildIndexBySceneName(string sceneName)
        {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrEmpty(path))
                    continue;
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                    return i;
            }

            return -1;
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
                : new Selectable[] { quickMatchButton, localPlayButton, onlineButton, backButton };
            MenuNavigationGroup.SelectFirstAvailable(play);
        }

        public void OpenQuickMatch()
        {
            ShutdownAnyExistingOnlineSession();
            QuickMatchSession.EnsureExists().ClearSession();

            LobbyContext.EnsureExists().Init(true);
            if (PublicRoomLobbyContext.Instance != null)
                PublicRoomLobbyContext.Instance.SetLanRoomListActive(false);

            CharacterSelectData.isLocalMatch = true;
            SceneManager.LoadScene(QuickMatchSceneName);
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
