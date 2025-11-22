using UnityEngine;
using UnityEditor;
using Adaptabrawl.Data;

namespace Adaptabrawl.Editor
{
    [CustomEditor(typeof(FighterDef))]
    public class FighterDefEditor : UnityEditor.Editor
    {
        private SerializedProperty fighterPrefabProp;
        private SerializedProperty fighterNameProp;
        private SerializedProperty descriptionProp;
        private SerializedProperty portraitProp;
        private SerializedProperty maxHealthProp;
        private SerializedProperty moveSpeedProp;
        private SerializedProperty jumpForceProp;
        private SerializedProperty dashSpeedProp;
        private SerializedProperty dashDurationProp;
        private SerializedProperty weightProp;
        private SerializedProperty baseDamageMultiplierProp;
        private SerializedProperty baseDefenseMultiplierProp;
        private SerializedProperty armorBreakThresholdProp;
        
        private void OnEnable()
        {
            fighterPrefabProp = serializedObject.FindProperty("fighterPrefab");
            fighterNameProp = serializedObject.FindProperty("fighterName");
            descriptionProp = serializedObject.FindProperty("description");
            portraitProp = serializedObject.FindProperty("portrait");
            maxHealthProp = serializedObject.FindProperty("maxHealth");
            moveSpeedProp = serializedObject.FindProperty("moveSpeed");
            jumpForceProp = serializedObject.FindProperty("jumpForce");
            dashSpeedProp = serializedObject.FindProperty("dashSpeed");
            dashDurationProp = serializedObject.FindProperty("dashDuration");
            weightProp = serializedObject.FindProperty("weight");
            baseDamageMultiplierProp = serializedObject.FindProperty("baseDamageMultiplier");
            baseDefenseMultiplierProp = serializedObject.FindProperty("baseDefenseMultiplier");
            armorBreakThresholdProp = serializedObject.FindProperty("armorBreakThreshold");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Title
            EditorGUILayout.Space();
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 16;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("Fighter Definition", titleStyle);
            EditorGUILayout.Space();
            
            // Prefab field with special highlight
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Visual Setup", EditorStyles.boldLabel);
            
            GameObject previousPrefab = fighterPrefabProp.objectReferenceValue as GameObject;
            EditorGUILayout.PropertyField(fighterPrefabProp, new GUIContent("Fighter Prefab", "Drag a Shinabro prefab here (e.g., Player_Fighter, Player_Hammer)"));
            GameObject currentPrefab = fighterPrefabProp.objectReferenceValue as GameObject;
            
            // Auto-name suggestion
            if (currentPrefab != null && previousPrefab != currentPrefab)
            {
                if (string.IsNullOrEmpty(fighterNameProp.stringValue))
                {
                    string suggestedName = currentPrefab.name.Replace("Player_", "");
                    if (EditorUtility.DisplayDialog("Auto-Name Fighter?", 
                        $"Would you like to name this fighter '{suggestedName}'?", "Yes", "No"))
                    {
                        fighterNameProp.stringValue = suggestedName;
                    }
                }
            }
            
            if (fighterPrefabProp.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ No prefab assigned! Drag a Shinabro prefab here.\n\nAvailable prefabs in: Assets/Shinabro/Platform_Animation/Prefabs/", MessageType.Warning);
                
                if (GUILayout.Button("Browse Shinabro Prefabs"))
                {
                    string path = "Assets/Shinabro/Platform_Animation/Prefabs";
                    Object obj = EditorGUIUtility.Load(path);
                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(path);
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"✓ Using prefab: {currentPrefab.name}", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            // Basic Info
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(fighterNameProp);
            EditorGUILayout.PropertyField(descriptionProp);
            EditorGUILayout.PropertyField(portraitProp);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            // Base Stats with presets
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Base Stats", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Stat Presets:", GUILayout.Width(80));
            if (GUILayout.Button("Fast", GUILayout.Width(60)))
            {
                ApplyFastPreset();
            }
            if (GUILayout.Button("Balanced", GUILayout.Width(60)))
            {
                ApplyBalancedPreset();
            }
            if (GUILayout.Button("Tank", GUILayout.Width(60)))
            {
                ApplyTankPreset();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
            
            EditorGUILayout.PropertyField(maxHealthProp);
            EditorGUILayout.PropertyField(moveSpeedProp);
            EditorGUILayout.PropertyField(jumpForceProp);
            EditorGUILayout.PropertyField(dashSpeedProp);
            EditorGUILayout.PropertyField(dashDurationProp);
            EditorGUILayout.PropertyField(weightProp, new GUIContent("Weight", "Affects knockback resistance (higher = harder to knock back)"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            // Combat Stats
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Combat Stats", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(baseDamageMultiplierProp, new GUIContent("Base Damage Multiplier", "Multiplier for all damage dealt"));
            EditorGUILayout.PropertyField(baseDefenseMultiplierProp, new GUIContent("Base Defense Multiplier", "Multiplier for damage received"));
            EditorGUILayout.PropertyField(armorBreakThresholdProp);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            // Hurtbox Configuration
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Hurtbox Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Hurtboxes define where the fighter can be hit. These are automatically created when the fighter spawns.", MessageType.Info);
            
            SerializedProperty hurtboxArray = serializedObject.FindProperty("hurtboxes");
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Hurtbox", GUILayout.Height(25)))
            {
                AddHurtbox(hurtboxArray);
            }
            if (hurtboxArray.arraySize > 0)
            {
                if (GUILayout.Button("Reset to Default", GUILayout.Height(25), GUILayout.Width(120)))
                {
                    if (EditorUtility.DisplayDialog("Reset Hurtboxes", "Reset to default hurtbox configuration?", "Yes", "Cancel"))
                    {
                        ResetHurtboxes(hurtboxArray);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
            
            if (hurtboxArray.arraySize > 0)
            {
                for (int i = 0; i < hurtboxArray.arraySize; i++)
                {
                    DrawHurtboxElement(hurtboxArray.GetArrayElementAtIndex(i), i, hurtboxArray);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No hurtboxes defined! Add at least one hurtbox.", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
            
            // Draw the rest using default inspector
            EditorGUILayout.Space();
            DrawPropertiesExcluding(serializedObject, new string[] { 
                "m_Script", 
                "fighterPrefab",
                "fighterName",
                "description",
                "portrait",
                "maxHealth",
                "moveSpeed",
                "jumpForce",
                "dashSpeed",
                "dashDuration",
                "weight",
                "baseDamageMultiplier",
                "baseDefenseMultiplier",
                "armorBreakThreshold",
                "hurtboxes"
            });
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawHurtboxElement(SerializedProperty hurtboxProp, int index, SerializedProperty array)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Header with delete button
            EditorGUILayout.BeginHorizontal();
            SerializedProperty nameProp = hurtboxProp.FindPropertyRelative("name");
            nameProp.stringValue = EditorGUILayout.TextField("Name", nameProp.stringValue);
            
            if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(18)))
            {
                array.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            // Properties
            EditorGUILayout.PropertyField(hurtboxProp.FindPropertyRelative("offset"), new GUIContent("Offset"));
            EditorGUILayout.PropertyField(hurtboxProp.FindPropertyRelative("size"), new GUIContent("Size"));
            EditorGUILayout.PropertyField(hurtboxProp.FindPropertyRelative("isActive"), new GUIContent("Active by Default"));
            EditorGUILayout.PropertyField(hurtboxProp.FindPropertyRelative("damageMultiplier"), new GUIContent("Damage Multiplier"));
            EditorGUILayout.PropertyField(hurtboxProp.FindPropertyRelative("gizmoColor"), new GUIContent("Editor Color"));
            
            // Info
            float damageMultiplier = hurtboxProp.FindPropertyRelative("damageMultiplier").floatValue;
            if (damageMultiplier > 1f)
            {
                EditorGUILayout.HelpBox($"Critical area: Takes {damageMultiplier:F1}× damage", MessageType.Info);
            }
            else if (damageMultiplier < 1f)
            {
                EditorGUILayout.HelpBox($"Resistant area: Takes {damageMultiplier:F1}× damage", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }
        
        private void AddHurtbox(SerializedProperty array)
        {
            array.InsertArrayElementAtIndex(array.arraySize);
            SerializedProperty newElement = array.GetArrayElementAtIndex(array.arraySize - 1);
            
            newElement.FindPropertyRelative("name").stringValue = $"Hurtbox_{array.arraySize}";
            newElement.FindPropertyRelative("offset").vector2Value = Vector2.zero;
            newElement.FindPropertyRelative("size").vector2Value = new Vector2(1f, 1f);
            newElement.FindPropertyRelative("isActive").boolValue = true;
            newElement.FindPropertyRelative("damageMultiplier").floatValue = 1f;
            newElement.FindPropertyRelative("gizmoColor").colorValue = new Color(1f, 0f, 0f, 0.3f);
        }
        
        private void ResetHurtboxes(SerializedProperty array)
        {
            array.ClearArray();
            
            // Add default body hurtbox
            array.InsertArrayElementAtIndex(0);
            SerializedProperty body = array.GetArrayElementAtIndex(0);
            body.FindPropertyRelative("name").stringValue = "Body";
            body.FindPropertyRelative("offset").vector2Value = Vector2.zero;
            body.FindPropertyRelative("size").vector2Value = new Vector2(1f, 2f);
            body.FindPropertyRelative("isActive").boolValue = true;
            body.FindPropertyRelative("damageMultiplier").floatValue = 1f;
            body.FindPropertyRelative("gizmoColor").colorValue = new Color(1f, 0f, 0f, 0.3f);
            
            // Add default head hurtbox
            array.InsertArrayElementAtIndex(1);
            SerializedProperty head = array.GetArrayElementAtIndex(1);
            head.FindPropertyRelative("name").stringValue = "Head";
            head.FindPropertyRelative("offset").vector2Value = new Vector2(0f, 1.5f);
            head.FindPropertyRelative("size").vector2Value = new Vector2(0.5f, 0.5f);
            head.FindPropertyRelative("isActive").boolValue = true;
            head.FindPropertyRelative("damageMultiplier").floatValue = 1.2f;
            head.FindPropertyRelative("gizmoColor").colorValue = new Color(1f, 0.5f, 0f, 0.3f);
        }
        
        private void ApplyFastPreset()
        {
            maxHealthProp.floatValue = 90f;
            moveSpeedProp.floatValue = 7.5f;
            jumpForceProp.floatValue = 14f;
            dashSpeedProp.floatValue = 18f;
            dashDurationProp.floatValue = 0.2f;
            weightProp.floatValue = 0.8f;
            baseDamageMultiplierProp.floatValue = 0.9f;
            baseDefenseMultiplierProp.floatValue = 0.9f;
            serializedObject.ApplyModifiedProperties();
        }
        
        private void ApplyBalancedPreset()
        {
            maxHealthProp.floatValue = 100f;
            moveSpeedProp.floatValue = 5f;
            jumpForceProp.floatValue = 10f;
            dashSpeedProp.floatValue = 12f;
            dashDurationProp.floatValue = 0.3f;
            weightProp.floatValue = 1f;
            baseDamageMultiplierProp.floatValue = 1f;
            baseDefenseMultiplierProp.floatValue = 1f;
            serializedObject.ApplyModifiedProperties();
        }
        
        private void ApplyTankPreset()
        {
            maxHealthProp.floatValue = 120f;
            moveSpeedProp.floatValue = 4f;
            jumpForceProp.floatValue = 8f;
            dashSpeedProp.floatValue = 10f;
            dashDurationProp.floatValue = 0.4f;
            weightProp.floatValue = 1.5f;
            baseDamageMultiplierProp.floatValue = 1.2f;
            baseDefenseMultiplierProp.floatValue = 1.1f;
            serializedObject.ApplyModifiedProperties();
        }
    }
}

