using UnityEngine;
using Adaptabrawl.Data;
using System.Collections.Generic;
using System.Linq;

namespace Adaptabrawl.Gameplay
{
    public class AdaptiveConditionSystem : MonoBehaviour
    {
        [Header("Active Conditions")]
        private List<ActiveCondition> activeConditions = new List<ActiveCondition>();
        
        [Header("References")]
        private List<FighterController> fighters = new List<FighterController>();
        
        [Header("Events")]
        public System.Action<ConditionDef> OnConditionActivated;
        public System.Action<ConditionDef> OnConditionDeactivated;
        public System.Action<string> OnConditionBanner; // For UI banner display
        
        private void Start()
        {
            // Find all fighters in scene
            fighters.AddRange(FindObjectsOfType<FighterController>());
        }
        
        public void ActivateCondition(ConditionDef conditionDef)
        {
            if (conditionDef == null) return;
            
            // Check if condition already active
            if (activeConditions.Any(c => c.conditionDef == conditionDef))
            {
                // Refresh if can stack or refresh
                var existing = activeConditions.First(c => c.conditionDef == conditionDef);
                if (conditionDef.canStack || conditionDef.duration > 0f)
                {
                    existing.timer = conditionDef.duration;
                }
                return;
            }
            
            // Create new active condition
            var activeCondition = new ActiveCondition
            {
                conditionDef = conditionDef,
                timer = conditionDef.duration,
                appliedModifiers = new List<AppliedModifier>()
            };
            
            activeConditions.Add(activeCondition);
            
            // Apply modifiers
            ApplyConditionModifiers(conditionDef);
            
            // Show banner
            if (conditionDef.showBanner)
            {
                string bannerText = !string.IsNullOrEmpty(conditionDef.bannerText) 
                    ? conditionDef.bannerText 
                    : conditionDef.conditionName;
                OnConditionBanner?.Invoke(bannerText);
            }
            
            OnConditionActivated?.Invoke(conditionDef);
            
            // Play activate sound
            if (conditionDef.activateSound != null)
            {
                // AudioSource.PlayClipAtPoint(conditionDef.activateSound, Vector3.zero);
            }
        }
        
        public void DeactivateCondition(ConditionDef conditionDef)
        {
            if (conditionDef == null) return;
            
            var condition = activeConditions.FirstOrDefault(c => c.conditionDef == conditionDef);
            if (condition == null) return;
            
            // Remove modifiers
            RemoveConditionModifiers(conditionDef);
            
            activeConditions.Remove(condition);
            OnConditionDeactivated?.Invoke(conditionDef);
        }
        
        private void Update()
        {
            UpdateConditions();
        }
        
        private void UpdateConditions()
        {
            List<ActiveCondition> toRemove = new List<ActiveCondition>();
            
            foreach (var condition in activeConditions)
            {
                if (condition.conditionDef.duration > 0f)
                {
                    condition.timer -= Time.deltaTime;
                    if (condition.timer <= 0f)
                    {
                        toRemove.Add(condition);
                    }
                }
            }
            
            foreach (var condition in toRemove)
            {
                DeactivateCondition(condition.conditionDef);
            }
        }
        
        private void ApplyConditionModifiers(ConditionDef conditionDef)
        {
            // Apply global modifiers
            if (conditionDef.globalModifiers != null)
            {
                foreach (var modifier in conditionDef.globalModifiers)
                {
                    ApplyGlobalModifier(modifier);
                }
            }
            
            // Apply fighter-specific modifiers
            if (conditionDef.fighterModifiers != null)
            {
                foreach (var fighterMod in conditionDef.fighterModifiers)
                {
                    ApplyFighterModifiers(fighterMod);
                }
            }
        }
        
        private void RemoveConditionModifiers(ConditionDef conditionDef)
        {
            // Remove global modifiers
            if (conditionDef.globalModifiers != null)
            {
                foreach (var modifier in conditionDef.globalModifiers)
                {
                    RemoveGlobalModifier(modifier);
                }
            }
            
            // Remove fighter-specific modifiers
            if (conditionDef.fighterModifiers != null)
            {
                foreach (var fighterMod in conditionDef.fighterModifiers)
                {
                    RemoveFighterModifiers(fighterMod);
                }
            }
        }
        
        private void ApplyGlobalModifier(GlobalModifier modifier)
        {
            // Apply global physics/environment modifiers
            // This would modify things like friction, gravity, etc.
            // Implementation depends on how these are stored
        }
        
        private void RemoveGlobalModifier(GlobalModifier modifier)
        {
            // Remove global modifier
        }
        
        private void ApplyFighterModifiers(FighterSpecificModifier fighterMod)
        {
            foreach (var fighter in fighters)
            {
                if (fighterMod.fighterName == "All" || fighter.FighterDef.fighterName == fighterMod.fighterName)
                {
                    ApplyFighterStatModifiers(fighter, fighterMod.statModifiers);
                    ApplyFighterMoveModifiers(fighter, fighterMod.moveModifiers);
                }
            }
        }
        
        private void RemoveFighterModifiers(FighterSpecificModifier fighterMod)
        {
            foreach (var fighter in fighters)
            {
                if (fighterMod.fighterName == "All" || fighter.FighterDef.fighterName == fighterMod.fighterName)
                {
                    RemoveFighterStatModifiers(fighter, fighterMod.statModifiers);
                    RemoveFighterMoveModifiers(fighter, fighterMod.moveModifiers);
                }
            }
        }
        
        private void ApplyFighterStatModifiers(FighterController fighter, StatModifier[] modifiers)
        {
            if (modifiers == null) return;
            
            // Apply stat modifiers to fighter
            // This would modify fighter stats like moveSpeed, damage, etc.
        }
        
        private void RemoveFighterStatModifiers(FighterController fighter, StatModifier[] modifiers)
        {
            if (modifiers == null) return;
            
            // Remove stat modifiers
        }
        
        private void ApplyFighterMoveModifiers(FighterController fighter, MoveModifier[] modifiers)
        {
            if (modifiers == null) return;
            
            // Apply move modifiers
            // This would modify move properties like damage, speed, armor frames
        }
        
        private void RemoveFighterMoveModifiers(FighterController fighter, MoveModifier[] modifiers)
        {
            if (modifiers == null) return;
            
            // Remove move modifiers
        }
        
        public ConditionDef[] GetActiveConditions()
        {
            return activeConditions.Select(c => c.conditionDef).ToArray();
        }
        
        private class ActiveCondition
        {
            public ConditionDef conditionDef;
            public float timer;
            public List<AppliedModifier> appliedModifiers;
        }
        
        private class AppliedModifier
        {
            public string targetName;
            public string propertyName;
            public float originalValue;
            public float modifiedValue;
        }
    }
}

