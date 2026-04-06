using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Adaptabrawl.Networking
{
    /// <summary>
    /// Same-LAN room discovery: UDP broadcast + administratively-scoped multicast (239.x).
    /// Multicast often survives Wi‑Fi ↔ Ethernet paths where raw broadcast is filtered.
    /// </summary>
    public static class LanRoomDiscovery
    {
        private const string Prefix = "ABDISC|";

        /// <summary>Fixed group for Adaptabrawl LAN discovery / beacons (not routed to the internet).</summary>
        public static readonly IPAddress LanMulticastGroup = IPAddress.Parse("239.255.192.177");

        private static Thread s_HostThread;
        private static volatile bool s_HostRunning;
        private static UdpClient s_HostUdp;

        /// <summary>Host listens for FIND and sends beacons on the companion of <paramref name="gamePort"/> (7777 ↔ 7778 only).</summary>
        public static void StartHostResponder(string roomCode, ushort gamePort, int companionServiceUdpPort)
        {
            StopHostResponder();
            s_HostRunning = true;
            var normalizedCode = NormalizeCode(roomCode);
            s_HostThread = new Thread(() => HostThreadProc(normalizedCode, gamePort, companionServiceUdpPort))
            {
                IsBackground = true,
                Name = "LanRoomDiscovery-Host"
            };
            s_HostThread.Start();
        }

        public static void StopHostResponder()
        {
            s_HostRunning = false;
            try
            {
                s_HostUdp?.Close();
            }
            catch
            {
                // ignored
            }

            s_HostUdp = null;
            if (s_HostThread != null && s_HostThread.IsAlive)
                s_HostThread.Join(1500);
            s_HostThread = null;
        }

        private static void HostThreadProc(string roomCode, ushort gamePort, int companionServiceUdpPort)
        {
            try
            {
                UdpClient tryClient = null;
                try
                {
                    tryClient = new UdpClient();
                    tryClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    tryClient.Client.Bind(new IPEndPoint(IPAddress.Any, companionServiceUdpPort));
                    tryClient.Client.ReceiveTimeout = 500;
                    s_HostUdp = tryClient;
                    tryClient = null;
                }
                catch (SocketException ex)
                {
                    try
                    {
                        tryClient?.Close();
                    }
                    catch
                    {
                        // ignored
                    }

                    Debug.LogError(
                        $"[LanRoomDiscovery] Could not bind discovery/beacon UDP *:{companionServiceUdpPort} ({ex.SocketErrorCode}). " +
                        "Another Adaptabrawl window may be using that port, or the companion port is still held by the OS.");
                    return;
                }

                if (s_HostUdp == null)
                    return;

                TryJoinMulticastListen(s_HostUdp);

                var lastBeaconUtc = DateTime.MinValue;
                var beaconInterval = TimeSpan.FromSeconds(5);
                var advertisedIp = LanAddressHints.GetPrimaryLanIpv4();
                if (string.IsNullOrEmpty(advertisedIp))
                    advertisedIp = "127.0.0.1";

                Debug.Log(
                    $"[LanRoomDiscovery] Host listening UDP *:{companionServiceUdpPort} (FIND + beacons; game {gamePort}) + multicast {LanMulticastGroup} (room {roomCode}). " +
                    "Clients send FIND to both 7777 and 7778. If joins fail, allow UDP in Windows Firewall.");

                while (s_HostRunning)
                {
                    try
                    {
                        if (DateTime.UtcNow - lastBeaconUtc >= beaconInterval)
                        {
                            SendLanRoomBeaconPayload(roomCode, advertisedIp, gamePort, companionServiceUdpPort);
                            lastBeaconUtc = DateTime.UtcNow;
                        }

                        // ref endpoint must not be null on all runtimes (IL2CPP / modern .NET).
                        var remote = new IPEndPoint(IPAddress.Any, 0);
                        byte[] data;
                        try
                        {
                            data = s_HostUdp.Receive(ref remote);
                        }
                        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                        {
                            continue;
                        }

                        var text = Encoding.UTF8.GetString(data).Trim();
                        if (!TryParseFind(text, out var requestedCode))
                            continue;
                        if (!string.Equals(requestedCode, roomCode, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var replyIp = GetOutboundIpv4To(remote.Address);
                        var response = Encoding.UTF8.GetBytes($"{Prefix}HOST|{replyIp}|{gamePort}|{roomCode}\n");
                        s_HostUdp.Send(response, response.Length, remote);
                        Debug.Log($"[LanRoomDiscovery] Replied to {remote} → connect game at {replyIp}:{gamePort}");
                    }
                    catch (SocketException)
                    {
                        // transient
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LanRoomDiscovery] Host responder stopped: {e.Message}");
            }
        }

        private static bool TryParseFind(string text, out string code)
        {
            code = null;
            if (string.IsNullOrEmpty(text))
                return false;
            const string find = "FIND|";
            if (!text.StartsWith(Prefix, StringComparison.Ordinal))
                return false;
            var rest = text.Substring(Prefix.Length);
            if (!rest.StartsWith(find, StringComparison.Ordinal))
                return false;
            code = NormalizeCode(rest.Substring(find.Length));
            return code.Length > 0;
        }

        /// <summary>BEACON|roomCode|hostIpv4|gamePort — for LAN room browser.</summary>
        public static bool TryParseBeacon(string text, out string code, out string hostIp, out ushort gamePort)
        {
            code = null;
            hostIp = null;
            gamePort = 0;
            if (string.IsNullOrEmpty(text))
                return false;
            const string tag = "BEACON|";
            if (!text.StartsWith(Prefix, StringComparison.Ordinal))
                return false;
            var rest = text.Substring(Prefix.Length);
            if (!rest.StartsWith(tag, StringComparison.Ordinal))
                return false;
            var payload = rest.Substring(tag.Length);
            var parts = payload.Split('|');
            if (parts.Length < 3)
                return false;
            code = NormalizeCode(parts[0]);
            hostIp = parts[1].Trim();
            if (!ushort.TryParse(parts[2].Trim(), out gamePort))
                return false;
            return code.Length == 6 && IPAddress.TryParse(hostIp, out _);
        }

        private static void SendLanRoomBeaconPayload(string roomCode, string hostIp, ushort gamePort, int beaconPort)
        {
            var payload = Encoding.UTF8.GetBytes($"{Prefix}BEACON|{roomCode}|{hostIp}|{gamePort}\n");
            try
            {
                using var udp = new UdpClient();
                udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udp.EnableBroadcast = true;
                try
                {
                    udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 32);
                }
                catch
                {
                    // ignored
                }

                SendPayloadToAllLanEndpoints(udp, payload, beaconPort);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LanRoomDiscovery] Beacon send failed: {ex.Message}");
            }
        }

        private static bool TryParseHostReply(string text, out string ip, out ushort port, out string code)
        {
            ip = null;
            port = 0;
            code = null;
            if (!text.StartsWith(Prefix, StringComparison.Ordinal))
                return false;
            var rest = text.Substring(Prefix.Length);
            if (!rest.StartsWith("HOST|", StringComparison.Ordinal))
                return false;
            var payload = rest.Substring(5);
            var parts = payload.Split('|');
            if (parts.Length < 3)
                return false;
            ip = parts[0].Trim();
            if (!ushort.TryParse(parts[1].Trim(), out port))
                return false;
            code = NormalizeCode(parts[2]);
            return code.Length > 0 && IPAddress.TryParse(ip, out _);
        }

        /// <summary>Sends FIND to UDP <see cref="LanUdpPorts.AllServicePorts"/> (7777 and 7778) so every host is reached.</summary>
        public static Task<(bool ok, string hostIp, ushort hostPort)> DiscoverHostAsync(
            string roomCode,
            int timeoutMs,
            CancellationToken cancellationToken = default)
        {
            var normalized = NormalizeCode(roomCode);
            return Task.Run(
                () =>
                {
                    try
                    {
                        using var udp = new UdpClient();
                        udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        udp.EnableBroadcast = true;
                        try
                        {
                            udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 32);
                            udp.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
                        }
                        catch
                        {
                            // ignored
                        }

                        var req = Encoding.UTF8.GetBytes($"{Prefix}FIND|{normalized}\n");

                        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                        var nextFindUtc = DateTime.MinValue;
                        const int findIntervalMs = 1500;

                        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
                        {
                            if (DateTime.UtcNow >= nextFindUtc)
                            {
                                foreach (var p in LanUdpPorts.AllServicePorts)
                                    SendPayloadToAllLanEndpoints(udp, req, p);

                                nextFindUtc = DateTime.UtcNow.AddMilliseconds(findIntervalMs);
                            }

                            var remaining = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                            if (remaining <= 0)
                                break;
                            udp.Client.ReceiveTimeout = Math.Min(500, Math.Max(50, remaining));

                            try
                            {
                                var remote = new IPEndPoint(IPAddress.Any, 0);
                                var data = udp.Receive(ref remote);
                                var text = Encoding.UTF8.GetString(data).Trim();
                                if (TryParseHostReply(text, out var ip, out var port, out var replyCode) &&
                                    string.Equals(
                                        NormalizeCode(replyCode),
                                        normalized,
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    Debug.Log($"[LanRoomDiscovery] Found host at {ip}:{port} (from {remote})");
                                    return (true, ip, port);
                                }
                            }
                            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
                            {
                                // keep scanning until deadline
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"[LanRoomDiscovery] DiscoverHostAsync: {e.Message}");
                    }

                    return (false, string.Empty, (ushort)0);
                },
                cancellationToken);
        }

        /// <summary>
        /// Joins the LAN discovery multicast group on every IPv4 interface that looks usable.
        /// Windows often only receives multicasts on the interface you explicitly join; a single default join
        /// misses packets when another PC uses Wi‑Fi and you use Ethernet (same router).
        /// </summary>
        public static void TryJoinMulticastListen(UdpClient udp)
        {
            if (udp == null)
                return;

            var socket = udp.Client;
            var group = LanMulticastGroup;
            var anyJoined = false;

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    if (IPAddress.IsLoopback(ua.Address))
                        continue;
                    var b = ua.Address.GetAddressBytes();
                    if (b.Length >= 2 && b[0] == 169 && b[1] == 254)
                        continue;

                    try
                    {
                        socket.SetSocketOption(
                            SocketOptionLevel.IP,
                            SocketOptionName.AddMembership,
                            new MulticastOption(group, ua.Address));
                        anyJoined = true;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[LanRoomDiscovery] Multicast join on {ua.Address}: {ex.Message}");
                    }
                }
            }

            if (!anyJoined)
            {
                try
                {
                    socket.SetSocketOption(
                        SocketOptionLevel.IP,
                        SocketOptionName.AddMembership,
                        new MulticastOption(group));
                    anyJoined = true;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LanRoomDiscovery] Multicast listen failed; using broadcast only: {ex.Message}");
                }
            }

            try
            {
                socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        /// Broadcast + per-interface broadcast + LAN multicast so Wi‑Fi/Ethernet guests can reach the host.
        /// </summary>
        private static void SendPayloadToAllLanEndpoints(UdpClient udp, byte[] payload, int port)
        {
            var sentKeys = new HashSet<string>();
            void TrySend(IPEndPoint ep)
            {
                var key = ep.Address + ":" + ep.Port;
                if (!sentKeys.Add(key))
                    return;
                try
                {
                    udp.Send(payload, payload.Length, ep);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LanRoomDiscovery] UDP send to {ep} failed: {ex.Message}");
                }
            }

            // Same-machine: OS often does not loop global broadcast back to another local process; loopback is reliable.
            TrySend(new IPEndPoint(IPAddress.Loopback, port));

            TrySend(new IPEndPoint(LanMulticastGroup, port));
            TrySend(new IPEndPoint(IPAddress.Broadcast, port));

            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    if (IPAddress.IsLoopback(ua.Address))
                        continue;

                    var ipBytes = ua.Address.GetAddressBytes();
                    if (ipBytes.Length != 4)
                        continue;
                    if (ipBytes[0] == 169 && ipBytes[1] == 254)
                        continue;

                    var bcast = ComputeBroadcastAddressForTypicalHomeLan(ipBytes);
                    TrySend(new IPEndPoint(bcast, port));
                }
            }
        }

        /// <summary>/24-style broadcast (x.y.z.255). Matches typical home routers; avoids IPv4Mask API differences across Unity targets.</summary>
        private static IPAddress ComputeBroadcastAddressForTypicalHomeLan(byte[] ipBytes)
        {
            return new IPAddress(new byte[] { ipBytes[0], ipBytes[1], ipBytes[2], 255 });
        }

        private static string NormalizeCode(string code) => code == null ? string.Empty : code.Trim();

        private static string GetOutboundIpv4To(IPAddress destination)
        {
            try
            {
                using var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                s.Connect(new IPEndPoint(destination, 65300));
                var local = (IPEndPoint)s.LocalEndPoint;
                return local.Address.ToString();
            }
            catch
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up)
                        continue;
                    foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                            continue;
                        if (IPAddress.IsLoopback(ua.Address))
                            continue;
                        var b = ua.Address.GetAddressBytes();
                        if (b.Length >= 2 && b[0] == 169 && b[1] == 254)
                            continue;
                        return ua.Address.ToString();
                    }
                }

                return "127.0.0.1";
            }
        }
    }

    /// <summary>Helpers for showing / parsing LAN IPv4 when UDP discovery is blocked by firewall or Wi‑Fi gear.</summary>
    public static class LanAddressHints
    {
        /// <summary>Best-effort IPv4 to show the host (prefers 192.168.x, then 10.x).</summary>
        public static string GetPrimaryLanIpv4()
        {
            string tenNet = null;
            string any = null;
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up)
                    continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                    continue;

                foreach (var ua in ni.GetIPProperties().UnicastAddresses)
                {
                    if (ua.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;
                    if (IPAddress.IsLoopback(ua.Address))
                        continue;
                    var b = ua.Address.GetAddressBytes();
                    if (b.Length < 4 || (b[0] == 169 && b[1] == 254))
                        continue;
                    var s = ua.Address.ToString();
                    if (b[0] == 192 && b[1] == 168)
                        return s;
                    if (b[0] == 10)
                        tenNet ??= s;
                    if (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                        any ??= s;
                    any ??= s;
                }
            }

            return tenNet ?? any ?? "";
        }

        /// <summary>True if the string is an IPv4, optionally with :port (e.g. 192.168.1.5:7778).</summary>
        public static bool LooksLikeIpv4WithOptionalPort(string raw, out string ipv4, out int port)
        {
            ipv4 = null;
            port = 0;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            raw = raw.Trim();
            var hostPart = raw;
            if (raw.Contains(":"))
            {
                var idx = raw.LastIndexOf(':');
                hostPart = raw.Substring(0, idx).Trim();
                if (!int.TryParse(raw.Substring(idx + 1).Trim(), out port) || port < 1 || port > 65535)
                    return false;
            }

            // .NET parses dotless numeric strings as IPv4 (e.g. "689111" → 0.10.131.215). Our 6-digit room codes
            // must not take this path so they can resolve via the LAN list / discovery join flow.
            if (hostPart.Length == 6)
            {
                var allDigit = true;
                for (var i = 0; i < hostPart.Length; i++)
                {
                    var c = hostPart[i];
                    if (c < '0' || c > '9')
                    {
                        allDigit = false;
                        break;
                    }
                }

                if (allDigit)
                    return false;
            }

            if (!IPAddress.TryParse(hostPart, out var addr))
                return false;
            if (addr.AddressFamily != AddressFamily.InterNetwork)
                return false;
            ipv4 = hostPart;
            return true;
        }
    }
}
