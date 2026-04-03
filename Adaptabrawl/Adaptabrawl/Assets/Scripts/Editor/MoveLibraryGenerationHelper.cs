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
