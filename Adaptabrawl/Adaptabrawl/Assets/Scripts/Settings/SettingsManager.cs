using System.Collections.Generic;
using Adaptabrawl.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Adaptabrawl.Settings
{
    public class SettingsManager : MonoBehaviour
    {
        public const float MinUIScale = 0.9f;
        public const float MaxUIScale = 1.1f;

        [Header("Input Settings")]
        [SerializeField] private InputActionAsset inputActions;
        
        [Header("Audio Settings")]
        [SerializeField] private float masterVolume = 1f;
        [SerializeField] private float musicVolume = 0.7f;
        [SerializeField] private float sfxVolume = 1f;
        
        [Header("Video Settings")]
        [SerializeField] private int targetFPS = 60;
        [SerializeField] private bool vsyncEnabled = true;
        [SerializeField] private int qualityLevel = 2;
        [SerializeField] private FullScreenMode displayMode = FullScreenMode.FullScreenWindow;
        
        [Header("Accessibility")]
        [SerializeField] private float uiScale = 1f;
        [SerializeField] private bool colorBlindMode = false;
        [SerializeField] private bool showHitboxes = false;

        private readonly Dictionary<int, Vector2> baseReferenceResolutions = new Dictionary<int, Vector2>();
        private readonly Dictionary<int, float> baseScaleFactors = new Dictionary<int, float>();
        
        private static SettingsManager instance;
        public static SettingsManager Instance => instance;
        public static float SFXVolumeScale { get; private set; } = 1f;

        public static SettingsManager EnsureExists()
        {
            if (instance != null)
                return instance;

            var existing = FindFirstObjectByType<SettingsManager>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var host = new GameObject("SettingsManagerContext");
            return host.AddComponent<SettingsManager>();
        }
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }

            // If SettingsManager is placed on the same scene object as SettingsUI,
            // spin up a dedicated persistent host so the UI object can stay scene-local.
            if (instance == null && GetComponent<SettingsUI>() != null)
            {
                SpawnPersistentManagerHost();
                Destroy(this);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
            SyncSettingsContext();
        }

        private static void SpawnPersistentManagerHost()
        {
            if (instance != null)
                return;

            var host = new GameObject("SettingsManagerContext");
            host.AddComponent<SettingsManager>();
        }
        
        private void Start()
        {
            if (instance != this)
                return;

            ApplySettings();
        }

        private void OnEnable()
        {
            if (instance == this)
                SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            if (instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }

        private void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            ApplyAccessibilitySettings();
        }
        
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            SaveSettings();
            ApplyAudioSettings();
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            SaveSettings();
            ApplyAudioSettings();
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            SaveSettings();
            ApplyAudioSettings();
        }
        
        public void SetTargetFPS(int fps)
        {
            targetFPS = fps;
            SaveSettings();
            ApplyVideoSettings();
        }
        
        public void SetVSync(bool enabled)
        {
            vsyncEnabled = enabled;
            SaveSettings();
            ApplyVideoSettings();
        }
        
        public void SetQualityLevel(int level)
        {
            qualityLevel = Mathf.Clamp(level, 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(qualityLevel);
            SaveSettings();
        }

        public void SetDisplayMode(FullScreenMode mode)
        {
            displayMode = NormalizeDisplayMode(mode);
            SaveSettings();
            ApplyVideoSettings();
        }
        
        public void SetUIScale(float scale)
        {
            uiScale = Mathf.Clamp(scale, MinUIScale, MaxUIScale);
            SaveSettings();
            ApplyAccessibilitySettings();
        }
        
        public void SetColorBlindMode(bool enabled)
        {
            colorBlindMode = enabled;
            SaveSettings();
            ApplyAccessibilitySettings();
        }
        
        public void SetShowHitboxes(bool show)
        {
            showHitboxes = show;
            SaveSettings();
        }
        
        public void RemapInput(string actionName, InputBinding binding)
        {
            if (inputActions != null)
            {
                var action = inputActions.FindAction(actionName);
                if (action != null)
                {
                    // Remap input binding
                    // This would require more detailed implementation
                }
            }
        }
        
        private void ApplySettings()
        {
            ApplyAudioSettings();
            ApplyVideoSettings();
            ApplyAccessibilitySettings();
        }
        
        private void ApplyAudioSettings()
        {
            AudioListener.volume = masterVolume;
            SFXVolumeScale = sfxVolume;

            if (MusicManager.Instance != null)
                MusicManager.Instance.SetVolume(musicVolume);
        }
        
        private void ApplyVideoSettings()
        {
            displayMode = NormalizeDisplayMode(displayMode);
            Screen.fullScreenMode = displayMode;
            Application.targetFrameRate = targetFPS;
            QualitySettings.vSyncCount = vsyncEnabled ? 1 : 0;
            if (QualitySettings.names.Length > 0)
                QualitySettings.SetQualityLevel(qualityLevel);
        }
        
        private void ApplyAccessibilitySettings()
        {
            ApplyUIScaleToCanvases();
            // Color blind mode post-process pipeline is not wired yet.
        }

        private void ApplyUIScaleToCanvases()
        {
            float scale = Mathf.Clamp(uiScale, MinUIScale, MaxUIScale);
            var scalers = FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var scaler in scalers)
            {
                if (scaler == null) continue;

                int id = scaler.GetInstanceID();

                if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    if (!baseReferenceResolutions.ContainsKey(id))
                        baseReferenceResolutions[id] = scaler.referenceResolution;

                    Vector2 baseRef = baseReferenceResolutions[id];
                    if (baseRef.x <= 0f || baseRef.y <= 0f)
                        baseRef = new Vector2(1920f, 1080f);

                    scaler.referenceResolution = baseRef / scale;
                }
                else if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
                {
                    if (!baseScaleFactors.ContainsKey(id))
                        baseScaleFactors[id] = scaler.scaleFactor;

                    scaler.scaleFactor = baseScaleFactors[id] * scale;
                }
            }
        }
        
        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetInt("TargetFPS", targetFPS);
            PlayerPrefs.SetInt("VSync", vsyncEnabled ? 1 : 0);
            PlayerPrefs.SetInt("QualityLevel", qualityLevel);
            PlayerPrefs.SetInt("DisplayMode", (int)displayMode);
            PlayerPrefs.SetFloat("UIScale", uiScale);
            PlayerPrefs.SetInt("ColorBlindMode", colorBlindMode ? 1 : 0);
            PlayerPrefs.SetInt("ShowHitboxes", showHitboxes ? 1 : 0);
            PlayerPrefs.Save();
            SyncSettingsContext();
        }
        
        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            targetFPS = PlayerPrefs.GetInt("TargetFPS", 60);
            vsyncEnabled = PlayerPrefs.GetInt("VSync", 1) == 1;
            qualityLevel = Mathf.Clamp(PlayerPrefs.GetInt("QualityLevel", 2), 0, Mathf.Max(0, QualitySettings.names.Length - 1));
            displayMode = NormalizeDisplayMode((FullScreenMode)PlayerPrefs.GetInt("DisplayMode", (int)FullScreenMode.FullScreenWindow));
            uiScale = Mathf.Clamp(PlayerPrefs.GetFloat("UIScale", 1f), MinUIScale, MaxUIScale);
            colorBlindMode = PlayerPrefs.GetInt("ColorBlindMode", 0) == 1;
            showHitboxes = PlayerPrefs.GetInt("ShowHitboxes", 0) == 1;
        }

        private void SyncSettingsContext()
        {
            SettingsContext.EnsureExists().SetValues(
                masterVolume,
                musicVolume,
                sfxVolume,
                targetFPS,
                vsyncEnabled,
                qualityLevel,
                displayMode,
                uiScale,
                colorBlindMode,
                showHitboxes);
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
        
        // Public getters
        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;
        public int TargetFPS => targetFPS;
        public bool VSyncEnabled => vsyncEnabled;
        public int QualityLevel => qualityLevel;
        public FullScreenMode DisplayMode => displayMode;
        public float UIScale => uiScale;
        public bool ColorBlindMode => colorBlindMode;
        public bool ShowHitboxes => showHitboxes;
    }
}
