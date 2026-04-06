using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Fighters
{
    public static class FighterFactory
    {
        public static readonly Color DefaultShadowSilhouetteColor = new Color(0.02f, 0.03f, 0.05f, 1f);

        public static FighterController CreateFighter(FighterDef fighterDef, Vector3 position, bool facingRight = true)
        {
            if (fighterDef == null)
            {
                Debug.LogError("FighterFactory: FighterDef is null!");
                return null;
            }
            
            GameObject fighterObj;
            
            // Use prefab if assigned, otherwise create from scratch
            if (fighterDef.fighterPrefab != null)
            {
                // Instantiate from Shinabro prefab
                fighterObj = Object.Instantiate(fighterDef.fighterPrefab, position, Quaternion.identity);
                fighterObj.name = fighterDef.fighterName;
                
                Debug.Log($"FighterFactory: Created fighter '{fighterDef.fighterName}' from prefab '{fighterDef.fighterPrefab.name}'");
            }
            else
            {
                // Create empty GameObject as fallback
                fighterObj = new GameObject(fighterDef.fighterName);
                fighterObj.transform.position = position;
                
                // Add visual representation (placeholder)
                SpriteRenderer spriteRenderer = fighterObj.AddComponent<SpriteRenderer>();
                spriteRenderer.color = Color.white;
                
                Debug.LogWarning($"FighterFactory: No prefab assigned for '{fighterDef.fighterName}', creating placeholder.");
            }
            
            // Add FighterController (main component)
            FighterController fighterController = fighterObj.GetComponent<FighterController>();
            if (fighterController == null)
            {
                fighterController = fighterObj.AddComponent<FighterController>();
            }
            fighterController.SetFighterDef(fighterDef);
            
            // Combat state machine (root — no colliders)
            EnsureComponent<Combat.CombatFSM>(fighterObj);

            // Movement
            EnsureComponent<MovementController>(fighterObj);

            // Systems (root)
            EnsureComponent<StatusEffectSystem>(fighterObj);
            EnsureComponent<Combat.DamageSystem>(fighterObj);
            EnsureComponent<Attack.AttackSystem>(fighterObj);
            EnsureComponent<Defend.DefenseSystem>(fighterObj);
            EnsureComponent<Evade.EvadeSystem>(fighterObj);
            EnsureComponent<Input.PlayerInputHandler>(fighterObj);

            // Animation bridge
            EnsureComponent<AnimationBridge>(fighterObj);

            // Bootstrap: adds CameraBoundsConstraint to the Stander child in Start()
            EnsureComponent<StanderCameraConstraint>(fighterObj);

            // Bootstrap: adds FighterHurtbox + HitboxEmitter to the Stander child in Start()
            // so all combat colliders follow root-motion instead of staying on the root.
            EnsureComponent<StanderCombatSetup>(fighterObj);

            // Keep the full character and weapon set reading as one clean shadow shape in-match.
            var shadowVisual = EnsureComponent<ShadowSilhouetteVisual>(fighterObj);
            shadowVisual.Configure(DefaultShadowSilhouetteColor, disableParticleSystems: false, disableTrails: false);
            
            // Set facing
            fighterController.SetFacing(facingRight);
            
            return fighterController;
        }
        
        /// <summary>
        /// Ensures a component exists on the GameObject, adding it if necessary.
        /// </summary>
        private static T EnsureComponent<T>(GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                component = obj.AddComponent<T>();
            }
            return component;
        }
        
        public static FighterDef CreateStrikerFighter()
        {
            FighterDef striker = ScriptableObject.CreateInstance<FighterDef>();
            striker.fighterName = "Striker";
            striker.description = "A pressure-focused fighter with frame traps and heavy attacks.";
            striker.maxHealth = 100f;
            striker.moveSpeed = 6f;
            striker.jumpForce = 12f;
            striker.dashSpeed = 15f;
            striker.dashDuration = 0.25f;
            striker.weight = 1.2f;
            striker.baseDamageMultiplier = 1.1f;
            striker.baseDefenseMultiplier = 0.9f;
            striker.playStyle = FighterPlayStyle.Strength;
            
            // Create moves
            striker.lightAttack = CreateStrikerLightAttack();
            striker.heavyAttack = CreateStrikerHeavyAttack();
            
            return striker;
        }
        
        public static FighterDef CreateElusiveFighter()
        {
            FighterDef elusive = ScriptableObject.CreateInstance<FighterDef>();
            elusive.fighterName = "Elusive";
            elusive.description = "A mobile fighter with dodge cancels and counter windows.";
            elusive.maxHealth = 90f;
            elusive.moveSpeed = 7f;
            elusive.jumpForce = 14f;
            elusive.dashSpeed = 18f;
            elusive.dashDuration = 0.2f;
            elusive.weight = 0.8f;
            elusive.baseDamageMultiplier = 0.9f;
            elusive.baseDefenseMultiplier = 1.0f;
            elusive.playStyle = FighterPlayStyle.Invasion;
            
            // Create moves
            elusive.lightAttack = CreateElusiveLightAttack();
            elusive.heavyAttack = CreateElusiveHeavyAttack();
            
            return elusive;
        }
        
        private static MoveDef CreateStrikerLightAttack()
        {
            MoveDef move = ScriptableObject.CreateInstance<MoveDef>();
            move.moveName = "Light Punch";
            move.moveType = MoveType.LightAttack;
            move.startupFrames = 4;
            move.activeFrames = 2;
            move.recoveryFrames = 8;
            move.damage = 8f;
            move.knockbackForce = 3f;
            move.hitstopFrames = 3;
            move.hitstunFrames = 12;
            move.blockstunFrames = 8;
            move.hitboxSize = new Vector2(1.2f, 1f);
            move.hitboxOffset = new Vector2(0.6f, 0f);
            move.canCancelIntoDodge = true;
            move.canCancelIntoOtherMoves = true;
            move.cancelWindowStart = 6;
            move.cancelWindowEnd = 12;
            return move;
        }
        
        private static MoveDef CreateStrikerHeavyAttack()
        {
            MoveDef move = ScriptableObject.CreateInstance<MoveDef>();
            move.moveName = "Heavy Strike";
            move.moveType = MoveType.HeavyAttack;
            move.startupFrames = 12;
            move.activeFrames = 4;
            move.recoveryFrames = 20;
            move.damage = 20f;
            move.knockbackForce = 8f;
            move.hitstopFrames = 8;
            move.hitstunFrames = 25;
            move.blockstunFrames = 15;
            move.hitboxSize = new Vector2(1.5f, 1.2f);
            move.hitboxOffset = new Vector2(0.8f, 0f);
            move.armorFrames = 8;
            move.armorBreaksOnHeavy = true;
            move.canCancelIntoDodge = false;
            move.canCancelIntoOtherMoves = false;
            return move;
        }
        
        private static MoveDef CreateElusiveLightAttack()
        {
            MoveDef move = ScriptableObject.CreateInstance<MoveDef>();
            move.moveName = "Quick Strike";
            move.moveType = MoveType.LightAttack;
            move.startupFrames = 3;
            move.activeFrames = 2;
            move.recoveryFrames = 6;
            move.damage = 6f;
            move.knockbackForce = 2f;
            move.hitstopFrames = 2;
            move.hitstunFrames = 10;
            move.blockstunFrames = 6;
            move.hitboxSize = new Vector2(1f, 0.8f);
            move.hitboxOffset = new Vector2(0.5f, 0f);
            move.canCancelIntoDodge = true;
            move.canCancelIntoOtherMoves = true;
            move.cancelWindowStart = 4;
            move.cancelWindowEnd = 8;
            return move;
        }
        
        private static MoveDef CreateElusiveHeavyAttack()
        {
            MoveDef move = ScriptableObject.CreateInstance<MoveDef>();
            move.moveName = "Counter Strike";
            move.moveType = MoveType.HeavyAttack;
            move.startupFrames = 8;
            move.activeFrames = 3;
            move.recoveryFrames = 15;
            move.damage = 16f;
            move.knockbackForce = 6f;
            move.hitstopFrames = 6;
            move.hitstunFrames = 20;
            move.blockstunFrames = 12;
            move.hitboxSize = new Vector2(1.3f, 1f);
            move.hitboxOffset = new Vector2(0.7f, 0f);
            move.invincibilityFrames = 4;
            move.canCancelIntoDodge = true;
            move.canCancelIntoOtherMoves = false;
            move.cancelWindowStart = 10;
            move.cancelWindowEnd = 20;
            return move;
        }
    }

    [DisallowMultipleComponent]
    public sealed class ShadowSilhouetteVisual : MonoBehaviour
    {
        private Color _silhouetteColor = FighterFactory.DefaultShadowSilhouetteColor;
        private bool _disableParticleSystems;
        private bool _disableTrails;
        private Material _silhouetteMaterial;

        public void Configure(Color silhouetteColor, bool disableParticleSystems, bool disableTrails)
        {
            _silhouetteColor = silhouetteColor;
            _disableParticleSystems = disableParticleSystems;
            _disableTrails = disableTrails;
            ApplyToHierarchy();
        }

        public void ApplyToHierarchy()
        {
            Material silhouetteMaterial = GetOrCreateSilhouetteMaterial();

            foreach (var particleSystem in GetComponentsInChildren<ParticleSystem>(true))
            {
                if (!_disableParticleSystems)
                    continue;

                particleSystem.Clear(true);
                particleSystem.gameObject.SetActive(false);
            }

            foreach (var trail in GetComponentsInChildren<TrailRenderer>(true))
            {
                if (_disableTrails)
                {
                    trail.emitting = false;
                    trail.enabled = false;
                }
            }

            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                if (renderer is ParticleSystemRenderer)
                {
                    if (_disableParticleSystems)
                        renderer.enabled = false;
                    continue;
                }

                if (renderer is TrailRenderer)
                    continue;

                if (renderer is SpriteRenderer spriteRenderer && spriteRenderer.sprite != null)
                {
                    spriteRenderer.color = _silhouetteColor;
                    continue;
                }

                Material[] currentMaterials = renderer.sharedMaterials;
                int materialCount = currentMaterials != null && currentMaterials.Length > 0
                    ? currentMaterials.Length
                    : 1;
                Material[] silhouetteMaterials = new Material[materialCount];
                for (int i = 0; i < materialCount; i++)
                    silhouetteMaterials[i] = silhouetteMaterial;

                renderer.sharedMaterials = silhouetteMaterials;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private Material GetOrCreateSilhouetteMaterial()
        {
            if (_silhouetteMaterial == null)
            {
                Shader shader = Shader.Find("Unlit/Color");
                if (shader == null)
                    shader = Shader.Find("Standard");

                _silhouetteMaterial = new Material(shader)
                {
                    name = "ShadowSilhouetteRuntime",
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            if (_silhouetteMaterial.HasProperty("_Color"))
                _silhouetteMaterial.color = _silhouetteColor;
            if (_silhouetteMaterial.HasProperty("_Glossiness"))
                _silhouetteMaterial.SetFloat("_Glossiness", 0f);
            if (_silhouetteMaterial.HasProperty("_Metallic"))
                _silhouetteMaterial.SetFloat("_Metallic", 0f);
            if (_silhouetteMaterial.HasProperty("_EmissionColor"))
                _silhouetteMaterial.SetColor("_EmissionColor", Color.black);

            return _silhouetteMaterial;
        }

        private void OnDestroy()
        {
            if (_silhouetteMaterial != null)
            {
                Destroy(_silhouetteMaterial);
                _silhouetteMaterial = null;
            }
        }
    }
}
