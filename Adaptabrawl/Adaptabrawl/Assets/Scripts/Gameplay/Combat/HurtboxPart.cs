using UnityEngine;

using Adaptabrawl.Gameplay;

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
