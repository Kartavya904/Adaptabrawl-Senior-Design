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
    /// Same-LAN room discovery over UDP broadcast. Host answers FIND requests with the game IP/port
    /// so clients can connect via UnityTransport without typing an IP address.
    /// </summary>
    public static class LanRoomDiscovery
    {
        private const string Prefix = "ABDISC|";

        private static Thread s_HostThread;
        private static volatile bool s_HostRunning;
        private static UdpClient s_HostUdp;

        public static void StartHostResponder(string roomCode, ushort gamePort, int discoveryPort)
        {
            StopHostResponder();
            s_HostRunning = true;
            var normalizedCode = NormalizeCode(roomCode);
            s_HostThread = new Thread(() => HostThreadProc(normalizedCode, gamePort, discoveryPort))
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

        private static void HostThreadProc(string roomCode, ushort gamePort, int discoveryPort)
        {
            try
            {
                s_HostUdp = new UdpClient();
                s_HostUdp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                s_HostUdp.Client.Bind(new IPEndPoint(IPAddress.Any, discoveryPort));
                s_HostUdp.Client.ReceiveTimeout = 500;

                Debug.Log(
                    $"[LanRoomDiscovery] Host listening on UDP port {discoveryPort} (room {roomCode}). Allow inbound UDP {discoveryPort} in firewall if joins fail.");

                while (s_HostRunning)
                {
                    try
                    {
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
            catch (SocketException ex)
            {
                Debug.LogError(
                    $"[LanRoomDiscovery] Could not bind discovery port {discoveryPort} ({ex.SocketErrorCode}). Another copy of the game or app may be using it. {ex.Message}");
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

        public static Task<(bool ok, string hostIp, ushort hostPort)> DiscoverHostAsync(
            string roomCode,
            int discoveryPort,
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
                        var req = Encoding.UTF8.GetBytes($"{Prefix}FIND|{normalized}\n");

                        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                        var nextFindUtc = DateTime.MinValue;
                        const int findIntervalMs = 1500;

                        while (DateTime.UtcNow < deadline && !cancellationToken.IsCancellationRequested)
                        {
                            if (DateTime.UtcNow >= nextFindUtc)
                            {
                                SendFindToAllBroadcastEndpoints(udp, req, discoveryPort);
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
        /// 255.255.255.255 is often dropped on Wi‑Fi / Windows; subnet broadcasts (e.g. 192.168.1.255) are much more reliable.
        /// </summary>
        private static void SendFindToAllBroadcastEndpoints(UdpClient udp, byte[] req, int discoveryPort)
        {
            var sentKeys = new HashSet<string>();
            void TrySend(IPEndPoint ep)
            {
                var key = ep.Address + ":" + ep.Port;
                if (!sentKeys.Add(key))
                    return;
                try
                {
                    udp.Send(req, req.Length, ep);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LanRoomDiscovery] FIND send to {ep} failed: {ex.Message}");
                }
            }

            TrySend(new IPEndPoint(IPAddress.Broadcast, discoveryPort));

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
                    TrySend(new IPEndPoint(bcast, discoveryPort));
                }
            }
        }

        /// <summary>/24-style broadcast (x.y.z.255). Matches typical home routers; avoids IPv4Mask API differences across Unity targets.</summary>
        private static IPAddress ComputeBroadcastAddressForTypicalHomeLan(byte[] ipBytes)
        {
            return new IPAddress(new[] { ipBytes[0], ipBytes[1], ipBytes[2], 255 });
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
}
