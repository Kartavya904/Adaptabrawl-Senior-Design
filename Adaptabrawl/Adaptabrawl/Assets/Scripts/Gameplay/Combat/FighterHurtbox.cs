using UnityEngine;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Combat
{
    /// <summary>
    /// Placed on the Stander child by StanderCombatSetup.
    /// Represents the fighter's vulnerable area and routes incoming hits
    /// up to the root FighterController.
    ///
    /// Adjust Size and Offset in the Inspector to fit your character.
    /// </summary>
    public class FighterHurtbox : MonoBehaviour
    {
        [Header("Hurtbox Shape — adjust to fit character")]
        [Tooltip("Width and height of the hurtbox in world units.")]
        public Vector2 size   = new Vector2(1f, 2f);
        [Tooltip("Offset from the Stander pivot.")]
        public Vector2 offset = new Vector2(0f, 1f);

        [Header("Runtime")]
        [SerializeField] private FighterController owner;

        private BoxCollider2D col;

        private void Awake()
        {
            // Find the root FighterController (walking up the hierarchy)
            owner = GetComponentInParent<FighterController>();

            col = gameObject.AddComponent<BoxCollider2D>();
            col.size   = size;
            col.offset = offset;
            col.isTrigger = true;
        }

        /// <summary>Called by ActiveHitbox when it overlaps this hurtbox.</summary>
        public void ReceiveHit(float damage, Vector2 knockbackDir, float knockbackForce)
        {
            if (owner == null || owner.IsDead) return;
            owner.TakeDamage(damage);
        }

        public FighterController Owner => owner;
        public bool IsActive => col != null && col.enabled;

        // Sync collider shape if values are tweaked at runtime
        private void OnValidate()
        {
            if (col != null)
            {
                col.size   = size;
                col.offset = offset;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawCube(transform.position + (Vector3)offset, new Vector3(size.x, size.y, 0.1f));
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
            Gizmos.DrawWireCube(transform.position + (Vector3)offset, new Vector3(size.x, size.y, 0.1f));
        }
    }
}
