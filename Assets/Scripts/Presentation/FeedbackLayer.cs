using System.Collections;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>Spawns short-lived floating "+N" popups for score feedback (spec 9.7).</summary>
    public sealed class FeedbackLayer : MonoBehaviour
    {
        private const float ScoreBackdropOpacity = 0.5f;
        private const float ScoreBackdropSpinDegreesPerSecond = 720f;
        private RectTransform _root;

        public RectTransform Build(Transform parent)
        {
            var root = UIFactory.CreateUIObject("FeedbackLayer", parent);
            UIFactory.StretchFull(root);
            var canvasGroup = root.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            _root = root;
            return root;
        }

        public const int DefaultPopupFontSize = 22;
        public const int DefaultFlyingPopupFontSize = 20;

        /// <summary>
        /// <paramref name="floatDown"/> flips the usual float-up direction to float down instead — used for
        /// popups anchored near the top of the screen, which would otherwise run off-screen.
        /// <paramref name="fontSize"/> defaults to <see cref="DefaultPopupFontSize"/>; callers pass a larger
        /// value for red subtraction text (poison-flipped score, enemy damage), which needs to read clearly
        /// at a glance and was getting lost at the same size as every other popup. The backdrop/container
        /// scale with it so a bigger font never clips.
        /// </summary>
        public void SpawnPopup(RectTransform anchor, string text, Color color, bool floatDown = false, int fontSize = DefaultPopupFontSize)
        {
            if (anchor == null)
            {
                return;
            }

            // Always render above everything else on screen, so a popup anchored behind an opaque panel still shows.
            _root.SetAsLastSibling();

            float scale = (float)fontSize / DefaultPopupFontSize;
            var container = UIFactory.CreateUIObject("PopupContainer", _root);
            // Small random horizontal jitter so several popups landing on the
            // same cell (e.g. two group-bonus hits in a row) stay legible
            // instead of perfectly overlapping.
            float jitterX = Random.Range(-16f, 16f);
            container.position = anchor.position + new Vector3(jitterX, 0f, 0f);
            container.sizeDelta = new Vector2(160f, 40f) * scale;

            // Plain square backdrop — the 45°-rotated "diamond" shape is reserved for HudView/ShopView's Lueur icon.
            var backdrop = UIFactory.CreatePanel(container, "Backdrop", UITheme.Panel);
            backdrop.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            backdrop.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            backdrop.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            backdrop.rectTransform.anchoredPosition = Vector2.zero;
            backdrop.rectTransform.sizeDelta = new Vector2(26f, 26f) * scale;
            var backdropColor = backdrop.color;
            backdropColor.a = ScoreBackdropOpacity;
            backdrop.color = backdropColor;

            var popup = UIFactory.CreateText(container, "Popup", text, fontSize, color);
            UIFactory.StretchFull(popup.rectTransform);

            StartCoroutine(AnimatePopup(container, backdrop, popup, floatDown));
        }

        /// <summary>
        /// Spawns a popup at <paramref name="fromWorldPosition"/> that flies toward <paramref name="toAnchor"/>
        /// and fades out on arrival, unlike <see cref="SpawnPopup"/>'s float-up-in-place.
        /// <paramref name="fontSize"/> defaults to <see cref="DefaultFlyingPopupFontSize"/> — see SpawnPopup's
        /// own doc comment for why callers size red subtraction text up.
        /// </summary>
        public void SpawnFlyingPopup(Vector3 fromWorldPosition, RectTransform toAnchor, string text, Color color, int fontSize = DefaultFlyingPopupFontSize)
        {
            if (toAnchor == null)
            {
                return;
            }

            _root.SetAsLastSibling();

            float scale = (float)fontSize / DefaultFlyingPopupFontSize;
            var popup = UIFactory.CreateText(_root, "FlyingPopup", text, fontSize, color);
            popup.rectTransform.position = fromWorldPosition;
            popup.rectTransform.sizeDelta = new Vector2(120f, 36f) * scale;
            StartCoroutine(AnimateFlyingPopup(popup, toAnchor));
        }

        /// <summary>
        /// A Pollen gain: the usual <see cref="SpawnFlyingPopup"/> plus a small swarm of pollen grains that
        /// puff outward from <paramref name="fromWorldPosition"/> and then get pulled into the Pollen counter
        /// at <paramref name="toAnchor"/>.
        /// </summary>
        public void SpawnPollenFlight(Vector3 fromWorldPosition, RectTransform toAnchor, string text, Color color, int grains = 7)
        {
            if (toAnchor == null)
            {
                return;
            }

            SpawnFlyingPopup(fromWorldPosition, toAnchor, text, color);
            for (int i = 0; i < grains; i++)
            {
                var grain = UIFactory.CreatePanel(_root, "PollenGrain", Color.white);
                grain.raycastTarget = false;
                grain.sprite = VisualDefaults.PollenSprite != null ? VisualDefaults.PollenSprite : BackgroundShapeFactory.GetShape(BackgroundSprite.Glow);
                if (VisualDefaults.PollenSprite == null)
                {
                    grain.color = VisualDefaults.GoldenColor;
                }
                grain.preserveAspect = true;
                float size = Random.Range(14f, 24f);
                grain.rectTransform.sizeDelta = new Vector2(size, size);
                grain.rectTransform.position = fromWorldPosition;
                StartCoroutine(AnimatePollenGrain(grain, toAnchor, Random.Range(0f, 0.15f)));
            }
        }

        /// <summary>
        /// Pollinating the garden: from every cell in <paramref name="sources"/> (a harvested group), a few
        /// glowing pollen motes sweep along curved paths toward the Pollen counter at <paramref name="toAnchor"/>,
        /// each leaving a fading trail of golden specks behind it. Purely visual.
        /// </summary>
        public void SpawnPollenStream(System.Collections.Generic.IReadOnlyList<Vector3> sources, RectTransform toAnchor, int motesPerSource = 2)
        {
            if (toAnchor == null || sources == null)
            {
                return;
            }
            _root.SetAsLastSibling();
            var glow = BackgroundShapeFactory.GetShape(BackgroundSprite.Glow);
            for (int i = 0; i < sources.Count; i++)
            {
                for (int m = 0; m < motesPerSource; m++)
                {
                    StartCoroutine(AnimatePollenMote(sources[i], toAnchor, glow, Random.Range(0f, 0.35f)));
                }
            }
        }

        private IEnumerator AnimatePollenMote(Vector3 start, RectTransform toAnchor, Sprite glow, float delay)
        {
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
            if (toAnchor == null)
            {
                yield break;
            }

            var mote = UIFactory.CreatePanel(_root, "PollenMote", new Color(1f, 0.9f, 0.45f, 1f));
            mote.raycastTarget = false;
            mote.sprite = glow;
            mote.rectTransform.sizeDelta = new Vector2(22f, 22f);
            mote.rectTransform.position = start;

            const float duration = 0.95f;
            const float trailInterval = 0.025f;
            const float trailLife = 0.5f;
            var trail = new System.Collections.Generic.List<Image>();
            var trailAge = new System.Collections.Generic.List<float>();

            // Quadratic bezier whose control point is pushed sideways and up, so the motes arc and swirl rather than travel in a straight line.
            Vector3 end = toAnchor.position;
            Vector3 dir = end - start;
            Vector3 side = new Vector3(-dir.y, dir.x, 0f).normalized * Random.Range(-1f, 1f) * Mathf.Max(60f, dir.magnitude * 0.35f);
            Vector3 control = (start + end) * 0.5f + side + Vector3.up * Random.Range(30f, 110f);
            float wobbleSeed = Random.Range(0f, 6.28f);

            float t = 0f;
            float sinceTrail = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                sinceTrail += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float e = p * p * (3f - 2f * p); // smoothstep
                if (toAnchor != null)
                {
                    end = toAnchor.position;
                }
                Vector3 pos = (1f - e) * (1f - e) * start + 2f * (1f - e) * e * control + e * e * end;
                pos += new Vector3(Mathf.Sin(wobbleSeed + t * 14f), Mathf.Cos(wobbleSeed + t * 11f), 0f) * 4f * (1f - p);
                mote.rectTransform.position = pos;
                float twinkle = 0.8f + 0.2f * Mathf.Sin(t * 30f);
                mote.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.55f, p) * twinkle;
                mote.color = new Color(1f, 0.9f, 0.45f, p < 0.85f ? 1f : 1f - (p - 0.85f) / 0.15f);

                while (sinceTrail >= trailInterval)
                {
                    sinceTrail -= trailInterval;
                    var dot = UIFactory.CreatePanel(_root, "PollenTrail", new Color(1f, 0.82f, 0.3f, 0.7f));
                    dot.raycastTarget = false;
                    dot.sprite = glow;
                    float s = Random.Range(8f, 14f);
                    dot.rectTransform.sizeDelta = new Vector2(s, s);
                    dot.rectTransform.position = pos + new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0f);
                    dot.rectTransform.SetSiblingIndex(mote.rectTransform.GetSiblingIndex());
                    trail.Add(dot);
                    trailAge.Add(0f);
                }
                UpdateTrail(trail, trailAge, trailLife);
                yield return null;
            }

            Destroy(mote.gameObject);
            while (trail.Count > 0)
            {
                UpdateTrail(trail, trailAge, trailLife);
                yield return null;
            }
        }

        private static void UpdateTrail(System.Collections.Generic.List<Image> trail, System.Collections.Generic.List<float> ages, float life)
        {
            for (int i = trail.Count - 1; i >= 0; i--)
            {
                ages[i] += Time.deltaTime;
                float p = ages[i] / life;
                if (trail[i] == null || p >= 1f)
                {
                    if (trail[i] != null)
                    {
                        Destroy(trail[i].gameObject);
                    }
                    trail.RemoveAt(i);
                    ages.RemoveAt(i);
                    continue;
                }
                trail[i].color = new Color(1f, 0.82f, 0.3f, 0.7f * (1f - p));
                trail[i].rectTransform.localScale = Vector3.one * (1f - 0.6f * p);
            }
        }

        /// <summary>
        /// A puff of <paramref name="count"/> small copies of <paramref name="sprite"/> (the per-color damage art) that
        /// spread outward with a little drag, spin, and fade — floating, with no gravity. Each lives <paramref name="life"/>
        /// seconds; <paramref name="speed"/> is the average outward speed in pixels per second.
        /// </summary>
        public void SpawnFloatingPetals(Vector3 worldPosition, Sprite sprite, int count, float minSize, float maxSize, float life, float speed)
        {
            if (sprite == null)
            {
                return;
            }
            _root.SetAsLastSibling();
            for (int i = 0; i < count; i++)
            {
                var img = UIFactory.CreatePanel(_root, "FloatingPetal", Color.white);
                img.raycastTarget = false;
                img.sprite = sprite;
                img.preserveAspect = true;
                float size = Random.Range(minSize, maxSize);
                img.rectTransform.sizeDelta = new Vector2(size, size);
                img.rectTransform.position = worldPosition;
                img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector3 velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * speed * Random.Range(0.5f, 1.2f);
                StartCoroutine(AnimateFloatingPetal(img, velocity, Random.Range(-220f, 220f), life * Random.Range(0.8f, 1.2f)));
            }
        }

        private IEnumerator AnimateFloatingPetal(Image img, Vector3 velocity, float spin, float life)
        {
            float t = 0f;
            while (t < life)
            {
                if (img == null)
                {
                    yield break;
                }
                float dt = Time.deltaTime;
                t += dt;
                float p = Mathf.Clamp01(t / life);
                velocity *= 1f - 2.2f * dt; // drag only: petals drift to a stop instead of falling
                img.rectTransform.position += velocity * dt;
                img.rectTransform.localRotation *= Quaternion.Euler(0f, 0f, spin * dt);
                img.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 0.5f, p);
                img.color = new Color(1f, 1f, 1f, 1f - p * p);
                yield return null;
            }
            if (img != null)
            {
                Destroy(img.gameObject);
            }
        }

        private IEnumerator AnimatePollenGrain(Image grain, RectTransform toAnchor, float delay)
        {
            var rect = grain.rectTransform;
            rect.localScale = Vector3.zero;
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            // Burst: drift outward in a random direction (slightly upward), easing out; then get pulled to the counter, easing in.
            const float burstDuration = 0.28f;
            const float flightDuration = 0.5f;
            Vector3 start = rect.position;
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 burstTarget = start + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle) + 0.4f, 0f) * Random.Range(28f, 70f);
            float t = 0f;
            while (t < burstDuration)
            {
                if (rect == null)
                {
                    yield break;
                }
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / burstDuration);
                float eased = 1f - (1f - p) * (1f - p);
                rect.position = Vector3.Lerp(start, burstTarget, eased);
                rect.localScale = Vector3.one * Mathf.Lerp(0f, 1.15f, eased);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 12f) * 12f);
                yield return null;
            }

            t = 0f;
            Vector3 flightStart = rect != null ? rect.position : burstTarget;
            while (t < flightDuration)
            {
                if (rect == null || toAnchor == null)
                {
                    break;
                }
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / flightDuration);
                float eased = p * p;
                rect.position = Vector3.Lerp(flightStart, toAnchor.position, eased);
                rect.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.5f, eased);
                var c = grain.color;
                c.a = 1f - Mathf.Clamp01((p - 0.75f) / 0.25f);
                grain.color = c;
                yield return null;
            }

            if (grain != null)
            {
                Destroy(grain.gameObject);
            }
        }

        /// <summary>
        /// Thief's steal: spawns a non-interactive preview of the stolen piece at <paramref name="fromWorldPosition"/>
        /// (its hand slot — already empty by the time this plays, see GameBootstrap.PlayThiefStealEffect) that
        /// flies toward <paramref name="toAnchor"/> (Thief's own icon) and fades out on arrival, same flight
        /// curve as <see cref="SpawnFlyingPopup"/>. Uses ShapePreviewFactory.Build with no trait/tooltip — the
        /// ghost is purely visual, never clickable or hoverable.
        /// </summary>
        public void SpawnFlyingPiece(Vector3 fromWorldPosition, RectTransform toAnchor, PieceShape shape, PieceColor color)
        {
            if (toAnchor == null)
            {
                return;
            }

            _root.SetAsLastSibling();

            var container = UIFactory.CreateUIObject("FlyingPiece", _root);
            container.position = fromWorldPosition;
            container.sizeDelta = new Vector2(100f, 110f);
            var canvasGroup = container.gameObject.AddComponent<CanvasGroup>();
            ShapePreviewFactory.Build(container, shape, color, null, null, null);
            StartCoroutine(AnimateFlyingPiece(container, canvasGroup, toAnchor));
        }

        private IEnumerator AnimateFlyingPiece(RectTransform rect, CanvasGroup canvasGroup, RectTransform toAnchor)
        {
            const float duration = 0.5f;
            float t = 0f;
            Vector3 startPos = rect.position;
            Vector3 startScale = rect.localScale;

            while (t < duration)
            {
                if (rect == null || toAnchor == null)
                {
                    break;
                }
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = p * p;
                rect.position = Vector3.Lerp(startPos, toAnchor.position, eased);
                rect.localScale = Vector3.Lerp(startScale, startScale * 0.4f, eased);

                float fadeP = Mathf.Clamp01((p - 0.7f) / 0.3f);
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeP);

                yield return null;
            }

            if (rect != null)
            {
                Destroy(rect.gameObject);
            }
        }

        private IEnumerator AnimateFlyingPopup(Text text, RectTransform toAnchor)
        {
            var rect = text.rectTransform;
            // Ease-in (accelerating) rather than a linear float — reads as being pulled toward a fixed destination.
            const float duration = 0.5f;
            float t = 0f;
            Vector3 startPos = rect.position;
            Color startColor = text.color;

            while (t < duration)
            {
                if (rect == null || toAnchor == null)
                {
                    break;
                }
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                float eased = p * p;
                rect.position = Vector3.Lerp(startPos, toAnchor.position, eased);

                // Only starts fading in the final stretch, so it reads clearly
                // for most of the flight and just disappears on arrival.
                float fadeP = Mathf.Clamp01((p - 0.7f) / 0.3f);
                var c = startColor;
                c.a = Mathf.Lerp(1f, 0f, fadeP);
                text.color = c;

                yield return null;
            }

            if (text != null)
            {
                Destroy(text.gameObject);
            }
        }

        private const float DamageOrbSize = 20f;
        private const float AttackSpriteSize = 52f;
        private const float DamageOrbFlightDuration = 0.5f;
        // Faster than the damage orb's own flight — this one now plays once per enemy in a sequential
        // chain (see GameBootstrap.PlayEnemyEffectsSequentially), so a slow flight here multiplies into a
        // noticeably sluggish whole sequence when several enemies are active at once.
        private const float EnemyEffectCircleFlightDuration = 0.3f;
        private const float DamageRevealDuration = 1f;
        // Each of the pulse-in (start) and pulse-out (end) ramps takes this fraction of DamageRevealDuration;
        // the middle stretch holds at rest scale so the number stays fully readable.
        private const float DamageRevealPulseFraction = 0.2f;
        private const float DamageRevealPulsePeakScale = 1.3f;

        /// <summary>
        /// Replaces a flying "-X" text popup for enemy damage: a small plain circle (no text at all) flies
        /// from <paramref name="fromWorldPosition"/> to <paramref name="toAnchor"/>, and only once it actually
        /// lands does <paramref name="damageText"/> appear — at the enemy's own position, pulsing in then back
        /// out over <see cref="DamageRevealDuration"/> — so the number reads as something that just hit the
        /// enemy rather than something flying past it mid-air. <paramref name="onArrival"/> fires at that same
        /// moment (right before the reveal starts), so a caller's own hit-react effect (e.g. HudView's icon
        /// shrink) can be synced to it instead of playing at throw time.
        /// </summary>
        public void SpawnFlyingDamageOrb(Vector3 fromWorldPosition, RectTransform toAnchor, string damageText, Color color, int fontSize = DefaultFlyingPopupFontSize, System.Action onArrival = null, Sprite attackSprite = null, Sprite petalSprite = null)
        {
            if (toAnchor == null)
            {
                return;
            }

            _root.SetAsLastSibling();

            var orbRect = UIFactory.CreateUIObject("DamageOrb", _root);
            var orbImage = orbRect.gameObject.AddComponent<Image>();
            orbImage.raycastTarget = false;
            // A per-color attack sprite (see VisualDefaults.GetAttackSprite) replaces the plain circle; it points right and is turned toward the target in flight.
            orbImage.sprite = attackSprite != null ? attackSprite : UISprites.CountBadge;
            orbImage.color = attackSprite != null ? Color.white : color;
            orbImage.preserveAspect = true;
            orbRect.sizeDelta = attackSprite != null ? new Vector2(AttackSpriteSize, AttackSpriteSize) : new Vector2(DamageOrbSize, DamageOrbSize);
            orbRect.position = fromWorldPosition;

            StartCoroutine(AnimateDamageOrb(orbRect, toAnchor, damageText, color, fontSize, onArrival, attackSprite, petalSprite));
        }

        private IEnumerator AnimateDamageOrb(RectTransform orbRect, RectTransform toAnchor, string damageText, Color color, int fontSize, System.Action onArrival, Sprite attackSprite = null, Sprite petalSprite = null)
        {
            float t = 0f;
            float shedTimer = 0f;
            Vector3 startPos = orbRect.position;

            while (t < DamageOrbFlightDuration)
            {
                if (orbRect == null || toAnchor == null)
                {
                    break;
                }
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / DamageOrbFlightDuration);
                float eased = p * p;
                orbRect.position = Vector3.Lerp(startPos, toAnchor.position, eased);
                if (attackSprite != null)
                {
                    Vector3 dir = toAnchor.position - startPos;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    orbRect.rotation = Quaternion.Euler(0f, 0f, angle);
                    // Sheds small copies of the projectile as it flies.
                    shedTimer += Time.deltaTime;
                    if (shedTimer >= 0.05f)
                    {
                        shedTimer = 0f;
                        SpawnFloatingPetals(orbRect.position, petalSprite ?? attackSprite, 1, 14f, 22f, 0.8f, 18f);
                    }
                }
                yield return null;
            }

            Vector3 arrivalPosition = toAnchor != null ? toAnchor.position : startPos;
            if (orbRect != null)
            {
                Destroy(orbRect.gameObject);
            }

            if (attackSprite != null)
            {
                SpawnFloatingPetals(arrivalPosition, petalSprite ?? attackSprite, 10, 22f, 34f, 1.1f, 150f);
            }
            onArrival?.Invoke();
            yield return StartCoroutine(AnimateDamageReveal(arrivalPosition, damageText, color, fontSize));
        }

        /// <summary>
        /// A small plain circle (no text), flying from <paramref name="fromWorldPosition"/> to
        /// <paramref name="toAnchor"/> on the same ease-in curve as <see cref="SpawnFlyingDamageOrb"/> — used
        /// for an enemy's grid effect (Locker's lock, Poisoner's poison) landing on a cell. <paramref
        /// name="onArrival"/> fires once it lands (or immediately if <paramref name="toAnchor"/> is null),
        /// so the caller can gate the actual effect's reveal on the cell behind it rather than showing it
        /// before the circle even arrives.
        /// </summary>
        public void SpawnFlyingEffectCircle(Vector3 fromWorldPosition, RectTransform toAnchor, Color color, System.Action onArrival)
        {
            if (toAnchor == null)
            {
                onArrival?.Invoke();
                return;
            }

            _root.SetAsLastSibling();

            var orbRect = UIFactory.CreateUIObject("EffectCircle", _root);
            var orbImage = orbRect.gameObject.AddComponent<Image>();
            orbImage.sprite = UISprites.CountBadge;
            orbImage.color = color;
            orbRect.sizeDelta = new Vector2(DamageOrbSize, DamageOrbSize);
            orbRect.position = fromWorldPosition;

            StartCoroutine(AnimateFlyingEffectCircle(orbRect, toAnchor, onArrival));
        }

        private IEnumerator AnimateFlyingEffectCircle(RectTransform orbRect, RectTransform toAnchor, System.Action onArrival)
        {
            float t = 0f;
            Vector3 startPos = orbRect.position;

            while (t < EnemyEffectCircleFlightDuration)
            {
                if (orbRect == null || toAnchor == null)
                {
                    break;
                }
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / EnemyEffectCircleFlightDuration);
                float eased = p * p;
                orbRect.position = Vector3.Lerp(startPos, toAnchor.position, eased);
                yield return null;
            }

            if (orbRect != null)
            {
                Destroy(orbRect.gameObject);
            }

            onArrival?.Invoke();
        }

        private IEnumerator AnimateDamageReveal(Vector3 worldPosition, string text, Color color, int fontSize)
        {
            var container = UIFactory.CreateUIObject("DamageReveal", _root);
            container.position = worldPosition;
            container.sizeDelta = new Vector2(160f, 50f);
            container.localScale = Vector3.zero;

            var label = UIFactory.CreateText(container, "DamageText", text, fontSize, color);
            UIFactory.StretchFull(label.rectTransform);

            float t = 0f;
            while (t < DamageRevealDuration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / DamageRevealDuration);
                float scale;
                if (p < DamageRevealPulseFraction)
                {
                    float inP = p / DamageRevealPulseFraction;
                    scale = inP < 0.5f
                        ? Mathf.Lerp(0f, DamageRevealPulsePeakScale, inP / 0.5f)
                        : Mathf.Lerp(DamageRevealPulsePeakScale, 1f, (inP - 0.5f) / 0.5f);
                }
                else if (p > 1f - DamageRevealPulseFraction)
                {
                    float outP = (p - (1f - DamageRevealPulseFraction)) / DamageRevealPulseFraction;
                    scale = outP < 0.5f
                        ? Mathf.Lerp(1f, DamageRevealPulsePeakScale, outP / 0.5f)
                        : Mathf.Lerp(DamageRevealPulsePeakScale, 0f, (outP - 0.5f) / 0.5f);
                }
                else
                {
                    scale = 1f;
                }
                container.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            if (container != null)
            {
                Destroy(container.gameObject);
            }
        }

        private IEnumerator AnimatePopup(RectTransform container, Image backdrop, Text text, bool floatDown)
        {
            const float duration = 1.3f;
            const float holdFraction = 0.35f; // stay fully opaque before fading
            float t = 0f;
            Vector3 startPos = container.position;
            Color textColor = text.color;
            Color backdropColor = backdrop.color;
            float verticalSign = floatDown ? -1f : 1f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / duration);
                container.position = startPos + new Vector3(0f, verticalSign * 60f * p, 0f);

                float fadeP = Mathf.Clamp01((p - holdFraction) / (1f - holdFraction));
                float alpha = Mathf.Lerp(1f, 0f, fadeP);

                var tc = textColor;
                tc.a = alpha;
                text.color = tc;

                var dc = backdropColor;
                dc.a = backdropColor.a * alpha;
                backdrop.color = dc;
                backdrop.rectTransform.Rotate(0f, 0f, ScoreBackdropSpinDegreesPerSecond * Time.deltaTime);

                yield return null;
            }

            if (container != null)
            {
                Destroy(container.gameObject);
            }
        }

        private const float ImpactLineDuration = 0.25f;
        private const float ImpactLineThickness = 6f;

        /// <summary>
        /// Brief fading streak drawn straight from <paramref name="fromWorldPosition"/> to
        /// <paramref name="toWorldPosition"/> — used when a Joker combat trait retargets damage away from
        /// the default front enemy (Range/Précision), so the player can actually see where the hit went
        /// instead of having to notice which HP bar moved. Not used for Bombe/Éclat's spread, which already
        /// reads as "everyone" without needing a directional cue.
        /// </summary>
        public void SpawnImpactLine(Vector3 fromWorldPosition, Vector3 toWorldPosition, Color color)
        {
            _root.SetAsLastSibling();

            var line = UIFactory.CreatePanel(_root, "ImpactLine", color);
            line.raycastTarget = false;
            var rt = line.rectTransform;
            Vector3 delta = toWorldPosition - fromWorldPosition;
            float distance = delta.magnitude;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(distance, ImpactLineThickness);
            rt.position = fromWorldPosition + delta * 0.5f;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            rt.rotation = Quaternion.Euler(0f, 0f, angle);
            StartCoroutine(ImpactLineRoutine(line));
        }

        private IEnumerator ImpactLineRoutine(Image line)
        {
            float t = 0f;
            Color start = line.color;
            while (t < ImpactLineDuration)
            {
                t += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(t / ImpactLineDuration);
                line.color = new Color(start.r, start.g, start.b, start.a * alpha);
                yield return null;
            }
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }
    }
}
