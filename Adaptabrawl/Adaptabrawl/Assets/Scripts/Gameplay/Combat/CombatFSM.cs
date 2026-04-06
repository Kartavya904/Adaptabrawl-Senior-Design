using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Gameplay;
using System.Collections;

namespace Adaptabrawl.Combat
{
    public class CombatFSM : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FighterController fighterController;
        [SerializeField] private Animator combatAnimator;
        
        [Header("State")]
        private CombatState currentState = CombatState.Idle;
        private MoveDef currentMove = null;
        private int currentFrame = 0;
        private int totalFrames = 0;
        private bool hitboxWindowActive = false;
        
        [Header("Input Buffer")]
        [SerializeField] private float inputBufferWindow = 0.16f;
        private float lastInputTime = -1f;
        private MoveDef bufferedMove = null;
        
        [Header("Cancel Windows")]
        private bool inCancelWindow = false;
        private int cancelWindowStart = 0;
        private int cancelWindowEnd = 0;
        
        [Header("Events")]
        public System.Action<CombatState> OnStateChanged;
        public System.Action<MoveDef> OnMoveStarted;
        public System.Action<MoveDef> OnMoveEnded;
        /// <summary>Fired when a move enters its active (hitbox) frames. Passes the active MoveDef.</summary>
        public System.Action<MoveDef> OnHitboxActive;
        /// <summary>Fired when a move's active frames end.</summary>
        public System.Action OnHitboxInactive;
        
        private void Start()
        {
            if (fighterController == null)
                fighterController = GetComponent<FighterController>();

            ResolveCombatAnimator();
        }

        private void OnDisable()
        {
            DeactivateHitboxWindow();
        }
        
        private void Update()
        {
            UpdateInputBuffer();
            UpdateState();
        }
        
        private void UpdateInputBuffer()
        {
            // Clear old buffered inputs
            if (bufferedMove != null && Time.time - lastInputTime > inputBufferWindow)
            {
                bufferedMove = null;
            }
        }
        
        private void UpdateState()
        {
            if (currentMove == null)
            {
                currentState = CombatState.Idle;
                return;
            }
            
            currentFrame++;
            
            // Update cancel window
            if (currentMove != null)
            {
                inCancelWindow = currentFrame >= currentMove.cancelWindowStart && 
                                currentFrame <= currentMove.cancelWindowEnd;
            }
            
            // State transitions based on frame data
            if (currentFrame <= currentMove.startupFrames)
            {
                if (currentState != CombatState.Startup)
                {
                    SetState(CombatState.Startup);
                }
            }
            else if (currentFrame <= currentMove.startupFrames + currentMove.activeFrames)
            {
                if (currentState != CombatState.Active)
                {
                    SetState(CombatState.Active);
                    ActivateHitboxWindow();
                }
            }
            else if (currentFrame <= totalFrames)
            {
                if (currentState != CombatState.Recovery)
                {
                    SetState(CombatState.Recovery);
                    DeactivateHitboxWindow();
                }
            }
            else
            {
                // Hold the move until the live attack animation is actually finished.
                if (IsMoveAnimationStillPlaying())
                    return;

                EndMove();
            }
        }
        
        public bool TryStartMove(MoveDef move)
        {
            if (move == null) return false;
            
            // Check if we can start this move
            if (!CanStartMove(move))
            {
                // Buffer the input
                BufferInput(move);
                return false;
            }
            
            // Start the move
            StartMove(move);
            return true;
        }
        
        private bool CanStartMove(MoveDef move)
        {
            // Can't start new move if in certain states
            if (currentState == CombatState.Stunned || 
                currentState == CombatState.Staggered ||
                currentState == CombatState.ArmorBroken)
            {
                return false;
            }

            // Do not allow attack spam while any current move is still running.
            // We buffer the latest requested attack and execute it after the move ends.
            if (currentMove != null && IsAttackMove(move))
                return false;

            // Can cancel if in cancel window
            if (currentMove != null && inCancelWindow)
            {
                if (move.moveType == Data.MoveType.Dodge && currentMove.canCancelIntoDodge)
                    return true;
                if (move.moveType == Data.MoveType.Block && currentMove.canCancelIntoBlock)
                    return true;
                if (currentMove.canCancelIntoOtherMoves)
                    return true;
            }

            return currentMove == null && currentState == CombatState.Idle;
        }
        
        private void BufferInput(MoveDef move)
        {
            bufferedMove = move;
            lastInputTime = Time.time;
        }
        
        private void StartMove(MoveDef move)
        {
            currentMove = move;
            currentFrame = 0;
            totalFrames = ResolveMoveDurationFrames(move);
            inCancelWindow = false;
            cancelWindowStart = move.cancelWindowStart;
            cancelWindowEnd = move.cancelWindowEnd;
            inputBufferWindow = move.inputBufferWindow;
            
            SetState(CombatState.Startup);
            OnMoveStarted?.Invoke(move);
            
            // Apply status effects to self
            ApplyStatusEffects(move.statusEffectsOnSelf);
        }
        
        private void EndMove()
        {
            DeactivateHitboxWindow();

            if (currentMove != null)
            {
                OnMoveEnded?.Invoke(currentMove);
            }
            
            currentMove = null;
            currentFrame = 0;
            totalFrames = 0;
            SetState(CombatState.Idle);
            
            // Try to execute buffered move
            if (bufferedMove != null)
            {
                MoveDef buffered = bufferedMove;
                bufferedMove = null;
                TryStartMove(buffered);
            }
        }
        
        public void SetStunned(int frames)
        {
            SetState(CombatState.Stunned);
            StartCoroutine(StunCoroutine(frames));
        }
        
        public void SetStaggered(float duration)
        {
            SetState(CombatState.Staggered);
            StartCoroutine(StaggerCoroutine(duration));
        }
        
        public void SetArmorBroken(float duration)
        {
            SetState(CombatState.ArmorBroken);
            StartCoroutine(ArmorBreakCoroutine(duration));
        }
        
        private IEnumerator StunCoroutine(int frames)
        {
            yield return new WaitForSeconds(frames / 60f); // Assuming 60 FPS
            if (currentState == CombatState.Stunned)
            {
                SetState(CombatState.Idle);
            }
        }
        
        private IEnumerator StaggerCoroutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (currentState == CombatState.Staggered)
            {
                SetState(CombatState.Idle);
            }
        }
        
        private IEnumerator ArmorBreakCoroutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            if (currentState == CombatState.ArmorBroken)
            {
                SetState(CombatState.Idle);
            }
        }
        
        private void SetState(CombatState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnStateChanged?.Invoke(newState);
            }
        }

        private void ActivateHitboxWindow()
        {
            if (hitboxWindowActive)
                return;

            hitboxWindowActive = true;
            OnHitboxActive?.Invoke(currentMove);
        }

        private void DeactivateHitboxWindow()
        {
            if (!hitboxWindowActive)
                return;

            hitboxWindowActive = false;
            OnHitboxInactive?.Invoke();
        }
        
        private void ApplyStatusEffects(Data.StatusEffectData[] effects)
        {
            if (effects == null || effects.Length == 0) return;
            
            var statusSystem = GetComponent<StatusEffectSystem>();
            if (statusSystem != null)
            {
                foreach (var effect in effects)
                {
                    statusSystem.ApplyStatus(effect.statusDef, effect.stacks, effect.duration);
                }
            }
        }

        private static bool IsAttackMove(MoveDef move)
        {
            if (move == null)
                return false;

            return move.moveType == MoveType.LightAttack
                || move.moveType == MoveType.HeavyAttack
                || move.moveType == MoveType.Special;
        }

        private static int ResolveMoveDurationFrames(MoveDef move)
        {
            if (move is AnimatedMoveDef animatedMove)
            {
                int animationFrames = animatedMove.GetAnimationLengthFrames();
                if (animationFrames > 0)
                    return Mathf.Max(move.totalFrames, animationFrames);
            }

            return move.totalFrames;
        }

        private void ResolveCombatAnimator()
        {
            if (combatAnimator != null && combatAnimator.runtimeAnimatorController != null)
                return;

            PlayerController_Platform pcp = GetComponentInChildren<PlayerController_Platform>();
            if (pcp != null)
            {
                combatAnimator = pcp.GetComponent<Animator>();
                if (combatAnimator != null && combatAnimator.runtimeAnimatorController != null)
                    return;
            }

            Animator[] animators = GetComponentsInChildren<Animator>(true);
            foreach (Animator candidate in animators)
            {
                if (candidate != null && candidate.runtimeAnimatorController != null)
                {
                    combatAnimator = candidate;
                    return;
                }
            }
        }

        private bool IsMoveAnimationStillPlaying()
        {
            if (!IsAttackMove(currentMove))
                return false;

            ResolveCombatAnimator();
            if (combatAnimator == null || !combatAnimator.isActiveAndEnabled)
                return false;

            AnimatorStateInfo stateInfo = combatAnimator.GetCurrentAnimatorStateInfo(0);
            if (combatAnimator.IsInTransition(0))
                return true;

            return stateInfo.IsTag("Attack")
                || stateInfo.IsName("Attack1")
                || stateInfo.IsName("Attack2")
                || stateInfo.IsName("Attack3")
                || stateInfo.IsName("Skill1")
                || stateInfo.IsName("Skill2")
                || stateInfo.IsName("Skill3")
                || stateInfo.IsName("Skill4")
                || stateInfo.IsName("Skill5")
                || stateInfo.IsName("Skill6")
                || stateInfo.IsName("Skill7")
                || stateInfo.IsName("Skill8");
        }

        public void ForceResetState()
        {
            DeactivateHitboxWindow();

            currentMove = null;
            currentFrame = 0;
            totalFrames = 0;
            inCancelWindow = false;
            cancelWindowStart = 0;
            cancelWindowEnd = 0;
            bufferedMove = null;
            lastInputTime = -1f;

            SetState(CombatState.Idle);
        }
        
        // Public getters
        public CombatState CurrentState => currentState;
        public MoveDef CurrentMove => currentMove;
        public int CurrentFrame => currentFrame;
        public bool InCancelWindow => inCancelWindow;
        public bool CanAct => currentState == CombatState.Idle || 
                             currentState == CombatState.Recovery ||
                             (inCancelWindow && currentMove != null);
    }
}
