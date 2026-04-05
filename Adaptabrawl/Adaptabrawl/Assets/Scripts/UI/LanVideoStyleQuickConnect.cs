using System.Collections;
using TMPro;
using UnityEngine;
using Adaptabrawl.Networking;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// Minimal Host + address + Join-as-client strip (same pattern as common Unity Netcode LAN tutorials).
    /// Uses <see cref="LobbyManager"/> so behavior matches the party room: 0.0.0.0 host bind, 7777/7778 clone fallback,
    /// direct IP or 6-digit code after <see cref="LobbyManager.Disconnect"/>.
    /// </summary>
    public class LanVideoStyleQuickConnect : MonoBehaviour
    {
        [SerializeField] private LobbyManager lobbyManager;
        [SerializeField] private TMP_InputField clientAddressInput;
        [SerializeField] private TextMeshProUGUI statusText;

        private void OnEnable()
        {
            if (lobbyManager == null)
                return;
            lobbyManager.OnRoomJoinFailed += OnJoinFailed;
            lobbyManager.OnRoomJoined += ClearStatus;
            lobbyManager.OnRoomCodeGenerated += ClearStatusFromRoomCode;
        }

        private void OnDisable()
        {
            if (lobbyManager == null)
                return;
            lobbyManager.OnRoomJoinFailed -= OnJoinFailed;
            lobbyManager.OnRoomJoined -= ClearStatus;
            lobbyManager.OnRoomCodeGenerated -= ClearStatusFromRoomCode;
        }

        private void OnDestroy()
        {
            if (lobbyManager == null)
                return;
            lobbyManager.OnRoomJoinFailed -= OnJoinFailed;
            lobbyManager.OnRoomJoined -= ClearStatus;
            lobbyManager.OnRoomCodeGenerated -= ClearStatusFromRoomCode;
        }

        private void OnJoinFailed(string msg)
        {
            if (statusText != null)
            {
                statusText.text = msg;
                statusText.color = Color.red;
            }
        }

        private void ClearStatus()
        {
            if (statusText != null)
                statusText.text = "";
        }

        private void ClearStatusFromRoomCode(string _)
        {
            ClearStatus();
        }

        /// <summary>Unity UI → Host (same as tutorial StartHost after transport setup).</summary>
        public void OnClickStartHost()
        {
            if (lobbyManager == null)
                return;
            ClearStatus();
            lobbyManager.CreateRoom();
        }

        /// <summary>Unity UI → leave any local host/client then join remote host (IP:port or 6-digit code).</summary>
        public void OnClickJoinAsClient()
        {
            if (lobbyManager == null || clientAddressInput == null)
                return;

            var raw = clientAddressInput.text.Trim();
            if (string.IsNullOrEmpty(raw))
                return;

            ClearStatus();
            StopAllCoroutines();
            StartCoroutine(CoJoinAfterDisconnect(raw));
        }

        private IEnumerator CoJoinAfterDisconnect(string raw)
        {
            lobbyManager.Disconnect();
            yield return new WaitForSecondsRealtime(0.35f);

            if (LanAddressHints.LooksLikeIpv4WithOptionalPort(raw, out var hostIp, out var hostPort))
            {
                lobbyManager.JoinRoomByDirectIpv4(hostIp, hostPort);
                yield break;
            }

            var code = raw.ToUpperInvariant();
            if (code.Length != 6)
            {
                if (statusText != null)
                {
                    statusText.text =
                        "Use 6-digit room code, or IPv4 like 192.168.1.5:7777 (clone: 127.0.0.1:7777).";
                    statusText.color = Color.red;
                }

                yield break;
            }

            lobbyManager.JoinRoom(code);
        }
    }
}
