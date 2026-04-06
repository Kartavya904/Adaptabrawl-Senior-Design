using System;
using System.Collections.Generic;
using System.Linq;
using Adaptabrawl.Gameplay;
using UnityEngine;

namespace Adaptabrawl.AI
{
    public sealed class QuickMatchTrainingReport
    {
        public readonly Dictionary<QuickMatchDifficultyTier, QuickMatchChampionRecord> champions = new Dictionary<QuickMatchDifficultyTier, QuickMatchChampionRecord>();
        public readonly List<QuickMatchTierTrainingOutcome> outcomes = new List<QuickMatchTierTrainingOutcome>();

        public string BuildSummary()
        {
            if (outcomes.Count == 0)
                return "No model changes were made.";

            return string.Join(" | ", outcomes.Select(outcome =>
                $"{outcome.tier}: {(outcome.promoted ? "updated" : "kept")} {outcome.previousScore:F1}->{outcome.finalScore:F1}"));
        }
    }

    public sealed class QuickMatchTierTrainingOutcome
    {
        public QuickMatchDifficultyTier tier;
        public bool promoted;
        public float previousScore;
        public float candidateScore;
        public float finalScore;
        public string notes;
    }

    public sealed class QuickMatchChampionRecord
    {
        public QuickMatchDifficultyTier tier;
        public QuickMatchHeuristicModel model;
        public QuickMatchModelMetadata metadata;
        public float rawScore;
        public float normalizedScore;
    }

    internal sealed class QuickMatchCandidateScore
    {
        public QuickMatchHeuristicModel model;
        public float rawScore;
        public float normalizedScore;
    }

    public static class QuickMatchTrainer
    {
        private const float PromotionMargin = 1f;
        private const int CandidatesPerSeed = 48;

        public static QuickMatchTrainingReport TrainChampionSet(int seed, bool saveToPersistent)
        {
            var rng = new System.Random(seed == 0 ? Environment.TickCount : seed);
            var candidates = BuildCandidateBatch(rng);
            ScoreCandidates(candidates);

            var report = new QuickMatchTrainingReport();
            PromoteTierChampion(QuickMatchDifficultyTier.Extreme, SelectExtremeChampion(candidates), seed, saveToPersistent, report);
            PromoteTierChampion(QuickMatchDifficultyTier.Trainer, SelectAnchorChampion(candidates, 0.5f), seed, saveToPersistent, report);
            PromoteTierChampion(QuickMatchDifficultyTier.Dummy, SelectAnchorChampion(candidates, 0.125f), seed, saveToPersistent, report);
            return report;
        }

        public static Dictionary<QuickMatchDifficultyTier, QuickMatchChampionRecord> BuildSeedChampionSet(int seed)
        {
            var report = TrainChampionSet(seed, saveToPersistent: false);
            return report.champions;
        }

        private static List<QuickMatchCandidateScore> BuildCandidateBatch(System.Random rng)
        {
            var candidates = new List<QuickMatchCandidateScore>();
            var seeds = new[]
            {
                CreateTierSeed(QuickMatchDifficultyTier.Dummy),
                CreateTierSeed(QuickMatchDifficultyTier.Trainer),
                CreateTierSeed(QuickMatchDifficultyTier.Extreme)
            };

            foreach (QuickMatchHeuristicModel seedModel in seeds)
            {
                candidates.Add(new QuickMatchCandidateScore { model = seedModel.Clone() });
                candidates.Add(new QuickMatchCandidateScore { model = Mutate(seedModel, rng, 0.15f) });

                for (int i = 0; i < CandidatesPerSeed; i++)
                {
                    float scale = Mathf.Lerp(0.12f, 0.42f, (float)i / Mathf.Max(1, CandidatesPerSeed - 1));
                    candidates.Add(new QuickMatchCandidateScore { model = Mutate(seedModel, rng, scale) });
                }
            }

            return candidates;
        }

        private static void ScoreCandidates(List<QuickMatchCandidateScore> candidates)
        {
            foreach (QuickMatchCandidateScore candidate in candidates)
                candidate.rawScore = EvaluateRawScore(candidate.model);

            float min = candidates.Min(candidate => candidate.rawScore);
            float max = candidates.Max(candidate => candidate.rawScore);
            float range = Mathf.Max(0.0001f, max - min);

            foreach (QuickMatchCandidateScore candidate in candidates)
                candidate.normalizedScore = Mathf.Clamp01((candidate.rawScore - min) / range);
        }

        private static QuickMatchCandidateScore SelectExtremeChampion(List<QuickMatchCandidateScore> candidates)
        {
            float percentileFloor = GetPercentile(candidates.Select(candidate => candidate.normalizedScore), 0.75f);
            return candidates
                .Where(candidate => candidate.normalizedScore >= percentileFloor)
                .OrderByDescending(candidate => candidate.normalizedScore)
                .ThenByDescending(candidate => candidate.rawScore)
                .First();
        }

        private static QuickMatchCandidateScore SelectAnchorChampion(List<QuickMatchCandidateScore> candidates, float target)
        {
            return candidates
                .OrderBy(candidate => Mathf.Abs(candidate.normalizedScore - target))
                .ThenByDescending(candidate => candidate.rawScore)
                .First();
        }

        private static void PromoteTierChampion(
            QuickMatchDifficultyTier tier,
            QuickMatchCandidateScore candidate,
            int seed,
            bool saveToPersistent,
            QuickMatchTrainingReport report)
        {
            candidate.model.tier = tier.ToString();
            candidate.model.Clamp();

            bool hasExisting = QuickMatchModelStore.TryLoadChampion(tier, bootstrapIfMissing: false, out QuickMatchHeuristicModel existingModel, out QuickMatchModelMetadata existingMetadata);
            float existingScore = hasExisting && existingMetadata != null ? existingMetadata.evaluation.value : float.MinValue;
            bool promote = !hasExisting || candidate.rawScore > existingScore + PromotionMargin;

            var finalRecord = promote
                ? BuildChampionRecord(tier, candidate, seed, existingMetadata != null ? existingMetadata.policyId : string.Empty)
                : new QuickMatchChampionRecord
                {
                    tier = tier,
                    model = existingModel,
                    metadata = existingMetadata,
                    rawScore = existingMetadata != null ? existingMetadata.evaluation.value : candidate.rawScore,
                    normalizedScore = existingMetadata != null ? existingMetadata.evaluation.normalizedScore : candidate.normalizedScore
                };

            if (promote && saveToPersistent)
                QuickMatchModelStore.SaveChampion(tier, finalRecord.model, finalRecord.metadata);

            report.champions[tier] = finalRecord;
            report.outcomes.Add(new QuickMatchTierTrainingOutcome
            {
                tier = tier,
                promoted = promote,
                previousScore = hasExisting ? existingScore : 0f,
                candidateScore = candidate.rawScore,
                finalScore = finalRecord.rawScore,
                notes = promote
                    ? $"Promoted {finalRecord.metadata.policyId}"
                    : $"Retained {existingMetadata?.policyId ?? "existing champion"}"
            });
        }

        private static QuickMatchChampionRecord BuildChampionRecord(
            QuickMatchDifficultyTier tier,
            QuickMatchCandidateScore candidate,
            int seed,
            string replacesPolicyId)
        {
            string policyId = $"{tier.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}";
            candidate.model.policyId = policyId;
            candidate.model.source = "trained";

            return new QuickMatchChampionRecord
            {
                tier = tier,
                model = candidate.model.Clone(),
                rawScore = candidate.rawScore,
                normalizedScore = candidate.normalizedScore,
                metadata = new QuickMatchModelMetadata
                {
                    tier = tier.ToString(),
                    policyId = policyId,
                    createdUtc = DateTime.UtcNow.ToString("o"),
                    training = new QuickMatchModelTrainingInfo
                    {
                        trainingConfig = "heuristic_grid_search_v1",
                        seed = seed,
                        steps = CandidatesPerSeed * 3
                    },
                    evaluation = new QuickMatchModelEvaluationInfo
                    {
                        protocol = "heuristic_grid_search_v1",
                        primaryMetric = "composite_strength_score",
                        value = candidate.rawScore,
                        normalizedScore = candidate.normalizedScore,
                        winRate = Mathf.Lerp(0.18f, 0.92f, candidate.normalizedScore),
                        confidence = $"Deterministic heuristic tuning score; normalized={candidate.normalizedScore:F3}"
                    },
                    promotion = new QuickMatchModelPromotionInfo
                    {
                        status = "champion",
                        replacesPolicyId = replacesPolicyId,
                        notes = $"Tier assignment followed normalized anchors for {tier}."
                    }
                }
            };
        }

        private static QuickMatchHeuristicModel CreateTierSeed(QuickMatchDifficultyTier tier)
        {
            var model = new QuickMatchHeuristicModel
            {
                tier = tier.ToString(),
                policyId = $"{tier.ToString().ToLowerInvariant()}-seed",
                source = "seed"
            };

            switch (tier)
            {
                case QuickMatchDifficultyTier.Dummy:
                    model.decisionIntervalSeconds = 0.34f;
                    model.reactionSeconds = 0.38f;
                    model.minActionSpacingSeconds = 0.32f;
                    model.optimalRange = 1.9f;
                    model.rangeTolerance = 0.65f;
                    model.closeRange = 0.95f;
                    model.threatRange = 1.3f;
                    model.aggression = 0.22f;
                    model.approachBias = 0.36f;
                    model.retreatBias = 0.18f;
                    model.pressureBias = 0.18f;
                    model.punishBias = 0.12f;
                    model.lowHealthDefenseBias = 0.2f;
                    model.randomness = 0.46f;
                    model.blockLikelihood = 0.22f;
                    model.dodgeLikelihood = 0.12f;
                    model.minBlockHoldSeconds = 0.1f;
                    model.maxBlockHoldSeconds = 0.22f;
                    model.heavyAttackBias = 0.08f;
                    model.specialAttackBias = 0.05f;
                    model.jumpLikelihood = 0.08f;
                    model.crouchLikelihood = 0.05f;
                    model.comboBias = 0.1f;
                    break;

                case QuickMatchDifficultyTier.Trainer:
                    model.decisionIntervalSeconds = 0.2f;
                    model.reactionSeconds = 0.24f;
                    model.minActionSpacingSeconds = 0.18f;
                    model.optimalRange = 1.65f;
                    model.rangeTolerance = 0.48f;
                    model.closeRange = 1.05f;
                    model.threatRange = 1.7f;
                    model.aggression = 0.55f;
                    model.approachBias = 0.58f;
                    model.retreatBias = 0.26f;
                    model.pressureBias = 0.5f;
                    model.punishBias = 0.42f;
                    model.lowHealthDefenseBias = 0.5f;
                    model.randomness = 0.18f;
                    model.blockLikelihood = 0.45f;
                    model.dodgeLikelihood = 0.28f;
                    model.minBlockHoldSeconds = 0.14f;
                    model.maxBlockHoldSeconds = 0.38f;
                    model.heavyAttackBias = 0.2f;
                    model.specialAttackBias = 0.17f;
                    model.jumpLikelihood = 0.13f;
                    model.crouchLikelihood = 0.08f;
                    model.comboBias = 0.38f;
                    break;

                case QuickMatchDifficultyTier.Extreme:
                    model.decisionIntervalSeconds = 0.1f;
                    model.reactionSeconds = 0.08f;
                    model.minActionSpacingSeconds = 0.1f;
                    model.optimalRange = 1.52f;
                    model.rangeTolerance = 0.32f;
                    model.closeRange = 1f;
                    model.threatRange = 2.15f;
                    model.aggression = 0.82f;
                    model.approachBias = 0.78f;
                    model.retreatBias = 0.16f;
                    model.pressureBias = 0.72f;
                    model.punishBias = 0.84f;
                    model.lowHealthDefenseBias = 0.64f;
                    model.randomness = 0.06f;
                    model.blockLikelihood = 0.58f;
                    model.dodgeLikelihood = 0.46f;
                    model.minBlockHoldSeconds = 0.18f;
                    model.maxBlockHoldSeconds = 0.44f;
                    model.heavyAttackBias = 0.28f;
                    model.specialAttackBias = 0.3f;
                    model.jumpLikelihood = 0.16f;
                    model.crouchLikelihood = 0.11f;
                    model.comboBias = 0.72f;
                    break;
            }

            model.Clamp();
            return model;
        }

        private static QuickMatchHeuristicModel Mutate(QuickMatchHeuristicModel source, System.Random rng, float scale)
        {
            var model = source.Clone();
            model.mutationGeneration++;

            model.decisionIntervalSeconds += Signed(rng) * 0.12f * scale;
            model.reactionSeconds += Signed(rng) * 0.14f * scale;
            model.minActionSpacingSeconds += Signed(rng) * 0.1f * scale;
            model.optimalRange += Signed(rng) * 0.55f * scale;
            model.rangeTolerance += Signed(rng) * 0.35f * scale;
            model.closeRange += Signed(rng) * 0.3f * scale;
            model.threatRange += Signed(rng) * 0.65f * scale;

            model.aggression += Signed(rng) * scale;
            model.approachBias += Signed(rng) * scale;
            model.retreatBias += Signed(rng) * scale;
            model.pressureBias += Signed(rng) * scale;
            model.punishBias += Signed(rng) * scale;
            model.lowHealthDefenseBias += Signed(rng) * scale;
            model.randomness += Signed(rng) * scale;

            model.blockLikelihood += Signed(rng) * scale;
            model.dodgeLikelihood += Signed(rng) * scale;
            model.minBlockHoldSeconds += Signed(rng) * 0.25f * scale;
            model.maxBlockHoldSeconds += Signed(rng) * 0.35f * scale;

            model.heavyAttackBias += Signed(rng) * scale;
            model.specialAttackBias += Signed(rng) * scale;
            model.jumpLikelihood += Signed(rng) * scale;
            model.crouchLikelihood += Signed(rng) * scale;
            model.comboBias += Signed(rng) * scale;

            model.Clamp();
            return model;
        }

        private static float EvaluateRawScore(QuickMatchHeuristicModel model)
        {
            model.Clamp();

            float cadence = ScoreCadence(model);
            float spacing = ScoreSpacing(model);
            float offense = ScoreOffense(model);
            float defense = ScoreDefense(model);
            float stability = ScoreStability(model);

            float rawScore = (cadence * 22f)
                + (spacing * 18f)
                + (offense * 27f)
                + (defense * 23f)
                + (stability * 10f);

            return Mathf.Clamp(rawScore, 0f, 100f);
        }

        private static float ScoreCadence(QuickMatchHeuristicModel model)
        {
            float decision = 1f - Mathf.InverseLerp(0.08f, 0.65f, model.decisionIntervalSeconds);
            float reaction = 1f - Mathf.InverseLerp(0.04f, 0.7f, model.reactionSeconds);
            float actionGap = 1f - Mathf.InverseLerp(0.08f, 0.55f, model.minActionSpacingSeconds);
            return Mathf.Clamp01((decision * 0.45f) + (reaction * 0.35f) + (actionGap * 0.2f));
        }

        private static float ScoreSpacing(QuickMatchHeuristicModel model)
        {
            float optimalRangeScore = 1f - Mathf.Abs(model.optimalRange - 1.65f) / 1.65f;
            float toleranceScore = 1f - Mathf.Abs(model.rangeTolerance - 0.45f) / 0.9f;
            float threatScore = 1f - Mathf.Abs(model.threatRange - 1.85f) / 1.85f;
            float closeScore = 1f - Mathf.Abs(model.closeRange - 1.0f) / 1.0f;
            return Mathf.Clamp01((optimalRangeScore * 0.35f) + (toleranceScore * 0.25f) + (threatScore * 0.25f) + (closeScore * 0.15f));
        }

        private static float ScoreOffense(QuickMatchHeuristicModel model)
        {
            float attackVariety = Mathf.Clamp01(1f - Mathf.Abs(model.heavyAttackBias - model.specialAttackBias));
            return Mathf.Clamp01(
                (model.aggression * 0.26f)
                + (model.approachBias * 0.12f)
                + (model.pressureBias * 0.18f)
                + (model.punishBias * 0.18f)
                + (model.heavyAttackBias * 0.08f)
                + (model.specialAttackBias * 0.08f)
                + (model.comboBias * 0.07f)
                + (attackVariety * 0.03f));
        }

        private static float ScoreDefense(QuickMatchHeuristicModel model)
        {
            float blockHoldScore = 1f - Mathf.Abs(((model.minBlockHoldSeconds + model.maxBlockHoldSeconds) * 0.5f) - 0.28f) / 0.6f;
            return Mathf.Clamp01(
                (model.blockLikelihood * 0.32f)
                + (model.dodgeLikelihood * 0.24f)
                + (model.retreatBias * 0.12f)
                + (model.lowHealthDefenseBias * 0.2f)
                + (blockHoldScore * 0.12f));
        }

        private static float ScoreStability(QuickMatchHeuristicModel model)
        {
            float randomnessPenalty = Mathf.Clamp01(1f - Mathf.Abs(model.randomness - 0.18f) / 0.72f);
            float jumpScore = Mathf.Clamp01(1f - Mathf.Abs(model.jumpLikelihood - 0.14f) / 0.4f);
            float crouchScore = Mathf.Clamp01(1f - Mathf.Abs(model.crouchLikelihood - 0.08f) / 0.35f);
            return Mathf.Clamp01((randomnessPenalty * 0.5f) + (jumpScore * 0.25f) + (crouchScore * 0.25f));
        }

        private static float GetPercentile(IEnumerable<float> values, float percentile)
        {
            float[] ordered = values.OrderBy(value => value).ToArray();
            if (ordered.Length == 0)
                return 0f;

            int index = Mathf.Clamp(Mathf.CeilToInt(percentile * ordered.Length) - 1, 0, ordered.Length - 1);
            return ordered[index];
        }

        private static float Signed(System.Random rng)
        {
            return ((float)rng.NextDouble() * 2f) - 1f;
        }
    }
}
