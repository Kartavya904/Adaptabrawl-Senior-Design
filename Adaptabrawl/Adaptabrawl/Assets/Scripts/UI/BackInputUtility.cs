using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using Adaptabrawl.Settings;

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
            var bindings = ControlBindingsContext.EnsureExists();
            if (bindings.WasActionPressedThisFrame(ControlProfileId.GlobalKeyboardPlayer1, ControlActionId.BackCancel))
                return true;

            var lobby = Adaptabrawl.Gameplay.LobbyContext.Instance;
            if (lobby != null
                && Adaptabrawl.Gameplay.LobbyContext.IsDualKeyboardMode(lobby.p1InputDevice, lobby.p2InputDevice)
                && bindings.WasActionPressedThisFrame(ControlProfileId.GlobalKeyboardPlayer2, ControlActionId.BackCancel))
                return true;

            for (int i = 0; i < Gamepad.all.Count; i++)
            {
                if (Gamepad.all[i] != null && bindings.WasActionPressedThisFrame(ControlProfileId.GlobalController, ControlActionId.BackCancel, i))
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
