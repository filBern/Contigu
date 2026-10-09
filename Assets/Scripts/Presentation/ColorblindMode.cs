using System;
using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Player-toggleable accessibility setting, off by default. When on, every rendered piece color (grid
    /// cells via GridCellView, every piece preview via ShapePreviewFactory) additionally shows one of
    /// ColorblindShapeFactory's small geometric shapes, so color alone is never the only way to distinguish
    /// piece colors. Persisted directly via PlayerPrefs rather than folded into IMetaStatsStore.
    /// </summary>
    public static class ColorblindMode
    {
        private const string PrefsKey = "ColorblindMode";

        /// <summary>Fired whenever <see cref="Toggle"/> changes the setting, so subscribed views can refresh immediately.</summary>
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
