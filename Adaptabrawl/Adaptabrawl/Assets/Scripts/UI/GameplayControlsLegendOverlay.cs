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

        private static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.11f, 0.94f);
        private static readonly Color OutlineColor = new Color(0.78f, 0.22f, 0.22f, 0.55f);
        private static readonly Color Player1TabColor = new Color(0.86f, 0.22f, 0.22f, 1f);
        private static readonly Color Player2TabColor = new Color(0.28f, 0.58f, 0.96f, 1f);
        private static readonly Color HeaderColor = new Color(0.97f, 0.97f, 0.98f, 1f);
        private static readonly Color ActionColor = new Color(0.76f, 0.79f, 0.84f, 1f);
        private static readonly Color BindingColor = Color.white;

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
            RectTransform stackRect = CreateRect("LegendStack", rootRect);
            stackRect.anchorMin = new Vector2(0f, 0.5f);
            stackRect.anchorMax = new Vector2(0f, 0.5f);
            stackRect.pivot = new Vector2(0f, 0.5f);
            stackRect.anchoredPosition = new Vector2(24f, 0f);
            stackRect.sizeDelta = new Vector2(344f, 660f);

            VerticalLayoutGroup stackLayout = stackRect.gameObject.AddComponent<VerticalLayoutGroup>();
            stackLayout.spacing = 18f;
            stackLayout.childAlignment = TextAnchor.MiddleLeft;
            stackLayout.childControlHeight = false;
            stackLayout.childControlWidth = true;
            stackLayout.childForceExpandHeight = false;
            stackLayout.childForceExpandWidth = false;

            panelViews = new[]
            {
                BuildPanel(stackRect, "P1", "PLAYER 1", Player1TabColor),
                BuildPanel(stackRect, "P2", "PLAYER 2", Player2TabColor)
            };
        }

        private LegendPanelView BuildPanel(Transform parent, string tabLabel, string headerPrefix, Color tabColor)
        {
            RectTransform cardRect = CreateRect($"{headerPrefix}Legend", parent);
            cardRect.sizeDelta = new Vector2(332f, 280f);

            LayoutElement layoutElement = cardRect.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 332f;
            layoutElement.preferredHeight = 280f;

            Image panelImage = cardRect.gameObject.AddComponent<Image>();
            panelImage.color = PanelColor;

            Outline outline = cardRect.gameObject.AddComponent<Outline>();
            outline.effectColor = OutlineColor;
            outline.effectDistance = new Vector2(2f, -2f);

            RectTransform tabRect = CreateRect("Tab", cardRect);
            tabRect.anchorMin = new Vector2(0f, 0f);
            tabRect.anchorMax = new Vector2(0f, 1f);
            tabRect.pivot = new Vector2(0f, 0.5f);
            tabRect.offsetMin = Vector2.zero;
            tabRect.offsetMax = new Vector2(34f, 0f);
            Image tabImage = tabRect.gameObject.AddComponent<Image>();
            tabImage.color = tabColor;

            TextMeshProUGUI tabText = CreateText("TabText", tabRect, tabLabel, 18f, FontStyles.Bold, TextAlignmentOptions.Center, HeaderColor);
            RectTransform tabTextRect = tabText.rectTransform;
            tabTextRect.anchorMin = new Vector2(0.5f, 0.5f);
            tabTextRect.anchorMax = new Vector2(0.5f, 0.5f);
            tabTextRect.sizeDelta = new Vector2(92f, 28f);
            tabTextRect.anchoredPosition = Vector2.zero;
            tabTextRect.localEulerAngles = new Vector3(0f, 0f, 90f);

            RectTransform contentRect = CreateRect("Content", cardRect);
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(50f, 16f);
            contentRect.offsetMax = new Vector2(-14f, -16f);

            VerticalLayoutGroup contentLayout = contentRect.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 10f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlHeight = false;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;

            TextMeshProUGUI headerText = CreateText("Header", contentRect, headerPrefix, 19f, FontStyles.Bold, TextAlignmentOptions.Left, HeaderColor);
            headerText.textWrappingMode = TextWrappingModes.NoWrap;
            headerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            Dictionary<ControlActionId, TextMeshProUGUI> bindingLabels = new Dictionary<ControlActionId, TextMeshProUGUI>();
            foreach (ControlActionId action in LegendActions)
            {
                RectTransform rowRect = CreateRect($"Row_{action}", contentRect);
                rowRect.sizeDelta = new Vector2(0f, 28f);
                rowRect.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

                HorizontalLayoutGroup rowLayout = rowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 8f;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;
                rowLayout.childControlHeight = true;
                rowLayout.childControlWidth = true;
                rowLayout.childForceExpandHeight = true;
                rowLayout.childForceExpandWidth = false;

                TextMeshProUGUI actionText = CreateText("Action", rowRect, GetLegendActionLabel(action), 15f, FontStyles.Bold, TextAlignmentOptions.Left, ActionColor);
                LayoutElement actionLayout = actionText.gameObject.AddComponent<LayoutElement>();
                actionLayout.preferredWidth = 118f;

                TextMeshProUGUI bindingText = CreateText("Binding", rowRect, "Unbound", 15f, FontStyles.Normal, TextAlignmentOptions.Right, BindingColor);
                LayoutElement bindingLayout = bindingText.gameObject.AddComponent<LayoutElement>();
                bindingLayout.flexibleWidth = 1f;

                bindingLabels[action] = bindingText;
            }

            return new LegendPanelView
            {
                headerPrefix = headerPrefix,
                headerText = headerText,
                bindingLabels = bindingLabels
            };
        }

        private void RefreshPanel(LegendPanelView panelView, int playerNumber)
        {
            if (panelView == null)
                return;

            ControlBindingsContext bindings = ControlBindingsContext.EnsureExists();
            ResolvePlayerProfile(playerNumber, out ControlProfileId profile, out string deviceLabel);

            panelView.headerText.text = $"{panelView.headerPrefix}  |  {deviceLabel}";
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
            public string headerPrefix;
            public TextMeshProUGUI headerText;
            public Dictionary<ControlActionId, TextMeshProUGUI> bindingLabels;
        }
    }
}
