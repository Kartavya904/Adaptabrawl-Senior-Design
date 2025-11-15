using UnityEngine;
using Adaptabrawl.Data;
using System.Collections;

namespace Adaptabrawl.Combat
{
    public class CombatFSM : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private FighterController fighterController;
        [SerializeField] private HitboxManager hitboxManager;
        
        [Header("State")]
        private CombatState currentState = CombatState.Idle;
        private MoveDef currentMove = null;
        private int currentFrame = 0;
        private int totalFrames = 0;
        
        [Header("Input Buffer")]
        private float inputBufferWindow = 0.1f;
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
        public System.Action OnHitboxActive;
        public System.Action OnHitboxInactive;
        
        private void Start()
        {
            if (fighterController == null)
                fighterController = GetComponent<FighterController>();
            if (hitboxManager == null)
                hitboxManager = GetComponent<HitboxManager>();
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
                    hitboxManager?.ActivateHitbox(currentMove);
                    OnHitboxActive?.Invoke();
                }
            }
            else if (currentFrame <= currentMove.totalFrames)
            {
                if (currentState != CombatState.Active)
                {
                    SetState(CombatState.Recovery);
                    hitboxManager?.DeactivateHitbox();
                    OnHitboxInactive?.Invoke();
                }
            }
            else
            {
                // Move complete
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
            
            // Can start if idle or in recovery
            if (currentState == CombatState.Idle || 
                currentState == CombatState.Recovery)
            {
                return true;
            }
            
            return false;
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
            totalFrames = move.totalFrames;
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

