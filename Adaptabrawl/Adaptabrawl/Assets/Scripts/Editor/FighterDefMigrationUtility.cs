using UnityEngine;
using UnityEditor;
using Adaptabrawl.Data;

namespace Adaptabrawl.Editor
{
    /// <summary>
    /// One-click migration to populate new FighterDef fields (moveLibrary, jump/aerial/dodge/crouch attacks)
    /// for all existing FighterDef assets created before the extension.
    /// </summary>
    public static class FighterDefMigrationUtility
    {
        [MenuItem("Adaptabrawl/Migrate/FighterDefs - Populate Extended Moves")]
        public static void PopulateExtendedMovesOnAllFighters()
        {
            string[] guids = AssetDatabase.FindAssets("t:FighterDef");
            int count = guids.Length;
            try
            {
                for (int i = 0; i < count; i++)
                {
                    string guid = guids[i];
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var fighter = AssetDatabase.LoadAssetAtPath<FighterDef>(path);
                    if (fighter == null)
                        continue;

                    EditorUtility.DisplayProgressBar(
                        "Migrating FighterDefs",
                        $"Updating {fighter.fighterName} ({i + 1}/{count})",
                        (float)i / Mathf.Max(1, count));

                    bool changed = PopulateExtendedMovesForFighter(fighter);
                    if (changed)
                    {
                        EditorUtility.SetDirty(fighter);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static bool PopulateExtendedMovesForFighter(FighterDef fighter)
        {
            bool changed = false;

            // Try to resolve or confirm the MoveLibrary for this fighter.
            if (fighter.moveLibrary == null && fighter.fighterPrefab != null)
            {
                var weaponType = MoveLibraryGenerationHelper.GetWeaponTypeFromPrefabName(fighter.fighterPrefab.name);
                if (weaponType.HasValue)
                {
                    string libraryPath =
                        $"{MoveLibraryGenerationHelper.DefaultFightersPath}/{MoveLibraryGenerationHelper.DefaultMovesSubpath}/{weaponType.Value}/MoveLibrary_{weaponType.Value}.asset";
                    var library = AssetDatabase.LoadAssetAtPath<MoveLibrary>(libraryPath);
                    if (library != null)
                    {
                        fighter.moveLibrary = library;
                        changed = true;
                    }
                }
            }

            var lib = fighter.moveLibrary;
            if (lib == null)
            {
                // No library available; nothing more we can infer automatically.
                return changed;
            }

            // Populate jump/air and utility moves if they are still unassigned.
            if (fighter.jumpAttackPrimary == null && lib.jumpAttack1 != null)
            {
                fighter.jumpAttackPrimary = lib.jumpAttack1;
                changed = true;
            }

            if (fighter.aerialSpecial == null && lib.skill8_Air != null)
            {
                fighter.aerialSpecial = lib.skill8_Air;
                changed = true;
            }

            if (fighter.dodgeAttack == null && lib.dodgeAttack != null)
            {
                fighter.dodgeAttack = lib.dodgeAttack;
                changed = true;
            }

            if (fighter.crouchAttack == null && lib.crouchAttack != null)
            {
                fighter.crouchAttack = lib.crouchAttack;
                changed = true;
            }

            return changed;
        }
    }
}

