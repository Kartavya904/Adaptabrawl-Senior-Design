using UnityEditor;
using UnityEngine;

namespace Adaptabrawl.Editor
{
    public static class ApplyWeaponHitboxes
    {
        [MenuItem("Tools/Bake Hitboxes Onto Weapons")]
        public static void BakeHitboxes()
        {
            Debug.LogWarning("Bake Hitboxes Onto Weapons is deprecated. Runtime now builds unified 3D weapon strike volumes automatically.");
            EditorUtility.DisplayDialog(
                "Deprecated Tool",
                "Weapon hitboxes are no longer baked into prefabs. Runtime now builds unified 3D weapon strike volumes automatically.",
                "OK");
        }
    }
}
