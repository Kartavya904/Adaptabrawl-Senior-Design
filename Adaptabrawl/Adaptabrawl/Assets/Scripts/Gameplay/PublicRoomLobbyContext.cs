using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Adaptabrawl.Networking;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Separate from <see cref="LobbyContext"/>. DontDestroyOnLoad holder for LAN public-room discovery.
    /// Keeps <see cref="CurrentPublicRooms"/> in sync: <see cref="LanLobbyRoomListService"/> rescans the network
    /// every <see cref="PublicRoomScanIntervalSeconds"/> (default 5) and updates this list.
    /// </summary>
    public class PublicRoomLobbyContext : MonoBehaviour
    {
        public static PublicRoomLobbyContext Instance { get; private set; }

        private readonly List<LanAdvertisedRoom> _currentPublicRooms = new List<LanAdvertisedRoom>();
        private LanLobbyRoomListService _roomListService;

        /// <summary>Last published list of rooms heard on the LAN (updated every scan tick).</summary>
        public IReadOnlyList<LanAdvertisedRoom> CurrentPublicRooms => _currentPublicRooms;

        /// <summary>UTC time of the last scan publish (every ~5s while online).</summary>
        public DateTime LastPublicRoomScanUtc { get; private set; }

        /// <summary>Interval in seconds between list refreshes (from <see cref="LanLobbyRoomListService"/>).</summary>
        public float PublicRoomScanIntervalSeconds =>
            _roomListService != null ? _roomListService.RefreshIntervalSeconds : 5f;

        /// <summary>Fired on the main thread whenever the stored list is refreshed.</summary>
        public event Action<IReadOnlyList<LanAdvertisedRoom>> CurrentRoomsChanged;

        public LanLobbyRoomListService RoomListService => _roomListService;

        [Header("Debug (builds & editor)")]
        [Tooltip("Draws a small overlay: local IPv4s, multicast group, beacon port, and how many LAN rooms were heard.")]
        [SerializeField]
        private bool showLanDebugOverlay;

        [Tooltip("Logs to the Unity Console whenever the LAN room list updates.")]
        [SerializeField]
        private bool logLanRoomUpdatesToConsole;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _roomListService = GetComponent<LanLobbyRoomListService>();
            if (_roomListService == null)
                _roomListService = gameObject.AddComponent<LanLobbyRoomListService>();

            _roomListService.OnRoomsUpdated += HandleRoomListUpdated;
        }

        private void OnDestroy()
        {
            if (_roomListService != null)
                _roomListService.OnRoomsUpdated -= HandleRoomListUpdated;
        }

        private void HandleRoomListUpdated(IReadOnlyList<LanAdvertisedRoom> rooms)
        {
            _currentPublicRooms.Clear();
            if (rooms != null && rooms.Count > 0)
                _currentPublicRooms.AddRange(rooms);

            LastPublicRoomScanUtc = DateTime.UtcNow;

            if (logLanRoomUpdatesToConsole)
            {
                var primary = LanAddressHints.GetPrimaryLanIpv4();
                if (_currentPublicRooms.Count == 0)
                    Debug.Log($"[PublicRoomLobbyContext] LAN list: 0 rooms (primary IPv4: {primary}). " +
                              $"Beacon UDP {LanRoomDiscovery.DefaultBeaconPort}, multicast {LanRoomDiscovery.LanMulticastGroup}. " +
                              "If this stays empty on another PC, check Windows firewall (UDP inbound on host) and router client isolation.");
                else
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"[PublicRoomLobbyContext] LAN list: {_currentPublicRooms.Count} room(s), primary IPv4: {primary}");
                    foreach (var r in _currentPublicRooms)
                        sb.AppendLine($"  {r.RoomCode} -> {r.HostIpv4}:{r.GamePort}");
                    Debug.Log(sb.ToString().TrimEnd());
                }
            }

            CurrentRoomsChanged?.Invoke(_currentPublicRooms);
        }

        private void OnGUI()
        {
            if (!showLanDebugOverlay)
                return;

            const float w = 420f;
            var area = new Rect(10f, 10f, w, 220f);
            GUI.Box(area, "Adaptabrawl LAN debug (PublicRoomLobbyContext)");

            var sb = new StringBuilder();
            sb.AppendLine($"Rooms heard: {_currentPublicRooms.Count}  (last tick {LastPublicRoomScanUtc:HH:mm:ss} UTC)");
            sb.AppendLine($"Primary IPv4: {LanAddressHints.GetPrimaryLanIpv4()}");
            sb.AppendLine($"Multicast: {LanRoomDiscovery.LanMulticastGroup}  beacon port: {LanRoomDiscovery.DefaultBeaconPort}");
            sb.AppendLine("Local IPv4s:");
            var listed = 0;
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;
                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (listed >= 12)
                        break;
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    if (IPAddress.IsLoopback(ua.Address))
                        continue;
                    sb.AppendLine($"  {ua.Address}");
                    listed++;
                }

                if (listed >= 12)
                    break;
            }

            if (listed >= 12)
                sb.AppendLine("  …");

            GUI.Label(new Rect(20f, 35f, w - 20f, 190f), sb.ToString());
        }

        public static PublicRoomLobbyContext EnsureExists()
        {
            if (Instance != null)
                return Instance;
            var go = new GameObject("PublicRoomLobbyContext");
            return go.AddComponent<PublicRoomLobbyContext>();
        }

        /// <summary>Start/stop listening for LAN room beacons (online vs local).</summary>
        public void SetLanRoomListActive(bool active)
        {
            if (_roomListService == null)
                return;

            if (active)
                _roomListService.StartScanning();
            else
                _roomListService.StopScanning();
        }

        /// <summary>Human-readable lines for UI (code → ip:port).</summary>
        public static string FormatRoomsForDisplay(IReadOnlyList<LanAdvertisedRoom> rooms, float scanIntervalSeconds)
        {
            if (rooms == null || rooms.Count == 0)
                return $"LAN rooms (scan every {scanIntervalSeconds:0}s): none heard yet.";

            var lines = new System.Text.StringBuilder();
            lines.AppendLine($"LAN rooms (scan every {scanIntervalSeconds:0}s):");
            foreach (var r in rooms)
                lines.AppendLine($"  {r.RoomCode}  →  {r.HostIpv4}:{r.GamePort}");
            return lines.ToString().TrimEnd();
        }

        /// <summary>Runs an immediate publish (same as the periodic tick).</summary>
        public void RequestRoomListRefresh()
        {
            _roomListService?.RequestRefresh();
        }
    }
}
