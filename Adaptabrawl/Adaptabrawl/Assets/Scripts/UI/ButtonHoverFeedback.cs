using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// Installs consistent hover feedback on every button across all scenes.
    /// Buttons are grouped by visual family so similar controls share the same
    /// motion language while still preserving their existing scene tint colors.
    /// </summary>
    public sealed class ButtonHoverInstaller : MonoBehaviour
    {
        private static ButtonHoverInstaller instance;
        private float nextScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            var installerObject = new GameObject("ButtonHoverInstaller");
            instance = installerObject.AddComponent<ButtonHoverInstaller>();
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
            InstallLoadedScenes();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (Time.unscaledTime < nextScanTime)
                return;

            nextScanTime = Time.unscaledTime + 0.75f;
            InstallLoadedScenes();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode _)
        {
            InstallScene(scene);
            nextScanTime = Time.unscaledTime + 0.15f;
        }

        private void InstallLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                InstallScene(SceneManager.GetSceneAt(i));
            }
        }

        private static void InstallScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Button[] buttons = roots[i].GetComponentsInChildren<Button>(true);
                for (int j = 0; j < buttons.Length; j++)
                {
                    Button button = buttons[j];
                    if (button == null || button.GetComponent<ButtonHoverFeedback>() != null)
                        continue;

                    button.gameObject.AddComponent<ButtonHoverFeedback>();
                }
            }
        }
    }

    [RequireComponent(typeof(Button))]
    public sealed class ButtonHoverFeedback : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        private enum HoverProfile
        {
            TextMenu,
            FramedAction,
            Arrow
        }

        private readonly List<Graphic> childGraphics = new List<Graphic>();
        private readonly List<Color> childBaseColors = new List<Color>();

        private Button button;
        private RectTransform rectTransform;
        private Graphic targetGraphic;
        private HoverProfile profile;

        private Vector2 baseAnchoredPosition;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private Vector2 hoverOffset;
        private float hoverScale = 1f;
        private float pressScale = 1f;
        private float hoverRotationZ;
        private Color accentColor = Color.white;
        private float childTintStrength = 0.7f;

        private bool pointerInside;
        private bool isSelected;
        private bool isPressed;
        private float hoverLerp;
        private float pressLerp;

        private void Awake()
        {
            CacheReferences();
            ResetVisualsImmediate();
        }

        private void OnEnable()
        {
            CacheReferences();
            ResetVisualsImmediate();
        }

        private void OnDisable()
        {
            pointerInside = false;
            isSelected = false;
            isPressed = false;
            hoverLerp = 0f;
            pressLerp = 0f;
            ResetVisualsImmediate();
        }

        private void Update()
        {
            if (button == null || rectTransform == null)
                return;

            bool canHover = button.IsInteractable() && isActiveAndEnabled;
            bool controllerFocusVisible = isSelected && MenuNavigationGroup.IsControllerFocusVisible(gameObject);
            float hoverTarget = canHover && (pointerInside || controllerFocusVisible) ? 1f : 0f;
            float pressTarget = canHover && isPressed ? 1f : 0f;

            hoverLerp = Mathf.MoveTowards(hoverLerp, hoverTarget, Time.unscaledDeltaTime * 8f);
            pressLerp = Mathf.MoveTowards(pressLerp, pressTarget, Time.unscaledDeltaTime * 14f);

            float easedHover = Mathf.SmoothStep(0f, 1f, hoverLerp);
            float easedPress = Mathf.SmoothStep(0f, 1f, pressLerp);

            rectTransform.anchoredPosition = Vector2.Lerp(baseAnchoredPosition, baseAnchoredPosition + hoverOffset, easedHover);
            rectTransform.localRotation = Quaternion.Slerp(baseRotation, baseRotation * Quaternion.Euler(0f, 0f, hoverRotationZ), easedHover);

            float appliedScale = Mathf.Lerp(1f, hoverScale, easedHover) * Mathf.Lerp(1f, pressScale, easedPress);
            rectTransform.localScale = baseScale * appliedScale;

            for (int i = 0; i < childGraphics.Count; i++)
            {
                Graphic graphic = childGraphics[i];
                if (graphic == null)
                    continue;

                if (hoverLerp <= 0.001f && pressLerp <= 0.001f)
                    childBaseColors[i] = graphic.color;

                Color baseColor = childBaseColors[i];
                if (profile == HoverProfile.TextMenu && graphic is TMP_Text)
                {
                    graphic.color = baseColor;
                    continue;
                }

                Color tintedColor = Color.Lerp(baseColor, ResolveTint(baseColor), easedHover * childTintStrength);
                graphic.color = tintedColor;
            }
        }

        public void OnPointerEnter(PointerEventData _)
        {
            pointerInside = true;
            if (button != null)
                MenuNavigationGroup.NotifyPointerHover(button);
        }

        public void OnPointerExit(PointerEventData _)
        {
            pointerInside = false;
            isPressed = false;
        }

        public void OnSelect(BaseEventData _)
        {
            isSelected = true;
        }

        public void OnDeselect(BaseEventData _)
        {
            isSelected = false;
            isPressed = false;
        }

        public void OnPointerDown(PointerEventData _)
        {
            if (button != null && button.IsInteractable())
                isPressed = true;
        }

        public void OnPointerUp(PointerEventData _)
        {
            isPressed = false;
        }

        public void RecaptureBasePosition()
        {
            if (rectTransform != null)
            {
                baseAnchoredPosition = rectTransform.anchoredPosition;
                baseScale = rectTransform.localScale;
                baseRotation = rectTransform.localRotation;
            }
        }

        private void CacheReferences()
        {
            button = GetComponent<Button>();
            rectTransform = transform as RectTransform;
            targetGraphic = button != null ? button.targetGraphic : null;

            if (rectTransform == null)
                return;

            baseAnchoredPosition = rectTransform.anchoredPosition;
            baseScale = rectTransform.localScale;
            baseRotation = rectTransform.localRotation;

            childGraphics.Clear();
            childBaseColors.Clear();

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null || graphic == targetGraphic)
                    continue;

                childGraphics.Add(graphic);
                childBaseColors.Add(graphic.color);
            }

            profile = ResolveProfile();
            ConfigureProfile(profile);
        }

        private HoverProfile ResolveProfile()
        {
            string lowerName = gameObject.name.ToLowerInvariant();
            if (lowerName.Contains("arrow") || lowerName.Contains("left") || lowerName.Contains("right"))
                return HoverProfile.Arrow;

            TMP_Text text = GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                string label = (text.text ?? string.Empty).Trim();
                if (label == "<" || label == ">" || label == "<<" || label == ">>")
                    return HoverProfile.Arrow;
            }

            Image image = targetGraphic as Image;
            if (image == null || image.sprite == null || image.color.a <= 0.05f)
                return HoverProfile.TextMenu;

            return HoverProfile.FramedAction;
        }

        private void ConfigureProfile(HoverProfile resolvedProfile)
        {
            switch (resolvedProfile)
            {
                case HoverProfile.Arrow:
                    hoverScale = 1.12f;
                    pressScale = 0.92f;
                    hoverRotationZ = ResolveArrowDirection() * -6f;
                    hoverOffset = new Vector2(ResolveArrowDirection() * 8f, 2f);
                    accentColor = ResolveAccentColor(new Color(1f, 0.82f, 0.48f, 1f));
                    childTintStrength = 0.9f;
                    break;

                case HoverProfile.FramedAction:
                    hoverScale = 1.04f;
                    pressScale = 0.96f;
                    hoverRotationZ = 0f;
                    hoverOffset = new Vector2(0f, 4f);
                    accentColor = ResolveAccentColor(new Color(0.56f, 0.88f, 1f, 1f));
                    childTintStrength = 0.6f;
                    break;

                default:
                    hoverScale = 1.05f;
                    pressScale = 0.975f;
                    hoverRotationZ = 0f;
                    hoverOffset = new Vector2(0f, 5f);
                    accentColor = ResolveAccentColor(new Color(1f, 0.92f, 0.7f, 1f));
                    childTintStrength = 0.85f;
                    break;
            }
        }

        private int ResolveArrowDirection()
        {
            string lowerName = gameObject.name.ToLowerInvariant();
            if (lowerName.Contains("left"))
                return -1;
            if (lowerName.Contains("right"))
                return 1;

            TMP_Text text = GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                string label = (text.text ?? string.Empty).Trim();
                if (label.Contains("<"))
                    return -1;
                if (label.Contains(">"))
                    return 1;
            }

            return 1;
        }

        private Color ResolveAccentColor(Color fallback)
        {
            if (button == null)
                return fallback;

            return button.colors.highlightedColor;
        }

        private Color ResolveTint(Color baseColor)
        {
            Color tint = Color.Lerp(baseColor, accentColor, 0.75f);
            tint.a = baseColor.a;
            return tint;
        }

        private void ResetVisualsImmediate()
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = baseAnchoredPosition;
                rectTransform.localScale = baseScale;
                rectTransform.localRotation = baseRotation;
            }

            for (int i = 0; i < childGraphics.Count; i++)
            {
                Graphic graphic = childGraphics[i];
                if (graphic == null)
                    continue;

                graphic.color = childBaseColors[i];
            }
        }
    }
}
