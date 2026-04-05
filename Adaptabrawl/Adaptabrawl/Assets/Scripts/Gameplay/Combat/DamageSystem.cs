using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Combat;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Combat
{
    public class DamageSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FighterController fighterController;
        [SerializeField] private CombatFSM combatFSM;
        
        [Header("Events")]
        public System.Action<float, FighterController> OnDamageDealt;
        public System.Action<FighterController, MoveDef> OnBlocked;
        
        private void Start()
        {
            if (fighterController == null)
                fighterController = GetComponent<FighterController>();
            if (combatFSM == null)
                combatFSM = GetComponent<CombatFSM>();
        }
        
        public void DealDamage(FighterController target, MoveDef move, float hurtboxMultiplier = 1f)
        {
            if (target == null || move == null) return;
            
            // Calculate damage
            float baseDamage = move.damage;
            float damageMultiplier = fighterController != null && fighterController.FighterDef != null
                ? fighterController.FighterDef.baseDamageMultiplier
                : 1f;
            
            // Apply hurtbox multiplier (e.g., headshot bonus)
            damageMultiplier *= hurtboxMultiplier;
            
            // Apply status effect modifiers
            var statusSystem = GetComponent<StatusEffectSystem>();
            if (statusSystem != null)
            {
                damageMultiplier *= statusSystem.GetDamageMultiplier();
            }
            
            float finalDamage = baseDamage * damageMultiplier;
            
            // Apply target's defense multiplier (higher = takes less damage)
            if (target.FighterDef != null)
            {
                finalDamage /= target.FighterDef.baseDefenseMultiplier;
            }
            
            // Apply damage to target
            target.TakeDamage(finalDamage);
            
            // Apply hitstop
            if (move.hitstopFrames > 0)
            {
                var gameManager = Object.FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                    gameManager.TriggerHitStop(move.hitstopFrames / 60f);
            }
            
            // Apply hitstun
            var targetCombat = target.GetComponent<CombatFSM>();
            if (targetCombat != null)
            {
                targetCombat.SetStunned(move.hitstunFrames);
            }
            
            // Apply status effects
            ApplyStatusEffectsToTarget(target, move.statusEffectsOnHit);
            
            // Check for armor break
            CheckArmorBreak(target, move);
            
            OnDamageDealt?.Invoke(finalDamage, target);
        }
        
        public void HandleBlock(FighterController target, MoveDef move)
        {
            if (target == null || move == null) return;
            
            // Apply blockstun
            var targetCombat = target.GetComponent<CombatFSM>();
            if (targetCombat != null)
            {
                targetCombat.SetStunned(move.blockstunFrames);
            }
            
            OnBlocked?.Invoke(target, move);
        }
        
        private void ApplyStatusEffectsToTarget(FighterController target, StatusEffectData[] effects)
        {
            if (effects == null || effects.Length == 0) return;
            
            var targetStatusSystem = target.GetComponent<StatusEffectSystem>();
            if (targetStatusSystem != null)
            {
                foreach (var effect in effects)
                {
                    targetStatusSystem.ApplyStatus(effect.statusDef, effect.stacks, effect.duration);
                }
            }
        }
        
        private void CheckArmorBreak(FighterController target, MoveDef move)
        {
            if (move.moveType != MoveType.HeavyAttack) return;
            
            var targetCombat = target.GetComponent<CombatFSM>();
            if (targetCombat != null && targetCombat.CurrentState == CombatState.Active)
            {
                // Check if target has armor
                var targetMove = targetCombat.CurrentMove;
                if (targetMove != null && targetMove.armorFrames > 0)
                {
                    // Break armor
                    targetCombat.SetArmorBroken(2f); // 2 second armor break
                }
            }
        }
    }
}
