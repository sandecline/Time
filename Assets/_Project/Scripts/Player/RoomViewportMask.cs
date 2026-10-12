using UnityEngine;

namespace TimeMetroidvania.Player
{
    /// <summary>只遮蔽当前房间可视范围之外的世界画面，不影响其他房间的玩法对象。</summary>
    [DefaultExecutionOrder(500)]
    [RequireComponent(typeof(Camera))]
    public sealed class RoomViewportMask : MonoBehaviour
    {
        private readonly SpriteRenderer[] panels = new SpriteRenderer[4];
        private Camera view;
        private Sprite pixel;
        private Bounds room;
        private bool hasRoom;

        public Bounds CurrentRoom => room;
        public bool HasRoom => hasRoom;

        private void Awake()
        {
            view = GetComponent<Camera>();
            pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f), 1f);
            for (int i = 0; i < panels.Length; i++)
            {
                GameObject panel = new($"RoomMask_{i}");
                panel.transform.SetParent(transform, false);
                panel.layer = gameObject.layer;
                SpriteRenderer renderer = panel.AddComponent<SpriteRenderer>();
                renderer.sprite = pixel;
                renderer.sortingOrder = 32000;
                panels[i] = renderer;
                panel.SetActive(false);
            }
        }

        public void SetRoom(Bounds bounds)
        {
            room = bounds;
            hasRoom = true;
            UpdatePanels();
        }

        public void ClearRoom()
        {
            hasRoom = false;
            foreach (SpriteRenderer renderer in panels)
                if (renderer != null) renderer.gameObject.SetActive(false);
        }

        private void LateUpdate() => UpdatePanels();

        private void UpdatePanels()
        {
            if (!hasRoom || view == null || !view.orthographic) return;
            float halfHeight = view.orthographicSize;
            float halfWidth = halfHeight * view.aspect;
            Vector3 cameraPosition = transform.position;
            float left = cameraPosition.x - halfWidth;
            float right = cameraPosition.x + halfWidth;
            float bottom = cameraPosition.y - halfHeight;
            float top = cameraPosition.y + halfHeight;
            float innerLeft = Mathf.Clamp(room.min.x, left, right);
            float innerRight = Mathf.Clamp(room.max.x, left, right);
            float innerBottom = Mathf.Clamp(room.min.y, bottom, top);
            float innerTop = Mathf.Clamp(room.max.y, bottom, top);
            if (innerRight < innerLeft) innerRight = innerLeft;
            if (innerTop < innerBottom) innerTop = innerBottom;

            SetPanel(0, left, innerLeft, bottom, top);
            SetPanel(1, innerRight, right, bottom, top);
            SetPanel(2, innerLeft, innerRight, bottom, innerBottom);
            SetPanel(3, innerLeft, innerRight, innerTop, top);
        }

        private void SetPanel(int index, float minX, float maxX, float minY, float maxY)
        {
            SpriteRenderer renderer = panels[index];
            if (renderer == null) return;
            bool visible = maxX - minX > 0.001f && maxY - minY > 0.001f;
            renderer.gameObject.SetActive(visible);
            if (!visible) return;
            renderer.color = view.backgroundColor;
            renderer.transform.position = new Vector3((minX + maxX) * 0.5f,
                (minY + maxY) * 0.5f, view.transform.position.z + 1f);
            renderer.transform.localScale = new Vector3(maxX - minX, maxY - minY, 1f);
        }

        private void OnDestroy()
        {
            if (pixel != null) Destroy(pixel);
        }
    }
}
