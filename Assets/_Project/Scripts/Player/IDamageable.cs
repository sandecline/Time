namespace TimeMetroidvania.Player
{
    /// <summary>接收一次伤害尝试。</summary>
    public interface IDamageable
    {
        /// <summary>尝试接收伤害；时停期间可先缓存，待时间恢复后结算。</summary>
        /// <param name="damageInfo">伤害来源、数值、命中点和击退信息。</param>
        /// <returns>本次伤害是否被接受，包括延后结算的伤害。</returns>
        bool TryTakeDamage(DamageInfo damageInfo);
    }
}
