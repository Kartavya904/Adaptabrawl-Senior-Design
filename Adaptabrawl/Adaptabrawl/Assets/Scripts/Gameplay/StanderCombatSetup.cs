using UnityEngine;

using Adaptabrawl.Combat;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Rebuilds the live combat rig on the spawned Shinabro Stander:
    /// Adaptabrawl-owned hurtbox parts plus runtime weapon strike volumes.
    /// Safe to call after prefab swaps.
    /// </summary>
    public class StanderCombatSetup : MonoBehaviour
    {
        private void Start()
        {
            RunSetup();
        }

        public void RunSetup()
        {
            Transform stander = transform.Find("Stander");

            if (stander == null)
            {
                PlayerController_Platform pcp = GetComponentInChildren<PlayerController_Platform>();
                if (pcp != null)
                    stander = pcp.transform;
            }

            if (stander == null)
            {
                Debug.LogWarning($"[StanderCombatSetup] No Stander child found on '{gameObject.name}'.");
                return;
            }

            FighterController owner = GetComponent<FighterController>();
            if (owner == null)
            {
                Debug.LogWarning($"[StanderCombatSetup] No FighterController found on '{gameObject.name}'.");
                return;
            }

            CombatVolumeBuilder.Rebuild(stander, owner);
            Debug.Log($"[StanderCombatSetup] Unified 3D combat volumes rebuilt for '{gameObject.name}'.");
        }
    }
}
