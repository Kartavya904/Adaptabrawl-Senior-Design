using UnityEngine;

namespace Adaptabrawl.Data
{
    [CreateAssetMenu(fileName = "New Move", menuName = "Adaptabrawl/Move Definition")]
    public class MoveDef : ScriptableObject
    {
        [Header("Basic Info")]
        public string moveName;
        public MoveType moveType;
        
        [Header("Frame Data")]
        public int startupFrames = 5; // Frames before hitbox becomes active
        public int activeFrames = 3; // Frames where hitbox is active
        public int recoveryFrames = 10; // Frames after hitbox ends before can act
        public int totalFrames => startupFrames + activeFrames + recoveryFrames;
        
        [Header("Combat Properties")]
        public float damage = 10f;
        public float knockbackForce = 5f;
        public Vector2 knockbackDirection = Vector2.right;
        public int hitstopFrames = 5; // Freeze frames on hit
        public int blockstunFrames = 10; // Frames opponent is stunned when blocked
        public int hitstunFrames = 15; // Frames opponent is stunned when hit
        
        [Header("Hitbox")]
        [Tooltip("Use legacy single hitbox (backwards compatible) or leave empty to use hitboxDefinitions")]
        public Vector2 hitboxOffset = Vector2.zero;
        public Vector2 hitboxSize = new Vector2(1f, 1f);
        
        [Tooltip("Multiple hitboxes for complex attacks - overrides single hitbox if defined")]
        public HitboxDefinition[] hitboxDefinitions = new HitboxDefinition[]
        {
            new HitboxDefinition 
            { 
                name = "Primary", 
                offset = Vector2.zero, 
                size = new Vector2(1f, 1f),
                activeStartFrame = 0,
                activeEndFrame = -1, // -1 means use move's activeFrames
                damageMultiplier = 1f
            }
        };
        
        public bool isProjectile = false;
        public GameObject projectilePrefab; // If projectile
        
        [Header("Armor & Invincibility")]
        public int armorFrames = 0; // Frames with super armor (can't be interrupted)
        public int invincibilityFrames = 0; // Frames with invincibility
        public bool armorBreaksOnHeavy = true;
        
        [Header("Cancel Windows")]
        public bool canCancelIntoDodge = false;
        public bool canCancelIntoBlock = false;
        public bool canCancelIntoOtherMoves = false;
        public int cancelWindowStart = 0; // Frame when cancel window opens
        public int cancelWindowEnd = 0; // Frame when cancel window closes
        
        [Header("Input Buffer")]
        public float inputBufferWindow = 0.1f; // Seconds to buffer input
        
        [Header("Status Effects")]
        public StatusEffectData[] statusEffectsOnHit;
        public StatusEffectData[] statusEffectsOnSelf;
        
        [Header("Visual/Audio")]
        public GameObject hitEffectPrefab;
        public AudioClip hitSound;
        public AudioClip whiffSound;
    }
    
    public enum MoveType
    {
        LightAttack,
        HeavyAttack,
        Special,
        Dash,
        Dodge,
        Block,
        Parry
    }
    
    [System.Serializable]
    public class StatusEffectData
    {
        public StatusDef statusDef;
        public int stacks = 1;
        public float duration = 5f;
    }
    
    [System.Serializable]
    public class HitboxDefinition
    {
        [Tooltip("Name for identification")]
        public string name = "Hitbox";
        
        [Tooltip("Offset from fighter position")]
        public Vector2 offset;
        
        [Tooltip("Size of the hitbox")]
        public Vector2 size = new Vector2(1f, 1f);
        
        [Tooltip("Frame when this hitbox becomes active (relative to move start)")]
        public int activeStartFrame = 0;
        
        [Tooltip("Frame when this hitbox deactivates (-1 = use move's active frames)")]
        public int activeEndFrame = -1;
        
        [Tooltip("Damage multiplier for this specific hitbox (1.0 = normal, 1.5 = sweetspot)")]
        [Range(0.5f, 2f)]
        public float damageMultiplier = 1f;
        
        [Tooltip("Knockback direction override (leave zero to use move's default)")]
        public Vector2 knockbackDirectionOverride = Vector2.zero;
        
        [Tooltip("Knockback force multiplier")]
        [Range(0.5f, 2f)]
        public float knockbackMultiplier = 1f;
        
        [Tooltip("Is this a sweetspot (plays different effects)?")]
        public bool isSweetspot = false;
        
        [Tooltip("Color to display in editor")]
        public Color gizmoColor = new Color(0f, 1f, 0f, 0.3f);
    }
}

