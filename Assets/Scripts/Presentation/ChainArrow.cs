using UnityEngine;
using UnityEngine.UI;

namespace Contigu.Presentation
{
    /// <summary>
    /// A hand-drawn dashed arrow from one fertilizer badge to the next one in the order (1 -> 2 -> 3 -> 4), kept between the two badges every frame
    /// (a swap or a layout change moves them). It crosses the panel diagonally where the grid wraps to its next row, which is what shows that the
    /// order, not the position, is what the fertilizers' effects and the Roots follow.
    /// </summary>
    public sealed class ChainArrow : MonoBehaviour
    {
        public RectTransform A;
        public RectTransform B;

        private RectTransform _rect;
        private RectTransform _head;
        private const float HeadSize = 22f;

        public static GameObject Create(Transform parent, RectTransform a, RectTransform b)
        {
            var line = Resources.Load<Sprite>("Icons/ChainLine");
            var head = Resources.Load<Sprite>("Icons/ChainHead");
            if (line == null || head == null)
            {
                return null;
            }
            var root = UIFactory.CreateUIObject("ChainArrow", parent);
            root.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            root.sizeDelta = new Vector2(40f, 10f);
            var lineImage = UIFactory.CreatePanel(root, "Line", new Color(1f, 1f, 1f, 0.85f));
            lineImage.sprite = line;
            lineImage.raycastTarget = false;
            UIFactory.StretchFull(lineImage.rectTransform);
            var headImage = UIFactory.CreatePanel(root, "Head", new Color(1f, 1f, 1f, 0.9f));
            headImage.sprite = head;
            headImage.preserveAspect = true;
            headImage.raycastTarget = false;
            headImage.rectTransform.anchorMin = headImage.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            headImage.rectTransform.pivot = new Vector2(0.8f, 0.5f);
            headImage.rectTransform.sizeDelta = new Vector2(HeadSize, HeadSize);
            headImage.rectTransform.anchoredPosition = Vector2.zero;
            var arrow = root.gameObject.AddComponent<ChainArrow>();
            arrow.A = a;
            arrow.B = b;
            arrow._rect = root;
            arrow._head = headImage.rectTransform;
            root.SetAsFirstSibling();   // behind the badges and the Roots chips
            return root.gameObject;
        }

        private void LateUpdate()
        {
            if (A == null || B == null || _rect == null)
            {
                return;
            }
            Vector3 from = A.TransformPoint(A.rect.center);
            Vector3 to = B.TransformPoint(B.rect.center);
            Vector3 delta = to - from;
            float scale = Mathf.Max(0.0001f, _rect.lossyScale.x);
            float distance = delta.magnitude / scale;
            // Start and end at the edge of each badge.
            float radius = Mathf.Max(A.rect.width, A.rect.height) * 0.5f * 0.85f;
            float length = distance - 2f * radius;
            bool show = length > 18f;
            if (show != _shown)
            {
                _shown = show;
                for (int i = 0; i < _rect.childCount; i++) { _rect.GetChild(i).gameObject.SetActive(show); }
            }
            if (!show)
            {
                return;
            }
            Vector3 dir = delta.normalized;
            Vector3 mid = (from + to) * 0.5f;
            _rect.position = mid;
            _rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            _rect.sizeDelta = new Vector2(length, 10f);
        }

        private bool _shown = true;
    }
}
