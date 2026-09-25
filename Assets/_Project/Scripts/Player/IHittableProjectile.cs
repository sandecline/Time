using UnityEngine;

namespace TimeMetroidvania.Player
{
    public interface IHittableProjectile
    {
        bool TryDestroyByAttack(GameObject attacker);
    }
}
