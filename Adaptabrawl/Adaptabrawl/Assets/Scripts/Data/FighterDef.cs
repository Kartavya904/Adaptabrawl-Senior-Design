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
        
        [Header("Moveset")]
        public MoveDef lightAttack;
        public MoveDef heavyAttack;
        public MoveDef[] specialMoves;
        
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
    
    [System.Serializable]
    public class HurtboxDefinition
    {
        [Tooltip("Name for identification")]
        public string name = "Hurtbox";
        
        [Tooltip("Offset from fighter center")]
        public Vector2 offset;
        
        [Tooltip("Size of the hurtbox")]
        public Vector2 size = new Vector2(1f, 1f);
        
        [Tooltip("Is this hurtbox active by default?")]
        public bool isActive = true;
        
        [Tooltip("Damage multiplier for hits on this hurtbox (1.0 = normal, 1.5 = critical area)")]
        [Range(0.5f, 2f)]
        public float damageMultiplier = 1f;
        
        [Tooltip("Color to display in editor")]
        public Color gizmoColor = new Color(1f, 0f, 0f, 0.3f);
    }
}

