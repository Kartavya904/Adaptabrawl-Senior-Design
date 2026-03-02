using UnityEngine;

namespace Adaptabrawl.Data
{
    [CreateAssetMenu(fileName = "New Fighter", menuName = "Adaptabrawl/Fighter Definition")]
    public class FighterDef : ScriptableObject
    {
        [Header("Visual Prefab")]
        [Tooltip("Drag a Shinabro prefab here (e.g., Player_Fighter, Player_Hammer, etc.)")]
        public GameObject fighterPrefab;
        
        [Header("Basic Info")]
        public string fighterName;
        public string description;
        public Sprite portrait;
        
        [Header("Base Stats")]
        public float maxHealth = 100f;
        public float moveSpeed = 5f;
        public float jumpForce = 10f;
        public float dashSpeed = 12f;
        public float dashDuration = 0.3f;
        public float weight = 1f; // Affects knockback
        
        [Header("Combat Stats")]
        public float baseDamageMultiplier = 1f;
        public float baseDefenseMultiplier = 1f;
        public int armorBreakThreshold = 3; // Hits needed to break armor
        
        [Header("Hurtbox Configuration")]
        [Tooltip("Default hurtboxes for this fighter - automatically created on spawn")]
        public HurtboxDefinition[] hurtboxes = new HurtboxDefinition[]
        {
            new HurtboxDefinition 
            { 
                name = "Body", 
                offset = Vector2.zero, 
                size = new Vector2(1f, 2f),
                isActive = true,
                damageMultiplier = 1f
            },
            new HurtboxDefinition 
            { 
                name = "Head", 
                offset = new Vector2(0f, 1.5f), 
                size = new Vector2(0.5f, 0.5f),
                isActive = true,
                damageMultiplier = 1.2f // Head takes more damage
            }
        };
        
        [Header("Moveset (Core)")]
        [Tooltip("Primary fast ground attack (usually Shinabro Attack1).")]
        public MoveDef lightAttack;
        [Tooltip("Primary heavy/finisher ground attack (usually Shinabro Attack3).")]
        public MoveDef heavyAttack;
        [Tooltip("Array of specials/skills (usually Shinabro Skill1–Skill8).")]
        public MoveDef[] specialMoves;

        [Header("Moveset (Extended / Shinabro)")]
        [Tooltip("Optional: full Shinabro move library generated for this fighter's weapon type.")]
        public MoveLibrary moveLibrary;

        [Tooltip("Preferred jump/air attack used for previews or specialized input (typically JumpAttack1).")]
        public AnimatedMoveDef jumpAttackPrimary;

        [Tooltip("Preferred aerial special used for previews or air-combo starters (typically Skill8_Air).")]
        public AnimatedMoveDef aerialSpecial;

        [Tooltip("Optional: attack performed out of a dodge (Shinabro DodgeAttack).")]
        public AnimatedMoveDef dodgeAttack;

        [Tooltip("Optional: crouching attack (Shinabro CrouchAttack).")]
        public AnimatedMoveDef crouchAttack;
        
        [Header("Adaptation Hooks")]
        public bool canAdaptToConditions = true;
        public AdaptationRule[] adaptationRules;
    }
    
    [System.Serializable]
    public class AdaptationRule
    {
        public string conditionName;
        public StatModifier[] statModifiers;
        public MoveModifier[] moveModifiers;
    }
    
    /// <summary>
    /// Shape of a single hurtbox so it can match body parts (box for torso/head, capsule for limbs).
    /// </summary>
    public enum HurtboxShape
    {
        Box,
        Capsule
    }

    [System.Serializable]
    public class HurtboxDefinition
    {
        [Tooltip("Name for identification (e.g. Body, Head, LeftArm, RightLeg)")]
        public string name = "Hurtbox";
        
        [Tooltip("Offset from fighter center")]
        public Vector2 offset;
        
        [Tooltip("Size of the hurtbox (for Box: width/height; for Capsule: width = diameter, height = length)")]
        public Vector2 size = new Vector2(1f, 1f);
        
        [Tooltip("Box = rectangular part (torso, head). Capsule = rounded part (limbs).")]
        public HurtboxShape shape = HurtboxShape.Box;
        
        [Tooltip("For Capsule: Vertical = tall, Horizontal = wide. Ignored for Box.")]
        public CapsuleDirection2D capsuleDirection = CapsuleDirection2D.Vertical;
        
        [Tooltip("Is this hurtbox active by default?")]
        public bool isActive = true;
        
        [Tooltip("Damage multiplier for hits on this hurtbox (1.0 = normal, 1.5 = critical area)")]
        [Range(0.5f, 2f)]
        public float damageMultiplier = 1f;
        
        [Tooltip("Color to display in editor")]
        public Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);
    }
}

