using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Gameplay;
using System.Collections.Generic;

namespace Adaptabrawl.Combat
{
    public class HitboxManager : MonoBehaviour
    {
        [Header("Hitbox")]
        [SerializeField] private Transform hitboxParent;
        [SerializeField] private LayerMask hitLayers;
        
        private MoveDef activeMove = null;
        private BoxCollider2D hitboxCollider = null; // Legacy support
        private List<Collider2D> hitTargets = new List<Collider2D>();
        private HitboxHurtboxSpawner spawner;
        private int currentFrame = 0;
        
        [Header("Events")]
        public System.Action<Collider2D, MoveDef> OnHit;
        public System.Action<Collider2D, MoveDef> OnBlocked;
        
        private void Awake()
        {
            spawner = GetComponent<HitboxHurtboxSpawner>();
            
            // Legacy hitbox creation for backwards compatibility
            if (hitboxCollider == null && spawner == null)
            {
                GameObject hitboxObj = new GameObject("Hitbox");
                hitboxObj.transform.SetParent(hitboxParent != null ? hitboxParent : transform);
                hitboxObj.transform.localPosition = Vector3.zero;
                hitboxCollider = hitboxObj.AddComponent<BoxCollider2D>();
                hitboxCollider.isTrigger = true;
                hitboxCollider.enabled = false;
            }
        }
        
        public void ActivateHitbox(MoveDef move)
        {
            if (move == null) return;
            
            activeMove = move;
            hitTargets.Clear();
            currentFrame = 0;
            
            // Use new spawner system if available
            if (spawner != null)
            {
                spawner.SpawnHitboxesForMove(move);
            }
            else if (hitboxCollider != null)
            {
                // Legacy fallback
                hitboxCollider.size = move.hitboxSize;
                hitboxCollider.offset = move.hitboxOffset;
                hitboxCollider.enabled = true;
            }
        }
        
        public void DeactivateHitbox()
        {
            activeMove = null;
            currentFrame = 0;
            
            // Clear spawned hitboxes
            if (spawner != null)
            {
                spawner.ClearHitboxes();
            }
            else if (hitboxCollider != null)
            {
                hitboxCollider.enabled = false;
            }
            
            hitTargets.Clear();
        }
        
        /// <summary>
        /// Updates hitbox state based on current frame. Call this each frame during an attack.
        /// </summary>
        public void UpdateFrame(int frame)
        {
            currentFrame = frame;
            
            if (spawner != null && activeMove != null)
            {
                spawner.UpdateHitboxes(currentFrame);
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (activeMove == null) return;
            if (hitTargets.Contains(other)) return; // Already hit this target
            
            // Check if it's a valid target
            if (((1 << other.gameObject.layer) & hitLayers) == 0) return;
            
            // Check if it's a hurtbox
            var hurtbox = other.GetComponent<Hurtbox>();
            if (hurtbox == null) return;
            
            // Check if it's the same fighter
            if (hurtbox.Owner == GetComponent<FighterController>()) return;
            
            // Check if target is blocking
            var targetFighter = hurtbox.Owner;
            var targetCombat = targetFighter.GetComponent<CombatFSM>();
            
            if (targetCombat != null && targetCombat.CurrentState == CombatState.Blocking)
            {
                // Handle block
                HandleBlock(other, targetFighter);
            }
            else
            {
                // Handle hit
                HandleHit(other, targetFighter);
            }
            
            hitTargets.Add(other);
        }
        
        private void HandleHit(Collider2D hurtbox, FighterController target)
        {
            if (activeMove == null) return;
            
            var damageSystem = GetComponent<DamageSystem>();
            if (damageSystem != null)
            {
                // Get damage multiplier from hurtbox if spawner is available
                float hurtboxMultiplier = 1f;
                if (spawner != null)
                {
                    hurtboxMultiplier = spawner.GetHurtboxDamageMultiplier(hurtbox);
                }
                
                damageSystem.DealDamage(target, activeMove, hurtboxMultiplier);
            }
            
            OnHit?.Invoke(hurtbox, activeMove);
        }
        
        private void HandleBlock(Collider2D hurtbox, FighterController target)
        {
            if (activeMove == null) return;
            
            var damageSystem = GetComponent<DamageSystem>();
            if (damageSystem != null)
            {
                damageSystem.HandleBlock(target, activeMove);
            }
            
            OnBlocked?.Invoke(hurtbox, activeMove);
        }
    }
}

