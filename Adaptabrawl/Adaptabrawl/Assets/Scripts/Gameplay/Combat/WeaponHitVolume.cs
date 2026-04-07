using System.Collections.Generic;

using UnityEngine;

using Adaptabrawl.Data;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Settings;

namespace Adaptabrawl.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class WeaponHitVolume : MonoBehaviour
    {
        [SerializeField] private FighterController owner;
        [SerializeField] private DamageSystem damageSystem;
        [SerializeField] private Collider triggerCollider;
        [SerializeField] private MoveDef currentMove;
        [SerializeField] private bool isActive;

        private readonly HashSet<int> hitTargets = new HashSet<int>();
        private CombatFSM combatFSM;
        private Mesh runtimeMesh;
        private static Material lineMaterial;
        private static readonly int ZTest = Shader.PropertyToID("_ZTest");

        private void Awake()
        {
            if (triggerCollider == null)
                triggerCollider = GetComponent<Collider>();

            if (owner == null)
                owner = GetComponentInParent<FighterController>();

            if (damageSystem == null && owner != null)
                damageSystem = owner.GetComponent<DamageSystem>();

            combatFSM = GetComponentInParent<CombatFSM>();
            SyncRuntimeState();
        }

        private void OnEnable()
        {
            Subscribe();
            SyncRuntimeState();
        }

        private void Start()
        {
            Subscribe();
            SyncRuntimeState();
        }

        private void OnDisable()
        {
            Unsubscribe();
            isActive = false;
            currentMove = null;
            hitTargets.Clear();
            SyncRuntimeState();
        }

        private void OnDestroy()
        {
            Unsubscribe();

            if (runtimeMesh != null)
                Object.Destroy(runtimeMesh);
        }

        public void Initialize(FighterController ownerFighter, DamageSystem sourceDamageSystem, Collider sourceCollider, Mesh ownedMesh)
        {
            owner = ownerFighter;
            damageSystem = sourceDamageSystem;
            triggerCollider = sourceCollider;
            runtimeMesh = ownedMesh;

            if (triggerCollider != null)
                triggerCollider.isTrigger = true;

            combatFSM = GetComponentInParent<CombatFSM>();
            SyncRuntimeState();
        }

        public void SyncRuntimeState()
        {
            if (triggerCollider != null)
                triggerCollider.enabled = enabled && gameObject.activeInHierarchy;
        }

        private void OnTriggerEnter(Collider other)
        {
            TryApplyHit(other);
        }

        private void OnTriggerStay(Collider other)
        {
            TryApplyHit(other);
        }

        private void FixedUpdate()
        {
            if (!isActive || currentMove == null || triggerCollider == null || owner == null)
                return;

            HurtboxPart[] hurtboxParts = Object.FindObjectsByType<HurtboxPart>(FindObjectsSortMode.None);
            foreach (HurtboxPart hurtboxPart in hurtboxParts)
            {
                if (hurtboxPart == null || !hurtboxPart.IsActive || hurtboxPart.HurtboxCollider == null)
                    continue;

                if (hurtboxPart.Owner == null || hurtboxPart.Owner == owner)
                    continue;

                if (!IsOverlapping(hurtboxPart.HurtboxCollider))
                    continue;

                TryApplyHit(hurtboxPart.HurtboxCollider);
            }
        }

        private void HandleHitboxActive(MoveDef move)
        {
            if (!CanDealDamage(move))
            {
                isActive = false;
                currentMove = null;
                hitTargets.Clear();
                SyncRuntimeState();
                return;
            }

            currentMove = move;
            isActive = true;
            hitTargets.Clear();
            SyncRuntimeState();
        }

        private void HandleHitboxInactive()
        {
            isActive = false;
            currentMove = null;
            hitTargets.Clear();
            SyncRuntimeState();
        }

        private void TryApplyHit(Collider other)
        {
            if (!isActive || currentMove == null || damageSystem == null || owner == null || other == null)
                return;

            HurtboxPart hurtboxPart = other.GetComponent<HurtboxPart>() ?? other.GetComponentInParent<HurtboxPart>();
            if (hurtboxPart == null || !hurtboxPart.IsActive)
                return;

            FighterController target = hurtboxPart.Owner;
            if (target == null || target == owner)
                return;

            if (target.IsInvulnerable)
                return;

            int targetId = target.GetInstanceID();
            if (hitTargets.Contains(targetId))
                return;

            hitTargets.Add(targetId);
            // Debug.Log(
            //     $"[CombatHit] {(owner != null && owner.FighterDef != null ? owner.FighterDef.fighterName : gameObject.name)} " +
            //     $"used '{currentMove.moveName}' and hit P{target.PlayerNumber} " +
            //     $"({(target.FighterDef != null ? target.FighterDef.fighterName : target.gameObject.name)}) " +
            //     $"on {hurtboxPart.BodyPart} via '{transform.parent?.name ?? gameObject.name}'.");
            Vector3 hitPosition = hurtboxPart.HurtboxCollider != null
                ? hurtboxPart.HurtboxCollider.bounds.center
                : hurtboxPart.transform.position;

            Vector3 hitDirection = (target.transform.position - owner.transform.position).normalized;
            if (hitDirection.sqrMagnitude < 0.0001f)
                hitDirection = owner.FacingRight ? Vector3.right : Vector3.left;

            damageSystem.DealDamage(target, currentMove, hurtboxPart.DamageMultiplier, hitPosition, hitDirection);
        }

        private bool CanDealDamage(MoveDef move)
        {
            // Specials use SpecialMoveHitVolume only. Including Special here double-applies
            // damage (separate hitTargets per volume) for the same animation.
            return move != null
                && move.damage > 0f
                && (move.moveType == MoveType.LightAttack
                    || move.moveType == MoveType.HeavyAttack);
        }

        private bool IsOverlapping(Collider other)
        {
            if (triggerCollider == null || other == null || !triggerCollider.enabled || !other.enabled)
                return false;

            if (!triggerCollider.bounds.Intersects(other.bounds))
                return false;

            return Physics.ComputePenetration(
                triggerCollider,
                triggerCollider.transform.position,
                triggerCollider.transform.rotation,
                other,
                other.transform.position,
                other.transform.rotation,
                out _,
                out _);
        }

        private void Subscribe()
        {
            if (combatFSM == null)
                combatFSM = GetComponentInParent<CombatFSM>();

            if (combatFSM == null)
                return;

            combatFSM.OnMoveStarted -= HandleMoveStarted;
            combatFSM.OnMoveEnded -= HandleMoveEnded;
            combatFSM.OnMoveStarted += HandleMoveStarted;
            combatFSM.OnMoveEnded += HandleMoveEnded;
        }

        private void Unsubscribe()
        {
            if (combatFSM == null)
                return;

            combatFSM.OnMoveStarted -= HandleMoveStarted;
            combatFSM.OnMoveEnded -= HandleMoveEnded;
        }

        private void HandleMoveStarted(MoveDef move)
        {
            if (!CanDealDamage(move))
            {
                isActive = false;
                currentMove = null;
                hitTargets.Clear();
                return;
            }

            currentMove = move;
            isActive = true;
            hitTargets.Clear();
        }

        private void HandleMoveEnded(MoveDef move)
        {
            if (move != currentMove)
                return;

            isActive = false;
            currentMove = null;
            hitTargets.Clear();
        }

        public FighterController Owner => owner;
        public MoveDef CurrentMove => currentMove;
        public bool IsActive => isActive;

        private void OnRenderObject()
        {
            if (!ShouldRenderDebugVolume())
                return;

            EnsureLineMaterial();
            lineMaterial.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.LINES);
            GL.Color(isActive ? new Color(0.1f, 1f, 0.35f, 1f) : new Color(0.1f, 0.8f, 1f, 0.65f));
            DrawColliderWireframe(triggerCollider);
            GL.End();
            GL.PopMatrix();
        }

        private bool ShouldRenderDebugVolume()
        {
            if (!Application.isPlaying || triggerCollider == null)
                return false;

            UnityEngine.Camera currentCamera = UnityEngine.Camera.current;
            if (currentCamera == null || !currentCamera.CompareTag("MainCamera"))
                return false;

            SettingsContext context = SettingsContext.Instance ?? SettingsContext.EnsureExists();
            return context != null && context.showHitboxes;
        }

        private static void EnsureLineMaterial()
        {
            if (lineMaterial != null)
                return;

            Shader shader = Shader.Find("Hidden/Internal-Colored");
            lineMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            lineMaterial.SetInt(ZTest, (int)UnityEngine.Rendering.CompareFunction.LessEqual);
        }

        private static void DrawColliderWireframe(Collider collider)
        {
            if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null && meshCollider.sharedMesh.isReadable)
            {
                DrawMeshWireframe(meshCollider.sharedMesh, meshCollider.transform.localToWorldMatrix);
                return;
            }

            DrawBounds(collider.bounds);
        }

        private static void DrawMeshWireframe(Mesh mesh, Matrix4x4 matrix)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = matrix.MultiplyPoint3x4(vertices[triangles[i]]);
                Vector3 b = matrix.MultiplyPoint3x4(vertices[triangles[i + 1]]);
                Vector3 c = matrix.MultiplyPoint3x4(vertices[triangles[i + 2]]);

                DrawLine(a, b);
                DrawLine(b, c);
                DrawLine(c, a);
            }
        }

        private static void DrawBounds(Bounds bounds)
        {
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            Vector3[] corners =
            {
                center + new Vector3(-extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, -extents.y, -extents.z),
                center + new Vector3(extents.x, extents.y, -extents.z),
                center + new Vector3(-extents.x, extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y, extents.z),
                center + new Vector3(extents.x, -extents.y, extents.z),
                center + new Vector3(extents.x, extents.y, extents.z),
                center + new Vector3(-extents.x, extents.y, extents.z)
            };

            DrawLine(corners[0], corners[1]);
            DrawLine(corners[1], corners[2]);
            DrawLine(corners[2], corners[3]);
            DrawLine(corners[3], corners[0]);

            DrawLine(corners[4], corners[5]);
            DrawLine(corners[5], corners[6]);
            DrawLine(corners[6], corners[7]);
            DrawLine(corners[7], corners[4]);

            DrawLine(corners[0], corners[4]);
            DrawLine(corners[1], corners[5]);
            DrawLine(corners[2], corners[6]);
            DrawLine(corners[3], corners[7]);
        }

        private static void DrawLine(Vector3 start, Vector3 end)
        {
            GL.Vertex(start);
            GL.Vertex(end);
        }
    }
}
