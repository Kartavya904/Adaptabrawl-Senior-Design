namespace Adaptabrawl.Gameplay
{
    using Adaptabrawl.Settings;

    /// <summary>
    /// Consistent setup UI copy from lobby device choice (keyboard vs controller).
    /// </summary>
    public static class LobbySetupInputHints
    {
        public static string DeviceLabel(int device01) => device01 == 1 ? "Controller" : "Keyboard";

        public static string ControllerReadyConfirmLine(int device01)
        {
            var bindings = ControlBindingsContext.EnsureExists();
            string label = device01 == 1
                ? bindings.GetDefaultBindingsLabel(ControlProfileId.GlobalController, ControlActionId.ReadyUp)
                : bindings.GetDefaultBindingsLabel(ControlProfileId.GlobalKeyboardPlayer1, ControlActionId.ReadyUp);
            return $"{label} — ready up";
        }

        public static string ControllerReadyCancelLine(int device01, bool isPlayer1)
        {
            var bindings = ControlBindingsContext.EnsureExists();
            string label = device01 == 1
                ? bindings.GetDefaultBindingsLabel(ControlProfileId.GlobalController, ControlActionId.BackCancel)
                : bindings.GetDefaultBindingsLabel(
                    isPlayer1 ? ControlProfileId.GlobalKeyboardPlayer1 : ControlProfileId.GlobalKeyboardPlayer2,
                    ControlActionId.BackCancel);
            return $"{label} — not ready";
        }

        public static string CharacterBrowseLine(int device01, bool isPlayer1)
        {
            if (device01 == 1)
                return "Stick / D-pad — change fighter";
            return isPlayer1 ? "A / D — change fighter" : "Arrow keys — change fighter";
        }

        public static string CharacterConfirmLine(int device01, bool isPlayer1)
        {
            if (device01 == 1)
                return "X (Cross) — lock in";
            return isPlayer1 ? "F — lock in" : "Enter — lock in";
        }

        public static string CharacterCancelLine(int device01, bool isPlayer1)
        {
            var bindings = ControlBindingsContext.EnsureExists();
            string label = device01 == 1
                ? bindings.GetDefaultBindingsLabel(ControlProfileId.GlobalController, ControlActionId.BackCancel)
                : bindings.GetDefaultBindingsLabel(
                    isPlayer1 ? ControlProfileId.GlobalKeyboardPlayer1 : ControlProfileId.GlobalKeyboardPlayer2,
                    ControlActionId.BackCancel);
            return $"{label} — change pick";
        }

        public static string CharacterReadyStatusLine(int device01, bool lockedIn)
        {
            string dev = DeviceLabel(device01);
            if (lockedIn)
                return $"LOCKED ({dev})";
            return $"SELECT ({dev})";
        }

        public static string ArenaReadyLine(int device01, bool isPlayer1)
        {
            var bindings = ControlBindingsContext.EnsureExists();
            string label = device01 == 1
                ? bindings.GetDefaultBindingsLabel(ControlProfileId.GlobalController, ControlActionId.ReadyUp)
                : bindings.GetDefaultBindingsLabel(
                    isPlayer1 ? ControlProfileId.GlobalKeyboardPlayer1 : ControlProfileId.GlobalKeyboardPlayer2,
                    ControlActionId.ReadyUp);
            return $"{label} — ready";
        }

        public static string ArenaUnreadyLine(int device01, bool isPlayer1)
        {
            var bindings = ControlBindingsContext.EnsureExists();
            string label = device01 == 1
                ? bindings.GetDefaultBindingsLabel(ControlProfileId.GlobalController, ControlActionId.BackCancel)
                : bindings.GetDefaultBindingsLabel(
                    isPlayer1 ? ControlProfileId.GlobalKeyboardPlayer1 : ControlProfileId.GlobalKeyboardPlayer2,
                    ControlActionId.BackCancel);
            return $"{label} — undo ready";
        }

        public static string CharacterConfirmButtonRich(int device01, bool lockedIn, bool isPlayer1)
        {
            string dev = DeviceLabel(device01);
            string browse = CharacterBrowseLine(device01, isPlayer1);
            string cancel = CharacterCancelLine(device01, isPlayer1);
            if (lockedIn)
                return $"LOCKED IN\n<size=62%><color=#aaaaaa>{dev}</color>\n{cancel}\n<size=52%>Unlock to browse fighters again.</size>";
            string confirm = CharacterConfirmLine(device01, isPlayer1);
            return $"Lock in\n<size=62%><color=#88ccff>{dev}</color>\n{confirm}\n{browse}\n<size=52%><color=#999999>When locked: {cancel}</color></size>";
        }

        public static string ControllerReadyButtonRich(bool ready, int device01, bool isPlayer1)
        {
            if (ready)
                return $"Ready!\n<size=65%>{ControllerReadyCancelLine(device01, isPlayer1)}</size>";
            return $"Ready\n<size=65%><color=#88ccff>{DeviceLabel(device01)}</color>\n{ControllerReadyConfirmLine(device01)}</size>";
        }

        public static string ArenaReadyButtonRich(bool ready, int device01, bool isPlayer1)
        {
            string dev = DeviceLabel(device01);
            string undo = ArenaUnreadyLine(device01, isPlayer1);
            if (ready)
                return $"READY!\n<size=62%>{undo}\n<size=52%>Use the same control to undo.</size>";
            return $"Ready\n<size=62%><color=#88ccff>{dev}</color>\n{ArenaReadyLine(device01, isPlayer1)}\n<size=52%><color=#999>After ready: {undo}</color></size>";
        }
    }
}
