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
            MoveLibraryGenerationHelper.EnsureFolderExists(savePath);
            EditorUtility.DisplayProgressBar("Generating Move Library", "Creating assets...", 0f);
            try
            {
                MoveLibrary library = MoveLibraryGenerationHelper.GenerateMoveLibrary(
                    selectedWeapon,
                    savePath,
                    generateHitboxes,
                    autoCalculateFrames,
                    setupComboChains,
                    attack1Damage,
                    attack2Damage,
                    attack3Damage,
                    (msg, p) => EditorUtility.DisplayProgressBar("Generating Move Library", msg, p));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
                string libraryPath = $"{savePath}/MoveLibrary_{selectedWeapon}.asset";
                EditorUtility.DisplayDialog(
                    "Success!",
                    $"Move Library generated successfully!\n\n" +
                    $"Created {GetGeneratedMoveCount(library)} moves with hitboxes.\n\n" +
                    $"Location: {libraryPath}\n\n" +
                    "All moves are configured and ready to use in the Inspector!",
                    "OK");
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

