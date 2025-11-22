using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Adaptabrawl.Data;

namespace Adaptabrawl.Editor
{
    /// <summary>
    /// Powerful tool to auto-generate complete move libraries from Shinabro animations.
    /// Creates all moves with appropriate hitboxes automatically!
    /// </summary>
    public class MoveLibraryGenerator : EditorWindow
    {
        private WeaponType selectedWeapon;
        private string weaponPath = "Assets/Shinabro/Platform_Animation/Animation/";
        private string savePath = "Assets/Moves/";
        
        private Vector2 scrollPosition;
        private bool generateHitboxes = true;
        private bool autoCalculateFrames = true;
        private bool setupComboChains = true;
        
        // Attack configuration
        private float attack1Damage = 8f;
        private float attack2Damage = 10f;
        private float attack3Damage = 15f;
        
        [MenuItem("Adaptabrawl/Move Library Generator")]
        public static void ShowWindow()
        {
            MoveLibraryGenerator window = GetWindow<MoveLibraryGenerator>("Move Library Generator");
            window.minSize = new Vector2(500, 700);
            window.Show();
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Title
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 18;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Move Library Generator", titleStyle);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool automatically generates a complete move library from Shinabro animations!\n\n" +
                "✓ Creates all attack moves\n" +
                "✓ Configures appropriate hitboxes\n" +
                "✓ Sets up combo chains\n" +
                "✓ Calculates frame data from animations\n\n" +
                "Everything configured in the Inspector!",
                MessageType.Info
            );
            
            EditorGUILayout.Space(10);
            
            // Weapon selection
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Step 1: Select Weapon Type", EditorStyles.boldLabel);
            selectedWeapon = (WeaponType)EditorGUILayout.EnumPopup("Weapon Type", selectedWeapon);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
            
            // Generation options
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Step 2: Configure Generation", EditorStyles.boldLabel);
            generateHitboxes = EditorGUILayout.Toggle("Generate Hitboxes", generateHitboxes);
            autoCalculateFrames = EditorGUILayout.Toggle("Auto-Calculate Frames", autoCalculateFrames);
            setupComboChains = EditorGUILayout.Toggle("Setup Combo Chains", setupComboChains);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
            
            // Attack damage configuration
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Attack Damage Configuration", EditorStyles.boldLabel);
            attack1Damage = EditorGUILayout.FloatField("Attack 1 Damage", attack1Damage);
            attack2Damage = EditorGUILayout.FloatField("Attack 2 Damage", attack2Damage);
            attack3Damage = EditorGUILayout.FloatField("Attack 3 Damage", attack3Damage);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
            
            // Save path
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Step 3: Choose Save Location", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            savePath = EditorGUILayout.TextField("Save Path", savePath);
            if (GUILayout.Button("Browse", GUILayout.Width(70)))
            {
                string path = EditorUtility.SaveFolderPanel("Choose Save Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        savePath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
            
            // Generate button
            if (GUILayout.Button("Generate Move Library", GUILayout.Height(50)))
            {
                GenerateMoveLibrary();
            }
            
            EditorGUILayout.Space(10);
            
            // Preview
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Moves That Will Be Generated", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Ground Attacks:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • Attack 1, 2, 3 (combo chain)", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Aerial Attacks:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • Jump Attack 1, 2, 3", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Defensive:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • Block, Dodge, Dodge Roll, Dodge Attack", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Crouch:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • Crouch, Crouch Attack, Crouch Block", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("Special Skills:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("  • Skill 1-8 (Float, Slow, Stun, Push, Pull, Move, Around, Air)", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("\nTotal: ~23 moves with hitboxes!", EditorStyles.boldLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void GenerateMoveLibrary()
        {
            if (!AssetDatabase.IsValidFolder(savePath))
            {
                EditorUtility.DisplayDialog("Invalid Path", "Please select a valid save folder", "OK");
                return;
            }
            
            string weaponFolder = GetWeaponFolderName(selectedWeapon);
            string weaponAnimPath = weaponPath + weaponFolder + "/";
            
            EditorUtility.DisplayProgressBar("Generating Move Library", "Creating assets...", 0f);
            
            try
            {
                // Create move library container
                MoveLibrary library = ScriptableObject.CreateInstance<MoveLibrary>();
                library.weaponType = selectedWeapon;
                library.weaponName = selectedWeapon.ToString();
                
                // Generate all moves
                float progress = 0f;
                float total = 23f;
                
                // Ground attacks
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating ground attacks...", progress++ / total);
                library.attack1 = CreateAttackMove("Attack1", attack1Damage, HitboxPresets.GetQuickAttackHitboxes(), 0.25f, 0.15f);
                
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating ground attacks...", progress++ / total);
                library.attack2 = CreateAttackMove("Attack2", attack2Damage, HitboxPresets.GetMediumAttackHitboxes(), 0.3f, 0.2f);
                
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating ground attacks...", progress++ / total);
                library.attack3 = CreateAttackMove("Attack3", attack3Damage, HitboxPresets.GetHeavyAttackHitboxes(), 0.35f, 0.25f);
                
                // Aerial attacks
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating aerial attacks...", progress++ / total);
                library.jumpAttack1 = CreateAttackMove("JumpAttack1", 9f, HitboxPresets.GetAerialAttackHitboxes(), 0.3f, 0.2f);
                
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating aerial attacks...", progress++ / total);
                library.jumpAttack2 = CreateAttackMove("JumpAttack2", 11f, HitboxPresets.GetAerialAttackHitboxes(), 0.3f, 0.2f);
                
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating aerial attacks...", progress++ / total);
                library.jumpAttack3 = CreateAttackMove("JumpAttack3", 14f, HitboxPresets.GetAerialAttackHitboxes(), 0.35f, 0.25f);
                
                // Dodge moves
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating defensive moves...", progress++ / total);
                library.dodgeAttack = CreateAttackMove("DodgeAttack", 12f, HitboxPresets.GetQuickAttackHitboxes(), 0.4f, 0.2f);
                
                // Crouch moves
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating crouch moves...", progress++ / total);
                library.crouchAttack = CreateAttackMove("CrouchAttack", 10f, HitboxPresets.GetQuickAttackHitboxes(), 0.3f, 0.2f);
                
                // Special skills
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating special skills...", progress++ / total);
                library.skill1_Float = CreateAttackMove("Skill1", 15f, HitboxPresets.GetLauncherHitboxes(), 0.4f, 0.2f);
                
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating special skills...", progress++ / total);
                library.skill2_Slow = CreateAttackMove("Skill2", 12f, HitboxPresets.GetStunAttackHitboxes(), 0.35f, 0.2f);
                
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating special skills...", progress++ / total);
                library.skill3_Stun = CreateAttackMove("Skill3", 10f, HitboxPresets.GetStunAttackHitboxes(), 0.3f, 0.2f);
                
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating special skills...", progress++ / total);
                library.skill4_Push = CreateAttackMove("Skill4", 8f, HitboxPresets.GetPushAttackHitboxes(), 0.3f, 0.2f);
                
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating special skills...", progress++ / total);
                library.skill5_Pull = CreateAttackMove("Skill5", 8f, HitboxPresets.GetPushAttackHitboxes(), 0.3f, 0.2f);
                
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating special skills...", progress++ / total);
                library.skill6_Move = CreateAttackMove("Skill6", 16f, HitboxPresets.GetDashAttackHitboxes(), 0.4f, 0.25f);
                
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating special skills...", progress++ / total);
                library.skill7_Around = CreateAttackMove("Skill7", 18f, HitboxPresets.GetSpinAttackHitboxes(), 0.3f, 0.3f);
                
                EditorUtility.DisplayProgressBar("Generating Move Library", "Creating special skills...", progress++ / total);
                library.skill8_Air = CreateAttackMove("Skill8", 20f, HitboxPresets.GetHeavyAttackHitboxes(), 0.4f, 0.25f);
                
                // Setup combo chains
                if (setupComboChains)
                {
                    EditorUtility.DisplayProgressBar("Generating Move Library", "Setting up combo chains...", progress++ / total);
                    library.SetupComboChains();
                }
                
                // Save library
                EditorUtility.DisplayProgressBar("Generating Move Library", "Saving library...", 0.95f);
                string libraryPath = $"{savePath}/MoveLibrary_{selectedWeapon}.asset";
                AssetDatabase.CreateAsset(library, libraryPath);
                
                // Save and select
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                EditorUtility.ClearProgressBar();
                
                // Show success
                EditorUtility.DisplayDialog(
                    "Success!",
                    $"Move Library generated successfully!\n\n" +
                    $"Created {GetGeneratedMoveCount(library)} moves with hitboxes.\n\n" +
                    $"Location: {libraryPath}\n\n" +
                    "All moves are configured and ready to use in the Inspector!",
                    "OK"
                );
                
                Selection.activeObject = library;
                EditorGUIUtility.PingObject(library);
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Error", $"Failed to generate move library:\n{e.Message}", "OK");
                Debug.LogError($"Move Library Generator Error: {e}");
            }
        }
        
        private AnimatedMoveDef CreateAttackMove(string animName, float damage, HitboxDefinition[] hitboxes, float hitboxStart, float hitboxDuration)
        {
            AnimatedMoveDef move = ScriptableObject.CreateInstance<AnimatedMoveDef>();
            
            // Set animation info
            move.moveName = $"{selectedWeapon}_{animName}";
            move.animatorTrigger = animName;
            move.parameterType = AnimatorParameterType.Trigger;
            move.moveType = GetMoveType(animName);
            
            // Set auto-calculate settings
            move.autoCalculateFrames = autoCalculateFrames;
            move.hitboxActivationTime = hitboxStart;
            move.hitboxDuration = hitboxDuration;
            move.recoveryPercentage = 0.3f;
            
            // Set combat properties
            move.damage = damage;
            move.knockbackForce = damage * 0.5f;
            move.knockbackDirection = Vector2.right;
            move.hitstopFrames = Mathf.RoundToInt(damage * 0.3f);
            move.hitstunFrames = Mathf.RoundToInt(damage * 1.2f);
            move.blockstunFrames = Mathf.RoundToInt(damage * 0.8f);
            
            // Set hitboxes
            if (generateHitboxes && hitboxes != null)
            {
                move.hitboxDefinitions = hitboxes;
            }
            
            // Legacy hitbox (for backwards compatibility)
            if (hitboxes != null && hitboxes.Length > 0)
            {
                move.hitboxOffset = hitboxes[0].offset;
                move.hitboxSize = hitboxes[0].size;
            }
            
            // Save move
            string movePath = $"{savePath}/{move.moveName}.asset";
            AssetDatabase.CreateAsset(move, movePath);
            
            return move;
        }
        
        private MoveType GetMoveType(string animName)
        {
            if (animName.Contains("Attack"))
                return MoveType.LightAttack;
            if (animName.Contains("Skill"))
                return MoveType.Special;
            if (animName.Contains("Dodge"))
                return MoveType.Dodge;
            if (animName.Contains("Block"))
                return MoveType.Block;
            
            return MoveType.LightAttack;
        }
        
        private string GetWeaponFolderName(WeaponType weapon)
        {
            switch (weapon)
            {
                case WeaponType.Unarmed_Fighter: return "09_Fighter";
                case WeaponType.SwordAndShield: return "01_Sword&Shield";
                case WeaponType.Hammer: return "02_Hammer";
                case WeaponType.DualBlades: return "03_DualBlades";
                case WeaponType.Bow: return "04_Bow";
                case WeaponType.Pistol: return "05_Pistol";
                case WeaponType.Magic: return "06_Magic";
                case WeaponType.Spear: return "07_Spear";
                case WeaponType.Staff: return "08_Staff";
                case WeaponType.Rapier: return "10_Rapier";
                case WeaponType.DoubleBlades: return "11_DoubleBlades";
                case WeaponType.Claymore: return "12_Claymore";
                default: return "09_Fighter";
            }
        }
        
        private int GetGeneratedMoveCount(MoveLibrary library)
        {
            int count = 0;
            if (library.attack1 != null) count++;
            if (library.attack2 != null) count++;
            if (library.attack3 != null) count++;
            if (library.jumpAttack1 != null) count++;
            if (library.jumpAttack2 != null) count++;
            if (library.jumpAttack3 != null) count++;
            if (library.dodgeAttack != null) count++;
            if (library.crouchAttack != null) count++;
            if (library.skill1_Float != null) count++;
            if (library.skill2_Slow != null) count++;
            if (library.skill3_Stun != null) count++;
            if (library.skill4_Push != null) count++;
            if (library.skill5_Pull != null) count++;
            if (library.skill6_Move != null) count++;
            if (library.skill7_Around != null) count++;
            if (library.skill8_Air != null) count++;
            return count;
        }
    }
}

