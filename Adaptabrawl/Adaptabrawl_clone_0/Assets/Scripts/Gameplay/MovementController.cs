using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Combat;

namespace Adaptabrawl.Gameplay
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovementController : MonoBehaviour
    {
        [Header("References")]
        private Rigidbody2D rb;
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
        
        [Header("Physics")]
        [SerializeField] private float friction = 0.8f;
        [SerializeField] private float airControl = 0.5f;
        [SerializeField] private float maxFallSpeed = 20f;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
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
            if (isGrounded && !isDashing)
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
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
            float controlMultiplier = isGrounded ? 1f : airControl;
            Vector2 velocity = rb.linearVelocity;
            velocity.x = moveInput.x * moveSpeed * controlMultiplier;
            rb.linearVelocity = velocity;
        }
        
        private void ApplyDashMovement()
        {
            rb.linearVelocity = dashDirection * dashSpeed;
        }
        
        private void ApplyFriction()
        {
            if (isGrounded && Mathf.Abs(moveInput.x) < 0.1f && !isDashing)
            {
                Vector2 velocity = rb.linearVelocity;
                velocity.x *= friction;
                rb.linearVelocity = velocity;
            }
        }
        
        private void ClampFallSpeed()
        {
            if (rb.linearVelocity.y < -maxFallSpeed)
            {
                Vector2 velocity = rb.linearVelocity;
                velocity.y = -maxFallSpeed;
                rb.linearVelocity = velocity;
            }
        }
        
        // Public getters
        public bool IsGrounded => isGrounded;
        public bool IsDashing => isDashing;
        public Vector2 MoveInput => moveInput;
    }
}

