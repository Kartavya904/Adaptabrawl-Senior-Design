using UnityEngine;
using UnityEngine.InputSystem;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Attack;
using Adaptabrawl.Defend;
using Adaptabrawl.Evade;

namespace Adaptabrawl.Input
{
    [RequireComponent(typeof(FighterController))]
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Input Actions")]
        [SerializeField] private InputActionAsset inputActions;
        private InputActionMap playerMap;
        
        [Header("Input Actions")]
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction attackAction;
        private InputAction heavyAttackAction;
        private InputAction blockAction;
        private InputAction parryAction;
        private InputAction dodgeAction;
        
        [Header("References")]
        private FighterController fighterController;
        private MovementController movementController;
        private AttackSystem attackSystem;
        private DefenseSystem defenseSystem;
        private EvadeSystem evadeSystem;
        
        private void Awake()
        {
            fighterController = GetComponent<FighterController>();
            movementController = GetComponent<MovementController>();
            attackSystem = GetComponent<AttackSystem>();
            defenseSystem = GetComponent<DefenseSystem>();
            evadeSystem = GetComponent<EvadeSystem>();
            
            // Load input actions if not assigned
            if (inputActions == null)
            {
                inputActions = Resources.Load<InputActionAsset>("InputSystem_Actions");
            }
            
            if (inputActions != null)
            {
                playerMap = inputActions.FindActionMap("Player");
                
                if (playerMap != null)
                {
                    moveAction = playerMap.FindAction("Move");
                    jumpAction = playerMap.FindAction("Jump");
                    attackAction = playerMap.FindAction("Attack");
                    // Note: Input actions may need to be extended for heavy attack, block, parry, dodge
                }
            }
        }
        
        private void OnEnable()
        {
            if (playerMap != null)
            {
                playerMap.Enable();
                
                // Subscribe to actions
                if (moveAction != null)
                    moveAction.performed += OnMove;
                if (jumpAction != null)
                {
                    jumpAction.performed += OnJump;
                    jumpAction.canceled += OnJumpCancel;
                }
                if (attackAction != null)
                {
                    attackAction.performed += OnAttack;
                    attackAction.canceled += OnAttackCancel;
                }
            }
        }
        
        private void OnDisable()
        {
            if (playerMap != null)
            {
                // Unsubscribe from actions
                if (moveAction != null)
                    moveAction.performed -= OnMove;
                if (jumpAction != null)
                {
                    jumpAction.performed -= OnJump;
                    jumpAction.canceled -= OnJumpCancel;
                }
                if (attackAction != null)
                {
                    attackAction.performed -= OnAttack;
                    attackAction.canceled -= OnAttackCancel;
                }
                
                playerMap.Disable();
            }
        }
        
        private void Update()
        {
            // Update movement input continuously
            if (moveAction != null && movementController != null)
            {
                Vector2 moveInput = moveAction.ReadValue<Vector2>();
                movementController.SetMoveInput(moveInput);
            }
        }
        
        private void OnMove(InputAction.CallbackContext context)
        {
            if (movementController != null)
            {
                Vector2 moveInput = context.ReadValue<Vector2>();
                movementController.SetMoveInput(moveInput);
            }
        }
        
        private void OnJump(InputAction.CallbackContext context)
        {
            if (movementController != null)
            {
                movementController.Jump();
            }
        }
        
        private void OnJumpCancel(InputAction.CallbackContext context)
        {
            // Handle jump cancel if needed
        }
        
        private void OnAttack(InputAction.CallbackContext context)
        {
            if (attackSystem != null)
            {
                attackSystem.OnLightAttackInput(true);
            }
        }
        
        private void OnAttackCancel(InputAction.CallbackContext context)
        {
            if (attackSystem != null)
            {
                attackSystem.OnLightAttackInput(false);
            }
        }
        
        // Additional input methods for heavy attack, block, parry, dodge
        // These would need corresponding Input Actions to be added to the Input Action Asset
        
        public void OnHeavyAttackInput(bool pressed)
        {
            if (attackSystem != null)
            {
                attackSystem.OnHeavyAttackInput(pressed);
            }
        }
        
        public void OnBlockInput(bool held)
        {
            if (defenseSystem != null)
            {
                defenseSystem.OnBlockInput(held);
            }
        }
        
        public void OnParryInput(bool pressed)
        {
            if (defenseSystem != null)
            {
                defenseSystem.OnParryInput(pressed);
            }
        }
        
        public void OnDodgeInput(bool pressed)
        {
            if (evadeSystem != null)
            {
                evadeSystem.OnDodgeInput(pressed);
            }
        }
    }
}

