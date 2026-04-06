using Adaptabrawl.Combat;
using Adaptabrawl.Evade;
using Adaptabrawl.Gameplay;
using UnityEngine;

namespace Adaptabrawl.AI
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public sealed class QuickMatchCpuController : MonoBehaviour
    {
        [SerializeField] private QuickMatchDifficultyTier tier = QuickMatchDifficultyTier.Trainer;

        private FighterController self;
        private FighterController opponent;
        private PlayerController_Platform platformController;
        private CombatFSM selfCombat;
        private CombatFSM opponentCombat;
        private EvadeSystem evadeSystem;
        private QuickMatchHeuristicModel model;

        private bool initialized;
        private bool previousBlockHeld;
        private float nextDecisionTime;
        private float nextActionTime;
        private float blockHoldUntil;
        private float currentMoveIntent;
        private bool currentCrouch;

        private bool pulseJump;
        private bool pulseAttack;
        private bool pulseDodge;
        private bool pulseSpecialLight;
        private bool pulseSpecialHeavy;

        public void Initialize(
            FighterController controlledFighter,
            FighterController opposingFighter,
            QuickMatchDifficultyTier difficultyTier,
            QuickMatchHeuristicModel overrideModel = null)
        {
            self = controlledFighter;
            opponent = opposingFighter;
            tier = difficultyTier;
            model = overrideModel != null ? overrideModel.Clone() : LoadModelForTier(difficultyTier);
            model.Clamp();

            ResolveReferences();
            initialized = self != null && opponent != null && platformController != null;

            currentMoveIntent = 0f;
            currentCrouch = false;
            blockHoldUntil = 0f;
            nextDecisionTime = Time.time + model.reactionSeconds;
            nextActionTime = Time.time + model.minActionSpacingSeconds;

            if (platformController != null)
                platformController.isNetworkControlled = true;

            ClearNetworkState();
        }

        private void Update()
        {
            if (!initialized)
                return;

            ResolveReferences();
            if (self == null || opponent == null || platformController == null)
                return;

            if (self.IsDead || opponent.IsDead || self.IsInputLocked)
            {
                currentMoveIntent = 0f;
                currentCrouch = false;
                blockHoldUntil = 0f;
                ApplyCommands(blockHeld: false);
                return;
            }

            if (Time.time >= nextDecisionTime)
                Think();

            bool blockHeld = Time.time < blockHoldUntil;
            ApplyCommands(blockHeld);
        }

        private void OnDisable()
        {
            if (platformController != null)
                platformController.isNetworkControlled = false;

            ClearNetworkState();
        }

        private void Think()
        {
            pulseJump = false;
            pulseAttack = false;
            pulseDodge = false;
            pulseSpecialLight = false;
            pulseSpecialHeavy = false;

            currentMoveIntent = 0f;
            currentCrouch = false;
            nextDecisionTime = Time.time + GetDecisionDelay();

            float signedDistance = opponent.GetArenaPosition().x - self.GetArenaPosition().x;
            float absoluteDistance = Mathf.Abs(signedDistance);
            float directionToOpponent = Mathf.Approximately(signedDistance, 0f)
                ? (self.FacingRight ? 1f : -1f)
                : Mathf.Sign(signedDistance);

            bool canAct = selfCombat == null || selfCombat.CanAct;
            bool opponentThreatening = IsOpponentThreatening(absoluteDistance);
            bool punishWindow = opponentCombat != null
                && opponentCombat.CurrentState == CombatState.Recovery
                && absoluteDistance <= model.threatRange;

            float healthRatio = Mathf.Clamp01(self.CurrentHealth / Mathf.Max(1f, self.MaxHealth));
            float lowHealthFactor = 1f - healthRatio;

            if (opponentThreatening)
            {
                float blockChance = Mathf.Clamp01(model.blockLikelihood + (lowHealthFactor * model.lowHealthDefenseBias * 0.45f));
                float dodgeChance = Mathf.Clamp01(model.dodgeLikelihood + (lowHealthFactor * 0.15f));

                if (canAct && evadeSystem != null && evadeSystem.CanDodge && Roll(dodgeChance))
                {
                    pulseDodge = true;
                    currentMoveIntent = -directionToOpponent;
                    nextActionTime = Time.time + model.minActionSpacingSeconds;
                    return;
                }

                if (Roll(blockChance))
                {
                    blockHoldUntil = Time.time + Random.Range(model.minBlockHoldSeconds, model.maxBlockHoldSeconds);
                    currentMoveIntent = 0f;
                    return;
                }

                currentMoveIntent = -directionToOpponent * Mathf.Clamp01(model.retreatBias + (lowHealthFactor * 0.2f));
                return;
            }

            if (absoluteDistance > model.optimalRange + model.rangeTolerance)
                currentMoveIntent = directionToOpponent * Mathf.Clamp01(model.approachBias + (model.aggression * 0.15f));
            else if (absoluteDistance < model.closeRange)
                currentMoveIntent = Roll(model.retreatBias) ? -directionToOpponent : 0f;
            else if (Roll(model.pressureBias * 0.35f))
                currentMoveIntent = directionToOpponent * 0.35f;

            if (Roll(model.crouchLikelihood * Mathf.Clamp01(1f - (absoluteDistance / Mathf.Max(0.1f, model.threatRange)))))
                currentCrouch = true;

            if (!canAct || Time.time < nextActionTime)
            {
                if (absoluteDistance > model.optimalRange + model.rangeTolerance && Roll(model.jumpLikelihood * 0.35f))
                    pulseJump = true;
                return;
            }

            if (absoluteDistance <= model.threatRange)
            {
                QueueBestAttack(absoluteDistance, punishWindow);
                return;
            }

            if (absoluteDistance > model.optimalRange + model.rangeTolerance && Roll(model.jumpLikelihood))
            {
                pulseJump = true;
                nextActionTime = Time.time + model.minActionSpacingSeconds;
            }
        }

        private void QueueBestAttack(float absoluteDistance, bool punishWindow)
        {
            float rangePressure = Mathf.Clamp01(1f - (absoluteDistance / Mathf.Max(0.1f, model.threatRange)));
            float lightWeight = Mathf.Max(0.1f, 1.2f + (model.comboBias * 0.5f) - (model.heavyAttackBias * 0.4f));
            float heavyWeight = 0.15f + model.heavyAttackBias + (punishWindow ? model.punishBias * 0.4f : 0f);
            float specialWeight = 0.1f + model.specialAttackBias + (rangePressure * 0.25f);

            float roll = Random.value * (lightWeight + heavyWeight + specialWeight);
            if (roll < lightWeight)
            {
                pulseAttack = true;
            }
            else if (roll < lightWeight + heavyWeight)
            {
                pulseSpecialHeavy = true;
            }
            else
            {
                pulseSpecialLight = true;
            }

            nextActionTime = Time.time + model.minActionSpacingSeconds + Random.Range(0.03f, 0.08f);
        }

        private bool IsOpponentThreatening(float absoluteDistance)
        {
            if (opponentCombat == null)
                return absoluteDistance <= model.closeRange;

            return absoluteDistance <= model.threatRange
                && (opponentCombat.CurrentState == CombatState.Startup || opponentCombat.CurrentState == CombatState.Active);
        }

        private float GetDecisionDelay()
        {
            float delay = model.decisionIntervalSeconds + Random.Range(-model.randomness, model.randomness) * 0.08f;
            return Mathf.Max(0.05f, delay);
        }

        private void ApplyCommands(bool blockHeld)
        {
            if (platformController == null)
                return;

            EnsureSkillBuffer();

            platformController.netLeft = currentMoveIntent < -0.1f;
            platformController.netRight = currentMoveIntent > 0.1f;
            platformController.netCrouch = currentCrouch;
            platformController.netSprint = false;
            platformController.netBlock = blockHeld;
            platformController.netBlockDown = !previousBlockHeld && blockHeld;
            platformController.netBlockUp = previousBlockHeld && !blockHeld;
            platformController.netJump = pulseJump;
            platformController.netAttack = pulseAttack;
            platformController.netDodge = pulseDodge;
            platformController.netSkills[0] = pulseSpecialLight;
            platformController.netSkills[1] = pulseSpecialHeavy;

            for (int i = 2; i < platformController.netSkills.Length; i++)
                platformController.netSkills[i] = false;

            previousBlockHeld = blockHeld;
            pulseJump = false;
            pulseAttack = false;
            pulseDodge = false;
            pulseSpecialLight = false;
            pulseSpecialHeavy = false;
        }

        private void ClearNetworkState()
        {
            if (platformController == null)
                return;

            EnsureSkillBuffer();

            platformController.netLeft = false;
            platformController.netRight = false;
            platformController.netCrouch = false;
            platformController.netSprint = false;
            platformController.netJump = false;
            platformController.netAttack = false;
            platformController.netBlock = false;
            platformController.netBlockDown = false;
            platformController.netBlockUp = false;
            platformController.netDodge = false;

            for (int i = 0; i < platformController.netSkills.Length; i++)
                platformController.netSkills[i] = false;

            previousBlockHeld = false;
        }

        private void EnsureSkillBuffer()
        {
            if (platformController.netSkills == null || platformController.netSkills.Length < 8)
                platformController.netSkills = new bool[8];
        }

        private void ResolveReferences()
        {
            if (self != null && platformController == null)
                platformController = self.GetPlayerController();

            if (self != null && selfCombat == null)
                selfCombat = self.GetComponent<CombatFSM>();

            if (self != null && evadeSystem == null)
                evadeSystem = self.GetComponent<EvadeSystem>();

            if (opponent != null && opponentCombat == null)
                opponentCombat = opponent.GetComponent<CombatFSM>();
        }

        private QuickMatchHeuristicModel LoadModelForTier(QuickMatchDifficultyTier difficultyTier)
        {
            if (QuickMatchModelStore.TryLoadChampion(difficultyTier, out QuickMatchHeuristicModel loadedModel, out _))
                return loadedModel;

            return new QuickMatchHeuristicModel
            {
                tier = difficultyTier.ToString(),
                policyId = $"{difficultyTier.ToString().ToLowerInvariant()}-fallback"
            };
        }

        private static bool Roll(float chance)
        {
            return Random.value <= Mathf.Clamp01(chance);
        }
    }
}
