using System.Reflection;
using TMPro;
using UnityEngine;

namespace Adaptabrawl.UI
{
    internal static class SetupCountdownVisualUtility
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        public static TextMeshProUGUI EnsureCountdown(TextMeshProUGUI current, string cloneName)
        {
            if (current != null)
                return current;

            var arenaUi = Object.FindFirstObjectByType<ArenaSelectUI>(FindObjectsInactive.Include);
            if (arenaUi == null)
                return null;

            var field = typeof(ArenaSelectUI).GetField("countdownText", PrivateInstance);
            if (field == null)
                return null;

            var template = field.GetValue(arenaUi) as TextMeshProUGUI;
            if (template == null)
                return null;

            Transform parent = template.transform.parent;
            Transform existing = parent != null ? parent.Find(cloneName) : null;
            if (existing != null && existing.TryGetComponent(out TextMeshProUGUI existingText))
                return existingText;

            var clone = Object.Instantiate(template.gameObject, parent);
            clone.name = cloneName;
            clone.SetActive(false);

            if (clone.TryGetComponent(out TextMeshProUGUI countdown))
                countdown.text = string.Empty;

            return countdown;
        }
    }
}
