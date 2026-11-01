using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>
    /// Every layout number, color and timing that gets adjusted by eye, gathered in one asset (Assets/Resources/UITweaks.asset) so it can be
    /// changed in the Inspector without touching the code. The views read it when they build their screen: change a value, then reload the
    /// scene (the demo scenes do it for you, see <see cref="SandboxScenario"/>). The defaults are the values the game ships with.
    /// </summary>
    [CreateAssetMenu(fileName = "UITweaks", menuName = "LASTBLOOM/UI Tweaks")]
    public sealed class UITweaks : ScriptableObject
    {
        [Header("Boutique : étagères")]
        [Tooltip("Taille maximale d'un article sur l'étagère (unités du canvas).")] [Range(60f, 160f)] public float shopItemMaxSize = 100f;
        [Tooltip("Taille minimale d'un article quand la place manque.")] [Range(40f, 120f)] public float shopItemMinSize = 64f;
        [Tooltip("Espace voulu entre deux articles.")] [Range(0f, 60f)] public float shopItemPreferredSpacing = 24f;
        public float shopItemMinSpacing = 8f;
        public float shopItemMaxSpacing = 32f;
        [Tooltip("Espace total laissé libre entre une tablette et les deux panneaux latéraux (somme des deux côtés).")] [Range(0f, 120f)] public float shelfEdgeMargin = 40f;
        [Tooltip("Part de sa case que remplit l'icône d'une tile upgrade (1 = toute la case).")] [Range(0.4f, 1f)] public float upgradeGlyphScale = 0.8f;

        [Header("Boutique : étiquette de prix")]
        public Color priceTagColor = new Color(1f, 0.969f, 0.91f, 1f);
        public Color priceTagOutline = new Color(0.075f, 0.129f, 0.18f, 1f);
        [Range(-20f, 20f)] public float priceTagRotation = -4f;
        [Range(14f, 36f)] public int priceTagFontSize = 22;
        [Range(12f, 40f)] public float priceTagIconSize = 24f;
        [Tooltip("Décalage de l'étiquette par rapport au coin haut droit de l'article.")] public Vector2 priceTagOffset = new Vector2(8f, 8f);

        [Header("Écrans de choix (packs, Forge, Compost…)")]
        [Tooltip("De combien l'étagère du haut descend.")] [Range(0f, 120f)] public float upperShelfDrop = 56f;
        [Tooltip("Taille de case pour des graines (rangée à plusieurs).")] [Range(60f, 160f)] public float seedCellSize = 96f;
        [Tooltip("Taille de case pour des icônes (upgrades, fertilizers).")] [Range(60f, 200f)] public float iconCellSize = 120f;
        [Tooltip("Taille de case maximale quand il n'y a qu'une rangée.")] [Range(100f, 260f)] public float singleRowCellSize = 190f;
        [Tooltip("Part de la case que remplit une icône de pack.")] [Range(0.3f, 1f)] public float iconFillScale = 0.64f;
        [Tooltip("Part de la case que remplit une pièce affichée sans tuile fixe.")] [Range(0.5f, 1f)] public float previewScale = 0.9f;
        [Tooltip("Espace entre deux icônes, en part de la taille de case.")] [Range(0f, 0.6f)] public float iconGapPerCell = 0.04f;

        [Header("Plateau")]
        public float screenMargin = 16f;
        public float columnGap = 24f;
        [Tooltip("Débord de la planche de bois autour de la colonne de gauche.")] [Range(0f, 30f)] public float leftColumnFramePad = 10f;
        public Color boardWood = new Color(0.33f, 0.22f, 0.13f, 1f);
        public Color boardDark = new Color(0.13f, 0.09f, 0.06f, 1f);
        [Range(100f, 200f)] public float handSlotSize = 150f;
        [Range(0f, 30f)] public float handSlotSpacing = 12f;

        [Header("Menu principal")]
        [Tooltip("Hauteur du logo (0 = milieu de l'écran).")] public float logoY = 200f;
        public float subtitleY = 22f;
        public float buttonRowY = -226f;
        public float sprigY = -134f;
        [Range(60f, 260f)] public float sprigSize = 130f;
        [Range(2f, 20f)] public float sprigHopEvery = 5.5f;

        [Header("Sélecteur de challenge")]
        [Range(300f, 520f)] public float challengeCardWidth = 380f;
        [Range(380f, 640f)] public float challengeCardHeight = 480f;
        [Range(60f, 140f)] public float challengeBannerHeight = 92f;

        [Header("Animations")]
        [Tooltip("Secondes sans poser de tuile avant que le reflet traverse le plateau.")] [Range(2f, 30f)] public float glintAfterIdle = 8f;
        [Tooltip("Durée du vol d'un fertilizer vers son emplacement.")] [Range(0.2f, 2f)] public float fertilizerFlightSeconds = 0.65f;

        private static UITweaks _current;

        /// <summary>Bumped whenever a value is edited in the Inspector (see <see cref="OnValidate"/>): the demo scenes watch it to rebuild.</summary>
        public static int EditVersion { get; private set; }

        /// <summary>The asset at Resources/UITweaks, or an all-default one when it is missing.</summary>
        public static UITweaks Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<UITweaks>("UITweaks");
                    if (_current == null)
                    {
                        _current = CreateInstance<UITweaks>();
                    }
                }
                return _current;
            }
        }

        private void OnValidate()
        {
            EditVersion++;
        }
    }
}
