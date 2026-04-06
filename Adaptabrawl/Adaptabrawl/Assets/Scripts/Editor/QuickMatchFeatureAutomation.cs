using Adaptabrawl.AI;
using UnityEditor;
using UnityEngine;

namespace Adaptabrawl.Editor
{
    public static class QuickMatchFeatureAutomation
    {
        [MenuItem("Tools/Adaptabrawl/Quick Match/Build Scene And Train Models")]
        public static void BuildSceneAndTrainModels()
        {
            int seed = QuickMatchModelEditorUtility.GenerateTrainingSeed();
            QuickMatchSceneBuilder.BuildOrUpdateQuickMatchScene();
            QuickMatchSceneBuilder.UpdateStartSceneForQuickMatch();
            QuickMatchTrainingReport report = QuickMatchModelEditorUtility.TrainAndExportChampionModelsInternal(seed);
            Debug.Log($"[QuickMatchFeatureAutomation] Seed={seed} | {report.BuildSummary()}");
        }

        public static void BuildSceneTrainModelsAndExit()
        {
            try
            {
                BuildSceneAndTrainModels();
                AssetDatabase.SaveAssets();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }
    }
}
