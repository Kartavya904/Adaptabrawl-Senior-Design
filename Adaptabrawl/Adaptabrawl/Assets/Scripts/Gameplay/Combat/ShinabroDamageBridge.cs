using UnityEngine;

namespace Adaptabrawl.Combat
{
    /// <summary>
    /// Legacy compatibility stub.
    /// Damage now routes through FighterController directly from the unified 3D combat pipeline.
    /// </summary>
    public class ShinabroDamageBridge : MonoBehaviour
    {
        private void Start()
        {
            Debug.LogWarning("[ShinabroDamageBridge] Deprecated component detected. Unified combat no longer uses the Shinabro damage bridge.");
            enabled = false;
        }
    }
}
