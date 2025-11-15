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
        public Vector2 hitboxOffset = Vector2.zero;
        public Vector2 hitboxSize = new Vector2(1f, 1f);
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
}

