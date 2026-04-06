using System.Collections.Generic;
using System.Linq;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Settings;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Adaptabrawl.UI
{
    public sealed class GameplayControlsLegendInstaller : MonoBehaviour
    {
        private const string GameSceneName = "GameScene";
        private const string OnlineGameSceneName = "OnlineGameScene";

        private static GameplayControlsLegendInstaller instance;

        private GameplayControlsLegendOverlay activeOverlay;
        private float nextAttachAttemptTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            GameObject installerObject = new GameObject("GameplayControlsLegendInstaller");
            instance = installerObject.AddComponent<GameplayControlsLegendInstaller>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            nextAttachAttemptTime = 0f;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (!IsSupportedScene())
            {
                activeOverlay = null;
                return;
            }

            if (activeOverlay != null && activeOverlay.isActiveAndEnabled)
                return;

            if (Time.unscaledTime < nextAttachAttemptTime)
                return;

            nextAttachAttemptTime = Time.unscaledTime + 0.5f;
            TryAttachOverlay();
        }

        private void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            activeOverlay = null;
            nextAttachAttemptTime = Time.unscaledTime + 0.15f;
        }

        private void TryAttachOverlay()
        {
            if (!IsSupportedScene())
                return;

            Canvas hostCanvas = ResolveHostCanvas();
            if (hostCanvas == null)
                return;

            GameplayControlsLegendOverlay existing = hostCanvas.GetComponentInChildren<GameplayControlsLegendOverlay>(true);
            if (existing != null)
            {
                activeOverlay = existing;
                activeOverlay.RefreshLegend();
                return;
            }

            GameObject overlayObject = new GameObject("GameplayControlsLegendOverlay");
            overlayObject.transform.SetParent(hostCanvas.transform, false);
            activeOverlay = overlayObject.AddComponent<GameplayControlsLegendOverlay>();
            activeOverlay.Initialize(hostCanvas);
        }

        private static bool IsSupportedScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return sceneName == GameSceneName || sceneName == OnlineGameSceneName;
        }

        private static Canvas ResolveHostCanvas()
        {
            GameHUD gameHud = FindFirstObjectByType<GameHUD>(FindObjectsInactive.Include);
            if (gameHud != null)
            {
                Canvas hudCanvas = gameHud.GetComponentInParent<Canvas>();
                if (hudCanvas != null)
                    return hudCanvas;
            }

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return canvases
                .Where(canvas => canvas != null
                                 && canvas.isActiveAndEnabled
                                 && canvas.renderMode != RenderMode.WorldSpace
                                 && canvas.gameObject.scene == SceneManager.GetActiveScene()
                                 && canvas.name == "Canvas")
                .OrderByDescending(canvas => canvas.sortingOrder)
                .FirstOrDefault();
        }
    }

    public sealed class GameplayControlsLegendOverlay : MonoBehaviour
    {
        private static readonly ControlActionId[] LegendActions =
        {
            ControlActionId.Crouch,
            ControlActionId.Jump,
            ControlActionId.Attack,
            ControlActionId.Block,
            ControlActionId.Dodge,
            ControlActionId.SpecialLight,
            ControlActionId.SpecialHeavy
        };

        private const float PanelWidth = 316f;
        private const float PanelHeight = 214f;
        private const float SideMargin = 28f;
        private const float TopMargin = 74f;

        private static readonly Color PanelColor = new Color(0.44f, 0.44f, 0.47f, 0.88f);
        private static readonly Color ShadowColor = new Color(0f, 0f, 0f, 0.22f);
        private static readonly Color HeaderColor = new Color(0.97f, 0.97f, 0.98f, 1f);
        private static readonly Color BodyTextColor = new Color(0.97f, 0.97f, 0.98f, 1f);
        private static readonly Color DeviceTextColor = new Color(0.94f, 0.94f, 0.96f, 0.8f);

        private Canvas hostCanvas;
        private RectTransform rootRect;
        private TMP_FontAsset fontAsset;
        private LegendPanelView[] panelViews;

        public void Initialize(Canvas canvas)
        {
            hostCanvas = canvas;
            rootRect = gameObject.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);

            CanvasGroup canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            fontAsset = ResolveFontAsset();
            BuildLayout();
            RefreshLegend();
        }

        private void OnEnable()
        {
            ControlBindingsContext.EnsureExists().BindingsChanged += RefreshLegend;
        }

        private void OnDisable()
        {
            if (ControlBindingsContext.Instance != null)
                ControlBindingsContext.Instance.BindingsChanged -= RefreshLegend;
        }

        public void RefreshLegend()
        {
            if (hostCanvas == null || panelViews == null || panelViews.Length != 2)
                return;

            RefreshPanel(panelViews[0], 1);
            RefreshPanel(panelViews[1], 2);
        }

        private void BuildLayout()
        {
            panelViews = new[]
            {
                BuildPanel(rootRect, "Player 1", isRightAligned: false),
                BuildPanel(rootRect, "Player 2", isRightAligned: true)
            };
        }

        private LegendPanelView BuildPanel(Transform parent, string headerLabel, bool isRightAligned)
        {
            RectTransform cardRect = CreateRect($"{headerLabel.Replace(" ", string.Empty)}Legend", parent);
            cardRect.anchorMin = isRightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            cardRect.anchorMax = isRightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            cardRect.pivot = isRightAligned ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            cardRect.anchoredPosition = isRightAligned
                ? new Vector2(-SideMargin, -TopMargin)
                : new Vector2(SideMargin, -TopMargin);
            cardRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            Image panelImage = cardRect.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;

            Shadow shadow = cardRect.gameObject.AddComponent<Shadow>();
            shadow.effectColor = ShadowColor;
            shadow.effectDistance = new Vector2(0f, -4f);

            RectTransform contentRect = CreateRect("Content", cardRect);
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(18f, 14f);
            contentRect.offsetMax = new Vector2(-18f, -14f);

            VerticalLayoutGroup contentLayout = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 6f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            TextMeshProUGUI headerText = CreateText("Header", contentRect, headerLabel, 17f, FontStyles.Bold, TextAlignmentOptions.Center, HeaderColor);
            headerText.textWrappingMode = TextWrappingModes.NoWrap;
            headerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            TextMeshProUGUI deviceText = CreateText("Device", contentRect, "Keyboard", 11f, FontStyles.Normal, TextAlignmentOptions.Center, DeviceTextColor);
            deviceText.textWrappingMode = TextWrappingModes.NoWrap;
            deviceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            RectTransform dividerRect = CreateRect("Divider", contentRect);
            dividerRect.sizeDelta = new Vector2(0f, 1f);
            LayoutElement dividerLayout = dividerRect.gameObject.AddComponent<LayoutElement>();
            dividerLayout.preferredHeight = 1f;
            Image dividerImage = dividerRect.gameObject.AddComponent<Image>();
            dividerImage.color = new Color(1f, 1f, 1f, 0.2f);

            Dictionary<ControlActionId, TextMeshProUGUI> bindingLabels = new Dictionary<ControlActionId, TextMeshProUGUI>();
            foreach (ControlActionId action in LegendActions)
            {
                RectTransform rowRect = CreateRect($"Row_{action}", contentRect);
                rowRect.sizeDelta = new Vector2(0f, 20f);
                rowRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

                HorizontalLayoutGroup rowLayout = rowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 10f;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;
                rowLayout.childControlHeight = true;
                rowLayout.childControlWidth = true;
                rowLayout.childForceExpandHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.padding = new RectOffset(0, 0, 0, 0);

                TextMeshProUGUI actionText = CreateText("Action", rowRect, GetLegendActionLabel(action), 12f, FontStyles.Bold, TextAlignmentOptions.Left, BodyTextColor);
                LayoutElement actionLayout = actionText.gameObject.AddComponent<LayoutElement>();
                actionLayout.preferredWidth = 104f;

                TextMeshProUGUI bindingText = CreateText("Binding", rowRect, "Unbound", 12f, FontStyles.Normal, TextAlignmentOptions.Right, BodyTextColor);
                LayoutElement bindingLayout = bindingText.gameObject.AddComponent<LayoutElement>();
                bindingLayout.flexibleWidth = 1f;

                bindingLabels[action] = bindingText;
            }

            return new LegendPanelView
            {
                headerText = headerText,
                deviceText = deviceText,
                bindingLabels = bindingLabels
            };
        }

        private void RefreshPanel(LegendPanelView panelView, int playerNumber)
        {
            if (panelView == null)
                return;

            ControlBindingsContext bindings = ControlBindingsContext.EnsureExists();
            ResolvePlayerProfile(playerNumber, out ControlProfileId profile, out string deviceLabel);

            panelView.deviceText.text = deviceLabel;
            foreach (ControlActionId action in LegendActions)
            {
                if (!panelView.bindingLabels.TryGetValue(action, out TextMeshProUGUI bindingText) || bindingText == null)
                    continue;

                bindingText.text = bindings.GetBindingsLabel(profile, action);
            }
        }

        private static void ResolvePlayerProfile(int playerNumber, out ControlProfileId profile, out string deviceLabel)
        {
            int p1Device = LobbyContext.Instance != null ? LobbyContext.Instance.p1InputDevice : CharacterSelectData.finalP1ControllerIndex;
            int p2Device = LobbyContext.Instance != null ? LobbyContext.Instance.p2InputDevice : CharacterSelectData.finalP2ControllerIndex;
            bool dualKeyboardMode = LobbyContext.IsDualKeyboardMode(p1Device, p2Device);
            bool useController = playerNumber == 1 ? p1Device == 1 : p2Device == 1;

            if (useController)
            {
                profile = ControlProfileId.Controller;
                deviceLabel = "CONTROLLER";
                return;
            }

            profile = ControlBindingProfileResolver.ResolveGameplayKeyboardProfile(playerNumber, dualKeyboardMode);
            deviceLabel = dualKeyboardMode
                ? playerNumber == 1 ? "KEYBOARD 1P" : "KEYBOARD 2P"
                : "KEYBOARD";
        }

        private static string GetLegendActionLabel(ControlActionId action)
        {
            return action switch
            {
                ControlActionId.SpecialLight => "Special Light",
                ControlActionId.SpecialHeavy => "Special Heavy",
                _ => ControlBindingsContext.EnsureExists().GetActionLabel(action)
            };
        }

        private TMP_FontAsset ResolveFontAsset()
        {
            if (TMP_Settings.defaultFontAsset != null)
                return TMP_Settings.defaultFontAsset;

            return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        private RectTransform CreateRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = hostCanvas != null ? hostCanvas.gameObject.layer : 5;
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, string text, float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = fontAsset;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = fontStyle;
            label.alignment = alignment;
            label.color = color;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        private sealed class LegendPanelView
        {
            public TextMeshProUGUI headerText;
            public TextMeshProUGUI deviceText;
            public Dictionary<ControlActionId, TextMeshProUGUI> bindingLabels;
        }
    }
}
