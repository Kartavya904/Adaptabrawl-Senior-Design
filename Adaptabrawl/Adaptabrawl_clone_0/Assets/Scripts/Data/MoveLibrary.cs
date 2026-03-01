using UnityEngine;

namespace Adaptabrawl.Data
{
    /// <summary>
    /// Container for all moves of a specific weapon type (e.g., Fighter, Hammer, Sword&Shield).
    /// Pre-configured with Shinabro animations and hitboxes.
    /// </summary>
    [CreateAssetMenu(fileName = "New Move Library", menuName = "Adaptabrawl/Move Library")]
    public class MoveLibrary : ScriptableObject
    {
        [Header("Weapon Type")]
        public WeaponType weaponType;
        public string weaponName;
        
        [Header("Basic Attacks")]
        [Tooltip("First ground attack in combo chain")]
        public AnimatedMoveDef attack1;
        
        [Tooltip("Second ground attack in combo chain")]
        public AnimatedMoveDef attack2;
        
        [Tooltip("Third ground attack in combo chain (finisher)")]
        public AnimatedMoveDef attack3;
        
        [Header("Aerial Attacks")]
        public AnimatedMoveDef jumpAttack1;
        public AnimatedMoveDef jumpAttack2;
        public AnimatedMoveDef jumpAttack3;
        
        [Header("Defensive Moves")]
        public AnimatedMoveDef block;
        public AnimatedMoveDef dodge;
        public AnimatedMoveDef dodgeRoll;
        public AnimatedMoveDef dodgeAttack;
        
        [Header("Crouch Moves")]
        public AnimatedMoveDef crouch;
        public AnimatedMoveDef crouchAttack;
        public AnimatedMoveDef crouchBlock;
        
        [Header("Special Attacks (Skills)")]
        [Tooltip("Skill 1: Float - Launches enemy upward")]
        public AnimatedMoveDef skill1_Float;
        
        [Tooltip("Skill 2: Slow - Slows enemy movement")]
        public AnimatedMoveDef skill2_Slow;
        
        [Tooltip("Skill 3: Stun - Stuns enemy")]
        public AnimatedMoveDef skill3_Stun;
        
        [Tooltip("Skill 4: Push - Pushes enemy away")]
        public AnimatedMoveDef skill4_Push;
        
        [Tooltip("Skill 5: Pull - Pulls enemy toward you")]
        public AnimatedMoveDef skill5_Pull;
        
        [Tooltip("Skill 6: Move - Dash forward with attack")]
        public AnimatedMoveDef skill6_Move;
        
        [Tooltip("Skill 7: Around - Spinning/AOE attack")]
        public AnimatedMoveDef skill7_Around;
        
        [Tooltip("Skill 8: Air - Aerial special attack")]
        public AnimatedMoveDef skill8_Air;
        
        [Header("Utility")]
        public AnimatedMoveDef weaponSheath;
        public AnimatedMoveDef weaponUnsheath;
        
        /// <summary>
        /// Gets all ground attacks
        /// </summary>
        public AnimatedMoveDef[] GetGroundAttacks()
        {
            return new AnimatedMoveDef[] { attack1, attack2, attack3 };
        }
        
        /// <summary>
        /// Gets all aerial attacks
        /// </summary>
        public AnimatedMoveDef[] GetAerialAttacks()
        {
            return new AnimatedMoveDef[] { jumpAttack1, jumpAttack2, jumpAttack3 };
        }
        
        /// <summary>
        /// Gets all special attacks
        /// </summary>
        public AnimatedMoveDef[] GetSpecialAttacks()
        {
            return new AnimatedMoveDef[] { 
                skill1_Float, skill2_Slow, skill3_Stun, skill4_Push,
                skill5_Pull, skill6_Move, skill7_Around, skill8_Air
            };
        }
        
        /// <summary>
        /// Gets all defensive moves
        /// </summary>
        public AnimatedMoveDef[] GetDefensiveMoves()
        {
            return new AnimatedMoveDef[] { block, dodge, dodgeRoll };
        }
        
        /// <summary>
        /// Sets up combo chains for ground attacks
        /// </summary>
        public void SetupComboChains()
        {
            if (attack1 != null)
            {
                attack1.canCombo = true;
                attack1.nextComboMove = attack2;
            }
            
            if (attack2 != null)
            {
                attack2.canCombo = true;
                attack2.nextComboMove = attack3;
            }
            
            if (attack3 != null)
            {
                attack3.canCombo = false;
            }
        }
    }
    
    public enum WeaponType
    {
        Unarmed_Fighter,
        SwordAndShield,
        Hammer,
        DualBlades,
        Bow,
        Pistol,
        Magic,
        Spear,
        Staff,
        Rapier,
        DoubleBlades,
        Claymore
    }
}

