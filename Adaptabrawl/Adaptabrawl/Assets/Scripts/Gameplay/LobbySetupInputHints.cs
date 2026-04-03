namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Consistent setup UI copy from lobby device choice (keyboard vs controller).
    /// </summary>
    public static class LobbySetupInputHints
    {
        public static string DeviceLabel(int device01) => device01 == 1 ? "Controller" : "Keyboard";

        public static string ControllerReadyConfirmLine(int device01)
        {
            return device01 == 1 ? "Triangle — ready up" : "Space — ready up";
        }

        public static string ControllerReadyCancelLine(int device01, bool isPlayer1)
        {
            if (device01 == 1)
                return "Circle — not ready";
            return isPlayer1 ? "Esc — not ready" : "Backspace — not ready";
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
                return "Triangle — lock in";
            return isPlayer1 ? "Space — lock in" : "Enter — lock in";
        }

        public static string CharacterCancelLine(int device01, bool isPlayer1)
        {
            if (device01 == 1)
                return "Circle — change pick";
            return isPlayer1 ? "Esc — change pick" : "Backspace — change pick";
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
            if (device01 == 1)
                return "Triangle — ready";
            return isPlayer1 ? "Space — ready" : "Enter — ready";
        }

        public static string ArenaUnreadyLine(int device01, bool isPlayer1)
        {
            if (device01 == 1)
                return "Circle — undo ready";
            return isPlayer1 ? "Esc — undo ready" : "Backspace — undo ready";
        }

        public static string CharacterConfirmButtonRich(int device01, bool lockedIn, bool isPlayer1)
        {
            string dev = DeviceLabel(device01);
            string browse = CharacterBrowseLine(device01, isPlayer1);
            if (lockedIn)
                return $"LOCKED IN\n<size=65%><color=#aaaaaa>{dev}</color>\n{CharacterCancelLine(device01, isPlayer1)}</size>";
            string confirm = CharacterConfirmLine(device01, isPlayer1);
            return $"Lock in\n<size=65%><color=#88ccff>{dev}</color>\n{confirm}\n{browse}</size>";
        }

        public static string ControllerReadyButtonRich(bool ready, int device01, bool isPlayer1)
        {
            if (ready)
                return $"Ready!\n<size=65%>{ControllerReadyCancelLine(device01, isPlayer1)}</size>";
            return $"Ready\n<size=65%><color=#88ccff>{DeviceLabel(device01)}</color>\n{ControllerReadyConfirmLine(device01)}</size>";
        }

        public static string ArenaReadyButtonRich(bool ready, int device01, bool isPlayer1)
        {
            if (ready)
                return $"READY!\n<size=65%>{ArenaUnreadyLine(device01, isPlayer1)}</size>";
            string dev = DeviceLabel(device01);
            return $"Ready\n<size=65%><color=#88ccff>{dev}</color>\n{ArenaReadyLine(device01, isPlayer1)}</size>";
        }
    }
}
