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
                Debug.LogWarning($"[StanderCombatSetup] No 'Stander' child on '{gameObject.name}'. Combat volumes not attached.");
                Destroy(this);
                return;
            }

            if (stander.GetComponent<FighterHurtbox>() == null)
                stander.gameObject.AddComponent<FighterHurtbox>();

            if (stander.GetComponent<HitboxEmitter>() == null)
                stander.gameObject.AddComponent<HitboxEmitter>();

            Destroy(this);
        }
    }
}
