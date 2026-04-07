using System.Collections.Generic;

using UnityEngine;

using Adaptabrawl.Data;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class SpecialMoveHitVolume : MonoBehaviour
    {
        [SerializeField] private FighterController owner;
        [SerializeField] private DamageSystem damageSystem;
        [SerializeField] private BoxCollider triggerCollider;
        [SerializeField] private MoveDef currentMove;
        [SerializeField] private bool isActive;

        private readonly HashSet<int> hitTargets = new HashSet<int>();
        private CombatFSM combatFSM;

        private void Awake()
        {
            if (triggerCollider == null)
                triggerCollider = GetComponent<BoxCollider>();

            if (owner == null)
                owner = GetComponentInParent<FighterController>();

            if (damageSystem == null && owner != null)
                damageSystem = owner.GetComponent<DamageSystem>();

            if (triggerCollider != null)
                triggerCollider.isTrigger = true;

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

        public void Initialize(FighterController ownerFighter, DamageSystem sourceDamageSystem, BoxCollider sourceCollider)
        {
            owner = ownerFighter;
            damageSystem = sourceDamageSystem;
            triggerCollider = sourceCollider;

            if (triggerCollider != null)
                triggerCollider.isTrigger = true;

            combatFSM = GetComponentInParent<CombatFSM>();
            SyncRuntimeState();
        }

        public void SyncRuntimeState()
        {
            if (triggerCollider != null)
                triggerCollider.enabled = enabled && gameObject.activeInHierarchy && isActive;
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

        private void HandleMoveStarted(MoveDef move)
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
            ApplyMoveVolume(move);
            SyncRuntimeState();
        }

        private void HandleMoveEnded(MoveDef move)
        {
            if (move != currentMove)
                return;

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
            if (target == null || target == owner || target.IsInvulnerable)
                return;

            int targetId = target.GetInstanceID();
            if (hitTargets.Contains(targetId))
                return;

            hitTargets.Add(targetId);

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
            return move != null
                && move.moveType == MoveType.Special
                && move.damage > 0f;
        }

        private void ApplyMoveVolume(MoveDef move)
        {
            if (triggerCollider == null || move == null)
                return;

            Bounds bounds = CalculateMoveBounds(move);
            triggerCollider.center = bounds.center;
            triggerCollider.size = new Vector3(
                Mathf.Max(bounds.size.x, 0.12f),
                Mathf.Max(bounds.size.y, 0.12f),
                Mathf.Max(bounds.size.z, 0.8f));
        }

        private static Bounds CalculateMoveBounds(MoveDef move)
        {
            if (move.hitboxDefinitions != null && move.hitboxDefinitions.Length > 0)
            {
                Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
                Vector2 max = new Vector2(float.MinValue, float.MinValue);
                bool hasDefinition = false;

                foreach (HitboxDefinition definition in move.hitboxDefinitions)
                {
                    if (definition == null)
                        continue;

                    Vector2 halfSize = definition.size * 0.5f;
                    min = Vector2.Min(min, definition.offset - halfSize);
                    max = Vector2.Max(max, definition.offset + halfSize);
                    hasDefinition = true;
                }

                if (hasDefinition)
                {
                    Vector2 size2D = max - min;
                    Vector2 center2D = min + size2D * 0.5f;
                    return new Bounds(
                        new Vector3(center2D.x, center2D.y, 0f),
                        new Vector3(size2D.x, size2D.y, 0.8f));
                }
            }

            return new Bounds(
                new Vector3(move.hitboxOffset.x, move.hitboxOffset.y, 0f),
                new Vector3(move.hitboxSize.x, move.hitboxSize.y, 0.8f));
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
    }
}
