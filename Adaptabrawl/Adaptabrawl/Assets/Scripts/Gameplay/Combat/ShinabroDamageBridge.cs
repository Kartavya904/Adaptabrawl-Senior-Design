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
            // Stop polling once our system has already registered death.
            if (fc.IsDead) return;

            float current = pcp.currentHealth;
            float delta   = lastHealth - current;

            if (delta > 0f)
            {
                Debug.Log($"[ShinabroDamageBridge] Detected {delta} damage on '{gameObject.name}' — forwarding to FighterController.");
                fc.TakeDamage(delta);
            }

            lastHealth = current;

            // Shinabro clamps its own currentHealth at 0, so a large hit on a low-HP
            // fighter produces a smaller delta than the actual damage dealt.
            // If Shinabro's own dead flag is set but our FighterController hasn't died
            // yet (e.g. FC still has HP remaining), force-kill it now.
            if (pcp.isDead && !fc.IsDead)
            {
                Debug.Log($"[ShinabroDamageBridge] Shinabro marked dead but FighterController still alive — forcing kill.");
                fc.TakeDamage(fc.CurrentHealth + 1f);
            }
        }
    }
}
