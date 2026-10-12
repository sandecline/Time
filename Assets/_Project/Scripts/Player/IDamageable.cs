namespace TimeMetroidvania.Player
{
    /// <summary>接收一次伤害尝试。</summary>
    public interface IDamageable
    {
        /// <summary>尝试结算伤害；仅在本次伤害实际生效时返回真。</summary>
        /// <param name="damageInfo">伤害来源、数值、命中点和击退信息。</param>
        /// <returns>本次伤害是否实际生效。</returns>
        bool TryTakeDamage(DamageInfo damageInfo);
    }
}
