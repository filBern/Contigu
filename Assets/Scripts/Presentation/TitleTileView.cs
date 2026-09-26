using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// One tile of the main menu's "CONTIGU" title (see MainMenuView) —
    /// re-tints itself to one of the OTHER 3 board colors on hover
    /// (explicit request: "lorsqu'on hover sur une tuile, on change sa
    /// couleur de manière random (une des trois autres couleurs)"). Joker
    /// is excluded from the pool here — it's a deck wildcard, not a
    /// paintable tile color.
    /// </summary>
    public sealed class TitleTileView : MonoBehaviour, IPointerEnterHandler
    {
        private static readonly PieceColor[] Colors =
        {
            PieceColor.Coral, PieceColor.Teal, PieceColor.Violet, PieceColor.Lime
        };

        private Image _image;
        private PieceColor _color;

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
        }

        public static PieceColor RandomColor()
        {
            return Colors[Random.Range(0, Colors.Length)];
        }
    }
}
