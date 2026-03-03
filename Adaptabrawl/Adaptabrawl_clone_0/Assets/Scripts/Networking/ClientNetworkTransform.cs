using Unity.Netcode.Components;
using UnityEngine;

namespace Adaptabrawl.Networking
{
    /// <summary>
    /// NetworkTransform that lets the client owner be authoritative for their transform.
    /// Reduces jitter and input lag compared to server-authoritative NetworkTransform.
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
