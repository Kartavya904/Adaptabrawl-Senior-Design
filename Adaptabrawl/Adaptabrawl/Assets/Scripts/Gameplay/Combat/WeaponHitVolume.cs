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
