using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Combat;
using Adaptabrawl.Fighters;

namespace Adaptabrawl.Gameplay
{
    [RequireComponent(typeof(CombatFSM))]
    [RequireComponent(typeof(StatusEffectSystem))]
    public class FighterController : MonoBehaviour
    {
        [Header("Fighter Data")]
        [SerializeField] private FighterDef fighterDef;

        [Header("Components")]
        private CombatFSM combatFSM;
        private StatusEffectSystem statusSystem;
        private MovementController movementController;

        [Header("Health")]
        [SerializeField] private float currentHealth;
        [SerializeField] private float maxHealth;

        private bool _initialized;
        private int _playerNumber = 1; // 1 or 2, set by LocalGameManager
        
        [Header("Spawn / Return")]
        private Vector3 _spawnPosition;
        [SerializeField] private float returnToSpawnSpeed = 3f;

        [Header("Facing")]
        [SerializeField] private bool facingRight = true;

        [Header("Audio")]
        [SerializeField] private AudioClip swapSoundClip;
        private AudioSource _audioSource;

        private GameSceneFighterCoordinator sceneCoordinator;
        
        [Header("Events")]
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action OnDeath;
        public System.Action<bool> OnFacingChanged;
        public System.Action<FighterController, FighterDef> OnFighterDefinitionChanged;
        
        public FighterDef FighterDef => fighterDef;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool FacingRight => facingRight;
        public bool IsDead => currentHealth <= 0f;
        public int PlayerNumber => _playerNumber;
        public bool IsInputLocked
        {
            get
            {
                var pcp = GetPlayerController();
                return pcp != null && pcp.inputLocked;
            }
        }

        /// <summary>Set by LocalGameManager after spawning so SwapClassification knows which input config to use.</summary>
        public void SetPlayerNumber(int num) { _playerNumber = num; }
        
        private void Awake()
        {
            combatFSM = GetComponent<CombatFSM>();
            statusSystem = GetComponent<StatusEffectSystem>();
            movementController = GetComponent<MovementController>();
            
            if (movementController == null)
                movementController = gameObject.AddComponent<MovementController>();

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            if (swapSoundClip == null) swapSoundClip = Resources.Load<AudioClip>("SFX/swap");
        }


        
        private void Start()
        {
            // Only initialize here if SetFighterDef() hasn't already done it.
            if (!_initialized)
                InitializeFighter();
        }

        private void InitializeFighter()
        {
            if (fighterDef == null)
            {
                Debug.LogError("FighterController: FighterDef is not assigned!");
                return;
            }

            _initialized = true;
            maxHealth = fighterDef.maxHealth;
            currentHealth = maxHealth;
            Debug.Log($"[FighterController] '{fighterDef.fighterName}' initialized — maxHealth={maxHealth}, currentHealth={currentHealth}");

            // Initialize movement
            if (movementController != null)
            {
                movementController.Initialize(fighterDef);
            }

            // Push FighterDef stats to the Shinabro PlayerController_Platform
            var pcp = GetComponentInChildren<PlayerController_Platform>();
            if (pcp != null)
            {
                float baseSpeed = 5f;
                float animSpeedMult = fighterDef.moveSpeed / baseSpeed;
                float attackDmg = 10f * fighterDef.baseDamageMultiplier;
                float skillDmg = 20f * fighterDef.baseDamageMultiplier;
                pcp.ApplyFighterStats(fighterDef.moveSpeed, fighterDef.jumpForce,
                                      attackDmg, skillDmg, fighterDef.maxHealth, currentHealth, animSpeedMult);
            }
            
            // Initialize status system
            if (statusSystem != null)
            {
                statusSystem.Initialize(this);
            }
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnFighterDefinitionChanged?.Invoke(this, fighterDef);
        }
        
        /// <summary>
        /// True while the Shinabro Stander's Animator has the "Block" bool set,
        /// meaning the player is actively holding the block button.
        /// </summary>
        public bool IsBlocking
        {
            get
            {
                var pcp = GetComponentInChildren<PlayerController_Platform>();
                if (pcp == null) return false;
                var anim = pcp.GetComponent<Animator>();
                return anim != null && anim.GetBool("Block");
            }
        }

        public void TakeDamage(float damage)
        {
            if (IsDead) return;

            // Reduce incoming damage to 10% when the fighter is actively blocking.
            if (IsBlocking)
            {
                Debug.Log($"[FighterController] '{(fighterDef != null ? fighterDef.fighterName : gameObject.name)}' blocked — damage reduced from {damage} to {damage * 0.1f:F1}");
                damage *= 0.1f;
            }

            // Allow health to go negative (overkill) so a large hit on a low-health
            // fighter always triggers death — the Slider's minValue clamps the display.
            currentHealth -= damage;
            Debug.Log($"[FighterController] '{(fighterDef != null ? fighterDef.fighterName : gameObject.name)}' took {damage} damage — health now {currentHealth}/{maxHealth}");
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0f)
            {
                Die();
            }
        }
        
        public void Heal(float amount)
        {
            if (IsDead) return;
            
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        private void Die()
        {
            // Force health to exactly 0 and push a final event so the slider clears immediately
            currentHealth = 0f;
            OnHealthChanged?.Invoke(0f, maxHealth);

            OnDeath?.Invoke();

            // Disable combat and movement
            if (combatFSM != null)
                combatFSM.enabled = false;
            if (movementController != null)
                movementController.enabled = false;

            // Trigger character-specific death animation if available
            var pcp = GetComponentInChildren<PlayerController_Platform>();
            if (pcp != null)
            {
                pcp.isDead = true;
                var anim = pcp.GetComponent<Animator>();
                if (anim != null) anim.CrossFadeInFixedTime("Death", 0.15f);
            }
        }
        
        /// <summary>
        /// Re-fires the current health values to all OnHealthChanged listeners.
        /// Called by GameManager after the HUD has had time to subscribe.
        /// </summary>
        public void BroadcastHealth() => OnHealthChanged?.Invoke(currentHealth, maxHealth);

        /// <summary>
        /// Disables player input, combat, and movement. Called at the start of every round
        /// during the pre-round buffer, and by ReturnToSpawnCoroutine during walk-back.
        /// </summary>
        /// <summary>
        /// Freezes player input for game-state reasons (buffer, walk-back).
        /// Does NOT mark the fighter as dead.
        /// </summary>
        public void LockInput()
        {
            if (combatFSM != null)          combatFSM.enabled = false;
            if (movementController != null) movementController.enabled = false;
            var pcp = GetComponentInChildren<PlayerController_Platform>();
            if (pcp != null) pcp.inputLocked = true;
        }

        /// <summary>
        /// Releases the game-state input lock and restores physics/root-motion.
        /// Called when the pre-round buffer expires.
        /// </summary>
        public void UnlockInput()
        {
            if (combatFSM != null)          combatFSM.enabled = true;
            if (movementController != null) movementController.enabled = true;

            var pcp = GetComponentInChildren<PlayerController_Platform>();
            if (pcp != null)
            {
                pcp.inputLocked = false;

                // Unfreeze physics now that the round is live
                var rb3d = pcp.GetComponent<Rigidbody>();
                if (rb3d != null) rb3d.isKinematic = false;

                // Restore root motion so run/walk animations move the character
                var standerAnim = pcp.GetComponent<Animator>();
                if (standerAnim != null) standerAnim.applyRootMotion = true;
            }
        }

        /// <summary>
        /// Store the world position the fighter should return to at the start of each round.
        /// Call this once after spawning the fighter.
        /// </summary>
        public void SetSpawnPosition(Vector3 pos) => _spawnPosition = pos;

        public void RegisterSceneCoordinator(GameSceneFighterCoordinator coordinator)
        {
            sceneCoordinator = coordinator;
        }

        public GameSceneFighterCoordinator GetSceneCoordinator() => sceneCoordinator;

        public PlayerController_Platform GetPlayerController() => GetComponentInChildren<PlayerController_Platform>();

        public Transform GetArenaTransform()
        {
            var pcp = GetPlayerController();
            return pcp != null ? pcp.transform : transform;
        }

        public Vector3 GetArenaPosition() => GetArenaTransform().position;

        public void SetArenaPosition(Vector3 position)
        {
            Transform arenaTransform = GetArenaTransform();
            if (arenaTransform == null)
                return;

            arenaTransform.position = new Vector3(position.x, position.y, 0f);
        }

        /// <summary>
        /// Starts the walk-back-to-spawn coroutine on this fighter.
        /// <paramref name="onComplete"/> is invoked when the fighter has arrived.
        /// </summary>
        public void StartReturnToSpawn(System.Action onComplete)
        {
            StartCoroutine(ReturnToSpawnCoroutine(returnToSpawnSpeed, onComplete));
        }

        public void StartReturnToSpawn(float moveDurationSeconds, System.Action onComplete)
        {
            float distance = Mathf.Abs(_spawnPosition.x - GetArenaPosition().x);
            float effectiveDuration = Mathf.Max(0.01f, moveDurationSeconds);
            float customSpeed = distance <= 0.01f ? returnToSpawnSpeed : distance / effectiveDuration;
            StartCoroutine(ReturnToSpawnCoroutine(customSpeed, onComplete));
        }

        private System.Collections.IEnumerator ReturnToSpawnCoroutine(float moveSpeedOverride, System.Action onComplete)
        {
            // Freeze all input and systems while walking back
            LockInput();
            ClearGameplayState(true);

            var pcp = GetPlayerController();

            if (pcp == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            var standerAnim = pcp.GetComponent<Animator>();
            var rb3d        = pcp.GetComponent<Rigidbody>();

            // Freeze physics so we can drive position directly
            if (rb3d != null) rb3d.isKinematic = true;

            if (standerAnim != null)
            {
                standerAnim.speed = 1f;
                // Disable root motion — we control the position ourselves
                standerAnim.applyRootMotion = false;
            }

            float targetX = _spawnPosition.x;

            // Face toward the spawn point before starting to move
            bool goRight = targetX > pcp.transform.position.x;
            if (Mathf.Abs(targetX - pcp.transform.position.x) > 0.05f)
            {
                pcp.transform.rotation = Quaternion.LookRotation(goRight ? Vector3.right : Vector3.left);
                facingRight = goRight;
            }

            // Play run animation
            if (standerAnim != null)
            {
                standerAnim.SetBool("Run",  true);
                standerAnim.SetBool("Walk", false);
            }

            const float arrivalThreshold = 0.1f;

            // Move along X only — Y stays fixed so the character stays on the ground
            while (true)
            {
                float xDist = targetX - pcp.transform.position.x;

                if (Mathf.Abs(xDist) <= arrivalThreshold) break;

                float step = Mathf.Sign(xDist) * moveSpeedOverride * Time.deltaTime;
                // Clamp so we don't overshoot
                if (Mathf.Abs(step) > Mathf.Abs(xDist)) step = xDist;

                pcp.transform.position = new Vector3(
                    pcp.transform.position.x + step,
                    pcp.transform.position.y,   // Y unchanged
                    0f);

                yield return null;
            }

            // Snap X exactly to spawn; Y stays at whatever ground height the character is at
            pcp.transform.position = new Vector3(targetX, pcp.transform.position.y, 0f);

            if (standerAnim != null)
            {
                standerAnim.SetBool("Run",  false);
                standerAnim.SetBool("Walk", false);
            }

            // Force character to face the center of the arena for the next round
            bool faceRight = targetX < 0f;
            pcp.transform.rotation = Quaternion.LookRotation(faceRight ? Vector3.right : Vector3.left);
            facingRight = faceRight;

            onComplete?.Invoke();
        }

        public void SetFacing(bool right)
        {
            bool changed = facingRight != right;
            facingRight = right;

            var pcp = GetPlayerController();
            if (pcp != null)
            {
                pcp.transform.rotation = Quaternion.LookRotation(right ? Vector3.right : Vector3.left);
            }
            else
            {
                // Fallback for missing PCP
                transform.localScale = new Vector3(right ? 1f : -1f, 1f, 1f);
            }

            if (changed)
                OnFacingChanged?.Invoke(right);
        }

        public void ClearGameplayState(bool snapToIdle = true)
        {
            combatFSM?.ForceResetState();
            movementController?.SetMoveInput(Vector2.zero);

            var pcp = GetPlayerController();
            if (pcp != null)
                pcp.ResetGameplayState(snapToIdle);
        }
        
        public void SetFighterDef(FighterDef def)
        {
            fighterDef = def;
            _initialized = false;
            InitializeFighter();
        }

        /// <summary>
        /// Swaps classification mid-match: destroys old Shinabro child prefab,
        /// instantiates new one from newDef.fighterPrefab, re-wires input/combat,
        /// and scales health proportionally.
        /// </summary>
        public void SwapClassification(FighterDef newDef)
        {
            if (newDef == null || newDef.fighterPrefab == null) return;

            // 1. Capture state from old prefab
            float healthPercent = maxHealth > 0 ? currentHealth / maxHealth : 1f;
            var oldPcp = GetPlayerController();
            Vector3 oldPos = oldPcp != null ? oldPcp.transform.position : transform.position;
            Quaternion oldRot = oldPcp != null ? oldPcp.transform.rotation : transform.rotation;
            int oldGamepadIndex = oldPcp != null ? oldPcp.gamepadIndex : -1;

            // 2. Destroy old Shinabro child IMMEDIATELY to prevent two PCPs
            //    reading input on the same frame (Destroy is deferred!)
            if (oldPcp != null)
                DestroyImmediate(oldPcp.gameObject);

            // 3. Instantiate new prefab as child of this FighterController root
            GameObject newChild = Instantiate(newDef.fighterPrefab, oldPos, oldRot, transform);

            // 4. Apply layer to match the rest of the fighter
            newChild.transform.localScale = Vector3.one;
            newChild.layer = gameObject.layer;
            foreach (Transform t in newChild.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = gameObject.layer;

            // 5. Update FighterDef and health (preserve health percentage)
            fighterDef = newDef;
            maxHealth = newDef.maxHealth;
            currentHealth = maxHealth * healthPercent;

            // 6. Re-wire input on new PCP using ConfigureForPlayer
            //    This properly sets ALL keys (WASD for P1, Arrows for P2, skill keys, etc.)
            var newPcp = newChild.GetComponentInChildren<PlayerController_Platform>();
            if (newPcp != null)
            {
                newPcp.ConfigureForPlayer(_playerNumber, oldGamepadIndex);

                // Push FighterDef stats to new PCP
                float baseSpeed = 5f;
                float animSpeedMult = newDef.moveSpeed / baseSpeed;
                float attackDmg = 10f * newDef.baseDamageMultiplier;
                float skillDmg = 20f * newDef.baseDamageMultiplier;
                newPcp.ApplyFighterStats(newDef.moveSpeed, newDef.jumpForce,
                                         attackDmg, skillDmg, maxHealth, currentHealth, animSpeedMult);

                // Re-apply CameraBoundsConstraint to the active Stander so it cannot exit the screen
                if (newPcp.GetComponent<CameraBoundsConstraint>() == null)
                    newPcp.gameObject.AddComponent<CameraBoundsConstraint>();
            }

            // 7. Re-run combat setup (hitboxes, hurtboxes on new child)
            var combatSetup = GetComponent<StanderCombatSetup>();
            if (combatSetup != null) combatSetup.RunSetup();
            var shadowVisual = GetComponent<ShadowSilhouetteVisual>();
            if (shadowVisual != null) shadowVisual.ApplyToHierarchy();

            // 8. Notify systems
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnFighterDefinitionChanged?.Invoke(this, fighterDef);
            Debug.Log($"[FighterController] P{_playerNumber} SWAPPED to '{newDef.fighterName}' (full visual swap)");

            // Phase 1: Swap Invincibility & VFX
            StartCoroutine(SwapJuiceRoutine(newChild));
        }

        private System.Collections.IEnumerator SwapJuiceRoutine(GameObject newPcpObject)
        {
            var hurtbox = newPcpObject.GetComponentInChildren<FighterHurtbox>();
            var renderers = newPcpObject.GetComponentsInChildren<Renderer>(true);

            // Create a cool "teleport" ring visual using Unity primitives
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(ring.GetComponent<Collider>());
            ring.transform.position = newPcpObject.transform.position + Vector3.up * 1f;
            ring.transform.localScale = new Vector3(2f, 4f, 2f);
            var ringMaterial = ring.GetComponent<Renderer>().material;
            ringMaterial.color = new Color(0.2f, 0.8f, 1f, 0.5f); // Bright blue sci-fi burst
            // We use rendering modes strictly via standard shader if possible, but basic color is fine for now

            // Grant 1.5 seconds of Invincibility and blink
            if (hurtbox != null) hurtbox.enabled = false;

            float duration = 1.5f;
            float blinkTimer = 0f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                blinkTimer += Time.deltaTime;

                // Shrink the ring really fast
                if (ring != null)
                {
                    ring.transform.localScale = Vector3.Lerp(new Vector3(2f, 4f, 2f), new Vector3(0f, 4f, 0f), elapsed / 0.3f);
                    if (elapsed > 0.3f) Destroy(ring);
                }

                // Blink character meshes rapidly during I-Frames
                if (blinkTimer > 0.1f)
                {
                    blinkTimer = 0f;
                    foreach (var r in renderers)
                        if (r != null) r.enabled = !r.enabled;
                }

                yield return null;
            }

            // Clean up VFX and restore hurtbox/materials
            if (ring != null) Destroy(ring);
            foreach (var r in renderers) 
                if (r != null) r.enabled = true;

            if (hurtbox != null) hurtbox.enabled = true;
        }

        /// <summary>
        /// Called by GameManager between rounds. Restores full health, re-enables
        /// CombatFSM and MovementController (disabled by Die()), and resets the
        /// Shinabro PlayerController_Platform so it can fight again.
        /// </summary>
        public void ResetForNewRound()
        {
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            // Reset Shinabro's health on the Stander child and re-enable its colliders.
            // NOTE: pcp.isDead, Rigidbody.isKinematic, and applyRootMotion are intentionally
            // left frozen here. LockInput() keeps them frozen through the pre-round buffer;
            // UnlockInput() releases them only when the round actually goes live.
            var pcp = GetPlayerController();
            if (pcp != null)
            {
                pcp.currentHealth = pcp.maxHealth;
                pcp.isDead = false;

                // Re-enable bone colliders disabled by Die()
                foreach (var col in pcp.GetComponentsInChildren<Collider>())
                    col.enabled = true;

                var standerAnim = pcp.GetComponent<Animator>();
                if (standerAnim != null) standerAnim.speed = 1f;

                var hurtbox = pcp.GetComponentInChildren<FighterHurtbox>(true);
                if (hurtbox != null)
                    hurtbox.SetPartsEnabled(hurtbox.enabled);

                foreach (var hitVolume in pcp.GetComponentsInChildren<WeaponHitVolume>(true))
                    hitVolume.SyncRuntimeState();
            }

            Debug.Log($"[FighterController] '{(fighterDef != null ? fighterDef.fighterName : gameObject.name)}' reset for new round.");
        }
    }
}
