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
        private GameManager       _gameManager;

        // -----------------------------------------------------------------------
        // Unity lifecycle
        // -----------------------------------------------------------------------

        private void Start()
        {
            _p1Fill = GetFillImage(p1HealthSlider);
            _p2Fill = GetFillImage(p2HealthSlider);

            // Fall back to Unity's built-in knob (circle) sprite when none is assigned.
            if (winBubbleSprite == null)
                winBubbleSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");

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

        private void UpdateP1Health(float current, float max) =>
            ApplyHealth(p1HealthSlider, _p1Fill, current, max);

        private void UpdateP2Health(float current, float max) =>
            ApplyHealth(p2HealthSlider, _p2Fill, current, max);

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

        private void OnRoundEnd(FighterController winner) => RefreshBubbles();

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
    }
}
