using System;
using UnityEngine;

namespace Adaptabrawl.Settings
{
    /// <summary>
    /// Persistent DontDestroyOnLoad holder for current settings values.
    /// Other systems can read this context without depending on the Settings scene.
    /// </summary>
    public class SettingsContext : MonoBehaviour
    {
        public static SettingsContext Instance { get; private set; }

        [Header("Audio")]
        public float masterVolume = 1f;
        public float musicVolume = 0.7f;
        public float sfxVolume = 1f;

        [Header("Video")]
        public int targetFPS = 60;
        public bool vsyncEnabled = true;
        public int qualityLevel = 2;
        public FullScreenMode displayMode = FullScreenMode.FullScreenWindow;

        [Header("Accessibility")]
        public float uiScale = 1f;
        public bool colorBlindMode;
        public bool showHitboxes;

        /// <summary>
        /// Raised whenever any settings value in this context changes.
        /// </summary>
        public event Action SettingsChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureExists();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static SettingsContext EnsureExists()
        {
            if (Instance != null)
                return Instance;

            var existing = FindFirstObjectByType<SettingsContext>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var go = new GameObject("SettingsContext");
            return go.AddComponent<SettingsContext>();
        }

        public void SetValues(
            float newMasterVolume,
            float newMusicVolume,
            float newSfxVolume,
            int newTargetFps,
            bool newVsyncEnabled,
            int newQualityLevel,
            FullScreenMode newDisplayMode,
            float newUiScale,
            bool newColorBlindMode,
            bool newShowHitboxes)
        {
            int clampedQuality = Mathf.Clamp(newQualityLevel, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            float clampedUiScale = Mathf.Clamp(newUiScale, SettingsManager.MinUIScale, SettingsManager.MaxUIScale);
            FullScreenMode normalizedDisplayMode = NormalizeDisplayMode(newDisplayMode);

            bool changed =
                !Mathf.Approximately(masterVolume, newMasterVolume) ||
                !Mathf.Approximately(musicVolume, newMusicVolume) ||
                !Mathf.Approximately(sfxVolume, newSfxVolume) ||
                targetFPS != newTargetFps ||
                vsyncEnabled != newVsyncEnabled ||
                qualityLevel != clampedQuality ||
                displayMode != normalizedDisplayMode ||
                !Mathf.Approximately(uiScale, clampedUiScale) ||
                colorBlindMode != newColorBlindMode ||
                showHitboxes != newShowHitboxes;

            masterVolume = Mathf.Clamp01(newMasterVolume);
            musicVolume = Mathf.Clamp01(newMusicVolume);
            sfxVolume = Mathf.Clamp01(newSfxVolume);
            targetFPS = newTargetFps;
            vsyncEnabled = newVsyncEnabled;
            qualityLevel = clampedQuality;
            displayMode = normalizedDisplayMode;
            uiScale = clampedUiScale;
            colorBlindMode = newColorBlindMode;
            showHitboxes = newShowHitboxes;

            if (changed)
                SettingsChanged?.Invoke();
        }

        private static FullScreenMode NormalizeDisplayMode(FullScreenMode mode)
        {
            return mode switch
            {
                FullScreenMode.ExclusiveFullScreen => FullScreenMode.ExclusiveFullScreen,
                FullScreenMode.FullScreenWindow => FullScreenMode.FullScreenWindow,
                FullScreenMode.Windowed => FullScreenMode.Windowed,
                _ => FullScreenMode.FullScreenWindow
            };
        }
    }
}
