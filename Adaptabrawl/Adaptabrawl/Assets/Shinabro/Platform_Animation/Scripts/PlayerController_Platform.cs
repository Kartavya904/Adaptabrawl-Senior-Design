using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Using new input system for gamepads

public class PlayerController_Platform : MonoBehaviour
{
    Animator anim;

    [Header("Player Input Settings")]
    [Tooltip("Set to 0 for Controller 1, 1 for Controller 2, or -1 to Ignore Controllers.")]
    public int gamepadIndex = -1;

    [Header("Keyboard Controls")]
    public KeyCode keyLeft = KeyCode.A;
    public KeyCode keyRight = KeyCode.D;
    public KeyCode keyJump = KeyCode.W;
    public KeyCode keyCrouch = KeyCode.S;
    public KeyCode keyAttack = KeyCode.F; // Replacing Mouse0
    public KeyCode keyBlock = KeyCode.G;  // Replacing Mouse1
    public KeyCode keyDodge = KeyCode.Space;
    public KeyCode keySprint = KeyCode.LeftShift;
    
    // Skill Keys
    public KeyCode skill1 = KeyCode.Alpha1;
    public KeyCode skill2 = KeyCode.Alpha2;
    public KeyCode skill3 = KeyCode.Alpha3;
    public KeyCode skill4 = KeyCode.Alpha4;
    public KeyCode skill5 = KeyCode.Alpha5;
    public KeyCode skill6 = KeyCode.Alpha6;
    public KeyCode skill7 = KeyCode.Alpha7;
    public KeyCode skill8 = KeyCode.Alpha8;

    [Header("Rotation speed")]
    public float speed_rot;

    [Header("Movement speed during jump")]
    public float speed_move;

    [Header("Time available for combo")]
    public int term;

    [Header("Input Buffer Settings")]
    public float inputBufferWindow = 0.4f;
    [Tooltip("How far through the attack animation (0.0 to 1.0) before you can chain the next attack without hitting Idle. E.g., 0.85 = 85% complete.")]
    public float comboChainPoint = 0.85f;

    private float attackBufferTimer = 0f;
    private float transitionDelay = 0f;

    public bool isJump;
    
    // Status lock trackers
    private bool isAttacking;
    private bool canChainAttack;

    // --- Input Helper Methods ---
    private Gamepad GetGamepad()
    {
        if (gamepadIndex >= 0 && Gamepad.all.Count > gamepadIndex)
        {
            return Gamepad.all[gamepadIndex];
        }
        return null;
    }

    private bool IsRightPressed()
    {
        Gamepad pad = GetGamepad();
        bool padRight = pad != null && (pad.leftStick.x.ReadValue() > 0.5f || pad.dpad.right.isPressed);
        return Input.GetKey(keyRight) || padRight;
    }

    private bool IsLeftPressed()
    {
        Gamepad pad = GetGamepad();
        bool padLeft = pad != null && (pad.leftStick.x.ReadValue() < -0.5f || pad.dpad.left.isPressed);
        return Input.GetKey(keyLeft) || padLeft;
    }

    private bool IsCrouchPressed()
    {
        Gamepad pad = GetGamepad();
        bool padDown = pad != null && (pad.leftStick.y.ReadValue() < -0.5f || pad.dpad.down.isPressed);
        return Input.GetKey(keyCrouch) || padDown;
    }

    private bool IsSprintPressed()
    {
        Gamepad pad = GetGamepad();
        bool padSprint = pad != null && pad.leftTrigger.isPressed;
        return Input.GetKey(keySprint) || padSprint;
    }

    private bool WasJumpPressed()
    {
        Gamepad pad = GetGamepad();
        bool padJump = pad != null && pad.buttonSouth.wasPressedThisFrame;
        return Input.GetKeyDown(keyJump) || padJump;
    }

    private bool WasAttackPressed()
    {
        Gamepad pad = GetGamepad();
        bool padAttack = pad != null && pad.buttonWest.wasPressedThisFrame; // X/Square
        // Defaulting to Mouse0 for fallback legacy support along with keyAttack
        return Input.GetKeyDown(keyAttack) || Input.GetMouseButtonDown(0) || padAttack;
    }

    private bool IsBlockPressed()
    {
        Gamepad pad = GetGamepad();
        bool padBlock = pad != null && pad.rightTrigger.isPressed;
        return Input.GetKey(keyBlock) || Input.GetMouseButton(1) || padBlock;
    }

    private bool WasBlockPressed()
    {
        Gamepad pad = GetGamepad();
        bool padBlock = pad != null && pad.rightTrigger.wasPressedThisFrame;
        return Input.GetKeyDown(keyBlock) || Input.GetMouseButtonDown(1) || padBlock;
    }

    private bool WasBlockReleased()
    {
        Gamepad pad = GetGamepad();
        bool padBlock = pad != null && pad.rightTrigger.wasReleasedThisFrame;
        return Input.GetKeyUp(keyBlock) || Input.GetMouseButtonUp(1) || padBlock;
    }

    private bool WasDodgePressed()
    {
        Gamepad pad = GetGamepad();
        bool padDodge = pad != null && pad.buttonEast.wasPressedThisFrame; // B/Circle
        return Input.GetKeyDown(keyDodge) || padDodge;
    }

    private bool WasSkillPressed(int index)
    {
        KeyCode[] keys = { skill1, skill2, skill3, skill4, skill5, skill6, skill7, skill8 };
        if (index >= 0 && index < keys.Length && Input.GetKeyDown(keys[index])) return true;
        
        // Example pad mapping for first 4 skills: D-Pad or Shoulders
        Gamepad pad = GetGamepad();
        if (pad != null)
        {
            if (index == 0) return pad.leftShoulder.wasPressedThisFrame;
            if (index == 1) return pad.rightShoulder.wasPressedThisFrame;
        }
        return false;
    }
    // ----------------------------

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        Rotate();
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        if (transitionDelay > 0f)
        {
            transitionDelay -= Time.deltaTime;
            isAttacking = true;
            canChainAttack = false;
        }
        else
        {
            // Check if the animator is currently in an attack state. 
            AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
            isAttacking = stateInfo.IsTag("Attack") || stateInfo.IsName("Attack1") || stateInfo.IsName("Attack2") || stateInfo.IsName("Attack3") ||
                          stateInfo.IsName("Skill1") || stateInfo.IsName("Skill2") || stateInfo.IsName("Skill3") || stateInfo.IsName("Skill4") ||
                          stateInfo.IsName("Skill5") || stateInfo.IsName("Skill6") || stateInfo.IsName("Skill7") || stateInfo.IsName("Skill8");
            
            float animProgress = stateInfo.normalizedTime % 1f;
            bool isTransitioning = anim.IsInTransition(0);
            
            // Allow chaining if we're mostly done with the current attack (e.g. 85%), or not attacking at all.
            canChainAttack = !isAttacking || (isAttacking && animProgress >= comboChainPoint && !isTransitioning);
        }

        if (!isJump)
        {            
            // Register input into buffer
            if (WasAttackPressed())
            {
                attackBufferTimer = inputBufferWindow;
            }

            // Decrease buffer timer
            if (attackBufferTimer > 0f)
            {
                attackBufferTimer -= Time.deltaTime;
            }

            Attack();
            
            if (!isAttacking) 
            {
                Dodge();
                Jump();
                Block();
                Crouch();

                Skill1();
                Skill2();
                Skill3();
                Skill4();
                Skill5();
                Skill6();
                Skill7();
                Skill8();
            }
        }
    }

    Quaternion rot;
    
    void Rotate()
    {
        if (IsRightPressed())
        {            
            Move();            
            rot = Quaternion.LookRotation(Vector3.right);
        }
        else if (IsLeftPressed())
        {            
            Move();
            rot = Quaternion.LookRotation(Vector3.left);
        }
        else
        {            
            anim.SetBool("Run", false);
            anim.SetBool("Walk", false);
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, rot, speed_rot * Time.deltaTime);
    }
    
    void Move()
    {
        if (isJump)
        {            
            transform.position += transform.forward * speed_move * Time.deltaTime;            
            anim.SetBool("Run", false);
            anim.SetBool("Walk", false);
        }
        else
        {            
            anim.SetBool("Run", true);
            anim.SetBool("Walk", IsSprintPressed());
        }
    }

    int clickCount;
    float timer;
    bool isTimer;
    
    void Attack()
    {
        // Only increase combo timer if we are NOT currently attacking
        if (isTimer && !isAttacking)
        {
            timer += Time.deltaTime;
        }

        // If combo timer expires, reset combo
        if (isTimer && timer > term)
        {
            clickCount = 0;
            isTimer = false;
            timer = 0f;
        }
        
        // Execute attack if there's a buffered input and we are free to chain our attacks
        if (attackBufferTimer > 0f && canChainAttack)
        {
            // Consume buffer
            attackBufferTimer = 0f;
            transitionDelay = 0.15f; // Prevent 1-frame transition skips
            
            switch (clickCount)
            {
                case 0:
                    anim.SetTrigger("Attack1");
                    isTimer = true;
                    clickCount = 1;
                    break;
                case 1:
                    anim.SetTrigger("Attack2");
                    clickCount = 2;
                    break;
                case 2:
                    anim.SetTrigger("Attack3");
                    clickCount = 0;
                    isTimer = false;
                    break;
            }
            
            // Reset combo timer upon executing a new attack
            timer = 0f;
        }
    }
    
    void Dodge()
    {
        if (WasDodgePressed())
        {            
            anim.SetTrigger("Dodge");
        }
    }

    void Block()
    {
        if (WasBlockPressed())
        {
            anim.SetBool("Block", true);
        }
        if (WasBlockReleased() || (!IsBlockPressed() && anim.GetBool("Block")))
        {
            anim.SetBool("Block", false);
        }
    }

    void Crouch()
    {
        bool isCrouching = IsCrouchPressed();
        anim.SetBool("Crouch", isCrouching);
    }

    void Jump()
    {
        if (WasJumpPressed())
        {            
            anim.SetBool("Block", false);
            anim.SetBool("Crouch", false);
            anim.SetTrigger("Jump");
            isJump = true;
        }
    }
    
    void JumpEnd()
    {
        isJump = false;
    }

    void Skill1() { if (WasSkillPressed(0)) anim.SetTrigger("Skill1"); }
    void Skill2() { if (WasSkillPressed(1)) anim.SetTrigger("Skill2"); }
    void Skill3() { if (WasSkillPressed(2)) anim.SetTrigger("Skill3"); }
    void Skill4() { if (WasSkillPressed(3)) anim.SetTrigger("Skill4"); }
    void Skill5() { if (WasSkillPressed(4)) anim.SetTrigger("Skill5"); }
    void Skill6() { if (WasSkillPressed(5)) anim.SetTrigger("Skill6"); }
    void Skill7() { if (WasSkillPressed(6)) anim.SetTrigger("Skill7"); }
    void Skill8() { if (WasSkillPressed(7)) anim.SetTrigger("Skill8"); }
}
