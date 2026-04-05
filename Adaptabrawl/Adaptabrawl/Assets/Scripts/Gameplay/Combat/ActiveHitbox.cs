using UnityEngine;

namespace Adaptabrawl.Combat
{
    /// <summary>
    /// Legacy compatibility stub for the retired 2D hitbox path.
    /// Unified combat now uses WeaponHitVolume instead.
    /// </summary>
    public class ActiveHitbox : MonoBehaviour
    {
        public void Init(float dmg, float knockback, Vector2 hitboxSize, Vector2 hitboxOffset, float duration, Gameplay.FighterController ownerFighter)
        {
            enabled = false;
            Destroy(gameObject);
        }
    }
}
