using System.Collections.Generic;

using UnityEngine;

using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Combat
{
    /// <summary>
    /// Adaptabrawl-owned hurtbox rig manager.
    /// Runtime body-part colliders are derived from the character skeleton and
    /// registered here so combat can resolve a struck fighter from any part.
    /// </summary>
    [DisallowMultipleComponent]
    public class FighterHurtbox : MonoBehaviour
    {
        [SerializeField] private FighterController owner;
        [SerializeField] private List<HurtboxPart> parts = new List<HurtboxPart>();

        private readonly Dictionary<Collider, HurtboxPart> partsByCollider = new Dictionary<Collider, HurtboxPart>();

        private void Awake()
        {
            if (owner == null)
                owner = GetComponentInParent<FighterController>();

            RebuildLookup();
        }

        private void OnEnable()
        {
            SetPartsEnabled(true);
        }

        private void OnDisable()
        {
            SetPartsEnabled(false);
        }

        public void ResetParts(IEnumerable<HurtboxPart> hurtboxParts)
        {
            parts.Clear();

            if (hurtboxParts != null)
            {
                foreach (HurtboxPart part in hurtboxParts)
                {
                    if (part != null)
                        parts.Add(part);
                }
            }

            RebuildLookup();
            SetPartsEnabled(enabled);
        }

        public void SetPartsEnabled(bool isEnabled)
        {
            foreach (HurtboxPart part in parts)
            {
                if (part == null)
                    continue;

                part.SetEnabled(isEnabled);
            }
        }

        public HurtboxPart ResolvePart(Collider other)
        {
            if (other == null)
                return null;

            if (partsByCollider.TryGetValue(other, out HurtboxPart part))
                return part;

            part = other.GetComponent<HurtboxPart>() ?? other.GetComponentInParent<HurtboxPart>();
            if (part != null && part.HurtboxCollider != null)
                partsByCollider[part.HurtboxCollider] = part;

            return part;
        }

        public FighterController Owner => owner;
        public IReadOnlyList<HurtboxPart> Parts => parts;

        private void RebuildLookup()
        {
            partsByCollider.Clear();

            foreach (HurtboxPart part in parts)
            {
                if (part?.HurtboxCollider == null)
                    continue;

                partsByCollider[part.HurtboxCollider] = part;
            }
        }
    }
}
