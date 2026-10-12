using UnityEngine;

namespace TimeMetroidvania.Player
{
    public readonly struct DamageInfo
    {
        public DamageInfo(GameObject source, int amount, Vector2 hitPoint, Vector2 knockback)
        {
            Source = source;
            Amount = amount;
            HitPoint = hitPoint;
            Knockback = knockback;
        }

        public GameObject Source { get; }
        public int Amount { get; }
        public Vector2 HitPoint { get; }
        public Vector2 Knockback { get; }
    }
}
