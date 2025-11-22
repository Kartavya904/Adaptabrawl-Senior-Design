using UnityEngine;
using UnityEditor;
using Adaptabrawl.Data;

namespace Adaptabrawl.Editor
{
    /// <summary>
    /// Custom editor for MoveDef with visual hitbox preview
    /// </summary>
    [CustomEditor(typeof(MoveDef))]
    public class MoveDefEditor : UnityEditor.Editor
    {
        private bool showHitboxPreview = true;
        private float previewScale = 50f;
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            MoveDef move = (MoveDef)target;
            
            // Title
            EditorGUILayout.Space();
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 16;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("Move Definition", titleStyle);
            EditorGUILayout.Space();
            
            // Basic Info
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Basic Info", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moveName"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("moveType"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            // Frame Data with visual timeline
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Frame Data", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("startupFrames"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("activeFrames"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("recoveryFrames"));
            
            // Visual timeline
            DrawFrameTimeline(move);
            
            EditorGUILayout.LabelField($"Total: {move.totalFrames} frames ({move.totalFrames / 60f:F2}s @ 60 FPS)", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            // Combat Properties
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Combat Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("damage"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("knockbackForce"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("knockbackDirection"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hitstopFrames"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("blockstunFrames"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hitstunFrames"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            // Hitbox Section - This is the key part!
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Hitbox Configuration", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox("Configure multiple hitboxes for complex attacks. Each hitbox can have different timing, size, and damage multipliers.", MessageType.Info);
            
            // Quick add buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Hitbox", GUILayout.Height(25)))
            {
                AddHitbox(move);
            }
            if (move.hitboxDefinitions != null && move.hitboxDefinitions.Length > 0)
            {
                if (GUILayout.Button("Clear All", GUILayout.Height(25), GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog("Clear Hitboxes", "Remove all hitboxes?", "Yes", "Cancel"))
                    {
                        ClearHitboxes(move);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);
            
            // Hitbox list
            SerializedProperty hitboxArray = serializedObject.FindProperty("hitboxDefinitions");
            if (hitboxArray != null && hitboxArray.arraySize > 0)
            {
                for (int i = 0; i < hitboxArray.arraySize; i++)
                {
                    DrawHitboxElement(hitboxArray.GetArrayElementAtIndex(i), i, move);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No hitboxes defined. Click 'Add Hitbox' to create one.", MessageType.Warning);
            }
            
            // Legacy fields (for backwards compatibility)
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Legacy Single Hitbox (Deprecated)", EditorStyles.miniLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hitboxOffset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hitboxSize"));
            EditorGUI.indentLevel--;
            
            EditorGUILayout.Space(5);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isProjectile"));
            if (move.isProjectile)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("projectilePrefab"));
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            // Visual Preview
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            showHitboxPreview = EditorGUILayout.Foldout(showHitboxPreview, "Hitbox Visual Preview", true);
            if (showHitboxPreview)
            {
                DrawHitboxPreview(move);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            // Draw remaining properties
            DrawPropertiesExcluding(serializedObject, new string[] {
                "m_Script",
                "moveName",
                "moveType",
                "startupFrames",
                "activeFrames",
                "recoveryFrames",
                "damage",
                "knockbackForce",
                "knockbackDirection",
                "hitstopFrames",
                "blockstunFrames",
                "hitstunFrames",
                "hitboxOffset",
                "hitboxSize",
                "hitboxDefinitions",
                "isProjectile",
                "projectilePrefab"
            });
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void DrawFrameTimeline(MoveDef move)
        {
            Rect rect = GUILayoutUtility.GetRect(10, 30);
            
            float totalWidth = rect.width;
            float startupWidth = (float)move.startupFrames / move.totalFrames * totalWidth;
            float activeWidth = (float)move.activeFrames / move.totalFrames * totalWidth;
            float recoveryWidth = (float)move.recoveryFrames / move.totalFrames * totalWidth;
            
            // Startup
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, startupWidth, rect.height), new Color(1f, 1f, 0f, 0.5f));
            // Active
            EditorGUI.DrawRect(new Rect(rect.x + startupWidth, rect.y, activeWidth, rect.height), new Color(0f, 1f, 0f, 0.5f));
            // Recovery
            EditorGUI.DrawRect(new Rect(rect.x + startupWidth + activeWidth, rect.y, recoveryWidth, rect.height), new Color(1f, 0f, 0f, 0.5f));
            
            // Labels
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            GUI.Label(new Rect(rect.x, rect.y, startupWidth, rect.height), $"Startup\n{move.startupFrames}f", labelStyle);
            GUI.Label(new Rect(rect.x + startupWidth, rect.y, activeWidth, rect.height), $"Active\n{move.activeFrames}f", labelStyle);
            GUI.Label(new Rect(rect.x + startupWidth + activeWidth, rect.y, recoveryWidth, rect.height), $"Recovery\n{move.recoveryFrames}f", labelStyle);
        }
        
        private void DrawHitboxElement(SerializedProperty hitboxProp, int index, MoveDef move)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Header with delete button
            EditorGUILayout.BeginHorizontal();
            SerializedProperty nameProp = hitboxProp.FindPropertyRelative("name");
            nameProp.stringValue = EditorGUILayout.TextField("Name", nameProp.stringValue);
            
            if (GUILayout.Button("×", GUILayout.Width(25), GUILayout.Height(18)))
            {
                DeleteHitbox(move, index);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            EditorGUILayout.EndHorizontal();
            
            // Properties
            EditorGUILayout.PropertyField(hitboxProp.FindPropertyRelative("offset"));
            EditorGUILayout.PropertyField(hitboxProp.FindPropertyRelative("size"));
            EditorGUILayout.PropertyField(hitboxProp.FindPropertyRelative("activeStartFrame"));
            EditorGUILayout.PropertyField(hitboxProp.FindPropertyRelative("activeEndFrame"));
            EditorGUILayout.PropertyField(hitboxProp.FindPropertyRelative("damageMultiplier"));
            EditorGUILayout.PropertyField(hitboxProp.FindPropertyRelative("knockbackMultiplier"));
            EditorGUILayout.PropertyField(hitboxProp.FindPropertyRelative("knockbackDirectionOverride"));
            EditorGUILayout.PropertyField(hitboxProp.FindPropertyRelative("isSweetspot"));
            EditorGUILayout.PropertyField(hitboxProp.FindPropertyRelative("gizmoColor"));
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
        
        private void DrawHitboxPreview(MoveDef move)
        {
            if (move.hitboxDefinitions == null || move.hitboxDefinitions.Length == 0)
            {
                EditorGUILayout.HelpBox("No hitboxes to preview", MessageType.Info);
                return;
            }
            
            previewScale = EditorGUILayout.Slider("Preview Scale", previewScale, 20f, 100f);
            
            Rect previewRect = GUILayoutUtility.GetRect(200, 200);
            EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f, 1f));
            
            // Draw fighter representation (simple circle)
            Vector2 center = previewRect.center;
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(center, Vector3.forward, 10f);
            
            // Draw hitboxes
            foreach (var hitbox in move.hitboxDefinitions)
            {
                Vector2 hitboxCenter = center + hitbox.offset * previewScale;
                Vector2 hitboxSize = hitbox.size * previewScale;
                
                Rect hitboxRect = new Rect(
                    hitboxCenter.x - hitboxSize.x * 0.5f,
                    hitboxCenter.y - hitboxSize.y * 0.5f,
                    hitboxSize.x,
                    hitboxSize.y
                );
                
                // Fill
                EditorGUI.DrawRect(hitboxRect, hitbox.gizmoColor);
                
                // Border
                Handles.color = new Color(hitbox.gizmoColor.r, hitbox.gizmoColor.g, hitbox.gizmoColor.b, 1f);
                Handles.DrawSolidRectangleWithOutline(hitboxRect, Color.clear, Handles.color);
                
                // Label
                GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel);
                labelStyle.normal.textColor = Color.white;
                GUI.Label(hitboxRect, $"{hitbox.name}\n×{hitbox.damageMultiplier:F1}", labelStyle);
            }
        }
        
        private void AddHitbox(MoveDef move)
        {
            Undo.RecordObject(move, "Add Hitbox");
            
            var newHitbox = new HitboxDefinition
            {
                name = $"Hitbox_{(move.hitboxDefinitions?.Length ?? 0) + 1}",
                offset = Vector2.right * 0.5f,
                size = new Vector2(1f, 1f),
                activeStartFrame = 0,
                activeEndFrame = -1,
                damageMultiplier = 1f,
                knockbackMultiplier = 1f,
                gizmoColor = new Color(0f, 1f, 0f, 0.3f)
            };
            
            if (move.hitboxDefinitions == null)
            {
                move.hitboxDefinitions = new HitboxDefinition[] { newHitbox };
            }
            else
            {
                var list = new System.Collections.Generic.List<HitboxDefinition>(move.hitboxDefinitions);
                list.Add(newHitbox);
                move.hitboxDefinitions = list.ToArray();
            }
            
            EditorUtility.SetDirty(move);
        }
        
        private void DeleteHitbox(MoveDef move, int index)
        {
            Undo.RecordObject(move, "Delete Hitbox");
            
            var list = new System.Collections.Generic.List<HitboxDefinition>(move.hitboxDefinitions);
            list.RemoveAt(index);
            move.hitboxDefinitions = list.ToArray();
            
            EditorUtility.SetDirty(move);
        }
        
        private void ClearHitboxes(MoveDef move)
        {
            Undo.RecordObject(move, "Clear Hitboxes");
            move.hitboxDefinitions = new HitboxDefinition[0];
            EditorUtility.SetDirty(move);
        }
    }
}

