using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    public enum SprigMood { Happy, Neutral, Sad, Cheer }

    /// <summary>One bubble of the first-run guide: what Sprig says, how he looks, and which on-screen element (if any) gets a pulsing frame. <see cref="Targets"/> is evaluated when the step starts; the frame wraps all returned transforms, and the step is skipped if every target is missing/hidden.</summary>
    public sealed class SprigGuideStep
    {
        public string Text;
        public SprigMood Mood = SprigMood.Happy;
        public Func<RectTransform[]> Targets;
    }

    /// <summary>
    /// Sprig, the garden guide: a small seed sprite who explains the basics in speech bubbles during the
    /// first-run guide (<see cref="RunGuide"/>) and drops short non-blocking comments at the start of a run
    /// (<see cref="Say"/>). The end screen reuses <see cref="BuildSpeaker"/> and the line pools below.
    /// Art: Resources/Characters/Sprig_&lt;Mood&gt;.png (Design/Characters/generate_sprig.py).
    /// </summary>
    public sealed class SprigView : MonoBehaviour
    {
        private const float TypeInterval = 0.018f;
        private const float GuidePortraitSize = 210f;
        private const float ToastPortraitSize = 120f;
        private const float BubbleWidthGuide = 430f;
        private const float BubbleWidthToast = 300f;

        private static readonly Dictionary<SprigMood, Sprite> _portraits = new Dictionary<SprigMood, Sprite>();
        private static readonly System.Random _rng = new System.Random();

        private RectTransform _toastRoot;
        private Image _toastPortrait;
        private Text _toastText;
        private Coroutine _toastRoutine;
        private CanvasGroup _toastGroup;

        private RectTransform _guideRoot;
        private Image _guidePortrait;
        private Text _guideText;
        private Text _guideHint;
        private RectTransform _speakerRoot;
        private RectTransform _ring;
        private Image[] _ringBars;
        private bool _guideClicked;
        private bool _guiding;

        public bool IsGuiding { get { return _guiding; } }

        public static Sprite Portrait(SprigMood mood)
        {
            if (!_portraits.TryGetValue(mood, out var sprite))
            {
                sprite = Resources.Load<Sprite>("Characters/Sprig_" + mood);
                _portraits[mood] = sprite;
            }
            return sprite;
        }

        /// <summary>Portrait image plus a parchment speech bubble to its right, anchored bottom-left of <paramref name="parent"/> at (<paramref name="x"/>, <paramref name="y"/>). Returns the portrait; the bubble's text comes back via <paramref name="text"/>, and the whole block's root via <paramref name="root"/>.</summary>
        public static Image BuildSpeaker(Transform parent, string name, float portraitSize, float bubbleWidth, int fontSize, out Text text, out RectTransform root)
        {
            root = UIFactory.CreateUIObject(name, parent);
            root.anchorMin = root.anchorMax = new Vector2(0f, 0f);
            root.pivot = new Vector2(0f, 0f);
            root.sizeDelta = new Vector2(portraitSize + bubbleWidth + 10f, portraitSize);

            var portrait = UIFactory.CreatePanel(root, "Portrait", Color.white);
            portrait.raycastTarget = false;
            portrait.preserveAspect = true;
            portrait.sprite = Portrait(SprigMood.Happy);
            portrait.gameObject.AddComponent<SprigFloat>();
            portrait.rectTransform.anchorMin = portrait.rectTransform.anchorMax = new Vector2(0f, 0f);
            portrait.rectTransform.pivot = new Vector2(0f, 0f);
            portrait.rectTransform.anchoredPosition = Vector2.zero;
            portrait.rectTransform.sizeDelta = new Vector2(portraitSize, portraitSize);

            var bubble = UIFactory.CreateSlicedImage(root, "Bubble", UISprites.CardBackground);
            bubble.raycastTarget = false;
            bubble.rectTransform.anchorMin = bubble.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            bubble.rectTransform.pivot = new Vector2(0f, 0.5f);
            bubble.rectTransform.anchoredPosition = new Vector2(portraitSize - 8f, portraitSize * 0.12f);
            bubble.rectTransform.sizeDelta = new Vector2(bubbleWidth, portraitSize * 0.74f);

            text = UIFactory.CreateText(bubble.transform, "Text", "", fontSize, UITheme.TextPrimary, TextAnchor.MiddleLeft);
            text.raycastTarget = false;
            // The pack's Digitalt font draws "!" like a capital I; Sprig's speech is hand-lettered instead (Resources/Font/PatrickHand-Regular).
            var speechFont = Resources.Load<Font>("Font/PatrickHand-Regular");
            text.font = speechFont != null ? speechFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (speechFont != null)
            {
                text.fontSize = Mathf.RoundToInt(fontSize * 1.15f);
            }
            UIFactory.StretchFull(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(22f, 14f);
            text.rectTransform.offsetMax = new Vector2(-18f, -14f);
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return portrait;
        }

        public void Build(Transform parent)
        {
            // Toast: small, never blocks clicks.
            _toastPortrait = BuildSpeaker(parent, "SprigToast", ToastPortraitSize, BubbleWidthToast, 16, out _toastText, out _toastRoot);
            _toastRoot.anchoredPosition = new Vector2(12f, 8f);
            _toastGroup = _toastRoot.gameObject.AddComponent<CanvasGroup>();
            _toastRoot.gameObject.SetActive(false);

            // Guide: dimmed full-screen click catcher + frame + big Sprig.
            var dim = UIFactory.CreatePanel(parent, "SprigGuide", new Color(0.03f, 0.05f, 0.08f, 0.5f));
            _guideRoot = dim.rectTransform;
            UIFactory.StretchFull(_guideRoot);
            var click = dim.gameObject.AddComponent<Button>();
            click.transition = Selectable.Transition.None;
            click.onClick.AddListener(() => _guideClicked = true);

            _ring = UIFactory.CreateUIObject("Ring", _guideRoot);
            _ring.anchorMin = _ring.anchorMax = _ring.pivot = new Vector2(0.5f, 0.5f);
            _ringBars = new Image[4];
            for (int i = 0; i < 4; i++)
            {
                var bar = UIFactory.CreatePanel(_ring, "Bar" + i, new Color(1f, 0.84f, 0.29f, 1f));
                bar.raycastTarget = false;
                _ringBars[i] = bar;
            }

            _guidePortrait = BuildSpeaker(_guideRoot, "Speaker", GuidePortraitSize, BubbleWidthGuide, 20, out _guideText, out var speakerRoot);
            speakerRoot.anchoredPosition = new Vector2(40f, 40f);
            _speakerRoot = speakerRoot;

            _guideHint = UIFactory.CreateText(speakerRoot, "Hint", "Click to continue", 15, UITheme.TextMutedOnBackground, TextAnchor.MiddleLeft);
            _guideHint.raycastTarget = false;
            _guideHint.rectTransform.anchorMin = _guideHint.rectTransform.anchorMax = new Vector2(0f, 0f);
            _guideHint.rectTransform.pivot = new Vector2(0f, 0f);
            _guideHint.rectTransform.anchoredPosition = new Vector2(GuidePortraitSize + 12f, 14f);
            _guideHint.rectTransform.sizeDelta = new Vector2(300f, 24f);

            _guideRoot.gameObject.SetActive(false);
        }

        /// <summary>Short non-blocking comment from Sprig in the corner, fading away after <paramref name="seconds"/>.</summary>
        public void Say(string line, SprigMood mood, float seconds = 5f)
        {
            if (_guiding)
            {
                return;
            }
            if (_toastRoutine != null)
            {
                StopCoroutine(_toastRoutine);
            }
            _toastRoutine = StartCoroutine(ToastRoutine(line, mood, seconds));
        }

        private IEnumerator ToastRoutine(string line, SprigMood mood, float seconds)
        {
            _toastPortrait.sprite = Portrait(mood);
            _toastText.text = line;
            _toastRoot.SetAsLastSibling();
            _toastRoot.gameObject.SetActive(true);
            var group = _toastGroup;
            group.blocksRaycasts = false;
            group.interactable = false;
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                group.alpha = Mathf.Clamp01(t / 0.25f);
                yield return null;
            }
            group.alpha = 1f;
            yield return new WaitForSeconds(seconds);
            t = 0f;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                group.alpha = 1f - Mathf.Clamp01(t / 0.5f);
                yield return null;
            }
            _toastRoot.gameObject.SetActive(false);
            _toastRoutine = null;
        }

        public void RunGuide(IList<SprigGuideStep> steps, Action onDone = null)
        {
            if (_guiding)
            {
                return;
            }
            StartCoroutine(GuideRoutine(steps, onDone));
        }

        private IEnumerator GuideRoutine(IList<SprigGuideStep> steps, Action onDone)
        {
            _guiding = true;
            if (_toastRoot != null)
            {
                _toastRoot.gameObject.SetActive(false);
            }
            _guideRoot.SetAsLastSibling();
            _guideRoot.gameObject.SetActive(true);
            SfxManager.Play(SfxId.Overlay);

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];
                RectTransform[] targets = step.Targets != null ? step.Targets() : null;
                bool wantsTarget = step.Targets != null;
                bool haveTarget = FrameTargets(targets);
                if (wantsTarget && !haveTarget)
                {
                    continue;
                }
                _ring.gameObject.SetActive(haveTarget);
                PlaceSpeakerAwayFromRing(haveTarget);
                _guidePortrait.sprite = Portrait(step.Mood);
                _guideHint.text = i == steps.Count - 1 ? "Click to start" : "Click to continue";
                _guideClicked = false;

                // Typewriter; a click mid-way completes the line, the next click advances.
                _guideText.text = "";
                int shown = 0;
                float next = 0f;
                while (shown < step.Text.Length)
                {
                    if (_guideClicked)
                    {
                        _guideClicked = false;
                        break;
                    }
                    next += Time.unscaledDeltaTime;
                    while (next >= TypeInterval && shown < step.Text.Length)
                    {
                        next -= TypeInterval;
                        shown++;
                    }
                    _guideText.text = step.Text.Substring(0, shown);
                    PulseRing();
                    yield return null;
                }
                _guideText.text = step.Text;
                while (!_guideClicked)
                {
                    PulseRing();
                    yield return null;
                }
                _guideClicked = false;
                SfxManager.Play(SfxId.Overlay);
            }

            _guideRoot.gameObject.SetActive(false);
            _guiding = false;
            onDone?.Invoke();
        }

        /// <summary>Positions the pulsing frame around the union of <paramref name="targets"/>' screen rects. False if none is usable.</summary>
        private bool FrameTargets(RectTransform[] targets)
        {
            if (targets == null)
            {
                return false;
            }
            bool any = false;
            Vector3 min = Vector3.zero, max = Vector3.zero;
            var corners = new Vector3[4];
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null || !targets[i].gameObject.activeInHierarchy)
                {
                    continue;
                }
                targets[i].GetWorldCorners(corners);
                for (int c = 0; c < 4; c++)
                {
                    if (!any)
                    {
                        min = max = corners[c];
                        any = true;
                    }
                    else
                    {
                        min = Vector3.Min(min, corners[c]);
                        max = Vector3.Max(max, corners[c]);
                    }
                }
            }
            if (!any)
            {
                return false;
            }
            Vector3 scale = _guideRoot.lossyScale;
            float sx = scale.x != 0f ? scale.x : 1f;
            float sy = scale.y != 0f ? scale.y : 1f;
            const float pad = 10f;
            _ring.position = (min + max) / 2f;
            _ring.sizeDelta = new Vector2((max.x - min.x) / sx + pad * 2f, (max.y - min.y) / sy + pad * 2f);
            float w = _ring.sizeDelta.x, h = _ring.sizeDelta.y, th = 5f;
            Place(_ringBars[0], 0f, h / 2f - th / 2f, w, th);
            Place(_ringBars[1], 0f, -h / 2f + th / 2f, w, th);
            Place(_ringBars[2], -w / 2f + th / 2f, 0f, th, h);
            Place(_ringBars[3], w / 2f - th / 2f, 0f, th, h);
            return true;
        }

        /// <summary>Moves Sprig + his bubble to whichever screen corner (or left-middle) doesn't cover the framed element.</summary>
        private void PlaceSpeakerAwayFromRing(bool haveTarget)
        {
            Rect screen = _guideRoot.rect;
            Vector2 size = _speakerRoot.sizeDelta;
            const float margin = 24f;
            Vector2[] candidates =
            {
                new Vector2(margin, margin),
                new Vector2(margin, screen.height - size.y - margin),
                new Vector2(screen.width - size.x - margin, margin),
                new Vector2(screen.width - size.x - margin, screen.height - size.y - margin),
                new Vector2(margin, (screen.height - size.y) / 2f),
            };
            Vector2 chosen = candidates[0];
            if (haveTarget)
            {
                var corners = new Vector3[4];
                _ring.GetWorldCorners(corners);
                Vector2 a = (Vector2)_guideRoot.InverseTransformPoint(corners[0]) - screen.min;
                Vector2 b = (Vector2)_guideRoot.InverseTransformPoint(corners[2]) - screen.min;
                var target = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                float bestOverlap = float.MaxValue;
                for (int i = 0; i < candidates.Length; i++)
                {
                    var box = new Rect(candidates[i], size);
                    float ox = Mathf.Max(0f, Mathf.Min(box.xMax, target.xMax) - Mathf.Max(box.xMin, target.xMin));
                    float oy = Mathf.Max(0f, Mathf.Min(box.yMax, target.yMax) - Mathf.Max(box.yMin, target.yMin));
                    float overlap = ox * oy;
                    if (overlap < bestOverlap)
                    {
                        bestOverlap = overlap;
                        chosen = candidates[i];
                        if (overlap <= 0f)
                        {
                            break;
                        }
                    }
                }
            }
            _speakerRoot.anchoredPosition = chosen;
        }

        private static void Place(Image bar, float x, float y, float w, float h)
        {
            var r = bar.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        private void PulseRing()
        {
            float a = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
            for (int i = 0; i < _ringBars.Length; i++)
            {
                _ringBars[i].color = new Color(1f, 0.84f, 0.29f, a);
            }
        }

        // ---------- Lines ----------

        private static readonly string[] RunStartLines =
        {
            "Fresh soil, fresh start. Let's grow something!",
            "The pests are hungry today. Plant smart!",
            "Same-colored seeds love to sit together.",
            "Tip: a full row or column is a harvest. Easy Pollen!",
            "Don't forget your fertilizers — they stack up nicely.",
            "Shuffle is free of shame when you're stuck.",
        };

        private static readonly string[] VictoryLines =
        {
            "The whole garden is in bloom! Wow!",
            "Not a single pest left. You're a natural!",
            "Best harvest I've ever seen!",
        };

        private static readonly string[] DefeatLines =
        {
            "The pests won this time... but seeds always come back!",
            "Every gardener loses a few crops. Try again?",
            "Next time, harvest earlier. I believe in you!",
        };

        private const string NewRecordLine = "A new record! The soil is proud of you!";

        public static string RunStartLine()
        {
            return RunStartLines[_rng.Next(RunStartLines.Length)];
        }

        public static string EndLine(bool victory, bool newRecord, out SprigMood mood)
        {
            if (newRecord)
            {
                mood = SprigMood.Cheer;
                return NewRecordLine;
            }
            mood = victory ? SprigMood.Cheer : SprigMood.Sad;
            var pool = victory ? VictoryLines : DefeatLines;
            return pool[_rng.Next(pool.Length)];
        }
    }
}
