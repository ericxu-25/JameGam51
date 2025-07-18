using UnityEngine;

namespace Globals
{
    /// <summary>
    /// Script which scales a sprite to match a rect transform on start
    /// </summary>
    public class MatchRectTransform : MonoBehaviour
    {
        [SerializeField, Tooltip("RectTransform to match")]
        private RectTransform target;

        [SerializeField, Tooltip("Own sprite to match to RectTransform")]
        private Sprite sprite;

        private void Start()
        {
            MatchToRectTransform();
        }

        /// <summary>
        /// Attempts to match the size of the parent transform by setting own transform's local scale.
        /// </summary>
        [ContextMenu("MatchToRectTransform")]
        public void MatchToRectTransform() {
            if (target == null)
            {
                Debug.LogWarning("Cannot match to null");
            }
            Rect targetRect = target.rect;
            Rect ownRect = sprite.rect;
            float horizontalScale = targetRect.width * target.lossyScale.x / ownRect.width * sprite.pixelsPerUnit;
            float verticalScale = targetRect.height * target.lossyScale.y / ownRect.height * sprite.pixelsPerUnit;
            horizontalScale /= transform.lossyScale.x;
            verticalScale /= transform.lossyScale.y;
            transform.localScale = Vector3.Scale(transform.localScale, new Vector3(horizontalScale, verticalScale, transform.localScale.z));
            transform.position = targetRect.center + Helpers.Vec3ToVec2(target.position);
            Debug.Log("Matched sprite with size " + ownRect.size.ToString() + " to target rect of size: " + targetRect.size.ToString());
        }
    }
}
