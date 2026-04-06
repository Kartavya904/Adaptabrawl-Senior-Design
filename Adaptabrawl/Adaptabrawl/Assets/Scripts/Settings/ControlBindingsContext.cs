using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Adaptabrawl.Settings
{
    public enum ControlProfileId
    {
        Keyboard1P,
        Keyboard2PPlayer1,
        Keyboard2PPlayer2,
        Controller,
        GlobalKeyboardPlayer1,
        GlobalKeyboardPlayer2,
        GlobalController
    }

    public enum ControlActionId
    {
        MoveLeft,
        MoveRight,
        Crouch,
        Jump,
        Attack,
        Block,
        Dodge,
        SpecialLight,
        SpecialHeavy,
        ReadyUp,
        Pause,
        BackCancel
    }

    public enum ControlBindingKind
    {
        Key,
        MouseLeft,
        MouseRight,
        GamepadSouth,
        GamepadEast,
        GamepadWest,
        GamepadNorth,
        GamepadLeftShoulder,
        GamepadRightShoulder,
        GamepadLeftTrigger,
        GamepadRightTrigger,
        GamepadStart,
        GamepadSelect,
        GamepadDpadLeft,
        GamepadDpadRight,
        GamepadDpadUp,
        GamepadDpadDown,
        GamepadLeftStickLeft,
        GamepadLeftStickRight,
        GamepadLeftStickUp,
        GamepadLeftStickDown
    }

    public enum ControllerDisplayFamily
    {
        Generic,
        Xbox,
        PlayStation
    }

    [Serializable]
    public class ControlBinding
    {
        public ControlBindingKind kind;
        public KeyCode keyCode;

        public ControlBinding Clone()
        {
            return new ControlBinding
            {
                kind = kind,
                keyCode = keyCode
            };
        }

        public static ControlBinding Key(KeyCode keyCode)
        {
            return new ControlBinding
            {
                kind = ControlBindingKind.Key,
                keyCode = keyCode
            };
        }

        public static ControlBinding Binding(ControlBindingKind kind)
        {
            return new ControlBinding
            {
                kind = kind,
                keyCode = KeyCode.None
            };
        }
    }

    [Serializable]
    public class ControlActionBindings
    {
        public ControlActionId action;
        public List<ControlBinding> bindings = new List<ControlBinding>();

        public ControlActionBindings Clone()
        {
            var clone = new ControlActionBindings
            {
                action = action,
                bindings = new List<ControlBinding>(bindings.Count)
            };

            foreach (var binding in bindings)
            {
                if (binding != null)
                    clone.bindings.Add(binding.Clone());
            }

            return clone;
        }
    }

    [Serializable]
    public class ControlProfileBindings
    {
        public ControlProfileId profile;
        public List<ControlActionBindings> actions = new List<ControlActionBindings>();

        public ControlProfileBindings Clone()
        {
            var clone = new ControlProfileBindings
            {
                profile = profile,
                actions = new List<ControlActionBindings>(actions.Count)
            };

            foreach (var actionBindings in actions)
            {
                if (actionBindings != null)
                    clone.actions.Add(actionBindings.Clone());
            }

            return clone;
        }
    }

    [Serializable]
    public class ControlBindingsSaveData
    {
        public int version = 1;
        public List<ControlProfileBindings> profiles = new List<ControlProfileBindings>();
    }

    public class ControlBindingsContext : MonoBehaviour
    {
        public static ControlBindingsContext Instance { get; private set; }

        private const string PlayerPrefsKey = "ControlBindingsContext.SaveData";
        private const float StickThreshold = 0.22f;

        private List<ControlProfileBindings> defaultProfiles = new List<ControlProfileBindings>();
        [SerializeField] private List<ControlProfileBindings> currentProfiles = new List<ControlProfileBindings>();

        public event Action BindingsChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureExists();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        public static ControlBindingsContext EnsureExists()
        {
            if (Instance != null)
                return Instance;

            var existing = FindFirstObjectByType<ControlBindingsContext>(FindObjectsInactive.Include);
            if (existing != null)
                return existing;

            var go = new GameObject("ControlBindingsContext");
            return go.AddComponent<ControlBindingsContext>();
        }

        public bool HasConnectedController => Gamepad.all.Any(pad => pad != null);

        public ControllerDisplayFamily GetControllerFamily()
        {
            var pad = Gamepad.all.FirstOrDefault(gamepad => gamepad != null);
            if (pad == null)
                return ControllerDisplayFamily.Generic;

            string label = $"{pad.displayName} {pad.description.interfaceName} {pad.description.manufacturer} {pad.description.product}".ToLowerInvariant();
            if (label.Contains("dualsense") || label.Contains("dualshock") || label.Contains("playstation") || label.Contains("wireless controller"))
                return ControllerDisplayFamily.PlayStation;
            if (label.Contains("xbox") || label.Contains("xinput"))
                return ControllerDisplayFamily.Xbox;

            return ControllerDisplayFamily.Generic;
        }

        public IReadOnlyList<ControlBinding> GetCurrentBindings(ControlProfileId profile, ControlActionId action)
        {
            return GetBindings(currentProfiles, profile, action);
        }

        public IReadOnlyList<ControlBinding> GetDefaultBindings(ControlProfileId profile, ControlActionId action)
        {
            return GetBindings(defaultProfiles, profile, action);
        }

        public void SetSingleBinding(ControlProfileId profile, ControlActionId action, ControlBinding binding)
        {
            if (binding == null)
                return;

            var actionBindings = GetOrCreateActionBindings(currentProfiles, profile, action);
            actionBindings.bindings.Clear();
            actionBindings.bindings.Add(binding.Clone());
            Persist();
        }

        public void ResetProfile(ControlProfileId profile)
        {
            var defaultProfile = FindProfile(defaultProfiles, profile);
            if (defaultProfile == null)
                return;

            int targetIndex = currentProfiles.FindIndex(item => item.profile == profile);
            var clone = defaultProfile.Clone();

            if (targetIndex >= 0)
                currentProfiles[targetIndex] = clone;
            else
                currentProfiles.Add(clone);

            Persist();
        }

        public void ResetAllToDefaults()
        {
            currentProfiles = CloneProfiles(defaultProfiles);
            Persist();
        }

        public bool IsActionHeld(ControlProfileId profile, ControlActionId action, int gamepadIndex = -1)
        {
            return EvaluateBindings(GetCurrentBindings(profile, action), gamepadIndex, InputPhase.Held);
        }

        public bool WasActionPressedThisFrame(ControlProfileId profile, ControlActionId action, int gamepadIndex = -1)
        {
            return EvaluateBindings(GetCurrentBindings(profile, action), gamepadIndex, InputPhase.Pressed);
        }

        public bool WasActionReleasedThisFrame(ControlProfileId profile, ControlActionId action, int gamepadIndex = -1)
        {
            return EvaluateBindings(GetCurrentBindings(profile, action), gamepadIndex, InputPhase.Released);
        }

        public string GetActionLabel(ControlActionId action)
        {
            return action switch
            {
                ControlActionId.MoveLeft => "Move Left",
                ControlActionId.MoveRight => "Move Right",
                ControlActionId.Crouch => "Crouch",
                ControlActionId.Jump => "Jump",
                ControlActionId.Attack => "Attack",
                ControlActionId.Block => "Block",
                ControlActionId.Dodge => "Dodge",
                ControlActionId.SpecialLight => "Special Light",
                ControlActionId.SpecialHeavy => "Special Heavy",
                ControlActionId.ReadyUp => "Ready Up",
                ControlActionId.Pause => "Pause",
                ControlActionId.BackCancel => "Back / Cancel",
                _ => action.ToString()
            };
        }

        public string GetBindingsLabel(ControlProfileId profile, ControlActionId action)
        {
            return GetBindingsLabel(GetCurrentBindings(profile, action));
        }

        public string GetDefaultBindingsLabel(ControlProfileId profile, ControlActionId action)
        {
            return GetBindingsLabel(GetDefaultBindings(profile, action));
        }

        public string GetBindingsLabel(IEnumerable<ControlBinding> bindings)
        {
            if (bindings == null)
                return "Unbound";

            var parts = new List<string>();
            foreach (var binding in bindings)
            {
                if (binding == null)
                    continue;

                string label = GetBindingLabel(binding);
                if (!string.IsNullOrWhiteSpace(label))
                    parts.Add(label);
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "Unbound";
        }

        public string GetBindingLabel(ControlBinding binding)
        {
            if (binding == null)
                return "Unbound";

            ControllerDisplayFamily family = GetControllerFamily();
            return binding.kind switch
            {
                ControlBindingKind.Key => GetKeyLabel(binding.keyCode),
                ControlBindingKind.MouseLeft => "Left Mouse",
                ControlBindingKind.MouseRight => "Right Mouse",
                ControlBindingKind.GamepadSouth => family == ControllerDisplayFamily.PlayStation ? "Cross" : family == ControllerDisplayFamily.Xbox ? "A" : "South Button",
                ControlBindingKind.GamepadEast => family == ControllerDisplayFamily.PlayStation ? "Circle" : family == ControllerDisplayFamily.Xbox ? "B" : "East Button",
                ControlBindingKind.GamepadWest => family == ControllerDisplayFamily.PlayStation ? "Square" : family == ControllerDisplayFamily.Xbox ? "X" : "West Button",
                ControlBindingKind.GamepadNorth => family == ControllerDisplayFamily.PlayStation ? "Triangle" : family == ControllerDisplayFamily.Xbox ? "Y" : "North Button",
                ControlBindingKind.GamepadLeftShoulder => family == ControllerDisplayFamily.PlayStation ? "L1" : family == ControllerDisplayFamily.Xbox ? "LB" : "Left Shoulder",
                ControlBindingKind.GamepadRightShoulder => family == ControllerDisplayFamily.PlayStation ? "R1" : family == ControllerDisplayFamily.Xbox ? "RB" : "Right Shoulder",
                ControlBindingKind.GamepadLeftTrigger => family == ControllerDisplayFamily.PlayStation ? "L2" : family == ControllerDisplayFamily.Xbox ? "LT" : "Left Trigger",
                ControlBindingKind.GamepadRightTrigger => family == ControllerDisplayFamily.PlayStation ? "R2" : family == ControllerDisplayFamily.Xbox ? "RT" : "Right Trigger",
                ControlBindingKind.GamepadStart => family == ControllerDisplayFamily.PlayStation ? "Options" : family == ControllerDisplayFamily.Xbox ? "Menu" : "Start",
                ControlBindingKind.GamepadSelect => family == ControllerDisplayFamily.PlayStation ? "Create" : family == ControllerDisplayFamily.Xbox ? "View" : "Select",
                ControlBindingKind.GamepadDpadLeft => "D-Pad Left",
                ControlBindingKind.GamepadDpadRight => "D-Pad Right",
                ControlBindingKind.GamepadDpadUp => "D-Pad Up",
                ControlBindingKind.GamepadDpadDown => "D-Pad Down",
                ControlBindingKind.GamepadLeftStickLeft => "Left Stick Left",
                ControlBindingKind.GamepadLeftStickRight => "Left Stick Right",
                ControlBindingKind.GamepadLeftStickUp => "Left Stick Up",
                ControlBindingKind.GamepadLeftStickDown => "Left Stick Down",
                _ => binding.kind.ToString()
            };
        }

        private void Initialize()
        {
            defaultProfiles = BuildDefaultProfiles();
            currentProfiles = CloneProfiles(defaultProfiles);
            LoadSavedBindings();
        }

        private void Persist()
        {
            var data = new ControlBindingsSaveData
            {
                version = 1,
                profiles = CloneProfiles(currentProfiles)
            };

            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            BindingsChanged?.Invoke();
        }

        private void LoadSavedBindings()
        {
            if (!PlayerPrefs.HasKey(PlayerPrefsKey))
                return;

            string raw = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
                return;

            try
            {
                var data = JsonUtility.FromJson<ControlBindingsSaveData>(raw);
                if (data?.profiles == null || data.profiles.Count == 0)
                    return;

                foreach (var savedProfile in data.profiles)
                {
                    if (savedProfile == null)
                        continue;

                    var targetProfile = GetOrCreateProfile(currentProfiles, savedProfile.profile);
                    foreach (var savedAction in savedProfile.actions)
                    {
                        if (savedAction == null)
                            continue;

                        var targetAction = GetOrCreateActionBindings(targetProfile, savedAction.action);
                        targetAction.bindings = new List<ControlBinding>();
                        foreach (var binding in savedAction.bindings)
                        {
                            if (binding != null)
                                targetAction.bindings.Add(binding.Clone());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ControlBindingsContext] Failed to load saved bindings. Resetting to defaults. {ex.Message}");
                currentProfiles = CloneProfiles(defaultProfiles);
            }
        }

        private static List<ControlProfileBindings> BuildDefaultProfiles()
        {
            return new List<ControlProfileBindings>
            {
                BuildProfile(ControlProfileId.Keyboard1P, new Dictionary<ControlActionId, IEnumerable<ControlBinding>>
                {
                    { ControlActionId.MoveLeft, new[] { ControlBinding.Key(KeyCode.A), ControlBinding.Key(KeyCode.LeftArrow) } },
                    { ControlActionId.MoveRight, new[] { ControlBinding.Key(KeyCode.D), ControlBinding.Key(KeyCode.RightArrow) } },
                    { ControlActionId.Crouch, new[] { ControlBinding.Key(KeyCode.S), ControlBinding.Key(KeyCode.DownArrow) } },
                    { ControlActionId.Jump, new[] { ControlBinding.Key(KeyCode.W), ControlBinding.Key(KeyCode.UpArrow) } },
                    { ControlActionId.Attack, new[] { ControlBinding.Binding(ControlBindingKind.MouseLeft), ControlBinding.Key(KeyCode.F) } },
                    { ControlActionId.Block, new[] { ControlBinding.Binding(ControlBindingKind.MouseRight), ControlBinding.Key(KeyCode.G) } },
                    { ControlActionId.Dodge, new[] { ControlBinding.Key(KeyCode.Space) } },
                    { ControlActionId.SpecialLight, new[] { ControlBinding.Key(KeyCode.Q) } },
                    { ControlActionId.SpecialHeavy, new[] { ControlBinding.Key(KeyCode.E) } }
                }),
                BuildProfile(ControlProfileId.Keyboard2PPlayer1, new Dictionary<ControlActionId, IEnumerable<ControlBinding>>
                {
                    { ControlActionId.MoveLeft, new[] { ControlBinding.Key(KeyCode.A) } },
                    { ControlActionId.MoveRight, new[] { ControlBinding.Key(KeyCode.D) } },
                    { ControlActionId.Crouch, new[] { ControlBinding.Key(KeyCode.S) } },
                    { ControlActionId.Jump, new[] { ControlBinding.Key(KeyCode.W) } },
                    { ControlActionId.Attack, new[] { ControlBinding.Key(KeyCode.Tab) } },
                    { ControlActionId.Block, new[] { ControlBinding.Key(KeyCode.R) } },
                    { ControlActionId.Dodge, new[] { ControlBinding.Key(KeyCode.LeftShift) } },
                    { ControlActionId.SpecialLight, new[] { ControlBinding.Key(KeyCode.E) } },
                    { ControlActionId.SpecialHeavy, new[] { ControlBinding.Key(KeyCode.Q) } }
                }),
                BuildProfile(ControlProfileId.Keyboard2PPlayer2, new Dictionary<ControlActionId, IEnumerable<ControlBinding>>
                {
                    { ControlActionId.MoveLeft, new[] { ControlBinding.Key(KeyCode.LeftArrow) } },
                    { ControlActionId.MoveRight, new[] { ControlBinding.Key(KeyCode.RightArrow) } },
                    { ControlActionId.Crouch, new[] { ControlBinding.Key(KeyCode.DownArrow) } },
                    { ControlActionId.Jump, new[] { ControlBinding.Key(KeyCode.UpArrow) } },
                    { ControlActionId.Attack, new[] { ControlBinding.Key(KeyCode.L) } },
                    { ControlActionId.Block, new[] { ControlBinding.Key(KeyCode.Semicolon) } },
                    { ControlActionId.Dodge, new[] { ControlBinding.Key(KeyCode.RightShift) } },
                    { ControlActionId.SpecialLight, new[] { ControlBinding.Key(KeyCode.Keypad1) } },
                    { ControlActionId.SpecialHeavy, new[] { ControlBinding.Key(KeyCode.Keypad2) } }
                }),
                BuildProfile(ControlProfileId.Controller, new Dictionary<ControlActionId, IEnumerable<ControlBinding>>
                {
                    { ControlActionId.MoveLeft, new[] { ControlBinding.Binding(ControlBindingKind.GamepadLeftStickLeft), ControlBinding.Binding(ControlBindingKind.GamepadDpadLeft) } },
                    { ControlActionId.MoveRight, new[] { ControlBinding.Binding(ControlBindingKind.GamepadLeftStickRight), ControlBinding.Binding(ControlBindingKind.GamepadDpadRight) } },
                    { ControlActionId.Crouch, new[] { ControlBinding.Binding(ControlBindingKind.GamepadLeftStickDown), ControlBinding.Binding(ControlBindingKind.GamepadDpadDown) } },
                    { ControlActionId.Jump, new[] { ControlBinding.Binding(ControlBindingKind.GamepadLeftStickUp), ControlBinding.Binding(ControlBindingKind.GamepadDpadUp), ControlBinding.Binding(ControlBindingKind.GamepadSouth) } },
                    { ControlActionId.Attack, new[] { ControlBinding.Binding(ControlBindingKind.GamepadWest) } },
                    { ControlActionId.Block, new[] { ControlBinding.Binding(ControlBindingKind.GamepadRightTrigger) } },
                    { ControlActionId.Dodge, new[] { ControlBinding.Binding(ControlBindingKind.GamepadEast) } },
                    { ControlActionId.SpecialLight, new[] { ControlBinding.Binding(ControlBindingKind.GamepadRightShoulder) } },
                    { ControlActionId.SpecialHeavy, new[] { ControlBinding.Binding(ControlBindingKind.GamepadLeftShoulder) } }
                }),
                BuildProfile(ControlProfileId.GlobalKeyboardPlayer1, new Dictionary<ControlActionId, IEnumerable<ControlBinding>>
                {
                    { ControlActionId.ReadyUp, new[] { ControlBinding.Key(KeyCode.Return) } },
                    { ControlActionId.Pause, new[] { ControlBinding.Key(KeyCode.Escape) } },
                    { ControlActionId.BackCancel, new[] { ControlBinding.Key(KeyCode.Escape) } }
                }),
                BuildProfile(ControlProfileId.GlobalKeyboardPlayer2, new Dictionary<ControlActionId, IEnumerable<ControlBinding>>
                {
                    { ControlActionId.ReadyUp, new[] { ControlBinding.Key(KeyCode.Return) } },
                    { ControlActionId.Pause, new[] { ControlBinding.Key(KeyCode.Delete) } },
                    { ControlActionId.BackCancel, new[] { ControlBinding.Key(KeyCode.Delete) } }
                }),
                BuildProfile(ControlProfileId.GlobalController, new Dictionary<ControlActionId, IEnumerable<ControlBinding>>
                {
                    { ControlActionId.ReadyUp, new[] { ControlBinding.Binding(ControlBindingKind.GamepadNorth) } },
                    { ControlActionId.Pause, new[] { ControlBinding.Binding(ControlBindingKind.GamepadStart) } },
                    { ControlActionId.BackCancel, new[] { ControlBinding.Binding(ControlBindingKind.GamepadEast) } }
                })
            };
        }

        private static ControlProfileBindings BuildProfile(ControlProfileId profileId, Dictionary<ControlActionId, IEnumerable<ControlBinding>> actionMap)
        {
            var profile = new ControlProfileBindings
            {
                profile = profileId,
                actions = new List<ControlActionBindings>()
            };

            foreach (var pair in actionMap)
            {
                profile.actions.Add(new ControlActionBindings
                {
                    action = pair.Key,
                    bindings = pair.Value?.Select(binding => binding.Clone()).ToList() ?? new List<ControlBinding>()
                });
            }

            return profile;
        }

        private IReadOnlyList<ControlBinding> GetBindings(List<ControlProfileBindings> profiles, ControlProfileId profile, ControlActionId action)
        {
            var bindings = FindProfile(profiles, profile)?.actions
                ?.FirstOrDefault(item => item.action == action)
                ?.bindings;

            return bindings != null
                ? bindings
                : Array.Empty<ControlBinding>();
        }

        private static ControlProfileBindings FindProfile(List<ControlProfileBindings> profiles, ControlProfileId profile)
        {
            return profiles?.FirstOrDefault(item => item.profile == profile);
        }

        private static ControlProfileBindings GetOrCreateProfile(List<ControlProfileBindings> profiles, ControlProfileId profile)
        {
            var existing = FindProfile(profiles, profile);
            if (existing != null)
                return existing;

            var created = new ControlProfileBindings { profile = profile };
            profiles.Add(created);
            return created;
        }

        private static ControlActionBindings GetOrCreateActionBindings(List<ControlProfileBindings> profiles, ControlProfileId profile, ControlActionId action)
        {
            return GetOrCreateActionBindings(GetOrCreateProfile(profiles, profile), action);
        }

        private static ControlActionBindings GetOrCreateActionBindings(ControlProfileBindings profile, ControlActionId action)
        {
            var existing = profile.actions.FirstOrDefault(item => item.action == action);
            if (existing != null)
                return existing;

            var created = new ControlActionBindings { action = action };
            profile.actions.Add(created);
            return created;
        }

        private static List<ControlProfileBindings> CloneProfiles(List<ControlProfileBindings> source)
        {
            var clones = new List<ControlProfileBindings>(source.Count);
            foreach (var profile in source)
            {
                if (profile != null)
                    clones.Add(profile.Clone());
            }

            return clones;
        }

        private static Gamepad GetGamepad(int gamepadIndex)
        {
            if (gamepadIndex < 0 || Gamepad.all.Count <= gamepadIndex)
                return null;

            return Gamepad.all[gamepadIndex];
        }

        private bool EvaluateBindings(IEnumerable<ControlBinding> bindings, int gamepadIndex, InputPhase phase)
        {
            var gamepad = GetGamepad(gamepadIndex);
            foreach (var binding in bindings)
            {
                if (binding == null)
                    continue;

                if (EvaluateBinding(binding, gamepad, phase))
                    return true;
            }

            return false;
        }

        private static bool EvaluateBinding(ControlBinding binding, Gamepad gamepad, InputPhase phase)
        {
            switch (binding.kind)
            {
                case ControlBindingKind.Key:
                    return phase switch
                    {
                        InputPhase.Held => UnityEngine.Input.GetKey(binding.keyCode),
                        InputPhase.Pressed => UnityEngine.Input.GetKeyDown(binding.keyCode),
                        InputPhase.Released => UnityEngine.Input.GetKeyUp(binding.keyCode),
                        _ => false
                    };

                case ControlBindingKind.MouseLeft:
                    return phase switch
                    {
                        InputPhase.Held => UnityEngine.Input.GetMouseButton(0),
                        InputPhase.Pressed => UnityEngine.Input.GetMouseButtonDown(0),
                        InputPhase.Released => UnityEngine.Input.GetMouseButtonUp(0),
                        _ => false
                    };

                case ControlBindingKind.MouseRight:
                    return phase switch
                    {
                        InputPhase.Held => UnityEngine.Input.GetMouseButton(1),
                        InputPhase.Pressed => UnityEngine.Input.GetMouseButtonDown(1),
                        InputPhase.Released => UnityEngine.Input.GetMouseButtonUp(1),
                        _ => false
                    };

                case ControlBindingKind.GamepadSouth:
                    return EvaluateButton(gamepad?.buttonSouth, phase);
                case ControlBindingKind.GamepadEast:
                    return EvaluateButton(gamepad?.buttonEast, phase);
                case ControlBindingKind.GamepadWest:
                    return EvaluateButton(gamepad?.buttonWest, phase);
                case ControlBindingKind.GamepadNorth:
                    return EvaluateButton(gamepad?.buttonNorth, phase);
                case ControlBindingKind.GamepadLeftShoulder:
                    return EvaluateButton(gamepad?.leftShoulder, phase);
                case ControlBindingKind.GamepadRightShoulder:
                    return EvaluateButton(gamepad?.rightShoulder, phase);
                case ControlBindingKind.GamepadLeftTrigger:
                    return EvaluateButton(gamepad?.leftTrigger, phase);
                case ControlBindingKind.GamepadRightTrigger:
                    return EvaluateButton(gamepad?.rightTrigger, phase);
                case ControlBindingKind.GamepadStart:
                    return EvaluateButton(gamepad?.startButton, phase);
                case ControlBindingKind.GamepadSelect:
                    return EvaluateButton(gamepad?.selectButton, phase);
                case ControlBindingKind.GamepadDpadLeft:
                    return EvaluateButton(gamepad?.dpad.left, phase);
                case ControlBindingKind.GamepadDpadRight:
                    return EvaluateButton(gamepad?.dpad.right, phase);
                case ControlBindingKind.GamepadDpadUp:
                    return EvaluateButton(gamepad?.dpad.up, phase);
                case ControlBindingKind.GamepadDpadDown:
                    return EvaluateButton(gamepad?.dpad.down, phase);
                case ControlBindingKind.GamepadLeftStickLeft:
                    return EvaluateStickDirection(gamepad, phase, gamepad != null ? gamepad.leftStick.left : null, gamepad != null && gamepad.leftStick.x.ReadValue() <= -StickThreshold);
                case ControlBindingKind.GamepadLeftStickRight:
                    return EvaluateStickDirection(gamepad, phase, gamepad != null ? gamepad.leftStick.right : null, gamepad != null && gamepad.leftStick.x.ReadValue() >= StickThreshold);
                case ControlBindingKind.GamepadLeftStickUp:
                    return EvaluateStickDirection(gamepad, phase, gamepad != null ? gamepad.leftStick.up : null, gamepad != null && gamepad.leftStick.y.ReadValue() >= StickThreshold);
                case ControlBindingKind.GamepadLeftStickDown:
                    return EvaluateStickDirection(gamepad, phase, gamepad != null ? gamepad.leftStick.down : null, gamepad != null && gamepad.leftStick.y.ReadValue() <= -StickThreshold);
                default:
                    return false;
            }
        }

        private static bool EvaluateButton(ButtonControl button, InputPhase phase)
        {
            if (button == null)
                return false;

            return phase switch
            {
                InputPhase.Held => button.isPressed,
                InputPhase.Pressed => button.wasPressedThisFrame,
                InputPhase.Released => button.wasReleasedThisFrame,
                _ => false
            };
        }

        private static bool EvaluateStickDirection(Gamepad gamepad, InputPhase phase, ButtonControl directionButton, bool held)
        {
            if (gamepad == null)
                return false;

            return phase switch
            {
                InputPhase.Held => held,
                InputPhase.Pressed => directionButton != null ? directionButton.wasPressedThisFrame : held,
                InputPhase.Released => directionButton != null && directionButton.wasReleasedThisFrame,
                _ => false
            };
        }

        private static string GetKeyLabel(KeyCode keyCode)
        {
            return keyCode switch
            {
                KeyCode.None => "Unbound",
                KeyCode.LeftArrow => "Left Arrow",
                KeyCode.RightArrow => "Right Arrow",
                KeyCode.UpArrow => "Up Arrow",
                KeyCode.DownArrow => "Down Arrow",
                KeyCode.LeftShift => "Left Shift",
                KeyCode.RightShift => "Right Shift",
                KeyCode.LeftControl => "Left Ctrl",
                KeyCode.RightControl => "Right Ctrl",
                KeyCode.Return => "Enter",
                KeyCode.Keypad0 => "Numpad 0",
                KeyCode.Keypad1 => "Numpad 1",
                KeyCode.Keypad2 => "Numpad 2",
                KeyCode.Keypad3 => "Numpad 3",
                KeyCode.Keypad4 => "Numpad 4",
                KeyCode.Keypad5 => "Numpad 5",
                KeyCode.Keypad6 => "Numpad 6",
                KeyCode.Keypad7 => "Numpad 7",
                KeyCode.Keypad8 => "Numpad 8",
                KeyCode.Keypad9 => "Numpad 9",
                KeyCode.Delete => "Delete",
                _ => keyCode.ToString()
            };
        }

        private enum InputPhase
        {
            Held,
            Pressed,
            Released
        }
    }

    public static class ControlBindingProfileResolver
    {
        public static ControlProfileId ResolveGameplayKeyboardProfile(int playerNumber, bool dualKeyboardMode)
        {
            if (!dualKeyboardMode)
                return ControlProfileId.Keyboard1P;

            return playerNumber == 2
                ? ControlProfileId.Keyboard2PPlayer2
                : ControlProfileId.Keyboard2PPlayer1;
        }

        public static ControlProfileId ResolveGlobalKeyboardProfile(int playerNumber, bool dualKeyboardMode)
        {
            if (!dualKeyboardMode)
                return ControlProfileId.GlobalKeyboardPlayer1;

            return playerNumber == 2
                ? ControlProfileId.GlobalKeyboardPlayer2
                : ControlProfileId.GlobalKeyboardPlayer1;
        }
    }
}
