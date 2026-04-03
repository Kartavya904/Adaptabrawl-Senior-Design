using UnityEngine;
using UnityEditor;
using Adaptabrawl.Combat;

namespace Adaptabrawl.Editor
{
    public class ApplyWeaponHitboxes
    {
        [MenuItem("Tools/Bake Hitboxes Onto Weapons")]
        public static void BakeHitboxes()
        {
            // The characters requested
            string[] prefabs = { "Player_Hammer", "Player_DualBlades", "Player_Sword&Shield", "Player_Spear" };
            
            foreach (var pName in prefabs)
            {
                string path = $"Assets/Shinabro/Platform_Animation/Prefabs/{pName}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"Could not find prefab at {path}");
                    continue;
                }
                
                // Instantiate the prefab to modify it
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                
                Transform stander = inst.transform.Find("Stander");
                if (stander != null)
                {
                    // Find all weapon meshes
                    Transform[] allTransforms = stander.GetComponentsInChildren<Transform>(true);
                    foreach (Transform t in allTransforms)
                    {
                        if (t.name.StartsWith("Weapon_") && !t.name.Contains("Shield"))
                        {
                            // Delete any old Hitbox before making a new one
                            Transform existingHitbox = t.Find("WeaponHitbox");
                            if (existingHitbox != null)
                            {
                                Object.DestroyImmediate(existingHitbox.gameObject);
                            }
                            
                            GameObject hbObj = new GameObject("WeaponHitbox");
                                hbObj.transform.SetParent(t, false);
                                hbObj.transform.localPosition = Vector3.zero;
                                hbObj.transform.localRotation = Quaternion.identity;
                                
                                HitboxEmitter emitter = hbObj.AddComponent<HitboxEmitter>();
                                
                                // Clean default configs and replace to specifically scale to weapon
                                SerializedObject so = new SerializedObject(emitter);
                                SerializedProperty configsProp = so.FindProperty("configs");
                                configsProp.ClearArray();
                                
                                // Create logic for customizing size and offset
                                Vector3 weaponOffset = Vector3.zero;
                                Vector2 weaponSize = new Vector2(1f, 1f);
                                
                                Renderer renderer = t.GetComponentInChildren<Renderer>();
                                if (renderer != null)
                                {
                                    // Get local bounds relative to the weapon transform
                                    Bounds b;
                                    if (renderer.transform == t)
                                    {
                                        b = renderer.localBounds;
                                    }
                                    else
                                    {
                                        // Calculate bounds of the child renderer relative to the weapon root
                                        b = renderer.localBounds;
                                        b.center = t.InverseTransformPoint(renderer.transform.TransformPoint(b.center));
                                        b.size = t.InverseTransformVector(renderer.transform.TransformVector(b.size));
                                    }

                                    float xExt = Mathf.Abs(b.extents.x);
                                    float yExt = Mathf.Abs(b.extents.y);
                                    float zExt = Mathf.Abs(b.extents.z);
                                    
                                    // Start EXACTLY at the center of the 3D mesh
                                    weaponOffset = b.center;
                                    
                                    float primaryAxisSize = 0f;
                                    float secondaryAxisSize = 0f;
                                    string dominantAxis = "Y";
                                    
                                    // Determine the longest axis (shaft/blade)
                                    if (zExt > yExt && zExt > xExt)
                                    {
                                        primaryAxisSize = Mathf.Abs(b.size.z);
                                        secondaryAxisSize = Mathf.Max(Mathf.Abs(b.size.x), Mathf.Abs(b.size.y));
                                        dominantAxis = "Z";
                                    }
                                    else if (xExt > yExt && xExt > zExt)
                                    {
                                        primaryAxisSize = Mathf.Abs(b.size.x);
                                        secondaryAxisSize = Mathf.Max(Mathf.Abs(b.size.y), Mathf.Abs(b.size.z));
                                        dominantAxis = "X";
                                    }
                                    else
                                    {
                                        primaryAxisSize = Mathf.Abs(b.size.y);
                                        secondaryAxisSize = Mathf.Max(Mathf.Abs(b.size.x), Mathf.Abs(b.size.z));
                                        dominantAxis = "Y";
                                    }

                                    // Shape it based on weapon type
                                    if (t.name.Contains("Hammer"))
                                    {
                                        // Push the offset towards the far end of the dominant axis
                                        float shift = primaryAxisSize * 0.35f;
                                        
                                        if (dominantAxis == "Z") weaponOffset.z += (Mathf.Sign(b.center.z) == 0 ? 1 : Mathf.Sign(b.center.z)) * shift;
                                        else if (dominantAxis == "X") weaponOffset.x += (Mathf.Sign(b.center.x) == 0 ? 1 : Mathf.Sign(b.center.x)) * shift;
                                        else weaponOffset.y += (Mathf.Sign(b.center.y) == 0 ? 1 : Mathf.Sign(b.center.y)) * shift;
                                        
                                        weaponSize = new Vector2(secondaryAxisSize * 1.5f, primaryAxisSize * 0.3f);
                                    }
                                    else if (t.name.Contains("Spear") || t.name.Contains("Staff"))
                                    {
                                        // Tip is very far end
                                        float shift = primaryAxisSize * 0.45f;
                                        
                                        if (dominantAxis == "Z") weaponOffset.z += (Mathf.Sign(b.center.z) == 0 ? 1 : Mathf.Sign(b.center.z)) * shift;
                                        else if (dominantAxis == "X") weaponOffset.x += (Mathf.Sign(b.center.x) == 0 ? 1 : Mathf.Sign(b.center.x)) * shift;
                                        else weaponOffset.y += (Mathf.Sign(b.center.y) == 0 ? 1 : Mathf.Sign(b.center.y)) * shift;
                                        
                                        weaponSize = new Vector2(secondaryAxisSize * 1.2f, primaryAxisSize * 0.2f);
                                    }
                                    else // Swords, Blades
                                    {
                                        // Blade is mostly the whole thing
                                        weaponSize = new Vector2(secondaryAxisSize * 1.2f, primaryAxisSize * 0.9f);
                                        // Offset is just exactly the center!
                                    }
                                }
                                
                                // Clean default configs and replace to specifically scale to weapon
                                SerializedProperty defaultConfig = so.FindProperty("defaultConfig");
                                defaultConfig.FindPropertyRelative("offset").vector3Value = weaponOffset;
                                defaultConfig.FindPropertyRelative("size").vector2Value = weaponSize;

                                // Add LightAttack
                                configsProp.InsertArrayElementAtIndex(0);
                                SerializedProperty lightAttack = configsProp.GetArrayElementAtIndex(0);
                                lightAttack.FindPropertyRelative("moveType").enumValueIndex = (int)Adaptabrawl.Data.MoveType.LightAttack;
                                lightAttack.FindPropertyRelative("damage").floatValue = 8f;
                                lightAttack.FindPropertyRelative("knockbackForce").floatValue = 3f;
                                lightAttack.FindPropertyRelative("size").vector2Value = weaponSize;
                                lightAttack.FindPropertyRelative("offset").vector3Value = weaponOffset;
                                lightAttack.FindPropertyRelative("duration").floatValue = 0.083f;
                                
                                // Add HeavyAttack
                                configsProp.InsertArrayElementAtIndex(1);
                                SerializedProperty heavyAttack = configsProp.GetArrayElementAtIndex(1);
                                heavyAttack.FindPropertyRelative("moveType").enumValueIndex = (int)Adaptabrawl.Data.MoveType.HeavyAttack;
                                heavyAttack.FindPropertyRelative("damage").floatValue = 20f;
                                heavyAttack.FindPropertyRelative("knockbackForce").floatValue = 8f;
                                heavyAttack.FindPropertyRelative("size").vector2Value = weaponSize * 1.3f;
                                heavyAttack.FindPropertyRelative("offset").vector3Value = weaponOffset;
                                heavyAttack.FindPropertyRelative("duration").floatValue = 0.1f;
                                
                                so.ApplyModifiedProperties();
                        }
                    }
                }
                
                // Save and clean up
                PrefabUtility.SaveAsPrefabAsset(inst, path);
                Object.DestroyImmediate(inst);
                Debug.Log($"Baked hitboxes for {pName}");
            }
            
            Debug.Log("Finished baking hitboxes directly onto weapon prefabs.");
        }
    }
}
