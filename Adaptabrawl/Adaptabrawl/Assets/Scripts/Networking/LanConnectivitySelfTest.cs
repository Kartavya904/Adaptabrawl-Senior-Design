using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Adaptabrawl.Networking
{
    public enum LanConnectivityTestState
    {
        Success,
        Warning,
        Failure
    }

    public readonly struct LanConnectivityTestResult
    {
        public LanConnectivityTestResult(
            LanConnectivityTestState state,
            string summary,
            string details,
            string primaryLanIpv4,
            bool firewallCheckSupported,
            bool privateNetworkAllowed,
            bool publicNetworkAllowed,
            DateTime completedAtUtc)
        {
            State = state;
            Summary = summary ?? string.Empty;
            Details = details ?? string.Empty;
            PrimaryLanIpv4 = primaryLanIpv4 ?? string.Empty;
            FirewallCheckSupported = firewallCheckSupported;
            PrivateNetworkAllowed = privateNetworkAllowed;
            PublicNetworkAllowed = publicNetworkAllowed;
            CompletedAtUtc = completedAtUtc;
        }

        public LanConnectivityTestState State { get; }
        public string Summary { get; }
        public string Details { get; }
        public string PrimaryLanIpv4 { get; }
        public bool FirewallCheckSupported { get; }
        public bool PrivateNetworkAllowed { get; }
        public bool PublicNetworkAllowed { get; }
        public DateTime CompletedAtUtc { get; }

        public bool IsSuccess => State == LanConnectivityTestState.Success;
    }

    /// <summary>
    /// Best-effort LAN readiness probe for Windows Firewall and local socket availability.
    /// This cannot force the OS firewall dialog, but opening inbound listeners is what
    /// typically causes Windows to prompt the user the first time.
    /// </summary>
    public static class LanConnectivitySelfTest
    {
        private const int TcpPromptPort = LobbyManager.DefaultLanGamePort;
        private const string UdpRuleNamePrefix = "Adaptabrawl LAN UDP";
        private const string TcpRuleNamePrefix = "Adaptabrawl LAN TCP";
        private const int FirewallPromptPollIntervalMs = 400;
        private const int FirewallPromptWaitMs = 8000;
        private const int UdpRoundTripTimeoutMs = 1200;

        public static LanConnectivityTestResult? LastResult { get; private set; }

        public static Task<LanConnectivityTestResult> RunAsync(bool waitForWindowsFirewallConfirmation)
        {
            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            return Task.Run(() => Run(waitForWindowsFirewallConfirmation, isWindows));
        }

        public static Task<bool> TryEnsureWindowsFirewallAccessAsync()
        {
            bool isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
            return Task.Run(() => isWindows && TryEnsureWindowsFirewallAccess());
        }

        private static LanConnectivityTestResult Run(bool waitForWindowsFirewallConfirmation, bool isWindows)
        {
            string primaryIpv4 = LanAddressHints.GetPrimaryLanIpv4();
            if (string.IsNullOrEmpty(primaryIpv4))
            {
                return CacheResult(
                    new LanConnectivityTestResult(
                        LanConnectivityTestState.Failure,
                        "No LAN adapter is ready.",
                        "Connect this device to Wi-Fi or Ethernet on the same router before using online play.",
                        string.Empty,
                        firewallCheckSupported: false,
                        privateNetworkAllowed: false,
                        publicNetworkAllowed: false,
                        completedAtUtc: DateTime.UtcNow));
            }

            bool firewallCheckSupported = false;
            bool privateAllowed = false;
            bool publicAllowed = false;

            if (isWindows)
                TryGetWindowsFirewallAccess(out firewallCheckSupported, out privateAllowed, out publicAllowed);

            UdpClient gameUdp = null;
            UdpClient serviceUdp = null;
            TcpListener firewallPromptListener = null;

            try
            {
                if (!TryBindUdpPort(LanUdpPorts.GamePrimary, out gameUdp, out string bindError))
                    return BuildBindFailure(primaryIpv4, bindError);
                if (!TryBindUdpPort(LanUdpPorts.GameCompanion, out serviceUdp, out bindError))
                    return BuildBindFailure(primaryIpv4, bindError);
                if (!TryBindTcpPort(TcpPromptPort, out firewallPromptListener, out bindError))
                    return BuildBindFailure(primaryIpv4, bindError);

                if (!TryRunUdpRoundTrip(primaryIpv4, out string roundTripError))
                {
                    return CacheResult(
                        new LanConnectivityTestResult(
                            LanConnectivityTestState.Failure,
                            "LAN socket probe failed.",
                            roundTripError,
                            primaryIpv4,
                            firewallCheckSupported,
                            privateAllowed,
                            publicAllowed,
                            DateTime.UtcNow));
                }

                if (isWindows)
                {
                    int remainingMs = waitForWindowsFirewallConfirmation ? FirewallPromptWaitMs : FirewallPromptPollIntervalMs;
                    while (remainingMs > 0 && !privateAllowed)
                    {
                        Task.Delay(FirewallPromptPollIntervalMs).Wait();
                        remainingMs -= FirewallPromptPollIntervalMs;
                        TryGetWindowsFirewallAccess(out firewallCheckSupported, out privateAllowed, out publicAllowed);
                    }
                }

                if (isWindows && firewallCheckSupported && !privateAllowed)
                {
                    return CacheResult(
                        new LanConnectivityTestResult(
                            LanConnectivityTestState.Warning,
                            "LAN ports are open, but Windows Firewall private-network permission is still missing.",
                            "If Windows shows a firewall prompt, allow this app on Private networks and run Test again. If no prompt appears, manually allow the app through Windows Defender Firewall.",
                            primaryIpv4,
                            firewallCheckSupported,
                            privateAllowed,
                            publicAllowed,
                            DateTime.UtcNow));
                }

                string summary = isWindows && firewallCheckSupported
                    ? publicAllowed
                        ? "LAN access ready. Windows Firewall allows this app on private and public networks."
                        : "LAN access ready. Windows Firewall allows this app on private networks."
                    : "LAN access ready. The local socket probe completed successfully.";

                string details =
                    $"Host IPv4 {primaryIpv4} is reachable locally. Online play uses only UDP {LanUdpPorts.GamePrimary} and {LanUdpPorts.GameCompanion} (game + discovery/beacons).";

                return CacheResult(
                    new LanConnectivityTestResult(
                        LanConnectivityTestState.Success,
                        summary,
                        details,
                        primaryIpv4,
                        firewallCheckSupported,
                        privateAllowed,
                        publicAllowed,
                        DateTime.UtcNow));
            }
            finally
            {
                try { gameUdp?.Close(); } catch { }
                try { serviceUdp?.Close(); } catch { }
                try { firewallPromptListener?.Stop(); } catch { }
            }
        }

        private static LanConnectivityTestResult BuildBindFailure(string primaryIpv4, string bindError)
        {
            return CacheResult(
                new LanConnectivityTestResult(
                    LanConnectivityTestState.Failure,
                    "LAN test could not reserve the required ports.",
                    bindError,
                    primaryIpv4,
                    firewallCheckSupported: false,
                    privateNetworkAllowed: false,
                    publicNetworkAllowed: false,
                    completedAtUtc: DateTime.UtcNow));
        }

        private static LanConnectivityTestResult CacheResult(LanConnectivityTestResult result)
        {
            LastResult = result;
            return result;
        }

        private static bool TryBindUdpPort(int port, out UdpClient client, out string error)
        {
            client = null;
            error = string.Empty;

            try
            {
                client = new UdpClient(AddressFamily.InterNetwork);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                client.EnableBroadcast = true;
                client.Client.Bind(new IPEndPoint(IPAddress.Any, port));
                return true;
            }
            catch (SocketException ex)
            {
                client?.Close();
                error = FormatSocketBindError(port, "UDP", ex);
                return false;
            }
        }

        private static bool TryBindTcpPort(int port, out TcpListener listener, out string error)
        {
            listener = null;
            error = string.Empty;

            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                listener.Start(1);
                return true;
            }
            catch (SocketException ex)
            {
                try { listener?.Stop(); } catch { }
                error = FormatSocketBindError(port, "TCP", ex);
                return false;
            }
        }

        private static string FormatSocketBindError(int port, string protocol, SocketException ex)
        {
            return ex.SocketErrorCode switch
            {
                SocketError.AddressAlreadyInUse =>
                    $"{protocol} {port} is already in use. Close extra Unity Play windows or any other Adaptabrawl instance, then try again.",
                SocketError.AccessDenied =>
                    $"{protocol} {port} could not be opened because Windows denied access. Allow the app through Windows Firewall and try again.",
                _ =>
                    $"Could not open {protocol} {port}: {ex.Message}"
            };
        }

        private static bool TryRunUdpRoundTrip(string hostIpv4, out string error)
        {
            error = string.Empty;

            try
            {
                using var receiver = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
                using var sender = new UdpClient(AddressFamily.InterNetwork);

                receiver.Client.ReceiveTimeout = UdpRoundTripTimeoutMs;
                sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                var payload = Encoding.UTF8.GetBytes($"ADAPTABRAWL_PROBE|{Guid.NewGuid():N}");
                var receivePort = ((IPEndPoint)receiver.Client.LocalEndPoint).Port;
                sender.Send(payload, payload.Length, new IPEndPoint(IPAddress.Parse(hostIpv4), receivePort));

                var remote = new IPEndPoint(IPAddress.Any, 0);
                byte[] received = receiver.Receive(ref remote);
                if (received.Length != payload.Length)
                {
                    error = "The local UDP probe returned an unexpected payload.";
                    return false;
                }

                for (int i = 0; i < payload.Length; i++)
                {
                    if (payload[i] == received[i])
                        continue;

                    error = "The local UDP probe payload was corrupted.";
                    return false;
                }

                return true;
            }
            catch (SocketException ex)
            {
                error = $"The local UDP probe timed out or failed: {ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                error = $"The local UDP probe failed unexpectedly: {ex.Message}";
                return false;
            }
        }

        private static void TryGetWindowsFirewallAccess(out bool supported, out bool privateAllowed, out bool publicAllowed)
        {
            supported = false;
            privateAllowed = false;
            publicAllowed = false;

            string applicationPath = TryGetCurrentProcessPath();
            if (string.IsNullOrWhiteSpace(applicationPath))
                return;

            if (!TryReadWindowsFirewallRules(out string output))
                return;

            try
            {
                supported = true;
                string currentProgram = string.Empty;
                bool enabled = false;
                bool allow = false;
                bool inbound = false;
                bool appliesToPrivate = false;
                bool appliesToPublic = false;

                string[] lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i]?.Trim() ?? string.Empty;
                    if (line.Length == 0)
                    {
                        if (PathsMatch(currentProgram, applicationPath) && enabled && allow && inbound)
                        {
                            privateAllowed |= appliesToPrivate;
                            publicAllowed |= appliesToPublic;
                        }

                        currentProgram = string.Empty;
                        enabled = false;
                        allow = false;
                        inbound = false;
                        appliesToPrivate = false;
                        appliesToPublic = false;
                        continue;
                    }

                    int separator = line.IndexOf(':');
                    if (separator <= 0)
                        continue;

                    string key = line.Substring(0, separator).Trim();
                    string value = line.Substring(separator + 1).Trim();

                    if (key.Equals("Program", StringComparison.OrdinalIgnoreCase))
                    {
                        currentProgram = value.Trim('"');
                        continue;
                    }

                    if (key.Equals("Enabled", StringComparison.OrdinalIgnoreCase))
                    {
                        enabled = value.Equals("Yes", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    if (key.Equals("Action", StringComparison.OrdinalIgnoreCase))
                    {
                        allow = value.Equals("Allow", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    if (key.Equals("Direction", StringComparison.OrdinalIgnoreCase))
                    {
                        inbound = value.Equals("In", StringComparison.OrdinalIgnoreCase) ||
                                  value.Equals("Inbound", StringComparison.OrdinalIgnoreCase);
                        continue;
                    }

                    if (key.Equals("Profiles", StringComparison.OrdinalIgnoreCase))
                    {
                        appliesToPrivate =
                            value.IndexOf("Private", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            value.IndexOf("All", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            value.IndexOf("Any", StringComparison.OrdinalIgnoreCase) >= 0;
                        appliesToPublic =
                            value.IndexOf("Public", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            value.IndexOf("All", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            value.IndexOf("Any", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }

                if (PathsMatch(currentProgram, applicationPath) && enabled && allow && inbound)
                {
                    privateAllowed |= appliesToPrivate;
                    publicAllowed |= appliesToPublic;
                }
            }
            catch
            {
                supported = false;
                privateAllowed = false;
                publicAllowed = false;
            }
        }

        private static bool TryReadWindowsFirewallRules(out string output)
        {
            output = string.Empty;

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "advfirewall firewall show rule name=all verbose",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using Process process = Process.Start(startInfo);
                if (process == null)
                    return false;

                output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit(5000);

                if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(output))
                {
                    output = error;
                    return false;
                }

                return !string.IsNullOrWhiteSpace(output);
            }
            catch
            {
                output = string.Empty;
                return false;
            }
        }

        private static bool TryEnsureWindowsFirewallAccess()
        {
            string applicationPath = TryGetCurrentProcessPath();
            if (string.IsNullOrWhiteSpace(applicationPath))
                return false;

            string executableName = Path.GetFileNameWithoutExtension(applicationPath);
            if (string.IsNullOrWhiteSpace(executableName))
                executableName = "Current App";

            string udpRuleName = $"{UdpRuleNamePrefix} - {executableName}";
            string tcpRuleName = $"{TcpRuleNamePrefix} - {executableName}";
            string escapedPath = EscapePowerShellSingleQuotedString(applicationPath);
            string escapedUdpRuleName = EscapePowerShellSingleQuotedString(udpRuleName);
            string escapedTcpRuleName = EscapePowerShellSingleQuotedString(tcpRuleName);

            string script =
                "$ErrorActionPreference='Stop'; " +
                $"& netsh advfirewall firewall delete rule name='{escapedUdpRuleName}' | Out-Null; " +
                $"& netsh advfirewall firewall delete rule name='{escapedTcpRuleName}' | Out-Null; " +
                $"& netsh advfirewall firewall add rule name='{escapedUdpRuleName}' dir=in action=allow program='{escapedPath}' enable=yes profile=any protocol=UDP localport={LanUdpPorts.GamePrimary},{LanUdpPorts.GameCompanion} remoteip=localsubnet | Out-Null; " +
                $"& netsh advfirewall firewall add rule name='{escapedTcpRuleName}' dir=in action=allow program='{escapedPath}' enable=yes profile=any protocol=TCP localport={TcpPromptPort} remoteip=localsubnet | Out-Null;";

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = false
                };

                using Process process = Process.Start(startInfo);
                if (process == null)
                    return false;

                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static string TryGetCurrentProcessPath()
        {
            try
            {
                using Process process = Process.GetCurrentProcess();
                return process.MainModule?.FileName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool PathsMatch(string left, string right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
                return false;

            return string.Equals(
                left.Trim(),
                right.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string EscapePowerShellSingleQuotedString(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace("'", "''");
        }
    }
}
