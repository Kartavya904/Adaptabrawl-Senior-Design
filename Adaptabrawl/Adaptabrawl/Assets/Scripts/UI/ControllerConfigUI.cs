using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

namespace Adaptabrawl.UI
{
    public class ControllerConfigUI : MonoBehaviour
    {
        [Header("Scene Manager")]
        [SerializeField] private SetupSceneManager setupManager;

        [Header("Player 1 Controls (Host)")]
        [SerializeField] private TextMeshProUGUI p1ControllerText;
        [SerializeField] private Button p1ToggleBtn;
        [SerializeField] private Button p1ReadyBtn;
        [SerializeField] private TextMeshProUGUI p1ReadyText;

        [Header("Player 2 Controls (Client)")]
        [SerializeField] private TextMeshProUGUI p2ControllerText;
        [SerializeField] private Button p2ToggleBtn;
        [SerializeField] private Button p2ReadyBtn;
        [SerializeField] private TextMeshProUGUI p2ReadyText;

        [Header("Navigation")]
        [SerializeField] private Button continueButton;

        private string[] configOptions = { "Keyboard", "Gamepad" };

        private void Start()
        {
            if (setupManager == null)
                setupManager = FindFirstObjectByType<SetupSceneManager>();

            // Button bindings
            if (p1ToggleBtn != null) p1ToggleBtn.onClick.AddListener(RequestToggleController);
            if (p1ReadyBtn != null) p1ReadyBtn.onClick.AddListener(RequestToggleReady);

            if (p2ToggleBtn != null) p2ToggleBtn.onClick.AddListener(RequestToggleController);
            if (p2ReadyBtn != null) p2ReadyBtn.onClick.AddListener(RequestToggleReady);

            // Hide the continue button since the server automatically moves us forward now
            if (continueButton != null) continueButton.gameObject.SetActive(false);

            // Subscribe to Network updates so the UI redraws anytime someone clicks across the world
            if (setupManager != null)
            {
                setupManager.OnControllerConfigChanged += UpdateUI;
            }

            UpdateUI();
        }

        private void OnDestroy()
        {
            if (setupManager != null)
            {
                setupManager.OnControllerConfigChanged -= UpdateUI;
            }
        }

        private void RequestToggleController()
        {
            if (setupManager != null && NetworkManager.Singleton != null)
            {
                setupManager.ToggleControllerServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        private void RequestToggleReady()
        {
            if (setupManager != null && NetworkManager.Singleton != null)
            {
                setupManager.ToggleReadyServerRpc(NetworkManager.Singleton.LocalClientId);
            }
        }

        private void UpdateUI()
        {
            if (setupManager == null) return;
            if (NetworkManager.Singleton == null) return;

            bool isHost = NetworkManager.Singleton.IsServer;
            bool isClient = !isHost;

            // Lock the buttons, so only the Host can click Player 1, and only Client can click Player 2
            if (p1ToggleBtn != null) p1ToggleBtn.interactable = isHost && !setupManager.p1ControllerReady.Value;
            if (p1ReadyBtn != null) p1ReadyBtn.interactable = isHost;

            if (p2ToggleBtn != null) p2ToggleBtn.interactable = isClient && !setupManager.p2ControllerReady.Value;
            if (p2ReadyBtn != null) p2ReadyBtn.interactable = isClient;

            // Update UI Texts based strictly on the absolute truth of the Server NetworkVariables
            if (p1ControllerText != null) p1ControllerText.text = configOptions[setupManager.p1ControllerIndex.Value];
            if (p2ControllerText != null) p2ControllerText.text = configOptions[setupManager.p2ControllerIndex.Value];

            bool r1 = setupManager.p1ControllerReady.Value;
            bool r2 = setupManager.p2ControllerReady.Value;

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
