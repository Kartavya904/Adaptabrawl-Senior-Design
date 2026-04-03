using UnityEngine;
using UnityEngine.InputSystem;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Reads P1/P2 input for online play using the same device assignment as <see cref="LobbyContext"/>
    /// (keyboard vs controller and correct gamepad slot). Matches <see cref="PlayerController_Platform"/> gamepad logic.
    /// </summary>
    public static class OnlineLobbyInputSampler
    {
        private const float StickDead = 0.5f;

        public static void SamplePlayer(int playerOneOrTwo, out bool left, out bool right, out bool crouch, out bool sprint,
            out bool jumpDown, out bool attackDown, out bool blockHeld, out bool blockDown, out bool blockUp, out bool dodgeDown)
        {
            left = right = crouch = sprint = false;
            jumpDown = attackDown = blockDown = blockUp = dodgeDown = false;
            blockHeld = false;

            var lobby = LobbyContext.Instance;
            int p1Dev = lobby != null ? lobby.p1InputDevice : 0;
            int p2Dev = lobby != null ? lobby.p2InputDevice : 0;

            bool useGamepad = playerOneOrTwo == 1 ? p1Dev == 1 : p2Dev == 1;
            if (useGamepad && LobbyContext.TryGetGamepadForPlayer(playerOneOrTwo, p1Dev, p2Dev, out var pad))
            {
                SampleGamepad(pad, out left, out right, out crouch, out sprint,
                    out jumpDown, out attackDown, out blockHeld, out blockDown, out blockUp, out dodgeDown);
                return;
            }

            if (playerOneOrTwo == 1)
                SampleKeyboardP1(out left, out right, out crouch, out sprint,
                    out jumpDown, out attackDown, out blockHeld, out blockDown, out blockUp, out dodgeDown);
            else
                SampleKeyboardP2(out left, out right, out crouch, out sprint,
                    out jumpDown, out attackDown, out blockHeld, out blockDown, out blockUp, out dodgeDown);
        }

        private static void SampleGamepad(Gamepad pad, out bool left, out bool right, out bool crouch, out bool sprint,
            out bool jumpDown, out bool attackDown, out bool blockHeld, out bool blockDown, out bool blockUp, out bool dodgeDown)
        {
            float sx = pad.leftStick.x.ReadValue();
            float sy = pad.leftStick.y.ReadValue();
            left = sx < -StickDead || pad.dpad.left.isPressed;
            right = sx > StickDead || pad.dpad.right.isPressed;
            crouch = sy < -StickDead || pad.dpad.down.isPressed;
            sprint = pad.leftTrigger.isPressed;
            jumpDown = pad.buttonSouth.wasPressedThisFrame;
            attackDown = pad.buttonWest.wasPressedThisFrame;
            blockHeld = pad.rightTrigger.isPressed;
            blockDown = pad.rightTrigger.wasPressedThisFrame;
            blockUp = pad.rightTrigger.wasReleasedThisFrame;
            dodgeDown = pad.buttonEast.wasPressedThisFrame;
        }

        private static void SampleKeyboardP1(out bool left, out bool right, out bool crouch, out bool sprint,
            out bool jumpDown, out bool attackDown, out bool blockHeld, out bool blockDown, out bool blockUp, out bool dodgeDown)
        {
            // UnityEngine.Input — fully qualified so "Input" does not resolve to Adaptabrawl.Input.
            left = UnityEngine.Input.GetKey(KeyCode.A);
            right = UnityEngine.Input.GetKey(KeyCode.D);
            crouch = UnityEngine.Input.GetKey(KeyCode.S);
            sprint = UnityEngine.Input.GetKey(KeyCode.LeftShift);
            jumpDown = UnityEngine.Input.GetKeyDown(KeyCode.W);
            attackDown = UnityEngine.Input.GetKeyDown(KeyCode.F);
            blockHeld = UnityEngine.Input.GetKey(KeyCode.G);
            blockDown = UnityEngine.Input.GetKeyDown(KeyCode.G);
            blockUp = UnityEngine.Input.GetKeyUp(KeyCode.G);
            dodgeDown = UnityEngine.Input.GetKeyDown(KeyCode.Space);
        }

        private static void SampleKeyboardP2(out bool left, out bool right, out bool crouch, out bool sprint,
            out bool jumpDown, out bool attackDown, out bool blockHeld, out bool blockDown, out bool blockUp, out bool dodgeDown)
        {
            left = UnityEngine.Input.GetKey(KeyCode.LeftArrow);
            right = UnityEngine.Input.GetKey(KeyCode.RightArrow);
            crouch = UnityEngine.Input.GetKey(KeyCode.DownArrow);
            sprint = UnityEngine.Input.GetKey(KeyCode.RightShift);
            jumpDown = UnityEngine.Input.GetKeyDown(KeyCode.UpArrow);
            attackDown = UnityEngine.Input.GetKeyDown(KeyCode.Keypad1);
            blockHeld = UnityEngine.Input.GetKey(KeyCode.Keypad2);
            blockDown = UnityEngine.Input.GetKeyDown(KeyCode.Keypad2);
            blockUp = UnityEngine.Input.GetKeyUp(KeyCode.Keypad2);
            dodgeDown = UnityEngine.Input.GetKeyDown(KeyCode.Keypad0);
        }
    }
}
