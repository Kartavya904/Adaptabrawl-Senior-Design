using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Adaptabrawl.UI
{
    /// <summary>
    /// Builds explicit up/down navigation between UI selectables so gamepads and keyboards
    /// can move focus predictably (top-to-bottom lists, wrap optional). Assign in the inspector
    /// or call <see cref="ApplyVerticalChain"/> from code.
    /// </summary>
    [DisallowMultipleComponent]
    public class MenuNavigationGroup : MonoBehaviour
    {
        [Tooltip("Top-to-bottom (or desired) order. Null entries are skipped.")]
        [SerializeField] private List<Selectable> orderedSelectables = new List<Selectable>();

        [SerializeField] private bool applyOnEnable = true;

        [SerializeField] private bool selectFirstOnEnable = true;

        [SerializeField] private bool wrapVertical = true;

        private void OnEnable()
        {
            if (applyOnEnable)
                ApplyVerticalChain(orderedSelectables, wrapVertical);

            if (selectFirstOnEnable)
                SelectFirstAvailable(orderedSelectables);
        }

        /// <summary>
        /// Wire vertical neighbors in list order (index 0 = top). Skips null entries.
        /// </summary>
        public static void ApplyVerticalChain(IList<Selectable> order, bool wrap)
        {
            if (order == null || order.Count == 0) return;

            var chain = new List<Selectable>();
            for (int i = 0; i < order.Count; i++)
            {
                if (order[i] != null) chain.Add(order[i]);
            }

            int n = chain.Count;
            if (n == 0) return;

            for (int i = 0; i < n; i++)
            {
                Selectable up = i > 0 ? chain[i - 1] : (wrap ? chain[n - 1] : null);
                Selectable down = i < n - 1 ? chain[i + 1] : (wrap ? chain[0] : null);

                var nav = chain[i].navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnUp = up;
                nav.selectOnDown = down;
                chain[i].navigation = nav;
            }
        }

        /// <summary>Left-to-right neighbor links (e.g. prev/next arrows on one row).</summary>
        public static void ApplyHorizontalChain(IList<Selectable> order, bool wrap)
        {
            if (order == null || order.Count == 0) return;

            var chain = new List<Selectable>();
            for (int i = 0; i < order.Count; i++)
            {
                if (order[i] != null) chain.Add(order[i]);
            }

            int n = chain.Count;
            if (n == 0) return;

            for (int i = 0; i < n; i++)
            {
                Selectable left = i > 0 ? chain[i - 1] : (wrap ? chain[n - 1] : null);
                Selectable right = i < n - 1 ? chain[i + 1] : (wrap ? chain[0] : null);

                var nav = chain[i].navigation;
                nav.mode = Navigation.Mode.Explicit;
                nav.selectOnLeft = left;
                nav.selectOnRight = right;
                chain[i].navigation = nav;
            }
        }

        public static void SelectFirstAvailable(IList<Selectable> order)
        {
            MenuNavigationFocusGate.RequestSelection(order);
        }

        public static bool IsControllerFocusVisible(GameObject target)
        {
            return MenuNavigationFocusGate.IsControllerFocusVisible(target);
        }

        public static void NotifyPointerHover(Selectable hoveredSelectable)
        {
            MenuNavigationFocusGate.NotifyPointerHover(hoveredSelectable);
        }

        public void Refresh()
        {
            ApplyVerticalChain(orderedSelectables, wrapVertical);
        }
    }

    internal enum MenuInputMode
    {
        None,
        Pointer,
        Controller
    }

    internal sealed class MenuNavigationFocusGate : MonoBehaviour
    {
        private static MenuNavigationFocusGate instance;
        private readonly List<Selectable> pendingSelection = new List<Selectable>();

        private MenuInputMode inputMode = MenuInputMode.None;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            var host = new GameObject("MenuNavigationFocusGate");
            instance = host.AddComponent<MenuNavigationFocusGate>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            ClearSelection();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Update()
        {
            if (DetectPointerActivity())
            {
                SetInputMode(MenuInputMode.Pointer);
                return;
            }

            if (DetectControllerStyleActivity())
            {
                SetInputMode(MenuInputMode.Controller);
            }
        }

        private void OnSceneLoaded(Scene _, LoadSceneMode __)
        {
            inputMode = MenuInputMode.None;
            ClearSelection();
        }

        public static void RequestSelection(IList<Selectable> order)
        {
            EnsureInstance();
            instance.StorePending(order);

            if (instance.inputMode == MenuInputMode.Controller)
            {
                instance.SelectPendingFirstAvailable();
            }
            else
            {
                ClearSelection();
            }
        }

        public static bool IsControllerFocusVisible(GameObject target)
        {
            return instance != null
                   && instance.inputMode == MenuInputMode.Controller
                   && EventSystem.current != null
                   && EventSystem.current.currentSelectedGameObject == target;
        }

        public static void NotifyPointerHover(Selectable hoveredSelectable)
        {
            EnsureInstance();
            instance.SetInputMode(MenuInputMode.Pointer);

            if (hoveredSelectable == null || EventSystem.current == null)
                return;

            if (EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private static void EnsureInstance()
        {
            if (instance == null)
                Bootstrap();
        }

        private void StorePending(IList<Selectable> order)
        {
            pendingSelection.Clear();
            if (order == null)
                return;

            for (int i = 0; i < order.Count; i++)
            {
                if (order[i] != null)
                    pendingSelection.Add(order[i]);
            }
        }

        private void SetInputMode(MenuInputMode newMode)
        {
            if (inputMode == newMode)
                return;

            inputMode = newMode;

            if (inputMode == MenuInputMode.Controller)
            {
                SelectPendingFirstAvailable();
            }
            else
            {
                ClearSelection();
            }
        }

        private void SelectPendingFirstAvailable()
        {
            var es = EventSystem.current;
            if (es == null)
                return;

            if (es.currentSelectedGameObject != null
                && es.currentSelectedGameObject.activeInHierarchy)
            {
                return;
            }

            for (int i = 0; i < pendingSelection.Count; i++)
            {
                Selectable selectable = pendingSelection[i];
                if (selectable != null
                    && selectable.IsInteractable()
                    && selectable.gameObject.activeInHierarchy)
                {
                    es.SetSelectedGameObject(selectable.gameObject);
                    return;
                }
            }
        }

        private static void ClearSelection()
        {
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private static bool DetectPointerActivity()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return false;

            return mouse.delta.ReadValue().sqrMagnitude > 0.01f
                   || mouse.leftButton.wasPressedThisFrame
                   || mouse.rightButton.wasPressedThisFrame
                   || mouse.middleButton.wasPressedThisFrame;
        }

        private static bool DetectControllerStyleActivity()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.wasPressedThisFrame
                    || keyboard.downArrowKey.wasPressedThisFrame
                    || keyboard.leftArrowKey.wasPressedThisFrame
                    || keyboard.rightArrowKey.wasPressedThisFrame
                    || keyboard.tabKey.wasPressedThisFrame
                    || keyboard.enterKey.wasPressedThisFrame
                    || keyboard.numpadEnterKey.wasPressedThisFrame
                    || keyboard.spaceKey.wasPressedThisFrame
                    || keyboard.wKey.wasPressedThisFrame
                    || keyboard.aKey.wasPressedThisFrame
                    || keyboard.sKey.wasPressedThisFrame
                    || keyboard.dKey.wasPressedThisFrame)
                {
                    return true;
                }
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
                return false;

            return gamepad.dpad.up.wasPressedThisFrame
                   || gamepad.dpad.down.wasPressedThisFrame
                   || gamepad.dpad.left.wasPressedThisFrame
                   || gamepad.dpad.right.wasPressedThisFrame
                   || gamepad.buttonSouth.wasPressedThisFrame
                   || gamepad.buttonNorth.wasPressedThisFrame
                   || gamepad.buttonWest.wasPressedThisFrame
                   || gamepad.buttonEast.wasPressedThisFrame
                   || gamepad.startButton.wasPressedThisFrame
                   || gamepad.selectButton.wasPressedThisFrame
                   || gamepad.leftShoulder.wasPressedThisFrame
                   || gamepad.rightShoulder.wasPressedThisFrame;
        }
    }
}
