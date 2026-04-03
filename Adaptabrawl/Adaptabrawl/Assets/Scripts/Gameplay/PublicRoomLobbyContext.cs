using System;
using System.Collections.Generic;
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
            CurrentRoomsChanged?.Invoke(_currentPublicRooms);
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
