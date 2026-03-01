using UnityEngine;
using Adaptabrawl.Data;
using System.Collections.Generic;
using System.Linq;

namespace Adaptabrawl.Gameplay
{
    public class StatusEffectSystem : MonoBehaviour
    {
        [Header("References")]
        private FighterController fighterController;
        
        [Header("Status Effects")]
        private Dictionary<StatusDef, ActiveStatusEffect> activeStatuses = new Dictionary<StatusDef, ActiveStatusEffect>();
        
        [Header("Events")]
        public System.Action<StatusDef, int> OnStatusApplied; // status, stacks
        public System.Action<StatusDef> OnStatusRemoved;
        public System.Action<StatusDef, int> OnStatusStackChanged; // status, new stacks
        public System.Action<float> OnDamageOverTime; // damage amount
        
        public void Initialize(FighterController controller)
        {
            fighterController = controller;
        }
        
        private void Update()
        {
            UpdateStatusEffects();
        }
        
        public void ApplyStatus(StatusDef statusDef, int stacks = 1, float duration = -1f)
        {
            if (statusDef == null) return;
            
            float actualDuration = duration > 0f ? duration : statusDef.baseDuration;
            
            if (activeStatuses.ContainsKey(statusDef))
            {
                // Status already active
                var existing = activeStatuses[statusDef];
                
                if (statusDef.canStack)
                {
                    // Add stacks
                    existing.stacks = Mathf.Min(existing.stacks + stacks, statusDef.maxStacks);
                    OnStatusStackChanged?.Invoke(statusDef, existing.stacks);
                }
                
                if (statusDef.refreshOnReapply)
                {
                    // Refresh duration
                    existing.duration = actualDuration;
                    existing.timer = actualDuration;
                }
            }
            else
            {
                // New status
                var newStatus = new ActiveStatusEffect
                {
                    statusDef = statusDef,
                    stacks = Mathf.Min(stacks, statusDef.maxStacks),
                    duration = actualDuration,
                    timer = actualDuration,
                    dotTimer = 0f
                };
                
                activeStatuses[statusDef] = newStatus;
                OnStatusApplied?.Invoke(statusDef, newStatus.stacks);
                
                // Apply stat modifiers
                ApplyStatModifiers(statusDef, newStatus.stacks);
                
                // Play apply sound
                if (statusDef.applySound != null)
                {
                    // AudioSource.PlayClipAtPoint(statusDef.applySound, transform.position);
                }
            }
        }
        
        public void RemoveStatus(StatusDef statusDef)
        {
            if (statusDef == null || !activeStatuses.ContainsKey(statusDef)) return;
            
            var status = activeStatuses[statusDef];
            
            // Remove stat modifiers
            RemoveStatModifiers(statusDef, status.stacks);
            
            activeStatuses.Remove(statusDef);
            OnStatusRemoved?.Invoke(statusDef);
            
            // Play remove sound
            if (statusDef.removeSound != null)
            {
                // AudioSource.PlayClipAtPoint(statusDef.removeSound, transform.position);
            }
        }
        
        public bool HasStatus(StatusDef statusDef)
        {
            return activeStatuses.ContainsKey(statusDef);
        }
        
        public int GetStatusStacks(StatusDef statusDef)
        {
            if (activeStatuses.ContainsKey(statusDef))
            {
                return activeStatuses[statusDef].stacks;
            }
            return 0;
        }
        
        private void UpdateStatusEffects()
        {
            List<StatusDef> toRemove = new List<StatusDef>();
            
            foreach (var kvp in activeStatuses)
            {
                var status = kvp.Value;
                status.timer -= Time.deltaTime;
                
                // Check duration
                if (status.duration > 0f && status.timer <= 0f)
                {
                    toRemove.Add(kvp.Key);
                    continue;
                }
                
                // Handle damage over time
                if (kvp.Key.isDamageOverTime)
                {
                    status.dotTimer -= Time.deltaTime;
                    if (status.dotTimer <= 0f)
                    {
                        float damage = kvp.Key.dotDamagePerTick * status.stacks;
                        if (fighterController != null)
                        {
                            fighterController.TakeDamage(damage);
                            OnDamageOverTime?.Invoke(damage);
                        }
                        
                        status.dotTimer = kvp.Key.dotTickInterval;
                        
                        // Play tick sound
                        if (kvp.Key.tickSound != null)
                        {
                            // AudioSource.PlayClipAtPoint(kvp.Key.tickSound, transform.position);
                        }
                    }
                }
            }
            
            // Remove expired statuses
            foreach (var statusDef in toRemove)
            {
                RemoveStatus(statusDef);
            }
        }
        
        private void ApplyStatModifiers(StatusDef statusDef, int stacks)
        {
            if (statusDef.statModifiers == null || fighterController == null) return;
            
            foreach (var modifier in statusDef.statModifiers)
            {
                ApplyStatModifier(modifier, stacks);
            }
        }
        
        private void RemoveStatModifiers(StatusDef statusDef, int stacks)
        {
            if (statusDef.statModifiers == null || fighterController == null) return;
            
            foreach (var modifier in statusDef.statModifiers)
            {
                RemoveStatModifier(modifier, stacks);
            }
        }
        
        private void ApplyStatModifier(StatModifier modifier, int stacks)
        {
            // This would modify fighter stats
            // Implementation depends on how stats are stored
            // For now, we'll store modifiers and apply them when needed
        }
        
        private void RemoveStatModifier(StatModifier modifier, int stacks)
        {
            // Remove stat modifier
        }
        
        public float GetDamageMultiplier()
        {
            float multiplier = 1f;
            
            foreach (var kvp in activeStatuses)
            {
                var status = kvp.Value;
                // Check for damage modifiers in stat modifiers
                // This is a simplified version
            }
            
            return multiplier;
        }
        
        public float GetSpeedMultiplier()
        {
            float multiplier = 1f;
            
            foreach (var kvp in activeStatuses)
            {
                var status = kvp.Value;
                // Check for speed modifiers
            }
            
            return multiplier;
        }
        
        public StatusDef[] GetActiveStatuses()
        {
            return activeStatuses.Keys.ToArray();
        }
        
        private class ActiveStatusEffect
        {
            public StatusDef statusDef;
            public int stacks;
            public float duration;
            public float timer;
            public float dotTimer;
        }
    }
}

