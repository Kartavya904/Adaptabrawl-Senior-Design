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
        
        [Header("SFX")]
        [SerializeField] private AudioClip[] swingClips;
        private AudioSource _audioSource;

        private void Start()
        {
            fighterController = GetComponent<FighterController>();
            combatFSM = GetComponent<CombatFSM>();

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            if (swingClips == null || swingClips.Length == 0)
                swingClips = new AudioClip[] { Resources.Load<AudioClip>("SFX/slash") };
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
    }
}
