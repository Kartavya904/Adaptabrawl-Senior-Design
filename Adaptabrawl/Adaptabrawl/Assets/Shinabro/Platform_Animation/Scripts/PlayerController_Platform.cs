using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Using new input system for gamepads
using Adaptabrawl.Attack;
using Adaptabrawl.Defend;
using Adaptabrawl.Evade;
using Adaptabrawl.Gameplay;

public class PlayerController_Platform : MonoBehaviour
{
    Animator anim;
    private FighterController fighterController;
    private MovementController movementController;

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

    [Header("Gamepad — movement")]
    [Tooltip("Left stick X must exceed this (±) to count as left/right. D-pad always works.")]
    [Range(0.05f, 0.95f)]
    public float moveStickDeadZone = 0.22f;

    [Header("Rotation speed")]
    public float speed_rot;

    [Header("Movement speed during jump")]
    public float speed_move;

    [Header("Ground movement speed")]
    public float ground_move_speed = 5f;

    [Header("Ground locomotion tuning")]
    [Range(0.1f, 1f)]
    public float backwardSpeedMultiplier = 0.82f;
    [Range(1f, 30f)]
    public float locomotionDirectionSharpness = 12f;

    [Header("Time available for combo")]
    public int term;

    [Header("Input Buffer Settings")]
    public float inputBufferWindow = 0.4f;
    [Tooltip("How far through the attack animation (0.0 to 1.0) before you can chain the next attack without hitting Idle. E.g., 0.85 = 85% complete.")]
    public float comboChainPoint = 0.85f;

    [Header("Combat & Health System")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float attackDamage = 10f;
    public float skillDamage = 20f;
    [Tooltip("The exact name of the Death state/animation in the Animator window.")]
    public string deathAnimationState = "Death";

    private float transitionDelay = 0f;
    private AttackSystem attackSystem;
    private DefenseSystem defenseSystem;
    private EvadeSystem evadeSystem;

    public bool isJump;
    private float _animSpeedMultiplier = 1f;
    
    // Status lock trackers
    private bool isAttacking;
    private bool canChainAttack;
    public bool isDead;
    private float currentHorizontalIntent;
    private float locomotionDirection;
    private bool scriptedLocomotionActive;
    private float scriptedLocomotionDirection;

    private static readonly int RunHash = Animator.StringToHash("Run");
    private static readonly int WalkHash = Animator.StringToHash("Walk");
    private static readonly int BlockHash = Animator.StringToHash("Block");
    private static readonly int CrouchHash = Animator.StringToHash("Crouch");
    private static readonly int LocomotionDirectionHash = Animator.StringToHash("LocomotionDirection");

    [Header("Input Lock (game-state, not death)")]
    [Tooltip("Set by the game manager during pre-round buffers and walk-backs. " +
             "Blocks all player input without marking the character as dead.")]
    public bool inputLocked = false;

    [Header("Network Input Override")]
    public bool isNetworkControlled = false;
    public bool netRight;
    public bool netLeft;
    public bool netCrouch;
    public bool netSprint;
    public bool netJump;
    public bool netAttack;
    public bool netBlock;
    public bool netBlockDown;
    public bool netBlockUp;
    public bool netDodge;
    public bool[] netSkills = new bool[8];

    public void ConsumeNetworkTriggers()
    {
        netJump = false;
        netAttack = false;
        netBlockDown = false;
        netBlockUp = false;
        netDodge = false;
        for (int i = 0; i < 8; i++) netSkills[i] = false;
    }

    // --- Input Helper Methods ---
    private Gamepad GetGamepad()
    {
        if (gamepadIndex >= 0 && Gamepad.all.Count > gamepadIndex)
        {
            return Gamepad.all[gamepadIndex];
        }
        return null;
    }

    private bool GamepadMovementActive()
    {
        return gamepadIndex >= 0 && GetGamepad() != null;
    }

    private bool GamepadHorizontalRight(Gamepad pad)
    {
        float x = pad.leftStick.x.ReadValue();
        if (x > moveStickDeadZone) return true;
        Vector2 d = pad.dpad.ReadValue();
        return d.x > 0.35f || pad.dpad.right.isPressed;
    }

    private bool GamepadHorizontalLeft(Gamepad pad)
    {
        float x = pad.leftStick.x.ReadValue();
        if (x < -moveStickDeadZone) return true;
        Vector2 d = pad.dpad.ReadValue();
        return d.x < -0.35f || pad.dpad.left.isPressed;
    }

    private bool GamepadVerticalDown(Gamepad pad)
    {
        float y = pad.leftStick.y.ReadValue();
        if (y < -moveStickDeadZone) return true;
        Vector2 d = pad.dpad.ReadValue();
        return d.y < -0.35f || pad.dpad.down.isPressed;
    }

    private bool IsRightPressed()
    {
        if (isNetworkControlled) return netRight;
        if (GamepadMovementActive())
            return GamepadHorizontalRight(GetGamepad());
        return Input.GetKey(keyRight);
    }

    private bool IsLeftPressed()
    {
        if (isNetworkControlled) return netLeft;
        if (GamepadMovementActive())
            return GamepadHorizontalLeft(GetGamepad());
        return Input.GetKey(keyLeft);
    }

    private bool IsCrouchPressed()
    {
        if (isNetworkControlled) return netCrouch;
        if (GamepadMovementActive())
            return GamepadVerticalDown(GetGamepad());
        return Input.GetKey(keyCrouch);
    }

    private bool IsSprintPressed()
    {
        if (isNetworkControlled) return netSprint;
        if (GamepadMovementActive())
            return GetGamepad().leftTrigger.isPressed;
        return Input.GetKey(keySprint);
    }

    private bool WasJumpPressed()
    {
        if (isNetworkControlled) return netJump;
        if (GamepadMovementActive())
            return GetGamepad().buttonSouth.wasPressedThisFrame;
        return Input.GetKeyDown(keyJump);
    }

    private bool WasAttackPressed()
    {
        if (isNetworkControlled) return netAttack;
        if (GamepadMovementActive())
            return GetGamepad().buttonWest.wasPressedThisFrame;
        return Input.GetKeyDown(keyAttack) || Input.GetMouseButtonDown(0);
    }

    private bool IsBlockPressed()
    {
        if (isNetworkControlled) return netBlock;
        if (GamepadMovementActive())
            return GetGamepad().rightTrigger.isPressed;
        return Input.GetKey(keyBlock) || Input.GetMouseButton(1);
    }

    private bool WasBlockPressed()
    {
        if (isNetworkControlled) return netBlockDown;
        if (GamepadMovementActive())
            return GetGamepad().rightTrigger.wasPressedThisFrame;
        return Input.GetKeyDown(keyBlock) || Input.GetMouseButtonDown(1);
    }

    private bool WasBlockReleased()
    {
        if (isNetworkControlled) return netBlockUp;
        if (GamepadMovementActive())
            return GetGamepad().rightTrigger.wasReleasedThisFrame;
        return Input.GetKeyUp(keyBlock) || Input.GetMouseButtonUp(1);
    }

    private bool WasDodgePressed()
    {
        if (isNetworkControlled) return netDodge;
        if (GamepadMovementActive())
            return GetGamepad().buttonEast.wasPressedThisFrame;
        return Input.GetKeyDown(keyDodge);
    }

    private bool WasSkillPressed(int index)
    {
        if (isNetworkControlled) return index >= 0 && index < 8 ? netSkills[index] : false;
        if (GamepadMovementActive())
        {
            Gamepad pad = GetGamepad();
            if (index == 0) return pad.leftShoulder.wasPressedThisFrame;
            if (index == 1) return pad.rightShoulder.wasPressedThisFrame;
            return false;
        }
        KeyCode[] keys = { skill1, skill2, skill3, skill4, skill5, skill6, skill7, skill8 };
        return index >= 0 && index < keys.Length && Input.GetKeyDown(keys[index]);
    }

    /// <summary>
    /// Called by LocalGameManager to assign an input device to this player.
    /// padIndex >= 0 means gamepad-only; -1 means keyboard-only.
    /// When playerNumber == 2 and no gamepad is assigned, keys are remapped to
    /// arrow keys so P2's keyboard doesn't clash with P1's WASD bindings.
    /// </summary>
    public void ConfigureForPlayer(int playerNumber, int padIndex)
    {
        gamepadIndex = padIndex;

        if (playerNumber == 2 && padIndex < 0)
        {
            // Remap P2 to arrow keys + right-side keys so it doesn't share P1's WASD
            keyLeft   = KeyCode.LeftArrow;
            keyRight  = KeyCode.RightArrow;
            keyJump   = KeyCode.UpArrow;
            keyCrouch = KeyCode.DownArrow;
            keyAttack = KeyCode.L;
            keyBlock  = KeyCode.Semicolon;
            keyDodge  = KeyCode.RightShift;
            keySprint = KeyCode.RightControl;
            skill1    = KeyCode.Keypad1;
            skill2    = KeyCode.Keypad2;
            skill3    = KeyCode.Keypad3;
            skill4    = KeyCode.Keypad4;
            skill5    = KeyCode.Keypad5;
            skill6    = KeyCode.Keypad6;
            skill7    = KeyCode.Keypad7;
            skill8    = KeyCode.Keypad8;
        }

        Debug.Log($"[PlayerController_Platform] Player {playerNumber} configured — " +
                  $"gamepadIndex={padIndex}, " +
                  $"device={(padIndex >= 0 ? $"Gamepad {padIndex}" : "Keyboard")}");
    }

    /// <summary>
    /// Called by FighterController to apply FighterDef stats at runtime.
    /// Modifies speed, damage, health, and animator speed to reflect character classification.
    /// </summary>
    public void ApplyFighterStats(float moveSpeed, float jumpForce, float attackDmg,
                                   float skillDmg, float maxHealthVal, float currentHealthVal, float animSpeedMult)
    {
        ground_move_speed = moveSpeed;
        speed_move = jumpForce * 0.5f;  // Scale air movement with jump force
        attackDamage = attackDmg;
        skillDamage = skillDmg;
        maxHealth = maxHealthVal;
        currentHealth = currentHealthVal;

        _animSpeedMultiplier = animSpeedMult;
        if (anim != null) anim.speed = _animSpeedMultiplier;

        Debug.Log($"[PlayerController_Platform] Stats applied — speed_move={speed_move:F1}, " +
                  $"attackDmg={attackDamage:F1}, skillDmg={skillDamage:F1}, " +
                  $"health={currentHealth}/{maxHealth}, animSpeed={_animSpeedMultiplier:F2}");
    }
    // ----------------------------

    private void Start()
    {
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
        if (anim != null) anim.speed = _animSpeedMultiplier;

        fighterController = GetComponentInParent<FighterController>();
        movementController = fighterController != null ? fighterController.GetComponent<MovementController>() : null;
        attackSystem = GetComponentInParent<AttackSystem>();
        defenseSystem = GetComponentInParent<DefenseSystem>();
        evadeSystem = GetComponentInParent<EvadeSystem>();
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        FighterController fighterController = GetComponentInParent<FighterController>();
        if (fighterController != null)
        {
            fighterController.TakeDamage(damage);
            currentHealth = fighterController.CurrentHealth;
            isDead = fighterController.IsDead;
            return;
        }

        // Block logic: if currently blocking, negate or reduce damage (e.g., take 0 damage or 10% damage)
        if (anim.GetBool("Block"))
        {
            Debug.Log($"{gameObject.name} BLOCKED the attack!");
            // Optional: You can still take chip damage here, e.g., damage *= 0.1f;
            return; 
        }

        currentHealth -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage! HP left: {currentHealth}/{maxHealth}");

        // If you had a Hurt animation, you could trigger it here.
        // anim.SetTrigger("Hurt");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log($"{gameObject.name} has DIED!");
        
        // Force the animator to transition to the Death state, ignoring triggers/lines map
        anim.CrossFadeInFixedTime(deathAnimationState, 0.15f); 

        // Turn off colliders so they don't block attacks/players while dead
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // Disable rigidbodies from moving
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // Leave this script enabled ONLY if we needed it to run death countdowns, 
        // otherwise we just return early in Update(). 
    }

    private void Update()
    {
        if (isDead || inputLocked) return;

        currentHorizontalIntent = GetFilteredHorizontalIntent();
        movementController?.SetMoveInput(new Vector2(currentHorizontalIntent, IsCrouchPressed() ? -1f : 0f));
        UpdateAttackState();
        UpdateLocomotion();
        transform.position = new Vector3(transform.position.x, transform.position.y, 0f);

        if (!isJump)
        {
            Crouch();

            if (WasAttackPressed())
            {
                anim.SetBool("Crouch", false);
                attackSystem?.OnLightAttackInput(true);
            }

            for (int i = 0; i < 8; i++)
            {
                if (WasSkillPressed(i))
                {
                    anim.SetBool("Crouch", false);
                    attackSystem?.TrySpecialAttack(i);
                    break;
                }
            }

            if (!isAttacking)
            {
                Dodge();
                Jump();
                Block();
            }
            else if (!IsCrouchPressed() && anim.GetBool("Crouch"))
            {
                anim.SetBool("Crouch", false);
            }
        }

        if (isNetworkControlled) ConsumeNetworkTriggers();
    }

    private float GetRawHorizontalIntent()
    {
        float horizontal = 0f;

        if (IsLeftPressed())
            horizontal -= 1f;

        if (IsRightPressed())
            horizontal += 1f;

        return Mathf.Clamp(horizontal, -1f, 1f);
    }

    private float GetFilteredHorizontalIntent()
    {
        float desired = GetRawHorizontalIntent();
        if (fighterController == null || Mathf.Abs(desired) < 0.01f)
            return desired;

        var coordinator = fighterController.GetSceneCoordinator();
        return coordinator != null ? coordinator.FilterHorizontalIntent(fighterController, desired) : desired;
    }

    private void Rotate()
    {
        if (Mathf.Abs(currentHorizontalIntent) < 0.01f)
        {
            ApplyGroundLocomotionState(0f);
            return;
        }

        Move();
    }
    
    private void Move()
    {
        if (isJump)
        {
            transform.position += Vector3.right * (currentHorizontalIntent * speed_move * Time.deltaTime);
            ApplyGroundLocomotionState(0f);
            return;
        }

        float direction = GetSignedLocomotionDirection(currentHorizontalIntent);
        ApplyGroundLocomotionState(direction);
    }

    private void UpdateAttackState()
    {
        if (transitionDelay > 0f)
        {
            transitionDelay -= Time.deltaTime;
            isAttacking = true;
            canChainAttack = false;
            return;
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        isAttacking = stateInfo.IsTag("Attack") || stateInfo.IsName("Attack1") || stateInfo.IsName("Attack2") || stateInfo.IsName("Attack3") ||
                      stateInfo.IsName("Skill1") || stateInfo.IsName("Skill2") || stateInfo.IsName("Skill3") || stateInfo.IsName("Skill4") ||
                      stateInfo.IsName("Skill5") || stateInfo.IsName("Skill6") || stateInfo.IsName("Skill7") || stateInfo.IsName("Skill8");

        float animProgress = stateInfo.normalizedTime % 1f;
        bool isTransitioning = anim.IsInTransition(0);

        canChainAttack = !isAttacking || (isAttacking && animProgress >= comboChainPoint && !isTransitioning);
    }

    private void UpdateLocomotion()
    {
        if (isAttacking || IsBlockPressed() || anim.GetBool(BlockHash))
        {
            ApplyGroundLocomotionState(0f);
            return;
        }

        Rotate();
    }

    private void SetLegacyLocomotionState(bool run, bool walk)
    {
        anim.SetBool(RunHash, run);
        anim.SetBool(WalkHash, walk);
    }

    private float GetSignedLocomotionDirection(float horizontalIntent)
    {
        if (fighterController == null || Mathf.Abs(horizontalIntent) < 0.01f)
            return 0f;

        float facingSign = fighterController.FacingRight ? 1f : -1f;
        return Mathf.Sign(horizontalIntent) == facingSign ? 1f : -1f;
    }

    private void ApplyGroundLocomotionState(float targetDirection)
    {
        if (anim == null)
            return;

        float sharpness = Mathf.Max(1f, locomotionDirectionSharpness);
        locomotionDirection = Mathf.MoveTowards(
            locomotionDirection,
            targetDirection,
            sharpness * Time.deltaTime);

        anim.SetFloat(LocomotionDirectionHash, locomotionDirection);

        if (Mathf.Abs(targetDirection) < 0.01f)
        {
            SetLegacyLocomotionState(false, false);
            return;
        }

        if (targetDirection > 0f)
        {
            SetLegacyLocomotionState(true, false);
            return;
        }

        SetLegacyLocomotionState(false, false);

        int targetStateHash = WalkHash;
        AnimatorStateInfo currentState = anim.GetCurrentAnimatorStateInfo(0);
        if (anim.IsInTransition(0) || currentState.shortNameHash == targetStateHash)
            return;

        anim.CrossFadeInFixedTime(targetStateHash, 0.08f);
    }

    public void SetScriptedLocomotion(float signedDirection)
    {
        scriptedLocomotionActive = Mathf.Abs(signedDirection) > 0.01f;
        scriptedLocomotionDirection = Mathf.Clamp(signedDirection, -1f, 1f);
        ApplyGroundLocomotionState(scriptedLocomotionDirection);
    }

    public void ClearScriptedLocomotion()
    {
        scriptedLocomotionActive = false;
        scriptedLocomotionDirection = 0f;
        ApplyGroundLocomotionState(0f);
    }

    private bool ShouldDriveGroundLocomotionManually()
    {
        return !isJump
            && !isAttacking
            && GetActiveLocomotionDirectionMagnitude() > 0.01f
            && !anim.GetBool(BlockHash)
            && !anim.GetBool(CrouchHash)
            && !IsDodgeAnimationActive()
            && !scriptedLocomotionActive;
    }

    private void OnAnimatorMove()
    {
        if (anim == null || !anim.applyRootMotion || isDead || inputLocked)
            return;

        if (scriptedLocomotionActive)
        {
            float directionSign = fighterController != null && fighterController.FacingRight ? 1f : -1f;
            float deltaX = directionSign * scriptedLocomotionDirection * ground_move_speed * GetGroundSpeedMultiplier(scriptedLocomotionDirection) * Time.deltaTime;
            transform.position = new Vector3(transform.position.x + deltaX, transform.position.y, 0f);
            return;
        }

        if (ShouldDriveGroundLocomotionManually())
        {
            float deltaX = currentHorizontalIntent * ground_move_speed * GetGroundSpeedMultiplier(locomotionDirection) * Time.deltaTime;
            transform.position = new Vector3(transform.position.x + deltaX, transform.position.y, 0f);
            return;
        }

        Vector3 deltaPosition = anim.deltaPosition;
        transform.position = new Vector3(
            transform.position.x + deltaPosition.x,
            transform.position.y + deltaPosition.y,
            0f);
    }

    void Dodge()
    {
        if (WasDodgePressed())
        {
            if (evadeSystem != null && evadeSystem.TryDodge(GetMoveDirection()))
            {
                anim.SetBool("Crouch", false);
                anim.SetTrigger("Dodge");
            }
        }
    }

    void Block()
    {
        defenseSystem?.OnBlockInput(IsBlockPressed());

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
        bool isCrouching = IsCrouchPressed() && !isAttacking && !anim.GetBool("Block");
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

    Vector2 GetMoveDirection()
    {
        Vector2 moveDirection = Vector2.zero;

        moveDirection.x = GetRawHorizontalIntent();

        if (IsCrouchPressed())
            moveDirection.y -= 1f;

        return moveDirection.sqrMagnitude > 0f ? moveDirection.normalized : Vector2.zero;
    }
    
    void JumpEnd()
    {
        isJump = false;
    }

    public void ResetGameplayState(bool snapToIdle)
    {
        transitionDelay = 0f;
        isAttacking = false;
        canChainAttack = false;
        isJump = false;
        currentHorizontalIntent = 0f;
        locomotionDirection = 0f;
        scriptedLocomotionActive = false;
        scriptedLocomotionDirection = 0f;

        if (anim == null)
            return;

        anim.SetFloat(LocomotionDirectionHash, 0f);
        anim.SetBool(RunHash, false);
        anim.SetBool(WalkHash, false);
        anim.SetBool(BlockHash, false);
        anim.SetBool(CrouchHash, false);

        anim.ResetTrigger("Jump");
        anim.ResetTrigger("Dodge");
        anim.ResetTrigger("Attack1");
        anim.ResetTrigger("Attack2");
        anim.ResetTrigger("Attack3");
        anim.ResetTrigger("Skill1");
        anim.ResetTrigger("Skill2");
        anim.ResetTrigger("Skill3");
        anim.ResetTrigger("Skill4");
        anim.ResetTrigger("Skill5");
        anim.ResetTrigger("Skill6");
        anim.ResetTrigger("Skill7");
        anim.ResetTrigger("Skill8");

        if (snapToIdle)
            anim.CrossFadeInFixedTime("Idle", 0.05f);
    }

    public bool IsDodgeAnimationActive()
    {
        if (anim == null || !anim.isActiveAndEnabled)
            return false;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        if (anim.IsInTransition(0))
            return stateInfo.IsName("Dodge") || stateInfo.IsName("DodgeRoll") || stateInfo.IsTag("Dodge");

        return stateInfo.IsName("Dodge")
            || stateInfo.IsName("DodgeRoll")
            || stateInfo.IsTag("Dodge");
    }

    private float GetActiveLocomotionDirectionMagnitude()
    {
        if (scriptedLocomotionActive)
            return Mathf.Abs(scriptedLocomotionDirection);

        return Mathf.Abs(locomotionDirection);
    }

    private float GetGroundSpeedMultiplier(float signedDirection)
    {
        return signedDirection < -0.01f ? backwardSpeedMultiplier : 1f;
    }


}
