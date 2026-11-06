using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>The full-screen scenery of the current challenge's world (see ChallengeAmbiance), cross-faded when a new run picks another challenge. Sits right above the flat background fill and under every drifting shape and UI element.</summary>
    public sealed class ChallengeSceneryView : MonoBehaviour
    {
        private const float FadeSeconds = 0.7f;

        private Image _front;
        private Image _back;
        private string _currentName;
        private Coroutine _fade;

        public void Build(Transform parent)
        {
            _back = CreateLayer(parent, "SceneryBack");
            _front = CreateLayer(parent, "SceneryFront");
            _back.color = new Color(1f, 1f, 1f, 0f);
            _front.color = new Color(1f, 1f, 1f, 0f);
        }

        private static Image CreateLayer(Transform parent, string name)
        {
            var image = UIFactory.CreatePanel(parent, name, Color.white);
            image.raycastTarget = false;
            image.preserveAspect = false;
            UIFactory.StretchFull(image.rectTransform);
            return image;
        }

        /// <summary>Shows <paramref name="ambiance"/>'s scenery (instantly on the first call, a cross-fade after).</summary>
        public void Apply(ChallengeAmbiance ambiance)
        {
            if (_front == null || ambiance.SceneryName == _currentName)
            {
                return;
            }
            var sprite = Resources.Load<Sprite>("Backgrounds/" + ambiance.SceneryName);
            if (sprite == null)
            {
                return;
            }
            bool first = _currentName == null;
            _currentName = ambiance.SceneryName;
            if (_fade != null)
            {
                StopCoroutine(_fade);
            }
            if (first || !isActiveAndEnabled)
            {
                _front.sprite = sprite;
                _front.color = Color.white;
                _back.color = new Color(1f, 1f, 1f, 0f);
                _fade = null;
                return;
            }
            _fade = StartCoroutine(CrossFade(sprite));
        }

        private IEnumerator CrossFade(Sprite next)
        {
            // The old scenery moves to the back layer at full strength; the new one fades in over it.
            _back.sprite = _front.sprite;
            _back.color = Color.white;
            _front.sprite = next;
            float t = 0f;
            while (t < FadeSeconds)
            {
                t += Time.deltaTime;
                _front.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / FadeSeconds));
                yield return null;
            }
            _front.color = Color.white;
            _back.color = new Color(1f, 1f, 1f, 0f);
            _fade = null;
        }
    }
}
