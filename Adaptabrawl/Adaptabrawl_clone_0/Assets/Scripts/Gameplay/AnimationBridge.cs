using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Combat;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Bridges Shinabro animations with Adaptabrawl combat system.
    /// Plays animations and triggers hitboxes at appropriate times.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AnimationBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private HitboxManager hitboxManager;
        [SerializeField] private HitboxHurtboxSpawner spawner;
        
        [Header("Current State")]
        [SerializeField] private AnimatedMoveDef currentMove;
        [SerializeField] private float animationTime = 0f;
        [SerializeField] private bool isPlayingMove = false;
        [SerializeField] private int currentFrame = 0;
        
        [Header("Combo System")]
        [SerializeField] private bool canCombo = false;
        [SerializeField] private float comboTimer = 0f;
        [SerializeField] private AnimatedMoveDef queuedComboMove;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        public bool IsPlayingMove => isPlayingMove;
        public AnimatedMoveDef CurrentMove => currentMove;
        public bool CanCombo => canCombo;
        
        // Events
        public System.Action<AnimatedMoveDef> OnMoveStart;
        public System.Action<AnimatedMoveDef> OnMoveEnd;
        public System.Action<AnimatedMoveDef> OnHitboxActivate;
        public System.Action<AnimatedMoveDef> OnHitboxDeactivate;
        
        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            
            if (hitboxManager == null)
                hitboxManager = GetComponent<HitboxManager>();
            
            if (spawner == null)
                spawner = GetComponent<HitboxHurtboxSpawner>();
        }
        
        private void Update()
        {
            if (isPlayingMove)
            {
                UpdateMove();
            }
            
            if (canCombo)
            {
                UpdateComboWindow();
            }
        }
        
        /// <summary>
        /// Plays an animated move
        /// </summary>
        public bool PlayMove(AnimatedMoveDef move)
        {
            if (move == null)
            {
                Debug.LogWarning("AnimationBridge: Attempted to play null move");
                return false;
            }
            
            // Check if already playing a move
            if (isPlayingMove && !canCombo)
            {
                if (showDebugInfo)
                    Debug.Log($"AnimationBridge: Cannot play {move.moveName}, already playing {currentMove.moveName}");
                return false;
            }
            
            // If in combo window, queue the move
            if (canCombo)
            {
                queuedComboMove = move;
                return true;
            }
            
            StartMove(move);
            return true;
        }
        
        private void StartMove(AnimatedMoveDef move)
        {
            currentMove = move;
            animationTime = 0f;
            currentFrame = 0;
            isPlayingMove = true;
            canCombo = false;
            queuedComboMove = null;
            
            // Trigger animation
            TriggerAnimation(move);
            
            // Spawn hitboxes
            if (spawner != null)
            {
                spawner.SpawnHitboxesForMove(move);
            }
            
            OnMoveStart?.Invoke(move);
            
            if (showDebugInfo)
                Debug.Log($"AnimationBridge: Started move {move.moveName}");
        }
        
        private void UpdateMove()
        {
            if (currentMove == null) return;
            
            animationTime += Time.deltaTime;
            currentFrame = Mathf.RoundToInt(animationTime * 60f); // 60 FPS
            
            // Update hitboxes based on frame
            UpdateHitboxes();
            
            // Check for combo window
            if (currentMove.canCombo && !canCombo)
            {
                float animLength = currentMove.GetAnimationLength();
                float comboStartTime = animLength * 0.6f; // Combo window opens at 60% of animation
                
                if (animationTime >= comboStartTime)
                {
                    canCombo = true;
                    comboTimer = 0f;
                }
            }
            
            // Check if move is complete
            float moveLength = currentMove.GetAnimationLength();
            if (animationTime >= moveLength)
            {
                EndMove();
            }
        }
        
        private void UpdateHitboxes()
        {
            if (hitboxManager == null || currentMove == null) return;
            
            // Update hitbox manager with current frame
            hitboxManager.UpdateFrame(currentFrame);
            
            // Check if we just entered active frames
            if (currentFrame == currentMove.startupFrames)
            {
                hitboxManager.ActivateHitbox(currentMove);
                OnHitboxActivate?.Invoke(currentMove);
                
                if (showDebugInfo)
                    Debug.Log($"AnimationBridge: Activated hitbox for {currentMove.moveName} at frame {currentFrame}");
            }
            
            // Check if we just left active frames
            if (currentFrame == currentMove.startupFrames + currentMove.activeFrames)
            {
                hitboxManager.DeactivateHitbox();
                OnHitboxDeactivate?.Invoke(currentMove);
                
                if (showDebugInfo)
                    Debug.Log($"AnimationBridge: Deactivated hitbox for {currentMove.moveName} at frame {currentFrame}");
            }
        }
        
        private void UpdateComboWindow()
        {
            comboTimer += Time.deltaTime;
            
            if (comboTimer >= currentMove.comboWindow)
            {
                canCombo = false;
                queuedComboMove = null;
            }
        }
        
        private void EndMove()
        {
            AnimatedMoveDef finishedMove = currentMove;
            
            // Deactivate hitboxes
            if (hitboxManager != null)
            {
                hitboxManager.DeactivateHitbox();
            }
            
            OnMoveEnd?.Invoke(finishedMove);
            
            if (showDebugInfo)
                Debug.Log($"AnimationBridge: Ended move {finishedMove.moveName}");
            
            // Check for queued combo
            if (queuedComboMove != null)
            {
                AnimatedMoveDef nextMove = queuedComboMove;
                currentMove = null;
                isPlayingMove = false;
                canCombo = false;
                
                StartMove(nextMove);
            }
            else
            {
                currentMove = null;
                isPlayingMove = false;
                canCombo = false;
            }
        }
        
        private void TriggerAnimation(AnimatedMoveDef move)
        {
            if (animator == null) return;
            
            switch (move.parameterType)
            {
                case AnimatorParameterType.Trigger:
                    animator.SetTrigger(move.animatorTrigger);
                    break;
                    
                case AnimatorParameterType.Bool:
                    animator.SetBool(move.animatorTrigger, true);
                    break;
                    
                case AnimatorParameterType.Int:
                    // For int parameters, you'd need to specify the value
                    animator.SetInteger(move.animatorTrigger, 1);
                    break;
                    
                case AnimatorParameterType.Float:
                    animator.SetFloat(move.animatorTrigger, 1f);
                    break;
            }
        }
        
        /// <summary>
        /// Stops current move immediately
        /// </summary>
        public void StopMove()
        {
            if (isPlayingMove)
            {
                if (hitboxManager != null)
                {
                    hitboxManager.DeactivateHitbox();
                }
                
                isPlayingMove = false;
                canCombo = false;
                currentMove = null;
            }
        }
        
        /// <summary>
        /// Checks if a move can be played
        /// </summary>
        public bool CanPlayMove()
        {
            return !isPlayingMove || canCombo;
        }
        
        /// <summary>
        /// Animation Event - Called by Unity animation events
        /// </summary>
        public void AnimEvent_ActivateHitbox()
        {
            if (hitboxManager != null && currentMove != null)
            {
                hitboxManager.ActivateHitbox(currentMove);
                OnHitboxActivate?.Invoke(currentMove);
            }
        }
        
        /// <summary>
        /// Animation Event - Called by Unity animation events
        /// </summary>
        public void AnimEvent_DeactivateHitbox()
        {
            if (hitboxManager != null)
            {
                hitboxManager.DeactivateHitbox();
                OnHitboxDeactivate?.Invoke(currentMove);
            }
        }
        
        /// <summary>
        /// Animation Event - Called when animation completes
        /// </summary>
        public void AnimEvent_MoveComplete()
        {
            EndMove();
        }
        
        /// <summary>
        /// Animation Event - Open combo window
        /// </summary>
        public void AnimEvent_ComboWindowOpen()
        {
            if (currentMove != null && currentMove.canCombo)
            {
                canCombo = true;
                comboTimer = 0f;
            }
        }
    }
}

