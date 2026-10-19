using System.Collections;
using System.Collections.Generic;
using Contigu.Core;
using Contigu.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// The round's entrance: a band sweeps across the screen announcing "ROUND N", the weather it brings, and — for a round with bosses —
    /// a red band naming them. Plays at the start of a run and every time the shop closes.
    /// </summary>
    public sealed class RoundBannerView : MonoBehaviour
    {
        private const float BandHeight = 96f;
        private const float SlideIn = 0.34f;
        private const float SlideOut = 0.3f;
        private const float Hold = 1.15f;
        private const float BossHold = 1.7f;

        private RectTransform _root;
        private Image _band;
        private Text _title;
        private Text _subtitle;
        private Image _weatherIcon;
        private Coroutine _routine;

        public void Build(Transform parent)
        {
            _root = UIFactory.CreateUIObject("RoundBanner", parent);
            UIFactory.StretchFull(_root);
            _root.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;

            _band = UIFactory.CreatePanel(_root, "Band", UITheme.PanelLight);
            _band.raycastTarget = false;
            UIFactory.AddThickOutline(_band, UITheme.Border);
            _band.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            _band.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            _band.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _band.rectTransform.sizeDelta = new Vector2(0f, BandHeight);

            _title = UIFactory.CreateText(_band.transform, "Title", "", 46, UITheme.TextPrimary);
            _title.raycastTarget = false;
            _title.rectTransform.anchorMin = new Vector2(0f, 0.38f);
            _title.rectTransform.anchorMax = new Vector2(1f, 1f);
            _title.rectTransform.offsetMin = Vector2.zero;
            _title.rectTransform.offsetMax = Vector2.zero;

            _subtitle = UIFactory.CreateText(_band.transform, "Subtitle", "", 22, UITheme.TextMuted);
            _subtitle.raycastTarget = false;
            _subtitle.rectTransform.anchorMin = new Vector2(0f, 0f);
            _subtitle.rectTransform.anchorMax = new Vector2(1f, 0.4f);
            _subtitle.rectTransform.offsetMin = Vector2.zero;
            _subtitle.rectTransform.offsetMax = Vector2.zero;

            _weatherIcon = UIFactory.CreatePanel(_band.transform, "WeatherIcon", Color.white);
            _weatherIcon.raycastTarget = false;
            _weatherIcon.preserveAspect = true;
            _weatherIcon.rectTransform.anchorMin = _weatherIcon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _weatherIcon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _weatherIcon.rectTransform.sizeDelta = new Vector2(64f, 64f);

            _root.gameObject.SetActive(false);
        }

        /// <summary>Announces <paramref name="run"/>'s current round.</summary>
        public void Play(RunManager run)
        {
            var bosses = new List<string>();
            if (run.HasActiveEncounter)
            {
                foreach (var enemy in run.CurrentEncounter)
                {
                    if (enemy.Definition.Role == EnemyRole.Boss)
                    {
                        bosses.Add(enemy.Definition.Name);
                    }
                }
            }
            bool boss = bosses.Count > 0 || run.IsBossRound;
            var weather = run.CurrentWeather;
            _title.text = "ROUND " + run.CurrentRoundNumber + (boss ? " - BOSS" : "");
            _subtitle.text = (boss && bosses.Count > 0 ? string.Join(" + ", bosses) + "  ·  " : "") + WeatherCatalog.GetName(weather);
            _band.color = boss ? new Color(0.95f, 0.62f, 0.55f, 1f) : UITheme.PanelLight;
            _weatherIcon.sprite = VisualDefaults.GetWeatherSprite(weather);
            _weatherIcon.enabled = _weatherIcon.sprite != null;
            // Icon sits at the right end of the band text.
            _weatherIcon.rectTransform.anchorMin = _weatherIcon.rectTransform.anchorMax = new Vector2(0.78f, 0.5f);
            _weatherIcon.rectTransform.anchoredPosition = Vector2.zero;

            if (_routine != null)
            {
                StopCoroutine(_routine);
            }
            _routine = StartCoroutine(Slide(boss ? BossHold : Hold));
        }

        private IEnumerator Slide(float hold)
        {
            _root.gameObject.SetActive(true);
            float width = Mathf.Max(1280f, _root.rect.width);
            var band = _band.rectTransform;
            float t = 0f;
            while (t < SlideIn)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / SlideIn);
                float eased = 1f - (1f - p) * (1f - p) * (1f - p);
                band.anchoredPosition = new Vector2(Mathf.Lerp(-width, 0f, eased), 0f);
                yield return null;
            }
            band.anchoredPosition = Vector2.zero;
            yield return new WaitForSeconds(hold);
            t = 0f;
            while (t < SlideOut)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / SlideOut);
                band.anchoredPosition = new Vector2(Mathf.Lerp(0f, width, p * p), 0f);
                yield return null;
            }
            _root.gameObject.SetActive(false);
            _routine = null;
        }
    }
}
