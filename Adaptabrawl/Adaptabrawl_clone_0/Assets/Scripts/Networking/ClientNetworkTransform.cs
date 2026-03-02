using Unity.Netcode.Components;
using UnityEngine;

namespace Adaptabrawl.Networking
{
    /// <summary>
    /// A NetworkTransform that allows the client owner to be authoritative for their transform.
    /// This is strictly required for the player to move smoothly on their own screen and sync movement.
    /// Default NetworkTransform is Server Authoritative, causing jitter and input lag for clients.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
