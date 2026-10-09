using System.Collections;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// One tile of the main menu's "CONTIGU" title (see MainMenuView) — re-tints itself to one of the other
    /// 3 board colors on hover, plus a brief pulse (same constants/coroutine shape as GridCellView.Pulse).
    /// Joker is excluded from the color pool — it's a deck wildcard, not a paintable tile color.
    /// </summary>
    public sealed class TitleTileView : MonoBehaviour, IPointerEnterHandler
    {
        private const float PulseDuration = 0.28f;
        private const float PulsePeakScale = 1.18f;
        private const float PulsePeakFraction = 0.4f;

        private static readonly PieceColor[] Colors =
        {
            PieceColor.Coral, PieceColor.Teal, PieceColor.Violet, PieceColor.Lime
        };

        private Image _image;
        private PieceColor _color;
        private Coroutine _pulseCoroutine;

        public void Init(Image image, PieceColor initialColor)
        {
            _image = image;
            _color = initialColor;
            _image.color = VisualDefaults.GetColor(_color);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PieceColor next;
            do
            {
                next = RandomColor();
            } while (next == _color);
            _color = next;
            _image.color = VisualDefaults.GetColor(_color);

            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
            }
            _pulseCoroutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            var rt = (RectTransform)transform;
            float t = 0f;
            while (t < PulseDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / PulseDuration);
                float scale = p < PulsePeakFraction
                    ? Mathf.Lerp(1f, PulsePeakScale, p / PulsePeakFraction)
                    : Mathf.Lerp(PulsePeakScale, 1f, (p - PulsePeakFraction) / (1f - PulsePeakFraction));
                rt.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            rt.localScale = Vector3.one;
            _pulseCoroutine = null;
        }

        public static PieceColor RandomColor()
        {
            return Colors[Random.Range(0, Colors.Length)];
        }
    }
}
