using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// "Chips x mult" readout sitting in the gap between the grid and the
    /// hand: shows the running chips/mult (and their product, the
    /// placement's running total) of whichever placement is currently
    /// animating (see GameBootstrap.PlayPlacementSequence and
    /// PlacementResult.Chips/.Mult, which this mirrors progressively as the
    /// sequence plays out). A blue chips pill and a red mult pill reuse the
    /// existing button sprites, with a plain total-score readout above them.
    /// Kept separate from <see cref="HudView"/> since it needs to sit at a
    /// specific screen position, not inside the top bar's horizontal layout.
    /// </summary>
    public sealed class ComboView : MonoBehaviour
    {
        private const float PulseDuration = 0.35f;
        private const float PulsePeakScale = 1.2f;
        private const float PulsePeakFraction = 0.4f;
        private const float PillWidth = 99f;
        private const float PillHeight = 58f;
        private const float PillSpacing = 8f;
        private const float XLabelWidth = 22f;
        private const float TotalRowSpacing = 4.35f;

        private RectTransform _root;
        private Text _totalText;
        private Text _chipsText;
        private Text _multText;
        private RectTransform _chipsPill;
        private RectTransform _multPill;
        private Coroutine _chipsPulseCoroutine;
        private Coroutine _multPulseCoroutine;

        /// <summary>Makes the whole card explain itself on hover (the Score x Mult readout is the core of the game and never said what it is).</summary>
        public void AttachTooltip(TooltipView tooltip)
        {
            var hit = UIFactory.CreatePanel(_root, "TooltipHit", new Color(0f, 0f, 0f, 0f));
            UIFactory.StretchFull(hit.rectTransform);
            hit.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            hit.gameObject.AddComponent<HoverTooltip>().Init(tooltip, "Score",
                "Points x Mult. Points come from the groups you form and the lines you harvest; Mult from fertilizers and upgrades. The result is the damage dealt to the first pest.", "This placement", UITheme.TextMuted);
        }

        public RectTransform Build(Transform parent)
        {
            _root = UIFactory.CreateUIObject("ComboView", parent);
            var backgroundRect = UIFactory.CreateUIObject("Background", _root);
            UIFactory.StretchFull(backgroundRect);
            var background = backgroundRect.gameObject.AddComponent<Image>();
            // The same parchment card as the Fertilizers panel under it, so the column reads as one set.
            background.sprite = UISprites.CardBackground;
            background.type = Image.Type.Sliced;
            background.color = Color.white;
            background.raycastTarget = false;
            backgroundRect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var rootLayout = _root.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.spacing = TotalRowSpacing;
            rootLayout.childAlignment = TextAnchor.MiddleCenter;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;
            // childControlWidth/Height default to false, which would leave the group positioning children by their unset RectTransform height while ContentSizeFitter sizes the root off preferredHeight, creating a gap; forcing control here keeps them in sync.
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.padding = new RectOffset(12, 12, 10, 12);
            var rootFitter = _root.gameObject.AddComponent<ContentSizeFitter>();
            rootFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rootFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Running total (chips x mult).
            _totalText = UIFactory.CreateText(_root, "TotalText", "0", 58, UITheme.TextPrimary);
            var totalLayout = _totalText.gameObject.AddComponent<LayoutElement>();
            totalLayout.preferredWidth = PillWidth * 2f + PillSpacing * 2f;
            totalLayout.preferredHeight = 62f;

            var row = UIFactory.CreateUIObject("Row", _root);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = PillSpacing;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            var rowFitter = row.gameObject.AddComponent<ContentSizeFitter>();
            rowFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var rowLayoutElement = row.gameObject.AddComponent<LayoutElement>();
            rowLayoutElement.preferredWidth = PillWidth * 2f + XLabelWidth + PillSpacing * 2f;
            rowLayoutElement.preferredHeight = PillHeight;

            var chipsPill = UIFactory.CreateSlicedImage(row, "ChipsPill", UISprites.ChooseButtonBackground);
            chipsPill.rectTransform.sizeDelta = new Vector2(PillWidth, PillHeight);
            var chipsLayout = chipsPill.gameObject.AddComponent<LayoutElement>();
            chipsLayout.preferredWidth = PillWidth;
            chipsLayout.preferredHeight = PillHeight;
            _chipsPill = chipsPill.rectTransform;
            _chipsText = UIFactory.CreateText(chipsPill.transform, "ChipsText", "0", 38, Color.white);
            UIFactory.StretchFull(_chipsText.rectTransform);

            var xLabel = UIFactory.CreateText(row, "XLabel", "x", 32, UITheme.TextPrimary);
            var xLayout = xLabel.gameObject.AddComponent<LayoutElement>();
            xLayout.preferredWidth = XLabelWidth;
            xLayout.preferredHeight = PillHeight;

            var multPill = UIFactory.CreateSlicedImage(row, "MultPill", UISprites.CancelButtonBackground);
            multPill.rectTransform.sizeDelta = new Vector2(PillWidth, PillHeight);
            var multLayout = multPill.gameObject.AddComponent<LayoutElement>();
            multLayout.preferredWidth = PillWidth;
            multLayout.preferredHeight = PillHeight;
            _multPill = multPill.rectTransform;
            _multText = UIFactory.CreateText(multPill.transform, "MultText", "1", 38, Color.white);
            UIFactory.StretchFull(_multText.rectTransform);

            return _root;
        }

        /// <summary>
        /// Updates both pills plus the total readout above them (chips *
        /// mult). <paramref name="chips"/> mirrors <see cref="Core.PlacementResult.Chips"/>,
        /// <paramref name="mult"/> mirrors <see cref="Core.PlacementResult.Mult"/>.
        /// <paramref name="mult"/> is a float since progressive modifiers
        /// keep their fractional value; the pill shows it as a whole number
        /// when it is one, one decimal otherwise, and the total readout
        /// rounds the product once, matching <see cref="Core.PlacementResult.TotalScore"/>.
        /// </summary>
        public void Show(int chips, float mult)
        {
            _root.gameObject.SetActive(true);
            _chipsText.text = chips.ToString();
            _multText.text = FormatMult(mult);
            _totalText.text = Mathf.RoundToInt(chips * mult).ToString();
        }

        /// <summary>Overrides just the running-total readout, without touching the (by this point frozen) chips/mult pills; used by GameBootstrap's post-cascade "drain" animation to count the total down toward 0 as it's transferred into enemy damage.</summary>
        public void SetTotal(int total)
        {
            _totalText.text = total.ToString();
        }

        /// <summary>Overrides just the two pills (chips and mult), leaving the total readout alone; used with <see cref="SetTotal"/> by the drain animation so chips fall to 0 and mult to 1 in step with the total.</summary>
        public void SetPills(int chips, float mult)
        {
            _chipsText.text = chips.ToString();
            _multText.text = FormatMult(mult);
        }

        /// <summary>Anchor for the total readout — GameBootstrap flies a "-X" popup from here to the damaged enemy's icon as the drain starts, showing the actual damage amount rather than letting the HP bar just quietly tick down.</summary>
        public RectTransform TotalTransform
        {
            get { return _totalText.rectTransform; }
        }

        private static string FormatMult(float mult)
        {
            float rounded = Mathf.Round(mult);
            if (Mathf.Abs(mult - rounded) < 0.05f)
            {
                return Mathf.RoundToInt(mult).ToString();
            }
            return mult.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        }

        public void Hide()
        {
            _root.gameObject.SetActive(false);
        }

        /// <summary>Brief scale-bounce on the blue chips pill, used whenever <see cref="Core.PlacementResult.Chips"/> grows.</summary>
        public void PulseChips()
        {
            _chipsPulseCoroutine = RestartPulse(_chipsPulseCoroutine, _chipsPill);
        }

        /// <summary>Same bounce as <see cref="PulseChips"/>, but on the red mult pill, used whenever <see cref="Core.PlacementResult.Mult"/> grows.</summary>
        public void PulseMult()
        {
            _multPulseCoroutine = RestartPulse(_multPulseCoroutine, _multPill);
        }

        private Coroutine RestartPulse(Coroutine running, RectTransform target)
        {
            if (running != null)
            {
                StopCoroutine(running);
            }
            return StartCoroutine(PulseRoutine(target));
        }

        private IEnumerator PulseRoutine(RectTransform target)
        {
            float t = 0f;
            while (t < PulseDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / PulseDuration);
                float scale = p < PulsePeakFraction
                    ? Mathf.Lerp(1f, PulsePeakScale, p / PulsePeakFraction)
                    : Mathf.Lerp(PulsePeakScale, 1f, (p - PulsePeakFraction) / (1f - PulsePeakFraction));
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            target.localScale = Vector3.one;
        }
    }
}
