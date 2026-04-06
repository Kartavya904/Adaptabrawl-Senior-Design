using UnityEngine;

namespace Adaptabrawl.AI
{
    public static class QuickMatchRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureQuickMatchChampionsExist()
        {
            try
            {
                QuickMatchModelStore.EnsureModelsReady();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
