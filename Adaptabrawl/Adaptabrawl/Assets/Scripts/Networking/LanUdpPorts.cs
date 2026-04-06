namespace Adaptabrawl.Networking
{
    /// <summary>
    /// LAN UDP uses exactly two ports: Unity Transport (game) binds one; discovery FIND/replies and room beacons
    /// use the other. A second instance on the same machine swaps which is which (7777 vs 7778).
    /// </summary>
    public static class LanUdpPorts
    {
        public const ushort GamePrimary = 7777;
        public const ushort GameCompanion = 7778;

        /// <summary>Every UDP port used for discovery/beacons — clients probe both so they always reach the host.</summary>
        public static readonly ushort[] AllServicePorts = { GamePrimary, GameCompanion };
    }
}
