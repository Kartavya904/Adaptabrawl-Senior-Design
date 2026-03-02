using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using System;

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

        private string[] configOptions = { "Keyboard", "Controller" };
        private IDisposable inputListener;

        private void Start()
        {
            if (setupManager == null)
                setupManager = FindFirstObjectByType<SetupSceneManager>();

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
            }

            UpdateUI();
        }

        private void OnEnable()
        {
            inputListener = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
        }

        private void OnDisable()
        {
            inputListener?.Dispose();
        }

        private void OnAnyButtonPressed(InputControl control)
        {
            if (setupManager == null) return;

            bool networked = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
            int p1Idx = networked ? setupManager.p1ControllerIndex.Value : setupManager.LocalP1ControllerIndex;
            int p2Idx = networked ? setupManager.p2ControllerIndex.Value : setupManager.LocalP2ControllerIndex;
            bool r1 = networked ? setupManager.p1ControllerReady.Value : setupManager.LocalP1ControllerReady;
            bool r2 = networked ? setupManager.p2ControllerReady.Value : setupManager.LocalP2ControllerReady;

            bool isKeyboard = control.device is Keyboard;
            bool isGamepad = control.device is Gamepad;

            // P1 uses Keyboard (index 0): Space to ready
            if (isKeyboard && control.name == "space" && p1Idx == 0 && !r1)
            {
                RequestToggleReady(1);
                return;
            }

            // P2 uses Controller (index 1): Square (buttonWest) or Triangle (buttonNorth) to ready
            if (isGamepad && (control.name == "buttonWest" || control.name == "buttonNorth") && p2Idx == 1 && !r2)
            {
                RequestToggleReady(2);
                return;
            }

            // Edge case: P2 chose Keyboard, so Space also readies P2 (after P1 is already ready)
            if (isKeyboard && control.name == "space" && p2Idx == 0 && r1 && !r2)
            {
                RequestToggleReady(2);
            }
        }

        private void OnDestroy()
        {
            if (setupManager != null)
            {
                setupManager.OnControllerConfigChanged -= UpdateUI;
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

            // Lock buttons: Host owns P1, Client owns P2, Local owns both
            if (p1ToggleBtn != null) p1ToggleBtn.interactable = (isHost || isLocal) && !r1;
            if (p1ReadyBtn != null) p1ReadyBtn.interactable = isHost || isLocal;

            if (p2ToggleBtn != null) p2ToggleBtn.interactable = (isClient || isLocal) && !r2;
            if (p2ReadyBtn != null) p2ReadyBtn.interactable = isClient || isLocal;

            // Update display texts
            if (p1ControllerText != null) p1ControllerText.text = configOptions[p1Idx];
            if (p2ControllerText != null) p2ControllerText.text = configOptions[p2Idx];

            // Toggle button labels: show what switching WOULD change TO
            if (p1ToggleBtnText != null) p1ToggleBtnText.text = p1Idx == 0 ? "Change to Controller" : "Change to Keyboard";
            if (p2ToggleBtnText != null) p2ToggleBtnText.text = p2Idx == 0 ? "Change to Controller" : "Change to Keyboard";

            // Ready button labels: show the input hint based on device, cleared once ready
            string p1ReadyHint = p1Idx == 0 ? "Press Space to ready up" : "Press (Y) to ready up";
            string p2ReadyHint = p2Idx == 0 ? "Press Space to ready up" : "Press (Y) to ready up";

            if (p1ReadyBtnText != null)
                p1ReadyBtnText.text = r1 ? "Ready!" : $"Ready\n<size=70%>{p1ReadyHint}</size>";
            if (p2ReadyBtnText != null)
                p2ReadyBtnText.text = r2 ? "Ready!" : $"Ready\n<size=70%>{p2ReadyHint}</size>";

            if (p1ReadyText != null)
            {
                p1ReadyText.text = r1 ? "Ready" : "Not Ready";
                p1ReadyText.color = r1 ? Color.green : Color.white;
            }

            if (p2ReadyText != null)
            {
                p2ReadyText.text = r2 ? "Ready" : "Not Ready";
                p2ReadyText.color = r2 ? Color.green : Color.white;
            }
        }
    }
}
