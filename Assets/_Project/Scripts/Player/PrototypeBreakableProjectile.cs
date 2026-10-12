using UnityEngine;

namespace TimeMetroidvania.Player
{
    /// <summary>
    /// 灰盒阶段的可斩弹幕；正式敌人弹幕将在对象池阶段替换该组件。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class PrototypeBreakableProjectile : MonoBehaviour, IHittableProjectile
    {
        private bool destroyed;

        public bool TryDestroyByAttack(GameObject attacker)
        {
            if (destroyed)
            {
                return false;
            }

            destroyed = true;
            Destroy(gameObject);
            return true;
        }
    }
}
