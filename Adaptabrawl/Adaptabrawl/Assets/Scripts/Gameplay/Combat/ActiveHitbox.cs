using UnityEngine;
using System.Collections.Generic;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Combat
{
    /// <summary>
    /// Spawned as a child of the Stander by HitboxEmitter for the duration of a move's
    /// active frames. Destroys itself automatically when the active window closes.
    ///
    /// Because it is parented to the Stander it follows root-motion automatically.
    /// </summary>
    [RequireComponent(typeof(BoxCollider2D))]
    public class ActiveHitbox : MonoBehaviour
    {
        [Header("Attack Data")]
        [SerializeField] private float damage;
        [SerializeField] private float knockbackForce;

        [Header("Owner")]
        [SerializeField] private FighterController owner;

        private BoxCollider2D col;
        private readonly HashSet<FighterController> alreadyHit = new HashSet<FighterController>();

        private void Awake()
        {
            col = GetComponent<BoxCollider2D>();
            col.isTrigger = true;
        }

        /// <summary>
        /// Called by HitboxEmitter immediately after spawning.
        /// </summary>
        public void Init(float dmg, float knockback, Vector2 size, Vector2 offset,
                         float duration, FighterController ownerFighter)
        {
            damage        = dmg;
            knockbackForce = knockback;
            owner         = ownerFighter;

            col.size   = size;
            col.offset = offset;

            Destroy(gameObject, duration);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            FighterHurtbox hurtbox = other.GetComponent<FighterHurtbox>();
            if (hurtbox == null) return;
            if (hurtbox.Owner == null || hurtbox.Owner == owner) return;
            if (alreadyHit.Contains(hurtbox.Owner)) return;

            alreadyHit.Add(hurtbox.Owner);

            // Knockback direction: away from the attacker's position
            Vector2 dir = ((Vector2)hurtbox.transform.position - (Vector2)transform.position).normalized;
            if (dir == Vector2.zero) dir = Vector2.right;

            hurtbox.ReceiveHit(damage, dir, knockbackForce);
        }

        private void OnDrawGizmosSelected()
        {
            if (col == null) return;
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.4f);
            Gizmos.DrawCube(transform.position + (Vector3)col.offset, new Vector3(col.size.x, col.size.y, 0.1f));
            Gizmos.color = new Color(1f, 0.6f, 0f, 1f);
            Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, new Vector3(col.size.x, col.size.y, 0.1f));
        }
    }
}
