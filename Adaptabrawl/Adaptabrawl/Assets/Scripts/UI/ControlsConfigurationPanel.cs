using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Adaptabrawl.Settings;

namespace Adaptabrawl.UI
{
    public class ControlsConfigurationPanel : MonoBehaviour
    {
        private enum ControlsTab
        {
            Keyboard1P,
            Keyboard2P,
            Controller,
            Global
        }

        private static readonly ControlActionId[] GameplayActions =
        {
            ControlActionId.MoveLeft,
            ControlActionId.MoveRight,
            ControlActionId.Crouch,
            ControlActionId.Jump,
            ControlActionId.Attack,
            ControlActionId.Block,
            ControlActionId.Dodge,
            ControlActionId.SpecialLight,
            ControlActionId.SpecialHeavy
        };

        private static readonly ControlActionId[] GlobalActions =
        {
            ControlActionId.ReadyUp,
            ControlActionId.Pause,
            ControlActionId.BackCancel
        };

        private static readonly KeyCode[] RebindableKeys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
        private static readonly ControlBindingKind[] ControllerCaptureBindings =
        {
            ControlBindingKind.GamepadSouth,
            ControlBindingKind.GamepadEast,
            ControlBindingKind.GamepadWest,
            ControlBindingKind.GamepadNorth,
            ControlBindingKind.GamepadLeftShoulder,
            ControlBindingKind.GamepadRightShoulder,
            ControlBindingKind.GamepadLeftTrigger,
            ControlBindingKind.GamepadRightTrigger,
            ControlBindingKind.GamepadStart,
            ControlBindingKind.GamepadSelect,
            ControlBindingKind.GamepadDpadLeft,
            ControlBindingKind.GamepadDpadRight,
            ControlBindingKind.GamepadDpadUp,
            ControlBindingKind.GamepadDpadDown,
            ControlBindingKind.GamepadLeftStickLeft,
            ControlBindingKind.GamepadLeftStickRight,
            ControlBindingKind.GamepadLeftStickUp,
            ControlBindingKind.GamepadLeftStickDown
        };

        private readonly Dictionary<ControlsTab, Button> tabButtons = new Dictionary<ControlsTab, Button>();
        private readonly List<Selectable> activeSelectables = new List<Selectable>();

        private RectTransform rootRect;
        private RectTransform panelRect;
        private RectTransform contentRect;
        private GameObject captureOverlay;
        private TextMeshProUGUI capturePrompt;
        private Button captureCancelButton;
        private Button closeButton;
        private Button resetTabButton;
        private ControlsTab activeTab;
        private ControlProfileId captureProfile;
        private ControlActionId captureAction;
        private bool isBuilt;
        private bool isCapturing;

        public bool IsOpen => isBuilt && rootRect != null && rootRect.gameObject.activeSelf;
        public bool IsCapturing => isCapturing;

        public void Show()
        {
            EnsureBuilt();
            rootRect.gameObject.SetActive(true);
            Refresh();
        }

        public void Hide()
        {
            if (!isBuilt || rootRect == null)
                return;

            isCapturing = false;
            if (captureOverlay != null)
                captureOverlay.SetActive(false);

            rootRect.gameObject.SetActive(false);
        }

        public bool HandleBackRequested()
        {
            if (!IsOpen || isCapturing)
                return false;

            Hide();
            return true;
        }

        private void Update()
        {
            if (!isCapturing)
                return;

            ControlBinding captured = IsControllerProfile(captureProfile)
                ? TryCaptureControllerBinding()
                : TryCaptureKeyboardBinding();

            if (captured == null)
                return;

            ControlBindingsContext.EnsureExists().SetSingleBinding(captureProfile, captureAction, captured);
            isCapturing = false;
            if (captureOverlay != null)
                captureOverlay.SetActive(false);
            Refresh();
        }

        private void EnsureBuilt()
        {
            if (isBuilt)
                return;

            rootRect = CreateRect("ControlsConfigurationOverlay", transform);
            Stretch(rootRect);
            AddImage(rootRect.gameObject, new Color(0f, 0f, 0f, 0.72f));

            panelRect = CreateRect("Panel", rootRect);
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(1220f, 760f);
            AddImage(panelRect.gameObject, new Color(0.06f, 0.06f, 0.08f, 0.98f));
            panelRect.gameObject.AddComponent<Outline>().effectColor = new Color(0.70f, 0.70f, 0.70f, 0.75f);

            BuildHeader();
            BuildTabs();
            BuildBody();
            BuildFooter();
            BuildCaptureOverlay();

            rootRect.gameObject.SetActive(false);
            isBuilt = true;
        }

        private void Refresh()
        {
            if (!isBuilt)
                return;

            if (activeTab == ControlsTab.Controller && !ControlBindingsContext.EnsureExists().HasConnectedController)
                activeTab = ControlsTab.Keyboard1P;

            foreach (Transform child in contentRect)
                Destroy(child.gameObject);

            activeSelectables.Clear();
            foreach (var pair in tabButtons)
            {
                if (pair.Value == null)
                    continue;

                bool visible = pair.Key != ControlsTab.Controller || ControlBindingsContext.EnsureExists().HasConnectedController;
                pair.Value.gameObject.SetActive(visible);
                if (!visible)
                    continue;

                StyleTabButton(pair.Key, pair.Value);
                activeSelectables.Add(pair.Value);
            }

            switch (activeTab)
            {
                case ControlsTab.Keyboard1P:
                    BuildSingleProfileTable(ControlProfileId.Keyboard1P, GameplayActions);
                    break;
                case ControlsTab.Keyboard2P:
                    BuildDualKeyboardTable();
                    break;
                case ControlsTab.Controller:
                    BuildSingleProfileTable(ControlProfileId.Controller, GameplayActions);
                    break;
                case ControlsTab.Global:
                    BuildGlobalTables();
                    break;
            }

            if (resetTabButton != null)
                activeSelectables.Add(resetTabButton);
            if (closeButton != null)
                activeSelectables.Add(closeButton);

            MenuNavigationGroup.ApplyVerticalChain(activeSelectables, wrap: false);
            MenuNavigationGroup.SelectFirstAvailable(activeSelectables);
        }

        private void StartCapture(ControlProfileId profile, ControlActionId action)
        {
            isCapturing = true;
            captureProfile = profile;
            captureAction = action;
            if (capturePrompt != null)
            {
                string deviceLabel = IsControllerProfile(profile) ? "controller input" : "key or mouse input";
                capturePrompt.text = $"Press a {deviceLabel} for\n{ControlBindingsContext.EnsureExists().GetActionLabel(action)}";
            }

            if (captureOverlay != null)
                captureOverlay.SetActive(true);

            if (captureCancelButton != null)
                captureCancelButton.Select();
        }

        private void CancelCapture()
        {
            isCapturing = false;
            if (captureOverlay != null)
                captureOverlay.SetActive(false);
        }

        private static bool IsControllerProfile(ControlProfileId profile)
        {
            return profile == ControlProfileId.Controller || profile == ControlProfileId.GlobalController;
        }

        private void BuildHeader()
        {
            RectTransform header = CreateRect("Header", panelRect);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.offsetMin = new Vector2(0f, -92f);
            header.offsetMax = Vector2.zero;
            AddImage(header.gameObject, new Color(0.03f, 0.03f, 0.04f, 1f));

            TextMeshProUGUI title = CreateLabel("Title", header, "CONFIGURATIONS PANEL", 30, FontStyles.Bold, TextAlignmentOptions.Center);
            Stretch(title.rectTransform);
        }

        private void BuildTabs()
        {
            RectTransform tabs = CreateRect("Tabs", panelRect);
            tabs.anchorMin = new Vector2(0f, 1f);
            tabs.anchorMax = new Vector2(1f, 1f);
            tabs.pivot = new Vector2(0.5f, 1f);
            tabs.offsetMin = new Vector2(42f, -150f);
            tabs.offsetMax = new Vector2(-42f, -98f);

            var layout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;

            activeTab = ControlsTab.Keyboard1P;
            CreateTabButton(tabs, ControlsTab.Keyboard1P, "Keyboard (1P)");
            CreateTabButton(tabs, ControlsTab.Keyboard2P, "Keyboard (2P)");
            CreateTabButton(tabs, ControlsTab.Controller, "Controller");
            CreateTabButton(tabs, ControlsTab.Global, "Global");
        }

        private void BuildBody()
        {
            RectTransform body = CreateRect("Body", panelRect);
            body.anchorMin = new Vector2(0f, 0f);
            body.anchorMax = new Vector2(1f, 1f);
            body.offsetMin = new Vector2(42f, 86f);
            body.offsetMax = new Vector2(-42f, -162f);
            AddImage(body.gameObject, new Color(0.09f, 0.09f, 0.11f, 1f));

            RectTransform viewport = CreateRect("Viewport", body);
            Stretch(viewport);
            var maskImage = AddImage(viewport.gameObject, new Color(1f, 1f, 1f, 0.02f));
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            contentRect = CreateRect("Content", viewport);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            var contentLayout = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(18, 18, 18, 18);
            contentLayout.spacing = 12f;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentRect.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = body.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.scrollSensitivity = 28f;
            scroll.movementType = ScrollRect.MovementType.Clamped;
        }

        private void BuildFooter()
        {
            RectTransform footer = CreateRect("Footer", panelRect);
            footer.anchorMin = new Vector2(0f, 0f);
            footer.anchorMax = new Vector2(1f, 0f);
            footer.pivot = new Vector2(0.5f, 0f);
            footer.offsetMin = new Vector2(42f, 28f);
            footer.offsetMax = new Vector2(-42f, 78f);

            var layout = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
            layout.childAlignment = TextAnchor.MiddleRight;

            resetTabButton = CreateActionButton(footer, "Reset Tab", () =>
            {
                foreach (var profile in GetProfilesForActiveTab())
                    ControlBindingsContext.EnsureExists().ResetProfile(profile);
                Refresh();
            }, new Color(0.16f, 0.16f, 0.18f, 1f));

            closeButton = CreateActionButton(footer, "Close", Hide, new Color(0.92f, 0.92f, 0.92f, 1f), Color.black);
        }

        private void BuildCaptureOverlay()
        {
            captureOverlay = CreateRect("CaptureOverlay", rootRect).gameObject;
            Stretch(captureOverlay.GetComponent<RectTransform>());
            AddImage(captureOverlay, new Color(0f, 0f, 0f, 0.78f));

            RectTransform dialog = CreateRect("Dialog", captureOverlay.transform);
            dialog.anchorMin = new Vector2(0.5f, 0.5f);
            dialog.anchorMax = new Vector2(0.5f, 0.5f);
            dialog.pivot = new Vector2(0.5f, 0.5f);
            dialog.sizeDelta = new Vector2(480f, 220f);
            AddImage(dialog.gameObject, new Color(0.08f, 0.08f, 0.1f, 1f));
            dialog.gameObject.AddComponent<Outline>().effectColor = new Color(0.85f, 0.85f, 0.85f, 0.6f);

            capturePrompt = CreateLabel("Prompt", dialog, "Press an input", 26, FontStyles.Bold, TextAlignmentOptions.Center);
            capturePrompt.rectTransform.anchorMin = new Vector2(0f, 0.45f);
            capturePrompt.rectTransform.anchorMax = new Vector2(1f, 1f);
            capturePrompt.rectTransform.offsetMin = new Vector2(20f, 0f);
            capturePrompt.rectTransform.offsetMax = new Vector2(-20f, -18f);

            TextMeshProUGUI help = CreateLabel("Help", dialog, "Use the Cancel button to abort rebinding.", 17, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.85f, 0.85f));
            help.rectTransform.anchorMin = new Vector2(0f, 0.24f);
            help.rectTransform.anchorMax = new Vector2(1f, 0.48f);
            help.rectTransform.offsetMin = new Vector2(24f, 0f);
            help.rectTransform.offsetMax = new Vector2(-24f, 0f);

            RectTransform buttonRow = CreateRect("Buttons", dialog);
            buttonRow.anchorMin = new Vector2(0f, 0f);
            buttonRow.anchorMax = new Vector2(1f, 0.24f);
            buttonRow.offsetMin = new Vector2(24f, 20f);
            buttonRow.offsetMax = new Vector2(-24f, -18f);
            var layout = buttonRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;

            captureCancelButton = CreateActionButton(buttonRow, "Cancel", CancelCapture, new Color(0.18f, 0.18f, 0.20f, 1f));
            captureOverlay.SetActive(false);
        }

        private void CreateTabButton(RectTransform parent, ControlsTab tab, string label)
        {
            Button button = CreateActionButton(parent, label, () =>
            {
                activeTab = tab;
                Refresh();
            }, new Color(0.18f, 0.18f, 0.20f, 1f));

            tabButtons[tab] = button;
        }

        private void StyleTabButton(ControlsTab tab, Button button)
        {
            if (button == null || button.targetGraphic is not Image image)
                return;

            bool active = tab == activeTab;
            image.color = active ? new Color(0.92f, 0.92f, 0.92f, 1f) : new Color(0.16f, 0.16f, 0.18f, 1f);
            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.color = active ? Color.black : Color.white;
                label.fontStyle = FontStyles.Bold;
            }
        }

        private void BuildSingleProfileTable(ControlProfileId profile, IReadOnlyList<ControlActionId> actions)
        {
            BuildTableHeader("Action", "Default", "Current");
            foreach (var action in actions)
            {
                BuildThreeColumnRow(
                    ControlBindingsContext.EnsureExists().GetActionLabel(action),
                    ControlBindingsContext.EnsureExists().GetDefaultBindingsLabel(profile, action),
                    ControlBindingsContext.EnsureExists().GetBindingsLabel(profile, action),
                    () => StartCapture(profile, action));
            }
        }

        private void BuildDualKeyboardTable()
        {
            BuildTableHeader("Action", "Default (P1)", "Current (P1)", "Default (P2)", "Current (P2)");
            foreach (var action in GameplayActions)
            {
                BuildFiveColumnRow(
                    ControlBindingsContext.EnsureExists().GetActionLabel(action),
                    ControlBindingsContext.EnsureExists().GetDefaultBindingsLabel(ControlProfileId.Keyboard2PPlayer1, action),
                    ControlBindingsContext.EnsureExists().GetBindingsLabel(ControlProfileId.Keyboard2PPlayer1, action),
                    () => StartCapture(ControlProfileId.Keyboard2PPlayer1, action),
                    ControlBindingsContext.EnsureExists().GetDefaultBindingsLabel(ControlProfileId.Keyboard2PPlayer2, action),
                    ControlBindingsContext.EnsureExists().GetBindingsLabel(ControlProfileId.Keyboard2PPlayer2, action),
                    () => StartCapture(ControlProfileId.Keyboard2PPlayer2, action));
            }
        }

        private void BuildGlobalTables()
        {
            BuildSectionLabel("Keyboard");
            BuildTableHeader("Action", "Default (P1)", "Current (P1)", "Default (P2)", "Current (P2)");
            foreach (var action in GlobalActions)
            {
                BuildFiveColumnRow(
                    ControlBindingsContext.EnsureExists().GetActionLabel(action),
                    ControlBindingsContext.EnsureExists().GetDefaultBindingsLabel(ControlProfileId.GlobalKeyboardPlayer1, action),
                    ControlBindingsContext.EnsureExists().GetBindingsLabel(ControlProfileId.GlobalKeyboardPlayer1, action),
                    () => StartCapture(ControlProfileId.GlobalKeyboardPlayer1, action),
                    ControlBindingsContext.EnsureExists().GetDefaultBindingsLabel(ControlProfileId.GlobalKeyboardPlayer2, action),
                    ControlBindingsContext.EnsureExists().GetBindingsLabel(ControlProfileId.GlobalKeyboardPlayer2, action),
                    () => StartCapture(ControlProfileId.GlobalKeyboardPlayer2, action));
            }

            BuildSpacer();
            BuildSectionLabel("Controller");
            BuildTableHeader("Action", "Default", "Current");
            foreach (var action in GlobalActions)
            {
                bool allowClick = ControlBindingsContext.EnsureExists().HasConnectedController;
                BuildThreeColumnRow(
                    ControlBindingsContext.EnsureExists().GetActionLabel(action),
                    ControlBindingsContext.EnsureExists().GetDefaultBindingsLabel(ControlProfileId.GlobalController, action),
                    ControlBindingsContext.EnsureExists().GetBindingsLabel(ControlProfileId.GlobalController, action),
                    allowClick ? () => StartCapture(ControlProfileId.GlobalController, action) : null);
            }
        }

        private void BuildSectionLabel(string label)
        {
            RectTransform row = CreateRow();
            AddImage(row.gameObject, new Color(0.12f, 0.12f, 0.14f, 1f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 8, 8);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            CreateLabel("SectionLabel", row, label.ToUpperInvariant(), 22, FontStyles.Bold, TextAlignmentOptions.Left);
        }

        private void BuildSpacer()
        {
            RectTransform spacer = CreateRow();
            spacer.sizeDelta = new Vector2(0f, 8f);
            spacer.gameObject.AddComponent<LayoutElement>().preferredHeight = 8f;
        }

        private void BuildTableHeader(params string[] labels)
        {
            RectTransform row = CreateRow();
            AddImage(row.gameObject, new Color(0.12f, 0.12f, 0.14f, 1f));
            ConfigureRowLayout(row);
            float[] widths = GetColumnWidths(labels.Length);
            for (int i = 0; i < labels.Length; i++)
            {
                CreateCellLabel(row, labels[i], 18, FontStyles.Bold, widths[i], TextAlignmentOptions.Center);
            }
        }

        private void BuildThreeColumnRow(string actionLabel, string defaultLabel, string currentLabel, Action currentClick)
        {
            RectTransform row = CreateRow();
            ConfigureRowLayout(row);
            float[] widths = GetColumnWidths(3);
            CreateCellLabel(row, actionLabel, 18, FontStyles.Normal, widths[0], TextAlignmentOptions.Left);
            CreateCellLabel(row, defaultLabel, 17, FontStyles.Normal, widths[1], TextAlignmentOptions.Center, new Color(0.82f, 0.82f, 0.82f, 1f));
            CreateCurrentBindingButton(row, currentLabel, currentClick, widths[2]);
        }

        private void BuildFiveColumnRow(string actionLabel, string defaultLeft, string currentLeft, Action currentLeftClick, string defaultRight, string currentRight, Action currentRightClick)
        {
            RectTransform row = CreateRow();
            ConfigureRowLayout(row);
            float[] widths = GetColumnWidths(5);
            CreateCellLabel(row, actionLabel, 18, FontStyles.Normal, widths[0], TextAlignmentOptions.Left);
            CreateCellLabel(row, defaultLeft, 16, FontStyles.Normal, widths[1], TextAlignmentOptions.Center, new Color(0.82f, 0.82f, 0.82f, 1f));
            CreateCurrentBindingButton(row, currentLeft, currentLeftClick, widths[2]);
            CreateCellLabel(row, defaultRight, 16, FontStyles.Normal, widths[3], TextAlignmentOptions.Center, new Color(0.82f, 0.82f, 0.82f, 1f));
            CreateCurrentBindingButton(row, currentRight, currentRightClick, widths[4]);
        }

        private RectTransform CreateRow()
        {
            RectTransform row = CreateRect("Row", contentRect);
            row.sizeDelta = new Vector2(0f, 60f);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
            return row;
        }

        private void ConfigureRowLayout(RectTransform row)
        {
            AddImage(row.gameObject, new Color(0.10f, 0.10f, 0.12f, 1f));
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 10f;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
        }

        private void CreateCellLabel(RectTransform parent, string text, float fontSize, FontStyles fontStyle, float widthFraction, TextAlignmentOptions alignment, Color? color = null)
        {
            RectTransform cell = CreateRect("Cell", parent);
            var layout = cell.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = widthFraction;
            layout.preferredWidth = 0f;
            layout.minWidth = 0f;
            TextMeshProUGUI label = CreateLabel("Text", cell, text, fontSize, fontStyle, alignment, color ?? Color.white);
            label.textWrappingMode = TextWrappingModes.Normal;
        }

        private void CreateCurrentBindingButton(RectTransform parent, string label, Action onClick, float widthFraction)
        {
            Button button = CreateActionButton(parent, label, onClick, new Color(0.18f, 0.18f, 0.20f, 1f));
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.flexibleWidth = widthFraction;
            layout.preferredWidth = 0f;
            layout.minWidth = 0f;
            button.interactable = onClick != null;
            if (onClick != null)
                activeSelectables.Add(button);
        }

        private static float[] GetColumnWidths(int columnCount)
        {
            return columnCount switch
            {
                3 => new[] { 1f, 1f, 1f },
                5 => new[] { 1f, 1f, 1f, 1f, 1f },
                _ => new[] { 1f }
            };
        }

        private IReadOnlyList<ControlProfileId> GetProfilesForActiveTab()
        {
            return activeTab switch
            {
                ControlsTab.Keyboard1P => new[] { ControlProfileId.Keyboard1P },
                ControlsTab.Keyboard2P => new[] { ControlProfileId.Keyboard2PPlayer1, ControlProfileId.Keyboard2PPlayer2 },
                ControlsTab.Controller => new[] { ControlProfileId.Controller },
                ControlsTab.Global => new[] { ControlProfileId.GlobalKeyboardPlayer1, ControlProfileId.GlobalKeyboardPlayer2, ControlProfileId.GlobalController },
                _ => Array.Empty<ControlProfileId>()
            };
        }

        private ControlBinding TryCaptureKeyboardBinding()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
                return ControlBinding.Binding(ControlBindingKind.MouseLeft);

            if (UnityEngine.Input.GetMouseButtonDown(1))
                return ControlBinding.Binding(ControlBindingKind.MouseRight);

            foreach (var keyCode in RebindableKeys)
            {
                if (keyCode == KeyCode.None)
                    continue;

                if (UnityEngine.Input.GetKeyDown(keyCode))
                    return ControlBinding.Key(keyCode);
            }

            return null;
        }

        private ControlBinding TryCaptureControllerBinding()
        {
            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                if (Gamepad.all[i] == null)
                    continue;

                foreach (var bindingKind in ControllerCaptureBindings)
                {
                    var candidate = ControlBinding.Binding(bindingKind);
                    if (WasControllerBindingPressed(candidate, i))
                        return candidate;
                }
            }

            return null;
        }

        private bool WasControllerBindingPressed(ControlBinding binding, int gamepadIndex)
        {
            return ManualGamepadCapture(binding.kind, gamepadIndex);
        }

        private static bool ManualGamepadCapture(ControlBindingKind kind, int gamepadIndex)
        {
            if (gamepadIndex < 0 || gamepadIndex >= Gamepad.all.Count || Gamepad.all[gamepadIndex] == null)
                return false;

            var pad = Gamepad.all[gamepadIndex];
            return kind switch
            {
                ControlBindingKind.GamepadSouth => pad.buttonSouth.wasPressedThisFrame,
                ControlBindingKind.GamepadEast => pad.buttonEast.wasPressedThisFrame,
                ControlBindingKind.GamepadWest => pad.buttonWest.wasPressedThisFrame,
                ControlBindingKind.GamepadNorth => pad.buttonNorth.wasPressedThisFrame,
                ControlBindingKind.GamepadLeftShoulder => pad.leftShoulder.wasPressedThisFrame,
                ControlBindingKind.GamepadRightShoulder => pad.rightShoulder.wasPressedThisFrame,
                ControlBindingKind.GamepadLeftTrigger => pad.leftTrigger.wasPressedThisFrame,
                ControlBindingKind.GamepadRightTrigger => pad.rightTrigger.wasPressedThisFrame,
                ControlBindingKind.GamepadStart => pad.startButton.wasPressedThisFrame,
                ControlBindingKind.GamepadSelect => pad.selectButton.wasPressedThisFrame,
                ControlBindingKind.GamepadDpadLeft => pad.dpad.left.wasPressedThisFrame,
                ControlBindingKind.GamepadDpadRight => pad.dpad.right.wasPressedThisFrame,
                ControlBindingKind.GamepadDpadUp => pad.dpad.up.wasPressedThisFrame,
                ControlBindingKind.GamepadDpadDown => pad.dpad.down.wasPressedThisFrame,
                ControlBindingKind.GamepadLeftStickLeft => pad.leftStick.left.wasPressedThisFrame,
                ControlBindingKind.GamepadLeftStickRight => pad.leftStick.right.wasPressedThisFrame,
                ControlBindingKind.GamepadLeftStickUp => pad.leftStick.up.wasPressedThisFrame,
                ControlBindingKind.GamepadLeftStickDown => pad.leftStick.down.wasPressedThisFrame,
                _ => false
            };
        }

        private static Button CreateActionButton(Transform parent, string label, Action onClick, Color backgroundColor, Color? textColor = null)
        {
            RectTransform rect = CreateRect("Button", parent);
            AddImage(rect.gameObject, backgroundColor);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            if (onClick != null)
                button.onClick.AddListener(() => onClick());

            TextMeshProUGUI text = CreateLabel("Label", rect, label, 18, FontStyles.Bold, TextAlignmentOptions.Center, textColor ?? Color.white);
            Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.Normal;
            rect.sizeDelta = new Vector2(220f, 46f);
            return button;
        }

        private static TextMeshProUGUI CreateLabel(string name, Transform parent, string text, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color? color = null)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = color ?? Color.white;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            if (TMP_Settings.defaultFontAsset != null)
                label.font = TMP_Settings.defaultFontAsset;
            return label;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Image AddImage(GameObject go, Color color)
        {
            Image image = go.GetComponent<Image>();
            if (image == null)
                image = go.AddComponent<Image>();
            image.color = color;
            return image;
        }
    }
}
