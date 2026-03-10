using UnityEngine;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Combat
{
    /// <summary>
    /// Placed on the Stander child alongside PlayerController_Platform.
    /// Watches PlayerController_Platform.currentHealth each frame and, when it
    /// decreases, forwards the damage delta to the root FighterController so
    /// our health system and UI stay in sync.
    ///
    /// This avoids any modifications to the Shinabro package scripts.
    /// </summary>
    public class ShinabroDamageBridge : MonoBehaviour
    {
        private PlayerController_Platform pcp;
        private FighterController fc;
        private float lastHealth;

        private void Start()
        {
            pcp = GetComponent<PlayerController_Platform>();
            fc  = GetComponentInParent<FighterController>();

            if (pcp == null)
            {
                Debug.LogWarning("[ShinabroDamageBridge] No PlayerController_Platform found on '" + gameObject.name + "'. Bridge inactive.");
                enabled = false;
                return;
            }

            if (fc == null)
            {
                Debug.LogWarning("[ShinabroDamageBridge] No FighterController found in parents of '" + gameObject.name + "'. Bridge inactive.");
                enabled = false;
                return;
            }

            lastHealth = pcp.currentHealth;
            Debug.Log($"[ShinabroDamageBridge] Ready on '{gameObject.name}' — tracking pcp.currentHealth → FighterController '{fc.gameObject.name}'.");
        }

        private void Update()
        {
            float current = pcp.currentHealth;
            float delta   = lastHealth - current;

            if (delta > 0f)
            {
                Debug.Log($"[ShinabroDamageBridge] Detected {delta} damage on '{gameObject.name}' — forwarding to FighterController.");
                fc.TakeDamage(delta);
            }

            lastHealth = current;
        }
    }
}
