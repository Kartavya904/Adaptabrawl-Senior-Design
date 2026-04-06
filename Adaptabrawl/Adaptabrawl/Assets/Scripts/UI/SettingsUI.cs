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
        private static readonly Color DropdownTemplateColor = new Color(0.12f, 0.12f, 0.16f, 0.98f);
        private static readonly Color DropdownItemColor = new Color(0.18f, 0.18f, 0.24f, 1f);
        private static readonly Color DropdownCheckmarkColor = new Color(0.85f, 0.2f, 0.2f, 1f);
        private static readonly Color ToggleOnColor = new Color(0.15f, 0.60f, 0.25f, 1f);
        private static readonly Color ToggleOffColor = new Color(0.20f, 0.20f, 0.28f, 1f);
        private static readonly Color ToggleOnTextColor = new Color(0.90f, 1f, 0.92f, 1f);
        private static readonly Color ToggleOffTextColor = new Color(0.75f, 0.75f, 0.80f, 1f);
        private static readonly float[] UIScaleOptions = { 0.9f, 0.95f, 1f, 1.05f, 1.1f };
        private static readonly string[] UIScaleOptionLabels = { "0.90x", "0.95x", "1.00x", "1.05x", "1.10x" };
        private const float RowSpacing = 56f;

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
        [SerializeField] private TMP_Dropdown displayModeDropdown;
        
        [Header("Accessibility")]
        [SerializeField] private Slider uiScaleSlider;
        [SerializeField] private TMP_Dropdown uiScaleDropdown;
        [SerializeField] private TextMeshProUGUI uiScaleText;
        [SerializeField] private Toggle colorBlindToggle;
        [SerializeField] private Toggle showHitboxesToggle;
        
        [Header("Navigation")]
        [SerializeField] private Button backButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;

        [Tooltip("Optional top-to-bottom order for D-pad / stick UI navigation. If empty, a default chain is built from common controls.")]
        [SerializeField] private Selectable[] menuFocusOrder;
        
        private Resolution[] resolutions;
        private ControlsConfigurationPanel controlsConfigurationPanel;

        private void Awake()
        {
            EnsureControlsPanel();
        }
        
        private void Start()
        {
            settingsManager = SettingsManager.EnsureExists();
            
            InitializeUI();
            SetupButtonListeners();
            LoadCurrentSettings();
            WireMenuControllerNavigation();
            EnsureControlsPanel();
        }

        private void OnDestroy()
        {
            RemoveButtonListeners();
        }

        private void LateUpdate()
        {
            if (!BackInputUtility.WasBackOrCancelPressedThisFrame()) return;
            if (BackInputUtility.IsTextInputFocused()) return;

            if (controlsConfigurationPanel != null && controlsConfigurationPanel.IsOpen)
            {
                if (controlsConfigurationPanel.IsCapturing)
                    return;

                if (controlsConfigurationPanel.HandleBackRequested())
                    return;
            }

            GoBack();
        }

        public void OpenControlsConfiguration()
        {
            EnsureControlsPanel();
            controlsConfigurationPanel?.Show();
        }

        private void WireMenuControllerNavigation()
        {
            if (menuFocusOrder != null && menuFocusOrder.Length > 0)
            {
                MenuNavigationGroup.ApplyVerticalChain(menuFocusOrder, wrap: false);
                MenuNavigationGroup.SelectFirstAvailable(menuFocusOrder);
                return;
            }

            var list = new List<Selectable>();
            void AddSel(Selectable s) { if (s != null) list.Add(s); }

            AddSel(masterVolumeSlider);
            AddSel(musicVolumeSlider);
            AddSel(sfxVolumeSlider);
            AddSel(qualityDropdown);
            AddSel(resolutionDropdown);
            AddSel(vsyncToggle);
            AddSel(fpsDropdown);
            AddSel(displayModeDropdown);
            AddSel(uiScaleDropdown != null ? uiScaleDropdown : uiScaleSlider);
            AddSel(colorBlindToggle);
            AddSel(showHitboxesToggle);
            if (applyButton != null) list.Add(applyButton);
            if (resetButton != null) list.Add(resetButton);
            if (backButton != null) list.Add(backButton);

            if (list.Count == 0) return;
            MenuNavigationGroup.ApplyVerticalChain(list, wrap: false);
            MenuNavigationGroup.SelectFirstAvailable(list);
        }
        
        private void InitializeUI()
        {
            EnsureDisplayModeDropdown();
            EnsureUIScaleControl();

            EnsureDropdownTemplate(qualityDropdown);
            EnsureDropdownTemplate(resolutionDropdown);
            EnsureDropdownTemplate(fpsDropdown);
            EnsureDropdownTemplate(displayModeDropdown);
            EnsureDropdownTemplate(uiScaleDropdown);

            // Initialize resolution dropdown
            var distinctResolutions = new List<Resolution>();
            var seenResolutions = new HashSet<string>();
            foreach (var resolution in Screen.resolutions)
            {
                string key = $"{resolution.width}x{resolution.height}";
                if (seenResolutions.Add(key))
                    distinctResolutions.Add(resolution);
            }
            if (distinctResolutions.Count == 0)
                distinctResolutions.Add(Screen.currentResolution);
            resolutions = distinctResolutions.ToArray();

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

            if (displayModeDropdown != null)
            {
                displayModeDropdown.ClearOptions();
                displayModeDropdown.AddOptions(new List<string>
                {
                    "Fullscreen",
                    "Borderless",
                    "Windowed"
                });
            }

            if (uiScaleDropdown != null)
            {
                uiScaleDropdown.ClearOptions();
                uiScaleDropdown.AddOptions(new List<string>(UIScaleOptionLabels));
                uiScaleDropdown.RefreshShownValue();
            }
        }

        private void ConfigureUIScaleSliderRange()
        {
            if (uiScaleSlider == null) return;
            uiScaleSlider.minValue = SettingsManager.MinUIScale;
            uiScaleSlider.maxValue = SettingsManager.MaxUIScale;
            uiScaleSlider.wholeNumbers = false;
        }

        private void EnsureUIScaleControl()
        {
            if (uiScaleDropdown == null && uiScaleSlider != null)
                uiScaleDropdown = CreateDropdownFromReference(uiScaleSlider.GetComponent<RectTransform>(), "UIScaleDropdown");

            if (uiScaleSlider != null && uiScaleDropdown != null)
                uiScaleSlider.gameObject.SetActive(false);

            if (uiScaleDropdown == null)
                ConfigureUIScaleSliderRange();
        }

        private void EnsureDisplayModeDropdown()
        {
            if (displayModeDropdown != null)
                return;

            RectTransform fpsRow = fpsDropdown != null ? fpsDropdown.transform.parent as RectTransform : null;
            RectTransform vsyncRow = vsyncToggle != null ? vsyncToggle.transform.parent as RectTransform : null;
            if (fpsRow == null || fpsRow.parent == null || vsyncRow == null)
                return;

            RectTransform existingRuntimeRow = FindChildRectByName(fpsRow.parent, "DisplayModeRow");
            if (existingRuntimeRow != null)
            {
                displayModeDropdown = existingRuntimeRow.GetComponentInChildren<TMP_Dropdown>(true);
                return;
            }

            GameObject clonedRow = Instantiate(fpsRow.gameObject, fpsRow.parent);
            clonedRow.name = "DisplayModeRow";

            RectTransform clonedRowRect = clonedRow.GetComponent<RectTransform>();
            if (clonedRowRect != null)
                clonedRowRect.anchoredPosition = new Vector2(clonedRowRect.anchoredPosition.x, vsyncRow.anchoredPosition.y - RowSpacing);

            TextMeshProUGUI rowLabel = clonedRow.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (rowLabel != null)
                rowLabel.text = "Display Mode";

            displayModeDropdown = clonedRow.GetComponentInChildren<TMP_Dropdown>(true);
            if (displayModeDropdown != null)
                displayModeDropdown.gameObject.name = "DisplayModeDropdown";

            ShiftRectY(FindChildRectByName(fpsRow.parent, "VideoDiv"), -RowSpacing);
            ShiftRectY(FindChildRectByName(fpsRow.parent, "AccessLabel"), -RowSpacing);
            ShiftRectY(uiScaleSlider != null ? uiScaleSlider.transform.parent as RectTransform : uiScaleDropdown != null ? uiScaleDropdown.transform.parent as RectTransform : null, -RowSpacing);
            ShiftRectY(colorBlindToggle != null ? colorBlindToggle.transform.parent as RectTransform : null, -RowSpacing);
            ShiftRectY(showHitboxesToggle != null ? showHitboxesToggle.transform.parent as RectTransform : null, -RowSpacing);
        }

        private void EnsureDropdownTemplate(TMP_Dropdown dropdown)
        {
            if (dropdown == null) return;

            bool hasValidTemplate =
                dropdown.template != null &&
                dropdown.template.GetComponentInChildren<Toggle>(true) != null &&
                dropdown.itemText != null;

            if (!hasValidTemplate)
                BuildRuntimeDropdownTemplate(dropdown);

            dropdown.RefreshShownValue();
        }

        private void BuildRuntimeDropdownTemplate(TMP_Dropdown dropdown)
        {
            if (dropdown == null) return;

            if (dropdown.captionText == null)
                dropdown.captionText = dropdown.GetComponentInChildren<TextMeshProUGUI>(true);

            RectTransform dropdownRect = dropdown.GetComponent<RectTransform>();
            float controlHeight = dropdownRect != null ? Mathf.Max(30f, dropdownRect.rect.height) : 34f;
            float templateHeight = controlHeight * 5f;

            // Remove stale runtime template (if any) so references don't drift.
            Transform oldTemplate = dropdown.transform.Find("RuntimeTemplate");
            if (oldTemplate != null)
                Destroy(oldTemplate.gameObject);

            GameObject templateObj = new GameObject("RuntimeTemplate", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            RectTransform templateRect = templateObj.GetComponent<RectTransform>();
            templateRect.SetParent(dropdown.transform, false);
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -(controlHeight + 4f));
            templateRect.sizeDelta = new Vector2(0f, templateHeight);

            Image templateImage = templateObj.GetComponent<Image>();
            templateImage.color = DropdownTemplateColor;

            ScrollRect scrollRect = templateObj.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20f;

            GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            RectTransform viewportRect = viewportObj.GetComponent<RectTransform>();
            viewportRect.SetParent(templateRect, false);
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;

            Image viewportImage = viewportObj.GetComponent<Image>();
            viewportImage.color = new Color(1f, 1f, 1f, 0.03f);
            Mask viewportMask = viewportObj.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            RectTransform contentRect = contentObj.GetComponent<RectTransform>();
            contentRect.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout = contentObj.GetComponent<VerticalLayoutGroup>();
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 2f;

            ContentSizeFitter fitter = contentObj.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            GameObject itemObj = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(Toggle), typeof(LayoutElement));
            RectTransform itemRect = itemObj.GetComponent<RectTransform>();
            itemRect.SetParent(contentRect, false);
            itemRect.anchorMin = new Vector2(0f, 0.5f);
            itemRect.anchorMax = new Vector2(1f, 0.5f);
            itemRect.sizeDelta = new Vector2(0f, controlHeight);

            Image itemImage = itemObj.GetComponent<Image>();
            itemImage.color = DropdownItemColor;

            LayoutElement itemLayout = itemObj.GetComponent<LayoutElement>();
            itemLayout.preferredHeight = controlHeight;

            Toggle itemToggle = itemObj.GetComponent<Toggle>();
            itemToggle.targetGraphic = itemImage;
            itemToggle.isOn = true;

            GameObject checkmarkObj = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
            RectTransform checkmarkRect = checkmarkObj.GetComponent<RectTransform>();
            checkmarkRect.SetParent(itemRect, false);
            checkmarkRect.anchorMin = new Vector2(0f, 0.5f);
            checkmarkRect.anchorMax = new Vector2(0f, 0.5f);
            checkmarkRect.anchoredPosition = new Vector2(12f, 0f);
            checkmarkRect.sizeDelta = new Vector2(14f, 14f);

            Image checkmarkImage = checkmarkObj.GetComponent<Image>();
            checkmarkImage.color = DropdownCheckmarkColor;
            itemToggle.graphic = checkmarkImage;

            GameObject labelObj = new GameObject("Item Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.SetParent(itemRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(30f, 2f);
            labelRect.offsetMax = new Vector2(-8f, -2f);

            TextMeshProUGUI itemLabel = labelObj.GetComponent<TextMeshProUGUI>();
            itemLabel.text = "Option";
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabel.color = Color.white;
            itemLabel.textWrappingMode = TextWrappingModes.NoWrap;
            itemLabel.raycastTarget = false;

            if (dropdown.captionText != null)
            {
                itemLabel.font = dropdown.captionText.font;
                itemLabel.fontSize = dropdown.captionText.fontSize;
                itemLabel.fontStyle = dropdown.captionText.fontStyle;
            }
            else if (TMP_Settings.defaultFontAsset != null)
            {
                itemLabel.font = TMP_Settings.defaultFontAsset;
                itemLabel.fontSize = 24f;
                itemLabel.fontStyle = FontStyles.Normal;
            }

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;
            dropdown.itemImage = checkmarkImage;

            templateObj.SetActive(false);
        }

        private TMP_Dropdown CreateDropdownFromReference(RectTransform referenceRect, string objectName)
        {
            if (referenceRect == null || referenceRect.parent == null)
                return null;

            GameObject dropdownObj = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
            RectTransform dropdownRect = dropdownObj.GetComponent<RectTransform>();
            dropdownRect.SetParent(referenceRect.parent, false);
            dropdownRect.anchorMin = referenceRect.anchorMin;
            dropdownRect.anchorMax = referenceRect.anchorMax;
            dropdownRect.pivot = referenceRect.pivot;
            dropdownRect.anchoredPosition = referenceRect.anchoredPosition;
            dropdownRect.sizeDelta = referenceRect.sizeDelta;
            dropdownRect.localScale = referenceRect.localScale;
            dropdownRect.SetSiblingIndex(referenceRect.GetSiblingIndex() + 1);

            Image background = dropdownObj.GetComponent<Image>();
            background.color = DropdownItemColor;

            TMP_Dropdown dropdown = dropdownObj.GetComponent<TMP_Dropdown>();

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.SetParent(dropdownRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 2f);
            labelRect.offsetMax = new Vector2(-30f, -2f);

            TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
            label.text = "1.00x";
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 12f;

            GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.SetParent(dropdownRect, false);
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = new Vector2(1f, 1f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(24f, 0f);
            arrowRect.anchoredPosition = new Vector2(-12f, 0f);

            TextMeshProUGUI arrowText = arrowObj.GetComponent<TextMeshProUGUI>();
            arrowText.text = "▼";
            arrowText.alignment = TextAlignmentOptions.Center;
            arrowText.color = new Color(0.80f, 0.80f, 0.85f, 1f);
            arrowText.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                arrowText.font = TMP_Settings.defaultFontAsset;
            arrowText.fontSize = 11f;

            dropdown.captionText = label;
            dropdown.AddOptions(new List<string> { "Option" });
            BuildRuntimeDropdownTemplate(dropdown);
            dropdown.RefreshShownValue();

            return dropdown;
        }

        private static RectTransform FindChildRectByName(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
                return null;

            Transform child = parent.Find(childName);
            return child as RectTransform;
        }

        private static void ShiftRectY(RectTransform rect, float deltaY)
        {
            if (rect == null || Mathf.Approximately(deltaY, 0f))
                return;

            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y + deltaY);
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
            if (displayModeDropdown != null)
                displayModeDropdown.onValueChanged.AddListener(OnDisplayModeChanged);
            
            // Accessibility
            if (uiScaleDropdown != null)
                uiScaleDropdown.onValueChanged.AddListener(OnUIScaleOptionChanged);
            else if (uiScaleSlider != null)
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

        private void RemoveButtonListeners()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);

            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            if (vsyncToggle != null)
                vsyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);
            if (fpsDropdown != null)
                fpsDropdown.onValueChanged.RemoveListener(OnFPSChanged);
            if (displayModeDropdown != null)
                displayModeDropdown.onValueChanged.RemoveListener(OnDisplayModeChanged);

            if (uiScaleDropdown != null)
                uiScaleDropdown.onValueChanged.RemoveListener(OnUIScaleOptionChanged);
            if (uiScaleSlider != null)
                uiScaleSlider.onValueChanged.RemoveListener(OnUIScaleChanged);
            if (colorBlindToggle != null)
                colorBlindToggle.onValueChanged.RemoveListener(OnColorBlindChanged);
            if (showHitboxesToggle != null)
                showHitboxesToggle.onValueChanged.RemoveListener(OnShowHitboxesChanged);

            if (backButton != null)
                backButton.onClick.RemoveListener(GoBack);
            if (applyButton != null)
                applyButton.onClick.RemoveListener(ApplySettings);
            if (resetButton != null)
                resetButton.onClick.RemoveListener(ResetToDefaults);
        }
        
        private void LoadCurrentSettings()
        {
            if (settingsManager == null) return;
            
            // Load audio settings
            if (masterVolumeSlider != null)
                masterVolumeSlider.SetValueWithoutNotify(settingsManager.MasterVolume);
            if (musicVolumeSlider != null)
                musicVolumeSlider.SetValueWithoutNotify(settingsManager.MusicVolume);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.SetValueWithoutNotify(settingsManager.SFXVolume);
            
            // Load video settings
            if (qualityDropdown != null)
            {
                int clampedQuality = Mathf.Clamp(settingsManager.QualityLevel, 0, Mathf.Max(0, qualityDropdown.options.Count - 1));
                qualityDropdown.SetValueWithoutNotify(clampedQuality);
                qualityDropdown.RefreshShownValue();
            }
            if (vsyncToggle != null)
                vsyncToggle.SetIsOnWithoutNotify(settingsManager.VSyncEnabled);
            if (fpsDropdown != null)
            {
                fpsDropdown.SetValueWithoutNotify(FPSIndexFromTarget(settingsManager.TargetFPS));
                fpsDropdown.RefreshShownValue();
            }
            if (displayModeDropdown != null)
            {
                displayModeDropdown.SetValueWithoutNotify(DisplayModeIndexFromMode(settingsManager.DisplayMode));
                displayModeDropdown.RefreshShownValue();
            }
            
            // Load accessibility settings
            if (uiScaleDropdown != null)
            {
                uiScaleDropdown.SetValueWithoutNotify(UIScaleIndexFromValue(settingsManager.UIScale));
                uiScaleDropdown.RefreshShownValue();
            }
            else if (uiScaleSlider != null)
            {
                uiScaleSlider.SetValueWithoutNotify(Mathf.Clamp(settingsManager.UIScale, SettingsManager.MinUIScale, SettingsManager.MaxUIScale));
            }
            if (colorBlindToggle != null)
                colorBlindToggle.SetIsOnWithoutNotify(settingsManager.ColorBlindMode);
            if (showHitboxesToggle != null)
                showHitboxesToggle.SetIsOnWithoutNotify(settingsManager.ShowHitboxes);
            
            UpdateTextLabels();
            UpdateToggleVisual(vsyncToggle);
            UpdateToggleVisual(colorBlindToggle);
            UpdateToggleVisual(showHitboxesToggle);
        }

        private static int FPSIndexFromTarget(int targetFps)
        {
            return targetFps switch
            {
                30 => 0,
                60 => 1,
                120 => 2,
                144 => 3,
                _ => 4 // Unlimited or custom value
            };
        }

        private static int UIScaleIndexFromValue(float scale)
        {
            int closestIndex = 0;
            float bestDelta = float.MaxValue;
            for (int i = 0; i < UIScaleOptions.Length; i++)
            {
                float delta = Mathf.Abs(scale - UIScaleOptions[i]);
                if (delta < bestDelta)
                {
                    bestDelta = delta;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private static float UIScaleValueFromIndex(int index)
        {
            int clamped = Mathf.Clamp(index, 0, UIScaleOptions.Length - 1);
            return UIScaleOptions[clamped];
        }

        private static int DisplayModeIndexFromMode(FullScreenMode mode)
        {
            return mode switch
            {
                FullScreenMode.ExclusiveFullScreen => 0,
                FullScreenMode.FullScreenWindow => 1,
                FullScreenMode.Windowed => 2,
                _ => 1
            };
        }

        private static FullScreenMode DisplayModeFromIndex(int index)
        {
            return index switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.FullScreenWindow,
                2 => FullScreenMode.Windowed,
                _ => FullScreenMode.FullScreenWindow
            };
        }
        
        private void UpdateTextLabels()
        {
            if (masterVolumeText != null && masterVolumeSlider != null)
                masterVolumeText.text = $"Master: {Mathf.RoundToInt(masterVolumeSlider.value * 100)}%";
            if (musicVolumeText != null && musicVolumeSlider != null)
                musicVolumeText.text = $"Music: {Mathf.RoundToInt(musicVolumeSlider.value * 100)}%";
            if (sfxVolumeText != null && sfxVolumeSlider != null)
                sfxVolumeText.text = $"SFX: {Mathf.RoundToInt(sfxVolumeSlider.value * 100)}%";
            if (uiScaleText != null)
            {
                float displayScale = uiScaleDropdown != null
                    ? UIScaleValueFromIndex(uiScaleDropdown.value)
                    : uiScaleSlider != null
                        ? uiScaleSlider.value
                        : SettingsManager.MinUIScale;

                uiScaleText.text = $"UI Scale: {displayScale:F2}x";
            }
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
            if (resolutions == null || resolutions.Length == 0) return;
            if (index < 0 || index >= resolutions.Length) return;

            Resolution resolution = resolutions[index];
            FullScreenMode mode = settingsManager != null ? settingsManager.DisplayMode : Screen.fullScreenMode;
            Screen.SetResolution(resolution.width, resolution.height, mode != FullScreenMode.Windowed);
            Screen.fullScreenMode = mode;
        }
        
        private void OnVSyncChanged(bool enabled)
        {
            if (settingsManager != null)
                settingsManager.SetVSync(enabled);
            UpdateToggleVisual(vsyncToggle);
        }
        
        private void OnFPSChanged(int index)
        {
            int fps = index switch
            {
                0 => 30,
                1 => 60,
                2 => 120,
                3 => 144,
                4 => -1, // Unlimited
                _ => 60
            };

            if (settingsManager != null)
                settingsManager.SetTargetFPS(fps);
            else
                Application.targetFrameRate = fps;
        }

        private void OnDisplayModeChanged(int index)
        {
            if (settingsManager == null)
                return;

            settingsManager.SetDisplayMode(DisplayModeFromIndex(index));
        }
        
        // Accessibility callbacks
        private void OnUIScaleChanged(float value)
        {
            if (settingsManager != null)
                settingsManager.SetUIScale(value);
            UpdateTextLabels();
        }

        private void OnUIScaleOptionChanged(int index)
        {
            if (settingsManager != null)
                settingsManager.SetUIScale(UIScaleValueFromIndex(index));
            UpdateTextLabels();
        }
        
        private void OnColorBlindChanged(bool enabled)
        {
            if (settingsManager != null)
                settingsManager.SetColorBlindMode(enabled);
            UpdateToggleVisual(colorBlindToggle);
        }
        
        private void OnShowHitboxesChanged(bool enabled)
        {
            if (settingsManager != null)
                settingsManager.SetShowHitboxes(enabled);
            UpdateToggleVisual(showHitboxesToggle);
        }

        private void UpdateToggleVisual(Toggle toggle)
        {
            if (toggle == null)
                return;

            bool isOn = toggle.isOn;

            if (toggle.targetGraphic is Image toggleBackground)
                toggleBackground.color = isOn ? ToggleOnColor : ToggleOffColor;

            if (toggle.graphic is Image checkmark)
                checkmark.color = isOn ? ToggleOnTextColor : ToggleOffTextColor;

            TextMeshProUGUI stateLabel = toggle.transform.Find("StateLabel")?.GetComponent<TextMeshProUGUI>();
            if (stateLabel == null)
                stateLabel = toggle.GetComponentInChildren<TextMeshProUGUI>(true);

            if (stateLabel != null)
            {
                stateLabel.text = isOn ? "ON" : "OFF";
                stateLabel.color = isOn ? ToggleOnTextColor : ToggleOffTextColor;
            }
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
            settingsManager.SetDisplayMode(FullScreenMode.FullScreenWindow);
            settingsManager.SetUIScale(1f);
            settingsManager.SetColorBlindMode(false);
            settingsManager.SetShowHitboxes(false);
            
            LoadCurrentSettings();
        }
        
        private void GoBack()
        {
            string prev = PlayerPrefs.GetString("PreviousScene", "");
            PlayerPrefs.DeleteKey("PreviousScene");
            PlayerPrefs.DeleteKey("WasPaused");

            if (!string.IsNullOrEmpty(prev))
                SceneManager.LoadScene(prev);
            else
                SceneManager.LoadScene("StartScene");
        }

        private void EnsureControlsPanel()
        {
            if (controlsConfigurationPanel != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
                return;

            Transform existing = canvas.transform.Find("ControlsConfigurationPanelHost");
            if (existing != null && existing.TryGetComponent(out ControlsConfigurationPanel existingPanel))
            {
                controlsConfigurationPanel = existingPanel;
                return;
            }

            var host = new GameObject("ControlsConfigurationPanelHost", typeof(RectTransform));
            host.transform.SetParent(canvas.transform, false);
            controlsConfigurationPanel = host.AddComponent<ControlsConfigurationPanel>();
        }
    }
}
