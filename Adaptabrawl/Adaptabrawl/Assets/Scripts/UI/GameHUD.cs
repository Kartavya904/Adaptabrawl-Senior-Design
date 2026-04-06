using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// Drives the in-game HUD:
    ///   • Health bar Sliders with colour coding for both players
    ///   • Round timer displayed as mm:ss
    ///   • Circle-sprite round-win bubbles under each health bar
    ///
    /// Health colours (all Inspector-adjustable):
    ///   > thresholdHigh  → colorHigh (default green)
    ///   > thresholdLow   → colorMid  (default yellow)
    ///   ≤ thresholdLow   → colorLow  (default red)
    ///
    /// Round-win bubbles:
    ///   Assign p1BubblesContainer / p2BubblesContainer in the Inspector.
    ///   The script creates one Image per possible round win and lights it up
    ///   as wins are earned.  Assign winBubbleSprite or leave null to fall back
    ///   to Unity's built-in circle (Knob) sprite.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        // -----------------------------------------------------------------------
        // Inspector fields
        // -----------------------------------------------------------------------

        [Header("Player 1 Health")]
        [SerializeField] private Slider p1HealthSlider;

        [Header("Player 2 Health")]
        [SerializeField] private Slider p2HealthSlider;

        [Header("Health Colors")]
        [Tooltip("Fill color when health is above the High threshold.")]
        [SerializeField] private Color colorHigh = Color.green;
        [Tooltip("Fill color when health is between the Low and High thresholds.")]
        [SerializeField] private Color colorMid  = Color.yellow;
        [Tooltip("Fill color when health is at or below the Low threshold.")]
        [SerializeField] private Color colorLow  = Color.red;
        [Tooltip("Health fraction above which the High color is used (0.75 = 75%).")]
        [Range(0f, 1f)]
        [SerializeField] private float thresholdHigh = 0.75f;
        [Tooltip("Health fraction below which the Low color is used (0.25 = 25%).")]
        [Range(0f, 1f)]
        [SerializeField] private float thresholdLow  = 0.25f;

        [Header("Timer")]
        [Tooltip("The 'Timer' TMP text element in the scene. Displays time as mm:ss.")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Ghost Health Bars (Chipping)")]
        [SerializeField] private Slider p1GhostSlider;
        [SerializeField] private Slider p2GhostSlider;
        [Tooltip("Seconds to wait before ghost bar starts draining.")]
        [SerializeField] private float ghostDrainDelay = 0.1f;
        [Tooltip("Units per second the ghost bar drains.")]
        [SerializeField] private float ghostDrainSpeed = 0.4f;

        [Header("Countdown Overlay")]
        [Tooltip("Large centered TMP text for 3-2-1-FIGHT display. Assign in Inspector.")]
        [SerializeField] private TextMeshProUGUI countdownText;
        [SerializeField] private Color countdownNumberColor = new Color(1f, 0.85f, 0.1f);
        [SerializeField] private Color countdownFightColor  = new Color(1f, 0.25f, 0.1f);

        [Header("Round Banners")]
        [Tooltip("Panel that slides down from top. Assign in Inspector.")]
        [SerializeField] private RectTransform bannerPanel;
        [SerializeField] private TextMeshProUGUI bannerText;

        [Header("KO Screen Flash")]
        [Tooltip("Full-screen white Image (alpha=0 at start). Assign in Inspector.")]
        [SerializeField] private Image screenFlashImage;

        [Header("Round Win Bubbles")]
        [Tooltip("Empty RectTransform placed under Player 1's health bar. " +
                 "Win-bubble Images will be parented here at runtime.")]
        [SerializeField] private RectTransform p1BubblesContainer;
        [Tooltip("Empty RectTransform placed under Player 2's health bar. " +
                 "Win-bubble Images will be parented here at runtime.")]
        [SerializeField] private RectTransform p2BubblesContainer;
        [Tooltip("Circle sprite used for each bubble. " +
                 "Leave null to fall back to Unity's built-in Knob (circle) sprite.")]
        [SerializeField] private Sprite winBubbleSprite;
        [Tooltip("Color of a filled (earned) win bubble.")]
        [SerializeField] private Color winBubbleColor   = Color.white;
        [Tooltip("Color of an unfilled (not yet earned) win bubble).")]
        [SerializeField] private Color emptyBubbleColor = new Color(1f, 1f, 1f, 0.25f);
        [Tooltip("Diameter of each bubble in UI pixels.")]
        [SerializeField] private float bubbleSize    = 30f;
        [Tooltip("Horizontal gap between bubbles in UI pixels.")]
        [SerializeField] private float bubbleSpacing = 8f;
        [Tooltip("Total rounds needed to win the match — sets how many bubbles to pre-create.")]
        [SerializeField] private int roundsToWin = 2;

        // -----------------------------------------------------------------------
        // Runtime state
        // -----------------------------------------------------------------------

        private FighterController _p1;
        private FighterController _p2;
        private Image             _p1Fill;
        private Image             _p2Fill;
        private Image             _p1GhostFill;
        private Image             _p2GhostFill;
        private GameManager       _gameManager;
        private Coroutine         _p1GhostCoroutine;
        private Coroutine         _p2GhostCoroutine;
        private static Sprite     s_FallbackBubbleSprite;

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------

        private void Start()
        {
            _p1Fill = GetFillImage(p1HealthSlider);
            _p2Fill = GetFillImage(p2HealthSlider);
            _p1GhostFill = GetFillImage(p1GhostSlider);
            _p2GhostFill = GetFillImage(p2GhostSlider);

            // Tint ghost bars orange
            SetGhostColor(_p1GhostFill);
            SetGhostColor(_p2GhostFill);

            // Hide countdown and banner at start
            if (countdownText != null)  { countdownText.gameObject.SetActive(false); }
            if (bannerPanel != null)    { bannerPanel.gameObject.SetActive(false); }
            if (screenFlashImage != null) { var c = screenFlashImage.color; c.a = 0f; screenFlashImage.color = c; }

            // Built-in editor sprite names vary across Unity versions, so generate a small
            // runtime bubble sprite instead of depending on Knob.psd.
            if (winBubbleSprite == null)
                winBubbleSprite = GetOrCreateFallbackBubbleSprite();

            StartCoroutine(ConnectRoutine());
        }

        private void OnDestroy()
        {
            if (_p1 != null) _p1.OnHealthChanged -= UpdateP1Health;
            if (_p2 != null) _p2.OnHealthChanged -= UpdateP2Health;

            if (_gameManager != null)
            {
                _gameManager.OnRoundTimerUpdate -= UpdateTimer;
                _gameManager.OnRoundEnd         -= OnRoundEnd;
                _gameManager.OnRoundStart       -= OnRoundStart;
                _gameManager.OnRoundCountdown   -= OnRoundCountdown;
            }
        }

        // -----------------------------------------------------------------------
        // Connection coroutine (waits one frame for spawners)
        // -----------------------------------------------------------------------

        private System.Collections.IEnumerator ConnectRoutine()
        {
            yield return null;

            // --- Fighters ---
            LocalGameManager lgm = FindFirstObjectByType<LocalGameManager>();
            if (lgm != null)
            {
                if (lgm.Player1 != null && lgm.Player2 != null)
                    BindFighters(lgm.Player1, lgm.Player2);
                else
                    lgm.OnFightersSpawned += BindFighters;
            }
            else
            {
                FighterController[] fighters =
                    FindObjectsByType<FighterController>(FindObjectsSortMode.InstanceID);
                if (fighters.Length >= 2)
                    BindFighters(fighters[0], fighters[1]);
                else
                    Debug.LogWarning("GameHUD: Could not find fighters — health bars will not update.");
            }

            // --- GameManager (timer + round wins) ---
            _gameManager = FindFirstObjectByType<GameManager>();
            if (_gameManager != null)
            {
                _gameManager.OnRoundTimerUpdate += UpdateTimer;
                _gameManager.OnRoundEnd         += OnRoundEnd;
                _gameManager.OnRoundStart       += OnRoundStart;
                _gameManager.OnRoundCountdown   += OnRoundCountdown;
                UpdateTimer(_gameManager.RoundTimer);
                RefreshBubbles();
            }
        }

        // -----------------------------------------------------------------------
        // Fighter binding
        // -----------------------------------------------------------------------

        private void BindFighters(FighterController p1, FighterController p2)
        {
            _p1 = p1;
            _p2 = p2;

            if (_p1 != null)
            {
                _p1.OnHealthChanged += UpdateP1Health;
                UpdateP1Health(_p1.CurrentHealth, _p1.MaxHealth);
            }

            if (_p2 != null)
            {
                _p2.OnHealthChanged += UpdateP2Health;
                UpdateP2Health(_p2.CurrentHealth, _p2.MaxHealth);
            }
        }

        // -----------------------------------------------------------------------
        // Health bar helpers
        // -----------------------------------------------------------------------

        private static Image GetFillImage(Slider slider)
        {
            if (slider == null || slider.fillRect == null) return null;
            return slider.fillRect.GetComponent<Image>();
        }

        private void UpdateP1Health(float current, float max)
        {
            ApplyHealth(p1HealthSlider, _p1Fill, current, max);
            DriveGhostBar(ref _p1GhostCoroutine, p1GhostSlider, current, max);
        }

        private void UpdateP2Health(float current, float max)
        {
            ApplyHealth(p2HealthSlider, _p2Fill, current, max);
            DriveGhostBar(ref _p2GhostCoroutine, p2GhostSlider, current, max);
        }

        private void ApplyHealth(Slider slider, Image fill, float current, float max)
        {
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = max;
                slider.value    = current;
            }

            if (fill != null && max > 0f)
            {
                float ratio = current / max;
                if      (ratio > thresholdHigh) fill.color = colorHigh;
                else if (ratio > thresholdLow)  fill.color = colorMid;
                else                            fill.color = colorLow;
            }
        }

        private void DriveGhostBar(ref Coroutine handle, Slider ghostSlider, float current, float max)
        {
            if (ghostSlider == null) return;
            ghostSlider.minValue = 0f;
            ghostSlider.maxValue = max;
            // Only drain ghost if actual bar went down
            if (current < ghostSlider.value)
            {
                if (handle != null) StopCoroutine(handle);
                handle = StartCoroutine(DrainGhostRoutine(ghostSlider, current));
            }
            else
            {
                ghostSlider.value = current; // snap up immediately on heal
            }
        }

        private System.Collections.IEnumerator DrainGhostRoutine(Slider ghostSlider, float targetValue)
        {
            yield return new WaitForSeconds(ghostDrainDelay);
            while (ghostSlider != null && ghostSlider.value > targetValue)
            {
                ghostSlider.value -= ghostDrainSpeed * ghostSlider.maxValue * Time.deltaTime;
                if (ghostSlider.value < targetValue) ghostSlider.value = targetValue;
                yield return null;
            }
        }

        private static void SetGhostColor(Image img)
        {
            if (img != null)
                img.color = new Color(1f, 0.6f, 0.1f, 0.75f);
        }

        // -----------------------------------------------------------------------
        // Timer — mm:ss format
        // -----------------------------------------------------------------------

        private void UpdateTimer(float remaining)
        {
            if (timerText == null) return;

            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
            int minutes      = totalSeconds / 60;
            int seconds      = totalSeconds % 60;
            timerText.text   = $"{minutes:00}:{seconds:00}";
        }

        // -----------------------------------------------------------------------
        // Round-win bubbles
        // -----------------------------------------------------------------------

        private void OnRoundEnd(FighterController winner)
        {
            RefreshBubbles();
            string msg = winner != null ? "KO!" : "TIME!";
            StartCoroutine(ShowBannerRoutine(msg));
            StartCoroutine(ScreenFlashRoutine());
        }

        private void OnRoundStart(int roundNumber)
        {
            string label = roundNumber >= 3 ? "Final Round!" : $"Round {roundNumber}";
            StartCoroutine(ShowBannerRoutine(label));
        }

        private void OnRoundCountdown(int tick)
        {
            if (countdownText == null) return;
            if (tick > 0)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text  = tick.ToString();
                countdownText.color = countdownNumberColor;
                StartCoroutine(PunchScaleRoutine(countdownText.transform, 1.4f, 0.15f));
            }
            else
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text  = "FIGHT!";
                countdownText.color = countdownFightColor;
                StartCoroutine(PunchScaleRoutine(countdownText.transform, 1.6f, 0.2f,
                    fadeOutDelay: 0.75f, onDone: () => countdownText.gameObject.SetActive(false)));
            }
        }

        private System.Collections.IEnumerator PunchScaleRoutine(
            Transform target, float peakScale, float punchDuration,
            float fadeOutDelay = 0f, System.Action onDone = null)
        {
            float t = 0f;
            while (t < punchDuration)
            {
                float p = t / punchDuration;
                float s = Mathf.Lerp(peakScale, 1f, p);
                target.localScale = Vector3.one * s;
                t += Time.deltaTime;
                yield return null;
            }
            target.localScale = Vector3.one;
            if (fadeOutDelay > 0f) yield return new WaitForSeconds(fadeOutDelay);
            onDone?.Invoke();
        }

        private System.Collections.IEnumerator ShowBannerRoutine(string message)
        {
            if (bannerPanel == null || bannerText == null) yield break;
            bannerText.text = message;
            bannerPanel.gameObject.SetActive(true);

            // Slide in from top
            Vector2 offscreen = new Vector2(0f, 200f);
            Vector2 onscreen  = Vector2.zero;
            float elapsed = 0f, slideDuration = 0.25f;
            while (elapsed < slideDuration)
            {
                bannerPanel.anchoredPosition = Vector2.Lerp(offscreen, onscreen, elapsed / slideDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            bannerPanel.anchoredPosition = onscreen;

            yield return new WaitForSeconds(1.5f);

            elapsed = 0f;
            while (elapsed < slideDuration)
            {
                bannerPanel.anchoredPosition = Vector2.Lerp(onscreen, offscreen, elapsed / slideDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            bannerPanel.gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator ScreenFlashRoutine()
        {
            if (screenFlashImage == null) yield break;
            Color c = screenFlashImage.color;
            c.a = 0.7f;
            screenFlashImage.color = c;
            float elapsed = 0f, fadeDuration = 0.35f;
            while (elapsed < fadeDuration)
            {
                c.a = Mathf.Lerp(0.7f, 0f, elapsed / fadeDuration);
                screenFlashImage.color = c;
                elapsed += Time.deltaTime;
                yield return null;
            }
            c.a = 0f;
            screenFlashImage.color = c;
        }

        private void RefreshBubbles()
        {
            if (_gameManager == null) return;
            var wins = _gameManager.RoundWins;

            int p1Wins = (_p1 != null && wins.TryGetValue(_p1, out int w1)) ? w1 : 0;
            int p2Wins = (_p2 != null && wins.TryGetValue(_p2, out int w2)) ? w2 : 0;

            BuildBubbles(p1BubblesContainer, p1Wins);
            BuildBubbles(p2BubblesContainer, p2Wins);
        }

        /// <summary>
        /// Destroys any existing bubble children in <paramref name="container"/> and
        /// rebuilds them: <paramref name="wins"/> filled bubbles followed by empty ones
        /// up to <see cref="roundsToWin"/> total.
        /// </summary>
        private void BuildBubbles(RectTransform container, int wins)
        {
            if (container == null) return;

            // Clear old bubbles
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            int total = Mathf.Max(roundsToWin, wins); // never fewer slots than actual wins
            float totalWidth = total * bubbleSize + Mathf.Max(0, total - 1) * bubbleSpacing;
            float startX     = -totalWidth * 0.5f + bubbleSize * 0.5f;

            for (int i = 0; i < total; i++)
            {
                GameObject bubbleObj = new GameObject($"WinBubble_{i}");
                bubbleObj.transform.SetParent(container, false);

                RectTransform rt = bubbleObj.AddComponent<RectTransform>();
                rt.sizeDelta       = new Vector2(bubbleSize, bubbleSize);
                rt.anchoredPosition = new Vector2(startX + i * (bubbleSize + bubbleSpacing), 0f);

                Image img = bubbleObj.AddComponent<Image>();
                img.sprite           = winBubbleSprite;
                img.preserveAspect   = false;
                img.color            = i < wins ? winBubbleColor : emptyBubbleColor;
            }
        }

        private static Sprite GetOrCreateFallbackBubbleSprite()
        {
            if (s_FallbackBubbleSprite != null)
                return s_FallbackBubbleSprite;

            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "HUD_WinBubbleFallback"
            };

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.42f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
                }
            }

            texture.Apply();
            s_FallbackBubbleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                size);

            return s_FallbackBubbleSprite;
        }
    }
}
