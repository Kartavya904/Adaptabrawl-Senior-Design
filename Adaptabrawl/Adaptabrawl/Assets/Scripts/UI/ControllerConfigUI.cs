using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Settings;

namespace Adaptabrawl.UI
{
    public class ControllerConfigUI : MonoBehaviour
    {
        [Header("Scene Manager")]
        [SerializeField] private SetupSceneManager setupManager;

        [Header("Player 1 Controls (Host)")]
        [SerializeField] private TextMeshProUGUI p1ControllerText;
        [SerializeField] private Button p1ToggleBtn;
        [SerializeField] private TextMeshProUGUI p1ToggleBtnText;  // Text child of the toggle button
        [SerializeField] private Button p1ReadyBtn;
        [SerializeField] private TextMeshProUGUI p1ReadyBtnText;   // Text child of the ready button
        [SerializeField] private TextMeshProUGUI p1ReadyText;

        [Header("Player 2 Controls (Client)")]
        [SerializeField] private TextMeshProUGUI p2ControllerText;
        [SerializeField] private Button p2ToggleBtn;
        [SerializeField] private TextMeshProUGUI p2ToggleBtnText;  // Text child of the toggle button
        [SerializeField] private Button p2ReadyBtn;
        [SerializeField] private TextMeshProUGUI p2ReadyBtnText;   // Text child of the ready button
        [SerializeField] private TextMeshProUGUI p2ReadyText;

        [Header("Navigation")]
        [SerializeField] private Button continueButton;  // Legacy: kept hidden, auto-advances on both ready
        [SerializeField] private Button backButton;

        [Header("Phase countdown (optional TMP — 3,2,1 before character select)")]
        [SerializeField] private TextMeshProUGUI controllerPhaseCountdownText;

        [Tooltip("Optional vertical focus order. If empty: P1 toggle → P1 ready → P2 toggle → P2 ready → back.")]
        [SerializeField] private Selectable[] controllerConfigFocusOrder;

        private string[] configOptions = { "Keyboard", "Controller" };

        private void Start()
        {
            if (setupManager == null)
                setupManager = FindFirstObjectByType<SetupSceneManager>();

            controllerPhaseCountdownText = SetupCountdownVisualUtility.EnsureCountdown(
                controllerPhaseCountdownText,
                "ControllerPhaseCountdownText");

            // Button bindings
            if (p1ToggleBtn != null) p1ToggleBtn.onClick.AddListener(() => RequestToggleController(1));
            if (p1ReadyBtn != null) p1ReadyBtn.onClick.AddListener(() => RequestToggleReady(1));

            if (p2ToggleBtn != null) p2ToggleBtn.onClick.AddListener(() => RequestToggleController(2));
            if (p2ReadyBtn != null) p2ReadyBtn.onClick.AddListener(() => RequestToggleReady(2));

            // Continue button: legacy, auto-advances — keep hidden
            if (continueButton != null) continueButton.gameObject.SetActive(false);

            // Back button: only meaningful in local play (online session already committed)
            if (backButton != null)
            {
                bool isLocal = CharacterSelectData.isLocalMatch;
                backButton.gameObject.SetActive(isLocal);
                if (isLocal)
                    backButton.onClick.AddListener(() => setupManager?.GoBackToLocalJoin());
            }

            // Subscribe to Network updates so the UI redraws anytime someone clicks across the world
            if (setupManager != null)
            {
                setupManager.OnControllerConfigChanged += UpdateUI;
                setupManager.OnControllerToCharacterCountdownTick += OnControllerToCharacterCountdownTick;
            }

            UpdateUI();
            WireControllerConfigMenuNavigation();
        }

        private void WireControllerConfigMenuNavigation()
        {
            Selectable[] order = controllerConfigFocusOrder != null && controllerConfigFocusOrder.Length > 0
                ? controllerConfigFocusOrder
                : new Selectable[] { p1ToggleBtn, p1ReadyBtn, p2ToggleBtn, p2ReadyBtn, backButton };

            var list = new System.Collections.Generic.List<Selectable>();
            foreach (var s in order)
            {
                if (s != null) list.Add(s);
            }

            if (list.Count == 0) return;
            MenuNavigationGroup.ApplyVerticalChain(list, wrap: false);
            MenuNavigationGroup.SelectFirstAvailable(list);
        }

        private void OnControllerToCharacterCountdownTick(int n)
        {
            if (controllerPhaseCountdownText == null) return;
            if (n <= 0)
            {
                controllerPhaseCountdownText.gameObject.SetActive(false);
                return;
            }
            controllerPhaseCountdownText.gameObject.SetActive(true);
            controllerPhaseCountdownText.text = n.ToString();
        }

        private void Update()
        {
            if (setupManager == null || setupManager.ControllerPhaseCountdownActive) return;

            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            bool isHost = networked && NetworkManager.Singleton.IsServer;
            bool isLocal = CharacterSelectData.isLocalMatch;
            bool isClient = networked && !isHost && !isLocal;

            int p1Idx = networked ? setupManager.p1ControllerIndex.Value : setupManager.LocalP1ControllerIndex;
            int p2Idx = networked ? setupManager.p2ControllerIndex.Value : setupManager.LocalP2ControllerIndex;
            bool r1 = networked ? setupManager.p1ControllerReady.Value : setupManager.LocalP1ControllerReady;
            bool r2 = networked ? setupManager.p2ControllerReady.Value : setupManager.LocalP2ControllerReady;

            if (!networked)
            {
                if (!r1 && WasReadyPressedForPlayer(1, p1Idx, p2Idx))
                {
                    RequestToggleReady(1);
                    return;
                }

                if (!r2 && WasReadyPressedForPlayer(2, p1Idx, p2Idx))
                    RequestToggleReady(2);

                return;
            }

            if ((isHost || isLocal) && !r1 && WasReadyPressedForPlayer(1, p1Idx, p2Idx))
            {
                RequestToggleReady(1);
                return;
            }

            if ((isClient || isLocal) && !r2 && WasReadyPressedForPlayer(2, p1Idx, p2Idx))
            {
                RequestToggleReady(2);
            }
        }

        private void OnDestroy()
        {
            if (setupManager != null)
            {
                setupManager.OnControllerConfigChanged -= UpdateUI;
                setupManager.OnControllerToCharacterCountdownTick -= OnControllerToCharacterCountdownTick;
            }
        }

        private void RequestToggleController(int targetPlayer)
        {
            if (setupManager == null) return;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                setupManager.ToggleControllerServerRpc(NetworkManager.Singleton.LocalClientId, targetPlayer);
            else
                setupManager.LocalToggleController(targetPlayer);
        }

        private void RequestToggleReady(int targetPlayer)
        {
            if (setupManager == null) return;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                setupManager.ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId, targetPlayer);
            else
                setupManager.LocalToggleReady(targetPlayer);
        }

        private void UpdateUI()
        {
            if (setupManager == null) return;

            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            bool isHost = networked && NetworkManager.Singleton.IsServer;
            bool isLocal = CharacterSelectData.isLocalMatch;
            bool isClient = !isHost && !isLocal;

            // Read state from the right source
            int p1Idx = networked ? setupManager.p1ControllerIndex.Value : setupManager.LocalP1ControllerIndex;
            int p2Idx = networked ? setupManager.p2ControllerIndex.Value : setupManager.LocalP2ControllerIndex;
            bool r1 = networked ? setupManager.p1ControllerReady.Value : setupManager.LocalP1ControllerReady;
            bool r2 = networked ? setupManager.p2ControllerReady.Value : setupManager.LocalP2ControllerReady;
            bool countdown = setupManager.ControllerPhaseCountdownActive;

            // Lock buttons: Host owns P1, Client owns P2, Local owns both
            if (p1ToggleBtn != null) p1ToggleBtn.interactable = (isHost || isLocal) && !r1 && !countdown && setupManager.CanToggleControllerChoice(1, networked);
            if (p1ReadyBtn != null) p1ReadyBtn.interactable = (isHost || isLocal) && !countdown;

            if (p2ToggleBtn != null) p2ToggleBtn.interactable = (isClient || isLocal) && !r2 && !countdown && setupManager.CanToggleControllerChoice(2, networked);
            if (p2ReadyBtn != null) p2ReadyBtn.interactable = (isClient || isLocal) && !countdown;

            // Update display texts
            if (p1ControllerText != null) p1ControllerText.text = configOptions[p1Idx];
            if (p2ControllerText != null) p2ControllerText.text = configOptions[p2Idx];

            // Toggle button labels: show what switching WOULD change TO
            if (p1ToggleBtnText != null)
                p1ToggleBtnText.text = p1Idx == 0
                    ? (setupManager.CanToggleControllerChoice(1, networked) ? "Change to Controller" : "Controller Unavailable")
                    : "Change to Keyboard";
            if (p2ToggleBtnText != null)
                p2ToggleBtnText.text = p2Idx == 0
                    ? (setupManager.CanToggleControllerChoice(2, networked) ? "Change to Controller" : "Controller Unavailable")
                    : "Change to Keyboard";

            if (p1ReadyBtnText != null)
                p1ReadyBtnText.text = LobbySetupInputHints.ControllerReadyButtonRich(r1, p1Idx, true);
            if (p2ReadyBtnText != null)
                p2ReadyBtnText.text = LobbySetupInputHints.ControllerReadyButtonRich(r2, p2Idx, false);

            string p1Dev = LobbySetupInputHints.DeviceLabel(p1Idx);
            string p2Dev = LobbySetupInputHints.DeviceLabel(p2Idx);
            if (p1ReadyText != null)
            {
                p1ReadyText.text = r1 ? $"Ready ({p1Dev})" : $"Not ready ({p1Dev})";
                p1ReadyText.color = r1 ? Color.green : Color.white;
            }

            if (p2ReadyText != null)
            {
                p2ReadyText.text = r2 ? $"Ready ({p2Dev})" : $"Not ready ({p2Dev})";
                p2ReadyText.color = r2 ? Color.green : Color.white;
            }
        }

        private static bool WasReadyPressedForPlayer(int playerNumber, int p1Device, int p2Device)
        {
            var bindings = ControlBindingsContext.EnsureExists();
            if (playerNumber == 1 && p1Device == 1 && LobbyContext.TryGetGamepadForPlayer(1, p1Device, p2Device, out var g1))
                return bindings.WasActionPressedThisFrame(ControlProfileId.GlobalController, ControlActionId.ReadyUp, g1 != null ? LobbyContext.GetGamepadListIndexForPlayer(1, p1Device, p2Device) : -1);

            if (playerNumber == 2 && p2Device == 1 && LobbyContext.TryGetGamepadForPlayer(2, p1Device, p2Device, out var g2))
                return bindings.WasActionPressedThisFrame(ControlProfileId.GlobalController, ControlActionId.ReadyUp, g2 != null ? LobbyContext.GetGamepadListIndexForPlayer(2, p1Device, p2Device) : -1);

            bool dualKeyboard = LobbyContext.IsDualKeyboardMode(p1Device, p2Device);
            var profile = ControlBindingProfileResolver.ResolveGlobalKeyboardProfile(playerNumber, dualKeyboard);
            return bindings.WasActionPressedThisFrame(profile, ControlActionId.ReadyUp);
        }
    }
}
