using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Adaptabrawl.AI;
using Adaptabrawl.Gameplay;
using UnityEditor;
using UnityEngine;

namespace Adaptabrawl.Editor
{
    public static class QuickMatchModelEditorUtility
    {
        private const string CatalogAssetPath = "Assets/Resources/QuickMatch/QuickMatchModelCatalog.asset";
        private const string TrainingLogPath = "Assets/AI trainings/logs/quick_match_training_latest.txt";
        private const string DummyModelPath = "Assets/AI trainings/models/Dummy/dummy_model.json";
        private const string DummyMetadataPath = "Assets/AI trainings/models/Dummy/dummy_metadata.json";
        private const string TrainerModelPath = "Assets/AI trainings/models/Trainer/trainer_model.json";
        private const string TrainerMetadataPath = "Assets/AI trainings/models/Trainer/trainer_metadata.json";
        private const string ExtremeModelPath = "Assets/AI trainings/models/Extreme/extreme_model.json";
        private const string ExtremeMetadataPath = "Assets/AI trainings/models/Extreme/extreme_metadata.json";

        public static int GenerateTrainingSeed()
        {
            return unchecked((int)DateTime.UtcNow.Ticks);
        }

        [MenuItem("Tools/Adaptabrawl/Quick Match/Train And Export Champion Models")]
        public static void TrainAndExportChampionModelsMenu()
        {
            int seed = GenerateTrainingSeed();
            QuickMatchTrainingReport report = TrainAndExportChampionModelsInternal(seed);
            EditorUtility.DisplayDialog("Quick Match Models", report.BuildSummary(), "OK");
        }

        [MenuItem("Tools/Adaptabrawl/Quick Match/Export Persistent Models To Assets")]
        public static void ExportPersistentModelsToAssets()
        {
            var champions = new Dictionary<QuickMatchDifficultyTier, QuickMatchChampionRecord>();
            foreach (QuickMatchDifficultyTier tier in System.Enum.GetValues(typeof(QuickMatchDifficultyTier)))
            {
                if (!QuickMatchModelStore.TryLoadChampion(tier, out QuickMatchHeuristicModel model, out QuickMatchModelMetadata metadata))
                    continue;

                champions[tier] = new QuickMatchChampionRecord
                {
                    tier = tier,
                    model = model,
                    metadata = metadata,
                    rawScore = metadata.evaluation.value,
                    normalizedScore = metadata.evaluation.normalizedScore
                };
            }

            ExportChampionRecords(champions);
            EditorUtility.DisplayDialog("Quick Match Models", "Persistent champion overrides were exported to Assets/AI trainings/.", "OK");
        }

        public static QuickMatchTrainingReport TrainAndExportChampionModelsInternal(int seed)
        {
            QuickMatchTrainingReport report = QuickMatchTrainer.TrainChampionSet(seed, saveToPersistent: true);
            ExportChampionRecords(report.champions);
            WriteTrainingLog(report, seed);
            return report;
        }

        public static void ExportChampionRecords(IReadOnlyDictionary<QuickMatchDifficultyTier, QuickMatchChampionRecord> champions)
        {
            foreach (KeyValuePair<QuickMatchDifficultyTier, QuickMatchChampionRecord> pair in champions)
            {
                QuickMatchDifficultyTier tier = pair.Key;
                QuickMatchChampionRecord record = pair.Value;
                if (record == null || record.model == null || record.metadata == null)
                    continue;

                string modelPath = GetModelPath(tier);
                string metadataPath = GetMetadataPath(tier);
                record.metadata.assetPath = modelPath;

                WriteAssetFile(modelPath, JsonUtility.ToJson(record.model, true));
                WriteAssetFile(metadataPath, JsonUtility.ToJson(record.metadata, true));
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            EnsureCatalogAsset();
            AssetDatabase.SaveAssets();
        }

        public static void EnsureCatalogAsset()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<QuickMatchModelCatalog>(CatalogAssetPath);
            if (catalog == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ToAbsoluteAssetPath(CatalogAssetPath)));
                catalog = ScriptableObject.CreateInstance<QuickMatchModelCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogAssetPath);
            }

            catalog.dummy.modelAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(DummyModelPath);
            catalog.dummy.metadataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(DummyMetadataPath);
            catalog.trainer.modelAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(TrainerModelPath);
            catalog.trainer.metadataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(TrainerMetadataPath);
            catalog.extreme.modelAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(ExtremeModelPath);
            catalog.extreme.metadataAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(ExtremeMetadataPath);
            EditorUtility.SetDirty(catalog);
        }

        private static string GetModelPath(QuickMatchDifficultyTier tier)
        {
            return tier switch
            {
                QuickMatchDifficultyTier.Dummy => DummyModelPath,
                QuickMatchDifficultyTier.Trainer => TrainerModelPath,
                QuickMatchDifficultyTier.Extreme => ExtremeModelPath,
                _ => TrainerModelPath
            };
        }

        private static string GetMetadataPath(QuickMatchDifficultyTier tier)
        {
            return tier switch
            {
                QuickMatchDifficultyTier.Dummy => DummyMetadataPath,
                QuickMatchDifficultyTier.Trainer => TrainerMetadataPath,
                QuickMatchDifficultyTier.Extreme => ExtremeMetadataPath,
                _ => TrainerMetadataPath
            };
        }

        private static void WriteAssetFile(string assetPath, string contents)
        {
            string absolutePath = ToAbsoluteAssetPath(assetPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllText(absolutePath, contents);
        }

        private static void WriteTrainingLog(QuickMatchTrainingReport report, int seed)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Quick Match Training Log");
            builder.AppendLine($"GeneratedUtc: {DateTime.UtcNow:o}");
            builder.AppendLine($"Seed: {seed}");
            builder.AppendLine($"Summary: {report.BuildSummary()}");
            builder.AppendLine();
            builder.AppendLine("Tier Outcomes");

            foreach (QuickMatchTierTrainingOutcome outcome in report.outcomes)
            {
                builder.AppendLine($"- Tier: {outcome.tier}");
                builder.AppendLine($"  Promoted: {outcome.promoted}");
                builder.AppendLine($"  PreviousScore: {outcome.previousScore:F3}");
                builder.AppendLine($"  CandidateScore: {outcome.candidateScore:F3}");
                builder.AppendLine($"  FinalScore: {outcome.finalScore:F3}");
                builder.AppendLine($"  Notes: {outcome.notes}");
            }

            builder.AppendLine();
            builder.AppendLine("Champion Records");
            foreach (QuickMatchDifficultyTier tier in Enum.GetValues(typeof(QuickMatchDifficultyTier)))
            {
                if (!report.champions.TryGetValue(tier, out QuickMatchChampionRecord record) || record == null || record.metadata == null)
                    continue;

                builder.AppendLine($"- Tier: {tier}");
                builder.AppendLine($"  PolicyId: {record.metadata.policyId}");
                builder.AppendLine($"  AssetPath: {record.metadata.assetPath}");
                builder.AppendLine($"  RawScore: {record.rawScore:F3}");
                builder.AppendLine($"  NormalizedScore: {record.normalizedScore:F3}");
                builder.AppendLine($"  WinRateProxy: {record.metadata.evaluation.winRate:F3}");
                builder.AppendLine($"  PromotionNotes: {record.metadata.promotion.notes}");
            }

            WriteAssetFile(TrainingLogPath, builder.ToString());
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static string ToAbsoluteAssetPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }
    }
}
