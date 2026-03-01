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

    [Header("Combat & Health System")]
    public float maxHealth = 100f;
    public float currentHealth;
    [Tooltip("The center point of this character's attack hitboxes")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public float attackDamage = 10f;
    public float skillDamage = 20f;
    [Tooltip("Put the opposing player's layer here, or default 'Player' if everyone is on the same layer.")]
    public LayerMask enemyLayers;
    [Tooltip("The exact name of the Death state/animation in the Animator window.")]
    public string deathAnimationState = "Death";

    private float attackBufferTimer = 0f;
    private float transitionDelay = 0f;

    public bool isJump;
    
    // Status lock trackers
    private bool isAttacking;
    private bool canChainAttack;
    public bool isDead;

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
        currentHealth = maxHealth;

        // Auto-create attack point if not assigned
        if (attackPoint == null)
        {
            GameObject ap = new GameObject("AttackPoint");
            ap.transform.SetParent(this.transform);
            // Default offset slightly in front and up
            ap.transform.localPosition = new Vector3(0, 1f, 1f); 
            attackPoint = ap.transform;
        }

        // Automatically map detailed bone-based hitboxes to the character if missing
        if (GetComponentsInChildren<Collider>().Length == 0)
        {
            GenerateBoneColliders();
        }
    }

    private void GenerateBoneColliders()
    {
        SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null && smr.bones != null && smr.bones.Length > 0)
        {
            foreach (Transform bone in smr.bones)
            {
                // Skip if this bone already has a collider
                if (bone.GetComponent<Collider>() != null) continue;

                CapsuleCollider col = bone.gameObject.AddComponent<CapsuleCollider>();
                col.isTrigger = true; // Trigger prevents them from breaking gravity/physics

                // Calculate size based on distance to child bone
                if (bone.childCount > 0)
                {
                    Transform child = bone.GetChild(0);
                    Vector3 localChildPos = bone.InverseTransformPoint(child.position);
                    float localLength = localChildPos.magnitude;
                    
                    // Prevent infinitely small or zero-length colliders
                    if (localLength < 0.01f)
                    {
                        col.radius = 0.05f;
                        col.height = 0.1f;
                        col.center = Vector3.zero;
                        continue;
                    }

                    col.height = localLength;
                    col.radius = Mathf.Max(0.05f, localLength * 0.25f);
                    col.center = localChildPos / 2f; // Center the capsule halfway down the bone
                    
                    // Determine which way the capsule should face (X=0, Y=1, Z=2)
                    float absX = Mathf.Abs(localChildPos.x);
                    float absY = Mathf.Abs(localChildPos.y);
                    float absZ = Mathf.Abs(localChildPos.z);

                    if (absX >= absY && absX >= absZ) col.direction = 0;
                    else if (absY >= absX && absY >= absZ) col.direction = 1;
                    else col.direction = 2;
                }
                else
                {
                    // Leaf nodes (like fingertips or toes)
                    col.radius = 0.05f;
                    col.height = 0.1f;
                    col.center = Vector3.zero;
                }
            }
            Debug.Log($"Automatically generated a precise skeletal hitbox mapping for: {gameObject.name}");
        }
        else
        {
            // Fallback to a single blocky body shape if no skeleton exists
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.4f;
            capsule.center = new Vector3(0, 1f, 0);
            capsule.isTrigger = true;
            Debug.Log($"Generated fallback default body hitbox for: {gameObject.name}");
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

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
        if (isDead) return;

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
            
            // Adjust damage based on combo hit (e.g., hit 3 does more damage)
            float currentDmg = attackDamage;
            
            switch (clickCount)
            {
                case 0:
                    anim.SetTrigger("Attack1");
                    isTimer = true;
                    clickCount = 1;
                    StartCoroutine(DealDamageAfterDelay(0.2f, currentDmg));
                    break;
                case 1:
                    anim.SetTrigger("Attack2");
                    clickCount = 2;
                    StartCoroutine(DealDamageAfterDelay(0.2f, currentDmg * 1.2f));
                    break;
                case 2:
                    anim.SetTrigger("Attack3");
                    clickCount = 0;
                    isTimer = false;
                    StartCoroutine(DealDamageAfterDelay(0.3f, currentDmg * 1.5f));
                    break;
            }
            
            // Reset combo timer upon executing a new attack
            timer = 0f;
        }
    }

    private IEnumerator DealDamageAfterDelay(float delay, float damage)
    {
        yield return new WaitForSeconds(delay);

        if (isDead) yield break;

        // Perform checks against actual character Colliders to perfectly match their shape
        if (attackPoint != null)
        {
            // Find all players in the scene directly
            PlayerController_Platform[] allPlayers = FindObjectsOfType<PlayerController_Platform>();

            foreach (PlayerController_Platform enemy in allPlayers)
            {
                // Ensure we don't hit ourselves and they aren't already dead
                if (enemy == this || enemy.isDead) continue;

                bool hit = false;
                Collider[] enemyColliders = enemy.GetComponentsInChildren<Collider>();

                if (enemyColliders.Length > 0)
                {
                    // Check if our attack sphere overlaps with ANY of their physical shaped hitboxes (limbs, body, etc)
                    foreach (Collider col in enemyColliders)
                    {
                        // ClosestPoint returns the exact point on the surface of their collider closest to our attack.
                        Vector3 closestPoint = col.ClosestPoint(attackPoint.position);
                        
                        if (Vector3.Distance(attackPoint.position, closestPoint) <= attackRange)
                        {
                            hit = true;
                            break; 
                        }
                    }
                }
                else
                {
                    // Fallback to center distance if they somehow have zero colliders
                    Vector3 enemyCenter = enemy.transform.position + Vector3.up * 1f;
                    if (Vector3.Distance(attackPoint.position, enemyCenter) <= attackRange)
                    {
                        hit = true;
                    }
                }

                if (hit)
                {
                    enemy.TakeDamage(damage);
                }
            }
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

    void Skill1() { if (WasSkillPressed(0)) { anim.SetTrigger("Skill1"); StartCoroutine(DealDamageAfterDelay(0.4f, skillDamage)); } }
    void Skill2() { if (WasSkillPressed(1)) { anim.SetTrigger("Skill2"); StartCoroutine(DealDamageAfterDelay(0.4f, skillDamage)); } }
    void Skill3() { if (WasSkillPressed(2)) { anim.SetTrigger("Skill3"); StartCoroutine(DealDamageAfterDelay(0.4f, skillDamage)); } }
    void Skill4() { if (WasSkillPressed(3)) { anim.SetTrigger("Skill4"); StartCoroutine(DealDamageAfterDelay(0.4f, skillDamage)); } }
    void Skill5() { if (WasSkillPressed(4)) { anim.SetTrigger("Skill5"); StartCoroutine(DealDamageAfterDelay(0.4f, skillDamage)); } }
    void Skill6() { if (WasSkillPressed(5)) { anim.SetTrigger("Skill6"); StartCoroutine(DealDamageAfterDelay(0.4f, skillDamage)); } }
    void Skill7() { if (WasSkillPressed(6)) { anim.SetTrigger("Skill7"); StartCoroutine(DealDamageAfterDelay(0.4f, skillDamage)); } }
    void Skill8() { if (WasSkillPressed(7)) { anim.SetTrigger("Skill8"); StartCoroutine(DealDamageAfterDelay(0.4f, skillDamage)); } }

    private void OnDrawGizmosSelected()
    {
        // Visualize the hitbox in the Unity Editor
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
