using UnityEngine;
using Adaptabrawl.Combat;
using Adaptabrawl.Data;

namespace Adaptabrawl.VFX
{
    public class VFXManager : MonoBehaviour
    {
        [Header("Hit Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject blockEffectPrefab;
        [SerializeField] private GameObject parryEffectPrefab;
        
        [Header("Status Effects")]
        [SerializeField] private GameObject poisonEffectPrefab;
        [SerializeField] private GameObject heavyAttackEffectPrefab;
        [SerializeField] private GameObject lowHPEffectPrefab;
        
        [Header("Condition Effects")]
        [SerializeField] private GameObject conditionBannerPrefab;
        
        private void Start()
        {
            // Subscribe to combat events
            var fighters = FindObjectsOfType<FighterController>();
            foreach (var fighter in fighters)
            {
                var damageSystem = fighter.GetComponent<DamageSystem>();
                if (damageSystem != null)
                {
                    damageSystem.OnDamageDealt += OnDamageDealt;
                    damageSystem.OnBlocked += OnBlocked;
                }
                
                var hitboxManager = fighter.GetComponent<HitboxManager>();
                if (hitboxManager != null)
                {
                    hitboxManager.OnHit += OnHit;
                    hitboxManager.OnBlocked += OnBlocked;
                }
            }
        }
        
        private void OnHit(Collider2D hurtbox, MoveDef move)
        {
            if (hurtbox == null || move == null) return;
            
            // Spawn hit effect
            Vector3 hitPosition = hurtbox.transform.position;
            SpawnEffect(hitEffectPrefab, hitPosition);
            
            // Play hit sound
            if (move.hitSound != null)
            {
                AudioSource.PlayClipAtPoint(move.hitSound, hitPosition);
            }
        }
        
        private void OnBlocked(Collider2D hurtbox, MoveDef move)
        {
            if (hurtbox == null || move == null) return;
            
            // Spawn block effect
            Vector3 blockPosition = hurtbox.transform.position;
            SpawnEffect(blockEffectPrefab, blockPosition);
        }
        
        private void OnDamageDealt(float damage, FighterController target)
        {
            // Could spawn damage numbers, screen shake, etc.
        }
        
        public void SpawnParryEffect(Vector3 position)
        {
            SpawnEffect(parryEffectPrefab, position);
        }
        
        public void SpawnStatusEffect(StatusDef status, Vector3 position)
        {
            GameObject effectPrefab = null;
            
            switch (status.statusType)
            {
                case StatusType.Poison:
                    effectPrefab = poisonEffectPrefab;
                    break;
                case StatusType.HeavyAttackState:
                    effectPrefab = heavyAttackEffectPrefab;
                    break;
                case StatusType.LowHPState:
                    effectPrefab = lowHPEffectPrefab;
                    break;
            }
            
            if (effectPrefab != null)
            {
                SpawnEffect(effectPrefab, position);
            }
        }
        
        private void SpawnEffect(GameObject prefab, Vector3 position)
        {
            if (prefab != null)
            {
                GameObject effect = Instantiate(prefab, position, Quaternion.identity);
                // Auto-destroy after particle system completes
                ParticleSystem ps = effect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
                }
                else
                {
                    Destroy(effect, 2f); // Default destroy time
                }
            }
        }
    }
}

