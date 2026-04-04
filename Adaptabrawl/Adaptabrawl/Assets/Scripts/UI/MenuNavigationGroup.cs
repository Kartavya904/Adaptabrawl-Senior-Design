using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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
            if (order == null) return;
            var es = EventSystem.current;
            if (es == null) return;

            for (int i = 0; i < order.Count; i++)
            {
                var s = order[i];
                if (s != null && s.IsInteractable() && s.gameObject.activeInHierarchy)
                {
                    es.SetSelectedGameObject(s.gameObject);
                    return;
                }
            }
        }

        public void Refresh()
        {
            ApplyVerticalChain(orderedSelectables, wrapVertical);
        }
    }
}
