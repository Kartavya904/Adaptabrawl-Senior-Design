using System;
using UnityEngine;

namespace Adaptabrawl.AI
{
    [Serializable]
    public class QuickMatchHeuristicModel
    {
        public int schemaVersion = 1;
        public string tier = "Trainer";
        public string policyId = "";
        public string source = "generated";
        public int mutationGeneration;

        [Header("Cadence")]
        public float decisionIntervalSeconds = 0.2f;
        public float reactionSeconds = 0.2f;
        public float minActionSpacingSeconds = 0.18f;

        [Header("Spacing")]
        public float optimalRange = 1.7f;
        public float rangeTolerance = 0.45f;
        public float closeRange = 1.1f;
        public float threatRange = 1.75f;

        [Header("Temperament")]
        public float aggression = 0.55f;
        public float approachBias = 0.6f;
        public float retreatBias = 0.2f;
        public float pressureBias = 0.45f;
        public float punishBias = 0.35f;
        public float lowHealthDefenseBias = 0.4f;
        public float randomness = 0.18f;

        [Header("Defence")]
        public float blockLikelihood = 0.35f;
        public float dodgeLikelihood = 0.25f;
        public float minBlockHoldSeconds = 0.15f;
        public float maxBlockHoldSeconds = 0.4f;

        [Header("Actions")]
        public float heavyAttackBias = 0.25f;
        public float specialAttackBias = 0.2f;
        public float jumpLikelihood = 0.14f;
        public float crouchLikelihood = 0.08f;
        public float comboBias = 0.42f;

        public QuickMatchHeuristicModel Clone()
        {
            return new QuickMatchHeuristicModel
            {
                schemaVersion = schemaVersion,
                tier = tier,
                policyId = policyId,
                source = source,
                mutationGeneration = mutationGeneration,
                decisionIntervalSeconds = decisionIntervalSeconds,
                reactionSeconds = reactionSeconds,
                minActionSpacingSeconds = minActionSpacingSeconds,
                optimalRange = optimalRange,
                rangeTolerance = rangeTolerance,
                closeRange = closeRange,
                threatRange = threatRange,
                aggression = aggression,
                approachBias = approachBias,
                retreatBias = retreatBias,
                pressureBias = pressureBias,
                punishBias = punishBias,
                lowHealthDefenseBias = lowHealthDefenseBias,
                randomness = randomness,
                blockLikelihood = blockLikelihood,
                dodgeLikelihood = dodgeLikelihood,
                minBlockHoldSeconds = minBlockHoldSeconds,
                maxBlockHoldSeconds = maxBlockHoldSeconds,
                heavyAttackBias = heavyAttackBias,
                specialAttackBias = specialAttackBias,
                jumpLikelihood = jumpLikelihood,
                crouchLikelihood = crouchLikelihood,
                comboBias = comboBias
            };
        }

        public void Clamp()
        {
            decisionIntervalSeconds = Mathf.Clamp(decisionIntervalSeconds, 0.08f, 0.65f);
            reactionSeconds = Mathf.Clamp(reactionSeconds, 0.04f, 0.7f);
            minActionSpacingSeconds = Mathf.Clamp(minActionSpacingSeconds, 0.08f, 0.55f);
            optimalRange = Mathf.Clamp(optimalRange, 0.85f, 3.1f);
            rangeTolerance = Mathf.Clamp(rangeTolerance, 0.15f, 1.5f);
            closeRange = Mathf.Clamp(closeRange, 0.6f, 2.4f);
            threatRange = Mathf.Clamp(threatRange, 0.8f, 3.2f);

            aggression = Mathf.Clamp01(aggression);
            approachBias = Mathf.Clamp01(approachBias);
            retreatBias = Mathf.Clamp01(retreatBias);
            pressureBias = Mathf.Clamp01(pressureBias);
            punishBias = Mathf.Clamp01(punishBias);
            lowHealthDefenseBias = Mathf.Clamp01(lowHealthDefenseBias);
            randomness = Mathf.Clamp(randomness, 0.01f, 0.9f);

            blockLikelihood = Mathf.Clamp01(blockLikelihood);
            dodgeLikelihood = Mathf.Clamp01(dodgeLikelihood);
            minBlockHoldSeconds = Mathf.Clamp(minBlockHoldSeconds, 0.05f, 1f);
            maxBlockHoldSeconds = Mathf.Clamp(maxBlockHoldSeconds, minBlockHoldSeconds, 1.5f);

            heavyAttackBias = Mathf.Clamp01(heavyAttackBias);
            specialAttackBias = Mathf.Clamp01(specialAttackBias);
            jumpLikelihood = Mathf.Clamp01(jumpLikelihood);
            crouchLikelihood = Mathf.Clamp01(crouchLikelihood);
            comboBias = Mathf.Clamp01(comboBias);
        }
    }
}
