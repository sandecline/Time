namespace TimeMetroidvania.Player
{
    public interface IDamageable
    {
        bool TryTakeDamage(DamageInfo damageInfo);
    }
}
