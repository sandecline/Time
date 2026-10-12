using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>待机与跑动共用完整背负直杆，保持长度一致，并随当前身体帧小幅摆动。</summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class PlayerCarriedWeaponVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private Sprite[] bodyOnlySprites;
        [SerializeField] private Vector2 rightHandOffset = new(2.65f, 1.75f);
        [SerializeField] private float rightCarryAngle = 45f;
        [SerializeField] private FramePose[] framePoses;

        [System.Serializable]
        private struct FramePose
        {
            public Sprite bodySprite;
            public Vector2 offset;
            public float angle;
        }

        private void LateUpdate() => RefreshVisual();

        public void RefreshVisual()
        {
            if (bodyRenderer == null || weaponRenderer == null) return;
            bool visible = false;
            Vector2 offset = rightHandOffset;
            float angle = rightCarryAngle;
            if (bodyOnlySprites != null)
                foreach (Sprite sprite in bodyOnlySprites)
                    if (sprite != null && sprite == bodyRenderer.sprite) { visible = true; break; }
            if (framePoses != null)
                foreach (FramePose pose in framePoses)
                    if (pose.bodySprite != null && pose.bodySprite == bodyRenderer.sprite)
                    {
                        visible = true;
                        offset = pose.offset;
                        angle = pose.angle;
                        break;
                    }
            weaponRenderer.enabled = visible && bodyRenderer.enabled;
            if (!visible) return;
            float facing = bodyRenderer.flipX ? -1f : 1f;
            weaponRenderer.transform.localPosition = new Vector3(offset.x * facing, offset.y, 0f);
            weaponRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, angle * facing);
            weaponRenderer.color = bodyRenderer.color;
            weaponRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            weaponRenderer.sortingOrder = bodyRenderer.sortingOrder - 1;
        }
    }
}
