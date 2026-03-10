using UnityEngine;
using Adaptabrawl.Data;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Bridges Shinabro animations with Adaptabrawl combat system.
    /// Plays animations and tracks frame timing for combo windows.
    /// Hitbox activation is handled by HitboxEmitter on the Stander via CombatFSM events.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AnimationBridge : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

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

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                var animators = GetComponentsInChildren<Animator>(true);
                foreach (var a in animators)
                {
                    if (a != null && a.runtimeAnimatorController != null)
                    {
                        animator = a;
                        break;
                    }
                }
            }
        }

        private void Update()
        {
            if (isPlayingMove)
                UpdateMove();

            if (canCombo)
                UpdateComboWindow();
        }

        /// <summary>Plays an animated move.</summary>
        public bool PlayMove(AnimatedMoveDef move)
        {
            if (move == null)
            {
                Debug.LogWarning("AnimationBridge: Attempted to play null move");
                return false;
            }

            if (isPlayingMove && !canCombo)
            {
                if (showDebugInfo)
                    Debug.Log($"AnimationBridge: Cannot play {move.moveName}, already playing {currentMove.moveName}");
                return false;
            }

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

            TriggerAnimation(move);
            OnMoveStart?.Invoke(move);

            if (showDebugInfo)
                Debug.Log($"AnimationBridge: Started move {move.moveName}");
        }

        private void UpdateMove()
        {
            if (currentMove == null) return;

            animationTime += Time.deltaTime;
            currentFrame = Mathf.RoundToInt(animationTime * 60f);

            // Check for combo window
            if (currentMove.canCombo && !canCombo)
            {
                float animLength = currentMove.GetAnimationLength();
                if (animationTime >= animLength * 0.6f)
                {
                    canCombo = true;
                    comboTimer = 0f;
                }
            }

            float moveLength = currentMove.GetAnimationLength();
            if (animationTime >= moveLength)
                EndMove();
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
            OnMoveEnd?.Invoke(finishedMove);

            if (showDebugInfo)
                Debug.Log($"AnimationBridge: Ended move {finishedMove.moveName}");

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
            if (animator == null || animator.runtimeAnimatorController == null) return;

            switch (move.parameterType)
            {
                case AnimatorParameterType.Trigger:
                    animator.SetTrigger(move.animatorTrigger);
                    break;
                case AnimatorParameterType.Bool:
                    animator.SetBool(move.animatorTrigger, true);
                    break;
                case AnimatorParameterType.Int:
                    animator.SetInteger(move.animatorTrigger, 1);
                    break;
                case AnimatorParameterType.Float:
                    animator.SetFloat(move.animatorTrigger, 1f);
                    break;
            }
        }

        public void StopMove()
        {
            isPlayingMove = false;
            canCombo = false;
            currentMove = null;
        }

        public bool CanPlayMove() => !isPlayingMove || canCombo;

        // Animation Events (called by Unity animation clips)
        public void AnimEvent_MoveComplete() => EndMove();

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
