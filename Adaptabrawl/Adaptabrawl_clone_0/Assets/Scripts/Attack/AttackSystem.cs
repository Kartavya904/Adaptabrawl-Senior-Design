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
        
        [Header("Input")]
        private bool lightAttackPressed = false;
        private bool heavyAttackPressed = false;
        
        private void Start()
        {
            fighterController = GetComponent<FighterController>();
            combatFSM = GetComponent<CombatFSM>();
        }
        
        public void OnLightAttackInput(bool pressed)
        {
            lightAttackPressed = pressed;
            if (pressed)
            {
                TryLightAttack();
            }
        }
        
        public void OnHeavyAttackInput(bool pressed)
        {
            heavyAttackPressed = pressed;
            if (pressed)
            {
                TryHeavyAttack();
            }
        }
        
        private void TryLightAttack()
        {
            if (fighterController == null || fighterController.FighterDef == null) return;
            if (combatFSM == null) return;
            
            var lightAttack = fighterController.FighterDef.lightAttack;
            if (lightAttack != null)
            {
                combatFSM.TryStartMove(lightAttack);
            }
        }
        
        private void TryHeavyAttack()
        {
            if (fighterController == null || fighterController.FighterDef == null) return;
            if (combatFSM == null) return;
            
            var heavyAttack = fighterController.FighterDef.heavyAttack;
            if (heavyAttack != null)
            {
                combatFSM.TryStartMove(heavyAttack);
            }
        }
    }
}

