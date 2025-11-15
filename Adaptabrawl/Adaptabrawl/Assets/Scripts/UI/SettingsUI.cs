using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Adaptabrawl.Settings;
using System.Collections.Generic;

namespace Adaptabrawl.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SettingsManager settingsManager;
        
        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeText;
        [SerializeField] private TextMeshProUGUI musicVolumeText;
        [SerializeField] private TextMeshProUGUI sfxVolumeText;
        
        [Header("Video Settings")]
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle vsyncToggle;
        [SerializeField] private TMP_Dropdown fpsDropdown;
        
        [Header("Accessibility")]
        [SerializeField] private Slider uiScaleSlider;
        [SerializeField] private TextMeshProUGUI uiScaleText;
        [SerializeField] private Toggle colorBlindToggle;
        [SerializeField] private Toggle showHitboxesToggle;
        
        [Header("Navigation")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        
        private Resolution[] resolutions;
        
        private void Start()
        {
            if (settingsManager == null)
                settingsManager = SettingsManager.Instance;
            
            if (settingsManager == null)
            {
                GameObject settingsObj = new GameObject("SettingsManager");
                settingsManager = settingsObj.AddComponent<SettingsManager>();
            }
            
            InitializeUI();
            SetupButtonListeners();
            LoadCurrentSettings();
        }
        
        private void InitializeUI()
        {
            // Initialize resolution dropdown
            resolutions = Screen.resolutions;
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                List<string> options = new List<string>();
                int currentResolutionIndex = 0;
                
                for (int i = 0; i < resolutions.Length; i++)
                {
                    string option = resolutions[i].width + " x " + resolutions[i].height;
                    options.Add(option);
                    
                    if (resolutions[i].width == Screen.currentResolution.width &&
                        resolutions[i].height == Screen.currentResolution.height)
                    {
                        currentResolutionIndex = i;
                    }
                }
                
                resolutionDropdown.AddOptions(options);
                resolutionDropdown.value = currentResolutionIndex;
                resolutionDropdown.RefreshShownValue();
            }
            
            // Initialize quality dropdown
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                List<string> qualityOptions = new List<string>();
                foreach (string name in QualitySettings.names)
                {
                    qualityOptions.Add(name);
                }
                qualityDropdown.AddOptions(qualityOptions);
            }
            
            // Initialize FPS dropdown
            if (fpsDropdown != null)
            {
                fpsDropdown.ClearOptions();
                List<string> fpsOptions = new List<string>
                {
                    "30", "60", "120", "144", "Unlimited"
                };
                fpsDropdown.AddOptions(fpsOptions);
            }
        }
        
        private void SetupButtonListeners()
        {
            // Audio sliders
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            
            // Video settings
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            if (vsyncToggle != null)
                vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            if (fpsDropdown != null)
                fpsDropdown.onValueChanged.AddListener(OnFPSChanged);
            
            // Accessibility
            if (uiScaleSlider != null)
                uiScaleSlider.onValueChanged.AddListener(OnUIScaleChanged);
            if (colorBlindToggle != null)
                colorBlindToggle.onValueChanged.AddListener(OnColorBlindChanged);
            if (showHitboxesToggle != null)
                showHitboxesToggle.onValueChanged.AddListener(OnShowHitboxesChanged);
            
            // Navigation buttons
            if (backButton != null)
                backButton.onClick.AddListener(GoBack);
            if (applyButton != null)
                applyButton.onClick.AddListener(ApplySettings);
            if (resetButton != null)
                resetButton.onClick.AddListener(ResetToDefaults);
        }
        
        private void LoadCurrentSettings()
        {
            if (settingsManager == null) return;
            
            // Load audio settings
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = settingsManager.MasterVolume;
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = settingsManager.MusicVolume;
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = settingsManager.SFXVolume;
            
            // Load video settings
            if (qualityDropdown != null)
                qualityDropdown.value = settingsManager.QualityLevel;
            if (vsyncToggle != null)
                vsyncToggle.isOn = settingsManager.VSyncEnabled;
            
            // Load accessibility settings
            if (uiScaleSlider != null)
                uiScaleSlider.value = settingsManager.UIScale;
            if (colorBlindToggle != null)
                colorBlindToggle.isOn = settingsManager.ColorBlindMode;
            if (showHitboxesToggle != null)
                showHitboxesToggle.isOn = settingsManager.ShowHitboxes;
            
            UpdateTextLabels();
        }
        
        private void UpdateTextLabels()
        {
            if (masterVolumeText != null && masterVolumeSlider != null)
                masterVolumeText.text = $"Master: {Mathf.RoundToInt(masterVolumeSlider.value * 100)}%";
            if (musicVolumeText != null && musicVolumeSlider != null)
                musicVolumeText.text = $"Music: {Mathf.RoundToInt(musicVolumeSlider.value * 100)}%";
            if (sfxVolumeText != null && sfxVolumeSlider != null)
                sfxVolumeText.text = $"SFX: {Mathf.RoundToInt(sfxVolumeSlider.value * 100)}%";
            if (uiScaleText != null && uiScaleSlider != null)
                uiScaleText.text = $"UI Scale: {uiScaleSlider.value:F1}x";
        }
        
        // Audio callbacks
        private void OnMasterVolumeChanged(float value)
        {
            if (settingsManager != null)
                settingsManager.SetMasterVolume(value);
            UpdateTextLabels();
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            if (settingsManager != null)
                settingsManager.SetMusicVolume(value);
            UpdateTextLabels();
        }
        
        private void OnSFXVolumeChanged(float value)
        {
            if (settingsManager != null)
                settingsManager.SetSFXVolume(value);
            UpdateTextLabels();
        }
        
        // Video callbacks
        private void OnQualityChanged(int index)
        {
            if (settingsManager != null)
                settingsManager.SetQualityLevel(index);
        }
        
        private void OnResolutionChanged(int index)
        {
            Resolution resolution = resolutions[index];
            Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        }
        
        private void OnVSyncChanged(bool enabled)
        {
            if (settingsManager != null)
                settingsManager.SetVSync(enabled);
        }
        
        private void OnFPSChanged(int index)
        {
            if (settingsManager == null) return;
            
            int fps = index switch
            {
                0 => 30,
                1 => 60,
                2 => 120,
                3 => 144,
                4 => -1, // Unlimited
                _ => 60
            };
            
            if (fps > 0)
                settingsManager.SetTargetFPS(fps);
            else
                Application.targetFrameRate = -1;
        }
        
        // Accessibility callbacks
        private void OnUIScaleChanged(float value)
        {
            if (settingsManager != null)
                settingsManager.SetUIScale(value);
            UpdateTextLabels();
        }
        
        private void OnColorBlindChanged(bool enabled)
        {
            if (settingsManager != null)
                settingsManager.SetColorBlindMode(enabled);
        }
        
        private void OnShowHitboxesChanged(bool enabled)
        {
            if (settingsManager != null)
                settingsManager.SetShowHitboxes(enabled);
        }
        
        private void ApplySettings()
        {
            // Settings are applied immediately, but this can be used for confirmation
            Debug.Log("Settings applied!");
        }
        
        private void ResetToDefaults()
        {
            if (settingsManager == null) return;
            
            // Reset to default values
            settingsManager.SetMasterVolume(1f);
            settingsManager.SetMusicVolume(0.7f);
            settingsManager.SetSFXVolume(1f);
            settingsManager.SetQualityLevel(2);
            settingsManager.SetVSync(true);
            settingsManager.SetTargetFPS(60);
            settingsManager.SetUIScale(1f);
            settingsManager.SetColorBlindMode(false);
            settingsManager.SetShowHitboxes(false);
            
            LoadCurrentSettings();
        }
        
        private void GoBack()
        {
            SceneManager.LoadScene("StartScene");
        }
    }
}

