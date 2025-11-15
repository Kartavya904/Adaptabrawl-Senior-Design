using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Adaptabrawl.Gameplay;
using Adaptabrawl.Data;
using System.Collections.Generic;
using System.Linq;

namespace Adaptabrawl.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("Player 1 HUD")]
        [SerializeField] private FighterController player1;
        [SerializeField] private Slider player1HealthBar;
        [SerializeField] private TextMeshProUGUI player1HealthText;
        [SerializeField] private Transform player1StatusContainer;
        [SerializeField] private GameObject statusIconPrefab;
        
        [Header("Player 2 HUD")]
        [SerializeField] private FighterController player2;
        [SerializeField] private Slider player2HealthBar;
        [SerializeField] private TextMeshProUGUI player2HealthText;
        [SerializeField] private Transform player2StatusContainer;
        
        [Header("Condition Banner")]
        [SerializeField] private GameObject conditionBanner;
        [SerializeField] private TextMeshProUGUI conditionBannerText;
        [SerializeField] private float bannerDisplayTime = 3f;
        
        private Dictionary<FighterController, List<StatusIcon>> statusIcons = new Dictionary<FighterController, List<StatusIcon>>();
        
        private void Start()
        {
            // Subscribe to health changes
            if (player1 != null)
            {
                player1.OnHealthChanged += UpdatePlayer1Health;
                SubscribeToStatusEffects(player1);
            }
            
            if (player2 != null)
            {
                player2.OnHealthChanged += UpdatePlayer2Health;
                SubscribeToStatusEffects(player2);
            }
            
            // Subscribe to condition system
            var conditionSystem = FindObjectOfType<AdaptiveConditionSystem>();
            if (conditionSystem != null)
            {
                conditionSystem.OnConditionBanner += ShowConditionBanner;
            }
        }
        
        private void OnDestroy()
        {
            if (player1 != null)
            {
                player1.OnHealthChanged -= UpdatePlayer1Health;
            }
            
            if (player2 != null)
            {
                player2.OnHealthChanged -= UpdatePlayer2Health;
            }
        }
        
        private void Update()
        {
            UpdateStatusIcons();
        }
        
        private void UpdatePlayer1Health(float current, float max)
        {
            if (player1HealthBar != null)
            {
                player1HealthBar.value = current / max;
            }
            
            if (player1HealthText != null)
            {
                player1HealthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }
        
        private void UpdatePlayer2Health(float current, float max)
        {
            if (player2HealthBar != null)
            {
                player2HealthBar.value = current / max;
            }
            
            if (player2HealthText != null)
            {
                player2HealthText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
            }
        }
        
        private void SubscribeToStatusEffects(FighterController fighter)
        {
            var statusSystem = fighter.GetComponent<StatusEffectSystem>();
            if (statusSystem != null)
            {
                statusSystem.OnStatusApplied += (status, stacks) => OnStatusApplied(fighter, status, stacks);
                statusSystem.OnStatusRemoved += (status) => OnStatusRemoved(fighter, status);
                statusSystem.OnStatusStackChanged += (status, stacks) => OnStatusStackChanged(fighter, status, stacks);
            }
        }
        
        private void OnStatusApplied(FighterController fighter, StatusDef status, int stacks)
        {
            CreateStatusIcon(fighter, status, stacks);
        }
        
        private void OnStatusRemoved(FighterController fighter, StatusDef status)
        {
            RemoveStatusIcon(fighter, status);
        }
        
        private void OnStatusStackChanged(FighterController fighter, StatusDef status, int stacks)
        {
            UpdateStatusIcon(fighter, status, stacks);
        }
        
        private void CreateStatusIcon(FighterController fighter, StatusDef status, int stacks)
        {
            Transform container = GetStatusContainer(fighter);
            if (container == null || statusIconPrefab == null) return;
            
            GameObject iconObj = Instantiate(statusIconPrefab, container);
            StatusIcon icon = iconObj.GetComponent<StatusIcon>();
            if (icon == null)
                icon = iconObj.AddComponent<StatusIcon>();
            
            icon.Initialize(status, stacks);
            
            if (!statusIcons.ContainsKey(fighter))
                statusIcons[fighter] = new List<StatusIcon>();
            
            statusIcons[fighter].Add(icon);
        }
        
        private void RemoveStatusIcon(FighterController fighter, StatusDef status)
        {
            if (!statusIcons.ContainsKey(fighter)) return;
            
            var icon = statusIcons[fighter].FirstOrDefault(i => i.StatusDef == status);
            if (icon != null)
            {
                statusIcons[fighter].Remove(icon);
                Destroy(icon.gameObject);
            }
        }
        
        private void UpdateStatusIcon(FighterController fighter, StatusDef status, int stacks)
        {
            if (!statusIcons.ContainsKey(fighter)) return;
            
            var icon = statusIcons[fighter].FirstOrDefault(i => i.StatusDef == status);
            if (icon != null)
            {
                icon.UpdateStacks(stacks);
            }
        }
        
        private void UpdateStatusIcons()
        {
            foreach (var kvp in statusIcons)
            {
                var fighter = kvp.Key;
                var statusSystem = fighter.GetComponent<StatusEffectSystem>();
                if (statusSystem == null) continue;
                
                var activeStatuses = statusSystem.GetActiveStatuses();
                
                foreach (var icon in kvp.Value)
                {
                    if (icon.StatusDef != null)
                    {
                        // Update timer if status has duration
                        // This would require getting remaining time from status system
                    }
                }
            }
        }
        
        private Transform GetStatusContainer(FighterController fighter)
        {
            if (fighter == player1)
                return player1StatusContainer;
            else if (fighter == player2)
                return player2StatusContainer;
            return null;
        }
        
        private void ShowConditionBanner(string text)
        {
            if (conditionBanner != null && conditionBannerText != null)
            {
                conditionBannerText.text = text;
                conditionBanner.SetActive(true);
                
                // Hide after display time
                StartCoroutine(HideBannerAfterDelay());
            }
        }
        
        private System.Collections.IEnumerator HideBannerAfterDelay()
        {
            yield return new WaitForSeconds(bannerDisplayTime);
            if (conditionBanner != null)
            {
                conditionBanner.SetActive(false);
            }
        }
    }
}

