using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// Two full-width progress bars flush against the top and bottom edges of the screen: the top bar tracks
    /// round score against the round's quota, the bottom bar tracks remaining piece budget for the round.
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
        // Defaults match round 1 so the first SetScores call (before Refresh's own SetRound runs) never shows a stale 0/0.
        private int _roundNumber = 1;
        private int _roundCount = RunConfig.RoundCount;

        /// <summary>True while the score bar is hidden in favor of the enemy icon row. While true, SetScores is a no-op.</summary>
        private bool _isEncounterMode;

        /// <summary>The ScoreBar's own root, hidden entirely during an encounter round. Captured by climbing from _scoreFillRect's parent rather than widening BuildBar's signature just for this.</summary>
        private GameObject _scoreBarRoot;

        private const int MaxEnemyIcons = 6;
        private const float EnemyIconSize = 44f;
        private const float EnemySlotWidth = 76f;
        private const float EnemyIconTopPadding = 14f;
        // GameBootstrap's status text position derives from this directly (see BuildUI), so the two stay in sync.
        public const float EnemyBandHeight = BarHeight + EnemyIconTopPadding;
        private GameObject _enemyBandRoot;
        private readonly List<GameObject> _enemySlots = new List<GameObject>();

        /// <summary>
        /// True once a slot's death has actually been revealed (icon tinted dead-gray by
        /// <see cref="SetEnemyHpDisplay"/>, not merely true in Core). <see cref="SetEncounter"/> reads this
        /// instead of EnemyInstance.IsDead so a kill stays alive-looking until the HP drain animation finishes.
        /// Reset to all-false when a new round's encounter arrives.
        /// </summary>
        private readonly bool[] _enemySlotRevealedDead = new bool[MaxEnemyIcons];

        /// <summary>Which EnemyInstance list <see cref="_enemySlotRevealedDead"/> was last reset for — compared by reference, never by value.</summary>
        private IReadOnlyList<EnemyInstance> _lastEncounterRefForDeathReveal;
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

            // Lueur is a whole-run currency (see RunManager.Lueur), shown as a rotated-square "diamond" icon
            // plus number in a horizontal layout group that sizes to its contents.
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
            // Plain Image has no ILayoutElement, so without this the HorizontalLayoutGroup gives it zero width.
            var lueurIconLayout = lueurIcon.gameObject.AddComponent<LayoutElement>();
            lueurIconLayout.preferredWidth = 54f;
            lueurIconLayout.preferredHeight = 54f;

            _lueurLabel = UIFactory.CreateText(_lueurContainer, "LueurLabel", "", 84, VisualDefaults.GoldenColor);
            _lueurLabel.alignment = TextAnchor.MiddleCenter;
        }

        /// <summary>Builds a flat track rectangle plus a solid-color fill rectangle, rather than a sprite-based slider, so the bar fully follows UITheme colors.</summary>
        private static void BuildBar(Transform parent, string name, Color fillColor, bool top,
            out RectTransform fillRect, out Text label)
        {
            float edgeY = top ? 1f : 0f;
            var bg = UIFactory.CreatePanel(parent, name, UITheme.PanelLight);
            UIFactory.AddThickOutline(bg, UITheme.Border);
            // Stretched full-width and flush against the top or bottom edge (zero anchoredPosition means no gap).
            bg.rectTransform.anchorMin = new Vector2(0f, edgeY);
            bg.rectTransform.anchorMax = new Vector2(1f, edgeY);
            bg.rectTransform.pivot = new Vector2(0.5f, edgeY);
            bg.rectTransform.anchoredPosition = Vector2.zero;
            bg.rectTransform.sizeDelta = new Vector2(0f, BarHeight);

            // The fill's right edge is driven directly by anchorMax.x (see SetRatio), a layout resize rather than Image.Type.Filled.
            var fillImg = UIFactory.CreatePanel(bg.transform, "Fill", fillColor);
            var rt = fillImg.rectTransform;
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var text = UIFactory.CreateText(bg.transform, "Label", "", 32, UITheme.TextPrimary);
            UIFactory.StretchFull(text.rectTransform);

            fillRect = rt;
            label = text;
        }

        /// <summary>
        /// A light band flush against the top edge holding a row of small square portraits, one per enemy
        /// slot up to <see cref="MaxEnemyIcons"/>, left-to-right in targeting order (leftmost always takes
        /// damage first, see RunManager.ApplyDamageToEncounter), each with an HP readout below its icon.
        /// Built once, hidden until SetEncounter populates and shows the slots the current encounter needs.
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
                // Explicit size (not just the LayoutElement's preferred size) so the
                // icon/label below, anchored relative to this rect, don't depend on
                // Unity's implicit zero-size default rect for a freshly created
                // RectTransform — childControlHeight/Width is false on the row's
                // HorizontalLayoutGroup, so nothing else would ever set this.
                float slotHeight = EnemyIconTopPadding + EnemyIconSize + 20f;
                slot.sizeDelta = new Vector2(EnemySlotWidth, slotHeight);
                var slotLayoutElement = slot.gameObject.AddComponent<LayoutElement>();
                slotLayoutElement.preferredWidth = EnemySlotWidth;
                slotLayoutElement.preferredHeight = slotHeight;

                var icon = UIFactory.CreatePanel(slot, "EnemyIcon" + i, UITheme.Danger);
                icon.rectTransform.anchorMin = new Vector2(0.5f, 1f);
                icon.rectTransform.anchorMax = new Vector2(0.5f, 1f);
                icon.rectTransform.pivot = new Vector2(0.5f, 1f);
                icon.rectTransform.anchoredPosition = new Vector2(0f, -EnemyIconTopPadding);
                icon.rectTransform.sizeDelta = new Vector2(EnemyIconSize, EnemyIconSize);
                UIFactory.AddThickOutline(icon, UITheme.Border);

                // Hover tooltip with this enemy's On-Shuffle effect — Init'd fresh each SetEncounter call below.
                var iconView = icon.gameObject.AddComponent<EnemyIconView>();

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

        /// <summary>Anchor for the Lueur readout — the presentation layer flies each Lueur group's popup toward this point (see GameBootstrap.PlayPlacementSequence).</summary>
        public RectTransform LueurLabelTransform
        {
            get { return _lueurContainer; }
        }

        /// <summary>Anchor on the pieces bar — the presentation layer flies each round-end "unused piece -> Lueur" popup from this point (see GameBootstrap.PlayRoundEndLueurBonusSequence).</summary>
        public RectTransform PiecesBarTransform
        {
            get { return _piecesLabel.rectTransform; }
        }

        public void Refresh(RunManager run)
        {
            SetRound(run.CurrentRoundNumber, run.Challenge.RoundCount);
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

        /// <summary>Which round is in progress, folded into the score bar's label by SetScores. Stored rather than passed to SetScores directly since it doesn't change across a single placement's score cascade.</summary>
        public void SetRound(int roundNumber, int roundCount)
        {
            _roundNumber = roundNumber;
            _roundCount = roundCount;
        }

        /// <summary>Updates just the Lueur label, letting the presentation layer animate it up progressively instead of jumping straight to the final value.</summary>
        public void SetLueur(int lueur)
        {
            _lueurLabel.text = lueur.ToString();
        }

        /// <summary>Updates just the score bar, letting the presentation layer animate it in sync with score popups. No-op while <see cref="_isEncounterMode"/> is set.</summary>
        public void SetScores(int roundScore, int quota)
        {
            if (_isEncounterMode)
            {
                return;
            }
            string roundLabel = "Round " + _roundNumber + "/" + _roundCount;
            _scoreLabel.text = roundLabel + "  —  " + roundScore + " / " + quota;
            _scoreFillRect.GetComponent<Image>().color = UITheme.ButtonSelected;
            SetRatio(_scoreFillRect, quota > 0 ? (float)roundScore / quota : 0f);
        }

        /// <summary>
        /// Hides the quota bar and shows the enemy band instead, with one icon per enemy in
        /// <paramref name="encounter"/>, left-to-right in targeting order (leftmost always takes damage
        /// first, see RunManager.ApplyDamageToEncounter). Each icon is tinted by identity
        /// (<see cref="EnemyIconColor"/>) with a current/max HP label below it. A slot whose death has
        /// already been revealed (see <see cref="_enemySlotRevealedDead"/>) is hidden outright rather than
        /// dimmed, so HorizontalLayoutGroup re-centers the remaining icons. Caps at <see cref="MaxEnemyIcons"/>.
        /// </summary>
        public void SetEncounter(IReadOnlyList<EnemyInstance> encounter)
        {
            if (!_isEncounterMode)
            {
                _isEncounterMode = true;
                _scoreBarRoot.SetActive(false);
                _enemyBandRoot.SetActive(true);
            }
            if (!ReferenceEquals(encounter, _lastEncounterRefForDeathReveal))
            {
                _lastEncounterRefForDeathReveal = encounter;
                System.Array.Clear(_enemySlotRevealedDead, 0, _enemySlotRevealedDead.Length);
            }

            for (int i = 0; i < _enemySlots.Count; i++)
            {
                bool show = i < encounter.Count && !_enemySlotRevealedDead[i];
                _enemySlots[i].SetActive(show);
                if (!show)
                {
                    continue;
                }

                var enemy = encounter[i];
                // Deliberately skips color/alpha for an already-dead enemy: this runs after every placement,
                // and re-applying the dead-gray tint here would erase whatever FadeOutEnemySlot is fading to.
                if (!enemy.IsDead)
                {
                    _enemyIconImages[i].color = EnemyIconColor(enemy.Definition.Id, false);
                    _enemyIconLabels[i].color = UITheme.TextPrimary;
                }
                // CurrentMaxHp, not Definition.MaxHp — Reclaimer's ceiling can grow past the shared Definition's value.
                _enemyIconLabels[i].text = enemy.CurrentHp + "/" + enemy.CurrentMaxHp;
                _enemyIconViews[i].Init(_tooltip, enemy.Definition.Name, HaterDescription(enemy));
            }
        }

        /// <summary>Color Hater/Shape Hater's tooltip names the specific color/shape this instance was randomly assigned on spawn, rather than the Definition's generic text. Every other enemy's description is returned unchanged.</summary>
        private static string HaterDescription(EnemyInstance enemy)
        {
            if (enemy.Definition.Id == EnemyId.ColorHater && enemy.HatedColor.HasValue)
            {
                return "Hates " + VisualDefaults.GetColorName(enemy.HatedColor.Value) + ". Every point scored through a " + VisualDefaults.GetColorName(enemy.HatedColor.Value) + " tile is cancelled for the rest of the round.";
            }
            if (enemy.Definition.Id == EnemyId.ShapeHater && enemy.HatedShape.HasValue)
            {
                return "Hates the " + VisualDefaults.GetShapeName(enemy.HatedShape.Value) + " shape. Every point scored through a tile originally placed by a " + VisualDefaults.GetShapeName(enemy.HatedShape.Value) + " is cancelled for the rest of the round.";
            }
            return enemy.Definition.Description;
        }

        /// <summary>
        /// Overrides just one enemy slot's HP label and dead/alive tint, letting the presentation layer hold
        /// a slot's HP at its pre-placement value through the score cascade and animate it down in sync with
        /// the combo total. <paramref name="isDead"/> lets the caller keep the icon tinted alive throughout
        /// the drain and flip it to dead/gray only once the displayed HP reaches 0, instead of the model's
        /// already-dead state leaking into the icon early. No-op if <paramref name="index"/> is out of range
        /// or its slot isn't shown.
        /// </summary>
        public void SetEnemyHpDisplay(int index, int hp, int maxHp, EnemyId identity, bool isDead)
        {
            if (index < 0 || index >= _enemyIconLabels.Count || !_enemySlots[index].activeSelf)
            {
                return;
            }
            _enemyIconLabels[index].text = hp + "/" + maxHp;
            _enemyIconImages[index].color = EnemyIconColor(identity, isDead);
            if (isDead)
            {
                // The exact moment this kill is first visually revealed — see _enemySlotRevealedDead's own doc comment.
                _enemySlotRevealedDead[index] = true;
            }
        }

        /// <summary>Anchor for a feedback popup on a given enemy slot's icon — used for Thief's steal effect. Null if out of range or that slot isn't shown.</summary>
        public RectTransform GetEnemyIconTransform(int index)
        {
            if (index < 0 || index >= _enemyIconImages.Count || !_enemySlots[index].activeSelf)
            {
                return null;
            }
            return _enemyIconImages[index].rectTransform;
        }

        /// <summary>
        /// Fades a defeated enemy's icon and HP label to fully transparent over <paramref name="duration"/>
        /// seconds, then deactivates the slot: HorizontalLayoutGroup excludes an inactive child from layout,
        /// so remaining icons re-center automatically. SetEncounter won't reactivate it again this round (see
        /// _enemySlotRevealedDead). Meant to be yielded directly by the caller's own coroutine, not run
        /// through StartCoroutine.
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
            _enemySlots[index].SetActive(false);
        }

        /// <summary>Flat per-identity tint for an enemy's icon. A defeated one dims to near-transparent gray regardless of identity. Takes <paramref name="isDead"/> explicitly so SetEnemyHpDisplay's animated calls can report death on their own schedule, independent of the model's already-updated state.</summary>
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
                // HeavyLocker/Plague are Locker's/Poisoner's Boss-tier escalations: same family hue, darker and more saturated.
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
                case EnemyId.Basic:
                    return new Color(0.580f, 0.631f, 0.675f); // neutral steel gray — no special effect
                case EnemyId.ColorHater:
                    return new Color(0.902f, 0.494f, 0.133f); // orange
                case EnemyId.ShapeHater:
                    return new Color(0.086f, 0.627f, 0.522f); // teal
                default:
                    return UITheme.TextMuted;
            }
        }

        /// <summary>Updates just the pieces bar, used by GameBootstrap.PlayRoundEndLueurBonusSequence to count it down one unused piece at a time instead of jumping straight to empty.</summary>
        public void SetPieces(int piecesRemaining, int budget)
        {
            _piecesLabel.text = piecesRemaining + " / " + budget;
            SetRatio(_piecesFillRect, budget > 0 ? (float)piecesRemaining / budget : 0f);
        }
    }
}
