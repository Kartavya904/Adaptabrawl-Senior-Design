using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Adaptabrawl.UI;
using Adaptabrawl.Settings;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Adaptabrawl.Editor
{
    /// <summary>
    /// Editor tool that auto-generates the complete Settings UI inside SettingsScene.
    /// Usage: Tools → Adaptabrawl → Build Settings UI
    /// </summary>
    public static class SettingsUIBuilder
    {
        // ─── Design constants ────────────────────────────────────────────────
        private static readonly Color BG_DARK        = new Color(0.07f, 0.07f, 0.10f, 1f);
        private static readonly Color PANEL_COLOR     = new Color(0.10f, 0.10f, 0.14f, 0.97f);
        private static readonly Color HEADER_COLOR    = new Color(0.85f, 0.20f, 0.20f, 1f);   // Adaptabrawl red
        private static readonly Color SECTION_COLOR   = new Color(0.14f, 0.14f, 0.20f, 1f);
        private static readonly Color SLIDER_BG       = new Color(0.18f, 0.18f, 0.24f, 1f);
        private static readonly Color SLIDER_FILL     = new Color(0.85f, 0.20f, 0.20f, 1f);
        private static readonly Color BUTTON_BACK     = new Color(0.20f, 0.20f, 0.28f, 1f);
        private static readonly Color BUTTON_APPLY    = new Color(0.15f, 0.60f, 0.25f, 1f);
        private static readonly Color BUTTON_RESET    = new Color(0.70f, 0.15f, 0.15f, 1f);
        private static readonly Color TEXT_PRIMARY    = new Color(0.95f, 0.95f, 0.95f, 1f);
        private static readonly Color TEXT_SECONDARY  = new Color(0.65f, 0.65f, 0.70f, 1f);
        private static readonly Color TOGGLE_ON       = new Color(0.85f, 0.20f, 0.20f, 1f);
        private static readonly Color DIVIDER_COLOR   = new Color(0.85f, 0.20f, 0.20f, 0.5f);

        private const float PANEL_WIDTH  = 820f;
        private const float PANEL_HEIGHT = 660f;
        private const float ROW_HEIGHT   = 52f;
        private const float SECTION_GAP  = 16f;

        // ─── Entry point ─────────────────────────────────────────────────────
        [MenuItem("Tools/Adaptabrawl/Build Settings UI")]
        public static void BuildSettingsUI()
        {
            // --- Validate scene --------------------------------------------------
            if (!EditorSceneManager.GetActiveScene().name.Contains("Settings"))
            {
                bool open = EditorUtility.DisplayDialog(
                    "Build Settings UI",
                    "This tool is designed to run inside SettingsScene.\n\nOpen SettingsScene now and rebuild?",
                    "Open & Build", "Cancel");

                if (!open) return;

                string[] guids = AssetDatabase.FindAssets("SettingsScene t:Scene");
                if (guids.Length == 0)
                {
                    EditorUtility.DisplayDialog("Error", "Could not find SettingsScene.unity in project.", "OK");
                    return;
                }
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                EditorSceneManager.OpenScene(path);
            }

            // --- Find or create Canvas -------------------------------------------
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // Configure CanvasScaler for 1920×1080 reference
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            // --- Clear old Settings UI children (if rebuilding) ------------------
            Transform oldSettings = canvas.transform.Find("SettingsRoot");
            if (oldSettings != null) Object.DestroyImmediate(oldSettings.gameObject);

            // --- Find or create SettingsManager GO --------------------------------
            SettingsManager settingsMgr = Object.FindFirstObjectByType<SettingsManager>();
            if (settingsMgr == null)
            {
                GameObject smGO = new GameObject("SettingsManager");
                settingsMgr = smGO.AddComponent<SettingsManager>();
            }

            // Ensure SettingsUI is on the same GameObject
            SettingsUI settingsUI = settingsMgr.GetComponent<SettingsUI>();
            if (settingsUI == null) settingsUI = settingsMgr.gameObject.AddComponent<SettingsUI>();

            // ─── Build root ───────────────────────────────────────────────────
            GameObject root = CreateRect("SettingsRoot", canvas.transform);
            FillParent(root);
            SetColor(root, BG_DARK);

            // ─── Center panel ────────────────────────────────────────────────
            GameObject panel = CreateRect("Panel", root.transform);
            SetAnchors(panel, 0.5f, 0.5f, 0.5f, 0.5f);
            SetSize(panel, PANEL_WIDTH, PANEL_HEIGHT);
            SetColor(panel, PANEL_COLOR);
            AddOutline(panel, DIVIDER_COLOR, 2f);

            // ─── Title bar ───────────────────────────────────────────────────
            GameObject titleBar = CreateRect("TitleBar", panel.transform);
            SetAnchors(titleBar, 0f, 1f, 1f, 1f);
            SetOffsets(titleBar, 0f, -70f, 0f, 0f);
            SetColor(titleBar, HEADER_COLOR);

            TextMeshProUGUI titleText = CreateTMPText("TitleText", titleBar.transform,
                "SETTINGS", 26, FontStyles.Bold, TextAlignmentOptions.Center, TEXT_PRIMARY);
            FillParent(titleText.gameObject);

            // Red accent divider
            GameObject divider = CreateRect("Divider", panel.transform);
            SetAnchors(divider, 0f, 1f, 1f, 1f);
            SetOffsets(divider, 20f, -72f, -20f, -70f);
            SetColor(divider, DIVIDER_COLOR);

            float contentTop = -90f;   // y offset from panel top

            // ─── AUDIO section ──────────────────────────────────────────────
            float sectionY = contentTop;

            GameObject audioLabel = CreateSectionLabel("AudioLabel", panel.transform,
                "-- AUDIO --", sectionY);
            sectionY -= 36f;

            Slider masterSlider; TextMeshProUGUI masterText;
            CreateSliderRow("MasterRow", panel.transform, "Master Volume", ref sectionY,
                out masterSlider, out masterText);

            Slider musicSlider; TextMeshProUGUI musicText;
            CreateSliderRow("MusicRow", panel.transform, "Music Volume", ref sectionY,
                out musicSlider, out musicText);

            Slider sfxSlider; TextMeshProUGUI sfxText;
            CreateSliderRow("SFXRow", panel.transform, "SFX Volume", ref sectionY,
                out sfxSlider, out sfxText);

            sectionY -= SECTION_GAP;
            CreateDividerLine("AudioDiv", panel.transform, sectionY);
            sectionY -= SECTION_GAP;

            // ─── VIDEO section ───────────────────────────────────────────────
            CreateSectionLabel("VideoLabel", panel.transform, "-- VIDEO --", sectionY);
            sectionY -= 36f;

            TMP_Dropdown qualityDrop; CreateDropdownRow("QualityRow", panel.transform,
                "Quality Level", ref sectionY, out qualityDrop);

            TMP_Dropdown resDrop; CreateDropdownRow("ResRow", panel.transform,
                "Resolution", ref sectionY, out resDrop);

            TMP_Dropdown fpsDrop; CreateDropdownRow("FPSRow", panel.transform,
                "Target FPS", ref sectionY, out fpsDrop);

            Toggle vsyncToggle; CreateToggleRow("VSyncRow", panel.transform,
                "VSync", ref sectionY, out vsyncToggle);

            sectionY -= SECTION_GAP;
            CreateDividerLine("VideoDiv", panel.transform, sectionY);
            sectionY -= SECTION_GAP;

            // ─── ACCESSIBILITY section ────────────────────────────────────────
            CreateSectionLabel("AccessLabel", panel.transform, "-- ACCESSIBILITY --", sectionY);
            sectionY -= 36f;

            Slider uiScaleSlider; TextMeshProUGUI uiScaleText;
            CreateSliderRow("UIScaleRow", panel.transform, "UI Scale", ref sectionY,
                out uiScaleSlider, out uiScaleText);

            Toggle colorBlindToggle; CreateToggleRow("ColorBlindRow", panel.transform,
                "Color Blind Mode", ref sectionY, out colorBlindToggle);

            Toggle hitboxToggle; CreateToggleRow("HitboxRow", panel.transform,
                "Show Hitboxes", ref sectionY, out hitboxToggle);

            // ─── Bottom buttons ───────────────────────────────────────────────
            Button backBtn   = CreateBottomButton("BackBtn",   panel.transform, "BACK",   BUTTON_BACK,  -1f);
            Button applyBtn  = CreateBottomButton("ApplyBtn",  panel.transform, "APPLY",  BUTTON_APPLY,  0f);
            Button resetBtn  = CreateBottomButton("ResetBtn",  panel.transform, "RESET",  BUTTON_RESET,  1f);

            // ─── Wire SettingsUI references via SerializedObject ──────────────
            SerializedObject so = new SerializedObject(settingsUI);

            so.FindProperty("settingsManager").objectReferenceValue  = settingsMgr;
            so.FindProperty("masterVolumeSlider").objectReferenceValue = masterSlider;
            so.FindProperty("musicVolumeSlider").objectReferenceValue  = musicSlider;
            so.FindProperty("sfxVolumeSlider").objectReferenceValue    = sfxSlider;
            so.FindProperty("masterVolumeText").objectReferenceValue   = masterText;
            so.FindProperty("musicVolumeText").objectReferenceValue    = musicText;
            so.FindProperty("sfxVolumeText").objectReferenceValue      = sfxText;
            so.FindProperty("qualityDropdown").objectReferenceValue    = qualityDrop;
            so.FindProperty("resolutionDropdown").objectReferenceValue = resDrop;
            so.FindProperty("vsyncToggle").objectReferenceValue        = vsyncToggle;
            so.FindProperty("fpsDropdown").objectReferenceValue        = fpsDrop;
            so.FindProperty("uiScaleSlider").objectReferenceValue      = uiScaleSlider;
            so.FindProperty("uiScaleText").objectReferenceValue        = uiScaleText;
            so.FindProperty("colorBlindToggle").objectReferenceValue   = colorBlindToggle;
            so.FindProperty("showHitboxesToggle").objectReferenceValue = hitboxToggle;
            so.FindProperty("backButton").objectReferenceValue         = backBtn;
            so.FindProperty("applyButton").objectReferenceValue        = applyBtn;
            so.FindProperty("resetButton").objectReferenceValue        = resetBtn;

            so.ApplyModifiedProperties();

            // ─── Save scene ───────────────────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

            EditorUtility.DisplayDialog(
                "✅ Settings UI Built",
                "All UI elements created and wired to SettingsUI!\n\nPress Play to test, or Ctrl+S to save.",
                "OK");

            Debug.Log("[SettingsUIBuilder] Settings UI built successfully.");
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: Create basic RectTransform GameObject
        // ─────────────────────────────────────────────────────────────────────
        private static GameObject CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void FillParent(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchors(GameObject go, float minX, float minY, float maxX, float maxY)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(minX, minY);
            rt.anchorMax = new Vector2(maxX, maxY);
            rt.anchoredPosition = Vector2.zero;
        }

        private static void SetOffsets(GameObject go, float left, float bottom, float right, float top)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(right, top);
        }

        private static void SetSize(GameObject go, float w, float h)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void SetColor(GameObject go, Color c)
        {
            var img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            img.color = c;
        }

        private static void AddOutline(GameObject go, Color c, float thickness)
        {
            // We use a simple Outline component for a subtle border
            var outline = go.AddComponent<Outline>();
            outline.effectColor = c;
            outline.effectDistance = new Vector2(thickness, -thickness);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: TextMeshPro text
        // ─────────────────────────────────────────────────────────────────────
        private static TextMeshProUGUI CreateTMPText(string name, Transform parent,
            string text, float size, FontStyles style,
            TextAlignmentOptions align, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.color = color;

            // Assign font — prevents "Can't Generate Mesh, No Font Asset" warning
            var defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null)
                tmp.font = defaultFont;
            else
            {
                // Fallback: load TMP's built-in LiberationSans from Resources
                var fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                if (fallback != null) tmp.font = fallback;
            }

            return tmp;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: Section header label
        // ─────────────────────────────────────────────────────────────────────
        private static GameObject CreateSectionLabel(string name, Transform parent,
            string text, float topY)
        {
            GameObject go = CreateRect(name, parent);
            SetAnchors(go, 0f, 1f, 1f, 1f);
            go.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, topY - 18f);
            SetSize(go, 0, 32f);
            SetOffsets(go, 24f, topY - 36f, -24f, topY);
            SetColor(go, SECTION_COLOR);

            var txt = CreateTMPText("Label", go.transform,
                text, 14f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HEADER_COLOR);
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(14f, 0f); rt.offsetMax = Vector2.zero;
            return go;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: Thin divider line
        // ─────────────────────────────────────────────────────────────────────
        private static void CreateDividerLine(string name, Transform parent, float y)
        {
            GameObject go = CreateRect(name, parent);
            SetAnchors(go, 0f, 1f, 1f, 1f);
            SetOffsets(go, 24f, y - 1f, -24f, y);
            SetColor(go, DIVIDER_COLOR);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: Row with label + slider + value text
        // ─────────────────────────────────────────────────────────────────────
        private static void CreateSliderRow(string name, Transform parent, string label,
            ref float topY, out Slider slider, out TextMeshProUGUI valueText)
        {
            float rowH = ROW_HEIGHT;
            GameObject row = CreateRect(name, parent);
            SetAnchors(row, 0f, 1f, 1f, 1f);
            SetOffsets(row, 24f, topY - rowH, -24f, topY);
            topY -= rowH + 4f;

            // Label
            var lbl = CreateTMPText("Label", row.transform,
                label, 13f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, TEXT_PRIMARY);
            var lblRT = lbl.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0f, 0f);
            lblRT.anchorMax = new Vector2(0.28f, 1f);
            lblRT.offsetMin = new Vector2(8f, 0f);
            lblRT.offsetMax = Vector2.zero;

            // Value text (right side)
            valueText = CreateTMPText("ValueText", row.transform,
                "100%", 13f, FontStyles.Bold, TextAlignmentOptions.MidlineRight, TEXT_SECONDARY);
            var valRT = valueText.GetComponent<RectTransform>();
            valRT.anchorMin = new Vector2(0.78f, 0f);
            valRT.anchorMax = new Vector2(1f, 1f);
            valRT.offsetMin = Vector2.zero;
            valRT.offsetMax = new Vector2(-8f, 0f);

            // Slider
            GameObject sliderGO = CreateRect("Slider", row.transform);
            var sliderRT = sliderGO.GetComponent<RectTransform>();
            sliderRT.anchorMin = new Vector2(0.28f, 0.2f);
            sliderRT.anchorMax = new Vector2(0.78f, 0.8f);
            sliderRT.offsetMin = new Vector2(8f, 0f);
            sliderRT.offsetMax = new Vector2(-8f, 0f);

            slider = BuildUnitySlider(sliderGO);
        }

        private static Slider BuildUnitySlider(GameObject sliderGO)
        {
            var slider = sliderGO.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.75f;

            // Background
            GameObject bg = CreateRect("Background", sliderGO.transform);
            FillParent(bg);
            SetColor(bg, SLIDER_BG);

            // Fill area
            GameObject fillArea = CreateRect("Fill Area", sliderGO.transform);
            var fillAreaRT = fillArea.GetComponent<RectTransform>();
            fillAreaRT.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRT.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRT.offsetMin = new Vector2(5f, 0f);
            fillAreaRT.offsetMax = new Vector2(-5f, 0f);

            GameObject fill = CreateRect("Fill", fillArea.transform);
            SetColor(fill, SLIDER_FILL);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0f, 1f);
            fillRT.offsetMin = Vector2.zero;
            fillRT.offsetMax = new Vector2(10f, 0f);

            // Handle area
            GameObject handleArea = CreateRect("Handle Slide Area", sliderGO.transform);
            FillParent(handleArea);

            GameObject handle = CreateRect("Handle", handleArea.transform);
            SetColor(handle, TEXT_PRIMARY);
            var handleRT = handle.GetComponent<RectTransform>();
            handleRT.anchorMin = new Vector2(0f, 0f);
            handleRT.anchorMax = new Vector2(0f, 1f);
            handleRT.sizeDelta = new Vector2(16f, 0f);

            slider.fillRect   = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;

            return slider;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: Row with label + dropdown
        // ─────────────────────────────────────────────────────────────────────
        private static void CreateDropdownRow(string name, Transform parent, string label,
            ref float topY, out TMP_Dropdown dropdown)
        {
            float rowH = ROW_HEIGHT;
            GameObject row = CreateRect(name, parent);
            SetAnchors(row, 0f, 1f, 1f, 1f);
            SetOffsets(row, 24f, topY - rowH, -24f, topY);
            topY -= rowH + 4f;

            var lbl = CreateTMPText("Label", row.transform,
                label, 13f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, TEXT_PRIMARY);
            var lblRT = lbl.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0f, 0f);
            lblRT.anchorMax = new Vector2(0.40f, 1f);
            lblRT.offsetMin = new Vector2(8f, 0f);
            lblRT.offsetMax = Vector2.zero;

            // Dropdown
            GameObject dropGO = CreateRect("Dropdown", row.transform);
            var dropRT = dropGO.GetComponent<RectTransform>();
            dropRT.anchorMin = new Vector2(0.42f, 0.1f);
            dropRT.anchorMax = new Vector2(1f, 0.9f);
            dropRT.offsetMin = Vector2.zero;
            dropRT.offsetMax = new Vector2(-8f, 0f);

            SetColor(dropGO, SLIDER_BG);
            dropdown = dropGO.AddComponent<TMP_Dropdown>();

            // Label inside dropdown
            var dropLabel = CreateTMPText("Label", dropGO.transform,
                "Option A", 12f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, TEXT_PRIMARY);
            var dropLabelRT = dropLabel.GetComponent<RectTransform>();
            dropLabelRT.anchorMin = Vector2.zero;
            dropLabelRT.anchorMax = Vector2.one;
            dropLabelRT.offsetMin = new Vector2(10f, 2f);
            dropLabelRT.offsetMax = new Vector2(-30f, -2f);

            // Arrow text
            var arrow = CreateTMPText("Arrow", dropGO.transform,
                "▼", 11f, FontStyles.Normal, TextAlignmentOptions.MidlineRight, TEXT_SECONDARY);
            var arrowRT = arrow.GetComponent<RectTransform>();
            arrowRT.anchorMin = new Vector2(1f, 0f);
            arrowRT.anchorMax = new Vector2(1f, 1f);
            arrowRT.sizeDelta = new Vector2(26f, 0f);
            arrowRT.anchoredPosition = new Vector2(-13f, 0f);

            dropdown.captionText = dropLabel;
            dropdown.AddOptions(new System.Collections.Generic.List<string> { "Option A", "Option B", "Option C" });
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: Row with label + toggle
        // ─────────────────────────────────────────────────────────────────────
        private static void CreateToggleRow(string name, Transform parent, string label,
            ref float topY, out Toggle toggle)
        {
            float rowH = ROW_HEIGHT;
            GameObject row = CreateRect(name, parent);
            SetAnchors(row, 0f, 1f, 1f, 1f);
            SetOffsets(row, 24f, topY - rowH, -24f, topY);
            topY -= rowH + 4f;

            var lbl = CreateTMPText("Label", row.transform,
                label, 13f, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, TEXT_PRIMARY);
            var lblRT = lbl.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0f, 0f);
            lblRT.anchorMax = new Vector2(0.70f, 1f);
            lblRT.offsetMin = new Vector2(8f, 0f);
            lblRT.offsetMax = Vector2.zero;

            // Toggle background
            GameObject toggleGO = CreateRect("Toggle", row.transform);
            var toggleRT = toggleGO.GetComponent<RectTransform>();
            toggleRT.anchorMin = new Vector2(0.72f, 0.2f);
            toggleRT.anchorMax = new Vector2(0.72f, 0.8f);
            toggleRT.sizeDelta = new Vector2(52f, 0f);
            toggleRT.anchoredPosition = new Vector2(26f, 0f);

            SetColor(toggleGO, SLIDER_BG);
            toggle = toggleGO.AddComponent<Toggle>();

            // Checkmark
            GameObject checkGO = CreateRect("Checkmark", toggleGO.transform);
            SetColor(checkGO, TOGGLE_ON);
            var checkImg = checkGO.GetComponent<Image>();
            var checkRT = checkGO.GetComponent<RectTransform>();
            checkRT.anchorMin = new Vector2(0.1f, 0.1f);
            checkRT.anchorMax = new Vector2(0.9f, 0.9f);
            checkRT.offsetMin = Vector2.zero;
            checkRT.offsetMax = Vector2.zero;

            toggle.graphic = checkImg;
            toggle.targetGraphic = toggleGO.GetComponent<Image>();
            toggle.isOn = false;

            // "ON"/"OFF" label
            var stateLabel = CreateTMPText("StateLabel", toggleGO.transform,
                "OFF", 11f, FontStyles.Bold, TextAlignmentOptions.Center, TEXT_SECONDARY);
            FillParent(stateLabel.gameObject);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Helper: Bottom navigation button
        // ─────────────────────────────────────────────────────────────────────
        private static Button CreateBottomButton(string name, Transform parent,
            string label, Color bgColor, float xSlot)
        {
            // xSlot: -1 = left, 0 = center, 1 = right
            float btnW = 200f, btnH = 44f;
            float spacing = 220f;

            GameObject btnGO = CreateRect(name, parent);
            SetAnchors(btnGO, 0.5f, 0f, 0.5f, 0f);
            btnGO.GetComponent<RectTransform>().anchoredPosition =
                new Vector2(xSlot * spacing, 34f);
            SetSize(btnGO, btnW, btnH);
            SetColor(btnGO, bgColor);

            var txt = CreateTMPText("Text", btnGO.transform,
                label, 13f, FontStyles.Bold, TextAlignmentOptions.Center, TEXT_PRIMARY);
            FillParent(txt.gameObject);

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnGO.GetComponent<Image>();
            return btn;
        }
    }
}
