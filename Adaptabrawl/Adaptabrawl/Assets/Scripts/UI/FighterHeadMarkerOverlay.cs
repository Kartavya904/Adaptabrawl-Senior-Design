using System.Collections.Generic;
using System.Linq;
using Adaptabrawl.Data;
using Adaptabrawl.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Adaptabrawl.UI
{
    /// <summary>
    /// Creates lightweight screen-space markers above each fighter's head so players can
    /// quickly identify P1/P2 and the current classification during gameplay.
    /// </summary>
    public sealed class FighterHeadMarkerInstaller : MonoBehaviour
    {
        private const string GameSceneName = "GameScene";
        private const string OnlineGameSceneName = "OnlineGameScene";

        private static FighterHeadMarkerInstaller instance;

        private FighterHeadMarkerOverlay activeOverlay;
        private float nextAttachAttemptTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            var installerObject = new GameObject("FighterHeadMarkerInstaller");
            instance = installerObject.AddComponent<FighterHeadMarkerInstaller>();
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

            FighterController[] fighters = FindObjectsByType<FighterController>(FindObjectsSortMode.None);
            if (fighters == null || fighters.Length == 0)
                return;

            FighterHeadMarkerOverlay existing = FindFirstObjectByType<FighterHeadMarkerOverlay>(FindObjectsInactive.Include);
            if (existing != null)
            {
                activeOverlay = existing;
                return;
            }

            Canvas hostCanvas = ResolveHostCanvas();
            if (hostCanvas == null)
                return;

            GameObject overlayObject = new GameObject("FighterHeadMarkerOverlay");
            overlayObject.transform.SetParent(hostCanvas.transform, false);
            activeOverlay = overlayObject.AddComponent<FighterHeadMarkerOverlay>();
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
                .Where(canvas => canvas != null && canvas.isActiveAndEnabled && canvas.renderMode != RenderMode.WorldSpace)
                .OrderByDescending(canvas => canvas.sortingOrder)
                .FirstOrDefault();
        }
    }

    public sealed class FighterHeadMarkerOverlay : MonoBehaviour
    {
        private const float RebindInterval = 0.5f;
        private const float EmphasisDuration = 3f;

        private readonly Dictionary<FighterController, MarkerWidget> widgets = new Dictionary<FighterController, MarkerWidget>();

        private Canvas hostCanvas;
        private RectTransform rootRect;
        private TMP_FontAsset fontAsset;
        private float nextRebindTime;

        public void Initialize(Canvas canvas)
        {
            hostCanvas = canvas;
            rootRect = gameObject.AddComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            fontAsset = ResolveFontAsset();
        }

        private void OnEnable()
        {
            nextRebindTime = 0f;
        }

        private void Update()
        {
            if (hostCanvas == null)
                return;

            if (Time.unscaledTime >= nextRebindTime)
            {
                RebuildBindings();
                nextRebindTime = Time.unscaledTime + RebindInterval;
            }

            UnityEngine.Camera worldCamera = UnityEngine.Camera.main;
            UnityEngine.Camera uiCamera = hostCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : hostCanvas.worldCamera != null
                    ? hostCanvas.worldCamera
                    : worldCamera;

            foreach (var kvp in widgets)
            {
                kvp.Value.Update(worldCamera, rootRect, uiCamera);
            }
        }

        private void OnDestroy()
        {
            foreach (var kvp in widgets)
            {
                if (kvp.Key != null)
                    kvp.Key.OnFighterDefinitionChanged -= OnFighterDefinitionChanged;

                kvp.Value.Dispose();
            }

            widgets.Clear();
        }

        private void RebuildBindings()
        {
            FighterController[] fighters = FindObjectsByType<FighterController>(FindObjectsSortMode.None)
                .Where(fighter => fighter != null)
                .OrderBy(fighter => fighter.PlayerNumber)
                .ToArray();

            HashSet<FighterController> liveSet = new HashSet<FighterController>(fighters);

            List<FighterController> staleKeys = new List<FighterController>();
            foreach (var kvp in widgets)
            {
                if (kvp.Key == null || !liveSet.Contains(kvp.Key))
                {
                    if (kvp.Key != null)
                        kvp.Key.OnFighterDefinitionChanged -= OnFighterDefinitionChanged;

                    kvp.Value.Dispose();
                    staleKeys.Add(kvp.Key);
                }
            }

            for (int i = 0; i < staleKeys.Count; i++)
                widgets.Remove(staleKeys[i]);

            for (int i = 0; i < fighters.Length; i++)
            {
                FighterController fighter = fighters[i];
                if (widgets.ContainsKey(fighter))
                    continue;

                MarkerWidget widget = new MarkerWidget(rootRect, fontAsset, fighter);
                widgets.Add(fighter, widget);
                fighter.OnFighterDefinitionChanged += OnFighterDefinitionChanged;
                widget.SetPlayStyle(fighter.FighterDef != null ? fighter.FighterDef.playStyle : FighterPlayStyle.Balanced, true);
            }
        }

        private void OnFighterDefinitionChanged(FighterController fighter, FighterDef fighterDef)
        {
            if (fighter == null)
                return;

            if (widgets.TryGetValue(fighter, out MarkerWidget widget))
            {
                FighterPlayStyle playStyle = fighterDef != null ? fighterDef.playStyle : FighterPlayStyle.Balanced;
                widget.SetPlayStyle(playStyle, true);
            }
        }

        private static TMP_FontAsset ResolveFontAsset()
        {
            if (TMP_Settings.defaultFontAsset != null)
                return TMP_Settings.defaultFontAsset;

            return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        }

        private sealed class MarkerWidget
        {
            private static readonly Color BubbleColor = new Color(0.03f, 0.04f, 0.06f, 0.95f);
            private static readonly Color LabelColor = Color.white;

            private readonly FighterController fighter;
            private readonly RectTransform root;
            private readonly RectTransform iconRoot;
            private readonly Image iconPlate;
            private readonly Image iconSymbol;

            private Transform cachedHeadBone;
            private Animator cachedAnimator;
            private float nextAnchorRefreshTime;
            private float emphasisEndTime;
            private FighterPlayStyle currentPlayStyle;
            private Color currentStyleColor;

            public MarkerWidget(RectTransform parent, TMP_FontAsset fontAsset, FighterController fighterController)
            {
                fighter = fighterController;

                root = CreateRect("HeadMarker_" + GetPlayerLabel(fighterController), parent, new Vector2(154f, 77f), Vector2.zero);

                RectTransform bubbleRect = CreateRect("Bubble", root, new Vector2(74f, 60f), new Vector2(-24f, 0f));
                Image bubbleImage = bubbleRect.gameObject.AddComponent<Image>();
                bubbleImage.sprite = MarkerSpriteFactory.GetBubbleSprite();
                bubbleImage.color = BubbleColor;
                bubbleImage.raycastTarget = false;

                RectTransform labelRect = CreateRect("Label", bubbleRect, new Vector2(52f, 34f), new Vector2(0f, 10f));
                TextMeshProUGUI label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
                label.font = fontAsset;
                label.text = GetPlayerLabel(fighterController);
                label.fontSize = 19f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = LabelColor;
                label.raycastTarget = false;

                iconRoot = CreateRect("StyleIcon", root, new Vector2(41f, 41f), new Vector2(34f, -1f));
                iconPlate = iconRoot.gameObject.AddComponent<Image>();
                iconPlate.sprite = MarkerSpriteFactory.GetBadgeSprite();
                iconPlate.raycastTarget = false;

                RectTransform symbolRect = CreateRect("Symbol", iconRoot, new Vector2(24f, 24f), Vector2.zero);
                iconSymbol = symbolRect.gameObject.AddComponent<Image>();
                iconSymbol.color = Color.white;
                iconSymbol.raycastTarget = false;
            }

            public void Dispose()
            {
                if (root != null)
                    Object.Destroy(root.gameObject);
            }

            public void SetPlayStyle(FighterPlayStyle playStyle, bool emphasize)
            {
                currentPlayStyle = playStyle;
                currentStyleColor = ResolveStyleColor(playStyle);
                iconPlate.color = currentStyleColor;
                iconSymbol.sprite = MarkerSpriteFactory.GetPlayStyleSprite(playStyle);

                if (emphasize)
                    emphasisEndTime = Time.unscaledTime + EmphasisDuration;
            }

            public void Update(UnityEngine.Camera worldCamera, RectTransform canvasRect, UnityEngine.Camera uiCamera)
            {
                if (fighter == null || root == null || canvasRect == null || worldCamera == null)
                {
                    SetVisible(false);
                    return;
                }

                if (!TryGetAnchorWorldPosition(out Vector3 anchorWorldPosition))
                {
                    SetVisible(false);
                    return;
                }

                Vector3 screenPoint = worldCamera.WorldToScreenPoint(anchorWorldPosition);
                if (screenPoint.z <= 0f)
                {
                    SetVisible(false);
                    return;
                }

                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
                {
                    SetVisible(false);
                    return;
                }

                SetVisible(true);
                root.anchoredPosition = localPoint;

                float emphasisRemaining = Mathf.Max(0f, emphasisEndTime - Time.unscaledTime);
                if (emphasisRemaining > 0f)
                {
                    float normalized = emphasisRemaining / EmphasisDuration;
                    float pulse = 1f + Mathf.Sin(Time.unscaledTime * 10f) * 0.12f * normalized;
                    iconRoot.localScale = Vector3.one * pulse;
                    iconPlate.color = Color.Lerp(currentStyleColor, Color.white, 0.18f * normalized);
                }
                else
                {
                    iconRoot.localScale = Vector3.one;
                    iconPlate.color = currentStyleColor;
                }
            }

            private bool TryGetAnchorWorldPosition(out Vector3 worldPosition)
            {
                RefreshAnchorReferencesIfNeeded();

                if (cachedHeadBone != null)
                {
                    float extraLift = 0.22f;
                    if (TryGetRendererBounds(ignoreWeapons: true, out Bounds bodyBounds))
                        extraLift = Mathf.Clamp(bodyBounds.size.y * 0.08f, 0.16f, 0.28f);

                    worldPosition = cachedHeadBone.position + Vector3.up * extraLift;
                    return true;
                }

                if (TryGetRendererBounds(ignoreWeapons: true, out Bounds filteredBounds)
                    || TryGetRendererBounds(ignoreWeapons: false, out filteredBounds))
                {
                    worldPosition = new Vector3(
                        filteredBounds.center.x,
                        filteredBounds.max.y + Mathf.Clamp(filteredBounds.size.y * 0.06f, 0.14f, 0.26f),
                        filteredBounds.center.z);
                    return true;
                }

                worldPosition = fighter.transform.position + Vector3.up * 2f;
                return true;
            }

            private void RefreshAnchorReferencesIfNeeded()
            {
                if (Time.unscaledTime < nextAnchorRefreshTime)
                    return;

                nextAnchorRefreshTime = Time.unscaledTime + 0.5f;
                cachedHeadBone = null;
                cachedAnimator = fighter != null ? fighter.GetComponentInChildren<Animator>() : null;

                if (cachedAnimator != null && cachedAnimator.isHuman)
                    cachedHeadBone = cachedAnimator.GetBoneTransform(HumanBodyBones.Head);
            }

            private bool TryGetRendererBounds(bool ignoreWeapons, out Bounds bounds)
            {
                Renderer[] renderers = fighter.GetComponentsInChildren<Renderer>(true);
                bool hasBounds = false;
                bounds = default;

                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer == null || !renderer.enabled || renderer is ParticleSystemRenderer)
                        continue;

                    string lowerName = renderer.gameObject.name.ToLowerInvariant();
                    if (ignoreWeapons && (lowerName.Contains("weapon")
                                          || lowerName.Contains("sword")
                                          || lowerName.Contains("hammer")
                                          || lowerName.Contains("blade")
                                          || lowerName.Contains("spear")))
                    {
                        continue;
                    }

                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                return hasBounds;
            }

            private void SetVisible(bool visible)
            {
                if (root != null && root.gameObject.activeSelf != visible)
                    root.gameObject.SetActive(visible);
            }

            private static string GetPlayerLabel(FighterController fighterController)
            {
                int playerNumber = fighterController != null ? fighterController.PlayerNumber : 0;
                return playerNumber == 2 ? "P2" : "P1";
            }

            private static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 anchoredPosition)
            {
                GameObject gameObject = new GameObject(name, typeof(RectTransform));
                gameObject.layer = 5;
                RectTransform rect = gameObject.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = size;
                rect.anchoredPosition = anchoredPosition;
                rect.localScale = Vector3.one;
                return rect;
            }

            private static Color ResolveStyleColor(FighterPlayStyle playStyle)
            {
                return playStyle switch
                {
                    FighterPlayStyle.Strength => new Color(0.95f, 0.36f, 0.28f, 1f),
                    FighterPlayStyle.Defense => new Color(0.28f, 0.73f, 1f, 1f),
                    FighterPlayStyle.Invasion => new Color(0.36f, 0.93f, 0.64f, 1f),
                    _ => new Color(0.98f, 0.74f, 0.28f, 1f)
                };
            }
        }

        private static class MarkerSpriteFactory
        {
            private static Sprite bubbleSprite;
            private static Sprite badgeSprite;
            private static readonly Dictionary<FighterPlayStyle, Sprite> styleSprites = new Dictionary<FighterPlayStyle, Sprite>();

            public static Sprite GetBubbleSprite()
            {
                bubbleSprite ??= BuildBubbleSprite();
                return bubbleSprite;
            }

            public static Sprite GetBadgeSprite()
            {
                badgeSprite ??= BuildCircleSprite();
                return badgeSprite;
            }

            public static Sprite GetPlayStyleSprite(FighterPlayStyle playStyle)
            {
                if (!styleSprites.TryGetValue(playStyle, out Sprite sprite))
                {
                    sprite = LoadProjectPlayStyleSprite(playStyle) ?? BuildPlayStyleSprite(playStyle);
                    styleSprites[playStyle] = sprite;
                }

                return sprite;
            }

            private static Sprite LoadProjectPlayStyleSprite(FighterPlayStyle playStyle)
            {
                string resourcePath = ResolveIconResourcePath(playStyle);
                Sprite resourceSprite = Resources.LoadAll<Sprite>(resourcePath)
                    .Where(sprite => sprite != null)
                    .OrderByDescending(sprite => Mathf.Min(sprite.rect.width, sprite.rect.height))
                    .ThenBy(sprite => Mathf.Abs(sprite.rect.width - sprite.rect.height))
                    .FirstOrDefault();

                if (resourceSprite != null)
                    return resourceSprite;

#if UNITY_EDITOR
                string assetPath = ResolveIconAssetPath(playStyle);
                if (string.IsNullOrEmpty(assetPath))
                    return null;

                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
                return assets
                    .OfType<Sprite>()
                    .Where(sprite => sprite != null)
                    .OrderByDescending(sprite => Mathf.Min(sprite.rect.width, sprite.rect.height))
                    .ThenBy(sprite => Mathf.Abs(sprite.rect.width - sprite.rect.height))
                    .FirstOrDefault();
#else
                return null;
#endif
            }

            private static string ResolveIconResourcePath(FighterPlayStyle playStyle)
            {
                switch (playStyle)
                {
                    case FighterPlayStyle.Strength:
                        return "Images/Icons/Offensive";
                    case FighterPlayStyle.Defense:
                        return "Images/Icons/Defensive";
                    case FighterPlayStyle.Invasion:
                        return "Images/Icons/Evasion";
                    default:
                        return "Images/Icons/Balanced";
                }
            }

            private static string ResolveIconAssetPath(FighterPlayStyle playStyle)
            {
                switch (playStyle)
                {
                    case FighterPlayStyle.Strength:
                        return "Assets/Images/Icons/Offensive.png";
                    case FighterPlayStyle.Defense:
                        return "Assets/Images/Icons/Defensive.png";
                    case FighterPlayStyle.Invasion:
                        return "Assets/Images/Icons/Evasion.png";
                    default:
                        return "Assets/Images/Icons/Balanced.png";
                }
            }

            private static Sprite BuildBubbleSprite()
            {
                return BuildSprite(128, pixels =>
                {
                    FillCircle(pixels, 128, new Vector2(64f, 82f), 36f, Color.white);
                    FillPolygon(pixels, 128, new[]
                    {
                        new Vector2(36f, 66f),
                        new Vector2(92f, 66f),
                        new Vector2(64f, 16f)
                    }, Color.white);
                });
            }

            private static Sprite BuildCircleSprite()
            {
                return BuildSprite(96, pixels =>
                {
                    FillCircle(pixels, 96, new Vector2(48f, 48f), 42f, Color.white);
                });
            }

            private static Sprite BuildPlayStyleSprite(FighterPlayStyle playStyle)
            {
                return playStyle switch
                {
                    FighterPlayStyle.Strength => BuildStrengthSprite(),
                    FighterPlayStyle.Defense => BuildDefenseSprite(),
                    FighterPlayStyle.Invasion => BuildInvasionSprite(),
                    _ => BuildBalancedSprite()
                };
            }

            private static Sprite BuildBalancedSprite()
            {
                return BuildSprite(96, pixels =>
                {
                    Vector2[] outer =
                    {
                        new Vector2(48f, 10f),
                        new Vector2(86f, 48f),
                        new Vector2(48f, 86f),
                        new Vector2(10f, 48f)
                    };

                    Vector2[] inner =
                    {
                        new Vector2(48f, 28f),
                        new Vector2(68f, 48f),
                        new Vector2(48f, 68f),
                        new Vector2(28f, 48f)
                    };

                    FillPolygon(pixels, 96, outer, Color.white);
                    FillPolygon(pixels, 96, inner, Color.clear);
                    FillCircle(pixels, 96, new Vector2(48f, 48f), 6f, Color.white);
                });
            }

            private static Sprite BuildStrengthSprite()
            {
                return BuildSprite(96, pixels =>
                {
                    Vector2[] burst =
                    {
                        new Vector2(48f, 8f),
                        new Vector2(60f, 32f),
                        new Vector2(88f, 48f),
                        new Vector2(60f, 64f),
                        new Vector2(48f, 88f),
                        new Vector2(36f, 64f),
                        new Vector2(8f, 48f),
                        new Vector2(36f, 32f)
                    };

                    FillPolygon(pixels, 96, burst, Color.white);
                    FillCircle(pixels, 96, new Vector2(48f, 48f), 11f, Color.clear);
                });
            }

            private static Sprite BuildDefenseSprite()
            {
                return BuildSprite(96, pixels =>
                {
                    Vector2[] outer =
                    {
                        new Vector2(48f, 10f),
                        new Vector2(78f, 22f),
                        new Vector2(70f, 62f),
                        new Vector2(48f, 86f),
                        new Vector2(26f, 62f),
                        new Vector2(18f, 22f)
                    };

                    Vector2[] inner =
                    {
                        new Vector2(48f, 24f),
                        new Vector2(64f, 30f),
                        new Vector2(60f, 56f),
                        new Vector2(48f, 70f),
                        new Vector2(36f, 56f),
                        new Vector2(32f, 30f)
                    };

                    FillPolygon(pixels, 96, outer, Color.white);
                    FillPolygon(pixels, 96, inner, Color.clear);
                });
            }

            private static Sprite BuildInvasionSprite()
            {
                return BuildSprite(96, pixels =>
                {
                    FillPolygon(pixels, 96, new[]
                    {
                        new Vector2(20f, 70f),
                        new Vector2(48f, 20f),
                        new Vector2(62f, 20f),
                        new Vector2(34f, 70f)
                    }, Color.white);

                    FillPolygon(pixels, 96, new[]
                    {
                        new Vector2(42f, 78f),
                        new Vector2(70f, 28f),
                        new Vector2(84f, 28f),
                        new Vector2(56f, 78f)
                    }, Color.white);
                });
            }

            private static Sprite BuildSprite(int size, System.Action<Color32[]> painter)
            {
                Color32[] pixels = new Color32[size * size];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(0, 0, 0, 0);

                painter(pixels);

                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                texture.name = "MarkerSprite_" + size;
                texture.filterMode = FilterMode.Bilinear;
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.SetPixels32(pixels);
                texture.Apply();
                return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            }

            private static void FillCircle(Color32[] pixels, int size, Vector2 center, float radius, Color color)
            {
                float radiusSquared = radius * radius;
                int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
                int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(center.x + radius));
                int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
                int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(center.y + radius));

                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        float dx = x + 0.5f - center.x;
                        float dy = y + 0.5f - center.y;
                        if (dx * dx + dy * dy <= radiusSquared)
                            pixels[y * size + x] = color;
                    }
                }
            }

            private static void FillPolygon(Color32[] pixels, int size, IReadOnlyList<Vector2> points, Color color)
            {
                if (points == null || points.Count < 3)
                    return;

                float minX = points.Min(point => point.x);
                float maxX = points.Max(point => point.x);
                float minY = points.Min(point => point.y);
                float maxY = points.Max(point => point.y);

                int startX = Mathf.Max(0, Mathf.FloorToInt(minX));
                int endX = Mathf.Min(size - 1, Mathf.CeilToInt(maxX));
                int startY = Mathf.Max(0, Mathf.FloorToInt(minY));
                int endY = Mathf.Min(size - 1, Mathf.CeilToInt(maxY));

                for (int y = startY; y <= endY; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        if (PointInPolygon(points, x + 0.5f, y + 0.5f))
                            pixels[y * size + x] = color;
                    }
                }
            }

            private static bool PointInPolygon(IReadOnlyList<Vector2> polygon, float px, float py)
            {
                bool inside = false;
                for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
                {
                    Vector2 a = polygon[i];
                    Vector2 b = polygon[j];

                    bool intersects = ((a.y > py) != (b.y > py))
                        && (px < (b.x - a.x) * (py - a.y) / ((b.y - a.y) + Mathf.Epsilon) + a.x);

                    if (intersects)
                        inside = !inside;
                }

                return inside;
            }
        }
    }
}
