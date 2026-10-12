using UnityEngine;

namespace TimeMetroidvania.Enemy
{
    /// <summary>共用地形查询，只接受实体地形，避免水体与攻击 Trigger 遮挡视线。</summary>
    public static class WorldCollision2D
    {
        private static readonly RaycastHit2D[] Hits = new RaycastHit2D[32];
        private static readonly Collider2D[] Overlaps = new Collider2D[32];
        public static ContactFilter2D TerrainFilter => new ContactFilter2D
        {
            useLayerMask = true, layerMask = LayerMask.GetMask("Ground"), useTriggers = false
        };

        public static bool Blocks(Vector2 from, Vector2 to, float radius = 0f)
        {
            radius = Mathf.Max(0f, radius);
            // 显式检查起点，避免关闭 queriesStartInColliders 后漏掉贴墙或嵌墙弹幕。
            int overlaps = radius > 0f
                ? Physics2D.OverlapCircle(from, radius, TerrainFilter, Overlaps)
                : Physics2D.OverlapPoint(from, TerrainFilter, Overlaps);
            if (overlaps > 0) return true;
            Vector2 delta = to - from;
            if (delta.sqrMagnitude <= Mathf.Epsilon) return false;
            // 视线必须使用射线；零半径 CircleCast 在物理后端不能可靠检测墙体。
            int count = radius > 0f
                ? Physics2D.CircleCast(from, radius, delta.normalized, TerrainFilter, Hits, delta.magnitude)
                : Physics2D.Raycast(from, delta.normalized, TerrainFilter, Hits, delta.magnitude);
            return count > 0;
        }
    }
}
