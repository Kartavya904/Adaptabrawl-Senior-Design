using System;
using Adaptabrawl.Gameplay;
using UnityEngine;

namespace Adaptabrawl.AI
{
    [CreateAssetMenu(fileName = "QuickMatchModelCatalog", menuName = "Adaptabrawl/Quick Match/Model Catalog")]
    public class QuickMatchModelCatalog : ScriptableObject
    {
        public QuickMatchTierModelDefaults dummy = new QuickMatchTierModelDefaults();
        public QuickMatchTierModelDefaults trainer = new QuickMatchTierModelDefaults();
        public QuickMatchTierModelDefaults extreme = new QuickMatchTierModelDefaults();

        public QuickMatchTierModelDefaults GetDefaults(QuickMatchDifficultyTier tier)
        {
            return tier switch
            {
                QuickMatchDifficultyTier.Dummy => dummy,
                QuickMatchDifficultyTier.Trainer => trainer,
                QuickMatchDifficultyTier.Extreme => extreme,
                _ => trainer
            };
        }
    }

    [Serializable]
    public class QuickMatchTierModelDefaults
    {
        public TextAsset modelAsset;
        public TextAsset metadataAsset;
    }
}
