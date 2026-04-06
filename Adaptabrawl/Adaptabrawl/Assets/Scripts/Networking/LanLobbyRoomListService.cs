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
    /// Listens on both <see cref="LanUdpPorts.AllServicePorts"/> because hosts send beacons on the companion of their game port.
    /// </summary>
    public class LanLobbyRoomListService : MonoBehaviour
    {
        [Tooltip("How often the public room list is published (prune stale + notify listeners).")]
        [SerializeField]
        private float refreshIntervalSeconds = 5f;

        /// <summary>Same as the scan / publish interval (default 5s).</summary>
        public float RefreshIntervalSeconds => refreshIntervalSeconds;

        [Tooltip("Drop a room if no beacon received for this long (hosts send every 5s).")]
        [SerializeField]
        private float staleRoomSeconds = 14f;

        private readonly List<Thread> _threads = new List<Thread>();
        private readonly List<UdpClient> _udpClients = new List<UdpClient>();
        private readonly object _udpLock = new object();
        private volatile bool _running;

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
            foreach (var port in LanUdpPorts.AllServicePorts)
            {
                var listenPort = port;
                var t = new Thread(() => ReceiveLoop(listenPort))
                {
                    IsBackground = true,
                    Name = $"LanLobbyRoomList-{listenPort}"
                };
                _threads.Add(t);
                t.Start();
            }

            if (refreshIntervalSeconds > 0.15f)
                InvokeRepeating(nameof(PublishIfChanged), refreshIntervalSeconds, refreshIntervalSeconds);

            PublishIfChanged();
        }

        public void StopScanning()
        {
            _running = false;
            CancelInvoke(nameof(PublishIfChanged));

            lock (_udpLock)
            {
                foreach (var c in _udpClients)
                {
                    try
                    {
                        c?.Close();
                    }
                    catch
                    {
                        // ignored
                    }
                }

                _udpClients.Clear();
            }

            foreach (var t in _threads)
            {
                if (t != null && t.IsAlive)
                    t.Join(1200);
            }

            _threads.Clear();

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

        private void ReceiveLoop(int listenPort)
        {
            UdpClient client = null;
            try
            {
                client = new UdpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.Client.Bind(new IPEndPoint(IPAddress.Any, listenPort));
                client.Client.ReceiveTimeout = 750;

                LanRoomDiscovery.TryJoinMulticastListen(client);

                lock (_udpLock)
                    _udpClients.Add(client);

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
                Debug.LogWarning($"[LanLobbyRoomListService] Receive loop {listenPort} stopped: {ex.Message}");
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
