using UnityEngine;

namespace Adaptabrawl.Data
{
    [CreateAssetMenu(fileName = "New Condition", menuName = "Adaptabrawl/Condition Definition")]
    public class ConditionDef : ScriptableObject
    {
        [Header("Basic Info")]
        public string conditionName;
        public ConditionType conditionType;
        public string description;
        public Sprite conditionIcon;
        
        [Header("Duration")]
        public float duration = 30f; // -1 for permanent
        public bool canStack = false;
        
        [Header("Modifiers")]
        public GlobalModifier[] globalModifiers; // Affects all fighters
        public FighterSpecificModifier[] fighterModifiers; // Affects specific fighters
        
        [Header("Triggers")]
        public ConditionTrigger[] triggers; // When this condition activates
        
        [Header("Visual/Audio")]
        public GameObject visualEffectPrefab;
        public AudioClip activateSound;
        public AudioClip ambientSound;
        
        [Header("UI")]
        public bool showBanner = true; // Show banner when condition activates
        public string bannerText;
    }
    
    public enum ConditionType
    {
        StageEnvironment, // Slippery floor, etc.
        Weather, // Thick fog, etc.
        MatchModifier, // Blood moon, etc.
        RoundState // Low HP state, etc.
    }
    
    [System.Serializable]
    public class GlobalModifier
    {
        public string propertyName; // "friction", "gravity", "projectileSpeed", etc.
        public ModifierOperation operation;
        public float value;
    }
    
    [System.Serializable]
    public class FighterSpecificModifier
    {
        public string fighterName; // "All" or specific fighter name
        public StatModifier[] statModifiers;
        public MoveModifier[] moveModifiers;
    }
    
    [System.Serializable]
    public class StatModifier
    {
        public string statName;
        public ModifierOperation operation;
        public float value;
    }
    
    [System.Serializable]
    public class MoveModifier
    {
        public string moveName; // "All" or specific move name
        public float damageMultiplier = 1f;
        public float speedMultiplier = 1f;
        public int additionalArmorFrames = 0;
    }
    
    [System.Serializable]
    public class ConditionTrigger
    {
        public TriggerType triggerType;
        public float triggerValue; // HP threshold, time, etc.
        public bool triggerOnce = false;
    }
    
    public enum TriggerType
    {
        OnMatchStart,
        OnRoundStart,
        OnHPBelow,
        OnTimeElapsed,
        OnConditionMet
    }
    
    public enum ModifierOperation
    {
        Add,
        Multiply,
        Set
    }
}

