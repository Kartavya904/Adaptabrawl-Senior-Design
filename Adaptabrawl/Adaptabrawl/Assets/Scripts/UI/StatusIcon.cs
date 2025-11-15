using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Adaptabrawl.Data;

namespace Adaptabrawl.UI
{
    public class StatusIcon : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI stackText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerFill;
        
        private StatusDef statusDef;
        private int currentStacks = 1;
        
        public StatusDef StatusDef => statusDef;
        
        public void Initialize(StatusDef status, int stacks)
        {
            statusDef = status;
            currentStacks = stacks;
            
            if (iconImage != null && status.statusIcon != null)
            {
                iconImage.sprite = status.statusIcon;
                iconImage.color = status.statusColor;
            }
            
            UpdateStacks(stacks);
            
            // Show/hide timer based on status settings
            if (timerText != null)
            {
                timerText.gameObject.SetActive(status.showTimer);
            }
            
            if (stackText != null)
            {
                stackText.gameObject.SetActive(status.showStacks && status.canStack);
            }
        }
        
        public void UpdateStacks(int stacks)
        {
            currentStacks = stacks;
            
            if (stackText != null && statusDef != null && statusDef.showStacks)
            {
                if (stacks > 1)
                {
                    stackText.text = stacks.ToString();
                    stackText.gameObject.SetActive(true);
                }
                else
                {
                    stackText.gameObject.SetActive(false);
                }
            }
        }
        
        public void UpdateTimer(float remainingTime, float totalTime)
        {
            if (timerText != null && statusDef != null && statusDef.showTimer)
            {
                timerText.text = remainingTime.ToString("F1");
            }
            
            if (timerFill != null && totalTime > 0f)
            {
                timerFill.fillAmount = remainingTime / totalTime;
            }
        }
    }
}

