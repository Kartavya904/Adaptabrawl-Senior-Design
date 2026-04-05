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
            [Tooltip("3D Local offset from the emitter transform.")]
            public Vector3 offset = new Vector3(0.75f, 0.5f, 0f);

            [Header("Timing")]
            [Tooltip("How long (seconds) the hitbox stays active. Match to the move's active frames / 60.")]
            public float duration = 0.1f;
        }

        [Tooltip("Add one entry per move type you want to deal damage. " +
                 "Any move type without an entry uses the Default Config below.")]
        [SerializeField] private List<HitboxConfig> configs = new List<HitboxConfig>
        {
            new HitboxConfig { moveType = MoveType.LightAttack,  damage = 8f,  knockbackForce = 3f,  size = new Vector2(1.5f, 1f),  offset = new Vector3(0.75f, 0.5f, 0f), duration = 0.083f },
            new HitboxConfig { moveType = MoveType.HeavyAttack,  damage = 20f, knockbackForce = 8f,  size = new Vector2(2f,   1.2f), offset = new Vector3(1f,    0.5f, 0f), duration = 0.1f  },
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
            enabled = false;
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
            Debug.Log($"[HitboxEmitter] OnHitboxActive — move='{move?.moveName}', spawning hitbox on '{gameObject.name}'.");
            ClearCurrentHitbox();

            HitboxConfig cfg = GetConfig(move != null ? move.moveType : MoveType.LightAttack);

            // In previous static system, offset was a world-space 2D offset from Stander.
            // Since HitboxEmitter is now parented to the actual weapon, we apply offset
            // physically to local space, so it naturally rotates with the weapon swing!
            
            Vector3 localOffset = cfg.offset;
            if (owner != null && !owner.FacingRight)
                localOffset.x = -localOffset.x; // Optional flip logic if needed locally

            GameObject hitboxObj = new GameObject("ActiveHitbox");
            hitboxObj.transform.SetParent(transform, false);
            hitboxObj.transform.localPosition = localOffset;
            hitboxObj.transform.localRotation = Quaternion.identity;

            currentHitbox = hitboxObj.AddComponent<ActiveHitbox>();
            // Pass zero offset to ActiveHitbox, because we moved its transform.position already!
            currentHitbox.Init(cfg.damage, cfg.knockbackForce, cfg.size, Vector2.zero, cfg.duration, owner);

            // Phase 1: Toggle Weapon Trail!
            var tr = GetComponentInParent<TrailRenderer>();
            if (tr == null)
            {
                tr = transform.parent.gameObject.AddComponent<TrailRenderer>();
                tr.time = 0.15f;
                tr.startWidth = 0.4f;
                tr.endWidth = 0f;
                tr.material = new Material(Shader.Find("Sprites/Default"));
                tr.material.color = new Color(1f, 0.5f, 0f, 0.8f); // Bright orange slash
                tr.emitting = false;
            }
            tr.emitting = true;
        }

        private void OnHitboxInactive()
        {
            ClearCurrentHitbox();
            var tr = GetComponentInParent<TrailRenderer>();
            if (tr != null) tr.emitting = false;
        }

        private void ClearCurrentHitbox()
        {
            if (currentHitbox != null)
            {
                Destroy(currentHitbox.gameObject);
                currentHitbox = null;
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Visualize in the editor properly translated and rotated by the weapon
            Gizmos.matrix = transform.localToWorldMatrix;
            foreach(var cfg in configs)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
                Vector3 localOffset = cfg.offset;
                Gizmos.DrawCube(localOffset, new Vector3(cfg.size.x, cfg.size.y, 0.1f));
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 1f);
                Gizmos.DrawWireCube(localOffset, new Vector3(cfg.size.x, cfg.size.y, 0.1f));
            }
            Gizmos.matrix = Matrix4x4.identity;
        }

        private HitboxConfig GetConfig(MoveType moveType)
        {
            foreach (var c in configs)
                if (c.moveType == moveType) return c;
            return defaultConfig;
        }

        /// <summary>
        /// Replaces the Light/Heavy hitbox configs with weapon-mesh-derived values.
        /// Called at runtime by StanderCombatSetup so hitboxes track the weapon's
        /// actual striking surface without requiring an editor bake step.
        /// </summary>
        public void ApplyWeaponConfig(Vector3 lightOffset, Vector2 lightSize,
                                      Vector3 heavyOffset, Vector2 heavySize)
        {
            configs.Clear();
            configs.Add(new HitboxConfig
            {
                moveType       = MoveType.LightAttack,
                damage         = 8f,
                knockbackForce = 3f,
                size           = lightSize,
                offset         = lightOffset,
                duration       = 0.083f
            });
            configs.Add(new HitboxConfig
            {
                moveType       = MoveType.HeavyAttack,
                damage         = 20f,
                knockbackForce = 8f,
                size           = heavySize,
                offset         = heavyOffset,
                duration       = 0.1f
            });
            defaultConfig.offset = lightOffset;
            defaultConfig.size   = lightSize;
        }
    }
}
