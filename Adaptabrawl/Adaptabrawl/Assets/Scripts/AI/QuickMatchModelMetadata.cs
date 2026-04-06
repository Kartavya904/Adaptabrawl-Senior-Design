using System;

namespace Adaptabrawl.AI
{
    [Serializable]
    public class QuickMatchModelMetadata
    {
        public int schemaVersion = 1;
        public string tier = "";
        public string policyType = "Heuristic";
        public string assetPath = "";
        public string policyId = "";
        public string createdUtc = "";
        public QuickMatchModelTrainingInfo training = new QuickMatchModelTrainingInfo();
        public QuickMatchModelEvaluationInfo evaluation = new QuickMatchModelEvaluationInfo();
        public QuickMatchModelPromotionInfo promotion = new QuickMatchModelPromotionInfo();
    }

    [Serializable]
    public class QuickMatchModelTrainingInfo
    {
        public string gitCommit = "";
        public string trainingConfig = "heuristic_grid_search_v1";
        public int seed;
        public int steps;
    }

    [Serializable]
    public class QuickMatchModelEvaluationInfo
    {
        public string protocol = "heuristic_grid_search_v1";
        public string primaryMetric = "composite_strength_score";
        public float value;
        public float normalizedScore;
        public float winRate;
        public string confidence = "";
    }

    [Serializable]
    public class QuickMatchModelPromotionInfo
    {
        public string status = "champion";
        public string replacesPolicyId = "";
        public string notes = "";
    }
}
