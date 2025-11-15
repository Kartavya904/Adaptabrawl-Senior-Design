using UnityEngine;

namespace Adaptabrawl.Data
{
    [CreateAssetMenu(fileName = "New Fighter", menuName = "Adaptabrawl/Fighter Definition")]
    public class FighterDef : ScriptableObject
    {
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
}

