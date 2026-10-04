using System;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Player-toggleable accessibility setting (spec extension, explicit
    /// request: "accessibilité daltonisme" -> "mode daltonien activable,
    /// off par défaut" — deliberately opt-in rather than always-on, since
    /// a permanent per-color icon on every tile was already tried once and
    /// explicitly removed for looking cluttered, see VisualDefaults.
    /// TileSprite's own doc comment). When on, every rendered piece color
    /// (grid cells via GridCellView, every piece preview via
    /// ShapePreviewFactory — hand, shop, draft, deck view, per-color
    /// modifier badges) additionally shows one of ColorblindShapeFactory's
    /// small black geometric shapes, so color alone is never the only way
    /// to tell two piece colors apart.
    ///
    /// A single bool doesn't need the MetaStats JSON file's structure, so
    /// it's persisted directly via PlayerPrefs rather than folded into
    /// IMetaStatsStore.
    /// </summary>
    public static class ColorblindMode
    {
        private const string PrefsKey = "ColorblindMode";

        /// <summary>Fired whenever <see cref="Toggle"/> changes the setting — every view that renders a piece color subscribes so a toggle takes effect immediately instead of only on the next unrelated refresh.</summary>
        public static event Action Changed;

        public static bool IsEnabled { get; private set; } = PlayerPrefs.GetInt(PrefsKey, 0) != 0;

        public static void Toggle()
        {
            IsEnabled = !IsEnabled;
            PlayerPrefs.SetInt(PrefsKey, IsEnabled ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
