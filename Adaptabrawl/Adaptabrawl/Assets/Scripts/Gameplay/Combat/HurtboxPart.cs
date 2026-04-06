using UnityEngine;

using Adaptabrawl.Gameplay;
using Adaptabrawl.Settings;

namespace Adaptabrawl.Combat
{
    [DisallowMultipleComponent]
    public class HurtboxPart : MonoBehaviour
    {
        [SerializeField] private FighterHurtbox rig;
        [SerializeField] private FighterController owner;
        [SerializeField] private BodyPartType bodyPart = BodyPartType.Torso;
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private Collider hurtboxCollider;

        private static Material lineMaterial;
        private static readonly int ZTest = Shader.PropertyToID("_ZTest");

        public void Initialize(
            FighterHurtbox hurtboxRig,
            FighterController ownerFighter,
            BodyPartType partType,
            float multiplier,
            Collider sourceCollider)
        {
            rig = hurtboxRig;
            owner = ownerFighter;
            bodyPart = partType;
            damageMultiplier = multiplier;
            hurtboxCollider = sourceCollider;

            if (hurtboxCollider != null)
                hurtboxCollider.isTrigger = true;
        }

        public void SetEnabled(bool isEnabled)
        {
            if (hurtboxCollider != null)
                hurtboxCollider.enabled = isEnabled;
        }

        public FighterHurtbox Rig => rig;
        public FighterController Owner => owner;
        public BodyPartType BodyPart => bodyPart;
        public float DamageMultiplier => damageMultiplier;
        public Collider HurtboxCollider => hurtboxCollider;
        public bool IsActive => enabled && hurtboxCollider != null && hurtboxCollider.enabled && rig != null && rig.enabled;

        private void OnRenderObject()
        {
            if (!ShouldRenderDebugVolume())
                return;

            EnsureLineMaterial();
            lineMaterial.SetPass(0);

            GL.PushMatrix();
            GL.Begin(GL.LINES);
            GL.Color(GetBodyPartColor());
            DrawColliderWireframe(hurtboxCollider);
            GL.End();
            GL.PopMatrix();
        }

        private bool ShouldRenderDebugVolume()
        {
            if (!Application.isPlaying || hurtboxCollider == null)
                return false;

            UnityEngine.Camera currentCamera = UnityEngine.Camera.current;
            if (currentCamera == null || !currentCamera.CompareTag("MainCamera"))
                return false;

            SettingsContext context = SettingsContext.Instance ?? SettingsContext.EnsureExists();
            return context != null && context.showHitboxes;
        }

        private Color GetBodyPartColor()
        {
            return bodyPart switch
            {
                BodyPartType.Head => new Color(1f, 0.7f, 0.2f, 1f),
                BodyPartType.Arm => new Color(1f, 0.35f, 0.35f, 1f),
                BodyPartType.Leg => new Color(0.95f, 0.2f, 0.6f, 1f),
                _ => new Color(1f, 0.15f, 0.15f, 1f)
            };
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
            switch (collider)
            {
                case BoxCollider boxCollider:
                    DrawBox(boxCollider);
                    break;
                case CapsuleCollider capsuleCollider:
                    DrawCapsuleBounds(capsuleCollider.bounds);
                    break;
                case SphereCollider sphereCollider:
                    DrawSphereBounds(sphereCollider.bounds);
                    break;
                case MeshCollider meshCollider:
                    DrawBounds(meshCollider.bounds);
                    break;
                default:
                    DrawBounds(collider.bounds);
                    break;
            }
        }

        private static void DrawBox(BoxCollider collider)
        {
            Matrix4x4 matrix = Matrix4x4.TRS(collider.transform.position, collider.transform.rotation, collider.transform.lossyScale);
            Vector3 halfSize = collider.size * 0.5f;
            Vector3 center = collider.center;

            Vector3[] corners =
            {
                matrix.MultiplyPoint3x4(center + new Vector3(-halfSize.x, -halfSize.y, -halfSize.z)),
                matrix.MultiplyPoint3x4(center + new Vector3(halfSize.x, -halfSize.y, -halfSize.z)),
                matrix.MultiplyPoint3x4(center + new Vector3(halfSize.x, halfSize.y, -halfSize.z)),
                matrix.MultiplyPoint3x4(center + new Vector3(-halfSize.x, halfSize.y, -halfSize.z)),
                matrix.MultiplyPoint3x4(center + new Vector3(-halfSize.x, -halfSize.y, halfSize.z)),
                matrix.MultiplyPoint3x4(center + new Vector3(halfSize.x, -halfSize.y, halfSize.z)),
                matrix.MultiplyPoint3x4(center + new Vector3(halfSize.x, halfSize.y, halfSize.z)),
                matrix.MultiplyPoint3x4(center + new Vector3(-halfSize.x, halfSize.y, halfSize.z))
            };

            DrawBoundsEdges(corners);
        }

        private static void DrawCapsuleBounds(Bounds bounds)
        {
            DrawBounds(bounds);
        }

        private static void DrawSphereBounds(Bounds bounds)
        {
            DrawBounds(bounds);
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

            DrawBoundsEdges(corners);
        }

        private static void DrawBoundsEdges(Vector3[] corners)
        {
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

    public enum BodyPartType
    {
        Unknown,
        Head,
        Torso,
        Arm,
        Leg
    }
}
