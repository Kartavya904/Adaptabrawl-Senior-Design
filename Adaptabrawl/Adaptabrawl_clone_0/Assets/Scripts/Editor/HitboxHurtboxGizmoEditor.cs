using UnityEngine;
using UnityEditor;
using Adaptabrawl.Combat;
using Adaptabrawl.Data;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Editor
{
    /// <summary>
    /// Scene view editor for visually editing hitboxes and hurtboxes
    /// </summary>
    [CustomEditor(typeof(HitboxHurtboxSpawner))]
    public class HitboxHurtboxGizmoEditor : UnityEditor.Editor
    {
        private HitboxHurtboxSpawner spawner;
        private FighterController fighterController;
        private bool editHurtboxes = true;
        private int selectedHurtboxIndex = -1;
        
        private void OnEnable()
        {
            spawner = (HitboxHurtboxSpawner)target;
            fighterController = spawner.GetComponent<FighterController>();
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hitbox/Hurtbox Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("This component automatically creates hitboxes and hurtboxes based on FighterDef and MoveDef configurations.", MessageType.Info);
            EditorGUILayout.Space();
            
            // Reference section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fighterController"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            // Debug section
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Scene View Visualization", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showGizmos"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showLabels"));
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
            
            // Hurtbox management
            if (fighterController != null && fighterController.FighterDef != null)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Hurtboxes for {fighterController.FighterDef.fighterName}", EditorStyles.boldLabel);
                
                if (GUILayout.Button("Edit Hurtboxes in FighterDef"))
                {
                    Selection.activeObject = fighterController.FighterDef;
                }
                
                var hurtboxes = fighterController.FighterDef.hurtboxes;
                if (hurtboxes != null && hurtboxes.Length > 0)
                {
                    EditorGUILayout.LabelField($"Total Hurtboxes: {hurtboxes.Length}", EditorStyles.miniLabel);
                    foreach (var hurtbox in hurtboxes)
                    {
                        EditorGUILayout.LabelField($"• {hurtbox.name} - Size: {hurtbox.size}, Damage: ×{hurtbox.damageMultiplier:F1}", EditorStyles.miniLabel);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No hurtboxes defined in FighterDef!", MessageType.Warning);
                }
                
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("Assign a FighterController with a valid FighterDef to see hurtbox configuration.", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            // Runtime info
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
                
                var spawnedHurtboxes = spawner.GetHurtboxes();
                EditorGUILayout.LabelField($"Active Hurtboxes: {spawnedHurtboxes.Count}");
                
                var spawnedHitboxes = spawner.GetHitboxes();
                EditorGUILayout.LabelField($"Active Hitboxes: {spawnedHitboxes.Count}");
                
                EditorGUILayout.EndVertical();
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private void OnSceneGUI()
        {
            if (fighterController == null || fighterController.FighterDef == null)
                return;
            
            var fighterDef = fighterController.FighterDef;
            if (fighterDef.hurtboxes == null || fighterDef.hurtboxes.Length == 0)
                return;
            
            // Draw and allow editing of hurtboxes
            for (int i = 0; i < fighterDef.hurtboxes.Length; i++)
            {
                DrawHurtboxHandle(i, fighterDef.hurtboxes[i]);
            }
        }
        
        private void DrawHurtboxHandle(int index, HurtboxDefinition hurtbox)
        {
            Vector3 fighterPos = spawner.transform.position;
            Vector3 hurtboxWorldPos = fighterPos + (Vector3)hurtbox.offset;
            
            // Draw the hurtbox
            Handles.color = hurtbox.gizmoColor;
            Handles.DrawWireCube(hurtboxWorldPos, hurtbox.size);
            
            // Position handle
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(hurtboxWorldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(fighterController.FighterDef, "Move Hurtbox");
                hurtbox.offset = newPos - fighterPos;
                EditorUtility.SetDirty(fighterController.FighterDef);
            }
            
            // Draw resize handles
            DrawResizeHandles(hurtbox, hurtboxWorldPos);
            
            // Label
            Handles.Label(hurtboxWorldPos + Vector3.up * (hurtbox.size.y * 0.5f + 0.3f), 
                $"{hurtbox.name}\n×{hurtbox.damageMultiplier:F1} damage");
        }
        
        private void DrawResizeHandles(HurtboxDefinition hurtbox, Vector3 center)
        {
            float handleSize = HandleUtility.GetHandleSize(center) * 0.1f;
            
            // Right handle
            EditorGUI.BeginChangeCheck();
            Vector3 rightHandle = center + Vector3.right * (hurtbox.size.x * 0.5f);
            Vector3 newRight = Handles.Slider(rightHandle, Vector3.right, handleSize, Handles.DotHandleCap, 0.1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(fighterController.FighterDef, "Resize Hurtbox");
                float delta = (newRight - rightHandle).x;
                hurtbox.size.x += delta * 2f;
                EditorUtility.SetDirty(fighterController.FighterDef);
            }
            
            // Top handle
            EditorGUI.BeginChangeCheck();
            Vector3 topHandle = center + Vector3.up * (hurtbox.size.y * 0.5f);
            Vector3 newTop = Handles.Slider(topHandle, Vector3.up, handleSize, Handles.DotHandleCap, 0.1f);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(fighterController.FighterDef, "Resize Hurtbox");
                float delta = (newTop - topHandle).y;
                hurtbox.size.y += delta * 2f;
                EditorUtility.SetDirty(fighterController.FighterDef);
            }
        }
    }
}

