using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Combat;
using Adaptabrawl.Gameplay;
using Adaptabrawl.VFX;
using Adaptabrawl.Camera;

namespace Adaptabrawl.Combat
{
    public class DamageSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FighterController fighterController;
        [SerializeField] private CombatFSM combatFSM;
        
        [Header("SFX")]
        [SerializeField] private AudioClip[] lightHitClips;
        [SerializeField] private AudioClip[] heavyHitClips;
        [SerializeField] private AudioClip[] blockClips;
        private AudioSource _audioSource;

        [Header("Events")]
        public System.Action<float, FighterController> OnDamageDealt;
        public System.Action<FighterController, MoveDef> OnBlocked;
        
        private void Start()
        {
            if (fighterController == null)
                fighterController = GetComponent<FighterController>();
            if (combatFSM == null)
                combatFSM = GetComponent<CombatFSM>();

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            if (lightHitClips == null || lightHitClips.Length == 0)
                lightHitClips = new AudioClip[] { Resources.Load<AudioClip>("SFX/hit") };
            if (heavyHitClips == null || heavyHitClips.Length == 0)
                heavyHitClips = new AudioClip[] { Resources.Load<AudioClip>("SFX/heavy_hit") };
            if (blockClips == null || blockClips.Length == 0)
                blockClips = new AudioClip[] { Resources.Load<AudioClip>("SFX/block") };
        }
        
        public void DealDamage(
            FighterController target,
            MoveDef move,
            float hurtboxMultiplier = 1f,
            Vector3? hitPosition = null,
            Vector3? hitDirection = null)
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
            ApplyKnockback(target, move);

            Vector3 resolvedHitPosition = hitPosition ?? target.transform.position;
            Vector3 resolvedHitDirection = hitDirection ?? GetHitDirection(target);
            DamageImpactSettings impactSettings = BuildImpactSettings(finalDamage, hurtboxMultiplier, move);

            VFXManager vfxManager = VFXManager.EnsureExists();
            if (vfxManager != null)
            {
                vfxManager.SpawnDamageImpact(resolvedHitPosition, resolvedHitDirection, impactSettings);
            }
            
            // Apply hitstop
            if (move.hitstopFrames > 0)
            {
                var gameManager = Object.FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                    gameManager.TriggerHitStop(move.hitstopFrames / 60f);
            }

            ImpactCameraShake cameraShake = ImpactCameraShake.EnsureExistsOnMainCamera();
            if (cameraShake != null)
            {
                float shakeStrength = Mathf.Clamp(0.18f + impactSettings.Intensity * 0.42f, 0.24f, 0.8f);
                float shakeDuration = Mathf.Clamp(0.08f + move.hitstopFrames / 100f, 0.1f, 0.22f);
                float rotationStrength = Mathf.Clamp(0.65f + impactSettings.Intensity * 1.2f, 0.85f, 2.4f);
                cameraShake.AddShake(shakeStrength, shakeDuration, rotationStrength);
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
            PlayHitSound(move.moveType == MoveType.HeavyAttack || move.moveType == MoveType.Special);

            OnDamageDealt?.Invoke(finalDamage, target);
        }

        private void ApplyKnockback(FighterController target, MoveDef move)
        {
            if (target == null || target.IsDead) return;
            var rb = target.GetComponent<Rigidbody2D>();
            if (rb == null) return;

            Vector3 dir = GetHitDirection(target);
            float strength = move.moveType switch
            {
                MoveType.HeavyAttack => 5.5f,
                MoveType.Special     => 4.5f,
                _                   => 2.5f
            };
            rb.AddForce(new Vector2(dir.x, 0.3f) * strength, ForceMode2D.Impulse);
        }

        private Vector3 GetHitDirection(FighterController target)
        {
            if (fighterController == null || target == null)
                return Vector3.right;

            Vector3 delta = target.transform.position - fighterController.transform.position;
            delta.y = 0f;
            delta.z = 0f;

            if (delta.sqrMagnitude < 0.0001f)
                return fighterController.FacingRight ? Vector3.right : Vector3.left;

            return delta.normalized;
        }

        private DamageImpactSettings BuildImpactSettings(float finalDamage, float hurtboxMultiplier, MoveDef move)
        {
            float intensity = Mathf.Clamp01(finalDamage / 22f);
            intensity += Mathf.Clamp01((hurtboxMultiplier - 1f) * 0.6f);

            if (move.moveType == MoveType.HeavyAttack)
                intensity += 0.25f;
            else if (move.moveType == MoveType.Special)
                intensity += 0.12f;

            FighterDef attackerDef = fighterController != null ? fighterController.FighterDef : null;

            float particleMultiplier = 1f;
            float splashRangeMultiplier = 1f;
            float particleSizeMultiplier = 1f;

            if (attackerDef != null)
            {
                switch (attackerDef.playStyle)
                {
                    case FighterPlayStyle.Strength:
                        particleMultiplier *= 1.45f;
                        splashRangeMultiplier *= 1.28f;
                        particleSizeMultiplier *= 1.12f;
                        break;
                    case FighterPlayStyle.Defense:
                        particleMultiplier *= 0.95f;
                        splashRangeMultiplier *= 0.92f;
                        particleSizeMultiplier *= 1.05f;
                        break;
                    case FighterPlayStyle.Invasion:
                        particleMultiplier *= 0.82f;
                        splashRangeMultiplier *= 0.98f;
                        particleSizeMultiplier *= 0.92f;
                        break;
                    default:
                        particleMultiplier *= 1.05f;
                        splashRangeMultiplier *= 1.06f;
                        break;
                }

                float heftBonus = Mathf.InverseLerp(0.75f, 1.8f, attackerDef.weight);
                particleMultiplier += heftBonus * 0.4f;
                splashRangeMultiplier += heftBonus * 0.3f;
                particleSizeMultiplier += heftBonus * 0.18f;
            }

            particleMultiplier *= Mathf.Lerp(1f, 1.28f, intensity);
            splashRangeMultiplier *= Mathf.Lerp(1f, 1.22f, intensity);

            intensity = Mathf.Clamp01(intensity);
            return new DamageImpactSettings(intensity, particleMultiplier, splashRangeMultiplier, particleSizeMultiplier);
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

        private void PlayHitSound(bool isHeavy)
        {
            if (_audioSource == null) return;
            var clips = isHeavy ? heavyHitClips : lightHitClips;
            if (clips != null && clips.Length > 0)
                _audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }

        private void PlayBlockSound()
        {
            if (_audioSource == null || blockClips == null || blockClips.Length == 0) return;
            _audioSource.PlayOneShot(blockClips[Random.Range(0, blockClips.Length)]);
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
