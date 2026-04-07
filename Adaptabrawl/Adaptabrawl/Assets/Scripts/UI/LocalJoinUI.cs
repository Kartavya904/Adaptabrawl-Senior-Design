using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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
                p2JoinButton.onClick.AddListener(() => RequestPlayer2Join(GetDefaultP2JoinDeviceName()));

            if (p2JoinButton != null && EventSystem.current != null)
                MenuNavigationGroup.SelectFirstAvailable(new Selectable[] { p2JoinButton });

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

            UpdateUI(false);

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

            int p1DeviceIndex = GetDefaultP1DeviceIndex();
            int p2DeviceIndex = deviceName == "Controller" ? 1 : 0;

            var lobby = LobbyContext.EnsureExists();
            lobby.SetPlayerDisplayNames(lobby.p1Name, lobby.p2Name);
            lobby.SetInputDevices(p1DeviceIndex, p2DeviceIndex);

            setupManager.SetLocalDevices(p1DeviceIndex, p2DeviceIndex);

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
                p1StatusText.text = $"Player 1: Joined ({GetDeviceLabel(GetDefaultP1DeviceIndex())})";
                p1StatusText.color = Color.green;
            }

            if (p2StatusText != null)
            {
                if (!p2Joined)
                {
                    p2StatusText.text = GetDefaultP2Prompt();
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

        private int GetDefaultP1DeviceIndex()
        {
            return LobbyContext.ConnectedGamepadCount() >= 2 ? 1 : 0;
        }

        private string GetDefaultP2JoinDeviceName()
        {
            return LobbyContext.ConnectedGamepadCount() >= 2 ? "Controller" : "Keyboard";
        }

        private string GetDefaultP2Prompt()
        {
            return LobbyContext.ConnectedGamepadCount() >= 2
                ? "Player 2: Press X to Join"
                : "Player 2: Press Space to Join";
        }

        private static string GetDeviceLabel(int deviceIndex)
        {
            return deviceIndex == 1 ? "Controller" : "Keyboard";
        }
    }
}
