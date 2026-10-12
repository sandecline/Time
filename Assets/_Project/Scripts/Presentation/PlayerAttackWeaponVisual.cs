using TimeMetroidvania.Player;
using UnityEngine;

namespace TimeMetroidvania.Presentation
{
    /// <summary>攻击身体帧挂接完整指针，按握点定位，世界空间等比旋转，避免父级非等比缩放折歪轴线。</summary>
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public sealed class PlayerAttackWeaponVisual : MonoBehaviour
    {
        [System.Serializable]
        private struct GripPose
        {
            public Sprite bodySprite;
            public Vector2 grip;
            public float angle;
        }

        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer weaponRenderer;
        [SerializeField] private GripPose[] poses;
        [SerializeField, Min(0.1f)] private float weaponLength = 1.16f;
        [SerializeField, Range(0f, 1f)] private float gripFraction = 0.24f;

        private void LateUpdate() => RefreshVisual();

        public void RefreshVisual()
        {
            if (bodyRenderer == null || weaponRenderer == null || weaponRenderer.sprite == null) return;
            weaponRenderer.enabled = false;
            if (poses == null) return;
            foreach (GripPose pose in poses)
            {
                if (pose.bodySprite == null || pose.bodySprite != bodyRenderer.sprite) continue;
                float facing = bodyRenderer.flipX ? -1f : 1f;
                Vector3 grip = bodyRenderer.transform.TransformPoint(new Vector3(pose.grip.x * facing, pose.grip.y, 0f));
                Quaternion rotation = Quaternion.Euler(0f, 0f, pose.angle * facing);
                Vector3 axis = rotation * Vector3.up;
                float scale = weaponLength / weaponRenderer.sprite.bounds.size.y;
                Vector3 parentScale = weaponRenderer.transform.parent.lossyScale;
                weaponRenderer.transform.SetPositionAndRotation(grip - axis * (weaponLength * gripFraction), rotation);
                weaponRenderer.transform.localScale = new Vector3(scale / parentScale.x, scale / parentScale.y, 1f);
                weaponRenderer.color = bodyRenderer.color;
                weaponRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
                weaponRenderer.sortingOrder = bodyRenderer.sortingOrder + 1;
                weaponRenderer.enabled = bodyRenderer.enabled;
                return;
            }
        }
    }
}
