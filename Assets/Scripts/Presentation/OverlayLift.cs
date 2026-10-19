using System.Collections.Generic;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Added to every shop choice overlay (seed pickers, mastery, modifier upgrade, draft). While the overlay is open it
    /// lifts the registered panels (fertilizers, next-round enemies, Pollen counter) above its dimmed backdrop, so they
    /// stay readable; closing the overlay puts them back where they were.
    /// </summary>
    public sealed class OverlayLift : MonoBehaviour
    {
        /// <summary>Panels to keep readable above any overlay; set once by GameBootstrap after the views are built.</summary>
        public static readonly List<RectTransform> Panels = new List<RectTransform>();

        /// <summary>Hides (or shows again) every registered panel, lifted or not — used by full-screen overlays such as the seed bag that must show nothing else.</summary>
        public static void SetPanelsHidden(bool hidden)
        {
            for (int i = 0; i < Panels.Count; i++)
            {
                var panel = Panels[i];
                if (panel == null)
                {
                    continue;
                }
                var group = panel.GetComponent<CanvasGroup>();
                if (group == null)
                {
                    if (!hidden)
                    {
                        continue;
                    }
                    group = panel.gameObject.AddComponent<CanvasGroup>();
                }
                group.alpha = hidden ? 0f : 1f;
                group.blocksRaycasts = !hidden;
            }
        }

        private readonly List<RectTransform> _lifted = new List<RectTransform>();
        private readonly List<Transform> _parents = new List<Transform>();
        private readonly List<int> _indices = new List<int>();

        private void OnEnable()
        {
            _lifted.Clear();
            _parents.Clear();
            _indices.Clear();
            for (int i = 0; i < Panels.Count; i++)
            {
                var panel = Panels[i];
                if (panel == null || !panel.gameObject.activeInHierarchy)
                {
                    continue;
                }
                _lifted.Add(panel);
                _parents.Add(panel.parent);
                _indices.Add(panel.GetSiblingIndex());
                panel.SetParent(transform.parent, false);
                panel.SetAsLastSibling();
            }
        }

        private void LateUpdate()
        {
            // The owning view may move its overlay to the front after enabling it; keep the lifted panels above.
            int overlayIndex = transform.GetSiblingIndex();
            for (int i = 0; i < _lifted.Count; i++)
            {
                if (_lifted[i] != null && _lifted[i].GetSiblingIndex() < overlayIndex)
                {
                    _lifted[i].SetAsLastSibling();
                }
            }
        }

        private void OnDisable()
        {
            for (int i = _lifted.Count - 1; i >= 0; i--)
            {
                if (_lifted[i] == null || _parents[i] == null)
                {
                    continue;
                }
                _lifted[i].SetParent(_parents[i], false);
                _lifted[i].SetSiblingIndex(_indices[i]);
            }
            _lifted.Clear();
            _parents.Clear();
            _indices.Clear();
        }
    }
}
