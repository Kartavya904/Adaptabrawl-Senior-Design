using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// Shared Escape + controller Circle/B (east face) detection for "back / cancel" flows.
    /// </summary>
    public static class BackInputUtility
    {
        /// <summary>True if Escape was pressed this frame or any connected gamepad pressed Circle/B.</summary>
        public static bool WasBackOrCancelPressedThisFrame()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                return true;

            foreach (var pad in Gamepad.all)
            {
                if (pad != null && pad.buttonEast.wasPressedThisFrame)
                    return true;
            }

            return false;
        }

        /// <summary>Skip global back while the user is typing in a UI field.</summary>
        public static bool IsTextInputFocused()
        {
            var es = EventSystem.current;
            if (es == null || es.currentSelectedGameObject == null)
                return false;

            if (es.currentSelectedGameObject.GetComponent<TMP_InputField>() is { isFocused: true })
                return true;

            if (es.currentSelectedGameObject.GetComponent<InputField>() is { isFocused: true })
                return true;

            return false;
        }
    }
}
