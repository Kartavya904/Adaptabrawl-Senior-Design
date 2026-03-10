using UnityEngine;
using System.Collections.Generic;
using Adaptabrawl.Data;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Combat
{
    /// <summary>
    /// Placed on the Stander child by StanderCombatSetup.
    /// Listens to CombatFSM.OnHitboxActive (fired when a move enters its active frames)
    /// and spawns an ActiveHitbox as a child of the Stander so it follows root-motion.
    ///
    /// Adjust the per-move-type configs in the Inspector to balance combat.
    /// </summary>
    public class HitboxEmitter : MonoBehaviour
    {
        // -----------------------------------------------------------------------
        // Per-move-type configuration — all Inspector-adjustable
        // -----------------------------------------------------------------------

        [System.Serializable]
        public class HitboxConfig
        {
            [Tooltip("Which move type this config applies to.")]
            public MoveType moveType = MoveType.LightAttack;

            [Header("Damage & Knockback")]
            [Tooltip("Base damage dealt on hit.")]
            public float damage = 10f;
            [Tooltip("Knockback impulse magnitude.")]
            public float knockbackForce = 4f;

            [Header("Hitbox Shape")]
            [Tooltip("Width and height of the hitbox in world units.")]
            public Vector2 size   = new Vector2(1.5f, 1f);
            [Tooltip("Offset from the Stander pivot (positive X = in front when facing right).")]
            public Vector2 offset = new Vector2(0.75f, 0.5f);

            [Header("Timing")]
            [Tooltip("How long (seconds) the hitbox stays active. Match to the move's active frames / 60.")]
            public float duration = 0.1f;
        }

        [Tooltip("Add one entry per move type you want to deal damage. " +
                 "Any move type without an entry uses the Default Config below.")]
        [SerializeField] private List<HitboxConfig> configs = new List<HitboxConfig>
        {
            new HitboxConfig { moveType = MoveType.LightAttack,  damage = 8f,  knockbackForce = 3f,  size = new Vector2(1.5f, 1f),  offset = new Vector2(0.75f, 0.5f), duration = 0.083f },
            new HitboxConfig { moveType = MoveType.HeavyAttack,  damage = 20f, knockbackForce = 8f,  size = new Vector2(2f,   1.2f), offset = new Vector2(1f,    0.5f), duration = 0.1f  },
            new HitboxConfig { moveType = MoveType.SpecialAttack,damage = 15f, knockbackForce = 6f,  size = new Vector2(1.8f, 1.1f), offset = new Vector2(0.9f,  0.5f), duration = 0.1f  },
            new HitboxConfig { moveType = MoveType.AerialAttack, damage = 12f, knockbackForce = 5f,  size = new Vector2(1.5f, 1f),   offset = new Vector2(0.75f, 0f),   duration = 0.1f  },
        };

        [Header("Default Config (used when move type has no entry)")]
        [SerializeField] private HitboxConfig defaultConfig = new HitboxConfig();

        // -----------------------------------------------------------------------
        // Runtime
        // -----------------------------------------------------------------------

        private FighterController owner;
        private CombatFSM fsm;
        private ActiveHitbox currentHitbox;

        private void Start()
        {
            // Owner is the root FighterController
            owner = GetComponentInParent<FighterController>();

            // CombatFSM lives on the root
            fsm = GetComponentInParent<CombatFSM>();
            if (fsm != null)
            {
                fsm.OnHitboxActive   += OnHitboxActive;
                fsm.OnHitboxInactive += OnHitboxInactive;
            }
            else
            {
                Debug.LogWarning($"[HitboxEmitter] No CombatFSM found in parents of '{gameObject.name}'.");
            }
        }

        private void OnDestroy()
        {
            if (fsm != null)
            {
                fsm.OnHitboxActive   -= OnHitboxActive;
                fsm.OnHitboxInactive -= OnHitboxInactive;
            }
        }

        private void OnHitboxActive(MoveDef move)
        {
            // Destroy any lingering hitbox from a previous move
            ClearCurrentHitbox();

            HitboxConfig cfg = GetConfig(move != null ? move.moveType : MoveType.LightAttack);

            // Flip X offset when fighter faces left
            Vector2 offset = cfg.offset;
            if (owner != null && !owner.FacingRight)
                offset.x = -offset.x;

            GameObject hitboxObj = new GameObject("ActiveHitbox");
            hitboxObj.transform.SetParent(transform, false);
            hitboxObj.transform.localPosition = Vector3.zero;

            currentHitbox = hitboxObj.AddComponent<ActiveHitbox>();
            currentHitbox.Init(cfg.damage, cfg.knockbackForce, cfg.size, offset, cfg.duration, owner);
        }

        private void OnHitboxInactive()
        {
            ClearCurrentHitbox();
        }

        private void ClearCurrentHitbox()
        {
            if (currentHitbox != null)
            {
                Destroy(currentHitbox.gameObject);
                currentHitbox = null;
            }
        }

        private HitboxConfig GetConfig(MoveType moveType)
        {
            foreach (var c in configs)
                if (c.moveType == moveType) return c;
            return defaultConfig;
        }
    }
}
