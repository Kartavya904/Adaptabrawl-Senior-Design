using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Adaptabrawl.Data;

namespace Adaptabrawl.Editor
{
    public class FighterPrefabSetupWindow : EditorWindow
    {
        private GameObject selectedPrefab;
        private string fighterName = "";
        private string fighterDescription = "";
        private FighterPreset selectedPreset = FighterPreset.Balanced;
        
        private Vector2 scrollPosition;
        
        private enum FighterPreset
        {
            Fast,
            Balanced,
            Tank,
            Custom
        }
        
        [MenuItem("Adaptabrawl/Fighter Setup Wizard")]
        public static void ShowWindow()
        {
            FighterPrefabSetupWindow window = GetWindow<FighterPrefabSetupWindow>("Fighter Setup Wizard");
            window.minSize = new Vector2(400, 500);
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
            EditorGUILayout.LabelField("Fighter Setup Wizard", titleStyle);
            EditorGUILayout.Space(10);
            
            EditorGUILayout.HelpBox("Quickly create a new fighter using Shinabro prefabs!", MessageType.Info);
            EditorGUILayout.Space(10);
            
            // Step 1: Select Prefab
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Step 1: Select Shinabro Prefab", EditorStyles.boldLabel);
            
            selectedPrefab = (GameObject)EditorGUILayout.ObjectField("Fighter Prefab", selectedPrefab, typeof(GameObject), false);
            
            if (selectedPrefab == null)
            {
                EditorGUILayout.HelpBox("Select a prefab from the Shinabro folder.", MessageType.Warning);
                
                if (GUILayout.Button("Browse Shinabro Prefabs"))
                {
                    ShowPrefabPicker();
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Selected: {selectedPrefab.name}", MessageType.None);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
            
            // Step 2: Basic Info
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Step 2: Basic Information", EditorStyles.boldLabel);
            
            fighterName = EditorGUILayout.TextField("Fighter Name", fighterName);
            
            if (string.IsNullOrEmpty(fighterName) && selectedPrefab != null)
            {
                if (GUILayout.Button("Auto-Generate Name"))
                {
                    fighterName = selectedPrefab.name.Replace("Player_", "");
                }
            }
            
            fighterDescription = EditorGUILayout.TextField("Description", fighterDescription, GUILayout.Height(60));
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
            
            // Step 3: Stats Preset
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Step 3: Choose Stats Preset", EditorStyles.boldLabel);
            
            selectedPreset = (FighterPreset)EditorGUILayout.EnumPopup("Preset", selectedPreset);
            
            EditorGUILayout.Space(5);
            
            switch (selectedPreset)
            {
                case FighterPreset.Fast:
                    EditorGUILayout.HelpBox("Fast Fighter:\n• Lower health (90)\n• High speed (7.5)\n• Low weight (0.8)\n• Good for hit-and-run tactics", MessageType.None);
                    break;
                case FighterPreset.Balanced:
                    EditorGUILayout.HelpBox("Balanced Fighter:\n• Medium health (100)\n• Medium speed (5)\n• Medium weight (1.0)\n• All-around performance", MessageType.None);
                    break;
                case FighterPreset.Tank:
                    EditorGUILayout.HelpBox("Tank Fighter:\n• High health (120)\n• Low speed (4)\n• High weight (1.5)\n• Hard to knock back, high damage", MessageType.None);
                    break;
                case FighterPreset.Custom:
                    EditorGUILayout.HelpBox("Custom: You'll manually set all stats after creation", MessageType.None);
                    break;
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
            
            // Create Button
            EditorGUI.BeginDisabledGroup(selectedPrefab == null || string.IsNullOrEmpty(fighterName));
            
            if (GUILayout.Button("Create Fighter Definition", GUILayout.Height(40)))
            {
                CreateFighterDef();
            }
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(10);
            
            // Quick Reference
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Available Prefabs", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Player_Fighter - Martial artist", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Player_Sword&Shield - Warrior", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Player_Hammer - Heavy fighter", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Player_DualBlades - Dual wielder", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Player_Bow - Archer", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Player_Pistol - Gunslinger", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Player_Magic - Mage", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Player_Spear - Spear fighter", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Player_Staff - Staff wielder", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Player_Rapier - Fencer", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Player_DoubleBlades - Double blades", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("• Player_Claymore - Greatsword", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void ShowPrefabPicker()
        {
            string path = "Assets/Shinabro/Platform_Animation/Prefabs";
            string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { path });
            
            GenericMenu menu = new GenericMenu();
            
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                
                if (prefab != null && prefab.name.StartsWith("Player_"))
                {
                    menu.AddItem(new GUIContent(prefab.name), false, () => {
                        selectedPrefab = prefab;
                        Repaint();
                    });
                }
            }
            
            menu.ShowAsContext();
        }
        
        private void CreateFighterDef()
        {
            // Create the asset
            FighterDef newFighter = ScriptableObject.CreateInstance<FighterDef>();
            
            // Set basic info
            newFighter.fighterName = fighterName;
            newFighter.description = fighterDescription;
            newFighter.fighterPrefab = selectedPrefab;
            
            // Apply preset stats
            ApplyPresetStats(newFighter, selectedPreset);
            
            // Save the asset
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Fighter Definition",
                $"Fighter_{fighterName}",
                "asset",
                "Choose where to save the fighter definition",
                "Assets"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(newFighter, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                
                // Select the new asset
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = newFighter;
                
                EditorUtility.DisplayDialog("Success!", 
                    $"Fighter '{fighterName}' created successfully!\n\nYou can now customize it further in the Inspector.", 
                    "OK");
                
                // Reset the window
                selectedPrefab = null;
                fighterName = "";
                fighterDescription = "";
                selectedPreset = FighterPreset.Balanced;
            }
        }
        
        private void ApplyPresetStats(FighterDef fighter, FighterPreset preset)
        {
            switch (preset)
            {
                case FighterPreset.Fast:
                    fighter.maxHealth = 90f;
                    fighter.moveSpeed = 7.5f;
                    fighter.jumpForce = 14f;
                    fighter.dashSpeed = 18f;
                    fighter.dashDuration = 0.2f;
                    fighter.weight = 0.8f;
                    fighter.baseDamageMultiplier = 0.9f;
                    fighter.baseDefenseMultiplier = 0.9f;
                    break;
                    
                case FighterPreset.Balanced:
                    fighter.maxHealth = 100f;
                    fighter.moveSpeed = 5f;
                    fighter.jumpForce = 10f;
                    fighter.dashSpeed = 12f;
                    fighter.dashDuration = 0.3f;
                    fighter.weight = 1f;
                    fighter.baseDamageMultiplier = 1f;
                    fighter.baseDefenseMultiplier = 1f;
                    break;
                    
                case FighterPreset.Tank:
                    fighter.maxHealth = 120f;
                    fighter.moveSpeed = 4f;
                    fighter.jumpForce = 8f;
                    fighter.dashSpeed = 10f;
                    fighter.dashDuration = 0.4f;
                    fighter.weight = 1.5f;
                    fighter.baseDamageMultiplier = 1.2f;
                    fighter.baseDefenseMultiplier = 1.1f;
                    break;
                    
                case FighterPreset.Custom:
                    // Use defaults
                    fighter.maxHealth = 100f;
                    fighter.moveSpeed = 5f;
                    fighter.jumpForce = 10f;
                    fighter.dashSpeed = 12f;
                    fighter.dashDuration = 0.3f;
                    fighter.weight = 1f;
                    fighter.baseDamageMultiplier = 1f;
                    fighter.baseDefenseMultiplier = 1f;
                    break;
            }
        }
    }
}

