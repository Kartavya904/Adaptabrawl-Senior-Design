using System.Collections.Generic;

using UnityEngine;

using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Combat
{
    public static class CombatVolumeBuilder
    {
        private const string RuntimeWeaponVolumeName = "CombatWeaponVolume";
        private const string RuntimeSpecialVolumeName = "CombatSpecialMoveVolume";
        private const string RuntimeCoreHurtboxPrefix = "RuntimeCoreHurtbox_";
        private static readonly string[] WeaponStrikeKeywords = { "blade", "head", "tip", "spike", "sword", "hammer", "spear", "axe", "dualblade", "rapier", "staff", "bow" };
        private static readonly string[] WeaponIgnoreKeywords = { "shield", "handle", "hilt", "grip", "pommel", "guard" };

        public static void Rebuild(Transform stander, FighterController owner)
        {
            if (stander == null || owner == null)
                return;

            CleanupLegacyVolumes(stander);

            FighterHurtbox hurtbox = stander.GetComponent<FighterHurtbox>();
            if (hurtbox == null)
                hurtbox = stander.gameObject.AddComponent<FighterHurtbox>();

            List<HurtboxPart> parts = BuildHurtboxRig(stander, owner, hurtbox);
            hurtbox.ResetParts(parts);

            BuildWeaponVolumes(stander, owner);
            BuildSpecialMoveVolume(stander, owner);
        }

        private static void CleanupLegacyVolumes(Transform stander)
        {
            foreach (Transform t in stander.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "WeaponHitbox"
                    || t.name == RuntimeWeaponVolumeName
                    || t.name == RuntimeSpecialVolumeName
                    || t.name.StartsWith(RuntimeCoreHurtboxPrefix))
                    Object.Destroy(t.gameObject);
            }

            foreach (Collider2D collider2D in stander.GetComponentsInChildren<Collider2D>(true))
                Object.Destroy(collider2D);

            foreach (HitboxEmitter emitter in stander.GetComponentsInChildren<HitboxEmitter>(true))
                emitter.enabled = false;

            foreach (ShinabroDamageBridge bridge in stander.GetComponentsInChildren<ShinabroDamageBridge>(true))
                Object.Destroy(bridge);

        }

        private static List<HurtboxPart> BuildHurtboxRig(Transform stander, FighterController owner, FighterHurtbox rig)
        {
            List<Collider> bodyColliders = CollectBodyColliders(stander);
            if (bodyColliders.Count == 0)
                bodyColliders = GenerateBodyColliders(stander);

            Bounds bodyBounds = GetBodyBounds(stander);
            AddSupplementalCoreColliders(stander, bodyBounds, bodyColliders);
            List<HurtboxPart> parts = new List<HurtboxPart>(bodyColliders.Count);

            foreach (Collider collider in bodyColliders)
            {
                if (collider == null)
                    continue;

                collider.isTrigger = true;

                BodyPartType partType = ClassifyBodyPart(stander, bodyBounds, collider);
                float multiplier = GetDamageMultiplier(partType);

                HurtboxPart part = collider.GetComponent<HurtboxPart>();
                if (part == null)
                    part = collider.gameObject.AddComponent<HurtboxPart>();

                part.Initialize(rig, owner, partType, multiplier, collider);
                parts.Add(part);
            }

            return parts;
        }

        private static void BuildWeaponVolumes(Transform stander, FighterController owner)
        {
            DamageSystem damageSystem = owner.GetComponent<DamageSystem>();

            foreach (Transform weaponRoot in stander.GetComponentsInChildren<Transform>(true))
            {
                if (!weaponRoot.name.StartsWith("Weapon_") || weaponRoot.name.Contains("Shield"))
                    continue;

                MeshFilter strikeMesh = SelectStrikeMesh(weaponRoot);
                if (strikeMesh == null || strikeMesh.sharedMesh == null)
                    continue;

                WeaponStrikeProfile profile = GetStrikeProfile(weaponRoot.name, strikeMesh.name);
                Mesh runtimeMesh = BuildRuntimeWeaponMesh(strikeMesh.sharedMesh, profile);
                if (runtimeMesh == null)
                    continue;

                GameObject volumeObject = new GameObject(RuntimeWeaponVolumeName);
                volumeObject.transform.SetParent(strikeMesh.transform, false);
                volumeObject.transform.localPosition = Vector3.zero;
                volumeObject.transform.localRotation = Quaternion.identity;
                volumeObject.transform.localScale = Vector3.one;

                MeshCollider meshCollider = volumeObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = runtimeMesh;
                meshCollider.convex = true;
                meshCollider.isTrigger = true;
                meshCollider.enabled = false;

                WeaponHitVolume hitVolume = volumeObject.AddComponent<WeaponHitVolume>();
                hitVolume.Initialize(owner, damageSystem, meshCollider, runtimeMesh);
            }
        }

        private static void BuildSpecialMoveVolume(Transform stander, FighterController owner)
        {
            if (stander == null || owner == null)
                return;

            DamageSystem damageSystem = owner.GetComponent<DamageSystem>();

            GameObject volumeObject = new GameObject(RuntimeSpecialVolumeName);
            volumeObject.transform.SetParent(stander, false);
            volumeObject.transform.localPosition = Vector3.zero;
            volumeObject.transform.localRotation = Quaternion.identity;
            volumeObject.transform.localScale = Vector3.one;

            BoxCollider boxCollider = volumeObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(0.1f, 0.1f, 0.8f);
            boxCollider.enabled = false;

            SpecialMoveHitVolume hitVolume = volumeObject.AddComponent<SpecialMoveHitVolume>();
            hitVolume.Initialize(owner, damageSystem, boxCollider);
        }

        private static List<Collider> CollectBodyColliders(Transform stander)
        {
            List<Collider> colliders = new List<Collider>();

            foreach (Collider collider in stander.GetComponentsInChildren<Collider>(true))
            {
                if (collider == null)
                    continue;

                if (collider.GetComponentInParent<WeaponHitVolume>() != null)
                    continue;

                if (IsWeaponTransform(collider.transform))
                    continue;

                colliders.Add(collider);
            }

            return colliders;
        }

        private static List<Collider> GenerateBodyColliders(Transform stander)
        {
            List<Collider> generated = new List<Collider>();
            SkinnedMeshRenderer smr = stander.GetComponentInChildren<SkinnedMeshRenderer>();

            if (smr == null || smr.bones == null || smr.bones.Length == 0)
            {
                CapsuleCollider fallback = stander.gameObject.AddComponent<CapsuleCollider>();
                fallback.height = 2f;
                fallback.radius = 0.4f;
                fallback.center = new Vector3(0f, 1f, 0f);
                fallback.isTrigger = true;
                generated.Add(fallback);
                return generated;
            }

            foreach (Transform bone in smr.bones)
            {
                if (bone == null || IsWeaponTransform(bone))
                    continue;

                Collider existing = bone.GetComponent<Collider>();
                if (existing != null)
                {
                    generated.Add(existing);
                    continue;
                }

                CapsuleCollider capsule = bone.gameObject.AddComponent<CapsuleCollider>();
                capsule.isTrigger = true;

                if (bone.childCount > 0)
                {
                    Transform child = bone.GetChild(0);
                    Vector3 localChildPos = bone.InverseTransformPoint(child.position);
                    float localLength = localChildPos.magnitude;

                    if (localLength < 0.01f)
                    {
                        capsule.radius = 0.05f;
                        capsule.height = 0.1f;
                        capsule.center = Vector3.zero;
                    }
                    else
                    {
                        capsule.height = localLength;
                        capsule.radius = Mathf.Max(0.05f, localLength * 0.25f);
                        capsule.center = localChildPos * 0.5f;

                        float absX = Mathf.Abs(localChildPos.x);
                        float absY = Mathf.Abs(localChildPos.y);
                        float absZ = Mathf.Abs(localChildPos.z);

                        if (absX >= absY && absX >= absZ)
                            capsule.direction = 0;
                        else if (absY >= absX && absY >= absZ)
                            capsule.direction = 1;
                        else
                            capsule.direction = 2;
                    }
                }
                else
                {
                    capsule.radius = 0.05f;
                    capsule.height = 0.1f;
                    capsule.center = Vector3.zero;
                }

                generated.Add(capsule);
            }

            return generated;
        }

        private static void AddSupplementalCoreColliders(Transform stander, Bounds bodyBounds, List<Collider> colliders)
        {
            if (stander == null || colliders == null)
                return;

            float bodyWidth = Mathf.Max(bodyBounds.size.x, 0.55f);
            float bodyHeight = Mathf.Max(bodyBounds.size.y, 1.6f);
            float bodyDepth = Mathf.Max(bodyBounds.size.z, bodyWidth * 0.6f);

            Transform pelvisAnchor = FindBestBodyTransform(stander, "pelvis", "hips", "hip");
            Transform spineAnchor = FindBestBodyTransform(stander, "spine", "spine1", "spine2", "chest");
            Transform chestAnchor = FindBestBodyTransform(stander, "chest", "upperchest", "spine2", "spine1");
            Transform neckAnchor = FindBestBodyTransform(stander, "neck", "head");
            Transform headAnchor = FindBestBodyTransform(stander, "head");

            colliders.Add(CreateRuntimeBoxCollider(
                pelvisAnchor != null ? pelvisAnchor : stander,
                $"{RuntimeCoreHurtboxPrefix}Pelvis",
                pelvisAnchor != null
                    ? Vector3.zero
                    : new Vector3(0f, Mathf.Lerp(bodyBounds.min.y, bodyBounds.max.y, 0.34f), 0f),
                new Vector3(bodyWidth * 0.34f, bodyHeight * 0.16f, bodyDepth * 0.34f)));

            colliders.Add(CreateRuntimeBoxCollider(
                spineAnchor != null ? spineAnchor : stander,
                $"{RuntimeCoreHurtboxPrefix}LowerTorso",
                spineAnchor != null
                    ? Vector3.zero
                    : new Vector3(0f, Mathf.Lerp(bodyBounds.min.y, bodyBounds.max.y, 0.5f), 0f),
                new Vector3(bodyWidth * 0.32f, bodyHeight * 0.24f, bodyDepth * 0.26f)));

            colliders.Add(CreateRuntimeBoxCollider(
                chestAnchor != null ? chestAnchor : stander,
                $"{RuntimeCoreHurtboxPrefix}UpperTorso",
                chestAnchor != null
                    ? Vector3.zero
                    : new Vector3(0f, Mathf.Lerp(bodyBounds.min.y, bodyBounds.max.y, 0.68f), 0f),
                new Vector3(bodyWidth * 0.38f, bodyHeight * 0.22f, bodyDepth * 0.3f)));

            colliders.Add(CreateRuntimeCapsuleCollider(
                neckAnchor != null ? neckAnchor : stander,
                $"{RuntimeCoreHurtboxPrefix}Neck",
                neckAnchor != null
                    ? Vector3.zero
                    : new Vector3(0f, Mathf.Lerp(bodyBounds.min.y, bodyBounds.max.y, 0.82f), 0f),
                bodyHeight * 0.1f,
                bodyWidth * 0.08f,
                1));

            colliders.Add(CreateRuntimeBoxCollider(
                headAnchor != null ? headAnchor : stander,
                $"{RuntimeCoreHurtboxPrefix}Head",
                headAnchor != null
                    ? Vector3.zero
                    : new Vector3(0f, Mathf.Lerp(bodyBounds.min.y, bodyBounds.max.y, 0.93f), 0f),
                new Vector3(bodyWidth * 0.32f, bodyHeight * 0.18f, bodyDepth * 0.28f)));
        }

        private static Bounds GetBodyBounds(Transform stander)
        {
            SkinnedMeshRenderer smr = stander.GetComponentInChildren<SkinnedMeshRenderer>();
            if (smr == null)
                return new Bounds(Vector3.up, new Vector3(1f, 2f, 1f));

            Bounds bounds = smr.localBounds;
            bounds.center = stander.InverseTransformPoint(smr.transform.TransformPoint(bounds.center));
            bounds.size = AbsVector(stander.InverseTransformVector(smr.transform.TransformVector(bounds.size)));
            return bounds;
        }

        private static Transform FindBestBodyTransform(Transform stander, params string[] keywords)
        {
            foreach (Transform child in stander.GetComponentsInChildren<Transform>(true))
            {
                if (child == null || IsWeaponTransform(child))
                    continue;

                string name = child.name.ToLowerInvariant();
                for (int i = 0; i < keywords.Length; i++)
                {
                    if (name.Contains(keywords[i]))
                        return child;
                }
            }

            return null;
        }

        private static BoxCollider CreateRuntimeBoxCollider(Transform parent, string name, Vector3 localPosition, Vector3 size)
        {
            GameObject colliderObject = new GameObject(name);
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.localPosition = localPosition;
            colliderObject.transform.localRotation = Quaternion.identity;
            colliderObject.transform.localScale = Vector3.one;

            BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = Vector3.zero;
            collider.size = new Vector3(
                Mathf.Max(size.x, 0.08f),
                Mathf.Max(size.y, 0.08f),
                Mathf.Max(size.z, 0.08f));

            return collider;
        }

        private static CapsuleCollider CreateRuntimeCapsuleCollider(
            Transform parent,
            string name,
            Vector3 localPosition,
            float height,
            float radius,
            int direction)
        {
            GameObject colliderObject = new GameObject(name);
            colliderObject.transform.SetParent(parent, false);
            colliderObject.transform.localPosition = localPosition;
            colliderObject.transform.localRotation = Quaternion.identity;
            colliderObject.transform.localScale = Vector3.one;

            CapsuleCollider collider = colliderObject.AddComponent<CapsuleCollider>();
            collider.isTrigger = true;
            collider.center = Vector3.zero;
            collider.direction = direction;
            collider.height = Mathf.Max(height, radius * 2f + 0.04f);
            collider.radius = Mathf.Max(radius, 0.04f);
            return collider;
        }

        private static BodyPartType ClassifyBodyPart(Transform stander, Bounds bodyBounds, Collider collider)
        {
            Vector3 localCenter = stander.InverseTransformPoint(collider.bounds.center);
            float normalizedHeight = Mathf.InverseLerp(bodyBounds.min.y, bodyBounds.max.y, localCenter.y);
            float normalizedSide = Mathf.Abs(localCenter.x) / Mathf.Max(bodyBounds.extents.x, 0.001f);

            string name = collider.transform.name.ToLowerInvariant();
            if (name.Contains("head") || name.Contains("neck"))
                return BodyPartType.Head;
            if (name.Contains("arm") || name.Contains("hand") || name.Contains("shoulder"))
                return BodyPartType.Arm;
            if (name.Contains("leg") || name.Contains("foot") || name.Contains("thigh") || name.Contains("calf"))
                return BodyPartType.Leg;
            if (name.Contains("spine") || name.Contains("chest") || name.Contains("torso") || name.Contains("hip"))
                return BodyPartType.Torso;

            if (normalizedHeight >= 0.82f)
                return BodyPartType.Head;
            if (normalizedHeight <= 0.35f)
                return BodyPartType.Leg;
            if (normalizedSide >= 0.42f)
                return BodyPartType.Arm;

            return BodyPartType.Torso;
        }

        private static float GetDamageMultiplier(BodyPartType partType)
        {
            switch (partType)
            {
                case BodyPartType.Head:
                    return 1.2f;
                case BodyPartType.Arm:
                    return 0.9f;
                case BodyPartType.Leg:
                    return 0.85f;
                default:
                    return 1f;
            }
        }

        private static MeshFilter SelectStrikeMesh(Transform weaponRoot)
        {
            MeshFilter[] meshFilters = weaponRoot.GetComponentsInChildren<MeshFilter>(true);
            MeshFilter best = null;
            int bestScore = int.MinValue;

            foreach (MeshFilter meshFilter in meshFilters)
            {
                if (meshFilter == null || meshFilter.sharedMesh == null)
                    continue;

                MeshRenderer renderer = meshFilter.GetComponent<MeshRenderer>();
                if (renderer == null || !renderer.enabled)
                    continue;

                string name = meshFilter.name.ToLowerInvariant();
                int score = 0;

                foreach (string keyword in WeaponStrikeKeywords)
                {
                    if (name.Contains(keyword))
                        score += 25;
                }

                foreach (string keyword in WeaponIgnoreKeywords)
                {
                    if (name.Contains(keyword))
                        score -= 100;
                }

                score += Mathf.RoundToInt(meshFilter.sharedMesh.bounds.size.sqrMagnitude * 10f);

                if (score > bestScore)
                {
                    best = meshFilter;
                    bestScore = score;
                }
            }

            return best;
        }

        private static WeaponStrikeProfile GetStrikeProfile(string weaponName, string meshName)
        {
            string combined = $"{weaponName} {meshName}".ToLowerInvariant();

            if (combined.Contains("hammer"))
                return WeaponStrikeProfile.Hammer;
            if (combined.Contains("staff"))
                return WeaponStrikeProfile.Staff;
            if (combined.Contains("spear"))
                return WeaponStrikeProfile.Spear;
            if (combined.Contains("dual") || combined.Contains("blade") || combined.Contains("sword") || combined.Contains("rapier") || combined.Contains("claymore"))
                return WeaponStrikeProfile.Blade;

            return WeaponStrikeProfile.Default;
        }

        private static Mesh BuildRuntimeWeaponMesh(Mesh sourceMesh, WeaponStrikeProfile profile)
        {
            if (sourceMesh == null)
                return null;

            if (!sourceMesh.isReadable)
                return Object.Instantiate(sourceMesh);

            return ExtractStrikeMesh(sourceMesh, profile);
        }

        private static Mesh ExtractStrikeMesh(Mesh sourceMesh, WeaponStrikeProfile profile)
        {
            Vector3[] vertices = sourceMesh.vertices;
            int[] triangles = sourceMesh.triangles;

            if (vertices == null || vertices.Length == 0 || triangles == null || triangles.Length == 0)
                return Object.Instantiate(sourceMesh);

            int axis = GetDominantAxis(sourceMesh.bounds.size);
            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (Vector3 vertex in vertices)
            {
                float projection = GetAxisValue(vertex, axis);
                if (projection < min)
                    min = projection;
                if (projection > max)
                    max = projection;
            }

            bool positiveFar = Mathf.Abs(max) >= Mathf.Abs(min);
            float length = Mathf.Max(Mathf.Abs(max - min), 0.001f);
            float keepFraction = GetKeepFraction(profile);
            float threshold = positiveFar
                ? max - (length * keepFraction)
                : min + (length * keepFraction);

            Dictionary<int, int> remap = new Dictionary<int, int>();
            List<Vector3> keptVertices = new List<Vector3>();
            List<int> keptTriangles = new List<int>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];

                int keepCount = 0;
                if (PassesThreshold(vertices[a], axis, positiveFar, threshold)) keepCount++;
                if (PassesThreshold(vertices[b], axis, positiveFar, threshold)) keepCount++;
                if (PassesThreshold(vertices[c], axis, positiveFar, threshold)) keepCount++;

                if (keepCount < 2)
                    continue;

                keptTriangles.Add(GetOrAddVertex(remap, keptVertices, vertices, a));
                keptTriangles.Add(GetOrAddVertex(remap, keptVertices, vertices, b));
                keptTriangles.Add(GetOrAddVertex(remap, keptVertices, vertices, c));
            }

            if (keptTriangles.Count < 3)
                return Object.Instantiate(sourceMesh);

            Mesh strikeMesh = new Mesh
            {
                name = $"{sourceMesh.name}_StrikeVolume"
            };

            strikeMesh.SetVertices(keptVertices);
            strikeMesh.SetTriangles(keptTriangles, 0);
            strikeMesh.RecalculateBounds();
            strikeMesh.RecalculateNormals();
            return strikeMesh;
        }

        private static bool IsWeaponTransform(Transform transform)
        {
            Transform current = transform;
            while (current != null)
            {
                if (current.name.StartsWith("Weapon_"))
                    return true;

                current = current.parent;
            }

            return false;
        }

        private static bool PassesThreshold(Vector3 vertex, int axis, bool positiveFar, float threshold)
        {
            float projection = GetAxisValue(vertex, axis);
            return positiveFar ? projection >= threshold : projection <= threshold;
        }

        private static int GetDominantAxis(Vector3 size)
        {
            if (size.x >= size.y && size.x >= size.z)
                return 0;
            if (size.y >= size.x && size.y >= size.z)
                return 1;
            return 2;
        }

        private static float GetAxisValue(Vector3 value, int axis)
        {
            switch (axis)
            {
                case 0:
                    return value.x;
                case 1:
                    return value.y;
                default:
                    return value.z;
            }
        }

        private static float GetKeepFraction(WeaponStrikeProfile profile)
        {
            switch (profile)
            {
                case WeaponStrikeProfile.Hammer:
                    return 0.35f;
                case WeaponStrikeProfile.Spear:
                    return 0.2f;
                case WeaponStrikeProfile.Staff:
                    return 0.72f;
                case WeaponStrikeProfile.Blade:
                    return 0.55f;
                default:
                    return 0.45f;
            }
        }

        private static int GetOrAddVertex(Dictionary<int, int> remap, List<Vector3> keptVertices, Vector3[] sourceVertices, int sourceIndex)
        {
            if (remap.TryGetValue(sourceIndex, out int remappedIndex))
                return remappedIndex;

            remappedIndex = keptVertices.Count;
            remap[sourceIndex] = remappedIndex;
            keptVertices.Add(sourceVertices[sourceIndex]);
            return remappedIndex;
        }

        private static Vector3 AbsVector(Vector3 vector)
        {
            return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
        }

        private enum WeaponStrikeProfile
        {
            Default,
            Hammer,
            Spear,
            Staff,
            Blade
        }
    }
}
