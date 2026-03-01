using UnityEngine;

namespace Adaptabrawl.Data
{
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
        public string moveName;
        public float damageMultiplier = 1f;
        public float speedMultiplier = 1f;
        public int additionalArmorFrames = 0;
    }
    
    public enum ModifierOperation
    {
        Add,
        Multiply,
        Set
    }
}

