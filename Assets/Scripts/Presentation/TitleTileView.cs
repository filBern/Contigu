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
        private const float EntranceDuration = 0.25f;
        private const float EntrancePeakScale = 1.2f;
        private const float EntrancePeakFraction = 0.6f;
        // Alpha reaches 1 well before scale settles (see EntranceDuration) so the tile reads as "arrived"
        // early in its own pop rather than still fading in during the overshoot.
        private const float EntranceFadeFraction = 0.3f;

        private static readonly PieceColor[] Colors =
        {
            PieceColor.Coral, PieceColor.Teal, PieceColor.Violet, PieceColor.Lime
        };

        private Image _image;
        private PieceColor _color;
        private Coroutine _pulseCoroutine;
        private Coroutine _entranceCoroutine;

        public void Init(Image image, PieceColor initialColor)
        {
            _image = image;
            _color = initialColor;
            ApplyColor();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.pointerId >= 0) { return; } // touch has no hover
            RecolorAndPulse();
        }

        private void ApplyColor()
        {
            _image.sprite = VisualDefaults.GetTileSprite(_color);
            _image.color = VisualDefaults.GetTileTint(_color);
        }

        private void RecolorAndPulse()
        {
            PieceColor next;
            do
            {
                next = RandomColor();
            } while (next == _color);
            _color = next;
            ApplyColor();

            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
            }
            _pulseCoroutine = StartCoroutine(PulseRoutine());
        }

        /// <summary>
        /// Pop-in entrance played once, right after Init, instead of every title tile just appearing at
        /// once — starts invisible/collapsed, waits <paramref name="delay"/> seconds (MainMenuView staggers
        /// this per tile left-to-right for a cascade), then scales up with the same overshoot-then-settle
        /// shape as Pulse, fading in over its first <see cref="EntranceFadeFraction"/>.
        /// </summary>
        public void PlayEntrance(float delay)
        {
            // Defensive stop-and-restart (same pattern as Pulse's own _pulseCoroutine), in case Show() ever
            // runs more than once on the same tile.
            if (_entranceCoroutine != null)
            {
                StopCoroutine(_entranceCoroutine);
            }
            var rt = (RectTransform)transform;
            rt.localScale = Vector3.zero;
            var c = _image.color;
            _image.color = new Color(c.r, c.g, c.b, 0f);
            _entranceCoroutine = StartCoroutine(EntranceRoutine(delay));
        }

        private IEnumerator EntranceRoutine(float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            var rt = (RectTransform)transform;
            float t = 0f;
            while (t < EntranceDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / EntranceDuration);
                float scale = p < EntrancePeakFraction
                    ? Mathf.Lerp(0f, EntrancePeakScale, p / EntrancePeakFraction)
                    : Mathf.Lerp(EntrancePeakScale, 1f, (p - EntrancePeakFraction) / (1f - EntrancePeakFraction));
                rt.localScale = new Vector3(scale, scale, 1f);
                var c = _image.color;
                _image.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(p / EntranceFadeFraction));
                yield return null;
            }
            rt.localScale = Vector3.one;
            var finalColor = _image.color;
            _image.color = new Color(finalColor.r, finalColor.g, finalColor.b, 1f);
            _entranceCoroutine = null;
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
