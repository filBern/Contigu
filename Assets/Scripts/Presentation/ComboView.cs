using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Big, prominent "Combo: +N" readout sitting in the gap between the grid
    /// and the hand — shows the running total of whichever placement is
    /// currently animating (see GameBootstrap.PlayPlacementSequence). Kept
    /// separate from <see cref="HudView"/> since it needs to sit at a very
    /// specific screen position, not inside the top bar's horizontal layout.
    /// </summary>
    public sealed class ComboView : MonoBehaviour
    {
        private Text _text;

        public RectTransform Build(Transform parent)
        {
            _text = UIFactory.CreateText(parent, "Combo", "", 30, UITheme.Modifier);
            _text.fontStyle = FontStyle.Bold;
            var outline = _text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);

            var rt = _text.rectTransform;
            rt.gameObject.SetActive(false);
            return rt;
        }

        public void Show(int amount)
        {
            _text.gameObject.SetActive(true);
            _text.text = "Combo: +" + amount;
        }

        public void Hide()
        {
            _text.gameObject.SetActive(false);
        }
    }
}
