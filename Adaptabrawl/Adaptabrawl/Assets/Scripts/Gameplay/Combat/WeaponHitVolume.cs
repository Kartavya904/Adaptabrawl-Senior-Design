using System.Collections.Generic;

using UnityEngine;

using Adaptabrawl.Data;
using Adaptabrawl.Gameplay;

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

            int targetId = target.GetInstanceID();
            if (hitTargets.Contains(targetId))
                return;

            hitTargets.Add(targetId);
            Debug.Log(
                $"[CombatHit] {(owner != null && owner.FighterDef != null ? owner.FighterDef.fighterName : gameObject.name)} " +
                $"used '{currentMove.moveName}' and hit P{target.PlayerNumber} " +
                $"({(target.FighterDef != null ? target.FighterDef.fighterName : target.gameObject.name)}) " +
                $"on {hurtboxPart.BodyPart} via '{transform.parent?.name ?? gameObject.name}'.");
            damageSystem.DealDamage(target, currentMove, hurtboxPart.DamageMultiplier);
        }

        private bool CanDealDamage(MoveDef move)
        {
            return move != null
                && move.damage > 0f
                && (move.moveType == MoveType.LightAttack
                    || move.moveType == MoveType.HeavyAttack
                    || move.moveType == MoveType.Special);
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
    }
}
