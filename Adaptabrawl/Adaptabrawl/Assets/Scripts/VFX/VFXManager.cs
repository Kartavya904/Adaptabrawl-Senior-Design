using UnityEngine;
using Adaptabrawl.Combat;
using Adaptabrawl.Data;
using Adaptabrawl.Gameplay;
using Adaptabrawl.UI;
using UnityEngine.UI;

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
        private static readonly Color BloodMaterialColor = new Color(0.86f, 0.05f, 0.05f, 0.95f);
        private static readonly Color BloodSprayBrightColor = new Color(0.96f, 0.08f, 0.08f, 1f);
        private static readonly Color BloodSprayMidColor = new Color(0.78f, 0.02f, 0.02f, 1f);
        private static readonly Color BloodSprayDarkColor = new Color(0.52f, 0.01f, 0.01f, 1f);
        private static readonly Color BloodSplashBrightColor = new Color(0.84f, 0.05f, 0.05f, 1f);
        private static readonly Color BloodSplashMidColor = new Color(0.64f, 0.02f, 0.02f, 1f);
        private static readonly Color BloodSplashDarkColor = new Color(0.4f, 0.01f, 0.01f, 1f);

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
        [SerializeField] private bool useScreenSpaceBloodBypass = true;
        [SerializeField] private Gradient damageSprayGradient;
        
        [Header("Status Effects")]
        [SerializeField] private GameObject poisonEffectPrefab;
        [SerializeField] private GameObject heavyAttackEffectPrefab;
        [SerializeField] private GameObject lowHPEffectPrefab;
        
        [Header("Condition Effects")]
        [SerializeField] private GameObject conditionBannerPrefab;

        private Material runtimeParticleMaterial;
        private Canvas runtimeDamageOverlayCanvas;
        private RectTransform runtimeDamageOverlayRoot;
        private Texture2D runtimeDamageOverlayTexture;
        private Sprite runtimeDamageOverlaySprite;
        private readonly System.Collections.Generic.HashSet<DamageSystem> subscribedDamageSystems = new System.Collections.Generic.HashSet<DamageSystem>();

        private void Awake()
        {
            Instance = this;
            EnsureDamageGradient();

            if (useScreenSpaceBloodBypass)
            {
                EnsureDamageOverlayRoot();
                GetRuntimeDamageOverlaySprite();
            }
        }

        private void OnDestroy()
        {
            if (runtimeParticleMaterial != null)
                Destroy(runtimeParticleMaterial);

            if (runtimeDamageOverlaySprite != null)
                Destroy(runtimeDamageOverlaySprite);

            if (runtimeDamageOverlayTexture != null)
                Destroy(runtimeDamageOverlayTexture);

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

            if (useScreenSpaceBloodBypass && SpawnOverlayDamageImpact(position, hitDirection, settings))
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
                    new GradientColorKey(BloodSprayBrightColor, 0f),
                    new GradientColorKey(BloodSprayMidColor, 0.5f),
                    new GradientColorKey(BloodSprayDarkColor, 1f)
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
            main.startColor = BloodSplashBrightColor;
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
                    new GradientColorKey(BloodSplashBrightColor, 0f),
                    new GradientColorKey(BloodSplashMidColor, 0.65f),
                    new GradientColorKey(BloodSplashDarkColor, 1f)
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

        private bool SpawnOverlayDamageImpact(Vector3 position, Vector3 hitDirection, DamageImpactSettings settings)
        {
            RectTransform overlayRoot = EnsureDamageOverlayRoot();
            UnityEngine.Camera worldCamera = UnityEngine.Camera.main;
            if (overlayRoot == null || worldCamera == null)
                return false;

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(position);
            if (screenPoint.z <= 0f)
                return false;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRoot, screenPoint, null, out Vector2 localPoint))
                return false;

            GameObject impactObject = new GameObject("DamageBloodOverlay");
            impactObject.transform.SetParent(overlayRoot, false);

            RectTransform impactRect = impactObject.AddComponent<RectTransform>();
            impactRect.anchorMin = new Vector2(0.5f, 0.5f);
            impactRect.anchorMax = new Vector2(0.5f, 0.5f);
            impactRect.pivot = new Vector2(0.5f, 0.5f);
            impactRect.anchoredPosition = localPoint;
            impactRect.sizeDelta = Vector2.zero;
            impactRect.SetAsLastSibling();

            Vector2 sprayDirection = ResolveOverlaySprayDirection(worldCamera, position, hitDirection);
            float intensity = Mathf.Clamp01(settings.Intensity);
            float sizeScale = Mathf.Lerp(0.9f, 1.35f, intensity) * Mathf.Max(0.85f, settings.ParticleSizeMultiplier);
            float travelScale = Mathf.Lerp(0.85f, 1.3f, intensity) * Mathf.Max(0.85f, settings.SplashRangeMultiplier);

            var droplets = new System.Collections.Generic.List<OverlayBloodDroplet>();
            int dropletCount = Mathf.Clamp(
                Mathf.RoundToInt(Mathf.Lerp(10f, 24f, intensity) * settings.ParticleMultiplier),
                10,
                28);

            CreateOverlayDroplet(
                impactRect,
                droplets,
                BloodSplashBrightColor,
                sprayDirection * Random.Range(4f, 12f),
                new Vector2(Random.Range(28f, 44f), Random.Range(20f, 34f)) * sizeScale,
                sprayDirection * Random.Range(22f, 54f) * travelScale,
                Random.Range(-80f, 80f),
                Random.Range(120f, 210f),
                Random.Range(0.22f, 0.32f),
                Random.Range(0.1f, 0.22f));

            for (int i = 0; i < dropletCount; i++)
            {
                Vector2 spread = Random.insideUnitCircle * Mathf.Lerp(0.9f, 1.7f, intensity);
                Vector2 dropletDirection = (sprayDirection * Random.Range(0.75f, 1.35f)
                    + spread
                    + Vector2.up * Random.Range(0.18f, 0.65f)).normalized;

                if (dropletDirection.sqrMagnitude < 0.0001f)
                    dropletDirection = sprayDirection;

                float dropletSize = Random.Range(8f, 18f) * sizeScale;
                Color dropletColor = Color.Lerp(BloodSprayMidColor, BloodSprayBrightColor, Random.value);
                dropletColor.a = Random.Range(0.86f, 0.98f);

                CreateOverlayDroplet(
                    impactRect,
                    droplets,
                    dropletColor,
                    Random.insideUnitCircle * Random.Range(2f, 10f),
                    new Vector2(
                        dropletSize * Random.Range(0.7f, 1.35f),
                        dropletSize * Random.Range(0.7f, 1.35f)),
                    dropletDirection * Random.Range(135f, 335f) * travelScale,
                    Random.Range(-260f, 260f),
                    Random.Range(360f, 760f),
                    Random.Range(0.18f, 0.42f),
                    Random.Range(0.06f, 0.24f));
            }

            Canvas.ForceUpdateCanvases();
            StartCoroutine(AnimateOverlayDroplets(impactObject, droplets));
            return true;
        }

        private RectTransform EnsureDamageOverlayRoot()
        {
            if (runtimeDamageOverlayRoot != null)
                return runtimeDamageOverlayRoot;

            Canvas referenceCanvas = ResolveReferenceOverlayCanvas();

            GameObject canvasObject = new GameObject("DamageOverlayCanvas");
            canvasObject.transform.SetParent(transform, false);

            runtimeDamageOverlayCanvas = canvasObject.AddComponent<Canvas>();
            runtimeDamageOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            runtimeDamageOverlayCanvas.pixelPerfect = false;
            runtimeDamageOverlayCanvas.targetDisplay = referenceCanvas != null ? referenceCanvas.targetDisplay : 0;
            runtimeDamageOverlayCanvas.sortingOrder = referenceCanvas != null ? referenceCanvas.sortingOrder - 1 : 0;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            GameObject rootObject = new GameObject("DamageOverlayRoot");
            rootObject.transform.SetParent(canvasObject.transform, false);
            runtimeDamageOverlayRoot = rootObject.AddComponent<RectTransform>();
            runtimeDamageOverlayRoot.anchorMin = Vector2.zero;
            runtimeDamageOverlayRoot.anchorMax = Vector2.one;
            runtimeDamageOverlayRoot.offsetMin = Vector2.zero;
            runtimeDamageOverlayRoot.offsetMax = Vector2.zero;
            runtimeDamageOverlayRoot.pivot = new Vector2(0.5f, 0.5f);

            return runtimeDamageOverlayRoot;
        }

        private Canvas ResolveReferenceOverlayCanvas()
        {
            GameHUD gameHud = FindFirstObjectByType<GameHUD>(FindObjectsInactive.Include);
            if (gameHud != null)
            {
                Canvas hudCanvas = gameHud.GetComponentInParent<Canvas>();
                if (hudCanvas != null && hudCanvas.renderMode != RenderMode.WorldSpace)
                    return hudCanvas;
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Canvas bestCanvas = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                Canvas canvas = canvases[i];
                if (canvas == null || !canvas.isActiveAndEnabled || canvas.renderMode == RenderMode.WorldSpace)
                    continue;

                if (bestCanvas == null || canvas.sortingOrder > bestCanvas.sortingOrder)
                    bestCanvas = canvas;
            }

            return bestCanvas;
        }

        private void CreateOverlayDroplet(
            RectTransform parent,
            System.Collections.Generic.List<OverlayBloodDroplet> droplets,
            Color color,
            Vector2 initialOffset,
            Vector2 size,
            Vector2 velocity,
            float angularVelocity,
            float gravity,
            float lifetime,
            float growth)
        {
            GameObject dropletObject = new GameObject("BloodDroplet");
            dropletObject.transform.SetParent(parent, false);

            RectTransform dropletRect = dropletObject.AddComponent<RectTransform>();
            dropletRect.anchorMin = new Vector2(0.5f, 0.5f);
            dropletRect.anchorMax = new Vector2(0.5f, 0.5f);
            dropletRect.pivot = new Vector2(0.5f, 0.5f);
            dropletRect.anchoredPosition = initialOffset;
            dropletRect.sizeDelta = size;

            Image dropletImage = dropletObject.AddComponent<Image>();
            dropletImage.sprite = GetRuntimeDamageOverlaySprite();
            dropletImage.color = color;
            dropletImage.raycastTarget = false;

            droplets.Add(new OverlayBloodDroplet(
                dropletRect,
                dropletImage,
                size,
                velocity,
                angularVelocity,
                gravity,
                lifetime,
                growth,
                color,
                Random.Range(0f, 360f)));
        }

        private Vector2 ResolveOverlaySprayDirection(UnityEngine.Camera worldCamera, Vector3 position, Vector3 hitDirection)
        {
            Vector3 normalizedHitDirection = hitDirection.sqrMagnitude > 0.0001f
                ? hitDirection.normalized
                : Vector3.right;

            Vector3 origin = worldCamera.WorldToScreenPoint(position);
            Vector3 screenTarget = worldCamera.WorldToScreenPoint(position + normalizedHitDirection);
            Vector2 screenDirection = new Vector2(screenTarget.x - origin.x, screenTarget.y - origin.y);

            if (screenDirection.sqrMagnitude > 0.0001f)
                return screenDirection.normalized;

            return normalizedHitDirection.x >= 0f
                ? new Vector2(1f, 0.25f).normalized
                : new Vector2(-1f, 0.25f).normalized;
        }

        private System.Collections.IEnumerator AnimateOverlayDroplets(
            GameObject impactObject,
            System.Collections.Generic.List<OverlayBloodDroplet> droplets)
        {
            float longestLifetime = 0f;
            for (int i = 0; i < droplets.Count; i++)
                longestLifetime = Mathf.Max(longestLifetime, droplets[i].Lifetime);

            float elapsed = 0f;
            while (impactObject != null && elapsed < longestLifetime)
            {
                float deltaTime = Time.deltaTime;
                elapsed += deltaTime;

                for (int i = 0; i < droplets.Count; i++)
                {
                    OverlayBloodDroplet droplet = droplets[i];
                    if (droplet.Rect == null || droplet.Image == null)
                        continue;

                    droplet.Elapsed += deltaTime;
                    float t = Mathf.Clamp01(droplet.Elapsed / droplet.Lifetime);
                    droplet.Velocity += Vector2.down * droplet.Gravity * deltaTime;
                    droplet.Rotation += droplet.AngularVelocity * deltaTime;

                    droplet.Rect.anchoredPosition += droplet.Velocity * deltaTime;
                    droplet.Rect.localRotation = Quaternion.Euler(0f, 0f, droplet.Rotation);
                    droplet.Rect.sizeDelta = droplet.StartSize * (1f + droplet.Growth * t);

                    Color color = Color.Lerp(droplet.StartColor, BloodSplashDarkColor, t * 0.85f);
                    color.a = Mathf.Lerp(droplet.StartColor.a, 0f, t * t);
                    droplet.Image.color = color;
                }

                yield return null;
            }

            if (impactObject != null)
                Destroy(impactObject);
        }

        private Sprite GetRuntimeDamageOverlaySprite()
        {
            if (runtimeDamageOverlaySprite != null)
                return runtimeDamageOverlaySprite;

            const int textureSize = 32;
            runtimeDamageOverlayTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            runtimeDamageOverlayTexture.name = "DamageOverlayBloodTexture";
            runtimeDamageOverlayTexture.wrapMode = TextureWrapMode.Clamp;
            runtimeDamageOverlayTexture.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[textureSize * textureSize];
            Vector2 center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
            float radius = textureSize * 0.5f - 1f;

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = Mathf.Clamp01(1f - distance);
                    alpha *= alpha;
                    pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            runtimeDamageOverlayTexture.SetPixels(pixels);
            runtimeDamageOverlayTexture.Apply();
            runtimeDamageOverlaySprite = Sprite.Create(
                runtimeDamageOverlayTexture,
                new Rect(0f, 0f, textureSize, textureSize),
                new Vector2(0.5f, 0.5f),
                textureSize);

            return runtimeDamageOverlaySprite;
        }

        private Material GetRuntimeParticleMaterial()
        {
            if (runtimeParticleMaterial != null)
                return runtimeParticleMaterial;

            Shader shader = Shader.Find("Sprites/Default");
            runtimeParticleMaterial = shader != null
                ? new Material(shader)
                : new Material(Shader.Find("Standard"));

            runtimeParticleMaterial.color = BloodMaterialColor;
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
                    new GradientColorKey(BloodSprayBrightColor, 0f),
                    new GradientColorKey(BloodSprayMidColor, 0.55f),
                    new GradientColorKey(BloodSprayDarkColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
        }

        private sealed class OverlayBloodDroplet
        {
            public OverlayBloodDroplet(
                RectTransform rect,
                Image image,
                Vector2 startSize,
                Vector2 velocity,
                float angularVelocity,
                float gravity,
                float lifetime,
                float growth,
                Color startColor,
                float rotation)
            {
                Rect = rect;
                Image = image;
                StartSize = startSize;
                Velocity = velocity;
                AngularVelocity = angularVelocity;
                Gravity = gravity;
                Lifetime = lifetime;
                Growth = growth;
                StartColor = startColor;
                Rotation = rotation;
            }

            public RectTransform Rect { get; }
            public Image Image { get; }
            public Vector2 StartSize { get; }
            public Vector2 Velocity { get; set; }
            public float AngularVelocity { get; }
            public float Gravity { get; }
            public float Lifetime { get; }
            public float Growth { get; }
            public Color StartColor { get; }
            public float Rotation { get; set; }
            public float Elapsed { get; set; }
        }
    }
}
