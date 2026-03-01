using UnityEngine;
using UnityEngine.InputSystem;

namespace Adaptabrawl.Settings
{
    public class SettingsManager : MonoBehaviour
    {
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
        
        [Header("Accessibility")]
        [SerializeField] private float uiScale = 1f;
        [SerializeField] private bool colorBlindMode = false;
        [SerializeField] private bool showHitboxes = false;
        
        private static SettingsManager instance;
        public static SettingsManager Instance => instance;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSettings();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            ApplySettings();
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
            qualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(qualityLevel);
            SaveSettings();
        }
        
        public void SetUIScale(float scale)
        {
            uiScale = Mathf.Clamp(scale, 0.5f, 2f);
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
            // Apply music and SFX volumes to respective audio sources
        }
        
        private void ApplyVideoSettings()
        {
            Application.targetFrameRate = targetFPS;
            QualitySettings.vSyncCount = vsyncEnabled ? 1 : 0;
            QualitySettings.SetQualityLevel(qualityLevel);
        }
        
        private void ApplyAccessibilitySettings()
        {
            // Apply UI scale
            // Apply color blind mode
        }
        
        private void SaveSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetInt("TargetFPS", targetFPS);
            PlayerPrefs.SetInt("VSync", vsyncEnabled ? 1 : 0);
            PlayerPrefs.SetInt("QualityLevel", qualityLevel);
            PlayerPrefs.SetFloat("UIScale", uiScale);
            PlayerPrefs.SetInt("ColorBlindMode", colorBlindMode ? 1 : 0);
            PlayerPrefs.SetInt("ShowHitboxes", showHitboxes ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        private void LoadSettings()
        {
            masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
            sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
            targetFPS = PlayerPrefs.GetInt("TargetFPS", 60);
            vsyncEnabled = PlayerPrefs.GetInt("VSync", 1) == 1;
            qualityLevel = PlayerPrefs.GetInt("QualityLevel", 2);
            uiScale = PlayerPrefs.GetFloat("UIScale", 1f);
            colorBlindMode = PlayerPrefs.GetInt("ColorBlindMode", 0) == 1;
            showHitboxes = PlayerPrefs.GetInt("ShowHitboxes", 0) == 1;
        }
        
        // Public getters
        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;
        public int TargetFPS => targetFPS;
        public bool VSyncEnabled => vsyncEnabled;
        public int QualityLevel => qualityLevel;
        public float UIScale => uiScale;
        public bool ColorBlindMode => colorBlindMode;
        public bool ShowHitboxes => showHitboxes;
    }
}

