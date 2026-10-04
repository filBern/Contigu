using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Two progress bars, flush against the top and bottom edges of the
    /// screen and spanning its full width (no margin), built from flat
    /// geometric rectangles in UITheme colors (see BuildBar) — the top
    /// bar tracks round score against the round's quota, the bottom bar
    /// tracks remaining piece budget for the round. Replaces the old
    /// text-only readout (round number, round score, total score) — the
    /// run's overall progress isn't shown moment-to-moment, just what the
    /// player needs to finish the current round.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private const float BarHeight = 68f;
        private RectTransform _scoreFillRect;
        private Text _scoreLabel;
        private RectTransform _piecesFillRect;
        private Text _piecesLabel;
        private RectTransform _lueurContainer;
        private Text _lueurLabel;
        // Folded into the score bar's own label (see SetRound/SetScores)
        // rather than a separate readout — on explicit request: "il faut
        // mettre a quelle round on est rendu sur le nombre total a
        // réussir". Defaults match round 1 of an 8-round run so the very
        // first SetScores call (before Refresh's own SetRound runs) never
        // shows a stale 0/0.
        private int _roundNumber = 1;
        private int _roundCount = RunConfig.RoundCount;
        private bool _isEndless;

        /// <summary>
        /// True while the score bar is hidden in favor of the enemy icon
        /// row (see SetEncounter) — set by SetEncounter, cleared by Refresh
        /// whenever RunManager.HasActiveEncounter is false. While true,
        /// SetScores becomes a no-op (spec extension, explicit request:
        /// "ajouter un petit peu d'autobattling") so GameBootstrap's own
        /// progressive-score-popup calls (written for the quota bar, see
        /// PlayPlacementSequence) can't re-show a bar that no longer means
        /// anything this round.
        /// </summary>
        private bool _isEncounterMode;

        /// <summary>The ScoreBar's own root (its track Image's GameObject) — hidden entirely during an encounter round rather than repurposed, on explicit request: "Enleve la progress bar pour le quota. Au lieu met une petite image en haut pour chaque ennemi". Captured by climbing from _scoreFillRect (its own parent) rather than widening BuildBar's signature just for this.</summary>
        private GameObject _scoreBarRoot;

        private const int MaxEnemyIcons = 3;
        private const float EnemyIconSize = 44f;
        private const float EnemySlotWidth = 76f;
        // Gap above the icon within its slot, pushing it down from the very
        // top of the band — explicit request: "l'ennemi est trop haut".
        private const float EnemyIconTopPadding = 14f;
        // Taller than the plain score bar it replaces (was just BarHeight)
        // to fit EnemyIconTopPadding above the icon without crowding its HP
        // label below — GameBootstrap's status text position derives from
        // this directly (see BuildUI) so the two can never drift apart.
        public const float EnemyBandHeight = BarHeight + EnemyIconTopPadding;
        private GameObject _enemyBandRoot;
        private readonly List<GameObject> _enemySlots = new List<GameObject>();
        private readonly List<Image> _enemyIconImages = new List<Image>();
        private readonly List<Text> _enemyIconLabels = new List<Text>();
        private readonly List<EnemyIconView> _enemyIconViews = new List<EnemyIconView>();
        private TooltipView _tooltip;

        public void Build(Transform parent, Transform lueurParent, TooltipView tooltip)
        {
            _tooltip = tooltip;
            BuildBar(parent, "ScoreBar", UITheme.ButtonSelected, top: true, out _scoreFillRect, out _scoreLabel);
            _scoreBarRoot = _scoreFillRect.parent.gameObject;
            BuildBar(parent, "PiecesBar", UITheme.Success, top: false, out _piecesFillRect, out _piecesLabel);
            BuildEnemyBand(parent);

            // Persistent readout displayed outside and above the combo card
            // (moved here from below the status text on explicit request —
            // was a small top-right corner readout, then 18->22->44->66->99;
            // now slightly reduced to 84 after the layout review) — Lueur is a
            // whole-run currency (see
            // RunManager.Lueur), not tied to either bar's own round-scoped
            // progress, so it gets its own spot rather than folding into the
            // score bar's label. The "Lueur: " text prefix is gone (explicit
            // request, after seeing the itch page mockups: "au lieu de
            // marquer Lueur: ... mettre le petit losange orange") — a small
            // rotated-square "diamond" icon stands in for it instead, since
            // no gem/diamond sprite exists in the Colorful UI pack. Icon and
            // number use a horizontal layout group and size themselves to
            // their contents, staying centered as the number's digit count grows.
            _lueurContainer = UIFactory.CreateUIObject("LueurContainer", lueurParent);
            var lueurLayout = _lueurContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            lueurLayout.spacing = 19f;
            lueurLayout.childAlignment = TextAnchor.MiddleCenter;
            lueurLayout.childControlWidth = true;
            lueurLayout.childControlHeight = true;
            lueurLayout.childForceExpandWidth = false;
            lueurLayout.childForceExpandHeight = false;
            var lueurFitter = _lueurContainer.gameObject.AddComponent<ContentSizeFitter>();
            lueurFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            lueurFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var lueurIcon = UIFactory.CreatePanel(_lueurContainer, "LueurIcon", VisualDefaults.GoldenColor);
            lueurIcon.rectTransform.sizeDelta = new Vector2(35f, 35f);
            lueurIcon.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            // Plain Image has no ILayoutElement, so without this the
            // HorizontalLayoutGroup gives it zero width to work with (same
            // gotcha as HandView's Shuffle button — see BuildShuffleButton).
            // Sized a bit larger than the 18px square itself to leave room
            // for the diamond's rotated corners (18 * sqrt(2) ≈ 25px
            // diagonal) without crowding the number next to it.
            var lueurIconLayout = lueurIcon.gameObject.AddComponent<LayoutElement>();
            lueurIconLayout.preferredWidth = 54f;
            lueurIconLayout.preferredHeight = 54f;

            _lueurLabel = UIFactory.CreateText(_lueurContainer, "LueurLabel", "", 84, VisualDefaults.GoldenColor);
            _lueurLabel.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>
        /// Own flat geometric bar (a plain cream track rectangle plus a
        /// solid-color fill rectangle) instead of the "Colorful UI" pack's
        /// rounded-pill slider sprites — on explicit request, after seeing
        /// them next to the new DA ("refaire l'asset ... des deux progress
        /// bar"): those sprites' own baked-in purple/blue art doesn't follow
        /// UITheme at all (CreateSlicedImage tints sprites white, i.e. not
        /// at all), so they kept showing their old colors no matter what
        /// the rest of the reskin changed. A plain rectangle also fits this
        /// bar's own "flush against the screen edge, full width" shape
        /// better than a rounded pill did.
        /// </summary>
        private static void BuildBar(Transform parent, string name, Color fillColor, bool top,
            out RectTransform fillRect, out Text label)
        {
            float edgeY = top ? 1f : 0f;
            // Both bars share the same flat track color and only differ by
            // fill color.
            var bg = UIFactory.CreatePanel(parent, name, UITheme.PanelLight);
            UIFactory.AddThickOutline(bg, UITheme.Border);
            // Stretched full-width (anchor min/max x = 0/1) and flush against
            // the top or bottom edge (anchor, pivot and anchoredPosition all
            // pinned to that same edge — zero anchoredPosition means no gap).
            bg.rectTransform.anchorMin = new Vector2(0f, edgeY);
            bg.rectTransform.anchorMax = new Vector2(1f, edgeY);
            bg.rectTransform.pivot = new Vector2(0.5f, edgeY);
            bg.rectTransform.anchoredPosition = Vector2.zero;
            bg.rectTransform.sizeDelta = new Vector2(0f, BarHeight);

            // The fill's RIGHT edge is driven directly by anchorMax.x (see
            // SetRatio) — a pure layout resize, not Image.Type.Filled — so
            // the bar's width is guaranteed to track the ratio with no
            // dependency on fill-shader/mesh behavior.
            var fillImg = UIFactory.CreatePanel(bg.transform, "Fill", fillColor);
            var rt = fillImg.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Dark text — the track/fill are both light (cream/mustard/mint)
            // now, unlike the old dark-purple sprite this used to sit on.
            var text = UIFactory.CreateText(bg.transform, "Label", "", 32, UITheme.TextPrimary);
            UIFactory.StretchFull(text.rectTransform);

            fillRect = rt;
            label = text;
        }

        /// <summary>
        /// A light band flush against the top edge (explicit request:
        /// "ajouter une bande en haut de l'écran avec une couleur clair
        /// pour mettre les ennemies à l'intérieur"), holding a row of small
        /// square portraits — one per enemy slot up to <see
        /// cref="MaxEnemyIcons"/>, left-to-right in encounter/targeting
        /// order (GDD §07: "Which enemy should I kill first?" — leftmost is
        /// always the one taking damage — see RunManager.
        /// ApplyDamageToEncounter), each with its own HP readout directly
        /// BELOW its icon (explicit request: "il faut ajouter la vie d'un
        /// ennemi sous lui") rather than overlaid on it. Built once, hidden
        /// until SetEncounter populates and shows exactly as many slots as
        /// the current round's encounter has.
        /// </summary>
        private void BuildEnemyBand(Transform parent)
        {
            var band = UIFactory.CreatePanel(parent, "EnemyBand", UITheme.PanelLight);
            UIFactory.AddThickOutline(band, UITheme.Border);
            band.rectTransform.anchorMin = new Vector2(0f, 1f);
            band.rectTransform.anchorMax = new Vector2(1f, 1f);
            band.rectTransform.pivot = new Vector2(0.5f, 1f);
            band.rectTransform.anchoredPosition = Vector2.zero;
            band.rectTransform.sizeDelta = new Vector2(0f, EnemyBandHeight);
            _enemyBandRoot = band.gameObject;

            var row = UIFactory.CreateUIObject("EnemyIconRow", band.transform);
            row.anchorMin = new Vector2(0.5f, 0.5f);
            row.anchorMax = new Vector2(0.5f, 0.5f);
            row.pivot = new Vector2(0.5f, 0.5f);
            row.anchoredPosition = Vector2.zero;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            var fitter = row.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            for (int i = 0; i < MaxEnemyIcons; i++)
            {
                var slot = UIFactory.CreateUIObject("EnemySlot" + i, row);
                var slotLayoutElement = slot.gameObject.AddComponent<LayoutElement>();
                slotLayoutElement.preferredWidth = EnemySlotWidth;
                slotLayoutElement.preferredHeight = EnemyIconTopPadding + EnemyIconSize + 20f;

                var icon = UIFactory.CreatePanel(slot, "EnemyIcon" + i, UITheme.Danger);
                icon.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                icon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                icon.rectTransform.pivot = new Vector2(0.5f, 1f);
                icon.rectTransform.anchoredPosition = new Vector2(0f, -EnemyIconTopPadding);
                icon.rectTransform.sizeDelta = new Vector2(EnemyIconSize, EnemyIconSize);
                UIFactory.AddThickOutline(icon, UITheme.Border);

                // Hover tooltip with this enemy's On-Shuffle effect (explicit
                // request: "pouvoir hover sur l'ennemi pour avoir plus de
                // détails sur ce qu'il fait comme effet lorsqu'on
                // shuffle") — Init'd fresh each SetEncounter call below.
                var iconView = icon.gameObject.AddComponent<EnemyIconView>();

                // Directly BELOW the icon, not overlaid on it — explicit
                // request above.
                var label = UIFactory.CreateText(slot, "Hp", "", 13, UITheme.TextPrimary);
                label.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                label.rectTransform.pivot = new Vector2(0.5f, 1f);
                label.rectTransform.anchoredPosition = new Vector2(0f, -(EnemyIconTopPadding + EnemyIconSize + 2f));
                label.rectTransform.sizeDelta = new Vector2(EnemySlotWidth, 16f);

                slot.gameObject.SetActive(false);
                _enemySlots.Add(slot.gameObject);
                _enemyIconImages.Add(icon);
                _enemyIconLabels.Add(label);
                _enemyIconViews.Add(iconView);
            }
            _enemyBandRoot.SetActive(false);
        }

        /// <summary>Resizes a bar's fill rect so its right edge sits at <paramref name="ratio"/> (0-1) of the bar's width.</summary>
        private static void SetRatio(RectTransform fillRect, float ratio)
        {
            var max = fillRect.anchorMax;
            max.x = Mathf.Clamp01(ratio);
            fillRect.anchorMax = max;
        }

        /// <summary>Anchor for the Lueur readout (icon + number together) — the presentation layer flies each Lueur group's popup toward this point (see GameBootstrap.PlayPlacementSequence) instead of just adding the total in one lump sum.</summary>
        public RectTransform LueurLabelTransform
        {
            get { return _lueurContainer; }
        }

        /// <summary>Anchor on the pieces bar — the presentation layer flies each round-end "unused piece -> Lueur" popup FROM this point (see GameBootstrap.PlayRoundEndLueurBonusSequence), the mirror image of LueurLabelTransform above as a destination.</summary>
        public RectTransform PiecesBarTransform
        {
            get { return _piecesLabel.rectTransform; }
        }

        public void Refresh(RunManager run)
        {
            SetRound(run.CurrentRoundNumber, run.Challenge.RoundCount, run.IsEndless);
            SetPieces(run.PiecesRemainingThisRound, run.CurrentBudget);
            if (run.HasActiveEncounter)
            {
                SetEncounter(run.CurrentEncounter);
            }
            else
            {
                if (_isEncounterMode)
                {
                    _isEncounterMode = false;
                    _scoreBarRoot.SetActive(true);
                    _enemyBandRoot.SetActive(false);
                }
                SetScores(run.RoundScore, run.CurrentQuota);
            }
            SetLueur(run.Lueur);
        }

        /// <summary>Which round is currently in progress, folded into the score bar's own label by SetScores (on explicit request: "il faut mettre a quelle round on est rendu sur le nombre total a réussir") — stored rather than passed to SetScores directly since the round itself never changes across that method's own many progressive-update calls within a single placement's score cascade. <paramref name="isEndless"/> drops the "/roundCount" denominator (explicit request added Endless mode — a fixed cap no longer applies once the player keeps going past it, so "Round 9/8" would misread as overshooting a limit).</summary>
        public void SetRound(int roundNumber, int roundCount, bool isEndless)
        {
            _roundNumber = roundNumber;
            _roundCount = roundCount;
            _isEndless = isEndless;
        }

        /// <summary>
        /// Updates just the Lueur label, without touching anything else —
        /// same idea as <see cref="SetScores"/>, lets the presentation layer
        /// animate Lueur up progressively (one group at a time) instead of
        /// always jumping straight to the final value. Used to also pulse
        /// the label on every increase, removed outright on explicit
        /// request ("Enlève le pulse complètement sur l'effet lueur en
        /// haut de la grille") after several rounds of trying to tune its
        /// intensity/positioning/timing down to something that still read
        /// as too much.
        /// </summary>
        public void SetLueur(int lueur)
        {
            _lueurLabel.text = lueur.ToString();
        }

        /// <summary>
        /// Updates just the score bar, without touching the pieces bar — lets
        /// the presentation layer animate the score up progressively in sync
        /// with score popups instead of always jumping straight to the final
        /// value. No-op while <see cref="_isEncounterMode"/> is set (see its
        /// own doc comment) — GameBootstrap calls this unconditionally
        /// throughout its score-popup cascade regardless of round type.
        /// </summary>
        public void SetScores(int roundScore, int quota)
        {
            if (_isEncounterMode)
            {
                return;
            }
            string roundLabel = _isEndless ? "Round " + _roundNumber : "Round " + _roundNumber + "/" + _roundCount;
            _scoreLabel.text = roundLabel + "  —  " + roundScore + " / " + quota;
            _scoreFillRect.GetComponent<Image>().color = UITheme.ButtonSelected;
            SetRatio(_scoreFillRect, quota > 0 ? (float)roundScore / quota : 0f);
        }

        /// <summary>
        /// Hides the quota bar entirely and shows the light enemy band
        /// instead, with one small square per enemy in <paramref
        /// name="encounter"/>, left-to-right in targeting order (spec
        /// extension, explicit request: "Enleve la progress bar pour le
        /// quota. Au lieu met une petite image en haut pour chaque ennemi
        /// de gauche a droite pour la priorité" — the leftmost icon is
        /// always the one actually taking damage, see RunManager.
        /// ApplyDamageToEncounter). Each icon is tinted by enemy identity
        /// (<see cref="EnemyIconColor"/>) and carries its own "current/max
        /// HP" label directly below it ("il faut ajouter la vie d'un
        /// ennemi sous lui"); a dead enemy's slot stays in place (so the
        /// roster's own order/count never visibly shifts) but dims heavily.
        /// Caps at <see cref="MaxEnemyIcons"/> slots — this vertical slice
        /// never schedules more than 2 (see EncounterCatalog's round 4).
        /// </summary>
        public void SetEncounter(IReadOnlyList<EnemyInstance> encounter)
        {
            if (!_isEncounterMode)
            {
                _isEncounterMode = true;
                _scoreBarRoot.SetActive(false);
                _enemyBandRoot.SetActive(true);
            }

            for (int i = 0; i < _enemySlots.Count; i++)
            {
                bool show = i < encounter.Count;
                _enemySlots[i].SetActive(show);
                if (!show)
                {
                    continue;
                }

                var enemy = encounter[i];
                // Deliberately does NOT touch color/alpha for an already-
                // dead enemy — this is called after EVERY placement
                // (GameBootstrap.OnCellClicked), including ones that don't
                // target this slot at all, so re-applying the plain dead-gray
                // tint here would instantly erase whatever FadeOutEnemySlot
                // is (or already has) faded it to. A genuinely FRESH enemy
                // at this slot (a new round — EnemyInstance is never reused
                // across rounds) is never dead on its very first Refresh, so
                // this still resets the tint/alpha for it correctly; a
                // currently-alive enemy's tint is kept in sync every time as
                // before.
                if (!enemy.IsDead)
                {
                    _enemyIconImages[i].color = EnemyIconColor(enemy.Definition.Id, false);
                    _enemyIconLabels[i].color = UITheme.TextPrimary;
                }
                _enemyIconLabels[i].text = enemy.CurrentHp + "/" + enemy.Definition.MaxHp;
                _enemyIconViews[i].Init(_tooltip, enemy.Definition.Name, enemy.Definition.Description);
            }
        }

        /// <summary>
        /// Overrides just one enemy slot's HP label AND dead/alive tint,
        /// without touching anything else — lets the presentation layer
        /// hold a slot's HP (and its icon's color) at its pre-placement
        /// value through the score cascade and then animate both down in
        /// sync with the combo total (spec extension, explicit requests:
        /// "il faut faire les dégâts seulement à la fin du calcule ... une
        /// animation où on descend le pointage du combo pour le transférer
        /// en dégâts progressif", then "les ennemies deviennent mort avant
        /// l'animation de dégât, il faut vraiment attendre que l'ennemi
        /// soit rendu à 0hp" — <paramref name="isDead"/> lets the caller
        /// keep the icon tinted as still-alive throughout the drain and
        /// only flip it to the dead/gray tint on the very last frame, once
        /// the displayed HP has actually reached 0, instead of the real
        /// model's already-dead state leaking into the icon's color early),
        /// the same "presentation layer animates progressively" idea as
        /// <see cref="SetScores"/>. No-op if <paramref name="index"/> is out
        /// of range or its slot isn't currently shown.
        /// </summary>
        public void SetEnemyHpDisplay(int index, int hp, int maxHp, EnemyId identity, bool isDead)
        {
            if (index < 0 || index >= _enemyIconLabels.Count || !_enemySlots[index].activeSelf)
            {
                return;
            }
            _enemyIconLabels[index].text = hp + "/" + maxHp;
            _enemyIconImages[index].color = EnemyIconColor(identity, isDead);
        }

        /// <summary>
        /// Fades a defeated enemy's icon and HP label out to fully
        /// transparent over <paramref name="duration"/> seconds — explicit
        /// request: "Lorsqu'un ennemi se rend a 0HP, attends 0.25 secondes
        /// puis fait une animation de fade out" (the 0.25s wait itself is
        /// the caller's job, before yielding into this — see GameBootstrap.
        /// DrainComboIntoDamage). The slot itself is deliberately left
        /// active rather than hidden outright, same reasoning as
        /// EnemyIconColor's dead-gray tint: a defeated enemy keeps its
        /// place in the row instead of shifting the others (see
        /// SetEncounter's own doc comment). Meant to be yielded directly by
        /// the caller's own coroutine, not run through StartCoroutine.
        /// </summary>
        public IEnumerator FadeOutEnemySlot(int index, float duration)
        {
            if (index < 0 || index >= _enemySlots.Count || !_enemySlots[index].activeSelf)
            {
                yield break;
            }
            var icon = _enemyIconImages[index];
            var label = _enemyIconLabels[index];
            Color iconStart = icon.color;
            Color labelStart = label.color;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(t / duration);
                icon.color = new Color(iconStart.r, iconStart.g, iconStart.b, iconStart.a * alpha);
                label.color = new Color(labelStart.r, labelStart.g, labelStart.b, labelStart.a * alpha);
                yield return null;
            }
            icon.color = new Color(iconStart.r, iconStart.g, iconStart.b, 0f);
            label.color = new Color(labelStart.r, labelStart.g, labelStart.b, 0f);
        }

        /// <summary>Flat per-identity tint for an enemy's icon (no sprite art exists yet for any enemy) — a defeated one dims to near-transparent gray regardless of identity, so "dead" always reads the same way no matter which enemy it was. Takes <paramref name="isDead"/> explicitly rather than reading EnemyInstance.IsDead directly so SetEnemyHpDisplay's held/animated calls can report death on their own schedule, independent of the live model's already-updated state (see its own doc comment).</summary>
        private static Color EnemyIconColor(EnemyId identity, bool isDead)
        {
            if (isDead)
            {
                return new Color(0.5f, 0.5f, 0.5f, 0.35f);
            }
            switch (identity)
            {
                case EnemyId.Locker:
                    return UITheme.LightBlue;
                case EnemyId.Poisoner:
                    return UITheme.Danger;
                // HeavyLocker/Plague are Locker's/Poisoner's own Boss-tier
                // escalations (see EnemyCatalog) — same family hue, darker
                // and more saturated to read as "the tougher version".
                case EnemyId.HeavyLocker:
                    return new Color(0.173f, 0.384f, 0.573f); // darker/more saturated LightBlue
                case EnemyId.Plague:
                    return new Color(0.671f, 0.169f, 0.235f); // darker/more saturated Danger
                case EnemyId.Thief:
                    return UITheme.ButtonSelected; // gold — stealing
                case EnemyId.Reclaimer:
                    return UITheme.Success; // green — it's the healer
                case EnemyId.Leech:
                    return new Color(0.573f, 0.329f, 0.667f); // purple — the other healer, kept visually distinct from Reclaimer
                default:
                    return UITheme.TextMuted;
            }
        }

        /// <summary>
        /// Updates just the pieces bar, without touching the score bar — same
        /// "presentation layer animates progressively" idea as <see
        /// cref="SetScores"/>, used by GameBootstrap.PlayRoundEndLueurBonusSequence
        /// to count the bar down one unused piece at a time as each converts
        /// into +1 Lueur, instead of jumping straight to empty.
        /// </summary>
        public void SetPieces(int piecesRemaining, int budget)
        {
            _piecesLabel.text = piecesRemaining + " / " + budget;
            SetRatio(_piecesFillRect, budget > 0 ? (float)piecesRemaining / budget : 0f);
        }
    }
}
