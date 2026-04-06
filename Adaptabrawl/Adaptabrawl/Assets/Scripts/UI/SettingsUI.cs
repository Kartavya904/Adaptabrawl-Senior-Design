using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Adaptabrawl.Settings;
using System.Collections.Generic;
using System.Collections;
using Adaptabrawl.Networking;

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
        private static readonly Color NetworkTestIdleColor = new Color(0.72f, 0.72f, 0.78f, 1f);
        private static readonly Color NetworkTestSuccessColor = new Color(0.52f, 0.90f, 0.58f, 1f);
        private static readonly Color NetworkTestWarningColor = new Color(0.96f, 0.79f, 0.36f, 1f);
        private static readonly Color NetworkTestFailureColor = new Color(0.95f, 0.48f, 0.48f, 1f);
        private static readonly Color TestButtonColor = new Color(0.16f, 0.38f, 0.58f, 1f);
        private static readonly float[] UIScaleOptions = { 0.9f, 0.95f, 1f, 1.05f, 1.1f };
        private static readonly string[] UIScaleOptionLabels = { "0.90x", "0.95x", "1.00x", "1.05x", "1.10x" };
        private const float RowSpacing = 56f;
        private const float SectionGap = 16f;
        private const float BottomButtonSpacing = 220f;
        private const float StatusLabelBottomOffset = 126f;
        private const float DropdownTemplateGap = 1f;
        private const float MinimumDropdownFontSize = 16f;
        private const float MinimumDropdownArrowFontSize = 15f;

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
        [SerializeField] private Button testButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private TextMeshProUGUI networkTestStatusText;

        [Tooltip("Optional top-to-bottom order for D-pad / stick UI navigation. If empty, a default chain is built from common controls.")]
        [SerializeField] private Selectable[] menuFocusOrder;
        
        private Resolution[] resolutions;
        private ControlsConfigurationPanel controlsConfigurationPanel;
        private bool networkTestInProgress;

        private void Awake()
        {
            EnsureControlsPanel();
            EnsureNetworkTestUi();
        }
        
        private void Start()
        {
            settingsManager = SettingsManager.EnsureExists();
            
            InitializeUI();
            EnsureNetworkTestUi();
            SetupButtonListeners();
            LoadCurrentSettings();
            WireMenuControllerNavigation();
            EnsureControlsPanel();
            RefreshNetworkTestStatusFromCache();
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
            AddSel(fpsDropdown);
            AddSel(displayModeDropdown);
            AddSel(vsyncToggle);
            AddSel(uiScaleDropdown != null ? uiScaleDropdown : uiScaleSlider);
            AddSel(colorBlindToggle);
            AddSel(showHitboxesToggle);
            if (testButton != null) list.Add(testButton);
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

            if (uiScaleText != null)
                uiScaleText.gameObject.SetActive(false);

            if (uiScaleDropdown == null)
                ConfigureUIScaleSliderRange();
        }

        private void EnsureDisplayModeDropdown()
        {
            RectTransform fpsRow = fpsDropdown != null ? fpsDropdown.transform.parent as RectTransform : null;
            RectTransform vsyncRow = vsyncToggle != null ? vsyncToggle.transform.parent as RectTransform : null;
            if (fpsRow == null || fpsRow.parent == null || vsyncRow == null)
                return;

            RectTransform displayModeRow = FindChildRectByName(fpsRow.parent, "DisplayModeRow");
            if (displayModeRow == null)
            {
                GameObject clonedRow = Instantiate(fpsRow.gameObject, fpsRow.parent);
                clonedRow.name = "DisplayModeRow";
                displayModeRow = clonedRow.GetComponent<RectTransform>();
            }

            TextMeshProUGUI rowLabel = displayModeRow != null
                ? displayModeRow.transform.Find("Label")?.GetComponent<TextMeshProUGUI>()
                : null;
            if (rowLabel != null)
                rowLabel.text = "Display Mode";

            displayModeDropdown = displayModeRow != null ? displayModeRow.GetComponentInChildren<TMP_Dropdown>(true) : null;
            if (displayModeDropdown != null)
                displayModeDropdown.gameObject.name = "DisplayModeDropdown";

            if (displayModeRow == null)
                return;

            float fpsY = fpsRow.anchoredPosition.y;
            float desiredDisplayModeY = fpsY - RowSpacing;
            float desiredVsyncY = desiredDisplayModeY - RowSpacing;

            SetRectY(displayModeRow, desiredDisplayModeY);
            SetRectY(vsyncRow, desiredVsyncY);
            SetRectY(FindChildRectByName(fpsRow.parent, "VideoDiv"), desiredVsyncY - SectionGap);
            SetRectY(FindChildRectByName(fpsRow.parent, "AccessLabel"), desiredVsyncY - (SectionGap + 36f));
            SetRectY(uiScaleSlider != null ? uiScaleSlider.transform.parent as RectTransform : uiScaleDropdown != null ? uiScaleDropdown.transform.parent as RectTransform : null, desiredVsyncY - (SectionGap + 36f + RowSpacing));
            SetRectY(colorBlindToggle != null ? colorBlindToggle.transform.parent as RectTransform : null, desiredVsyncY - (SectionGap + 36f + (RowSpacing * 2f)));
            SetRectY(showHitboxesToggle != null ? showHitboxesToggle.transform.parent as RectTransform : null, desiredVsyncY - (SectionGap + 36f + (RowSpacing * 3f)));
        }

        private void EnsureDropdownTemplate(TMP_Dropdown dropdown)
        {
            if (dropdown == null) return;

            NormalizeDropdownCaption(dropdown);
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
            float fontSize = Mathf.Max(MinimumDropdownFontSize, Mathf.Round(controlHeight * 0.38f));
            float templateHeight = controlHeight * 5f;

            if (dropdown.template != null && dropdown.template.parent == dropdown.transform)
                Destroy(dropdown.template.gameObject);

            Transform oldTemplate = dropdown.transform.Find("RuntimeTemplate");
            if (oldTemplate != null)
                Destroy(oldTemplate.gameObject);

            GameObject templateObj = new GameObject("RuntimeTemplate", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
            RectTransform templateRect = templateObj.GetComponent<RectTransform>();
            templateRect.SetParent(dropdown.transform, false);
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -(controlHeight + DropdownTemplateGap));
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
            layout.spacing = 0f;
            layout.padding = new RectOffset(0, 0, 0, 0);

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
            labelRect.offsetMin = new Vector2(34f, 0f);
            labelRect.offsetMax = new Vector2(-10f, 0f);

            TextMeshProUGUI itemLabel = labelObj.GetComponent<TextMeshProUGUI>();
            itemLabel.text = "Option";
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabel.color = Color.white;
            itemLabel.textWrappingMode = TextWrappingModes.NoWrap;
            itemLabel.raycastTarget = false;

            if (dropdown.captionText != null)
            {
                itemLabel.font = dropdown.captionText.font;
                itemLabel.fontSize = Mathf.Max(dropdown.captionText.fontSize, fontSize);
                itemLabel.fontStyle = dropdown.captionText.fontStyle;
            }
            else if (TMP_Settings.defaultFontAsset != null)
            {
                itemLabel.font = TMP_Settings.defaultFontAsset;
                itemLabel.fontSize = fontSize;
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
            float controlHeight = Mathf.Max(30f, referenceRect.rect.height);
            float fontSize = Mathf.Max(MinimumDropdownFontSize, Mathf.Round(controlHeight * 0.38f));

            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.SetParent(dropdownRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 0f);
            labelRect.offsetMax = new Vector2(-32f, 0f);

            TextMeshProUGUI label = labelObj.GetComponent<TextMeshProUGUI>();
            label.text = "1.00x";
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = fontSize;

            GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform arrowRect = arrowObj.GetComponent<RectTransform>();
            arrowRect.SetParent(dropdownRect, false);
            arrowRect.anchorMin = new Vector2(1f, 0f);
            arrowRect.anchorMax = new Vector2(1f, 1f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(28f, 0f);
            arrowRect.anchoredPosition = new Vector2(-12f, 0f);

            TextMeshProUGUI arrowText = arrowObj.GetComponent<TextMeshProUGUI>();
            arrowText.text = "▼";
            arrowText.alignment = TextAlignmentOptions.Center;
            arrowText.color = new Color(0.80f, 0.80f, 0.85f, 1f);
            arrowText.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                arrowText.font = TMP_Settings.defaultFontAsset;
            arrowText.fontSize = Mathf.Max(MinimumDropdownArrowFontSize, fontSize - 1f);

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

        private static void SetRectY(RectTransform rect, float y)
        {
            if (rect == null)
                return;

            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }

        private static void NormalizeDropdownCaption(TMP_Dropdown dropdown)
        {
            if (dropdown == null)
                return;

            if (dropdown.captionText == null)
                dropdown.captionText = dropdown.GetComponentInChildren<TextMeshProUGUI>(true);

            if (dropdown.captionText == null)
                return;

            RectTransform dropdownRect = dropdown.GetComponent<RectTransform>();
            float controlHeight = dropdownRect != null ? Mathf.Max(30f, dropdownRect.rect.height) : 34f;
            float fontSize = Mathf.Max(MinimumDropdownFontSize, Mathf.Round(controlHeight * 0.38f));

            dropdown.captionText.fontSize = fontSize;
            dropdown.captionText.alignment = TextAlignmentOptions.MidlineLeft;
            dropdown.captionText.textWrappingMode = TextWrappingModes.NoWrap;
            dropdown.captionText.overflowMode = TextOverflowModes.Ellipsis;
            dropdown.captionText.raycastTarget = false;

            RectTransform captionRect = dropdown.captionText.rectTransform;
            captionRect.anchorMin = Vector2.zero;
            captionRect.anchorMax = Vector2.one;
            captionRect.offsetMin = new Vector2(12f, 0f);
            captionRect.offsetMax = new Vector2(-32f, 0f);

            TextMeshProUGUI arrowText = dropdown.transform.Find("Arrow")?.GetComponent<TextMeshProUGUI>();
            if (arrowText != null)
            {
                arrowText.fontSize = Mathf.Max(MinimumDropdownArrowFontSize, fontSize - 1f);
                arrowText.alignment = TextAlignmentOptions.Center;
                arrowText.raycastTarget = false;
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
            if (testButton != null)
                testButton.onClick.AddListener(RunLanConnectivityTest);
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

            if (testButton != null)
                testButton.onClick.RemoveListener(RunLanConnectivityTest);
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
            if (uiScaleText != null && uiScaleText.gameObject.activeSelf)
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

        public void RunLanConnectivityTest()
        {
            if (networkTestInProgress)
                return;

            StartCoroutine(CoRunLanConnectivityTest());
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

        private IEnumerator CoRunLanConnectivityTest()
        {
            networkTestInProgress = true;
            SetNetworkTestStatus("Testing LAN access and Windows Firewall permissions...", NetworkTestIdleColor);
            SetTestButtonState(interactable: false, label: "TESTING...");

            var task = LanConnectivitySelfTest.RunAsync(waitForWindowsFirewallConfirmation: true);
            while (!task.IsCompleted)
                yield return null;

            if (task.IsFaulted)
            {
                string message = task.Exception?.GetBaseException().Message ?? "LAN test failed unexpectedly.";
                SetNetworkTestStatus(message, NetworkTestFailureColor);
            }
            else
            {
                LanConnectivityTestResult result = task.Result;
                if (ShouldRequestFirewallApproval(result))
                {
                    SetNetworkTestStatus("Windows needs admin approval to allow LAN lobbies. Accept the prompt to add firewall rules...", NetworkTestWarningColor);
                    SetTestButtonState(interactable: false, label: "APPROVE...");

                    var approvalTask = LanConnectivitySelfTest.TryEnsureWindowsFirewallAccessAsync();
                    while (!approvalTask.IsCompleted)
                        yield return null;

                    if (!approvalTask.IsFaulted && !approvalTask.IsCanceled && approvalTask.Result)
                    {
                        SetNetworkTestStatus("Firewall rules added. Re-checking LAN access...", NetworkTestIdleColor);
                        var rerunTask = LanConnectivitySelfTest.RunAsync(waitForWindowsFirewallConfirmation: false);
                        while (!rerunTask.IsCompleted)
                            yield return null;

                        if (rerunTask.IsFaulted)
                        {
                            string rerunMessage = rerunTask.Exception?.GetBaseException().Message ?? "LAN re-check failed unexpectedly.";
                            SetNetworkTestStatus(rerunMessage, NetworkTestFailureColor);
                        }
                        else
                        {
                            ApplyNetworkTestResult(rerunTask.Result);
                        }
                    }
                    else
                    {
                        SetNetworkTestStatus(
                            "Firewall access was not approved. Click TEST again and allow the Windows admin prompt so other devices on this Wi-Fi can join.",
                            NetworkTestWarningColor);
                    }
                }
                else
                {
                    ApplyNetworkTestResult(result);
                }
            }

            SetTestButtonState(interactable: true, label: "TEST");
            networkTestInProgress = false;
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

        private void EnsureNetworkTestUi()
        {
            if (backButton == null)
                return;

            if (testButton == null)
            {
                Transform existingButton = backButton.transform.parent != null
                    ? backButton.transform.parent.Find("TestBtn")
                    : null;

                if (existingButton != null)
                    testButton = existingButton.GetComponent<Button>();
            }

            if (testButton == null)
                testButton = CreateNetworkTestButton();

            if (networkTestStatusText == null)
            {
                Transform existingStatus = backButton.transform.parent != null
                    ? backButton.transform.parent.Find("LanTestStatusText")
                    : null;

                if (existingStatus != null)
                    networkTestStatusText = existingStatus.GetComponent<TextMeshProUGUI>();
            }

            if (networkTestStatusText == null)
                networkTestStatusText = CreateNetworkTestStatusLabel();
        }

        private Button CreateNetworkTestButton()
        {
            if (backButton == null)
                return null;

            GameObject buttonObject = Instantiate(backButton.gameObject, backButton.transform.parent);
            buttonObject.name = "TestBtn";

            RectTransform backRect = backButton.GetComponent<RectTransform>();
            RectTransform testRect = buttonObject.GetComponent<RectTransform>();
            if (backRect != null && testRect != null)
            {
                testRect.anchorMin = backRect.anchorMin;
                testRect.anchorMax = backRect.anchorMax;
                testRect.pivot = backRect.pivot;
                testRect.sizeDelta = backRect.sizeDelta;
                testRect.anchoredPosition = new Vector2(backRect.anchoredPosition.x - BottomButtonSpacing, backRect.anchoredPosition.y);
            }

            Image buttonImage = buttonObject.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.color = TestButtonColor;

            TextMeshProUGUI label = buttonObject.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
                label.text = "TEST";

            Button button = buttonObject.GetComponent<Button>();
            if (button != null)
                button.onClick.RemoveAllListeners();

            return button;
        }

        private TextMeshProUGUI CreateNetworkTestStatusLabel()
        {
            if (backButton == null || backButton.transform.parent == null)
                return null;

            GameObject statusObject = new GameObject("LanTestStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.SetParent(backButton.transform.parent, false);
            statusRect.anchorMin = new Vector2(0.5f, 0f);
            statusRect.anchorMax = new Vector2(0.5f, 0f);
            statusRect.pivot = new Vector2(0.5f, 0.5f);
            statusRect.anchoredPosition = new Vector2(0f, StatusLabelBottomOffset);
            statusRect.sizeDelta = new Vector2(860f, 34f);

            TextMeshProUGUI statusLabel = statusObject.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI referenceLabel = backButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (referenceLabel != null)
            {
                statusLabel.font = referenceLabel.font;
                statusLabel.fontSharedMaterial = referenceLabel.fontSharedMaterial;
            }

            statusLabel.fontSize = 18f;
            statusLabel.alignment = TextAlignmentOptions.Center;
            statusLabel.textWrappingMode = TextWrappingModes.NoWrap;
            statusLabel.overflowMode = TextOverflowModes.Ellipsis;
            statusLabel.color = NetworkTestIdleColor;
            statusLabel.text = "Run TEST before online play to confirm LAN access.";
            statusLabel.raycastTarget = false;
            return statusLabel;
        }

        private void RefreshNetworkTestStatusFromCache()
        {
            if (!LanConnectivitySelfTest.LastResult.HasValue)
            {
                SetNetworkTestStatus("Run TEST before online play to confirm LAN access.", NetworkTestIdleColor);
                return;
            }

            ApplyNetworkTestResult(LanConnectivitySelfTest.LastResult.Value);
        }

        private void ApplyNetworkTestResult(LanConnectivityTestResult result)
        {
            Color statusColor = result.State switch
            {
                LanConnectivityTestState.Success => NetworkTestSuccessColor,
                LanConnectivityTestState.Warning => NetworkTestWarningColor,
                _ => NetworkTestFailureColor
            };

            string primaryLine = result.Summary;
            if (!string.IsNullOrEmpty(result.PrimaryLanIpv4))
                primaryLine = $"{primaryLine} IPv4: {result.PrimaryLanIpv4}";

            SetNetworkTestStatus(primaryLine, statusColor);
            Debug.Log($"[SettingsUI] LAN test: {result.Summary} {result.Details}");
        }

        private static bool ShouldRequestFirewallApproval(LanConnectivityTestResult result)
        {
            return result.State == LanConnectivityTestState.Warning &&
                   result.FirewallCheckSupported &&
                   !result.PrivateNetworkAllowed;
        }

        private void SetNetworkTestStatus(string message, Color color)
        {
            if (networkTestStatusText == null)
                return;

            networkTestStatusText.text = message;
            networkTestStatusText.color = color;
        }

        private void SetTestButtonState(bool interactable, string label)
        {
            if (testButton != null)
                testButton.interactable = interactable;

            if (testButton == null)
                return;

            TextMeshProUGUI text = testButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null)
                text.text = label;
        }
    }
}
