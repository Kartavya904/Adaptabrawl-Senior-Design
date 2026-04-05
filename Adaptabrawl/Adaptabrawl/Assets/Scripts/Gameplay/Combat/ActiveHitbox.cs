using UnityEngine;
using System.Collections.Generic;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Combat
{
    /// <summary>
    /// Spawned as a child of the Stander by HitboxEmitter for the duration of a move's
    /// active frames. Destroys itself automatically when the active window closes.
    ///
    /// Uses Physics2D.OverlapBox polling (rather than OnTriggerEnter2D) so it works
    /// regardless of whether a Rigidbody2D is present — the Shinabro Stander uses a
    /// 3D Rigidbody which the 2D trigger system does not see.
    /// </summary>
    public class ActiveHitbox : MonoBehaviour
    {
        [Header("Attack Data")]
        [SerializeField] private float damage;
        [SerializeField] private float knockbackForce;

        [Header("Shape")]
        [SerializeField] private Vector2 size;
        [SerializeField] private Vector2 offset;

        [Header("Owner")]
        [SerializeField] private FighterController owner;

        private readonly HashSet<FighterController> alreadyHit = new HashSet<FighterController>();

        /// <summary>Called by HitboxEmitter immediately after spawning.</summary>
        public void Init(float dmg, float knockback, Vector2 hitboxSize, Vector2 hitboxOffset,
                         float duration, FighterController ownerFighter)
        {
            damage        = dmg;
            knockbackForce = knockback;
            size          = hitboxSize;
            offset        = hitboxOffset;
            owner         = ownerFighter;

            Destroy(gameObject, duration);
        }

        private void Update()
        {
            // Poll every frame — no Rigidbody2D required
            Vector2 worldCenter = (Vector2)transform.position + offset;
            float angle = transform.eulerAngles.z;
            Collider2D[] hits = Physics2D.OverlapBoxAll(worldCenter, size, angle);

            foreach (Collider2D hit in hits)
            {
                FighterHurtbox hurtbox = hit.GetComponent<FighterHurtbox>();
                if (hurtbox == null) continue;
                if (hurtbox.Owner == null || hurtbox.Owner == owner) continue;
                if (alreadyHit.Contains(hurtbox.Owner)) continue;

                Debug.Log($"[ActiveHitbox] HIT — target='{hurtbox.Owner.gameObject.name}', damage={damage}");
                alreadyHit.Add(hurtbox.Owner);

                Vector2 dir = ((Vector2)hurtbox.transform.position - worldCenter).normalized;
                if (dir == Vector2.zero) dir = owner != null && owner.FacingRight ? Vector2.right : Vector2.left;

                hurtbox.ReceiveHit(damage, dir, knockbackForce);

                // Phase 1: Hit-stop juice! If multiple hits connect, they slightly extend the hitstop!
                var gm = Object.FindFirstObjectByType<GameManager>();
                if (gm != null) gm.TriggerHitStop(0.12f);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position + (Vector3)offset, Quaternion.Euler(0, 0, transform.eulerAngles.z), Vector3.one);
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.4f);
            Gizmos.DrawCube(Vector3.zero, new Vector3(size.x, size.y, 0.1f));
            Gizmos.color = new Color(1f, 0.6f, 0f, 1f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0.1f));
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
