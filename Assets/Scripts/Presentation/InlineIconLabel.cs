using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// A Text whose word "Pollen" is shown as the Pollen icon: legacy uGUI Text cannot draw an inline image, so the word is replaced by a run of blanks and
    /// the icon is placed over it, where the text generator says the blanks ended up (after the layout of the frame). If the generator does not give back
    /// what was expected, the plain word comes back instead: the text is never left with a hole.
    /// </summary>
    public sealed class InlineIconLabel : MonoBehaviour
    {
        private const string Blanks = "    "; // non-breaking: the run never splits across lines
        private static readonly Regex PollenWord = new Regex("<color=#[0-9A-Fa-f]{6}>Pollen</color>|\\bPollen\\b", RegexOptions.IgnoreCase);
        private static readonly Regex Tags = new Regex("<[^>]+>");

        private Text _text;
        private string _withWord;
        private readonly List<int> _starts = new List<int>();
        private readonly List<Image> _icons = new List<Image>();
        private Sprite _sprite;
        private Coroutine _place;

        /// <summary>Shows <paramref name="richText"/> in <paramref name="label"/> with "Pollen" replaced by the icon (the word itself when there is no icon art).</summary>
        public static void Set(Text label, string richText, Sprite pollenIcon)
        {
            var host = label.GetComponent<InlineIconLabel>();
            if (host == null)
            {
                host = label.gameObject.AddComponent<InlineIconLabel>();
            }
            host.Apply(label, richText, pollenIcon);
        }

        private void Apply(Text label, string richText, Sprite sprite)
        {
            _text = label;
            _sprite = sprite;
            _withWord = richText ?? string.Empty;
            _starts.Clear();
            for (int i = 0; i < _icons.Count; i++)
            {
                _icons[i].gameObject.SetActive(false);
            }
            if (sprite == null || !PollenWord.IsMatch(_withWord))
            {
                _text.text = _withWord;
                return;
            }
            // Replace each occurrence, remembering where the blanks start among the characters that are really drawn (rich-text tags excluded).
            var sb = new System.Text.StringBuilder();
            int last = 0;
            foreach (Match m in PollenWord.Matches(_withWord))
            {
                sb.Append(_withWord, last, m.Index - last);
                _starts.Add(Tags.Replace(sb.ToString(), string.Empty).Length);
                sb.Append(Blanks);
                last = m.Index + m.Length;
            }
            sb.Append(_withWord, last, _withWord.Length - last);
            _text.text = sb.ToString();
            Schedule();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_text != null && _starts.Count > 0 && isActiveAndEnabled)
            {
                Schedule();
            }
        }

        private void OnEnable()
        {
            if (_text != null && _starts.Count > 0)
            {
                Schedule();
            }
        }

        private void Schedule()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }
            if (_place != null)
            {
                StopCoroutine(_place);
            }
            _place = StartCoroutine(PlaceAfterLayout());
        }

        private IEnumerator PlaceAfterLayout()
        {
            yield return new WaitForEndOfFrame(); // the canvas has rebuilt the text by now
            _place = null;
            if (_text == null || _starts.Count == 0)
            {
                yield break;
            }
            var gen = _text.cachedTextGenerator;
            var chars = gen.characters;
            var lines = gen.lines;
            for (int n = 0; n < _starts.Count; n++)
            {
                if (_starts[n] + Blanks.Length >= chars.Count || lines.Count == 0)
                {
                    GiveBackTheWord();
                    yield break;
                }
            }
            var rect = _text.rectTransform;
            // The text generator lays glyphs out around the rectangle's own pivot (Text sets that as its generation pivot), so its coordinates are local ones once scaled.
            float unit = 1f / _text.pixelsPerUnit;
            for (int n = 0; n < _starts.Count; n++)
            {
                int start = _starts[n];
                float width = 0f;
                for (int k = 0; k < Blanks.Length; k++)
                {
                    width += chars[start + k].charWidth;
                }
                int line = 0;
                for (int l = 0; l < lines.Count; l++)
                {
                    if (lines[l].startCharIdx <= start) { line = l; }
                }
                float left = chars[start].cursorPos.x;
                float centerX = (left + width * 0.5f) * unit;
                float centerY = (lines[line].topY - lines[line].height * 0.5f) * unit;
                float size = lines[line].height * unit * 0.9f;
                var icon = IconAt(n);
                icon.gameObject.SetActive(true);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = rect.pivot;
                icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                icon.rectTransform.anchoredPosition = new Vector2(centerX, centerY);
                icon.rectTransform.sizeDelta = new Vector2(size, size);
            }
        }

        private void GiveBackTheWord()
        {
            _starts.Clear();
            for (int i = 0; i < _icons.Count; i++)
            {
                _icons[i].gameObject.SetActive(false);
            }
            _text.text = _withWord;
        }

        private Image IconAt(int index)
        {
            while (_icons.Count <= index)
            {
                var icon = UIFactory.CreatePanel(transform, "PollenIcon" + _icons.Count, Color.white);
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                icon.sprite = _sprite;
                _icons.Add(icon);
            }
            _icons[index].sprite = _sprite;
            return _icons[index];
        }
    }
}
