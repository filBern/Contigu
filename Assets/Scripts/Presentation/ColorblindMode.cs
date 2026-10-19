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

        public static bool IsEnabled { get; private set; } = GamePrefs.GetInt(PrefsKey, 0) != 0;

        /// <summary>Back to the default (off) without persisting — used by SettingsView's Reset data after GamePrefs.DeleteAll, since this static survives a scene reload.</summary>
        public static void ResetToDefault()
        {
            IsEnabled = false;
        }

        public static void Toggle()
        {
            IsEnabled = !IsEnabled;
            GamePrefs.SetInt(PrefsKey, IsEnabled ? 1 : 0);
            GamePrefs.Save();
            Changed?.Invoke();
        }
    }
}
