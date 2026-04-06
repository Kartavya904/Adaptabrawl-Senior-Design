using UnityEngine;
using Adaptabrawl.Combat;
using Adaptabrawl.Data;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.VFX
{
    public readonly struct DamageImpactSettings
    {
        public DamageImpactSettings(
            float intensity,
            float particleMultiplier,
            float splashRangeMultiplier,
            float particleSizeMultiplier)
        {
            Intensity = intensity;
            ParticleMultiplier = particleMultiplier;
            SplashRangeMultiplier = splashRangeMultiplier;
            ParticleSizeMultiplier = particleSizeMultiplier;
        }

        public float Intensity { get; }
        public float ParticleMultiplier { get; }
        public float SplashRangeMultiplier { get; }
        public float ParticleSizeMultiplier { get; }
    }

    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        public static VFXManager EnsureExists()
        {
            if (Instance != null)
                return Instance;

            VFXManager existing = FindFirstObjectByType<VFXManager>();
            if (existing != null)
                return existing;

            GameObject managerObject = new GameObject("VFXManager_Auto");
            return managerObject.AddComponent<VFXManager>();
        }

        [Header("Hit Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject blockEffectPrefab;
        [SerializeField] private GameObject parryEffectPrefab;
        [SerializeField] private bool useProceduralImpactVFX = true;
        [SerializeField] private Gradient damageSprayGradient;
        
        [Header("Status Effects")]
        [SerializeField] private GameObject poisonEffectPrefab;
        [SerializeField] private GameObject heavyAttackEffectPrefab;
        [SerializeField] private GameObject lowHPEffectPrefab;
        
        [Header("Condition Effects")]
        [SerializeField] private GameObject conditionBannerPrefab;

        private Material runtimeParticleMaterial;
        private readonly System.Collections.Generic.HashSet<DamageSystem> subscribedDamageSystems = new System.Collections.Generic.HashSet<DamageSystem>();

        private void Awake()
        {
            Instance = this;
            EnsureDamageGradient();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
        
        private void Start()
        {
            SubscribeToExistingFighters();
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
        
        private void OnBlockedFromDamage(FighterController target, MoveDef move)
        {
            if (target == null || move == null) return;
            
            // Spawn block effect at target position
            Vector3 blockPosition = target.transform.position;
            SpawnEffect(blockEffectPrefab, blockPosition);
        }
        
        private void OnDamageDealt(float damage, FighterController target)
        {
            // Could spawn damage numbers, screen shake, etc.
        }

        public void SpawnDamageImpact(Vector3 position, Vector3 hitDirection, DamageImpactSettings settings)
        {
            if (hitEffectPrefab != null)
                SpawnEffect(hitEffectPrefab, position);

            if (!useProceduralImpactVFX)
                return;

            SpawnDamageSpray(position, hitDirection, settings);
            SpawnDamageSplash(position, settings);
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

        private void SubscribeToExistingFighters()
        {
            FighterController[] fighters = FindObjectsByType<FighterController>(FindObjectsSortMode.None);
            foreach (FighterController fighter in fighters)
            {
                DamageSystem damageSystem = fighter.GetComponent<DamageSystem>();
                if (damageSystem == null || subscribedDamageSystems.Contains(damageSystem))
                    continue;

                damageSystem.OnDamageDealt += OnDamageDealt;
                damageSystem.OnBlocked += OnBlockedFromDamage;
                subscribedDamageSystems.Add(damageSystem);
            }
        }

        private void SpawnDamageSpray(Vector3 position, Vector3 hitDirection, DamageImpactSettings settings)
        {
            GameObject sprayObject = new GameObject("DamageImpactVFX");
            sprayObject.transform.position = position;
            sprayObject.SetActive(false);

            ParticleSystem particles = sprayObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystemRenderer renderer = sprayObject.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetRuntimeParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sortMode = ParticleSystemSortMode.Distance;

            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.45f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                2.6f * settings.SplashRangeMultiplier,
                (6.6f + settings.Intensity * 3.4f) * settings.SplashRangeMultiplier);
            main.startSize = new ParticleSystem.MinMaxCurve(
                0.05f * settings.ParticleSizeMultiplier,
                (0.14f + settings.Intensity * 0.06f) * settings.ParticleSizeMultiplier);
            main.startColor = damageSprayGradient.Evaluate(Random.value);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.82f;
            main.maxParticles = 96;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.08f * settings.SplashRangeMultiplier;
            shape.angle = Mathf.Lerp(24f, 34f, Mathf.Clamp01(settings.Intensity));

            Vector3 direction = hitDirection.sqrMagnitude > 0.0001f ? hitDirection.normalized : Vector3.right;
            sprayObject.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.55f, 0.04f, 0.04f), 0f),
                    new GradientColorKey(new Color(0.35f, 0.02f, 0.02f), 0.5f),
                    new GradientColorKey(new Color(0.16f, 0.01f, 0.01f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.75f, 0.4f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = particles.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-0.4f, 0.4f);
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(0.25f, 0.9f);
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-0.22f, 0.22f);

            int burstCount = Mathf.RoundToInt(Mathf.Lerp(18f, 42f, Mathf.Clamp01(settings.Intensity)) * settings.ParticleMultiplier);
            sprayObject.SetActive(true);
            particles.Emit(burstCount);
            particles.Play();

            Destroy(sprayObject, main.duration + main.startLifetime.constantMax + 0.25f);
        }

        private void SpawnDamageSplash(Vector3 position, DamageImpactSettings settings)
        {
            GameObject splashObject = new GameObject("DamageSplashVFX");
            splashObject.transform.position = position;
            splashObject.SetActive(false);

            ParticleSystem splash = splashObject.AddComponent<ParticleSystem>();
            splash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystemRenderer renderer = splash.GetComponent<ParticleSystemRenderer>();
            renderer.material = GetRuntimeParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            ParticleSystem.MainModule main = splash.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = 0.55f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.46f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                1.2f * settings.SplashRangeMultiplier,
                3.8f * settings.SplashRangeMultiplier);
            main.startSize = new ParticleSystem.MinMaxCurve(
                0.03f * settings.ParticleSizeMultiplier,
                0.08f * settings.ParticleSizeMultiplier);
            main.startColor = new Color(0.28f, 0.015f, 0.015f, 0.9f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 1.1f;
            main.maxParticles = 80;

            ParticleSystem.EmissionModule emission = splash.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = splash.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f * settings.SplashRangeMultiplier;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = splash.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.42f, 0.03f, 0.03f), 0f),
                    new GradientColorKey(new Color(0.2f, 0.015f, 0.015f), 0.65f),
                    new GradientColorKey(new Color(0.1f, 0.01f, 0.01f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.72f, 0f),
                    new GradientAlphaKey(0.45f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);

            int burstCount = Mathf.RoundToInt(Mathf.Lerp(10f, 28f, Mathf.Clamp01(settings.Intensity)) * settings.ParticleMultiplier);
            splashObject.SetActive(true);
            splash.Emit(burstCount);
            splash.Play();

            Destroy(splashObject, main.duration + main.startLifetime.constantMax + 0.3f);
        }

        private Material GetRuntimeParticleMaterial()
        {
            if (runtimeParticleMaterial != null)
                return runtimeParticleMaterial;

            Shader shader = Shader.Find("Sprites/Default");
            runtimeParticleMaterial = shader != null
                ? new Material(shader)
                : new Material(Shader.Find("Standard"));

            runtimeParticleMaterial.color = new Color(0.55f, 0.04f, 0.04f, 0.9f);
            return runtimeParticleMaterial;
        }

        private void EnsureDamageGradient()
        {
            if (damageSprayGradient != null && damageSprayGradient.colorKeys.Length > 0)
                return;

            damageSprayGradient = new Gradient();
            damageSprayGradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.72f, 0.08f, 0.08f), 0f),
                    new GradientColorKey(new Color(0.45f, 0.03f, 0.03f), 0.55f),
                    new GradientColorKey(new Color(0.2f, 0.01f, 0.01f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
        }
    }
}
