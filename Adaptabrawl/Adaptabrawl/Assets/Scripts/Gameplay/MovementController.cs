using UnityEngine;
using Adaptabrawl.Data;

namespace Adaptabrawl.Gameplay
{
    public class MovementController : MonoBehaviour
    {
        [Header("References")]
        private FighterController fighterController;
        private FighterDef fighterDef;
        
        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundLayer;
        
        [Header("Movement")]
        private float moveSpeed;
        private float jumpForce;
        private float dashSpeed;
        private float dashDuration;
        private Vector2 moveInput;
        private bool isGrounded;
        private bool isDashing;
        private float dashTimer;
        private Vector2 dashDirection;
        
        private void Awake()
        {
            fighterController = GetComponent<FighterController>();
            
            if (groundCheck == null)
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                groundCheck = groundCheckObj.transform;
            }
        }
        
        public void Initialize(FighterDef def)
        {
            fighterDef = def;
            if (fighterDef != null)
            {
                moveSpeed = fighterDef.moveSpeed;
                jumpForce = fighterDef.jumpForce;
                dashSpeed = fighterDef.dashSpeed;
                dashDuration = fighterDef.dashDuration;
            }
        }
        
        private void Update()
        {
            CheckGrounded();
            UpdateDash();
        }
        
        private void FixedUpdate()
        {
            if (isDashing)
            {
                ApplyDashMovement();
            }
            else
            {
                ApplyNormalMovement();
            }
            
            ApplyFriction();
            ClampFallSpeed();
        }
        
        private void CheckGrounded()
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        
        public void SetMoveInput(Vector2 input)
        {
            moveInput = input;
            
            // Update facing direction
            if (fighterController != null && Mathf.Abs(input.x) > 0.1f)
            {
                fighterController.SetFacing(input.x > 0f);
            }
        }
        
        public void Jump()
        {
            // Physics handled by Shinabro prefab's own Rigidbody
        }
        
        public void Dash(Vector2 direction)
        {
            if (!isDashing && dashTimer <= 0f)
            {
                isDashing = true;
                dashTimer = dashDuration;
                dashDirection = direction.normalized;
                if (dashDirection.magnitude < 0.1f)
                {
                    dashDirection = fighterController != null && fighterController.FacingRight 
                        ? Vector2.right 
                        : Vector2.left;
                }
            }
        }
        
        private void UpdateDash()
        {
            if (isDashing)
            {
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0f)
                {
                    isDashing = false;
                }
            }
        }
        
        private void ApplyNormalMovement()
        {
            // Physics handled by Shinabro prefab's own Rigidbody
        }

        private void ApplyDashMovement()
        {
            // Physics handled by Shinabro prefab's own Rigidbody
        }

        private void ApplyFriction()
        {
            // Physics handled by Shinabro prefab's own Rigidbody
        }

        private void ClampFallSpeed()
        {
            // Physics handled by Shinabro prefab's own Rigidbody
        }
        
        // Public getters
        public bool IsGrounded => isGrounded;
        public bool IsDashing => isDashing;
        public Vector2 MoveInput => moveInput;
    }
}
