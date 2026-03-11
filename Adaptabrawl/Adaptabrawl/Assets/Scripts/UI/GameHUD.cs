using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// Drives health bar Sliders for both players with color coding, a round
    /// timer, and round-win indicators.
    ///
    /// Health colors (all Inspector-adjustable):
    ///   > 75%  → High color (default green)
    ///   > 25%  → Mid  color (default yellow)
    ///   ≤ 25%  → Low  color (default red)
    ///
    /// Assign p1HealthSlider / p2HealthSlider in the Inspector.
    /// Optionally assign timerText, p1WinsText, p2WinsText for the full round HUD.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Player 1")]
        [SerializeField] private Slider p1HealthSlider;

        [Header("Player 2")]
        [SerializeField] private Slider p2HealthSlider;

        [Header("Health Colors")]
        [Tooltip("Fill color when health is above the High threshold.")]
        [SerializeField] private Color colorHigh = Color.green;
        [Tooltip("Fill color when health is between the Low and High thresholds.")]
        [SerializeField] private Color colorMid  = Color.yellow;
        [Tooltip("Fill color when health is at or below the Low threshold.")]
        [SerializeField] private Color colorLow  = Color.red;
        [Tooltip("Health fraction above which the High color is used (default 0.75 = 75%).")]
        [Range(0f, 1f)]
        [SerializeField] private float thresholdHigh = 0.75f;
        [Tooltip("Health fraction below which the Low color is used (default 0.25 = 25%).")]
        [Range(0f, 1f)]
        [SerializeField] private float thresholdLow  = 0.25f;

        [Header("Timer")]
        [Tooltip("TMP text element that shows the remaining round time (whole seconds).")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Round Wins")]
        [Tooltip("TMP text showing Player 1 round wins, e.g. '● ●'.")]
        [SerializeField] private TextMeshProUGUI p1WinsText;
        [Tooltip("TMP text showing Player 2 round wins.")]
        [SerializeField] private TextMeshProUGUI p2WinsText;

        // -----------------------------------------------------------------------
        // Runtime
        // -----------------------------------------------------------------------

        private FighterController _p1;
        private FighterController _p2;
        private Image _p1Fill;
        private Image _p2Fill;
        private GameManager _gameManager;

        private void Start()
        {
            _p1Fill = GetFillImage(p1HealthSlider);
            _p2Fill = GetFillImage(p2HealthSlider);

            StartCoroutine(ConnectRoutine());
        }

        private static Image GetFillImage(Slider slider)
        {
            if (slider == null || slider.fillRect == null) return null;
            return slider.fillRect.GetComponent<Image>();
        }

        private System.Collections.IEnumerator ConnectRoutine()
        {
            yield return null; // one-frame wait for spawners

            // Connect fighters via LocalGameManager (or direct fallback)
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
                FighterController[] fighters = FindObjectsByType<FighterController>(FindObjectsSortMode.InstanceID);
                if (fighters.Length >= 2)
                    BindFighters(fighters[0], fighters[1]);
                else
                    Debug.LogWarning("GameHUD: Could not find fighters — health bars will not update.");
            }

            // Connect GameManager for timer + round wins
            _gameManager = FindFirstObjectByType<GameManager>();
            if (_gameManager != null)
            {
                _gameManager.OnRoundTimerUpdate += UpdateTimer;
                _gameManager.OnRoundEnd         += OnRoundEnd;
                UpdateTimer(_gameManager.RoundTimer);
                RefreshWinDisplay();
            }
        }

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
                if (ratio > thresholdHigh)
                    fill.color = colorHigh;
                else if (ratio > thresholdLow)
                    fill.color = colorMid;
                else
                    fill.color = colorLow;
            }
        }

        private void UpdateTimer(float remaining)
        {
            if (timerText != null)
                timerText.text = Mathf.CeilToInt(Mathf.Max(0f, remaining)).ToString();
        }

        private void OnRoundEnd(FighterController winner) => RefreshWinDisplay();

        private void RefreshWinDisplay()
        {
            if (_gameManager == null) return;
            var wins = _gameManager.RoundWins;

            if (p1WinsText != null && _p1 != null && wins.TryGetValue(_p1, out int w1))
                p1WinsText.text = new string('●', w1);

            if (p2WinsText != null && _p2 != null && wins.TryGetValue(_p2, out int w2))
                p2WinsText.text = new string('●', w2);
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
    }
}
