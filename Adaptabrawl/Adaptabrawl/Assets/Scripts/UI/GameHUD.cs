using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// Drives health bars and fighter name labels for both players.
    ///
    /// Setup in the Inspector:
    ///   1. Attach this script to any GameObject in the GameScene (e.g. "GameHUD").
    ///   2. Create a Canvas with two health bar groups (see README in this script for layout).
    ///   3. Assign the Image "fill" objects and optional TextMeshProUGUI labels below.
    ///
    /// The script auto-connects to LocalGameManager and subscribes to OnHealthChanged events
    /// so the bars update in real-time whenever a fighter takes damage or is healed.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Player 1 Health Bar")]
        [Tooltip("The Image component used as the health-bar fill (set Image Type to Filled, Fill Method to Horizontal).")]
        [SerializeField] private Image p1HealthFill;
        [Tooltip("Optional Slider alternative — set its Min=0, Max=1. Either Image OR Slider will work.")]
        [SerializeField] private Slider p1HealthSlider;
        [Tooltip("Optional text showing 'current / max' HP.")]
        [SerializeField] private TextMeshProUGUI p1HealthText;
        [Tooltip("Optional label showing the fighter's name.")]
        [SerializeField] private TextMeshProUGUI p1NameText;

        [Header("Player 2 Health Bar")]
        [SerializeField] private Image p2HealthFill;
        [SerializeField] private Slider p2HealthSlider;
        [SerializeField] private TextMeshProUGUI p2HealthText;
        [SerializeField] private TextMeshProUGUI p2NameText;

        [Header("Health Bar Colors")]
        [SerializeField] private Color colorHealthy  = new Color(0.18f, 0.80f, 0.25f); // green
        [SerializeField] private Color colorCaution  = new Color(1.00f, 0.80f, 0.10f); // yellow
        [SerializeField] private Color colorCritical = new Color(0.90f, 0.15f, 0.10f); // red
        [SerializeField] [Range(0.01f, 0.99f)] private float cautionThreshold  = 0.50f;
        [SerializeField] [Range(0.01f, 0.99f)] private float criticalThreshold = 0.25f;

        private FighterController _p1;
        private FighterController _p2;

        private void Start()
        {
            // Wait one frame to make sure LocalGameManager.Start() has already run.
            StartCoroutine(ConnectToGameManager());
        }

        private System.Collections.IEnumerator ConnectToGameManager()
        {
            yield return null; // one-frame wait

            LocalGameManager lgm = FindFirstObjectByType<LocalGameManager>();
            if (lgm != null)
            {
                if (lgm.Player1 != null && lgm.Player2 != null)
                    BindFighters(lgm.Player1, lgm.Player2);
                else
                    lgm.OnFightersSpawned += BindFighters;
                yield break;
            }

            // Fallback: no LocalGameManager — find FighterControllers directly.
            FighterController[] fighters = FindObjectsByType<FighterController>(FindObjectsSortMode.InstanceID);
            if (fighters.Length >= 2)
            {
                BindFighters(fighters[0], fighters[1]);
            }
            else
            {
                Debug.LogWarning("GameHUD: Could not find fighters — health bars will not update.");
            }
        }

        private void BindFighters(FighterController p1, FighterController p2)
        {
            _p1 = p1;
            _p2 = p2;

            if (_p1 != null)
            {
                _p1.OnHealthChanged += UpdateP1Health;
                // Initialise bars with current values right away.
                UpdateP1Health(_p1.CurrentHealth, _p1.MaxHealth);
                if (p1NameText != null && _p1.FighterDef != null)
                    p1NameText.text = _p1.FighterDef.fighterName;
            }

            if (_p2 != null)
            {
                _p2.OnHealthChanged += UpdateP2Health;
                UpdateP2Health(_p2.CurrentHealth, _p2.MaxHealth);
                if (p2NameText != null && _p2.FighterDef != null)
                    p2NameText.text = _p2.FighterDef.fighterName;
            }
        }

        private void UpdateP1Health(float current, float max)
        {
            ApplyHealthToBar(current, max, p1HealthFill, p1HealthSlider, p1HealthText);
        }

        private void UpdateP2Health(float current, float max)
        {
            ApplyHealthToBar(current, max, p2HealthFill, p2HealthSlider, p2HealthText);
        }

        private void ApplyHealthToBar(float current, float max,
                                      Image fill, Slider slider, TextMeshProUGUI label)
        {
            float ratio = max > 0f ? current / max : 0f;
            Color barColor = ratio > cautionThreshold  ? colorHealthy
                           : ratio > criticalThreshold ? colorCaution
                                                       : colorCritical;

            if (fill != null)
            {
                fill.fillAmount = ratio;
                fill.color = barColor;
            }

            if (slider != null)
            {
                slider.value = ratio;
                // Colour the fill rect of the slider if it has one.
                if (slider.fillRect != null)
                {
                    var fillImage = slider.fillRect.GetComponent<Image>();
                    if (fillImage != null) fillImage.color = barColor;
                }
            }

            if (label != null)
                label.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
        }

        private void OnDestroy()
        {
            if (_p1 != null) _p1.OnHealthChanged -= UpdateP1Health;
            if (_p2 != null) _p2.OnHealthChanged -= UpdateP2Health;
        }
    }
}
