using UnityEngine;
using UnityEditor;
using Adaptabrawl.Data;

namespace Adaptabrawl.Editor
{
    /// <summary>
    /// Shared logic for generating move libraries from Shinabro weapon types.
    /// Used by Fighter Setup Wizard and Move Library Generator.
    /// </summary>
    public static class MoveLibraryGenerationHelper
    {
        public const string DefaultFightersPath = "Assets/Prefabs/Fighters";
        public const string DefaultMovesSubpath = "Moves";

        /// <summary>
        /// Maps Shinabro prefab name (e.g. Player_Hammer) to WeaponType.
        /// Returns null if prefab is not a known Player_ prefab.
        /// </summary>
        public static WeaponType? GetWeaponTypeFromPrefabName(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return null;
            switch (prefabName)
            {
                case "Player_Fighter": return WeaponType.Unarmed_Fighter;
                case "Player_Sword&Shield": return WeaponType.SwordAndShield;
                case "Player_Hammer": return WeaponType.Hammer;
                case "Player_DualBlades": return WeaponType.DualBlades;
                case "Player_Bow": return WeaponType.Bow;
                case "Player_Pistol": return WeaponType.Pistol;
                case "Player_Magic": return WeaponType.Magic;
                case "Player_Spear": return WeaponType.Spear;
                case "Player_Staff": return WeaponType.Staff;
                case "Player_Rapier": return WeaponType.Rapier;
                case "Player_DoubleBlades": return WeaponType.DoubleBlades;
                case "Player_Claymore": return WeaponType.Claymore;
                default: return null;
            }
        }

        /// <summary>
        /// Ensures a folder path exists under Assets, creating each segment if needed.
        /// </summary>
        public static void EnsureFolderExists(string pathUnderAssets)
        {
            if (string.IsNullOrEmpty(pathUnderAssets) || !pathUnderAssets.StartsWith("Assets/"))
                return;
            string[] parts = pathUnderAssets.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        /// <summary>
        /// Generates a full move library for the given weapon type and saves all assets to savePath.
        /// Returns the created MoveLibrary. Portrait is not set; caller assigns to FighterDef.
        /// </summary>
        public static MoveLibrary GenerateMoveLibrary(
            WeaponType weapon,
            string savePath,
            bool generateHitboxes = true,
            bool autoCalculateFrames = true,
            bool setupComboChains = true,
            float attack1Damage = 8f,
            float attack2Damage = 10f,
            float attack3Damage = 15f,
            System.Action<string, float> onProgress = null)
        {
            EnsureFolderExists(savePath);

            MoveLibrary library = ScriptableObject.CreateInstance<MoveLibrary>();
            library.weaponType = weapon;
            library.weaponName = weapon.ToString();

            float total = 17f;
            float progress = 0f;
            void Report(string msg) => onProgress?.Invoke(msg, progress++ / total);

            Report("Ground attacks...");
            library.attack1 = CreateAndSaveMove(weapon, savePath, "Attack1", attack1Damage, HitboxPresets.GetQuickAttackHitboxes(), 0.25f, 0.15f, generateHitboxes, autoCalculateFrames);
            library.attack2 = CreateAndSaveMove(weapon, savePath, "Attack2", attack2Damage, HitboxPresets.GetMediumAttackHitboxes(), 0.3f, 0.2f, generateHitboxes, autoCalculateFrames);
            library.attack3 = CreateAndSaveMove(weapon, savePath, "Attack3", attack3Damage, HitboxPresets.GetHeavyAttackHitboxes(), 0.35f, 0.25f, generateHitboxes, autoCalculateFrames);

            Report("Aerial attacks...");
            library.jumpAttack1 = CreateAndSaveMove(weapon, savePath, "JumpAttack1", 9f, HitboxPresets.GetAerialAttackHitboxes(), 0.3f, 0.2f, generateHitboxes, autoCalculateFrames);
            library.jumpAttack2 = CreateAndSaveMove(weapon, savePath, "JumpAttack2", 11f, HitboxPresets.GetAerialAttackHitboxes(), 0.3f, 0.2f, generateHitboxes, autoCalculateFrames);
            library.jumpAttack3 = CreateAndSaveMove(weapon, savePath, "JumpAttack3", 14f, HitboxPresets.GetAerialAttackHitboxes(), 0.35f, 0.25f, generateHitboxes, autoCalculateFrames);

            Report("Defensive / crouch...");
            library.dodgeAttack = CreateAndSaveMove(weapon, savePath, "DodgeAttack", 12f, HitboxPresets.GetQuickAttackHitboxes(), 0.4f, 0.2f, generateHitboxes, autoCalculateFrames);
            library.crouchAttack = CreateAndSaveMove(weapon, savePath, "CrouchAttack", 10f, HitboxPresets.GetQuickAttackHitboxes(), 0.3f, 0.2f, generateHitboxes, autoCalculateFrames);

            Report("Special skills...");
            library.skill1_Float = CreateAndSaveMove(weapon, savePath, "Skill1", 15f, HitboxPresets.GetLauncherHitboxes(), 0.4f, 0.2f, generateHitboxes, autoCalculateFrames);
            library.skill2_Slow = CreateAndSaveMove(weapon, savePath, "Skill2", 12f, HitboxPresets.GetStunAttackHitboxes(), 0.35f, 0.2f, generateHitboxes, autoCalculateFrames);
            library.skill3_Stun = CreateAndSaveMove(weapon, savePath, "Skill3", 10f, HitboxPresets.GetStunAttackHitboxes(), 0.3f, 0.2f, generateHitboxes, autoCalculateFrames);
            library.skill4_Push = CreateAndSaveMove(weapon, savePath, "Skill4", 8f, HitboxPresets.GetPushAttackHitboxes(), 0.3f, 0.2f, generateHitboxes, autoCalculateFrames);
            library.skill5_Pull = CreateAndSaveMove(weapon, savePath, "Skill5", 8f, HitboxPresets.GetPushAttackHitboxes(), 0.3f, 0.2f, generateHitboxes, autoCalculateFrames);
            library.skill6_Move = CreateAndSaveMove(weapon, savePath, "Skill6", 16f, HitboxPresets.GetDashAttackHitboxes(), 0.4f, 0.25f, generateHitboxes, autoCalculateFrames);
            library.skill7_Around = CreateAndSaveMove(weapon, savePath, "Skill7", 18f, HitboxPresets.GetSpinAttackHitboxes(), 0.3f, 0.3f, generateHitboxes, autoCalculateFrames);
            library.skill8_Air = CreateAndSaveMove(weapon, savePath, "Skill8", 20f, HitboxPresets.GetHeavyAttackHitboxes(), 0.4f, 0.25f, generateHitboxes, autoCalculateFrames);

            if (setupComboChains)
                library.SetupComboChains();

            string libraryPath = $"{savePath}/MoveLibrary_{weapon}.asset";
            AssetDatabase.CreateAsset(library, libraryPath);
            return library;
        }

        private static AnimatedMoveDef CreateAndSaveMove(
            WeaponType weapon,
            string savePath,
            string animName,
            float damage,
            HitboxDefinition[] hitboxes,
            float hitboxStart,
            float hitboxDuration,
            bool generateHitboxes,
            bool autoCalculateFrames)
        {
            AnimatedMoveDef move = ScriptableObject.CreateInstance<AnimatedMoveDef>();
            move.moveName = $"{weapon}_{animName}";
            move.animatorTrigger = animName;
            move.parameterType = AnimatorParameterType.Trigger;
            move.moveType = GetMoveType(animName);
            move.autoCalculateFrames = autoCalculateFrames;
            move.hitboxActivationTime = hitboxStart;
            move.hitboxDuration = hitboxDuration;
            move.recoveryPercentage = 0.3f;
            move.damage = damage;
            move.knockbackForce = damage * 0.5f;
            move.knockbackDirection = Vector2.right;
            move.hitstopFrames = Mathf.RoundToInt(damage * 0.3f);
            move.hitstunFrames = Mathf.RoundToInt(damage * 1.2f);
            move.blockstunFrames = Mathf.RoundToInt(damage * 0.8f);
            if (generateHitboxes && hitboxes != null)
                move.hitboxDefinitions = hitboxes;
            if (hitboxes != null && hitboxes.Length > 0)
            {
                move.hitboxOffset = hitboxes[0].offset;
                move.hitboxSize = hitboxes[0].size;
            }
            string movePath = $"{savePath}/{move.moveName}.asset";
            AssetDatabase.CreateAsset(move, movePath);
            return move;
        }

        private static MoveType GetMoveType(string animName)
        {
            if (animName.Contains("Attack")) return MoveType.LightAttack;
            if (animName.Contains("Skill")) return MoveType.Special;
            if (animName.Contains("Dodge")) return MoveType.Dodge;
            if (animName.Contains("Block")) return MoveType.Block;
            return MoveType.LightAttack;
        }
    }
}
