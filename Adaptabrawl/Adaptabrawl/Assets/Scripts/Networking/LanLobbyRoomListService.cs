using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace Adaptabrawl.Networking
{
    /// <summary>One row from a periodic LAN beacon (room code + how to connect).</summary>
    public readonly struct LanAdvertisedRoom : IEquatable<LanAdvertisedRoom>
    {
        public string RoomCode { get; }
        public string HostIpv4 { get; }
        public ushort GamePort { get; }

        public LanAdvertisedRoom(string roomCode, string hostIpv4, ushort gamePort)
        {
            RoomCode = roomCode;
            HostIpv4 = hostIpv4;
            GamePort = gamePort;
        }

        public bool Equals(LanAdvertisedRoom other) =>
            RoomCode == other.RoomCode && HostIpv4 == other.HostIpv4 && GamePort == other.GamePort;
    }

    /// <summary>
    /// Lives on <see cref="Gameplay.PublicRoomLobbyContext"/> (DontDestroyOnLoad). Listens for LAN room beacons and
    /// every few seconds publishes an updated list of advertised room codes on the same Wi‑Fi.
    /// </summary>
    public class LanLobbyRoomListService : MonoBehaviour
    {
        [Tooltip("Must match <see cref=\"LanRoomDiscovery.DefaultBeaconPort\"/> and host beacon sends.")]
        [SerializeField]
        private int beaconListenPort = LanRoomDiscovery.DefaultBeaconPort;

        [Tooltip("How often the public room list is published (prune stale + notify listeners).")]
        [SerializeField]
        private float refreshIntervalSeconds = 5f;

        /// <summary>Same as the scan / publish interval (default 5s).</summary>
        public float RefreshIntervalSeconds => refreshIntervalSeconds;

        [Tooltip("Drop a room if no beacon received for this long (hosts send every 5s).")]
        [SerializeField]
        private float staleRoomSeconds = 14f;

        private Thread _thread;
        private volatile bool _running;
        private UdpClient _udp;

        private readonly object _lock = new object();
        private readonly Dictionary<string, (LanAdvertisedRoom entry, DateTime lastSeenUtc)> _byCode =
            new Dictionary<string, (LanAdvertisedRoom, DateTime)>();

        /// <summary>Fired on the Unity main thread every <see cref="refreshIntervalSeconds"/> with the current list (may be unchanged).</summary>
        public event Action<IReadOnlyList<LanAdvertisedRoom>> OnRoomsUpdated;

        private void OnDestroy()
        {
            StopScanning();
        }

        public void StartScanning()
        {
            if (_running)
                return;

            _running = true;
            _thread = new Thread(ReceiveLoop)
            {
                IsBackground = true,
                Name = "LanLobbyRoomList"
            };
            _thread.Start();

            if (refreshIntervalSeconds > 0.15f)
                InvokeRepeating(nameof(PublishIfChanged), refreshIntervalSeconds, refreshIntervalSeconds);

            PublishIfChanged();
        }

        public void StopScanning()
        {
            _running = false;
            CancelInvoke(nameof(PublishIfChanged));

            try
            {
                _udp?.Close();
            }
            catch
            {
                // ignored
            }

            _udp = null;
            if (_thread != null && _thread.IsAlive)
                _thread.Join(1200);
            _thread = null;

            lock (_lock)
                _byCode.Clear();

            OnRoomsUpdated?.Invoke(Array.Empty<LanAdvertisedRoom>());
        }

        /// <summary>Thread-safe snapshot after pruning stale entries.</summary>
        public IReadOnlyList<LanAdvertisedRoom> GetSnapshot()
        {
            lock (_lock)
            {
                PruneStaleUnsafe();
                return BuildSortedListUnsafe();
            }
        }

        /// <summary>Force a main-thread refresh (e.g. when opening the join panel).</summary>
        public void RequestRefresh()
        {
            PublishIfChanged();
        }

        private void ReceiveLoop()
        {
            try
            {
                var client = new UdpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(new IPEndPoint(IPAddress.Any, beaconListenPort));
                client.Client.ReceiveTimeout = 750;

                LanRoomDiscovery.TryJoinMulticastListen(client);

                _udp = client;

                while (_running)
                {
                    try
                    {
                        var remote = new IPEndPoint(IPAddress.Any, 0);
                        byte[] data;
                        try
                        {
                            data = client.Receive(ref remote);
                        }
                        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                        {
                            continue;
                        }

                        var text = Encoding.UTF8.GetString(data).Trim();
                        if (!LanRoomDiscovery.TryParseBeacon(text, out var code, out var ip, out var port))
                            continue;

                        lock (_lock)
                        {
                            var entry = new LanAdvertisedRoom(code, ip, port);
                            _byCode[code] = (entry, DateTime.UtcNow);
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (SocketException)
                    {
                        // transient
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LanLobbyRoomListService] Receive loop stopped: {ex.Message}");
            }
        }

        private void PruneStaleUnsafe()
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-staleRoomSeconds);
            var remove = new List<string>();
            foreach (var kv in _byCode)
            {
                if (kv.Value.lastSeenUtc < cutoff)
                    remove.Add(kv.Key);
            }

            foreach (var k in remove)
                _byCode.Remove(k);
        }

        private List<LanAdvertisedRoom> BuildSortedListUnsafe()
        {
            var list = new List<LanAdvertisedRoom>(_byCode.Count);
            foreach (var kv in _byCode.Values)
                list.Add(kv.entry);
            list.Sort((a, b) => string.CompareOrdinal(a.RoomCode, b.RoomCode));
            return list;
        }

        private void PublishIfChanged()
        {
            List<LanAdvertisedRoom> snapshot;
            lock (_lock)
            {
                PruneStaleUnsafe();
                snapshot = BuildSortedListUnsafe();
            }

            OnRoomsUpdated?.Invoke(snapshot);
        }
    }
}
