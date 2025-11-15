using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Combat;

namespace Adaptabrawl.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CombatFSM))]
    [RequireComponent(typeof(StatusEffectSystem))]
    public class FighterController : MonoBehaviour
    {
        [Header("Fighter Data")]
        [SerializeField] private FighterDef fighterDef;
        
        [Header("Components")]
        private Rigidbody2D rb;
        private CombatFSM combatFSM;
        private StatusEffectSystem statusSystem;
        private MovementController movementController;
        
        [Header("Health")]
        [SerializeField] private float currentHealth;
        private float maxHealth;
        
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
            rb = GetComponent<Rigidbody2D>();
            combatFSM = GetComponent<CombatFSM>();
            statusSystem = GetComponent<StatusEffectSystem>();
            movementController = GetComponent<MovementController>();
            
            if (movementController == null)
                movementController = gameObject.AddComponent<MovementController>();
        }
        
        private void Start()
        {
            InitializeFighter();
        }
        
        private void InitializeFighter()
        {
            if (fighterDef == null)
            {
                Debug.LogError("FighterController: FighterDef is not assigned!");
                return;
            }
            
            maxHealth = fighterDef.maxHealth;
            currentHealth = maxHealth;
            
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
            
            currentHealth = Mathf.Max(0f, currentHealth - damage);
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
            OnDeath?.Invoke();
            // Disable combat and movement
            if (combatFSM != null)
                combatFSM.enabled = false;
            if (movementController != null)
                movementController.enabled = false;
        }
        
        public void ApplyKnockback(Vector2 force)
        {
            if (rb != null)
            {
                rb.AddForce(force, ForceMode2D.Impulse);
            }
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
            InitializeFighter();
        }
    }
}

