using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using System;
using System.Collections;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.UI
{
    public class LocalJoinUI : MonoBehaviour
    {
        [Header("Scene Manager")]
        [SerializeField] private SetupSceneManager setupManager;

        [Header("UI Elements")]
        [SerializeField] private Button p2JoinButton;
        [SerializeField] private TextMeshProUGUI p1StatusText;
        [SerializeField] private TextMeshProUGUI p2StatusText;
        [SerializeField] private TextMeshProUGUI countdownText;

        private IDisposable inputEventListener;
        private bool p2HasJoined = false;

        private void Start()
        {
            if (setupManager == null)
                setupManager = FindFirstObjectByType<SetupSceneManager>();

            if (p2JoinButton != null)
                p2JoinButton.onClick.AddListener(() => RequestPlayer2Join("Keyboard"));

            UpdateUI(false);
        }

        private void OnEnable()
        {
            // Listen to all input events to dynamically change the UI
            inputEventListener = InputSystem.onAnyButtonPress.Call(OnAnyButtonPressed);
        }

        private void OnDisable()
        {
            inputEventListener?.Dispose();
        }

        private void OnAnyButtonPressed(InputControl control)
        {
            if (p2HasJoined) return;

            bool isGamepad = control.device is Gamepad;
            bool isKeyboard = control.device is Keyboard;

            // Dynamically update the join button text
            if (p2StatusText != null)
            {
                if (isGamepad)
                {
                    p2StatusText.text = "Player 2: Press X to Join";
                }
                else if (isKeyboard)
                {
                    p2StatusText.text = "Player 2: Press Space to Join";
                }
            }

            // Let them join via input
            if (isGamepad && (control.name == "buttonSouth" || control.name == "buttonWest" || control.name == "start")) // A, X, Start
            {
                RequestPlayer2Join("Controller");
            }
            else if (isKeyboard && control.name == "space")
            {
                RequestPlayer2Join("Keyboard");
            }
        }

        private void RequestPlayer2Join(string deviceName = "Keyboard")
        {
            if (p2HasJoined) return;
            if (setupManager == null) return;

            p2HasJoined = true;

            // Map device name to controller index (0=Keyboard, 1=Controller)
            int p2DeviceIndex = deviceName == "Controller" ? 1 : 0;

            // Persist input devices in LobbyContext — P1 is always keyboard at local join stage
            var lobby = LobbyContext.EnsureExists();
            lobby.SetPlayerDisplayNames(lobby.p1Name, lobby.p2Name);
            lobby.SetInputDevices(0, p2DeviceIndex);

            // Tell SetupSceneManager what devices were detected so ControllerConfig is pre-filled correctly
            setupManager.SetLocalDevices(0, p2DeviceIndex); // P1 is always Keyboard on host

            // Update UI visually
            if (p2JoinButton != null) p2JoinButton.gameObject.SetActive(false);

            if (p2StatusText != null)
            {
                p2StatusText.text = $"Player 2: Joined ({deviceName})";
                p2StatusText.color = p1StatusText != null ? p1StatusText.color : Color.green;

                // Match P1's text style (alignment, font size) but keep P2's own position
                if (p1StatusText != null)
                {
                    p2StatusText.alignment = p1StatusText.alignment;
                    p2StatusText.fontSize = p1StatusText.fontSize;
                    p2StatusText.fontStyle = p1StatusText.fontStyle;
                }
            }

            // Start countdown
            StartCoroutine(JoinCountdownRoutine());
        }

        private IEnumerator JoinCountdownRoutine()
        {
            // 3-second countdown before moving to controller config (matches other setup phases).
            for (int i = 3; i > 0; i--)
            {
                if (countdownText != null)
                {
                    countdownText.gameObject.SetActive(true);
                    countdownText.text = i.ToString();
                }
                yield return new WaitForSeconds(1f);
            }

            // Only call the RPC if the network is running; otherwise call ShowControllerConfig directly
            if (setupManager != null)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                    setupManager.ConfirmLocalPlayer2JoinedServerRpc();
                else
                    setupManager.ShowControllerConfig();
            }
        }

        private void UpdateUI(bool p2Joined)
        {
            if (p1StatusText != null)
            {
                p1StatusText.text = "Player 1: Joined (Keyboard)";
                p1StatusText.color = Color.green;
            }

            if (p2StatusText != null)
            {
                if (!p2Joined)
                {
                    // Default string before they press a generic button
                    p2StatusText.color = Color.yellow;
                }
            }

            if (p2JoinButton != null)
            {
                p2JoinButton.gameObject.SetActive(!p2Joined);
            }

            if (countdownText != null && !p2Joined)
            {
                countdownText.gameObject.SetActive(false);
            }
        }
    }
}
