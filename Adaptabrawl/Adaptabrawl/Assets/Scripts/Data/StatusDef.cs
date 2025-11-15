using UnityEngine;

namespace Adaptabrawl.Data
{
    [CreateAssetMenu(fileName = "New Status", menuName = "Adaptabrawl/Status Definition")]
    public class StatusDef : ScriptableObject
    {
        [Header("Basic Info")]
        public string statusName;
        public StatusType statusType;
        public Sprite statusIcon;
        public Color statusColor = Color.white;
        
        [Header("Properties")]
        public bool canStack = false;
        public int maxStacks = 1;
        public float baseDuration = 5f;
        public bool refreshOnReapply = true; // Reset timer when reapplied
        
        [Header("Effects")]
        public StatModifier[] statModifiers;
        public bool isDamageOverTime = false;
        public float dotDamagePerTick = 1f;
        public float dotTickInterval = 0.5f; // Seconds between ticks
        
        [Header("Visual/Audio")]
        public GameObject visualEffectPrefab;
        public AudioClip applySound;
        public AudioClip tickSound;
        public AudioClip removeSound;
        
        [Header("UI")]
        public bool showTimer = true;
        public bool showStacks = false;
    }
    
    public enum StatusType
    {
        Poison,
        HeavyAttackState, // Slow movement, armor frames
        LowHPState, // Enhanced abilities when low HP
        Stagger, // Can't act
        ArmorBreak, // Armor is broken
        Buff,
        Debuff
    }
    
}

