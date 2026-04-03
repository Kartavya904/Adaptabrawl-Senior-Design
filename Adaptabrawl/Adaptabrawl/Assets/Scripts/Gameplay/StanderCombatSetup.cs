using UnityEngine;
using Adaptabrawl.Combat;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Bootstrap component placed on the root fighter by FighterFactory.
    /// In Start() — after the full prefab hierarchy is live — it:
    ///   1. Attaches FighterHurtbox + ShinabroDamageBridge to the Stander root.
    ///   2. Walks every Weapon_* mesh inside the skeleton and bakes a HitboxEmitter
    ///      onto each one, sized to that weapon's actual mesh bounds.
    ///
    /// This replaces the editor-only "Tools/Bake Hitboxes Onto Weapons" step so
    /// hitboxes are derived automatically every time the game starts.
    /// </summary>
    public class StanderCombatSetup : MonoBehaviour
    {
        private void Start()
        {
            Transform stander = transform.Find("Stander");

            if (stander == null)
            {
                string children = "";
                foreach (Transform c in transform) children += $"'{c.name}' ";
                Debug.LogWarning($"[StanderCombatSetup] No 'Stander' child on '{gameObject.name}'. " +
                                 $"Children found: {children}. Combat volumes NOT attached.");
                Destroy(this);
                return;
            }

            // Hurtbox and damage bridge always live on the Stander root.
            if (stander.GetComponent<FighterHurtbox>() == null)
                stander.gameObject.AddComponent<FighterHurtbox>();

            if (stander.GetComponent<ShinabroDamageBridge>() == null)
                stander.gameObject.AddComponent<ShinabroDamageBridge>();

            // Bake a HitboxEmitter onto every Weapon_* mesh (shields excluded).
            bool weaponFound = false;
            foreach (Transform t in stander.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Weapon_") || t.name.Contains("Shield"))
                    continue;

                BakeWeaponHitbox(t);
                weaponFound = true;
            }

            if (!weaponFound)
            {
                // Fallback for fighters with no weapon mesh hierarchy.
                if (stander.GetComponent<HitboxEmitter>() == null)
                    stander.gameObject.AddComponent<HitboxEmitter>();
                Debug.LogWarning($"[StanderCombatSetup] No Weapon_* transforms under '{gameObject.name}' " +
                                 $"— added fallback HitboxEmitter on Stander root.");
            }

            Debug.Log($"[StanderCombatSetup] Setup complete on '{gameObject.name}' " +
                      $"({(weaponFound ? "weapon hitboxes baked" : "fallback emitter")}).");
            Destroy(this);
        }

        // -------------------------------------------------------------------

        /// <summary>
        /// Creates (or reuses) a WeaponHitbox child on <paramref name="weaponTransform"/>,
        /// adds a HitboxEmitter, and configures it from the mesh's own bounds.
        /// Mirrors ApplyWeaponHitboxes.BakeHitboxes() without needing AssetDatabase.
        /// </summary>
        private static void BakeWeaponHitbox(Transform weaponTransform)
        {
            // Reuse the WeaponHitbox GO if the prefab was already editor-baked.
            Transform hitboxRoot = weaponTransform.Find("WeaponHitbox");
            if (hitboxRoot == null)
            {
                var hbObj = new GameObject("WeaponHitbox");
                hbObj.transform.SetParent(weaponTransform, false);
                hbObj.transform.localPosition = Vector3.zero;
                hbObj.transform.localRotation = Quaternion.identity;
                hitboxRoot = hbObj.transform;
            }

            HitboxEmitter emitter = hitboxRoot.GetComponent<HitboxEmitter>();
            if (emitter == null)
                emitter = hitboxRoot.gameObject.AddComponent<HitboxEmitter>();

            (Vector3 offset, Vector2 size) = CalculateWeaponHitbox(weaponTransform);
            // Light = exact mesh-derived values; Heavy = same position, 1.3× bigger.
            emitter.ApplyWeaponConfig(offset, size, offset, size * 1.3f);

            Debug.Log($"[StanderCombatSetup] Baked '{weaponTransform.name}' — " +
                      $"offset={offset:F2}  size={size:F2}");
        }

        /// <summary>
        /// Computes the hitbox offset (in weapon-local space) and size from the
        /// weapon renderer's mesh bounds, applying weapon-type-specific shaping.
        /// </summary>
        private static (Vector3 offset, Vector2 size) CalculateWeaponHitbox(Transform weaponTransform)
        {
            Renderer rend = weaponTransform.GetComponentInChildren<Renderer>();
            if (rend == null)
                return (Vector3.zero, new Vector2(1f, 1f));

            // Express mesh bounds in the weapon transform's local space.
            Bounds b;
            if (rend.transform == weaponTransform)
            {
                b = rend.localBounds;
            }
            else
            {
                b        = rend.localBounds;
                b.center = weaponTransform.InverseTransformPoint(
                               rend.transform.TransformPoint(b.center));
                b.size   = weaponTransform.InverseTransformVector(
                               rend.transform.TransformVector(b.size));
            }

            float xExt = Mathf.Abs(b.extents.x);
            float yExt = Mathf.Abs(b.extents.y);
            float zExt = Mathf.Abs(b.extents.z);

            float  primarySize, secondarySize;
            string dominantAxis;

            if (zExt > yExt && zExt > xExt)
            {
                primarySize   = Mathf.Abs(b.size.z);
                secondarySize = Mathf.Max(Mathf.Abs(b.size.x), Mathf.Abs(b.size.y));
                dominantAxis  = "Z";
            }
            else if (xExt > yExt && xExt > zExt)
            {
                primarySize   = Mathf.Abs(b.size.x);
                secondarySize = Mathf.Max(Mathf.Abs(b.size.y), Mathf.Abs(b.size.z));
                dominantAxis  = "X";
            }
            else
            {
                primarySize   = Mathf.Abs(b.size.y);
                secondarySize = Mathf.Max(Mathf.Abs(b.size.x), Mathf.Abs(b.size.z));
                dominantAxis  = "Y";
            }

            Vector3 offset = b.center; // start at mesh centre
            Vector2 size;
            string  wName  = weaponTransform.name;

            if (wName.Contains("Hammer"))
            {
                // Push toward the hammer head (far end of the dominant axis).
                float shift = primarySize * 0.35f;
                ApplyShift(ref offset, dominantAxis, GetSign(b.center, dominantAxis) * shift);
                size = new Vector2(secondarySize * 1.5f, primarySize * 0.3f);
            }
            else if (wName.Contains("Spear") || wName.Contains("Staff"))
            {
                // Push toward the spear tip.
                float shift = primarySize * 0.45f;
                ApplyShift(ref offset, dominantAxis, GetSign(b.center, dominantAxis) * shift);
                size = new Vector2(secondarySize * 1.2f, primarySize * 0.2f);
            }
            else
            {
                // Sword / DualBlade — blade covers most of the weapon length.
                size = new Vector2(secondarySize * 1.2f, primarySize * 0.9f);
            }

            return (offset, size);
        }

        private static float GetSign(Vector3 center, string axis)
        {
            float v = axis == "X" ? center.x : axis == "Y" ? center.y : center.z;
            return v == 0f ? 1f : Mathf.Sign(v);
        }

        private static void ApplyShift(ref Vector3 v, string axis, float shift)
        {
            if      (axis == "X") v.x += shift;
            else if (axis == "Y") v.y += shift;
            else                  v.z += shift;
        }
    }
}
