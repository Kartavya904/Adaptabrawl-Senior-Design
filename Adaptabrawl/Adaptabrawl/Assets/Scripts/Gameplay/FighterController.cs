using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Combat;

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
        
        [Header("Spawn / Return")]
        private Vector3 _spawnPosition;
        [SerializeField] private float returnToSpawnSpeed = 3f;

        [Header("Facing")]
        [SerializeField] private bool facingRight = true;
        
        [Header("Events")]
        public System.Action<float, float> OnHealthChanged; // current, max
        public System.Action OnDeath;
        public System.Action<bool> OnFacingChanged;
        
        public FighterDef FighterDef => fighterDef;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool FacingRight => facingRight;
        public bool IsDead => currentHealth <= 0f;
        
        private void Awake()
        {
            combatFSM = GetComponent<CombatFSM>();
            statusSystem = GetComponent<StatusEffectSystem>();
            movementController = GetComponent<MovementController>();
            
            if (movementController == null)
                movementController = gameObject.AddComponent<MovementController>();
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
            
            // Initialize status system
            if (statusSystem != null)
            {
                statusSystem.Initialize(this);
            }
            
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
        
        public void TakeDamage(float damage)
        {
            if (IsDead) return;

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

            // Trigger the Shinabro death animation on the Stander child.
            // pcp.Die() is private, so we replicate its key steps here.
            var pcp = GetComponentInChildren<PlayerController_Platform>();
            if (pcp != null && !pcp.isDead)
            {
                pcp.isDead = true;

                var standerAnim = pcp.GetComponent<Animator>();
                if (standerAnim != null)
                {
                    int deathHash = Animator.StringToHash(pcp.deathAnimationState);
                    if (standerAnim.HasState(0, deathHash))
                        standerAnim.CrossFadeInFixedTime(pcp.deathAnimationState, 0.15f);
                    else
                    {
                        // No "Death" state found — freeze the animator in its current pose.
                        standerAnim.speed = 0f;
                        Debug.LogWarning($"[FighterController] Animator has no '{pcp.deathAnimationState}' state — freezing pose instead.");
                    }
                }

                // Disable bone colliders so dead characters don't block attacks
                foreach (var col in pcp.GetComponentsInChildren<Collider>())
                    col.enabled = false;

                // Freeze the Rigidbody so the corpse doesn't slide
                var rb3d = pcp.GetComponent<Rigidbody>();
                if (rb3d != null) rb3d.isKinematic = true;
            }

            // Disable combat and movement
            if (combatFSM != null)
                combatFSM.enabled = false;
            if (movementController != null)
                movementController.enabled = false;
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

        /// <summary>
        /// Starts the walk-back-to-spawn coroutine on this fighter.
        /// <paramref name="onComplete"/> is invoked when the fighter has arrived.
        /// </summary>
        public void StartReturnToSpawn(System.Action onComplete)
        {
            StartCoroutine(ReturnToSpawnCoroutine(onComplete));
        }

        private System.Collections.IEnumerator ReturnToSpawnCoroutine(System.Action onComplete)
        {
            // Freeze all input and systems while walking back
            LockInput();

            var pcp = GetComponentInChildren<PlayerController_Platform>();

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

                float step = Mathf.Sign(xDist) * returnToSpawnSpeed * Time.deltaTime;
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

            onComplete?.Invoke();
        }

        public void SetFacing(bool right)
        {
            if (facingRight != right)
            {
                facingRight = right;
                transform.localScale = new Vector3(right ? 1f : -1f, 1f, 1f);
                OnFacingChanged?.Invoke(right);
            }
        }
        
        public void SetFighterDef(FighterDef def)
        {
            fighterDef = def;
            _initialized = false;
            InitializeFighter();
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
            var pcp = GetComponentInChildren<PlayerController_Platform>();
            if (pcp != null)
            {
                pcp.currentHealth = pcp.maxHealth;
                pcp.isDead = false;

                // Re-enable bone colliders disabled by Die()
                foreach (var col in pcp.GetComponentsInChildren<Collider>())
                    col.enabled = true;

                var standerAnim = pcp.GetComponent<Animator>();
                if (standerAnim != null) standerAnim.speed = 1f;
            }

            Debug.Log($"[FighterController] '{(fighterDef != null ? fighterDef.fighterName : gameObject.name)}' reset for new round.");
        }
    }
}

