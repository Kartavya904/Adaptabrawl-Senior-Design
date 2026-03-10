using UnityEngine;
using UnityEngine.UI;
using Adaptabrawl.Gameplay;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// Drives health bar Sliders for both players.
    /// Assign p1HealthSlider and p2HealthSlider in the Inspector.
    /// The Slider Min should be 0, Max should be 1.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Player 1")]
        [SerializeField] private Slider p1HealthSlider;

        [Header("Player 2")]
        [SerializeField] private Slider p2HealthSlider;

        private FighterController _p1;
        private FighterController _p2;

        private void Start()
        {
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
                BindFighters(fighters[0], fighters[1]);
            else
                Debug.LogWarning("GameHUD: Could not find fighters — health bars will not update.");
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

        private void UpdateP1Health(float current, float max)
        {
            if (p1HealthSlider != null)
                p1HealthSlider.value = max > 0f ? current / max : 0f;
        }

        private void UpdateP2Health(float current, float max)
        {
            if (p2HealthSlider != null)
                p2HealthSlider.value = max > 0f ? current / max : 0f;
        }

        private void OnDestroy()
        {
            if (_p1 != null) _p1.OnHealthChanged -= UpdateP1Health;
            if (_p2 != null) _p2.OnHealthChanged -= UpdateP2Health;
        }
    }
}
