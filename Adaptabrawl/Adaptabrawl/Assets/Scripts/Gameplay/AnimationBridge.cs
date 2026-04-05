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
        [SerializeField] private Combat.CombatFSM combatFSM;

        [Header("Current State")]
        [SerializeField] private AnimatedMoveDef currentMove;
        [SerializeField] private float animationTime = 0f;
        [SerializeField] private bool isPlayingMove = false;
        [SerializeField] private int currentFrame = 0;
        [SerializeField] private bool combatDrivenPlayback = false;

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
            ResolveAnimator();

            if (combatFSM == null)
                combatFSM = GetComponent<Combat.CombatFSM>();
        }

        private void OnEnable()
        {
            SubscribeToCombat();
        }

        private void Start()
        {
            SubscribeToCombat();
        }

        private void OnDisable()
        {
            UnsubscribeFromCombat();
        }

        private void Update()
        {
            var attackSystem = GetComponent<Adaptabrawl.Attack.AttackSystem>();
            if (combatFSM != null && combatFSM.isActiveAndEnabled && (attackSystem == null || attackSystem.enabled))
                return;

            if (isPlayingMove)
                UpdateMove();

            if (canCombo)
                UpdateComboWindow();
        }

        /// <summary>Plays an animated move.</summary>
        public bool PlayMove(AnimatedMoveDef move)
        {
            var attackSystem = GetComponent<Adaptabrawl.Attack.AttackSystem>();
            if (combatFSM != null && combatFSM.isActiveAndEnabled && (attackSystem == null || attackSystem.enabled))
            {
                Debug.LogWarning("AnimationBridge: CombatFSM owns move playback during live combat.");
                return false;
            }

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
            ResolveAnimator();
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

        private void ResolveAnimator()
        {
            if (animator != null && animator.runtimeAnimatorController != null)
                return;

            animator = GetComponent<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
                return;

            var animators = GetComponentsInChildren<Animator>(true);
            foreach (var a in animators)
            {
                if (a != null && a.runtimeAnimatorController != null)
                {
                    animator = a;
                    return;
                }
            }
        }

        private void SubscribeToCombat()
        {
            if (combatFSM == null)
                combatFSM = GetComponent<Combat.CombatFSM>();

            if (combatFSM == null)
                return;

            combatFSM.OnMoveStarted -= HandleCombatMoveStarted;
            combatFSM.OnMoveEnded -= HandleCombatMoveEnded;
            combatFSM.OnMoveStarted += HandleCombatMoveStarted;
            combatFSM.OnMoveEnded += HandleCombatMoveEnded;
        }

        private void UnsubscribeFromCombat()
        {
            if (combatFSM == null)
                return;

            combatFSM.OnMoveStarted -= HandleCombatMoveStarted;
            combatFSM.OnMoveEnded -= HandleCombatMoveEnded;
        }

        private void HandleCombatMoveStarted(MoveDef move)
        {
            AnimatedMoveDef animatedMove = move as AnimatedMoveDef;
            if (animatedMove == null)
                return;

            currentMove = animatedMove;
            animationTime = 0f;
            currentFrame = 0;
            isPlayingMove = true;
            canCombo = false;
            queuedComboMove = null;
            combatDrivenPlayback = true;

            TriggerAnimation(animatedMove);
            OnMoveStart?.Invoke(animatedMove);
        }

        private void HandleCombatMoveEnded(MoveDef move)
        {
            if (!combatDrivenPlayback)
                return;

            AnimatedMoveDef animatedMove = move as AnimatedMoveDef;
            if (animatedMove != null)
                OnMoveEnd?.Invoke(animatedMove);

            combatDrivenPlayback = false;
            currentMove = null;
            animationTime = 0f;
            currentFrame = 0;
            isPlayingMove = false;
            canCombo = false;
            queuedComboMove = null;
        }

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
