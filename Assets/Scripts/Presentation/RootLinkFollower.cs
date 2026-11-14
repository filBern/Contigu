using UnityEngine;

namespace Contigu.Presentation
{
    /// <summary>Keeps a Roots chip on the midpoint between two badges, every frame: the panel's layout settles after the chip is built, and a swap moves the badges.</summary>
    public sealed class RootLinkFollower : MonoBehaviour
    {
        public RectTransform A;
        public RectTransform B;
        /// <summary>Vertical shift in reference pixels (two chips on one pair sit one above the other).</summary>
        public float Offset;

        private RectTransform _rect;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void LateUpdate()
        {
            if (A == null || B == null)
            {
                return;
            }
            Vector3 mid = (A.TransformPoint(A.rect.center) + B.TransformPoint(B.rect.center)) * 0.5f;
            _rect.position = mid + new Vector3(0f, Offset * _rect.lossyScale.y, 0f);
        }
    }
}
