using UnityEngine;
using Adaptabrawl.Combat;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Bootstrap component placed on the root fighter by FighterFactory.
    /// In Start() — after the full prefab hierarchy is live — it finds the
    /// "Stander" child and attaches FighterHurtbox and HitboxEmitter to it
    /// so they follow root-motion, then destroys itself.
    /// </summary>
    public class StanderCombatSetup : MonoBehaviour
    {
        private void Start()
        {
            Transform stander = transform.Find("Stander");

            if (stander == null)
            {
                // List actual children to help diagnose naming mismatch
                string children = "";
                foreach (Transform c in transform) children += $"'{c.name}' ";
                Debug.LogWarning($"[StanderCombatSetup] No 'Stander' child on '{gameObject.name}'. Children found: {children}. Combat volumes NOT attached.");
                Destroy(this);
                return;
            }

            Debug.Log($"[StanderCombatSetup] Found Stander on '{gameObject.name}' — attaching FighterHurtbox + HitboxEmitter + ShinabroDamageBridge.");

            if (stander.GetComponent<FighterHurtbox>() == null)
                stander.gameObject.AddComponent<FighterHurtbox>();

            if (stander.GetComponent<HitboxEmitter>() == null)
                stander.gameObject.AddComponent<HitboxEmitter>();

            if (stander.GetComponent<ShinabroDamageBridge>() == null)
                stander.gameObject.AddComponent<ShinabroDamageBridge>();

            Destroy(this);
        }
    }
}
