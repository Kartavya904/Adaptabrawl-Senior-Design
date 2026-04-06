using System;
using System.IO;
using Adaptabrawl.Gameplay;
using UnityEngine;

namespace Adaptabrawl.AI
{
    public static class QuickMatchModelStore
    {
        private const string CatalogResourcePath = "QuickMatch/QuickMatchModelCatalog";
        private const string PersistentFolderName = "QuickMatchModels";

        private static bool defaultsEnsured;

        public static void EnsureModelsReady()
        {
            EnsureModelsReadyInternal(bootstrapIfMissing: true);
        }

        public static bool TryLoadChampion(QuickMatchDifficultyTier tier, out QuickMatchHeuristicModel model, out QuickMatchModelMetadata metadata)
        {
            return TryLoadChampion(tier, bootstrapIfMissing: true, out model, out metadata);
        }

        internal static bool TryLoadChampion(QuickMatchDifficultyTier tier, bool bootstrapIfMissing, out QuickMatchHeuristicModel model, out QuickMatchModelMetadata metadata)
        {
            EnsureModelsReadyInternal(bootstrapIfMissing);

            string persistentModelPath = GetPersistentModelPath(tier);
            string persistentMetadataPath = GetPersistentMetadataPath(tier);
            if (TryReadJson(persistentModelPath, out model) && TryReadJson(persistentMetadataPath, out metadata))
                return true;

            QuickMatchTierModelDefaults defaults = LoadCatalog()?.GetDefaults(tier);
            if (TryParseJson(defaults?.modelAsset != null ? defaults.modelAsset.text : string.Empty, out model)
                && TryParseJson(defaults?.metadataAsset != null ? defaults.metadataAsset.text : string.Empty, out metadata))
            {
                return true;
            }

            model = null;
            metadata = null;
            return false;
        }

        public static bool SaveChampion(QuickMatchDifficultyTier tier, QuickMatchHeuristicModel model, QuickMatchModelMetadata metadata)
        {
            if (model == null || metadata == null)
                return false;

            string directory = GetPersistentTierDirectory(tier);
            Directory.CreateDirectory(directory);
            File.WriteAllText(GetPersistentModelPath(tier), JsonUtility.ToJson(model, true));
            File.WriteAllText(GetPersistentMetadataPath(tier), JsonUtility.ToJson(metadata, true));
            return true;
        }

        public static string GetPersistentModelPath(QuickMatchDifficultyTier tier)
        {
            return Path.Combine(GetPersistentTierDirectory(tier), "champion_model.json");
        }

        public static string GetPersistentMetadataPath(QuickMatchDifficultyTier tier)
        {
            return Path.Combine(GetPersistentTierDirectory(tier), "champion_metadata.json");
        }

        private static string GetPersistentTierDirectory(QuickMatchDifficultyTier tier)
        {
            return Path.Combine(Application.persistentDataPath, PersistentFolderName, tier.ToString());
        }

        private static void EnsureModelsReadyInternal(bool bootstrapIfMissing)
        {
            if (defaultsEnsured)
                return;

            QuickMatchModelCatalog catalog = LoadCatalog();
            if (catalog != null)
            {
                CopyDefaultTierIfMissing(QuickMatchDifficultyTier.Dummy, catalog.dummy);
                CopyDefaultTierIfMissing(QuickMatchDifficultyTier.Trainer, catalog.trainer);
                CopyDefaultTierIfMissing(QuickMatchDifficultyTier.Extreme, catalog.extreme);
                defaultsEnsured = true;
                return;
            }

            if (!bootstrapIfMissing)
                return;

            Debug.LogWarning("[QuickMatchModelStore] No QuickMatchModelCatalog was found. Bootstrapping heuristic champions locally.");
            QuickMatchTrainer.TrainChampionSet(seed: Environment.TickCount, saveToPersistent: true);
            defaultsEnsured = true;
        }

        private static void CopyDefaultTierIfMissing(QuickMatchDifficultyTier tier, QuickMatchTierModelDefaults defaults)
        {
            if (defaults == null || defaults.modelAsset == null || defaults.metadataAsset == null)
                return;

            string directory = GetPersistentTierDirectory(tier);
            Directory.CreateDirectory(directory);

            string modelPath = GetPersistentModelPath(tier);
            if (!File.Exists(modelPath))
                File.WriteAllText(modelPath, defaults.modelAsset.text);

            string metadataPath = GetPersistentMetadataPath(tier);
            if (!File.Exists(metadataPath))
                File.WriteAllText(metadataPath, defaults.metadataAsset.text);
        }

        private static QuickMatchModelCatalog LoadCatalog()
        {
            return Resources.Load<QuickMatchModelCatalog>(CatalogResourcePath);
        }

        private static bool TryReadJson<T>(string path, out T value) where T : class
        {
            if (!File.Exists(path))
            {
                value = null;
                return false;
            }

            return TryParseJson(File.ReadAllText(path), out value);
        }

        private static bool TryParseJson<T>(string raw, out T value) where T : class
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                value = null;
                return false;
            }

            try
            {
                value = JsonUtility.FromJson<T>(raw);
                return value != null;
            }
            catch
            {
                value = null;
                return false;
            }
        }
    }
}
