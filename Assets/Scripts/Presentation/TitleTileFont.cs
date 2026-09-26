using System.Collections.Generic;

namespace Contigu.Presentation
{
    /// <summary>
    /// Simple 5-wide x 7-tall dot-matrix glyphs for the letters spelling
    /// "CONTIGU" — lets MainMenuView spell the game's own name out of the
    /// same colored square tiles gameplay itself is built from (spec
    /// extension, explicit request: "on va écrire le nom du jeu Contigu...
    /// écrit avec des tuiles"). Only the 7 distinct letters the title
    /// actually needs are defined; add more entries here if a future
    /// screen needs other letters.
    /// </summary>
    public static class TitleTileFont
    {
        public const int GlyphWidth = 5;
        public const int GlyphHeight = 7;

        // Each string is one row, top to bottom; '#' = filled tile, '.' = empty.
        private static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['C'] = new[]
            {
                ".###.",
                "#....",
                "#....",
                "#....",
                "#....",
                "#....",
                ".###.",
            },
            ['O'] = new[]
            {
                ".###.",
                "#...#",
                "#...#",
                "#...#",
                "#...#",
                "#...#",
                ".###.",
            },
            ['N'] = new[]
            {
                "#...#",
                "##..#",
                "#.#.#",
                "#.#.#",
                "#..##",
                "#...#",
                "#...#",
            },
            ['T'] = new[]
            {
                "#####",
                "..#..",
                "..#..",
                "..#..",
                "..#..",
                "..#..",
                "..#..",
            },
            ['I'] = new[]
            {
                "#####",
                "..#..",
                "..#..",
                "..#..",
                "..#..",
                "..#..",
                "#####",
            },
            ['G'] = new[]
            {
                ".###.",
                "#....",
                "#....",
                "#.###",
                "#...#",
                "#...#",
                ".###.",
            },
            ['U'] = new[]
            {
                "#...#",
                "#...#",
                "#...#",
                "#...#",
                "#...#",
                "#...#",
                ".###.",
            },
        };

        /// <summary>True if <paramref name="letter"/>'s glyph has a tile at (<paramref name="col"/>, <paramref name="row"/>) — false for an unknown letter or an out-of-range cell.</summary>
        public static bool IsCellFilled(char letter, int col, int row)
        {
            if (!Glyphs.TryGetValue(letter, out var rows) || row < 0 || row >= rows.Length)
            {
                return false;
            }
            var line = rows[row];
            return col >= 0 && col < line.Length && line[col] == '#';
        }
    }
}
