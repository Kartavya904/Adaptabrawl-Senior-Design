using Adaptabrawl.AI;
using UnityEditor;
using UnityEngine;

namespace Adaptabrawl.Editor
{
    public static class QuickMatchFeatureAutomation
    {
        [MenuItem("Adaptabrawl/Quick Match/Apply Scene Theme")]
        [MenuItem("Tools/Adaptabrawl/Quick Match/Build Scene Only")]
        public static void BuildSceneOnly()
        {
            QuickMatchSceneBuilder.BuildOrUpdateQuickMatchScene();
            QuickMatchSceneBuilder.UpdateStartSceneForQuickMatch();
            Debug.Log("[QuickMatchFeatureAutomation] Quick Match scene was rebuilt without retraining models.");
        }

        [MenuItem("Adaptabrawl/Quick Match/Build Scene And Train Models")]
        [MenuItem("Tools/Adaptabrawl/Quick Match/Build Scene And Train Models")]
        public static void BuildSceneAndTrainModels()
        {
            int seed = QuickMatchModelEditorUtility.GenerateTrainingSeed();
            BuildSceneOnly();
            QuickMatchTrainingReport report = QuickMatchModelEditorUtility.TrainAndExportChampionModelsInternal(seed);
            Debug.Log($"[QuickMatchFeatureAutomation] Seed={seed} | {report.BuildSummary()}");
        }

        public static void BuildSceneOnlyAndExit()
        {
            try
            {
                BuildSceneOnly();
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
