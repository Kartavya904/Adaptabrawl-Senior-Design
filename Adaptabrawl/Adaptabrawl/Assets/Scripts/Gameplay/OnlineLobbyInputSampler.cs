using UnityEngine;
using UnityEngine.InputSystem;
using Adaptabrawl.Settings;

namespace Adaptabrawl.Gameplay
{
    /// <summary>
    /// Reads P1/P2 input for online play using the same device assignment as <see cref="LobbyContext"/>
    /// (keyboard vs controller and correct gamepad slot). Matches <see cref="PlayerController_Platform"/> gamepad logic.
    /// </summary>
    public static class OnlineLobbyInputSampler
    {
        public static void SamplePlayer(int playerOneOrTwo, out bool left, out bool right, out bool crouch, out bool sprint,
            out bool jumpDown, out bool attackDown, out bool blockHeld, out bool blockDown, out bool blockUp, out bool dodgeDown)
        {
            left = right = crouch = sprint = false;
            jumpDown = attackDown = blockDown = blockUp = dodgeDown = false;
            blockHeld = false;

            var bindings = ControlBindingsContext.EnsureExists();
            var lobby = LobbyContext.Instance;
            int p1Dev = lobby != null ? lobby.p1InputDevice : 0;
            int p2Dev = lobby != null ? lobby.p2InputDevice : 0;
            bool dualKeyboard = LobbyContext.IsDualKeyboardMode(p1Dev, p2Dev);

            bool useGamepad = playerOneOrTwo == 1 ? p1Dev == 1 : p2Dev == 1;
            if (useGamepad)
            {
                int gamepadIndex = LobbyContext.GetGamepadListIndexForPlayer(playerOneOrTwo, p1Dev, p2Dev);
                left = bindings.IsActionHeld(ControlProfileId.Controller, ControlActionId.MoveLeft, gamepadIndex);
                right = bindings.IsActionHeld(ControlProfileId.Controller, ControlActionId.MoveRight, gamepadIndex);
                crouch = bindings.IsActionHeld(ControlProfileId.Controller, ControlActionId.Crouch, gamepadIndex);
                sprint = gamepadIndex >= 0 && gamepadIndex < Gamepad.all.Count && Gamepad.all[gamepadIndex] != null
                    && Gamepad.all[gamepadIndex].leftTrigger.isPressed;
                jumpDown = bindings.WasActionPressedThisFrame(ControlProfileId.Controller, ControlActionId.Jump, gamepadIndex);
                attackDown = bindings.WasActionPressedThisFrame(ControlProfileId.Controller, ControlActionId.Attack, gamepadIndex);
                blockHeld = bindings.IsActionHeld(ControlProfileId.Controller, ControlActionId.Block, gamepadIndex);
                blockDown = bindings.WasActionPressedThisFrame(ControlProfileId.Controller, ControlActionId.Block, gamepadIndex);
                blockUp = bindings.WasActionReleasedThisFrame(ControlProfileId.Controller, ControlActionId.Block, gamepadIndex);
                dodgeDown = bindings.WasActionPressedThisFrame(ControlProfileId.Controller, ControlActionId.Dodge, gamepadIndex);
                return;
            }

            ControlProfileId profile = ControlBindingProfileResolver.ResolveGameplayKeyboardProfile(playerOneOrTwo, dualKeyboard);
            left = bindings.IsActionHeld(profile, ControlActionId.MoveLeft);
            right = bindings.IsActionHeld(profile, ControlActionId.MoveRight);
            crouch = bindings.IsActionHeld(profile, ControlActionId.Crouch);
            sprint = dualKeyboard && playerOneOrTwo == 2
                ? UnityEngine.Input.GetKey(KeyCode.RightControl)
                : UnityEngine.Input.GetKey(KeyCode.LeftShift);
            jumpDown = bindings.WasActionPressedThisFrame(profile, ControlActionId.Jump);
            attackDown = bindings.WasActionPressedThisFrame(profile, ControlActionId.Attack);
            blockHeld = bindings.IsActionHeld(profile, ControlActionId.Block);
            blockDown = bindings.WasActionPressedThisFrame(profile, ControlActionId.Block);
            blockUp = bindings.WasActionReleasedThisFrame(profile, ControlActionId.Block);
            dodgeDown = bindings.WasActionPressedThisFrame(profile, ControlActionId.Dodge);
        }
    }
}
