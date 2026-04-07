using System.Collections;

using UnityEngine;
using Adaptabrawl.Data;
using Adaptabrawl.Combat;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Attack
{
    public class AttackSystem : MonoBehaviour
    {
        [Header("References")]
        private FighterController fighterController;
        private CombatFSM combatFSM;
        private Animator combatAnimator;
        private bool specialMoveLocked;
        private MoveDef activeSpecialMove;
        private string activeSpecialAnimatorTrigger;
        private Coroutine specialUnlockCoroutine;
        
        [Header("SFX")]
        [SerializeField] private AudioClip[] swingClips;
        private AudioSource _audioSource;

        private void Start()
        {
            fighterController = GetComponent<FighterController>();
            combatFSM = GetComponent<CombatFSM>();
            ResolveCombatAnimator();

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            if (swingClips == null || swingClips.Length == 0)
                swingClips = new AudioClip[] { Resources.Load<AudioClip>("SFX/slash") };
        }

        private void OnEnable()
        {
            SubscribeToCombat();
        }

        private void OnDisable()
        {
            UnsubscribeFromCombat();
            StopSpecialUnlockRoutine();
            specialMoveLocked = false;
            activeSpecialMove = null;
            activeSpecialAnimatorTrigger = null;
        }
        
        public void OnLightAttackInput(bool pressed)
        {
            if (pressed)
                TryLightAttack();
        }
        
        public void OnHeavyAttackInput(bool pressed)
        {
            if (pressed)
                TryHeavyAttack();
        }

        public bool TrySpecialAttack(int index)
        {
            if (!TryGetFighter(out FighterDef fighter))
                return false;

            if (specialMoveLocked || IsSpecialAnimationPlaying(activeSpecialAnimatorTrigger))
                return false;

            MoveDef[] specials = fighter.specialMoves;
            if ((specials == null || specials.Length == 0) && fighter.moveLibrary != null)
                specials = fighter.moveLibrary.GetSpecialAttacks();

            if (specials == null || index < 0 || index >= specials.Length || specials[index] == null)
                return false;

            bool success = combatFSM.TryStartMove(specials[index]);
            if (success) PlaySwingSound();
            return success;
        }
        
        private void TryLightAttack()
        {
            if (!TryGetFighter(out FighterDef fighter))
                return;

            MoveDef lightAttack = ResolveComboMove(fighter);
            if (lightAttack == null)
                lightAttack = fighter.lightAttack ?? fighter.moveLibrary?.attack1;

            if (lightAttack != null)
            {
                if (combatFSM.TryStartMove(lightAttack))
                    PlaySwingSound();
            }
        }
        
        private void TryHeavyAttack()
        {
            if (!TryGetFighter(out FighterDef fighter))
                return;

            MoveDef heavyAttack = fighter.heavyAttack ?? fighter.moveLibrary?.attack3;
            if (heavyAttack != null)
            {
                if (combatFSM.TryStartMove(heavyAttack))
                    PlaySwingSound();
            }
        }

        private void PlaySwingSound()
        {
            if (_audioSource != null && swingClips != null && swingClips.Length > 0)
                _audioSource.PlayOneShot(swingClips[Random.Range(0, swingClips.Length)], 0.75f);
        }

        private MoveDef ResolveComboMove(FighterDef fighter)
        {
            AnimatedMoveDef currentAnimatedMove = combatFSM.CurrentMove as AnimatedMoveDef;
            if (currentAnimatedMove != null && currentAnimatedMove.canCombo && currentAnimatedMove.nextComboMove != null)
                return currentAnimatedMove.nextComboMove;

            if (combatFSM.CurrentMove == fighter.moveLibrary?.attack1 && fighter.moveLibrary.attack2 != null)
                return fighter.moveLibrary.attack2;

            if (combatFSM.CurrentMove == fighter.moveLibrary?.attack2 && fighter.moveLibrary.attack3 != null)
                return fighter.moveLibrary.attack3;

            return null;
        }

        private bool TryGetFighter(out FighterDef fighter)
        {
            fighter = fighterController != null ? fighterController.FighterDef : null;
            return fighterController != null && fighter != null && combatFSM != null;
        }

        private void HandleMoveStarted(MoveDef move)
        {
            if (move == null || move.moveType != MoveType.Special)
                return;

            StopSpecialUnlockRoutine();
            specialMoveLocked = true;
            activeSpecialMove = move;
            activeSpecialAnimatorTrigger = ResolveAnimatorTrigger(move);
        }

        private void HandleMoveEnded(MoveDef move)
        {
            if (move == null || move != activeSpecialMove)
                return;

            StopSpecialUnlockRoutine();
            specialUnlockCoroutine = StartCoroutine(
                ReleaseSpecialLockAfterAnimation(
                    move,
                    activeSpecialAnimatorTrigger,
                    ResolveSpecialLockDuration(move)));
        }

        private void SubscribeToCombat()
        {
            if (combatFSM == null)
                combatFSM = GetComponent<CombatFSM>();

            if (combatFSM == null)
                return;

            combatFSM.OnMoveStarted -= HandleMoveStarted;
            combatFSM.OnMoveEnded -= HandleMoveEnded;
            combatFSM.OnMoveStarted += HandleMoveStarted;
            combatFSM.OnMoveEnded += HandleMoveEnded;
        }

        private void UnsubscribeFromCombat()
        {
            if (combatFSM == null)
                return;

            combatFSM.OnMoveStarted -= HandleMoveStarted;
            combatFSM.OnMoveEnded -= HandleMoveEnded;
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

            combatAnimator = GetComponentInChildren<Animator>();
        }

        private IEnumerator ReleaseSpecialLockAfterAnimation(
            MoveDef move,
            string expectedStateName,
            float fallbackDurationSeconds)
        {
            float fallbackUnlockAt = Time.time + Mathf.Max(0f, fallbackDurationSeconds);
            bool sawExpectedSpecialState = false;

            while (true)
            {
                bool animationPlaying = IsSpecialAnimationPlaying(expectedStateName);
                if (animationPlaying)
                {
                    sawExpectedSpecialState = true;
                }
                else if (Time.time >= fallbackUnlockAt)
                {
                    break;
                }
                else if (sawExpectedSpecialState)
                {
                    break;
                }

                yield return null;
            }

            specialUnlockCoroutine = null;
            specialMoveLocked = false;
            activeSpecialMove = null;
            activeSpecialAnimatorTrigger = null;
        }

        private void StopSpecialUnlockRoutine()
        {
            if (specialUnlockCoroutine == null)
                return;

            StopCoroutine(specialUnlockCoroutine);
            specialUnlockCoroutine = null;
        }

        private static string ResolveAnimatorTrigger(MoveDef move)
        {
            AnimatedMoveDef animatedMove = move as AnimatedMoveDef;
            return animatedMove != null ? animatedMove.animatorTrigger : null;
        }

        private static float ResolveSpecialLockDuration(MoveDef move)
        {
            float frameDuration = move != null
                ? (move.startupFrames + move.activeFrames + move.recoveryFrames) / 60f
                : 0f;

            return Mathf.Max(frameDuration + 0.2f, 0.55f);
        }

        private bool IsSpecialAnimationPlaying(string expectedStateName)
        {
            ResolveCombatAnimator();
            if (combatAnimator == null || !combatAnimator.isActiveAndEnabled)
                return false;

            AnimatorStateInfo stateInfo = combatAnimator.GetCurrentAnimatorStateInfo(0);
            if (IsSpecialState(stateInfo, expectedStateName))
                return true;

            if (!combatAnimator.IsInTransition(0))
                return false;

            AnimatorStateInfo nextStateInfo = combatAnimator.GetNextAnimatorStateInfo(0);
            return IsSpecialState(nextStateInfo, expectedStateName);
        }

        private static bool IsSpecialState(AnimatorStateInfo stateInfo, string expectedStateName)
        {
            if (CombatFSM.MatchesShinabroAnimatorState(stateInfo, expectedStateName))
                return true;

            return CombatFSM.MatchesShinabroAnimatorState(stateInfo, "Skill1")
                || CombatFSM.MatchesShinabroAnimatorState(stateInfo, "Skill2")
                || CombatFSM.MatchesShinabroAnimatorState(stateInfo, "Skill3")
                || CombatFSM.MatchesShinabroAnimatorState(stateInfo, "Skill4")
                || CombatFSM.MatchesShinabroAnimatorState(stateInfo, "Skill5")
                || CombatFSM.MatchesShinabroAnimatorState(stateInfo, "Skill6")
                || CombatFSM.MatchesShinabroAnimatorState(stateInfo, "Skill7")
                || CombatFSM.MatchesShinabroAnimatorState(stateInfo, "Skill8");
        }
    }
}
